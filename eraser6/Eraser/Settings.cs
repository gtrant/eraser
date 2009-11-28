/* 
 * $Id$
 * Copyright 2009 The Eraser Project
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

using System.IO;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;

using Eraser.Util;

namespace Eraser
{
	internal class Settings : Manager.SettingsManager
	{
		/// <summary>
		/// Registry-based storage backing for the Settings class.
		/// </summary>
		private class RegistrySettings : Manager.Settings, IDisposable
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="pluginId">The GUID of the plugin for which settings are stored.</param>
			/// <param name="key">The registry key to look for the settings in.</param>
			public RegistrySettings(Guid pluginId, RegistryKey key)
			{
				this.pluginID = pluginId;
				this.key = key;
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
				if (disposing)
					key.Close();
			}

			#endregion

			public override object this[string setting]
			{
				get
				{
					//Get the raw registry value
					object rawResult = key.GetValue(setting, null);

					//Check if it is a serialised object
					byte[] resultArray = rawResult as byte[];
					if (resultArray != null)
					{
						using (MemoryStream stream = new MemoryStream(resultArray))
							try
							{
								return new BinaryFormatter().Deserialize(stream);
							}
							catch (SerializationException)
							{
								key.DeleteValue(setting);
								MessageBox.Show(S._("Could not load the setting {0} for plugin {1}. " +
									"The setting has been lost.", key, pluginID.ToString()),
									S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Error,
									MessageBoxDefaultButton.Button1,
									S.IsRightToLeft(null) ? MessageBoxOptions.RtlReading : 0);
							}
					}
					else
					{
						return rawResult;
					}

					return null;
				}
				set
				{
					if (value == null)
					{
						key.DeleteValue(setting);
					}
					else
					{
						if (value is bool)
							key.SetValue(setting, value, RegistryValueKind.DWord);
						else if ((value is int) || (value is uint))
							key.SetValue(setting, value, RegistryValueKind.DWord);
						else if ((value is long) || (value is ulong))
							key.SetValue(setting, value, RegistryValueKind.QWord);
						else if (value is string)
							key.SetValue(setting, value, RegistryValueKind.String);
						else
							using (MemoryStream stream = new MemoryStream())
							{
								new BinaryFormatter().Serialize(stream, value);
								key.SetValue(setting, stream.ToArray(), RegistryValueKind.Binary);
							}
					}
				}
			}

			/// <summary>
			/// The GUID of the plugin whose settings this object is storing.
			/// </summary>
			private Guid pluginID;

			/// <summary>
			/// The registry key where the data is stored.
			/// </summary>
			private RegistryKey key;
		}

		public override void Save()
		{
		}

		protected override Manager.Settings GetSettings(Guid guid)
		{
			RegistryKey eraserKey = null;

			try
			{
				//Open the registry key containing the settings
				eraserKey = Registry.CurrentUser.OpenSubKey(Program.SettingsPath, true);
				if (eraserKey == null)
					eraserKey = Registry.CurrentUser.CreateSubKey(Program.SettingsPath);

				RegistryKey pluginsKey = eraserKey.OpenSubKey(guid.ToString(), true);
				if (pluginsKey == null)
					pluginsKey = eraserKey.CreateSubKey(guid.ToString());

				//Return the Settings object.
				return new RegistrySettings(guid, pluginsKey);
			}
			finally
			{
				if (eraserKey != null)
					eraserKey.Close();
			}
		}
	}

	internal class EraserSettings
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		private EraserSettings()
		{
			settings = Manager.ManagerLibrary.Instance.SettingsManager.ModuleSettings;
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
				return settings["Language"] == null ?
					GetCurrentCulture().Name :
					(string)settings["Language"];
			}
			set
			{
				settings["Language"] = value;
			}
		}

		/// <summary>
		/// Gets or sets whether the Shell Extension should be loaded into Explorer.
		/// </summary>
		public bool IntegrateWithShell
		{
			get
			{
				return settings["IntegrateWithShell"] == null ?
					true : Convert.ToBoolean(settings["IntegrateWithShell"],
						CultureInfo.InvariantCulture);
			}
			set
			{
				settings["IntegrateWithShell"] = value;
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
				return settings["HideWhenMinimised"] == null ?
					true : Convert.ToBoolean(settings["HideWhenMinimised"],
						CultureInfo.InvariantCulture);
			}
			set
			{
				settings["HideWhenMinimised"] = value;
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
				return settings["ClearCompletedTasks"] == null ?
					true : Convert.ToBoolean(settings["ClearCompletedTasks"],
						CultureInfo.InvariantCulture);
			}
			set
			{
				settings["ClearCompletedTasks"] = value;
			}
		}

		/// <summary>
		/// Gets the current UI culture, correct to the top-level culture (i.e., English
		/// instead of English (United Kingdom))
		/// </summary>
		/// <returns>The CultureInfo of the current UI culture, correct to the top level.</returns>
		private static CultureInfo GetCurrentCulture()
		{
			CultureInfo culture = CultureInfo.CurrentUICulture;
			while (culture.Parent != CultureInfo.InvariantCulture)
				culture = culture.Parent;

			return culture;
		}

		/// <summary>
		/// The data store behind the object.
		/// </summary>
		private Manager.Settings settings;

		/// <summary>
		/// The global instance of the settings class.
		/// </summary>
		private static EraserSettings instance;
	}
}
