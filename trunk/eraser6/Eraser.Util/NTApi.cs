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
			/// The ZwQueryInformationFile routine returns various kinds of information
			/// about a file object.
			/// </summary>
			/// <param name="FileHandle">Handle to a file object. The handle is created
			/// by a successful call to ZwCreateFile or ZwOpenFile.</param>
			/// <param name="IoStatusBlock">Pointer to an IO_STATUS_BLOCK structure
			/// that receives the final completion status and information about
			/// the operation. The Information member receives the number of bytes
			/// that this routine actually writes to the FileInformation buffer.</param>
			/// <param name="FileInformation">Pointer to a caller-allocated buffer
			/// into which the routine writes the requested information about the
			/// file object. The FileInformationClass parameter specifies the type
			/// of information that the caller requests.</param>
			/// <param name="Length">The size, in bytes, of the buffer pointed to
			/// by FileInformation.</param>
			/// <param name="FileInformationClass">Specifies the type of information
			/// to be returned about the file, in the buffer that FileInformation
			/// points to. Device and intermediate drivers can specify any of the
			/// following FILE_INFORMATION_CLASS enumeration values, which are defined
			/// in header file Wdm.h.</param>
			/// <returns>ZwQueryInformationFile returns STATUS_SUCCESS or an appropriate
			/// NTSTATUS error code.</returns>
			[DllImport("NtDll.dll")]
			private static extern uint NtQueryInformationFile(SafeFileHandle FileHandle,
				ref IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length,
				FILE_INFORMATION_CLASS FileInformationClass);

			public static FILE_STREAM_INFORMATION[] NtQueryInformationFile(SafeFileHandle FileHandle)
			{
				IO_STATUS_BLOCK status = new IO_STATUS_BLOCK();
				IntPtr fileInfoPtr = IntPtr.Zero;

				try
				{
					FILE_STREAM_INFORMATION streamInfo = new FILE_STREAM_INFORMATION();
					int fileInfoPtrLength = (Marshal.SizeOf(streamInfo) + 32768) / 2;
					uint ntStatus = 0;

					do
					{
						fileInfoPtrLength *= 2;
						if (fileInfoPtr != IntPtr.Zero)
							Marshal.FreeHGlobal(fileInfoPtr);
						fileInfoPtr = Marshal.AllocHGlobal(fileInfoPtrLength);

						ntStatus = NtQueryInformationFile(FileHandle, ref status, fileInfoPtr,
							(uint)fileInfoPtrLength, FILE_INFORMATION_CLASS.FileStreamInformation);
					}
					while (ntStatus != 0 /*STATUS_SUCCESS*/ && ntStatus == 0x80000005 /*STATUS_BUFFER_OVERFLOW*/);

					//Marshal the structure manually (argh!)
					List<FILE_STREAM_INFORMATION> result = new List<FILE_STREAM_INFORMATION>();
					unsafe
					{
						for (byte* i = (byte*)fileInfoPtr; streamInfo.NextEntryOffset != 0;
							i += streamInfo.NextEntryOffset)
						{
							byte* currStreamPtr = i;
							streamInfo.NextEntryOffset = *(uint*)currStreamPtr;
							currStreamPtr += sizeof(uint);

							streamInfo.StreamNameLength = *(uint*)currStreamPtr;
							currStreamPtr += sizeof(uint);

							streamInfo.StreamSize = *(long*)currStreamPtr;
							currStreamPtr += sizeof(long);

							streamInfo.StreamAllocationSize = *(long*)currStreamPtr;
							currStreamPtr += sizeof(long);

							streamInfo.StreamName = Marshal.PtrToStringUni((IntPtr)currStreamPtr,
								(int)streamInfo.StreamNameLength / 2);
							result.Add(streamInfo);
						}
					}

					return result.ToArray();
				}
				finally
				{
					Marshal.FreeHGlobal(fileInfoPtr);
				}
			}

			public struct IO_STATUS_BLOCK
			{
				public IntPtr PointerStatus;
				public UIntPtr Information;
			}
			
			public struct FILE_STREAM_INFORMATION
			{
				/// <summary>
				/// The offset of the next FILE_STREAM_INFORMATION entry. This
				/// member is zero if no other entries follow this one. 
				/// </summary>
				public uint NextEntryOffset;

				/// <summary>
				/// Length, in bytes, of the StreamName string. 
				/// </summary>
				public uint StreamNameLength;

				/// <summary>
				/// Size, in bytes, of the stream. 
				/// </summary>
				public long StreamSize;

				/// <summary>
				/// File stream allocation size, in bytes. Usually this value
				/// is a multiple of the sector or cluster size of the underlying
				/// physical device.
				/// </summary>
				public long StreamAllocationSize;

				/// <summary>
				/// Unicode string that contains the name of the stream. 
				/// </summary>
				public string StreamName;
			}

			public enum FILE_INFORMATION_CLASS
			{
				FileDirectoryInformation = 1,
				FileFullDirectoryInformation,
				FileBothDirectoryInformation,
				FileBasicInformation,
				FileStandardInformation,
				FileInternalInformation,
				FileEaInformation,
				FileAccessInformation,
				FileNameInformation,
				FileRenameInformation,
				FileLinkInformation,
				FileNamesInformation,
				FileDispositionInformation,
				FilePositionInformation,
				FileFullEaInformation,
				FileModeInformation,
				FileAlignmentInformation,
				FileAllInformation,
				FileAllocationInformation,
				FileEndOfFileInformation,
				FileAlternateNameInformation,
				FileStreamInformation,
				FilePipeInformation,
				FilePipeLocalInformation,
				FilePipeRemoteInformation,
				FileMailslotQueryInformation,
				FileMailslotSetInformation,
				FileCompressionInformation,
				FileCopyOnWriteInformation,
				FileCompletionInformation,
				FileMoveClusterInformation,
				FileQuotaInformation,
				FileReparsePointInformation,
				FileNetworkOpenInformation,
				FileObjectIdInformation,
				FileTrackingInformation,
				FileOleDirectoryInformation,
				FileContentIndexInformation,
				FileInheritContentIndexInformation,
				FileOleInformation,
				FileMaximumInformation
			}

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
