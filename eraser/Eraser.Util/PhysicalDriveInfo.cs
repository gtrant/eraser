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
						NativeMethods.FILE_READ_ATTRIBUTES))
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
		/// Opens a device in the Win32 Namespace.
		/// </summary>
		/// <param name="deviceName">The name of the device to open.</param>
		/// <param name="access">The access needed for the handle.</param>
		/// <returns>A <see cref="SafeFileHandle"/> to the device.</returns>
		private static SafeFileHandle OpenWin32Device(string deviceName, uint access)
		{
			//Define the DOS device name for access
			string dosDeviceName = string.Format(CultureInfo.InvariantCulture,
				"eraser{0}_{1}", System.Diagnostics.Process.GetCurrentProcess().Id,
				System.AppDomain.GetCurrentThreadId());
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
					"\\\\.\\{0}", dosDeviceName), access,
					NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, IntPtr.Zero,
					(int)FileMode.Open, (uint)FileAttributes.ReadOnly, IntPtr.Zero);
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

		/// <summary>
		/// Gets the volumes stored on that drive.
		/// </summary>
		public IList<VolumeInfo> Volumes
		{
			get
			{
				List<VolumeInfo> result = new List<VolumeInfo>();

				//Check every partition index on this drive.
				for (int i = 1; ; ++i)
				{
					string path = GetPartitionPath(i);
					using (SafeFileHandle handle = OpenWin32Device(path,
						NativeMethods.FILE_READ_ATTRIBUTES))
					{
						if (handle.IsInvalid)
							break;
					}

					//This partition index is valid. Check which VolumeInfo this maps to.
					foreach (VolumeInfo info in VolumeInfo.Volumes)
					{
						//Only check local drives
						if (info.VolumeId.Substring(0, 4) == "\\\\?\\")
						{
							//Check whether the DOS Device maps to the target of the symbolic link
							if (NativeMethods.NtQuerySymbolicLink(path) ==
								NativeMethods.QueryDosDevice(info.VolumeId.Substring(
									4, info.VolumeId.Length - 5)))
							{
								//Yes, this volume belongs to this disk
								result.Add(info);
								break;
							}
						}
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
					NativeMethods.GENERIC_READ))
				{
					if (handle.IsInvalid)
						throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

					long result = 0;
					uint returned = 0;
					if (NativeMethods.DeviceIoControl(handle, NativeMethods.IOCTL_DISK_GET_LENGTH_INFO,
						IntPtr.Zero, 0, out result, out returned, IntPtr.Zero))
					{
						return result;
					}

					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
				}
			}
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
}
