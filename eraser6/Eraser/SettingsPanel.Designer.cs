/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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
            this.eraseUnusedMethodLbl = new System.Windows.Forms.Label();
            this.eraseFilesMethod = new System.Windows.Forms.ComboBox();
            this.eraseUnusedMethod = new System.Windows.Forms.ComboBox();
            this.plugins = new Eraser.LightGroup();
            this.pluginsManager = new System.Windows.Forms.ListView();
            this.pluginsManagerColName = new System.Windows.Forms.ColumnHeader();
            this.pluginsManagerColAuthor = new System.Windows.Forms.ColumnHeader();
            this.pluginsManagerColVersion = new System.Windows.Forms.ColumnHeader();
            this.pluginsManagerColPath = new System.Windows.Forms.ColumnHeader();
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
            this.titleLabel.AccessibleDescription = null;
            this.titleLabel.AccessibleName = null;
            resources.ApplyResources(this.titleLabel, "titleLabel");
            this.errorProvider.SetError(this.titleLabel, resources.GetString("titleLabel.Error"));
            this.errorProvider.SetIconAlignment(this.titleLabel, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("titleLabel.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.titleLabel, ((int)(resources.GetObject("titleLabel.IconPadding"))));
            // 
            // titleIcon
            // 
            this.titleIcon.AccessibleDescription = null;
            this.titleIcon.AccessibleName = null;
            resources.ApplyResources(this.titleIcon, "titleIcon");
            this.titleIcon.BackgroundImage = null;
            this.errorProvider.SetError(this.titleIcon, resources.GetString("titleIcon.Error"));
            this.titleIcon.Font = null;
            this.errorProvider.SetIconAlignment(this.titleIcon, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("titleIcon.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.titleIcon, ((int)(resources.GetObject("titleIcon.IconPadding"))));
            this.titleIcon.Image = global::Eraser.Properties.Resources.ToolbarSettings;
            this.titleIcon.ImageLocation = null;
            // 
            // content
            // 
            this.content.AccessibleDescription = null;
            this.content.AccessibleName = null;
            resources.ApplyResources(this.content, "content");
            this.content.BackgroundImage = null;
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
            this.content.Controls.Add(this.eraseUnusedMethod);
            this.content.Controls.Add(this.eraseFilesMethod);
            this.content.Controls.Add(this.eraseUnusedMethodLbl);
            this.content.Controls.Add(this.eraseFilesMethodLbl);
            this.content.Controls.Add(this.erase);
            this.content.Controls.Add(this.lockedForceUnlock);
            this.content.Controls.Add(this.uiContextMenu);
            this.content.Controls.Add(this.ui);
            this.errorProvider.SetError(this.content, resources.GetString("content.Error"));
            this.content.Font = null;
            this.errorProvider.SetIconAlignment(this.content, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("content.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.content, ((int)(resources.GetObject("content.IconPadding"))));
            // 
            // ui
            // 
            this.ui.AccessibleDescription = null;
            this.ui.AccessibleName = null;
            resources.ApplyResources(this.ui, "ui");
            this.ui.BackgroundImage = null;
            this.errorProvider.SetError(this.ui, resources.GetString("ui.Error"));
            this.ui.Font = null;
            this.errorProvider.SetIconAlignment(this.ui, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("ui.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.ui, ((int)(resources.GetObject("ui.IconPadding"))));
            this.ui.Name = "ui";
            // 
            // uiContextMenu
            // 
            this.uiContextMenu.AccessibleDescription = null;
            this.uiContextMenu.AccessibleName = null;
            resources.ApplyResources(this.uiContextMenu, "uiContextMenu");
            this.uiContextMenu.BackgroundImage = null;
            this.uiContextMenu.Checked = true;
            this.uiContextMenu.CheckState = System.Windows.Forms.CheckState.Checked;
            this.errorProvider.SetError(this.uiContextMenu, resources.GetString("uiContextMenu.Error"));
            this.uiContextMenu.Font = null;
            this.errorProvider.SetIconAlignment(this.uiContextMenu, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("uiContextMenu.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.uiContextMenu, ((int)(resources.GetObject("uiContextMenu.IconPadding"))));
            this.uiContextMenu.Name = "uiContextMenu";
            this.uiContextMenu.UseVisualStyleBackColor = true;
            // 
            // lockedForceUnlock
            // 
            this.lockedForceUnlock.AccessibleDescription = null;
            this.lockedForceUnlock.AccessibleName = null;
            resources.ApplyResources(this.lockedForceUnlock, "lockedForceUnlock");
            this.lockedForceUnlock.BackgroundImage = null;
            this.lockedForceUnlock.Checked = true;
            this.lockedForceUnlock.CheckState = System.Windows.Forms.CheckState.Checked;
            this.errorProvider.SetError(this.lockedForceUnlock, resources.GetString("lockedForceUnlock.Error"));
            this.lockedForceUnlock.Font = null;
            this.errorProvider.SetIconAlignment(this.lockedForceUnlock, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("lockedForceUnlock.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.lockedForceUnlock, ((int)(resources.GetObject("lockedForceUnlock.IconPadding"))));
            this.lockedForceUnlock.Name = "lockedForceUnlock";
            this.lockedForceUnlock.UseVisualStyleBackColor = true;
            // 
            // erase
            // 
            this.erase.AccessibleDescription = null;
            this.erase.AccessibleName = null;
            resources.ApplyResources(this.erase, "erase");
            this.erase.BackgroundImage = null;
            this.errorProvider.SetError(this.erase, resources.GetString("erase.Error"));
            this.erase.Font = null;
            this.errorProvider.SetIconAlignment(this.erase, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("erase.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.erase, ((int)(resources.GetObject("erase.IconPadding"))));
            this.erase.Name = "erase";
            // 
            // eraseFilesMethodLbl
            // 
            this.eraseFilesMethodLbl.AccessibleDescription = null;
            this.eraseFilesMethodLbl.AccessibleName = null;
            resources.ApplyResources(this.eraseFilesMethodLbl, "eraseFilesMethodLbl");
            this.errorProvider.SetError(this.eraseFilesMethodLbl, resources.GetString("eraseFilesMethodLbl.Error"));
            this.eraseFilesMethodLbl.Font = null;
            this.errorProvider.SetIconAlignment(this.eraseFilesMethodLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseFilesMethodLbl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.eraseFilesMethodLbl, ((int)(resources.GetObject("eraseFilesMethodLbl.IconPadding"))));
            this.eraseFilesMethodLbl.Name = "eraseFilesMethodLbl";
            // 
            // eraseUnusedMethodLbl
            // 
            this.eraseUnusedMethodLbl.AccessibleDescription = null;
            this.eraseUnusedMethodLbl.AccessibleName = null;
            resources.ApplyResources(this.eraseUnusedMethodLbl, "eraseUnusedMethodLbl");
            this.errorProvider.SetError(this.eraseUnusedMethodLbl, resources.GetString("eraseUnusedMethodLbl.Error"));
            this.eraseUnusedMethodLbl.Font = null;
            this.errorProvider.SetIconAlignment(this.eraseUnusedMethodLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseUnusedMethodLbl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.eraseUnusedMethodLbl, ((int)(resources.GetObject("eraseUnusedMethodLbl.IconPadding"))));
            this.eraseUnusedMethodLbl.Name = "eraseUnusedMethodLbl";
            // 
            // eraseFilesMethod
            // 
            this.eraseFilesMethod.AccessibleDescription = null;
            this.eraseFilesMethod.AccessibleName = null;
            resources.ApplyResources(this.eraseFilesMethod, "eraseFilesMethod");
            this.eraseFilesMethod.BackgroundImage = null;
            this.eraseFilesMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.errorProvider.SetError(this.eraseFilesMethod, resources.GetString("eraseFilesMethod.Error"));
            this.eraseFilesMethod.Font = null;
            this.errorProvider.SetIconAlignment(this.eraseFilesMethod, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseFilesMethod.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.eraseFilesMethod, ((int)(resources.GetObject("eraseFilesMethod.IconPadding"))));
            this.eraseFilesMethod.Name = "eraseFilesMethod";
            // 
            // eraseUnusedMethod
            // 
            this.eraseUnusedMethod.AccessibleDescription = null;
            this.eraseUnusedMethod.AccessibleName = null;
            resources.ApplyResources(this.eraseUnusedMethod, "eraseUnusedMethod");
            this.eraseUnusedMethod.BackgroundImage = null;
            this.eraseUnusedMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.errorProvider.SetError(this.eraseUnusedMethod, resources.GetString("eraseUnusedMethod.Error"));
            this.eraseUnusedMethod.Font = null;
            this.errorProvider.SetIconAlignment(this.eraseUnusedMethod, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("eraseUnusedMethod.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.eraseUnusedMethod, ((int)(resources.GetObject("eraseUnusedMethod.IconPadding"))));
            this.eraseUnusedMethod.Name = "eraseUnusedMethod";
            // 
            // plugins
            // 
            this.plugins.AccessibleDescription = null;
            this.plugins.AccessibleName = null;
            resources.ApplyResources(this.plugins, "plugins");
            this.plugins.BackgroundImage = null;
            this.errorProvider.SetError(this.plugins, resources.GetString("plugins.Error"));
            this.plugins.Font = null;
            this.errorProvider.SetIconAlignment(this.plugins, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plugins.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.plugins, ((int)(resources.GetObject("plugins.IconPadding"))));
            this.plugins.Name = "plugins";
            // 
            // pluginsManager
            // 
            this.pluginsManager.AccessibleDescription = null;
            this.pluginsManager.AccessibleName = null;
            resources.ApplyResources(this.pluginsManager, "pluginsManager");
            this.pluginsManager.BackgroundImage = null;
            this.pluginsManager.CheckBoxes = true;
            this.pluginsManager.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.pluginsManagerColName,
            this.pluginsManagerColAuthor,
            this.pluginsManagerColVersion,
            this.pluginsManagerColPath});
            this.pluginsManager.ContextMenuStrip = this.pluginsMenu;
            this.errorProvider.SetError(this.pluginsManager, resources.GetString("pluginsManager.Error"));
            this.pluginsManager.Font = null;
            this.pluginsManager.FullRowSelect = true;
            this.pluginsManager.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            ((System.Windows.Forms.ListViewGroup)(resources.GetObject("pluginsManager.Groups"))),
            ((System.Windows.Forms.ListViewGroup)(resources.GetObject("pluginsManager.Groups1")))});
            this.errorProvider.SetIconAlignment(this.pluginsManager, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("pluginsManager.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.pluginsManager, ((int)(resources.GetObject("pluginsManager.IconPadding"))));
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
            this.pluginsMenu.AccessibleDescription = null;
            this.pluginsMenu.AccessibleName = null;
            resources.ApplyResources(this.pluginsMenu, "pluginsMenu");
            this.pluginsMenu.BackgroundImage = null;
            this.errorProvider.SetError(this.pluginsMenu, resources.GetString("pluginsMenu.Error"));
            this.pluginsMenu.Font = null;
            this.errorProvider.SetIconAlignment(this.pluginsMenu, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("pluginsMenu.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.pluginsMenu, ((int)(resources.GetObject("pluginsMenu.IconPadding"))));
            this.pluginsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.pluginsMenu.Name = "pluginsContextMenu";
            this.pluginsMenu.Opening += new System.ComponentModel.CancelEventHandler(this.pluginsMenu_Opening);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.AccessibleDescription = null;
            this.settingsToolStripMenuItem.AccessibleName = null;
            resources.ApplyResources(this.settingsToolStripMenuItem, "settingsToolStripMenuItem");
            this.settingsToolStripMenuItem.BackgroundImage = null;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.ShortcutKeyDisplayString = null;
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
            this.scheduler.AccessibleDescription = null;
            this.scheduler.AccessibleName = null;
            resources.ApplyResources(this.scheduler, "scheduler");
            this.scheduler.BackgroundImage = null;
            this.errorProvider.SetError(this.scheduler, resources.GetString("scheduler.Error"));
            this.scheduler.Font = null;
            this.errorProvider.SetIconAlignment(this.scheduler, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("scheduler.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.scheduler, ((int)(resources.GetObject("scheduler.IconPadding"))));
            this.scheduler.Name = "scheduler";
            // 
            // schedulerMissed
            // 
            this.schedulerMissed.AccessibleDescription = null;
            this.schedulerMissed.AccessibleName = null;
            resources.ApplyResources(this.schedulerMissed, "schedulerMissed");
            this.errorProvider.SetError(this.schedulerMissed, resources.GetString("schedulerMissed.Error"));
            this.schedulerMissed.Font = null;
            this.errorProvider.SetIconAlignment(this.schedulerMissed, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerMissed.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.schedulerMissed, ((int)(resources.GetObject("schedulerMissed.IconPadding"))));
            this.schedulerMissed.Name = "schedulerMissed";
            // 
            // schedulerMissedImmediate
            // 
            this.schedulerMissedImmediate.AccessibleDescription = null;
            this.schedulerMissedImmediate.AccessibleName = null;
            resources.ApplyResources(this.schedulerMissedImmediate, "schedulerMissedImmediate");
            this.schedulerMissedImmediate.BackgroundImage = null;
            this.schedulerMissedImmediate.Checked = true;
            this.errorProvider.SetError(this.schedulerMissedImmediate, resources.GetString("schedulerMissedImmediate.Error"));
            this.schedulerMissedImmediate.Font = null;
            this.errorProvider.SetIconAlignment(this.schedulerMissedImmediate, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerMissedImmediate.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.schedulerMissedImmediate, ((int)(resources.GetObject("schedulerMissedImmediate.IconPadding"))));
            this.schedulerMissedImmediate.Name = "schedulerMissedImmediate";
            this.schedulerMissedImmediate.TabStop = true;
            this.schedulerMissedImmediate.UseVisualStyleBackColor = true;
            // 
            // schedulerMissedIgnore
            // 
            this.schedulerMissedIgnore.AccessibleDescription = null;
            this.schedulerMissedIgnore.AccessibleName = null;
            resources.ApplyResources(this.schedulerMissedIgnore, "schedulerMissedIgnore");
            this.schedulerMissedIgnore.BackgroundImage = null;
            this.errorProvider.SetError(this.schedulerMissedIgnore, resources.GetString("schedulerMissedIgnore.Error"));
            this.schedulerMissedIgnore.Font = null;
            this.errorProvider.SetIconAlignment(this.schedulerMissedIgnore, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerMissedIgnore.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.schedulerMissedIgnore, ((int)(resources.GetObject("schedulerMissedIgnore.IconPadding"))));
            this.schedulerMissedIgnore.Name = "schedulerMissedIgnore";
            this.schedulerMissedIgnore.TabStop = true;
            this.schedulerMissedIgnore.UseVisualStyleBackColor = true;
            // 
            // saveSettings
            // 
            this.saveSettings.AccessibleDescription = null;
            this.saveSettings.AccessibleName = null;
            resources.ApplyResources(this.saveSettings, "saveSettings");
            this.saveSettings.BackgroundImage = null;
            this.errorProvider.SetError(this.saveSettings, resources.GetString("saveSettings.Error"));
            this.saveSettings.Font = null;
            this.errorProvider.SetIconAlignment(this.saveSettings, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("saveSettings.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.saveSettings, ((int)(resources.GetObject("saveSettings.IconPadding"))));
            this.saveSettings.Name = "saveSettings";
            this.saveSettings.UseVisualStyleBackColor = true;
            this.saveSettings.Click += new System.EventHandler(this.saveSettings_Click);
            // 
            // erasePRNGLbl
            // 
            this.erasePRNGLbl.AccessibleDescription = null;
            this.erasePRNGLbl.AccessibleName = null;
            resources.ApplyResources(this.erasePRNGLbl, "erasePRNGLbl");
            this.errorProvider.SetError(this.erasePRNGLbl, resources.GetString("erasePRNGLbl.Error"));
            this.erasePRNGLbl.Font = null;
            this.errorProvider.SetIconAlignment(this.erasePRNGLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("erasePRNGLbl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.erasePRNGLbl, ((int)(resources.GetObject("erasePRNGLbl.IconPadding"))));
            this.erasePRNGLbl.Name = "erasePRNGLbl";
            // 
            // erasePRNG
            // 
            this.erasePRNG.AccessibleDescription = null;
            this.erasePRNG.AccessibleName = null;
            resources.ApplyResources(this.erasePRNG, "erasePRNG");
            this.erasePRNG.BackgroundImage = null;
            this.erasePRNG.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.errorProvider.SetError(this.erasePRNG, resources.GetString("erasePRNG.Error"));
            this.erasePRNG.Font = null;
            this.erasePRNG.FormattingEnabled = true;
            this.errorProvider.SetIconAlignment(this.erasePRNG, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("erasePRNG.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.erasePRNG, ((int)(resources.GetObject("erasePRNG.IconPadding"))));
            this.erasePRNG.Name = "erasePRNG";
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            resources.ApplyResources(this.errorProvider, "errorProvider");
            // 
            // plausibleDeniability
            // 
            this.plausibleDeniability.AccessibleDescription = null;
            this.plausibleDeniability.AccessibleName = null;
            resources.ApplyResources(this.plausibleDeniability, "plausibleDeniability");
            this.plausibleDeniability.BackgroundImage = null;
            this.errorProvider.SetError(this.plausibleDeniability, resources.GetString("plausibleDeniability.Error"));
            this.plausibleDeniability.Font = null;
            this.errorProvider.SetIconAlignment(this.plausibleDeniability, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniability.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.plausibleDeniability, ((int)(resources.GetObject("plausibleDeniability.IconPadding"))));
            this.plausibleDeniability.Name = "plausibleDeniability";
            this.plausibleDeniability.UseVisualStyleBackColor = true;
            this.plausibleDeniability.CheckedChanged += new System.EventHandler(this.plausibleDeniability_CheckedChanged);
            // 
            // uiLanguageLbl
            // 
            this.uiLanguageLbl.AccessibleDescription = null;
            this.uiLanguageLbl.AccessibleName = null;
            resources.ApplyResources(this.uiLanguageLbl, "uiLanguageLbl");
            this.errorProvider.SetError(this.uiLanguageLbl, resources.GetString("uiLanguageLbl.Error"));
            this.uiLanguageLbl.Font = null;
            this.errorProvider.SetIconAlignment(this.uiLanguageLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("uiLanguageLbl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.uiLanguageLbl, ((int)(resources.GetObject("uiLanguageLbl.IconPadding"))));
            this.uiLanguageLbl.Name = "uiLanguageLbl";
            // 
            // uiLanguage
            // 
            this.uiLanguage.AccessibleDescription = null;
            this.uiLanguage.AccessibleName = null;
            resources.ApplyResources(this.uiLanguage, "uiLanguage");
            this.uiLanguage.BackgroundImage = null;
            this.uiLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.errorProvider.SetError(this.uiLanguage, resources.GetString("uiLanguage.Error"));
            this.uiLanguage.Font = null;
            this.uiLanguage.FormattingEnabled = true;
            this.errorProvider.SetIconAlignment(this.uiLanguage, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("uiLanguage.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.uiLanguage, ((int)(resources.GetObject("uiLanguage.IconPadding"))));
            this.uiLanguage.Name = "uiLanguage";
            // 
            // plausibleDeniabilityFiles
            // 
            this.plausibleDeniabilityFiles.AccessibleDescription = null;
            this.plausibleDeniabilityFiles.AccessibleName = null;
            resources.ApplyResources(this.plausibleDeniabilityFiles, "plausibleDeniabilityFiles");
            this.plausibleDeniabilityFiles.BackgroundImage = null;
            this.errorProvider.SetError(this.plausibleDeniabilityFiles, resources.GetString("plausibleDeniabilityFiles.Error"));
            this.plausibleDeniabilityFiles.Font = null;
            this.plausibleDeniabilityFiles.FormattingEnabled = true;
            this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFiles, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFiles.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.plausibleDeniabilityFiles, ((int)(resources.GetObject("plausibleDeniabilityFiles.IconPadding"))));
            this.plausibleDeniabilityFiles.Name = "plausibleDeniabilityFiles";
            this.plausibleDeniabilityFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.plausibleDeniabilityFiles.SelectedIndexChanged += new System.EventHandler(this.plausibleDeniabilityFiles_SelectedIndexChanged);
            // 
            // plausibleDeniabilityFilesAddFile
            // 
            this.plausibleDeniabilityFilesAddFile.AccessibleDescription = null;
            this.plausibleDeniabilityFilesAddFile.AccessibleName = null;
            resources.ApplyResources(this.plausibleDeniabilityFilesAddFile, "plausibleDeniabilityFilesAddFile");
            this.plausibleDeniabilityFilesAddFile.BackgroundImage = null;
            this.errorProvider.SetError(this.plausibleDeniabilityFilesAddFile, resources.GetString("plausibleDeniabilityFilesAddFile.Error"));
            this.plausibleDeniabilityFilesAddFile.Font = null;
            this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFilesAddFile, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFilesAddFile.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.plausibleDeniabilityFilesAddFile, ((int)(resources.GetObject("plausibleDeniabilityFilesAddFile.IconPadding"))));
            this.plausibleDeniabilityFilesAddFile.Name = "plausibleDeniabilityFilesAddFile";
            this.plausibleDeniabilityFilesAddFile.UseVisualStyleBackColor = true;
            this.plausibleDeniabilityFilesAddFile.Click += new System.EventHandler(this.plausibleDeniabilityFilesAddFile_Click);
            // 
            // plausibleDeniabilityFilesRemove
            // 
            this.plausibleDeniabilityFilesRemove.AccessibleDescription = null;
            this.plausibleDeniabilityFilesRemove.AccessibleName = null;
            resources.ApplyResources(this.plausibleDeniabilityFilesRemove, "plausibleDeniabilityFilesRemove");
            this.plausibleDeniabilityFilesRemove.BackgroundImage = null;
            this.errorProvider.SetError(this.plausibleDeniabilityFilesRemove, resources.GetString("plausibleDeniabilityFilesRemove.Error"));
            this.plausibleDeniabilityFilesRemove.Font = null;
            this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFilesRemove, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFilesRemove.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.plausibleDeniabilityFilesRemove, ((int)(resources.GetObject("plausibleDeniabilityFilesRemove.IconPadding"))));
            this.plausibleDeniabilityFilesRemove.Name = "plausibleDeniabilityFilesRemove";
            this.plausibleDeniabilityFilesRemove.UseVisualStyleBackColor = true;
            this.plausibleDeniabilityFilesRemove.Click += new System.EventHandler(this.plausibleDeniabilityFilesRemove_Click);
            // 
            // plausibleDeniabilityFilesAddFolder
            // 
            this.plausibleDeniabilityFilesAddFolder.AccessibleDescription = null;
            this.plausibleDeniabilityFilesAddFolder.AccessibleName = null;
            resources.ApplyResources(this.plausibleDeniabilityFilesAddFolder, "plausibleDeniabilityFilesAddFolder");
            this.plausibleDeniabilityFilesAddFolder.BackgroundImage = null;
            this.errorProvider.SetError(this.plausibleDeniabilityFilesAddFolder, resources.GetString("plausibleDeniabilityFilesAddFolder.Error"));
            this.plausibleDeniabilityFilesAddFolder.Font = null;
            this.errorProvider.SetIconAlignment(this.plausibleDeniabilityFilesAddFolder, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("plausibleDeniabilityFilesAddFolder.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.plausibleDeniabilityFilesAddFolder, ((int)(resources.GetObject("plausibleDeniabilityFilesAddFolder.IconPadding"))));
            this.plausibleDeniabilityFilesAddFolder.Name = "plausibleDeniabilityFilesAddFolder";
            this.plausibleDeniabilityFilesAddFolder.UseVisualStyleBackColor = true;
            this.plausibleDeniabilityFilesAddFolder.Click += new System.EventHandler(this.plausibleDeniabilityFilesAddFolder_Click);
            // 
            // schedulerClearCompleted
            // 
            this.schedulerClearCompleted.AccessibleDescription = null;
            this.schedulerClearCompleted.AccessibleName = null;
            resources.ApplyResources(this.schedulerClearCompleted, "schedulerClearCompleted");
            this.schedulerClearCompleted.BackgroundImage = null;
            this.errorProvider.SetError(this.schedulerClearCompleted, resources.GetString("schedulerClearCompleted.Error"));
            this.schedulerClearCompleted.Font = null;
            this.errorProvider.SetIconAlignment(this.schedulerClearCompleted, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("schedulerClearCompleted.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.schedulerClearCompleted, ((int)(resources.GetObject("schedulerClearCompleted.IconPadding"))));
            this.schedulerClearCompleted.Name = "schedulerClearCompleted";
            this.schedulerClearCompleted.UseVisualStyleBackColor = true;
            // 
            // openFileDialog
            // 
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            this.openFileDialog.Multiselect = true;
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // SettingsPanel
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.BackgroundImage = null;
            this.Controls.Add(this.saveSettings);
            this.errorProvider.SetError(this, resources.GetString("$this.Error"));
            this.Font = null;
            this.errorProvider.SetIconAlignment(this, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("$this.IconAlignment"))));
            this.errorProvider.SetIconPadding(this, ((int)(resources.GetObject("$this.IconPadding"))));
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
		private System.Windows.Forms.Label eraseUnusedMethodLbl;
		private System.Windows.Forms.Label eraseFilesMethodLbl;
		private LightGroup erase;
		private System.Windows.Forms.ComboBox eraseFilesMethod;
		private System.Windows.Forms.ComboBox eraseUnusedMethod;
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
