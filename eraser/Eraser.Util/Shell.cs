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

using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Eraser.Util
{
	public static class Shell
	{
		/// <summary>
		/// Gets or sets whether low disk space notifications are enabled for the
		/// current user.
		/// </summary>
		public static bool LowDiskSpaceNotificationsEnabled
		{
			get
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
					"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer"))
				{
					if (key == null)
						return true;
					return !Convert.ToBoolean(key.GetValue("NoLowDiskSpaceChecks", false));
				}
			}
			set
			{
				RegistryKey key = null;
				try
				{
					key = Registry.CurrentUser.OpenSubKey(
						"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", true);
					if (key == null)
						key = Registry.CurrentUser.CreateSubKey(
							"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer");
					key.SetValue("NoLowDiskSpaceChecks", !value);
				}
				finally
				{
					if (key != null)
						key.Close();
				}
			}
		}

		/// <summary>
		/// Parses the provided command line into its constituent arguments.
		/// </summary>
		/// <param name="commandLine">The command line to parse.</param>
		/// <returns>The arguments specified in the command line</returns>
		public static string[] ParseCommandLine(string commandLine)
		{
			int argc = 0;
			IntPtr argv = NativeMethods.CommandLineToArgvW(commandLine, out argc);
			string[] result = new string[argc];

			//Get the pointers to the arguments, then read the string.
			for (int i = 0; i < argc; ++i)
				result[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size));

			//Free the memory
			NativeMethods.LocalFree(argv);

			return result;
		}
	}
}
