/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	partial class CustomMethodEditorForm : Form
	{
		public CustomMethodEditorForm()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
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
			if (pass.Function == PassBasedErasureMethod.WriteRandom)
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
				pass.Function = PassBasedErasureMethod.WriteRandom;
				pass.OpaqueValue = null;
				item.SubItems[1].Text = S._("Random Data");
			}
			else
			{
				pass.Function = PassBasedErasureMethod.WriteConstant;
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
			passEditor.PassType = pass.Function == PassBasedErasureMethod.WriteRandom ?
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
			passesRemoveBtn.Enabled = passesDuplicateBtn.Enabled = passesLv.SelectedItems.Count >= 1;
			passGrp.Enabled = passEditor.Enabled = passesLv.SelectedItems.Count == 1;
		}

		private void passesAddBtn_Click(object sender, EventArgs e)
		{
			//Save the current pass being edited
			if (currentPass != null)
				SavePass(currentPass);

			//Then create a new, random pass, adding it to the list
			ErasureMethodPass pass = new ErasureMethodPass(PassBasedErasureMethod.WriteRandom, null);
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

		private void passesLv_ItemDrag(object sender, ItemDragEventArgs e)
		{
			//Save the currently edited pass before allowing the drag & drop operation.
			SavePass(currentPass);

			//Then initiate the drag & drop.
			passesLv.DoDragDrop(passesLv.SelectedItems, DragDropEffects.All);
		}

		private void passesLv_DragEnter(object sender, DragEventArgs e)
		{
			ListView.SelectedListViewItemCollection items =
				e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)) as
					ListView.SelectedListViewItemCollection;
			if (items == null)
				return;

			e.Effect = DragDropEffects.Move;
		}

		ListViewItem lastInsertionPoint;
		private void passesLv_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
			e.UseDefaultCursors = true;
			Point mousePoint = passesLv.PointToClient(Cursor.Position);
			ListViewItem insertionPoint = GetInsertionPoint(mousePoint);

			if (insertionPoint != lastInsertionPoint)
			{
				passesLv.Invalidate();
				passesLv.Update();

				using (Graphics g = passesLv.CreateGraphics())
				{
					//Only handle the exception: when insertionPoint is null, the item should
					//be appended to the back of the listview.
					if (insertionPoint == null)
					{
						if (passesLv.Items.Count > 0)
						{
							ListViewItem lastItem = passesLv.Items[passesLv.Items.Count - 1];
							g.FillRectangle(new SolidBrush(Color.Black),
								lastItem.Bounds.Left, lastItem.Bounds.Bottom - 1, passesLv.Width, 2);
						}
						else
						{
							g.FillRectangle(new SolidBrush(Color.Black),
								   0, 0, passesLv.Width, 2);
						}
					}
					else
					{
						g.FillRectangle(new SolidBrush(Color.Black),
							insertionPoint.Bounds.Left, insertionPoint.Bounds.Top - 1, passesLv.Width, 2);
					}
				}

				lastInsertionPoint = insertionPoint;
			}
		}

		private void passesLv_DragDrop(object sender, DragEventArgs e)
		{
			//Remove the insertion mark
			lastInsertionPoint = null;

			//Get the item we dragged and the item we dropped over.
			ListView.SelectedListViewItemCollection draggedItems =
				e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)) as
					ListView.SelectedListViewItemCollection;
			List<ListViewItem> items = new List<ListViewItem>(draggedItems.Count);
			foreach (ListViewItem item in draggedItems)
				items.Add(item);
			Point mousePoint = passesLv.PointToClient(Cursor.Position);
			ListViewItem dropItem = GetInsertionPoint(mousePoint);

			//If we do not have an item, it is not a valid drag & drop operation.
			if (items == null || items.Count == 0)
				return;

			//Ignore the operation if the drag source and the destination items match.
			if (items.IndexOf(dropItem) != -1)
				return;

			//Prevent the listview from refreshing to speed things up.
			passesLv.BeginUpdate();
			passesLv.Invalidate();

			//Remove the item we dragged
			foreach (ListViewItem item in items)
				item.Remove();

			//If we don't have an item we dropped over, it's the last item in the list.
			if (dropItem == null)
			{
				foreach (ListViewItem item in items)
					passesLv.Items.Add(item);
			}
			else
			{
				foreach (ListViewItem item in items)
					passesLv.Items.Insert(dropItem.Index, item);
			}

			//Renumber the passes for congruency
			RenumberPasses();
			EnableButtons();
			passesLv.EndUpdate();
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

		/// <summary>
		/// Calculates the item to insert the new dragged item before, based on
		/// mouse coordinates.
		/// </summary>
		/// <param name="point">The current location of the cursor.</param>
		/// <returns>The item to insert the new item before, or null if the item
		/// should be appended to the list.</returns>
		private ListViewItem GetInsertionPoint(Point point)
		{
			ListViewItem item = passesLv.GetItemAt(0, point.Y);
			if (item == null)
				return null;

			bool beforeItem = point.Y < item.Bounds.Height / 2 + item.Bounds.Y;
			if (beforeItem)
			{
				return item;
			}
			else if (item.Index == passesLv.Items.Count - 1)
			{
				return null;
			}
			else
			{
				return passesLv.Items[item.Index + 1];
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
