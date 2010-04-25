﻿namespace Eraser
{
	partial class BlackBoxUploadForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BlackBoxUploadForm));
            this.ButtonsBevel = new Trustbridge.Windows.Controls.BevelLine();
            this.ButtonsPnl = new System.Windows.Forms.Panel();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.TitleLbl = new System.Windows.Forms.Label();
            this.ProgressPb = new System.Windows.Forms.ProgressBar();
            this.UploadWorker = new System.ComponentModel.BackgroundWorker();
            this.ProgressLbl = new System.Windows.Forms.Label();
            this.ButtonsPnl.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonsBevel
            // 
            this.ButtonsBevel.AccessibleDescription = null;
            this.ButtonsBevel.AccessibleName = null;
            resources.ApplyResources(this.ButtonsBevel, "ButtonsBevel");
            this.ButtonsBevel.Angle = 90;
            this.ButtonsBevel.BackgroundImage = null;
            this.ButtonsBevel.Font = null;
            this.ButtonsBevel.Name = "ButtonsBevel";
            // 
            // ButtonsPnl
            // 
            this.ButtonsPnl.AccessibleDescription = null;
            this.ButtonsPnl.AccessibleName = null;
            resources.ApplyResources(this.ButtonsPnl, "ButtonsPnl");
            this.ButtonsPnl.BackColor = System.Drawing.SystemColors.Control;
            this.ButtonsPnl.BackgroundImage = null;
            this.ButtonsPnl.Controls.Add(this.ButtonsBevel);
            this.ButtonsPnl.Controls.Add(this.CancelBtn);
            this.ButtonsPnl.Font = null;
            this.ButtonsPnl.Name = "ButtonsPnl";
            // 
            // CancelBtn
            // 
            this.CancelBtn.AccessibleDescription = null;
            this.CancelBtn.AccessibleName = null;
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.BackgroundImage = null;
            this.CancelBtn.Font = null;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // TitleLbl
            // 
            this.TitleLbl.AccessibleDescription = null;
            this.TitleLbl.AccessibleName = null;
            resources.ApplyResources(this.TitleLbl, "TitleLbl");
            this.TitleLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.TitleLbl.Name = "TitleLbl";
            // 
            // ProgressPb
            // 
            this.ProgressPb.AccessibleDescription = null;
            this.ProgressPb.AccessibleName = null;
            resources.ApplyResources(this.ProgressPb, "ProgressPb");
            this.ProgressPb.BackgroundImage = null;
            this.ProgressPb.Font = null;
            this.ProgressPb.Name = "ProgressPb";
            // 
            // UploadWorker
            // 
            this.UploadWorker.WorkerReportsProgress = true;
            this.UploadWorker.WorkerSupportsCancellation = true;
            this.UploadWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.UploadWorker_DoWork);
            this.UploadWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.UploadWorker_RunWorkerCompleted);
            this.UploadWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.UploadWorker_ProgressChanged);
            // 
            // ProgressLbl
            // 
            this.ProgressLbl.AccessibleDescription = null;
            this.ProgressLbl.AccessibleName = null;
            resources.ApplyResources(this.ProgressLbl, "ProgressLbl");
            this.ProgressLbl.Font = null;
            this.ProgressLbl.Name = "ProgressLbl";
            // 
            // BlackBoxUploadForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.BackgroundImage = null;
            this.Controls.Add(this.ProgressLbl);
            this.Controls.Add(this.ProgressPb);
            this.Controls.Add(this.TitleLbl);
            this.Controls.Add(this.ButtonsPnl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "BlackBoxUploadForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BlackBoxUploadForm_FormClosing);
            this.ButtonsPnl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private Trustbridge.Windows.Controls.BevelLine ButtonsBevel;
		private System.Windows.Forms.Panel ButtonsPnl;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Label TitleLbl;
		private System.Windows.Forms.ProgressBar ProgressPb;
		private System.ComponentModel.BackgroundWorker UploadWorker;
		private System.Windows.Forms.Label ProgressLbl;
	}
}