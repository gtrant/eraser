/* 
 * $Id: ManagerLibrary.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using System.Text;
using System.Runtime.Serialization;

using Eraser.Plugins;
using Eraser.Util;

namespace Eraser.Manager
{
	/// <summary>
	/// The library instance which initializes and cleans up data required for the
	/// library to function.
	/// </summary>
	public class ManagerLibrary : IDisposable
	{
		public ManagerLibrary(PersistentStore persistentStore)
		{
			if (Instance != null)
				throw new InvalidOperationException("Only one ManagerLibrary instance can " +
					"exist at any one time");

			Instance = this;
			Settings = new ManagerSettings(persistentStore);
			Host.Initialise(persistentStore);
			Host.Instance.PluginLoad += OnPluginLoad;
			Host.Instance.Load();

			//Initialise the Entropy Poller last since it depends on the Host.
			entropyPoller = new EntropyPoller();
		}

		~ManagerLibrary()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (Settings == null)
				return;

			if (disposing)
			{
				entropyPoller.Abort();
				Host.Instance.Dispose();
			}

			Settings = null;
			Instance = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void OnPluginLoad(object sender, PluginLoadEventArgs e)
		{
			//If the plugin does not have an approval or denial, check for the presence of
			//a valid signature.
			IDictionary<Guid, bool> approvals = Settings.PluginApprovals;
			if (!approvals.ContainsKey(e.Plugin.AssemblyInfo.Guid) &&
				(e.Plugin.Assembly.GetName().GetPublicKey().Length == 0 ||
				!Security.VerifyStrongName(e.Plugin.Assembly.Location) ||
				e.Plugin.AssemblyAuthenticode == null))
			{
				e.Load = false;
			}

			//Is there an approval or denial?
			else if (approvals.ContainsKey(e.Plugin.AssemblyInfo.Guid))
				e.Load = approvals[e.Plugin.AssemblyInfo.Guid];

			//There's no approval or denial, what is the specified loading policy?
			else
				e.Load = e.Plugin.LoadingPolicy != PluginLoadingPolicy.DefaultOff;
		}

		/// <summary>
		/// The global library instance.
		/// </summary>
		public static ManagerLibrary Instance { get; private set; }

		/// <summary>
		/// Gets the settings object representing the settings for the Eraser Manager.
		/// </summary>
		public ManagerSettings Settings
		{
			get;
			private set;
		}

		/// <summary>
		/// The entropy poller thread which will gather entropy and push it to
		/// the PRNGs.
		/// </summary>
		private EntropyPoller entropyPoller;
	}
}
