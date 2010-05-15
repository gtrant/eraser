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
			if (!File.Exists(Path))
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
			if (!File.Exists(Path))
				return;

			//Create the progress manager.
			Progress = new SteppedProgressManager();

			try
			{
				//Depending on whether the path is a file or directory, execute the
				//correct fucntion.
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
			throw new NotImplementedException();
		}

		private void CopyFile(FileInfo info)
		{
			ProgressManager copyProgress = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(copyProgress, 0.5f,
				S._("Copying source file to destination")));

			try
			{
				info.CopyTo(Destination, delegate(long TotalFileSize, long TotalBytesTransferred)
					{
						copyProgress.Completed = TotalBytesTransferred;
						copyProgress.Total = TotalFileSize;
						OnProgressChanged(this, new ProgressChangedEventArgs(Progress, 
							new TaskProgressChangedEventArgs(info.FullName, 1, 1)));

						if (Task.Canceled)
							return IO.CopyProgressFunctionResult.Stop;
						return IO.CopyProgressFunctionResult.Continue;
					});
			}
			catch (OperationCanceledException)
			{
				//The copy was cancelled: Complete the copy part.
				copyProgress.MarkComplete();

				//We need to erase the partially copied copy of the file.
				FileInfo copiedFile = new FileInfo(Destination);
				List<StreamInfo> streams = new List<StreamInfo>(
					copiedFile.GetADSes().Select(x => new StreamInfo(copiedFile.FullName, x));
				streams.Add(new StreamInfo(copiedFile.FullName));
				long totalSize = streams.Sum(x => x.Length);

				foreach (StreamInfo stream in streams)
				{
					ProgressManager destroyProgress = new ProgressManager();
					Progress.Steps.Add(new SteppedProgressManagerStep(destroyProgress,
						0.5f * stream.Length / totalSize,
						S._("Erasing incomplete destination file")));
					EraseStream(stream, destroyProgress);
				}

				//Rethrow the exception.
				throw;
			}
		}
	}
}
