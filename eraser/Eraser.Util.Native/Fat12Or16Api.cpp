/* 
 * $Id: Fat12Or16Api.cpp 2993 2021-09-25 17:23:27Z gtrant $
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

#include "stdafx.h"
#include "FatApi.h"

using namespace System::IO;
using namespace System::Runtime::InteropServices;

namespace Eraser {
namespace Util {
	Fat12Or16Api::Fat12Or16Api(VolumeInfo^ info) : FatApi(info)
	{
		//Sanity checks: check that this volume is FAT12 or FAT16!
		if (info->VolumeFormat != L"FAT12" && info->VolumeFormat != "FAT16")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT12 or FAT16 volume."));
	}

	Fat12Or16Api::Fat12Or16Api(VolumeInfo^ info, IO::Stream^ stream) : FatApi(stream)
	{
		//Sanity checks: check that this volume is FAT12 or FAT16!
		if (info->VolumeFormat != L"FAT12" && info->VolumeFormat != "FAT16")
			throw gcnew ArgumentException(S::_(L"The volume provided is not a FAT12 or FAT16 volume."));
	}

	Fat12Or16Api::!Fat12Or16Api()
	{
		if (Fat != NULL)
		{
			delete[] Fat;
			Fat = NULL;
		}
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
		//Return the root directory if we get cluster 0, name is blank and the parent is null
		if (cluster == 0 && String::IsNullOrEmpty(name) && parent == nullptr)
			return gcnew RootDirectory(this);
		return gcnew Directory(name, parent, cluster, this);
	}

	long long Fat12Or16Api::ClusterToOffset(unsigned cluster)
	{
		unsigned long long sector = BootSector->ReservedSectorCount +						//Reserved area
			BootSector->FatCount * BootSector->SectorsPerFat +								//FAT area
			(BootSector->RootDirectoryEntryCount * sizeof(::FatDirectoryEntry) /			//Root directory area
				BootSector->BytesPerSector) +
			(static_cast<unsigned long long>(cluster) - 2) * BootSector->SectorsPerCluster;
		return SectorToOffset(sector);
	}

	unsigned Fat12Or16Api::DirectoryToCluster(String^ path)
	{
		//The path must start with a backslash as it must be volume-relative.
		if (path->Length != 0)
		{
			if (path[0] != L'\\')
				throw gcnew ArgumentException(S::_(L"The path provided is not volume relative. "
					L"Volume relative paths must begin with a backslash."));
			path = path->Remove(0, 1);
		}

		//Chop the path into it's constituent directory components
		array<String^>^ components = path->Split(Path::DirectorySeparatorChar,
			Path::AltDirectorySeparatorChar);

		//Traverse the directories until we get the cluster we want.
		unsigned cluster = 0;
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

	bool Fat12Or16Api::IsFat12()
	{
		unsigned long long numberOfSectors = (BootSector->SectorCount16 == 0 ?
			BootSector->SectorCount32 : BootSector->SectorCount16);
		unsigned long long availableSectors = numberOfSectors - (
			BootSector->ReservedSectorCount +													//Reserved area
			BootSector->FatCount * BootSector->SectorsPerFat +									//FAT area
			(BootSector->RootDirectoryEntryCount * sizeof(::FatDirectoryEntry) /			//Root directory area
				BootSector->BytesPerSector)
		);
		unsigned long long numberOfClusters = availableSectors / BootSector->SectorsPerCluster;

		return numberOfClusters <= 0xFF0;
	}

	Fat12Or16Api::RootDirectory::RootDirectory(Fat12Or16Api^ api)
		: Api(api),
		  FatDirectoryBase(String::Empty, nullptr, 0)
	{
	}

	Fat12Or16Api::RootDirectory::!RootDirectory()
	{
		if (Directory != NULL)
		{
			delete[] Directory;
			Directory = NULL;
			DirectorySize = 0;
		}
	}

	void Fat12Or16Api::RootDirectory::ReadDirectory()
	{
		//Calculate the starting sector of the root directory
		unsigned long long startPos = Api->SectorToOffset(Api->BootSector->ReservedSectorCount +
			Api->BootSector->FatCount * Api->BootSector->SectorsPerFat);
		int directoryLength = Api->BootSector->RootDirectoryEntryCount *
			sizeof(::FatDirectoryEntry);

		array<Byte>^ buffer = gcnew array<Byte>(directoryLength);
		Api->VolumeStream->Seek(startPos, SeekOrigin::Begin);
		Api->VolumeStream->Read(buffer, 0, directoryLength);

		DirectorySize = Api->BootSector->RootDirectoryEntryCount;
		Directory = new ::FatDirectoryEntry[DirectorySize];
		Marshal::Copy(buffer, 0, static_cast<IntPtr>(Directory), directoryLength);

		ParseDirectory();
	}

	void Fat12Or16Api::RootDirectory::WriteDirectory()
	{
		//Calculate the starting sector of the root directory
		unsigned long long startPos = Api->SectorToOffset(Api->BootSector->ReservedSectorCount +
			Api->BootSector->FatCount * Api->BootSector->SectorsPerFat);
		int directoryLength = Api->BootSector->RootDirectoryEntryCount *
			sizeof(::FatDirectoryEntry);

		array<Byte>^ buffer = gcnew array<Byte>(directoryLength);
		Marshal::Copy(static_cast<IntPtr>(Directory), buffer, 0, directoryLength);
		Api->VolumeStream->Seek(startPos, SeekOrigin::Begin);
		Api->VolumeStream->Write(buffer, 0, directoryLength);
	}

	unsigned Fat12Or16Api::RootDirectory::GetStartCluster(::FatDirectoryEntry& directory)
	{
		if (directory.Short.Attributes == 0x0F)
			throw gcnew ArgumentException(S::_(L"The provided directory is a long file name."));
		return directory.Short.StartClusterLow;
	}

	Fat12Or16Api::Directory::Directory(String^ name, FatDirectoryBase^ parent, unsigned cluster,
		Fat12Or16Api^ api) : FatDirectory(name, parent, cluster, api)
	{
	}

	unsigned Fat12Or16Api::Directory::GetStartCluster(::FatDirectoryEntry& directory)
	{
		if (directory.Short.Attributes == 0x0F)
			throw gcnew ArgumentException(S::_(L"The provided directory is a long file name."));
		return directory.Short.StartClusterLow;
	}
}
}
