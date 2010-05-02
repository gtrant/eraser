using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser
{
	public partial class ShellConfirmationDialog : Form
	{
		public ShellConfirmationDialog(Task task)
		{
			Task = task;
			InitializeComponent();
			Theming.ApplyTheme(this);

			//Set the icon of the dialog
			Bitmap bitmap = new Bitmap(Image.Width, Image.Height);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.DrawIcon(SystemIcons.Exclamation, new Rectangle(Point.Empty, Image.Size));
			}
			Image.Image = bitmap;

			//Focus on the No button
			NoBtn.Focus();
		}

		/// <summary>
		/// The task which is being confirmed.
		/// </summary>
		private Task Task;

		private void OptionsButton_Click(object sender, EventArgs e)
		{
			using (TaskPropertiesForm form = new TaskPropertiesForm())
			{
				form.Task = Task;
				if (form.ShowDialog(this) == DialogResult.OK)
					Task = form.Task;
			}
		}
	}
}
