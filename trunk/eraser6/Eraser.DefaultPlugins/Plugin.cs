/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
			ErasureMethodManager.Register(new Gutmann());				//35 passes
			ErasureMethodManager.Register(new GutmannLite());			//10 passes
			ErasureMethodManager.Register(new DoD_EcE());				//7 passes
			ErasureMethodManager.Register(new RCMP_TSSIT_OPS_II());		//7 passes
			ErasureMethodManager.Register(new Schneier());				//7 passes
			ErasureMethodManager.Register(new VSITR());					//7 passes
			ErasureMethodManager.Register(new DoD_E());					//3 passes
			ErasureMethodManager.Register(new HMGIS5Enhanced());		//3 passes
			ErasureMethodManager.Register(new USAF5020());				//3 passes
			ErasureMethodManager.Register(new USArmyAR380_19());		//3 passes
			ErasureMethodManager.Register(new GOSTP50739());			//2 passes
			ErasureMethodManager.Register(new HMGIS5Baseline());		//1 pass
			ErasureMethodManager.Register(new Pseudorandom());			//1 pass
			EraseCustom.RegisterAll();
			ErasureMethodManager.Register(new FirstLast16KB());

			PrngManager.Register(new RngCrypto());

			FileSystemManager.Register(new Fat32FileSystem());
			FileSystemManager.Register(new NtfsFileSystem());
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
				return settings["FL16Method"] == null ? Guid.Empty :
					(Guid)settings["FL16Method"];
			}
			set
			{
				settings["FL16Method"] = value;
			}
		}

		/// <summary>
		/// The set of custom erasure methods.
		/// </summary>
		public Dictionary<Guid, CustomErasureMethod> EraseCustom
		{
			get
			{
				return (Dictionary<Guid, CustomErasureMethod>)settings["EraseCustom"];
			}
			set
			{
				settings["EraseCustom"] = value;
			}
		}

		/// <summary>
		/// The data store for our settings.
		/// </summary>
		Settings settings;
	}
}
