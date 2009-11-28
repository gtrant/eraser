/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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

namespace Eraser.Util
{
	public static class NtfsApi
	{
		/// <summary>
		/// Gets the actual size of the MFT.
		/// </summary>
		/// <param name="volume">The volume to query.</param>
		/// <returns>The size of the MFT.</returns>
		public static long GetMftValidSize(VolumeInfo volume)
		{
			NTApi.NativeMethods.NTFS_VOLUME_DATA_BUFFER data =
				NTApi.NativeMethods.GetNtfsVolumeData(volume);
			return data.MftValidDataLength;
		}

		/// <summary>
		/// Gets the size of one MFT record segment.
		/// </summary>
		/// <param name="volume">The volume to query.</param>
		/// <returns>The size of one MFT record segment.</returns>
		public static long GetMftRecordSegmentSize(VolumeInfo volume)
		{
			NTApi.NativeMethods.NTFS_VOLUME_DATA_BUFFER data =
				NTApi.NativeMethods.GetNtfsVolumeData(volume);
			return data.BytesPerFileRecordSegment;
		}
	}
}
