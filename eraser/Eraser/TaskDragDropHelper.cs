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
using System.Text;

using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

using Eraser.Util;
using Eraser.Manager;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser
{
	static class TaskDragDropHelper
	{
		/// <summary>
		/// Parses a list of locations dropped on the target.
		/// </summary>
		/// <param name="e">The event argument.</param>
		/// <param name="recycleBin">A reference to a <typeparamref name="System.Boolean"/> value
		/// which holds whether the recycle bin was dropped.</param>
		/// <returns>The list of file paths dropped.</returns>
		public static ICollection<string> GetFiles(DragEventArgs e, out bool recycleBin)
		{
			//Get the file paths first.
			recycleBin = false;
			List<string> files = e.Data.GetDataPresent(DataFormats.FileDrop) ?
				new List<string>((string[])e.Data.GetData(DataFormats.FileDrop, false)) :
				new List<string>();

			//Then try to see if we have shell locations dropped on us.
			if (e.Data.GetDataPresent("Shell IDList Array"))
			{
				MemoryStream stream = (MemoryStream)e.Data.GetData("Shell IDList Array");
				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, buffer.Length);
				ShellCIDA cida = new ShellCIDA(buffer);

				if (cida.cidl > 0)
				{
					for (int i = 1; i <= cida.cidl; ++i)
					{
						if (cida.aoffset[i].Guid == Shell.KnownFolderIDs.RecycleBin)
						{
							recycleBin = true;
						}
					}
				}
			}

			return files;
		}

		/// <summary>
		/// Parses a list of locations dropped on the target, converting them to the appropriate
		/// IErasureTarget instance.
		/// </summary>
		/// <param name="e">The event argument.</param>
		/// <returns>A list of erasure targets which will erase all the files and directories
		/// as described by the drag-and-drop operation.</returns>
		public static ICollection<IErasureTarget> GetTargets(DragEventArgs e)
		{
			ICollection<IErasureTarget> result = new List<IErasureTarget>();
			foreach (IErasureTarget target in Host.Instance.ErasureTargetFactories)
			{
				//Skip targets not supporting IDragAndDropConfigurer
				if (!(target.Configurer is IDragAndDropConfigurerFactory<IErasureTarget>))
					continue;

				IDragAndDropConfigurerFactory<IErasureTarget> configurer =
					(IDragAndDropConfigurerFactory<IErasureTarget>)target.Configurer;
				foreach (IErasureTarget newTarget in configurer.ProcessArgument(e))
					result.Add(newTarget);
			}

			return result;
		}

		/// <summary>
		/// Handles the drag enter event.
		/// </summary>
		/// <param name="sender">The sender for the event.</param>
		/// <param name="e">Event argument for the drag & drop operation.</param>
		public static void OnDragEnter(Control control, DragEventArgs e,
			string descriptionMessage, ICollection<string> items)
		{
			//Replace the C# {0} with the %1 used by Windows.
			descriptionMessage = descriptionMessage.Replace("{0}", "%1");

			string descriptionInsert = string.Empty;
			string descriptionItemFormat = S._("{0}, ");
			foreach (string item in items)
			{
				if (descriptionInsert.Length < 259 &&
					(descriptionInsert.Length < 3 || descriptionInsert.Substring(descriptionInsert.Length - 3) != "..."))
				{
					string append = string.Format(CultureInfo.InvariantCulture,
						descriptionItemFormat, item);

					if (descriptionInsert.Length + append.Length > 259)
						descriptionInsert += ".....";
					else
						descriptionInsert += append;
				}
			}

			if (!string.IsNullOrEmpty(descriptionInsert))
				descriptionInsert = descriptionInsert.Remove(descriptionInsert.Length - 2);

			if (e.Data.GetDataPresent("DragImageBits"))
				DropTargetHelper.DragEnter(control, e.Data, new Point(e.X, e.Y), e.Effect,
					descriptionMessage, descriptionInsert);
		}

		public static void OnDrop(DragEventArgs e)
		{
			DropTargetHelper.Drop(e.Data, new Point(e.X, e.Y), e.Effect);
		}
	}
}
