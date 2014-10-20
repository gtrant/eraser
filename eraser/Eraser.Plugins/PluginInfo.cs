/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser.Plugins
{
	/// <summary>
	/// Structure holding the instance values of the plugin like handle and path.
	/// </summary>
	public class PluginInfo
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">The assembly representing this plugin.</param>
		/// <param name="path">The path to the ass</param>
		/// <param name="plugin"></param>
		internal PluginInfo(Assembly assembly, IPlugin plugin)
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

			//Get the persistent store for this assembly
			if (AssemblyInfo.Guid != Guid.Empty)
				PersistentStore = Host.Instance.PersistentStore.GetSubsection("Plugins").
					GetSubsection(AssemblyInfo.Guid.ToString());
		}

		/// <summary>
		/// Executes the plugin's initialisation routine.
		/// </summary>
		internal void Load(Host host)
		{
			if (Assembly.ReflectionOnly)
				Assembly = Assembly.Load(Assembly.GetName());

			try
			{
				//Iterate over every exported type, checking for the IPlugin implementation
				Type typePlugin = Assembly.GetExportedTypes().First(
					type => type.GetInterface("Eraser.Plugins.IPlugin", true) != null);

				//Initialize the plugin
				Plugin = (IPlugin)Activator.CreateInstance(typePlugin);
				Plugin.Initialize(this);
			}
			catch (InvalidOperationException e)
			{
				throw new FileLoadException(S._("Could not load the plugin."),
					Assembly.Location, e);
			}
			catch (System.Security.SecurityException e)
			{
				throw new FileLoadException(S._("Could not load the plugin."),
					Assembly.Location, e);
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
			private set
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
					else if (attr.Constructor.DeclaringType.GUID == typeof(PluginLoadingPolicyAttribute).GUID)
						LoadingPolicy = (PluginLoadingPolicy)attr.ConstructorArguments[0].Value;

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
		public PluginLoadingPolicy LoadingPolicy { get; internal set; }

		/// <summary>
		/// Gets the IPlugin interface which the plugin exposed. This may be null
		/// if the plugin was not loaded.
		/// </summary>
		public IPlugin Plugin { get; private set; }

		/// <summary>
		/// Gets the Persistent Data Store for this plugin.
		/// </summary>
		public PersistentStore PersistentStore { get; private set; }

		/// <summary>
		/// Gets whether this particular plugin is currently loaded in memory.
		/// </summary>
		public bool Loaded
		{
			get { return Plugin != null; }
		}

		private Assembly assembly;
	}
}
