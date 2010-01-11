/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	public partial class SettingsForm : Form
	{
		public SettingsForm()
		{
			InitializeComponent();
			UXThemeApi.UpdateControlTheme(this);

			//Populate the list of erasure passes, except the FL16KB.
			foreach (ErasureMethod method in ErasureMethodManager.Items.Values)
				if (method.Guid != new Guid("{0C2E07BF-0207-49a3-ADE8-46F9E1499C01}"))
					fl16MethodCmb.Items.Add(method);

			//Load the settings.
			DefaultPluginSettings settings = DefaultPlugin.Settings;
			if (settings.FL16Method != Guid.Empty)
				foreach (object item in fl16MethodCmb.Items)
					if (((ErasureMethod)item).Guid == settings.FL16Method)
					{
						fl16MethodCmb.SelectedItem = item;
						break;
					}

			if (fl16MethodCmb.SelectedIndex == -1)
			{
				Guid methodGuid =
					ManagerLibrary.Settings.DefaultFileErasureMethod;
				if (methodGuid == new FirstLast16KB().Guid)
					methodGuid = new Gutmann().Guid;
				
				foreach (object item in fl16MethodCmb.Items)
					if (((ErasureMethod)item).Guid == methodGuid)
					{
						fl16MethodCmb.SelectedItem = item;
						break;
					} 
			}

			if (DefaultPlugin.Settings.EraseCustom != null)
			{
				customMethods = new Dictionary<Guid,CustomErasureMethod>(
					DefaultPlugin.Settings.EraseCustom);

				//Display the whole set on the list.
				foreach (Guid guid in customMethods.Keys)
					AddMethod(customMethods[guid]);
			}
			else
				customMethods = new Dictionary<Guid, CustomErasureMethod>();
		}

		private void customMethod_ItemActivate(object sender, EventArgs e)
		{
			//Create the dialog
			CustomMethodEditorForm editorForm = new CustomMethodEditorForm();
			ListViewItem item = customMethod.SelectedItems[0];
			editorForm.Method = (CustomErasureMethod)item.Tag;

			if (editorForm.ShowDialog() == DialogResult.OK)
			{
				//Remove the old definition of the erasure method
				CustomErasureMethod method = editorForm.Method;
				if (customMethods.ContainsKey(method.Guid) &&
					removeCustomMethods.IndexOf(method.Guid) == -1)
				{
					removeCustomMethods.Add(method.Guid);
				}

				//Add the new definition
				foreach (CustomErasureMethod addMethod in addCustomMethods)
				{
					if (addMethod.Guid == method.Guid)
					{
						addCustomMethods.Remove(addMethod);
						break;
					}
				}

				addCustomMethods.Add(method);
				item.Tag = method;
				UpdateMethod(item);
			}
		}

		private void customMethodAdd_Click(object sender, EventArgs e)
		{
			CustomMethodEditorForm form = new CustomMethodEditorForm();
			if (form.ShowDialog() == DialogResult.OK)
			{
				CustomErasureMethod method = form.Method;
				addCustomMethods.Add(method);
				AddMethod(method);
			}
		}

		private void customMethodContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			e.Cancel = customMethod.SelectedIndices.Count == 0;
		}

		private void deleteMethodToolStripMenuItem_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in customMethod.SelectedItems)
			{
				CustomErasureMethod method = (CustomErasureMethod)item.Tag;
				if (addCustomMethods.IndexOf(method) >= 0)
					addCustomMethods.Remove(method);
				else
					removeCustomMethods.Add(method.Guid);
				customMethod.Items.Remove(item);
			}
		}

		private void okBtn_Click(object sender, EventArgs e)
		{
			//Save the settings to the settings dictionary
			if (fl16MethodCmb.SelectedIndex == -1)
			{
				errorProvider.SetError(fl16MethodCmb, S._("An invalid erasure method was selected."));
				return;
			}

			DefaultPlugin.Settings.FL16Method = ((ErasureMethod)fl16MethodCmb.SelectedItem).Guid;

			//Remove the old methods.
			foreach (Guid guid in removeCustomMethods)
			{
				customMethods.Remove(guid);
				ErasureMethodManager.Unregister(guid);
			}

			//Update the Erasure method manager on the methods
			foreach (CustomErasureMethod method in addCustomMethods)
			{
				customMethods.Add(method.Guid, method);
				ErasureMethodManager.Register(new EraseCustom(method), new object[] { method });
			}

			//Save the list of custom erasure methods
			DefaultPlugin.Settings.EraseCustom = customMethods;

			//Close the dialog
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// Adds the given method to the custom methods list.
		/// </summary>
		/// <param name="method">The method to add.</param>
		private void AddMethod(CustomErasureMethod method)
		{
			ListViewItem item = customMethod.Items.Add(method.Name);
			item.SubItems.Add(method.Passes.Length.ToString(CultureInfo.CurrentCulture));
			item.Tag = method;
		}

		/// <summary>
		/// Updates the UI which represents the given custom erasure method.
		/// </summary>
		/// <param name="item">The method to update.</param>
		private void UpdateMethod(ListViewItem item)
		{
			CustomErasureMethod method = (CustomErasureMethod)item.Tag;
			item.Text = method.Name;
			item.SubItems[1].Text = method.Passes.Length.ToString(CultureInfo.CurrentCulture);
		}

		private Dictionary<Guid, CustomErasureMethod> customMethods;
		private List<CustomErasureMethod> addCustomMethods = new List<CustomErasureMethod>();
		private List<Guid> removeCustomMethods = new List<Guid>();
	}
}
