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
using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Provides functions to handle erasures specific to FAT volumes.
	/// </summary>
	public abstract class FatFileSystem : WindowsFileSystem
	{
		public override void EraseOldFileSystemResidentFiles(VolumeInfo volume,
			DirectoryInfo tempDirectory, ErasureMethod method,
			FileSystemEntriesEraseProgress callback)
		{
			//Nothing to be done here. FAT doesn't store files in its FAT.
		}

		public override void EraseFileSystemObject(StreamInfo info, ErasureMethod method,
			ErasureMethodProgressFunction callback)
		{
			//Create the file stream, and call the erasure method to write to
			//the stream.
			long fileArea = GetFileArea(info.FullName);
			using (FileStream strm = info.Open(FileMode.Open, FileAccess.Write,
				FileShare.None, FileOptions.WriteThrough))
			{
				//Set the end of the stream after the wrap-round the cluster size
				strm.SetLength(fileArea);

				//If the stream is empty, there's nothing to overwrite. Continue
				//to the next entry
				if (strm.Length != 0)
				{
					//Then erase the file.
					method.Erase(strm, long.MaxValue,
						PrngManager.GetInstance(ManagerLibrary.Settings.ActivePrng),
						callback
					);
				}

				//Set the length of the file to 0.
				strm.Seek(0, SeekOrigin.Begin);
				strm.SetLength(0);
			}
		}

		public override void EraseDirectoryStructures(VolumeInfo info,
			FileSystemEntriesEraseProgress callback)
		{
			using (FileStream stream = info.Open(FileAccess.ReadWrite, FileShare.ReadWrite))
			{
				int directoriesCleaned = 0;
				FatApi api = GetFatApi(info, stream);
				HashSet<uint> eraseQueueClusters = new HashSet<uint>();
				List<FatDirectoryEntry> eraseQueue = new List<FatDirectoryEntry>();
				{
					FatDirectoryEntry entry = api.LoadDirectory(string.Empty);
					eraseQueue.Add(entry);
					eraseQueueClusters.Add(entry.Cluster);
				}

				using (VolumeLock volumeLock = info.LockVolume(stream))
				{
					while (eraseQueue.Count != 0)
					{
						if (callback != null)
							callback(directoriesCleaned, directoriesCleaned + eraseQueue.Count);

						FatDirectoryBase currentDir = api.LoadDirectory(eraseQueue[0].FullName);
						eraseQueue.RemoveAt(0);

						//Queue the subfolders in this directory
						foreach (KeyValuePair<string, FatDirectoryEntry> entry in currentDir.Items)
							if (entry.Value.EntryType == FatDirectoryEntryType.Directory)
							{
								//Check that we don't have the same cluster queued twice (e.g. for
								//long/8.3 file names)
								if (eraseQueueClusters.Contains(entry.Value.Cluster))
									continue;

								eraseQueueClusters.Add(entry.Value.Cluster);
								eraseQueue.Add(entry.Value);
							}

						currentDir.ClearDeletedEntries();
						++directoriesCleaned;
					}
				}
			}
		}

		protected override DateTime MinTimestamp
		{
			get
			{
				return new DateTime(1980, 1, 1, 0, 0, 0);
			}
		}

		/// <summary>
		///  Gets the FAT API to use to interface with the disk.
		/// </summary>
		protected abstract FatApi GetFatApi(VolumeInfo info, FileStream stream);
	}

	public class Fat12FileSystem : FatFileSystem
	{
		public override bool Supports(string fileSystemName)
		{
			if (fileSystemName == "FAT12")
				return true;
			return false;
		}

		protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
		{
			return new Fat12Api(info, stream);
		}
	}

	public class Fat16FileSystem : FatFileSystem
	{
		public override bool Supports(string fileSystemName)
		{
			if (fileSystemName == "FAT16")
				return true;
			return false;
		}

		protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
		{
			return new Fat16Api(info, stream);
		}
	}

	public class Fat32FileSystem : FatFileSystem
	{
		public override bool Supports(string fileSystemName)
		{
			if (fileSystemName == "FAT32")
				return true;
			return false;
		}

		protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
		{
			return new Fat32Api(info, stream);
		}
	}
}