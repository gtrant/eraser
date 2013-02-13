/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Eraser.Manager;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Util;
using System.Globalization;

namespace Eraser
{
	public partial class ProgressForm : Form
	{
		private Task task;
		private DateTime lastUpdate;

		public ProgressForm(Task task)
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
			this.task = task;
			this.lastUpdate = DateTime.Now;
			this.ActiveControl = hide;

			//Register the event handlers
			jobTitle.Text = task.ToString();
			task.TaskFinished += task_TaskFinished;

			//Set the current progress
			UpdateProgress();
		}

		private void ProgressForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			task.TaskFinished -= task_TaskFinished;
		}

		private void progressTimer_Tick(object sender, EventArgs e)
		{
			UpdateProgress();
		}

		private void task_TaskFinished(object sender, EventArgs e)
		{
			if (IsDisposed || !IsHandleCreated)
				return;
			if (InvokeRequired)
			{
				Invoke((EventHandler)task_TaskFinished, sender, e);
				return;
			}

			//Stop the progress timer
			progressTimer.Stop();

			//Update the UI. Set everything to 100%
			Task task = (Task)sender;
			timeLeft.Text = item.Text = pass.Text = string.Empty;
			overallProgressLbl.Text = S._("Total: {0,2:#0.00%}", 1.0);
			overallProgress.Value = overallProgress.Maximum;
			itemProgressLbl.Text = "100%";
			itemProgress.Style = ProgressBarStyle.Continuous;
			itemProgress.Value = itemProgress.Maximum;

			//Inform the user on the status of the task.
			LogLevel highestLevel = task.Log.Last().Highest;
			switch (highestLevel)
			{
				case LogLevel.Warning:
					status.Text = S._("Completed with warnings");
					break;
				case LogLevel.Error:
					status.Text = S._("Completed with errors");
					break;
				case LogLevel.Fatal:
					status.Text = S._("Not completed");
					break;
				default:
					status.Text = S._("Completed");
					break;
			}

			//Change the Stop button to be a Close button and the Hide button
			//to be disabled
			hide.Enabled = false;
			stop.Text = S._("Close");
		}

		private void hide_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void stop_Click(object sender, EventArgs e)
		{
			if (task.Executing)
				task.Cancel();
			Close();
		}

		private void UpdateProgress()
		{
			if (!task.Executing)
			{
				//The task is done! Bail out and let the completion handler reset the UI
				return;
			}

			//Get the name of the current erasure target to display the overall status
			SteppedProgressManager taskProgress = task.Progress;
			ErasureTargetProgressManagerStep taskStep =
				(ErasureTargetProgressManagerStep)taskProgress.CurrentStep;
			if (taskStep == null)
				return;
			else if (!string.IsNullOrEmpty(taskStep.Name))
				status.Text = taskStep.Name;
			else
				status.Text = S._("Erasing...");

			//The get the current step of the target to set the current item name
			SteppedProgressManagerStepBase targetStep =
				(SteppedProgressManagerStepBase)taskStep.Target.Progress.CurrentStep;
			if (targetStep == null)
				return;
			else if (!string.IsNullOrEmpty(targetStep.Name))
				item.Text = WrapItemName(targetStep.Name);

			//Determine if the tag information of the step's progress manager is an
			//object array or a string.
			ProgressManagerBase targetStepProgress = targetStep.Progress;
			{
				object tag = targetStepProgress.Tag;
				if (tag == null)
				{
					if (string.IsNullOrEmpty(targetStep.Name))
						item.Text = string.Empty;
				}
				else if (tag.GetType() == typeof(string))
					item.Text = (string)tag;
				else if (tag.GetType() == typeof(int[]))
					pass.Text = S._("{0} out of {1}", ((int[])tag)[0], ((int[])tag)[1]);
			}

			if (targetStepProgress.TimeLeft >= TimeSpan.Zero)
				timeLeft.Text = S._("About {0} left", RoundToSeconds(targetStepProgress.TimeLeft));
			else
				timeLeft.Text = S._("Unknown");

			if (!targetStepProgress.ProgressIndeterminate)
			{
				itemProgress.Style = ProgressBarStyle.Continuous;
				itemProgress.Value = (int)(targetStepProgress.Progress * 1000);
				itemProgressLbl.Text = targetStepProgress.Progress.ToString("#0%",
					CultureInfo.CurrentCulture);
			}
			else
			{
				itemProgress.Style = ProgressBarStyle.Marquee;
				itemProgressLbl.Text = string.Empty;
			}

			if (!taskProgress.ProgressIndeterminate)
			{
				overallProgress.Style = ProgressBarStyle.Continuous;
				overallProgress.Value = (int)(taskProgress.Progress * 1000);
				overallProgressLbl.Text = S._("Total: {0,2:#0.00%}", taskProgress.Progress);
			}
			else
			{
				overallProgress.Style = ProgressBarStyle.Marquee;
				overallProgressLbl.Text = S._("Total: Unknown");
			}
		}

		private string WrapItemName(string itemName)
		{
			StringBuilder result = new StringBuilder(itemName.Length);
			using (Graphics g = item.CreateGraphics())
			{
				//Split the long file name into lines which fit into the width of the label
				while (itemName.Length > 0)
				{
					int chars = 0;
					int lines = 0;
					g.MeasureString(itemName, item.Font, new SizeF(item.Width - 2, 15),
						StringFormat.GenericDefault, out chars, out lines);

					result.AppendLine(itemName.Substring(0, chars));
					itemName = itemName.Remove(0, chars);
				}
			}

			return result.ToString();
		}

		private static TimeSpan RoundToSeconds(TimeSpan span)
		{
			return new TimeSpan(span.Ticks - span.Ticks % TimeSpan.TicksPerSecond);
		}
	}
}
