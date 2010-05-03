﻿/* 
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

namespace Eraser.Manager
{
	partial class FileErasureTargetSettings
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
			this.fileBrowse = new System.Windows.Forms.Button();
			this.filePath = new System.Windows.Forms.TextBox();
			this.fileDialog = new System.Windows.Forms.OpenFileDialog();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// fileBrowse
			// 
			this.fileBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.fileBrowse.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.fileBrowse.Location = new System.Drawing.Point(75, 0);
			this.fileBrowse.Name = "fileBrowse";
			this.fileBrowse.Size = new System.Drawing.Size(75, 23);
			this.fileBrowse.TabIndex = 7;
			this.fileBrowse.Text = "Browse...";
			this.fileBrowse.UseVisualStyleBackColor = true;
			this.fileBrowse.Click += new System.EventHandler(this.fileBrowse_Click);
			// 
			// filePath
			// 
			this.filePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.filePath.Location = new System.Drawing.Point(0, 2);
			this.filePath.Name = "filePath";
			this.filePath.Size = new System.Drawing.Size(69, 20);
			this.filePath.TabIndex = 6;
			// 
			// fileDialog
			// 
			this.fileDialog.Filter = "All files (*.*)|*.*";
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// FileErasureTargetSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.fileBrowse);
			this.Controls.Add(this.filePath);
			this.Name = "FileErasureTargetSettings";
			this.Size = new System.Drawing.Size(150, 23);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button fileBrowse;
		private System.Windows.Forms.TextBox filePath;
		private System.Windows.Forms.OpenFileDialog fileDialog;
		private System.Windows.Forms.ErrorProvider errorProvider;
	}
}
