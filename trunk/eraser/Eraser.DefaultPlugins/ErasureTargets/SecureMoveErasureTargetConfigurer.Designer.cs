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

namespace Eraser.DefaultPlugins
{
	partial class SecureMoveErasureTargetConfigurer
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
			this.fileDialog = new System.Windows.Forms.OpenFileDialog();
			this.folderDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.fromLbl = new System.Windows.Forms.Label();
			this.fromTxt = new System.Windows.Forms.TextBox();
			this.toLbl = new System.Windows.Forms.Label();
			this.toTxt = new System.Windows.Forms.TextBox();
			this.fromBrowseMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.fromSelectFile = new System.Windows.Forms.ToolStripMenuItem();
			this.fromSelectFolder = new System.Windows.Forms.ToolStripMenuItem();
			this.toSelectButton = new System.Windows.Forms.Button();
			this.fromSelectButton = new System.Windows.Forms.SplitButton();
			this.fromBrowseMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// fromLbl
			// 
			this.fromLbl.AutoSize = true;
			this.fromLbl.Location = new System.Drawing.Point(-3, 3);
			this.fromLbl.Name = "fromLbl";
			this.fromLbl.Size = new System.Drawing.Size(38, 15);
			this.fromLbl.TabIndex = 0;
			this.fromLbl.Text = "From:";
			// 
			// fromTxt
			// 
			this.fromTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.fromTxt.Location = new System.Drawing.Point(41, 0);
			this.fromTxt.Name = "fromTxt";
			this.fromTxt.Size = new System.Drawing.Size(397, 23);
			this.fromTxt.TabIndex = 1;
			// 
			// toLbl
			// 
			this.toLbl.AutoSize = true;
			this.toLbl.Location = new System.Drawing.Point(-3, 33);
			this.toLbl.Name = "toLbl";
			this.toLbl.Size = new System.Drawing.Size(24, 15);
			this.toLbl.TabIndex = 4;
			this.toLbl.Text = "To:";
			// 
			// toTxt
			// 
			this.toTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.toTxt.Location = new System.Drawing.Point(41, 30);
			this.toTxt.Name = "toTxt";
			this.toTxt.Size = new System.Drawing.Size(397, 23);
			this.toTxt.TabIndex = 5;
			// 
			// fromBrowseMenu
			// 
			this.fromBrowseMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fromSelectFile,
            this.fromSelectFolder});
			this.fromBrowseMenu.Name = "contextMenuStrip1";
			this.fromBrowseMenu.Size = new System.Drawing.Size(176, 48);
			// 
			// fromSelectFile
			// 
			this.fromSelectFile.Name = "fromSelectFile";
			this.fromSelectFile.Size = new System.Drawing.Size(175, 22);
			this.fromSelectFile.Text = "Browse for File...";
			this.fromSelectFile.Click += new System.EventHandler(this.fromSelectButton_Click);
			// 
			// fromSelectFolder
			// 
			this.fromSelectFolder.Name = "fromSelectFolder";
			this.fromSelectFolder.Size = new System.Drawing.Size(175, 22);
			this.fromSelectFolder.Text = "Browse for Folder...";
			this.fromSelectFolder.Click += new System.EventHandler(this.fromSelectFolder_Click);
			// 
			// toSelectButton
			// 
			this.toSelectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.toSelectButton.Location = new System.Drawing.Point(444, 29);
			this.toSelectButton.Name = "toSelectButton";
			this.toSelectButton.Size = new System.Drawing.Size(75, 23);
			this.toSelectButton.TabIndex = 10;
			this.toSelectButton.Text = "Browse";
			this.toSelectButton.UseVisualStyleBackColor = true;
			this.toSelectButton.Click += new System.EventHandler(this.toSelectButton_Click);
			// 
			// fromSelectButton
			// 
			this.fromSelectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.fromSelectButton.ContextMenuStrip = this.fromBrowseMenu;
			this.fromSelectButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.fromSelectButton.Location = new System.Drawing.Point(444, -1);
			this.fromSelectButton.Name = "fromSelectButton";
			this.fromSelectButton.Size = new System.Drawing.Size(75, 23);
			this.fromSelectButton.TabIndex = 7;
			this.fromSelectButton.Text = "Browse";
			this.fromSelectButton.UseVisualStyleBackColor = true;
			this.fromSelectButton.Click += new System.EventHandler(this.fromSelectButton_Click);
			// 
			// SecureMoveErasureTargetConfigurer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.fromTxt);
			this.Controls.Add(this.fromLbl);
			this.Controls.Add(this.toTxt);
			this.Controls.Add(this.fromSelectButton);
			this.Controls.Add(this.toLbl);
			this.Controls.Add(this.toSelectButton);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Name = "SecureMoveErasureTargetConfigurer";
			this.Size = new System.Drawing.Size(519, 55);
			this.fromBrowseMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog fileDialog;
		private System.Windows.Forms.FolderBrowserDialog folderDialog;
		private System.Windows.Forms.Label fromLbl;
		private System.Windows.Forms.TextBox fromTxt;
		private System.Windows.Forms.Label toLbl;
		private System.Windows.Forms.TextBox toTxt;
		private System.Windows.Forms.SplitButton fromSelectButton;
		private System.Windows.Forms.ContextMenuStrip fromBrowseMenu;
		private System.Windows.Forms.ToolStripMenuItem fromSelectFile;
		private System.Windows.Forms.ToolStripMenuItem fromSelectFolder;
		private System.Windows.Forms.Button toSelectButton;
	}
}
