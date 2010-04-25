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
using System.Drawing;
using System.Windows.Forms;

namespace Eraser.Util
{
	internal static partial class NativeMethods
	{
		[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsThemeActive();

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

		[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr GetThemeMargins(SafeThemeHandle hTheme,
			IntPtr hdc, int iPartId, int iStateId, int iPropId, ref Rectangle prc,
			ref Rectangle pMargins);

		[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr GetThemeMargins(SafeThemeHandle hTheme,
			IntPtr hdc, int iPartId, int iStateId, int iPropId, IntPtr prc,
			ref Rectangle pMargins);

		[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr GetThemePartSize(SafeThemeHandle hTheme,
			IntPtr hdc, int iPartId, int iStateId, ref Rectangle prc,
			THEMESIZE eSize, ref Size psz);

		[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr GetThemePartSize(SafeThemeHandle hTheme,
			IntPtr hdc, int iPartId, int iStateId, IntPtr prc,
			THEMESIZE eSize, ref Size psz);

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

		public enum TMT_MARGINS
		{
			TMT_SIZINGMARGINS = 3601,
			TMT_CONTENTMARGINS,
			TMT_CAPTIONMARGINS
		}

		public enum THEMESIZE
		{
			TS_MIN,
			TS_TRUE,
			TS_DRAW
		}

		public const int WM_THEMECHANGED = 0x031A;
		public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
	}
}
