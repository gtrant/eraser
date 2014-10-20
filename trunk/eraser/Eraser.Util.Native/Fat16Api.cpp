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

#include <stdafx.h>
#include "FatApi.h"

using namespace System::IO;
using namespace System::Runtime::InteropServices;

namespace Eraser {
namespace Util {
	Fat16Api::Fat16Api(VolumeInfo^ info) : Fat12Or16Api(info)
	{
		//Sanity checks: check that this volume is FAT16!
		if (IsFat12() || info->VolumeFormat == L"FAT12")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT16 volume."));
	}

	Fat16Api::Fat16Api(VolumeInfo^ info, IO::Stream^ stream) : Fat12Or16Api(info, stream)
	{
		//Sanity checks: check that this volume is FAT16!
		if (IsFat12() || info->VolumeFormat == L"FAT12")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT16 volume."));
	}

	bool Fat16Api::IsClusterAllocated(unsigned cluster)
	{
		unsigned short* fatPtr = reinterpret_cast<unsigned short*>(Fat);
		if (
			fatPtr[cluster] <= 0x0001 ||
			(fatPtr[cluster] >= 0xFFF0 && fatPtr[cluster] <= 0xFFF6) ||
			fatPtr[cluster] == 0xFFF7
		)
			return false;

		return true;
	}

	unsigned Fat16Api::GetNextCluster(unsigned cluster)
	{
		unsigned short* fatPtr = reinterpret_cast<unsigned short*>(Fat);
		if (fatPtr[cluster] <= 0x0001 || (fatPtr[cluster] >= 0xFFF0 && fatPtr[cluster] <= 0xFFF6))
			throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked free."));
		else if (fatPtr[cluster] == 0xFFF7)
			throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked bad."));
		else if (fatPtr[cluster] >= 0xFFF8)
			return 0xFFFFFFFF;
		else
			return fatPtr[cluster];
	}

	unsigned Fat16Api::FileSize(unsigned cluster)
	{
		unsigned short* fatPtr = reinterpret_cast<unsigned short*>(Fat);
		for (unsigned result = 1; ; ++result)
		{
			if (fatPtr[cluster] <= 0x0001 || (fatPtr[cluster] >= 0xFFF0 && fatPtr[cluster] <= 0xFFF6))
				throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked free."));
			else if (fatPtr[cluster] == 0xFFF7)
				throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked bad."));
			else if (fatPtr[cluster] >= 0xFFF8)
				return ClusterSizeToSize(result);
			else
				cluster = fatPtr[cluster];
		}
	}
}
}
