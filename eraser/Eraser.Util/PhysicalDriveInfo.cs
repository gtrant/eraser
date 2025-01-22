/* 
 * $Id: PhysicalDriveInfo.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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
using System.Linq;
using System.Text;

using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace Eraser.Util
{
	public class PhysicalDriveInfo
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="index">The physical drive index in the computer.</param>
		public PhysicalDriveInfo(int index)
		{
			Index = index;
		}

		/// <summary>
		/// The physical drive index of the current drive in the computer.
		/// </summary>
		public int Index
		{
			get;
			private set;
		}

		/// <summary>
		/// Lists all physical drives in the computer.
		/// </summary>
		public static IList<PhysicalDriveInfo> Drives
		{
			get
			{
				List<PhysicalDriveInfo> result = new List<PhysicalDriveInfo>();

				//Iterate over every hard disk index until we find one that doesn't exist.
				for (int i = 0; ; ++i)
				{
					using (SafeFileHandle handle = OpenWin32Device(GetDiskPath(i),
						NativeMethods.FILE_READ_ATTRIBUTES, FileShare.ReadWrite, FileOptions.None))
					{
						if (handle.IsInvalid)
							break;
					}

					result.Add(new PhysicalDriveInfo(i));
				}

				return result.AsReadOnly();
			}
		}

		/// <summary>
		/// Gets the volumes stored on that drive.
		/// </summary>
		public IList<VolumeInfo> Volumes
		{
			get
			{
				List<VolumeInfo> result = new List<VolumeInfo>();

				//Check every volume for which disk it is on.
				foreach (VolumeInfo info in VolumeInfo.Volumes)
				{
					try
					{
						if (Equals(info.PhysicalDrive))
							result.Add(info);
					}
					catch (FileNotFoundException)
					{
						//That means that the volume has been deleted already. We can't
						//do anything about it.
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Gets the size of the disk, in bytes.
		/// </summary>
		public long Size
		{
			get
			{
				using (SafeFileHandle handle = OpenWin32Device(GetDiskPath(),
					NativeMethods.GENERIC_READ, FileShare.ReadWrite, FileOptions.None))
				{
					if (handle.IsInvalid)
						throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

					long result = 0;
					uint returned = 0;
					if (NativeMethods.DeviceIoControl(handle, NativeMethods.IOCTL_DISK_GET_LENGTH_INFO,
						IntPtr.Zero, 0, out result, sizeof(long), out returned, IntPtr.Zero))
					{
						return result;
					}

					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
				}
			}
		}

		/// <summary>
		/// Destroys all partitioning information on the drive.
		/// </summary>
		public void DeleteDriveLayout()
		{
			//Open the drive for read/write access
			using (SafeFileHandle handle = OpenHandle(FileAccess.ReadWrite, FileShare.ReadWrite,
				FileOptions.None))
			{
				//Issue the IOCTL_DISK_DELETE_DRIVE_LAYOUT control code
				uint returnSize = 0;
				if (!NativeMethods.DeviceIoControl(handle,
					NativeMethods.IOCTL_DISK_DELETE_DRIVE_LAYOUT, IntPtr.Zero, 0, IntPtr.Zero,
					0, out returnSize, IntPtr.Zero))
				{
					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
				}
			}
		}

		/// <summary>
		/// Opens a file with read, write, or read/write access.
		/// </summary>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <returns>A System.IO.FileStream object opened in the specified mode
		/// and access, unshared, and no special file options.</returns>
		public FileStream Open(FileAccess access)
		{
			return Open(access, FileShare.None, FileOptions.None);
		}

		/// <summary>
		/// Opens a file with read, write, or read/write access and the specified
		/// sharing option.
		/// </summary>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <param name="share">A System.IO.FileShare constant specifying the type
		/// of access other FileStream objects have to this file.</param>
		/// <returns>A System.IO.FileStream object opened with the specified mode,
		/// access, sharing options, and no special file options.</returns>
		public FileStream Open(FileAccess access, FileShare share)
		{
			return Open(access, share, FileOptions.None);
		}

		/// <summary>
		/// Opens a file with read, write, or read/write access, the specified
		/// sharing option, and other advanced options.
		/// </summary>
		/// <param name="mode">A System.IO.FileMode constant specifying the mode
		/// (for example, Open or Append) in which to open the file.</param>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <param name="share">A System.IO.FileShare constant specifying the type
		/// of access other FileStream objects have to this file.</param>
		/// <param name="options">The System.IO.FileOptions constant specifying
		/// the advanced file options to use when opening the file.</param>
		/// <returns>A System.IO.FileStream object opened with the specified mode,
		/// access, sharing options, and special file options.</returns>
		public FileStream Open(FileAccess access, FileShare share, FileOptions options)
		{
			SafeFileHandle handle = OpenHandle(access, share, options);

			//Check that the handle is valid
			if (handle.IsInvalid)
			{
				int errorCode = Marshal.GetLastWin32Error();
				handle.Close();
				throw Win32ErrorCode.GetExceptionForWin32Error(errorCode);
			}

			//Return the stream
			return new PhysicalDriveStream(this, handle, access);
		}

		private SafeFileHandle OpenHandle(FileAccess access, FileShare share,
			FileOptions options)
		{
			//Access mode
			uint iAccess = 0;
			switch (access)
			{
				case FileAccess.Read:
					iAccess = NativeMethods.GENERIC_READ;
					break;
				case FileAccess.ReadWrite:
					iAccess = NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE;
					break;
				case FileAccess.Write:
					iAccess = NativeMethods.GENERIC_WRITE;
					break;
			}

			return OpenHandle(iAccess, share, options);
		}

		private SafeFileHandle OpenHandle(uint access, FileShare share,
			FileOptions options)
		{
			//Sharing mode
			if ((share & FileShare.Inheritable) != 0)
				throw new NotSupportedException("Inheritable handles are not supported.");

			//Advanced options
			if ((options & FileOptions.Asynchronous) != 0)
				throw new NotSupportedException("Asynchronous handles are not implemented.");

			//Create the handle
			SafeFileHandle result = OpenWin32Device(GetDiskPath(), access, share, options);
			if (result.IsInvalid)
			{
				int errorCode = Marshal.GetLastWin32Error();
				result.Close();
				throw Win32ErrorCode.GetExceptionForWin32Error(errorCode);
			}

			return result;
		}

		/// <summary>
		/// Opens a device in the Win32 Namespace.
		/// </summary>
		/// <param name="deviceName">The name of the device to open.</param>
		/// <param name="access">The access needed for the handle.</param>
		/// <returns>A <see cref="SafeFileHandle"/> to the device.</returns>
		private static SafeFileHandle OpenWin32Device(string deviceName, uint access,
			FileShare share, FileOptions options)
		{
			//Define the DOS device name for access
			string dosDeviceName = string.Format(CultureInfo.InvariantCulture,
				"eraser{0}_{1}", System.Diagnostics.Process.GetCurrentProcess().Id,
				System.Threading.Thread.CurrentThread.ManagedThreadId);
			if (!NativeMethods.DefineDosDevice(
				NativeMethods.DosDeviceDefineFlags.RawTargetPath, dosDeviceName,
				deviceName))
			{
				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}

			try
			{
				//Open the device handle.
				return NativeMethods.CreateFile(string.Format(CultureInfo.InvariantCulture,
					"\\\\.\\{0}", dosDeviceName), access, (uint)share, IntPtr.Zero,
					(int)FileMode.Open, (uint)options, IntPtr.Zero);
			}
			finally
			{
				//Then undefine the DOS device
				if (!NativeMethods.DefineDosDevice(
					NativeMethods.DosDeviceDefineFlags.ExactMatchOnRmove |
					NativeMethods.DosDeviceDefineFlags.RawTargetPath |
					NativeMethods.DosDeviceDefineFlags.RemoveDefinition,
					dosDeviceName, deviceName))
				{
					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
				}
			}
		}

		public override bool Equals(object obj)
		{
			PhysicalDriveInfo info = obj as PhysicalDriveInfo;
			if (info == null)
				return base.Equals(obj);

			return Index == info.Index;
		}

		public override int GetHashCode()
		{
			return Index.GetHashCode();
		}

		/// <summary>
		/// The format string for accessing partitions.
		/// </summary>
		private static readonly string PartitionFormat = "\\Device\\Harddisk{0}\\Partition{1}";

		/// <summary>
		/// Gets the disk device name.
		/// </summary>
		/// <param name="disk">The zero-based disk index.</param>
		/// <returns>The device name of the disk.</returns>
		private static string GetDiskPath(int disk)
		{
			return string.Format(CultureInfo.InvariantCulture, PartitionFormat, disk, 0);
		}

		/// <summary>
		/// Gets the current disk device name.
		/// </summary>
		/// <returns>The device name of the disk.</returns>
		private string GetDiskPath()
		{
			return GetDiskPath(Index);
		}

		/// <summary>
		/// Gets the partition device name.
		/// </summary>
		/// <param name="partition">The one-based partition index.</param>
		/// <returns>The device name of the partition.</returns>
		private string GetPartitionPath(int partition)
		{
			return string.Format(CultureInfo.InvariantCulture, PartitionFormat, Index, partition);
		}
	}

	public class PhysicalDriveStream : FileStream
	{
		internal PhysicalDriveStream(PhysicalDriveInfo drive, SafeFileHandle handle,
			FileAccess access)
			: base(handle, access)
		{
			Drive = drive;
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		public override long Length
		{
			get
			{
				return Drive.Size;
			}
		}

		/// <summary>
		/// The <see cref="VolumeInfo"/> object this stream is encapsulating.
		/// </summary>
		private PhysicalDriveInfo Drive;
	}
}
