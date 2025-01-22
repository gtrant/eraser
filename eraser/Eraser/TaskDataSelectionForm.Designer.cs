/* 
 * $Id: TaskDataSelectionForm.Designer.cs 2993 2021-09-25 17:23:27Z gtrant $
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
            this.okBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.typeLbl = new System.Windows.Forms.Label();
            this.typeCmb = new System.Windows.Forms.ComboBox();
            this.typeSettingsPnl = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // methodLbl
            // 
            this.methodLbl.AccessibleDescription = null;
            this.methodLbl.AccessibleName = null;
            resources.ApplyResources(this.methodLbl, "methodLbl");
            this.errorProvider.SetError(this.methodLbl, resources.GetString("methodLbl.Error"));
            this.methodLbl.Font = null;
            this.errorProvider.SetIconAlignment(this.methodLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("methodLbl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.methodLbl, ((int)(resources.GetObject("methodLbl.IconPadding"))));
            this.methodLbl.Name = "methodLbl";
            // 
            // methodCmb
            // 
            this.methodCmb.AccessibleDescription = null;
            this.methodCmb.AccessibleName = null;
            resources.ApplyResources(this.methodCmb, "methodCmb");
            this.methodCmb.BackgroundImage = null;
            this.methodCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.errorProvider.SetError(this.methodCmb, resources.GetString("methodCmb.Error"));
            this.methodCmb.Font = null;
            this.methodCmb.FormattingEnabled = true;
            this.errorProvider.SetIconAlignment(this.methodCmb, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("methodCmb.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.methodCmb, ((int)(resources.GetObject("methodCmb.IconPadding"))));
            this.methodCmb.Name = "methodCmb";
            // 
            // okBtn
            // 
            this.okBtn.AccessibleDescription = null;
            this.okBtn.AccessibleName = null;
            resources.ApplyResources(this.okBtn, "okBtn");
            this.okBtn.BackgroundImage = null;
            this.errorProvider.SetError(this.okBtn, resources.GetString("okBtn.Error"));
            this.okBtn.Font = null;
            this.errorProvider.SetIconAlignment(this.okBtn, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("okBtn.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.okBtn, ((int)(resources.GetObject("okBtn.IconPadding"))));
            this.okBtn.Name = "okBtn";
            this.okBtn.UseVisualStyleBackColor = true;
            this.okBtn.Click += new System.EventHandler(this.ok_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.AccessibleDescription = null;
            this.cancelBtn.AccessibleName = null;
            resources.ApplyResources(this.cancelBtn, "cancelBtn");
            this.cancelBtn.BackgroundImage = null;
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.errorProvider.SetError(this.cancelBtn, resources.GetString("cancelBtn.Error"));
            this.cancelBtn.Font = null;
            this.errorProvider.SetIconAlignment(this.cancelBtn, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("cancelBtn.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.cancelBtn, ((int)(resources.GetObject("cancelBtn.IconPadding"))));
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.UseVisualStyleBackColor = true;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            resources.ApplyResources(this.errorProvider, "errorProvider");
            // 
            // typeLbl
            // 
            this.typeLbl.AccessibleDescription = null;
            this.typeLbl.AccessibleName = null;
            resources.ApplyResources(this.typeLbl, "typeLbl");
            this.errorProvider.SetError(this.typeLbl, resources.GetString("typeLbl.Error"));
            this.typeLbl.Font = null;
            this.errorProvider.SetIconAlignment(this.typeLbl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("typeLbl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.typeLbl, ((int)(resources.GetObject("typeLbl.IconPadding"))));
            this.typeLbl.Name = "typeLbl";
            // 
            // typeCmb
            // 
            this.typeCmb.AccessibleDescription = null;
            this.typeCmb.AccessibleName = null;
            resources.ApplyResources(this.typeCmb, "typeCmb");
            this.typeCmb.BackgroundImage = null;
            this.typeCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.errorProvider.SetError(this.typeCmb, resources.GetString("typeCmb.Error"));
            this.typeCmb.Font = null;
            this.typeCmb.FormattingEnabled = true;
            this.errorProvider.SetIconAlignment(this.typeCmb, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("typeCmb.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.typeCmb, ((int)(resources.GetObject("typeCmb.IconPadding"))));
            this.typeCmb.Name = "typeCmb";
            this.typeCmb.SelectedIndexChanged += new System.EventHandler(this.typeCmb_SelectedIndexChanged);
            // 
            // typeSettingsPnl
            // 
            this.typeSettingsPnl.AccessibleDescription = null;
            this.typeSettingsPnl.AccessibleName = null;
            resources.ApplyResources(this.typeSettingsPnl, "typeSettingsPnl");
            this.typeSettingsPnl.BackgroundImage = null;
            this.errorProvider.SetError(this.typeSettingsPnl, resources.GetString("typeSettingsPnl.Error"));
            this.typeSettingsPnl.Font = null;
            this.errorProvider.SetIconAlignment(this.typeSettingsPnl, ((System.Windows.Forms.ErrorIconAlignment)(resources.GetObject("typeSettingsPnl.IconAlignment"))));
            this.errorProvider.SetIconPadding(this.typeSettingsPnl, ((int)(resources.GetObject("typeSettingsPnl.IconPadding"))));
            this.typeSettingsPnl.Name = "typeSettingsPnl";
            this.typeSettingsPnl.TabStop = false;
            // 
            // TaskDataSelectionForm
            // 
            this.AcceptButton = this.okBtn;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = null;
            this.CancelButton = this.cancelBtn;
            this.Controls.Add(this.typeSettingsPnl);
            this.Controls.Add(this.typeCmb);
            this.Controls.Add(this.typeLbl);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.okBtn);
            this.Controls.Add(this.methodCmb);
            this.Controls.Add(this.methodLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = null;
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
		private System.Windows.Forms.Button okBtn;
		private System.Windows.Forms.Button cancelBtn;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.ComboBox typeCmb;
		private System.Windows.Forms.Label typeLbl;
		private System.Windows.Forms.GroupBox typeSettingsPnl;
	}
}