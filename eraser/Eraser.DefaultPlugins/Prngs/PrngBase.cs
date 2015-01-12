/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	abstract class PrngBase : IPrng
	{
		public override string ToString()
		{
			return Name;
		}

		#region IPrng Members

		public abstract string Name
		{
			get;
		}

		public abstract void Reseed(byte[] seed);

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
					"minValue is greater than maxValue");
			else if (minValue == maxValue)
				return minValue;
			return (Next() % (maxValue - minValue)) + minValue;
		}

		/// <summary>
		/// Returns a nonnegative random number.
		/// </summary>
		/// <returns>A 32-bit signed integer greater than or equal to zero and less
		/// than System.Int32.MaxValue.</returns>
		public int Next()
		{
			//Get the random-valued bytes to fill the int.
			byte[] rand = new byte[sizeof(int)];
			NextBytes(rand);

			//Then return the integral representation of the buffer.
			return Math.Abs(BitConverter.ToInt32(rand, 0));
		}

		/// <summary>
		/// Returns a random number between 0.0 and 1.0.
		/// </summary>
		/// <returns>A double-precision floating point number greater than or equal to 0.0,
		/// and less than 1.0.</returns>
		public double NextDouble()
		{
			//Get the random-valued bytes to fill the double.
			byte[] rand = new byte[sizeof(double)];
			NextBytes(rand);

			//Then return the absolute double representation of the buffer.
			double result = Math.Abs(BitConverter.ToDouble(rand, 0));

			//Make the result within 0.0 and 1.0.
			result = Math.Log10(result);
			while (result > 1.0)
				result /= 10.0;
			return result;
		}

		/// <summary>
		/// Fills the elements of a specified array of bytes with random numbers.
		/// </summary>
		/// <param name="buffer">An array of bytes to contain random numbers.</param>
		public abstract void NextBytes(byte[] buffer);

		#endregion

		#region IRegisterable Members

		public abstract Guid Guid
		{
			get;
		}

		#endregion
	}
}
