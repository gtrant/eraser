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
using System.Text;
using System.Windows.Forms;

using Eraser.Manager;
using Eraser.Manager.Plugin;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	public sealed class DefaultPlugin : IPlugin
	{
		public void Initialize(Host host)
		{
			Settings = new DefaultPluginSettings();

			//Then register the erasure methods et al.
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new Gutmann());				//35 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new DoD_EcE());				//7 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new RCMP_TSSIT_OPS_II());	//7 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new Schneier());				//7 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new VSITR());				//7 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new DoD_E());				//3 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new HMGIS5Enhanced());		//3 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new USAF5020());				//3 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new USArmyAR380_19());		//3 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new GOSTP50739());			//2 passes
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new HMGIS5Baseline());		//1 pass
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new Pseudorandom());			//1 pass
			EraseCustom.RegisterAll();
			ManagerLibrary.Instance.ErasureMethodRegistrar.Add(new FirstLast16KB());

			ManagerLibrary.Instance.PrngRegistrar.Add(new RngCrypto());

			ManagerLibrary.Instance.EntropySourceRegistrar.Add(new KernelEntropySource());

			ManagerLibrary.Instance.FileSystemRegistrar.Add(new Fat12FileSystem());
			ManagerLibrary.Instance.FileSystemRegistrar.Add(new Fat16FileSystem());
			ManagerLibrary.Instance.FileSystemRegistrar.Add(new Fat32FileSystem());
			ManagerLibrary.Instance.FileSystemRegistrar.Add(new NtfsFileSystem());

			ManagerLibrary.Instance.ErasureTargetRegistrar.Add(new FileErasureTarget());
			ManagerLibrary.Instance.ErasureTargetRegistrar.Add(new FolderErasureTarget());
			ManagerLibrary.Instance.ErasureTargetRegistrar.Add(new RecycleBinErasureTarget());
			ManagerLibrary.Instance.ErasureTargetRegistrar.Add(new UnusedSpaceErasureTarget());
			ManagerLibrary.Instance.ErasureTargetRegistrar.Add(new SecureMoveErasureTarget());
			ManagerLibrary.Instance.ErasureTargetRegistrar.Add(new DriveErasureTarget());
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public string Name
		{
			get { return S._("Default Erasure Methods and PRNGs"); }
		}

		public string Author
		{
			get { return S._("The Eraser Project <eraser-development@lists.sourceforge.net>"); }
		}

		public bool Configurable
		{
			get { return true; }
		}

		public void DisplaySettings(Control parent)
		{
			SettingsForm form = new SettingsForm();
			form.ShowDialog();
		}

		/// <summary>
		/// The dictionary holding settings for this plugin.
		/// </summary>
		internal static DefaultPluginSettings Settings;
	}

	/// <summary>
	/// A concrete class to manage the settings for this plugin.
	/// </summary>
	internal class DefaultPluginSettings
	{
		public DefaultPluginSettings()
		{
			settings = Manager.ManagerLibrary.Instance.SettingsManager.ModuleSettings;
		}

		/// <summary>
		/// The First/last 16 kilobyte erasure method.
		/// </summary>
		public Guid FL16Method
		{
			get
			{
				return settings.GetValue<Guid>("FL16Method");
			}
			set
			{
				settings.SetValue("FL16Method", value);
			}
		}

		/// <summary>
		/// The set of custom erasure methods.
		/// </summary>
		public Dictionary<Guid, CustomErasureMethod> EraseCustom
		{
			get
			{
				return settings.GetValue<Dictionary<Guid, CustomErasureMethod>>("EraseCustom");
			}
			set
			{
				settings.SetValue("EraseCustom", value);
			}
		}

		/// <summary>
		/// The data store for our settings.
		/// </summary>
		Settings settings;
	}
}
