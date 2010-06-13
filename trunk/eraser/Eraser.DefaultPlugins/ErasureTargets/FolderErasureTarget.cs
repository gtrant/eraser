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

		protected override List<StreamInfo> GetPaths()
		{
			//Get a list to hold all the resulting streams.
			List<StreamInfo> result = new List<StreamInfo>();

			//Open the root of the search, including every file matching the pattern
			DirectoryInfo dir = new DirectoryInfo(Path);

			//List recursively all the files which match the include pattern.
			FileInfo[] files = GetFiles(dir);

			//Then exclude each file and finalize the list and total file size
			Regex includePattern = string.IsNullOrEmpty(IncludeMask) ? null :
				new Regex(
					Regex.Escape(ExcludeMask).Replace("\\*", ".*").Replace("\\?", "."),
					RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex excludePattern = string.IsNullOrEmpty(ExcludeMask) ? null :
				new Regex(
					Regex.Escape(ExcludeMask).Replace("\\*", ".*").Replace("\\?", "."),
					RegexOptions.IgnoreCase | RegexOptions.Compiled);
			foreach (FileInfo file in files)
			{
				//Check that the file is included
				if (includePattern != null && !includePattern.Match(file.FullName).Success)
					continue;

				//Check that the file is not excluded
				if (excludePattern != null && excludePattern.Match(file.FullName).Success)
					continue;

				//Add the size of the file and its alternate data streams
				result.AddRange(GetPathADSes(file));

				//And the file itself
				result.Add(new StreamInfo(file.FullName));
			}

			//Return the filtered list.
			return result;
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

				//Remove the contents of the folder, deleting the folder if it is empty
				//at the end of it.
				EraseFolder();
			}
			finally
			{
				Progress = null;
			}
		}

		/// <summary>
		/// Erases the folder after all files have been deleted. This folder does not
		/// delete folders which have files within it.
		/// </summary>
		private void EraseFolder()
		{
			//Update the progress to show that folders are being removed.
			ProgressManager step = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(step,
				0.0f, S._("Removing folders...")));

			//Remove all subfolders which are empty.
			FileSystem fsManager = ManagerLibrary.Instance.FileSystemRegistrar[
				VolumeInfo.FromMountPoint(Path)];
			DirectoryInfo directory = new DirectoryInfo(Path);
				foreach (DirectoryInfo subDir in directory.GetDirectories())
					EraseFolder(subDir, step);

			if (DeleteIfEmpty)
			{
				//See if this is the root of a volume.
				bool isVolumeRoot = directory.Parent == null;
				foreach (VolumeInfo volume in VolumeInfo.Volumes)
					if (volume.IsReady)
						foreach (DirectoryInfo mountPoint in volume.MountPoints)
							if (directory.FullName == mountPoint.FullName)
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

		private void EraseFolder(DirectoryInfo info, ProgressManager progress)
		{
			foreach (DirectoryInfo subDir in info.GetDirectories())
				EraseFolder(subDir, progress);
			OnProgressChanged(this, new ProgressChangedEventArgs(progress,
				new TaskProgressChangedEventArgs(info.FullName, 0, 0)));

			FileSystemInfo[] files = info.GetFileSystemInfos();
			if (files.Length == 0)
			{
				try
				{
					ManagerLibrary.Instance.FileSystemRegistrar[
						VolumeInfo.FromMountPoint(Path)].DeleteFolder(info);
				}
				catch (UnauthorizedAccessException)
				{
					Logger.Log(new LogEntry(S._("The folder {0} could not be deleted because " +
						"the folder's permissions prevents the deletion of the folder.",
						info.FullName), LogLevel.Error));
				}
			}
		}
	}
}
