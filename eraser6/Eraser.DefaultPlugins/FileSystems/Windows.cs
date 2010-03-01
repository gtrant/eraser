﻿/* 
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
using System.Threading;
using Eraser.Manager;
using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Base class for all Windows filesystems.
	/// </summary>
	public abstract class WindowsFileSystem : FileSystem
	{
		public override void DeleteFile(FileInfo info)
		{
			//If the user wants plausible deniability, find a random file from the
			//list of decoys and write it over.
			if (Manager.ManagerLibrary.Settings.PlausibleDeniability)
			{
				using (FileStream fileStream = info.OpenWrite())
					CopyPlausibleDeniabilityFile(fileStream);
			}

			DeleteFileSystemInfo(info);
		}

		public override void DeleteFolder(DirectoryInfo info, bool recursive)
		{
			if (!recursive && info.GetFileSystemInfos().Length != 0)
				throw new InvalidOperationException(S._("The folder {0} cannot be deleted as it is " +
					"not empty."));

			//TODO: check for reparse points
			foreach (DirectoryInfo dir in info.GetDirectories())
				DeleteFolder(dir);
			foreach (FileInfo file in info.GetFiles())
				DeleteFile(file);

			DeleteFileSystemInfo(info);
		}

		/// <summary>
		/// Deletes a directory or file from disk. This assumes that directories already
		/// have been deleted.
		/// </summary>
		/// <param name="info">The file or directory to delete.</param>
		private void DeleteFileSystemInfo(FileSystemInfo info)
		{
			//If the file/directory doesn't exist, pass.
			if (!info.Exists)
				return;

			//Reset the file attributes to non-content indexed so indexing
			//services will not lock the file.
			try
			{
				info.Attributes = FileAttributes.NotContentIndexed;
			}
			catch (ArgumentException e)
			{
				//This is an undocumented exception: when the path we are setting
				//cannot be accessed (ERROR_ACCESS_DENIED is returned) an
				//ArgumentException is raised (no idea why!)
				throw new UnauthorizedAccessException(e.Message, e);
			}

			//Rename the file a few times to erase the entry from the file system
			//table.
			for (int i = 0, tries = 0; i < FileNameErasePasses; ++tries)
			{
				//Generate a new file name for the file/directory.
				string newPath = GenerateRandomFileName(info.GetParent(), info.Name.Length);

				try
				{
					//Reset the file access times: after every rename the file times may change.
					info.CreationTime = info.LastWriteTime = info.LastAccessTime = MinTimestamp;

					//Try to rename the file. If it fails, it is probably due to another
					//process locking the file. Defer, then rename again.
					info.MoveTo(newPath);
					++i;
				}
				catch (IOException e)
				{
					switch (System.Runtime.InteropServices.Marshal.GetLastWin32Error())
					{
						case Win32ErrorCode.AccessDenied:
							throw new UnauthorizedAccessException(S._("The file {0} could not " +
								"be erased because the file's permissions prevent access to the file.",
								info.FullName));

						case Win32ErrorCode.SharingViolation:
							//If after FilenameEraseTries the file is still locked, some program is
							//definitely using the file; throw an exception.
							if (tries > FileNameEraseTries)
								throw new IOException(S._("The file {0} is currently in use and " +
									"cannot be removed.", info.FullName), e);

							//Let the process locking the file release the lock
							Thread.Sleep(100);
							break;

						default:
							throw;
					}
				}
			}

			//Then delete the file.
			for (int i = 0; i < FileNameEraseTries; ++i)
				try
				{
					info.Delete();
					break;
				}
				catch (IOException e)
				{
					switch (System.Runtime.InteropServices.Marshal.GetLastWin32Error())
					{
						case Win32ErrorCode.AccessDenied:
							throw new UnauthorizedAccessException(S._("The file {0} could not " +
								"be erased because the file's permissions prevent access to the file.",
								info.FullName), e);

						case Win32ErrorCode.SharingViolation:
							//If after FilenameEraseTries the file is still locked, some program is
							//definitely using the file; throw an exception.
							if (i > FileNameEraseTries)
								throw new IOException(S._("The file {0} is currently in use and " +
									"cannot be removed.", info.FullName), e);

							//Let the process locking the file release the lock
							Thread.Sleep(100);
							break;

						default:
							throw;
					}
				}
		}

		public override void EraseClusterTips(VolumeInfo info, ErasureMethod method,
			ClusterTipsSearchProgress searchCallback, ClusterTipsEraseProgress eraseCallback)
		{
			//List all the files which can be erased.
			List<string> files = new List<string>();
			if (!info.IsMounted)
				throw new InvalidOperationException(S._("Could not erase cluster tips in {0} " +
					"as the volume is not mounted.", info.VolumeId));
			ListFiles(new DirectoryInfo(info.MountPoints[0]), files, searchCallback);

			//For every file, erase the cluster tips.
			for (int i = 0, j = files.Count; i != j; ++i)
			{
				//Get the file attributes for restoring later
				StreamInfo streamInfo = new StreamInfo(files[i]);
				if (!streamInfo.Exists)
					continue;

				FileAttributes fileAttr = streamInfo.Attributes;

				try
				{
					//Reset the file attributes.
					streamInfo.Attributes = FileAttributes.Normal;
					EraseFileClusterTips(files[i], method);
				}
				catch (UnauthorizedAccessException)
				{
					Logger.Log(S._("{0} did not have its cluster tips erased because you do not " +
						"have the required permissions to erase the file cluster tips.", files[i]),
						LogLevel.Information);
				}
				catch (IOException e)
				{
					Logger.Log(S._("{0} did not have its cluster tips erased. The error returned " +
						"was: {1}", files[i], e.Message), LogLevel.Error);
				}
				finally
				{
					streamInfo.Attributes = fileAttr;
				}

				eraseCallback(i, files.Count, files[i]);
			}
		}

		private void ListFiles(DirectoryInfo info, List<string> files,
			ClusterTipsSearchProgress searchCallback)
		{
			try
			{
				//Skip this directory if it is a reparse point
				if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
				{
					Logger.Log(S._("Files in {0} did not have their cluster tips erased because " +
						"it is a hard link or a symbolic link.", info.FullName),
						LogLevel.Information);
					return;
				}

				foreach (FileInfo file in info.GetFiles())
					if (file.IsProtectedSystemFile())
						Logger.Log(S._("{0} did not have its cluster tips erased, because it is " +
							"a system file", file.FullName), LogLevel.Information);
					else if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
						Logger.Log(S._("{0} did not have its cluster tips erased because it is a " +
							"hard link or a symbolic link.", file.FullName), LogLevel.Information);
					else if ((file.Attributes & FileAttributes.Compressed) != 0 ||
						(file.Attributes & FileAttributes.Encrypted) != 0 ||
						(file.Attributes & FileAttributes.SparseFile) != 0)
					{
						Logger.Log(S._("{0} did not have its cluster tips erased because it is " +
							"compressed, encrypted or a sparse file.", file.FullName),
							LogLevel.Information);
					}
					else
					{
						try
						{
							foreach (string i in file.GetADSes())
								files.Add(file.FullName + ':' + i);

							files.Add(file.FullName);
						}
						catch (UnauthorizedAccessException e)
						{
							Logger.Log(S._("{0} did not have its cluster tips erased because of " +
								"the following error: {1}", info.FullName, e.Message),
								LogLevel.Error);
						}
						catch (IOException e)
						{
							Logger.Log(S._("{0} did not have its cluster tips erased because of " +
								"the following error: {1}", info.FullName, e.Message),
								LogLevel.Error);
						}
					}

				foreach (DirectoryInfo subDirInfo in info.GetDirectories())
				{
					searchCallback(subDirInfo.FullName);
					ListFiles(subDirInfo, files, searchCallback);
				}
			}
			catch (UnauthorizedAccessException e)
			{
				Logger.Log(S._("{0} did not have its cluster tips erased because of the " +
					"following error: {1}", info.FullName, e.Message), LogLevel.Error);
			}
			catch (IOException e)
			{
				Logger.Log(S._("{0} did not have its cluster tips erased because of the " +
					"following error: {1}", info.FullName, e.Message), LogLevel.Error);
			}
		}

		/// <summary>
		/// Erases the cluster tips of the given file.
		/// </summary>
		/// <param name="file">The file to erase.</param>
		/// <param name="method">The erasure method to use.</param>
		private void EraseFileClusterTips(string file, ErasureMethod method)
		{
			//Get the file access times
			StreamInfo streamInfo = new StreamInfo(file);
			DateTime lastAccess = streamInfo.LastAccessTime;
			DateTime lastWrite = streamInfo.LastWriteTime;
			DateTime created = streamInfo.CreationTime;

			//And get the file lengths to know how much to overwrite
			long fileArea = GetFileArea(file);
			long fileLength = streamInfo.Length;

			//If the file length equals the file area there is no cluster tip to overwrite
			if (fileArea == fileLength)
				return;

			//Otherwise, create the stream, lengthen the file, then tell the erasure
			//method to erase the cluster tips.
			using (FileStream stream = streamInfo.Open(FileMode.Open, FileAccess.Write,
				FileShare.None, FileOptions.WriteThrough))
			{
				try
				{
					stream.SetLength(fileArea);
					stream.Seek(fileLength, SeekOrigin.Begin);

					//Erase the file
					method.Erase(stream, long.MaxValue,
						ManagerLibrary.Instance.PrngRegistrar[ManagerLibrary.Settings.ActivePrng],
						null);
				}
				finally
				{
					//Make sure the file length is restored!
					stream.SetLength(fileLength);

					//Reset the file times
					streamInfo.LastAccessTime = lastAccess;
					streamInfo.LastWriteTime = lastWrite;
					streamInfo.CreationTime = created;
				}
			}
		}

		public override long GetFileArea(string filePath)
		{
			StreamInfo info = new StreamInfo(filePath);
			VolumeInfo volume = VolumeInfo.FromMountPoint(info.Directory.FullName);
			long clusterSize = volume.ClusterSize;
			return (info.Length + (clusterSize - 1)) & ~(clusterSize - 1);
		}

		/// <summary>
		/// The minimum timestamp the file system can take. This is for secure file
		/// deletion.
		/// </summary>
		protected abstract DateTime MinTimestamp { get; }
	}
}