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
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;

namespace Eraser.Util
{
	public static class Shell
	{
		/// <summary>
		/// Gets or sets whether low disk space notifications are enabled for the
		/// current user.
		/// </summary>
		public static bool LowDiskSpaceNotificationsEnabled
		{
			get
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
					"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer"))
				{
					if (key == null)
						return true;
					return !Convert.ToBoolean(key.GetValue("NoLowDiskSpaceChecks", false));
				}
			}
			set
			{
				RegistryKey key = null;
				try
				{
					key = Registry.CurrentUser.OpenSubKey(
						"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", true);
					if (key == null)
						key = Registry.CurrentUser.CreateSubKey(
							"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer");
					key.SetValue("NoLowDiskSpaceChecks", !value);
				}
				finally
				{
					if (key != null)
						key.Close();
				}
			}
		}

		/// <summary>
		/// Parses the provided command line into its constituent arguments.
		/// </summary>
		/// <param name="commandLine">The command line to parse.</param>
		/// <returns>The arguments specified in the command line</returns>
		public static string[] ParseCommandLine(string commandLine)
		{
			int argc = 0;
			IntPtr argv = NativeMethods.CommandLineToArgvW(commandLine, out argc);
			string[] result = new string[argc];

			//Get the pointers to the arguments, then read the string.
			for (int i = 0; i < argc; ++i)
				result[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size));

			//Free the memory
			NativeMethods.LocalFree(argv);

			return result;
		}

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
				Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string[] relativeDirectories = relativeTo.Split(
				Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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
			if (!Path.IsPathRooted(path))
				path = Path.GetFullPath(path);

			//Split the directory path to its component folders
			string[] absoluteDirectories = absolutePath.FullName.Split(
				Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string[] relativeDirectories = path.Split(
				Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			//Compare element by element; if the absolute path compares till the end, the
			//provided path is a subdirectory
			for (int i = 0; i < absoluteDirectories.Length; ++i)
				if (absoluteDirectories[i] != relativeDirectories[i])
					return false;

			return true;
		}

		/// <summary>
		/// A List of known folder IDs in the shell namespace.
		/// </summary>
		public static class KnownFolderIDs
		{
			/// <summary>
			/// The Known Folder ID of the Recycle Bin
			/// </summary>
			public static readonly Guid RecycleBin = 
				new Guid(0xB7534046, 0x3ECB, 0x4C18, 0xBE, 0x4E, 0x64, 0xCD, 0x4C, 0xB7, 0xD6, 0xAC);

			/// <summary>
			/// Gets the PIDL for the given Known folder ID.
			/// </summary>
			/// <param name="folderId">The known folder ID to query.</param>
			/// <returns>The PIDL for the given folder.</returns>
			public static ShellItemIDList GetShellItemIdList(Guid folderId)
			{
				Guid guid = folderId;
				IntPtr pidl = IntPtr.Zero;
				NativeMethods.SHGetKnownFolderIDList(ref guid, 0, IntPtr.Zero, out pidl);

				try
				{
					return new ShellItemIDList(pidl);
				}
				finally
				{
					NativeMethods.ILFree(pidl);
				}
			}
		}
	}

	/// <summary>
	/// Retrieves the path of a known folder as an ITEMIDLIST structure.
	/// </summary>
	public class ShellCIDA
	{
		/// <summary>
		/// Parses the given buffer for CIDA elements
		/// </summary>
		/// <param name="buffer"></param>
		public ShellCIDA(byte[] buffer)
		{
			int offset = 0;
			cidl = BitConverter.ToUInt32(buffer, offset);
			aoffset = new ShellItemIDList[cidl + 1];

			for (int i = 0; i < aoffset.Length; ++i)
			{
				int pidlOffset = BitConverter.ToInt32(buffer, offset += sizeof(int));

				//Read the size of the IDL
				aoffset[i] = new ShellItemIDList(buffer.Skip(pidlOffset).ToArray());
			}
		}

		/// <summary>
		/// The number of PIDLs that are being transferred, not including the parent folder.
		/// </summary>
		public uint cidl
		{
			get;
			private set;
		}

		/// <summary>
		/// The first element of aoffset contains the fully-qualified PIDL of a parent folder.
		/// If this PIDL is empty, the parent folder is the desktop. Each of the remaining
		/// elements of the array contains an offset to one of the PIDLs to be transferred.
		/// All of these PIDLs are relative to the PIDL of the parent folder.
		/// </summary>
		public ShellItemIDList[] aoffset
		{
			get;
			private set;
		}
	}

	/// <summary>
	/// Contains a list of item identifiers.
	/// </summary>
	public class ShellItemIDList
	{
		public ShellItemIDList(byte[] buffer)
		{
			mkid = new ShellItemID(buffer);
		}

		public ShellItemIDList(IntPtr buffer)
		{
			mkid = new ShellItemID(buffer);
		}

		public ShellItemID mkid
		{
			get;
			private set;
		}

		/// <summary>
		/// The physical path to the object referenced by this IDL.
		/// </summary>
		/// <remarks>If this IDL references a virtual object, this will return
		/// null.</remarks>
		public string Path
		{
			get
			{
				IntPtr mkid = this.mkid.ToSHITEMID();
				try
				{
					StringBuilder result = new StringBuilder(NativeMethods.MaxPath);
					if (NativeMethods.SHGetPathFromIDList(mkid, result))
						return result.ToString();
				}
				finally
				{
					Marshal.FreeHGlobal(mkid);
				}

				return null;
			}
		}

		/// <summary>
		/// The GUID of the virtual folder referenced by this IDL.
		/// </summary>
		/// <remarks>If this IDL references a physical object, this will return
		/// <see cref="Guid.Empty"/></remarks>
		public Guid Guid
		{
			get
			{
				Guid[] guids = new Guid[] {
					Shell.KnownFolderIDs.RecycleBin
				};

				foreach (Guid guid in guids)
				{
					if (Shell.KnownFolderIDs.GetShellItemIdList(guid) == this)
						return guid;
				}

				return Guid.Empty;
			}
		}

		public static bool operator==(ShellItemIDList lhs, ShellItemIDList rhs)
		{
			return lhs.mkid == rhs.mkid;
		}

		public static bool operator!=(ShellItemIDList lhs, ShellItemIDList rhs)
		{
			return lhs.mkid != rhs.mkid;
		}

		public override bool Equals(object obj)
		{
			if (obj is ShellItemIDList)
				return this == (ShellItemIDList)obj;
			return this.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	/// <summary>
	/// Defines an item identifier. (native type: SHITEMID)
	/// </summary>
	public class ShellItemID
	{
		public ShellItemID(byte[] buffer)
		{
			short cb = BitConverter.ToInt16(buffer, 0);
			abID = new byte[cb];
			if (cb > 0)
				Buffer.BlockCopy(buffer, sizeof(short), abID, 0, cb - sizeof(short));
		}

		public ShellItemID(IntPtr buffer)
		{
			short cb = Marshal.ReadInt16(buffer);
			abID = new byte[cb];
			if (cb > 0)
				Marshal.Copy(new IntPtr(buffer.ToInt64() + sizeof(short)), abID, 0, cb - sizeof(short));
		}

		byte[] abID;

		/// <summary>
		/// Converts this ShellItemID to the native SHITEMID.
		/// </summary>
		/// <returns>A Pointer to an unmanaged block of memory which should be
		/// freed by Marshal.FreeHGlobal upon completion.</returns>
		internal IntPtr ToSHITEMID()
		{
			//Allocate the buffer
			IntPtr result = Marshal.AllocHGlobal(abID.Length + (abID.Length == 0 ? 0 : sizeof(short)));

			//Write the size of the identifier
			Marshal.WriteInt16(result, (short)abID.Length);

			//Then copy the block of memory
			Marshal.Copy(abID, 0, new IntPtr(result.ToInt64() + 2), abID.Length);
			return result;
		}

		public static bool operator==(ShellItemID lhs, ShellItemID rhs)
		{
			return lhs.abID.SequenceEqual(rhs.abID);
		}

		public static bool operator!=(ShellItemID lhs, ShellItemID rhs)
		{
			return !lhs.abID.SequenceEqual(rhs.abID);
		}

		public override bool Equals(object obj)
		{
			if (obj is ShellItemID)
				return this == (ShellItemID)obj;
			return this.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
