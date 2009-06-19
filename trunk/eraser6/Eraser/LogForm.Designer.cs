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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogForm));
			this.log = new System.Windows.Forms.ListView();
			this.logTimestampColumn = new System.Windows.Forms.ColumnHeader();
			this.logSeverityColumn = new System.Windows.Forms.ColumnHeader();
			this.logMessageColumn = new System.Windows.Forms.ColumnHeader();
			this.clear = new System.Windows.Forms.Button();
			this.close = new System.Windows.Forms.Button();
			this.copy = new System.Windows.Forms.Button();
			this.filterLabel = new System.Windows.Forms.Label();
			this.filterSeverity = new System.Windows.Forms.ComboBox();
			this.filterFilterType = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// log
			// 
			resources.ApplyResources(this.log, "log");
			this.log.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.logTimestampColumn,
            this.logSeverityColumn,
            this.logMessageColumn});
			this.log.FullRowSelect = true;
			this.log.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.log.MultiSelect = false;
			this.log.Name = "log";
			this.log.UseCompatibleStateImageBehavior = false;
			this.log.View = System.Windows.Forms.View.Details;
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
			// copy
			// 
			resources.ApplyResources(this.copy, "copy");
			this.copy.Name = "copy";
			this.copy.UseVisualStyleBackColor = true;
			this.copy.Click += new System.EventHandler(this.copy_Click);
			// 
			// filterLabel
			// 
			resources.ApplyResources(this.filterLabel, "filterLabel");
			this.filterLabel.Name = "filterLabel";
			// 
			// filterSeverity
			// 
			this.filterSeverity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.filterSeverity.Items.AddRange(new object[] {
            resources.GetString("filterSeverity.Items"),
            resources.GetString("filterSeverity.Items1"),
            resources.GetString("filterSeverity.Items2"),
            resources.GetString("filterSeverity.Items3"),
            resources.GetString("filterSeverity.Items4")});
			resources.ApplyResources(this.filterSeverity, "filterSeverity");
			this.filterSeverity.Name = "filterSeverity";
			this.filterSeverity.SelectedIndexChanged += new System.EventHandler(this.filter_Changed);
			// 
			// filterFilterType
			// 
			this.filterFilterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.filterFilterType.FormattingEnabled = true;
			this.filterFilterType.Items.AddRange(new object[] {
            resources.GetString("filterFilterType.Items"),
            resources.GetString("filterFilterType.Items1"),
            resources.GetString("filterFilterType.Items2")});
			resources.ApplyResources(this.filterFilterType, "filterFilterType");
			this.filterFilterType.Name = "filterFilterType";
			this.filterFilterType.SelectedIndexChanged += new System.EventHandler(this.filter_Changed);
			// 
			// LogForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.filterFilterType);
			this.Controls.Add(this.filterSeverity);
			this.Controls.Add(this.filterLabel);
			this.Controls.Add(this.copy);
			this.Controls.Add(this.close);
			this.Controls.Add(this.clear);
			this.Controls.Add(this.log);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LogForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.LogForm_FormClosed);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView log;
		private System.Windows.Forms.Button clear;
		private System.Windows.Forms.Button close;
		private System.Windows.Forms.ColumnHeader logTimestampColumn;
		private System.Windows.Forms.ColumnHeader logSeverityColumn;
		private System.Windows.Forms.ColumnHeader logMessageColumn;
		private System.Windows.Forms.Button copy;
		private System.Windows.Forms.Label filterLabel;
		private System.Windows.Forms.ComboBox filterSeverity;
		private System.Windows.Forms.ComboBox filterFilterType;
	}
}