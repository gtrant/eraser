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
using Microsoft.Win32.SafeHandles;

namespace Eraser.Util
{
	/// <summary>
	/// Stores DLL references for this class.
	/// </summary>
	internal static partial class NativeMethods
	{
		/// <summary>
		/// Writes user-mode minidump information to the specified file.
		/// </summary>
		/// <param name="hProcess">A handle to the process for which the information
		/// is to be generated.</param>
		/// <param name="ProcessId">The identifier of the process for which the information
		/// is to be generated.</param>
		/// <param name="hFile">A handle to the file in which the information is to be
		/// written.</param>
		/// <param name="DumpType">The type of information to be generated. This parameter
		/// can be one or more of the values from the MINIDUMP_TYPE enumeration.</param>
		/// <param name="ExceptionParam">A pointer to a MiniDumpExceptionInfo structure
		/// describing the client exception that caused the minidump to be generated.
		/// If the value of this parameter is NULL, no exception information is included
		/// in the minidump file.</param>
		/// <param name="UserStreamParam">Not supported. Use IntPtr.Zero</param>
		/// <param name="CallbackParam">Not supported. Use IntPtr.Zero</param>
		/// <returns>If the function succeeds, the return value is true; otherwise, the
		/// return value is false. To retrieve extended error information, call GetLastError.
		/// Note that the last error will be an HRESULT value.</returns>
		[DllImport("dbghelp.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId,
			SafeFileHandle hFile, MiniDumpType DumpType,
			ref MiniDumpExceptionInfo ExceptionParam, IntPtr UserStreamParam,
			IntPtr CallbackParam);

		/// <summary>
		/// Identifies the type of information that will be written to the minidump file
		/// by the MiniDumpWriteDump function.
		/// </summary>
		public enum MiniDumpType
		{
			/// <summary>
			/// Include just the information necessary to capture stack traces for all
			/// existing threads in a process.
			/// </summary>
			MiniDumpNormal = 0x00000000,

			/// <summary>
			/// Include the data sections from all loaded modules. This results in the
			/// inclusion of global variables, which can make the minidump file significantly
			/// larger. For per-module control, use the ModuleWriteDataSeg enumeration
			/// value from MODULE_WRITE_FLAGS.
			/// </summary>
			MiniDumpWithDataSegs = 0x00000001,

			/// <summary>
			/// Include all accessible memory in the process. The raw memory data is
			/// included at the end, so that the initial structures can be mapped directly
			/// without the raw memory information. This option can result in a very large
			/// file.
			/// </summary>
			MiniDumpWithFullMemory = 0x00000002,

			/// <summary>
			/// Include high-level information about the operating system handles that are
			/// active when the minidump is made.
			/// </summary>
			MiniDumpWithHandleData = 0x00000004,

			/// <summary>
			/// Stack and backing store memory written to the minidump file should be
			/// filtered to remove all but the pointer values necessary to reconstruct a
			/// stack trace. Typically, this removes any private information.
			/// </summary>
			MiniDumpFilterMemory = 0x00000008,

			/// <summary>
			/// Stack and backing store memory should be scanned for pointer references
			/// to modules in the module list. If a module is referenced by stack or backing
			/// store memory, the ModuleWriteFlags member of the MINIDUMP_CALLBACK_OUTPUT
			/// structure is set to ModuleReferencedByMemory.
			/// </summary>
			MiniDumpScanMemory = 0x00000010,

			/// <summary>
			/// Include information from the list of modules that were recently unloaded,
			/// if this information is maintained by the operating system.
			/// </summary>
			MiniDumpWithUnloadedModules = 0x00000020,

			/// <summary>
			/// Include pages with data referenced by locals or other stack memory.
			/// This option can increase the size of the minidump file significantly.
			/// </summary>
			MiniDumpWithIndirectlyReferencedMemory = 0x00000040,

			/// <summary>
			/// Filter module paths for information such as user names or important
			/// directories. This option may prevent the system from locating the image
			/// file and should be used only in special situations.
			/// </summary>
			MiniDumpFilterModulePaths = 0x00000080,

			/// <summary>
			/// Include complete per-process and per-thread information from the operating
			/// system.
			/// </summary>
			MiniDumpWithProcessThreadData = 0x00000100,

			/// <summary>
			/// Scan the virtual address space for PAGE_READWRITE memory to be included.
			/// </summary>
			MiniDumpWithPrivateReadWriteMemory = 0x00000200,

			/// <summary>
			/// Reduce the data that is dumped by eliminating memory regions that are not
			/// essential to meet criteria specified for the dump. This can avoid dumping
			/// memory that may contain data that is private to the user. However, it is
			/// not a guarantee that no private information will be present.
			/// </summary>
			MiniDumpWithoutOptionalData = 0x00000400,

			/// <summary>
			/// Include memory region information. For more information, see
			/// MINIDUMP_MEMORY_INFO_LIST.
			/// </summary>
			MiniDumpWithFullMemoryInfo = 0x00000800,

			/// <summary>
			/// Include thread state information. For more information, see
			/// MINIDUMP_THREAD_INFO_LIST.
			/// </summary>
			MiniDumpWithThreadInfo = 0x00001000,

			/// <summary>
			/// Include all code and code-related sections from loaded modules to capture
			/// executable content. For per-module control, use the ModuleWriteCodeSegs
			/// enumeration value from MODULE_WRITE_FLAGS. 
			/// </summary>
			MiniDumpWithCodeSegs = 0x00002000,

			/// <summary>
			/// Turns off secondary auxiliary-supported memory gathering.
			/// </summary>
			MiniDumpWithoutAuxiliaryState = 0x00004000,

			/// <summary>
			/// Requests that auxiliary data providers include their state in the dump
			/// image; the state data that is included is provider dependent. This option
			/// can result in a large dump image.
			/// </summary>
			MiniDumpWithFullAuxiliaryState = 0x00008000,

			/// <summary>
			/// Scans the virtual address space for PAGE_WRITECOPY memory to be included.
			/// </summary>
			MiniDumpWithPrivateWriteCopyMemory = 0x00010000,

			/// <summary>
			/// If you specify MiniDumpWithFullMemory, the MiniDumpWriteDump function will
			/// fail if the function cannot read the memory regions; however, if you include
			/// MiniDumpIgnoreInaccessibleMemory, the MiniDumpWriteDump function will
			/// ignore the memory read failures and continue to generate the dump. Note that
			/// the inaccessible memory regions are not included in the dump.
			/// </summary>
			MiniDumpIgnoreInaccessibleMemory = 0x00020000,

			/// <summary>
			/// Adds security token related data. This will make the "!token" extension work
			/// when processing a user-mode dump. 
			/// </summary>
			MiniDumpWithTokenInformation = 0x00040000
		}

		/// <summary>
		/// Contains the exception information written to the minidump file by the
		/// MiniDumpWriteDump function.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct MiniDumpExceptionInfo
		{
			/// <summary>
			/// The identifier of the thread throwing the exception.
			/// </summary>
			public uint ThreadId;

			/// <summary>
			///  A pointer to an EXCEPTION_POINTERS structure specifying a
			///  computer-independent description of the exception and the processor
			///  context at the time of the exception.
			/// </summary>
			public IntPtr ExceptionPointers;

			/// <summary>
			/// Determines where to get the memory regions pointed to by the
			/// ExceptionPointers member. Set to TRUE if the memory resides in the
			/// process being debugged (the target process of the debugger). Otherwise,
			/// set to FALSE if the memory resides in the address space of the calling
			/// program (the debugger process). If you are accessing local memory (in
			/// the calling process) you should not set this member to TRUE.
			/// </summary>
			[MarshalAs(UnmanagedType.Bool)]
			public bool ClientPointers;
		}
	}
}
