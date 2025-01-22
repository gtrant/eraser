/* 
 * $Id: BasePanel.Designer.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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
	partial class BasePanel
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BasePanel));
			this.titleLabel = new System.Windows.Forms.Label();
			this.content = new System.Windows.Forms.Panel();
			this.titleIcon = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.titleIcon)).BeginInit();
			this.SuspendLayout();
			// 
			// titleLabel
			// 
			resources.ApplyResources(this.titleLabel, "titleLabel");
			this.titleLabel.Name = "titleLabel";
			// 
			// content
			// 
			resources.ApplyResources(this.content, "content");
			this.content.Name = "content";
			// 
			// titleIcon
			// 
			resources.ApplyResources(this.titleIcon, "titleIcon");
			this.titleIcon.Name = "titleIcon";
			this.titleIcon.TabStop = false;
			// 
			// BasePanel
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.content);
			this.Controls.Add(this.titleIcon);
			this.Controls.Add(this.titleLabel);
			this.Name = "BasePanel";
			((System.ComponentModel.ISupportInitialize)(this.titleIcon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		protected System.Windows.Forms.Label titleLabel;
		protected System.Windows.Forms.PictureBox titleIcon;
		protected System.Windows.Forms.Panel content;
	}
}
