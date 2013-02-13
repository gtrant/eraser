/* 
 * $Id$
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eraser.Plugins
{
	/// <summary>
	/// Provides the event arguments for the PluginLoad event, raised when the Plugins
	/// library needs to decide whether to load a given plugin.
	/// </summary>
	public class PluginLoadEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="info">The plugin information to be passed to the approving
		/// delegate.</param>
		internal PluginLoadEventArgs(PluginInfo info)
		{
			Plugin = info;
			Load = true;
		}

		/// <summary>
		/// Gets the plugin associated with this event.
		/// </summary>
		public PluginInfo Plugin { get; private set; }

		/// <summary>
		/// Gets or Sets whether the current plugin should be loaded.
		/// </summary>
		public bool Load { get; set; }
	}
}
