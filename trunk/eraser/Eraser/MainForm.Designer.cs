/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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

namespace Eraser
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.contentPanel = new System.Windows.Forms.Panel();
            this.tbSchedule = new System.Windows.Forms.ToolStripMenuItem();
            this.notificationIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.notificationMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openEraserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.hideWhenMinimisedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.notificationIconTimer = new System.Windows.Forms.Timer(this.components);
            this.ToolBar = new Eraser.ToolBar();
            this.tbScheduleDropDown = new System.Windows.Forms.ToolStripMenuItem();
            this.tbScheduleMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.exportTaskListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importTaskListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tbTools = new System.Windows.Forms.ToolStripMenuItem();
            this.tbToolsDropDown = new System.Windows.Forms.ToolStripMenuItem();
            this.tbToolsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tbSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.tbHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.tbHelpDropDown = new System.Windows.Forms.ToolStripMenuItem();
            this.tbHelpMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.checkForUpdatesToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutEraserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkForUpdatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.eraserLogo = new System.Windows.Forms.PictureBox();
            this.notificationMenu.SuspendLayout();
            this.ToolBar.SuspendLayout();
            this.tbScheduleMenu.SuspendLayout();
            this.tbHelpMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.eraserLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // contentPanel
            // 
            resources.ApplyResources(this.contentPanel, "contentPanel");
            this.contentPanel.BackColor = System.Drawing.Color.White;
            this.contentPanel.Name = "contentPanel";
            // 
            // tbSchedule
            // 
            this.tbSchedule.Image = global::Eraser.Properties.Resources.ToolbarSchedule;
            this.tbSchedule.Name = "tbSchedule";
            this.tbSchedule.Padding = new System.Windows.Forms.Padding(0);
            resources.ApplyResources(this.tbSchedule, "tbSchedule");
            this.tbSchedule.Click += new System.EventHandler(this.tbSchedule_Click);
            // 
            // notificationIcon
            // 
            this.notificationIcon.ContextMenuStrip = this.notificationMenu;
            resources.ApplyResources(this.notificationIcon, "notificationIcon");
            this.notificationIcon.BalloonTipClicked += new System.EventHandler(this.notificationIcon_BalloonTipClicked);
            this.notificationIcon.BalloonTipClosed += new System.EventHandler(this.notificationIcon_BalloonTipClosed);
            this.notificationIcon.BalloonTipShown += new System.EventHandler(this.notificationIcon_BalloonTipShown);
            this.notificationIcon.DoubleClick += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // notificationMenu
            // 
            this.notificationMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openEraserToolStripMenuItem,
            this.toolStripMenuItem1,
            this.hideWhenMinimisedToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.notificationMenu.Name = "notificationMenu";
            resources.ApplyResources(this.notificationMenu, "notificationMenu");
            // 
            // openEraserToolStripMenuItem
            // 
            resources.ApplyResources(this.openEraserToolStripMenuItem, "openEraserToolStripMenuItem");
            this.openEraserToolStripMenuItem.Name = "openEraserToolStripMenuItem";
            this.openEraserToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // hideWhenMinimisedToolStripMenuItem
            // 
            this.hideWhenMinimisedToolStripMenuItem.CheckOnClick = true;
            this.hideWhenMinimisedToolStripMenuItem.Name = "hideWhenMinimisedToolStripMenuItem";
            resources.ApplyResources(this.hideWhenMinimisedToolStripMenuItem, "hideWhenMinimisedToolStripMenuItem");
            this.hideWhenMinimisedToolStripMenuItem.Click += new System.EventHandler(this.hideWhenMinimiseToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            resources.ApplyResources(this.exitToolStripMenuItem, "exitToolStripMenuItem");
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // notificationIconTimer
            // 
            this.notificationIconTimer.Tick += new System.EventHandler(this.notificationIconTimer_Tick);
            // 
            // ToolBar
            // 
            this.ToolBar.AllowItemReorder = true;
            this.ToolBar.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.ToolBar, "ToolBar");
            this.ToolBar.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.ToolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbSchedule,
            this.tbScheduleDropDown,
            this.tbTools,
            this.tbToolsDropDown,
            this.tbSettings,
            this.tbHelp,
            this.tbHelpDropDown});
            this.ToolBar.Name = "ToolBar";
            // 
            // tbScheduleDropDown
            // 
            this.tbScheduleDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbScheduleDropDown.DropDown = this.tbScheduleMenu;
            this.tbScheduleDropDown.Image = global::Eraser.Properties.Resources.ToolbarArrow;
            resources.ApplyResources(this.tbScheduleDropDown, "tbScheduleDropDown");
            this.tbScheduleDropDown.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.tbScheduleDropDown.Name = "tbScheduleDropDown";
            this.tbScheduleDropDown.Padding = new System.Windows.Forms.Padding(0);
            // 
            // tbScheduleMenu
            // 
            this.tbScheduleMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newTaskToolStripMenuItem,
            this.toolStripMenuItem2,
            this.exportTaskListToolStripMenuItem,
            this.importTaskListToolStripMenuItem});
            this.tbScheduleMenu.Name = "tbScheduleMenu";
            this.tbScheduleMenu.OwnerItem = this.tbScheduleDropDown;
            resources.ApplyResources(this.tbScheduleMenu, "tbScheduleMenu");
            // 
            // newTaskToolStripMenuItem
            // 
            this.newTaskToolStripMenuItem.Name = "newTaskToolStripMenuItem";
            resources.ApplyResources(this.newTaskToolStripMenuItem, "newTaskToolStripMenuItem");
            this.newTaskToolStripMenuItem.Click += new System.EventHandler(this.newTaskToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            // 
            // exportTaskListToolStripMenuItem
            // 
            this.exportTaskListToolStripMenuItem.Name = "exportTaskListToolStripMenuItem";
            resources.ApplyResources(this.exportTaskListToolStripMenuItem, "exportTaskListToolStripMenuItem");
            this.exportTaskListToolStripMenuItem.Click += new System.EventHandler(this.exportTaskListToolStripMenuItem_Click);
            // 
            // importTaskListToolStripMenuItem
            // 
            this.importTaskListToolStripMenuItem.Name = "importTaskListToolStripMenuItem";
            resources.ApplyResources(this.importTaskListToolStripMenuItem, "importTaskListToolStripMenuItem");
            this.importTaskListToolStripMenuItem.Click += new System.EventHandler(this.importTaskListToolStripMenuItem_Click);
            // 
            // tbTools
            // 
            this.tbTools.Name = "tbTools";
            resources.ApplyResources(this.tbTools, "tbTools");
            // 
            // tbToolsDropDown
            // 
            this.tbToolsDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbToolsDropDown.DropDown = this.tbToolsMenu;
            this.tbToolsDropDown.Image = global::Eraser.Properties.Resources.ToolbarArrow;
            resources.ApplyResources(this.tbToolsDropDown, "tbToolsDropDown");
            this.tbToolsDropDown.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.tbToolsDropDown.Name = "tbToolsDropDown";
            // 
            // tbToolsMenu
            // 
            this.tbToolsMenu.Name = "tbToolsMenu";
            this.tbToolsMenu.OwnerItem = this.tbToolsDropDown;
            resources.ApplyResources(this.tbToolsMenu, "tbToolsMenu");
            // 
            // tbSettings
            // 
            this.tbSettings.Image = global::Eraser.Properties.Resources.ToolbarSettings;
            this.tbSettings.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.tbSettings.Name = "tbSettings";
            this.tbSettings.Padding = new System.Windows.Forms.Padding(0);
            resources.ApplyResources(this.tbSettings, "tbSettings");
            this.tbSettings.Click += new System.EventHandler(this.tbSettings_Click);
            // 
            // tbHelp
            // 
            this.tbHelp.Image = global::Eraser.Properties.Resources.ToolbarHelp;
            this.tbHelp.Name = "tbHelp";
            this.tbHelp.Padding = new System.Windows.Forms.Padding(0);
            resources.ApplyResources(this.tbHelp, "tbHelp");
            this.tbHelp.Click += new System.EventHandler(this.tbHelp_Click);
            // 
            // tbHelpDropDown
            // 
            this.tbHelpDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbHelpDropDown.DropDown = this.tbHelpMenu;
            this.tbHelpDropDown.Image = global::Eraser.Properties.Resources.ToolbarArrow;
            resources.ApplyResources(this.tbHelpDropDown, "tbHelpDropDown");
            this.tbHelpDropDown.Name = "tbHelpDropDown";
            this.tbHelpDropDown.Padding = new System.Windows.Forms.Padding(0);
            // 
            // tbHelpMenu
            // 
            this.tbHelpMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.checkForUpdatesToolStripMenuItem1,
            this.aboutEraserToolStripMenuItem});
            this.tbHelpMenu.Name = "tbHelpMenu";
            this.tbHelpMenu.OwnerItem = this.tbHelpDropDown;
            resources.ApplyResources(this.tbHelpMenu, "tbHelpMenu");
            // 
            // checkForUpdatesToolStripMenuItem1
            // 
            this.checkForUpdatesToolStripMenuItem1.Name = "checkForUpdatesToolStripMenuItem1";
            resources.ApplyResources(this.checkForUpdatesToolStripMenuItem1, "checkForUpdatesToolStripMenuItem1");
            this.checkForUpdatesToolStripMenuItem1.Click += new System.EventHandler(this.checkForUpdatesToolStripMenuItem_Click);
            // 
            // aboutEraserToolStripMenuItem
            // 
            this.aboutEraserToolStripMenuItem.Name = "aboutEraserToolStripMenuItem";
            resources.ApplyResources(this.aboutEraserToolStripMenuItem, "aboutEraserToolStripMenuItem");
            this.aboutEraserToolStripMenuItem.Click += new System.EventHandler(this.aboutEraserToolStripMenuItem_Click);
            // 
            // checkForUpdatesToolStripMenuItem
            // 
            this.checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            resources.ApplyResources(this.checkForUpdatesToolStripMenuItem, "checkForUpdatesToolStripMenuItem");
            this.checkForUpdatesToolStripMenuItem.Click += new System.EventHandler(this.checkForUpdatesToolStripMenuItem_Click);
            // 
            // eraserLogo
            // 
            resources.ApplyResources(this.eraserLogo, "eraserLogo");
            this.eraserLogo.BackColor = System.Drawing.Color.Transparent;
            this.eraserLogo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.eraserLogo.Image = global::Eraser.Properties.Resources.Dialog_Logo62f;
            this.eraserLogo.Name = "eraserLogo";
            this.eraserLogo.TabStop = false;
            this.eraserLogo.Click += new System.EventHandler(this.eraserLogo_Click);
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(41)))), ((int)(((byte)(41)))));
            this.Controls.Add(this.eraserLogo);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.ToolBar);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.ToolBar;
            this.Name = "MainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.VisibleChanged += new System.EventHandler(this.MainForm_VisibleChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.notificationMenu.ResumeLayout(false);
            this.ToolBar.ResumeLayout(false);
            this.ToolBar.PerformLayout();
            this.tbScheduleMenu.ResumeLayout(false);
            this.tbHelpMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.eraserLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel contentPanel;
		private System.Windows.Forms.NotifyIcon notificationIcon;
		private System.Windows.Forms.Timer notificationIconTimer;
		private System.Windows.Forms.ContextMenuStrip notificationMenu;
		private System.Windows.Forms.ToolStripMenuItem openEraserToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem hideWhenMinimisedToolStripMenuItem;
		private ToolBar ToolBar;
		private System.Windows.Forms.ToolStripMenuItem tbSchedule;
		private System.Windows.Forms.ToolStripMenuItem tbSettings;
		private System.Windows.Forms.ToolStripMenuItem checkForUpdatesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tbHelp;
		private System.Windows.Forms.ToolStripMenuItem tbScheduleDropDown;
		private System.Windows.Forms.ToolStripMenuItem tbHelpDropDown;
		private System.Windows.Forms.ContextMenuStrip tbScheduleMenu;
		private System.Windows.Forms.ToolStripMenuItem newTaskToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip tbHelpMenu;
		private System.Windows.Forms.ToolStripMenuItem checkForUpdatesToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem aboutEraserToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem exportTaskListToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem importTaskListToolStripMenuItem;
		private System.Windows.Forms.PictureBox eraserLogo;
		private System.Windows.Forms.ToolStripMenuItem tbTools;
		private System.Windows.Forms.ToolStripMenuItem tbToolsDropDown;
		private System.Windows.Forms.ContextMenuStrip tbToolsMenu;
	}
}
