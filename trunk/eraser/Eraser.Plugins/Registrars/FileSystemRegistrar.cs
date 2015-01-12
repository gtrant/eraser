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

using Eraser.Util;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.Plugins.Registrars
{
	public class FileSystemRegistrar : Registrar<IFileSystem>
	{
		/// <summary>
		/// Gets the FileSystem object that implements the FileSystem interface
		/// for the given file system.
		/// </summary>
		/// <param name="volume">The volume to get the FileSystem provider for.</param>
		/// <returns>The FileSystem object providing interfaces to handle the
		/// given volume.</returns>
		/// <exception cref="NotSupportedException">Thrown when an unimplemented
		/// file system is requested.</exception>
		public IFileSystem this[VolumeInfo volume]
		{
			get
			{
				if (volume == null)
					throw new ArgumentNullException("volume");

				foreach (IFileSystem filesystem in this)
					if (filesystem.Name.ToUpperInvariant() ==
						volume.VolumeFormat.ToUpperInvariant())
					{
						return filesystem;
					}

				throw new NotSupportedException(S._("The file system on the drive {0} is not " +
					"supported.", volume));
			}
		}
	}
}
