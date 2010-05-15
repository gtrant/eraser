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
using System.Linq;
using System.Text;

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
	public abstract class FileSystemObjectErasureTarget : ErasureTarget
	{
		#region Serialization code
		protected FileSystemObjectErasureTarget(SerializationInfo info, StreamingContext context)
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
		protected FileSystemObjectErasureTarget()
			: base()
		{
		}

		/// <summary>
		/// The Progress of this target's erasure. This property must be set
		/// before <see cref="FileSystemObjectErasureTarget.Execute"/> is called.
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

		/// <summary>
		/// Retrieves the list of files/folders to erase as a list.
		/// </summary>
		/// <param name="totalSize">Returns the total size in bytes of the
		/// items.</param>
		/// <returns>A list containing the paths to all the files to be erased.</returns>
		protected abstract List<StreamInfo> GetPaths(out long totalSize);

		/// <summary>
		/// Gets all files in the provided directory.
		/// </summary>
		/// <param name="info">The directory to look files in.</param>
		/// <returns>A list of files found in the directory matching the IncludeMask
		/// property.</returns>
		protected FileInfo[] GetFiles(DirectoryInfo info)
		{
			List<FileInfo> result = new List<FileInfo>();
			if (info.Exists)
			{
				try
				{
					foreach (DirectoryInfo dir in info.GetDirectories())
						result.AddRange(GetFiles(dir));
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
		/// Adds ADSes of the given file to the list.
		/// </summary>
		/// <param name="file">The file to look for ADSes</param>
		protected StreamInfo[] GetPathADSes(FileInfo file, out long totalSize)
		{
			totalSize = 0;

			try
			{
				//Get the ADS names
				IList<string> adses = file.GetADSes();
				StreamInfo[] result = new StreamInfo[adses.Count];

				//Then prepend the path.
				for (int i = 0; i < adses.Count; ++i)
				{
					StreamInfo info = new StreamInfo(file.FullName + ':' + adses[i]);
					totalSize += info.Length;
					result[i] = info;
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
				foreach (OpenHandle handle in OpenHandle.Close(file.FullName))
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
					return GetPathADSes(file, out totalSize);
				else
					throw;
			}
			catch (UnauthorizedAccessException e)
			{
				//The system cannot read the file, assume no ADSes for lack of
				//more information.
				Logger.Log(e.Message, LogLevel.Error);
			}

			return new StreamInfo[0];
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

		/// <remarks>The <see cref="Progress"/> property must be defined prior
		/// to the execution of this function.</remarks>
		public override void Execute()
		{
			//Retrieve the list of files to erase.
			long dataTotal = 0;
			List<StreamInfo> paths = GetPaths(out dataTotal);

			//Get the erasure method if the user specified he wants the default.
			ErasureMethod method = EffectiveMethod;
			dataTotal = method.CalculateEraseDataSize(paths, dataTotal);

			//Set the event's current target status.
			if (Progress == null)
				throw new InvalidOperationException("The Progress property must not be null.");
			Task.Progress.Steps.Add(new SteppedProgressManagerStep(Progress,
				EraseWeight / Task.Targets.Count));

			//Iterate over every path, and erase the path.
			for (int i = 0; i < paths.Count; ++i)
			{
				ProgressManager step = new ProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(step,
					paths[i].Length / (float)dataTotal, S._("Erasing files...")));
				EraseStream(paths[i], step);
			}
		}

		/// <summary>
		/// Erases the provided stream, and updates progress using tyhe provided
		/// progress manager.
		/// </summary>
		/// <param name="info">The information regarding the stream that needs erasure.</param>
		/// <param name="progress">The progress manager for the erasure of the current
		/// stream.</param>
		protected void EraseStream(StreamInfo info, ProgressManager progress)
		{
			//Check that the file exists - we do not want to bother erasing nonexistant files
			if (!info.Exists)
			{
				Logger.Log(S._("The file {0} was not erased as the file does not exist.",
					info.FileName), LogLevel.Notice);
				return;
			}

			//Get the filesystem provider to handle the secure file erasures
			FileSystem fsManager = ManagerLibrary.Instance.FileSystemRegistrar[
				VolumeInfo.FromMountPoint(info.DirectoryName)];

			bool isReadOnly = false;

			try
			{
				//Update the task progress
				ErasureMethod method = EffectiveMethod;
				OnProgressChanged(this, new ProgressChangedEventArgs(progress,
					new TaskProgressChangedEventArgs(info.FullName, 0, method.Passes)));

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
					return;
				}

				fsManager.EraseFileSystemObject(info, method,
					delegate(long lastWritten, long totalData, int currentPass)
					{
						if (Task.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));

						progress.Total = totalData;
						progress.Completed += lastWritten;
						OnProgressChanged(this, new ProgressChangedEventArgs(progress,
							new TaskProgressChangedEventArgs(info.FullName, currentPass, method.Passes)));
					});

				//Remove the file.
				FileInfo fileInfo = info.File;
				if (fileInfo != null)
					fsManager.DeleteFile(fileInfo);
				progress.MarkComplete();
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
							info.FileName, S._("(locked by {0})",
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

		/// <summary>
		/// The weight attached with the erase process, so that tasks can depend on
		/// the erase task without it using up all the weights allocated.
		/// </summary>
		protected virtual float EraseWeight
		{
			get
			{
				return 1.0f;
			}
		}
	}
}
