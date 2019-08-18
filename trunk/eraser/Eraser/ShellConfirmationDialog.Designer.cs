namespace Eraser
{
	partial class ShellConfirmationDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShellConfirmationDialog));
            this.Image = new System.Windows.Forms.PictureBox();
            this.Message = new System.Windows.Forms.Label();
            this.YesBtn = new System.Windows.Forms.Button();
            this.NoBtn = new System.Windows.Forms.Button();
            this.OptionsButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.Image)).BeginInit();
            this.SuspendLayout();
            // 
            // Image
            // 
            resources.ApplyResources(this.Image, "Image");
            this.Image.Name = "Image";
            this.Image.TabStop = false;
            // 
            // Message
            // 
            resources.ApplyResources(this.Message, "Message");
            this.Message.Name = "Message";
            // 
            // YesBtn
            // 
            resources.ApplyResources(this.YesBtn, "YesBtn");
            this.YesBtn.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.YesBtn.Name = "YesBtn";
            this.YesBtn.UseVisualStyleBackColor = true;
            // 
            // NoBtn
            // 
            resources.ApplyResources(this.NoBtn, "NoBtn");
            this.NoBtn.DialogResult = System.Windows.Forms.DialogResult.No;
            this.NoBtn.Name = "NoBtn";
            this.NoBtn.UseVisualStyleBackColor = true;
            // 
            // OptionsButton
            // 
            resources.ApplyResources(this.OptionsButton, "OptionsButton");
            this.OptionsButton.Name = "OptionsButton";
            this.OptionsButton.UseVisualStyleBackColor = true;
            this.OptionsButton.Click += new System.EventHandler(this.OptionsButton_Click);
            // 
            // ShellConfirmationDialog
            // 
            this.AcceptButton = this.NoBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.NoBtn;
            this.Controls.Add(this.OptionsButton);
            this.Controls.Add(this.NoBtn);
            this.Controls.Add(this.YesBtn);
            this.Controls.Add(this.Message);
            this.Controls.Add(this.Image);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShellConfirmationDialog";
            this.ShowInTaskbar = false;
            ((System.ComponentModel.ISupportInitialize)(this.Image)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox Image;
		private System.Windows.Forms.Label Message;
		private System.Windows.Forms.Button YesBtn;
		private System.Windows.Forms.Button NoBtn;
		private System.Windows.Forms.Button OptionsButton;
	}
}