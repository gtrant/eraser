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

using Eraser.Manager;
using Eraser.Util;
using System.IO;

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

		protected override List<string> GetPaths(out long totalSize)
		{
			throw new NotImplementedException();
		}

		public override void Execute()
		{
			//If the path doesn't exist, exit.
			if (!File.Exists(Path))
				return;

			if ((File.GetAttributes(Path) & FileAttributes.Directory) != 0)
			{
				DirectoryInfo info = new DirectoryInfo(Path);
				MoveDirectory(info);
			}
			else
			{
				FileInfo info = new FileInfo(Path);
				MoveFile(info);
			}
		}

		private void MoveDirectory(DirectoryInfo info)
		{
			throw new NotImplementedException();
		}

		private void MoveFile(FileInfo info)
		{
			throw new NotImplementedException();
		}
	}
}
