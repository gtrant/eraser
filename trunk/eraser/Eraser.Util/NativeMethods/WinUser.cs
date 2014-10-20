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

namespace Eraser.Util
{
	internal static partial class NativeMethods
	{
		public const int BS_SPLITBUTTON = 0x0000000C;
		public const uint BCM_FIRST = 0x1600;
		public const uint BCM_SETDROPDOWNSTATE = BCM_FIRST + 6;
		public const uint BCN_FIRST = unchecked(0U - 1250U);
		public const uint BCN_DROPDOWN = BCN_FIRST + 0x0002;
		public const uint WM_NOTIFY = 0x004E;
		public const uint WM_PAINT = 0x000F;
		public const uint WM_CONTEXTMENU = 0x007B;

		#pragma warning disable 0649
		public struct NMHDR
		{
			public IntPtr hwndFrom;
			public uint idFrom;
			public uint code;
		}
		#pragma warning restore 0649
	}
}
