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

namespace Eraser.Plugins
{
	/// <summary>
	/// Reflection-only information retrieved from the assembly.
	/// </summary>
	public struct AssemblyInfo
	{
		/// <summary>
		/// The GUID of the assembly.
		/// </summary>
		public Guid Guid { get; set; }

		/// <summary>
		/// The publisher of the assembly.
		/// </summary>
		public string Author { get; set; }

		/// <summary>
		/// The version of the assembly.
		/// </summary>
		public Version Version { get; set; }

		public override bool Equals(object obj)
		{
			if (!(obj is AssemblyInfo))
				return false;
			return Equals((AssemblyInfo)obj);
		}

		public bool Equals(AssemblyInfo other)
		{
			return Guid == other.Guid;
		}

		public static bool operator ==(AssemblyInfo assembly1, AssemblyInfo assembly2)
		{
			return assembly1.Equals(assembly2);
		}

		public static bool operator !=(AssemblyInfo assembly1, AssemblyInfo assembly2)
		{
			return !assembly1.Equals(assembly2);
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}
	}
}
