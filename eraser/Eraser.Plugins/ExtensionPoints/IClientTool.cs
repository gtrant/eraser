/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
using System.Windows.Forms;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// Provides an abstract interface to allow plugins to register their menu
	/// items within the Eraser Tools menu.
	/// </summary>
	public interface IClientTool : IRegisterable
	{
		/// <summary>
		/// Called when the client requests for tools to be displayed. Plugins
		/// can insert their menu item in the provided <paramref name="menu"/>.
		/// </summary>
		/// <param name="menu">The menu for tools to insert context menu items
		/// into.</param>
		void RegisterTool(ContextMenuStrip menu);
	}
}
