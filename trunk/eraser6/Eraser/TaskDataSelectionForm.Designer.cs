/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
	partial class TaskDataSelectionForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TaskDataSelectionForm));
			this.file = new System.Windows.Forms.RadioButton();
			this.folder = new System.Windows.Forms.RadioButton();
			this.unused = new System.Windows.Forms.RadioButton();
			this.filePath = new System.Windows.Forms.TextBox();
			this.fileBrowse = new System.Windows.Forms.Button();
			this.folderPath = new System.Windows.Forms.TextBox();
			this.folderBrowse = new System.Windows.Forms.Button();
			this.folderIncludeLbl = new System.Windows.Forms.Label();
			this.folderInclude = new System.Windows.Forms.TextBox();
			this.folderExcludeLbl = new System.Windows.Forms.Label();
			this.folderExclude = new System.Windows.Forms.TextBox();
			this.folderDelete = new System.Windows.Forms.CheckBox();
			this.unusedDisk = new System.Windows.Forms.ComboBox();
			this.methodLbl = new System.Windows.Forms.Label();
			this.method = new System.Windows.Forms.ComboBox();
			this.ok = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			this.fileDialog = new System.Windows.Forms.OpenFileDialog();
			this.folderDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.unusedClusterTips = new System.Windows.Forms.CheckBox();
			this.recycleBin = new System.Windows.Forms.RadioButton();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// file
			// 
			resources.ApplyResources(this.file, "file");
			this.file.Name = "file";
			this.file.TabStop = true;
			this.file.UseVisualStyleBackColor = true;
			this.file.CheckedChanged += new System.EventHandler(this.data_CheckedChanged);
			// 
			// folder
			// 
			resources.ApplyResources(this.folder, "folder");
			this.folder.Name = "folder";
			this.folder.TabStop = true;
			this.folder.UseVisualStyleBackColor = true;
			this.folder.CheckedChanged += new System.EventHandler(this.data_CheckedChanged);
			// 
			// unused
			// 
			resources.ApplyResources(this.unused, "unused");
			this.unused.Name = "unused";
			this.unused.TabStop = true;
			this.unused.UseVisualStyleBackColor = true;
			this.unused.CheckedChanged += new System.EventHandler(this.data_CheckedChanged);
			// 
			// filePath
			// 
			resources.ApplyResources(this.filePath, "filePath");
			this.filePath.Name = "filePath";
			// 
			// fileBrowse
			// 
			resources.ApplyResources(this.fileBrowse, "fileBrowse");
			this.fileBrowse.Name = "fileBrowse";
			this.fileBrowse.UseVisualStyleBackColor = true;
			this.fileBrowse.Click += new System.EventHandler(this.fileBrowse_Click);
			// 
			// folderPath
			// 
			resources.ApplyResources(this.folderPath, "folderPath");
			this.folderPath.Name = "folderPath";
			// 
			// folderBrowse
			// 
			resources.ApplyResources(this.folderBrowse, "folderBrowse");
			this.folderBrowse.Name = "folderBrowse";
			this.folderBrowse.UseVisualStyleBackColor = true;
			this.folderBrowse.Click += new System.EventHandler(this.folderBrowse_Click);
			// 
			// folderIncludeLbl
			// 
			resources.ApplyResources(this.folderIncludeLbl, "folderIncludeLbl");
			this.folderIncludeLbl.Name = "folderIncludeLbl";
			// 
			// folderInclude
			// 
			resources.ApplyResources(this.folderInclude, "folderInclude");
			this.folderInclude.Name = "folderInclude";
			// 
			// folderExcludeLbl
			// 
			resources.ApplyResources(this.folderExcludeLbl, "folderExcludeLbl");
			this.folderExcludeLbl.Name = "folderExcludeLbl";
			// 
			// folderExclude
			// 
			resources.ApplyResources(this.folderExclude, "folderExclude");
			this.folderExclude.Name = "folderExclude";
			// 
			// folderDelete
			// 
			resources.ApplyResources(this.folderDelete, "folderDelete");
			this.folderDelete.Checked = true;
			this.folderDelete.CheckState = System.Windows.Forms.CheckState.Checked;
			this.folderDelete.Name = "folderDelete";
			this.folderDelete.UseVisualStyleBackColor = true;
			// 
			// unusedDisk
			// 
			resources.ApplyResources(this.unusedDisk, "unusedDisk");
			this.unusedDisk.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.unusedDisk.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.unusedDisk.FormattingEnabled = true;
			this.unusedDisk.Name = "unusedDisk";
			this.unusedDisk.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.unusedDisk_DrawItem);
			// 
			// methodLbl
			// 
			resources.ApplyResources(this.methodLbl, "methodLbl");
			this.methodLbl.Name = "methodLbl";
			// 
			// method
			// 
			resources.ApplyResources(this.method, "method");
			this.method.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.method.FormattingEnabled = true;
			this.method.Name = "method";
			this.method.SelectedIndexChanged += new System.EventHandler(this.method_SelectedIndexChanged);
			// 
			// ok
			// 
			resources.ApplyResources(this.ok, "ok");
			this.ok.Name = "ok";
			this.ok.UseVisualStyleBackColor = true;
			this.ok.Click += new System.EventHandler(this.ok_Click);
			// 
			// cancel
			// 
			resources.ApplyResources(this.cancel, "cancel");
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Name = "cancel";
			this.cancel.UseVisualStyleBackColor = true;
			// 
			// fileDialog
			// 
			resources.ApplyResources(this.fileDialog, "fileDialog");
			// 
			// folderDialog
			// 
			resources.ApplyResources(this.folderDialog, "folderDialog");
			this.folderDialog.ShowNewFolderButton = false;
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// unusedClusterTips
			// 
			resources.ApplyResources(this.unusedClusterTips, "unusedClusterTips");
			this.unusedClusterTips.Checked = true;
			this.unusedClusterTips.CheckState = System.Windows.Forms.CheckState.Checked;
			this.unusedClusterTips.Name = "unusedClusterTips";
			this.unusedClusterTips.UseVisualStyleBackColor = true;
			// 
			// recycleBin
			// 
			resources.ApplyResources(this.recycleBin, "recycleBin");
			this.recycleBin.Name = "recycleBin";
			this.recycleBin.TabStop = true;
			this.recycleBin.UseVisualStyleBackColor = true;
			this.recycleBin.CheckedChanged += new System.EventHandler(this.data_CheckedChanged);
			// 
			// TaskDataSelectionForm
			// 
			this.AcceptButton = this.ok;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancel;
			this.Controls.Add(this.recycleBin);
			this.Controls.Add(this.unusedClusterTips);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.ok);
			this.Controls.Add(this.method);
			this.Controls.Add(this.methodLbl);
			this.Controls.Add(this.unusedDisk);
			this.Controls.Add(this.folderDelete);
			this.Controls.Add(this.folderExclude);
			this.Controls.Add(this.folderExcludeLbl);
			this.Controls.Add(this.folderInclude);
			this.Controls.Add(this.folderIncludeLbl);
			this.Controls.Add(this.folderBrowse);
			this.Controls.Add(this.folderPath);
			this.Controls.Add(this.fileBrowse);
			this.Controls.Add(this.filePath);
			this.Controls.Add(this.unused);
			this.Controls.Add(this.folder);
			this.Controls.Add(this.file);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TaskDataSelectionForm";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RadioButton file;
		private System.Windows.Forms.RadioButton folder;
		private System.Windows.Forms.RadioButton unused;
		private System.Windows.Forms.TextBox filePath;
		private System.Windows.Forms.Button fileBrowse;
		private System.Windows.Forms.TextBox folderPath;
		private System.Windows.Forms.Button folderBrowse;
		private System.Windows.Forms.Label folderIncludeLbl;
		private System.Windows.Forms.TextBox folderInclude;
		private System.Windows.Forms.Label folderExcludeLbl;
		private System.Windows.Forms.TextBox folderExclude;
		private System.Windows.Forms.CheckBox folderDelete;
		private System.Windows.Forms.ComboBox unusedDisk;
		private System.Windows.Forms.Label methodLbl;
		private System.Windows.Forms.ComboBox method;
		private System.Windows.Forms.Button ok;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.OpenFileDialog fileDialog;
		private System.Windows.Forms.FolderBrowserDialog folderDialog;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.CheckBox unusedClusterTips;
		private System.Windows.Forms.RadioButton recycleBin;
	}
}