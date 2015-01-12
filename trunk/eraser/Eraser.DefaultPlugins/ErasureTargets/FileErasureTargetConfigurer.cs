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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Text.RegularExpressions;
using System.IO;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	partial class FileErasureTargetConfigurer : UserControl,
		IErasureTargetConfigurer, IDragAndDropConfigurerFactory<IErasureTarget>
	{
		public FileErasureTargetConfigurer()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
		}

		#region IConfigurer<ErasureTarget> Members

		public void LoadFrom(IErasureTarget target)
		{
			FileErasureTarget file = target as FileErasureTarget;
			if (file == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			filePath.Text = file.Path;
		}

		public bool SaveTo(IErasureTarget target)
		{
			FileErasureTarget file = target as FileErasureTarget;
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

		#region ICliConfigurer<ErasureTarget> Members

		public string Help()
		{
			return S._(@"file                Erases the specified file
  argument: file=<path>");
		}

		public bool ProcessArgument(string argument)
		{
			Regex regex = new Regex("file=(?<fileName>.*)",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
			Match match = regex.Match(argument);

			if (match.Groups["fileName"].Success)
			{
				filePath.Text = match.Groups["fileName"].Value;
				return true;
			}
			
			try	
			{
				if (File.Exists(argument))
				{
					filePath.Text = argument;
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
				if (Directory.Exists(file))
				{
					FolderErasureTarget target = new FolderErasureTarget();
					target.Path = file;
					result.Add(target);
				}
			}

			return result;
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
