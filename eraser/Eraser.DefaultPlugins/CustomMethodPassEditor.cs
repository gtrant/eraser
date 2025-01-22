/* 
 * $Id: CustomMethodPassEditor.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	partial class CustomMethodPassEditor : UserControl
	{
		public CustomMethodPassEditor()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
		}

		/// <summary>
		/// Gets or sets the type of this pass.
		/// </summary>
		[Description("The type of pass being edited")]
		[Category("Behavior")]
		public CustomMethodPassEditorPassType PassType
		{
			get
			{
				if (passTypeText.Checked)
					return CustomMethodPassEditorPassType.Text;
				else if (passTypeHex.Checked)
					return CustomMethodPassEditorPassType.Hex;
				return CustomMethodPassEditorPassType.Random;
			}
			set
			{
				switch (value)
				{
					case CustomMethodPassEditorPassType.Text:
					case CustomMethodPassEditorPassType.Hex:
						UpdateEditorSuitably();
						break;
					default:
						passTypeRandom.Checked = true;
						break;
				}
			}
		}

		[Description("The pass constant being edited.")]
		[Category("Behavior")]
		public byte[] PassData
		{
			get
			{
				//Return the cached string if the pass is a constant-writing pass.
				switch (PassType)
				{
					case CustomMethodPassEditorPassType.Random:
						return null;
					default:
						return passData;
				}
			}
			set
			{
				//Store the data into our class variable, so it can be edited.
				passData = value;

				//Update the UI
				UpdateEditorSuitably();
			}
		}


		/// <summary>
		/// Parses a string either for UTF-8 characters or hexadecimal digits, returning
		/// its value in a byte array.
		/// </summary>
		/// <param name="text">The string to parse.</param>
		/// <param name="parseHex">Parses <paramref name="text"/> as a string of
		/// hexadecimal numbers if true; a UTF-8 string otherwise.</param>
		/// <returns>An array containing the byte-wise representation of the input
		/// string.</returns>
		/// <exception cref="FormatException">Throws a <see cref="System.FormatException"/>
		/// if the array cannot be displayed in the given representation.</exception>
		private static byte[] GetConstantArray(string text, bool parseHex)
		{
			if (parseHex)
			{
				string str = text.Replace(" ", "").ToUpper(CultureInfo.CurrentCulture);
				List<byte> passConstantList = new List<byte>();

				if (!string.IsNullOrEmpty(str))
				{
					for (int i = 0, j = str.Length - 2; i < j; i += 2)
						passConstantList.Add(Convert.ToByte(str.Substring(i, 2), 16));
					passConstantList.Add(Convert.ToByte(str.Substring(Math.Max(0, str.Length - 2)), 16));
				}

				byte[] result = new byte[passConstantList.Count];
				passConstantList.CopyTo(result);
				return result;
			}

			return Encoding.UTF8.GetBytes(text);
		}

		/// <summary>
		/// Displays the pass constant stored by the <see cref="GetConstantArray"/>
		/// function.
		/// </summary>
		/// <param name="array">The array containing the constant to display.</param>
		/// <param name="asHex">Sets whether the array should be displayed as a
		/// hexadecimal string.</param>
		/// <exception cref="System.FormatException">Thrown when the hexadecimal string
		/// cannot be parsed as a a UTF-8 string, including the presence of NULL
		/// bytes</exception>
		/// <returns>A string containing the user-visible representation of the
		/// input array.</returns>
		private static string GetConstantStr(byte[] array, bool asHex)
		{
			if (array == null || array.Length == 0)
				return string.Empty;

			if (asHex)
			{
				StringBuilder displayText = new StringBuilder();
				foreach (byte b in array)
					displayText.Append(string.Format(CultureInfo.CurrentCulture,
						"{0:X2} ", b, 16));
				return displayText.ToString();
			}
			else
			{
				//Check for the presence of null bytes in the source string. If so,
				//the display is always hexadecimal.
				foreach (byte b in array)
					if (b == 0)
						throw new DecoderFallbackException("The custom pass constant contains " +
							"embedded NULL bytes which cannot be represented as text.");

				//Parse the binary data as UTF-8
				UTF8Encoding encoding = new UTF8Encoding(false, true);
				return encoding.GetString(array);
			}
		}

		/// <summary>
		/// Updates the editor with the pass constant, choosing a suitable display
		/// format based on the data.
		/// </summary>
		private void UpdateEditorSuitably()
		{
			//Try to display the pass data as a string, if possible, since it is
			//easier for users to understand.
			if (passData != null && passData.Length > 0)
				try
				{
					passTxt.Text = GetConstantStr(passData, false);
					passTypeText.Checked = true;
				}
				catch (DecoderFallbackException)
				{
					passTxt.Text = GetConstantStr(passData, true);
					passTypeHex.Checked = true;
				}
		}

		/// <summary>
		/// Updates the editor with the pass constant in the format requested by the user.
		/// </summary>
		private void UpdateEditor()
		{
			try
			{
				//Display the constant on the Text Field, if the pass type requires it.
				if (!passTypeRandom.Checked)
					passTxt.Text = GetConstantStr(passData, passTypeHex.Checked);
				else
					passTxt.Text = string.Empty;
				passTxt.Enabled = PassType != CustomMethodPassEditorPassType.Random;
			}
			catch (DecoderFallbackException)
			{
				passTypeHex.CheckedChanged -= passType_CheckedChanged;
				passTypeHex.Checked = true;
				passTypeHex.CheckedChanged += passType_CheckedChanged;

				MessageBox.Show(this, S._("The pass constant cannot be displayed as " +
					"text because it contains invalid characters."), S._("Eraser"),
					 MessageBoxButtons.OK, MessageBoxIcon.Information,
					 MessageBoxDefaultButton.Button1,
					 Localisation.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}
		}

		private void CustomMethodPassEditor_Validating(object sender, CancelEventArgs e)
		{
			if ((passTypeText.Checked || passTypeHex.Checked) &&
				(passData == null || passData.Length == 0))
			{
				errorProvider.SetError(passTxt, S._("The pass constant must not be empty."));
				errorProvider.SetIconPadding(passTxt, -errorProvider.Icon.Width);
				e.Cancel = true;
			}
		}

		private void passText_Validating(object sender, CancelEventArgs e)
		{
			//Try to parse the pass constant, showing the error if it fails.
			try
			{
				passData = GetConstantArray(passTxt.Text, passTypeHex.Checked);
			}
			catch (FormatException)
			{
				e.Cancel = true;
				errorProvider.SetError(passTxt, S._("The input text is invalid " +
					"for the current data type. Valid hexadecimal characters are the " +
					"digits 0-9 and letters A-F"));
			}
		}

		private void passTxt_Validated(object sender, EventArgs e)
		{
			errorProvider.Clear();
		}

		private void passType_CheckedChanged(object sender, EventArgs e)
		{
			//We are only concerned with the check event (since the uncheck event is
			//processed by the validate function.)
			if (!((RadioButton)sender).Checked)
				return;

			//Ok, update the UI.
			UpdateEditor();
		}

		/// <summary>
		/// The last-saved pass data.
		/// </summary>
		private byte[] passData;
	}

	/// <summary>
	/// The different pass types which this editor can edit.
	/// </summary>
	public enum CustomMethodPassEditorPassType
	{
		Text,
		Hex,
		Random
	}
}
