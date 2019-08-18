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

namespace Eraser
{
	partial class AboutForm
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

			ParentBitmap.Dispose();
			AboutBitmap.Dispose();
			AboutTextBitmap.Dispose();
			DoubleBufferBitmap.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
			this.animationTimer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// animationTimer
			// 
			this.animationTimer.Enabled = true;
			this.animationTimer.Interval = 50;
			this.animationTimer.Tick += new System.EventHandler(this.animationTimer_Tick);
			// 
			// AboutForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "AboutForm";
			this.ShowInTaskbar = false;
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.AboutForm_MouseUp);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.AboutForm_Paint);
			this.Click += new System.EventHandler(this.AboutForm_Click);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AboutForm_MouseDown);
			this.MouseLeave += new System.EventHandler(this.AboutForm_MouseLeave);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AboutForm_MouseMove);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Timer animationTimer;
	}
}