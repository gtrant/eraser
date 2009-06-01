/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
	public static class NTApi
	{
		/// <summary>
		/// Queries system parameters using the NT Native API.
		/// </summary>
		/// <param name="type">The type of information to retrieve.</param>
		/// <param name="data">The buffer to receive the information.</param>
		/// <param name="maxSize">The size of the buffer.</param>
		/// <param name="dataSize">Receives the amount of data written to the
		/// buffer.</param>
		/// <returns>A system error code.</returns>
		public static uint NtQuerySystemInformation(uint type, byte[] data,
			uint maxSize, out uint dataSize)
		{
			return NativeMethods.NtQuerySystemInformation(type, data, maxSize,
				out dataSize);
		}

		internal static class NativeMethods
		{
			/// <summary>
			/// Queries system parameters using the NT Native API.
			/// </summary>
			/// <param name="dwType">The type of information to retrieve.</param>
			/// <param name="dwData">The buffer to receive the information.</param>
			/// <param name="dwMaxSize">The size of the buffer.</param>
			/// <param name="dwDataSize">Receives the amount of data written to the
			/// buffer.</param>
			/// <returns>A system error code.</returns>
			[DllImport("NtDll.dll")]
			public static extern uint NtQuerySystemInformation(uint dwType, byte[] dwData,
				uint dwMaxSize, out uint dwDataSize);

			/// <summary>
			/// Represents volume data. This structure is passed to the
			/// FSCTL_GET_NTFS_VOLUME_DATA control code.
			/// </summary>
			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct NTFS_VOLUME_DATA_BUFFER
			{
				/// <summary>
				/// The serial number of the volume. This is a unique number assigned
				/// to the volume media by the operating system.
				/// </summary>
				public long VolumeSerialNumber;

				/// <summary>
				/// The number of sectors in the specified volume.
				/// </summary>
				public long NumberSectors;

				/// <summary>
				/// The number of used and free clusters in the specified volume.
				/// </summary>
				public long TotalClusters;

				/// <summary>
				/// The number of free clusters in the specified volume.
				/// </summary>
				public long FreeClusters;

				/// <summary>
				/// The number of reserved clusters in the specified volume.
				/// </summary>
				public long TotalReserved;

				/// <summary>
				/// The number of bytes in a sector on the specified volume.
				/// </summary>
				public uint BytesPerSector;

				/// <summary>
				/// The number of bytes in a cluster on the specified volume. This
				/// value is also known as the cluster factor.
				/// </summary>
				public uint BytesPerCluster;

				/// <summary>
				/// The number of bytes in a file record segment.
				/// </summary>
				public uint BytesPerFileRecordSegment;

				/// <summary>
				/// The number of clusters in a file record segment.
				/// </summary>
				public uint ClustersPerFileRecordSegment;

				/// <summary>
				/// The length of the master file table, in bytes.
				/// </summary>
				public long MftValidDataLength;

				/// <summary>
				/// The starting logical cluster number of the master file table.
				/// </summary>
				public long MftStartLcn;

				/// <summary>
				/// The starting logical cluster number of the master file table mirror.
				/// </summary>
				public long Mft2StartLcn;

				/// <summary>
				/// The starting logical cluster number of the master file table zone.
				/// </summary>
				public long MftZoneStart;

				/// <summary>
				/// The ending logical cluster number of the master file table zone.
				/// </summary>
				public long MftZoneEnd;

				public uint ByteCount;
				public ushort MajorVersion;
				public ushort MinorVersion;
			}

			/// <summary>
			/// Retrieves information about the specified NTFS file system volume.
			/// </summary>
			public const int FSCTL_GET_NTFS_VOLUME_DATA = (9 << 16) | (25 << 2);

			/// <summary>
			/// Sends the FSCTL_GET_NTFS_VOLUME_DATA control code, returning the resuling
			/// NTFS_VOLUME_DATA_BUFFER.
			/// </summary>
			/// <param name="volume">The volume to query.</param>
			/// <returns>The NTFS_VOLUME_DATA_BUFFER structure representing the data
			/// file systme structures for the volume.</returns>
			public static NTFS_VOLUME_DATA_BUFFER GetNtfsVolumeData(VolumeInfo volume)
			{
				using (SafeFileHandle volumeHandle = KernelApi.NativeMethods.CreateFile(
					volume.VolumeId.Remove(volume.VolumeId.Length - 1),
					KernelApi.NativeMethods.GENERIC_READ, KernelApi.NativeMethods.FILE_SHARE_READ |
					KernelApi.NativeMethods.FILE_SHARE_WRITE, IntPtr.Zero,
					KernelApi.NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero))
				{
					uint resultSize = 0;
					NTFS_VOLUME_DATA_BUFFER volumeData = new NTFS_VOLUME_DATA_BUFFER();
					if (DeviceIoControl(volumeHandle, FSCTL_GET_NTFS_VOLUME_DATA,
						IntPtr.Zero, 0, out volumeData, (uint)Marshal.SizeOf(volumeData),
						out resultSize, IntPtr.Zero))
					{
						return volumeData;
					}

					throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
				}
			}

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool DeviceIoControl(SafeFileHandle hDevice,
				uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
				out NTFS_VOLUME_DATA_BUFFER lpOutBuffer, uint nOutBufferSize,
				out uint lpBytesReturned, IntPtr lpOverlapped);
		}
	}
}
