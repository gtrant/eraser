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

namespace Eraser
{
	partial class LogForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogForm));
            this.log = new System.Windows.Forms.ListView();
            this.logTimestampColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.logSeverityColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.logMessageColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.logContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copySelectedEntriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clear = new System.Windows.Forms.Button();
            this.close = new System.Windows.Forms.Button();
            this.filterSeverityLabel = new System.Windows.Forms.Label();
            this.filterSeverityCombobox = new System.Windows.Forms.ComboBox();
            this.filterFilterTypeCombobox = new System.Windows.Forms.ComboBox();
            this.filterSessionLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.filterSessionCombobox = new System.Windows.Forms.ComboBox();
            this.logContextMenuStrip.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // log
            // 
            resources.ApplyResources(this.log, "log");
            this.log.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.logTimestampColumn,
            this.logSeverityColumn,
            this.logMessageColumn});
            this.log.ContextMenuStrip = this.logContextMenuStrip;
            this.log.FullRowSelect = true;
            this.log.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.log.Name = "log";
            this.log.UseCompatibleStateImageBehavior = false;
            this.log.View = System.Windows.Forms.View.Details;
            this.log.VirtualMode = true;
            this.log.ItemActivate += new System.EventHandler(this.log_ItemActivate);
            this.log.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.log_ItemSelectionChanged);
            this.log.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.log_RetrieveVirtualItem);
            this.log.VirtualItemsSelectionRangeChanged += new System.Windows.Forms.ListViewVirtualItemsSelectionRangeChangedEventHandler(this.log_VirtualItemsSelectionRangeChanged);
            // 
            // logTimestampColumn
            // 
            resources.ApplyResources(this.logTimestampColumn, "logTimestampColumn");
            // 
            // logSeverityColumn
            // 
            resources.ApplyResources(this.logSeverityColumn, "logSeverityColumn");
            // 
            // logMessageColumn
            // 
            resources.ApplyResources(this.logMessageColumn, "logMessageColumn");
            // 
            // logContextMenuStrip
            // 
            this.logContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copySelectedEntriesToolStripMenuItem});
            this.logContextMenuStrip.Name = "logContextMenuStrip";
            resources.ApplyResources(this.logContextMenuStrip, "logContextMenuStrip");
            this.logContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.logContextMenuStrip_Opening);
            // 
            // copySelectedEntriesToolStripMenuItem
            // 
            this.copySelectedEntriesToolStripMenuItem.Name = "copySelectedEntriesToolStripMenuItem";
            resources.ApplyResources(this.copySelectedEntriesToolStripMenuItem, "copySelectedEntriesToolStripMenuItem");
            this.copySelectedEntriesToolStripMenuItem.Click += new System.EventHandler(this.copySelectedEntriesToolStripMenuItem_Click);
            // 
            // clear
            // 
            resources.ApplyResources(this.clear, "clear");
            this.clear.Name = "clear";
            this.clear.UseVisualStyleBackColor = true;
            this.clear.Click += new System.EventHandler(this.clear_Click);
            // 
            // close
            // 
            resources.ApplyResources(this.close, "close");
            this.close.Name = "close";
            this.close.UseVisualStyleBackColor = true;
            this.close.Click += new System.EventHandler(this.close_Click);
            // 
            // filterSeverityLabel
            // 
            resources.ApplyResources(this.filterSeverityLabel, "filterSeverityLabel");
            this.filterSeverityLabel.Name = "filterSeverityLabel";
            // 
            // filterSeverityCombobox
            // 
            resources.ApplyResources(this.filterSeverityCombobox, "filterSeverityCombobox");
            this.filterSeverityCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterSeverityCombobox.Items.AddRange(new object[] {
            resources.GetString("filterSeverityCombobox.Items"),
            resources.GetString("filterSeverityCombobox.Items1"),
            resources.GetString("filterSeverityCombobox.Items2"),
            resources.GetString("filterSeverityCombobox.Items3"),
            resources.GetString("filterSeverityCombobox.Items4")});
            this.filterSeverityCombobox.Name = "filterSeverityCombobox";
            this.filterSeverityCombobox.SelectedIndexChanged += new System.EventHandler(this.filter_Changed);
            // 
            // filterFilterTypeCombobox
            // 
            resources.ApplyResources(this.filterFilterTypeCombobox, "filterFilterTypeCombobox");
            this.filterFilterTypeCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterFilterTypeCombobox.FormattingEnabled = true;
            this.filterFilterTypeCombobox.Items.AddRange(new object[] {
            resources.GetString("filterFilterTypeCombobox.Items"),
            resources.GetString("filterFilterTypeCombobox.Items1"),
            resources.GetString("filterFilterTypeCombobox.Items2")});
            this.filterFilterTypeCombobox.Name = "filterFilterTypeCombobox";
            this.filterFilterTypeCombobox.SelectedIndexChanged += new System.EventHandler(this.filter_Changed);
            // 
            // filterSessionLabel
            // 
            resources.ApplyResources(this.filterSessionLabel, "filterSessionLabel");
            this.filterSessionLabel.Name = "filterSessionLabel";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.filterSessionLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.filterSeverityLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.filterFilterTypeCombobox, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.filterSeverityCombobox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.filterSessionCombobox, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // filterSessionCombobox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.filterSessionCombobox, 3);
            resources.ApplyResources(this.filterSessionCombobox, "filterSessionCombobox");
            this.filterSessionCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterSessionCombobox.FormattingEnabled = true;
            this.filterSessionCombobox.Name = "filterSessionCombobox";
            this.filterSessionCombobox.SelectedIndexChanged += new System.EventHandler(this.filter_Changed);
            // 
            // LogForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.close);
            this.Controls.Add(this.log);
            this.Controls.Add(this.clear);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LogForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.LogForm_FormClosed);
            this.logContextMenuStrip.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView log;
		private System.Windows.Forms.Button clear;
		private System.Windows.Forms.Button close;
		private System.Windows.Forms.ColumnHeader logTimestampColumn;
		private System.Windows.Forms.ColumnHeader logSeverityColumn;
		private System.Windows.Forms.ColumnHeader logMessageColumn;
		private System.Windows.Forms.Label filterSeverityLabel;
		private System.Windows.Forms.ComboBox filterSeverityCombobox;
		private System.Windows.Forms.ComboBox filterFilterTypeCombobox;
		private System.Windows.Forms.Label filterSessionLabel;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ComboBox filterSessionCombobox;
		private System.Windows.Forms.ContextMenuStrip logContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem copySelectedEntriesToolStripMenuItem;
	}
}