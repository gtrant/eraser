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
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.fromLbl = new System.Windows.Forms.Label();
			this.fromTxt = new System.Windows.Forms.TextBox();
			this.fromSelectFileBtn = new System.Windows.Forms.Button();
			this.fromSelectFolderBtn = new System.Windows.Forms.Button();
			this.toLbl = new System.Windows.Forms.Label();
			this.toTxt = new System.Windows.Forms.TextBox();
			this.toBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// fromLbl
			// 
			this.fromLbl.AutoSize = true;
			this.fromLbl.Location = new System.Drawing.Point(0, 3);
			this.fromLbl.Name = "fromLbl";
			this.fromLbl.Size = new System.Drawing.Size(38, 15);
			this.fromLbl.TabIndex = 0;
			this.fromLbl.Text = "From:";
			// 
			// fromTxt
			// 
			this.fromTxt.Location = new System.Drawing.Point(44, 0);
			this.fromTxt.Name = "fromTxt";
			this.fromTxt.Size = new System.Drawing.Size(273, 23);
			this.fromTxt.TabIndex = 1;
			// 
			// fromSelectFileBtn
			// 
			this.fromSelectFileBtn.Location = new System.Drawing.Point(323, 0);
			this.fromSelectFileBtn.Name = "fromSelectFileBtn";
			this.fromSelectFileBtn.Size = new System.Drawing.Size(90, 23);
			this.fromSelectFileBtn.TabIndex = 2;
			this.fromSelectFileBtn.Text = "Select File...";
			this.fromSelectFileBtn.UseVisualStyleBackColor = true;
			// 
			// fromSelectFolderBtn
			// 
			this.fromSelectFolderBtn.Location = new System.Drawing.Point(419, 0);
			this.fromSelectFolderBtn.Name = "fromSelectFolderBtn";
			this.fromSelectFolderBtn.Size = new System.Drawing.Size(100, 23);
			this.fromSelectFolderBtn.TabIndex = 3;
			this.fromSelectFolderBtn.Text = "Select Folder...";
			this.fromSelectFolderBtn.UseVisualStyleBackColor = true;
			// 
			// toLbl
			// 
			this.toLbl.AutoSize = true;
			this.toLbl.Location = new System.Drawing.Point(0, 33);
			this.toLbl.Name = "toLbl";
			this.toLbl.Size = new System.Drawing.Size(24, 15);
			this.toLbl.TabIndex = 4;
			this.toLbl.Text = "To:";
			// 
			// toTxt
			// 
			this.toTxt.Location = new System.Drawing.Point(44, 29);
			this.toTxt.Name = "toTxt";
			this.toTxt.Size = new System.Drawing.Size(369, 23);
			this.toTxt.TabIndex = 5;
			// 
			// toBtn
			// 
			this.toBtn.Location = new System.Drawing.Point(419, 29);
			this.toBtn.Name = "toBtn";
			this.toBtn.Size = new System.Drawing.Size(100, 23);
			this.toBtn.TabIndex = 6;
			this.toBtn.Text = "Select Folder...";
			this.toBtn.UseVisualStyleBackColor = true;
			// 
			// SecureMoveErasureTargetConfigurer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.toBtn);
			this.Controls.Add(this.toTxt);
			this.Controls.Add(this.toLbl);
			this.Controls.Add(this.fromSelectFolderBtn);
			this.Controls.Add(this.fromSelectFileBtn);
			this.Controls.Add(this.fromTxt);
			this.Controls.Add(this.fromLbl);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Name = "SecureMoveErasureTargetConfigurer";
			this.Size = new System.Drawing.Size(519, 54);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.Label fromLbl;
		private System.Windows.Forms.TextBox fromTxt;
		private System.Windows.Forms.Button fromSelectFileBtn;
		private System.Windows.Forms.Button fromSelectFolderBtn;
		private System.Windows.Forms.Label toLbl;
		private System.Windows.Forms.TextBox toTxt;
		private System.Windows.Forms.Button toBtn;
	}
}
