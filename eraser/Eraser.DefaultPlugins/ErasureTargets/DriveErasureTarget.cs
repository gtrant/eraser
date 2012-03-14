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
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.IO;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Plugins.Registrars;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Represents a partition to be erased.
	/// </summary>
	[Serializable]
	[Guid("12CA079F-0B7A-48fa-B221-73AA217C1781")]
	class DriveErasureTarget : ErasureTargetBase
	{
		public DriveErasureTarget()
		{
		}

		#region Serialization code
		protected DriveErasureTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			string volumeId = info.GetString("Volume");
			int physicalDriveIndex = info.GetInt32("PhysicalDrive");

			if (volumeId != null)
				Volume = new VolumeInfo(volumeId);
			else if (physicalDriveIndex != -1)
				PhysicalDrive = new PhysicalDriveInfo(physicalDriveIndex);
			else
				throw new InvalidDataException();
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Volume", Volume == null ? null : Volume.VolumeId);
			info.AddValue("PhysicalDrive", PhysicalDrive == null ? -1 : PhysicalDrive.Index);
		}
		#endregion

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override string Name
		{
			get { return S._("Drive/Partition"); }
		}

		public override string ToString()
		{
			if (PhysicalDrive != null)
			{
				return S._("Hard disk {0}", PhysicalDrive.Index);
			}
			else if (Volume != null)
			{
				if (Volume.IsReady && Volume.IsMounted)
					return S._("Partition: {0}", Volume.MountPoints[0].GetDescription());
				else if (Volume.IsReady && Volume.PhysicalDrive != null)
					return S._("Hard disk {0} Partition {1}", Volume.PhysicalDrive.Index,
						Volume.PhysicalDrive.Volumes.IndexOf(Volume) + 1);
				else
					return S._("Partition");
			}
			else
				return null;
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new DriveErasureTargetConfigurer(); }
		}

		public sealed override IErasureMethod EffectiveMethod
		{
			get
			{
				if (Method != ErasureMethodRegistrar.Default)
					return base.EffectiveMethod;

				return Host.Instance.ErasureMethods[
					Host.Instance.Settings.DefaultUnusedSpaceErasureMethod];
			}
		}

		/// <summary>
		/// The Volume to erase.
		/// </summary>
		public VolumeInfo Volume
		{
			get
			{
				return volume;
			}
			set
			{
				if (value != null && physicalDrive != null)
					throw new InvalidOperationException("The Drive Erasure target can only " +
						"erase a volume or a physical drive, not both.");
				volume = value;
			}
		}
		private VolumeInfo volume;

		/// <summary>
		/// The Physical drive to erase.
		/// </summary>
		public PhysicalDriveInfo PhysicalDrive
		{
			get
			{
				return physicalDrive;
			}
			set
			{
				if (value != null && volume != null)
					throw new InvalidOperationException("The Drive Erasure target can only " +
						"erase a volume or a physical drive, not both.");
				physicalDrive = value;
			}
		}
		private PhysicalDriveInfo physicalDrive;

		public override void Execute()
		{
			//Check for sufficient privileges to run the erasure.
			if (!Security.IsAdministrator())
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
					Environment.OSVersion.Version >= new Version(6, 0))
				{
					Logger.Log(S._("The program does not have the required permissions to erase " +
						"the unused space on disk. Run the program as an administrator and retry " +
						"the operation."), LogLevel.Error);
				}
				else
				{
					Logger.Log(S._("The program does not have the required permissions to erase " +
						"the unused space on disk."), LogLevel.Error);
				}

				return;
			}

			Progress = new SteppedProgressManager();
			ProgressManager stepProgress = new ProgressManager();
			Progress.Steps.Add(new SteppedProgressManagerStep(stepProgress, 1.0f,
				ToString()));
			FileStream stream = null;

			try
			{
				//Overwrite the entire drive
				IErasureMethod method = EffectiveMethod;
				if (Volume != null)
				{
					stepProgress.Total = Volume.TotalSize;
					stream = Volume.Open(FileAccess.ReadWrite, FileShare.ReadWrite);
				}
				else if (PhysicalDrive != null)
				{
					stepProgress.Total = PhysicalDrive.Size;
					PhysicalDrive.DeleteDriveLayout();
					stream = PhysicalDrive.Open(FileAccess.ReadWrite, FileShare.ReadWrite);
				}
				else
					throw new InvalidOperationException(S._("The Drive erasure target requires a " +
						"volume or physical drive selected for erasure."));

				//Calculate the size of the erasure
				stepProgress.Total = method.CalculateEraseDataSize(null, stepProgress.Total);

				//Then run the erase task
				method.Erase(stream, long.MaxValue, Host.Instance.Prngs.ActivePrng,
					delegate(long lastWritten, long totalData, int currentPass)
					{
						stepProgress.Completed += lastWritten;
						stepProgress.Tag = new int[] { currentPass, method.Passes };

						if (Task.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));
					}
				);
			}
			finally
			{
				Progress = null;
				stream.Close();
			}
		}
	}
}
