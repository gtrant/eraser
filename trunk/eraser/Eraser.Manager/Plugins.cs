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
using System.Linq;

using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Path = System.IO.Path;

namespace Eraser.Manager.Plugin
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
		/// Getter that retrieves the global plugin host instance.
		/// </summary>
		public static Host Instance
		{
			get { return ManagerLibrary.Instance.Host; }
		}

		/// <summary>
		/// Retrieves the list of currently loaded plugins.
		/// </summary>
		/// <remarks>The returned list is read-only</remarks>
		public abstract IList<PluginInstance> Plugins
		{
			get;
		}

		/// <summary>
		/// Loads all plugins into memory.
		/// </summary>
		public abstract void Load();

		/// <summary>
		/// The plugin loaded event.
		/// </summary>
		public EventHandler<PluginLoadedEventArgs> PluginLoaded { get; set; }

		/// <summary>
		/// Event callback executor for the OnPluginLoad Event
		/// </summary>
		internal void OnPluginLoaded(object sender, PluginLoadedEventArgs e)
		{
			if (PluginLoaded != null)
				PluginLoaded(sender, e);
		}

		/// <summary>
		/// Loads a plugin.
		/// </summary>
		/// <param name="filePath">The absolute or relative file path to the
		/// DLL.</param>
		/// <returns>True if the plugin is loaded, false otherwise.</returns>
		/// <remarks>If a plugin is loaded twice, this function should do nothing
		/// and return True.</remarks>
		public abstract bool LoadPlugin(string filePath);
	}

	/// <summary>
	/// Event argument for the plugin loaded event.
	/// </summary>
	public class PluginLoadedEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance">The plugin instance of the recently loaded plugin.</param>
		public PluginLoadedEventArgs(PluginInstance instance)
		{
			Instance = instance;
		}

		/// <summary>
		/// The <see cref="PluginInstance"/> object representing the newly loaded plugin.
		/// </summary>
		public PluginInstance Instance { get; private set; }
	}

	/// <summary>
	/// The default plugins host implementation.
	/// </summary>
	internal class DefaultHost : Host
	{
		/// <summary>
		/// Constructor. Loads all plugins in the Plugins folder.
		/// </summary>
		public DefaultHost()
		{
		}

		public override void Load()
		{
			//Specify additional places to load assemblies from
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveReflectionDependency;

			//Load all core plugins first
			foreach (KeyValuePair<string, string> plugin in CorePlugins)
			{
				LoadCorePlugin(Path.Combine(PluginsFolder, plugin.Key), plugin.Value);
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
					catch (BadImageFormatException)
					{
					}
					catch (FileLoadException)
					{
					}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (plugins == null)
				return;

			if (disposing)
			{
				//Unload all the plugins. This will cause all the plugins to execute
				//the cleanup code.
				foreach (PluginInstance plugin in plugins)
					if (plugin.Plugin != null)
						plugin.Plugin.Dispose();
			}

			plugins = null;
		}

		/// <summary>
		/// The path to the folder containing the plugins.
		/// </summary>
		public readonly string PluginsFolder = Path.Combine(
			Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), //Assembly location
			"Plugins" //Plugins folder
		);

		/// <summary>
		/// The list of plugins which are core, the key is the file name, the value
		/// is the assembly name.
		/// </summary>
		private readonly KeyValuePair<string, string>[] CorePlugins =
			new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>(
					"Eraser.DefaultPlugins.dll",
					"Eraser.DefaultPlugins"
				)
			};

		public override IList<PluginInstance> Plugins
		{
			get { return plugins.AsReadOnly(); }
		}

		/// <summary>
		/// Verifies whether the provided assembly is a plugin.
		/// </summary>
		/// <param name="assembly">The assembly to verify.</param>
		/// <returns>True if the assembly provided is a plugin, false otherwise.</returns>
		private bool IsPlugin(Assembly assembly)
		{
			//Iterate over every exported type, checking if it implements IPlugin
			Type typePlugin = assembly.GetExportedTypes().FirstOrDefault(
					type => type.GetInterface("Eraser.Manager.Plugin.IPlugin", true) != null);

			//If the typePlugin type is empty the assembly doesn't implement IPlugin it's not
			//a plugin.
			return typePlugin != null;
		}

		/// <summary>
		/// Loads the assembly at the specified path, and verifying its assembly name,
		/// ensuring that the assembly contains a core plugin.
		/// </summary>
		/// <param name="filePath">The path to the assembly.</param>
		/// <param name="assemblyName">The name of the assembly.</param>
		private void LoadCorePlugin(string filePath, string assemblyName)
		{
			Assembly assembly = Assembly.ReflectionOnlyLoadFrom(filePath);
			if (assembly.GetName().FullName.Substring(0, assemblyName.Length + 1) !=
				assemblyName + ",")
			{
				throw new FileLoadException(S._("The Core plugin assembly is not one which" +
					"Eraser expects.\n\nCheck that the Eraser installation is not corrupt, or " +
					"reinstall the program."));
			}

			//Create the PluginInstance structure
			PluginInstance instance = new PluginInstance(assembly, null);

			//Ignore non-plugins
			if (!IsPlugin(instance.Assembly))
				throw new FileLoadException(S._("The provided Core plugin assembly is not a " +
					"plugin.\n\nCheck that the Eraser installation is not corrupt, or reinstall " +
					"the program."));

			//OK this assembly is a plugin
			lock (plugins)
				plugins.Add(instance);

			//Check for the presence of a valid signature: Core plugins must have the same
			//public key as the current assembly
			if (!assembly.GetName().GetPublicKey().SequenceEqual(
					Assembly.GetExecutingAssembly().GetName().GetPublicKey()))
			{
				throw new FileLoadException(S._("The provided Core plugin does not have an " +
					"identical public key as the Eraser assembly.\n\nCheck that the Eraser " +
					"installation is not corrupt, or reinstall the program."));
			}

			//Okay, everything's fine, initialise the plugin
			instance.Assembly = Assembly.Load(instance.Assembly.GetName());
			instance.LoadingPolicy = LoadingPolicy.Core;
			InitialisePlugin(instance);
		}

		public override bool LoadPlugin(string filePath)
		{
			//Create the PluginInstance structure
			Assembly reflectAssembly = Assembly.ReflectionOnlyLoadFrom(filePath);
			PluginInstance instance = new PluginInstance(reflectAssembly, null);

			//Check that the plugin hasn't yet been loaded.
			if (Plugins.Count(
					plugin => plugin.Assembly.GetName().FullName ==
					reflectAssembly.GetName().FullName) > 0)
			{
				return true;
			}

			//Ignore non-plugins
			if (!IsPlugin(instance.Assembly))
				return false;

			//OK this assembly is a plugin
			lock (plugins)
				plugins.Add(instance);

			//If the plugin does not have an approval or denial, check for the presence of
			//a valid signature.
			IDictionary<Guid, bool> approvals = ManagerLibrary.Settings.PluginApprovals;
			if (!approvals.ContainsKey(instance.AssemblyInfo.Guid) &&
				(reflectAssembly.GetName().GetPublicKey().Length == 0 ||
				!Security.VerifyStrongName(filePath) ||
				instance.AssemblyAuthenticode == null))
			{
				return false;
			}

			//Preliminary checks to verify whether the plugin can be loaded (safely) passes,
			//Load the assembly fully, and then initialise it.
			instance.Assembly = Assembly.Load(reflectAssembly.GetName());
			
			//The plugin either is explicitly allowed or disallowed to load, or
			//it has an Authenticode Signature as well as a Strong Name. Get the
			//loading policy of the plugin.
			{
				
			}

			bool initialisePlugin = false;

			//Is there an approval or denial?
			if (approvals.ContainsKey(instance.AssemblyInfo.Guid))
				initialisePlugin = approvals[instance.AssemblyInfo.Guid];

			//There's no approval or denial, what is the specified loading policy?
			else
				initialisePlugin = instance.LoadingPolicy != LoadingPolicy.DefaultOff;

			if (initialisePlugin)
			{
				InitialisePlugin(instance);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Initialises the given plugin from the plugin's description.
		/// </summary>
		/// <param name="instance">The <see cref="PluginInstance"/> structure to fill.</param>
		private void InitialisePlugin(PluginInstance instance)
		{
			try
			{
				//Iterate over every exported type, checking for the IPlugin implementation
				Type typePlugin = instance.Assembly.GetExportedTypes().First(
					type => type.GetInterface("Eraser.Manager.Plugin.IPlugin", true) != null);
				if (typePlugin == null)
					return;

				//Initialize the plugin
				instance.Plugin = (IPlugin)Activator.CreateInstance(
					instance.Assembly.GetType(typePlugin.ToString()));
				instance.Plugin.Initialize(this);

				//And broadcast the plugin load event
				OnPluginLoaded(this, new PluginLoadedEventArgs(instance));
			}
			catch (System.Security.SecurityException e)
			{
				MessageBox.Show(S._("Could not load the plugin {0}.\n\nThe error returned was: {1}",
					instance.Assembly.Location, e.Message), S._("Eraser"), MessageBoxButtons.OK,
					MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
					Localisation.IsRightToLeft(null) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}
		}

		private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			//Check the plugins folder
			foreach (string fileName in Directory.GetFiles(PluginsFolder))
			{
				FileInfo file = new FileInfo(fileName);
				if (file.Extension.Equals(".dll"))
					try
					{
						Assembly assembly = Assembly.ReflectionOnlyLoadFrom(file.FullName);
						if (assembly.GetName().FullName == args.Name)
							return Assembly.LoadFile(file.FullName);
					}
					catch (BadImageFormatException)
					{
					}
					catch (FileLoadException)
					{
					}
			}

			return null;
		}

		private Assembly ResolveReflectionDependency(object sender, ResolveEventArgs args)
		{
			return Assembly.ReflectionOnlyLoad(args.Name);
		}

		private List<PluginInstance> plugins = new List<PluginInstance>();
	}

	/// <summary>
	/// Structure holding the instance values of the plugin like handle and path.
	/// </summary>
	public class PluginInstance
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">The assembly representing this plugin.</param>
		/// <param name="path">The path to the ass</param>
		/// <param name="plugin"></param>
		internal PluginInstance(Assembly assembly, IPlugin plugin)
		{
			Assembly = assembly;
			Plugin = plugin;

			//Verify the certificate in the assembly.
			if (Security.VerifyAuthenticode(assembly.Location))
			{
				X509Certificate2 cert = new X509Certificate2(
					X509Certificate.CreateFromSignedFile(assembly.Location));
				AssemblyAuthenticode = cert;
			}
		}

		/// <summary>
		/// Gets the Assembly this plugin instance came from.
		/// </summary>
		public Assembly Assembly
		{
			get
			{
				return assembly;
			}
			internal set
			{
				assembly = value;

				AssemblyInfo info = new AssemblyInfo();
				info.Version = assembly.GetFileVersion();
				IList<CustomAttributeData> attributes = CustomAttributeData.GetCustomAttributes(assembly);
				foreach (CustomAttributeData attr in attributes)
					if (attr.Constructor.DeclaringType == typeof(GuidAttribute))
						info.Guid = new Guid((string)attr.ConstructorArguments[0].Value);
					else if (attr.Constructor.DeclaringType == typeof(AssemblyCompanyAttribute))
						info.Author = (string)attr.ConstructorArguments[0].Value;
					else if (attr.Constructor.DeclaringType == typeof(LoadingPolicyAttribute))
					{
						LoadingPolicy = (LoadingPolicy)attr.ConstructorArguments[0].Value;
						if (LoadingPolicy == LoadingPolicy.Core)
							LoadingPolicy = LoadingPolicy.None;
					}

				this.AssemblyInfo = info;
			}
		}

		/// <summary>
		/// Gets the attributes of the assembly, loading from reflection-only sources.
		/// </summary>
		public AssemblyInfo AssemblyInfo { get; private set; }

		/// <summary>
		/// The Authenticode signature used for signing the assembly.
		/// </summary>
		public X509Certificate2 AssemblyAuthenticode { get; private set; }

		/// <summary>
		/// Gets whether the plugin is required for the functioning of Eraser (and
		/// therefore cannot be disabled.)
		/// </summary>
		public LoadingPolicy LoadingPolicy { get; internal set; }

		/// <summary>
		/// Gets the IPlugin interface which the plugin exposed. This may be null
		/// if the plugin was not loaded.
		/// </summary>
		public IPlugin Plugin { get; internal set; }

		/// <summary>
		/// Gets whether this particular plugin is currently loaded in memory.
		/// </summary>
		public bool Loaded
		{
			get { return Plugin != null; }
		}

		private Assembly assembly;
	}

	/// <summary>
	/// Reflection-only information retrieved from the assembly.
	/// </summary>
	public struct AssemblyInfo
	{
		/// <summary>
		/// The GUID of the assembly.
		/// </summary>
		public Guid Guid { get; set; }

		/// <summary>
		/// The publisher of the assembly.
		/// </summary>
		public string Author { get; set; }

		/// <summary>
		/// The version of the assembly.
		/// </summary>
		public Version Version { get; set; }

		public override bool Equals(object obj)
		{
			if (!(obj is AssemblyInfo))
				return false;
			return Equals((AssemblyInfo)obj);
		}

		public bool Equals(AssemblyInfo other)
		{
			return Guid == other.Guid;
		}

		public static bool operator ==(AssemblyInfo assembly1, AssemblyInfo assembly2)
		{
			return assembly1.Equals(assembly2);
		}

		public static bool operator !=(AssemblyInfo assembly1, AssemblyInfo assembly2)
		{
			return !assembly1.Equals(assembly2);
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}
	}

	/// <summary>
	/// Basic plugin interface which allows for the main program to utilize the
	/// functions in the DLL
	/// </summary>
	public interface IPlugin : IDisposable
	{
		/// <summary>
		/// Initializer.
		/// </summary>
		/// <param name="host">The host object which can be used for two-way
		/// communication with the program.</param>
		void Initialize(Host host);

		/// <summary>
		/// The name of the plug-in, used for descriptive purposes in the UI
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// The author of the plug-in, used for display in the UI and for users
		/// to contact the author about bugs. Must be in the format:
		///		(.+) \&lt;([a-zA-Z0-9_.]+)@([a-zA-Z0-9_.]+)\.([a-zA-Z0-9]+)\&gt;
		/// </summary>
		/// <example>Joel Low <joel@joelsplace.sg></example>
		string Author
		{
			get;
		}

		/// <summary>
		/// Determines whether the plug-in is configurable.
		/// </summary>
		bool Configurable
		{
			get;
		}

		/// <summary>
		/// Fulfil a request to display the settings for this plug-in.
		/// </summary>
		/// <param name="parent">The parent control which the settings dialog should
		/// be parented with.</param>
		void DisplaySettings(Control parent);
	}

	/// <summary>
	/// Loading policies applicable for a given plugin.
	/// </summary>
	public enum LoadingPolicy
	{
		/// <summary>
		/// The host decides the best policy for loading the plugin.
		/// </summary>
		None,

		/// <summary>
		/// The host will enable the plugin by default.
		/// </summary>
		DefaultOn,

		/// <summary>
		/// The host will disable the plugin by default
		/// </summary>
		DefaultOff,

		/// <summary>
		/// The host must always load the plugin.
		/// </summary>
		/// <remarks>This policy does not have an effect when declared in the
		/// <see cref="LoadingPolicyAttribute"/> attribute and will be equivalent
		/// to <see cref="None"/>.</remarks>
		Core
	}

	/// <summary>
	/// Declares the loading policy for the assembly containing the plugin. Only
	/// plugins signed with an Authenticode signature will be trusted and have
	/// this attribute checked at initialisation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class LoadingPolicyAttribute : Attribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="policy">The policy used for loading the plugin.</param>
		public LoadingPolicyAttribute(LoadingPolicy policy)
		{
			Policy = policy;
		}

		/// <summary>
		/// The loading policy to be applied to the assembly.
		/// </summary>
		public LoadingPolicy Policy
		{
			get;
			set;
		}
	}
}
