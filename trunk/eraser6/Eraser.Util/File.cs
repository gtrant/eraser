/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net> @10/7/2008
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
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

namespace Eraser.Util
{
	public static class File
	{
		/// <summary>
		/// Gets the list of ADSes of the given file. 
		/// </summary>
		/// <param name="info">The FileInfo object with the file path etc.</param>
		/// <returns>A list containing the names of the ADSes of each file. The
		/// list will be empty if no ADSes exist.</returns>
		public static ICollection<string> GetADSes(FileInfo info)
		{
			List<string> result = new List<string>();
			using (FileStream stream = new StreamInfo(info.FullName).Open(FileMode.Open,
				FileAccess.Read, FileShare.ReadWrite))
			using (SafeFileHandle streamHandle = stream.SafeFileHandle)
			{
				//Allocate the structures
				NTApi.NativeMethods.FILE_STREAM_INFORMATION[] streams =
					NTApi.NativeMethods.NtQueryInformationFile(streamHandle);

				foreach (NTApi.NativeMethods.FILE_STREAM_INFORMATION streamInfo in streams)
				{
					//Get the name of the stream. The raw value is :NAME:$DATA
					string streamName = streamInfo.StreamName.Substring(1,
						streamInfo.StreamName.LastIndexOf(':') - 1);
					
					if (streamName.Length != 0)
						result.Add(streamName);
				}
			}

			return result;
		}

		/// <summary>
		/// Uses SHGetFileInfo to retrieve the description for the given file,
		/// folder or drive.
		/// </summary>
		/// <param name="path">A string that contains the path and file name for
		/// the file in question. Both absolute and relative paths are valid.
		/// Directories and volumes must contain the trailing \</param>
		/// <returns>A string containing the description</returns>
		public static string GetFileDescription(string path)
		{
			ShellApi.NativeMethods.SHFILEINFO shfi = new ShellApi.NativeMethods.SHFILEINFO();
			ShellApi.NativeMethods.SHGetFileInfo(path, 0, ref shfi, Marshal.SizeOf(shfi),
				ShellApi.NativeMethods.SHGetFileInfoFlags.SHGFI_DISPLAYNAME);
			return shfi.szDisplayName;
		}

		/// <summary>
		/// Uses SHGetFileInfo to retrieve the icon for the given file, folder or
		/// drive.
		/// </summary>
		/// <param name="path">A string that contains the path and file name for
		/// the file in question. Both absolute and relative paths are valid.
		/// Directories and volumes must contain the trailing \</param>
		/// <returns>An Icon object containing the bitmap</returns>
		public static Icon GetFileIcon(string path)
		{
			ShellApi.NativeMethods.SHFILEINFO shfi = new ShellApi.NativeMethods.SHFILEINFO();
			ShellApi.NativeMethods.SHGetFileInfo(path, 0, ref shfi, Marshal.SizeOf(shfi),
				ShellApi.NativeMethods.SHGetFileInfoFlags.SHGFI_SMALLICON |
				ShellApi.NativeMethods.SHGetFileInfoFlags.SHGFI_ICON);

			if (shfi.hIcon != IntPtr.Zero)
				return Icon.FromHandle(shfi.hIcon);
			else
				throw new IOException(string.Format(CultureInfo.CurrentCulture,
					"Could not load file icon from {0}", path),
					Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
		}

		/// <summary>
		/// Compacts the file path, fitting in the given width.
		/// </summary>
		/// <param name="longPath">The long file path.</param>
		/// <param name="newWidth">The target width of the text.</param>
		/// <param name="drawFont">The font used for drawing the text.</param>
		/// <returns>The compacted file path.</returns>
		public static string GetCompactPath(string longPath, int newWidth, Font drawFont)
		{
			using (Control ctrl = new Control())
			{
				//First check if the source string is too long.
				Graphics g = ctrl.CreateGraphics();
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
					if (!ShellApi.NativeMethods.PathCompactPathEx(builder, longPath,
						(uint)charCount--, 0))
					{
						return string.Empty;
					}
				}

				return builder.ToString();
			}
		}

		/// <summary>
		/// Determines if a given file is protected by SFC.
		/// </summary>
		/// <param name="filePath">The path to check</param>
		/// <returns>True if the file is protected.</returns>
		public static bool IsProtectedSystemFile(string filePath)
		{
			if (filePath.Length > 255)
				return false;
			if (SfcIsFileProtected(IntPtr.Zero, filePath))
				return true;

			switch (Marshal.GetLastWin32Error())
			{
				case 0: //ERROR_SUCCESS
				case 2: //ERROR_FILE_NOT_FOUND
					return false;

				default:
					throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
		}

		/// <summary>
		/// Checks whether the path given is compressed.
		/// </summary>
		/// <param name="path">The path to the file or folder</param>
		/// <returns>True if the file or folder is compressed.</returns>
		public static bool IsCompressed(string path)
		{
			ushort compressionStatus = 0;
			uint bytesReturned = 0;

			using (FileStream strm = new FileStream(
				KernelApi.NativeMethods.CreateFile(path,
				KernelApi.NativeMethods.GENERIC_READ | KernelApi.NativeMethods.GENERIC_WRITE,
				0, IntPtr.Zero, KernelApi.NativeMethods.OPEN_EXISTING,
				KernelApi.NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero), FileAccess.Read))
			{
				if (KernelApi.NativeMethods.DeviceIoControl(strm.SafeFileHandle,
					KernelApi.NativeMethods.FSCTL_GET_COMPRESSION, IntPtr.Zero, 0,
					out compressionStatus, sizeof(ushort), out bytesReturned, IntPtr.Zero))
				{
					return compressionStatus != KernelApi.NativeMethods.COMPRESSION_FORMAT_NONE;
				}
			}

			return false;
		}

		/// <summary>
		/// Sets whether the file system object pointed to by path is compressed.
		/// </summary>
		/// <param name="path">The path to the file or folder.</param>
		/// <returns>True if the file or folder has its compression value set.</returns>
		public static bool SetCompression(string path, bool compressed)
		{
			ushort compressionStatus = compressed ?
				KernelApi.NativeMethods.COMPRESSION_FORMAT_DEFAULT :
				KernelApi.NativeMethods.COMPRESSION_FORMAT_NONE;
			uint bytesReturned = 0;

			using (FileStream strm = new FileStream(
				KernelApi.NativeMethods.CreateFile(path,
				KernelApi.NativeMethods.GENERIC_READ | KernelApi.NativeMethods.GENERIC_WRITE,
				0, IntPtr.Zero, KernelApi.NativeMethods.OPEN_EXISTING,
				KernelApi.NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero), FileAccess.ReadWrite))
			{
				return KernelApi.NativeMethods.DeviceIoControl(strm.SafeFileHandle,
					KernelApi.NativeMethods.FSCTL_SET_COMPRESSION, ref compressionStatus,
					sizeof(ushort), IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
			}
		}

		/// <summary>
		/// Determines whether the specified file is protected. Applications
		/// should avoid replacing protected system files.
		/// </summary>
		/// <param name="RpcHandle">This parameter must be NULL.</param>
		/// <param name="ProtFileName">The name of the file.</param>
		/// <returns>If the file is protected, the return value is true.
		/// 
		/// If the file is not protected, the return value is false and
		/// Marshal.GetLastWin32Error() returns ERROR_FILE_NOT_FOUND. If the
		/// function fails, Marshal.GetLastWin32Error() will return a different
		/// error code.</returns>
		[DllImport("Sfc.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SfcIsFileProtected(IntPtr RpcHandle,
			string ProtFileName);

		/// <summary>
		/// Gets the human-readable representation of a file size from the byte-wise
		/// length of a file. This returns a KB = 1024 bytes (Windows convention.)
		/// </summary>
		/// <param name="bytes">The file size to scale.</param>
		/// <returns>A string containing the file size and the associated unit.
		/// Files larger than 1MB will be accurate to 2 decimal places.</returns>
		public static string GetHumanReadableFilesize(long bytes)
		{
			//List of units, in ascending scale
			string[] units = new string[] {
				"bytes",
				"KB",
				"MB",
				"GB",
				"TB",
				"PB",
				"EB"
			};

			double dBytes = (double)bytes;
			for (int i = 0; i != units.Length; ++i)
			{
				if (dBytes < 1020.0)
					if (i <= 1)
						return string.Format(CultureInfo.CurrentCulture,
							"{0} {1}", (int)dBytes, units[i]);
					else
						return string.Format(CultureInfo.CurrentCulture,
							"{0:0.00} {1}", dBytes, units[i]);
				dBytes /= 1024.0;
			}

			return string.Format(CultureInfo.CurrentCulture, "{0, 2} {1}",
				dBytes, units[units.Length - 1]);
		}
	}
}
