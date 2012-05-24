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

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Eraser.Util;
using System.Diagnostics;
using System.Globalization;

namespace Eraser.BlackBox
{
	public partial class BlackBoxMainForm : Form
	{
		public BlackBoxMainForm()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);

			ReportsLv.BeginUpdate();
			foreach (BlackBoxReport report in BlackBox.GetDumps())
			{
				ListViewItem item = ReportsLv.Items.Add(report.Timestamp.ToString(
					"g", CultureInfo.CurrentCulture));
				if (report.StackTrace.Count != 0)
					item.SubItems.Add(report.StackTrace[0].ExceptionType);
				else
					item.SubItems.Add(string.Empty);
				item.SubItems.Add(report.Submitted ?
					S._("Submitted") : S._("Not submitted"));
				item.Tag = report;
				item.Checked = !report.Submitted;
			}
			ReportsLv.EndUpdate();
		}

		private void ReportsLv_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			BlackBoxReport report = ((BlackBoxReport)ReportsLv.Items[e.Index].Tag);
			if (e.NewValue == CheckState.Checked && report.Submitted)
				e.NewValue = CheckState.Unchecked;
		}

		private void ReportsLv_ItemActivate(object sender, EventArgs e)
		{
			if (ReportsLv.SelectedItems.Count == 0)
				return;

			Process.Start((ReportsLv.SelectedItems[0].Tag as BlackBoxReport).Path);
		}

		private void ReportsMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			if (ReportsLv.SelectedItems.Count == 0)
			{
				e.Cancel = true;
			}
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			List<ListViewItem> selection = new List<ListViewItem>(
				ReportsLv.SelectedItems.Cast<ListViewItem>());
			foreach (ListViewItem item in selection)
			{
				try
				{
					((BlackBoxReport)item.Tag).Delete();
					item.Remove();
				}
				catch (UnauthorizedAccessException ex)
				{
					MessageBox.Show(this, S._("Could not delete report {0} because of " +
						"the following error: {1}", item.Text, ex.Message), S._("BlackBox"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void DataCollectionPolicyLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("http://eraser.heidi.ie/trac/wiki/DataCollectionPolicy");
		}

		private void SubmitBtn_Click(object sender, EventArgs e)
		{
			List<BlackBoxReport> reports = new List<BlackBoxReport>();
			foreach (ListViewItem item in ReportsLv.Items)
				if (item.Checked)
					reports.Add((BlackBoxReport)item.Tag);

			if (reports.Count != 0)
			{
				BlackBoxUploadForm form = new BlackBoxUploadForm(reports);
				form.Show();
			}

			Close();
		}

		private void PostponeBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// The global BlackBox instance.
		/// </summary>
		private BlackBox BlackBox = BlackBox.Get();
	}
}
