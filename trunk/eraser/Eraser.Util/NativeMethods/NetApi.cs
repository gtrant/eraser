/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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
using System.Runtime.InteropServices;
using System.Drawing;

namespace Eraser.Util
{
	internal static partial class NativeMethods
	{
		[DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
		public static extern uint NetStatisticsGet(string server, string service,
			uint level, uint options, out IntPtr bufptr);

		/// <summary>
		/// The NetApiBufferSize function returns the size, in bytes, of a buffer
		/// allocated by a call to the NetApiBufferAllocate function.
		/// </summary>
		/// <param name="Buffer">Pointer to a buffer returned by the NetApiBufferAllocate
		/// function.</param>
		/// <param name="ByteCount">Receives the size of the buffer, in bytes.</param>
		/// <returns>If the function succeeds, the return value is NERR_Success.
		/// 
		/// If the function fails, the return value is a system error code. For
		/// a list of error codes, see System Error Codes.</returns>
		[DllImport("Netapi32.dll")]
		public static extern uint NetApiBufferSize(IntPtr Buffer, out uint ByteCount);

		/// <summary>
		/// The NetApiBufferFree function frees the memory that the NetApiBufferAllocate
		/// function allocates. Call NetApiBufferFree to free the memory that other
		/// network management functions return.
		/// </summary>
		/// <param name="Buffer">Pointer to a buffer returned previously by another
		/// network management function.</param>
		/// <returns>If the function succeeds, the return value is NERR_Success.
		/// 
		/// If the function fails, the return value is a system error code. For
		/// a list of error codes, see System Error Codes.</returns>
		[DllImport("Netapi32.dll")]
		public static extern uint NetApiBufferFree(IntPtr Buffer);

		private const uint NERR_Success = 0;
	}
}
