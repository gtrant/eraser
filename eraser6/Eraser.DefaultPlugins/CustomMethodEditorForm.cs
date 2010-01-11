/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By:	Kasra Nassiri <cjax@users.sourceforge.net> @10-11-2008 04:18:04
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
	public partial class CustomMethodEditorForm : Form
	{
		public CustomMethodEditorForm()
		{
			InitializeComponent();
			UXThemeApi.UpdateControlTheme(this);
		}

		/// <summary>
		/// Sets or retrieves the CustomErasureMethod object with all the values
		/// in the dialog.
		/// </summary>
		public CustomErasureMethod Method
		{
			get
			{
				if (method == null)
				{
					method = new CustomErasureMethod();
					method.Guid = Guid.NewGuid();
				}

				//The method name.
				method.Name = nameTxt.Text;

				//Whether passes can be randomized when executing.
				method.RandomizePasses = randomizeChk.Checked;

				//And all the passes.
				ErasureMethodPass[] passes = new ErasureMethodPass[passesLv.Items.Count];
				for (int i = 0; i < passesLv.Items.Count; ++i)
					passes[i] = (ErasureMethodPass)passesLv.Items[i].Tag;
				method.Passes = passes;

				return method;
			}
			set
			{
				method = value;

				//Method name.
				nameTxt.Text = method.Name;

				//Randomize passes
				randomizeChk.Checked = method.RandomizePasses;

				//Every pass.
				foreach (ErasureMethodPass pass in method.Passes)
					AddPass(pass);
			}
		}

		/// <summary>
		/// Adds the given pass to the displayed list of passes.
		/// </summary>
		/// <param name="pass">The pass to add.</param>
		/// <returns>The item added to the list view.</returns>
		private ListViewItem AddPass(ErasureMethodPass pass)
		{
			ListViewItem item = new ListViewItem((passesLv.Items.Count + 1).ToString(
				CultureInfo.CurrentCulture));
			item.Tag = pass;
			if (pass.Function == ErasureMethod.WriteRandom)
				item.SubItems.Add(S._("Random Data"));
			else
				item.SubItems.Add(S._("Constant ({0} bytes)", ((byte[])pass.OpaqueValue).Length));

			passesLv.Items.Add(item);
			return item;
		}

		/// <summary>
		/// Saves the currently edited pass details to memory.
		/// </summary>
		private void SavePass(ListViewItem item)
		{
			ErasureMethodPass pass = (ErasureMethodPass)item.Tag;
			if (passEditor.PassType == CustomMethodPassEditorPassType.Random)
			{
				pass.Function = ErasureMethod.WriteRandom;
				pass.OpaqueValue = null;
				item.SubItems[1].Text = S._("Random Data");
			}
			else
			{
				pass.Function = ErasureMethod.WriteConstant;
				pass.OpaqueValue = passEditor.PassData;
				item.SubItems[1].Text = S._("Constant ({0} bytes)", passEditor.PassData.Length);
			}
		}

		/// <summary>
		/// Displays the pass associated with <paramref name="item"/> in the editing controls.
		/// </summary>
		/// <param name="item">The <see cref="ListViewItem"/> containing the pass to edit.</param>
		private void DisplayPass(ListViewItem item)
		{
			currentPass = item;
			ErasureMethodPass pass = (ErasureMethodPass)item.Tag;
			passEditor.PassData = (byte[])pass.OpaqueValue;
			passEditor.PassType = pass.Function == ErasureMethod.WriteRandom ?
				CustomMethodPassEditorPassType.Random :
				CustomMethodPassEditorPassType.Text;
		}

		/// <summary>
		/// Renumbers all pass entries' pass number to be in sync with its position.
		/// </summary>
		private void RenumberPasses()
		{
			foreach (ListViewItem item in passesLv.Items)
				item.Text = (item.Index + 1).ToString(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Enables buttons relevant to the currently selected items.
		/// </summary>
		private void EnableButtons()
		{
			passesRemoveBtn.Enabled = passesDuplicateBtn.Enabled = passesMoveUpBtn.Enabled =
				passesMoveDownBtn.Enabled = passesLv.SelectedItems.Count >= 1;
			passGrp.Enabled = passEditor.Enabled = passesLv.SelectedItems.Count == 1;

			ListView.SelectedListViewItemCollection items = passesLv.SelectedItems;
			if (items.Count > 0)
			{
				foreach (ListViewItem item in items)
				{
					int index = item.Index;
					passesMoveUpBtn.Enabled = passesMoveUpBtn.Enabled && index > 0;
					passesMoveDownBtn.Enabled = passesMoveDownBtn.Enabled && index < passesLv.Items.Count - 1;
				}
			}
		}

		private void passesAddBtn_Click(object sender, EventArgs e)
		{
			//Save the current pass being edited
			if (currentPass != null)
				SavePass(currentPass);

			//Then create a new, random pass, adding it to the list
			ErasureMethodPass pass = new ErasureMethodPass(ErasureMethod.WriteRandom, null);
			ListViewItem item = AddPass(pass);

			//If a pass is currently selected, insert the pass after the currently selected one.
			if (passesLv.SelectedIndices.Count > 0)
			{
				item.Remove();
				passesLv.Items.Insert(passesLv.SelectedIndices[passesLv.SelectedIndices.Count - 1] + 1,
					item);
				RenumberPasses();
			}
		}

		private void passesRemoveBtn_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem item in passesLv.SelectedItems)
				passesLv.Items.Remove(item);

			RenumberPasses();
		}

		private void passesDuplicateBtn_Click(object sender, EventArgs e)
		{
			//Save the current pass to prevent data loss
			SavePass(currentPass);

			foreach (ListViewItem item in passesLv.SelectedItems)
			{
				ErasureMethodPass oldPass = (ErasureMethodPass)item.Tag;
				ErasureMethodPass pass = new ErasureMethodPass(
					oldPass.Function, oldPass.OpaqueValue);
				AddPass(pass);
			}
		}

		private void passesMoveUpBtn_Click(object sender, EventArgs e)
		{
			//Save the current pass to prevent data loss
			SavePass(currentPass);

			foreach (ListViewItem item in passesLv.SelectedItems)
			{
				//Insert the current item into the index before, only if the item has got
				//space to move up!
				int index = item.Index;
				if (index >= 1)
				{
					passesLv.Items.RemoveAt(index);
					passesLv.Items.Insert(index - 1, item);
				}
			}

			RenumberPasses();
			EnableButtons();
		}

		private void passesMoveDownBtn_Click(object sender, EventArgs e)
		{
			//Save the current pass to prevent data loss
			SavePass(currentPass);

			ListView.SelectedListViewItemCollection items = passesLv.SelectedItems;
			for (int i = items.Count; i-- != 0; )
			{
				//Insert the current item into the index after, only if the item has got
				//space to move down.
				ListViewItem item = items[i];
				int index = item.Index;
				if (index < passesLv.Items.Count - 1)
				{
					passesLv.Items.RemoveAt(index);
					passesLv.Items.Insert(index + 1, item);
				}
			}

			RenumberPasses();
			EnableButtons();
		}

		private void passesLv_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			EnableButtons();

			//Determine if we should load or save the pass information
			if (!e.Item.Selected)
			{
				if (e.Item == currentPass)
				{
					SavePass(e.Item);
					currentPass = null;
				}
			}
			else if (passesLv.SelectedIndices.Count == 1)
			{
				DisplayPass(passesLv.SelectedItems[0]);
			}
		}
		
		private void okBtn_Click(object sender, EventArgs e)
		{
			//Clear the errorProvider status
			errorProvider.Clear();
			bool hasError = false;

			//Save the currently edited pass.
			if (passesLv.SelectedItems.Count == 1)
				SavePass(passesLv.SelectedItems[0]);

			//Validate the information
			if (nameTxt.Text.Length == 0)
			{
				errorProvider.SetError(nameTxt, S._("The name of the custom method cannot be empty."));
				errorProvider.SetIconPadding(nameTxt, -16);
				hasError = true;
			}

			//Validate all passes
			if (passesLv.Items.Count == 0)
			{
				errorProvider.SetError(passesLv, S._("The method needs to have at least one pass " +
					"defined."));
				errorProvider.SetIconPadding(passesLv, -16);
				hasError = true;
			}

			//If there are errors, don't close the dialog.
			if (!hasError)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		/// <summary>
		/// Holds the CustomErasureMethod object which client code may set to allow
		/// method editing.
		/// </summary>
		private CustomErasureMethod method;

		/// <summary>
		/// Holds the current Erasure pass that is being edited.
		/// </summary>
		private ListViewItem currentPass;
	}
}
