/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

using Eraser.Util;
using System.Drawing;

namespace System.Windows.Forms
{
	public class SplitButton : Button
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public SplitButton()
		{
			FlatStyle = FlatStyle.System;
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				if (IsSupportedOnCurrentPlatform)
					createParams.Style |= NativeMethods.BS_SPLITBUTTON;
				return createParams;
			}
		}

		protected override void WndProc(ref Message m)
		{
			switch ((uint)m.Msg)
			{
				case 0x2000 | NativeMethods.WM_NOTIFY:
					//Reflected WM_NOTIFY from parent. Check that the handle is ours
					if (m.HWnd == Handle)
					{
						//Then check the code of the message
						NativeMethods.NMHDR nmHdr = (NativeMethods.NMHDR)
							m.GetLParam(typeof(NativeMethods.NMHDR));

						//Handle only BCN_DROPDOWN messages
						if (nmHdr.code == NativeMethods.BCN_DROPDOWN)
						{
							//The dropdown portion of the button is being pressed, show the menu
							if (ContextMenuStrip != null)
							{
								Point point = new Point(Width - ContextMenuStrip.Width, Height);
								ContextMenuStrip.Show(this, point, ToolStripDropDownDirection.BelowRight);
								ContextMenuStrip.Closed += OnMenuClosed;
							}
						}
					}
					break;

				case NativeMethods.WM_PAINT:
					//Paint the control to have the dropdown portion as pressed when the
					//menu is shown.
					DropDownState = ContextMenuStrip != null && ContextMenuStrip.Visible;
					break;

				case NativeMethods.WM_CONTEXTMENU:
					//Swallow all context menu clicks on Vista and later -- otherwise
					//we will also show the context menu when the user right-clicks
					//the button.
					if (!IsSupportedOnCurrentPlatform)
						return;
					break;
			}

			base.WndProc(ref m);
		}

		/// <summary>
		/// Checks whether the current platform supports the control.
		/// </summary>
		private static bool IsSupportedOnCurrentPlatform
		{
			get
			{
				return Environment.OSVersion.Version.Major >= 6;
			}
		}

		private bool DropDownState
		{
			set
			{
				NativeMethods.SendMessage(this.Handle, NativeMethods.BCM_SETDROPDOWNSTATE,
					new UIntPtr(value ? 1U : 0), IntPtr.Zero);
			}
		}

		private void OnMenuClosed(object sender, EventArgs e)
		{
			DropDownState = false;
			ContextMenuStrip.Closed -= OnMenuClosed;
		}
	}
}
