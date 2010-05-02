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
using System.Reflection;
using System.Runtime.InteropServices;
using Eraser.Util;
using System.Globalization;

namespace Eraser.Manager
{
	public abstract class SettingsManager
	{
		/// <summary>
		/// Saves all the settings to persistent storage.
		/// </summary>
		public abstract void Save();

		/// <summary>
		/// Gets the dictionary holding settings for the calling assembly.
		/// </summary>
		public Settings ModuleSettings
		{
			get
			{
				return GetSettings(new Guid(((GuidAttribute)Assembly.GetCallingAssembly().
					GetCustomAttributes(typeof(GuidAttribute), false)[0]).Value));
			}
		}

		/// <summary>
		/// Gets the settings from the data source.
		/// </summary>
		/// <param name="value">The GUID of the calling plugin</param>
		/// <returns>The Settings object which will act as the data store.</returns>
		protected abstract Settings GetSettings(Guid value);
	}

	/// <summary>
	/// Settings class. Represents settings to a given client.
	/// </summary>
	public abstract class Settings
	{
		/// <summary>
		/// Gets the setting for the given name, coercing the object stored in the backend
		/// to the given type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the setting that is currently stored in the
		/// backend.</typeparam>
		/// <param name="name">The name of the setting that is used to uniquely refer
		/// to the value.</param>
		/// <param name="defaultValue">The default to return if the no data is assocated
		/// with the given setting.</param>
		/// <returns>The value stored in the backend, or null if none exists.</returns>
		public abstract T GetValue<T>(string name, T defaultValue);

		/// <summary>
		/// Overload for <see cref="GetValue"/> which returns a default for the given type.
		/// </summary>
		/// <typeparam name="T">The type of the setting that is currently stored in the
		/// backend.</typeparam>
		/// <param name="name">The name of the setting that is used to uniquely refer
		/// to the value.</param>
		/// <param name="defaultValue">The default to return if the no data is assocated
		/// with the given setting.</param>
		/// <returns>The value stored in the backend, or null if none exists.</returns>
		public T GetValue<T>(string name)
		{
			return GetValue<T>(name, default(T));
		}

		/// <summary>
		/// Sets the setting with the given name.
		/// </summary>
		/// <param name="name">The name of the setting.</param>
		/// <param name="value">The value to store in the backend. This may be serialised.</param>
		public abstract void SetValue(string name, object value);

		/// <summary>
		/// Gets or sets the given setting, without type hinting. This will not attempt to coerce
		/// a type from an old version of the assembly to its current type.
		/// </summary>
		/// <param name="setting">The name of the setting.</param>
		/// <returns>The object stored in the settings database, or null if undefined.</returns>
		[Obsolete("Use the GetValue<T> and SetValue functions instead")]
		public object this[string setting]
		{
			get
			{
				return GetValue<object>(setting);
			}
			set
			{
				SetValue(setting, value);
			}
		}
	}

	#region Default attributes
	/// <summary>
	/// Indicates that the marked class should be used as a default when no
	/// settings have been set by the user.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
	public abstract class DefaultAttribute : Attribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="priority">The priority of the current element in terms of
		/// it being the default.</param>
		protected DefaultAttribute(int priority)
		{
			Priority = priority;
		}

		/// <summary>
		/// The priority of the default.
		/// </summary>
		public int Priority
		{
			get
			{
				return priority;
			}
			private set
			{
				priority = value;
			}
		}

		private int priority;
	}

	/// <summary>
	/// Indicates that the marked class should be used as the default file erasure
	/// method.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	public sealed class DefaultFileErasureAttribute : DefaultAttribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="priority">The priority of the current element in terms of
		/// it being the default.</param>
		public DefaultFileErasureAttribute(int priority)
			: base(priority)
		{
		}
	}

	/// <summary>
	/// Indicates that the marked class should be used as the default unused space
	/// erasure method.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	public sealed class DefaultUnusedSpaceErasureAttribute : DefaultAttribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="priority">The priority of the current element in terms of
		/// it being the default.</param>
		public DefaultUnusedSpaceErasureAttribute(int priority)
			: base(priority)
		{
		}
	}

	/// <summary>
	/// Indicates that the marked class should be used as the default PRNG.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	public sealed class DefaultPrngAttribute : DefaultAttribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="priority">The priority of the current element in terms of
		/// it being the default.</param>
		public DefaultPrngAttribute(int priority)
			: base(priority)
		{
		}
	}
	#endregion

	/// <summary>
	/// Presents an opaque type for the management of the Manager settings.
	/// </summary>
	public class ManagerSettings
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="settings">The Settings object which is the data store for
		/// this object.</param>
		public ManagerSettings()
		{
			settings = ManagerLibrary.Instance.SettingsManager.ModuleSettings;
		}

		/// <summary>
		/// The default file erasure method. This is a GUID since methods are
		/// implemented through plugins and plugins may not be loaded and missing
		/// references may follow.
		/// </summary>
		public Guid DefaultFileErasureMethod
		{
			get
			{
				//If the user did not define anything for this field, check all plugins
				//and use the method which was declared by us to be the highest
				//priority default
				Guid result = settings.GetValue<Guid>("DefaultFileErasureMethod");
				if (result == Guid.Empty)
					result = FindHighestPriorityDefault(typeof(ErasureMethod),
						typeof(DefaultFileErasureAttribute));
				if (result == Guid.Empty)
					result = new Guid("{1407FC4E-FEFF-4375-B4FB-D7EFBB7E9922}");

				return result;
			}
			set
			{
				settings.SetValue("DefaultFileErasureMethod", value);
			}
		}

		/// <summary>
		/// The default unused space erasure method. This is a GUID since methods
		/// are implemented through plugins and plugins may not be loaded and
		/// missing references may follow.
		/// </summary>
		public Guid DefaultUnusedSpaceErasureMethod
		{
			get
			{
				Guid result = settings.GetValue<Guid>("DefaultUnusedSpaceErasureMethod");
				if (result == Guid.Empty)
					result = FindHighestPriorityDefault(typeof(UnusedSpaceErasureMethod),
						typeof(DefaultUnusedSpaceErasureAttribute));
				if (result == Guid.Empty)
					result = new Guid("{BF8BA267-231A-4085-9BF9-204DE65A6641}");
				return result;
			}
			set
			{
				settings.SetValue("DefaultUnusedSpaceErasureMethod", value);
			}
		}

		/// <summary>
		/// The PRNG used. This is a GUID since PRNGs are implemented through
		/// plugins and plugins may not be loaded and missing references may follow.
		/// </summary>
		public Guid ActivePrng
		{
			get
			{
				Guid result = settings.GetValue<Guid>("ActivePRNG");
				if (result == Guid.Empty)
					result = FindHighestPriorityDefault(typeof(Prng), typeof(DefaultPrngAttribute));
				if (result == Guid.Empty)
					result = new Guid("{6BF35B8E-F37F-476e-B6B2-9994A92C3B0C}");
				return result;
			}
			set
			{
				settings.SetValue("ActivePRNG", value);
			}
		}

		/// <summary>
		/// Whether files which are locked when being erased should be forcibly
		/// unlocked for erasure.
		/// </summary>
		public bool ForceUnlockLockedFiles
		{
			get
			{
				return settings.GetValue("ForceUnlockLockedFiles", true);
			}
			set
			{
				settings.SetValue("ForceUnlockLockedFiles", value);
			}
		}

		/// <summary>
		/// Whether missed tasks should be run when the program next starts.
		/// </summary>
		public bool ExecuteMissedTasksImmediately
		{
			get
			{
				return settings.GetValue("ExecuteMissedTasksImmediately", true);
			}
			set
			{
				settings.SetValue("ExecuteMissedTasksImmediately", value);
			}
		}

		/// <summary>
		/// Whether erasures should be run with plausible deniability. This is
		/// achieved by the executor copying files over the file to be removed
		/// before removing it.
		/// </summary>
		/// <seealso cref="PlausibleDeniabilityFiles"/>
		public bool PlausibleDeniability
		{
			get
			{
				return settings.GetValue("PlausibleDeniability", false);
			}
			set
			{
				settings.SetValue("PlausibleDeniability", value);
			}
		}

		/// <summary>
		/// The files which are overwritten with when a file has been erased.
		/// </summary>
		public IList<string> PlausibleDeniabilityFiles
		{
			get
			{
				return new SettingsList<string>(settings, "PlausibleDeniabilityFiles");
			}
		}

		#region Default Attributes retrieval
		/// <summary>
		/// Finds the type for the given attribute that is the default (i.e. the
		/// DefaultAttribute value is the highest) and that the type inherits
		/// from the given <paramref name="superClass"/>.
		/// </summary>
		/// <param name="superClass">A class that the default must inherit from.</param>
		/// <param name="attributeType">The attribute to look for.</param>
		/// <returns>The GUID of the type that is the default.</returns>
		private static Guid FindHighestPriorityDefault(Type superClass,
			Type attributeType)
		{
			//Check if we've computed the value before. If so, we return the cached
			//value.
			if (DefaultForAttributes.ContainsKey(attributeType) &&
				DefaultForAttributes[attributeType].ContainsKey(superClass))
			{
				return DefaultForAttributes[attributeType][superClass];
			}

			//We have not computed the value. Compute the default.
			Plugin.Host pluginHost = ManagerLibrary.Instance.Host;
			IList<Plugin.PluginInstance> plugins = pluginHost.Plugins;
			SortedList<int, Guid> priorities = new SortedList<int, Guid>();

			foreach (Plugin.PluginInstance plugin in plugins)
			{
				//Check whether the plugin is signed by us.
				byte[] pluginKey = plugin.Assembly.GetName().GetPublicKey();
				byte[] ourKey = Assembly.GetExecutingAssembly().GetName().GetPublicKey();

				if (pluginKey.Length != ourKey.Length ||
					!Security.VerifyStrongName(plugin.Assembly.Location))
					continue;
				bool officialPlugin = true;
				for (int i = 0, j = ourKey.Length; i != j; ++i)
					if (pluginKey[i] != ourKey[i])
						officialPlugin = false;
				if (!officialPlugin)
					continue;

				Type[] types = FindTypeAttributeInAssembly(plugin.Assembly,
					superClass, attributeType);

				//Prioritize the types.
				if (types != null)
					foreach (Type type in types)
					{
						object[] guids =
							type.GetCustomAttributes(typeof(GuidAttribute), false);
						DefaultAttribute defaultAttr = (DefaultAttribute)
							type.GetCustomAttributes(attributeType, false)[0];

						if (guids.Length == 1)
							priorities.Add(defaultAttr.Priority,
								new Guid(((GuidAttribute)guids[0]).Value));
					}
			}

			//If we actually have a result, cache it then return the result.
			if (priorities.Count > 0)
			{
				Guid result = priorities[priorities.Keys[priorities.Count - 1]];
				if (!DefaultForAttributes.ContainsKey(attributeType))
					DefaultForAttributes.Add(attributeType, new Dictionary<Type, Guid>());
				DefaultForAttributes[attributeType].Add(superClass, result);
				return result;
			}

			//If we do not have any results, don't store it.
			return Guid.Empty;
		}

		/// <summary>
		/// Finds a type with the given characteristics in the provided assembly.
		/// </summary>
		/// <param name="assembly">The assembly to look into.</param>
		/// <param name="superClass">A class the type must inherit from.</param>
		/// <param name="attributeType">The attribute the class must possess.</param>
		/// <returns>An array of types with the given characteristics.</returns>
		private static Type[] FindTypeAttributeInAssembly(Assembly assembly, Type superClass,
			Type attributeType)
		{
			//Yes, if we got here the plugin is signed by us. Find the
			//type which inherits from ErasureMethod.
			Type[] types = assembly.GetExportedTypes();
			List<Type> result = new List<Type>();
			foreach (Type type in types)
			{
				if (!type.IsPublic || type.IsAbstract)
					//Not interesting.
					continue;

				//Try to see if this class inherits from the specified super class.
				if (!type.IsSubclassOf(superClass))
					continue;

				//See if this class has the DefaultFileErasureAttribute
				object[] attributes = type.GetCustomAttributes(attributeType, false);
				if (attributes.Length > 0)
					result.Add(type);
			}

			return result.ToArray();
		}

		/// <summary>
		/// Caches the defaults as computed by FindHighestPriorityDefault. The first
		/// key is the attribute type, the second key is the superclass, the
		/// value is the Guid.
		/// </summary>
		private static Dictionary<Type, Dictionary<Type, Guid>> DefaultForAttributes =
			new Dictionary<Type, Dictionary<Type, Guid>>();
		#endregion

		/// <summary>
		/// Holds user decisions on whether the plugin will be loaded at the next
		/// start up.
		/// </summary>
		public IDictionary<Guid, bool> PluginApprovals
		{
			get
			{
				return new SettingsDictionary<Guid, bool>(settings, "ApprovedPlugins");
			}
		}

		/// <summary>
		/// The Settings object which is the data store of this object.
		/// </summary>
		private Settings settings;

		/// <summary>
		/// Encapsulates an abstract list that is used to store settings.
		/// </summary>
		/// <typeparam name="T">The type of the list element.</typeparam>
		private class SettingsList<T> : IList<T>
		{
			public SettingsList(Settings settings, string settingName)
			{
				Settings = settings;
				SettingName = settingName;
				List = new List<T>();

				T[] values = settings.GetValue<T[]>(settingName);
				if (values != null)
					List.AddRange(values);
			}

			~SettingsList()
			{
				Save();
			}

			#region IList<T> Members

			public int IndexOf(T item)
			{
				return List.IndexOf(item);
			}

			public void Insert(int index, T item)
			{
				List.Insert(index, item);
				Save();
			}

			public void RemoveAt(int index)
			{
				List.RemoveAt(index);
				Save();
			}

			public T this[int index]
			{
				get
				{
					return List[index];
				}
				set
				{
					List[index] = value;
					Save();
				}
			}

			#endregion

			#region ICollection<T> Members

			public void Add(T item)
			{
				List.Add(item);
				Save();
			}

			public void Clear()
			{
				List.Clear();
				Save();
			}

			public bool Contains(T item)
			{
				return List.Contains(item);
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				List.CopyTo(array, arrayIndex);
			}

			public int Count
			{
				get { return List.Count; }
			}

			public bool IsReadOnly
			{
				get { return false; }
			}

			public bool Remove(T item)
			{
				bool result = List.Remove(item);
				Save();
				return result;
			}

			#endregion

			#region IEnumerable<T> Members

			public IEnumerator<T> GetEnumerator()
			{
				return List.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return List.GetEnumerator();
			}

			#endregion

			/// <summary>
			/// Saves changes made to the list to the settings manager.
			/// </summary>
			private void Save()
			{
				Settings.SetValue(SettingName, List);
			}

			/// <summary>
			/// The settings object storing the settings.
			/// </summary>
			private Settings Settings;

			/// <summary>
			/// The name of the setting we are encapsulating.
			/// </summary>
			private string SettingName;

			/// <summary>
			/// The list we are using as scratch.
			/// </summary>
			private List<T> List;
		}

		/// <summary>
		/// Encapsulates an abstract dictionary that is used to store settings.
		/// </summary>
		/// <typeparam name="TKey">The key type of the dictionary.</typeparam>
		/// <typeparam name="TValue">The value type of the dictionary.</typeparam>
		private class SettingsDictionary<TKey, TValue> : IDictionary<TKey, TValue>
		{
			public SettingsDictionary(Settings settings, string settingName)
			{
				Settings = settings;
				SettingName = settingName;
				Dictionary = settings.GetValue<Dictionary<TKey, TValue>>(settingName);
				if (Dictionary == null)
					Dictionary = new Dictionary<TKey, TValue>();
			}

			~SettingsDictionary()
			{
				Save();
			}

			#region IDictionary<TKey,TValue> Members

			public void Add(TKey key, TValue value)
			{
				Dictionary.Add(key, value);
				Save();
			}

			public bool ContainsKey(TKey key)
			{
				return Dictionary.ContainsKey(key);
			}

			public ICollection<TKey> Keys
			{
				get { return Dictionary.Keys; }
			}

			public bool Remove(TKey key)
			{
				bool result = Dictionary.Remove(key);
				Save();
				return result;
			}

			public bool TryGetValue(TKey key, out TValue value)
			{
				return Dictionary.TryGetValue(key, out value);
			}

			public ICollection<TValue> Values
			{
				get { return Dictionary.Values; }
			}

			public TValue this[TKey key]
			{
				get
				{
					return Dictionary[key];
				}
				set
				{
					Dictionary[key] = value;
					Save();
				}
			}

			#endregion

			#region ICollection<KeyValuePair<TKey,TValue>> Members

			public void Add(KeyValuePair<TKey, TValue> item)
			{
				Dictionary.Add(item.Key, item.Value);
				Save();
			}

			public void Clear()
			{
				Dictionary.Clear();
				Save();
			}

			public bool Contains(KeyValuePair<TKey, TValue> item)
			{
				return Dictionary.ContainsKey(item.Key) && Dictionary[item.Key].Equals(item.Value);
			}

			public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return Dictionary.Count; }
			}

			public bool IsReadOnly
			{
				get { return false; }
			}

			public bool Remove(KeyValuePair<TKey, TValue> item)
			{
				if (Dictionary.ContainsKey(item.Key) && Dictionary[item.Key].Equals(item.Value))
				{
					bool result = Dictionary.Remove(item.Key);
					Save();
					return result;
				}

				return false;
			}

			#endregion

			#region IEnumerable<KeyValuePair<TKey,TValue>> Members

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
			{
				return Dictionary.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return Dictionary.GetEnumerator();
			}

			#endregion

			/// <summary>
			/// Saves changes made to the list to the settings manager.
			/// </summary>
			private void Save()
			{
				Settings.SetValue(SettingName, Dictionary);
			}

			/// <summary>
			/// The settings object storing the settings.
			/// </summary>
			private Settings Settings;

			/// <summary>
			/// The name of the setting we are encapsulating.
			/// </summary>
			private string SettingName;

			/// <summary>
			/// The list we are using as scratch.
			/// </summary>
			private Dictionary<TKey, TValue> Dictionary;
		}

	}
}
