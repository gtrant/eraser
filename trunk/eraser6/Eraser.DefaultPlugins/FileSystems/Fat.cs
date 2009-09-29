/* 
 * $Id: Plugin.cs 1100 2009-06-03 02:49:33Z lowjoel $
 * Copyright 2008 The Eraser Project
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

		protected override DateTime MinTimestamp
		{
			get
			{
				return new DateTime(1980, 1, 1, 0, 0, 0);
			}
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

		public override void EraseDirectoryStructures(VolumeInfo info,
			FileSystemEntriesEraseProgress callback)
		{
			Fat32Api api = new Fat32Api(info);
		}
	}
}