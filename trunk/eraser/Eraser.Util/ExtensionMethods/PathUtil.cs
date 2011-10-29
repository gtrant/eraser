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
using System.IO;

namespace Eraser.Util.ExtensionMethods
{
	/// <summary>
	/// Additional Path utility methods.
	/// </summary>
	public class PathUtil
	{
		/// <summary>
		/// Makes the first path relative to the second.
		/// </summary>
		/// <remarks>Modified from:
		/// http://mrpmorris.blogspot.com/2007/05/convert-absolute-path-to-relative-path.html</remarks>
		/// <param name="absolutePath">The path to use as the root of the relative path.</param>
		/// <param name="relativeTo">The path to make relative.</param>
		/// <returns>The relative path to the provided path.</returns>
		public static string MakeRelativeTo(FileSystemInfo absolutePath, string relativeTo)
		{
			string[] absoluteDirectories = absolutePath.FullName.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			string[] relativeDirectories = relativeTo.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

			//Get the shortest of the two paths
			int length = absoluteDirectories.Length < relativeDirectories.Length ?
				absoluteDirectories.Length : relativeDirectories.Length;

			//Use to determine where in the loop we exited
			int lastCommonRoot = -1;
			int index;

			//Find common root
			for (index = 0; index < length; index++)
				if (absoluteDirectories[index] == relativeDirectories[index])
					lastCommonRoot = index;
				else
					break;

			//If we didn't find a common prefix then throw
			if (lastCommonRoot == -1)
				throw new ArgumentException("Paths do not have a common base");

			//Build up the relative path
			StringBuilder relativePath = new StringBuilder();

			//Add on the ..
			for (index = lastCommonRoot + 1; index < absoluteDirectories.Length; index++)
				if (absoluteDirectories[index].Length > 0)
					relativePath.Append("..\\");

			//Add on the folders
			for (index = lastCommonRoot + 1; index < relativeDirectories.Length - 1; index++)
				relativePath.Append(relativeDirectories[index] + "\\");
			if (lastCommonRoot < relativeDirectories.Length - 1)
				relativePath.Append(relativeDirectories[relativeDirectories.Length - 1]);

			return relativePath.ToString();
		}

		/// <summary>
		/// Verifies if the path given is rooted at the given absolute path.
		/// </summary>
		/// <param name="absolutePath">The root path.</param>
		/// <param name="path">The path to verify.</param>
		/// <returns>True if the path provided is a subfolder/sub-file of the provided root path.</returns>
		public static bool IsRootedAt(FileSystemInfo absolutePath, string path)
		{
			//Convert the path in question to an absolute path
			if (!System.IO.Path.IsPathRooted(path))
				path = System.IO.Path.GetFullPath(path);

			//Split the directory path to its component folders
			string[] absoluteDirectories = absolutePath.FullName.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			string[] relativeDirectories = path.Split(
				System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

			//Compare element by element; if the absolute path compares till the end, the
			//provided path is a subdirectory
			for (int i = 0; i < absoluteDirectories.Length; ++i)
				if (absoluteDirectories[i] != relativeDirectories[i])
					return false;

			return true;
		}
	}
}
