/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Class representing a file to be erased.
	/// </summary>
	[Serializable]
	[Guid("0D741505-E1C4-400d-8470-598AF35E174D")]
	public class FileErasureTarget : FileSystemObjectErasureTarget
	{
		#region Serialization code
		protected FileErasureTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FileErasureTarget()
		{
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return S._("File"); }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FileErasureTargetConfigurer(); }
		}

		protected override List<StreamInfo> GetPaths()
		{
			List<StreamInfo> result = new List<StreamInfo>();
			FileInfo fileInfo = new FileInfo(Path);

			try
			{
				result.AddRange(GetPathADSes(fileInfo));
				result.Add(new StreamInfo(Path));
			}
			catch (SharingViolationException)
			{
				Logger.Log(S._("Could not list the Alternate Data Streams for file {0} " +
					"because the file is being used by another process. The file will not " +
					"be erased.", fileInfo.FullName), LogLevel.Error);
			}
			catch (FileNotFoundException)
			{
			}
			catch (DirectoryNotFoundException)
			{
			}

			return result;
		}

		public override void Execute()
		{
			Progress = new SteppedProgressManager();

			try
			{
				base.Execute();
			}
			finally
			{
				Progress = null;
			}
		}
	}
}
