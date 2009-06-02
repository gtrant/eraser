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
			this.TitleLabel = new System.Windows.Forms.Label();
			this.Content = new System.Windows.Forms.Panel();
			this.TitleIcon = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.TitleIcon)).BeginInit();
			this.SuspendLayout();
			// 
			// titleLabel
			// 
			resources.ApplyResources(this.TitleLabel, "titleLabel");
			this.TitleLabel.Name = "titleLabel";
			// 
			// content
			// 
			resources.ApplyResources(this.Content, "content");
			this.Content.Name = "content";
			// 
			// titleIcon
			// 
			resources.ApplyResources(this.TitleIcon, "titleIcon");
			this.TitleIcon.Name = "titleIcon";
			this.TitleIcon.TabStop = false;
			// 
			// BasePanel
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.Content);
			this.Controls.Add(this.TitleIcon);
			this.Controls.Add(this.TitleLabel);
			this.Name = "BasePanel";
			resources.ApplyResources(this, "$this");
			((System.ComponentModel.ISupportInitialize)(this.TitleIcon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.PictureBox titleIcon;
		private System.Windows.Forms.Panel content;

	}
}
