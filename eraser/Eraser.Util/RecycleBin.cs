﻿/* 
 * $Id: RecycleBin.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Garrett Trant <gtrant@users.sourceforge.net>
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

namespace Eraser.Util
{
	public static class RecycleBin
	{
		/// <summary>
		/// Empties the recycle bin for the current user.
		/// </summary>
		/// <param name="options">The list of flags to pass to the shell regarding
		/// the user feedback, etc.</param>
		public static void Empty(EmptyRecycleBinOptions options)
		{
			NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null,
				(NativeMethods.SHEmptyRecycleBinFlags)options);
		}
	}

	[Flags]
	public enum EmptyRecycleBinOptions
	{
		/// <summary>
		/// No flags specified.
		/// </summary>
		None = 0,

		/// <summary>
		/// No dialog box confirming the deletion of the objects will be displayed. 
		/// </summary>
		NoConfirmation = (int)NativeMethods.SHEmptyRecycleBinFlags.SHERB_NOCONFIRMATION,

		/// <summary>
		/// No dialog box indicating the progress will be displayed.
		/// </summary>
		NoProgressUI = (int)NativeMethods.SHEmptyRecycleBinFlags.SHERB_NOPROGRESSUI,

		/// <summary>
		/// No sound will be played when the operation is complete.
		/// </summary>
		NoSound = (int)NativeMethods.SHEmptyRecycleBinFlags.SHERB_NOSOUND
	}
}
