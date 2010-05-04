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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	public partial class FolderErasureTargetSettings : UserControl, IErasureTargetConfigurer
	{
		public FolderErasureTargetSettings()
		{
			InitializeComponent();
		}

		#region IErasureTargetConfigurer Members

		public void LoadFrom(ErasureTarget target)
		{
			FolderErasureTarget folder = target as FolderErasureTarget;
			if (folder == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			folderPath.Text = folder.Path;
			folderInclude.Text = folder.IncludeMask;
			folderExclude.Text = folder.ExcludeMask;
			folderDelete.Checked = folder.DeleteIfEmpty;
		}

		public bool SaveTo(ErasureTarget target)
		{
			FolderErasureTarget folder = target as FolderErasureTarget;
			if (folder == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			if (folderPath.Text.Length == 0)
			{
				errorProvider.SetError(folderPath, S._("Invalid folder path"));
				return false;
			}

			folder.Path = folderPath.Text;
			folder.IncludeMask = folderInclude.Text;
			folder.ExcludeMask = folderExclude.Text;
			folder.DeleteIfEmpty = folderDelete.Checked;
			return true;
		}

		#endregion

		private void folderBrowse_Click(object sender, EventArgs e)
		{
			try
			{
				folderDialog.SelectedPath = folderPath.Text;
				if (folderDialog.ShowDialog() == DialogResult.OK)
					folderPath.Text = folderDialog.SelectedPath;
			}
			catch (NotSupportedException)
			{
				MessageBox.Show(this, S._("The path you selected is invalid."), S._("Eraser"),
					MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
					Localisation.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}
		}
	}
}
