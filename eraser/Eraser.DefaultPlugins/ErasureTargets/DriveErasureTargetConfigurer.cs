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
using System.IO;
using System.Text.RegularExpressions;

using Eraser.Manager;
using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using System.Globalization;

namespace Eraser.DefaultPlugins
{
	public partial class DriveErasureTargetConfigurer : UserControl, IErasureTargetConfigurer
	{
		/// <summary>
		/// Represents an item in the list of drives.
		/// </summary>
		private class PartitionItem
		{
			public override string ToString()
			{
				if (!string.IsNullOrEmpty(Cache))
					return Cache;

				if (PhysicalDrive != null)
				{
					try
					{
						Cache = S._("Hard disk {0} ({1})", PhysicalDrive.Index,
							new FileSize(PhysicalDrive.Size));
					}
					catch (UnauthorizedAccessException)
					{
						Cache = S._("Hard disk {0}", PhysicalDrive.Index);
					}
				}
				else if (Volume != null)
				{
					try
					{
						if (Volume.IsMounted)
							Cache = Volume.MountPoints[0].GetDescription();
						else if (Volume.PhysicalDrive != null)
							Cache = S._("Partition {0} ({1})",
								Volume.PhysicalDrive.Volumes.IndexOf(Volume) + 1,
								new FileSize(Volume.TotalSize));
						else
							Cache = S._("Partition ({0})", new FileSize(Volume.TotalSize));
					}
					catch (UnauthorizedAccessException)
					{
						if (Volume.PhysicalDrive != null)
							Cache = S._("Partition {0}",
								Volume.PhysicalDrive.Volumes.IndexOf(Volume) + 1);
						else
							Cache = S._("Partition");
					}
				}
				else
					throw new InvalidOperationException();

				return Cache;
			}

			/// <summary>
			/// Stores the display text for rapid access.
			/// </summary>
			private string Cache;

			/// <summary>
			/// The Physical drive this partition refers to.
			/// </summary>
			public PhysicalDriveInfo PhysicalDrive;

			/// <summary>
			/// The volume this partition refers to.
			/// </summary>
			public VolumeInfo Volume;

			/// <summary>
			/// The icon of the drive.
			/// </summary>
			public Icon Icon;
		}

		public DriveErasureTargetConfigurer()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);

			//Populate the drives list
			List<VolumeInfo> volumes = new List<VolumeInfo>();
			foreach (PhysicalDriveInfo drive in PhysicalDriveInfo.Drives)
			{
				PartitionItem item = new PartitionItem();
				item.PhysicalDrive = drive;
				partitionCmb.Items.Add(item);

				foreach (VolumeInfo volume in drive.Volumes)
				{
					item = new PartitionItem();
					item.Volume = volume;

					if (volume.IsMounted)
					{
						DirectoryInfo root = volume.MountPoints[0];
						item.Icon = root.GetIcon();
					}
					
					partitionCmb.Items.Add(item);
					volumes.Add(volume);
				}
			}

			//And then add volumes which aren't accounted for (notably, Dynamic volumes)
			foreach (VolumeInfo volume in VolumeInfo.Volumes)
			{
				if (volumes.IndexOf(volume) == -1 && volume.VolumeType == DriveType.Fixed)
				{
					PartitionItem item = new PartitionItem();
					item.Volume = volume;

					if (volume.IsMounted)
					{
						DirectoryInfo root = volume.MountPoints[0];
						item.Icon = root.GetIcon();
					}

					partitionCmb.Items.Insert(0, item);
					volumes.Add(volume);
				}
			}

			if (partitionCmb.Items.Count != 0)
				partitionCmb.SelectedIndex = 0;
		}

		#region ICliConfigurer<ErasureTarget> Members

		public void Help()
		{
			throw new NotImplementedException();
		}

		public bool ProcessArgument(string argument)
		{
			//The hard disk index
			Regex hardDiskRegex = new Regex("^(drive=)?\\\\Device\\\\Harddisk(?<disk>[\\d]+)",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

			//PhysicalDrive index
			Regex physicalDriveIndex = new Regex("^(drive=)?\\\\\\\\\\.\\\\PhysicalDrive(?<disk>[\\d]+)",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

			//The volume GUID
			Regex volumeRegex = new Regex("^(drive=)?\\\\\\\\\\?\\\\Volume\\{(?<guid>([0-9a-f-]+))\\}",
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

			//Try to get the hard disk index.
			Match match = hardDiskRegex.Match(argument);
			if (!match.Groups["disk"].Success)
				match = physicalDriveIndex.Match(argument);
			if (match.Groups["disk"].Success)
			{
				//Get the index of the disk.
				int index = Convert.ToInt32(match.Groups["disk"].Value);

				//Create a physical drive info object for the target disk
				PhysicalDriveInfo target = new PhysicalDriveInfo(index);

				//Select it in the GUI.
				foreach (PartitionItem item in partitionCmb.Items)
					if (item.PhysicalDrive != null && item.PhysicalDrive.Equals(target))
						partitionCmb.SelectedItem = item;

				return true;
			}

			//Try to get the volume GUID
			match = volumeRegex.Match(argument);
			if (match.Groups["guid"].Success)
			{
				//Find the volume GUID
				Guid guid = new Guid(match.Groups["guid"].Value);

				//Create a volume info object for the target volume
				VolumeInfo target = new VolumeInfo(string.Format(CultureInfo.InvariantCulture,
					"\\\\?\\Volume{{{0}}}\\", guid));

				//Select it in the GUI.
				foreach (PartitionItem item in partitionCmb.Items)
					if (item.Volume != null && item.Volume.Equals(target))
						partitionCmb.SelectedItem = item;
				
				return true;
			}

			return false;
		}

		#endregion

		#region IConfigurer<ErasureTarget> Members

		public void LoadFrom(ErasureTarget target)
		{
			DriveErasureTarget partition = target as DriveErasureTarget;
			if (partition == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			foreach (PartitionItem item in partitionCmb.Items)
				if ((item.PhysicalDrive != null &&
						item.PhysicalDrive.Equals(partition.PhysicalDrive)) ||
					(item.Volume != null && item.Volume.Equals(partition.Volume)))
				{
					partitionCmb.SelectedItem = item;
					break;
				}
		}

		public bool SaveTo(ErasureTarget target)
		{
			DriveErasureTarget partition = target as DriveErasureTarget;
			if (partition == null)
				throw new ArgumentException("The provided erasure target type is not " +
					"supported by this configurer.");

			PartitionItem item = (PartitionItem)partitionCmb.SelectedItem;

			//Make sure we don't set both Volume and PhysicalDrive
			partition.PhysicalDrive = null;

			//Then set the proper values.
			partition.Volume = item.Volume;
			partition.PhysicalDrive = item.PhysicalDrive;
			return true;
		}

		#endregion

		private void OnDrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index == -1)
				return;

			Graphics g = e.Graphics;
			PartitionItem item = (PartitionItem)partitionCmb.Items[e.Index];
			Color textColour = e.ForeColor;
			PointF textPos = e.Bounds.Location;
			if (item.Icon != null)
				textPos.X += item.Icon.Width + 4;
			textPos.Y += 1;

			//Set the text colour and background colour if the control is disabled
			if ((e.State & DrawItemState.Disabled) == 0)
				e.DrawBackground();
			else
			{
				g.FillRectangle(new SolidBrush(SystemColors.ButtonFace), e.Bounds);
				textColour = SystemColors.GrayText;
			}

			if (item.Icon != null)
				g.DrawIcon(item.Icon, e.Bounds.X + 2, e.Bounds.Y);
			g.DrawString(item.ToString(), e.Font, new SolidBrush(textColour), textPos);
			if ((e.State & DrawItemState.Focus) != 0)
				e.DrawFocusRectangle();
		}
	}
}
