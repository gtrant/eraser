/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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
using System.Linq;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;

using Eraser.Manager;
using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Plugins.Registrars;

namespace Eraser
{
	public partial class TaskDataSelectionForm : Form
	{
		private class ErasureType
		{
			public ErasureType(IErasureTarget target)
			{
				Target = target;
			}

			public override string ToString()
			{
				return Target.Name;
			}

			public IErasureTarget Target;

			/// <summary>
			/// The configurer returned by the active erasure target type.
			/// </summary>
			public IErasureTargetConfigurer Configurer;
		}

		public TaskDataSelectionForm()
		{
			//Create the UI
			InitializeComponent();
			Theming.ApplyTheme(this);

			//Insert the types of erasure targets
			foreach (IErasureTarget target in Host.Instance.ErasureTargetFactories)
				typeCmb.Items.Add(new ErasureType(target));
			if (typeCmb.Items.Count != 0) 
				typeCmb.SelectedIndex = 0;

			//And the methods list
			methodCmb.Items.Add(ErasureMethodRegistrar.Default);
			foreach (IErasureMethod method in Host.Instance.ErasureMethods)
				methodCmb.Items.Add(method);
			if (methodCmb.Items.Count != 0)
				methodCmb.SelectedIndex = 0;
		}

		/// <summary>
		/// Retrieves the settings on the property page as the Eraser Manager API equivalent.
		/// </summary>
		/// <returns>An Erasure Target, containing the information to be erased.</returns>
		public IErasureTarget Target
		{
			get
			{
				ErasureType type = (ErasureType)typeCmb.SelectedItem;
				IErasureTarget result = type.Target;
				if (type.Configurer != null)
					type.Configurer.SaveTo(result);
				result.Method = (IErasureMethod)methodCmb.SelectedItem;

				return result;
			}
			set
			{
				//Set the erasure method.
				foreach (object item in methodCmb.Items)
					if (((IErasureMethod)item).Guid == value.Method.Guid)
						methodCmb.SelectedItem = item;

				//Set the active erasure type.
				foreach (ErasureType type in typeCmb.Items)
				{
					if (type.Target.GetType() == value.GetType())
					{
						type.Target = value;
						type.Configurer = value.Configurer;
						if (type.Configurer != null)
							type.Configurer.LoadFrom(value);

						typeCmb.SelectedItem = type;
						typeCmb_SelectedIndexChanged(typeCmb, EventArgs.Empty);
						break;
					}
				}
			}
		}

		private void typeCmb_SelectedIndexChanged(object sender, EventArgs e)
		{
			//Remove the old controls
			while (typeSettingsPnl.Controls.Count > 0)
				typeSettingsPnl.Controls.RemoveAt(0);

			//Then add in the new configurer
			ErasureType type = (ErasureType)typeCmb.SelectedItem;
			if (type.Configurer == null)
				type.Configurer = type.Target.Configurer;

			if (type.Configurer == null || !(type.Configurer is Control))
			{
				Label label = new Label();
				label.Text = S._("(This erasure type does not have any settings to define.)");
				label.Dock = DockStyle.Fill;
				typeSettingsPnl.Controls.Add(label);
				return;
			}

			Control control = type.Configurer as Control;
			typeSettingsPnl.Controls.Add(control);
			control.Dock = DockStyle.Fill;
		}

		private void ok_Click(object sender, EventArgs e)
		{
			ErasureType type = (ErasureType)typeCmb.SelectedItem;
			if (methodCmb.SelectedItem != ErasureMethodRegistrar.Default &&
				!type.Target.SupportsMethod((IErasureMethod)methodCmb.SelectedItem))
			{
				errorProvider.SetError(methodCmb, S._("The erasure method selected does " +
					"not support erasing the erasure target."));
			}
			else if (type.Configurer == null || type.Configurer.SaveTo(type.Target))
			{
				errorProvider.Clear();
				DialogResult = DialogResult.OK;
				Close();
			}
		}
	}
}
