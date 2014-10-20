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
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

using Eraser.Util;
using Eraser.Manager;
using Eraser.Plugins;
using Eraser.Plugins.Registrars;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Properties;

namespace Eraser
{
	public partial class MainForm : Form, INotificationSink
	{
		private BasePanel CurrPage;
		private SchedulerPanel SchedulerPage;
		private SettingsPanel SettingsPage;

		public MainForm()
		{
			InitializeComponent();
			SettingsPage = new SettingsPanel();
			SchedulerPage = new SchedulerPanel();
			contentPanel.Controls.Add(SchedulerPage);
			contentPanel.Controls.Add(SettingsPage);
			if (!IsHandleCreated)
				CreateHandle();

			Theming.ApplyTheme(this);
			Theming.ApplyTheme(notificationMenu);

			//We need to see if there are any tools to display
			foreach (IClientTool tool in Host.Instance.ClientTools)
				tool.RegisterTool(tbToolsMenu);
			if (tbToolsMenu.Items.Count == 0)
			{
				//There are none, hide the menu
				tbTools.Visible = false;
				tbToolsDropDown.Visible = false;
			}

			//We also need to see if we have any notifier classes we need to register.
			foreach (INotifier notifier in Host.Instance.Notifiers)
				notifier.Sink = this;
			Host.Instance.Notifiers.Registered += Notifier_Registered;

			//For every task we need to register the Task Started and Task Finished
			//event handlers for progress notifications
			foreach (Task task in Program.eraserClient.Tasks)
				OnTaskAdded(this, new TaskEventArgs(task));
			Program.eraserClient.TaskAdded += OnTaskAdded;
			Program.eraserClient.TaskDeleted += OnTaskDeleted;

			//Check if we have tasks running already.
			foreach (Task task in Program.eraserClient.Tasks)
				if (task.Executing)
				{
					OnTaskProcessing(task, EventArgs.Empty);
					break;
				}

			//Check the notification area context menu's minimise to tray item.
			hideWhenMinimisedToolStripMenuItem.Checked = EraserSettings.Get().HideWhenMinimised;

			//Set the docking style for each of the pages
			SchedulerPage.Dock = DockStyle.Fill;
			SettingsPage.Visible = false;

			//Show the default page.
			ChangePage(MainFormPage.Scheduler);
		}

		#region Notifications handling code
		/// <summary>
		/// Stores information about pending notifications.
		/// </summary>
		private struct NotificationInfo
		{
			public INotifier Source;
			public int Timeout;
			public ToolTipIcon Icon;
			public string Title;
			public string Message;
		}

		/// <summary>
		/// Diplays the given title, message and icon as a system notification area balloon.
		/// </summary>
		/// <param name="title">The title of the balloon.</param>
		/// <param name="message">The message to display.</param>
		/// <param name="icon">The icon to show.</param>
		public void ShowNotificationBalloon(string title, string message, ToolTipIcon icon)
		{
			NotificationInfo info;
			info.Source = null;
			info.Timeout = 0;
			info.Icon = icon;
			info.Title = title;
			info.Message = message;

			NotificationsQueue.Add(info);

			//Can we show the notification immediately?
			if (NotificationsQueue.Count == 1)
				ShowNextNotification();
		}

		public void ShowNotification(INotifier source, int timeout, ToolTipIcon icon,
			string title, string message)
		{
			NotificationInfo info;
			info.Source = source;
			info.Timeout = timeout;
			info.Icon = icon;
			info.Title = title;
			info.Message = message;

			NotificationsQueue.Add(info);

			//Can we show the notification immediately?
			if (NotificationsQueue.Count == 1)
				ShowNextNotification();
		}

		private void Notifier_Registered(object sender, EventArgs e)
		{
			((INotifier)sender).Sink = this;
		}

		private void ShowNextNotification()
		{
			Debug.Assert(NotificationsQueue.Count != 0);
			NotificationInfo info = NotificationsQueue[0];
			notificationIcon.ShowBalloonTip(info.Timeout, info.Title, info.Message, info.Icon);
		}

		private void notificationIcon_BalloonTipShown(object sender, EventArgs e)
		{
			Debug.Assert(NotificationsQueue.Count != 0);
			if (NotificationsQueue[0].Source != null)
				NotificationsQueue[0].Source.Shown(sender, e);
		}

		private void notificationIcon_BalloonTipClosed(object sender, EventArgs e)
		{
			Debug.Assert(NotificationsQueue.Count != 0);

			if (NotificationsQueue[0].Source != null)
				NotificationsQueue[0].Source.Closed(sender, e);
			NotificationsQueue.RemoveAt(0);

			if (NotificationsQueue.Count > 0)
				ShowNextNotification();
		}

		private void notificationIcon_BalloonTipClicked(object sender, EventArgs e)
		{
			Debug.Assert(NotificationsQueue.Count != 0);
			if (NotificationsQueue[0].Source != null)
				NotificationsQueue[0].Source.Clicked(sender, e);
		}

		/// <summary>
		/// The queue holding the list of notifications to be displayed sequentially.
		/// The notification being displayed is the first item in the list.
		/// </summary>
		private List<NotificationInfo> NotificationsQueue = new List<NotificationInfo>();
		#endregion

		/// <summary>
		/// Changes the active page displayed in the form.
		/// </summary>
		/// <param name="page">The new page to change to. No action is done when the
		/// current page is the same as the new page requested</param>
		public void ChangePage(MainFormPage page)
		{
			BasePanel oldPage = CurrPage;
			switch (page)
			{
				case MainFormPage.Scheduler:
					CurrPage = SchedulerPage;
					break;
				case MainFormPage.Settings:
					CurrPage = SettingsPage;
					break;
			}

			if (oldPage != CurrPage)
			{
				contentPanel.SuspendLayout();

				//Hide the old page before showing the new one
				if (oldPage != null)
					oldPage.Visible = false;

				//If the page is not set to dock, we need to specify the dimensions of the page
				//so it fits properly.
				if (CurrPage.Dock == DockStyle.None)
				{
					CurrPage.Anchor = AnchorStyles.Left | AnchorStyles.Right |
						AnchorStyles.Top;
					CurrPage.Left = 0;
					CurrPage.Top = 0;
					CurrPage.Width = contentPanel.Width;
				}

				//Show the new page then bring it to the top of the z-order.
				CurrPage.Visible = true;
				CurrPage.BringToFront();
				contentPanel.ResumeLayout();
			}
		}

		private static GraphicsPath CreateRoundRect(float X, float Y, float width,
			float height, float radius)
		{
			GraphicsPath result = new GraphicsPath();

			//Top line.
			result.AddLine(X + radius, Y, X + width - 2 * radius, Y);

			//Top-right corner
			result.AddArc(X + width - 2 * radius, Y, 2 * radius, 2 * radius, 270, 90);

			//Right line.
			result.AddLine(X + width, Y + radius, X + width, Y + height - 2 * radius);

			//Bottom-right corner
			result.AddArc(X + width - 2 * radius, Y + height - 2 * radius, 2 * radius, 2 * radius, 0, 90);

			//Bottom line.
			result.AddLine(X + width - 2 * radius, Y + height, X + radius, Y + height);

			//Bottom-left corner
			result.AddArc(X, Y + height - 2 *radius, 2 * radius, 2 * radius, 90, 90);

			//Left line
			result.AddLine(X, Y + height - 2 * radius, X, Y + radius);

			//Top-left corner
			result.AddArc(X, Y, 2 * radius, 2 * radius, 180, 90);
			result.CloseFigure();

			return result;
		}

		private void DrawBackground(Graphics dc)
		{
			//Draw the base background
			dc.FillRectangle(new SolidBrush(Color.FromArgb(unchecked((int)0xFF292929))),
				new Rectangle(new Point(0, 0), Size));

			//Then the side gradient
			dc.FillRectangle(new LinearGradientBrush(new Rectangle(0, 0, 338, Math.Max(1, ClientSize.Height)),
					Color.FromArgb(unchecked((int)0xFF363636)),
					Color.FromArgb(unchecked((int)0xFF292929)), 0.0),
				0, 0, 338, ClientSize.Height);

			//Draw the top background
			dc.FillRectangle(new SolidBrush(Color.FromArgb(unchecked((int)0xFF414141))),
				new Rectangle(0, 0, ClientSize.Width, 32));

			//The top gradient
			dc.DrawImage(Properties.Resources.BackgroundGradient, new Point(0, 0));

			dc.SmoothingMode = SmoothingMode.AntiAlias;
			dc.FillPath(Brushes.White, CreateRoundRect(11, 74, contentPanel.Width + 8, ClientSize.Height - 85, 3));
		}

		private void MainForm_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.SetClip(new Rectangle(0, 0, Width, Height), CombineMode.Intersect);
			DrawBackground(e.Graphics);
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			if (WindowState != FormWindowState.Minimized)
			{
				Bitmap bmp = new Bitmap(Width, Height);
				Graphics dc = Graphics.FromImage(bmp);
				DrawBackground(dc);

				CreateGraphics().DrawImage(bmp, new Point(0, 0));
			}
			else if (EraserSettings.Get().HideWhenMinimised)
			{
				Visible = false;
			}
		}

		private void tbSchedule_Click(object sender, EventArgs e)
		{
			ChangePage(MainFormPage.Scheduler);
		}

		private void tbSettings_Click(object sender, EventArgs e)
		{
			ChangePage(MainFormPage.Settings);
		}

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

		private void exportTaskListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.Filter = "Eraser 6 task lists (*.ersy)|*.ersy";
				dialog.DefaultExt = "ersy";
				dialog.OverwritePrompt = true;

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					using (FileStream stream = new FileStream(dialog.FileName,
						FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
					{
						Program.eraserClient.Tasks.SaveToStream(stream);
					}
				}
			}
		}

		private void importTaskListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.Filter = "Eraser 6 XML task lists (*.ersy)|*.ersy";
				dialog.DefaultExt = "ersy";

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					using (FileStream stream = new FileStream(dialog.FileName,
						FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						try
						{
							Program.eraserClient.Tasks.LoadFromStream(stream);
						}
						catch (InvalidDataException ex)
						{
							MessageBox.Show(S._("The task list could not be imported. The error " +
								"returned was: {0}", ex.Message), S._("Eraser"),
								MessageBoxButtons.OK, MessageBoxIcon.Error,
								MessageBoxDefaultButton.Button1,
								Localisation.IsRightToLeft(this) ?
									MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
						}
					}
				}
			}
		}

		private void tbHelp_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start(Path.Combine(Path.GetDirectoryName(
						Assembly.GetEntryAssembly().Location),
					"Eraser Documentation.pdf"));
			}
			catch (Win32Exception ex)
			{
				MessageBox.Show(this, S._("The Eraser documentation file could not be " +
					"opened. Check that Adobe Reader installed and that your Eraser " +
					"install is not corrupt.\n\nThe error returned was: {0}", ex.Message),
					S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button1, Localisation.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}
		}

		private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (UpdateForm form = new UpdateForm())
			{
				form.ShowDialog();
			}
		}

		private void aboutEraserToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (AboutForm form = new AboutForm(this))
			{
				form.ShowDialog();
			}
		}

		private void eraserLogo_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start("http://eraser.heidi.ie/");
			}
			catch (Win32Exception ex)
			{
				//We've got an error executing the the browser to pass the links: show an error
				//to the user.
				MessageBox.Show(S._("Could not open the required web page. The error returned " +
					"was: {0}", ex.Message), S._("Eraser"), MessageBoxButtons.OK,
					MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
					Localisation.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}
		}

		#region Task processing code (for notification area animation)
		void OnTaskAdded(object sender, TaskEventArgs e)
		{
			e.Task.TaskStarted += OnTaskProcessing;
			e.Task.TaskFinished += OnTaskProcessed;
		}

		void OnTaskDeleted(object sender, TaskEventArgs e)
		{
			e.Task.TaskStarted -= OnTaskProcessing;
			e.Task.TaskFinished -= OnTaskProcessed;
		}

		void OnTaskProcessing(object sender, EventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke((EventHandler)OnTaskProcessing, sender, e);
				return;
			}

			Task task = (Task)sender;
			string iconText = S._("Eraser") + " - " + S._("Processing:") + ' ' + task.ToString();
			if (iconText.Length >= 64)
				iconText = iconText.Remove(60) + "...";

			ProcessingAnimationFrame = 0;
			notificationIcon.Text = iconText;
			notificationIconTimer.Enabled = true;
		}

		void OnTaskProcessed(object sender, EventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke((EventHandler)OnTaskProcessed, sender, e);
				return;
			}

			//Reset the notification area icon.
			notificationIconTimer.Enabled = false;
			if (notificationIcon.Icon != null)
			{
				ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
				resources.ApplyResources(notificationIcon, "notificationIcon");
			}
		}

		private void notificationIconTimer_Tick(object sender, EventArgs e)
		{
			notificationIcon.Icon = ProcessingAnimationFrames[ProcessingAnimationFrame++];
			if (ProcessingAnimationFrame == ProcessingAnimationFrames.Length)
				ProcessingAnimationFrame = 0;
		}

		private int ProcessingAnimationFrame;
		private Icon[] ProcessingAnimationFrames = new Icon[] {
			Resources.NotifyBusy1,
			Resources.NotifyBusy2,
			Resources.NotifyBusy3,
			Resources.NotifyBusy4,
			Resources.NotifyBusy5,
			Resources.NotifyBusy4,
			Resources.NotifyBusy3,
			Resources.NotifyBusy2,
			Resources.NotifyBusy1
		};
		#endregion

		#region Minimise to tray code
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (EraserSettings.Get().HideWhenMinimised && e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				Visible = false;
			}
		}

		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			Application.Exit();
		}

		private void MainForm_VisibleChanged(object sender, EventArgs e)
		{
			if (Visible)
			{
				WindowState = FormWindowState.Normal;
				Activate();
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Visible = true;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void hideWhenMinimiseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EraserSettings.Get().HideWhenMinimised =
				hideWhenMinimisedToolStripMenuItem.Checked;
		}
		#endregion
	}

	public enum MainFormPage
	{
		Scheduler = 0,
		Settings
	}
}