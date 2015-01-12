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

namespace Eraser.BlackBox
{
	partial class BlackBoxMainForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BlackBoxMainForm));
			this.MainLbl = new System.Windows.Forms.Label();
			this.SubmitBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.BlackBoxPic = new System.Windows.Forms.PictureBox();
			this.ReportsLv = new System.Windows.Forms.ListView();
			this.ReportsLvTimestampColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ReportsLvErrorColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ReportsLvStatusColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ReportsMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DataCollectionPolicyLbl = new System.Windows.Forms.LinkLabel();
			((System.ComponentModel.ISupportInitialize)(this.BlackBoxPic)).BeginInit();
			this.ReportsMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainLbl
			// 
			resources.ApplyResources(this.MainLbl, "MainLbl");
			this.MainLbl.Name = "MainLbl";
			// 
			// SubmitBtn
			// 
			resources.ApplyResources(this.SubmitBtn, "SubmitBtn");
			this.SubmitBtn.Name = "SubmitBtn";
			this.SubmitBtn.UseVisualStyleBackColor = true;
			this.SubmitBtn.Click += new System.EventHandler(this.SubmitBtn_Click);
			// 
			// CancelBtn
			// 
			resources.ApplyResources(this.CancelBtn, "CancelBtn");
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.PostponeBtn_Click);
			// 
			// BlackBoxPic
			// 
			this.BlackBoxPic.Image = global::Eraser.BlackBox.Properties.Resources.BlackBox;
			resources.ApplyResources(this.BlackBoxPic, "BlackBoxPic");
			this.BlackBoxPic.Name = "BlackBoxPic";
			this.BlackBoxPic.TabStop = false;
			// 
			// ReportsLv
			// 
			resources.ApplyResources(this.ReportsLv, "ReportsLv");
			this.ReportsLv.CheckBoxes = true;
			this.ReportsLv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ReportsLvTimestampColumn,
            this.ReportsLvErrorColumn,
            this.ReportsLvStatusColumn});
			this.ReportsLv.ContextMenuStrip = this.ReportsMenuStrip;
			this.ReportsLv.FullRowSelect = true;
			this.ReportsLv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.ReportsLv.Name = "ReportsLv";
			this.ReportsLv.UseCompatibleStateImageBehavior = false;
			this.ReportsLv.View = System.Windows.Forms.View.Details;
			this.ReportsLv.ItemActivate += new System.EventHandler(this.ReportsLv_ItemActivate);
			// 
			// ReportsLvTimestampColumn
			// 
			resources.ApplyResources(this.ReportsLvTimestampColumn, "ReportsLvTimestampColumn");
			// 
			// ReportsLvErrorColumn
			// 
			resources.ApplyResources(this.ReportsLvErrorColumn, "ReportsLvErrorColumn");
			// 
			// ReportsLvStatusColumn
			// 
			resources.ApplyResources(this.ReportsLvStatusColumn, "ReportsLvStatusColumn");
			// 
			// ReportsMenuStrip
			// 
			this.ReportsMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
			this.ReportsMenuStrip.Name = "ReportsMenuStrip";
			resources.ApplyResources(this.ReportsMenuStrip, "ReportsMenuStrip");
			this.ReportsMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.ReportsMenuStrip_Opening);
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			resources.ApplyResources(this.deleteToolStripMenuItem, "deleteToolStripMenuItem");
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			// 
			// DataCollectionPolicyLbl
			// 
			resources.ApplyResources(this.DataCollectionPolicyLbl, "DataCollectionPolicyLbl");
			this.DataCollectionPolicyLbl.Name = "DataCollectionPolicyLbl";
			this.DataCollectionPolicyLbl.TabStop = true;
			this.DataCollectionPolicyLbl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DataCollectionPolicyLbl_LinkClicked);
			// 
			// BlackBoxMainForm
			// 
			this.AcceptButton = this.SubmitBtn;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.CancelBtn;
			this.Controls.Add(this.DataCollectionPolicyLbl);
			this.Controls.Add(this.ReportsLv);
			this.Controls.Add(this.BlackBoxPic);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.SubmitBtn);
			this.Controls.Add(this.MainLbl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BlackBoxMainForm";
			this.ShowInTaskbar = false;
			this.Load += new System.EventHandler(this.BlackBoxMainForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.BlackBoxPic)).EndInit();
			this.ReportsMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label MainLbl;
		private System.Windows.Forms.Button SubmitBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.PictureBox BlackBoxPic;
		private System.Windows.Forms.ListView ReportsLv;
		private System.Windows.Forms.ColumnHeader ReportsLvTimestampColumn;
		private System.Windows.Forms.ColumnHeader ReportsLvErrorColumn;
		private System.Windows.Forms.LinkLabel DataCollectionPolicyLbl;
		private System.Windows.Forms.ContextMenuStrip ReportsMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.ColumnHeader ReportsLvStatusColumn;
	}
}