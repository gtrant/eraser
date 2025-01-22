/* 
 * $Id: UnusedSpaceErasureTargetConfigurer.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	partial class UnusedSpaceErasureTargetConfigurer : UserControl, IErasureTargetConfigurer
	{
		/// <summary>
		/// Represents an item in the list of drives.
		/// </summary>
		private class DriveItem
		{
			public override string ToString()
			{
				return Label;
			}

			/// <summary>
			/// The drive selected.
			/// </summary>
			public string Drive;

			/// <summary>
			/// The label of the drive.
			/// </summary>
			public string Label;

			/// <summary>
			/// The icon of the drive.
			/// </summary>
			public Icon Icon;
		}

		public UnusedSpaceErasureTargetConfigurer()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);

			//Populate the drives list
			foreach (VolumeInfo volume in VolumeInfo.Volumes.Concat(VolumeInfo.NetworkDrives))
			{
				DriveType driveType = volume.VolumeType;
				if (driveType != DriveType.Unknown &&
					driveType != DriveType.NoRootDirectory &&
					driveType != DriveType.CDRom)
				{
					//Skip drives which are not mounted: we cannot erase their unused space.
					if (!volume.IsMounted)
						continue;

					DriveItem item = new DriveItem();
					DirectoryInfo root = volume.MountPoints[0];

					item.Drive = root.FullName;
					item.Label = root.GetDescription();
					item.Icon = root.GetIcon();
					unusedDisk.Items.Add(item);
				}
			}

			if (unusedDisk.Items.Count != 0)
				unusedDisk.SelectedIndex = 0;
		}

		#region IConfigurer<ErasureTarget> Members

		public void LoadFrom(IErasureTarget target)
		{
			UnusedSpaceErasureTarget unused = target as UnusedSpaceErasureTarget;
			if (unused == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			foreach (object item in unusedDisk.Items)
				if (((DriveItem)item).Drive == unused.Drive)
					unusedDisk.SelectedItem = item;
			unusedClusterTips.Checked = unused.EraseClusterTips;
		}

		public bool SaveTo(IErasureTarget target)
		{
			UnusedSpaceErasureTarget unused = target as UnusedSpaceErasureTarget;
			if (unused == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			unused.Drive = ((DriveItem)unusedDisk.SelectedItem).Drive;
			unused.EraseClusterTips = unusedClusterTips.Checked;
			return true;
		}

		#endregion

		#region ICliConfigurer<ErasureTarget> Members

		public string Help()
		{
			return S._(@"unused              Erases unused space in the volume.
  arguments: unused=<drive>[,clusterTips[=(true|false)]]
  clusterTips     If specified, the drive's files will have their
                  cluster tips erased. This parameter accepts a Boolean
                  value (true/false) as an argument; if none is specified
                  true is assumed.");
		}

		public bool ProcessArgument(string argument)
		{
			//The unused space erasure target, taking the optional clusterTips
			//argument which defaults to true; if none is specified it's assumed
			//false
			Regex regex = new Regex("unused=(?<unusedVolume>.*)(?<unusedTips>,clusterTips(=(?<unusedTipsValue>true|false))?)?",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
			Match match = regex.Match(argument);

			string[] trueValues = new string[] { "yes", "true" };
			if (match.Groups["unusedVolume"].Success)
			{
				foreach (object item in unusedDisk.Items)
				if (((DriveItem)item).Drive.ToUpperInvariant() ==
					match.Groups["unusedVolume"].Value.ToUpperInvariant())
				{
					unusedDisk.SelectedItem = item;
				}
	
				if (!match.Groups["unusedTips"].Success)
					unusedClusterTips.Checked = false;
				else if (!match.Groups["unusedTipsValue"].Success)
					unusedClusterTips.Checked = true;
				else
					unusedClusterTips.Checked =
						trueValues.Contains(match.Groups["unusedTipsValue"].Value);

				return true;
			}

			return false;
		}

		#endregion

		private void unusedDisk_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index == -1)
				return;

			Graphics g = e.Graphics;
			DriveItem item = (DriveItem)unusedDisk.Items[e.Index];
			Color textColour = e.ForeColor;
			PointF textPos = e.Bounds.Location;
			textPos.X += item.Icon.Width + 4;
			textPos.Y += 2;

			//Set the text colour and background colour if the control is disabled
			if ((e.State & DrawItemState.Disabled) == 0)
				e.DrawBackground();
			else
			{
				g.FillRectangle(new SolidBrush(SystemColors.ButtonFace), e.Bounds);
				textColour = SystemColors.GrayText;
			}

			g.DrawIcon(item.Icon, e.Bounds.X + 2, e.Bounds.Y);
			g.DrawString(item.Label, e.Font, new SolidBrush(textColour), textPos);
			if ((e.State & DrawItemState.Focus) != 0)
				e.DrawFocusRectangle();
		}
	}
}
