/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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

			//Update the title
			Text = string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Text, task.UIText);

			//Populate the list of sessions
			foreach (DateTime session in task.Log.Entries.Keys)
				filterSessionCombobox.Items.Add(session);
			if (task.Log.Entries.Keys.Count != 0)
				filterSessionCombobox.SelectedIndex = filterSessionCombobox.Items.Count - 1;

			//Set the filter settings
			filterFilterTypeCombobox.SelectedIndex = 0;
			filterSeverityCombobox.SelectedIndex = 0;

			//Display the log entries
			this.task = task;
			RefreshMessages();
			EnableButtons();

			//Register our event handler to get live log messages
			task.Log.Logged += task_Logged;
			task.Log.NewSession += task_NewSession;
		}

		private void LogForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			task.Log.Logged -= task_Logged;
		}

		private void filter_Changed(object sender, EventArgs e)
		{
			RefreshMessages();
		}

		private void task_NewSession(object sender, EventArgs e)
		{
			if (!IsHandleCreated)
				return;
			if (InvokeRequired)
			{
				Invoke(new EventHandler<EventArgs>(task_NewSession), sender, e);
				return;
			}

			filterSessionCombobox.Items.Add(task.Log.LastSession);
		}

		private void task_Logged(object sender, LogEventArgs e)
		{
			if (!IsHandleCreated)
				return;
			if (InvokeRequired)
			{
				Invoke(new EventHandler<LogEventArgs>(task_Logged), sender, e);
				return;
			}

			//Check whether the current entry meets the criteria for display. Since
			//this is an event handler for new log messages only, we should only
			//display this entry when the session in question is the last one.
			if (filterSessionCombobox.SelectedItem == null ||
				(DateTime)filterSessionCombobox.SelectedItem != task.Log.LastSession ||
				!MeetsCriteria(e.LogEntry))
			{
				return;
			}

			//Add it to the cache and increase our virtual list size.
			entryCache.Add(e.LogEntry);
			++log.VirtualListSize;

			//Enable the clear and copy log buttons only if we have entries to copy.
			EnableButtons();
		}

		private void log_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			LogEntry entry = entryCache[e.ItemIndex];
			e.Item = new ListViewItem(entry.Timestamp.ToString("F", CultureInfo.CurrentCulture));
			e.Item.SubItems.Add(entry.Level.ToString());
			e.Item.SubItems.Add(entry.Message);

			switch (entry.Level)
			{
				case LogLevel.Fatal:
					e.Item.ForeColor = Color.Red;
					break;
				case LogLevel.Error:
					e.Item.ForeColor = Color.OrangeRed;
					break;
				case LogLevel.Warning:
					e.Item.ForeColor = Color.Orange;
					break;
			}
		}

		private void log_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (e.IsSelected)
			{
				if (!selectedEntries.ContainsKey(e.ItemIndex))
					selectedEntries.Add(e.ItemIndex, null);
			}
			else
			{
				selectedEntries.Remove(e.ItemIndex);
			}
		}

		private void log_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
		{
			for (int i = e.StartIndex; i <= e.EndIndex; ++i)
			{
				if (e.IsSelected)
				{
					if (!selectedEntries.ContainsKey(i))
						selectedEntries.Add(i, null);
				}
				else
				{
					selectedEntries.Remove(i);
				}
			}
		}

		private void log_ItemActivate(object sender, EventArgs e)
		{
			if (selectedEntries.Count < 1)
				return;

			int currentEntryIndex = 0;
			LogEntry selectedEntry = new LogEntry();
			foreach (LogEntry entry in task.Log.Entries[(DateTime)filterSessionCombobox.SelectedItem])
			{
				//Only copy entries which meet the display criteria and that they are selected
				if (!MeetsCriteria(entry))
					continue;
				if (!selectedEntries.ContainsKey(currentEntryIndex++))
					continue;

				selectedEntry = entry;
				break;
			}

			//Decide on the icon
			MessageBoxIcon icon = MessageBoxIcon.None;
			switch (selectedEntry.Level)
			{
				case LogLevel.Information:
					icon = MessageBoxIcon.Information;
					break;
				case LogLevel.Warning:
					icon = MessageBoxIcon.Warning;
					break;
				case LogLevel.Error:
				case LogLevel.Fatal:
					icon = MessageBoxIcon.Error;
					break;
			}

			//Show the message
			MessageBox.Show(this, selectedEntry.Message,
				selectedEntry.Timestamp.ToString("F", CultureInfo.CurrentCulture),
				MessageBoxButtons.OK, icon);
		}

		private void logContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			copySelectedEntriesToolStripMenuItem.Enabled = selectedEntries.Count != 0;
		}

		private void copySelectedEntriesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Ensure we've got stuff to copy.
			if (selectedEntries.Count == 0)
				return;

			StringBuilder csvText = new StringBuilder();
			StringBuilder rawText = new StringBuilder();
			LogSessionDictionary logEntries = task.Log.Entries;

			DateTime sessionTime = (DateTime)filterSessionCombobox.SelectedItem;
			csvText.AppendLine(S._("Session: {0:F}", sessionTime));
			rawText.AppendLine(S._("Session: {0:F}", sessionTime));

			int currentEntryIndex = 0;
			foreach (LogEntry entry in logEntries[sessionTime])
			{
				//Only copy entries which meet the display criteria and that they are selected
				if (!MeetsCriteria(entry))
					continue;
				if (!selectedEntries.ContainsKey(currentEntryIndex++))
					continue;

				string timeStamp = entry.Timestamp.ToString("F", CultureInfo.CurrentCulture);
				string message = entry.Message;
				csvText.AppendFormat("\"{0}\",\"{1}\",\"{2}\"\n",
					timeStamp.Replace("\"", "\"\""), entry.Level.ToString(),
					message.Replace("\"", "\"\""));
				rawText.AppendFormat("{0}	{1}	{2}\n", timeStamp, entry.Level.ToString(),
					message);
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

		private void clear_Click(object sender, EventArgs e)
		{
			//Clear the backing store
			task.Log.Clear();

			//Reset the list of sessions
			filterSessionCombobox.Items.Clear();

			//And reset the list-view control
			log.VirtualListSize = 0;
			selectedEntries.Clear();
			entryCache.Clear();

			//Finally update the UI state.
			EnableButtons();
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
			//Check for the severity
			switch (filterFilterTypeCombobox.SelectedIndex)
			{
				case 0: //and above
					if (entry.Level < (LogLevel)filterSeverityCombobox.SelectedIndex)
						return false;
					break;

				case 1: //equal to
					if (entry.Level != (LogLevel)filterSeverityCombobox.SelectedIndex)
						return false;
					break;

				case 2: //and below
					if (entry.Level > (LogLevel)filterSeverityCombobox.SelectedIndex)
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
			//Check if we have a task
			if (task == null)
				return;

			Application.UseWaitCursor = true;
			LogSessionDictionary log = task.Log.Entries;
			entryCache.Clear();
			selectedEntries.Clear();

			//Iterate over every key
			foreach (DateTime sessionTime in log.Keys)
			{
				//Check for the session time
				if (filterSessionCombobox.SelectedItem == null || 
					sessionTime != (DateTime)filterSessionCombobox.SelectedItem)
					continue;

				foreach (LogEntry entry in log[sessionTime])
				{
					//Check if the entry meets the criteria for viewing
					if (MeetsCriteria(entry))
						entryCache.Add(entry);
				}
			}

			//Set the list view size and update all the control states
			this.log.VirtualListSize = entryCache.Count;
			this.log.Refresh();
			EnableButtons();
			Application.UseWaitCursor = false;
		}

		/// <summary>
		/// Enables/disables buttons based on the current system state.
		/// </summary>
		private void EnableButtons()
		{
			clear.Enabled = task.Log.Entries.Count > 0;
		}

		/// <summary>
		/// The task which this log is displaying entries for
		/// </summary>
		private Task task;

		/// <summary>
		/// Stores all log entries fulfilling the current criteria for rapid access.
		/// </summary>
		private List<LogEntry> entryCache = new List<LogEntry>();

		/// <summary>
		/// Stores all currently selected list view entry indices.
		/// </summary>
		private SortedList<int, object> selectedEntries = new SortedList<int, object>();
	}
}
