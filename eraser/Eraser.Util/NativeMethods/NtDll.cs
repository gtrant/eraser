/* 
 * $Id: NtDll.cs 2993 2021-09-25 17:23:27Z gtrant $
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
			out IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length,
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

		/// <summary>
		/// Represents a counted Unicode string.
		/// </summary>
		private struct UNICODE_STRING
		{
			/// <summary>
			/// Constructs a UNICODE_STRING object from an existing <see cref="System.String"/>
			/// object.
			/// </summary>
			/// <param name="str">The string to construct from.</param>
			public UNICODE_STRING(string str)
			{
				if (string.IsNullOrEmpty(str))
					MaximumLength = Length = 0;
				else
					MaximumLength = Length = checked((ushort)(str.Length * sizeof(char)));
				Buffer = str;
			}

			/// <summary>
			/// Allocates an empty string of the given length.
			/// </summary>
			/// <param name="length">The length, in characters, to allocate.</param>
			public UNICODE_STRING(ushort length)
			{
				MaximumLength = checked((ushort)(length * sizeof(char)));
				Length = 0;
				Buffer = new string('\0', length);
			}
			
			public override string ToString()
			{
				if (Length / sizeof(char) > Buffer.Length)
					return Buffer + new string('\0', Length - Buffer.Length / sizeof(char));
				else
					return Buffer.Substring(0, Length / sizeof(char));
			}

			/// <summary>
			/// Specifies the length, in bytes, of the string pointed to by the Buffer
			/// member, not including the terminating NULL character, if any.
			/// </summary>
			public ushort Length;

			/// <summary>
			/// Specifies the total size, in bytes, of memory allocated for Buffer. Up to
			/// MaximumLength bytes may be written into the buffer without trampling memory.
			/// </summary>
			public ushort MaximumLength;

			/// <summary>
			/// Pointer to a wide-character string.
			/// </summary>
			[MarshalAs(UnmanagedType.LPWStr)]
			public string Buffer;
		}

		/// <summary>
		/// The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to
		/// objects or object handles by routines that create objects and/or return
		/// handles to objects.
		/// </summary>
		private struct OBJECT_ATTRIBUTES : IDisposable
		{
			public OBJECT_ATTRIBUTES(UNICODE_STRING objectName)
				: this()
			{
				Length = (uint)Marshal.SizeOf(this);
				ObjectName = Marshal.AllocHGlobal(Marshal.SizeOf(objectName));
				Marshal.StructureToPtr(objectName, ObjectName, false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public void Dispose(bool disposing)
			{
				if (ObjectName != IntPtr.Zero)
					Marshal.FreeHGlobal(ObjectName);
			}
			
			/// <summary>
			/// The number of bytes of data contained in this structure.
			/// </summary>
			public uint Length;

			/// <summary>
			/// Optional handle to the root object directory for the path name specified by
			/// the ObjectName member. If RootDirectory is NULL, ObjectName must point
			/// to a fully-qualified object name that includes the full path to the target
			/// object. If RootDirectory is non-NULL, ObjectName specifies an object name
			/// relative to the RootDirectory directory. The RootDirectory handle can refer
			/// to a file system directory or an object directory in the object manager
			/// namespace.
			/// </summary>
			public IntPtr RootDirectory;

			/// <summary>
			/// Pointer to a Unicode string that contains the name of the object for which
			/// a handle is to be opened. This must either be a fully qualified object name,
			/// or a relative path name to the directory specified by the RootDirectory
			/// member.
			/// </summary>
			public IntPtr ObjectName;

			/// <summary>
			/// Bitmask of flags that specify object handle attributes. This member can
			/// contain one or more of the flags in the following table.
			/// </summary>
			OBJECT_ATTRIBUTESFlags Attributes;

			/// <summary>
			/// Specifies a security descriptor (SECURITY_DESCRIPTOR) for the object
			/// when the object is created. If this member is NULL, the object will
			/// receive default security settings.
			/// </summary>
			IntPtr SecurityDescriptor;

			/// <summary>
			/// Optional quality of service to be applied to the object when it is created.
			/// Used to indicate the security impersonation level and context tracking mode
			/// (dynamic or static). Currently, the InitializeObjectAttributes macro sets
			/// this member to NULL.
			/// </summary>
			IntPtr SecurityQualityOfService;
		}

		[Flags]
		public enum OBJECT_ATTRIBUTESFlags
		{
			/// <summary>
			/// No flags specified.
			/// </summary>
			None = 0
		}

		/// <summary>
		/// Opens an existing symbolic link.
		/// </summary>
		/// <param name="LinkHandle">A handle to the newly opened symbolic link object.</param>
		/// <param name="DesiredAccess">An ACCESS_MASK that specifies the requested access
		/// to the directory object. It is typical to use GENERIC_READ so the handle can be
		/// passed to the NtQueryDirectoryObject function.</param>
		/// <param name="ObjectAttributes">The attributes for the directory object.</param>
		/// <returns>The function returns STATUS_SUCCESS or an error status.</returns>
		[DllImport("NtDll.dll")]
		private static extern uint NtOpenSymbolicLinkObject(out IntPtr LinkHandle,
			uint DesiredAccess, ref OBJECT_ATTRIBUTES ObjectAttributes);

		/// <summary>
		/// Retrieves the target of a symbolic link.
		/// </summary>
		/// <param name="LinkHandle">A handle to the symbolic link object.</param>
		/// <param name="LinkTarget">A pointer to an initialized Unicode string that receives
		/// the target of the symbolic link. The MaximumLength and Buffer members must be
		/// set if the call fails.</param>
		/// <param name="ReturnedLength">A pointer to a variable that receives the length of
		/// the Unicode string returned in the LinkTarget parameter. If the function
		/// returns STATUS_BUFFER_TOO_SMALL, this variable receives the required buffer
		/// size.</param>
		/// <returns>The function returns STATUS_SUCCESS or an error status.</returns>
		[DllImport("NtDll.dll")]
		private static extern uint NtQuerySymbolicLinkObject(IntPtr LinkHandle,
			ref UNICODE_STRING LinkTarget, out uint ReturnedLength);

		/// <summary>
		/// Queries the provided symbolic link for its target.
		/// </summary>
		/// <param name="path">The path to query.</param>
		/// <returns>The destination of the symbolic link.</returns>
		public static string NtQuerySymbolicLink(string path)
		{
			uint status = 0;
			IntPtr handle = IntPtr.Zero;
			UNICODE_STRING drive = new UNICODE_STRING(path);
			OBJECT_ATTRIBUTES attributes = new OBJECT_ATTRIBUTES(drive);

			try
			{
				status = NtOpenSymbolicLinkObject(out handle, GENERIC_READ, ref attributes);
				if (status != 0)
					return null;
			}
			finally
			{
				attributes.Dispose();
			}

			UNICODE_STRING target = new UNICODE_STRING(MaxPath);
			uint length = 0;
			for ( ; ; )
			{
				status = NtQuerySymbolicLinkObject(handle, ref target, out length);
				if (status == 0)
					break;
				else if (status == 0xC0000023L) //STATUS_BUFFER_TOO_SMALL
					target = new UNICODE_STRING(target.MaximumLength);
				else
					return null;
			}

			return target.ToString();
		}
	}
}
