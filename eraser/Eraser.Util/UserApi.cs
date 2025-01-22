/* 
 * $Id: UserApi.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace Eraser.Util
{
	public static class UserApi
	{
		/// <summary>
		/// Gets the current position of the system caret.
		/// </summary>
		public static Point CaretPos
		{
			get
			{
				Point result = new Point();
				if (NativeMethods.GetCaretPos(out result))
					return result;
				return Point.Empty;
			}
		}

		/// <summary>
		/// Gets the cursor position fot the last message retrieved by GetMessage.
		/// </summary>
		public static uint MessagePos
		{
			get
			{
				return NativeMethods.GetMessagePos();
			}
		}

		/// <summary>
		/// Gets the message time for the last message retrieved by GetMessage.
		/// </summary>
		public static int MessageTime
		{
			get
			{
				return NativeMethods.GetMessageTime();
			}
		}

		/// <summary>
		/// Retrieves the handle to the ancestor of the specified window.
		/// </summary>
		/// <param name="window">A handle to the window whose ancestor is to be retrieved. If
		/// this parameter is the desktop window, the function returns null.</param>
		/// <param name="flags">The ancestor to be retrieved. This parameter can be any of
		/// the <see cref="GetAncestorFlags"/> flags.</param>
		/// <returns>The return value is the handle to the ancestor window.</returns>
		public static IWin32Window GetAncestor(IWin32Window window, GetAncestorFlags flags)
		{
			IntPtr result = NativeMethods.GetAncestor(window.Handle, flags);
			if (result == IntPtr.Zero)
				return null;

			return new Win32Window(result);
		}

		[Flags]
		public enum GetAncestorFlags : uint
		{
			/// <summary>
			/// Retrieves the parent window. This does not include the owner, as it does
			/// with the GetParent function.
			/// </summary>
			GA_PARENT = 1,

			/// <summary>
			/// Retrieves the root window by walking the chain of parent windows.
			/// </summary>
			GA_ROOT = 2,
			
			/// <summary>
			/// Retrieves the owned root window by walking the chain of parent and owner
			/// windows returned by GetParent. 
			/// </summary>
			GA_ROOTOWNER = 3
		}

		/// <summary>
		/// Sets a new parent for the given window.
		/// </summary>
		/// <param name="window">The window to set the new parent on.</param>
		/// <param name="parent">The new parent of the window.</param>
		/// <returns>A handle to the old parent window.</returns>
		public static IWin32Window SetParent(IWin32Window window, IWin32Window parent)
		{
			return new Win32Window(NativeMethods.SetParent(window.Handle, parent.Handle));
		}

		internal class Win32Window : IWin32Window
		{
			public Win32Window(IntPtr hwnd)
			{
				Hwnd = hwnd;
			}

			#region IWin32Window Members

			public IntPtr Handle
			{
				get { return Hwnd; }
			}

			#endregion

			private IntPtr Hwnd;
		}
	}
}
