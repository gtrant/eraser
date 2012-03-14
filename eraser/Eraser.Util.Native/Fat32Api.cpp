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
	Fat32Api::Fat32Api(VolumeInfo^ info) : FatApi(info)
	{
		//Sanity checks: check that this volume is FAT32!
		if (info->VolumeFormat != L"FAT32")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT32 volume."));
	}

	Fat32Api::Fat32Api(VolumeInfo^ info, Stream^ stream) : FatApi( stream)
	{
		//Sanity checks: check that this volume is FAT32!
		if (info->VolumeFormat != L"FAT32")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT32 volume."));
	}

	void Fat32Api::LoadFat()
	{
		Fat = new char[SectorSizeToSize(BootSector->Fat32ParameterBlock.SectorsPerFat)];

		//Seek to the FAT
		VolumeStream->Seek(SectorToOffset(BootSector->ReservedSectorCount), SeekOrigin::Begin);

		//Read the FAT
		array<Byte>^ buffer = gcnew array<Byte>(SectorSizeToSize(BootSector->Fat32ParameterBlock.SectorsPerFat));
		VolumeStream->Read(buffer, 0, SectorSizeToSize(BootSector->Fat32ParameterBlock.SectorsPerFat));
		Marshal::Copy(buffer, 0, static_cast<IntPtr>(Fat), buffer->Length);
	}

	FatDirectoryBase^ Fat32Api::LoadDirectory(unsigned cluster, String^ name,
		FatDirectoryBase^ parent)
	{
		return gcnew Directory(name, parent, cluster, this);
	}

	long long Fat32Api::ClusterToOffset(unsigned cluster)
	{
		unsigned long long sector = BootSector->ReservedSectorCount +				//Reserved area
			BootSector->FatCount * BootSector->Fat32ParameterBlock.SectorsPerFat +	//FAT area
			(static_cast<unsigned long long>(cluster) - 2) *  BootSector->SectorsPerCluster;
		return SectorToOffset(sector);
	}

	bool Fat32Api::IsClusterAllocated(unsigned cluster)
	{
		unsigned* fatPtr = reinterpret_cast<unsigned*>(Fat);
		if (
			fatPtr[cluster] <= 0x00000001 ||
			(fatPtr[cluster] >= 0x0FFFFFF0 && fatPtr[cluster] <= 0x0FFFFFF6) ||
			fatPtr[cluster] == 0x0FFFFFF7
		)
			return false;

		return true;
	}

	unsigned Fat32Api::GetNextCluster(unsigned cluster)
	{
		unsigned* fatPtr = reinterpret_cast<unsigned*>(Fat);
		if (fatPtr[cluster] <= 0x00000001 || (fatPtr[cluster] >= 0x0FFFFFF0 && fatPtr[cluster] <= 0x0FFFFFF6))
			throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked free."));
		else if (fatPtr[cluster] == 0x0FFFFFF7)
			throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked bad."));
		else if (fatPtr[cluster] >= 0x0FFFFFF8)
			return 0xFFFFFFFF;
		else
			return fatPtr[cluster];
	}

	unsigned Fat32Api::FileSize(unsigned cluster)
	{
		unsigned* fatPtr = reinterpret_cast<unsigned*>(Fat);
		for (unsigned result = 1; ; ++result)
		{
			if (fatPtr[cluster] <= 0x00000001 || (fatPtr[cluster] >= 0x0FFFFFF0 && fatPtr[cluster] <= 0x0FFFFFF6))
				throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked free."));
			else if (fatPtr[cluster] == 0x0FFFFFF7)
				throw gcnew ArgumentException(S::_(L"Invalid FAT cluster: cluster is marked bad."));
			else if (fatPtr[cluster] >= 0x0FFFFFF8)
				return ClusterSizeToSize(result);
			else
				cluster = fatPtr[cluster];
		}
	}

	unsigned Fat32Api::DirectoryToCluster(String^ path)
	{
		//The path must start with a backslash as it must be volume-relative.
		if (path->Length != 0)
		{
			if (path[0] != L'\\')
				throw gcnew ArgumentException(S::_(L"The path provided is not volume relative. " +
					L"Volume relative paths must begin with a backslash."));
			path = path->Remove(0, 1);
		}

		//Chop the path into it's constituent directory components
		array<String^>^ components = path->Split(Path::DirectorySeparatorChar,
			Path::AltDirectorySeparatorChar);

		//Traverse the directories until we get the cluster we want.
		unsigned cluster = BootSector->Fat32ParameterBlock.RootDirectoryCluster;
		FatDirectoryBase^ parentDir = nullptr;
		for each (String^ component in components)
		{
			if (String::IsNullOrEmpty(component))
				break;

			parentDir = LoadDirectory(cluster, parentDir == nullptr ? String::Empty : parentDir->Name,
				parentDir);
			cluster = parentDir->Items[component]->Cluster;
		}

		return cluster;
	}

	Fat32Api::Directory::Directory(String^ name, FatDirectoryBase^ parent, unsigned cluster, Fat32Api^ api)
		: FatDirectory(name, parent, cluster, api)
	{
	}

	unsigned Fat32Api::Directory::GetStartCluster(::FatDirectoryEntry& directory)
	{
		if (directory.Short.Attributes == 0x0F)
			throw gcnew ArgumentException(L"The provided directory is a long file name.");
		return directory.Short.StartClusterLow | (unsigned(directory.Short.StartClusterHigh) << 16);
	}
}
}
