/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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

#pragma pack(push)
#pragma pack(1)

struct FatBootSector
{
	unsigned char    JumpInstruction[3];              // jmp to executable code
	unsigned char    OemName[8];                      // OEM name and version

	unsigned short   BytesPerSector;                  // bytes per sector
	unsigned char    SectorsPerCluster;               // sectors per cluster
	unsigned short   ReservedSectorCount;             // number of reserved sectors (starting at 0)
	unsigned char    FatCount;                        // number of file allocation tables
	unsigned short   RootDirectoryEntryCount;         // number of root-directory entries (directory size)
	unsigned short   SectorCount16;                   // total number of sectors (0 if partition > 32Mb)
	unsigned char    MediaDescriptor;                 // media descriptor
	unsigned short   SectorsPerFat;                   // number of sectors per FAT, only for FAT12/FAT16
	unsigned short   SectorsPerTrack;                 // number of sectors per track
	unsigned short   HeadCount;                       // number of read/write heads
	unsigned int     HiddenSectorCount;               // number of hidden sectors
	unsigned int     SectorCount32;                   // number of sectors if SectorCount16 is 0

	union
	{
		struct
		{
			unsigned char    DriveNumber;             // 0x80 if first hard drive
			unsigned char    Reserved;                // may be used for dirty drive checking under NT
			unsigned char    BootSignature;           // 0x29 if extended boot-signature record
			unsigned int     VolumeID;                // volume ID number
			unsigned char    VolumeLabel[11];         // volume label
			unsigned char    FileSystemType[8];       // file-system type ("FAT12   " or "FAT16   ")
			unsigned char    BootLoader[448];         // operating system boot loader code
			unsigned char    BpbSignature[2];         // must be 0x55 0xAA
		} ExtendedBiosParameterBlock;
		struct
		{
			unsigned int     SectorsPerFat;
			unsigned short   FatFlags;
			unsigned short   Version;                  // version
			unsigned int     RootDirectoryCluster;     // cluster number of root directory
			unsigned short   FsInformationSector;      // sector number of the FS Information Sector
			unsigned short   BootSectorCopySector;     // sector number for a copy of this boot sector
			unsigned char    Reserved1[12];
			unsigned char    DriveNumber;
			unsigned char    Reserved2;
			unsigned char    BootSignature;
			unsigned int     VolumeID;                 // volume ID number
			unsigned char    VolumeLabel[11];          // volume label
			unsigned char    FileSystemType[8];        // file system type ("FAT32   ")
			unsigned char    BootLoader[420];          // operating system boot loader code
			unsigned char    BootSectorSignature[2];   // must be 0xFF 0xAA;
		} Fat32ParameterBlock;
	};
};

struct FatFsInformationSector
{
	unsigned int    Signature;               // Fs Information Sector signature, must be 0x52 0x52 0x61 0x41 (RRaA), or 0x41615252
	unsigned char   Reserved[480];           // must be 0
	unsigned int    Signature2;              // Fs Information Sector signature, must be 0x72 0x72 0x41 0x61 (rrAa), or 0x61417272
	unsigned int    FreeClusters;            // number of free clusters on the drive, or -1 if unknown
	unsigned int    MostRecentlyAllocated;   // the number of the most recently allocated cluster
	unsigned char   Reserved2[14];           // must be 0
	unsigned short  Signature3;              // Fs Information Sector signature, must be 0x55 0xAA
};

/// Represents a short (8.3) directory entry.
struct Fat8Dot3DirectoryEntry
{
	/// Base name. If Name[0] is:
	/// 
	/// 0x00		Entry is available and no subsequent entry is in use
	/// 0x05		Initial character is actually 0xE5. 0x05 is a valid kanji lead
	///				byte, and is used for support for filenames written in kanji.
	/// 0x2E 		'Dot' entry; either '.' or '..'
	/// 0xE5		Entry has been previously erased and is available. File undelete
	///				utilities must replace this character with a regular character
	///				as part of the undeletion process.
	char    Name[8];

	/// File extension
	char    Extension[3];

	/// File or directory attributes
	unsigned char    Attributes;
	unsigned char    Reserved;

	/// File creation time, fine resolution, multiples of 10ms from 0 to 199.
	unsigned char    CreateTimeFine;

	/// File creation time, coarse resolution.
	/// The following bitmask encodes time information:
	///		15-11 	Hours (0-23)
	///		10-5 	Minutes (0-59)
	///		4-0 	Seconds/2 (0-29)
	unsigned short   CreateTimeCoarse;

	/// File creation date. The following bitmask encodes the information:
	///		15-9 	Year (0 = 1980, 127 = 2107)
	///		8-5 	Month (1 = January, 12 = December)
	///		4-0 	Day (1 - 31)
	unsigned short   CreateDate;

	/// Last access date.  The following bitmask encodes the information:
	///		15-9 	Year (0 = 1980, 127 = 2107)
	///		8-5 	Month (1 = January, 12 = December)
	///		4-0 	Day (1 - 31)
	unsigned short   LastAccessDate;

	union
	{
		// EA-Index (used by OS/2 and NT) in FAT12 and FAT16
		unsigned short EAIndex;

		// High 2 bytes of first cluster number in FAT32
		unsigned short StartClusterHigh;
	};

	/// File modification time. The following bitmask encodes time information:
	///		15-11 	Hours (0-23)
	///		10-5 	Minutes (0-59)
	///		4-0 	Seconds/2 (0-29)
	unsigned short   ModifyTime;

	/// File modification date. The following bitmask encodes the information:
	///		15-9 	Year (0 = 1980, 127 = 2107)
	///		8-5 	Month (1 = January, 12 = December)
	///		4-0 	Day (1 - 31)
	unsigned short   ModifyDate;

	/// The low 16 bits of the starting cluster for the file for FAT32, the starting
	/// cluster for the file in FAT12/16. Entries with the Volume Label flag,
	/// subdirectory ".." pointing to root, and empty files with size 0 should have
	/// first cluster 0.
	unsigned short   StartClusterLow;

	/// The size of the file.
	unsigned int     FileSize;
};

/// Represents a long file name directory entry.
struct FatLfnDirectoryEntry
{
	/// Sequence identifier. The last entry has bit 0x40 set.
	unsigned char      Sequence;

	/// Unicode characters of name.
	wchar_t   Name1[5];

	/// Attributes: always 0x0F.
	unsigned char      Attributes;
	unsigned char      Reserved;

	/// Checksum for DOS file name.
	unsigned char      Checksum;

	/// Second part of name.
	wchar_t   Name2[6];

	/// Reserved. Set to 0.
	unsigned short     Reserved2;

	/// Third part of name.
	wchar_t   Name3[2];
};

/// The collection of Fat Directory entries.
union FatDirectoryEntry
{
	Fat8Dot3DirectoryEntry Short;
	FatLfnDirectoryEntry LongFileName;
};

typedef FatDirectoryEntry* FatDirectory;

#pragma pack(pop)
