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
using System.ComponentModel;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser
{
	public partial class LogForm : Form, ILogTarget
	{
		public LogForm(Task task)
		{
			InitializeComponent();
			Theming.ApplyTheme(this);

			//Update the title
			Text = string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Text, task);

			//Populate the list of sessions
			foreach (LogSinkBase sink in task.Log)
				filterSessionCombobox.Items.Add(sink.StartTime);
			if (filterSessionCombobox.Items.Count != 0)
				filterSessionCombobox.SelectedIndex = filterSessionCombobox.Items.Count - 1;

			//Set the filter settings
			filterFilterTypeCombobox.SelectedIndex = 0;
			filterSeverityCombobox.SelectedIndex = 0;

			//Display the log entries
			Task = task;
			RefreshMessages();
			EnableButtons();

			//Register our event handler to get live log messages
			if (Task.Log.Count > 0)
				Task.Log.Last().Chain(this);
			Task.TaskStarted += task_TaskStarted;
		}

		private void LogForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			Task.TaskStarted -= task_TaskStarted;
			if (Task.Log.Count > 0)
				Task.Log.Last().Unchain(this);
		}

		private void filter_Changed(object sender, EventArgs e)
		{
			RefreshMessages();
		}

		private void task_TaskStarted(object sender, EventArgs e)
		{
			if (IsDisposed || !IsHandleCreated)
				return;
			if (InvokeRequired)
			{
				Invoke((EventHandler<EventArgs>)task_TaskStarted, sender, e);
				return;
			}

			filterSessionCombobox.Items.Add(Task.Log.Last().StartTime);
		}

		private void log_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			LogEntry entry = EntryCache[e.ItemIndex];
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
				SelectedEntries.Add(e.ItemIndex, EntryCache[e.ItemIndex]);
			}
			else
			{
				SelectedEntries.Remove(e.ItemIndex);
			}
		}

		private void log_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
		{
			for (int i = e.StartIndex; i <= e.EndIndex; ++i)
			{
				if (e.IsSelected)
				{
					SelectedEntries.Add(i, EntryCache[i]);
				}
				else
				{
					SelectedEntries.Remove(i);
				}
			}
		}

		private void log_ItemActivate(object sender, EventArgs e)
		{
			if (SelectedEntries.Count < 1)
				return;

			//Get the selected entry from the entry cache.
			LogEntry selectedEntry = SelectedEntries.Values[0];

			//Decide on the icon.
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
				MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1,
				Localisation.IsRightToLeft(this) ?
					MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
		}

		private void logContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			copySelectedEntriesToolStripMenuItem.Enabled = SelectedEntries.Count != 0;
		}

		private void copySelectedEntriesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//Ensure we've got stuff to copy.
			if (SelectedEntries.Count == 0)
				return;

			StringBuilder csvText = new StringBuilder();
			StringBuilder rawText = new StringBuilder();

			DateTime sessionTime = (DateTime)filterSessionCombobox.SelectedItem;
			csvText.AppendLine(S._("Session: {0:F}", sessionTime));
			rawText.AppendLine(S._("Session: {0:F}", sessionTime));

			foreach (LogEntry entry in SelectedEntries.Values)
			{
				//Append the entry's contents to our buffer.
				string timeStamp = entry.Timestamp.ToString("F", CultureInfo.CurrentCulture);
				string message = entry.Message;
				csvText.AppendFormat("\"{0}\",\"{1}\",\"{2}\"\n",
					timeStamp.Replace("\"", "\"\""), entry.Level.ToString(),
					message.Replace("\"", "\"\""));
				rawText.AppendFormat("{0}	{1}	{2}\r\n", timeStamp, entry.Level.ToString(),
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
			Task.Log.Clear();

			//Reset the list of sessions
			filterSessionCombobox.Items.Clear();

			//And reset the list-view control
			log.VirtualListSize = 0;
			SelectedEntries.Clear();
			EntryCache.Clear();

			//Finally update the UI state.
			EnableButtons();
		}

		private void close_Click(object sender, EventArgs e)
		{
			Close();
		}

		#region ILogTarget Members

		public void OnEventLogged(object sender, LogEventArgs e)
		{
			if (IsDisposed || !IsHandleCreated)
				return;
			if (InvokeRequired)
			{
				Invoke((EventHandler<LogEventArgs>)OnEventLogged, sender, e);
				return;
			}

			//Check whether the current entry meets the criteria for display.
			if (filterSessionCombobox.SelectedItem == null || !MeetsCriteria(e.LogEntry))
			{
				return;
			}

			//Add it to the cache and increase our virtual list size.
			EntryCache.Add(e.LogEntry);
			++log.VirtualListSize;

			//Enable the clear and copy log buttons only if we have entries to copy.
			EnableButtons();
		}

		public void Chain(ILogTarget target)
		{
			throw new NotImplementedException();
		}

		public void Unchain(ILogTarget target)
		{
			throw new NotImplementedException();
		}

		#endregion

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
			if (Task == null)
				return;

			//Check if we have any session selected
			if (filterSessionCombobox.SelectedIndex == -1)
				return;

			Application.UseWaitCursor = true;
			LogSinkBase sink = Task.Log[filterSessionCombobox.SelectedIndex];
			EntryCache.Clear();
			SelectedEntries.Clear();
			EntryCache.AddRange(sink.Where(MeetsCriteria));

			//Set the list view size and update all the control states
			log.VirtualListSize = EntryCache.Count;
			log.Refresh();
			EnableButtons();
			Application.UseWaitCursor = false;
		}

		/// <summary>
		/// Enables/disables buttons based on the current system state.
		/// </summary>
		private void EnableButtons()
		{
			clear.Enabled = Task.Log.Count > 0;
		}

		/// <summary>
		/// The task which this log is displaying entries for
		/// </summary>
		private Task Task;

		/// <summary>
		/// Stores all log entries fulfilling the current criteria for rapid access.
		/// </summary>
		private List<LogEntry> EntryCache = new List<LogEntry>();

		/// <summary>
		/// Stores all currently selected list view entry indices. The key is the
		/// index which is selected.
		/// </summary>
		private SortedList<int, LogEntry> SelectedEntries = new SortedList<int, LogEntry>();
	}
}