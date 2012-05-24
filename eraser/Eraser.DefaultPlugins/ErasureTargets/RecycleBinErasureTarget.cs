/* 
 * $Id$
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

using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.IO;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using Microsoft.Win32;

namespace Eraser.DefaultPlugins
{
	[Serializable]
	[Guid("A1FA7354-0258-4903-88E9-0D31FC5F8D51")]
	public class RecycleBinErasureTarget : FileSystemObjectErasureTarget
	{
		#region Serialization code
		protected RecycleBinErasureTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		public RecycleBinErasureTarget()
		{
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return S._("Recycle Bin"); }
		}

		/// <summary>
		/// Retrieves the text to display representing this task.
		/// </summary>
		public override string ToString()
		{
			return S._("Recycle Bin");
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new RecycleBinErasureTargetConfigurer(); }
		}

		protected override List<StreamInfo> GetPaths()
		{
			List<DirectoryInfo> directories = new List<DirectoryInfo>();
			string[] rootDirectory = new string[] {
					"$RECYCLE.BIN",
					"RECYCLER",
					"RECYCLED"
				};
			string userSid = System.Security.Principal.WindowsIdentity.GetCurrent().
				User.ToString();

			//First try to get the recycle bin on each of of the physical volumes we have
			foreach (VolumeInfo volume in VolumeInfo.Volumes)
			{
				if (!volume.IsMounted)
					continue;

				foreach (string rootDir in rootDirectory)
				{
					//First get the global recycle bin for the current drive
					string recycleBinPath = System.IO.Path.Combine(
						volume.MountPoints[0].FullName, rootDir);
					if (!Directory.Exists(recycleBinPath))
						continue;

					//Try to see if we can get the user's own recycle bin
					if (Directory.Exists(System.IO.Path.Combine(recycleBinPath, userSid)))
						recycleBinPath = System.IO.Path.Combine(recycleBinPath, userSid);

					directories.Add(new DirectoryInfo(recycleBinPath));
				}
			}

			//Then try the Shell's known folders for Vista and later
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
				"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\BitBucket\\KnownFolder"))
			{
				if (key != null)
				{
					string[] knownFolders = key.GetSubKeyNames();
					foreach (string stringGuid in knownFolders)
					{
						Guid guid = new Guid(stringGuid);
						DirectoryInfo info = Shell.KnownFolderIDs.GetPath(guid);

						if (info == null)
							continue;

						foreach (string rootDir in rootDirectory)
						{
							//Known folders belong to the current user, so they do not store
							//objects in a folder with the user's SID
							string recycleBinPath = System.IO.Path.Combine(info.FullName, rootDir);
							if (!Directory.Exists(recycleBinPath))
								continue;

							directories.Add(new DirectoryInfo(recycleBinPath));
						}
					}
				}
			}

			//Then get all the files in each of the directories
			List<StreamInfo> result = new List<StreamInfo>();
			foreach (DirectoryInfo directory in directories)
				foreach (FileInfo file in GetFiles(directory))
				{
					try
					{
						//Add the ADSes
						result.AddRange(GetPathADSes(file));

						//Then the file itself
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
							"directory was deleted before it could be erased", file.FullName),
							LogLevel.Information);
					}
					catch (SharingViolationException)
					{
						Logger.Log(S._("Could not list the Alternate Data Streams for file {0} " +
							"because the file is being used by another process. The file will not " +
							"be erased.", file.FullName), LogLevel.Error);
					}
				}

			return result;
		}

		public override void Execute()
		{
			Progress = new SteppedProgressManager();

			try
			{
				base.Execute();

				//Empty the contents of the Recycle Bin
				EmptyRecycleBin();
			}
			finally
			{
				Progress = null;
			}
		}

		private void EmptyRecycleBin()
		{
			ProgressManager progress = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(progress,
				0.0f, S._("Emptying recycle bin...")));

			RecycleBin.Empty(EmptyRecycleBinOptions.NoConfirmation |
				EmptyRecycleBinOptions.NoProgressUI | EmptyRecycleBinOptions.NoSound);
		}
	}
}
