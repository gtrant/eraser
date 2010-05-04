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
	public partial class FileErasureTargetSettings : UserControl, IErasureTargetConfigurer
	{
		public FileErasureTargetSettings()
		{
			InitializeComponent();
		}

		#region IErasureTargetConfigurer Members

		public void LoadFrom(ErasureTarget target)
		{
			FileTarget file = target as FileTarget;
			if (file == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			filePath.Text = file.Path;
		}

		public bool SaveTo(ErasureTarget target)
		{
			FileTarget file = target as FileTarget;
			if (file == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			if (filePath.Text.Length == 0)
			{
				errorProvider.SetError(filePath, S._("Invalid file path"));
				return false;
			}

			file.Path = filePath.Text;
			return true;
		}

		#endregion

		private void fileBrowse_Click(object sender, EventArgs e)
		{
			fileDialog.FileName = filePath.Text;
			if (fileDialog.ShowDialog() == DialogResult.OK)
				filePath.Text = fileDialog.FileName;
		}
	}
}
