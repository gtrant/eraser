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

#include <stdafx.h>
#include "FatApi.h"

using namespace System::IO;
using namespace System::Runtime::InteropServices;

namespace Eraser {
namespace Util {
	Fat12Api::Fat12Api(VolumeInfo^ info) : Fat12Or16Api(info)
	{
		//Sanity checks: check that this volume is FAT16!
		if (!IsFat12() || info->VolumeFormat == L"FAT16")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT12 volume."));
	}

	Fat12Api::Fat12Api(VolumeInfo^ info, IO::Stream^ stream) : Fat12Or16Api(info, stream)
	{
		//Sanity checks: check that this volume is FAT16!
		if (!IsFat12() || info->VolumeFormat == L"FAT16")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT12 volume."));
	}

	bool Fat12Api::IsClusterAllocated(unsigned cluster)
	{
		unsigned nextCluster = GetFatValue(cluster);

		if (
			nextCluster <= 0x001 ||
			(nextCluster >= 0xFF0 && nextCluster <= 0xFF6) ||
			nextCluster == 0xFF7
		)
			return false;

		return true;
	}

	unsigned Fat12Api::GetNextCluster(unsigned cluster)
	{
		unsigned nextCluster = GetFatValue(cluster);
		if (nextCluster <= 0x001 || (nextCluster >= 0xFF0 && nextCluster <= 0xFF6))
			throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked free."));
		else if (nextCluster == 0xFF7)
			throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked bad."));
		else if (nextCluster >= 0xFF8)
			return 0xFFFFFFFF;
		else
			return nextCluster;
	}

	unsigned Fat12Api::FileSize(unsigned cluster)
	{
		for (unsigned result = 1; ; ++result)
		{
			unsigned nextCluster = GetFatValue(cluster);
			if (nextCluster <= 0x001 || (nextCluster >= 0xFFF0 && nextCluster <= 0xFF6))
				throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked free."));
			else if (nextCluster == 0xFF7)
				throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked bad."));
			else if (nextCluster >= 0xFF8)
				return ClusterSizeToSize(result);
			else
				cluster = nextCluster;
		}
	}

	unsigned Fat12Api::GetFatValue(unsigned cluster)
	{
		//Get the pointer to the FAT entry. Round the cluster value down to the nearest
		//even number (since 2 clusters share 3 bytes)
		char* fatEntry = Fat + ((cluster & ~1) / 2) * 3;
		unsigned fatValue = 0;
		for (size_t i = 0; i < 3; ++i)
			fatValue |= static_cast<unsigned>(static_cast<unsigned char>(*(fatEntry + i))) << (i * 8);

		//Get the correct half of the 24 bits. If the cluster is odd we take the 12 least significant bits
		if (cluster & 1)
			fatValue >>= 12;
		else
			fatValue &= 0xFFF;

		//Return the result.
		return fatValue;
	}
}
}
