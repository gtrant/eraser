/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Garrett Trant <gtrant@users.sourceforge.net> 
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

using Eraser.Plugins;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	public sealed class DefaultPlugin : IPlugin
	{
		public void Initialize(PluginInfo info)
		{
			Settings = new DefaultPluginSettings(info.PersistentStore);

			//Then register the erasure methods et al.
			Host.Instance.ErasureMethods.Add(new Gutmann());			//35 passes
			Host.Instance.ErasureMethods.Add(new DoD_EcE());			//7 passes
			Host.Instance.ErasureMethods.Add(new RCMP_TSSIT_OPS_II());	//7 passes
			Host.Instance.ErasureMethods.Add(new Schneier());			//7 passes
			Host.Instance.ErasureMethods.Add(new VSITR());				//7 passes
			Host.Instance.ErasureMethods.Add(new DoD_E());				//3 passes
			Host.Instance.ErasureMethods.Add(new HMGIS5Enhanced());		//3 passes
			Host.Instance.ErasureMethods.Add(new USAF5020());			//3 passes
			Host.Instance.ErasureMethods.Add(new USArmyAR380_19());		//3 passes
			Host.Instance.ErasureMethods.Add(new GOSTP50739());			//2 passes
			Host.Instance.ErasureMethods.Add(new HMGIS5Baseline());		//1 pass
			Host.Instance.ErasureMethods.Add(new Pseudorandom());		//1 pass
			EraseCustom.RegisterAll();
			Host.Instance.ErasureMethods.Add(new FirstLast16KB());

			Host.Instance.Prngs.Add(new RngCrypto());

			Host.Instance.EntropySources.Add(new KernelEntropySource());

			Host.Instance.FileSystems.Add(new Fat12FileSystem());
			Host.Instance.FileSystems.Add(new Fat16FileSystem());
			Host.Instance.FileSystems.Add(new Fat32FileSystem());
			Host.Instance.FileSystems.Add(new NtfsFileSystem());

			Host.Instance.ErasureTargetFactories.Add(new FileErasureTarget());
			Host.Instance.ErasureTargetFactories.Add(new FolderErasureTarget());
			Host.Instance.ErasureTargetFactories.Add(new RecycleBinErasureTarget());
			Host.Instance.ErasureTargetFactories.Add(new UnusedSpaceErasureTarget());
			Host.Instance.ErasureTargetFactories.Add(new SecureMoveErasureTarget());
			Host.Instance.ErasureTargetFactories.Add(new DriveErasureTarget());
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
            using (SettingsForm form = new SettingsForm())
            {
                form.ShowDialog();
            }
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
		public DefaultPluginSettings(PersistentStore store)
		{
			Store = store;
		}

		/// <summary>
		/// The First/last 16 kilobyte erasure method.
		/// </summary>
		public Guid FL16Method
		{
			get
			{
				return Store.GetValue<Guid>("FL16Method");
			}
			set
			{
				Store.SetValue("FL16Method", value);
			}
		}

		/// <summary>
		/// The set of custom erasure methods.
		/// </summary>
		public Dictionary<Guid, CustomErasureMethod> EraseCustom
		{
			get
			{
				return Store.GetValue<Dictionary<Guid, CustomErasureMethod>>("EraseCustom");
			}
			set
			{
				Store.SetValue("EraseCustom", value);
			}
		}

		/// <summary>
		/// The data store for our settings.
		/// </summary>
		PersistentStore Store;
	}
}
