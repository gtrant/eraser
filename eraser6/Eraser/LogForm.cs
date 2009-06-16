/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Eraser.Manager;
using System.Globalization;
using Eraser.Util;
using System.IO;

namespace Eraser
{
	public partial class LogForm : Form
	{
		public LogForm(Task task)
		{
			InitializeComponent();
			UXThemeApi.UpdateControlTheme(this);
			this.task = task;

			//Update the title
			Text = string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Text, task.UIText);

			//Set the filter settings
			filterFilterType.SelectedIndex = 0;
			filterSeverity.SelectedIndex = 0;

			//Add all the existing log messages
			RefreshMessages();
			EnableButtons();

			//Register our event handler to get live log messages
			task.Log.Logged += task_Logged;
		}

		private void LogForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			task.Log.Logged -= task_Logged;
		}

		private void task_Logged(object sender, LogEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new EventHandler<LogEventArgs>(task_Logged), sender, e);
				return;
			}

			//Check whether the current entry meets the criteria for display.
			if (!MeetsCriteria(e.LogEntry))
				return;

			ListViewItem item = log.Items.Add(e.LogEntry.Timestamp.ToString("F", CultureInfo.CurrentCulture));
			item.SubItems.Add(e.LogEntry.Level.ToString());
			item.SubItems.Add(e.LogEntry.Message);
			if (log.Groups.Count != 0)
				item.Group = log.Groups[log.Groups.Count - 1];

			switch (e.LogEntry.Level)
			{
				case LogLevel.Fatal:
					item.ForeColor = Color.Red;
					break;
				case LogLevel.Error:
					item.ForeColor = Color.OrangeRed;
					break;
				case LogLevel.Warning:
					item.ForeColor = Color.Orange;
					break;
			}

			//Enable the clear and copy log buttons only if we have entries to copy.
			EnableButtons();
		}

		private void filter_Changed(object sender, EventArgs e)
		{
			RefreshMessages();
		}

		private void clear_Click(object sender, EventArgs e)
		{
			task.Log.Clear();
			log.Items.Clear();
			EnableButtons();
		}

		private void copy_Click(object sender, EventArgs e)
		{
			StringBuilder csvText = new StringBuilder();
			StringBuilder rawText = new StringBuilder();
			LogSessionDictionary logEntries = task.Log.Entries;

			foreach (DateTime sessionTime in logEntries.Keys)
			{
				csvText.AppendLine(S._("Session: {0:F}", sessionTime));
				rawText.AppendLine(S._("Session: {0:F}", sessionTime));
				foreach (LogEntry entry in logEntries[sessionTime])
				{
					//Only copy entries which meet the display criteria.
					if (!MeetsCriteria(entry))
						continue;

					string timeStamp = entry.Timestamp.ToString("F", CultureInfo.CurrentCulture);
					string message = entry.Message;
					csvText.AppendFormat("\"{0}\",\"{1}\",\"{2}\"\n",
						timeStamp.Replace("\"", "\"\""), entry.Level.ToString(),
						message.Replace("\"", "\"\""));
					rawText.AppendFormat("{0}	{1}	{2}", timeStamp, entry.Level.ToString(),
						message);
				}
			}

			if (csvText.Length > 0 || rawText.Length > 0)
			{
				//Set the simple text data for data-unaware applications like Word
				DataObject tableText = new DataObject();
				tableText.SetText(rawText.ToString());

				//Then a UTF-8 stream CSV for Excel
				byte[] bytes = Encoding.UTF8.GetBytes(csvText.ToString());
				MemoryStream tableStream = new MemoryStream(bytes);
				tableText.SetData(DataFormats.CommaSeparatedValue, tableStream);

				//Set the clipboard
				Clipboard.SetDataObject(tableText, true);
			}
		}

		private void close_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Checks whether the given log entry meets the current display criteria.
		/// </summary>
		/// <param name="entry">The entry to check.</param>
		/// <returns>True if the entry meets the display criteria.</returns>
		private bool MeetsCriteria(LogEntry entry)
		{
			//Check if the current message meets criteria
			switch (filterFilterType.SelectedIndex)
			{
				case 0: //and above
					if (entry.Level < (LogLevel)filterSeverity.SelectedIndex)
						return false;
					break;

				case 1: //equal to
					if (entry.Level != (LogLevel)filterSeverity.SelectedIndex)
						return false;
					break;

				case 2: //and below
					if (entry.Level > (LogLevel)filterSeverity.SelectedIndex)
						return false;
					break;
			}

			return true;
		}

		/// <summary>
		/// Updates all messages in the list view to show only those meeting the
		/// selection criteria.
		/// </summary>
		private void RefreshMessages()
		{
			Application.UseWaitCursor = true;

			this.log.BeginUpdate();
			this.log.Items.Clear();
			this.log.Groups.Clear();
			LogSessionDictionary log = task.Log.Entries;

			//Iterate over every key
			foreach (DateTime sessionTime in log.Keys)
			{
				ListViewGroup sessionGroup = new ListViewGroup(S._("Session: {0:F}", sessionTime));
				this.log.Groups.Add(sessionGroup);

				foreach (LogEntry entry in log[sessionTime])
					task_Logged(this, new LogEventArgs(entry));

				if (sessionGroup.Items.Count == 0)
				{
					ListViewItem item = this.log.Items.Add(string.Empty);
					item.Group = sessionGroup;
				}	
			}

			this.log.EndUpdate();
			Application.UseWaitCursor = false;
		}

		/// <summary>
		/// Enables/disables buttons based on the current system state.
		/// </summary>
		private void EnableButtons()
		{
			clear.Enabled = copy.Enabled = task.Log.Entries.Count > 0;
		}

		/// <summary>
		/// The task which this log is displaying entries for
		/// </summary>
		private Task task;
	}
}
