/* 
 * $Id$
 * Copyright 2008 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net>
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

using System.Threading;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Eraser.Util;

namespace Eraser.Manager
{
	/// <summary>
	/// An interface class for all pseudorandom number generators used for the
	/// random data erase passes.
	/// </summary>
	public abstract class Prng
	{
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// The name of this erase pass, used for display in the UI
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// The GUID for this PRNG.
		/// </summary>
		public abstract Guid Guid
		{
			get;
		}

		/// <summary>
		/// Reseeds the PRNG. This can be called by inherited classes, but its most
		/// important function is to provide new seeds regularly. The PRNGManager
		/// will call this function once in a whle to maintain the quality of
		/// generated numbers.
		/// </summary>
		/// <param name="seed">An arbitrary length of information that will be
		/// used to reseed the PRNG</param>
		protected internal abstract void Reseed(byte[] seed);

		#region Random members
		/// <summary>
		/// Returns a nonnegative random number less than the specified maximum.
		/// </summary>
		/// <param name="maxValue">The exclusive upper bound of the random number
		/// to be generated. maxValue must be greater than or equal to zero.</param>
		/// <returns>A 32-bit signed integer greater than or equal to zero, and
		/// less than maxValue; that is, the range of return values ordinarily
		/// includes zero but not maxValue. However, if maxValue equals zero,
		/// maxValue is returned.</returns>
		public int Next(int maxValue)
		{
			if (maxValue == 0)
				return 0;
			return Next() % maxValue;
		}

		/// <summary>
		/// Returns a random number within a specified range.
		/// </summary>
		/// <param name="minValue">The inclusive lower bound of the random number
		/// returned.</param>
		/// <param name="maxValue">The exclusive upper bound of the random number
		/// returned. maxValue must be greater than or equal to minValue.</param>
		/// <returns>A 32-bit signed integer greater than or equal to minValue and
		/// less than maxValue; that is, the range of return values includes minValue
		/// but not maxValue. If minValue equals maxValue, minValue is returned.</returns>
		public int Next(int minValue, int maxValue)
		{
			if (minValue > maxValue)
				throw new ArgumentOutOfRangeException("minValue", minValue,
					S._("minValue is greater than maxValue"));
			else if (minValue == maxValue)
				return minValue;
			return (Next() % (maxValue - minValue)) + minValue;
		}

		/// <summary>
		/// Returns a nonnegative random number.
		/// </summary>
		/// <returns>A 32-bit signed integer greater than or equal to zero and less
		/// than System.Int32.MaxValue.</returns>
		public unsafe int Next()
		{
			//Declare a return variable
			int result;
			int* fResult = &result;

			//Get the random-valued bytes to fill the int.
			byte[] rand = new byte[sizeof(int)];
			NextBytes(rand);

			//Copy the random buffer into the int.
			fixed (byte* fRand = rand)
			{
				byte* pResult = (byte*)fResult;
				byte* pRand = fRand;
				for (int i = 0; i != sizeof(int); ++i)
					*pResult++ = *pRand++;
			}

			return Math.Abs(result);
		}

		/// <summary>
		/// Fills the elements of a specified array of bytes with random numbers.
		/// </summary>
		/// <param name="buffer">An array of bytes to contain random numbers.</param>
		public abstract void NextBytes(byte[] buffer);
		#endregion
	}

	/// <summary>
	/// Class managing all the PRNG algorithms.
	/// </summary>
	public class PrngManager
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public PrngManager()
		{
		}

		/// <summary>
		/// Retrieves all currently registered erasure methods.
		/// </summary>
		/// <returns>A mutable list, with an instance of each PRNG.</returns>
		public static Dictionary<Guid, Prng> Items
		{
			get
			{
				lock (ManagerLibrary.Instance.PRNGManager.prngs)
					return ManagerLibrary.Instance.PRNGManager.prngs;
			}
		}

		/// <summary>
		/// Retrieves the instance of the PRNG with the given GUID.
		/// </summary>
		/// <param name="value">The GUID of the PRNG.</param>
		/// <returns>The PRNG instance.</returns>
		public static Prng GetInstance(Guid value)
		{
			lock (ManagerLibrary.Instance.PRNGManager.prngs)
			{
				if (!ManagerLibrary.Instance.PRNGManager.prngs.ContainsKey(value))
					throw new PrngNotFoundException(value);
				return ManagerLibrary.Instance.PRNGManager.prngs[value];
			}
		}

		/// <summary>
		/// Allows plug-ins to register PRNGs with the main program. Thread-safe.
		/// </summary>
		/// <param name="method"></param>
		public static void Register(Prng prng)
		{
			lock (ManagerLibrary.Instance.PRNGManager.prngs)
				ManagerLibrary.Instance.PRNGManager.prngs.Add(prng.Guid, prng);
		}

		/// <summary>
		/// Allows the EntropyThread to get entropy to the PRNG functions as seeds.
		/// </summary>
		/// <param name="entropy">An array of bytes, being entropy for the PRNG.</param>
		internal void AddEntropy(byte[] entropy)
		{
			lock (ManagerLibrary.Instance.PRNGManager.prngs)
				foreach (Prng prng in prngs.Values)
					prng.Reseed(entropy);
		}

		/// <summary>
		/// Gets entropy from the EntropyThread.
		/// </summary>
		/// <returns>A buffer of arbitrary length containing random information.</returns>
		public static byte[] GetEntropy()
		{
			return ManagerLibrary.Instance.EntropySourceManager.Poller.GetPool();
		}

		/// <summary>
		/// The list of currently registered erasure methods.
		/// </summary>
		private Dictionary<Guid, Prng> prngs = new Dictionary<Guid, Prng>();
	}
}
