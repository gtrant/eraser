/* 
 * $Id: Settings.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using System.Windows.Forms;
using System.Text;
using System.Linq;

using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;

using Eraser.Util;
using Eraser.Plugins;

namespace Eraser
{
	internal class Settings : PersistentStore
	{
		/// <summary>
		/// Registry-based storage backing for the Settings class.
		/// </summary>
		private sealed class RegistrySettings : PersistentStore, IDisposable
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="key">The registry key to look for the settings in.</param>
			public RegistrySettings(RegistryKey key)
			{
				if (key == null)
					throw new ArgumentNullException("key");

				Key = key;
			}

			#region IDisposable Members

			~RegistrySettings()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (Key == null)
					return;

				if (disposing)
					Key.Close();
				Key = null;
			}

			#endregion

			public override T GetValue<T>(string name, T defaultValue)
			{
				//Determine the type of T. If it is an IEnumerable or IDictionary, use our
				//concrete types.
				Type typeOfT = typeof(T);
				if (typeOfT.IsInterface)
				{
					//Is it a dictionary?
					if (typeOfT.Name == "IDictionary`2")
					{
						//This is a System.Collections.Generic.IDictionary
						Type[] keyValueType = typeOfT.GetGenericArguments();

						Type settingsDictionary = typeof(SettingsDictionary<,>);
						Type typeOfResult = settingsDictionary.MakeGenericType(keyValueType);

						ConstructorInfo ctor = typeOfResult.GetConstructor(new Type[] {
							typeof(PersistentStore), typeof(string) });
						return (T)ctor.Invoke(new object[] { this, name });
					}

					//Or an IEnumerable?
					else if (typeOfT.Name == "IEnumerable`1")
					{
						//This is a System.Collections.Generic.IEnumerable
						Type[] keyValueType = typeOfT.GetGenericArguments();
						return (T)GetList<T>(name, keyValueType[0]);
					}

					//Or an IList<T>, ICollection<T>
					else
					{
						foreach (Type type in typeOfT.GetInterfaces())
							if (type.IsGenericType)
							{
								if (type.GetInterfaces().Any(
										x => x == typeof(System.Collections.IEnumerable)) &&
									type.Name == "IEnumerable`1")
								{
									//This is a System.Collections.Generic.IEnumerable
									Type[] keyValueType = typeOfT.GetGenericArguments();
									return (T)GetList<T>(name, keyValueType[0]);
								}
							}
					}
				}

				return (T)GetScalar(name, defaultValue);
			}

			private object GetScalar<T>(string name, T defaultValue)
			{
				//Get the raw registry value
				object rawResult = Key.GetValue(name, null);
				if (rawResult == null)
					return defaultValue;

				//Check if it is a serialised object
				byte[] resultArray = rawResult as byte[];
				if (resultArray != null)
				{
					using (MemoryStream stream = new MemoryStream(resultArray))
						try
						{
							BinaryFormatter formatter = new BinaryFormatter();
							if (typeof(T) != typeof(object))
								formatter.Binder = new TypeNameSerializationBinder(typeof(T));
							return (T)formatter.Deserialize(stream);
						}
						catch (InvalidCastException)
						{
							Key.DeleteValue(name);
							MessageBox.Show(S._("Could not load the setting {0}\\{1}. The " +
								"setting has been lost.", Key, name),
								S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Error,
								MessageBoxDefaultButton.Button1,
								Localisation.IsRightToLeft(null) ? MessageBoxOptions.RtlReading : 0);
						}
				}
				else if (typeof(T) == typeof(Guid))
				{
					return new Guid((string)rawResult);
				}
				else if (typeof(T).GetInterfaces().Any(x => x == typeof(IConvertible)))
				{
					return Convert.ChangeType(rawResult, typeof(T));
				}
				else
				{
					return rawResult;
				}

				return defaultValue;
			}

			private object GetList<T>(string name, Type type)
			{
				//Make sure that type is either a string or the type can be converted from a
				//string (via IConvertible)
				if (type != typeof(string) && !type.GetInterfaces().Any(x => x == typeof(IConvertible)))
				{
					return GetScalar<T>(name, default(T));
				}
				
				Type settingsList = typeof(SettingsList<>);
				Type typeOfResult = settingsList.MakeGenericType(type);

				//Get the constructor.
				Type typeOfTArray = typeof(ICollection<>).MakeGenericType(type);
				ConstructorInfo ctor = typeOfResult.GetConstructor(new Type[] {
					typeof(PersistentStore), typeof(string), typeOfTArray });

				//Get the values currently in the registry
				string[] values = (string[])GetScalar<string[]>(name, null);

				//Convert the values from a string array to the type expected
				object array = null;
				if (type == typeof(string))
				{
					array = values;
				}
				else
				{
					array = typeof(List<>).MakeGenericType(type).GetConstructor(new Type[0]).
						Invoke(new object[0]);
					foreach (string item in values)
					{
						((System.Collections.IList)array).Add(Convert.ChangeType(item, type));
					}
				}
				
				return ctor.Invoke(new object[] { this, name, array });
			}

			public override void SetValue(string name, object value)
			{
				if (value == null)
				{
					Key.DeleteValue(name);
				}
				else
				{
					//Determine the type of T. If it is an IEnumerable, store it as a string array
					Type typeOfT = value.GetType();
					foreach (Type type in typeOfT.GetInterfaces())
						if (type.IsGenericType &&
							type.Name == "IEnumerable`1" &&
							type.GetInterfaces().Any(
								x => x == typeof(System.Collections.IEnumerable)))
						{
							//Check that we know how to convert the item type
							Type itemType = type.GetGenericArguments()[0];
							string[] registryValue = null;
							if (typeOfT == typeof(string[]))
							{
								registryValue = (string[])value;
							}
							else if (itemType == typeof(string))
							{
								registryValue = new List<string>((IEnumerable<string>)value).
									ToArray();
							}
							else if (itemType.GetInterfaces().Any(x => x == typeof(IConvertible)))
							{
								List<string> collection = new List<string>();
								foreach (object item in (System.Collections.IEnumerable)value)
									collection.Add((string)
										Convert.ChangeType(item, typeof(string)));

								registryValue = collection.ToArray();
							}

							if (registryValue != null)
							{
								Key.SetValue(name, registryValue, RegistryValueKind.MultiString);
								return;
							}
						}

					if (value is bool)
						Key.SetValue(name, value, RegistryValueKind.DWord);
					else if ((value is int) || (value is uint))
						Key.SetValue(name, value, RegistryValueKind.DWord);
					else if ((value is long) || (value is ulong))
						Key.SetValue(name, value, RegistryValueKind.QWord);
					else if ((value is string) || (value is Guid))
						Key.SetValue(name, value, RegistryValueKind.String);
					else
						using (MemoryStream stream = new MemoryStream())
						{
							new BinaryFormatter().Serialize(stream, value);
							Key.SetValue(name, stream.ToArray(), RegistryValueKind.Binary);
						}
				}
			}

			public override PersistentStore GetSubsection(string subsectionName)
			{
				RegistryKey subKey = null;

				try
				{
					//Open the registry key containing the settings
					subKey = Key.OpenSubKey(subsectionName, true);
					if (subKey == null)
						subKey = Key.CreateSubKey(subsectionName);

					PersistentStore result = new RegistrySettings(subKey);
					subKey = null;
					return result;
				}
				finally
				{
					if (subKey != null)
						subKey.Close();
				}
			}

			/// <summary>
			/// The registry key where the data is stored.
			/// </summary>
			private RegistryKey Key;
		}

		private Settings()
		{
			RegistryKey eraserKey = null;

			try
			{
				//Open the registry key containing the settings
				eraserKey = Registry.CurrentUser.OpenSubKey(Program.SettingsPath, true);
				if (eraserKey == null)
					eraserKey = Registry.CurrentUser.CreateSubKey(Program.SettingsPath);

				//Return the Settings object.
				registry = new RegistrySettings(eraserKey);
				eraserKey = null;
			}
			finally
			{
				if (eraserKey != null)
					eraserKey.Close();
			}
		}

		public static Settings Get()
		{
			return Instance;
		}

		public override PersistentStore GetSubsection(string subsectionName)
		{
			return registry.GetSubsection(subsectionName);
		}

		public override T GetValue<T>(string name, T defaultValue)
		{
			return registry.GetValue<T>(name, defaultValue);
		}

		public override void SetValue(string name, object value)
		{
			registry.SetValue(name, value);
		}

		private RegistrySettings registry;

		/// <summary>
		/// The global Settings instance.
		/// </summary>
		private static Settings Instance = new Settings();
	}

	/// <summary>
	/// Encapsulates an abstract list that is used to store settings.
	/// </summary>
	/// <typeparam name="T">The type of the list element.</typeparam>
	class SettingsList<T> : IList<T>
	{
		public SettingsList(PersistentStore store, string settingName)
			: this(store, settingName, null)
		{
		}

		public SettingsList(PersistentStore store, string settingName, IEnumerable<T> values)
		{
			Store = store;
			SettingName = settingName;
			List = new List<T>();

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
			Store.SetValue(SettingName, List);
		}

		/// <summary>
		/// The settings object storing the settings.
		/// </summary>
		private PersistentStore Store;

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
	class SettingsDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		public SettingsDictionary(PersistentStore store, string settingName)
		{
			Store = store;
			SettingName = settingName;
		}

		#region IDictionary<TKey,TValue> Members

		public void Add(TKey key, TValue value)
		{
			KeyStore.SetValue(key.ToString(), value);
		}

		public bool ContainsKey(TKey key)
		{
			TValue outValue;
			return TryGetValue(key, out outValue);
		}

		public ICollection<TKey> Keys
		{
			get { throw new NotSupportedException(); }
		}

		public bool Remove(TKey key)
		{
			KeyStore.SetValue(key.ToString(), null);
			return true;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			value = KeyStore.GetValue<TValue>(key.ToString());
			return !value.Equals(default(TValue));
		}

		public ICollection<TValue> Values
		{
			get { throw new NotSupportedException(); }
		}

		public TValue this[TKey key]
		{
			get
			{
				return KeyStore.GetValue<TValue>(key.ToString());
			}
			set
			{
				KeyStore.SetValue(key.ToString(), value);
			}
		}

		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			TValue outValue;
			if (TryGetValue(item.Key, out outValue) && item.Equals(outValue))
				return true;

			return false;
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		public int Count
		{
			get { throw new NotSupportedException(); }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (Contains(item))
			{
				this[item.Key] = default(TValue);
				return true;
			}

			return false;
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotSupportedException();
		}

		#endregion

		/// <summary>
		/// Gets the Persistent Store for this dictionary.
		/// </summary>
		private PersistentStore KeyStore
		{
			get
			{
				return Store.GetSubsection(SettingName);
			}
		}

		/// <summary>
		/// The settings object storing the settings.
		/// </summary>
		private PersistentStore Store;

		/// <summary>
		/// The name of the setting we are encapsulating.
		/// </summary>
		private string SettingName;
	}

	internal class EraserSettings
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		private EraserSettings()
		{
			settings = Settings.Get();
		}

		/// <summary>
		/// Gets the singleton instance of the Eraser UI Settings.
		/// </summary>
		/// <returns>The global instance of the Eraser UI settings.</returns>
		public static EraserSettings Get()
		{
			if (instance == null)
				instance = new EraserSettings();
			return instance;
		}

		/// <summary>
		/// Gets or sets the LCID of the language which the UI should be displayed in.
		/// </summary>
		public string Language
		{
			get
			{
				return settings.GetValue("Language", GetCurrentCulture().Name);
			}
			set
			{
				settings.SetValue("Language", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the Shell Extension should be loaded into Explorer.
		/// </summary>
		public bool IntegrateWithShell
		{
			get
			{
				return settings.GetValue("IntegrateWithShell", true);
			}
			set
			{
				settings.SetValue("IntegrateWithShell", value);
			}
		}

		/// <summary>
		/// Gets or sets a value on whether the main frame should be minimised to the
		/// system notification area.
		/// </summary>
		public bool HideWhenMinimised
		{
			get
			{
				return settings.GetValue("HideWhenMinimised", true);
			}
			set
			{
				settings.SetValue("HideWhenMinimised", value);
			}
		}

		/// <summary>
		/// Gets ot setts a value whether tasks which were completed successfully
		/// should be removed by the Eraser client.
		/// </summary>
		public bool ClearCompletedTasks
		{
			get
			{
				return settings.GetValue("ClearCompletedTasks", true);
			}
			set
			{
				settings.SetValue("ClearCompletedTasks", value);
			}
		}

		/// <summary>
		/// Gets the most specific UI culture with a localisation available, defaulting to English
		/// if none exist.
		/// </summary>
		/// <returns>The CultureInfo of the current UI culture, correct to the top level.</returns>
		private static CultureInfo GetCurrentCulture()
		{
			System.Reflection.Assembly entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
			CultureInfo culture = CultureInfo.CurrentUICulture;
			while (culture.Parent != CultureInfo.InvariantCulture &&
				!Localisation.LocalisationExists(culture, entryAssembly))
			{
				culture = culture.Parent;
			}

			//Default to English if any of our cultures don't exist.
			if (!Localisation.LocalisationExists(culture, entryAssembly))
				culture = new CultureInfo("en");

			return culture;
		}

		/// <summary>
		/// The data store behind the object.
		/// </summary>
		private PersistentStore settings;

		/// <summary>
		/// The global instance of the settings class.
		/// </summary>
		private static EraserSettings instance;
	}
}
