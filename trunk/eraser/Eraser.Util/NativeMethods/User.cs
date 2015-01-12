/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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

namespace Eraser.Util
{
	/// <summary>
	/// Classes, structs and constants imported from User32.dll
	/// </summary>
	internal static partial class NativeMethods
	{
		/// <summary>
		/// The GetCaretPos function copies the caret's position to the specified
		/// POINT structure.
		/// </summary>
		/// <param name="lpPoint">[out] Pointer to the POINT structure that is to
		/// receive the client coordinates of the caret.</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCaretPos(out Point lpPoint);

		/// <summary>
		/// The GetMessagePos function retrieves the cursor position for the
		/// last message retrieved by the GetMessage function.
		/// </summary>
		/// <returns>The return value specifies the x- and y-coordinates of the
		/// cursor position. The x-coordinate is the low order short and the
		/// y-coordinate is the high-order short.</returns>
		[DllImport("User32.dll")]
		public static extern uint GetMessagePos();

		/// <summary>
		/// The GetMessageTime function retrieves the message time for the last
		/// message retrieved by the GetMessage function. The time is a long
		/// integer that specifies the elapsed time, in milliseconds, from the
		/// time the system was started to the time the message was created
		/// (that is, placed in the thread's message queue).
		/// </summary>
		/// <returns>The return value specifies the message time.</returns>
		[DllImport("User32.dll")]
		public static extern int GetMessageTime();

		/// <summary>
		/// Sends the specified message to a window or windows. The SendMessage
		/// function calls the window procedure for the specified window and does
		/// not return until the window procedure has processed the message.
		/// </summary>
		/// <param name="hWnd">Handle to the window whose window procedure will
		/// receive the message. If this parameter is HWND_BROADCAST, the message
		/// is sent to all top-level windows in the system, including disabled
		/// or invisible unowned windows, overlapped windows, and pop-up windows;
		/// but the message is not sent to child windows.</param>
		/// <param name="Msg">Specifies the message to be sent.</param>
		/// <param name="wParam">Specifies additional message-specific information.</param>
		/// <param name="lParam">Specifies additional message-specific information.</param>
		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, UIntPtr wParam,
			IntPtr lParam);

		/// <summary>
		/// Paints via double-buffering, which reduces flicker. This extended
		/// style also enables alpha-blended marquee selection on systems where
		/// it is supported.
		/// </summary>
		public const uint LVS_EX_DOUBLEBUFFER = 0x00010000u;

		public const uint LVM_FIRST = 0x1000;

		/// <summary>
		/// Sets extended styles in list-view controls.
		/// </summary>
		public const uint LVM_SETEXTENDEDLISTVIEWSTYLE = (LVM_FIRST + 54);

		/// <summary>
		/// A handle to the window whose ancestor is to be retrieved. If this parameter
		/// is the desktop window, the function returns NULL.
		/// </summary>
		/// <param name="hwnd">Retrieves the handle to the ancestor of the specified
		/// window.</param>
		/// <param name="gaFlags">The ancestor to be retrieved. This parameter can be one
		/// of the <see cref="GetAncestorFlags"/ >values.</param>
		/// <returns>The return value is the handle to the ancestor window.</returns>
		[DllImport("User32.dll")]
		public static extern IntPtr GetAncestor(IntPtr hwnd, UserApi.GetAncestorFlags gaFlags);

		/// <summary>
		/// Changes the parent window of the specified child window.
		/// </summary>
		/// <param name="hWndChild">A handle to the child window.</param>
		/// <param name="hWndNewParent">A handle to the new parent window. If
		/// this parameter is NULL, the desktop window becomes the new parent window.
		/// If this parameter is HWND_MESSAGE, the child window becomes a message-only
		/// window.</param>
		/// <returns>If the function succeeds, the return value is a handle to the
		/// previous parent window.
		/// 
		/// If the function fails, the return value is NULL. To get extended error
		/// information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
	}
}
