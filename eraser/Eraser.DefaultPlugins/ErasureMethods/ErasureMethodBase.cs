/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: 
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	abstract class ErasureMethodBase : IErasureMethod
	{
		public override string ToString()
		{
			if (Passes == 0)
				return Name;
			return Passes == 1 ? S._("{0} (1 pass)", Name) :
				S._("{0} ({1} passes)", Name, Passes);
		}

		/// <summary>
		/// Helper function. This function will write random data to the stream
		/// using the provided PRNG.
		/// </summary>
		/// <param name="strm">The buffer to populate with data to write to disk.</param>
		/// <param name="prng">The PRNG used.</param>
		public static void WriteRandom(byte[] buffer, object value)
		{
			((IPrng)value).NextBytes(buffer);
		}

		/// <summary>
		/// Helper function. This function will write the repeating pass constant.
		/// to the provided buffer.
		/// </summary>
		/// <param name="strm">The buffer to populate with data to write to disk.</param>
		/// <param name="value">The byte[] to write.</param>
		public static void WriteConstant(byte[] buffer, object value)
		{
			byte[] constant = (byte[])value;
			for (int i = 0; i < buffer.Length; ++i)
				buffer[i] = constant[i % constant.Length];
		}

		#region IErasureMethod Members

		public abstract string Name
		{
			get;
		}

		public abstract int Passes
		{
			get;
		}

		public abstract long CalculateEraseDataSize(ICollection<StreamInfo> paths,
			long targetSize);

		public abstract void Erase(Stream stream, long erasureLength, IPrng prng,
			ErasureMethodProgressFunction callback);

		#endregion

		#region IRegisterable Members

		public abstract Guid Guid
		{
			get;
		}

		#endregion
	}

	abstract class DriveErasureMethodBase : ErasureMethodBase, IDriveErasureMethod
	{
		public virtual void EraseDriveSpace(Stream stream, IPrng prng,
			ErasureMethodProgressFunction callback)
		{
			Erase(stream, long.MaxValue, prng, callback);
		}
	}

	/// <summary>
	/// Pass-based erasure method. This subclass of erasure methods follow a fixed
	/// pattern (constant or random data) for every pass, although the order of
	/// passes can be randomized. This is to simplify definitions of classes in
	/// plugins.
	/// 
	/// Since instances of this class apply data by passes, they can by default
	/// erase drives as well.
	/// </summary>
	abstract class PassBasedErasureMethod : DriveErasureMethodBase
	{
		public override int Passes
		{
			get { return PassesSet.Length; }
		}

		/// <summary>
		/// Whether the passes should be randomized before running them in random
		/// order.
		/// </summary>
		protected abstract bool RandomizePasses
		{
			get;
		}

		/// <summary>
		/// The set of Pass objects describing the passes in this erasure method.
		/// </summary>
		protected abstract ErasureMethodPass[] PassesSet
		{
			get;
		}

		public override long CalculateEraseDataSize(ICollection<StreamInfo> paths, long targetSize)
		{
			//Simple. Amount of data multiplied by passes.
			return targetSize * Passes;
		}

		/// <summary>
		/// Shuffles the passes in the input array, effectively randomizing the
		/// order or rewrites.
		/// </summary>
		/// <param name="passes">The input set of passes.</param>
		/// <returns>The shuffled set of passes.</returns>
		protected static ErasureMethodPass[] ShufflePasses(ErasureMethodPass[] passes)
		{
			//Make a copy.
			ErasureMethodPass[] result = new ErasureMethodPass[passes.Length];
			passes.CopyTo(result, 0);

			//Randomize.
			IPrng rand = Host.Instance.Prngs.ActivePrng;
			for (int i = 0; i < result.Length; ++i)
			{
				int val = rand.Next(result.Length - 1);
				ErasureMethodPass tmpPass = result[val];
				result[val] = result[i];
				result[i] = tmpPass;
			}

			return result;
		}

		public override void Erase(Stream stream, long erasureLength, IPrng prng,
			ErasureMethodProgressFunction callback)
		{
			//Randomize the order of the passes
			ErasureMethodPass[] randomizedPasses = PassesSet;
			if (RandomizePasses)
				randomizedPasses = ShufflePasses(randomizedPasses);

			//Remember the starting position of the stream.
			long strmStart = stream.Position;
			long strmLength = Math.Min(stream.Length - strmStart, erasureLength);
			long totalData = CalculateEraseDataSize(null, strmLength);

			//Allocate memory for a buffer holding data for the pass.
			byte[] buffer = new byte[Math.Min(DiskOperationUnit, strmLength)];

			//Run every pass!
			for (int pass = 0; pass < Passes; ++pass)
			{
				//Do a progress callback first.
				if (callback != null)
					callback(0, totalData, pass + 1);

				//Start from the beginning again
				stream.Seek(strmStart, SeekOrigin.Begin);

				//Write the buffer to disk.
				long toWrite = strmLength;
				int dataStopped = buffer.Length;
				while (toWrite > 0)
				{
					//Calculate how much of the buffer to write to disk.
					int amount = (int)Math.Min(toWrite, buffer.Length - dataStopped);

					//If we have no data left, get more!
					if (amount == 0)
					{
						randomizedPasses[pass].Execute(buffer, prng);
						dataStopped = 0;
						continue;
					}

					//Write the data.
					stream.Write(buffer, dataStopped, amount);
					stream.Flush();
					toWrite -= amount;

					//Do a progress callback.
					if (callback != null)
						callback(amount, totalData, pass + 1);
				}
			}
		}

		/// <summary>
		/// Disk operation write unit. Chosen such that this value mod 3, 4, 512,
		/// and 1024 is 0
		/// </summary>
		public const int DiskOperationUnit = 1536 * 4096;

		/// <summary>
		/// Unused space erasure file size. Each of the files used in erasing
		/// unused space will be of this size.
		/// </summary>
		public const int FreeSpaceFileUnit = DiskOperationUnit * 36;
	}

	/// <summary>
	/// A pass object. This object holds both the pass function, as well as the
	/// data used for the pass (random, byte, or triplet)
	/// </summary>
	class ErasureMethodPass
	{
		public override string ToString()
		{
			return OpaqueValue == null ? S._("Random") : OpaqueValue.ToString();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="function">The delegate to the function.</param>
		/// <param name="opaqueValue">The opaque value passed to the function.</param>
		public ErasureMethodPass(ErasureMethodPassFunction function, object opaqueValue)
		{
			Function = function;
			OpaqueValue = opaqueValue;
		}

		/// <summary>
		/// Executes the pass.
		/// </summary>
		/// <param name="buffer">The buffer to populate with the data to write.</param>
		/// <param name="prng">The PRNG used for random passes.</param>
		public void Execute(byte[] buffer, IPrng prng)
		{
			Function(buffer, OpaqueValue == null ? prng : OpaqueValue);
		}

		/// <summary>
		/// The function to execute for this pass.
		/// </summary>
		public ErasureMethodPassFunction Function { get; set; }

		/// <summary>
		/// The value to be passed to the executing function.
		/// </summary>
		public object OpaqueValue { get; set; }

		/// <summary>
		/// The prototype of a pass.
		/// </summary>
		/// <param name="strm">The buffer to populate with data to write to disk.</param>
		/// <param name="opaque">An opaque value, depending on the type of callback.</param>
		public delegate void ErasureMethodPassFunction(byte[] buffer, object opaque);
	}
}
