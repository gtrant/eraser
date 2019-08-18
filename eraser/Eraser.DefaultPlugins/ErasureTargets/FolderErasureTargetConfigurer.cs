/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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
	partial class FolderErasureTargetConfigurer : UserControl,
		IErasureTargetConfigurer, IDragAndDropConfigurerFactory<IErasureTarget>
	{
		public FolderErasureTargetConfigurer()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
		}

		#region IConfigurer<ErasureTarget> Members

		public void LoadFrom(IErasureTarget target)
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

		public bool SaveTo(IErasureTarget target)
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

		#region ICliConfigurer<ErasureTarget> Members

		public string Help()
		{
			return S._(@"dir                 Erases files and folders in the directory
  arguments: dir=<directory>[,-excludeMask][,+includeMask][,deleteIfEmpty[=true|false]]
    excludeMask     A wildcard expression for files and folders to
                    exclude.
    includeMask     A wildcard expression for files and folders to
                    include.
                    The include mask is applied before the exclude mask.
    deleteIfEmpty   Deletes the folder at the end of the erasure if it is
                    empty. If this parameter is not specified, it defaults
                    to true.");
		}

		public bool ProcessArgument(string argument)
		{
			//The directory target, taking a list of + and - wildcard expressions.
			Regex regex = new Regex("dir=(?<directoryName>.*)(?<directoryParams>(?<directoryExcludeMask>,-[^,]+)|(?<directoryIncludeMask>,\\+[^,]+)|(?<directoryDeleteIfEmpty>,deleteIfEmpty(=(?<directoryDeleteIfEmptyValue>true|false))?))*",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
			Match match = regex.Match(argument);

			string[] trueValues = new string[] { "yes", "true" };
			if (match.Groups["directoryName"].Success)
			{
				folderPath.Text = match.Groups["directoryName"].Value;
				if (!match.Groups["directoryDeleteIfEmpty"].Success)
					folderDelete.Checked = true;
				else if (!match.Groups["directoryDeleteIfEmptyValue"].Success)
					folderDelete.Checked = true;
				else
					folderDelete.Checked =
						trueValues.Contains(match.Groups["directoryDeleteIfEmptyValue"].Value);

				if (match.Groups["directoryExcludeMask"].Success)
					folderExclude.Text += match.Groups["directoryExcludeMask"].Value.Remove(0, 2) + ' ';
				if (match.Groups["directoryIncludeMask"].Success)
					folderInclude.Text += match.Groups["directoryIncludeMask"].Value.Remove(0, 2) + ' ';

				return true;
			}

			try
			{
				if (Directory.Exists(argument))
				{
					folderPath.Text = argument;
					folderDelete.Checked = false;
					folderInclude.Text = folderExclude.Text = string.Empty;
					return true;
				}
			}
			catch (NotSupportedException)
			{
			}

			return false;
		}

		#endregion

		#region IDragAndDropConfigurer<IErasureTarget> Members

		public ICollection<IErasureTarget> ProcessArgument(DragEventArgs e)
		{
			List<string> files = e.Data.GetDataPresent(DataFormats.FileDrop) ?
				new List<string>((string[])e.Data.GetData(DataFormats.FileDrop, false)) :
				new List<string>();

			List<IErasureTarget> result = new List<IErasureTarget>();
			foreach (string file in files)
			{
				if (File.Exists(file))
				{
					FileErasureTarget target = new FileErasureTarget();
					target.Path = file;
					result.Add(target);
				}
			}

			return result;
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
