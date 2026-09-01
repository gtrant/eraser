/* 
 * $Id: ManagerLibrary.cs 2998 2026-07-18 22:51:27Z gtrant $
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
using System.Reflection;

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
				if (Host.Instance != null)
				{
					Host.Instance.PluginLoad -= OnPluginLoad;
					Host.Instance.Dispose();
				}
				entropyPoller.Abort();
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
			try
			{
				// First, perform comprehensive security validation
				if (!PluginSecurityValidator.ValidatePluginSecurity(e.Plugin))
				{
					Logger.Log(string.Format("Plugin failed security validation: {0}", 
						e.Plugin.Assembly?.FullName ?? "Unknown"), LogLevel.Warning);
					RecordPluginRejection(e.Plugin, "Security validation failed");
					e.Load = false;
					return;
				}

				//Check for explicit approval or denial
				IDictionary<Guid, bool> approvals = Settings.PluginApprovals;
				
				if (approvals.ContainsKey(e.Plugin.AssemblyInfo.Guid))
				{
					bool approved = approvals[e.Plugin.AssemblyInfo.Guid];
					e.Load = approved;
					
					if (approved)
					{
						Logger.Log(string.Format("Plugin explicitly approved: {0}", 
							e.Plugin.Assembly.FullName), LogLevel.Information);
						RecordPluginApproval(e.Plugin, "Explicit approval");
					}
					else
					{
						Logger.Log(string.Format("Plugin explicitly denied: {0}", 
							e.Plugin.Assembly.FullName), LogLevel.Warning);
						RecordPluginRejection(e.Plugin, "Explicit denial");
					}
					return;
				}

				// For plugins without explicit approval/denial, apply enhanced validation
				bool shouldLoad = ShouldLoadUnknownPlugin(e.Plugin);
				e.Load = shouldLoad;

				if (shouldLoad)
				{
					Logger.Log(string.Format("Plugin approved for loading: {0}", 
						e.Plugin.Assembly.FullName), LogLevel.Information);
					RecordPluginApproval(e.Plugin, "Passed validation checks");
				}
				else
				{
					Logger.Log(string.Format("Plugin rejected: {0}", 
						e.Plugin.Assembly.FullName), LogLevel.Warning);
					RecordPluginRejection(e.Plugin, "Failed validation criteria");
				}
			}
			catch (Exception ex)
			{
				// Never let plugin validation errors crash the application
				Logger.Log(string.Format("Error during plugin validation: {0}", ex.Message), LogLevel.Error);
				RecordPluginRejection(e.Plugin, $"Validation error: {ex.Message}");
				e.Load = false;
			}
		}

		/// <summary>
		/// Determines if an unknown plugin should be loaded based on enhanced criteria.
		/// </summary>
		/// <param name="plugin">The plugin to evaluate.</param>
		/// <returns>True if the plugin should be loaded.</returns>
		private bool ShouldLoadUnknownPlugin(PluginInfo plugin)
		{
			try
			{
				// Check loading policy
				if (plugin.LoadingPolicy == PluginLoadingPolicy.DefaultOff)
				{
					return false;
				}

				// Verify basic security requirements are met
				AssemblyName assemblyName = plugin.Assembly.GetName();
				
				// Must have a strong name
				if (assemblyName.GetPublicKey()?.Length == 0)
				{
					Logger.Log(string.Format("Plugin rejected: No strong name signature: {0}", 
						plugin.Assembly?.FullName ?? "Unknown"), LogLevel.Warning);
					return false;
				}

				// Must have Authenticode signature
				if (plugin.AssemblyAuthenticode == null)
				{
					Logger.Log(string.Format("Plugin rejected: No Authenticode signature: {0}", 
						plugin.Assembly?.FullName ?? "Unknown"), LogLevel.Warning);
					return false;
				}

				// Additional checks for non-core plugins
				if (!IsCorePlugin(plugin))
				{
					// For third-party plugins, require additional verification
					return ValidateThirdPartyPlugin(plugin);
				}

				return true;
			}
			catch (Exception ex)
			{
				Logger.Log(string.Format("Error evaluating unknown plugin: {0}", ex.Message), LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Validates additional requirements for third-party plugins.
		/// </summary>
		/// <param name="plugin">The plugin to validate.</param>
		/// <returns>True if the plugin meets third-party requirements.</returns>
		private bool ValidateThirdPartyPlugin(PluginInfo plugin)
		{
			// Check if third-party plugins are allowed
			if (!Settings.AllowThirdPartyPlugins)
			{
				Logger.Log(string.Format("Third-party plugin rejected by policy: {0}", 
					plugin.Assembly?.FullName ?? "Unknown"), LogLevel.Information);
				return false;
			}

			// Additional validation for third-party plugins could include:
			// - Certificate authority validation
			// - Plugin size limits
			// - Resource usage monitoring
			// - Sandboxing requirements

			return true;
		}

		/// <summary>
		/// Checks if a plugin is a core plugin.
		/// </summary>
		/// <param name="plugin">The plugin to check.</param>
		/// <returns>True if this is a core plugin.</returns>
		private bool IsCorePlugin(PluginInfo plugin)
		{
			string assemblyName = plugin.Assembly.GetName().Name;
			string[] corePlugins = { "Eraser.DefaultPlugins" };
			
			foreach (string corePlugin in corePlugins)
			{
				if (assemblyName.Equals(corePlugin, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			
			return false;
		}

		/// <summary>
		/// Records a plugin approval in the audit trail.
		/// </summary>
		/// <param name="plugin">The approved plugin.</param>
		/// <param name="reason">Reason for approval.</param>
		private void RecordPluginApproval(PluginInfo plugin, string reason)
		{
			try
			{
				string auditMessage = $"PLUGIN_APPROVED: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | " +
					$"Assembly: {plugin.Assembly?.FullName ?? "Unknown"} | " +
					$"GUID: {plugin.AssemblyInfo.Guid} | " +
					$"Version: {plugin.AssemblyInfo.Version} | " +
					$"Reason: {reason}";

				Logger.Log(auditMessage, LogLevel.Notice);
			}
			catch (Exception ex)
			{
				Logger.Log($"Failed to record plugin approval: {ex.Message}", LogLevel.Error);
			}
		}

		/// <summary>
		/// Records a plugin rejection in the audit trail.
		/// </summary>
		/// <param name="plugin">The rejected plugin.</param>
		/// <param name="reason">Reason for rejection.</param>
		private void RecordPluginRejection(PluginInfo plugin, string reason)
		{
			try
			{
				string auditMessage = plugin == null
    ? $"PLUGIN_REJECTED: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | Assembly: Unknown | GUID: {Guid.Empty} | Version: Unknown | Reason: {reason ?? "Unknown"}"
    : $"PLUGIN_REJECTED: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | " +
      $"Assembly: {plugin.Assembly?.FullName ?? "Unknown"} | " +
      $"GUID: plugin?.AssemblyInfo.Guid.ToString() ?? Guid.Empty.ToString()\n | " +
      $"Version: {{(plugin?.AssemblyInfo.Version?.ToString() ?? \"Unknown\")}}\n | " +
      $"Reason: {reason ?? "Unknown"}";

				Logger.Log(auditMessage, LogLevel.Warning);
			}
			catch (Exception ex)
			{
				Logger.Log($"Failed to record plugin rejection: {ex.Message}", LogLevel.Error);
			}
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