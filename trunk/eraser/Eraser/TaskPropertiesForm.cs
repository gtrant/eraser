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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.IO;

using System.Globalization;
using Eraser.Manager;
using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser
{
	public partial class TaskPropertiesForm : Form
	{
		public TaskPropertiesForm()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
			scheduleTime.CustomFormat = DateTimeFormatInfo.CurrentInfo.ShortTimePattern;

			//Set a default task type
			typeManual.Checked = true;
			scheduleDaily.Checked = true;
		}

		/// <summary>
		/// Sets or retrieves the task object to be edited or being edited.
		/// </summary>
		public Task Task
		{
			get { UpdateTaskFromUI(); return task; }
			set { task = value; UpdateUIFromTask(); }
		}

		/// <summary>
		/// Updates the local task object from the UI elements.
		/// </summary>
		private void UpdateTaskFromUI()
		{
			//Set the name of the task
			task.Name = name.Text;

			//And the schedule, if selected.
			if (typeManual.Checked)
			{
				task.Schedule = Schedule.RunManually;
			}
			else if (typeImmediate.Checked)
			{
				task.Schedule = Schedule.RunNow;
			}
			else if (typeRestart.Checked)
			{
				task.Schedule = Schedule.RunOnRestart;
			}
			else if (typeRecurring.Checked)
			{
				RecurringSchedule schedule = new RecurringSchedule();
				schedule.ExecutionTime = new DateTime(1, 1, 1, scheduleTime.Value.Hour,
					scheduleTime.Value.Minute, scheduleTime.Value.Second);

				if (scheduleDaily.Checked)
				{
					if (scheduleDailyByDay.Checked)
					{
						schedule.ScheduleType = RecurringScheduleUnit.Daily;
						schedule.Frequency = (int)scheduleDailyByDayFreq.Value;
					}
					else
					{
						schedule.ScheduleType = RecurringScheduleUnit.Weekdays;
					}
				}
				else if (scheduleWeekly.Checked)
				{
					schedule.ScheduleType = RecurringScheduleUnit.Weekly;
					schedule.Frequency = (int)scheduleWeeklyFreq.Value;
					DaysOfWeek weeklySchedule = 0;
					if (scheduleWeeklyMonday.Checked)
						weeklySchedule |= DaysOfWeek.Monday;
					if (scheduleWeeklyTuesday.Checked)
						weeklySchedule |= DaysOfWeek.Tuesday;
					if (scheduleWeeklyWednesday.Checked)
						weeklySchedule |= DaysOfWeek.Wednesday;
					if (scheduleWeeklyThursday.Checked)
						weeklySchedule |= DaysOfWeek.Thursday;
					if (scheduleWeeklyFriday.Checked)
						weeklySchedule |= DaysOfWeek.Friday;
					if (scheduleWeeklySaturday.Checked)
						weeklySchedule |= DaysOfWeek.Saturday;
					if (scheduleWeeklySunday.Checked)
						weeklySchedule |= DaysOfWeek.Sunday;
					schedule.WeeklySchedule = weeklySchedule;
				}
				else if (scheduleMonthly.Checked)
				{
					schedule.ScheduleType = RecurringScheduleUnit.Monthly;
					schedule.Frequency = (int)scheduleMonthlyFreq.Value;
					schedule.MonthlySchedule = (int)scheduleMonthlyDayNumber.Value;
				}
				else
					throw new ArgumentException("No such scheduling method.");

				task.Schedule = schedule;
			}
		}

		/// <summary>
		/// Updates the UI elements to reflect the data in the Task object.
		/// </summary>
		private void UpdateUIFromTask()
		{
			//Set the name of the task
			name.Text = task.Name;

			//And the schedule, if selected.
			if (task.Schedule == Schedule.RunManually)
			{
				typeManual.Checked = true;
			}
			else if (task.Schedule == Schedule.RunNow)
			{
				typeImmediate.Checked = true;
			}
			else if (task.Schedule == Schedule.RunOnRestart)
			{
				typeRestart.Checked = true;
			}
			else
			{
				typeRecurring.Checked = true;
				RecurringSchedule schedule = (RecurringSchedule)task.Schedule;
				scheduleTime.Value = scheduleTime.MinDate.Add(schedule.ExecutionTime.TimeOfDay);

				switch (schedule.ScheduleType)
				{
					case RecurringScheduleUnit.Daily:
						scheduleDaily.Checked = true;
						scheduleDailyByDay.Checked = true;
						scheduleDailyByDayFreq.Value = schedule.Frequency;
						break;
					case RecurringScheduleUnit.Weekdays:
						scheduleDaily.Checked = true;
						scheduleDailyByWeekday.Checked = true;
						break;
					case RecurringScheduleUnit.Weekly:
						scheduleWeeklyFreq.Value = schedule.Frequency;
						scheduleWeekly.Checked = true;
						scheduleWeeklyMonday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Monday) != 0;
						scheduleWeeklyTuesday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Tuesday) != 0;
						scheduleWeeklyWednesday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Wednesday) != 0;
						scheduleWeeklyThursday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Thursday) != 0;
						scheduleWeeklyFriday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Friday) != 0;
						scheduleWeeklySaturday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Saturday) != 0;
						scheduleWeeklySunday.Checked =
							(schedule.WeeklySchedule & DaysOfWeek.Sunday) != 0;
						break;
					case RecurringScheduleUnit.Monthly:
						scheduleMonthly.Checked = true;
						scheduleMonthlyFreq.Value = schedule.Frequency;
						scheduleMonthlyDayNumber.Value = schedule.MonthlySchedule;
						break;
					default:
						throw new ArgumentException("Unknown schedule type.");
				}
			}
		}

		/// <summary>
		/// Triggered when the list view needs to display an item.
		/// </summary>
		/// <param name="sender">The list view.</param>
		/// <param name="e">Event argument.</param>
		private void data_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			ErasureTarget target = task.Targets[e.ItemIndex];
			e.Item = new ListViewItem(PathUtil.GetCompactPath(target.UIText,
				data.Columns[0].Width, data.Font));
			e.Item.ToolTipText = target.UIText;

			e.Item.SubItems.Add(target.Method == ErasureMethodRegistrar.Default ?
				S._("(default)") : target.Method.Name);
		}

		/// <summary>
		/// Triggered when the user clicks on the Add Data button.
		/// </summary>
		/// <param name="sender">The button.</param>
		/// <param name="e">Event argument.</param>
		private void dataAdd_Click(object sender, EventArgs e)
		{
			using (TaskDataSelectionForm form = new TaskDataSelectionForm())
			{
				if (form.ShowDialog() == DialogResult.OK)
				{
					ErasureTarget target = form.Target;
					task.Targets.Add(target);
					errorProvider.Clear();

					++data.VirtualListSize;
				}
			}
		}

		/// <summary>
		/// Generated when the user double-clicks an item in the list-view.
		/// </summary>
		/// <param name="sender">The list-view which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void data_ItemActivate(object sender, EventArgs e)
		{
			using (TaskDataSelectionForm form = new TaskDataSelectionForm())
			{
				form.Target = task.Targets[data.SelectedIndices[0]];

				if (form.ShowDialog() == DialogResult.OK)
				{
					ErasureTarget target = form.Target;
					task.Targets.RemoveAt(data.SelectedIndices[0]);
					task.Targets.Insert(data.SelectedIndices[0], target);
				}
			}
		}

		private void data_DragEnter(object sender, DragEventArgs e)
		{
			//Get the list of files.
			bool recycleBin = false;
			List<string> paths = new List<string>(TaskDragDropHelper.GetFiles(e, out recycleBin));

			for (int i = 0; i < paths.Count; ++i)
			{
				//Just use the file name/directory name.
				paths[i] = Path.GetFileName(paths[i]);
			}

			//Add the recycle bin if it was dropped.
			if (recycleBin)
				paths.Add(S._("Recycle Bin"));

			string description = null;
			if (paths.Count == 0)
			{
				e.Effect = DragDropEffects.None;
				description = S._("Cannot add the selected items");
			}
			else
			{
				e.Effect = DragDropEffects.Copy;
				description = S._("Add {0}");
			}

			TaskDragDropHelper.OnDragEnter(this, e, description, paths);
		}

		private void data_DragLeave(object sender, EventArgs e)
		{
			DropTargetHelper.DragLeave((Control)sender);
		}

		private void data_DragOver(object sender, DragEventArgs e)
		{
			DropTargetHelper.DragOver(new Point(e.X, e.Y), e.Effect);
		}

		private void data_DragDrop(object sender, DragEventArgs e)
		{
			TaskDragDropHelper.OnDrop(e);
			if (e.Effect == DragDropEffects.None)
				return;

			//Determine our action.
			bool recycleBin = false;
			List<string> paths = new List<string>(TaskDragDropHelper.GetFiles(e, out recycleBin));

			//Add the targets
			foreach (ErasureTarget target in TaskDragDropHelper.GetTargets(paths, recycleBin))
			{
				task.Targets.Add(target);
				++data.VirtualListSize;

				errorProvider.Clear();
			}
		}

		/// <summary>
		/// Generated when the user right-clicks on the data selection list-view.
		/// </summary>
		/// <param name="sender">The menu being opened.</param>
		/// <param name="e">Event argument.</param>
		private void dataContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			if (data.SelectedIndices.Count == 0)
			{
				e.Cancel = true;
				return;
			}
		}

		/// <summary>
		/// Generated when the user selects the menu itm to remove the selected
		/// data from the list of data to erase.
		/// </summary>
		/// <param name="sender">The object triggering the event.</param>
		/// <param name="e">Event argument.</param>
		private void deleteDataToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (data.SelectedIndices.Count == 0)
				return;

			//Get the list of selected indices; sort them in decreasing order so that we
			//can iterate and remove items without changing indices
			SortedSet<int> indices = new SortedSet<int>();
			foreach (int index in data.SelectedIndices)
				indices.Add(index);

			//Remove the items from the list view
			data.SelectedIndices.Clear();
			data.VirtualListSize -= indices.Count;

			//Then finally remove the items from the task list
			foreach (int index in indices.Reverse())
				task.Targets.RemoveAt(index);
		}

		/// <summary>
		/// Generated when the task schedule type changes.
		/// </summary>
		/// <param name="sender">The object triggering the event.</param>
		/// <param name="e">Event argument.</param>
		private void taskType_CheckedChanged(object sender, EventArgs e)
		{
			scheduleTimeLbl.Enabled = scheduleTime.Enabled = schedulePattern.Enabled =
				scheduleDaily.Enabled = scheduleWeekly.Enabled =
				scheduleMonthly.Enabled = typeRecurring.Checked;
			nonRecurringPanel.Visible = !typeRecurring.Checked;
			
			scheduleSpan_CheckedChanged(sender, e);
		}

		/// <summary>
		/// Generated when any of the schedule spans have been clicked.
		/// </summary>
		/// <param name="sender">The radio button triggering the event.</param>
		/// <param name="e">Event argument.</param>
		private void scheduleSpan_Clicked(object sender, EventArgs e)
		{
			//Check the selected radio button
			scheduleDaily.Checked = sender == scheduleDaily;
			scheduleWeekly.Checked = sender == scheduleWeekly;
			scheduleMonthly.Checked = sender == scheduleMonthly;

			//Then trigger the checked changed event.
			scheduleSpan_CheckedChanged(sender, e);
		}

		/// <summary>
		/// Generated when the scheduling frequency is changed.
		/// </summary>
		/// <param name="sender">The object triggering the event.</param>
		/// <param name="e">Event argument.</param>
		private void scheduleSpan_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton[] group = new RadioButton[] {
				scheduleDaily, scheduleWeekly, scheduleMonthly
			};
			if (group.Contains(sender) && ((RadioButton)sender).Checked)
			{
				foreach (RadioButton button in group)
					if (button != sender)
						button.Checked = false;
			}

			scheduleDailyByDay.Enabled = scheduleDailyByDayLbl.Enabled =
				scheduleDailyByWeekday.Enabled = scheduleDaily.Checked &&
				typeRecurring.Checked;
			scheduleWeeklyLbl.Enabled = scheduleWeeklyFreq.Enabled =
				scheduleWeeklyFreqLbl.Enabled = scheduleWeeklyMonday.Enabled =
				scheduleWeeklyTuesday.Enabled = scheduleWeeklyWednesday.Enabled =
				scheduleWeeklyThursday.Enabled = scheduleWeeklyFriday.Enabled =
				scheduleWeeklySaturday.Enabled = scheduleWeeklySunday.Enabled =
				scheduleWeekly.Checked && typeRecurring.Checked;
			scheduleMonthlyLbl.Enabled = scheduleMonthlyDayNumber.Enabled =
				scheduleMonthlyEveryLbl.Enabled = scheduleMonthlyFreq.Enabled =
				scheduleMonthlyMonthLbl.Enabled = scheduleMonthly.Checked &&
				typeRecurring.Checked;

			scheduleDailySpan_CheckedChanged(sender, e);
		}

		/// <summary>
		/// Generated when any of the daily frequency radio buttons are clicked.
		/// </summary>
		/// <param name="sender">The radio button which triggers the event.</param>
		/// <param name="e">Event argument.</param>
		private void scheduleDailySpan_Clicked(object sender, EventArgs e)
		{
			scheduleDailyByDay.CheckedChanged -= scheduleDailySpan_CheckedChanged;
			scheduleDailyByWeekday.CheckedChanged -= scheduleDailySpan_CheckedChanged;

			scheduleDailyByDay.Checked = sender == scheduleDailyByDay;
			scheduleDailyByWeekday.Checked = sender == scheduleDailyByWeekday;

			scheduleDailyByDay.CheckedChanged += scheduleDailySpan_CheckedChanged;
			scheduleDailyByWeekday.CheckedChanged += scheduleDailySpan_CheckedChanged;
			
			scheduleDailySpan_CheckedChanged(sender, e);
		}

		/// <summary>
		/// Generated when the daily frequency argument is changed.
		/// </summary>
		/// <param name="sender">The object triggering the event.</param>
		/// <param name="e">Event argument.</param>
		private void scheduleDailySpan_CheckedChanged(object sender, EventArgs e)
		{
			scheduleDailyByDayLbl.Enabled = scheduleDailyByDayFreq.Enabled =
				scheduleDailyByDay.Checked && scheduleDaily.Checked && typeRecurring.Checked;
		}

		/// <summary>
		/// Generated when the dialog is closed.
		/// </summary>
		/// <param name="sender">The object triggering the event.</param>
		/// <param name="e">Event argument.</param>
		private void ok_Click(object sender, EventArgs e)
		{
			if (task.Targets.Count == 0)
			{
				errorProvider.SetIconPadding(data, -16);
				errorProvider.SetIconAlignment(data, ErrorIconAlignment.BottomRight);
				errorProvider.SetError(data, S._("The task has no data to erase."));
				container.SelectedIndex = 0;
				return;
			}
			else if (typeRecurring.Checked && scheduleWeekly.Checked)
			{
				if (!scheduleWeeklyMonday.Checked && !scheduleWeeklyTuesday.Checked &&
					!scheduleWeeklyWednesday.Checked && !scheduleWeeklyThursday.Checked &&
					!scheduleWeeklyFriday.Checked && !scheduleWeeklySaturday.Checked &&
					!scheduleWeeklySunday.Checked)
				{
					errorProvider.SetIconPadding(scheduleWeeklyDays, -16);
					errorProvider.SetError(scheduleWeeklyDays, S._("The task needs to run " +
						"on at least one day a week"));
					container.SelectedIndex = 1;
					return;
				}
			}

			errorProvider.Clear();

			//Close the dialog
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// The task being edited.
		/// </summary>
		private Task task = new Task();
	}
}