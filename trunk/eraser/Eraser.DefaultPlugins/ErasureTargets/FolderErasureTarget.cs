/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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
using System.Xml;
using System.Xml.Serialization;
using System.Security.Permissions;
using System.IO;
using System.Globalization;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

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

		protected override void ReadXml(XmlReader reader, bool advance)
		{
            
			IncludeMask = reader.GetAttribute("includeMask");
			ExcludeMask = reader.GetAttribute("excludeMask");
            bool deleteIfEmpty = true;
            bool.TryParse(reader.GetAttribute("deleteIfEmpty"), out deleteIfEmpty);
            DeleteIfEmpty = deleteIfEmpty;

            base.ReadXml(reader, false);
           
			/*if (reader.HasAttributes)
			{
				bool deleteIfEmpty = true;
				bool.TryParse(reader.GetAttribute("deleteIfEmpty"), out deleteIfEmpty);
				DeleteIfEmpty = deleteIfEmpty;
			}*/

			if (advance)
				reader.Read();
		}

		public override void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("includeMask", IncludeMask);
			writer.WriteAttributeString("excludeMask", ExcludeMask);
			writer.WriteAttributeString("deleteIfEmpty",
				DeleteIfEmpty.ToString(CultureInfo.InvariantCulture));
			base.WriteXml(writer);
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

				try
				{
					//Add the size of the file and its alternate data streams
					result.AddRange(GetPathADSes(file));

					//And the file itself
					result.Add(new StreamInfo(file.FullName));
				}
				catch (FileNotFoundException)
				{
					Logger.Log(S._("The file {0} was not erased because it was deleted " +
						"before it could be erased.", file.FullName), LogLevel.Information);
				}
				catch (DirectoryNotFoundException)
				{
					Logger.Log(S._("The file {0} was not erased because the containing " +
						"directory was deleted before it could be erased.", file.FullName),
						LogLevel.Information);
				}
				catch (SharingViolationException)
				{
					Logger.Log(S._("Could not list the Alternate Data Streams for file {0} " +
						"because the file is being used by another process. The file will not " +
						"be erased.", file.FullName), LogLevel.Error);
				}
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

			//Erase all subdirectories which are not reparse points.
			DirectoryInfo directory = new DirectoryInfo(Path);
			if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
				foreach (DirectoryInfo subDir in directory.GetDirectories())
					EraseFolder(subDir, step);

			//Does the user want this directory to be erased if there are no more
			//entries within it?
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
				if (!isVolumeRoot && directory.Exists)
				{
					IFileSystem fsManager = Host.Instance.FileSystems[
						VolumeInfo.FromMountPoint(Path)];
					if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
					{
						if (directory.GetFiles("*", SearchOption.AllDirectories).Length == 0)
							fsManager.DeleteFolder(directory, true);
					}
					else
					{
						fsManager.DeleteFolder(directory, false);
					}
				}
			}
		}

		private void EraseFolder(DirectoryInfo info, ProgressManager progress)
		{
			//Skip all symbolic links and junctions as we want to retain the
			//contents of those directories.
			if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
				return;

			//Iterate over each directory and erase the subdirectories.
			foreach (DirectoryInfo subDir in info.GetDirectories())
				EraseFolder(subDir, progress);

			//Public progress updates.
			progress.Tag = info.FullName;

			//Ensure that the current directory is empty before deleting.
			FileSystemInfo[] files = info.GetFileSystemInfos();
			if (files.Length == 0)
			{
				try
				{
					Host.Instance.FileSystems[VolumeInfo.FromMountPoint(Path)].
						DeleteFolder(info, true);
				}
				catch (DirectoryNotFoundException)
				{
					Logger.Log(new LogEntry(S._("The folder {0} was not erased because " +
						"the containing directory was deleted before it could be erased.",
						info.FullName), LogLevel.Information));
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
