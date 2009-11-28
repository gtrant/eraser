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
using System.Runtime.Serialization;

namespace Eraser.Manager
{
	/// <summary>
	/// The library instance which initializes and cleans up data required for the
	/// library to function.
	/// </summary>
	public class ManagerLibrary : IDisposable
	{
		public ManagerLibrary(SettingsManager settings)
		{
			if (Instance != null)
				throw new InvalidOperationException("Only one ManagerLibrary instance can " +
					"exist at any one time");

			Instance = this;
			SettingsManager = settings;

			EntropySourceManager = new EntropySourceManager();
			PRNGManager = new PrngManager();
			ErasureMethodManager = new ErasureMethodManager();
			FileSystemManager = new FileSystemManager();
			Host = new Plugin.DefaultHost();
			Host.Load();
		}

		~ManagerLibrary()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				EntropySourceManager.Poller.Abort();
				Host.Dispose();
				SettingsManager.Save();
			}

			SettingsManager = null;
			Instance = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// The global library instance.
		/// </summary>
		public static ManagerLibrary Instance { get; private set; }

		/// <summary>
		/// The global instance of the EntropySource Manager
		/// </summary>
		internal EntropySourceManager EntropySourceManager;

		/// <summary>
		/// The global instance of the PRNG Manager.
		/// </summary>
		internal PrngManager PRNGManager;

		/// <summary>
		/// The global instance of the Erasure method manager.
		/// </summary>
		internal ErasureMethodManager ErasureMethodManager;

		/// <summary>
		/// The global instance of the File System manager.
		/// </summary>
		internal FileSystemManager FileSystemManager;

		/// <summary>
		/// Global instance of the Settings manager.
		/// </summary>
		public SettingsManager SettingsManager { get; set; }

		/// <summary>
		/// Gets the settings object representing the settings for the Eraser
		/// Manager. This is just shorthand for the local classes.
		/// </summary>
		public static ManagerSettings Settings
		{
			get
			{
				if (settingsInstance == null)
					settingsInstance = new ManagerSettings();
				return settingsInstance;
			}
		}

		/// <summary>
		/// The singleton instance for <see cref="Settings"/>.
		/// </summary>
		private static ManagerSettings settingsInstance;

		/// <summary>
		/// The global instance of the Plugin host.
		/// </summary>
		internal Plugin.DefaultHost Host;
	}
}
