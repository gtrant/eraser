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
	}
}
