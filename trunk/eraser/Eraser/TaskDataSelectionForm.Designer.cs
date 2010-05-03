/* 
 * $Id$
 * Copyright 2008-2010 The Eraser Project
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
	partial class TaskDataSelectionForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TaskDataSelectionForm));
			this.methodLbl = new System.Windows.Forms.Label();
			this.methodCmb = new System.Windows.Forms.ComboBox();
			this.ok = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.typeLbl = new System.Windows.Forms.Label();
			this.typeCmb = new System.Windows.Forms.ComboBox();
			this.typeSettingsPnl = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// methodLbl
			// 
			resources.ApplyResources(this.methodLbl, "methodLbl");
			this.methodLbl.Name = "methodLbl";
			// 
			// methodCmb
			// 
			resources.ApplyResources(this.methodCmb, "methodCmb");
			this.methodCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.methodCmb.FormattingEnabled = true;
			this.methodCmb.Name = "methodCmb";
			// 
			// ok
			// 
			resources.ApplyResources(this.ok, "ok");
			this.ok.Name = "ok";
			this.ok.UseVisualStyleBackColor = true;
			this.ok.Click += new System.EventHandler(this.ok_Click);
			// 
			// cancel
			// 
			resources.ApplyResources(this.cancel, "cancel");
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Name = "cancel";
			this.cancel.UseVisualStyleBackColor = true;
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// typeLbl
			// 
			resources.ApplyResources(this.typeLbl, "typeLbl");
			this.typeLbl.Name = "typeLbl";
			// 
			// typeCmb
			// 
			resources.ApplyResources(this.typeCmb, "typeCmb");
			this.typeCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.typeCmb.FormattingEnabled = true;
			this.typeCmb.Name = "typeCmb";
			this.typeCmb.SelectedIndexChanged += new System.EventHandler(this.typeCmb_SelectedIndexChanged);
			// 
			// typeSettingsPnl
			// 
			resources.ApplyResources(this.typeSettingsPnl, "typeSettingsPnl");
			this.typeSettingsPnl.Name = "typeSettingsPnl";
			// 
			// TaskDataSelectionForm
			// 
			this.AcceptButton = this.ok;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancel;
			this.Controls.Add(this.typeSettingsPnl);
			this.Controls.Add(this.typeCmb);
			this.Controls.Add(this.typeLbl);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.ok);
			this.Controls.Add(this.methodCmb);
			this.Controls.Add(this.methodLbl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TaskDataSelectionForm";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label methodLbl;
		private System.Windows.Forms.ComboBox methodCmb;
		private System.Windows.Forms.Button ok;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.ComboBox typeCmb;
		private System.Windows.Forms.Label typeLbl;
		private System.Windows.Forms.Panel typeSettingsPnl;
	}
}