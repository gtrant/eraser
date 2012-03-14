/* 
 * $Id: ProgressManager.cs 2406 2012-01-12 05:19:39Z lowjoel $
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

using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	abstract class FileSystemBase : IFileSystem
	{
		#region IFileSystem Members

		public abstract string Name
		{
			get;
		}

		public abstract void ResetFileTimes(FileSystemInfo info);

		public abstract void DeleteFile(FileInfo info);

		public abstract void DeleteFolder(DirectoryInfo info, bool recursive);

		public abstract void EraseClusterTips(VolumeInfo info, IErasureMethod method,
			ClusterTipsSearchProgress searchCallback, ClusterTipsEraseProgress eraseCallback);

		public abstract void EraseOldFileSystemResidentFiles(Util.VolumeInfo volume,
			DirectoryInfo tempDirectory, IErasureMethod method,
			FileSystemEntriesEraseProgress callback);

		public abstract void EraseDirectoryStructures(VolumeInfo info,
			FileSystemEntriesEraseProgress callback);

		public abstract void EraseFileSystemObject(StreamInfo info, IErasureMethod method,
			ErasureMethodProgressFunction callback);

		public abstract long GetFileArea(StreamInfo filePath);

		#endregion

		#region IRegisterable Members

		public abstract Guid Guid
		{
			get;
		}

		#endregion

		/// <summary>
		/// Gets a random file from within the provided directory.
		/// </summary>
		/// <param name="info">The directory to get a random file name from.</param>
		/// <returns>A string containing the full path to the file.</returns>
		public static string GetRandomFile(DirectoryInfo info)
		{
			//First retrieve the list of files and folders in the provided directory.
			FileSystemInfo[] entries = null;
			try
			{
				entries = info.GetFileSystemInfos();
			}
			catch (DirectoryNotFoundException)
			{
				return string.Empty;
			}
			if (entries.Length == 0)
				return string.Empty;

			//Find a random entry.
			IPrng prng = Host.Instance.Prngs.ActivePrng;
			string result = string.Empty;
			while (result.Length == 0)
			{
				int index = prng.Next(entries.Length - 1);
				if (entries[index] is DirectoryInfo)
					result = GetRandomFile((DirectoryInfo)entries[index]);
				else
					result = ((FileInfo)entries[index]).FullName;
			}

			return result;
		}

		/// <summary>
		/// Generates a random file name with the given length.
		/// </summary>
		/// <remarks>The generated file name is guaranteed not to exist.</remarks>
		/// <param name="info">The directory to generate the file name in. This
		/// parameter can be null to indicate merely a random file name</param>
		/// <param name="length">The length of the file name to generate.</param>
		/// <returns>A full path to a file containing random file name.</returns>
		public static string GenerateRandomFileName(DirectoryInfo info, int length)
		{
			//Get the PRNG we are going to use
			IPrng prng = Host.Instance.Prngs.ActivePrng;

			//Initialsie the base name, if any.
			string resultPrefix = info == null ? string.Empty : info.FullName +
				Path.DirectorySeparatorChar;

			//Variables to store the intermediates.
			byte[] resultAry = new byte[length];
			string result = string.Empty;
			List<string> prohibitedFileNames = new List<string>(new string[] {
				"CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
				"COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3",
				"LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
			});

			do
			{
				prng.NextBytes(resultAry);

				//Validate the name
				string validFileNameChars = "0123456789abcdefghijklmnopqrstuvwxyz" +
					"ABCDEFGHIJKLMNOPQRSTUVWXYZ _+=-()[]{}',`~!";
				for (int j = 0, k = resultAry.Length; j < k; ++j)
				{
					resultAry[j] = (byte)validFileNameChars[
						(int)resultAry[j] % validFileNameChars.Length];

					if (j == 0 || j == k - 1)
						//The first or last character cannot be whitespace
						while (Char.IsWhiteSpace((char)resultAry[j]))
							resultAry[j] = (byte)validFileNameChars[
								(int)resultAry[j] % validFileNameChars.Length];
				}

				result = Encoding.UTF8.GetString(resultAry);
			}
			while (info != null &&
				prohibitedFileNames.IndexOf(Path.GetFileNameWithoutExtension(result)) != -1 ||
				(Directory.Exists(resultPrefix + result) || File.Exists(resultPrefix + result)));
			return resultPrefix + result;
		}

		/// <summary>
		/// The number of times file names are renamed to erase the file name from
		/// the file system table.
		/// </summary>
		public const int FileNameErasePasses = 7;

		/// <summary>
		/// The maximum number of times Eraser tries to erase a file/folder before
		/// it gives up.
		/// </summary>
		public const int FileNameEraseTries = 50;
	}
}
