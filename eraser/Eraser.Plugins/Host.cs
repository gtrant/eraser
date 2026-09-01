/* 
 * $Id: Host.cs 2998 2026-07-18 22:51:27Z gtrant $
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
using System.Linq;

using System.IO;
using System.Reflection;
using System.Security;

using Eraser.Util;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Plugins.Registrars;
using System.Runtime.InteropServices;

namespace Eraser.Plugins
{
	/// <summary>
	/// The plugins host interface which is used for communicating with the host
	/// program.
	/// </summary>
	/// <remarks>Remember to call Load to load the plugins into memory, otherwise
	/// they will never be loaded.</remarks>
	public abstract class Host : IDisposable
	{
		#region IDisposable members
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Instance = null;
			}
		}

		/// <summary>
		/// Cleans up resources used by the host. Also unloads all loaded plugins.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		/// <summary>
		/// Initialises the Plugins library. This allows you to specify the registrars
		/// in charge of every extension point defined.
		/// 
		/// Call <see cref="Host.Load"/> when object
		/// initialisation is complete.
		/// </summary>
		/// <param name="store">The root persistent store for all plugins.</param>
		/// <remarks>Call <see cref="Host.Instance.Dispose"/> when exiting.</remarks>
		public static void Initialise(PersistentStore store)
		{
			new DefaultHost(store);
		}

		/// <summary>
		/// Constructor. Sets the global Plugin Host instance.
		/// </summary>
		/// <param name="store">The root persistent store for all plugins.</param>
		/// <see cref="Host.Instance"/>
		protected Host(PersistentStore store)
		{
			if (Instance != null)
				throw new InvalidOperationException("Only one global Plugin Host instance can " +
					"exist at any one point of time.");
			Instance = this;
			PersistentStore = store;
			Settings = new Settings(PersistentStore);

			EntropySources = new EntropySourceRegistrar();
			Prngs = new PrngRegistrar();
			ErasureMethods = new ErasureMethodRegistrar();
			ErasureTargetFactories = new ErasureTargetFactoryRegistrar();
			FileSystems = new FileSystemRegistrar();
			ClientTools = new ClientToolRegistrar();
			Notifiers = new NotifierRegistrar();
		}

		/// <summary>
		/// Getter that retrieves the global plugin host instance.
		/// </summary>
		public static Host Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// The root persistent store for all plugins.
		/// </summary>
		public PersistentStore PersistentStore
		{
			get;
			private set;
		}

		/// <summary>
		/// Global settings for all plugins.
		/// </summary>
		public Settings Settings
		{
			get;
			private set;
		}

		#region Plugin Loading and Management

		/// <summary>
		/// Retrieves the list of currently loaded plugins.
		/// </summary>
		/// <remarks>The returned list is read-only</remarks>
		public abstract IList<PluginInfo> Plugins
		{
			get;
		}

		/// <summary>
		/// Loads all plugins into memory.
		/// </summary>
		public abstract void Load();

		/// <summary>
		/// Loads a plugin.
		/// </summary>
		/// <param name="filePath">The absolute or relative file path to the
		/// DLL.</param>
		/// <returns>True if the plugin is loaded, false otherwise.</returns>
		/// <remarks>If a plugin is loaded twice, this function should do nothing
		/// and return True.</remarks>
		public abstract bool LoadPlugin(string filePath);

		/// <summary>
		/// The plugin load event, allowing clients to decide whether to load
		/// the given plugin.
		/// </summary>
		public EventHandler<PluginLoadEventArgs> PluginLoad { get; set; }

		/// <summary>
		/// The plugin loaded event.
		/// </summary>
		public EventHandler<PluginLoadedEventArgs> PluginLoaded { get; set; }

		/// <summary>
		/// Event callback executor for the OnPluginLoad event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnPluginLoad(object sender, PluginLoadEventArgs e)
		{
			if (PluginLoad != null)
				PluginLoad(sender, e);
		}

		/// <summary>
		/// Event callback executor for the OnPluginLoaded vent
		/// </summary>
		internal void OnPluginLoaded(object sender, PluginLoadedEventArgs e)
		{
			if (PluginLoaded != null)
				PluginLoaded(sender, e);
		}

		#endregion

		#region Type Registrars

		/// <summary>
		/// The global instance of the EntropySource Manager
		/// </summary>
		public EntropySourceRegistrar EntropySources
		{
			get;
			private set;
		}

		/// <summary>
		/// The global instance of the PRNG Manager.
		/// </summary>
		public PrngRegistrar Prngs
		{
			get;
			private set;
		}

		/// <summary>
		/// The global instance of the Erasure method Manager.
		/// </summary>
		public ErasureMethodRegistrar ErasureMethods
		{
			get;
			private set;
		}

		/// <summary>
		/// The global instance of the Erasure target Manager.
		/// </summary>
		public ErasureTargetFactoryRegistrar ErasureTargetFactories
		{
			get;
			private set;
		}

		/// <summary>
		/// The global instance of the File System manager.
		/// </summary>
		public FileSystemRegistrar FileSystems
		{
			get;
			private set;
		}

		/// <summary>
		/// The global instance of the Client Tools Registrar.
		/// </summary>
		public ClientToolRegistrar ClientTools
		{
			get;
			private set;
		}

		/// <summary>
		/// The global instance of the Notifier Registrar.
		/// </summary>
		public NotifierRegistrar Notifiers
		{
			get;
			private set;
		}
		#endregion
	}

	/// <summary>
	/// The default plugins host implementation.
	/// </summary>
	internal class DefaultHost : Host
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public DefaultHost(PersistentStore store)
			: base(store)
		{
			//Specify additional places to load assemblies from
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
		}

		protected override void Dispose(bool disposing)
		{
			if (plugins == null)
				return;

			if (disposing)
			{
				//Unload all the plugins. This will cause all the plugins to execute
				//the cleanup code.
				foreach (PluginInfo plugin in plugins)
					if (plugin.Plugin != null)
						plugin.Plugin.Dispose();
			}

			plugins = null;
			base.Dispose(disposing);
		}

		public override void Load()
		{
			//Load all core plugins first
			foreach (string name in CorePlugins)
			{
				if (!LoadPlugin(new AssemblyName(name)))
					throw new FileLoadException(S._("The required Core plugin {0} could not be " +
						"loaded. Repair the Eraser installation and try again.", name));
			}

			//Then load the rest
			foreach (string fileName in Directory.GetFiles(PluginsFolder))
			{
				FileInfo file = new FileInfo(fileName);
				if (file.Extension.Equals(".dll"))
					try
					{
						LoadPlugin(file.FullName);
					}
					catch (BadImageFormatException ex)
					{
						// Log bad image format - could be non-.NET DLL or corrupted
						Logger.Log(string.Format("Invalid plugin format: {0} - {1}", file.FullName, ex.Message), LogLevel.Warning);
						RecordFailedPluginLoad(file.FullName, "BadImageFormat", ex);
					}
					catch (FileLoadException ex)
					{
						// Log file load errors - could be dependency issues or access problems
						Logger.Log(string.Format("Plugin load failed: {0} - {1}", file.FullName, ex.Message), LogLevel.Error);
						RecordFailedPluginLoad(file.FullName, "LoadFailure", ex);
					}
					catch (SecurityException ex)
					{
						// Log security violations - this is critical for a security application
						Logger.Log(string.Format("Security violation loading plugin: {0} - {1}", file.FullName, ex.Message), LogLevel.Error);
						RecordFailedPluginLoad(file.FullName, "SecurityViolation", ex);
					}
					catch (Exception ex)
					{
						// Log any other unexpected errors during plugin loading
						Logger.Log(string.Format("Unexpected error loading plugin: {0} - {1}", file.FullName, ex.Message), LogLevel.Error);
						RecordFailedPluginLoad(file.FullName, "UnexpectedError", ex);
					}
			}
		}

		/// <summary>
		/// Verifies whether the provided assembly is a plugin.
		/// </summary>
		/// <param name="assembly">The assembly to verify.</param>
		/// <returns>True if the assembly provided is a plugin, false otherwise.</returns>
		private static bool IsPlugin(Assembly assembly)
		{
			//Iterate over every exported type, checking if it implements IPlugin
			Type typePlugin = assembly.GetExportedTypes().FirstOrDefault(
					type => type.GetInterface("Eraser.Plugins.IPlugin", true) != null);

			//If the typePlugin type is empty, the assembly doesn't implement IPlugin and thus
			//it is not a plugin.
			return typePlugin != null;
		}

		private bool LoadPlugin(AssemblyName name)
		{
			//Check the plugins folder
			foreach (string fileName in Directory.GetFiles(PluginsFolder))
			{
				FileInfo file = new FileInfo(fileName);
				if (file.Extension == ".dll")
					try
					{
						// .NET Core/5+ does not support ReflectionOnlyLoadFrom.
						// We use AssemblyName.GetAssemblyName to inspect the assembly identity 
						// without loading the actual bytes into the execution context.
						AssemblyName discoveredName = AssemblyName.GetAssemblyName(file.FullName);
						if (AssemblyName.ReferenceMatchesDefinition(name, discoveredName))
						{
							// Now load it properly for use
							Assembly assembly = Assembly.LoadFrom(file.FullName);
							return LoadPlugin(assembly);
						}
					}
					catch (BadImageFormatException ex)
					{
						Logger.Log(string.Format("Plugin load by name - invalid format: {0} - {1}", file.FullName, ex.Message), LogLevel.Debug);
					}
					catch (FileLoadException ex)
					{
						Logger.Log(string.Format("Plugin load by name - load failed: {0} - {1}", file.FullName, ex.Message), LogLevel.Debug);
					}
			}

			return false;
		}

		public override bool LoadPlugin(string filePath)
		{
			// .NET Core/5+ does not support ReflectionOnlyLoadFrom.
			// We load directly using LoadFrom instead.
			return LoadPlugin(Assembly.LoadFrom(filePath));
		}

		/// <summary>
		/// Checks the provided assembly for its name and attempts to load it using the
		/// plugin loading rules.
		/// </summary>
		/// <param name="assembly">The plugin to load. This assembly can be loaded
		/// in the reflection-only context for security.</param>
		/// <returns>True if the assembly was a plugin and loaded without error.</returns>
		private bool LoadPlugin(Assembly assembly)
		{
			PluginInfo instance = new PluginInfo(assembly, null);

			//Check that the plugin hasn't yet been loaded.
			if (Plugins.Count(
					plugin => plugin.Assembly.GetName().FullName ==
					assembly.GetName().FullName) > 0)
			{
				return true;
			}

			//Ignore non-plugins
			if (!IsPlugin(instance.Assembly))
				return false;

			//OK this assembly is a plugin
			lock (plugins)
				plugins.Add(instance);

			//Load the plugin, depending on type
			bool result = instance.LoadingPolicy == PluginLoadingPolicy.Core ?
				LoadCorePlugin(instance) : LoadNonCorePlugin(instance);
			if (result)
			{
				//And broadcast the plugin load event
				OnPluginLoaded(this, new PluginLoadedEventArgs(instance));
			}

			return result;
		}

		/// <summary>
		/// Verifies the assembly name and strong name of a plugin, ensuring that the assembly
		/// contains a core plugin before loading and initialising it.
		/// </summary>
		/// <param name="info">The plugin to load.</param>
		/// <returns>True if the plugin was loaded.</returns>
		private bool LoadCorePlugin(PluginInfo info)
		{
			//Check that this plugin's name appears in our list of core plugins, otherwise this
			//is a phony
			if (CorePlugins.Count(x => x == info.Assembly.GetName().Name) == 0)
			{
				info.LoadingPolicy = PluginLoadingPolicy.None;
				return LoadNonCorePlugin(info);
			}

			//Check for the presence of a valid signature: Core plugins must have the same
			//public key as the current assembly
			if (!info.Assembly.GetName().GetPublicKey().SequenceEqual(
					Assembly.GetExecutingAssembly().GetName().GetPublicKey()))
			{
				throw new FileLoadException(S._("The provided Core plugin does not have an " +
					"identical public key as the Eraser assembly.\n\nCheck that the Eraser " +
					"installation is not corrupt, or reinstall the program."));
			}

			//Load the plugin.
			info.Load(this);
			return true;
		}

		/// <summary>
		/// Queries the Plugin Host's owner on whether to load the current plugin.
		/// </summary>
		/// <param name="info">The plugin to load.</param>
		/// <returns>True if the plugin was loaded.</returns>
		private bool LoadNonCorePlugin(PluginInfo info)
		{
			PluginLoadEventArgs e = new PluginLoadEventArgs(info);
			OnPluginLoad(this, e);
			
			if (e.Load)
				info.Load(this);

			return e.Load;
		}

		private static bool AssemblyMatchesName(Assembly assembly, AssemblyName name)
		{
			AssemblyName assemblyName = assembly.GetName();
			return (name.Name == assemblyName.Name &&
				(name.Version == null || name.Version == assemblyName.Version) &&
				(name.ProcessorArchitecture == ProcessorArchitecture.None || name.ProcessorArchitecture == assemblyName.ProcessorArchitecture) &&
				(name.GetPublicKey() == null || name.GetPublicKey().SequenceEqual(assemblyName.GetPublicKey()))
			);
		}

		private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			// CRITICAL: Prevent re-entrancy to avoid stack overflow
			// Check if we're already inside this handler for this thread
			if (isResolving)
				return null;
			
			try
			{
				isResolving = true;
				
				// Parse the assembly name
				AssemblyName name = new AssemblyName(args.Name);
				
				// Don't try to resolve system/framework assemblies or resource assemblies
				if (name.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
					name.Name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
					name.Name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
					name.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) || // CRITICAL: Exclude resource assemblies
					name.GetPublicKeyToken()?.SequenceEqual(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }) == true ||
					name.GetPublicKeyToken()?.SequenceEqual(new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 }) == true)
				{
					return null;
				}
				
				// Cache the directory files
				string[] pluginFiles;
				try
				{
					pluginFiles = Directory.GetFiles(PluginsFolder, "*.dll", SearchOption.TopDirectoryOnly);
				}
				catch
				{
					return null;
				}
				
				// Check the plugins folder
				foreach (string fileName in pluginFiles)
				{
					FileInfo file = new FileInfo(fileName);
					try
					{
						string expectedFileName = name.Name + ".dll";
						if (!file.Name.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
							continue;
						
						AssemblyName discoveredName = AssemblyName.GetAssemblyName(file.FullName);
						if (AssemblyName.ReferenceMatchesDefinition(name, discoveredName))
						{
							return Assembly.LoadFrom(file.FullName);
						}
					}
					catch (BadImageFormatException)
					{
						// NO Logger.Log here - causes re-entrancy!
					}
					catch (FileLoadException)
					{
						// NO Logger.Log here - causes re-entrancy!
					}
					catch (Exception)
					{
						// NO Logger.Log here - causes re-entrancy!
					}
				}
				
				return null;
			}
			finally
			{
				isResolving = false;
			}
		}

		// Add this field to the DefaultHost class
		[ThreadStatic]
		private static bool isResolving;

		public override IList<PluginInfo> Plugins
		{
			get { return plugins.AsReadOnly(); }
		}

		/// <summary>
		/// Records a failed plugin load attempt for audit purposes.
		/// </summary>
		/// <param name="pluginPath">Path to the plugin that failed to load.</param>
		/// <param name="failureType">Type of failure.</param>
		/// <param name="exception">The exception that occurred.</param>
		private static void RecordFailedPluginLoad(string pluginPath, string failureType, Exception exception)
		{
			try
			{
				string auditMessage = $"PLUGIN_LOAD_FAILED: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | " +
					$"Path: {pluginPath} | " +
					$"FailureType: {failureType} | " +
					$"Error: {exception.Message}";

				Logger.Log(auditMessage, LogLevel.Warning);
			}
			catch (Exception ex)
			{
				// Never let audit recording prevent normal operation
				Logger.Log($"Failed to record plugin load failure: {ex.Message}", LogLevel.Error);
			}
		}

		/// <summary>
		/// Stores the list of plugins found within the Plugins folder.
		/// </summary>
		private List<PluginInfo> plugins = new List<PluginInfo>();

		/// <summary>
		/// The path to the folder containing the plugins.
		/// </summary>
		public readonly string PluginsFolder = Path.Combine(
			Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), //Assembly location
			"Plugins" //Plugins folder
		);

		/// <summary>
		/// The list of plugins which are core. This list contains the names of every
		/// assembly which are expected to be core plugins.
		/// </summary>
		private readonly string[] CorePlugins = new string[] {
			"Eraser.DefaultPlugins"
		};
	}
}
