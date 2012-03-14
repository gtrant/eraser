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

using System.IO;

using Eraser.Util;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// Provides functions to handle erasures specfic to file systems.
	/// </summary>
	public interface IFileSystem : IRegisterable
	{
		/// <summary>
		/// The name of the current filesystem.
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// Resets the created, modified, accessed and last update times for the given
		/// <paramref name="info"/>.
		/// </summary>
		/// <param name="info">The file to reset times.</param>
		void ResetFileTimes(FileSystemInfo info);

		/// <summary>
		/// Securely deletes the file reference from the directory structures
		/// as well as resetting the Date Created, Date Accessed and Date Modified
		/// records.
		/// </summary>
		/// <param name="info">The file to delete.</param>
		void DeleteFile(FileInfo info);

		/// <summary>
		/// Securely deletes the folder reference from the directory structures
		/// as well as all subfolders and files, resetting the Date Created, Date
		/// Accessed and Date Modified records.
		/// </summary>
		/// <param name="info">The folder to delete</param>
		/// <param name="recursive">True if the folder and all its subfolders and
		/// files to be securely deleted.</param>
		void DeleteFolder(DirectoryInfo info, bool recursive);

		/// <summary>
		/// Erases all file cluster tips in the given volume.
		/// </summary>
		/// <param name="info">The volume to search for file cluster tips and erase them.</param>
		/// <param name="method">The erasure method being employed.</param>
		/// <param name="log">The log manager instance that tracks log messages.</param>
		/// <param name="searchCallback">The callback function for search progress.</param>
		/// <param name="eraseCallback">The callback function for erasure progress.</param>
		void EraseClusterTips(VolumeInfo info, IErasureMethod method,
			ClusterTipsSearchProgress searchCallback, ClusterTipsEraseProgress eraseCallback);

		/// <summary>
		/// Erases old file system table-resident files. This creates small one-byte
		/// files until disk is full. This will erase unused space which was used for
		/// files resident in the file system table.
		/// </summary>
		/// <param name="volume">The directory information structure containing
		/// the path to store the temporary one-byte files. The file system table
		/// of that drive will be erased.</param>
		/// <param name="tempDirectory">The directory structure containing the path
		/// to store temporary files used for resident file cleaning.</param>
		/// <param name="method">The method used to erase the files.</param>
		void EraseOldFileSystemResidentFiles(VolumeInfo volume, DirectoryInfo tempDirectory,
			IErasureMethod method, FileSystemEntriesEraseProgress callback);

		/// <summary>
		/// Erases the unused space in the main filesystem structures by creating,
		/// files until the table grows.
		/// 
		/// This will overwrite unused portions of the table which were previously
		/// used to store file entries.
		/// </summary>
		/// <param name="info">The directory information structure containing
		/// the path to store the temporary files.</param>
		/// <param name="callback">The callback function to handle the progress
		/// of the file system entry erasure.</param>
		void EraseDirectoryStructures(VolumeInfo info, FileSystemEntriesEraseProgress callback);

		/// <summary>
		/// Erases the file system object from the drive.
		/// </summary>
		/// <param name="info"></param>
		void EraseFileSystemObject(StreamInfo info, IErasureMethod method,
			ErasureMethodProgressFunction callback);

		//TODO: This is supposed to be in VolumeInfo!
		/// <summary>
		/// Retrieves the size of the file on disk, calculated by the amount of
		/// clusters allocated by it.
		/// </summary>
		/// <param name="streamInfo">The Stream to get the area for.</param>
		/// <returns>The area of the file.</returns>
		long GetFileArea(StreamInfo filePath);
	}

	/// <summary>
	/// The function prototype for cluster tip search progress callbacks. This is
	/// called when the cluster tips are being searched.
	/// </summary>
	/// <param name="currentPath">The directory being searched</param>
	public delegate void ClusterTipsSearchProgress(string currentPath);

	/// <summary>
	/// The function prototype for cluster tip erasure callbacks. This is called when
	/// the cluster tips are being erased.
	/// </summary>
	/// <param name="currentFile">The current file index being erased.</param>
	/// <param name="totalFiles">The total number of files to be erased.</param>
	/// <param name="currentFilePath">The path to the current file being erased.</param>
	public delegate void ClusterTipsEraseProgress(int currentFile, int totalFiles,
		string currentFilePath);

	/// <summary>
	/// The prototype of callbacks handling the file system table erase progress.
	/// </summary>
	/// <param name="currentFile">The current file being erased.</param>
	/// <param name="totalFiles">The estimated number of files that must be
	/// erased.</param>
	public delegate void FileSystemEntriesEraseProgress(int currentFile, int totalFiles);
}
