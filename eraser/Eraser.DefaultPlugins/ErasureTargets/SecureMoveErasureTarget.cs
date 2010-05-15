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

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.IO;

using Eraser.Manager;
using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Class representing a path that needs to be moved.
	/// </summary>
	[Serializable]
	[Guid("18FB3523-4012-4718-8B9A-BADAA9084214")]
	public class SecureMoveErasureTarget : FileSystemObjectErasureTarget
	{
		#region Serialization code
		protected SecureMoveErasureTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Destination = (string)info.GetValue("Destination", typeof(string));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Destination", Destination);
		}
		#endregion

		public SecureMoveErasureTarget()
		{
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return S._("Secure move"); }
		}

		public override string UIText
		{
			get { return S._("Securely move {0}", Path); }
		}

		/// <summary>
		/// The destination of the move.
		/// </summary>
		public string Destination
		{
			get;
			set;
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new SecureMoveErasureTargetConfigurer(); }
		}

		protected override List<StreamInfo> GetPaths()
		{
			List<StreamInfo> result = new List<StreamInfo>();
			if (!(File.Exists(Path) || Directory.Exists(Path)))
				return result;

			FileInfo[] files = null;
			if ((File.GetAttributes(Path) & FileAttributes.Directory) == 0)
				files = new FileInfo[] { new FileInfo(Path) };
			else
				files = GetFiles(new DirectoryInfo(Path));

			foreach (FileInfo info in files)
			{
				//Add the alternate data streams
				result.AddRange(GetPathADSes(info));

				//And the file itself
				result.Add(new StreamInfo(info.FullName));
			}

			return result;
		}

		public override void Execute()
		{
			//If the path doesn't exist, exit.
			if (!File.Exists(Path) && !Directory.Exists(Path))
				return;

			//Create the progress manager.
			Progress = new SteppedProgressManager();
			Task.Progress.Steps.Add(new SteppedProgressManagerStep(Progress,
				1.0f / Task.Targets.Count));

			try
			{
				//Depending on whether the path is a file or directory, execute the
				//correct function.
				if ((File.GetAttributes(Path) & FileAttributes.Directory) != 0)
				{
					DirectoryInfo info = new DirectoryInfo(Path);
					CopyDirectory(info);
				}
				else
				{
					FileInfo info = new FileInfo(Path);
					CopyFile(info);
				}
			}
			finally
			{
				Progress = null;
			}
		}

		private void CopyDirectory(DirectoryInfo info)
		{
			//We need to get the files from the list of streams
			List<StreamInfo> streams = GetPaths();
			List<FileInfo> files = new List<FileInfo>(
				streams.Distinct(new StreamInfoFileEqualityComparer()).
				Select(x => x.File));
			long totalSize = streams.Sum(x => x.Length);

			foreach (FileInfo file in files)
			{
				//Compute the total size of the file on the disk (including ADSes)
				List<StreamInfo> fileStreams = new List<StreamInfo>(
					file.GetADSes().Select(x => new StreamInfo(file.FullName, x)));
				fileStreams.Add(new StreamInfo(file.FullName));
				long fileSize = fileStreams.Sum(x => x.Length);

				SteppedProgressManager fileProgress = new SteppedProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(fileProgress,
					fileSize / (float)totalSize, S._("Securely moving files and folders...")));

				//Add the copying step to the file progress.
				ProgressManager copyProgress = new ProgressManager();
				int totalPasses = 1 + EffectiveMethod.Passes;
				fileProgress.Steps.Add(new SteppedProgressManagerStep(copyProgress,
					1f / totalPasses));

				try
				{
					//Compute the path to the new directory.
					DirectoryInfo sourceDirectory = file.Directory;
					DirectoryInfo destDirectory = new DirectoryInfo(
						SourceToDestinationPath(file.DirectoryName));

					//Make sure all necessary folders exist before the copy.
					if (!destDirectory.Exists)
						destDirectory.Create();
					
					//Then copy the file.
					file.CopyTo(System.IO.Path.Combine(destDirectory.FullName, file.Name),
						delegate(long TotalFileSize, long TotalBytesTransferred)
						{
							return CopyProgress(copyProgress, file, TotalFileSize,
								TotalBytesTransferred);
						});
				}
				catch (OperationCanceledException)
				{
					//The copy was cancelled: Complete the copy part.
					copyProgress.MarkComplete();

					//We need to erase the partially copied copy of the file.
					SteppedProgressManager destroyProgress = new SteppedProgressManager();
					Progress.Steps.Add(new SteppedProgressManagerStep(destroyProgress, 0.5f,
						S._("Erasing incomplete destination file")));
					EraseFile(file, destroyProgress);

					//Rethrow the exception.
					throw;
				}

				//We copied the file over; erase the source file
				SteppedProgressManager eraseProgress = new SteppedProgressManager();
				fileProgress.Steps.Add(new SteppedProgressManagerStep(eraseProgress,
					(totalPasses - 1) / (float)totalPasses,
					S._("Erasing source files...")));
				EraseFile(file, eraseProgress);
			}

			//Then copy the timestamps from the source folders and delete the source.
			ProgressManager folderDeleteProgress = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(folderDeleteProgress, 0.0f,
				S._("Removing folders...")));

			Action<DirectoryInfo> CopyTimesAndDelete = null;
			CopyTimesAndDelete = delegate(DirectoryInfo subDirectory)
			{
				foreach (DirectoryInfo child in subDirectory.GetDirectories())
					CopyTimesAndDelete(child);

				//Update progress.
				OnProgressChanged(this, new ProgressChangedEventArgs(folderDeleteProgress,
					new TaskProgressChangedEventArgs(subDirectory.FullName, 1, 1)));

				//Get the directory which we copied to and copy the file times to the
				//destination directory
				DirectoryInfo destDirectory = new DirectoryInfo(
					SourceToDestinationPath(subDirectory.FullName));
				if (!destDirectory.Exists)
					destDirectory.Create();
				destDirectory.CopyTimes(subDirectory);

				//Then delete the source directory.
				FileSystem fsManager = ManagerLibrary.Instance.FileSystemRegistrar[
					VolumeInfo.FromMountPoint(Path)];
				fsManager.DeleteFolder(subDirectory);
			};
			CopyTimesAndDelete(info);
		}

		/// <summary>
		/// Converts the source path to the destination path.
		/// </summary>
		/// <param name="sourcePath">The source path to convert.</param>
		/// <returns>The destination path that the file would have been moved to.</returns>
		private string SourceToDestinationPath(string sourcePath)
		{
			DirectoryInfo source = new DirectoryInfo(Path);
			string baseDir = System.IO.Path.Combine(Destination, source.Name);
			return System.IO.Path.Combine(baseDir,
				Shell.MakeRelativeTo(source, sourcePath));
		}

		private void CopyFile(FileInfo info)
		{
			ProgressManager copyProgress = new ProgressManager();
			int totalPasses = 1 + EffectiveMethod.Passes;
			Progress.Steps.Add(new SteppedProgressManagerStep(copyProgress, 1.0f / totalPasses,
				S._("Copying source files to destination...")));

			try
			{
				//Make sure all necessary folders exist before the copy.
				Directory.CreateDirectory(Destination);

				//Then copy the file.
				string path = System.IO.Path.Combine(Destination, info.Name);
				info.CopyTo(path, delegate(long TotalFileSize, long TotalBytesTransferred)
					{
						return CopyProgress(copyProgress, info, TotalFileSize,
							TotalBytesTransferred);
					});
			}
			catch (OperationCanceledException)
			{
				//The copy was cancelled: Complete the copy part.
				copyProgress.MarkComplete();

				//We need to erase the partially copied copy of the file.
				SteppedProgressManager destroyProgress = new SteppedProgressManager();
				Progress.Steps.Add(new SteppedProgressManagerStep(destroyProgress, 0.5f,
					S._("Erasing incomplete destination file")));
				EraseFile(new FileInfo(Destination), destroyProgress);

				//Rethrow the exception.
				throw;
			}

			//Mark the copy as complete.
			copyProgress.MarkComplete();

			//Erase the source copy.
			SteppedProgressManager eraseProgress = new SteppedProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(eraseProgress,
				(totalPasses - 1) / totalPasses,
				S._("Erasing source files...")));
			EraseFile(info, eraseProgress);
		}

		/// <summary>
		/// Wrapper around <see cref="FileSystemObjectErasureTarget.EraseStream"/>
		/// that will erase every stream in the provided file.
		/// </summary>
		/// <param name="info">The file to erase.</param>
		/// <param name="eraseProgress">The progress manager for the entire
		/// erasure of the file.</param>
		private void EraseFile(FileInfo info, SteppedProgressManager eraseProgress)
		{
			List<StreamInfo> streams = new List<StreamInfo>(
				info.GetADSes().Select(x => new StreamInfo(info.FullName, x)));
			streams.Add(new StreamInfo(info.FullName));
			long fileSize = streams.Sum(x => x.Length);

			foreach (StreamInfo stream in streams)
			{
				ProgressManager progress = new ProgressManager();
				eraseProgress.Steps.Add(new SteppedProgressManagerStep(progress,
					stream.Length / (float)fileSize,
					S._("Erasing incomplete destination file")));
				EraseStream(stream, progress);
			}
		}

		private IO.CopyProgressFunctionResult CopyProgress(ProgressManager progress,
			FileInfo file, long TotalFileSize, long TotalBytesTransferred)	
		{
			progress.Completed = TotalBytesTransferred;
			progress.Total = TotalFileSize;
			OnProgressChanged(this, new ProgressChangedEventArgs(Progress,
				new TaskProgressChangedEventArgs(file.FullName, 1, 1)));

			if (Task.Canceled)
				return IO.CopyProgressFunctionResult.Stop;
			return IO.CopyProgressFunctionResult.Continue;
		}

		private class StreamInfoFileEqualityComparer : IEqualityComparer<StreamInfo>
		{
			#region IEqualityComparer<StreamInfo> Members

			public bool Equals(StreamInfo x, StreamInfo y)
			{
				return x.FileName == y.FileName;
			}

			public int GetHashCode(StreamInfo obj)
			{
				return obj.FileName.GetHashCode();
			}

			#endregion
		}
	}
}
