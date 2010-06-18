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
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Eraser.Util
{
	/// <summary>
	/// Stores Kernel32.dll functions, structs and constants.
	/// </summary>
	internal static partial class NativeMethods
	{
		/// <summary>
		/// Copies an existing file to a new file, notifying the application of
		/// its progress through a callback function.
		/// </summary>
		/// <param name="lpExistingFileName">The name of an existing file.
		/// 
		/// In the ANSI version of this function, the name is limited to MAX_PATH
		/// characters. To extend this limit to 32,767 wide characters, call the
		/// ]Unicode version of the function and prepend "\\?\" to the path.
		/// For more information, see Naming a File.
		/// 
		/// If lpExistingFileName does not exist, the CopyFileEx function fails,
		/// and the GetLastError function returns ERROR_FILE_NOT_FOUND.</param>
		/// <param name="lpNewFileName">The name of the new file.
		/// 
		/// In the ANSI version of this function, the name is limited to MAX_PATH
		/// characters. To extend this limit to 32,767 wide characters, call the
		/// Unicode version of the function and prepend "\\?\" to the path. For
		/// more information, see Naming a File.</param>
		/// <param name="lpProgressRoutine">The address of a callback function of
		/// type <see cref="ExtensionMethods.IO.CopyProgressFunction"/> that is
		/// called each time another portion of the file has been copied. This
		/// parameter can be NULL. For more information on the progress callback
		/// function, see the <see cref="ExtensionMethods.IO.CopyProgressFunction"/>
		/// function.</param>
		/// <param name="lpData">The argument to be passed to the callback function.
		/// This parameter can be NULL.</param>
		/// <param name="pbCancel">If this flag is set to TRUE during the copy
		/// operation, the operation is canceled. Otherwise, the copy operation
		/// will continue to completion.</param>
		/// <param name="dwCopyFlags">Flags that specify how the file is to be
		/// copied. This parameter can be a combination of the
		/// <see cref="CopyFileFlags"/> enumeration.
		/// </param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error information
		/// call <see cref="Marshal.GetLastWin32Error"/>.
		/// 
		/// If lpProgressRoutine returns PROGRESS_CANCEL due to the user canceling the
		/// operation, CopyFileEx will return zero and GetLastError will return
		/// ERROR_REQUEST_ABORTED. In this case, the partially copied destination file is
		/// deleted.
		/// 
		/// If lpProgressRoutine returns PROGRESS_STOP due to the user stopping the
		/// operation, CopyFileEx will return zero and GetLastError will return
		/// ERROR_REQUEST_ABORTED. In this case, the partially copied destination file
		/// is left intact.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		public static extern bool CopyFileEx(string lpExistingFileName,
			string lpNewFileName, CopyProgressFunction lpProgressRoutine,
			IntPtr lpData, ref bool pbCancel, CopyFileFlags dwCopyFlags);

		/// <summary>
		/// Flags used with <see cref="CopyFileEx"/>
		/// </summary>
		[Flags]
		public enum CopyFileFlags
		{
			/// <summary>
			/// An attempt to copy an encrypted file will succeed even if the
			/// destination copy cannot be encrypted.
			/// 
			/// Windows 2000: This value is not supported.
			/// </summary>
			AllowDecryptedDestination = 0x00000008,

			/// <summary>
			/// If the source file is a symbolic link, the destination file is
			/// also a symbolic link pointing to the same file that the source
			/// symbolic link is pointing to.
			/// 
			/// Windows Server 2003 and Windows XP/2000: This value is not
			/// supported.
			/// </summary>
			CopySymlink = 0x00000800,

			/// <summary>
			/// The copy operation fails immediately if the target file already
			/// exists.
			/// </summary>
			FailIfExists = 0x00000001,

			/// <summary>
			/// The copy operation is performed using unbuffered I/O, bypassing
			/// system I/O cache resources. Recommended for very large file
			/// transfers.
			///
			/// Windows Server 2003 and Windows XP/2000: This value is not
			/// supported.
			/// </summary>
			NoBuffering = 0x00001000,

			/// <summary>
			/// The file is copied and the original file is opened for write
			/// access.
			/// </summary>
			OpenSourceForWrite = 0x00000004,

			/// <summary>
			/// Progress of the copy is tracked in the target file in case the
			/// copy fails. The failed copy can be restarted at a later time by
			///	specifying the same values for lpExistingFileName and lpNewFileName
			///	as those used in the call that failed.
			/// </summary>
			Restartable = 0x00000002
		}

		/// <summary>
		/// An application-defined callback function used with the CopyFileEx,
		/// MoveFileTransacted, and MoveFileWithProgress functions. It is called when
		/// a portion of a copy or move operation is completed. The LPPROGRESS_ROUTINE
		/// type defines a pointer to this callback function. CopyProgressRoutine is
		/// a placeholder for the application-defined function name.
		/// </summary>
		/// <param name="TotalFileSize">The total size of the file, in bytes.</param>
		/// <param name="TotalBytesTransferred">The total number of bytes
		/// transferred from the source file to the destination file since the
		/// copy operation began.</param>
		/// <param name="StreamSize">The total size of the current file stream,
		/// in bytes.</param>
		/// <param name="StreamBytesTransferred">The total number of bytes in the
		/// current stream that have been transferred from the source file to the
		/// destination file since the copy operation began.</param>
		/// <param name="dwStreamNumber">A handle to the current stream. The
		/// first time CopyProgressRoutine is called, the stream number is 1.</param>
		/// <param name="dwCallbackReason">The reason that CopyProgressRoutine was
		/// called. This parameter can be one of the following values.</param>
		/// <param name="hSourceFile">A handle to the source file.</param>
		/// <param name="hDestinationFile">A handle to the destination file.</param>
		/// <param name="lpData">Argument passed to CopyProgressRoutine by CopyFileEx,
		/// MoveFileTransacted, or MoveFileWithProgress.</param>
		/// <returns>The CopyProgressRoutine function should return one of the
		/// <see cref="CopyProgressFunctionResult"/> values.</returns>
		public delegate ExtensionMethods.IO.CopyProgressFunctionResult CopyProgressFunction(
			long TotalFileSize, long TotalBytesTransferred, long StreamSize,
			long StreamBytesTransferred, uint dwStreamNumber,
			CopyProgressFunctionCallbackReasons dwCallbackReason,
			IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData);

		/// <summary>
		/// Callback reasons for the <see cref="CopyProgressFunction"/> callbacks.
		/// </summary>
		public enum CopyProgressFunctionCallbackReasons
		{
			/// <summary>
			/// Another part of the data file was copied.
			/// </summary>
			ChunkFinished = 0x00000000,

			/// <summary>
			/// Another stream was created and is about to be copied. This is
			/// the callback reason given when the callback routine is first invoked.
			/// </summary>
			StreamSwitch = 0x00000001
		}

		/// <summary>
		/// Deletes an existing file.
		/// </summary>
		/// <param name="lpFileName">The name of the file to be deleted.
		/// 
		/// In the ANSI version of this function, the name is limited to MAX_PATH
		/// characters. To extend this limit to 32,767 wide characters, call
		/// the Unicode version of the function and prepend "\\?\" to the path.
		/// For more information, see Naming a File.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero (0). To get extended
		/// error information, call Marshal.GetLastWin32Error().</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteFile(string lpFileName);

		/// <summary>
		/// Retrieves information about the current system.
		/// </summary>
		/// <param name="lpSystemInfo">A pointer to a SYSTEM_INFO structure that
		/// receives the information.</param>
		[DllImport("Kernel32.dll")]
		public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

		/// <summary>
		/// The QueryPerformanceCounter function retrieves the current value of
		/// the high-resolution performance counter.
		/// </summary>
		/// <param name="lpPerformanceCount">[out] Pointer to a variable that receives
		/// the current performance-counter value, in counts.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error. </returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		/// <summary>
		/// Contains information about the current computer system. This includes
		/// the architecture and type of the processor, the number of processors
		/// in the system, the page size, and other such information.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct SYSTEM_INFO
		{
			/// <summary>
			/// Represents a list of processor architectures.
			/// </summary>
			public enum ProcessorArchitecture : ushort
			{
				/// <summary>
				/// x64 (AMD or Intel).
				/// </summary>
				PROCESSOR_ARCHITECTURE_AMD64 = 9,

				/// <summary>
				/// Intel Itanium Processor Family (IPF).
				/// </summary>
				PROCESSOR_ARCHITECTURE_IA64 = 6,

				/// <summary>
				/// x86.
				/// </summary>
				PROCESSOR_ARCHITECTURE_INTEL = 0,

				/// <summary>
				/// Unknown architecture.
				/// </summary>
				PROCESSOR_ARCHITECTURE_UNKNOWN = 0xffff
			}

			/// <summary>
			/// The processor architecture of the installed operating system.
			/// This member can be one of the ProcessorArchitecture values.
			/// </summary>
			public ProcessorArchitecture processorArchitecture;

			/// <summary>
			/// This member is reserved for future use.
			/// </summary>
			private const ushort reserved = 0;

			/// <summary>
			/// The page size and the granularity of page protection and commitment.
			/// This is the page size used by the VirtualAlloc function.
			/// </summary>
			public uint pageSize;

			/// <summary>
			/// A pointer to the lowest memory address accessible to applications
			/// and dynamic-link libraries (DLLs).
			/// </summary>
			public IntPtr minimumApplicationAddress;

			/// <summary>
			/// A pointer to the highest memory address accessible to applications
			/// and DLLs.
			/// </summary>
			public IntPtr maximumApplicationAddress;

			/// <summary>
			/// A mask representing the set of processors configured into the system.
			/// Bit 0 is processor 0; bit 31 is processor 31.
			/// </summary>
			public IntPtr activeProcessorMask;

			/// <summary>
			/// The number of processors in the system.
			/// </summary>
			public uint numberOfProcessors;

			/// <summary>
			/// An obsolete member that is retained for compatibility. Use the
			/// wProcessorArchitecture, wProcessorLevel, and wProcessorRevision
			/// members to determine the type of processor.
			/// Name						Value
			/// PROCESSOR_INTEL_386			386
			/// PROCESSOR_INTEL_486			486
			/// PROCESSOR_INTEL_PENTIUM		586
			/// PROCESSOR_INTEL_IA64		2200
			/// PROCESSOR_AMD_X8664			8664
			/// </summary>
			public uint processorType;

			/// <summary>
			/// The granularity for the starting address at which virtual memory
			/// can be allocated. For more information, see VirtualAlloc.
			/// </summary>
			public uint allocationGranularity;

			/// <summary>
			/// The architecture-dependent processor level. It should be used only
			/// for display purposes. To determine the feature set of a processor,
			/// use the IsProcessorFeaturePresent function.
			/// 
			/// If wProcessorArchitecture is PROCESSOR_ARCHITECTURE_INTEL, wProcessorLevel
			/// is defined by the CPU vendor.
			/// If wProcessorArchitecture is PROCESSOR_ARCHITECTURE_IA64, wProcessorLevel
			/// is set to 1.
			/// </summary>
			public ushort processorLevel;

			/// <summary>
			/// The architecture-dependent processor revision. The following table
			/// shows how the revision value is assembled for each type of
			/// processor architecture.
			/// 
			/// Processor					Value
			/// Intel Pentium, Cyrix		The high byte is the model and the
			/// or NextGen 586				low byte is the stepping. For example,
			///								if the value is xxyy, the model number
			///								and stepping can be displayed as follows:
			///								Model xx, Stepping yy
			///	Intel 80386 or 80486		A value of the form xxyz.
			///								If xx is equal to 0xFF, y - 0xA is the model
			///								number, and z is the stepping identifier.
			/// 
			///								If xx is not equal to 0xFF, xx + 'A'
			///								is the stepping letter and yz is the minor stepping.
			/// </summary>
			public ushort processorRevision;
		}

		[DllImport("Kernel32.dll")]
		public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

		[Flags]
		public enum EXECUTION_STATE : uint
		{
			ES_AWAYMODE_REQUIRED = 0x00000040,
			ES_CONTINUOUS = 0x80000000,
			ES_DISPLAY_REQUIRED = 0x00000002,
			ES_SYSTEM_REQUIRED = 0x00000001,
			ES_USER_PRESENT = 0x00000004
		}

		/// <summary>
		/// Allocates a new console for the calling process.
		/// </summary>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		/// <remarks>A process can be associated with only one console, so the AllocConsole
		/// function fails if the calling process already has a console. A process can
		/// use the FreeConsole function to detach itself from its current console, then
		/// it can call AllocConsole to create a new console or AttachConsole to attach
		/// to another console.
		/// 
		/// If the calling process creates a child process, the child inherits the
		/// new console.
		/// 
		/// AllocConsole initializes standard input, standard output, and standard error
		/// handles for the new console. The standard input handle is a handle to the
		/// console's input buffer, and the standard output and standard error handles
		/// are handles to the console's screen buffer. To retrieve these handles, use
		/// the GetStdHandle function.
		/// 
		/// This function is primarily used by graphical user interface (GUI) application
		/// to create a console window. GUI applications are initialized without a
		/// console. Console applications are initialized with a console, unless they
		/// are created as detached processes (by calling the CreateProcess function
		/// with the DETACHED_PROCESS flag).</remarks>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AllocConsole();

		/// <summary>
		/// Detaches the calling process from its console.
		/// </summary>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		/// <remarks>A process can be attached to at most one console. If the calling
		/// process is not already attached to a console, the error code returned is
		/// ERROR_INVALID_PARAMETER (87).
		/// 
		/// A process can use the FreeConsole function to detach itself from its
		/// console. If other processes share the console, the console is not destroyed,
		/// but the process that called FreeConsole cannot refer to it. A console is
		/// closed when the last process attached to it terminates or calls FreeConsole.
		/// After a process calls FreeConsole, it can call the AllocConsole function to
		/// create a new console or AttachConsole to attach to another console.</remarks>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FreeConsole();

		/// <summary>
		/// The CreateFile function creates or opens a file, file stream, directory,
		/// physical disk, volume, console buffer, tape drive, communications resource,
		/// mailslot, or named pipe. The function returns a handle that can be used
		/// to access an object.
		/// </summary>
		/// <param name="FileName"></param>
		/// <param name="DesiredAccess"> access to the object, which can be read,
		/// write, or both</param>
		/// <param name="ShareMode">The sharing mode of an object, which can be
		/// read, write, both, or none</param>
		/// <param name="SecurityAttributes">A pointer to a SECURITY_ATTRIBUTES
		/// structure that determines whether or not the returned handle can be
		/// inherited by child processes. Can be null</param>
		/// <param name="CreationDisposition">An action to take on files that exist
		/// and do not exist</param>
		/// <param name="FlagsAndAttributes">The file attributes and flags.</param>
		/// <param name="hTemplateFile">A handle to a template file with the
		/// GENERIC_READ access right. The template file supplies file attributes
		/// and extended attributes for the file that is being created. This
		/// parameter can be null</param>
		/// <returns>If the function succeeds, the return value is an open handle
		/// to a specified file. If a specified file exists before the function
		/// all and dwCreationDisposition is CREATE_ALWAYS or OPEN_ALWAYS, a call
		/// to GetLastError returns ERROR_ALREADY_EXISTS, even when the function
		/// succeeds. If a file does not exist before the call, GetLastError
		/// returns 0.
		/// 
		/// If the function fails, the return value is INVALID_HANDLE_VALUE.
		/// To get extended error information, call Marshal.GetLastWin32Error().</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
			uint dwShareMode, IntPtr SecurityAttributes, uint dwCreationDisposition,
			uint dwFlagsAndAttributes, IntPtr hTemplateFile);

		public const uint FILE_READ_ATTRIBUTES = 0x0080;
		public const uint FILE_WRITE_ATTRIBUTES = 0x0100;
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const uint GENERIC_EXECUTE = 0x20000000;
		public const uint GENERIC_ALL = 0x10000000;

		public const uint FILE_SHARE_READ = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;
		public const uint FILE_SHARE_DELETE = 0x00000004;

		public const uint CREATE_NEW = 1;
		public const uint CREATE_ALWAYS = 2;
		public const uint OPEN_EXISTING = 3;
		public const uint OPEN_ALWAYS = 4;
		public const uint TRUNCATE_EXISTING = 5;

		public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
		public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
		public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
		public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
		public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
		public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
		public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
		public const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
		public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
		public const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
		public const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;

		/// <summary>
		/// Defines, redefines, or deletes MS-DOS device names.
		/// </summary>
		/// <param name="dwFlags">The controllable aspects of the DefineDosDevice function. This
		/// parameter can be one or more of the DosDeviceDefineFlags.</param>
		/// <param name="lpDeviceName">A pointer to an MS-DOS device name string specifying the
		/// device the function is defining, redefining, or deleting. The device name string must
		/// not have a colon as the last character, unless a drive letter is being defined,
		/// redefined, or deleted. For example, drive C would be the string "C:". In no case is
		/// a trailing backslash ("\") allowed.</param>
		/// <param name="lpTargetPath">A pointer to a path string that will implement this
		/// device. The string is an MS-DOS path string unless the DDD_RAW_TARGET_PATH flag
		/// is specified, in which case this string is a path string.</param>
		/// <returns>If the function succeeds, the return value is true.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DefineDosDevice(DosDeviceDefineFlags dwFlags,
			string lpDeviceName, string lpTargetPath);

		[Flags]
		public enum DosDeviceDefineFlags
		{
			/// <summary>
			/// If this value is specified along with DDD_REMOVE_DEFINITION, the function will
			/// use an exact match to determine which mapping to remove. Use this value to
			/// ensure that you do not delete something that you did not define.
			/// </summary>
			ExactMatchOnRmove = 0x00000004,

			/// <summary>
			/// Do not broadcast the WM_SETTINGCHANGE message. By default, this message is
			/// broadcast to notify the shell and applications of the change.
			/// </summary>
			NoBroadcastSystem = 0x00000008,

			/// <summary>
			/// Uses the lpTargetPath string as is. Otherwise, it is converted from an MS-DOS
			/// path to a path.
			/// </summary>
			RawTargetPath = 0x00000001,

			/// <summary>
			/// Removes the specified definition for the specified device. To determine which
			/// definition to remove, the function walks the list of mappings for the device,
			/// looking for a match of lpTargetPath against a prefix of each mapping associated
			/// with this device. The first mapping that matches is the one removed, and then
			/// the function returns.
			/// 
			/// If lpTargetPath is NULL or a pointer to a NULL string, the function will remove
			/// the first mapping associated with the device and pop the most recent one pushed.
			/// If there is nothing left to pop, the device name will be removed.
			/// 
			/// If this value is not specified, the string pointed to by the lpTargetPath
			/// parameter will become the new mapping for this device.
			/// </summary>
			RemoveDefinition = 0x00000002
		}

		/// <summary>
		/// Retrieves information about MS-DOS device names. The function can obtain the
		/// current mapping for a particular MS-DOS device name. The function can also obtain
		/// a list of all existing MS-DOS device names.
		/// 
		/// MS-DOS device names are stored as junctions in the object name space. The code
		/// that converts an MS-DOS path into a corresponding path uses these junctions to
		/// map MS-DOS devices and drive letters. The QueryDosDevice function enables an
		/// application to query the names of the junctions used to implement the MS-DOS
		/// device namespace as well as the value of each specific junction.
		/// </summary>
		/// <param name="lpDeviceName">An MS-DOS device name string specifying the target of
		/// the query. The device name cannot have a trailing backslash; for example,
		/// use "C:", not "C:\".
		/// 
		/// This parameter can be NULL. In that case, the QueryDosDevice function will
		/// store a list of all existing MS-DOS device names into the buffer pointed to
		/// by lpTargetPath.</param>
		/// <param name="lpTargetPath">A pointer to a buffer that will receive the result
		/// of the query. The function fills this buffer with one or more null-terminated
		/// strings. The final null-terminated string is followed by an additional NULL.
		/// 
		/// If lpDeviceName is non-NULL, the function retrieves information about the
		/// particular MS-DOS device specified by lpDeviceName. The first null-terminated
		/// string stored into the buffer is the current mapping for the device. The other
		/// null-terminated strings represent undeleted prior mappings for the device.
		/// 
		/// If lpDeviceName is NULL, the function retrieves a list of all existing MS-DOS
		/// device names. Each null-terminated string stored into the buffer is the name
		/// of an existing MS-DOS device, for example, \Device\HarddiskVolume1 or
		/// \Device\Floppy0.</param>
		/// <param name="length">The maximum number of TCHARs that can be stored into
		/// the buffer pointed to by lpTargetPath.</param>
		/// <returns>If the function succeeds, the return value is the number of TCHARs
		/// stored into the buffer pointed to by lpTargetPath.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.
		/// 
		/// If the buffer is too small, the function fails and the last error code is
		/// ERROR_INSUFFICIENT_BUFFER.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern uint QueryDosDevice([Optional] string lpDeviceName,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] [Out] char[] lpTargetPath, int length);

		private static string[] QueryDosDeviceInternal(string lpDeviceName)
		{
			char[] buffer = new char[32768];
			for ( ; ; buffer = new char[buffer.Length * 2])
			{
				uint written = NativeMethods.QueryDosDevice(lpDeviceName, buffer, buffer.Length);

				//Do we have enough space for all the text
				if (written != 0)
					return ParseNullDelimitedArray(buffer, (int)written);
				else if (Marshal.GetLastWin32Error() == Win32ErrorCode.InsufficientBuffer)
					continue;
				else
					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		public static string QueryDosDevice(string lpDeviceName)
		{
			string[] result = QueryDosDeviceInternal(lpDeviceName);
			return result.Length == 0 ? null : result[0];
		}

		public static string[] QueryDosDevices()
		{
			return QueryDosDeviceInternal(null);
		}

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
			out ushort lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned,
			IntPtr lpOverlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, ref ushort lpInBuffer, uint nInBufferSize,
			IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned,
			IntPtr lpOverlapped);

		public const uint FSCTL_GET_COMPRESSION = 0x9003C;
		public const uint FSCTL_SET_COMPRESSION = 0x9C040;
		public const ushort COMPRESSION_FORMAT_NONE = 0x0000;
		public const ushort COMPRESSION_FORMAT_DEFAULT = 0x0001;

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
			IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned,
			IntPtr lpOverlapped);

		public const uint FSCTL_LOCK_VOLUME = 0x90018;
		public const uint FSCTL_UNLOCK_VOLUME = 0x9001C;

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
			out DiskPerformanceInfoInternal lpOutBuffer, uint nOutBufferSize,
			out uint lpBytesReturned, IntPtr lpOverlapped);

		public const uint IOCTL_DISK_PERFORMANCE = ((0x00000007) << 16) | ((0x0008) << 2);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DiskPerformanceInfoInternal
		{
			public long BytesRead;
			public long BytesWritten;
			public long ReadTime;
			public long WriteTime;
			public long IdleTime;
			public uint ReadCount;
			public uint WriteCount;
			public uint QueueDepth;
			public uint SplitCount;
			public long QueryTime;
			public uint StorageDeviceNumber;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
			public string StorageManagerName;
		}

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private extern static bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
			out long lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned,
			IntPtr lpOverlapped);

		public static bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
			out long lpOutBuffer, out uint lpBytesReturned, IntPtr lpOverlapped)
		{
			return DeviceIoControl(hDevice, dwIoControlCode, lpInBuffer, nInBufferSize,
				out lpOutBuffer, sizeof(long), out lpBytesReturned, lpOverlapped);
		}

		/// <summary>
		/// Retrieves the length of the specified disk, volume, or partition.
		/// </summary>
		public const int IOCTL_DISK_GET_LENGTH_INFO =
			(0x00000007 << 16) | (0x0001 << 14) | (0x0017 << 2);

		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl(SafeFileHandle hDevice,
			uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
			out NTFS_VOLUME_DATA_BUFFER lpOutBuffer, uint nOutBufferSize,
			out uint lpBytesReturned, IntPtr lpOverlapped);

		/// <summary>
		/// Retrieves information about the specified NTFS file system volume.
		/// </summary>
		public const int FSCTL_GET_NTFS_VOLUME_DATA = (9 << 16) | (25 << 2);

		/// <summary>
		/// Retrieves a set of FAT file system attributes for a specified file or
		/// directory.
		/// </summary>
		/// <param name="lpFileName">The name of the file or directory.</param>
		/// <returns>If the function succeeds, the return value contains the attributes
		/// of the specified file or directory.
		/// 
		/// If the function fails, the return value is INVALID_FILE_ATTRIBUTES.
		/// To get extended error information, call Marshal.GetLastWin32Error.
		/// 
		/// The attributes can be one or more of the FILE_ATTRIBUTE_* values.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetFileAttributes(string lpFileName);

		/// <summary>
		/// Sets the attributes for a file or directory.
		/// </summary>
		/// <param name="lpFileName">The name of the file whose attributes are
		/// to be set.</param>
		/// <param name="dwFileAttributes">The file attributes to set for the file.
		/// This parameter can be one or more of the FILE_ATTRIBUTE_* values.
		/// However, all other values override FILE_ATTRIBUTE_NORMAL.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2205:UseManagedEquivalentsOfWin32Api")]
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetFileAttributes(string lpFileName,
			uint dwFileAttributes);

		/// <summary>
		/// Retrieves the size of the specified file.
		/// </summary>
		/// <param name="hFile">A handle to the file. The handle must have been
		/// created with either the GENERIC_READ or GENERIC_WRITE access right.
		/// For more information, see File Security and Access Rights.</param>
		/// <param name="lpFileSize">A reference to a long that receives the file
		/// size, in bytes.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileSizeEx(SafeFileHandle hFile, out long lpFileSize);

		/// <summary>
		/// Retrieves the date and time that a file or directory was created, last
		/// accessed, and last modified.
		/// </summary>
		/// <param name="hFile">A handle to the file or directory for which dates
		/// and times are to be retrieved. The handle must have been created using
		/// the CreateFile function with the GENERIC_READ access right. For more
		/// information, see File Security and Access Rights.</param>
		/// <param name="lpCreationTime">A pointer to a FILETIME structure to
		/// receive the date and time the file or directory was created. This
		/// parameter can be NULL if the application does not require this
		/// information.</param>
		/// <param name="lpLastAccessTime">A pointer to a FILETIME structure to
		/// receive the date and time the file or directory was last accessed. The
		/// last access time includes the last time the file or directory was
		/// written to, read from, or, in the case of executable files, run. This
		/// parameter can be NULL if the application does not require this
		/// information.</param>
		/// <param name="lpLastWriteTime">A pointer to a FILETIME structure to
		/// receive the date and time the file or directory was last written to,
		/// truncated, or overwritten (for example, with WriteFile or SetEndOfFile).
		/// This date and time is not updated when file attributes or security
		/// descriptors are changed. This parameter can be NULL if the application
		/// does not require this information.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error().</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileTime(SafeFileHandle hFile,
			out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
			out System.Runtime.InteropServices.ComTypes.FILETIME lpLastAccessTime,
			out System.Runtime.InteropServices.ComTypes.FILETIME lpLastWriteTime);

		/// <summary>
		/// Sets the date and time that the specified file or directory was created,
		/// last accessed, or last modified.
		/// </summary>
		/// <param name="hFile">A handle to the file or directory. The handle must
		/// have been created using the CreateFile function with the
		/// FILE_WRITE_ATTRIBUTES access right. For more information, see File
		/// Security and Access Rights.</param>
		/// <param name="lpCreationTime">A pointer to a FILETIME structure that
		/// contains the new creation date and time for the file or directory.
		/// This parameter can be NULL if the application does not need to change
		/// this information.</param>
		/// <param name="lpLastAccessTime">A pointer to a FILETIME structure that
		/// contains the new last access date and time for the file or directory.
		/// The last access time includes the last time the file or directory was
		/// written to, read from, or (in the case of executable files) run. This
		/// parameter can be NULL if the application does not need to change this
		/// information.
		/// 
		/// To preserve the existing last access time for a file even after accessing
		/// a file, call SetFileTime immediately after opening the file handle
		/// with this parameter's FILETIME structure members initialized to
		/// 0xFFFFFFFF.</param>
		/// <param name="lpLastWriteTime">A pointer to a FILETIME structure that
		/// contains the new last modified date and time for the file or directory.
		/// This parameter can be NULL if the application does not need to change
		/// this information.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetFileTime(SafeFileHandle hFile,
			ref System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
			ref System.Runtime.InteropServices.ComTypes.FILETIME lpLastAccessTime,
			ref System.Runtime.InteropServices.ComTypes.FILETIME lpLastWriteTime);

		/// <summary>
		/// Retrieves the name of a volume on a computer. FindFirstVolume is used
		/// to begin scanning the volumes of a computer.
		/// </summary>
		/// <param name="lpszVolumeName">A pointer to a buffer that receives a
		/// null-terminated string that specifies the unique volume name of the
		/// first volume found.</param>
		/// <param name="cchBufferLength">The length of the buffer to receive the
		/// name, in TCHARs.</param>
		/// <returns>If the function succeeds, the return value is a search handle
		/// used in a subsequent call to the FindNextVolume and FindVolumeClose
		/// functions.
		/// 
		/// If the function fails to find any volumes, the return value is the
		/// INVALID_HANDLE_VALUE error code. To get extended error information,
		/// call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle FindFirstVolume(StringBuilder lpszVolumeName,
			uint cchBufferLength);

		/// <summary>
		/// Continues a volume search started by a call to the FindFirstVolume
		/// function. FindNextVolume finds one volume per call.
		/// </summary>
		/// <param name="hFindVolume">The volume search handle returned by a previous
		/// call to the FindFirstVolume function.</param>
		/// <param name="lpszVolumeName">A pointer to a string that receives the
		/// unique volume name found.</param>
		/// <param name="cchBufferLength">The length of the buffer that receives
		/// the name, in TCHARs.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call GetLastError. If no matching files can be found, the
		/// GetLastError function returns the ERROR_NO_MORE_FILES error code. In
		/// that case, close the search with the FindVolumeClose function.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FindNextVolume(SafeHandle hFindVolume,
			StringBuilder lpszVolumeName, uint cchBufferLength);

		/// <summary>
		/// Closes the specified volume search handle. The FindFirstVolume and
		/// FindNextVolume functions use this search handle to locate volumes.
		/// </summary>
		/// <param name="hFindVolume">The volume search handle to be closed. This
		/// handle must have been previously opened by the FindFirstVolume function.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FindVolumeClose(SafeHandle hFindVolume);

		/// <summary>
		/// Retrieves the name of a volume mount point on the specified volume.
		/// FindFirstVolumeMountPoint is used to begin scanning the volume mount
		/// points on a volume.
		/// </summary>
		/// <param name="lpszRootPathName">The unique volume name of the volume
		/// to scan for volume mount points. A trailing backslash is required.</param>
		/// <param name="lpszVolumeMountPoint">A pointer to a buffer that receives
		/// the name of the first volume mount point found.</param>
		/// <param name="cchBufferLength">The length of the buffer that receives
		/// the volume mount point name, in TCHARs.</param>
		/// <returns>If the function succeeds, the return value is a search handle
		/// used in a subsequent call to the FindNextVolumeMountPoint and
		/// FindVolumeMountPointClose functions.
		/// 
		/// If the function fails to find a volume mount point on the volume, the
		/// return value is the INVALID_HANDLE_VALUE error code. To get extended
		/// error information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle FindFirstVolumeMountPoint(
			string lpszRootPathName, StringBuilder lpszVolumeMountPoint,
			uint cchBufferLength);

		/// <summary>
		/// Continues a volume mount point search started by a call to the
		/// FindFirstVolumeMountPoint function. FindNextVolumeMountPoint finds one
		/// volume mount point per call.
		/// </summary>
		/// <param name="hFindVolumeMountPoint">A mount-point search handle returned
		/// by a previous call to the FindFirstVolumeMountPoint function.</param>
		/// <param name="lpszVolumeMountPoint">A pointer to a buffer that receives
		/// the name of the volume mount point found.</param>
		/// <param name="cchBufferLength">The length of the buffer that receives
		/// the names, in TCHARs.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended error
		/// information, call GetLastError. If no matching files can be found, the
		/// GetLastError function returns the ERROR_NO_MORE_FILES error code. In
		/// that case, close the search with the FindVolumeMountPointClose function.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FindNextVolumeMountPoint(
			SafeHandle hFindVolumeMountPoint, StringBuilder lpszVolumeMountPoint,
			uint cchBufferLength);

		/// <summary>
		/// Closes the specified mount-point search handle. The FindFirstVolumeMountPoint
		/// and FindNextVolumeMountPoint  functions use this search handle to locate
		/// volume mount points on a specified volume.
		/// </summary>
		/// <param name="hFindVolumeMountPoint">The mount-point search handle to
		/// be closed. This handle must have been previously opened by the
		/// FindFirstVolumeMountPoint function.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		///
		/// If the function fails, the return value is zero. To get extended error
		/// information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FindVolumeMountPointClose(SafeHandle hFindVolumeMountPoint);

		/// <summary>
		/// Retrieves information about the specified disk, including the amount
		/// of free space on the disk.
		/// 
		/// The GetDiskFreeSpace function cannot report volume sizes that are
		/// greater than 2 gigabytes (GB). To ensure that your application works
		/// with large capacity hard drives, use the GetDiskFreeSpaceEx function.
		/// </summary>
		/// <param name="lpRootPathName">The root directory of the disk for which
		/// information is to be returned. If this parameter is NULL, the function
		/// uses the root of the current disk. If this parameter is a UNC name,
		/// it must include a trailing backslash (for example, \\MyServer\MyShare\).
		/// Furthermore, a drive specification must have a trailing backslash
		/// (for example, C:\). The calling application must have FILE_LIST_DIRECTORY
		/// access rights for this directory.</param>
		/// <param name="lpSectorsPerCluster">A pointer to a variable that receives
		/// the number of sectors per cluster.</param>
		/// <param name="lpBytesPerSector">A pointer to a variable that receives
		/// the number of bytes per sector.</param>
		/// <param name="lpNumberOfFreeClusters">A pointer to a variable that
		/// receives the total number of free clusters on the disk that are
		/// available to the user who is associated with the calling thread.
		/// 
		/// If per-user disk quotas are in use, this value may be less than the 
		/// total number of free clusters on the disk.</param>
		/// <param name="lpTotalNumberOfClusters">A pointer to a variable that
		/// receives the total number of clusters on the disk that are available
		/// to the user who is associated with the calling thread.
		/// 
		/// If per-user disk quotas are in use, this value may be less than the
		/// total number of clusters on the disk.</param>
		/// <returns>If the function succeeds, the return value is true. To get
		/// extended error information, call Marshal.GetLastWin32Error().</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetDiskFreeSpace(
			string lpRootPathName, out UInt32 lpSectorsPerCluster, out UInt32 lpBytesPerSector,
			out UInt32 lpNumberOfFreeClusters, out UInt32 lpTotalNumberOfClusters);


		/// <summary>
		/// Retrieves information about the amount of space that is available on
		/// a disk volume, which is the total amount of space, the total amount
		/// of free space, and the total amount of free space available to the
		/// user that is associated with the calling thread.
		/// </summary>
		/// <param name="lpDirectoryName">A directory on the disk.
		/// 
		/// If this parameter is NULL, the function uses the root of the current
		/// disk.
		/// 
		/// If this parameter is a UNC name, it must include a trailing backslash,
		/// for example, "\\MyServer\MyShare\".
		/// 
		/// This parameter does not have to specify the root directory on a disk.
		/// The function accepts any directory on a disk.
		/// 
		/// The calling application must have FILE_LIST_DIRECTORY access rights
		/// for this directory.</param>
		/// <param name="lpFreeBytesAvailable">A pointer to a variable that receives
		/// the total number of free bytes on a disk that are available to the
		/// user who is associated with the calling thread.
		/// 
		/// This parameter can be NULL.
		/// 
		/// If per-user quotas are being used, this value may be less than the
		/// total number of free bytes on a disk.</param>
		/// <param name="lpTotalNumberOfBytes">A pointer to a variable that receives
		/// the total number of bytes on a disk that are available to the user who
		/// is associated with the calling thread.
		/// 
		/// This parameter can be NULL.
		/// 
		/// If per-user quotas are being used, this value may be less than the
		/// total number of bytes on a disk.
		/// 
		/// To determine the total number of bytes on a disk or volume, use
		/// IOCTL_DISK_GET_LENGTH_INFO.</param>
		/// <param name="lpTotalNumberOfFreeBytes">A pointer to a variable that
		/// receives the total number of free bytes on a disk.
		/// 
		/// This parameter can be NULL.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero (0). To get extended
		/// error information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetDiskFreeSpaceEx(
			string lpDirectoryName,
			out UInt64 lpFreeBytesAvailable,
			out UInt64 lpTotalNumberOfBytes,
			out UInt64 lpTotalNumberOfFreeBytes);

		/// <summary>
		/// Determines whether a disk drive is a removable, fixed, CD-ROM, RAM disk,
		/// or network drive.
		/// </summary>
		/// <param name="lpRootPathName">The root directory for the drive.
		/// 
		/// A trailing backslash is required. If this parameter is NULL, the function
		/// uses the root of the current directory.</param>
		/// <returns>The return value specifies the type of drive, which can be
		/// one of the DriveInfo.DriveType values.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetDriveType(string lpRootPathName);

		/// <summary>
		/// Retrieves information about the file system and volume associated with
		/// the specified root directory.
		/// 
		/// To specify a handle when retrieving this information, use the
		/// GetVolumeInformationByHandleW function.
		/// 
		/// To retrieve the current compression state of a file or directory, use
		/// FSCTL_GET_COMPRESSION.
		/// </summary>
		/// <param name="lpRootPathName">    A pointer to a string that contains
		/// the root directory of the volume to be described.
		/// 
		/// If this parameter is NULL, the root of the current directory is used.
		/// A trailing backslash is required. For example, you specify
		/// \\MyServer\MyShare as "\\MyServer\MyShare\", or the C drive as "C:\".</param>
		/// <param name="lpVolumeNameBuffer">A pointer to a buffer that receives
		/// the name of a specified volume. The maximum buffer size is MAX_PATH+1.</param>
		/// <param name="nVolumeNameSize">The length of a volume name buffer, in
		/// TCHARs. The maximum buffer size is MAX_PATH+1.
		/// 
		/// This parameter is ignored if the volume name buffer is not supplied.</param>
		/// <param name="lpVolumeSerialNumber">A pointer to a variable that receives
		/// the volume serial number.
		/// 
		/// This parameter can be NULL if the serial number is not required.
		/// 
		/// This function returns the volume serial number that the operating system
		/// assigns when a hard disk is formatted. To programmatically obtain the
		/// hard disk's serial number that the manufacturer assigns, use the
		/// Windows Management Instrumentation (WMI) Win32_PhysicalMedia property
		/// SerialNumber.</param>
		/// <param name="lpMaximumComponentLength">A pointer to a variable that
		/// receives the maximum length, in TCHARs, of a file name component that
		/// a specified file system supports.
		/// 
		/// A file name component is the portion of a file name between backslashes.
		/// 
		/// The value that is stored in the variable that *lpMaximumComponentLength
		/// points to is used to indicate that a specified file system supports
		/// long names. For example, for a FAT file system that supports long names,
		/// the function stores the value 255, rather than the previous 8.3 indicator.
		/// Long names can also be supported on systems that use the NTFS file system.</param>
		/// <param name="lpFileSystemFlags">A pointer to a variable that receives
		/// flags associated with the specified file system.
		/// 
		/// This parameter can be one or more of the FS_FILE* flags. However,
		/// FS_FILE_COMPRESSION and FS_VOL_IS_COMPRESSED are mutually exclusive.</param>
		/// <param name="lpFileSystemNameBuffer">A pointer to a buffer that receives
		/// the name of the file system, for example, the FAT file system or the
		/// NTFS file system. The maximum buffer size is MAX_PATH+1.</param>
		/// <param name="nFileSystemNameSize">The length of the file system name
		/// buffer, in TCHARs. The maximum buffer size is MAX_PATH+1.
		/// 
		/// This parameter is ignored if the file system name buffer is not supplied.</param>
		/// <returns>If all the requested information is retrieved, the return value
		/// is nonzero.
		/// 
		/// 
		/// If not all the requested information is retrieved, the return value is
		/// zero (0). To get extended error information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetVolumeInformation(
			string lpRootPathName,
			StringBuilder lpVolumeNameBuffer,
			uint nVolumeNameSize,
			out uint lpVolumeSerialNumber,
			out uint lpMaximumComponentLength,
			out uint lpFileSystemFlags,
			StringBuilder lpFileSystemNameBuffer,
			uint nFileSystemNameSize);

		/// <summary>
		/// Retrieves the unique volume name for the specified volume mount point or root directory.
		/// </summary>
		/// <param name="lpszVolumeMountPoint">The path of a volume mount point (with a trailing
		/// backslash, "\") or a drive letter indicating a root directory (in the
		/// form "D:\").</param>
		/// <param name="lpszVolumeName">A pointer to a string that receives the
		/// volume name. This name is a unique volume name of the form
		/// "\\?\Volume{GUID}\" where GUID is the GUID that identifies the volume.</param>
		/// <param name="cchBufferLength">The length of the output buffer, in TCHARs.
		/// A reasonable size for the buffer to accommodate the largest possible
		/// volume name is 50 characters.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// 
		/// If the function fails, the return value is zero. To get extended
		/// error information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetVolumeNameForVolumeMountPoint(
			string lpszVolumeMountPoint, StringBuilder lpszVolumeName,
			uint cchBufferLength);

		/// <summary>
		/// Retrieves a list of path names for the specified volume name.
		/// </summary>
		/// <param name="lpszVolumeName">The volume name.</param>
		/// <param name="lpszVolumePathNames">A pointer to a buffer that receives
		/// the list of volume path names. The list is an array of null-terminated
		/// strings terminated by an additional NULL character. If the buffer is
		/// not large enough to hold the complete list, the buffer holds as much
		/// of the list as possible.</param>
		/// <param name="cchBufferLength">The length of the lpszVolumePathNames
		/// buffer, in TCHARs.</param>
		/// <param name="lpcchReturnLength">If the call is successful, this parameter
		/// is the number of TCHARs copied to the lpszVolumePathNames buffer. Otherwise,
		/// this parameter is the size of the buffer required to hold the complete
		/// list, in TCHARs.</param>
		/// <returns></returns>
		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetVolumePathNamesForVolumeName(string lpszVolumeName,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] lpszVolumePathNames,
			uint cchBufferLength, out uint lpcchReturnLength);

		/// <summary>
		/// Retrieves a list of path names for the specified volume name.
		/// </summary>
		/// <param name="lpszVolumeName">The volume name.</param>
		public static string[] GetVolumePathNamesForVolumeName(string lpszVolumeName)
		{
			uint returnLength = 0;
			char[] pathNamesBuffer = new char[NativeMethods.MaxPath];
			while (!NativeMethods.GetVolumePathNamesForVolumeName(lpszVolumeName,
				pathNamesBuffer, (uint)pathNamesBuffer.Length, out returnLength))
			{
				int errorCode = Marshal.GetLastWin32Error();
				switch (errorCode)
				{
					case Win32ErrorCode.NotReady:
						//The drive isn't ready yet: just return an empty list.
						return new string[0];
					case Win32ErrorCode.MoreData:
						pathNamesBuffer = new char[pathNamesBuffer.Length * 2];
						break;
					default:
						throw Win32ErrorCode.GetExceptionForWin32Error(errorCode);
				}
			}

			return ParseNullDelimitedArray(pathNamesBuffer, (int)returnLength);
		}

		public const int MaxPath = 260;
		public const int LongPath = 32768;

		/// <summary>
		/// Retrieves the product type for the operating system on the local computer, and
		/// maps the type to the product types supported by the specified operating system.
		/// </summary>
		/// <param name="dwOSMajorVersion">The major version number of the operating system.
		/// The minimum value is 6.
		/// 
		/// The combination of the dwOSMajorVersion, dwOSMinorVersion, dwSpMajorVersion,
		/// and dwSpMinorVersion parameters describes the maximum target operating system
		/// version for the application. For example, Windows Vista and Windows Server
		/// 2008 are version 6.0.0.0 and Windows 7 and Windows Server 2008 R2 are version
		/// 6.1.0.0.</param>
		/// <param name="dwOSMinorVersion">The minor version number of the operating
		/// system. The minimum value is 0.</param>
		/// <param name="dwSpMajorVersion">The major version number of the operating
		/// system service pack. The minimum value is 0.</param>
		/// <param name="dwSpMinorVersion">The minor version number of the operating
		/// system service pack. The minimum value is 0.</param>
		/// <param name="pdwReturnedProductType">The product type. This parameter
		/// cannot be NULL. If the specified operating system is less than the
		/// current operating system, this information is mapped to the types
		/// supported by the specified operating system. If the specified operating
		/// system is greater than the highest supported operating system, this
		/// information is mapped to the types supported by the current operating system.
		/// 
		/// If the product has not been activated and is no longer in the grace period,
		/// this parameter is set to PRODUCT_UNLICENSED (0xABCDABCD).</param>
		/// <returns>If the function succeeds, the return value is a nonzero value.
		/// If the software license is invalid or expired, the function succeeds
		/// but the pdwReturnedProductType parameter is set to PRODUCT_UNLICENSED.
		/// 
		/// If the function fails, the return value is zero. This function fails if
		/// one of the input parameters is invalid.</returns>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		public static extern bool GetProductInfo(uint dwOSMajorVersion,
			uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion,
			out WindowsEditions pdwReturnedProductType);

		/// <summary>
		/// Frees the specified local memory object and invalidates its handle.
		/// </summary>
		/// <param name="hMem">A handle to the local memory object. This handle is
		/// returned by either the LocalAlloc or LocalReAlloc function. It is not
		/// safe to free memory allocated with GlobalAlloc.</param>
		/// <returns>If the function succeeds, the return value is NULL.
		/// 
		/// If the function fails, the return value is equal to a handle to the
		/// local memory object. To get extended error information, call
		/// GetLastError.</returns>
		/// <remarks>If the process tries to examine or modify the memory after
		/// it has been freed, heap corruption may occur or an access violation
		/// exception (EXCEPTION_ACCESS_VIOLATION) may be generated.
		/// 
		/// If the hMem parameter is NULL, LocalFree ignores the parameter and
		/// returns NULL.
		/// 
		/// The LocalFree function will free a locked memory object. A locked
		/// memory object has a lock count greater than zero. The LocalLock
		/// function locks a local memory object and increments the lock count
		/// by one. The LocalUnlock function unlocks it and decrements the lock
		/// count by one. To get the lock count of a local memory object, use
		/// the LocalFlags function.
		/// 
		/// If an application is running under a debug version of the system,
		/// LocalFree will issue a message that tells you that a locked object
		/// is being freed. If you are debugging the application, LocalFree will
		/// enter a breakpoint just before freeing a locked object. This allows
		/// you to verify the intended behavior, then continue execution.</remarks>
		[DllImport("Kernel32.dll", SetLastError = true)]
		public static extern IntPtr LocalFree(IntPtr hMem);

		/// <summary>
		/// Parses a null-delimited array into a string array.
		/// </summary>
		/// <param name="buffer">The buffer to parse.</param>
		/// <param name="length">The valid length of the array.</param>
		/// <returns>The array found in the buffer</returns>
		private static string[] ParseNullDelimitedArray(char[] buffer, int length)
		{
			List<string> result = new List<string>();
			for (int lastIndex = 0, i = 0; i != length; ++i)
			{
				if (buffer[i] == '\0')
				{
					//If the string formed is empty, there are no elements left.
					if (i - lastIndex == 0)
						break;

					result.Add(new string(buffer, lastIndex, i - lastIndex));

					lastIndex = i + 1;
					if (buffer[lastIndex] == '\0')
						break;
				}
			}

			return result.ToArray();
		}
	}
}
