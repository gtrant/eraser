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
	partial class UpdateForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
            this.updateListDownloader = new System.ComponentModel.BackgroundWorker();
            this.updatesPanel = new System.Windows.Forms.Panel();
            this.updatesLv = new System.Windows.Forms.ListView();
            this.updatesLvNameCol = new System.Windows.Forms.ColumnHeader();
            this.updatesLvVersionCol = new System.Windows.Forms.ColumnHeader();
            this.updatesLvPublisherCol = new System.Windows.Forms.ColumnHeader();
            this.updatesLvFilesizeCol = new System.Windows.Forms.ColumnHeader();
            this.updatesBtn = new System.Windows.Forms.Button();
            this.updatesLbl = new System.Windows.Forms.Label();
            this.progressPanel = new System.Windows.Forms.Panel();
            this.progressCancelBtn = new System.Windows.Forms.Button();
            this.progressExplainLbl = new System.Windows.Forms.Label();
            this.progressProgressLbl = new System.Windows.Forms.Label();
            this.progressPb = new System.Windows.Forms.ProgressBar();
            this.progressLbl = new System.Windows.Forms.Label();
            this.downloader = new System.ComponentModel.BackgroundWorker();
            this.downloadingPnl = new System.Windows.Forms.Panel();
            this.downloadingCancelBtn = new System.Windows.Forms.Button();
            this.downloadingOverallPb = new System.Windows.Forms.ProgressBar();
            this.downloadingOverallLbl = new System.Windows.Forms.Label();
            this.downloadingItemPb = new System.Windows.Forms.ProgressBar();
            this.downloadingItemLbl = new System.Windows.Forms.Label();
            this.downloadingLv = new System.Windows.Forms.ListView();
            this.downloadingLvColName = new System.Windows.Forms.ColumnHeader();
            this.downloadingLvColAmount = new System.Windows.Forms.ColumnHeader();
            this.updatesImageList = new System.Windows.Forms.ImageList(this.components);
            this.downloadingLbl = new System.Windows.Forms.Label();
            this.installingPnl = new System.Windows.Forms.Panel();
            this.installingLv = new System.Windows.Forms.ListView();
            this.installingLvNameCol = new System.Windows.Forms.ColumnHeader();
            this.installingLvStatusCol = new System.Windows.Forms.ColumnHeader();
            this.installingLbl = new System.Windows.Forms.Label();
            this.installer = new System.ComponentModel.BackgroundWorker();
            this.updatesPanel.SuspendLayout();
            this.progressPanel.SuspendLayout();
            this.downloadingPnl.SuspendLayout();
            this.installingPnl.SuspendLayout();
            this.SuspendLayout();
            // 
            // updateListDownloader
            // 
            this.updateListDownloader.WorkerSupportsCancellation = true;
            this.updateListDownloader.DoWork += new System.ComponentModel.DoWorkEventHandler(this.updateListDownloader_DoWork);
            this.updateListDownloader.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.updateListDownloader_RunWorkerCompleted);
            // 
            // updatesPanel
            // 
            this.updatesPanel.AccessibleDescription = null;
            this.updatesPanel.AccessibleName = null;
            resources.ApplyResources(this.updatesPanel, "updatesPanel");
            this.updatesPanel.BackColor = System.Drawing.SystemColors.Control;
            this.updatesPanel.BackgroundImage = null;
            this.updatesPanel.Controls.Add(this.updatesLv);
            this.updatesPanel.Controls.Add(this.updatesBtn);
            this.updatesPanel.Controls.Add(this.updatesLbl);
            this.updatesPanel.Font = null;
            this.updatesPanel.Name = "updatesPanel";
            // 
            // updatesLv
            // 
            this.updatesLv.AccessibleDescription = null;
            this.updatesLv.AccessibleName = null;
            resources.ApplyResources(this.updatesLv, "updatesLv");
            this.updatesLv.BackgroundImage = null;
            this.updatesLv.CheckBoxes = true;
            this.updatesLv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.updatesLvNameCol,
            this.updatesLvVersionCol,
            this.updatesLvPublisherCol,
            this.updatesLvFilesizeCol});
            this.updatesLv.Font = null;
            this.updatesLv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.updatesLv.Name = "updatesLv";
            this.updatesLv.UseCompatibleStateImageBehavior = false;
            this.updatesLv.View = System.Windows.Forms.View.Details;
            this.updatesLv.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.updatesLv_ItemChecked);
            // 
            // updatesLvNameCol
            // 
            resources.ApplyResources(this.updatesLvNameCol, "updatesLvNameCol");
            // 
            // updatesLvVersionCol
            // 
            resources.ApplyResources(this.updatesLvVersionCol, "updatesLvVersionCol");
            // 
            // updatesLvPublisherCol
            // 
            resources.ApplyResources(this.updatesLvPublisherCol, "updatesLvPublisherCol");
            // 
            // updatesLvFilesizeCol
            // 
            resources.ApplyResources(this.updatesLvFilesizeCol, "updatesLvFilesizeCol");
            // 
            // updatesBtn
            // 
            this.updatesBtn.AccessibleDescription = null;
            this.updatesBtn.AccessibleName = null;
            resources.ApplyResources(this.updatesBtn, "updatesBtn");
            this.updatesBtn.BackgroundImage = null;
            this.updatesBtn.Font = null;
            this.updatesBtn.Name = "updatesBtn";
            this.updatesBtn.UseVisualStyleBackColor = true;
            this.updatesBtn.Click += new System.EventHandler(this.updatesBtn_Click);
            // 
            // updatesLbl
            // 
            this.updatesLbl.AccessibleDescription = null;
            this.updatesLbl.AccessibleName = null;
            resources.ApplyResources(this.updatesLbl, "updatesLbl");
            this.updatesLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.updatesLbl.Name = "updatesLbl";
            // 
            // progressPanel
            // 
            this.progressPanel.AccessibleDescription = null;
            this.progressPanel.AccessibleName = null;
            resources.ApplyResources(this.progressPanel, "progressPanel");
            this.progressPanel.BackgroundImage = null;
            this.progressPanel.Controls.Add(this.progressCancelBtn);
            this.progressPanel.Controls.Add(this.progressExplainLbl);
            this.progressPanel.Controls.Add(this.progressProgressLbl);
            this.progressPanel.Controls.Add(this.progressPb);
            this.progressPanel.Controls.Add(this.progressLbl);
            this.progressPanel.Font = null;
            this.progressPanel.Name = "progressPanel";
            this.progressPanel.UseWaitCursor = true;
            // 
            // progressCancelBtn
            // 
            this.progressCancelBtn.AccessibleDescription = null;
            this.progressCancelBtn.AccessibleName = null;
            resources.ApplyResources(this.progressCancelBtn, "progressCancelBtn");
            this.progressCancelBtn.BackgroundImage = null;
            this.progressCancelBtn.Font = null;
            this.progressCancelBtn.Name = "progressCancelBtn";
            this.progressCancelBtn.UseVisualStyleBackColor = true;
            this.progressCancelBtn.UseWaitCursor = true;
            this.progressCancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // progressExplainLbl
            // 
            this.progressExplainLbl.AccessibleDescription = null;
            this.progressExplainLbl.AccessibleName = null;
            resources.ApplyResources(this.progressExplainLbl, "progressExplainLbl");
            this.progressExplainLbl.Font = null;
            this.progressExplainLbl.Name = "progressExplainLbl";
            this.progressExplainLbl.UseWaitCursor = true;
            // 
            // progressProgressLbl
            // 
            this.progressProgressLbl.AccessibleDescription = null;
            this.progressProgressLbl.AccessibleName = null;
            resources.ApplyResources(this.progressProgressLbl, "progressProgressLbl");
            this.progressProgressLbl.Font = null;
            this.progressProgressLbl.Name = "progressProgressLbl";
            this.progressProgressLbl.UseWaitCursor = true;
            // 
            // progressPb
            // 
            this.progressPb.AccessibleDescription = null;
            this.progressPb.AccessibleName = null;
            resources.ApplyResources(this.progressPb, "progressPb");
            this.progressPb.BackgroundImage = null;
            this.progressPb.Font = null;
            this.progressPb.Name = "progressPb";
            this.progressPb.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressPb.UseWaitCursor = true;
            // 
            // progressLbl
            // 
            this.progressLbl.AccessibleDescription = null;
            this.progressLbl.AccessibleName = null;
            resources.ApplyResources(this.progressLbl, "progressLbl");
            this.progressLbl.ForeColor = System.Drawing.SystemColors.ControlText;
            this.progressLbl.Name = "progressLbl";
            this.progressLbl.UseWaitCursor = true;
            // 
            // downloader
            // 
            this.downloader.WorkerSupportsCancellation = true;
            this.downloader.DoWork += new System.ComponentModel.DoWorkEventHandler(this.downloader_DoWork);
            this.downloader.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.downloader_RunWorkerCompleted);
            // 
            // downloadingPnl
            // 
            this.downloadingPnl.AccessibleDescription = null;
            this.downloadingPnl.AccessibleName = null;
            resources.ApplyResources(this.downloadingPnl, "downloadingPnl");
            this.downloadingPnl.BackgroundImage = null;
            this.downloadingPnl.Controls.Add(this.downloadingCancelBtn);
            this.downloadingPnl.Controls.Add(this.downloadingOverallPb);
            this.downloadingPnl.Controls.Add(this.downloadingOverallLbl);
            this.downloadingPnl.Controls.Add(this.downloadingItemPb);
            this.downloadingPnl.Controls.Add(this.downloadingItemLbl);
            this.downloadingPnl.Controls.Add(this.downloadingLv);
            this.downloadingPnl.Controls.Add(this.downloadingLbl);
            this.downloadingPnl.Font = null;
            this.downloadingPnl.Name = "downloadingPnl";
            // 
            // downloadingCancelBtn
            // 
            this.downloadingCancelBtn.AccessibleDescription = null;
            this.downloadingCancelBtn.AccessibleName = null;
            resources.ApplyResources(this.downloadingCancelBtn, "downloadingCancelBtn");
            this.downloadingCancelBtn.BackgroundImage = null;
            this.downloadingCancelBtn.Font = null;
            this.downloadingCancelBtn.Name = "downloadingCancelBtn";
            this.downloadingCancelBtn.UseVisualStyleBackColor = true;
            this.downloadingCancelBtn.UseWaitCursor = true;
            this.downloadingCancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // downloadingOverallPb
            // 
            this.downloadingOverallPb.AccessibleDescription = null;
            this.downloadingOverallPb.AccessibleName = null;
            resources.ApplyResources(this.downloadingOverallPb, "downloadingOverallPb");
            this.downloadingOverallPb.BackgroundImage = null;
            this.downloadingOverallPb.Font = null;
            this.downloadingOverallPb.Name = "downloadingOverallPb";
            this.downloadingOverallPb.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // downloadingOverallLbl
            // 
            this.downloadingOverallLbl.AccessibleDescription = null;
            this.downloadingOverallLbl.AccessibleName = null;
            resources.ApplyResources(this.downloadingOverallLbl, "downloadingOverallLbl");
            this.downloadingOverallLbl.Font = null;
            this.downloadingOverallLbl.Name = "downloadingOverallLbl";
            // 
            // downloadingItemPb
            // 
            this.downloadingItemPb.AccessibleDescription = null;
            this.downloadingItemPb.AccessibleName = null;
            resources.ApplyResources(this.downloadingItemPb, "downloadingItemPb");
            this.downloadingItemPb.BackgroundImage = null;
            this.downloadingItemPb.Font = null;
            this.downloadingItemPb.Name = "downloadingItemPb";
            this.downloadingItemPb.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // downloadingItemLbl
            // 
            this.downloadingItemLbl.AccessibleDescription = null;
            this.downloadingItemLbl.AccessibleName = null;
            resources.ApplyResources(this.downloadingItemLbl, "downloadingItemLbl");
            this.downloadingItemLbl.Font = null;
            this.downloadingItemLbl.Name = "downloadingItemLbl";
            // 
            // downloadingLv
            // 
            this.downloadingLv.AccessibleDescription = null;
            this.downloadingLv.AccessibleName = null;
            resources.ApplyResources(this.downloadingLv, "downloadingLv");
            this.downloadingLv.BackgroundImage = null;
            this.downloadingLv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.downloadingLvColName,
            this.downloadingLvColAmount});
            this.downloadingLv.Font = null;
            this.downloadingLv.FullRowSelect = true;
            this.downloadingLv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.downloadingLv.Name = "downloadingLv";
            this.downloadingLv.SmallImageList = this.updatesImageList;
            this.downloadingLv.UseCompatibleStateImageBehavior = false;
            this.downloadingLv.View = System.Windows.Forms.View.Details;
            // 
            // downloadingLvColName
            // 
            resources.ApplyResources(this.downloadingLvColName, "downloadingLvColName");
            // 
            // downloadingLvColAmount
            // 
            resources.ApplyResources(this.downloadingLvColAmount, "downloadingLvColAmount");
            // 
            // updatesImageList
            // 
            this.updatesImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("updatesImageList.ImageStream")));
            this.updatesImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.updatesImageList.Images.SetKeyName(0, "Downloading.png");
            this.updatesImageList.Images.SetKeyName(1, "Installing.png");
            this.updatesImageList.Images.SetKeyName(2, "Installed.png");
            this.updatesImageList.Images.SetKeyName(3, "Error.png");
            // 
            // downloadingLbl
            // 
            this.downloadingLbl.AccessibleDescription = null;
            this.downloadingLbl.AccessibleName = null;
            resources.ApplyResources(this.downloadingLbl, "downloadingLbl");
            this.downloadingLbl.Name = "downloadingLbl";
            // 
            // installingPnl
            // 
            this.installingPnl.AccessibleDescription = null;
            this.installingPnl.AccessibleName = null;
            resources.ApplyResources(this.installingPnl, "installingPnl");
            this.installingPnl.BackgroundImage = null;
            this.installingPnl.Controls.Add(this.installingLv);
            this.installingPnl.Controls.Add(this.installingLbl);
            this.installingPnl.Font = null;
            this.installingPnl.Name = "installingPnl";
            this.installingPnl.UseWaitCursor = true;
            // 
            // installingLv
            // 
            this.installingLv.AccessibleDescription = null;
            this.installingLv.AccessibleName = null;
            resources.ApplyResources(this.installingLv, "installingLv");
            this.installingLv.BackgroundImage = null;
            this.installingLv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.installingLvNameCol,
            this.installingLvStatusCol});
            this.installingLv.Font = null;
            this.installingLv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.installingLv.Name = "installingLv";
            this.installingLv.ShowItemToolTips = true;
            this.installingLv.SmallImageList = this.updatesImageList;
            this.installingLv.UseCompatibleStateImageBehavior = false;
            this.installingLv.UseWaitCursor = true;
            this.installingLv.View = System.Windows.Forms.View.Details;
            // 
            // installingLvNameCol
            // 
            resources.ApplyResources(this.installingLvNameCol, "installingLvNameCol");
            // 
            // installingLvStatusCol
            // 
            resources.ApplyResources(this.installingLvStatusCol, "installingLvStatusCol");
            // 
            // installingLbl
            // 
            this.installingLbl.AccessibleDescription = null;
            this.installingLbl.AccessibleName = null;
            resources.ApplyResources(this.installingLbl, "installingLbl");
            this.installingLbl.Name = "installingLbl";
            this.installingLbl.UseWaitCursor = true;
            // 
            // installer
            // 
            this.installer.WorkerSupportsCancellation = true;
            this.installer.DoWork += new System.ComponentModel.DoWorkEventHandler(this.installer_DoWork);
            this.installer.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.installer_RunWorkerCompleted);
            // 
            // UpdateForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = null;
            this.Controls.Add(this.updatesPanel);
            this.Controls.Add(this.progressPanel);
            this.Controls.Add(this.installingPnl);
            this.Controls.Add(this.downloadingPnl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = null;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateForm";
            this.ShowInTaskbar = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UpdateForm_FormClosing);
            this.updatesPanel.ResumeLayout(false);
            this.updatesPanel.PerformLayout();
            this.progressPanel.ResumeLayout(false);
            this.progressPanel.PerformLayout();
            this.downloadingPnl.ResumeLayout(false);
            this.downloadingPnl.PerformLayout();
            this.installingPnl.ResumeLayout(false);
            this.installingPnl.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.ComponentModel.BackgroundWorker updateListDownloader;
		private System.Windows.Forms.Panel updatesPanel;
		private System.Windows.Forms.ListView updatesLv;
		private System.Windows.Forms.Button updatesBtn;
		private System.Windows.Forms.Label updatesLbl;
		private System.Windows.Forms.ColumnHeader updatesLvNameCol;
		private System.Windows.Forms.ColumnHeader updatesLvVersionCol;
		private System.Windows.Forms.ColumnHeader updatesLvPublisherCol;
		private System.Windows.Forms.ColumnHeader updatesLvFilesizeCol;
		private System.Windows.Forms.Panel progressPanel;
		private System.Windows.Forms.Label progressProgressLbl;
		private System.Windows.Forms.ProgressBar progressPb;
		private System.Windows.Forms.Label progressLbl;
		private System.ComponentModel.BackgroundWorker downloader;
		private System.Windows.Forms.Panel downloadingPnl;
		private System.Windows.Forms.ProgressBar downloadingOverallPb;
		private System.Windows.Forms.Label downloadingOverallLbl;
		private System.Windows.Forms.ProgressBar downloadingItemPb;
		private System.Windows.Forms.Label downloadingItemLbl;
		private System.Windows.Forms.ListView downloadingLv;
		private System.Windows.Forms.Label downloadingLbl;
		private System.Windows.Forms.ColumnHeader downloadingLvColName;
		private System.Windows.Forms.ColumnHeader downloadingLvColAmount;
		private System.Windows.Forms.ImageList updatesImageList;
		private System.Windows.Forms.Panel installingPnl;
		private System.Windows.Forms.Label installingLbl;
		private System.Windows.Forms.ListView installingLv;
		private System.Windows.Forms.ColumnHeader installingLvNameCol;
		private System.ComponentModel.BackgroundWorker installer;
		private System.Windows.Forms.ColumnHeader installingLvStatusCol;
		private System.Windows.Forms.Label progressExplainLbl;
		private System.Windows.Forms.Button progressCancelBtn;
		private System.Windows.Forms.Button downloadingCancelBtn;
	}
}