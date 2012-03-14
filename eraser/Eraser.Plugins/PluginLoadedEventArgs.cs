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

namespace Eraser.Plugins
{
	/// <summary>
	/// Event argument for the plugin loaded event.
	/// </summary>
	public class PluginLoadedEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance">The plugin instance of the recently loaded plugin.</param>
		public PluginLoadedEventArgs(PluginInfo info)
		{
			Plugin = info;
		}

		/// <summary>
		/// The <see cref="PluginInstance"/> object representing the newly loaded plugin.
		/// </summary>
		public PluginInfo Plugin { get; private set; }
	}
}
