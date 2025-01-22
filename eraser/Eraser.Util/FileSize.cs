/* 
 * $Id: FileSize.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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

using System.Globalization;

namespace Eraser.Util
{
	/// <summary>
	/// Gets the human-readable representation of a file size from the byte-wise
	/// length of a file. This returns a KB = 1024 bytes (Windows convention.)
	/// </summary>
	public struct FileSize : IConvertible
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filesize">The size of the file, in bytes.</param>
		public FileSize(long filesize)
			: this()
		{
			Size = filesize;
		}

		#region IConvertible Members

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int64;
		}

		public bool ToBoolean(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}

		public byte ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(Size);
		}

		public char ToChar(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}

		public DateTime ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}

		public decimal ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(Size);
		}

		public double ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(Size);
		}

		public short ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(Size);
		}

		public int ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(Size);
		}

		public long ToInt64(IFormatProvider provider)
		{
			return Size;
		}

		public sbyte ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(Size);
		}

		public float ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(Size);
		}

		public string ToString(IFormatProvider provider)
		{
			return ToString(Size);
		}

		public object ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType(Size, conversionType, provider);
		}

		public ushort ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(Size);
		}

		public uint ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(Size);
		}

		public ulong ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(Size);
		}

		#endregion

		/// <summary>
		/// The size of the file, in bytes.
		/// </summary>
		public long Size
		{
			get;
			private set;
		}

		/// <summary>
		/// Converts this file size to the concise equivalent.
		/// </summary>
		/// <returns>A string containing the file size and the associated unit.
		/// Files larger than 1MB will be accurate to 2 decimal places.</returns>
		public override string ToString()
		{
			return ToString(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Converts a file size to the concise equivalent.
		/// </summary>
		/// <param name="size">The size of the file to convert.</param>
		/// <returns>A string containing the file size and the associated unit.
		/// Files larger than 1MB will be accurate to 2 decimal places.</returns>
		public static string ToString(long size)
		{
			//List of units, in ascending scale
			string[] units = new string[] {
				S._("bytes"),
				S._("KB"),
				S._("MB"),
				S._("GB"),
				S._("TB"),
				S._("PB"),
				S._("EB")
			};

			double dSize = (double)size;
			for (int i = 0; i != units.Length; ++i)
			{
				if (dSize < 1000.0)
					if (i <= 1)
						return string.Format(CultureInfo.CurrentCulture,
							"{0} {1}", (int)dSize, units[i]);
					else
						return string.Format(CultureInfo.CurrentCulture,
							"{0:0.00} {1}", dSize, units[i]);
				dSize /= 1024.0;
			}

			return string.Format(CultureInfo.CurrentCulture, "{0, 2} {1}",
				dSize, units[units.Length - 1]);
		}
	}
}
