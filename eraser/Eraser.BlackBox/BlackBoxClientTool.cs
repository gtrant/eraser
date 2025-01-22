/* 
 * $Id: BlackBoxClientTool.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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

using Eraser.Plugins.ExtensionPoints;
using Eraser.Util;

namespace Eraser.BlackBox
{
	class BlackBoxClientTool : IClientTool
	{
		#region IClientTool Members

		public void RegisterTool(ContextMenuStrip menu)
		{
			ToolStripMenuItem item = new ToolStripMenuItem(S._("Manage BlackBox Reports"));
			item.Click += OnToolClicked;
			menu.Items.Add(item);
		}

		#endregion

		#region IRegisterable Members

		public Guid Guid
		{
			get { return new Guid("74C96E7D-570D-420A-A1EE-0CFCB0C46CCF"); }
		}

		#endregion

		private void OnToolClicked(object sender, EventArgs e)
		{
			BlackBoxMainForm form = BlackBoxMainForm.Get();

			Form owner = null;
			if (Application.OpenForms.Count > 0)
				owner = Application.OpenForms[0];
			form.ShowDialog(owner);
		}
	}
}
