/* 
 * $Id$
 * Copyright 2009 The Eraser Project
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
	Fat12Or16Api::Fat12Or16Api(VolumeInfo^ info) : FatApi(info)
	{
		//Sanity checks: check that this volume is FAT12 or FAT16!
		if (info->VolumeFormat != L"FAT")
			throw gcnew ArgumentException(L"The volume provided is not a FAT12 or FAT16 volume.");
	}

	Fat12Or16Api::Fat12Or16Api(VolumeInfo^ info, IO::Stream^ stream) : FatApi(info, stream)
	{
		//Sanity checks: check that this volume is FAT12 or FAT16!
		if (info->VolumeFormat != L"FAT")
			throw gcnew ArgumentException(L"The volume provided is not a FAT12 or FAT16 volume.");
	}

	void Fat12Or16Api::LoadFat()
	{
		Fat = new char[SectorSizeToSize(BootSector->SectorsPerFat)];

		//Seek to the FAT
		VolumeStream->Seek(SectorToOffset(BootSector->ReservedSectorCount), SeekOrigin::Begin);

		//Read the FAT
		array<Byte>^ buffer = gcnew array<Byte>(SectorSizeToSize(BootSector->SectorsPerFat));
		VolumeStream->Read(buffer, 0, SectorSizeToSize(BootSector->SectorsPerFat));
		Marshal::Copy(buffer, 0, static_cast<IntPtr>(Fat), buffer->Length);
	}

	FatDirectoryBase^ Fat12Or16Api::LoadDirectory(unsigned cluster, String^ name,
		FatDirectoryBase^ parent)
	{
		return gcnew Directory(name, parent, cluster, this);
	}

	long long Fat12Or16Api::ClusterToOffset(unsigned cluster)
	{
		unsigned long long sector = BootSector->ReservedSectorCount +											//Reserved area
			BootSector->FatCount * BootSector->SectorsPerFat +													//FAT area
			(BootSector->RootDirectoryEntryCount * sizeof(::FatDirectoryEntry) / (ClusterSize / SectorSize)) +	//Root directory area
			(static_cast<unsigned long long>(cluster) - 2) * (ClusterSize / SectorSize);
		return SectorToOffset(sector);
	}

	unsigned Fat12Or16Api::DirectoryToCluster(String^ path)
	{
		throw gcnew NotImplementedException();
	}

	bool Fat12Or16Api::IsFat12()
	{
		unsigned long long numberOfSectors = (BootSector->SectorCount16 == 0 ?
			BootSector->SectorCount32 : BootSector->SectorCount16);
		unsigned long long availableSectors = numberOfSectors - (
			BootSector->ReservedSectorCount +																//Reserved area
			BootSector->FatCount * BootSector->SectorsPerFat +												//FAT area
			(BootSector->RootDirectoryEntryCount * sizeof(::FatDirectoryEntry) / (ClusterSize / SectorSize))	//Root directory area
		);
		unsigned long long numberOfClusters = availableSectors / (ClusterSize / SectorSize);

		return numberOfClusters < 0xFF0;
	}

	Fat12Or16Api::Directory::Directory(String^ name, FatDirectoryBase^ parent, unsigned cluster,
		Fat12Or16Api^ api) : FatDirectory(name, parent, cluster, api)
	{
	}

	unsigned Fat12Or16Api::Directory::GetStartCluster(::FatDirectoryEntry& directory)
	{
		if (directory.Short.Attributes == 0x0F)
			throw gcnew ArgumentException(L"The provided directory is a long file name.");
		return directory.Short.StartClusterLow;
	}
}
}
