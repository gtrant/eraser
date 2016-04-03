﻿/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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

using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Globalization;
using System.Windows.Forms;

namespace Eraser.Util.ExtensionMethods
{
	/// <summary>
	/// Implements extension methods for IO-bound operations.
	/// </summary>
	public static partial class Methods
	{
		/// <summary>
		/// Copies the file times from the provided file.
		/// </summary>
		/// <param name="rhs">The file times to copy from.</param>
		public static void CopyTimes(this FileSystemInfo lhs, FileSystemInfo rhs)
		{
			lhs.SetTimes(rhs.GetLastUpdateTime(), rhs.CreationTime, rhs.LastWriteTime,
				rhs.LastAccessTime);
		}

		/// <summary>
		/// Gets the NTFS last-updated time from the object.
		/// </summary>
		/// <param name="info">The <see cref="FileSystemInfo"/> object to query.</param>
		/// <returns>The time the file object was last updated.</returns>
		public static DateTime GetLastUpdateTime(this FileSystemInfo info)
		{
			using (SafeFileHandle handle = OpenHandle(info, NativeMethods.FILE_READ_ATTRIBUTES))
			{
				return GetUpdateTime(handle);
			}
		}

		/// <summary>
		/// Deeply sets the file times associated with the current
		/// <see cref="FileSystemInfo"/> object.
		/// </summary>
		/// <param name="updateTime">The time the basic information was last set.</param>
		/// <param name="createdTime">The time the file was created.</param>
		/// <param name="lastModifiedTime">The time the file was last modified.</param>
		/// <param name="lastAccessedTime">The time the file was last accessed.</param>
		public static void SetTimes(this FileSystemInfo info, DateTime updateTime,
			DateTime createdTime, DateTime lastModifiedTime, DateTime lastAccessedTime)
		{
			using (SafeFileHandle handle = OpenHandle(info, NativeMethods.FILE_WRITE_ATTRIBUTES))
			{
				SetTimes(handle, updateTime, createdTime, lastModifiedTime, lastAccessedTime);
			}
		}

		/// <summary>
		/// Opens a handle to the file system info object. This can be directories
		/// or files.
		/// </summary>
		/// <param name="info">The file system object to open a handle on.</param>
		/// <returns>The file handle to the object. This handle is guaranteed to be
		/// valid.</returns>
		private static SafeFileHandle OpenHandle(FileSystemInfo info, uint desiredAccess)
		{
			uint flagsAndAttributes = 0;
			if (info is DirectoryInfo)
				flagsAndAttributes |= NativeMethods.FILE_FLAG_BACKUP_SEMANTICS;

			SafeFileHandle handle = NativeMethods.CreateFile(info.FullName, desiredAccess,
				(uint)FileShare.ReadWrite, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
				flagsAndAttributes, IntPtr.Zero);
			if (handle != null && !handle.IsInvalid)
				return handle;

			//If we fall through here, it is a reparse point (most likely) and
			//the target of the reparse point does not exist. We would then have to
			//set the time of the reparse point.
			handle = NativeMethods.CreateFile(info.FullName, desiredAccess,
				(uint)FileShare.ReadWrite, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
				flagsAndAttributes | NativeMethods.FILE_FLAG_BACKUP_SEMANTICS |
				NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
			if (handle == null || handle.IsInvalid)
				throw new IOException(S._("The file {0} cannot be opened for access.",
						info.FullName));

			return handle;
		}

		private static DateTime GetUpdateTime(SafeFileHandle handle)
		{
			NativeMethods.FILE_BASIC_INFORMATION fileInfo =
				new NativeMethods.FILE_BASIC_INFORMATION();
			IntPtr fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fileInfo));
			
			try
			{
				NativeMethods.IO_STATUS_BLOCK status;
				uint result = NativeMethods.NtQueryInformationFile(handle,
					out status, fileInfoPtr, (uint)Marshal.SizeOf(fileInfo),
					NativeMethods.FILE_INFORMATION_CLASS.FileBasicInformation);

				if (result != 0)
					throw new IOException();

				fileInfo = (NativeMethods.FILE_BASIC_INFORMATION)
					Marshal.PtrToStructure(fileInfoPtr, fileInfo.GetType());
				return DateTime.FromFileTime(fileInfo.ChangeTime);
			}
			finally
			{
				Marshal.FreeHGlobal(fileInfoPtr);
			}
		}

		internal static void SetTimes(SafeFileHandle handle, DateTime updateTime,
			DateTime createdTime, DateTime lastModifiedTime, DateTime lastAccessedTime)
		{
			NativeMethods.FILE_BASIC_INFORMATION fileInfo =
				new NativeMethods.FILE_BASIC_INFORMATION() {
					ChangeTime = updateTime.ToFileTime(),
					CreationTime = createdTime.ToFileTime(),
					LastAccessTime = lastAccessedTime.ToFileTime(),
					LastWriteTime = lastModifiedTime.ToFileTime()
				};

			if (fileInfo.ChangeTime == 0)
				throw new ArgumentOutOfRangeException("updateTime");
			if (fileInfo.CreationTime == 0)
				throw new ArgumentOutOfRangeException("createdTime");
			if (fileInfo.LastAccessTime == 0)
				throw new ArgumentOutOfRangeException("lastAccessedTime");
			if (fileInfo.LastWriteTime == 0)
				throw new ArgumentOutOfRangeException("lastModifiedTime");

			IntPtr fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fileInfo));
			try
			{
				Marshal.StructureToPtr(fileInfo, fileInfoPtr, true);
				NativeMethods.IO_STATUS_BLOCK status;
				uint result = NativeMethods.NtSetInformationFile(handle,
					out status, fileInfoPtr, (uint)Marshal.SizeOf(fileInfo),
					NativeMethods.FILE_INFORMATION_CLASS.FileBasicInformation);

				if (result != 0)
					throw new IOException();
			}
			finally
			{
				Marshal.FreeHGlobal(fileInfoPtr);
			}
		}

		/// <summary>
		/// Gets the parent directory of the current <see cref="System.IO.FileSystemInfo"/>
		/// object.
		/// </summary>
		/// <param name="info">The <see cref="System.IO.FileSystemInfo"/> object
		/// to query its parent.</param>
		/// <returns>The parent directory of the current
		/// <see cref="System.IO.FileSystemInfo"/> object, or null if info is
		/// already the root</returns>
		public static DirectoryInfo GetParent(this FileSystemInfo info)
		{
			FileInfo file = info as FileInfo;
			DirectoryInfo directory = info as DirectoryInfo;

			if (file != null)
				return file.Directory;
			else if (directory != null)
				return directory.Parent;
			else
				throw new ArgumentException("Unknown FileSystemInfo type.");
		}

		/// <summary>
		/// Moves the provided <see cref="System.IO.FileSystemInfo"/> object
		/// to the provided path.
		/// </summary>
		/// <param name="info">The <see cref="System.IO.FileSystemInfo"/> object
		/// to move.</param>
		/// <param name="path">The path to move the object to.</param>
		public static void MoveTo(this FileSystemInfo info, string path)
		{
			FileInfo file = info as FileInfo;
			DirectoryInfo directory = info as DirectoryInfo;

			if (file != null)
				file.MoveTo(path);
			else if (directory != null)
				directory.MoveTo(path);
			else
				throw new ArgumentException("Unknown FileSystemInfo type.");
		}

		/// <summary>
		/// Compacts the file path, fitting in the given width.
		/// </summary>
		/// <param name="info">The <see cref="System.IO.FileSystemObject"/> that should
		/// get a compact path.</param>
		/// <param name="newWidth">The target width of the text.</param>
		/// <param name="drawFont">The font used for drawing the text.</param>
		/// <returns>The compacted file path.</returns>
		public static string GetCompactPath(this FileSystemInfo info, int newWidth, Font drawFont)
		{
			return PathUtil.GetCompactPath(info.FullName, newWidth, drawFont);
		}
        /// <summary>
        /// Checks whether the path given is encrypted.
        /// </summary>
        /// <param name="info">The <see cref="System.IO.FileInfo"/> object</param>
        /// <returns>True if the file is encrypted.</returns>
        public static bool IsEncrypted(this FileSystemInfo info)
        {
            Boolean encryptionStatus = false;
            FileInfo fi = new FileInfo(info.FullName);
            if (fi.Attributes.HasFlag(FileAttributes.Encrypted))
            { encryptionStatus = true; }
            return encryptionStatus;
        }
		/// <summary>
		/// Checks whether the path given is compressed.
		/// </summary>
		/// <param name="info">The <see cref="System.IO.FileInfo"/> object</param>
		/// <returns>True if the file or folder is compressed.</returns>
		public static bool IsCompressed(this FileSystemInfo info)
		{
			ushort compressionStatus = 0;
			uint bytesReturned = 0;

			using (SafeFileHandle handle = NativeMethods.CreateFile(info.FullName,
				NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
				0, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
				NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
			{
				if (NativeMethods.DeviceIoControl(handle, NativeMethods.FSCTL_GET_COMPRESSION,
					IntPtr.Zero, 0, out compressionStatus, sizeof(ushort), out bytesReturned,
					IntPtr.Zero))
				{
					return compressionStatus != NativeMethods.COMPRESSION_FORMAT_NONE;
				}
			}

			return false;
		}

		/// <summary>
		/// Compresses the given file.
		/// </summary>
		/// <param name="info">The File to compress.</param>
		/// <returns>The success ofthe compression</returns>
		public static bool Compress(this FileSystemInfo info)
		{
			return SetCompression(info.FullName, true);
		}

		/// <summary>
		/// Uncompresses the given file.
		/// </summary>
		/// <param name="info">The File to uncompress.</param>
		/// <returns>The success of the uncompression</returns>
		public static bool Uncompress(this FileSystemInfo info)
		{
			return SetCompression(info.FullName, false);
		}

		/// <summary>
		/// Sets whether the file system object pointed to by path is compressed.
		/// </summary>
		/// <param name="path">The path to the file or folder.</param>
		/// <returns>True if the file or folder has its compression value set.</returns>
		private static bool SetCompression(string path, bool compressed)
		{
			ushort compressionStatus = compressed ?
				NativeMethods.COMPRESSION_FORMAT_DEFAULT :
				NativeMethods.COMPRESSION_FORMAT_NONE;
			uint bytesReturned = 0;

			using (SafeFileHandle handle = NativeMethods.CreateFile(path,
				NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
				0, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
				NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
			{
				return NativeMethods.DeviceIoControl(handle, NativeMethods.FSCTL_SET_COMPRESSION,
					ref compressionStatus, sizeof(ushort), IntPtr.Zero, 0, out bytesReturned,
					IntPtr.Zero);
			}
		}

		/// <summary>
		/// Uses SHGetFileInfo to retrieve the description for the given file,
		/// folder or drive.
		/// </summary>
		/// <param name="info">The file system object to query the description of.</param>
		/// <returns>A string containing the description</returns>
		public static string GetDescription(this FileSystemInfo info)
		{
			NativeMethods.SHFILEINFO shfi = new NativeMethods.SHFILEINFO();
			NativeMethods.SHGetFileInfo(info.FullName, 0, ref shfi, Marshal.SizeOf(shfi),
				NativeMethods.SHGetFileInfoFlags.SHGFI_DISPLAYNAME);
			return shfi.szDisplayName;
		}

		/// <summary>
		/// Uses SHGetFileInfo to retrieve the icon for the given file, folder or
		/// drive.
		/// </summary>
		/// <param name="info">The file system object to query the description of.</param>
		/// <returns>An Icon object containing the bitmap</returns>
		public static Icon GetIcon(this FileSystemInfo info)
		{
			NativeMethods.SHFILEINFO shfi = new NativeMethods.SHFILEINFO();
			NativeMethods.SHGetFileInfo(info.FullName, 0, ref shfi, Marshal.SizeOf(shfi),
				NativeMethods.SHGetFileInfoFlags.SHGFI_SMALLICON |
				NativeMethods.SHGetFileInfoFlags.SHGFI_ICON);

			if (shfi.hIcon != IntPtr.Zero)
				return Icon.FromHandle(shfi.hIcon);
			else
				throw new IOException(string.Format(CultureInfo.CurrentCulture,
					"Could not load file icon from {0}", info.FullName),
					Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error()));
		}

		/// <summary>
		/// Determines if a given file is protected by SFC.
		/// </summary>
		/// <param name="info">The file system object to check.</param>
		/// <returns>True if the file is protected.</returns>
		public static bool IsProtectedSystemFile(this FileSystemInfo info)
		{
			return NativeMethods.SfcIsFileProtected(IntPtr.Zero, info.FullName);
		}

		/// <summary>
		/// Copies an existing file to a new file, allowing the monitoring of the progress
		/// of the copy operation.
		/// </summary>
		/// <param name="info">The <see cref="System.IO.FileSystemInfo"/> object
		/// to copy.</param>
		/// <param name="destFileName">The name of the new file to copy to.</param>
		/// <param name="progress">The progress callback function to execute</param>
		/// <returns>A new file, or an overwrite of an existing file if the file exists.</returns>
		public static FileInfo CopyTo(this FileInfo info, string destFileName,
			CopyProgressFunction progress)
		{
			bool cancel = false;
			NativeMethods.CopyProgressFunction callback = delegate(
					long TotalFileSize, long TotalBytesTransferred, long StreamSize,
					long StreamBytesTransferred, uint dwStreamNumber,
					NativeMethods.CopyProgressFunctionCallbackReasons dwCallbackReason,
					IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData)
			{
				return progress(TotalFileSize, TotalBytesTransferred);
			};

			if (!NativeMethods.CopyFileEx(info.FullName, destFileName, callback, IntPtr.Zero,
				ref cancel, 0))
			{
				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}

			return new FileInfo(destFileName);
		}

		/// <summary>
		/// An application-defined callback function used with the <see cref="CopyTo" />
		/// function. It is called when a portion of a copy or move operation is
		/// completed.
		/// </summary>
		/// <param name="TotalFileSize">The total size of the file, in bytes.</param>
		/// <param name="TotalBytesTransferred">The total number of bytes
		/// transferred from the source file to the destination file since the
		/// copy operation began.</param>
		/// <returns>The <see cref="CopyProgressFunction"/> function should return
		/// one of the <see cref="CopyProgressFunctionResult"/> values.</returns>
		public delegate CopyProgressFunctionResult CopyProgressFunction(
			long TotalFileSize, long TotalBytesTransferred);

		/// <summary>
		/// Result codes which can be returned from the
		/// <see cref="CopyProgressFunction"/> callbacks.
		/// </summary>
		public enum CopyProgressFunctionResult
		{
			/// <summary>
			/// Cancel the copy operation and delete the destination file.
			/// </summary>
			Cancel = 1,

			/// <summary>
			/// Continue the copy operation.
			/// </summary>
			Continue = 0,

			/// <summary>
			/// Continue the copy operation, but stop invoking
			/// <see cref="CopyProgressRoutine"/> to report progress.
			/// </summary>
			Quiet = 3,

			/// <summary>
			/// Stop the copy operation. It can be restarted at a later time.
			/// </summary>
			Stop = 2
		}

		/// <summary>
		/// Gets the list of ADSes of the given file. 
		/// </summary>
		/// <param name="info">The FileInfo object with the file path etc.</param>
		/// <returns>A list containing the names of the ADSes of each file. The
		/// list will be empty if no ADSes exist.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static IList<StreamInfo> GetADSes(this FileInfo info)
		{
			List<StreamInfo> result = new List<StreamInfo>();
			using (SafeFileHandle streamHandle = NativeMethods.CreateFile(info.FullName,
				NativeMethods.GENERIC_READ, (uint)FileShare.ReadWrite, IntPtr.Zero,
				(uint)FileMode.Open, (uint)FileOptions.None, IntPtr.Zero))
			{
				if (streamHandle.IsInvalid)
					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

				//Allocate the structures
				NativeMethods.FILE_STREAM_INFORMATION[] streams = GetADSes(streamHandle);

				foreach (NativeMethods.FILE_STREAM_INFORMATION streamInfo in streams)
				{
					//Get the name of the stream. The raw value is :NAME:$DATA
					string streamName = streamInfo.StreamName.Substring(1,
						streamInfo.StreamName.LastIndexOf(':') - 1);

					if (streamName.Length != 0)
						result.Add(new StreamInfo(info.FullName, streamName));
				}
			}

			return result.AsReadOnly();
		}

		private static NativeMethods.FILE_STREAM_INFORMATION[] GetADSes(SafeFileHandle FileHandle)
		{
			NativeMethods.IO_STATUS_BLOCK status;
			IntPtr fileInfoPtr = IntPtr.Zero;

			try
			{
				NativeMethods.FILE_STREAM_INFORMATION streamInfo =
					new NativeMethods.FILE_STREAM_INFORMATION();
				int fileInfoPtrLength = (Marshal.SizeOf(streamInfo) + 32768) / 2;
				uint ntStatus = 0;

				do
				{
					fileInfoPtrLength *= 2;
					if (fileInfoPtr != IntPtr.Zero)
						Marshal.FreeHGlobal(fileInfoPtr);
					fileInfoPtr = Marshal.AllocHGlobal(fileInfoPtrLength);

					ntStatus = NativeMethods.NtQueryInformationFile(FileHandle, out status,
						fileInfoPtr, (uint)fileInfoPtrLength,
						NativeMethods.FILE_INFORMATION_CLASS.FileStreamInformation);
				}
				while (ntStatus != 0 /*STATUS_SUCCESS*/ && ntStatus == 0x80000005 /*STATUS_BUFFER_OVERFLOW*/);

				//Marshal the structure manually (argh!)
				List<NativeMethods.FILE_STREAM_INFORMATION> result =
					new List<NativeMethods.FILE_STREAM_INFORMATION>();
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
	}
}
