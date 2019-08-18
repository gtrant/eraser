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
			this.folderDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.fromLbl = new System.Windows.Forms.Label();
			this.fromTxt = new System.Windows.Forms.TextBox();
			this.toLbl = new System.Windows.Forms.Label();
			this.toTxt = new System.Windows.Forms.TextBox();
			this.toSelectButton = new System.Windows.Forms.Button();
			this.moveFileRadio = new System.Windows.Forms.RadioButton();
			this.moveFolderRadio = new System.Windows.Forms.RadioButton();
			this.fromSelectBtn = new System.Windows.Forms.Button();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.SuspendLayout();
			// 
			// fromLbl
			// 
			this.fromLbl.AutoSize = true;
			this.fromLbl.Location = new System.Drawing.Point(-3, 28);
			this.fromLbl.Name = "fromLbl";
			this.fromLbl.Size = new System.Drawing.Size(38, 15);
			this.fromLbl.TabIndex = 0;
			this.fromLbl.Text = "From:";
			// 
			// fromTxt
			// 
			this.fromTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.fromTxt.Location = new System.Drawing.Point(41, 25);
			this.fromTxt.Name = "fromTxt";
			this.fromTxt.Size = new System.Drawing.Size(397, 23);
			this.fromTxt.TabIndex = 3;
			// 
			// toLbl
			// 
			this.toLbl.AutoSize = true;
			this.toLbl.Location = new System.Drawing.Point(-3, 57);
			this.toLbl.Name = "toLbl";
			this.toLbl.Size = new System.Drawing.Size(24, 15);
			this.toLbl.TabIndex = 4;
			this.toLbl.Text = "To:";
			// 
			// toTxt
			// 
			this.toTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.toTxt.Location = new System.Drawing.Point(41, 54);
			this.toTxt.Name = "toTxt";
			this.toTxt.Size = new System.Drawing.Size(397, 23);
			this.toTxt.TabIndex = 5;
			// 
			// toSelectButton
			// 
			this.toSelectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.toSelectButton.Location = new System.Drawing.Point(444, 53);
			this.toSelectButton.Name = "toSelectButton";
			this.toSelectButton.Size = new System.Drawing.Size(75, 23);
			this.toSelectButton.TabIndex = 6;
			this.toSelectButton.Text = "Browse";
			this.toSelectButton.UseVisualStyleBackColor = true;
			this.toSelectButton.Click += new System.EventHandler(this.toSelectButton_Click);
			// 
			// moveFileRadio
			// 
			this.moveFileRadio.AutoSize = true;
			this.moveFileRadio.Checked = true;
			this.moveFileRadio.Location = new System.Drawing.Point(0, 0);
			this.moveFileRadio.Name = "moveFileRadio";
			this.moveFileRadio.Size = new System.Drawing.Size(85, 19);
			this.moveFileRadio.TabIndex = 1;
			this.moveFileRadio.TabStop = true;
			this.moveFileRadio.Text = "Move a File";
			this.moveFileRadio.UseVisualStyleBackColor = true;
			// 
			// moveFolderRadio
			// 
			this.moveFolderRadio.AutoSize = true;
			this.moveFolderRadio.Location = new System.Drawing.Point(91, 0);
			this.moveFolderRadio.Name = "moveFolderRadio";
			this.moveFolderRadio.Size = new System.Drawing.Size(100, 19);
			this.moveFolderRadio.TabIndex = 2;
			this.moveFolderRadio.TabStop = true;
			this.moveFolderRadio.Text = "Move a Folder";
			this.moveFolderRadio.UseVisualStyleBackColor = true;
			// 
			// fromSelectBtn
			// 
			this.fromSelectBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.fromSelectBtn.Location = new System.Drawing.Point(444, 24);
			this.fromSelectBtn.Name = "fromSelectBtn";
			this.fromSelectBtn.Size = new System.Drawing.Size(75, 23);
			this.fromSelectBtn.TabIndex = 4;
			this.fromSelectBtn.Text = "Browse";
			this.fromSelectBtn.UseVisualStyleBackColor = true;
			this.fromSelectBtn.Click += new System.EventHandler(this.fromSelectButton_Click);
			// 
			// SecureMoveErasureTargetConfigurer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.fromSelectBtn);
			this.Controls.Add(this.moveFolderRadio);
			this.Controls.Add(this.moveFileRadio);
			this.Controls.Add(this.fromTxt);
			this.Controls.Add(this.fromLbl);
			this.Controls.Add(this.toTxt);
			this.Controls.Add(this.toLbl);
			this.Controls.Add(this.toSelectButton);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Name = "SecureMoveErasureTargetConfigurer";
			this.Size = new System.Drawing.Size(519, 82);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.FolderBrowserDialog folderDialog;
		private System.Windows.Forms.Label fromLbl;
		private System.Windows.Forms.TextBox fromTxt;
		private System.Windows.Forms.Label toLbl;
		private System.Windows.Forms.TextBox toTxt;
		private System.Windows.Forms.Button toSelectButton;
		private System.Windows.Forms.RadioButton moveFileRadio;
		private System.Windows.Forms.RadioButton moveFolderRadio;
		private System.Windows.Forms.Button fromSelectBtn;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
	}
}
