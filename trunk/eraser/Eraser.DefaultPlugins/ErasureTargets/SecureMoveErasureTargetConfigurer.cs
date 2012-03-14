/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
using System.IO;
using System.Text.RegularExpressions;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	partial class SecureMoveErasureTargetConfigurer : UserControl, IErasureTargetConfigurer
	{
		public SecureMoveErasureTargetConfigurer()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
		}

		#region IConfigurer<ErasureTarget> Members

		public void LoadFrom(IErasureTarget target)
		{
			SecureMoveErasureTarget secureMove = target as SecureMoveErasureTarget;
			if (secureMove == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			fromTxt.Text = secureMove.Path;
			toTxt.Text = secureMove.Destination;

			moveFolderRadio.Checked =
				File.Exists(secureMove.Path) || Directory.Exists(secureMove.Path) &&
				(File.GetAttributes(secureMove.Path) & FileAttributes.Directory) != 0;
		}

		public bool SaveTo(IErasureTarget target)
		{
			SecureMoveErasureTarget secureMove = target as SecureMoveErasureTarget;
			if (secureMove == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			secureMove.Path = fromTxt.Text;
			secureMove.Destination = toTxt.Text;
			return true;
		}

		#endregion

		#region ICliConfigurer<ErasureTarget> Members

		public string Help()
		{
			return S._(@"move                Securely moves a file/directory to a new location
  arguments: move=<source>|<destination>");
		}

		public bool ProcessArgument(string argument)
		{
			//The secure move source and target, which are separated by a pipe.
			Regex regex = new Regex("(move=)?(?<source>.*)\\|(?<target>.*)",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
			Match match = regex.Match(argument);

			if (match.Groups["source"].Success && match.Groups["target"].Success)
			{
				//Get the source and destination paths
				fromTxt.Text = match.Groups["source"].Value;
				toTxt.Text = match.Groups["target"].Value;

				//Check the folder radio button if the source is a folder.
				moveFileRadio.Checked = !(
					moveFolderRadio.Checked = Directory.Exists(fromTxt.Text));
				return true;
			}

			return false;
		}

		#endregion

		private void fromSelectButton_Click(object sender, EventArgs e)
		{
			if (moveFolderRadio.Checked)
				fromTxt.Text = SelectFolder(fromTxt.Text, S._("Select the Source folder"));
			else
				fromTxt.Text = SelectFile(fromTxt.Text, S._("Select the Source file"));
		}

		private void toSelectButton_Click(object sender, EventArgs e)
		{
			if (moveFolderRadio.Checked)
				toTxt.Text = SelectFolder(toTxt.Text, S._("Move Source folder to:"));
			else
				toTxt.Text = SaveFile(toTxt.Text, S._("Save Source file to"));
		}

		private string SelectFile(string currentPath, string description)
		{
			openFileDialog.FileName = currentPath;
			openFileDialog.Title = description;
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				return openFileDialog.FileName;
			}

			return string.Empty;
		}

		private string SaveFile(string currentPath, string description)
		{
			saveFileDialog.FileName = currentPath;
			saveFileDialog.Title = description;
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				return saveFileDialog.FileName;
			}

			return string.Empty;
		}

		private string SelectFolder(string currentPath, string description)
		{
			folderDialog.SelectedPath = currentPath;
			folderDialog.Description = description;
			if (folderDialog.ShowDialog(this) == DialogResult.OK)
			{
				return folderDialog.SelectedPath;
			}

			return string.Empty;
		}
	}
}
