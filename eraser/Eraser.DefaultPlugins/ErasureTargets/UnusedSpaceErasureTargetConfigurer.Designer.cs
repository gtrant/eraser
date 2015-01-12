/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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
	partial class UnusedSpaceErasureTargetConfigurer
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
			this.unusedClusterTips = new System.Windows.Forms.CheckBox();
			this.unusedDisk = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// unusedClusterTips
			// 
			this.unusedClusterTips.AutoSize = true;
			this.unusedClusterTips.Checked = true;
			this.unusedClusterTips.CheckState = System.Windows.Forms.CheckState.Checked;
			this.unusedClusterTips.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.unusedClusterTips.Location = new System.Drawing.Point(0, 30);
			this.unusedClusterTips.Name = "unusedClusterTips";
			this.unusedClusterTips.Size = new System.Drawing.Size(113, 19);
			this.unusedClusterTips.TabIndex = 18;
			this.unusedClusterTips.Text = "Erase cluster tips";
			this.unusedClusterTips.UseVisualStyleBackColor = true;
			// 
			// unusedDisk
			// 
			this.unusedDisk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.unusedDisk.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.unusedDisk.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.unusedDisk.FormattingEnabled = true;
			this.unusedDisk.Location = new System.Drawing.Point(0, 0);
			this.unusedDisk.Name = "unusedDisk";
			this.unusedDisk.Size = new System.Drawing.Size(345, 24);
			this.unusedDisk.TabIndex = 17;
			this.unusedDisk.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.unusedDisk_DrawItem);
			// 
			// UnusedSpaceErasureTargetSettings
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.unusedClusterTips);
			this.Controls.Add(this.unusedDisk);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Name = "UnusedSpaceErasureTargetSettings";
			this.Size = new System.Drawing.Size(345, 49);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox unusedClusterTips;
		private System.Windows.Forms.ComboBox unusedDisk;
	}
}
