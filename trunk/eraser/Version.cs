/* 
 * $Id: Version.cs.in 2955 2014-10-11 13:32:19Z gtrant $
 * Copyright 2008-2013 The Eraser Project
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
using System.Reflection;
using System.Diagnostics;

[assembly: AssemblyFileVersion("6.2.0.2956")]
[assembly: AssemblyVersion("6.2.0.2956")]

namespace Eraser {
	internal static class BuildInfo
	{
		public static readonly DateTime BuildDate = DateTime.Parse("2014/10/11 14:41:38",
			System.Globalization.CultureInfo.InvariantCulture);
		public const bool CustomBuild = true;
		public static Version AssemblyFileVersion
		{
			get
			{
				FileVersionInfo version = FileVersionInfo.GetVersionInfo(
					Assembly.GetExecutingAssembly().Location);

				return new Version(version.FileMajorPart, version.FileMinorPart,
					version.FileBuildPart, version.FilePrivatePart);
			}
		}
	}
}
