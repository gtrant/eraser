/* 
 * $Id$
 * Copyright 2008-2010 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Garrett Trant <gtrant@users.sourceforge.net>
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
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;

namespace Eraser.Util
{
	public static class KernelApi
	{
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
		public static bool AllocConsole()
		{
			return NativeMethods.AllocConsole();
		}

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
		public static bool FreeConsole()
		{
			return NativeMethods.FreeConsole();
		}

		private static DateTime FileTimeToDateTime(System.Runtime.InteropServices.ComTypes.FILETIME value)
		{
			long time = (long)((((ulong)value.dwHighDateTime) << sizeof(int) * 8) |
				(uint)value.dwLowDateTime);
			return DateTime.FromFileTime(time);
		}

		private static System.Runtime.InteropServices.ComTypes.FILETIME DateTimeToFileTime(DateTime value)
		{
			long time = value.ToFileTime();

			System.Runtime.InteropServices.ComTypes.FILETIME result =
				new System.Runtime.InteropServices.ComTypes.FILETIME();
			result.dwLowDateTime = (int)(time & 0xFFFFFFFFL);
			result.dwHighDateTime = (int)(time >> 32);

			return result;
		}

		/// <summary>
		/// Converts a Win32 Error code to a HRESULT.
		/// </summary>
		/// <param name="errorCode">The error code to convert.</param>
		/// <returns>A HRESULT value representing the error code.</returns>
		internal static int GetHRForWin32Error(int errorCode)
		{
			const uint FACILITY_WIN32 = 7;
			return errorCode <= 0 ? errorCode :
				(int)((((uint)errorCode) & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000);
		}

		/// <summary>
		/// Gets a Exception for the given Win32 error code.
		/// </summary>
		/// <param name="errorCode">The error code.</param>
		/// <returns>An exception object representing the error code.</returns>
		internal static Exception GetExceptionForWin32Error(int errorCode)
		{
			int HR = GetHRForWin32Error(errorCode);
			return Marshal.GetExceptionForHR(HR);
		}

		public static void GetFileTime(SafeFileHandle file, out DateTime creationTime,
			out DateTime accessedTime, out DateTime modifiedTime)
		{
			System.Runtime.InteropServices.ComTypes.FILETIME accessedTimeNative =
				new System.Runtime.InteropServices.ComTypes.FILETIME();
			System.Runtime.InteropServices.ComTypes.FILETIME modifiedTimeNative =
				new System.Runtime.InteropServices.ComTypes.FILETIME();
			System.Runtime.InteropServices.ComTypes.FILETIME createdTimeNative =
				new System.Runtime.InteropServices.ComTypes.FILETIME();

			if (!NativeMethods.GetFileTime(file, out createdTimeNative, out accessedTimeNative,
				out modifiedTimeNative))
			{
				throw GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}

			creationTime = FileTimeToDateTime(createdTimeNative);
			accessedTime = FileTimeToDateTime(accessedTimeNative);
			modifiedTime = FileTimeToDateTime(modifiedTimeNative);
		}

		public static void SetFileTime(SafeFileHandle file, DateTime creationTime,
			DateTime accessedTime, DateTime modifiedTime)
		{
			System.Runtime.InteropServices.ComTypes.FILETIME accessedTimeNative =
				new System.Runtime.InteropServices.ComTypes.FILETIME();
			System.Runtime.InteropServices.ComTypes.FILETIME modifiedTimeNative =
				new System.Runtime.InteropServices.ComTypes.FILETIME();
			System.Runtime.InteropServices.ComTypes.FILETIME createdTimeNative =
				new System.Runtime.InteropServices.ComTypes.FILETIME();

			if (!NativeMethods.GetFileTime(file, out createdTimeNative,
				out accessedTimeNative, out modifiedTimeNative))
			{
				throw KernelApi.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}

			if (creationTime != DateTime.MinValue)
				createdTimeNative = DateTimeToFileTime(creationTime);
			if (accessedTime != DateTime.MinValue)
				accessedTimeNative = DateTimeToFileTime(accessedTime);
			if (modifiedTime != DateTime.MinValue)
				modifiedTimeNative = DateTimeToFileTime(modifiedTime);

			if (!NativeMethods.SetFileTime(file, ref createdTimeNative,
				ref accessedTimeNative, ref modifiedTimeNative))
			{
				throw KernelApi.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Retrieves the current value of the high-resolution performance counter.
		/// </summary>
		public static long PerformanceCounter
		{
			get
			{
				long result = 0;
				if (NativeMethods.QueryPerformanceCounter(out result))
					return result;
				return 0;
			}
		}

		/// <summary>
		/// Gets the current CPU type of the system.
		/// </summary>
		/// <returns>One of the <see cref="ProcessorTypes"/> enumeration values.</returns>
		public static ProcessorArchitecture ProcessorArchitecture
		{
			get
			{
				NativeMethods.SYSTEM_INFO info = new NativeMethods.SYSTEM_INFO();
				NativeMethods.GetSystemInfo(out info);

				switch (info.processorArchitecture)
				{
					case NativeMethods.SYSTEM_INFO.ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64:
						return ProcessorArchitecture.Amd64;
					case NativeMethods.SYSTEM_INFO.ProcessorArchitecture.PROCESSOR_ARCHITECTURE_IA64:
						return ProcessorArchitecture.IA64;
					case NativeMethods.SYSTEM_INFO.ProcessorArchitecture.PROCESSOR_ARCHITECTURE_INTEL:
						return ProcessorArchitecture.X86;
					default:
						return ProcessorArchitecture.None;
				}
			}
		}

		/// <summary>
		/// Enables an application to inform the system that it is in use, thereby
		/// preventing the system from entering sleep or turning off the display
		/// while the application is running.
		/// </summary>
		/// <param name="executionState">The thread's execution requirements. This
		/// parameter can be one or more of the EXECUTION_STATE values.</param>
		/// <returns>If the function succeeds, the return value is the previous
		/// thread execution state.
		/// 
		/// If the function fails, the return value is NULL.</returns>
		/// <remarks>The system automatically detects activities such as local keyboard
		/// or mouse input, server activity, and changing window focus. Activities
		/// that are not automatically detected include disk or CPU activity and
		/// video display.
		/// 
		/// Calling SetThreadExecutionState without ES_CONTINUOUS simply resets
		/// the idle timer; to keep the display or system in the working state,
		/// the thread must call SetThreadExecutionState periodically.
		/// 
		/// To run properly on a power-managed computer, applications such as fax
		/// servers, answering machines, backup agents, and network management
		/// applications must use both ES_SYSTEM_REQUIRED and ES_CONTINUOUS when
		/// they process events. Multimedia applications, such as video players
		/// and presentation applications, must use ES_DISPLAY_REQUIRED when they
		/// display video for long periods of time without user input. Applications
		/// such as word processors, spreadsheets, browsers, and games do not need
		/// to call SetThreadExecutionState.
		/// 
		/// The ES_AWAYMODE_REQUIRED value should be used only when absolutely
		/// necessary by media applications that require the system to perform
		/// background tasks such as recording television content or streaming media
		/// to other devices while the system appears to be sleeping. Applications
		/// that do not require critical background processing or that run on
		/// portable computers should not enable away mode because it prevents
		/// the system from conserving power by entering true sleep.
		/// 
		/// To enable away mode, an application uses both ES_AWAYMODE_REQUIRED and
		/// ES_CONTINUOUS; to disable away mode, an application calls
		/// SetThreadExecutionState with ES_CONTINUOUS and clears
		/// ES_AWAYMODE_REQUIRED. When away mode is enabled, any operation that
		/// would put the computer to sleep puts it in away mode instead. The computer
		/// appears to be sleeping while the system continues to perform tasks that
		/// do not require user input. Away mode does not affect the sleep idle
		/// timer; to prevent the system from entering sleep when the timer expires,
		/// an application must also set the ES_SYSTEM_REQUIRED value.
		/// 
		/// The SetThreadExecutionState function cannot be used to prevent the user
		/// from putting the computer to sleep. Applications should respect that
		/// the user expects a certain behavior when they close the lid on their
		/// laptop or press the power button.
		/// 
		/// This function does not stop the screen saver from executing. 
		/// </remarks>
		public static ThreadExecutionState SetThreadExecutionState(
			ThreadExecutionState executionState)
		{
			return (ThreadExecutionState)NativeMethods.SetThreadExecutionState(
				(NativeMethods.EXECUTION_STATE)executionState);
		}

		public class DiskPerformanceInfo
		{
			unsafe internal DiskPerformanceInfo(NativeMethods.DiskPerformanceInfoInternal info)
			{
				BytesRead = info.BytesRead;
				BytesWritten = info.BytesWritten;
				ReadTime = info.ReadTime;
				WriteTime = info.WriteTime;
				IdleTime = info.IdleTime;
				ReadCount = info.ReadCount;
				WriteCount = info.WriteCount;
				QueueDepth = info.QueueDepth;
				SplitCount = info.SplitCount;
				QueryTime = info.QueryTime;
				StorageDeviceNumber = info.StorageDeviceNumber;
				StorageManagerName = new string((char*)info.StorageManagerName);
			}

			public long BytesRead { get; private set; }
			public long BytesWritten { get; private set; }
			public long ReadTime { get; private set; }
			public long WriteTime { get; private set; }
			public long IdleTime { get; private set; }
			public uint ReadCount { get; private set; }
			public uint WriteCount { get; private set; }
			public uint QueueDepth { get; private set; }
			public uint SplitCount { get; private set; }
			public long QueryTime { get; private set; }
			public uint StorageDeviceNumber { get; private set; }
			public string StorageManagerName { get; private set; }
		}

		/// <summary>
		/// Queries the performance information for the given disk.
		/// </summary>
		/// <param name="diskHandle">A read-only handle to a device (disk).</param>
		/// <returns>A DiskPerformanceInfo structure describing the performance
		/// information for the given disk.</returns>
		public static DiskPerformanceInfo QueryDiskPerformanceInfo(SafeFileHandle diskHandle)
		{
			if (diskHandle.IsInvalid)
				throw new ArgumentException("The disk handle must not be invalid.");

			//This only works if the user has turned on the disk performance
			//counters with 'diskperf -y'. These counters are off by default
			NativeMethods.DiskPerformanceInfoInternal result =
				new NativeMethods.DiskPerformanceInfoInternal();
			uint bytesReturned = 0;
			if (NativeMethods.DeviceIoControl(diskHandle, NativeMethods.IOCTL_DISK_PERFORMANCE,
				IntPtr.Zero, 0, out result, (uint)Marshal.SizeOf(result), out bytesReturned, IntPtr.Zero))
			{
				return new DiskPerformanceInfo(result);
			}

			return null;
		}

		/// <summary>
		/// Stores Kernel32.dll functions, structs and constants.
		/// </summary>
		internal static class NativeMethods
		{
			/// <summary>
			/// Closes an open object handle.
			/// </summary>
			/// <param name="hObject">A valid handle to an open object.</param>
			/// <returns>If the function succeeds, the return value is true. To get
			/// extended error information, call Marshal.GetLastWin32Error().
			/// 
			/// If the application is running under a debugger, the function will throw
			/// an exception if it receives either a handle value that is not valid
			/// or a pseudo-handle value. This can happen if you close a handle twice,
			/// or if you call CloseHandle on a handle returned by the FindFirstFile
			/// function.</returns>
			[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CloseHandle(IntPtr hObject);

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
			/// Retrieves a pseudo handle for the current process.
			/// </summary>
			/// <returns>A pseudo handle to the current process.</returns>
			/// <remarks>A pseudo handle is a special constant, currently (HANDLE)-1,
			/// that is interpreted as the current process handle. For compatibility
			/// with future operating systems, it is best to call GetCurrentProcess
			/// instead of hard-coding this constant value. The calling process can
			/// use a pseudo handle to specify its own process whenever a process
			/// handle is required. Pseudo handles are not inherited by child processes.
			/// 
			/// This handle has the maximum possible access to the process object.
			/// For systems that support security descriptors, this is the maximum
			/// access allowed by the security descriptor for the calling process.
			/// For systems that do not support security descriptors, this is
			/// PROCESS_ALL_ACCESS. For more information, see Process Security and
			/// Access Rights.
			/// 
			/// A process can create a "real" handle to itself that is valid in the
			/// context of other processes, or that can be inherited by other processes,
			/// by specifying the pseudo handle as the source handle in a call to the
			/// DuplicateHandle function. A process can also use the OpenProcess
			/// function to open a real handle to itself.
			/// 
			/// The pseudo handle need not be closed when it is no longer needed.
			/// Calling the CloseHandle function with a pseudo handle has no effect.
			/// If the pseudo handle is duplicated by DuplicateHandle, the duplicate
			/// handle must be closed.</remarks>
			[DllImport("Kernel32.dll", SetLastError = true)]
			public static extern IntPtr GetCurrentProcess();

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

			/// <summary>
			/// Execution state values to be used in conjuction with SetThreadExecutionState
			/// </summary>
			[Flags]
			public enum EXECUTION_STATE : uint
			{
				ES_AWAYMODE_REQUIRED = 0x00000040,
				ES_CONTINUOUS = 0x80000000,
				ES_DISPLAY_REQUIRED = 0x00000002,
				ES_SYSTEM_REQUIRED = 0x00000001,
				ES_USER_PRESENT = 0x00000004
			}

			[DllImport("Kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool AllocConsole();

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
				out DiskPerformanceInfoInternal lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned,
				IntPtr lpOverlapped);

			public const uint IOCTL_DISK_PERFORMANCE = ((0x00000007) << 16) | ((0x0008) << 2);

			public unsafe struct DiskPerformanceInfoInternal
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
				public fixed short StorageManagerName[8];
			}
		
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
			public static extern bool GetVolumePathNamesForVolumeName(
				string lpszVolumeName, IntPtr lpszVolumePathNames, uint cchBufferLength,
				out uint lpcchReturnLength);

			/// <summary>
			/// The WNetOpenEnum function starts an enumeration of network resources or
			/// existing connections. You can continue the enumeration by calling the
			/// WNetEnumResource function.
			/// </summary>
			/// <param name="dwScope">Scope of the enumeration. This parameter can be one
			/// of the following values.
			/// 
			/// Value						Meaning
			/// RESOURCE_CONNECTED			Enumerate all currently connected resources. The
			///								function ignores the dwUsage parameter. For more
			///								information, see the following Remarks section.
			///	RESOURCE_CONTEXT			Enumerate only resources in the network context
			///								of the caller. Specify this value for a Network
			///								Neighborhood view. The function ignores the dwUsage
			///								parameter.
			///	RESOURCE_GLOBALNET			Enumerate all resources on the network.
			///	RESOURCE_REMEMBERED			Enumerate all remembered (persistent)
			///								connections. The function ignores the
			///								dwUsage parameter.</param>
			/// <param name="dwType">Resource types to be enumerated. This parameter can be
			/// a combination of the following values. If a network provider cannot distinguish
			/// between print and disk resources, it can enumerate all resources.
			/// This parameter is ignored unless the dwScope parameter is equal to
			/// RESOURCE_GLOBALNET. For more information, see the following Remarks section.
			/// 
			/// Value						Meaning
			/// RESOURCETYPE_ANY			All resources. This value cannot be combined
			///								with RESOURCETYPE_DISK or RESOURCETYPE_PRINT.
			///	RESOURCETYPE_DISK			All disk resources.
			///	RESOURCETYPE_PRINT			All print resources.</param>
			/// <param name="dwUsage">Resource usage type to be enumerated. This parameter
			/// can be a combination of the following values.
			/// 
			/// Value						Meaning
			/// 0							All resources.
			/// RESOURCEUSAGE_CONNECTABLE	All connectable resources.
			/// RESOURCEUSAGE_CONTAINER		All container resources.
			/// RESOURCEUSAGE_ATTACHED		Setting this value forces WNetOpenEnum to fail if
			///								the user is not authenticated. The function fails
			///								even if the network allows enumeration without
			///								authentication.
			/// RESOURCEUSAGE_ALL			Setting this value is equivalent to setting
			///								RESOURCEUSAGE_CONNECTABLE, RESOURCEUSAGE_CONTAINER,
			///								and RESOURCEUSAGE_ATTACHED.</param>
			/// <param name="lpNetResource">Pointer to a NETRESOURCE structure that specifies
			/// the container to enumerate. If the dwScope parameter is not RESOURCE_GLOBALNET,
			/// this parameter must be NULL.
			/// 
			/// If this parameter is NULL, the root of the network is assumed. (The system
			/// organizes a network as a hierarchy; the root is the topmost container in the
			/// network.)
			/// 
			/// If this parameter is not NULL, it must point to a NETRESOURCE structure. This
			/// structure can be filled in by the application or it can be returned by a call
			/// to the WNetEnumResource function. The NETRESOURCE structure must specify a
			/// container resource; that is, the RESOURCEUSAGE_CONTAINER value must be
			/// specified in the dwUsage parameter.
			/// 
			/// To enumerate all network resources, an application can begin the enumeration
			/// by calling WNetOpenEnum with the lpNetResource parameter set to NULL, and
			/// then use the returned handle to call WNetEnumResource to enumerate resources.
			/// If one of the resources in the NETRESOURCE array returned by the
			/// WNetEnumResource function is a container resource, you can call WNetOpenEnum
			/// to open the resource for further enumeration.</param>
			/// <param name="lphEnum">Pointer to an enumeration handle that can be used
			/// in a subsequent call to WNetEnumResource.</param>
			/// <returns>If the function succeeds, the return value is NO_ERROR.
			/// 
			/// If the function fails, the return value is a system error code.</returns>
			[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
			public static extern uint WNetOpenEnum(uint dwScope, uint dwType, uint dwUsage,
				IntPtr lpNetResource, out IntPtr lphEnum);

			/// <summary>
			/// The WNetEnumResource function continues an enumeration of network resources
			/// that was started by a call to the WNetOpenEnum function.
			/// </summary>
			/// <param name="hEnum">Handle that identifies an enumeration instance. This
			/// handle must be returned by the WNetOpenEnum function.</param>
			/// <param name="lpcCount">Pointer to a variable specifying the number of
			/// entries requested. If the number requested is –1, the function returns
			/// as many entries as possible.
			/// 
			/// If the function succeeds, on return the variable pointed to by this
			/// parameter contains the number of entries actually read.</param>
			/// <param name="lpBuffer">Pointer to the buffer that receives the enumeration
			/// results. The results are returned as an array of NETRESOURCE
			/// structures. Note that the buffer you allocate must be large enough to
			/// hold the structures, plus the strings to which their members point.
			/// For more information, see the following Remarks section.
			/// 
			/// The buffer is valid until the next call using the handle specified by
			/// the hEnum parameter. The order of NETRESOURCE structures in the array
			/// is not predictable.</param>
			/// <param name="lpBufferSize">Pointer to a variable that specifies the size
			/// of the lpBuffer parameter, in bytes. If the buffer is too small to receive
			/// even one entry, this parameter receives the required size of the buffer.</param>
			/// <returns>If the function succeeds, the return value is one of the
			/// following values.
			/// Return code				Description
			/// NO_ERROR				The enumeration succeeded, and the buffer
			///							contains the requested data. The calling application
			///							can continue to call WNetEnumResource to complete
			///							the enumeration.
			///	ERROR_NO_MORE_ITEMS		There are no more entries. The buffer contents are undefined.
			///	
			/// If the function fails, the return value is a system error code.</returns>
			[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
			public static extern uint WNetEnumResource(IntPtr hEnum, ref uint lpcCount,
				IntPtr lpBuffer, ref uint lpBufferSize);

			/// <summary>
			/// The WNetCloseEnum function ends a network resource enumeration started
			/// by a call to the WNetOpenEnum function.
			/// </summary>
			/// <param name="hEnum">Handle that identifies an enumeration instance. This
			/// handle must be returned by the WNetOpenEnum function.</param>
			/// <returns>If the function succeeds, the return value is NO_ERROR.
			/// 
			/// If the function fails, the return value is a system error code.</returns>
			[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
			public static extern uint WNetCloseEnum(IntPtr hEnum);

			/// <summary>
			/// The NETRESOURCE structure contains information about a network resource.
			/// The structure is returned during an enumeration of network resources.
			/// The NETRESOURCE structure is also specified when making or querying a
			/// network connection with calls to various Windows Networking functions.
			/// </summary>
			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct NETRESOURCE
			{
				/// <summary>
				/// The scope of the enumeration. This member can be one of the following
				/// values.
				/// Value				Meaning
				/// RESOURCE_CONNECTED	Enumerate currently connected resources. The dwUsage
				///						member cannot be specified.
				///	RESOURCE_GLOBALNET	Enumerate all resources on the network. The dwUsage
				///						member is specified.
				///	RESOURCE_REMEMBERED	Enumerate remembered (persistent) connections. The
				///						dwUsage member cannot be specified.
				/// </summary>
				public uint dwScope;

				/// <summary>
				/// The type of resource. This member can be one of the following values
				/// defined in the Winnetwk.h header file.
				/// Value				Meaning
				/// RESOURCETYPE_ANY	All resources.
				/// RESOURCETYPE_DISK	Disk resources.
				/// RESOURCETYPE_PRINT	Print resources.
				/// 
				/// The WNetEnumResource function can also return the value
				/// RESOURCETYPE_UNKNOWN if a resource is neither a disk nor a print resource.
				/// </summary>
				public uint dwType;

				/// <summary>
				/// The display options for the network object in a network browsing
				/// user interface. This member can be one of the following values
				/// defined in the Winnetwk.h header file.
				/// 
				/// Value								Meaning
				/// RESOURCEDISPLAYTYPE_GENERIC			The method used to display the object
				/// 0x00000000							does not matter.
				/// RESOURCEDISPLAYTYPE_DOMAIN			The object should be displayed as a domain.
				/// 0x00000001
				/// RESOURCEDISPLAYTYPE_SERVER			The object should be displayed as a server.
				/// 0x00000002
				/// RESOURCEDISPLAYTYPE_SHARE			The object should be displayed as a share.
				/// 0x00000003
				/// RESOURCEDISPLAYTYPE_FILE			The object should be displayed as a file.
				/// 0x00000004
				/// RESOURCEDISPLAYTYPE_GROUP			The object should be displayed as a group.
				/// 0x00000005
				/// RESOURCEDISPLAYTYPE_NETWORK			The object should be displayed as a network.
				/// 0x00000006
				/// RESOURCEDISPLAYTYPE_ROOT			The object should be displayed as a logical
				/// 0x00000007							root for the entire network.
				/// RESOURCEDISPLAYTYPE_SHAREADMIN		The object should be displayed as an
				/// 0x00000008							administrative share.
				/// RESOURCEDISPLAYTYPE_DIRECTORY		The object should be displayed as a directory.
				/// 0x00000009
				/// RESOURCEDISPLAYTYPE_TREE			The object should be displayed as a tree.
				///	0x0000000A              			This display type was used for a NetWare
				///										Directory Service (NDS) tree by the NetWare
				///										Workstation service supported on Windows XP
				///										and earlier.
				///	RESOURCEDISPLAYTYPE_NDSCONTAINER	The object should be displayed as a
				///	0x0000000A							Netware Directory Service container.
				///										This display type was used by the NetWare
				///										Workstation service supported on Windows XP
				///										and earlier.
				/// </summary>
				public uint dwDisplayType;

				/// <summary>
				/// A set of bit flags describing how the resource can be used.
				/// Note that this member can be specified only if the dwScope member is
				/// equal to RESOURCE_GLOBALNET. This member can be one of the following
				/// values.
				/// Value						Meaning
				/// RESOURCEUSAGE_CONNECTABLE	The resource is a connectable resource; the
				/// 0x00000001					name pointed to by the lpRemoteName member
				///								can be passed to the WNetAddConnection function
				///								to make a network connection.
				///	RESOURCEUSAGE_CONTAINER		The resource is a container resource; the
				///	0x00000002					name pointed to by the lpRemoteName member can
				///								be passed to the WNetOpenEnum function to
				///								enumerate the resources in the container.
				///	RESOURCEUSAGE_NOLOCALDEVICE	The resource is not a local device.
				///	0x00000004
				///	RESOURCEUSAGE_SIBLING		The resource is a sibling. This value is not
				///	0x00000008					used by Windows.
				///	RESOURCEUSAGE_ATTACHED		The resource must be attached. This value
				///	0x00000010					specifies that a function to enumerate resource
				///								this should fail if the caller is not
				///								authenticated, even if the network permits
				///								enumeration without authentication.
				/// </summary>
				public uint dwUsage;

				/// <summary>
				/// If the dwScope member is equal to RESOURCE_CONNECTED or RESOURCE_REMEMBERED,
				/// this member is a pointer to a null-terminated character string that
				/// specifies the name of a local device. This member is NULL if the
				/// connection does not use a device.
				/// </summary>
				public string lpLocalName;

				/// <summary>
				/// If the entry is a network resource, this member is a pointer to a
				/// null-terminated character string that specifies the remote network name.
				/// 
				/// If the entry is a current or persistent connection, lpRemoteName member
				/// points to the network name associated with the name pointed to by the
				/// lpLocalName member.
				/// 
				/// The string can be MAX_PATH characters in length, and it must follow the
				/// network provider's naming conventions.
				/// </summary>
				public string lpRemoteName;

				/// <summary>
				/// A pointer to a NULL-terminated string that contains a comment supplied
				/// by the network provider.
				/// </summary>
				public string lpComment;

				/// <summary>
				/// A pointer to a NULL-terminated string that contains the name of the
				/// provider that owns the resource. This member can be NULL if the
				/// provider name is unknown. To retrieve the provider name, you can
				/// call the WNetGetProviderName function.
				/// </summary>
				public string lpProvider;
			}

			public const int RESOURCE_CONNECTED = 0x00000001;
			public const int RESOURCETYPE_DISK = 0x00000001;

			/// <summary>
			/// The WNetGetConnection function retrieves the name of the network
			/// resource associated with a local device.
			/// </summary>
			/// <param name="lpLocalName">Pointer to a constant null-terminated string
			/// that specifies the name of the local device to get the network name
			/// for.</param>
			/// <param name="lpRemoteName">Pointer to a null-terminated string that
			/// receives the remote name used to make the connection.</param>
			/// <param name="lpnLength">Pointer to a variable that specifies the
			/// size of the buffer pointed to by the lpRemoteName parameter, in
			/// characters. If the function fails because the buffer is not large
			/// enough, this parameter returns the required buffer size.</param>
			/// <returns>If the function succeeds, the return value is NO_ERROR.
			/// 
			/// If the function fails, the return value is a system error code.</returns>
			[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
			public static extern uint WNetGetConnection(string lpLocalName,
				StringBuilder lpRemoteName, ref uint lpnLength);

			public const int MaxPath = 260;
			public const int LongPath = 32768;
		}
	}

	public enum ThreadExecutionState
	{
		/// <summary>
		/// No specific state
		/// </summary>
		None = 0,

		/// <summary>
		/// Enables away mode. This value must be specified with ES_CONTINUOUS.
		/// 
		/// Away mode should be used only by media-recording and media-distribution
		/// applications that must perform critical background processing on
		/// desktop computers while the computer appears to be sleeping.
		/// See remarks.
		/// 
		/// Windows Server 2003 and Windows XP/2000: ES_AWAYMODE_REQUIRED is
		/// not supported.
		/// </summary>
		AwayModeRequired = (int)KernelApi.NativeMethods.EXECUTION_STATE.ES_AWAYMODE_REQUIRED,

		/// <summary>
		/// Informs the system that the state being set should remain in effect
		/// until the next call that uses ES_CONTINUOUS and one of the other
		/// state flags is cleared.
		/// </summary>
		Continuous = unchecked((int)KernelApi.NativeMethods.EXECUTION_STATE.ES_CONTINUOUS),

		/// <summary>
		/// Forces the display to be on by resetting the display idle timer.
		/// </summary>
		DisplayRequired = (int)KernelApi.NativeMethods.EXECUTION_STATE.ES_DISPLAY_REQUIRED,

		/// <summary>
		/// Forces the system to be in the working state by resetting the system
		/// idle timer.
		/// </summary>
		SystemRequired = (int)KernelApi.NativeMethods.EXECUTION_STATE.ES_SYSTEM_REQUIRED,

		/// <summary>
		/// This value is not supported. If ES_USER_PRESENT is combined with
		/// other esFlags values, the call will fail and none of the specified
		/// states will be set.
		/// 
		/// Windows Server 2003 and Windows XP/2000: Informs the system that a
		/// user is present and resets the display and system idle timers.
		/// ES_USER_PRESENT must be called with ES_CONTINUOUS.
		/// </summary>
		UserPresent = (int)KernelApi.NativeMethods.EXECUTION_STATE.ES_USER_PRESENT
	}
}