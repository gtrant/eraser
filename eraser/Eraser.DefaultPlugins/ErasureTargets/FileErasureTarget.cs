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
using System.IO;

using Eraser.Manager;
using Eraser.Util;

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

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FileErasureTargetConfigurer(); }
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
