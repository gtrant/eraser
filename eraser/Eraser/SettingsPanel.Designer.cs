/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net> @10/18/2008
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
	partial class SettingsPanel
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsPanel));
			this.ui = new Eraser.LightGroup();
			this.uiContextMenu = new System.Windows.Forms.CheckBox();
			this.lockedForceUnlock = new System.Windows.Forms.CheckBox();
			this.erase = new Eraser.LightGroup();
			this.eraseFilesMethodLbl = new System.Windows.Forms.Label();
			this.eraseDriveMethodLbl = new System.Windows.Forms.Label();
			this.eraseFilesMethod = new System.Windows.Forms.ComboBox();
			this.eraseDriveMethod = new System.Windows.Forms.ComboBox();
			this.plugins = new Eraser.LightGroup();
			this.pluginsManager = new System.Windows.Forms.ListView();
			this.pluginsManagerColName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.pluginsManagerColAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.pluginsManagerColVersion = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.pluginsManagerColPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.pluginsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pluginsManagerImageList = new System.Windows.Forms.ImageList(this.components);
			this.scheduler = new Eraser.LightGroup();
			this.schedulerMissed = new System.Windows.Forms.Label();
			this.schedulerMissedImmediate = new System.Windows.Forms.RadioButton();
			this.schedulerMissedIgnore = new System.Windows.Forms.RadioButton();
			this.saveSettings = new System.Windows.Forms.Button();
			this.erasePRNGLbl = new System.Windows.Forms.Label();
			this.erasePRNG = new System.Windows.Forms.ComboBox();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.plausibleDeniability = new System.Windows.Forms.CheckBox();
			this.uiLanguageLbl = new System.Windows.Forms.Label();
			this.uiLanguage = new System.Windows.Forms.ComboBox();
			this.plausibleDeniabilityFiles = new System.Windows.Forms.ListBox();
			this.plausibleDeniabilityFilesAddFile = new System.Windows.Forms.Button();
			this.plausibleDeniabilityFilesRemove = new System.Windows.Forms.Button();
			this.plausibleDeniabilityFilesAddFolder = new System.Windows.Forms.Button();
			this.schedulerClearCompleted = new System.Windows.Forms.CheckBox();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			((System.ComponentModel.ISupportInitialize)(this.titleIcon)).BeginInit();
			this.content.SuspendLayout();
			this.pluginsMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// titleLabel
			// 
			this.errorProvider.SetIconAlignment(this.titleLabel, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("titleLabel.IconAlignment"))));
			resources.ApplyResources(this.titleLabel, "titleLabel");
			// 
			// titleIcon
			// 
			this.errorProvider.SetIconAlignment(this.titleIcon, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("titleIcon.IconAlignment"))));
			this.titleIcon.Image = global::Eraser.Properties.Resources.ToolbarSettings;
			// 
			// content
			// 
			this.content.Controls.Add(this.schedulerClearCompleted);
			this.content.Controls.Add(this.plausibleDeniabilityFilesAddFolder);
			this.content.Controls.Add(this.plausibleDeniabilityFilesRemove);
			this.content.Controls.Add(this.plausibleDeniabilityFilesAddFile);
			this.content.Controls.Add(this.plausibleDeniabilityFiles);
			this.content.Controls.Add(this.uiLanguage);
			this.content.Controls.Add(this.uiLanguageLbl);
			this.content.Controls.Add(this.plausibleDeniability);
			this.content.Controls.Add(this.erasePRNG);
			this.content.Controls.Add(this.erasePRNGLbl);
			this.content.Controls.Add(this.schedulerMissedIgnore);
			this.content.Controls.Add(this.schedulerMissedImmediate);
			this.content.Controls.Add(this.schedulerMissed);
			this.content.Controls.Add(this.scheduler);
			this.content.Controls.Add(this.pluginsManager);
			this.content.Controls.Add(this.plugins);
			this.content.Controls.Add(this.eraseDriveMethod);
			this.content.Controls.Add(this.eraseFilesMethod);
			this.content.Controls.Add(this.eraseDriveMethodLbl);
			this.content.Controls.Add(this.eraseFilesMethodLbl);
			this.content.Controls.Add(this.erase);
			this.content.Controls.Add(this.lockedForceUnlock);
			this.content.Controls.Add(this.uiContextMenu);
			this.content.Controls.Add(this.ui);
			this.errorProvider.SetIconAlignment(this.content, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("content.IconAlignment"))));
			resources.ApplyResources(this.content, "content");
			// 
			// ui
			// 
			resources.ApplyResources(this.ui, "ui");
			this.errorProvider.SetIconAlignment(this.ui, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("ui.IconAlignment"))));
			this.ui.Name = "ui";
			// 
			// uiContextMenu
			// 
			resources.ApplyResources(this.uiContextMenu, "uiContextMenu");
			this.uiContextMenu.Checked = true;
			this.uiContextMenu.CheckState = System.Windows.Forms.CheckState.Checked;
			this.errorProvider.SetIconAlignment(this.uiContextMenu, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("uiContextMenu.IconAlignment"))));
			this.uiContextMenu.Name = "uiContextMenu";
			this.uiContextMenu.UseVisualStyleBackColor = true;
			// 
			// lockedForceUnlock
			// 
			resources.ApplyResources(this.lockedForceUnlock, "lockedForceUnlock");
			this.lockedForceUnlock.Checked = true;
			this.lockedForceUnlock.CheckState = System.Windows.Forms.CheckState.Checked;
			this.errorProvider.SetIconAlignment(this.lockedForceUnlock, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("lockedForceUnlock.IconAlignment"))));
			this.lockedForceUnlock.Name = "lockedForceUnlock";
			this.lockedForceUnlock.UseVisualStyleBackColor = true;
			// 
			// erase
			// 
			resources.ApplyResources(this.erase, "erase");
			this.errorProvider.SetIconAlignment(this.erase, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("erase.IconAlignment"))));
			this.erase.Name = "erase";
			// 
			// eraseFilesMethodLbl
			// 
			resources.ApplyResources(this.eraseFilesMethodLbl, "eraseFilesMethodLbl");
			this.errorProvider.SetIconAlignment(this.eraseFilesMethodLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseFilesMethodLbl.IconAlignment"))));
			this.eraseFilesMethodLbl.Name = "eraseFilesMethodLbl";
			// 
			// eraseDriveMethodLbl
			// 
			resources.ApplyResources(this.eraseDriveMethodLbl, "eraseDriveMethodLbl");
			this.errorProvider.SetIconAlignment(this.eraseDriveMethodLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseDriveMethodLbl.IconAlignment"))));
			this.eraseDriveMethodLbl.Name = "eraseDriveMethodLbl";
			// 
			// eraseFilesMethod
			// 
			this.eraseFilesMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.errorProvider.SetIconAlignment(this.eraseFilesMethod, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseFilesMethod.IconAlignment"))));
			resources.ApplyResources(this.eraseFilesMethod, "eraseFilesMethod");
			this.eraseFilesMethod.Name = "eraseFilesMethod";
			// 
			// eraseDriveMethod
			// 
			this.eraseDriveMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.errorProvider.SetIconAlignment(this.eraseDriveMethod, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseDriveMethod.IconAlignment"))));
			resources.ApplyResources(this.eraseDriveMethod, "eraseDriveMethod");
			this.eraseDriveMethod.Name = "eraseDriveMethod";
			// 
			// plugins
			// 
			resources.ApplyResources(this.plugins, "plugins");
			this.errorProvider.SetIconAlignment(this.plugins, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plugins.IconAlignment"))));
			this.plugins.Name = "plugins";
			// 
			// pluginsManager
			// 
			resources.ApplyResources(this.pluginsManager, "pluginsManager");
			this.pluginsManager.CheckBoxes = true;
			this.pluginsManager.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.pluginsManagerColName,
            this.pluginsManagerColAuthor,
            this.pluginsManagerColVersion,
            this.pluginsManagerColPath});
			this.pluginsManager.ContextMenuStrip = this.pluginsMenu;
			this.pluginsManager.FullRowSelect = true;
			this.pluginsManager.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            ((System.Windows.Forms.ListViewGroup)(resources.GetObject("pluginsManager.Groups"))),
            ((System.Windows.Forms.ListViewGroup)(resources.GetObject("pluginsManager.Groups1")))});
			this.errorProvider.SetIconAlignment(this.pluginsManager, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("pluginsManager.IconAlignment"))));
			this.pluginsManager.Name = "pluginsManager";
			this.pluginsManager.SmallImageList = this.pluginsManagerImageList;
			this.pluginsManager.UseCompatibleStateImageBehavior = false;
			this.pluginsManager.View = System.Windows.Forms.View.Details;
			this.pluginsManager.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.pluginsManager_ItemCheck);
			// 
			// pluginsManagerColName
			// 
			resources.ApplyResources(this.pluginsManagerColName, "pluginsManagerColName");
			// 
			// pluginsManagerColAuthor
			// 
			resources.ApplyResources(this.pluginsManagerColAuthor, "pluginsManagerColAuthor");
			// 
			// pluginsManagerColVersion
			// 
			resources.ApplyResources(this.pluginsManagerColVersion, "pluginsManagerColVersion");
			// 
			// pluginsManagerColPath
			// 
			resources.ApplyResources(this.pluginsManagerColPath, "pluginsManagerColPath");
			// 
			// pluginsMenu
			// 
			this.errorProvider.SetIconAlignment(this.pluginsMenu, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("pluginsMenu.IconAlignment"))));
			this.pluginsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
			this.pluginsMenu.Name = "pluginsContextMenu";
			resources.ApplyResources(this.pluginsMenu, "pluginsMenu");
			this.pluginsMenu.Opening += new System.ComponentModel.CancelEventHandler(this.pluginsMenu_Opening);
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			resources.ApplyResources(this.settingsToolStripMenuItem, "settingsToolStripMenuItem");
			this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
			// 
			// pluginsManagerImageList
			// 
			this.pluginsManagerImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("pluginsManagerImageList.ImageStream")));
			this.pluginsManagerImageList.TransparentColor = System.Drawing.Color.Transparent;
			this.pluginsManagerImageList.Images.SetKeyName(0, "Key.png");
			// 
			// scheduler
			// 
			resources.ApplyResources(this.scheduler, "scheduler");
			this.errorProvider.SetIconAlignment(this.scheduler, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("scheduler.IconAlignment"))));
			this.scheduler.Name = "scheduler";
			// 
			// schedulerMissed
			// 
			resources.ApplyResources(this.schedulerMissed, "schedulerMissed");
			this.errorProvider.SetIconAlignment(this.schedulerMissed, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerMissed.IconAlignment"))));
			this.schedulerMissed.Name = "schedulerMissed";
			// 
			// schedulerMissedImmediate
			// 
			resources.ApplyResources(this.schedulerMissedImmediate, "schedulerMissedImmediate");
			this.schedulerMissedImmediate.Checked = true;
			this.errorProvider.SetIconAlignment(this.schedulerMissedImmediate, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerMissedImmediate.IconAlignment"))));
			this.schedulerMissedImmediate.Name = "schedulerMissedImmediate";
			this.schedulerMissedImmediate.TabStop = true;
			this.schedulerMissedImmediate.UseVisualStyleBackColor = true;
			// 
			// schedulerMissedIgnore
			// 
			resources.ApplyResources(this.schedulerMissedIgnore, "schedulerMissedIgnore");
			this.errorProvider.SetIconAlignment(this.schedulerMissedIgnore, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerMissedIgnore.IconAlignment"))));
			this.schedulerMissedIgnore.Name = "schedulerMissedIgnore";
			this.schedulerMissedIgnore.TabStop = true;
			this.schedulerMissedIgnore.UseVisualStyleBackColor = true;
			// 
			// saveSettings
			// 
			resources.ApplyResources(this.saveSettings, "saveSettings");
			this.errorProvider.SetIconAlignment(this.saveSettings, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("saveSettings.IconAlignment"))));
			this.saveSettings.Name = "saveSettings";
			this.saveSettings.UseVisualStyleBackColor = true;
			this.saveSettings.Click += new System.EventHandler(this.saveSettings_Click);
			// 
			// erasePRNGLbl
			// 
			resources.ApplyResources(this.erasePRNGLbl, "erasePRNGLbl");
			this.errorProvider.SetIconAlignment(this.erasePRNGLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("erasePRNGLbl.IconAlignment"))));
			this.erasePRNGLbl.Name = "erasePRNGLbl";
			// 
			// erasePRNG
			// 
			this.erasePRNG.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.erasePRNG.FormattingEnabled = true;
			this.errorProvider.SetIconAlignment(this.erasePRNG, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("erasePRNG.IconAlignment"))));
			resources.ApplyResources(this.erasePRNG, "erasePRNG");
			this.erasePRNG.Name = "erasePRNG";
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// plausibleDeniability
			// 
			resources.ApplyResources(this.plausibleDeniability, "plausibleDeniability");
			this.errorProvider.SetIconAlignment(this.plausibleDeniability, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniability.IconAlignment"))));
			this.plausibleDeniability.Name = "plausibleDeniability";
			this.plausibleDeniability.UseVisualStyleBackColor = true;
			this.plausibleDeniability.CheckedChanged += new System.EventHandler(this.plausibleDeniability_CheckedChanged);
			// 
			// uiLanguageLbl
			// 
			resources.ApplyResources(this.uiLanguageLbl, "uiLanguageLbl");
			this.errorProvider.SetIconAlignment(this.uiLanguageLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("uiLanguageLbl.IconAlignment"))));
			this.uiLanguageLbl.Name = "uiLanguageLbl";
			// 
			// uiLanguage
			// 
			this.uiLanguage.DisplayMember = "DisplayName";
			this.uiLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.uiLanguage.FormattingEnabled = true;
			this.errorProvider.SetIconAlignment(this.uiLanguage, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("uiLanguage.IconAlignment"))));
			resources.ApplyResources(this.uiLanguage, "uiLanguage");
			this.uiLanguage.Name = "uiLanguage";
			// 
			// plausibleDeniabilityFiles
			// 
			resources.ApplyResources(this.plausibleDeniabilityFiles, "plausibleDeniabilityFiles");
			this.plausibleDeniabilityFiles.FormattingEnabled = true;
			this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFiles, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFiles.IconAlignment"))));
			this.plausibleDeniabilityFiles.Name = "plausibleDeniabilityFiles";
			this.plausibleDeniabilityFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.plausibleDeniabilityFiles.SelectedIndexChanged += new System.EventHandler(this.plausibleDeniabilityFiles_SelectedIndexChanged);
			// 
			// plausibleDeniabilityFilesAddFile
			// 
			resources.ApplyResources(this.plausibleDeniabilityFilesAddFile, "plausibleDeniabilityFilesAddFile");
			this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFilesAddFile, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFilesAddFile.IconAlignment"))));
			this.plausibleDeniabilityFilesAddFile.Name = "plausibleDeniabilityFilesAddFile";
			this.plausibleDeniabilityFilesAddFile.UseVisualStyleBackColor = true;
			this.plausibleDeniabilityFilesAddFile.Click += new System.EventHandler(this.plausibleDeniabilityFilesAddFile_Click);
			// 
			// plausibleDeniabilityFilesRemove
			// 
			resources.ApplyResources(this.plausibleDeniabilityFilesRemove, "plausibleDeniabilityFilesRemove");
			this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFilesRemove, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFilesRemove.IconAlignment"))));
			this.plausibleDeniabilityFilesRemove.Name = "plausibleDeniabilityFilesRemove";
			this.plausibleDeniabilityFilesRemove.UseVisualStyleBackColor = true;
			this.plausibleDeniabilityFilesRemove.Click += new System.EventHandler(this.plausibleDeniabilityFilesRemove_Click);
			// 
			// plausibleDeniabilityFilesAddFolder
			// 
			resources.ApplyResources(this.plausibleDeniabilityFilesAddFolder, "plausibleDeniabilityFilesAddFolder");
			this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFilesAddFolder, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFilesAddFolder.IconAlignment"))));
			this.plausibleDeniabilityFilesAddFolder.Name = "plausibleDeniabilityFilesAddFolder";
			this.plausibleDeniabilityFilesAddFolder.UseVisualStyleBackColor = true;
			this.plausibleDeniabilityFilesAddFolder.Click += new System.EventHandler(this.plausibleDeniabilityFilesAddFolder_Click);
			// 
			// schedulerClearCompleted
			// 
			resources.ApplyResources(this.schedulerClearCompleted, "schedulerClearCompleted");
			this.schedulerClearCompleted.Name = "schedulerClearCompleted";
			this.schedulerClearCompleted.UseVisualStyleBackColor = true;
			// 
			// openFileDialog
			// 
			resources.ApplyResources(this.openFileDialog, "openFileDialog");
			this.openFileDialog.Multiselect = true;
			// 
			// SettingsPanel
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.saveSettings);
			this.errorProvider.SetIconAlignment(this, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("$this.IconAlignment"))));
			this.Name = "SettingsPanel";
			this.Controls.SetChildIndex(this.saveSettings, 0);
			this.Controls.SetChildIndex(this.titleLabel, 0);
			this.Controls.SetChildIndex(this.titleIcon, 0);
			this.Controls.SetChildIndex(this.content, 0);
			((System.ComponentModel.ISupportInitialize)(this.titleIcon)).EndInit();
			this.content.ResumeLayout(false);
			this.content.PerformLayout();
			this.pluginsMenu.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox lockedForceUnlock;
		private System.Windows.Forms.CheckBox uiContextMenu;
		private LightGroup ui;
		private System.Windows.Forms.Label eraseDriveMethodLbl;
		private System.Windows.Forms.Label eraseFilesMethodLbl;
		private LightGroup erase;
		private System.Windows.Forms.ComboBox eraseFilesMethod;
		private System.Windows.Forms.ComboBox eraseDriveMethod;
		private System.Windows.Forms.ListView pluginsManager;
		private System.Windows.Forms.ColumnHeader pluginsManagerColName;
		private System.Windows.Forms.ColumnHeader pluginsManagerColAuthor;
		private System.Windows.Forms.ColumnHeader pluginsManagerColVersion;
		private System.Windows.Forms.ColumnHeader pluginsManagerColPath;
		private LightGroup plugins;
		private System.Windows.Forms.RadioButton schedulerMissedIgnore;
		private System.Windows.Forms.RadioButton schedulerMissedImmediate;
		private System.Windows.Forms.Label schedulerMissed;
		private LightGroup scheduler;
		private System.Windows.Forms.Button saveSettings;
		private System.Windows.Forms.ComboBox erasePRNG;
		private System.Windows.Forms.Label erasePRNGLbl;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.CheckBox plausibleDeniability;
		private System.Windows.Forms.ContextMenuStrip pluginsMenu;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ComboBox uiLanguage;
		private System.Windows.Forms.Label uiLanguageLbl;
		private System.Windows.Forms.Button plausibleDeniabilityFilesRemove;
		private System.Windows.Forms.Button plausibleDeniabilityFilesAddFile;
		private System.Windows.Forms.ListBox plausibleDeniabilityFiles;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.Button plausibleDeniabilityFilesAddFolder;
		private System.Windows.Forms.ImageList pluginsManagerImageList;
		private System.Windows.Forms.CheckBox schedulerClearCompleted;
	}
}
