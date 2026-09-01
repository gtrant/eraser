/* 
 * $Id: ToolBar.cs 2998 2026-07-18 22:51:27Z gtrant $
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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using Eraser.Util;

namespace Eraser
{
	public partial class ToolBar : System.Windows.Forms.MenuStrip
	{
		public ToolBar()
		{
			//Create the base component
			InitializeComponent();
			Renderer = new EraserToolStripRenderer();
		}

		private class EraserToolStripRenderer : ToolStripRenderer
		{
			/// <summary>
			/// Scales a pixel value by the current DPI scale factor.
			/// </summary>
			private int ScaleForDpi(Graphics g, int value)
			{
				return (int)(value * g.DpiX / 96.0f);
			}

			protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
			{
				Graphics g = e.Graphics;

				// Scale pixel values for high DPI support
				int inflate = ScaleForDpi(g, 3);
				int offset = ScaleForDpi(g, 3);

				//Draw the actual text
				Rectangle tempRect = e.TextRectangle;
				tempRect.Inflate(inflate, inflate);
				tempRect.Offset(offset, offset);
				e.TextRectangle = tempRect;
				using (SolidBrush textBrush = new SolidBrush(TextColour))
					g.DrawString(e.Text, e.TextFont, textBrush, e.TextRectangle);

				//If the text has got a selection, draw an underline
				if (e.Item.Selected)
				{
					SizeF textSize = g.MeasureString(e.Text, e.TextFont);
					using (Pen underlinePen = new Pen(TextColour))
					{
						Point underlineStart = e.TextRectangle.Location;
						underlineStart.Offset(0, Point.Truncate(textSize.ToPointF()).Y);
						Point underlineEnd = underlineStart;
						underlineEnd.Offset(e.TextRectangle.Width, 0);

						g.DrawLine(underlinePen, underlineStart, underlineEnd);
					}
				}
			}

			/// <summary>
			/// The margin between a drop-down arrow and the surrounding items.
			/// </summary>
			private const int ArrowMargin = 0;

			/// <summary>
			/// The colour of the menu bar text.
			/// </summary>
			private readonly Color TextColour = Color.White;
		}
	}
}
