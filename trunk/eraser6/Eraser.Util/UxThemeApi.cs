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
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace Eraser.Util
{
	public static class UXThemeApi
	{
		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <remarks>This function will also set the volume on all child controls.</remarks>
		public static void UpdateControlTheme(Control control)
		{
			if (control is Form)
				((Form)control).Font = SystemFonts.MessageBoxFont;
			if (control is ListView)
				UpdateControlTheme((ListView)control);
			else if (control is ToolStrip)
				UpdateControlTheme((ToolStrip)control);

			if (control.ContextMenuStrip != null)
				UpdateControlTheme(control.ContextMenuStrip);
			
			foreach (Control child in control.Controls)
				UpdateControlTheme(child);
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="lv">The List View control to set the theme on.</param>
		public static void UpdateControlTheme(ListView lv)
		{
			try
			{
				NativeMethods.SetWindowTheme(lv.Handle, "EXPLORER", null);
				UserApi.NativeMethods.SendMessage(lv.Handle,
					UserApi.NativeMethods.LVM_SETEXTENDEDLISTVIEWSTYLE,
					(UIntPtr)UserApi.NativeMethods.LVS_EX_DOUBLEBUFFER,
					(IntPtr)UserApi.NativeMethods.LVS_EX_DOUBLEBUFFER);
			}
			catch (DllNotFoundException)
			{
			}
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="menu">The tool strip control to set the theme on.</param>
		public static void UpdateControlTheme(ToolStrip menu)
		{
			if (Environment.OSVersion.Version.Major >= 6)
				if (menu.Renderer is ToolStripProfessionalRenderer)
					menu.Renderer = new UXThemeMenuRenderer();

			foreach (ToolStripItem item in menu.Items)
			{
				ToolStripMenuItem toolStripItem = item as ToolStripMenuItem;
				if (toolStripItem != null)
					UpdateControlTheme(toolStripItem);
			}
		}

		/// <summary>
		/// Updates the control's theme to fit in with the latest Windows visuals.
		/// </summary>
		/// <param name="menu">The List View control to set the theme on.</param>
		public static void UpdateControlTheme(ToolStripDropDownItem menu)
		{
			UpdateControlTheme(menu.DropDown);
		}

		/// <summary>
		/// Stores functions, structs and constants from UxTheme.dll and User32.dll
		/// </summary>
		internal static class NativeMethods
		{
			/// <summary>
			/// Causes a window to use a different set of visual style information
			/// than its class normally uses.
			/// </summary>
			/// <param name="hwnd">Handle to the window whose visual style information
			/// is to be changed.</param>
			/// <param name="pszSubAppName">Pointer to a string that contains the
			/// application name to use in place of the calling application's name.
			/// If this parameter is NULL, the calling application's name is used.</param>
			/// <param name="pszSubIdList">Pointer to a string that contains a
			/// semicolon-separated list of class identifier (CLSID) names to use
			/// in place of the actual list passed by the window's class. If this
			/// parameter is NULL, the ID list from the calling class is used.</param>
			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			public static extern void SetWindowTheme(IntPtr hwnd, string pszSubAppName,
				string pszSubIdList);
		}
	}

	public class UXThemeMenuRenderer : ToolStripRenderer
	{
		~UXThemeMenuRenderer()
		{
			hTheme.Close();
		}

		protected override void Initialize(ToolStrip toolStrip)
		{
			base.Initialize(toolStrip);

			control = toolStrip;
			hTheme = NativeMethods.OpenThemeData(toolStrip.Handle, "MENU");
		}

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			IntPtr hDC = e.Graphics.GetHdc();
			Rectangle rect = e.AffectedBounds;

			if (NativeMethods.IsThemeBackgroundPartiallyTransparent(hTheme,
				(int)NativeMethods.MENUPARTS.MENU_POPUPBACKGROUND, 0))
			{
				NativeMethods.DrawThemeParentBackground(control.Handle, hDC, ref rect);
			}
			NativeMethods.DrawThemeBackground(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPBACKGROUND, 0, ref rect, ref rect);

			if (NativeMethods.IsThemeBackgroundPartiallyTransparent(hTheme,
				(int)NativeMethods.MENUPARTS.MENU_POPUPBORDERS, 0))
			{
				NativeMethods.DrawThemeParentBackground(control.Handle, hDC, ref rect);
			}
			NativeMethods.DrawThemeBackground(hTheme, hDC, (int)
				NativeMethods.MENUPARTS.MENU_POPUPBORDERS, 0, ref rect, ref rect);

			e.Graphics.ReleaseHdc();
		}

		protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
		{
			IntPtr hDC = e.Graphics.GetHdc();
			Rectangle rect = e.AffectedBounds;
			rect.Width = GutterWidth;
			rect.Inflate(-1, -1);
			rect.Offset(1, 0);

			if (NativeMethods.IsThemeBackgroundPartiallyTransparent(hTheme,
				(int)NativeMethods.MENUPARTS.MENU_POPUPGUTTER, 0))
			{
				NativeMethods.DrawThemeParentBackground(control.Handle, hDC, ref rect);
			}
			NativeMethods.DrawThemeBackground(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPGUTTER, 0, ref rect, ref rect);

			e.Graphics.ReleaseHdc();
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		{
			Rectangle rect = Rectangle.Truncate(e.Graphics.VisibleClipBounds);
			rect.Inflate(-1, 0);
			rect.Offset(1, 0);
			IntPtr hDC = e.Graphics.GetHdc();

			int itemState = (int)(e.Item.Selected ?
				(e.Item.Enabled ? NativeMethods.POPUPITEMSTATES.MPI_HOT :
					NativeMethods.POPUPITEMSTATES.MPI_DISABLEDHOT) :
				(e.Item.Enabled ? NativeMethods.POPUPITEMSTATES.MPI_NORMAL :
					NativeMethods.POPUPITEMSTATES.MPI_DISABLED));
			NativeMethods.DrawThemeBackground(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPITEM, itemState, ref rect, ref rect);

			e.Graphics.ReleaseHdc();
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
		{
			IntPtr hDC = e.Graphics.GetHdc();
			Rectangle rect = new Rectangle(GutterWidth, 0, e.Item.Width, e.Item.Height);
			rect.Inflate(4, 0);

			NativeMethods.DrawThemeBackground(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPSEPARATOR, 0, ref rect, ref rect);

			e.Graphics.ReleaseHdc();
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			if (!(e.Item is ToolStripMenuItem))
			{
				base.OnRenderItemCheck(e);
				return;
			}

			Rectangle imgRect = e.ImageRectangle;
			imgRect.Inflate(4, 3);
			imgRect.Offset(1, 0);
			Rectangle bgRect = imgRect;

			IntPtr hDC = e.Graphics.GetHdc();
			ToolStripMenuItem item = (ToolStripMenuItem)e.Item;

			int bgState = (int)(e.Item.Enabled ? NativeMethods.POPUPCHECKBACKGROUNDSTATES.MCB_NORMAL :
				NativeMethods.POPUPCHECKBACKGROUNDSTATES.MCB_DISABLED);
			NativeMethods.DrawThemeBackground(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPCHECKBACKGROUND, bgState,
				ref bgRect, ref bgRect);

			int checkState = (int)(item.Checked ?
				(item.Enabled ? NativeMethods.POPUPCHECKSTATES.MC_CHECKMARKNORMAL :
					NativeMethods.POPUPCHECKSTATES.MC_CHECKMARKDISABLED) : 0);
			if (NativeMethods.IsThemeBackgroundPartiallyTransparent(hTheme,
				(int)NativeMethods.MENUPARTS.MENU_POPUPCHECK, checkState))
			{
				NativeMethods.DrawThemeParentBackground(control.Handle, hDC, ref imgRect);
			}
			NativeMethods.DrawThemeBackground(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPCHECK, checkState,
				ref imgRect, ref imgRect);

			e.Graphics.ReleaseHdc();
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			int itemState = (int)(e.Item.Selected ?
				(e.Item.Enabled ? NativeMethods.POPUPITEMSTATES.MPI_HOT :
					NativeMethods.POPUPITEMSTATES.MPI_DISABLEDHOT) :
				(e.Item.Enabled ? NativeMethods.POPUPITEMSTATES.MPI_NORMAL :
					NativeMethods.POPUPITEMSTATES.MPI_DISABLED));

			Rectangle rect = new Rectangle(e.TextRectangle.Left, 0,
				e.Item.Width - e.TextRectangle.Left, e.Item.Height);
			IntPtr hFont = e.TextFont.ToHfont();
			IntPtr hDC = e.Graphics.GetHdc();
			NativeMethods.SelectObject(hDC, hFont);

			NativeMethods.DrawThemeText(hTheme, hDC,
				(int)NativeMethods.MENUPARTS.MENU_POPUPITEM, itemState, e.Text,
				-1, e.TextFormat | TextFormatFlags.WordEllipsis | TextFormatFlags.SingleLine,
				0, ref rect);

			e.Graphics.ReleaseHdc();
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			int itemState = (int)(e.Item.Enabled ? NativeMethods.POPUPSUBMENUSTATES.MSM_NORMAL :
				NativeMethods.POPUPSUBMENUSTATES.MSM_DISABLED);

			//Strangely, UxTheme won't draw any arrow once the starting coordinate
			//is beyond 5px. So draw the arrow on a backing image then blit
			//to the actual one.
			using (Bitmap backBmp = new Bitmap(e.ArrowRectangle.Width, e.ArrowRectangle.Height))
			{
				using (Graphics backGfx = Graphics.FromImage(backBmp))
				{
					IntPtr hDC = backGfx.GetHdc();

					Rectangle backRect = new Rectangle(new Point(0, 0), backBmp.Size);
					NativeMethods.DrawThemeBackground(hTheme, hDC,
						(int)NativeMethods.MENUPARTS.MENU_POPUPSUBMENU, itemState,
						ref backRect, ref backRect);
					backGfx.ReleaseHdc();
				}

				e.Graphics.DrawImageUnscaled(backBmp, e.ArrowRectangle);
			}
		}

		private static int GutterWidth
		{
			get
			{
				return 2 * (SystemInformation.MenuCheckSize.Width + SystemInformation.BorderSize.Width);
			}
		}

		private ToolStrip control;
		private SafeThemeHandle hTheme;

		/// <summary>
		/// Imported UxTheme functions and constants.
		/// </summary>
		internal static class NativeMethods
		{
			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			public static extern SafeThemeHandle OpenThemeData(IntPtr hwnd, string pszClassList);

			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr CloseThemeData(IntPtr hwndTeme);

			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr DrawThemeParentBackground(IntPtr hwnd,
				IntPtr hdc, ref Rectangle prc);

			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool IsThemeBackgroundPartiallyTransparent(
				SafeThemeHandle hTheme, int iPartId, int iStateId);

			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr DrawThemeBackground(
				SafeThemeHandle hTheme, IntPtr hdc, int iPartId, int iStateId,
				ref Rectangle pRect, ref Rectangle pClipRect);

			[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
			public extern static int DrawThemeText(SafeThemeHandle hTheme,
				IntPtr hDC, int iPartId, int iStateId,
				[MarshalAs(UnmanagedType.LPWStr)] string pszText, int iCharCount,
				TextFormatFlags dwTextFlag, int dwTextFlags2, ref Rectangle pRect);

			[DllImport("Gdi32.dll")]
			public extern static IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

			public enum MENUPARTS
			{
				MENU_MENUITEM_TMSCHEMA = 1,
				MENU_MENUDROPDOWN_TMSCHEMA = 2,
				MENU_MENUBARITEM_TMSCHEMA = 3,
				MENU_MENUBARDROPDOWN_TMSCHEMA = 4,
				MENU_CHEVRON_TMSCHEMA = 5,
				MENU_SEPARATOR_TMSCHEMA = 6,
				MENU_BARBACKGROUND = 7,
				MENU_BARITEM = 8,
				MENU_POPUPBACKGROUND = 9,
				MENU_POPUPBORDERS = 10,
				MENU_POPUPCHECK = 11,
				MENU_POPUPCHECKBACKGROUND = 12,
				MENU_POPUPGUTTER = 13,
				MENU_POPUPITEM = 14,
				MENU_POPUPSEPARATOR = 15,
				MENU_POPUPSUBMENU = 16,
				MENU_SYSTEMCLOSE = 17,
				MENU_SYSTEMMAXIMIZE = 18,
				MENU_SYSTEMMINIMIZE = 19,
				MENU_SYSTEMRESTORE = 20,
			}

			public enum POPUPCHECKSTATES
			{
				MC_CHECKMARKNORMAL = 1,
				MC_CHECKMARKDISABLED = 2,
				MC_BULLETNORMAL = 3,
				MC_BULLETDISABLED = 4,
			}

			public enum POPUPCHECKBACKGROUNDSTATES
			{
				MCB_DISABLED = 1,
				MCB_NORMAL = 2,
				MCB_BITMAP = 3,
			}

			public enum POPUPITEMSTATES
			{
				MPI_NORMAL = 1,
				MPI_HOT = 2,
				MPI_DISABLED = 3,
				MPI_DISABLEDHOT = 4,
			}

			public enum POPUPSUBMENUSTATES
			{
				MSM_NORMAL = 1,
				MSM_DISABLED = 2,
			}
		}
	}

	internal class SafeThemeHandle : SafeHandle
	{
		public SafeThemeHandle()
			: base(IntPtr.Zero, true)
		{
		}

		public override bool IsInvalid
		{
			get { return handle == IntPtr.Zero; }
		}

		protected override bool ReleaseHandle()
		{
			UXThemeMenuRenderer.NativeMethods.CloseThemeData(handle);
			handle = IntPtr.Zero;
			return true;
		}
	}
}
