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

#pragma once

#include <vector>
#include "Fat.h"

using namespace System;

namespace Eraser {
namespace Util {
	ref class FatDirectory;
	public ref class FatApi abstract
	{
	public:
		/// Constructor.
		/// 
		/// \param[in] info   The volume to create the FAT API for. The volume handle
		///                   created has read access only.
		FatApi(VolumeInfo^ info);

		/// Constructor.
		/// 
		/// \param[in] info   The volume to create the FAT API for.
		/// \param[in] stream The stream to use to read/write to the disk.
		FatApi(VolumeInfo^ info, IO::Stream^ stream);

	public:
		/// Loads the File Allocation Table from disk.
		virtual void LoadFat() = 0;
		
		/// Loads the directory structure representing the directory with the given
		/// volume-relative path.
		FatDirectory^ LoadDirectory(String^ directory);

		/// Loads the directory structure at the given cluster.
		virtual FatDirectory^ LoadDirectory(unsigned cluster, String^ name) = 0;

	internal:
		/// Converts a sector-based address to a byte offset relative to the start
		/// of the volume.
		virtual unsigned long long SectorToOffset(unsigned long long sector);

		/// Converts a cluster-based address to a byte offset relative to the start
		/// of the volume.
		virtual long long ClusterToOffset(unsigned cluster) = 0;

		/// Converts a sector-based file size fo the actual size of the file in bytes.
		unsigned SectorSizeToSize(unsigned size);

		/// Converts a cluster-based file size fo the actual size of the file in bytes.
		unsigned ClusterSizeToSize(unsigned size);

		/// Verifies that the given cluster is allocated and in use.
		/// 
		/// \param[in] cluster The cluster to verify.
		virtual bool IsClusterAllocated(unsigned cluster) = 0;

		/// Gets the next cluster in the file.
		///
		/// \param[in] cluster The current cluster to check.
		/// \return            0xFFFFFFFF if the cluster given is the last one,
		///                    otherwise the next cluster in the file.
		virtual unsigned GetNextCluster(unsigned cluster) = 0;

		/// Gets the size of the file in bytes starting at the given cluster.
		/// Make sure that the given cluster is the first one, there is no way
		/// to verify it is indeed the first one and if later clusters are given
		/// the calculated size will be wrong.
		virtual unsigned FileSize(unsigned cluster) = 0;

		/// Gets the contents of the file starting at the given cluster.
		std::vector<char> GetFileContents(unsigned cluster);

		/// Set the contents of the file starting at the given cluster. The length
		/// of the contents must exactly match the length of the file.
		/// 
		/// \param[in] buffer  The data to write.
		/// \param[in] length  The amount of data to write.
		/// \param[in] cluster The cluster to begin writing to.
		void SetFileContents(const void* buffer, size_t length, unsigned cluster);

		/// \see SetFileContents
		void SetFileContents(const std::vector<char>& contents, unsigned cluster);

		/// Resolves a directory to the position on-disk
		///
		/// \param[in] path A volume-relative path to the directory.
		virtual unsigned DirectoryToCluster(String^ path) = 0;

	protected:
		IO::Stream^ VolumeStream;

		unsigned SectorSize;                 // Size of one sector, in bytes
		unsigned ClusterSize;                // Size of one cluster, in bytes
		FatBootSector* BootSector;
		char* Fat;
	};

	/// Represents the types of FAT directory entries.
	public enum class FatDirectoryEntryTypes
	{
		File,
		Directory
	};

	/// Represents a FAT directory entry.
	public ref class FatDirectoryEntry
	{
	public:
		/// Constructor.
		/// 
		/// \param[in] name    The name of the entry.
		/// \param[in] type    The type of this entry.
		/// \param[in] cluster The first cluster of the file.
		FatDirectoryEntry(String^ name, FatDirectoryEntryTypes type, unsigned cluster);

		/// Gets the name of the file or directory.
		property String^ Name
		{
			String^ get() { return name; }
		private:
			void set(String^ value) { name = value; }
		}

		/// Gets the full path to the file or directory.
		property String^ FullName
		{
			String^ get();
		}

		/// Gets the parent directory of this entry.
		property FatDirectory^ Parent
		{
			FatDirectory^ get() { return parent; }
		private:
			void set(FatDirectory^ value) { parent = value; }
		}

		/// Gets the type of this entry.
		property FatDirectoryEntryTypes Type
		{
			FatDirectoryEntryTypes get() { return type; }
		private:
			void set(FatDirectoryEntryTypes value) { type = value; }
		}

		/// Gets the first cluster of this entry.
		property unsigned Cluster
		{
			unsigned get() { return cluster; }
		private:
			void set(unsigned value) { cluster = value; }
		}

	private:
		String^ name;
		FatDirectory^ parent;
		FatDirectoryEntryTypes type;
		unsigned cluster;
	};

	/// Represents a FAT directory list.
	public ref class FatDirectory abstract : FatDirectoryEntry
	{
	public:
		/// Constructor.
		/// 
		/// \param[in] name    The name of the current directory.
		/// \param[in] cluster The cluster at which the directory list starts.
		/// \param[in] api     The FAT API object which is creating this object.
		FatDirectory(String^ name, unsigned cluster, FatApi^ api);

		/// Compacts the directory structure.
		void ClearDeletedEntries();

		/// The list of files and subfolders in this directory.
		property Collections::Generic::Dictionary<String^, FatDirectoryEntry^>^ Items
		{
			Collections::Generic::Dictionary<String^, FatDirectoryEntry^>^ get()
			{
				return Entries;
			}
		}

	protected:
		/// Gets the start cluster from the given directory entry.
		virtual unsigned GetStartCluster(::FatDirectory& directory) = 0;

	private:
		FatDirectoryFile Directory;
		Collections::Generic::Dictionary<String^, FatDirectoryEntry^>^ Entries;

		FatApi^ Api;
	};

	public ref class Fat32Api : FatApi
	{
	public:
		Fat32Api(VolumeInfo^ info);
		Fat32Api(VolumeInfo^ info, IO::Stream^ stream);

	public:
		virtual void LoadFat() override;
		virtual FatDirectory^ LoadDirectory(unsigned cluster, String^ name) override;

	internal:
		virtual long long ClusterToOffset(unsigned cluster) override;
		virtual bool IsClusterAllocated(unsigned cluster) override;
		virtual unsigned GetNextCluster(unsigned cluster) override;
		virtual unsigned FileSize(unsigned cluster) override;
		virtual unsigned DirectoryToCluster(String^ path) override;

	private:
		ref class Directory : FatDirectory
		{
		public:
			Directory(String^ name, unsigned cluster, Fat32Api^ api);

		protected:
			virtual unsigned GetStartCluster(::FatDirectory& directory) override;
		};
	};
}
}
