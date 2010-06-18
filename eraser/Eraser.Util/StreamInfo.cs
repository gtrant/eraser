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

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Eraser.Util
{
	/// <summary>
	/// Provides methods for the deletion, and opening of file alternate data streams,
	/// and aids in the creation of <see cref="System.IO.FileStream"/> objects.
	/// </summary>
	public class StreamInfo : FileSystemInfo
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filename">The fully qualified name of the new file, or
		/// the relative file name.</param>
		public StreamInfo(string filename)
			: this(filename, null)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filename">The path to the file.</param>
		/// <param name="streamName">The name of the alternate data stream, or null
		/// to refer to the unnamed stream.</param>
		public StreamInfo(string filename, string streamName)
		{
			OriginalPath = filename;
			FullPath = Path.GetFullPath(filename);
			FileName = FullPath;
			StreamName = streamName;

			if (!string.IsNullOrEmpty(streamName))
			{
				OriginalPath += ":" + streamName;
				FullPath += ":" + streamName;
			}

			Refresh();
		}

		/// <summary>
		/// The full name of the stream, including the stream name if provided.
		/// </summary>
		public override string FullName
		{
			get
			{
				return FullPath;
			}
		}

		/// <summary>
		/// Gets a value indicating whether a file exists.
		/// </summary>
		public override bool Exists
		{
			get
			{
				bool result = System.IO.File.Exists(FullName);
				return result &&
					(string.IsNullOrEmpty(StreamName) || true/*TODO: verify the ADS exists*/);
			}
		}

		/// <summary>
		/// Gets a string representing the directory's full path.
		/// </summary>
		public String DirectoryName
		{
			get
			{
				return Path.GetDirectoryName(FullPath);
			}
		}

		/// <summary>
		/// Gets an instance of the parent directory.
		/// </summary>
		public DirectoryInfo Directory
		{
			get
			{
				return new DirectoryInfo(DirectoryName);
			}
		} 

		/// <summary>
		/// Gets the file which contains this stream.
		/// </summary>
		public FileInfo File
		{
			get
			{
				return new FileInfo(FileName);
			}
		}

		/// <summary>
		/// The full path to the file we are encapsulating.
		/// </summary>
		public string FileName
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name of the stream.
		/// </summary>
		public override string Name
		{
			get { return StreamName; }
		}

		/// <summary>
		/// Gets or sets a value that determines if the current file is read only.
		/// </summary>
		public bool IsReadOnly
		{
			get { return (Attributes & FileAttributes.ReadOnly) != 0; }
			set
			{
				Attributes = value ?
					(Attributes | FileAttributes.ReadOnly) :
					(Attributes & ~FileAttributes.ReadOnly);
			}
		}

		/// <summary>
		/// Gets the size, in bytes, of the current stream.
		/// </summary>
		public long Length
		{
			get
			{
				long fileSize;
				using (SafeFileHandle handle = OpenHandle(
					FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.None))
				{
					if (NativeMethods.GetFileSizeEx(handle, out fileSize))
						return fileSize;
				}

				return 0;
			}
		}

		/// <summary>
		/// Creates the file if it already does not exist, then creates the alternate
		/// data stream.
		/// </summary>
		public FileStream Create()
		{
			return Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None, FileOptions.None);
		}

		/// <summary>
		/// Permanently deletes the stream. If this refers to the unnamed stream, all
		/// alternate data streams are also deleted.
		/// </summary>
		public override void Delete()
		{
			if (!NativeMethods.DeleteFile(FullName))
			{
				int errorCode = Marshal.GetLastWin32Error();
				switch (errorCode)
				{
					case Win32ErrorCode.PathNotFound:
						break;
					default:
						throw Win32ErrorCode.GetExceptionForWin32Error(errorCode);
				}
			}
		}

		/// <summary>
		/// Opens a file in the specified mode.
		/// </summary>
		/// <param name="mode">A System.IO.FileMode constant specifying the mode
		/// (for example, Open or Append) in which to open the file.</param>
		/// <returns>A file opened in the specified mode, with read/write access,
		/// unshared, and no special file options.</returns>
		public FileStream Open(FileMode mode)
		{
			return Open(mode, FileAccess.ReadWrite, FileShare.None, FileOptions.None);
		}

		/// <summary>
		/// Opens a file in the specified mode with read, write, or read/write access.
		/// </summary>
		/// <param name="mode">A System.IO.FileMode constant specifying the mode
		/// (for example, Open or Append) in which to open the file.</param>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <returns>A System.IO.FileStream object opened in the specified mode
		/// and access, unshared, and no special file options.</returns>
		public FileStream Open(FileMode mode, FileAccess access)
		{
			return Open(mode, access, FileShare.None, FileOptions.None);
		}

		/// <summary>
		/// Opens a file in the specified mode with read, write, or read/write access
		/// and the specified sharing option.
		/// </summary>
		/// <param name="mode">A System.IO.FileMode constant specifying the mode
		/// (for example, Open or Append) in which to open the file.</param>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <param name="share">A System.IO.FileShare constant specifying the type
		/// of access other FileStream objects have to this file.</param>
		/// <returns>A System.IO.FileStream object opened with the specified mode,
		/// access, sharing options, and no special file options.</returns>
		public FileStream Open(FileMode mode, FileAccess access, FileShare share)
		{
			return Open(mode, access, share, FileOptions.None);
		}

		/// <summary>
		/// Opens a file in the specified mode with read, write, or read/write access,
		/// the specified sharing option, and other advanced options.
		/// </summary>
		/// <param name="mode">A System.IO.FileMode constant specifying the mode
		/// (for example, Open or Append) in which to open the file.</param>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <param name="share">A System.IO.FileShare constant specifying the type
		/// of access other FileStream objects have to this file.</param>
		/// <param name="options">The System.IO.FileOptions constant specifying
		/// the advanced file options to use when opening the file.</param>
		/// <returns>A System.IO.FileStream object opened with the specified mode,
		/// access, sharing options, and special file options.</returns>
		public FileStream Open(FileMode mode, FileAccess access, FileShare share,
			FileOptions options)
		{
			SafeFileHandle handle = OpenHandle(mode, access, share, options);

			//Check that the handle is valid
			if (handle.IsInvalid)
				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

			//Return the FileStream
			return new FileStream(handle, access);
		}

		/// <summary>
		/// Creates a read-only System.IO.FileStream.
		/// </summary>
		/// <returns>A new read-only System.IO.FileStream object.</returns>
		public FileStream OpenRead()
		{
			return Open(FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None);
		}

		/// <summary>
		/// Creates a write-only System.IO.FileStream.
		/// </summary>
		/// <returns>A new write-only unshared System.IO.FileStream object.</returns>
		public FileStream OpenWrite()
		{
			return Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, FileOptions.None);
		}

		private SafeFileHandle OpenHandle(FileMode mode, FileAccess access, FileShare share,
			FileOptions options)
		{
			//Access mode
			uint iAccess = 0;
			switch (access)
			{
				case FileAccess.Read:
					iAccess = NativeMethods.GENERIC_READ;
					break;
				case FileAccess.ReadWrite:
					iAccess = NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE;
					break;
				case FileAccess.Write:
					iAccess = NativeMethods.GENERIC_WRITE;
					break;
			}

			return OpenHandle(mode, iAccess, share, options);
		}

		private SafeFileHandle OpenHandle(FileMode mode, uint access, FileShare share,
			FileOptions options)
		{
			//Sharing mode
			if ((share & FileShare.Inheritable) != 0)
				throw new NotSupportedException("Inheritable handles are not supported.");

			//Advanced options
			if ((options & FileOptions.Asynchronous) != 0)
				throw new NotSupportedException("Asynchronous handles are not implemented.");

			//Create the handle
			SafeFileHandle result = NativeMethods.CreateFile(FullName, access,
				(uint)share, IntPtr.Zero, (uint)mode, (uint)options, IntPtr.Zero);
			if (result.IsInvalid)
			{
				int errorCode = Marshal.GetLastWin32Error();
				result.Close();
				throw Win32ErrorCode.GetExceptionForWin32Error(errorCode);
			}

			return result;
		}

		public void SetTimes(DateTime updateTime, DateTime createdTime, DateTime lastModifiedTime,
			DateTime lastAccessedTime)
		{
			using (SafeFileHandle streamHandle = OpenHandle(FileMode.Open,
					NativeMethods.FILE_WRITE_ATTRIBUTES, FileShare.ReadWrite,
					FileOptions.None))
			{
				ExtensionMethods.IO.SetTimes(streamHandle, updateTime, createdTime,
					lastModifiedTime, lastAccessedTime);
			}
		}

		/// <summary>
		/// Returns the path as a string.
		/// </summary>
		/// <returns>A string containing the path given to the constructor.</returns>
		public override string ToString()
		{
			return OriginalPath;
		}

		/// <summary>
		/// The name of the stream we are encapsulating.
		/// </summary>
		private string StreamName;
	}
}