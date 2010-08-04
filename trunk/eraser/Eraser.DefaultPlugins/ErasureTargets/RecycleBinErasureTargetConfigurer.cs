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
using System.Text.RegularExpressions;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	class RecycleBinErasureTargetConfigurer : IErasureTargetConfigurer
	{
		#region IConfigurer<ErasureTarget> Members

		public void LoadFrom(ErasureTarget target)
		{
		}

		public bool SaveTo(ErasureTarget target)
		{
			return true;
		}

		#endregion

		#region ICliConfigurer<ErasureTarget> Members

		public string Help()
		{
			return S._("recyclebin          Erases files and folders in the recycle bin");
		}

		public bool ProcessArgument(string argument)
		{
			Regex regex = new Regex("(?<recycleBin>recyclebin)",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
			Match match = regex.Match(argument);

			if (match.Groups["recycleBin"].Success)
			{
				return true;
			}

			return false;
		}

		#endregion
	}
}
