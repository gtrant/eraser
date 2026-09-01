/* 
 * $Id: ShellConfirmationDialog.cs 2998 2026-07-18 22:51:27Z gtrant $
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

using Eraser.Util;
using System;
using System.Drawing;
using System.Windows.Forms;
using Task = Eraser.Manager.Task;

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
