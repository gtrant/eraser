/* 
 * $Id: Fat.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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
using System.Runtime.InteropServices;

using System.IO;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Provides functions to handle erasures specific to FAT volumes.
	/// </summary>
	abstract class FatFileSystem : WindowsFileSystem
	{
		public override void EraseOldFileSystemResidentFiles(VolumeInfo volume,
			DirectoryInfo tempDirectory, IErasureMethod method,
			FileSystemEntriesEraseProgress callback)
		{
			//Nothing to be done here. FAT doesn't store files in its FAT.
		}

		public override void EraseFileSystemObject(StreamInfo info, IErasureMethod method,
			ErasureMethodProgressFunction callback)
		{
			//Create the file stream, and call the erasure method to write to
			//the stream.
			long fileArea = GetFileArea(info);
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
					method.Erase(strm, long.MaxValue, Host.Instance.Prngs.ActivePrng, callback);
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
			using (FatApi api = GetFatApi(info, stream))
			{
				int directoriesCleaned = 0;
				HashSet<uint> eraseQueueClusters = new HashSet<uint>();
				List<FatDirectoryEntry> eraseQueue = new List<FatDirectoryEntry>();

				try
				{
					{
						FatDirectoryEntry entry = api.LoadDirectory(string.Empty);
						eraseQueue.Add(entry);
						eraseQueueClusters.Add(entry.Cluster);
					}

					while (eraseQueue.Count != 0)
					{
						if (callback != null)
							callback(directoriesCleaned, directoriesCleaned + eraseQueue.Count);

						FatDirectoryBase currentDir = api.LoadDirectory(eraseQueue[0].FullName);
						eraseQueue[0].Dispose();
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
				catch (SharingViolationException)
				{
					Logger.Log(S._("Could not erase directory entries on the volume {0} because " +
						"the volume is currently in use."));
				}
				finally
				{
					foreach (FatDirectoryEntry entry in eraseQueue)
						entry.Dispose();
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

	[Guid("36C78D78-7EE4-4304-8068-10755651AF2D")]
	class Fat12FileSystem : FatFileSystem
	{
		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return "FAT12"; }
		}

		protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
		{
			return new Fat12Api(info, stream);
		}
	}

	[Guid("8C9DF746-1CD6-435d-8D04-3FE40A0A1C83")]
	class Fat16FileSystem : FatFileSystem
	{
		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return "FAT16"; }
		}

		protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
		{
			return new Fat16Api(info, stream);
		}
	}

	[Guid("1FCD66DC-179D-4402-8FF8-D19F74A4C398")]
	class Fat32FileSystem : FatFileSystem
	{
		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return "FAT32"; }
		}

		protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
		{
			return new Fat32Api(info, stream);
		}
	}
    [Guid("F4BF892D-2739-4DE0-B0F0-AD2280B7E15D")]
    class exFatFileSystem : FatFileSystem
    {
        public override Guid Guid
        {
            get { return GetType().GUID; }
        }

        public override string Name
        {
            get { return "exFAT"; }
        }

        protected override FatApi GetFatApi(VolumeInfo info, FileStream stream)
        {
            return new Fat32Api(info, stream);
        }
    }
}