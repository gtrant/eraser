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

using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.IO;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Represents a folder and its files which are to be erased.
	/// </summary>
	[Serializable]
	[Guid("F50B0A44-3AB1-4cab-B81E-1713AC3D28C9")]
	public class FolderErasureTarget : FileSystemObjectErasureTarget
	{
		#region Serialization code
		protected FolderErasureTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			IncludeMask = (string)info.GetValue("IncludeMask", typeof(string));
			ExcludeMask = (string)info.GetValue("ExcludeMask", typeof(string));
			DeleteIfEmpty = (bool)info.GetValue("DeleteIfEmpty", typeof(bool));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("IncludeMask", IncludeMask);
			info.AddValue("ExcludeMask", ExcludeMask);
			info.AddValue("DeleteIfEmpty", DeleteIfEmpty);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FolderErasureTarget()
		{
			IncludeMask = string.Empty;
			ExcludeMask = string.Empty;
			DeleteIfEmpty = true;
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return S._("Files in Folder"); }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FolderErasureTargetConfigurer(); }
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			//Get a list to hold all the resulting paths.
			List<string> result = new List<string>();

			//Open the root of the search, including every file matching the pattern
			DirectoryInfo dir = new DirectoryInfo(Path);

			//List recursively all the files which match the include pattern.
			FileInfo[] files = GetFiles(dir);

			//Then exclude each file and finalize the list and total file size
			totalSize = 0;
			if (ExcludeMask.Length != 0)
			{
				string regex = Regex.Escape(ExcludeMask).Replace("\\*", ".*").
					Replace("\\?", ".");
				Regex excludePattern = new Regex(regex, RegexOptions.IgnoreCase);
				foreach (FileInfo file in files)
					if (file.Exists &&
						(file.Attributes & FileAttributes.ReparsePoint) == 0 &&
						excludePattern.Matches(file.FullName).Count == 0)
					{
						totalSize += file.Length;
						GetPathADSes(result, out totalSize, file.FullName);
						result.Add(file.FullName);
					}
			}
			else
				foreach (FileInfo file in files)
				{
					if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
						continue;

					//Get the size of the file and its ADSes
					totalSize += file.Length;
					long adsesSize = 0;
					GetPathADSes(result, out adsesSize, file.FullName);
					totalSize += adsesSize;

					//Append this file to the list of files to erase.
					result.Add(file.FullName);
				}

			//Return the filtered list.
			return result;
		}

		/// <summary>
		/// Gets all files in the provided directory.
		/// </summary>
		/// <param name="info">The directory to look files in.</param>
		/// <returns>A list of files found in the directory matching the IncludeMask
		/// property.</returns>
		private FileInfo[] GetFiles(DirectoryInfo info)
		{
			List<FileInfo> result = new List<FileInfo>();
			if (info.Exists)
			{
				try
				{
					foreach (DirectoryInfo dir in info.GetDirectories())
						result.AddRange(GetFiles(dir));

					if (IncludeMask.Length == 0)
						result.AddRange(info.GetFiles());
					else
						result.AddRange(info.GetFiles(IncludeMask, SearchOption.TopDirectoryOnly));
				}
				catch (UnauthorizedAccessException e)
				{
					Logger.Log(S._("Could not erase files and subfolders in {0} because {1}",
						info.FullName, e.Message), LogLevel.Error);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// A wildcard expression stating the condition for the set of files to include.
		/// The include mask is applied before the exclude mask is applied. If this value
		/// is empty, all files and folders within the folder specified is included.
		/// </summary>
		public string IncludeMask { get; set; }

		/// <summary>
		/// A wildcard expression stating the condition for removing files from the set
		/// of included files. If this value is omitted, all files and folders extracted
		/// by the inclusion mask is erased.
		/// </summary>
		public string ExcludeMask { get; set; }

		/// <summary>
		/// Determines if Eraser should delete the folder after the erase process.
		/// </summary>
		public bool DeleteIfEmpty { get; set; }

		public override void Execute()
		{
			Progress = new SteppedProgressManager();
			try
			{
				base.Execute();

				//If the user requested a folder removal, do it.
				if (Directory.Exists(Path))
				{
					ProgressManager step = new ProgressManager();
					Progress.Steps.Add(new SteppedProgressManagerStep(step,
						0.0f, S._("Removing folders...")));

					//Remove all subfolders which are empty.
					FileSystem fsManager = ManagerLibrary.Instance.FileSystemRegistrar[
						VolumeInfo.FromMountPoint(Path)];
					Action<DirectoryInfo> eraseEmptySubFolders = null;
					eraseEmptySubFolders = delegate(DirectoryInfo info)
					{
						foreach (DirectoryInfo subDir in info.GetDirectories())
							eraseEmptySubFolders(subDir);
						OnProgressChanged(this, new ProgressChangedEventArgs(step,
							new TaskProgressChangedEventArgs(info.FullName, 0, 0)));

						FileSystemInfo[] files = info.GetFileSystemInfos();
						if (files.Length == 0)
							fsManager.DeleteFolder(info);
					};

					DirectoryInfo directory = new DirectoryInfo(Path);
					foreach (DirectoryInfo subDir in directory.GetDirectories())
						eraseEmptySubFolders(subDir);

					if (DeleteIfEmpty)
					{
						//See if this is the root of a volume.
						bool isVolumeRoot = directory.Parent == null;
						foreach (VolumeInfo volume in VolumeInfo.Volumes)
							foreach (string mountPoint in volume.MountPoints)
								if (directory.FullName == mountPoint)
									isVolumeRoot = true;

						//If the folder is a mount point, then don't delete it. If it isn't,
						//search for files under the folder to see if it is empty.
						if (!isVolumeRoot && directory.Exists &&
							directory.GetFiles("*", SearchOption.AllDirectories).Length == 0)
						{
							fsManager.DeleteFolder(directory);
						}
					}
				}
			}
			finally
			{
				Progress = null;
			}
		}
	}
}
