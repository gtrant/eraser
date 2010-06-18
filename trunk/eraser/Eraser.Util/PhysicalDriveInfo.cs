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
					string path = string.Format(CultureInfo.InvariantCulture,
						"\\Device\\Harddisk{0}\\Partition0", i);
					using (SafeFileHandle handle = OpenWin32Device(path))
					{
						if (handle.IsInvalid)
							break;

						result.Add(new PhysicalDriveInfo(i));
					}
				}

				return result.AsReadOnly();
			}
		}

		/// <summary>
		/// Opens a device in the Win32 Namespace.
		/// </summary>
		/// <param name="deviceName">The name of the device to open.</param>
		/// <returns>A <see cref="SafeFileHandle"/> to the device.</returns>
		private static SafeFileHandle OpenWin32Device(string deviceName)
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
					"\\\\.\\{0}", dosDeviceName), NativeMethods.FILE_READ_ATTRIBUTES,
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
					string path = string.Format(CultureInfo.InvariantCulture,
						"\\Device\\Harddisk{0}\\Partition{1}", Index, i);
					using (SafeFileHandle handle = OpenWin32Device(path))
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
		/// The regular expression which parses DOS Device names for the hard disk and partition.
		/// </summary>
		private static readonly Regex HarddiskPartitionRegex =
			new Regex("Harddisk([\\d]+)Partition([\\d]+)", RegexOptions.Compiled);
	}
}
