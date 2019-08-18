/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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

#include "Fat.h"

using namespace System;

namespace Eraser {
namespace Util {
	ref class FatDirectoryBase;

	/// <summary>
	/// Represents an abstract API to interface with FAT file systems
	/// </summary>
	public ref class FatApi abstract
	{
	protected:
		/// <summary>
		/// Constructor.
		/// </summary>
		/// 
		/// <param name="info">The volume to create the FAT API for. The volume
		/// handle created has read access only.</param>
		FatApi(VolumeInfo^ info);

		/// <summary>
		/// Constructor.
		/// </summary>
		/// 
		/// <param name="stream">The stream to use to read/write to the disk.</param>
		FatApi(IO::Stream^ stream);

		/// Destructor.
		virtual ~FatApi() { this->!FatApi(); }

		/// Finalizer.
		!FatApi();

	public:
		/// <summary>
		/// Loads the File Allocation Table from disk.
		/// </summary>
		virtual void LoadFat() = 0;
		
		/// <summary>
		/// Helper function to loads the directory structure representing the
		/// directory with the given volume-relative path.
		/// </summary>
		FatDirectoryBase^ LoadDirectory(String^ directory);

		/// <summary>
		/// Loads the directory structure at the given cluster.
		/// </summary>
		virtual FatDirectoryBase^ LoadDirectory(unsigned cluster, String^ name,
			FatDirectoryBase^ parent) = 0;

	internal:
		/// <summary>
		/// Converts a sector-based address to a byte offset relative to the start
		/// of the volume.
		/// </summary>
		virtual unsigned long long SectorToOffset(unsigned long long sector);

		/// <summary>
		/// Converts a cluster-based address to a byte offset relative to the start
		/// of the volume.
		/// </summary>
		virtual long long ClusterToOffset(unsigned cluster) = 0;

		/// Converts a sector-based file size fo the actual size of the file in bytes.
		unsigned SectorSizeToSize(unsigned size);

		/// <summary>
		/// Converts a cluster-based file size fo the actual size of the file in bytes.
		/// </summary>
		unsigned ClusterSizeToSize(unsigned size);

		/// <summary>
		/// Verifies that the given cluster is allocated and in use.
		/// </summary>
		/// <param name="cluster">The cluster to verify.</param>
		virtual bool IsClusterAllocated(unsigned cluster) = 0;

		/// <summary>
		/// Gets the next cluster in the file.
		/// </summary>
		/// <param name="cluster">The current cluster to check.</param>
		/// <return>0xFFFFFFFF if the cluster given is the last one, otherwise
		/// the next cluster in the file.</return>
		virtual unsigned GetNextCluster(unsigned cluster) = 0;

		/// <summary>
		/// Gets the size of the file in bytes starting at the given cluster.
		/// Make sure that the given cluster is the first one, there is no way
		/// to verify it is indeed the first one and if later clusters are given
		/// the calculated size will be wrong.
		/// </summary>
		virtual unsigned FileSize(unsigned cluster) = 0;

		/// <summary>
		/// Gets the contents of the file starting at the given cluster.
		/// </summary>
		array<Byte>^ GetFileContents(unsigned cluster);

		/// <summary>
		/// Set the contents of the file starting at the given cluster. The length
		/// of the contents must exactly match the length of the file.
		/// </summary>
		/// <param name="buffer">The data to write.</param>
		/// <param name="cluster">The cluster to begin writing to.</param>
		void SetFileContents(array<Byte>^ buffer, unsigned cluster);

		/// <summary>
		/// Resolves a directory to the position on-disk
		/// </summary>
		/// <param name="path">A volume-relative path to the directory.</param>
		virtual unsigned DirectoryToCluster(String^ path) = 0;

	protected:
		/// <summary>
		/// The stream used to access the volume.
		/// </summary>
		property IO::Stream^ VolumeStream
		{
			IO::Stream^ get() { return volumeStream; }
		private:
			void set(IO::Stream^ value) { volumeStream = value; }
		}

		property FatBootSector* BootSector
		{
			FatBootSector* get() { return bootSector; }
		private:
			void set(FatBootSector* value) { bootSector = value; }
		}

		property char* Fat
		{
			char* get() { return fat; }
			void set(char* value) { fat = value; }
		}

	private:
		IO::Stream^ volumeStream;
		FatBootSector* bootSector;
		char* fat;
	};

	/// <summary>
	/// Represents the types of FAT directory entries.
	/// </summary>
	public enum class FatDirectoryEntryType
	{
		File,
		Directory
	};

	/// <summary>
	/// Represents a FAT directory entry.
	/// </summary>
	public ref class FatDirectoryEntry
	{
	public:
		virtual ~FatDirectoryEntry() {}

	public:
		/// <summary>
		/// Gets the name of the file or directory.
		/// </summary>
		property String^ Name
		{
			String^ get() { return name; }
		private:
			void set(String^ value) { name = value; }
		}

		/// <summary>
		/// Gets the full path to the file or directory.
		/// </summary>
		property String^ FullName
		{
			String^ get();
		}

		/// <summary>
		/// Gets the parent directory of this entry.
		/// </summary>
		property FatDirectoryBase^ Parent
		{
			FatDirectoryBase^ get() { return parent; }
		private:
			void set(FatDirectoryBase^ value) { parent = value; }
		}

		/// <summary>
		/// Gets the type of this entry.
		/// </summary>
		property FatDirectoryEntryType EntryType
		{
			FatDirectoryEntryType get() { return type; }
		private:
			void set(FatDirectoryEntryType value) { type = value; }
		}

		/// <summary>
		/// Gets the first cluster of this entry.
		/// </summary>
		property unsigned Cluster
		{
			unsigned get() { return cluster; }
		private:
			void set(unsigned value) { cluster = value; }
		}

	internal:
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name">The name of the entry.</param>
		/// <param name="parent">The parent directory containing this file.</param>
		/// <param name="type">The type of this entry.</param>
		/// <param name="cluster">The first cluster of the file.</param>
		FatDirectoryEntry(String^ name, FatDirectoryBase^ parent, FatDirectoryEntryType type,
			unsigned cluster);

	private:
		String^ name;
		FatDirectoryBase^ parent;
		FatDirectoryEntryType type;
		unsigned cluster;
	};

	/// <summary>
	/// Represents an abstract FAT directory (can also represent the root directory of
	/// FAT12 and FAT16 volumes.)
	/// </summary>
	public ref class FatDirectoryBase abstract : FatDirectoryEntry
	{
	protected:
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name">The name of the current directory.</param>
		/// <param name="parent">The parent directory containing this directory.</param>
		/// <param name="cluster">The cluster at which the directory list starts.</param>
		FatDirectoryBase(String^ name, FatDirectoryBase^ parent, unsigned cluster);

		virtual ~FatDirectoryBase() {}

	public:
		/// <summary>
		/// Compacts the directory structure, updating the structure on-disk as well.
		/// </summary>
		void ClearDeletedEntries();

		/// <summary>
		/// The list of files and subfolders in this directory.
		/// </summary>
		property Collections::Generic::Dictionary<String^, FatDirectoryEntry^>^ Items
		{
			Collections::Generic::Dictionary<String^, FatDirectoryEntry^>^ get()
			{
				return Entries;
			}
		}

	protected:
		/// <summary>
		/// Reads the directory structures from disk.
		/// </summary>
		/// <remarks>This function must set the <see cref="Directory" /> instance
		/// as well as the <see cref="DirectorySize" /> fields. Furthermore, call
		/// the <see cref="ParseDirectory" /> function to initialise the directory
		/// entries on-disk.</remarks>
		virtual void ReadDirectory() = 0;

		/// <summary>
		/// Writes the directory to disk.
		/// </summary>
		virtual void WriteDirectory() = 0;

		/// <summary>
		/// This function reads the raw directory structures in <see cref="Directory" />
		/// and sets the <see cref="Entries" /> field for easier access to the directory
		/// entries.
		/// </summary>
		void ParseDirectory();

		/// <summary>
		/// Gets the start cluster from the given directory entry.
		/// </summary>
		virtual unsigned GetStartCluster(::FatDirectoryEntry& directory) = 0;

	protected:
		/// <summary>
		/// A pointer to the directory structure.
		/// </summary>
		property ::FatDirectory Directory
		{
			::FatDirectory get() { return directory; }
			void set(::FatDirectory value) { directory = value; }
		}

		/// <summary>
		/// The number of entries in the directory
		/// </summary>
		property size_t DirectorySize
		{
			size_t get() { return directorySize; }
			void set(size_t value) { directorySize = value; }
		}

	private:
		/// <summary>
		/// The list of parsed entries in the folder.
		/// </summary>
		Collections::Generic::Dictionary<String^, FatDirectoryEntry^>^ Entries;

		size_t directorySize;
		::FatDirectory directory;
	};

	/// <summary>
	/// Represents a FAT directory file.
	/// </summary>
	public ref class FatDirectory abstract : FatDirectoryBase
	{
	protected:
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name">The name of the current directory.</param>
		/// <param name="parent">The parent directory containing this directory.</param>
		/// <param name="cluster">The cluster at which the directory list starts.</param>
		/// <param name="api">The FAT API object which is creating this object.</param>
		FatDirectory(String^ name, FatDirectoryBase^ parent, unsigned cluster, FatApi^ api);

		/// Destructor.
		virtual ~FatDirectory() { this->!FatDirectory(); }

		/// Finalizer.
		!FatDirectory();

		virtual void ReadDirectory() override;
		virtual void WriteDirectory() override;

	private:
		FatApi^ Api;
	};

	public ref class Fat12Or16Api abstract : FatApi
	{
	protected:
		Fat12Or16Api(VolumeInfo^ info);
		Fat12Or16Api(VolumeInfo^ info, IO::Stream^ stream);

		virtual ~Fat12Or16Api() { this->!Fat12Or16Api(); }
		!Fat12Or16Api();

	public:
		virtual void LoadFat() override;
		virtual FatDirectoryBase^ LoadDirectory(unsigned cluster, String^ name,
			FatDirectoryBase^ parent) override;

	internal:
		virtual long long ClusterToOffset(unsigned cluster) override;
		virtual unsigned DirectoryToCluster(String^ path) override;

	protected:
		ref class RootDirectory : FatDirectoryBase
		{
		public:
			RootDirectory(Fat12Or16Api^ api);

			virtual ~RootDirectory() { this->!RootDirectory(); }
			!RootDirectory();

		protected:
			virtual void ReadDirectory() override;
			virtual void WriteDirectory() override;
			virtual unsigned GetStartCluster(::FatDirectoryEntry& directory) override;

		private:
			Fat12Or16Api^ Api;
		};

		ref class Directory : FatDirectory
		{
		public:
			Directory(String^ name, FatDirectoryBase^ parent, unsigned cluster, Fat12Or16Api^ api);

		protected:
			virtual unsigned GetStartCluster(::FatDirectoryEntry& directory) override;
		};

	protected:
		bool IsFat12();
	};

	public ref class Fat12Api : Fat12Or16Api
	{
	public:
		Fat12Api(VolumeInfo^ info);
		Fat12Api(VolumeInfo^ info, IO::Stream^ stream);

	internal:
		virtual bool IsClusterAllocated(unsigned cluster) override;
		virtual unsigned GetNextCluster(unsigned cluster) override;
		virtual unsigned FileSize(unsigned cluster) override;

	private:
		/// <summary>
		/// Retrieves the FAT value for the given cluster.
		/// </summary>
		unsigned GetFatValue(unsigned cluster);
	};

	public ref class Fat16Api : Fat12Or16Api
	{
	public:
		Fat16Api(VolumeInfo^ info);
		Fat16Api(VolumeInfo^ info, IO::Stream^ stream);

	internal:
		virtual bool IsClusterAllocated(unsigned cluster) override;
		virtual unsigned GetNextCluster(unsigned cluster) override;
		virtual unsigned FileSize(unsigned cluster) override;
	};

	public ref class Fat32Api : FatApi
	{
	public:
		Fat32Api(VolumeInfo^ info);
		Fat32Api(VolumeInfo^ info, IO::Stream^ stream);

		virtual ~Fat32Api() { this->!Fat32Api(); }
		!Fat32Api();

	public:
		virtual void LoadFat() override;
		virtual FatDirectoryBase^ LoadDirectory(unsigned cluster, String^ name,
			FatDirectoryBase^ parent) override;

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
			Directory(String^ name, FatDirectoryBase^ parent, unsigned cluster, Fat32Api^ api);

		protected:
			virtual unsigned GetStartCluster(::FatDirectoryEntry& directory) override;
		};
	};
}
}
