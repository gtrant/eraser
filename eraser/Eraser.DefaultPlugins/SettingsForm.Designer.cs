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
	partial class SettingsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
			this.customMethodContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteMethodToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.okBtn = new System.Windows.Forms.Button();
			this.cancelBtn = new System.Windows.Forms.Button();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.customPassName = new System.Windows.Forms.ColumnHeader();
			this.customPassPassCount = new System.Windows.Forms.ColumnHeader();
			this.containerTab = new System.Windows.Forms.TabControl();
			this.containerTabGeneralPnl = new System.Windows.Forms.TabPage();
			this.fl16MethodCmb = new System.Windows.Forms.ComboBox();
			this.fl16MethodLbl = new System.Windows.Forms.Label();
			this.containerTabEraseMethodsPnl = new System.Windows.Forms.TabPage();
			this.customMethodAdd = new System.Windows.Forms.Button();
			this.customMethod = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.customMethodContextMenuStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.containerTab.SuspendLayout();
			this.containerTabGeneralPnl.SuspendLayout();
			this.containerTabEraseMethodsPnl.SuspendLayout();
			this.SuspendLayout();
			// 
			// customMethodContextMenuStrip
			// 
			this.customMethodContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteMethodToolStripMenuItem});
			this.customMethodContextMenuStrip.Name = "customMethodContextMenuStrip";
			resources.ApplyResources(this.customMethodContextMenuStrip, "customMethodContextMenuStrip");
			this.customMethodContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.customMethodContextMenuStrip_Opening);
			// 
			// deleteMethodToolStripMenuItem
			// 
			this.deleteMethodToolStripMenuItem.Name = "deleteMethodToolStripMenuItem";
			resources.ApplyResources(this.deleteMethodToolStripMenuItem, "deleteMethodToolStripMenuItem");
			this.deleteMethodToolStripMenuItem.Click += new System.EventHandler(this.deleteMethodToolStripMenuItem_Click);
			// 
			// okBtn
			// 
			resources.ApplyResources(this.okBtn, "okBtn");
			this.okBtn.Name = "okBtn";
			this.okBtn.UseVisualStyleBackColor = true;
			this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
			// 
			// cancelBtn
			// 
			resources.ApplyResources(this.cancelBtn, "cancelBtn");
			this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelBtn.Name = "cancelBtn";
			this.cancelBtn.UseVisualStyleBackColor = true;
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// customPassName
			// 
			resources.ApplyResources(this.customPassName, "customPassName");
			// 
			// customPassPassCount
			// 
			resources.ApplyResources(this.customPassPassCount, "customPassPassCount");
			// 
			// containerTab
			// 
			this.containerTab.Controls.Add(this.containerTabGeneralPnl);
			this.containerTab.Controls.Add(this.containerTabEraseMethodsPnl);
			resources.ApplyResources(this.containerTab, "containerTab");
			this.containerTab.Name = "containerTab";
			this.containerTab.SelectedIndex = 0;
			// 
			// containerTabGeneralPnl
			// 
			this.containerTabGeneralPnl.Controls.Add(this.fl16MethodCmb);
			this.containerTabGeneralPnl.Controls.Add(this.fl16MethodLbl);
			resources.ApplyResources(this.containerTabGeneralPnl, "containerTabGeneralPnl");
			this.containerTabGeneralPnl.Name = "containerTabGeneralPnl";
			this.containerTabGeneralPnl.UseVisualStyleBackColor = true;
			// 
			// fl16MethodCmb
			// 
			resources.ApplyResources(this.fl16MethodCmb, "fl16MethodCmb");
			this.fl16MethodCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.fl16MethodCmb.FormattingEnabled = true;
			this.fl16MethodCmb.Name = "fl16MethodCmb";
			// 
			// fl16MethodLbl
			// 
			resources.ApplyResources(this.fl16MethodLbl, "fl16MethodLbl");
			this.fl16MethodLbl.Name = "fl16MethodLbl";
			// 
			// containerTabEraseMethodsPnl
			// 
			this.containerTabEraseMethodsPnl.Controls.Add(this.customMethodAdd);
			this.containerTabEraseMethodsPnl.Controls.Add(this.customMethod);
			resources.ApplyResources(this.containerTabEraseMethodsPnl, "containerTabEraseMethodsPnl");
			this.containerTabEraseMethodsPnl.Name = "containerTabEraseMethodsPnl";
			this.containerTabEraseMethodsPnl.UseVisualStyleBackColor = true;
			// 
			// customMethodAdd
			// 
			resources.ApplyResources(this.customMethodAdd, "customMethodAdd");
			this.customMethodAdd.Name = "customMethodAdd";
			this.customMethodAdd.UseVisualStyleBackColor = true;
			this.customMethodAdd.Click += new System.EventHandler(this.customMethodAdd_Click);
			// 
			// customMethod
			// 
			resources.ApplyResources(this.customMethod, "customMethod");
			this.customMethod.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.customMethod.ContextMenuStrip = this.customMethodContextMenuStrip;
			this.customMethod.FullRowSelect = true;
			this.customMethod.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.customMethod.MultiSelect = false;
			this.customMethod.Name = "customMethod";
			this.customMethod.UseCompatibleStateImageBehavior = false;
			this.customMethod.View = System.Windows.Forms.View.Details;
			this.customMethod.ItemActivate += new System.EventHandler(this.customMethod_ItemActivate);
			// 
			// columnHeader1
			// 
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			// 
			// columnHeader2
			// 
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			// 
			// SettingsForm
			// 
			this.AcceptButton = this.okBtn;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelBtn;
			this.Controls.Add(this.containerTab);
			this.Controls.Add(this.okBtn);
			this.Controls.Add(this.cancelBtn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.ShowInTaskbar = false;
			this.customMethodContextMenuStrip.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.containerTab.ResumeLayout(false);
			this.containerTabGeneralPnl.ResumeLayout(false);
			this.containerTabGeneralPnl.PerformLayout();
			this.containerTabEraseMethodsPnl.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button okBtn;
		private System.Windows.Forms.Button cancelBtn;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.ContextMenuStrip customMethodContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem deleteMethodToolStripMenuItem;
		private System.Windows.Forms.TabControl containerTab;
		private System.Windows.Forms.TabPage containerTabGeneralPnl;
		private System.Windows.Forms.ComboBox fl16MethodCmb;
		private System.Windows.Forms.Label fl16MethodLbl;
		private System.Windows.Forms.TabPage containerTabEraseMethodsPnl;
		private System.Windows.Forms.Button customMethodAdd;
		private System.Windows.Forms.ListView customMethod;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader customPassName;
		private System.Windows.Forms.ColumnHeader customPassPassCount;
	}
}
