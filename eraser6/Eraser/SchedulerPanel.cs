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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Eraser.Manager;
using Eraser.Util;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.ComponentModel;
using ProgressChangedEventArgs = Eraser.Manager.ProgressChangedEventArgs;

namespace Eraser
{
	internal partial class SchedulerPanel : Eraser.BasePanel
	{
		public SchedulerPanel()
		{
			InitializeComponent();
			UXThemeApi.UpdateControlTheme(schedulerDefaultMenu);
			CreateHandle();

			//Populate the scheduler list-view with the current task list
			ExecutorTasksCollection tasks = Program.eraserClient.Tasks;
			foreach (Task task in tasks)
				DisplayTask(task);

			//Hook the event machinery to our class. Handle the task Added and Removed
			//events.
			Program.eraserClient.TaskAdded += TaskAdded;
			Program.eraserClient.TaskDeleted += TaskDeleted;
		}

		private void DisplayTask(Task task)
		{
			//Add the item to the list view
			ListViewItem item = scheduler.Items.Add(task.UIText);
			item.SubItems.Add(string.Empty);
			item.SubItems.Add(string.Empty);

			//Set the tag of the item so we know which task on the LV corresponds
			//to the physical task object.
			item.Tag = task;

			//Add our event handlers to the task
			task.TaskStarted += task_TaskStarted;
			task.ProgressChanged += task_ProgressChanged;
			task.TaskFinished += task_TaskFinished;

			//Show the fields on the list view
			UpdateTask(item);
		}

		private void UpdateTask(ListViewItem item)
		{
			//Get the task object
			Task task = (Task)item.Tag;

			//Set the task name
			item.Text = task.UIText;

			//Set the next run time of the task
			if (task.Queued)
			{
				item.SubItems[1].Text = S._("Queued for execution");
				item.SubItems[2].Text = string.Empty;
			}
			else if (task.Schedule is RecurringSchedule)
				item.SubItems[1].Text = ((task.Schedule as RecurringSchedule).NextRun.
					ToString("F", CultureInfo.CurrentCulture));
			else if (task.Schedule == Schedule.RunNow || task.Schedule == Schedule.RunManually)
				item.SubItems[1].Text = S._("Not queued");
			else
				item.SubItems[1].Text = task.Schedule.UIText;

			//Set the group of the task.
			CategorizeTask(task, item);
		}

		private void CategorizeTask(Task task)
		{
			CategorizeTask(task, GetTaskItem(task));
		}

		private void CategorizeTask(Task task, ListViewItem item)
		{
			if (task.Schedule == Schedule.RunNow || task.Schedule == Schedule.RunManually)
				item.Group = scheduler.Groups["manual"];
			else if (task.Schedule == Schedule.RunOnRestart)
				item.Group = scheduler.Groups["restart"];
			else
				item.Group = scheduler.Groups["recurring"];
		}

		/// <summary>
		/// Handles the Task Added event.
		/// </summary>
		private void TaskAdded(object sender, TaskEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new EventHandler<TaskEventArgs>(TaskAdded), sender, e);
				return;
			}

			//Display a balloon notification if the parent frame has been minimised.
			MainForm parent = (MainForm)FindForm();
			if (parent != null && (parent.WindowState == FormWindowState.Minimized || !parent.Visible))
			{
				parent.ShowNotificationBalloon(S._("New task added"), S._("{0} " +
					"has just been added to the list of tasks.", e.Task.UIText),
					ToolTipIcon.Info);
			}

			DisplayTask(e.Task);
		}

		private void DeleteSelectedTasks()
		{
			if (MessageBox.Show(this, S._("Are you sure you want to delete the selected tasks?"),
				   S._("Eraser"), MessageBoxButtons.YesNo, MessageBoxIcon.Question,
				   MessageBoxDefaultButton.Button1,
				   S.IsRightToLeft(this) ? MessageBoxOptions.RtlReading : 0) != DialogResult.Yes)
			{
				return;
			}

			foreach (ListViewItem item in scheduler.SelectedItems)
			{
				Task task = (Task)item.Tag;
				if (!task.Executing)
					Program.eraserClient.Tasks.Remove(task);
			}
		}

		/// <summary>
		/// Handles the task deleted event.
		/// </summary>
		private void TaskDeleted(object sender, TaskEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new EventHandler<TaskEventArgs>(TaskDeleted), sender, e);
				return;
			}

			foreach (ListViewItem item in scheduler.Items)
				if (((Task)item.Tag) == e.Task)
				{
					scheduler.Items.Remove(item);
					break;
				}

			PositionProgressBar();
		}

		/// <summary>
		/// Handles the task start event.
		/// </summary>
		/// <param name="e">The task event object.</param>
		void task_TaskStarted(object sender, TaskEventArgs e)
		{
			if (scheduler.InvokeRequired)
			{
				Invoke(new EventHandler<TaskEventArgs>(task_TaskStarted), sender, e);
				return;
			}

			//Get the list view item
			ListViewItem item = GetTaskItem(e.Task);

			//Update the status.
			item.SubItems[1].Text = S._("Running...");

			//Show the progress bar
			schedulerProgress.Tag = item;
			schedulerProgress.Visible = true;
			schedulerProgress.Value = 0;
			PositionProgressBar();
		}

		/// <summary>
		/// Handles the progress event by the task.
		/// </summary>
		void task_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			//Make sure we handle the event in the main thread as this requires
			//GUI calls.
			if (scheduler.InvokeRequired)
			{
				Invoke((EventHandler<ProgressChangedEventArgs>)task_ProgressChanged, sender, e);
				return;
			}

			//Update the progress bar
			ErasureTarget target = (ErasureTarget)sender;
			schedulerProgress.Value = (int)(target.Task.Progress.Progress * 1000.0);
		}

		/// <summary>
		/// Handles the task completion event.
		/// </summary>
		void task_TaskFinished(object sender, TaskEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new EventHandler<TaskEventArgs>(task_TaskFinished), sender, e);
				return;
			}

			//Get the list view item
			ListViewItem item = GetTaskItem(e.Task);
			if (item == null)
				return;

			//Hide the progress bar
			if (schedulerProgress.Tag != null && schedulerProgress.Tag == item)
			{
				schedulerProgress.Tag = null;
				schedulerProgress.Visible = false;
			}

			//Get the exit status of the task.
			LogLevel highestLevel = LogLevel.Information;
			LogEntryCollection logs = e.Task.Log.LastSessionEntries;
			foreach (LogEntry log in logs)
				if (log.Level > highestLevel)
					highestLevel = log.Level;

			//Show a balloon to inform the user
			MainForm parent = (MainForm)FindForm();

			//TODO: Is this still needed?
			if (parent == null)
				throw new InvalidOperationException();
			if (parent.WindowState == FormWindowState.Minimized || !parent.Visible)
			{
				string message = null;
				ToolTipIcon icon = ToolTipIcon.None;

				switch (highestLevel)
				{
					case LogLevel.Warning:
						message = S._("The task {0} has completed with warnings.", e.Task.UIText);
						icon = ToolTipIcon.Warning;
						break;
					case LogLevel.Error:
						message = S._("The task {0} has completed with errors.", e.Task.UIText);
						icon = ToolTipIcon.Error;
						break;
					case LogLevel.Fatal:
						message = S._("The task {0} did not complete.", e.Task.UIText);
						icon = ToolTipIcon.Error;
						break;
					default:
						message = S._("The task {0} has completed.", e.Task.UIText);
						icon = ToolTipIcon.Info;
						break;
				}

				parent.ShowNotificationBalloon(S._("Task executed"), message,
					icon);
			}

			//If the user requested us to remove completed one-time tasks, do so.
			if (EraserSettings.Get().ClearCompletedTasks &&
				(e.Task.Schedule == Schedule.RunNow) && highestLevel < LogLevel.Warning)
			{
				Program.eraserClient.Tasks.Remove(e.Task);
			}

			//Otherwise update the UI
			else
			{
				switch (highestLevel)
				{
					case LogLevel.Warning:
						item.SubItems[2].Text = S._("Completed with warnings");
						break;
					case LogLevel.Error:
						item.SubItems[2].Text = S._("Completed with errors");
						break;
					case LogLevel.Fatal:
						item.SubItems[2].Text = S._("Not completed");
						break;
					default:
						item.SubItems[2].Text = S._("Completed");
						break;
				}

				//Recategorize the task. Do not assume the task has maintained the
				//category since run-on-restart tasks will be changed to immediately
				//run tasks.
				CategorizeTask(e.Task, item);

				//Update the status of the task.
				UpdateTask(item);
			}
		}

		/// <summary>
		/// Occurs when the user presses a key on the list view.
		/// </summary>
		/// <param name="sender">The list view which triggered the event.</param>
		/// <param name="e">Event argument.</param>
		private void scheduler_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
				DeleteSelectedTasks();
		}

		/// <summary>
		/// Occurs when the user double-clicks a scheduler item. This will result
		/// in the log viewer being called, or the progress dialog to be displayed.
		/// </summary>
		/// <param name="sender">The list view which triggered the event.</param>
		/// <param name="e">Event argument.</param>
		private void scheduler_ItemActivate(object sender, EventArgs e)
		{
			if (scheduler.SelectedItems.Count == 0)
				return;

			ListViewItem item = scheduler.SelectedItems[0];
			if (((Task)item.Tag).Executing)
				using (ProgressForm form = new ProgressForm((Task)item.Tag))
					form.ShowDialog();
			else
				editTaskToolStripMenuItem_Click(sender, e);
		}

		/// <summary>
		/// Occurs when the user drags a file over the scheduler
		/// </summary>
		private void scheduler_DragEnter(object sender, DragEventArgs e)
		{
			string descriptionMessage = string.Empty;
			string descriptionInsert = string.Empty;
			const string descrptionPlaceholder = "%1";
			if (!e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.None;
			else
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
				bool isTaskList = true;
				foreach (string file in files)
				{
					if (descriptionInsert.Length < 259 &&
						(descriptionInsert.Length < 3 || descriptionInsert.Substring(descriptionInsert.Length - 3) != "..."))
					{
						string append = string.Format(CultureInfo.InvariantCulture, "{0}, ",
							Path.GetFileNameWithoutExtension(file));
						if (descriptionInsert.Length + append.Length > 259)
						{
							descriptionInsert += ".....";
						}
						else
						{
							descriptionInsert += append;
						}
					}

					if (Path.GetExtension(file) != ".ersx")
						isTaskList = false;
				}
				descriptionInsert = descriptionInsert.Remove(descriptionInsert.Length - 2);

				if (isTaskList)
				{
					e.Effect = DragDropEffects.Copy;
					descriptionMessage = S._("Import tasks from {0}", descrptionPlaceholder);
				}
				else
				{
					e.Effect = DragDropEffects.Move;
					descriptionMessage = S._("Erase {0}", descrptionPlaceholder);
				}
			}

			DropTargetHelper.DragEnter(this, e.Data, new Point(e.X, e.Y), e.Effect,
				descriptionMessage, descriptionInsert);
		}

		private void scheduler_DragLeave(object sender, EventArgs e)
		{
			DropTargetHelper.DragLeave(this);
		}

		private void scheduler_DragOver(object sender, DragEventArgs e)
		{
			DropTargetHelper.DragOver(new Point(e.X, e.Y), e.Effect);
		}

		/// <summary>
		/// Occurs when the user drops a file into the scheduler.
		/// </summary>
		private void scheduler_DragDrop(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.None;
			else
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

				//Determine whether we are importing a task list or dragging files for
				//erasure.
				if (e.Effect == DragDropEffects.Copy)
				{
					foreach (string file in files)
						using (FileStream stream = new FileStream(file, FileMode.Open,
							FileAccess.Read, FileShare.Read))
						{
							try
							{
								Program.eraserClient.Tasks.LoadFromStream(stream);
							}
							catch (SerializationException ex)
							{
								MessageBox.Show(S._("Could not import task list from {0}. The error " +
									"returned was: {1}", file, ex.Message), S._("Eraser"),
									MessageBoxButtons.OK, MessageBoxIcon.Error,
									MessageBoxDefaultButton.Button1,
									S.IsRightToLeft(null) ? MessageBoxOptions.RtlReading : 0);
							}
						}
				}
				else if (e.Effect == DragDropEffects.Move)
				{
					//Create a task with the default settings
					Task task = new Task();
					foreach (string file in files)
					{
						FileSystemObjectTarget target;
						if (Directory.Exists(file))
							target = new FolderTarget();
						else
							target = new FileTarget();
						target.Path = file;

						task.Targets.Add(target);
					}

					//Add the task.
					Program.eraserClient.Tasks.Add(task);
				}
			}

			DropTargetHelper.Drop(e.Data, new Point(e.X, e.Y), e.Effect);
		}

		/// <summary>
		/// Occurs when the user right-clicks the list view.
		/// </summary>
		/// <param name="sender">The list view which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void schedulerMenu_Opening(object sender, CancelEventArgs e)
		{
			//If nothing's selected, show the Scheduler menu which just allows users to
			//create new tasks (like from the toolbar)
			if (scheduler.SelectedItems.Count == 0)
			{
				schedulerDefaultMenu.Show(schedulerMenu.Left, schedulerMenu.Top);
				e.Cancel = true;
				return;
			}

			bool aTaskNotQueued = false;
			bool aTaskExecuting = false;
			foreach (ListViewItem item in scheduler.SelectedItems)
			{
				Task task = (Task)item.Tag;
				aTaskNotQueued = aTaskNotQueued || (!task.Queued && !task.Executing);
				aTaskExecuting = aTaskExecuting || task.Executing;
			}

			runNowToolStripMenuItem.Enabled = aTaskNotQueued;
			cancelTaskToolStripMenuItem.Enabled = aTaskExecuting;

			editTaskToolStripMenuItem.Enabled = scheduler.SelectedItems.Count == 1 &&
				!((Task)scheduler.SelectedItems[0].Tag).Executing &&
				!((Task)scheduler.SelectedItems[0].Tag).Queued;
			deleteTaskToolStripMenuItem.Enabled = !aTaskExecuting;
		}

		/// <summary>
		/// Occurs when the user selects the New Task context menu item.
		/// </summary>
		/// <param name="sender">The menu which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void newTaskToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (TaskPropertiesForm form = new TaskPropertiesForm())
			{
				if (form.ShowDialog() == DialogResult.OK)
				{
					Task task = form.Task;
					Program.eraserClient.Tasks.Add(task);
				}
			}
		}

		/// <summary>
		/// Occurs whent the user selects the Run Now context menu item.
		/// </summary>
		/// <param name="sender">The menu which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void runNowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in scheduler.SelectedItems)
			{
				//Queue the task
				Task task = (Task)item.Tag;
				if (!task.Executing && !task.Queued)
				{
					Program.eraserClient.QueueTask(task);

					//Update the UI
					item.SubItems[1].Text = S._("Queued for execution");
				}
			}
		}

		/// <summary>
		/// Occurs whent the user selects the Cancel Task context menu item.
		/// </summary>
		/// <param name="sender">The menu which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void cancelTaskToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in scheduler.SelectedItems)
			{
				//Queue the task
				Task task = (Task)item.Tag;
				if (task.Executing || task.Queued)
				{
					task.Cancel();

					//Update the UI
					item.SubItems[1].Text = string.Empty;
				}
			}
		}

		/// <summary>
		/// Occurs when the user selects the View Task Log context menu item.
		/// </summary>
		/// <param name="sender">The menu item which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void viewTaskLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (scheduler.SelectedItems.Count != 1)
				return;

			ListViewItem item = scheduler.SelectedItems[0];
			using (LogForm form = new LogForm((Task)item.Tag))
				form.ShowDialog();
		}

		/// <summary>
		/// Occurs when the user selects the Edit Task context menu item.
		/// </summary>
		/// <param name="sender">The menu item which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void editTaskToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (scheduler.SelectedItems.Count != 1 ||
				((Task)scheduler.SelectedItems[0].Tag).Executing ||
				((Task)scheduler.SelectedItems[0].Tag).Queued)
			{
				return;
			}

			//Make sure that the task is not being executed, or else. This can
			//be done in the Client library, but there will be no effect on the
			//currently running task.
			ListViewItem item = scheduler.SelectedItems[0];
			Task task = (Task)item.Tag;
			if (task.Executing)
				return;

			//Edit the task.
			using (TaskPropertiesForm form = new TaskPropertiesForm())
			{
				form.Task = task;
				if (form.ShowDialog() == DialogResult.OK)
				{
					task = form.Task;

					//Update the list view
					UpdateTask(item);
				}
			}
		}

		/// <summary>
		/// Occurs when the user selects the Delete Task context menu item.
		/// </summary>
		/// <param name="sender">The menu item which generated this event.</param>
		/// <param name="e">Event argument.</param>
		private void deleteTaskToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DeleteSelectedTasks();
		}

		#region Item management
		/// <summary>
		/// Retrieves the ListViewItem for the given task.
		/// </summary>
		/// <param name="task">The task object whose list view entry is being sought.</param>
		/// <returns>A ListViewItem for the given task object.</returns>
		private ListViewItem GetTaskItem(Task task)
		{
			foreach (ListViewItem item in scheduler.Items)
				if (item.Tag == task)
					return item;

			return null;
		}

		/// <summary>
		/// Maintains the position of the progress bar.
		/// </summary>
		private void PositionProgressBar()
		{
			if (schedulerProgress.Tag == null)
				return;

			Rectangle rect = ((ListViewItem)schedulerProgress.Tag).SubItems[2].Bounds;
			schedulerProgress.Location = rect.Location;
			schedulerProgress.Size = rect.Size;
		}

		private void scheduler_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			e.DrawDefault = true;
			if (schedulerProgress.Tag != null)
				PositionProgressBar();
		}

		private void scheduler_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
		}
		#endregion
	}
}