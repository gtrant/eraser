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
using System.Diagnostics;
using System.IO;

namespace Eraser.Util
{
	public static class MiniDump
	{
		/// <summary>
		/// Dumps an application minidump of the current process to the provided stream.
		/// </summary>
		/// <param name="stream">The stream to write the minidump to.</param>
		public static void Dump(FileStream stream)
		{
			//Store the exception information
			NativeMethods.MiniDumpExceptionInfo exception = new NativeMethods.MiniDumpExceptionInfo() { ClientPointers = false, ExceptionPointers = Marshal.GetExceptionPointers(), ThreadId = (uint)System.Threading.Thread.CurrentThread.ManagedThreadId};

			NativeMethods.MiniDumpWriteDump(Process.GetCurrentProcess().Handle,
				(uint)Process.GetCurrentProcess().Id, stream.SafeFileHandle,
				NativeMethods.MiniDumpType.MiniDumpWithFullMemory,
				ref exception, IntPtr.Zero, IntPtr.Zero);
		}
	}
}
