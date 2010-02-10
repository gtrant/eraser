/* 
 * $Id$
 * Copyright 2008-2010 The Eraser Project
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
using System.Text;
using System.IO;
using System.Reflection;
using Eraser.Util;

namespace Eraser.Manager
{
	/// <summary>
	/// An interface class representing the method for erasure. If classes only
	/// inherit this class, then the method can only be used to erase abstract
	/// streams, not unused drive space.
	/// </summary>
	public abstract class ErasureMethod : IRegisterable
	{
		public override string ToString()
		{
			if (Passes == 0)
				return Name;
			return Passes == 1 ? S._("{0} (1 pass)", Name) :
				S._("{0} ({1} passes)", Name, Passes);
		}

		/// <summary>
		/// The name of this erase pass, used for display in the UI
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// The number of erase passes for this erasure method.
		/// </summary>
		public abstract int Passes
		{
			get;
		}

		/// <summary>
		/// The GUID for this erasure method.
		/// </summary>
		public abstract Guid Guid
		{
			get;
		}

		/// <summary>
		/// Calculates the total size of the erasure data that needs to be written.
		/// This is mainly for use by the Manager to determine how much data needs
		/// to be written to disk.
		/// </summary>
		/// <param name="paths">The list containing the file paths to erase. This
		/// may be null if the list of paths are unknown.</param>
		/// <param name="targetSize">The precomputed value of the total size of
		/// the files to be erased.</param>
		/// <returns>The total size of the files that need to be erased.</returns>
		/// <remarks>This function MAY be slow. Most erasure methods can
		/// calculate this amount fairly quickly as the number of files and the
		/// total size of the files (the ones that take most computation time)
		/// are already provided. However some exceptional cases may take a
		/// long time if the data set is large.</remarks>
		public abstract long CalculateEraseDataSize(ICollection<string> paths, long targetSize);

		/// <summary>
		/// The main bit of the class! This function is called whenever data has
		/// to be erased. Erase the stream passed in, using the given PRNG for
		/// randomness where necessary.
		/// 
		/// This function should be implemented thread-safe as using the same
		/// instance, this function may be called across different threads.
		/// </summary>
		/// <param name="stream">The stream which needs to be erased.</param>
		/// <param name="erasureLength">The length of the stream to erase. If all
		/// data in the stream should be overwritten, then pass in the maximum
		/// value for long, the function will take the minimum.</param>
		/// <param name="prng">The PRNG source for random data.</param>
		/// <param name="callback">The progress callback function.</param>
		public abstract void Erase(Stream stream, long erasureLength, Prng prng,
			ErasureMethodProgressFunction callback);

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
			Prng rand = ManagerLibrary.Instance.PrngRegistrar[ManagerLibrary.Settings.ActivePrng];
			for (int i = 0; i < result.Length; ++i)
			{
				int val = rand.Next(result.Length - 1);
				ErasureMethodPass tmpPass = result[val];
				result[val] = result[i];
				result[i] = tmpPass;
			}

			return result;
		}

		/// <summary>
		/// Helper function. This function will write random data to the stream
		/// using the provided PRNG.
		/// </summary>
		/// <param name="strm">The buffer to populate with data to write to disk.</param>
		/// <param name="prng">The PRNG used.</param>
		public static void WriteRandom(byte[] buffer, object value)
		{
			((Prng)value).NextBytes(buffer);
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
	}

	/// <summary>
	/// A simple callback for clients to retrieve progress information from
	/// the erase method.
	/// </summary>
	/// <param name="lastWritten">The amount of data written to the stream since
	/// the last call to the delegate.</param>
	/// <param name="totalData">The total amount of data that must be written to
	/// complete the erasure.</param>
	/// <param name="currentPass">The current pass number. The total number
	/// of passes can be found from the Passes property.</param>
	public delegate void ErasureMethodProgressFunction(long lastWritten, long totalData,
		int currentPass);

	/// <summary>
	/// A pass object. This object holds both the pass function, as well as the
	/// data used for the pass (random, byte, or triplet)
	/// </summary>
	public class ErasureMethodPass
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
		public void Execute(byte[] buffer, Prng prng)
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
	}

	/// <summary>
	/// The prototype of a pass.
	/// </summary>
	/// <param name="strm">The buffer to populate with data to write to disk.</param>
	/// <param name="opaque">An opaque value, depending on the type of callback.</param>
	public delegate void ErasureMethodPassFunction(byte[] buffer, object opaque);

	/// <summary>
	/// This class adds functionality to the ErasureMethod class to erase
	/// unused drive space.
	/// </summary>
	public abstract class UnusedSpaceErasureMethod : ErasureMethod
	{
		/// <summary>
		/// This function will allow clients to erase a file in a set of files
		/// used to fill the disk, thus achieving disk unused space erasure.
		/// 
		/// By default, this function will simply call the Erase method inherited
		/// from the ErasureMethod class.
		/// 
		/// This function should be implemented thread-safe as using the same
		/// instance, this function may be called across different threads.
		/// </summary>
		/// <param name="strm">The stream which needs to be erased.</param>
		/// <param name="prng">The PRNG source for random data.</param>
		/// <param name="callback">The progress callback function.</param>
		public virtual void EraseUnusedSpace(Stream stream, Prng prng, ErasureMethodProgressFunction callback)
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
	/// erase unused drive space as well.
	/// </summary>
	public abstract class PassBasedErasureMethod : UnusedSpaceErasureMethod
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

		public override long CalculateEraseDataSize(ICollection<string> paths, long targetSize)
		{
			//Simple. Amount of data multiplied by passes.
			return targetSize * Passes;
		}

		public override void Erase(Stream stream, long erasureLength, Prng prng,
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
	}

	/// <summary>
	/// Class managing all the erasure methods. This class pairs GUIDs with constructor
	/// prototypes, and when an instance of the erasure method is required, a new
	/// instance is created. This is unique to erasure methods since the other managers
	/// do not have run-time equivalents; they all are compile-time.
	/// </summary>
	public class ErasureMethodRegistrar : Registrar<ErasureMethod>
	{
		#region Default Erasure method
		private class DefaultMethod : ErasureMethod
		{
			public DefaultMethod()
			{
			}

			public override string Name
			{
				get { return S._("(default)"); }
			}

			public override int Passes
			{
				get { return 0; }
			}

			public override Guid Guid
			{
				get { return Guid.Empty; }
			}

			public override long CalculateEraseDataSize(ICollection<string> paths, long targetSize)
			{
				throw new InvalidOperationException("The DefaultMethod class should never " +
					"be used and should instead be replaced before execution!");
			}

			public override void Erase(Stream strm, long erasureLength, Prng prng,
				ErasureMethodProgressFunction callback)
			{
				throw new InvalidOperationException("The DefaultMethod class should never " +
					"be used and should instead be replaced before execution!");
			}
		}

		/// <summary>
		/// A dummy method placeholder used for representing the default erase
		/// method. Do not use this variable when trying to call the erase function,
		/// this is just a placeholder and will throw a InvalidOperationException.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ErasureMethod Default = new DefaultMethod();
		#endregion
	}

	public class ErasureMethodRegistrationEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="value">The GUID of the newly registered/unregistered
		/// erasure method.</param>
		public ErasureMethodRegistrationEventArgs(Guid value)
		{
			Guid = value;
		}

		/// <summary>
		/// The GUID of the newly registsered/unregistered erasure method.
		/// </summary>
		public Guid Guid { get; private set; }
	}
}
