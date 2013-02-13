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

namespace Eraser.DefaultPlugins
{
	partial class FolderErasureTargetConfigurer
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.folderDelete = new System.Windows.Forms.CheckBox();
			this.folderExclude = new System.Windows.Forms.TextBox();
			this.folderExcludeLbl = new System.Windows.Forms.Label();
			this.folderInclude = new System.Windows.Forms.TextBox();
			this.folderIncludeLbl = new System.Windows.Forms.Label();
			this.folderBrowse = new System.Windows.Forms.Button();
			this.folderPath = new System.Windows.Forms.TextBox();
			this.folderDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// folderDelete
			// 
			this.folderDelete.AutoSize = true;
			this.folderDelete.Checked = true;
			this.folderDelete.CheckState = System.Windows.Forms.CheckState.Checked;
			this.folderDelete.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.folderDelete.Location = new System.Drawing.Point(0, 89);
			this.folderDelete.Name = "folderDelete";
			this.folderDelete.Size = new System.Drawing.Size(140, 19);
			this.folderDelete.TabIndex = 20;
			this.folderDelete.Text = "Delete folder if empty";
			this.folderDelete.UseVisualStyleBackColor = true;
			// 
			// folderExclude
			// 
			this.folderExclude.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.folderExclude.Location = new System.Drawing.Point(104, 57);
			this.folderExclude.Name = "folderExclude";
			this.folderExclude.Size = new System.Drawing.Size(272, 23);
			this.folderExclude.TabIndex = 19;
			// 
			// folderExcludeLbl
			// 
			this.folderExcludeLbl.AutoSize = true;
			this.folderExcludeLbl.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.folderExcludeLbl.Location = new System.Drawing.Point(-3, 60);
			this.folderExcludeLbl.Name = "folderExcludeLbl";
			this.folderExcludeLbl.Size = new System.Drawing.Size(81, 15);
			this.folderExcludeLbl.TabIndex = 18;
			this.folderExcludeLbl.Text = "Exclude Mask:";
			// 
			// folderInclude
			// 
			this.folderInclude.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.folderInclude.Location = new System.Drawing.Point(104, 28);
			this.folderInclude.Name = "folderInclude";
			this.folderInclude.Size = new System.Drawing.Size(272, 23);
			this.folderInclude.TabIndex = 17;
			// 
			// folderIncludeLbl
			// 
			this.folderIncludeLbl.AutoSize = true;
			this.folderIncludeLbl.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.folderIncludeLbl.Location = new System.Drawing.Point(-3, 31);
			this.folderIncludeLbl.Name = "folderIncludeLbl";
			this.folderIncludeLbl.Size = new System.Drawing.Size(80, 15);
			this.folderIncludeLbl.TabIndex = 16;
			this.folderIncludeLbl.Text = "Include Mask:";
			// 
			// folderBrowse
			// 
			this.folderBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.folderBrowse.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.folderBrowse.Location = new System.Drawing.Point(301, -1);
			this.folderBrowse.Name = "folderBrowse";
			this.folderBrowse.Size = new System.Drawing.Size(75, 23);
			this.folderBrowse.TabIndex = 15;
			this.folderBrowse.Text = "Browse...";
			this.folderBrowse.UseVisualStyleBackColor = true;
			this.folderBrowse.Click += new System.EventHandler(this.folderBrowse_Click);
			// 
			// folderPath
			// 
			this.folderPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.folderPath.Location = new System.Drawing.Point(0, 0);
			this.folderPath.Name = "folderPath";
			this.folderPath.Size = new System.Drawing.Size(295, 23);
			this.folderPath.TabIndex = 14;
			// 
			// folderDialog
			// 
			this.folderDialog.Description = "Select a folder to erase.";
			this.folderDialog.ShowNewFolderButton = false;
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// FolderErasureTargetSettings
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.folderDelete);
			this.Controls.Add(this.folderExclude);
			this.Controls.Add(this.folderExcludeLbl);
			this.Controls.Add(this.folderInclude);
			this.Controls.Add(this.folderIncludeLbl);
			this.Controls.Add(this.folderBrowse);
			this.Controls.Add(this.folderPath);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Name = "FolderErasureTargetSettings";
			this.Size = new System.Drawing.Size(376, 108);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox folderDelete;
		private System.Windows.Forms.TextBox folderExclude;
		private System.Windows.Forms.Label folderExcludeLbl;
		private System.Windows.Forms.TextBox folderInclude;
		private System.Windows.Forms.Label folderIncludeLbl;
		private System.Windows.Forms.Button folderBrowse;
		private System.Windows.Forms.TextBox folderPath;
		private System.Windows.Forms.FolderBrowserDialog folderDialog;
		private System.Windows.Forms.ErrorProvider errorProvider;
	}
}
