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
using System.Linq;
using System.Text;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// An interface class for all pseudorandom number generators used for the
	/// random data erase passes.
	/// </summary>
	public interface IPrng : IRegisterable
	{
		/// <summary>
		/// Returns a string that represents the current IPrng. The suggsted return
		/// value is the Name of the IPrng.
		/// </summary>
		/// <returns>The string that represents the current IPrng.</returns>
		string ToString();

		/// <summary>
		/// The name of this erase pass, used for display in the UI
		/// </summary>
		string Name
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
		void Reseed(byte[] seed);

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
		int Next(int maxValue);

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
		int Next(int minValue, int maxValue);

		/// <summary>
		/// Returns a nonnegative random number.
		/// </summary>
		/// <returns>A 32-bit signed integer greater than or equal to zero and less
		/// than System.Int32.MaxValue.</returns>
		int Next();

		/// <summary>
		/// Returns a random number between 0.0 and 1.0.
		/// </summary>
		/// <returns>A double-precision floating point number greater than or equal to 0.0,
		/// and less than 1.0.</returns>
		double NextDouble();

		/// <summary>
		/// Fills the elements of a specified array of bytes with random numbers.
		/// </summary>
		/// <param name="buffer">An array of bytes to contain random numbers.</param>
		void NextBytes(byte[] buffer);
		#endregion
	}
}
