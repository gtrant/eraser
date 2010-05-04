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
using Eraser.Util.ExtensionMethods;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Class representing a tangible object (file/folder) to be erased.
	/// </summary>
	[Serializable]
	public abstract class FileSystemObjectTarget : ErasureTarget
	{
		#region Serialization code
		protected FileSystemObjectTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Path = (string)info.GetValue("Path", typeof(string));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Path", Path);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected FileSystemObjectTarget()
			: base()
		{
		}

		protected new SteppedProgressManager Progress
		{
			get
			{
				return (SteppedProgressManager)base.Progress;
			}
			set
			{
				base.Progress = value;
			}
		}

		/// <summary>
		/// Retrieves the list of files/folders to erase as a list.
		/// </summary>
		/// <param name="totalSize">Returns the total size in bytes of the
		/// items.</param>
		/// <returns>A list containing the paths to all the files to be erased.</returns>
		internal abstract List<string> GetPaths(out long totalSize);

		/// <summary>
		/// Adds ADSes of the given file to the list.
		/// </summary>
		/// <param name="list">The list to add the ADS paths to.</param>
		/// <param name="file">The file to look for ADSes</param>
		protected void GetPathADSes(ICollection<string> list, out long totalSize, string file)
		{
			totalSize = 0;

			try
			{
				//Get the ADS names
				IList<string> adses = new FileInfo(file).GetADSes();

				//Then prepend the path.
				foreach (string adsName in adses)
				{
					string adsPath = file + ':' + adsName;
					list.Add(adsPath);
					StreamInfo info = new StreamInfo(adsPath);
					totalSize += info.Length;
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (SharingViolationException)
			{
				//The system cannot open the file, try to force the file handle to close.
				if (!ManagerLibrary.Settings.ForceUnlockLockedFiles)
					throw;

				StringBuilder processStr = new StringBuilder();
				foreach (OpenHandle handle in OpenHandle.Close(file))
				{
					try
					{
						processStr.AppendFormat(
							System.Globalization.CultureInfo.InvariantCulture,
							"{0}, ", (System.Diagnostics.Process.GetProcessById(handle.ProcessId)).MainModule.FileName);
					}
					catch (System.ComponentModel.Win32Exception)
					{
						processStr.AppendFormat(
							System.Globalization.CultureInfo.InvariantCulture,
							"Process ID {0}, ", handle.ProcessId);
					}
				}

				if (processStr.Length == 0)
				{
					GetPathADSes(list, out totalSize, file);
					return;
				}
				else
					throw;
			}
			catch (UnauthorizedAccessException e)
			{
				//The system cannot read the file, assume no ADSes for lack of
				//more information.
				Logger.Log(e.Message, LogLevel.Error);
			}
		}

		/// <summary>
		/// The path to the file or folder referred to by this object.
		/// </summary>
		public string Path { get; set; }

		public sealed override ErasureMethod EffectiveMethod
		{
			get
			{
				if (Method != ErasureMethodRegistrar.Default)
					return base.EffectiveMethod;

				return ManagerLibrary.Instance.ErasureMethodRegistrar[
					ManagerLibrary.Settings.DefaultFileErasureMethod];
			}
		}

		public override string UIText
		{
			get
			{
				string fileName = System.IO.Path.GetFileName(Path);
				string directoryName = System.IO.Path.GetDirectoryName(Path);
				return string.IsNullOrEmpty(fileName) ?
						(string.IsNullOrEmpty(directoryName) ? Path : directoryName)
					: fileName;
			}
		}

		public override void Execute()
		{
			//Retrieve the list of files to erase.
			long dataTotal = 0;
			List<string> paths = GetPaths(out dataTotal);

			//Get the erasure method if the user specified he wants the default.
			ErasureMethod method = EffectiveMethod;
			dataTotal = method.CalculateEraseDataSize(paths, dataTotal);

			//Set the event's current target status.
			SteppedProgressManager progress = new SteppedProgressManager();
			Progress = progress;
			Task.Progress.Steps.Add(new SteppedProgressManagerStep(progress, 1.0f / Task.Targets.Count));

			//Iterate over every path, and erase the path.
			for (int i = 0; i < paths.Count; ++i)
			{
				//Check that the file exists - we do not want to bother erasing nonexistant files
				StreamInfo info = new StreamInfo(paths[i]);
				if (!info.Exists)
				{
					Logger.Log(S._("The file {0} was not erased as the file does not exist.",
						paths[i]), LogLevel.Notice);
					continue;
				}

				//Get the filesystem provider to handle the secure file erasures
				FileSystem fsManager = ManagerLibrary.Instance.FileSystemRegistrar[
					VolumeInfo.FromMountPoint(info.DirectoryName)];

				bool isReadOnly = false;

				try
				{
					//Update the task progress
					ProgressManager step = new ProgressManager();
					progress.Steps.Add(new SteppedProgressManagerStep(step,
						info.Length / (float)dataTotal, S._("Erasing files...")));
					OnProgressChanged(this, new ProgressChangedEventArgs(step,
						new TaskProgressChangedEventArgs(paths[i], 0, method.Passes)));

					//Remove the read-only flag, if it is set.
					if (isReadOnly = info.IsReadOnly)
						info.IsReadOnly = false;

					//Make sure the file does not have any attributes which may affect
					//the erasure process
					if ((info.Attributes & FileAttributes.Compressed) != 0 ||
						(info.Attributes & FileAttributes.Encrypted) != 0 ||
						(info.Attributes & FileAttributes.SparseFile) != 0)
					{
						//Log the error
						Logger.Log(S._("The file {0} could not be erased because the file was " +
							"either compressed, encrypted or a sparse file.", info.FullName),
							LogLevel.Error);
						continue;
					}

					fsManager.EraseFileSystemObject(info, method,
						delegate(long lastWritten, long totalData, int currentPass)
						{
							if (Task.Canceled)
								throw new OperationCanceledException(S._("The task was cancelled."));

							step.Total = totalData;
							step.Completed += lastWritten;
							OnProgressChanged(this, new ProgressChangedEventArgs(step,
								new TaskProgressChangedEventArgs(info.FullName, currentPass, method.Passes)));
						});

					//Remove the file.
					FileInfo fileInfo = info.File;
					if (fileInfo != null)
						fsManager.DeleteFile(fileInfo);
					step.MarkComplete();
				}
				catch (UnauthorizedAccessException)
				{
					Logger.Log(S._("The file {0} could not be erased because the file's " +
						"permissions prevent access to the file.", info.FullName), LogLevel.Error);
				}
				catch (SharingViolationException)
				{
					if (!ManagerLibrary.Settings.ForceUnlockLockedFiles)
						throw;

					StringBuilder processStr = new StringBuilder();
					foreach (OpenHandle handle in OpenHandle.Close(info.FullName))
					{
						try
						{
							processStr.AppendFormat(
								System.Globalization.CultureInfo.InvariantCulture,
								"{0}, ", System.Diagnostics.Process.GetProcessById(handle.ProcessId).MainModule.FileName);
						}
						catch (System.ComponentModel.Win32Exception)
						{
							processStr.AppendFormat(
								System.Globalization.CultureInfo.InvariantCulture,
								"Process ID {0}, ", handle.ProcessId);
						}
					}

					if (processStr.Length != 0)
						Logger.Log(S._("Could not force closure of file \"{0}\" {1}",
								paths[i], S._("(locked by {0})",
									processStr.ToString().Remove(processStr.Length - 2)).Trim()),
							LogLevel.Error);
				}
				finally
				{
					//Re-set the read-only flag if the file exists (i.e. there was an error)
					if (isReadOnly && info.Exists && !info.IsReadOnly)
						info.IsReadOnly = isReadOnly;
				}
			}

			Progress = null;
		}
	}

	/// <summary>
	/// Class representing a unused space erase.
	/// </summary>
	[Serializable]
	[Guid("A627BEC4-CAFC-46ce-92AD-209157C3177A")]
	public class UnusedSpaceTarget : ErasureTarget
	{
		#region Serialization code
		protected UnusedSpaceTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Drive = (string)info.GetValue("Drive", typeof(string));
			EraseClusterTips = (bool)info.GetValue("EraseClusterTips", typeof(bool));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Drive", Drive);
			info.AddValue("EraseClusterTips", EraseClusterTips);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public UnusedSpaceTarget()
			: base()
		{
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public sealed override ErasureMethod EffectiveMethod
		{
			get
			{
				if (Method == ErasureMethodRegistrar.Default)
					return base.EffectiveMethod;

				return ManagerLibrary.Instance.ErasureMethodRegistrar[
					ManagerLibrary.Settings.DefaultUnusedSpaceErasureMethod];
			}
		}

		public override bool SupportsMethod(ErasureMethod method)
		{
			return method == ErasureMethodRegistrar.Default ||
				method is UnusedSpaceErasureMethod;
		}

		/// <summary>
		/// Override the base class property so that we won't need to keep casting
		/// </summary>
		protected new SteppedProgressManager Progress
		{
			get
			{
				return (SteppedProgressManager)base.Progress;
			}
			set
			{
				base.Progress = value;
			}
		}

		public override string UIText
		{
			get { return S._("Unused disk space ({0})", Drive); }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new UnusedSpaceErasureTargetSettings(); }
		}

		/// <summary>
		/// The drive to erase
		/// </summary>
		public string Drive { get; set; }

		/// <summary>
		/// Whether cluster tips should be erased.
		/// </summary>
		public bool EraseClusterTips { get; set; }

		public override void Execute()
		{
			//Check for sufficient privileges to run the unused space erasure.
			if (!Security.IsAdministrator())
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
					Environment.OSVersion.Version >= new Version(6, 0))
				{
					Logger.Log(S._("The program does not have the required permissions to erase " +
						"the unused space on disk. Run the program as an administrator and retry " +
						"the operation."), LogLevel.Error);
				}
				else
				{
					Logger.Log(S._("The program does not have the required permissions to erase " +
						"the unused space on disk."), LogLevel.Error);
				}

				return;
			}

			//Check whether System Restore has any available checkpoints.
			if (SystemRestore.GetInstances().Count != 0)
			{
				Logger.Log(S._("This computer has had System Restore or Volume Shadow Copies " +
					"enabled. This may allow copies of files stored on the disk to be recovered " +
					"and pose a security concern.", Drive), LogLevel.Warning);
			}

			//If the user is under disk quotas, log a warning message
			if (VolumeInfo.FromMountPoint(Drive).HasQuota)
				Logger.Log(S._("The drive {0} has disk quotas active. This will prevent the " +
					"complete erasure of unused space and may pose a security concern.",
					Drive), LogLevel.Warning);

			//Get the erasure method if the user specified he wants the default.
			ErasureMethod method = EffectiveMethod;

			//Make a folder to dump our temporary files in
			DirectoryInfo info = new DirectoryInfo(Drive);
			VolumeInfo volInfo = VolumeInfo.FromMountPoint(Drive);
			FileSystem fsManager = ManagerLibrary.Instance.FileSystemRegistrar[volInfo];

			//Start sampling the speed of the task.
			Progress = new SteppedProgressManager();
			Task.Progress.Steps.Add(new SteppedProgressManagerStep(
				Progress, 1.0f / Task.Targets.Count));

			//Erase the cluster tips of every file on the drive.
			if (EraseClusterTips)
			{
				//Define the callback handlers
				ProgressManager tipSearch = new ProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(tipSearch,
					0.0f, S._("Searching for files' cluster tips...")));
				tipSearch.Total = 1;
				ClusterTipsSearchProgress searchProgress = delegate(string path)
				{
					if (Task.Canceled)
						throw new OperationCanceledException(S._("The task was cancelled."));

					OnProgressChanged(this, new ProgressChangedEventArgs(tipSearch,
						new TaskProgressChangedEventArgs(path, 0, 0)));
				};

				ProgressManager tipProgress = new ProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(tipProgress, 0.1f,
					S._("Erasing cluster tips...")));
				ClusterTipsEraseProgress eraseProgress =
					delegate(int currentFile, int totalFiles, string currentFilePath)
					{
						tipSearch.MarkComplete();
						tipProgress.Total = totalFiles;
						tipProgress.Completed = currentFile;
						OnProgressChanged(this, new ProgressChangedEventArgs(tipProgress,
							new TaskProgressChangedEventArgs(currentFilePath, 0, 0)));

						if (Task.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));
					};

				//Start counting statistics
				fsManager.EraseClusterTips(VolumeInfo.FromMountPoint(Drive),
					method, searchProgress, eraseProgress);
				tipProgress.MarkComplete();
			}

			bool lowDiskSpaceNotifications = Shell.LowDiskSpaceNotificationsEnabled;
			info = info.CreateSubdirectory(Path.GetFileName(
				FileSystem.GenerateRandomFileName(info, 18)));
			try
			{
				//Set the folder's compression flag off since we want to use as much
				//space as possible
				if (info.IsCompressed())
					info.Uncompress();

				//Disable the low disk space notifications
				Shell.LowDiskSpaceNotificationsEnabled = false;

				ProgressManager mainProgress = new ProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(mainProgress,
					EraseClusterTips ? 0.8f : 0.9f, S._("Erasing unused space...")));

				//Continue creating files while there is free space.
				while (volInfo.AvailableFreeSpace > 0)
				{
					//Generate a non-existant file name
					string currFile = FileSystem.GenerateRandomFileName(info, 18);

					//Create the stream
					using (FileStream stream = new FileStream(currFile, FileMode.CreateNew,
						FileAccess.Write, FileShare.None, 8, FileOptions.WriteThrough))
					{
						//Set the length of the file to be the amount of free space left
						//or the maximum size of one of these dumps.
						mainProgress.Total = mainProgress.Completed +
							method.CalculateEraseDataSize(null, volInfo.AvailableFreeSpace);
						long streamLength = Math.Min(ErasureMethod.FreeSpaceFileUnit,
							volInfo.AvailableFreeSpace);

						//Handle IO exceptions gracefully, because the filesystem
						//may require more space than demanded by us for file allocation.
						while (true)
							try
							{
								stream.SetLength(streamLength);
								break;
							}
							catch (IOException)
							{
								if (streamLength > volInfo.ClusterSize)
									streamLength -= volInfo.ClusterSize;
								else
									throw;
							}

						//Then run the erase task
						method.Erase(stream, long.MaxValue,
							ManagerLibrary.Instance.PrngRegistrar[ManagerLibrary.Settings.ActivePrng],
							delegate(long lastWritten, long totalData, int currentPass)
							{
								mainProgress.Completed += lastWritten;
								OnProgressChanged(this, new ProgressChangedEventArgs(mainProgress,
									new TaskProgressChangedEventArgs(Drive, currentPass, method.Passes)));

								if (Task.Canceled)
									throw new OperationCanceledException(S._("The task was cancelled."));
							}
						);
					}
				}

				//Mark the main bulk of the progress as complete
				mainProgress.MarkComplete();

				//Erase old resident file system table files
				ProgressManager residentProgress = new ProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(residentProgress,
					0.05f, S._("Old resident file system table files")));
				fsManager.EraseOldFileSystemResidentFiles(volInfo, info, method,
					delegate(int currentFile, int totalFiles)
					{
						residentProgress.Completed = currentFile;
						residentProgress.Total = totalFiles;
						OnProgressChanged(this, new ProgressChangedEventArgs(residentProgress,
							new TaskProgressChangedEventArgs(string.Empty, 0, 0)));

						if (Task.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));
					}
				);

				residentProgress.MarkComplete();
			}
			finally
			{
				//Remove the folder holding all our temporary files.
				ProgressManager tempFiles = new ProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(tempFiles,
					0.0f, S._("Removing temporary files...")));
				OnProgressChanged(this, new ProgressChangedEventArgs(tempFiles,
					new TaskProgressChangedEventArgs(string.Empty, 0, 0)));
				info.Delete(true);
				tempFiles.Completed = tempFiles.Total;

				//Reset the low disk space notifications
				Shell.LowDiskSpaceNotificationsEnabled = lowDiskSpaceNotifications;
			}

			//Then clean the old file system entries
			ProgressManager structureProgress = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(structureProgress,
				0.05f, S._("Erasing unused directory structures...")));
			fsManager.EraseDirectoryStructures(volInfo,
				delegate(int currentFile, int totalFiles)
				{
					if (Task.Canceled)
						throw new OperationCanceledException(S._("The task was cancelled."));

					//Compute the progress
					structureProgress.Total = totalFiles;
					structureProgress.Completed = currentFile;

					//Set the event parameters, then broadcast the progress event.
					OnProgressChanged(this, new ProgressChangedEventArgs(structureProgress,
						new TaskProgressChangedEventArgs(string.Empty, 0, 0)));
				}
			);

			structureProgress.MarkComplete();
			Progress = null;
		}
	}

	/// <summary>
	/// Class representing a file to be erased.
	/// </summary>
	[Serializable]
	[Guid("0D741505-E1C4-400d-8470-598AF35E174D")]
	public class FileTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected FileTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FileTarget()
		{
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FileErasureTargetSettings(); }
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<string> result = new List<string>();
			FileInfo fileInfo = new FileInfo(Path);

			if (fileInfo.Exists)
			{
				GetPathADSes(result, out totalSize, Path);
				totalSize += fileInfo.Length;
			}

			result.Add(Path);
			return result;
		}
	}

	/// <summary>
	/// Represents a folder and its files which are to be erased.
	/// </summary>
	[Serializable]
	[Guid("F50B0A44-3AB1-4cab-B81E-1713AC3D28C9")]
	public class FolderTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected FolderTarget(SerializationInfo info, StreamingContext context)
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
		public FolderTarget()
		{
			IncludeMask = string.Empty;
			ExcludeMask = string.Empty;
			DeleteIfEmpty = true;
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FolderErasureTargetSettings(); }
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
	}

	[Serializable]
	[Guid("A1FA7354-0258-4903-88E9-0D31FC5F8D51")]
	public class RecycleBinTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected RecycleBinTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		public RecycleBinTarget()
		{
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return null; }
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<string> result = new List<string>();
			string[] rootDirectory = new string[] {
					"$RECYCLE.BIN",
					"RECYCLER"
				};

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				foreach (string rootDir in rootDirectory)
				{
					DirectoryInfo dir = new DirectoryInfo(
						System.IO.Path.Combine(
							System.IO.Path.Combine(drive.Name, rootDir),
							System.Security.Principal.WindowsIdentity.GetCurrent().
								User.ToString()));
					if (!dir.Exists)
						continue;

					GetRecyclerFiles(dir, result, ref totalSize);
				}
			}

			return result;
		}

		/// <summary>
		/// Retrieves all files within this folder, without exclusions.
		/// </summary>
		/// <param name="info">The DirectoryInfo object representing the folder to traverse.</param>
		/// <param name="paths">The list of files to store path information in.</param>
		/// <param name="totalSize">Receives the total size of the files.</param>
		private void GetRecyclerFiles(DirectoryInfo info, List<string> paths,
			ref long totalSize)
		{
			try
			{
				foreach (FileInfo fileInfo in info.GetFiles())
				{
					if (!fileInfo.Exists || (fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
						continue;

					long adsSize = 0;
					GetPathADSes(paths, out adsSize, fileInfo.FullName);
					totalSize += adsSize;
					totalSize += fileInfo.Length;
					paths.Add(fileInfo.FullName);
				}

				foreach (DirectoryInfo directoryInfo in info.GetDirectories())
					if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) == 0)
						GetRecyclerFiles(directoryInfo, paths, ref totalSize);
			}
			catch (UnauthorizedAccessException e)
			{
				Logger.Log(e.Message, LogLevel.Error);
			}
		}

		/// <summary>
		/// Retrieves the text to display representing this task.
		/// </summary>
		public override string UIText
		{
			get
			{
				return S._("Recycle Bin");
			}
		}

		public override void Execute()
		{
			base.Execute();

			ProgressManager step = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(step,
				0.0f, S._("Emptying recycle bin...")));
			OnProgressChanged(this, new ProgressChangedEventArgs(step,
				new TaskProgressChangedEventArgs(string.Empty, 0, 0)));

			RecycleBin.Empty(EmptyRecycleBinOptions.NoConfirmation |
				EmptyRecycleBinOptions.NoProgressUI | EmptyRecycleBinOptions.NoSound);
		}
	}
}
