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
	partial class DriveErasureTargetConfigurer
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
			this.partitionCmb = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// partitionCmb
			// 
			this.partitionCmb.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.partitionCmb.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.partitionCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.partitionCmb.FormattingEnabled = true;
			this.partitionCmb.Location = new System.Drawing.Point(0, 0);
			this.partitionCmb.Name = "partitionCmb";
			this.partitionCmb.Size = new System.Drawing.Size(345, 21);
			this.partitionCmb.TabIndex = 18;
			this.partitionCmb.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.OnDrawItem);
			// 
			// PartitionErasureTargetConfigurer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.partitionCmb);
			this.Name = "PartitionErasureTargetConfigurer";
			this.Size = new System.Drawing.Size(345, 21);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox partitionCmb;
	}
}
