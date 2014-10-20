/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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
	/// <summary>
	/// Notifier plugin that will tell the user when there are pending reports for
	/// upload.
	/// </summary>
	class BlackBoxNotifier : INotifier
	{
		#region INotifier Members

		public INotificationSink Sink
		{
			set
			{
				sink = value;
				ShowNotification();
			}
		}
		
		private INotificationSink sink;

		public void Clicked(object sender, EventArgs e)
		{
			OnClick(sender, e);
		}

		public void Closed(object sender, EventArgs e)
		{
		}

		public void Shown(object sender, EventArgs e)
		{
		}

		#endregion

		#region IRegisterable Members

		public Guid Guid
		{
			get { return new Guid("74C96E7D-570D-420A-A1EE-0CFCB0C46CCF"); }
		}

		#endregion

		/// <summary>
		/// Shows the notification to the user.
		/// </summary>
		private void ShowNotification()
		{
			if (sink == null || HasShown)
				return;

			HasShown = true;
			sink.ShowNotification(this, 0, System.Windows.Forms.ToolTipIcon.Info,
				S._("Eraser BlackBox: Crash Reports Collected"),
				S._("There are crash reports which have yet to be submitted. Click on this " +
					"balloon to see the list of reports."));
		}

		/// <summary>
		/// Called when the user clicks on the balloon.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnClick(object sender, EventArgs e)
		{
			BlackBoxMainForm form = BlackBoxMainForm.Get();
			Form owner = null;
			if (Application.OpenForms.Count > 0)
				owner = Application.OpenForms[0];

			if (owner == null)
			{
				form.ShowInTaskbar = true;
				form.Show();
			}
			else
			{
				form.ShowDialog(owner);
			}
		}

		/// <summary>
		/// Instance member to only show the notification once.
		/// </summary>
		private bool HasShown;
	}
}
