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
	public static class IO
	{
		/// <summary>
		/// Gets the parent directory of the current <see cref="System.IO.FileSystemInfo"/>
		/// object.
		/// </summary>
		/// <param name="info">The <see cref="System.IO.FileSystemInfo"/>object
		/// to query its parent.</param>
		/// <returns>The parent directory of the current
		/// <see cref="System.IO.FileSystemInfo"/> object, or null if info is
		/// aleady the root</returns>
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
			using (Control ctrl = new Control())
			{
				//First check if the source string is too long.
				Graphics g = ctrl.CreateGraphics();
				string longPath = info.FullName;
				int width = g.MeasureString(longPath, drawFont).ToSize().Width;
				if (width <= newWidth)
					return longPath;

				//It is, shorten it.
				int aveCharWidth = width / longPath.Length;
				int charCount = newWidth / aveCharWidth;
				StringBuilder builder = new StringBuilder();
				builder.Append(longPath);
				builder.EnsureCapacity(charCount);

				while (g.MeasureString(builder.ToString(), drawFont).Width > newWidth)
				{
					if (!NativeMethods.PathCompactPathEx(builder, longPath,
						(uint)charCount--, 0))
					{
						return string.Empty;
					}
				}

				return builder.ToString();
			}
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
		/// <returns>The success ofthe uncompression</returns>
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
		/// <param name="info">The file systme object to check.</param>
		/// <returns>True if the file is protected.</returns>
		public static bool IsProtectedSystemFile(this FileSystemInfo info)
		{
			return NativeMethods.SfcIsFileProtected(IntPtr.Zero, info.FullName);
		}

		/// <summary>
		/// Gets the list of ADSes of the given file. 
		/// </summary>
		/// <param name="info">The FileInfo object with the file path etc.</param>
		/// <returns>A list containing the names of the ADSes of each file. The
		/// list will be empty if no ADSes exist.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static IList<string> GetADSes(this FileInfo info)
		{
			List<string> result = new List<string>();
			using (FileStream stream = new StreamInfo(info.FullName).Open(FileMode.Open,
				FileAccess.Read, FileShare.ReadWrite))
			using (SafeFileHandle streamHandle = stream.SafeFileHandle)
			{
				//Allocate the structures
				NativeMethods.FILE_STREAM_INFORMATION[] streams = GetADSes(streamHandle);

				foreach (NativeMethods.FILE_STREAM_INFORMATION streamInfo in streams)
				{
					//Get the name of the stream. The raw value is :NAME:$DATA
					string streamName = streamInfo.StreamName.Substring(1,
						streamInfo.StreamName.LastIndexOf(':') - 1);

					if (streamName.Length != 0)
						result.Add(streamName);
				}
			}

			return result.AsReadOnly();
		}

		private static NativeMethods.FILE_STREAM_INFORMATION[] GetADSes(SafeFileHandle FileHandle)
		{
			NativeMethods.IO_STATUS_BLOCK status = new NativeMethods.IO_STATUS_BLOCK();
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

					ntStatus = NativeMethods.NtQueryInformationFile(FileHandle, ref status,
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
