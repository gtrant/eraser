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

using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.IO;

using Eraser.Manager;
using Eraser.Util;

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

		public override IErasureTargetConfigurer Configurer
		{
			get { return new RecycleBinErasureTargetConfigurer(); }
		}

		protected override List<StreamInfo> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<StreamInfo> result = new List<StreamInfo>();
			string[] rootDirectory = new string[] {
					"$RECYCLE.BIN",
					"RECYCLER"
				};
			string userSid = System.Security.Principal.WindowsIdentity.GetCurrent().
				User.ToString();

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				foreach (string rootDir in rootDirectory)
				{
					DirectoryInfo dir = new DirectoryInfo(
						System.IO.Path.Combine(
							System.IO.Path.Combine(drive.Name, rootDir),
							userSid));
					if (!dir.Exists)
						continue;

					foreach (FileInfo file in GetFiles(dir))
					{
						if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
							continue;

						//Add the ADSes
						long adsSize = 0;
						result.AddRange(GetPathADSes(file, out adsSize));
						totalSize += adsSize;

						//Then the file itself
						totalSize += file.Length;
						result.Add(new StreamInfo(file.FullName));
					}
				}
			}

			return result;
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
			Progress = new SteppedProgressManager();
			try
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
			finally
			{
				Progress = null;
			}
		}
	}
}
