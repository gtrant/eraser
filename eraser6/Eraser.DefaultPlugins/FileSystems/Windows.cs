/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Base class for all Windows filesystems.
	/// </summary>
	public abstract class WindowsFileSystem : FileSystem
	{
		public override void DeleteFile(FileInfo info)
		{
			//Set the date of the file to be invalid to prevent forensic
			//detection
			info.CreationTime = info.LastWriteTime = info.LastAccessTime = MinTimestamp;
			info.Attributes = FileAttributes.Normal;
			info.Attributes = FileAttributes.NotContentIndexed;

			//Rename the file a few times to erase the entry from the file system
			//table.
			string newPath = GenerateRandomFileName(info.Directory, info.Name.Length);
			for (int i = 0, tries = 0; i < FileNameErasePasses; ++tries)
			{
				//Try to rename the file. If it fails, it is probably due to another
				//process locking the file. Defer, then rename again.
				try
				{
					info.MoveTo(newPath);
					++i;
				}
				catch (IOException)
				{
					Thread.Sleep(100);

					//If after FilenameEraseTries the file is still locked, some program is
					//definitely using the file; throw an exception.
					if (tries > FileNameEraseTries)
						throw new IOException(S._("The file {0} is currently in use and " +
							"cannot be removed.", info.FullName));
				}
			}

			//If the user wants plausible deniability, find a random file on the same
			//volume and write it over.
			if (Manager.ManagerLibrary.Settings.PlausibleDeniability)
			{
				CopyPlausibleDeniabilityFile(info.OpenWrite());
			}

			//Then delete the file.
			for (int i = 0; i < FileNameEraseTries; ++i)
				try
				{
					info.Delete();
					break;
				}
				catch (IOException)
				{
					if (i > FileNameEraseTries)
						throw new IOException(S._("The file {0} is currently in use and " +
							"cannot be removed.", info.FullName));
					Thread.Sleep(100);
				}
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

			//Then clean up this folder.
			for (int i = 0; i < FileNameErasePasses; ++i)
			{
				//Rename the folder.
				string newPath = GenerateRandomFileName(info.Parent, info.Name.Length);

				//Try to rename the file. If it fails, it is probably due to another
				//process locking the file. Defer, then rename again.
				try
				{
					info.MoveTo(newPath);
				}
				catch (IOException)
				{
					Thread.Sleep(100);
					--i;
				}
			}

			//Set the date of the directory to be invalid to prevent forensic
			//detection
			info.CreationTime = info.LastWriteTime = info.LastAccessTime = MinTimestamp;

			//Remove the folder
			info.Delete(true);
		}

		public override void EraseClusterTips(VolumeInfo info, ErasureMethod method,
			Logger log, ClusterTipsSearchProgress searchCallback,
			ClusterTipsEraseProgress eraseCallback)
		{
			//List all the files which can be erased.
			List<string> files = new List<string>();
			if (!info.IsMounted)
				throw new InvalidOperationException(S._("Could not erase cluster tips in {0} " +
					"as the volume is not mounted.", info.VolumeId));
			ListFiles(new DirectoryInfo(info.MountPoints[0]), files, log, searchCallback);

			//For every file, erase the cluster tips.
			for (int i = 0, j = files.Count; i != j; ++i)
			{
				//Get the file attributes for restoring later
				StreamInfo streamInfo = new StreamInfo(files[i]);
				FileAttributes fileAttr = streamInfo.Attributes;

				try
				{
					//Reset the file attributes.
					streamInfo.Attributes = FileAttributes.Normal;
					EraseFileClusterTips(files[i], method);
				}
				catch (UnauthorizedAccessException)
				{
					log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have its " +
						"cluster tips erased because you do not have the required permissions to " +
						"erase the file cluster tips.", files[i]), LogLevel.Error));
				}
				catch (IOException e)
				{
					log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have its " +
						"cluster tips erased. The error returned was: {1}", files[i],
						e.Message), LogLevel.Error));
				}
				finally
				{
					streamInfo.Attributes = fileAttr;
				}
				eraseCallback(i, files.Count, files[i]);
			}
		}

		private void ListFiles(DirectoryInfo info, List<string> files, Logger log,
			ClusterTipsSearchProgress searchCallback)
		{
			try
			{
				//Skip this directory if it is a reparse point
				if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
				{
					log.LastSessionEntries.Add(new LogEntry(S._("Files in {0} did " +
						"not have their cluster tips erased because it is a hard link or " +
						"a symbolic link.", info.FullName), LogLevel.Information));
					return;
				}

				foreach (FileInfo file in info.GetFiles())
					if (Util.File.IsProtectedSystemFile(file.FullName))
						log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have " +
							"its cluster tips erased, because it is a system file",
							file.FullName), LogLevel.Information));
					else if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
						log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have " +
							"its cluster tips erased because it is a hard link or a " +
							"symbolic link.", file.FullName), LogLevel.Information));
					else if ((file.Attributes & FileAttributes.Compressed) != 0 ||
						(file.Attributes & FileAttributes.Encrypted) != 0 ||
						(file.Attributes & FileAttributes.SparseFile) != 0)
					{
						log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have " +
							"its cluster tips erased because it is compressed, encrypted " +
							"or a sparse file.", file.FullName), LogLevel.Information));
					}
					else
					{
						try
						{
							foreach (string i in Util.File.GetADSes(file))
								files.Add(file.FullName + ':' + i);

							files.Add(file.FullName);
						}
						catch (UnauthorizedAccessException e)
						{
							log.LastSessionEntries.Add(new LogEntry(S._("{0} did not " +
								"have its cluster tips erased because of the following " +
								"error: {1}", info.FullName, e.Message), LogLevel.Error));
						}
						catch (IOException e)
						{
							log.LastSessionEntries.Add(new LogEntry(S._("{0} did not " +
								"have its cluster tips erased because of the following " +
								"error: {1}", info.FullName, e.Message), LogLevel.Error));
						}
					}

				foreach (DirectoryInfo subDirInfo in info.GetDirectories())
				{
					searchCallback(subDirInfo.FullName);
					ListFiles(subDirInfo, files, log, searchCallback);
				}
			}
			catch (UnauthorizedAccessException e)
			{
				log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have its " +
					"cluster tips erased because of the following error: {1}",
					info.FullName, e.Message), LogLevel.Error));
			}
			catch (IOException e)
			{
				log.LastSessionEntries.Add(new LogEntry(S._("{0} did not have its " +
					"cluster tips erased because of the following error: {1}",
					info.FullName, e.Message), LogLevel.Error));
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
					method.Erase(stream, long.MaxValue, PrngManager.GetInstance(
						ManagerLibrary.Settings.ActivePrng), null);
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
			VolumeInfo volume = VolumeInfo.FromMountpoint(info.Directory.FullName);
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
