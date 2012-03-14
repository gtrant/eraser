/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
	}
}
