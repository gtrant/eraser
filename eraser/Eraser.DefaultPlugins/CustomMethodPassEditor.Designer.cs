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
	partial class CustomMethodPassEditor
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
			this.components = new System.ComponentModel.Container();
			this.passTxt = new System.Windows.Forms.TextBox();
			this.passTypeGrp = new System.Windows.Forms.FlowLayoutPanel();
			this.passTypeText = new System.Windows.Forms.RadioButton();
			this.passTypeHex = new System.Windows.Forms.RadioButton();
			this.passTypeRandom = new System.Windows.Forms.RadioButton();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.passTypeGrp.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// passTxt
			// 
			this.passTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.passTxt.Location = new System.Drawing.Point(0, 23);
			this.passTxt.Multiline = true;
			this.passTxt.Name = "passTxt";
			this.passTxt.Size = new System.Drawing.Size(456, 188);
			this.passTxt.TabIndex = 6;
			this.passTxt.Validated += new System.EventHandler(this.passTxt_Validated);
			this.passTxt.Validating += new System.ComponentModel.CancelEventHandler(this.passText_Validating);
			// 
			// passTypeGrp
			// 
			this.passTypeGrp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.passTypeGrp.Controls.Add(this.passTypeText);
			this.passTypeGrp.Controls.Add(this.passTypeHex);
			this.passTypeGrp.Controls.Add(this.passTypeRandom);
			this.passTypeGrp.Location = new System.Drawing.Point(0, 0);
			this.passTypeGrp.Name = "passTypeGrp";
			this.passTypeGrp.Size = new System.Drawing.Size(456, 27);
			this.passTypeGrp.TabIndex = 5;
			// 
			// passTypeText
			// 
			this.passTypeText.AutoSize = true;
			this.passTypeText.Checked = true;
			this.passTypeText.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.passTypeText.Location = new System.Drawing.Point(3, 3);
			this.passTypeText.Name = "passTypeText";
			this.passTypeText.Size = new System.Drawing.Size(46, 17);
			this.passTypeText.TabIndex = 0;
			this.passTypeText.TabStop = true;
			this.passTypeText.Text = "Text";
			this.passTypeText.UseVisualStyleBackColor = true;
			this.passTypeText.CheckedChanged += new System.EventHandler(this.passType_CheckedChanged);
			// 
			// passTypeHex
			// 
			this.passTypeHex.AutoSize = true;
			this.passTypeHex.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.passTypeHex.Location = new System.Drawing.Point(55, 3);
			this.passTypeHex.Name = "passTypeHex";
			this.passTypeHex.Size = new System.Drawing.Size(86, 17);
			this.passTypeHex.TabIndex = 1;
			this.passTypeHex.Text = "Hexadecimal";
			this.passTypeHex.UseVisualStyleBackColor = true;
			this.passTypeHex.CheckedChanged += new System.EventHandler(this.passType_CheckedChanged);
			// 
			// passTypeRandom
			// 
			this.passTypeRandom.AutoSize = true;
			this.passTypeRandom.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.passTypeRandom.Location = new System.Drawing.Point(147, 3);
			this.passTypeRandom.Name = "passTypeRandom";
			this.passTypeRandom.Size = new System.Drawing.Size(65, 17);
			this.passTypeRandom.TabIndex = 2;
			this.passTypeRandom.Text = "Random";
			this.passTypeRandom.UseVisualStyleBackColor = true;
			this.passTypeRandom.CheckedChanged += new System.EventHandler(this.passType_CheckedChanged);
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// CustomMethodPassEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.passTxt);
			this.Controls.Add(this.passTypeGrp);
			this.Name = "CustomMethodPassEditor";
			this.Size = new System.Drawing.Size(456, 211);
			this.Validating += new System.ComponentModel.CancelEventHandler(this.CustomMethodPassEditor_Validating);
			this.passTypeGrp.ResumeLayout(false);
			this.passTypeGrp.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox passTxt;
		private System.Windows.Forms.FlowLayoutPanel passTypeGrp;
		private System.Windows.Forms.RadioButton passTypeText;
		private System.Windows.Forms.RadioButton passTypeHex;
		private System.Windows.Forms.RadioButton passTypeRandom;
		private System.Windows.Forms.ErrorProvider errorProvider;
	}
}
