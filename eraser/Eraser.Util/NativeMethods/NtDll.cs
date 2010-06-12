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
	internal static partial class NativeMethods
	{
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
		public static extern uint NtQueryInformationFile(SafeFileHandle FileHandle,
			ref IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length,
			FILE_INFORMATION_CLASS FileInformationClass);

		/// <summary>
		/// The ZwSetInformationFile routine changes various kinds of information
		/// about a file object.
		/// </summary>
		/// <param name="FileHandle">Handle to the file object. This handle is
		/// created by a successful call to ZwCreateFile or ZwOpenFile.</param>
		/// <param name="IoStatusBlock">Pointer to an IO_STATUS_BLOCK structure
		/// that receives the final completion status and information about the
		/// requested operation. The Information member receives the number of
		/// bytes set on the file.</param>
		/// <param name="FileInformation">Pointer to a buffer that contains the
		/// information to set for the file. The particular structure in this
		/// buffer is determined by the FileInformationClass parameter. Setting
		/// any member of the structure to zero tells ZwSetInformationFile to
		/// leave the current information about the file for that member
		/// unchanged.</param>
		/// <param name="Length">The size, in bytes, of the FileInformation
		/// buffer.</param>
		/// <param name="FileInformationClass">The type of information, supplied in
		/// the buffer pointed to by FileInformation, to set for the file. Device
		/// and intermediate drivers can specify any of the
		/// <see cref="FILE_INFORMATION_CLASS"/> values.</param>
		/// <returns>ZwSetInformationFile returns STATUS_SUCCESS or an appropriate
		/// error status.</returns>
		/// <remarks>ZwSetInformationFile changes information about a file. It
		/// ignores any member of a FILE_XXX_INFORMATION structure that is not
		/// supported by a particular device or file system.
		/// 
		/// If you set FileInformationClass to FileDispositionInformation, you
		/// can subsequently pass FileHandle to ZwClose but not to any other
		/// ZwXxxFile routine. Because FileDispositionInformation causes the file
		/// to be marked for deletion, it is a programming error to attempt any
		/// subsequent operation on the handle other than closing it.
		/// 
		/// If you set FileInformationClass to FilePositionInformation, and the
		/// preceding call to ZwCreateFile included the FILE_NO_INTERMEDIATE_BUFFERING
		/// flag in the CreateOptions parameter, certain restrictions on the
		/// CurrentByteOffset member of the FILE_POSITION_INFORMATION structure
		/// are enforced. For more information, see ZwCreateFile.
		/// 
		/// If you set FileInformationClass to FileEndOfFileInformation, and the
		/// EndOfFile member of FILE_END_OF_FILE_INFORMATION specifies an offset
		/// beyond the current end-of-file mark, ZwSetInformationFile extends
		/// the file and pads the extension with zeros.</remarks>
		[DllImport("NtDll.dll")]
		public static extern uint NtSetInformationFile(SafeFileHandle FileHandle,
			out IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length,
			FILE_INFORMATION_CLASS FileInformationClass);

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

		#pragma warning disable 0649
		/// <summary>
		/// The FILE_BASIC_INFORMATION structure is used as an argument to routines
		/// that query or set file information.
		/// </summary>
		public struct FILE_BASIC_INFORMATION
		{
			/// <summary>
			/// Specifies the time that the file was created.
			/// </summary>
			public long CreationTime;

			/// <summary>
			/// Specifies the time that the file was last accessed.
			/// </summary>
			public long LastAccessTime;
			
			/// <summary>
			/// Specifies the time that the file was last written to.
			/// </summary>
			public long LastWriteTime;

			/// <summary>
			/// Specifies the last time the file was changed.
			/// </summary>
			public long ChangeTime;

			/// <summary>
			/// Specifies one or more FILE_ATTRIBUTE_XXX flags.
			/// </summary>
			public uint FileAttributes;
		}
		#pragma warning restore 0649

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
	}
}
