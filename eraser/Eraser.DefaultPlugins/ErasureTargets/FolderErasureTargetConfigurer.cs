﻿/* 
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

using System.Text.RegularExpressions;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	public partial class FolderErasureTargetConfigurer : UserControl, IErasureTargetConfigurer
	{
		public FolderErasureTargetConfigurer()
		{
			InitializeComponent();
		}

		#region IConfigurer<ErasureTarget> Members

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

		#region ICliConfigurer<ErasureTarget> Members

		public void Help()
		{
			throw new NotImplementedException();
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
					folderDelete.Checked = false;
				else if (!match.Groups["directoryDeleteIfEmptyValue"].Success)
					folderDelete.Checked = true;
				else
					folderDelete.Checked =
						trueValues.Contains(match.Groups["directoryDeleteIfEmptyValue"].Value);

				if (match.Groups["directoryExcludeMask"].Success)
					folderExclude.Text += match.Groups["directoryExcludeMask"].Value.Remove(0, 2) + ' ';
				if (match.Groups["directoryIncludeMask"].Success)
					folderInclude.Text += match.Groups["directoryIncludeMask"].Value.Remove(0, 2) + ' ';
			}

			return false;
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
