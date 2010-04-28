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
using System.Text;
using System.Windows.Forms;

using Eraser.Manager.Plugin;

namespace Eraser.BlackBox
{
	public sealed class Plugin : IPlugin
	{
		public void Initialize(Host host)
		{
			//Initialise our crash handler
			BlackBox blackBox = BlackBox.Get();

			//Hook the Application's idle loop to display the form
			Application.Idle += OnGUIIdle;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public string Name
		{
			get { throw new NotImplementedException(); }
		}

		public string Author
		{
			get { throw new NotImplementedException(); }
		}

		public bool Configurable
		{
			get { throw new NotImplementedException(); }
		}

		public void DisplaySettings(Control parent)
		{
			throw new NotImplementedException();
		}

		public static void OnGUIIdle(object sender, EventArgs e)
		{
			Application.Idle -= OnGUIIdle;
			BlackBox blackBox = BlackBox.Get();

			bool allSubmitted = true;
			foreach (BlackBoxReport report in blackBox.GetDumps())
				if (!report.Submitted)
				{
					allSubmitted = false;
					break;
				}

			if (allSubmitted)
				return;

			BlackBoxMainForm form = new BlackBoxMainForm();
			form.Show();
		}
	}
}
