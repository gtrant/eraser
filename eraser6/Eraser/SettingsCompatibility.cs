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
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Eraser
{
	/// <summary>
	/// Upgrades all program settings to the newest format.
	/// </summary>
	public static class SettingsCompatibility
	{
		public static void Execute()
		{
			//First check for the existence of a Task list entry in the registry.
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Program.SettingsPath))
			using (RegistryKey mainProgramSettings = key.OpenSubKey(
				"3460478d-ed1b-4ecc-96c9-2ca0e8500557", true))
			{
				const string taskListValueName = @"TaskList";
				if (mainProgramSettings != null)
				{
					if (Array.Find(mainProgramSettings.GetValueNames(),
						delegate(string s) { return s == taskListValueName; }) != null)
					{
						//The TaskList value exists - copy the contents to the local application
						//data file.
						if (!Directory.Exists(Program.AppDataPath))
							Directory.CreateDirectory(Program.AppDataPath);

						if (!File.Exists(Program.TaskListPath))
						{
							byte[] data = (byte[])mainProgramSettings.GetValue(taskListValueName, null);
							using (MemoryStream memStream = new MemoryStream(data))
							using (FileStream stream = new FileStream(Program.TaskListPath,
								FileMode.CreateNew, FileAccess.Write, FileShare.None))
							{
								byte[] serializedData = (byte[])new BinaryFormatter().Deserialize(memStream);
								if (serializedData != null)
									stream.Write(serializedData, 0, serializedData.Length);
							}
						}

						//Delete the entry
						mainProgramSettings.DeleteValue("TaskList");
					}
				}
			}
		}
	}
}
