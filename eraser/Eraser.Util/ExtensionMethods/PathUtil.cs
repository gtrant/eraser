/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Eraser.Util.ExtensionMethods
{
	/// <summary>
	/// Additional Path utility methods.
	/// </summary>
	public class PathUtil
	{
		/// <summary>
		/// Makes the first path relative to the second.
		/// </summary>
		/// <remarks>Modified from:
		/// http://mrpmorris.blogspot.com/2007/05/convert-absolute-path-to-relative-path.html</remarks>
		/// <param name="absolutePath">The path to use as the root of the relative path.</param>
		/// <param name="relativeTo">The path to make relative.</param>
		/// <returns>The relative path to the provided path.</returns>
		public static string MakeRelativeTo(FileSystemInfo absolutePath, string relativeTo)
		{
			string[] absoluteDirectories = absolutePath.FullName.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			string[] relativeDirectories = relativeTo.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

			//Get the shortest of the two paths
			int length = absoluteDirectories.Length < relativeDirectories.Length ?
				absoluteDirectories.Length : relativeDirectories.Length;

			//Use to determine where in the loop we exited
			int lastCommonRoot = -1;
			int index;

			//Find common root
			for (index = 0; index < length; index++)
				if (absoluteDirectories[index] == relativeDirectories[index])
					lastCommonRoot = index;
				else
					break;

			//If we didn't find a common prefix then throw
			if (lastCommonRoot == -1)
				throw new ArgumentException("Paths do not have a common base");

			//Build up the relative path
			StringBuilder relativePath = new StringBuilder();

			//Add on the ..
			for (index = lastCommonRoot + 1; index < absoluteDirectories.Length; index++)
				if (absoluteDirectories[index].Length > 0)
					relativePath.Append("..\\");

			//Add on the folders
			for (index = lastCommonRoot + 1; index < relativeDirectories.Length - 1; index++)
				relativePath.Append(relativeDirectories[index] + "\\");
			if (lastCommonRoot < relativeDirectories.Length - 1)
				relativePath.Append(relativeDirectories[relativeDirectories.Length - 1]);

			return relativePath.ToString();
		}

		/// <summary>
		/// Verifies if the path given is rooted at the given absolute path.
		/// </summary>
		/// <param name="absolutePath">The root path.</param>
		/// <param name="path">The path to verify.</param>
		/// <returns>True if the path provided is a subfolder/sub-file of the provided root path.</returns>
		public static bool IsRootedAt(FileSystemInfo absolutePath, string path)
		{
			//Convert the path in question to an absolute path
			if (!System.IO.Path.IsPathRooted(path))
				path = System.IO.Path.GetFullPath(path);

			//Split the directory path to its component folders
			string[] absoluteDirectories = absolutePath.FullName.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			string[] relativeDirectories = path.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

			//Compare element by element; if the absolute path compares till the end, the
			//provided path is a subdirectory
			for (int i = 0; i < absoluteDirectories.Length; ++i)
				if (absoluteDirectories[i] != relativeDirectories[i])
					return false;

			return true;
		}

		/// <summary>
		/// Compacts the file path, fitting in the given width.
		/// </summary>
		/// <param name="longPath">The path to compact.</param>
		/// <param name="newWidth">The target width of the text.</param>
		/// <param name="drawFont">The font used for drawing the text.</param>
		/// <returns>The compacted file path.</returns>
		public static string GetCompactPath(string longPath, int newWidth, Font drawFont)
		{
			using (Control ctrl = new Control())
			using (Graphics g = ctrl.CreateGraphics())
			{
				//First check if the source string is too long.
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
		/// Compacts the file path, fitting in the given width.
		/// </summary>
		/// <param name="longPath">The path to compact.</param>
		/// <param name="newWidth">The target width of the text.</param>
		/// <param name="control">The control on which this text is drawn. This is used
		/// for font information.</param>
		/// <returns>The compacted file path.</returns>
		public static string GetCompactPath(string longPath, int newWidth, Control control)
		{
			return GetCompactPath(longPath, newWidth, control.Font);
		}

		/// <summary>
		/// Resolves the reparse point pointed to by the path.
		/// </summary>
		/// <param name="path">The path to the reparse point.</param>
		/// <returns>The NT Namespace Name of the reparse point.</returns>
		public static string ResolveReparsePoint(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException(path);

			//If the path is a directory, remove the trailing \
			if (Directory.Exists(path) && path[path.Length - 1] == '\\')
				path = path.Remove(path.Length - 1);

			using (SafeFileHandle handle = NativeMethods.CreateFile(path,
				NativeMethods.GENERIC_READ,
				NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
				IntPtr.Zero, NativeMethods.OPEN_EXISTING,
				NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT |
				NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
			{
				if (handle.IsInvalid)
					throw new System.IO.IOException(string.Format("Cannot open handle to {0}", path));

				int bufferSize = Marshal.SizeOf(typeof(NativeMethods.REPARSE_DATA_BUFFER)) + 260 * sizeof(char);
				IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

				//Get all the information about the reparse point.
				try
				{
					uint returnedBytes = 0;
					while (!NativeMethods.DeviceIoControl(handle, NativeMethods.FSCTL_GET_REPARSE_POINT,
						IntPtr.Zero, 0, buffer, (uint)bufferSize, out returnedBytes, IntPtr.Zero))
					{
						if (Marshal.GetLastWin32Error() == 122) //ERROR_INSUFFICIENT_BUFFER
						{
							bufferSize *= 2;
							Marshal.FreeHGlobal(buffer);
							buffer = Marshal.AllocHGlobal(bufferSize);
						}
						else
						{
							throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
						}
					}

					string result = ResolveReparsePoint(buffer, path);

					//Is it a directory? If it is, we need to add a trailing \
					if (Directory.Exists(path))
						result += '\\';
					return result;
				}
				finally
				{
					Marshal.FreeHGlobal(buffer);
				}
			}
		}

		private static string ResolveReparsePoint(IntPtr ptr, string path)
		{
			NativeMethods.REPARSE_DATA_BUFFER buffer = (NativeMethods.REPARSE_DATA_BUFFER)
				Marshal.PtrToStructure(ptr, typeof(NativeMethods.REPARSE_DATA_BUFFER));

			//Check that this Reparse Point has a Microsoft Tag
			if (((uint)buffer.ReparseTag & (1 << 31)) == 0)
			{
				//We can only handle Microsoft's reparse tags.
				throw new ArgumentException("Unknown Reparse point type.");
			}

			//Then handle the tags
			switch (buffer.ReparseTag)
			{
				case NativeMethods.REPARSE_DATA_TAG.IO_REPARSE_TAG_MOUNT_POINT:
					return ResolveReparsePointJunction((IntPtr)(ptr.ToInt64() + Marshal.SizeOf(buffer)));

				case NativeMethods.REPARSE_DATA_TAG.IO_REPARSE_TAG_SYMLINK:
					return ResolveReparsePointSymlink((IntPtr)(ptr.ToInt64() + Marshal.SizeOf(buffer)),
						path);

				default:
					throw new ArgumentException("Unsupported Reparse point type.");
			}
		}

		private static string ResolveReparsePointJunction(IntPtr ptr)
		{
			NativeMethods.REPARSE_DATA_BUFFER.MountPointReparseBuffer buffer =
				(NativeMethods.REPARSE_DATA_BUFFER.MountPointReparseBuffer)
				Marshal.PtrToStructure(ptr,
					typeof(NativeMethods.REPARSE_DATA_BUFFER.MountPointReparseBuffer));

			//Get the substitute and print names from the buffer.
			string substituteName;
			string printName;
			unsafe
			{
				char* path = (char*)(((byte*)ptr.ToInt64()) + Marshal.SizeOf(buffer));
				printName = new string(path + (buffer.PrintNameOffset / sizeof(char)), 0,
					buffer.PrintNameLength / sizeof(char));
				substituteName = new string(path + (buffer.SubstituteNameOffset / sizeof(char)), 0,
					buffer.SubstituteNameLength / sizeof(char));
			}

			return substituteName;
		}

		private static string ResolveReparsePointSymlink(IntPtr ptr, string path)
		{
			NativeMethods.REPARSE_DATA_BUFFER.SymbolicLinkReparseBuffer buffer =
				(NativeMethods.REPARSE_DATA_BUFFER.SymbolicLinkReparseBuffer)
				Marshal.PtrToStructure(ptr,
					typeof(NativeMethods.REPARSE_DATA_BUFFER.SymbolicLinkReparseBuffer));

			//Get the substitute and print names from the buffer.
			string substituteName;
			string printName;
			unsafe
			{
				char* pathBuffer = (char*)(((byte*)ptr.ToInt64()) + Marshal.SizeOf(buffer));
				printName = new string(pathBuffer + (buffer.PrintNameOffset / sizeof(char)), 0,
					buffer.PrintNameLength / sizeof(char));
				substituteName = new string(pathBuffer + (buffer.SubstituteNameOffset / sizeof(char)), 0,
					buffer.SubstituteNameLength / sizeof(char));
			}

			if ((buffer.Flags & NativeMethods.REPARSE_DATA_BUFFER.SymbolicLinkFlags.SYMLINK_FLAG_RELATIVE) != 0)
			{
				return Path.Combine(Path.GetDirectoryName(path), substituteName);
			}

			return substituteName;
		}
	}
}
