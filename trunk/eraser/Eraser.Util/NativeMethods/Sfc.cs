/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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

namespace Eraser.Util
{
	internal static partial class NativeMethods
	{
		/// <summary>
		/// Determines whether the specified file is protected. Applications
		/// should avoid replacing protected system files.
		/// </summary>
		/// <param name="RpcHandle">This parameter must be NULL.</param>
		/// <param name="ProtFileName">The name of the file.</param>
		/// <returns>If the file is protected, the return value is true.</returns>
		[DllImport("Sfc.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SfcIsFileProtected(IntPtr RpcHandle,
			string ProtFileName);
	}
}
