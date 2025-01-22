/* 
 * $Id: NtfsApi.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.IO;

namespace Eraser.Util
{
	public static class NtfsApi
	{
		/// <summary>
		/// Gets the actual size of the MFT.
		/// </summary>
		/// <param name="volume">The volume to query.</param>
		/// <returns>The size of the MFT.</returns>
		public static long GetMftValidSize(VolumeInfo volume)
		{
			NativeMethods.NTFS_VOLUME_DATA_BUFFER? volumeData = GetNtfsVolumeData(volume);
			if (volumeData == null)
				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

			return volumeData.Value.MftValidDataLength;
		}

		/// <summary>
		/// Gets the size of one MFT record segment.
		/// </summary>
		/// <param name="volume">The volume to query.</param>
		/// <returns>The size of one MFT record segment.</returns>
		public static long GetMftRecordSegmentSize(VolumeInfo volume)
		{
			try
			{
				return GetNtfsVolumeData(volume).BytesPerFileRecordSegment;
			}
			catch (UnauthorizedAccessException)
			{
				return Math.Min(volume.ClusterSize, 1024);
			}
		}

		/// <summary>
		/// Sends the FSCTL_GET_NTFS_VOLUME_DATA control code, returning the resuling
		/// NTFS_VOLUME_DATA_BUFFER.
		/// </summary>
		/// <param name="volume">The volume to query.</param>
		/// <returns>The NTFS_VOLUME_DATA_BUFFER structure representing the data
		/// file system structures for the volume, or null if the data could not be
		/// retrieved.</returns>
		internal static NativeMethods.NTFS_VOLUME_DATA_BUFFER GetNtfsVolumeData(VolumeInfo volume)
		{
			using (FileStream stream = volume.Open(FileAccess.Read, FileShare.ReadWrite,
				FileOptions.None))
			using (SafeFileHandle handle = stream.SafeFileHandle)
			{
				uint resultSize = 0;
				NativeMethods.NTFS_VOLUME_DATA_BUFFER volumeData =
					new NativeMethods.NTFS_VOLUME_DATA_BUFFER();
				if (NativeMethods.DeviceIoControl(handle,
					NativeMethods.FSCTL_GET_NTFS_VOLUME_DATA, IntPtr.Zero, 0, out volumeData,
					(uint)Marshal.SizeOf(volumeData), out resultSize, IntPtr.Zero))
				{
					return volumeData;
				}

				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}
	}
}