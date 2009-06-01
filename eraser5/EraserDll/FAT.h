// FAT.h
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
// 02111-1307, USA.

#ifndef FAT_H
#define FAT_H

///////////////////////////////////////////////////////////////////////////////
// structures and definitions for calling system interrupts using
// DeviceIoControl

#define VWIN32_DIOC_DOS_IOCTL       1
#define VWIN32_DIOC_DOS_INT25       2
#define VWIN32_DIOC_DOS_INT26       3
#define VWIN32_DIOC_DOS_DRIVEINFO   6

typedef struct _DIOC_REGISTERS
{
    E_UINT32 reg_EBX;
    E_UINT32 reg_EDX;
    E_UINT32 reg_ECX;
    E_UINT32 reg_EAX;
    E_UINT32 reg_EDI;
    E_UINT32 reg_ESI;
    E_UINT32 reg_Flags;
}
DIOC_REGISTERS, *PDIOC_REGISTERS;

#define CARRY_FLAG      1

#define LEVEL1_LOCK     1
#define LEVEL2_LOCK     2
#define LEVEL3_LOCK     3

// allow everything

#define LOCK_MAX_PERMISSION  0x001

// all MS-DOS data structures must be packed on a one-byte boundary.

#pragma pack(1)

typedef struct _DISKIO
{
    E_UINT32 diStartSector;        // sector number to start at
    E_UINT16 diSectors;            // number of sectors
    E_UINT32 diBuffer;             // address of buffer
}
DISKIO, *PDISKIO;


typedef struct _BOOTSECT
{
    E_UINT8    bsJump[3];          // jmp to executable code
    E_UINT8    bsOemName[8];       // OEM name and version

    // BPB (BIOS Parameter Block)
    E_UINT16   bsBytesPerSec;      // bytes per sector
    E_UINT8    bsSecPerClust;      // sectors per cluster
    E_UINT16   bsResSectors;       // number of reserved sectors (starting at 0)
    E_UINT8    bsFATs;             // number of file allocation tables
    E_UINT16   bsRootDirEnts;      // number of root-directory entries (directory size)
    E_UINT16   bsSectors;          // total number of sectors (0 if partition > 32Mb)
    E_UINT8    bsMedia;            // media descriptor
    E_UINT16   bsFATsecs;          // number of sectors per FAT
    E_UINT16   bsSecPerTrack;      // number of sectors per track
    E_UINT16   bsHeads;            // number of read/write heads
    E_UINT32   bsHiddenSectors;    // number of hidden sectors
    E_UINT32   bsHugeSectors;      // number of sectors if bsSectors is 0

    E_UINT8    bsDriveNumber;      // 80h if first hard drive
    E_UINT8    bsReserved;
    E_UINT8    bsBootSignature;    // 29h if extended boot-signature record
    E_UINT32   bsVolumeID;         // volume ID number
    E_UINT8    bsVolumeLabel[11];  // volume label
    E_UINT8    bsFileSysType[8];   // file-system type (FAT12 or FAT16)
}
BOOTSECTOR, *PBOOTSECTOR;

typedef struct _DIRENTRY
{
    E_UINT8    deName[8];          // base name
    E_UINT8    deExtension[3];     // extension
    E_UINT8    deAttributes;       // file or directory attributes
    E_UINT8    deReserved[6];
    E_UINT16   deLastAccessDate;   // *New Win95* - last access date
    E_UINT16   deEAhandle;         // *New FAT32* - high word of starting cluster
    E_UINT16   deCreateTime;       // creation or last modification time
    E_UINT16   deCreateDate;       // creation or last modification date
    E_UINT16   deStartCluster;     // starting cluster of the file or directory
    E_UINT32   deFileSize;         // size of the file in bytes
}
DIRENTRY, *PDIRENTRY;

typedef struct _LONGDIRENTRY
{
    E_UINT8   leSequence;         // sequence byte:1,2,3,..., last entry is or'ed with 40h
    wchar_t   leName[5];          // Unicode characters of name
    E_UINT8   leAttributes;       // Attributes: 0fh
    E_UINT8   leType;             // Long Entry Type: 0
    E_UINT8   leChksum;           // Checksum for matching short name alias
    wchar_t   leName2[6];         // More Unicode characters of name
    E_UINT16  leZero;             // reserved
    wchar_t   leName3[2];         // More Unicode characters of name
}
LONGDIRENTRY, *PLONGDIRENTRY;

// BPB for a FAT32 partition
typedef struct _A_BF_BPB
{
    E_UINT16    A_BF_BPB_BytesPerSector;
    E_UINT8     A_BF_BPB_SectorsPerCluster;
    E_UINT16    A_BF_BPB_ReservedSectors;
    E_UINT8     A_BF_BPB_NumberOfFATs;
    E_UINT16    A_BF_BPB_RootEntries;           // Ignored on FAT32 drives
    E_UINT16    A_BF_BPB_TotalSectors;
    E_UINT8     A_BF_BPB_MediaDescriptor;
    E_UINT16    A_BF_BPB_SectorsPerFAT;         // Always 0 on FAT32 BPB
    E_UINT16    A_BF_BPB_SectorsPerTrack;
    E_UINT16    A_BF_BPB_Heads;
    E_UINT16    A_BF_BPB_HiddenSectors;
    E_UINT16    A_BF_BPB_HiddenSectorsHigh;
    E_UINT16    A_BF_BPB_BigTotalSectors;
    E_UINT16    A_BF_BPB_BigTotalSectorsHigh;
    E_UINT16    A_BF_BPB_BigSectorsPerFat;
    E_UINT16    A_BF_BPB_BigSectorsPerFatHi;
    E_UINT16    A_BF_BPB_ExtFlags;
    E_UINT16    A_BF_BPB_FS_Version;
    E_UINT16    A_BF_BPB_RootDirStrtClus;
    E_UINT16    A_BF_BPB_RootDirStrtClusHi;
    E_UINT16    A_BF_BPB_FSInfoSec;
    E_UINT16    A_BF_BPB_BkUpBootSec;
    E_UINT16    A_BF_BPB_Reserved[6];
}
A_BF_BPB, PA_BF_BPB;

typedef struct _BOOTSECT32
{
    E_UINT8    bsJump[3];          // jmp instruction
    E_UINT8    bsOemName[8];       // OEM name and version

    // This portion is the FAT32 BPB
    A_BF_BPB  bpb;

    E_UINT8    bsDriveNumber;      // 80h if first hard drive
    E_UINT8    bsReserved;
    E_UINT8    bsBootSignature;    // 29h if extended boot-signature record
    E_UINT32   bsVolumeID;         // volume ID number
    E_UINT8    bsVolumeLabel[11];  // volume label
    E_UINT8    bsFileSysType[8];   // file-system type (FAT32)
}
BOOTSECTOR32, *PBOOTSECTOR32;

#pragma pack()

///////////////////////////////////////////////////////////////////////////////
// DISKERROR values

typedef E_INT32                 DISKERROR;
#define DISK_FAILURE            -1
#define DISK_SUCCESS            0
#define DISK_NOP                1
#define DISK_WRITE              2

///////////////////////////////////////////////////////////////////////////////
// type definitions for function pointers

struct wfeInfo;

typedef DISKERROR (*READSECTORS)(wfeInfo* /*pwInfo*/, E_UINT32 /*startSector*/, E_UINT16 /*sectorCount*/, E_PUINT8 /*pBuffer*/);
typedef DISKERROR (*WRITESECTORS)(wfeInfo* /*pwInfo*/, E_UINT32 /*startSector*/, E_UINT16 /*sectorCount*/, E_PUINT8 /*pBuffer*/);
typedef DISKERROR (*LOCKVOLUME)(wfeInfo* /*pwInfo*/, E_INT32 /*lockLevel*/, E_INT32 /*permissions*/);
typedef DISKERROR (*UNLOCKVOLUME)(wfeInfo* /*pwInfo*/);
typedef DISKERROR (*LOCKSTATE)(wfeInfo* /*pwInfo*/);
typedef DISKERROR (*LOCKDIRECTORY)(wfeInfo* /*pwInfo*/, LPCTSTR /*directory*/);
typedef DISKERROR (*UNLOCKDIRECTORY)(wfeInfo* /*pwInfo*/);
typedef E_UINT32 (*FATITEM)(E_PUINT8 /*buffer*/, E_UINT32 /*index*/);
typedef void (*FATINFO)(wfeInfo* /*pwInfo*/, E_PUINT8 /*bootSector*/);

typedef struct _DirectoryInformation
{
    _DirectoryInformation() :
        uCluster((E_UINT32)-1) {
    }

    E_UINT32    uCluster;
    CString     strPath;

} DIRINFO;

///////////////////////////////////////////////////////////////////////////////
// FATContext

typedef struct _FATContext
{
    _FATContext() {
        ZeroMemory(this, sizeof(_FATContext));
    }

    // pointers to file system specific functions
    READSECTORS     readSectors;
    WRITESECTORS    writeSectors;
    FATITEM         item;
    LOCKVOLUME      lockVolume;
    UNLOCKVOLUME    unlockVolume;
    LOCKSTATE       lockState;
    LOCKDIRECTORY   lockDirectory;
    UNLOCKDIRECTORY unlockDirectory;
    FATINFO         getInfo;

    // file system dependent values
    E_UINT32 uHighValue;
    E_UINT32 uLowValue;
    E_UINT32 uErrorValue;

} FATContext;

///////////////////////////////////////////////////////////////////////////////
// Wipe File Entries info structure passed to functions

// return values
#define WFE_FAILURE         0
#define WFE_SUCCESS         1
#define WFE_NOT_SUPPORTED   2
#define WFE_DISKMODIFIED    3
#define WFE_FATERROR        4
#define WFE_CHANGED         5

#include "Stack.h"

struct wfeInfo
{
    wfeInfo() {
        m_iVolume            = 0;
        m_uVolumeLocked      = 0;
        m_hVolume            = INVALID_HANDLE_VALUE;
        m_hDirectory         = INVALID_HANDLE_VALUE;
        m_uSectorsPerCluster = 0;
        m_uSectorSize        = 0;
        m_uSectorsPerFAT     = 0;
        m_uReservedSectors   = 0;
        m_uStartSector       = 0;
        m_uSectorsTotal      = 0;
        m_uClusterSize       = 0;
        m_uClustersTotal     = 0;
        m_uFATDirectorySize  = 0;
        m_uFATDirectoryStart = 0;
        m_pFAT               = 0;
    }

    ~wfeInfo() {
        if (m_hVolume != INVALID_HANDLE_VALUE) {
            CloseHandle(m_hVolume);
            m_hVolume = INVALID_HANDLE_VALUE;
        }
        if (isWindowsNT && m_hDirectory != INVALID_HANDLE_VALUE) {
            CloseHandle(m_hDirectory);
            m_hDirectory = INVALID_HANDLE_VALUE;
        }
        if (m_pFAT) {
            delete[] m_pFAT;
            m_pFAT = 0;
        }
    }

    // drive
    E_INT32   m_iVolume;                      // volume (1-based)
    E_UINT8   m_uVolumeLocked;                // lock state (NT)

    // handles
    HANDLE    m_hVolume;                      // handle to VWIN32 VxD (9x) or volume (NT)
    HANDLE    m_hDirectory;                   // handle to directory (NT)

    // sectors and FAT information
    E_UINT16  m_uSectorsPerCluster;           // sectors per cluster
    E_UINT16  m_uSectorSize;                  // sector size
    E_UINT16  m_uSectorsPerFAT;               // FAT síze (in sectors)

    E_UINT32  m_uReservedSectors;             // reserved sectors
    E_UINT32  m_uStartSector;                 // beginning of the data area
    E_UINT32  m_uSectorsTotal;                // sectors

    // clusters
    E_UINT32  m_uClusterSize;                 // cluster size
    E_UINT32  m_uClustersTotal;               // number of clusters

    // directories
    E_UINT16  m_uFATDirectorySize;            // size of FAT root directory
    E_UINT32  m_uFATDirectoryStart;           // the directory cluster (FAT32) or
                                              // start sector (FAT)
    CStack<DIRINFO> m_stDirectories;          // directories

    // FAT
    E_PUINT8  m_pFAT;                       // FAT buffer

    // filesystem specific data
    FATContext FS;
};

///////////////////////////////////////////////////////////////////////////////
// functions exported from the module

bool
getFATClusterAndSectorSize(LPCTSTR szDrive, E_UINT32& uCluster, E_UINT32& uSector);

E_UINT32
wipeFATFileEntries(CEraserContext *context, LPCTSTR szRetryMessage);

#endif
