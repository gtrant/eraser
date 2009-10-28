// FAT.cpp
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

#include "stdafx.h"
#include <winioctl.h>
#include "Resource.h"
#include "EraserDll.h"
#include "Common.h"
#include "FreeSpace.h"
#include "FAT.h"

// path to VWIN32.vxd
//
const LPCTSTR szVWIN32 = _T("\\\\.\\vwin32");

// helpful definitions
//
#define isClusterNotEnd(pwInfo, uIndex) \
    ((uIndex) <= (pwInfo)->FS.uLowValue)
#define isClusterError(pwInfo, uIndex) \
    (((uIndex) > (pwInfo)->FS.uLowValue) && ((uIndex) <= (pwInfo)->FS.uErrorValue))
#define clusterToSector(pwInfo, uCluster) \
    ((((uCluster) - 0x2) * (pwInfo)->m_uSectorsPerCluster) + (pwInfo)->m_uStartSector)
#define isEntryLocked(pwInfo) \
    (((pwInfo)->m_uVolumeLocked > 0) || ((pwInfo)->m_hDirectory != INVALID_HANDLE_VALUE))

#define MAX_SECTOR_BUFFER   2048

#define FAT12_MAX_CLUSTERS  0xFF0
#define FAT12_MAX_ERROR     0xFF7
#define FAT12_MAX_VALUE     0xFFF

#define FAT16_MAX_CLUSTERS  0xFFF0
#define FAT16_MAX_ERROR     0xFFF7
#define FAT16_MAX_VALUE     0xFFFF

#define FAT32_MAX_CLUSTERS  0x0FFFFFF0
#define FAT32_MAX_ERROR     0x0FFFFFF7
#define FAT32_MAX_VALUE     0x0FFFFFFF


// NT specific functions
//
static DISKERROR
readSectors_NT(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    ULARGE_INTEGER uiStart;
    E_UINT32 uSize = uSectors * pwInfo->m_uSectorSize;
    E_UINT32 uResult = 0;

    uiStart.QuadPart = UInt32x32To64(uStartSector, (E_UINT32)pwInfo->m_uSectorSize);

    uResult = SetFilePointer(pwInfo->m_hVolume, uiStart.LowPart, (E_PINT32)&uiStart.HighPart, FILE_BEGIN);

    if (uResult == (E_UINT32)-1 && GetLastError() != NO_ERROR) {
        return DISK_FAILURE;
    }

    if (ReadFile(pwInfo->m_hVolume, pBuffer, uSize, &uResult, NULL) && (uSize == uResult)) {
        return DISK_SUCCESS;
    } else {
        return DISK_FAILURE;
    }
}

static DISKERROR
writeSectors_NT(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    ULARGE_INTEGER uiStart;
    E_UINT32 uSize = uSectors * pwInfo->m_uSectorSize;
    E_UINT32 uResult = 0;

    uiStart.QuadPart = UInt32x32To64(uStartSector, (E_UINT32)pwInfo->m_uSectorSize);

    uResult = SetFilePointer(pwInfo->m_hVolume, uiStart.LowPart, (E_PINT32)&uiStart.HighPart, FILE_BEGIN);

    if (uResult == (E_UINT32)-1 && GetLastError() != NO_ERROR) {
        return DISK_FAILURE;
    }

    if (WriteFile(pwInfo->m_hVolume, pBuffer, uSize, &uResult, NULL) && (uSize == uResult)) {
        return DISK_SUCCESS;
    } else {
        return DISK_FAILURE;
    }
}

static DISKERROR
lockVolume_NT(wfeInfo *pwInfo, E_INT32 /*lockLevel*/, E_INT32 /*permissions*/)
{
    if (pwInfo->m_uVolumeLocked > 0) {
        pwInfo->m_uVolumeLocked++;
        return DISK_SUCCESS;
    }

    E_UINT32 uResult = 0;

    if (DeviceIoControl(pwInfo->m_hVolume, FSCTL_LOCK_VOLUME,
            NULL, 0, NULL, 0, &uResult, NULL)) {
        pwInfo->m_uVolumeLocked = 1;
    }

    // return DISK_SUCCESS even if we couldn't lock
    // the volume - we'll just attempt to lock the
    // directory instead

    return DISK_SUCCESS;
}

static DISKERROR
unlockVolume_NT(wfeInfo *pwInfo)
{
    if (pwInfo->m_uVolumeLocked > 1) {
        pwInfo->m_uVolumeLocked--;
        return DISK_SUCCESS;
    }

    if (pwInfo->m_uVolumeLocked == 1) {
        E_UINT32 uResult = 0;

        if (DeviceIoControl(pwInfo->m_hVolume, FSCTL_UNLOCK_VOLUME,
                NULL, 0, NULL, 0, &uResult, NULL)) {
            pwInfo->m_uVolumeLocked = 0;
            return DISK_SUCCESS;
        }
    }

    return DISK_FAILURE;
}

static DISKERROR
lockState_NT(wfeInfo*)
{
    // always return DISK_NOP for compatibility
    return DISK_NOP;
}

static DISKERROR
unlockDirectory_NT(wfeInfo *pwInfo)
{
    if (pwInfo->m_hDirectory != INVALID_HANDLE_VALUE) {
        CloseHandle(pwInfo->m_hDirectory);
        pwInfo->m_hDirectory = INVALID_HANDLE_VALUE;
        return DISK_SUCCESS;
    } else {
        return DISK_FAILURE;
    }
}

static DISKERROR
lockDirectory_NT(wfeInfo *pwInfo, LPCTSTR szDirectory)
{
    if (pwInfo->m_hDirectory != INVALID_HANDLE_VALUE) {
        unlockDirectory_NT(pwInfo);
    }

    // open without sharing allowed
    pwInfo->m_hDirectory = CreateFile(szDirectory, GENERIC_READ | GENERIC_WRITE,
                            0, NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);

    if (pwInfo->m_hDirectory == INVALID_HANDLE_VALUE) {
        return DISK_FAILURE;
    } else {
        return DISK_SUCCESS;
    }
}


// FAT32 specific functions
//
static DISKERROR
readSectors_FAT32(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    // mov  si, 0h             ; SI = 0 for read
    //
    // mov  cx, -1             ; cx must be -1
    // mov  dx, seg Buffer
    // mov  ds, dx
    // mov  bx, offset Buffer  ; Pointers to a DISKIO structure
    // mov  dl, DriveNum       ; The 1-based drive number
    //                         ; (0 = default; 1 = A, 2 = B, and so on).
    // mov  ax, 7305h          ; Ext_ABSDiskReadWrite
    // int 21h
    //
    // jc  error_handler       ; carry set means error

    DIOC_REGISTERS  reg;
    DISKIO          di;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX      = 0x7305;
    reg.reg_EBX      = (E_UINT32)&di;
    reg.reg_ECX      = 0xFFFF;
    reg.reg_EDX      = pwInfo->m_iVolume;
    reg.reg_ESI      = 0;
    reg.reg_Flags    = 1;

    // control block
    di.diStartSector = uStartSector;
    di.diSectors     = uSectors;
    di.diBuffer      = (E_UINT32)pBuffer;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_DRIVEINFO,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
writeSectors_FAT32(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    // mov  si, 6001h          ; write normal file data.
    //
    // mov  cx, -1             ; cx must be -1
    // mov  dx, seg Buffer
    // mov  ds, dx
    // mov  bx, offset Buffer  ; Pointers to a DISKIO structure
    // mov  dl, DriveNum       ; The 1-based drive number
    //                         ; (0 = default; 1 = A, 2 = B, and so on).
    // mov  ax, 7305h          ; Ext_ABSDiskReadWrite
    // int 21h
    //
    // jc  error_handler       ; carry set means error

    BOOL            bResult;
    E_UINT32        cb;
    DIOC_REGISTERS  reg;
    DISKIO          di;

    reg.reg_EAX         = 0x7305;
    reg.reg_EBX         = (E_UINT32)&di;
    reg.reg_ECX         = 0xFFFF;
    reg.reg_EDX         = pwInfo->m_iVolume;
    reg.reg_ESI         = 0x6001;
    reg.reg_Flags       = 1;

    // control block
    di.diStartSector    = uStartSector;
    di.diSectors        = uSectors;
    di.diBuffer         = (E_UINT32)pBuffer;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_DRIVEINFO,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
lockState_FAT32(wfeInfo *pwInfo)
{
    // mov ax, 440Dh        ; generic IOCTL
    // mov bl, DriveNum     ; Drive to poll. 1-based.
    // mov ch, DeviceCat    ; 48h for FAT32 drive
    // mov cl, 6Ch          ; Get Lock Flag State
    // int 21h
    //
    // jc error_handler
    // mov [AccessFlag], ax  ; state of access flag

    DIOC_REGISTERS  reg;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX     = 0x440D;
    reg.reg_ECX     = 0x486c;
    reg.reg_EBX     = pwInfo->m_iVolume;
    reg.reg_Flags   = 1;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_IOCTL,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return ((reg.reg_EAX == 0) ? DISK_NOP : DISK_WRITE);
}

static DISKERROR
lockVolume_FAT32(wfeInfo *pwInfo, E_INT32 iLockLevel, E_INT32 iPermissions)
{
    // mov ax, 440Dh        ; generic IOCTL
    // mov bh, LockLevel    ; Level of the lock. 0, 1, 2 or 3
    // mov bl, DriveNum     ; Drive to lock. 0 for default, 1 for A,...
    // mov ch, DeviceCat    ; 48h for FAT32 drive
    // mov cl, 4Ah          ; Lock Logical Volume
    // mov dx, Permissions  ; Bit  Meaning
    //                        0    0 = Write operations are failed
    //                        0    1 = Write operations are allowed
    //                        1    0 = New file mapping are allowed
    //                        1    1 = New file mapping are failed
    //                        2    1 = The volume is locked for formatting
    //                                 (specified when a level 0 lock is
    //                                  obtained for the second time).
    // int 21h
    //
    // jc error_handler

    DIOC_REGISTERS  reg;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX     = 0x440D;
    reg.reg_ECX     = 0x484A;
    reg.reg_EBX     = pwInfo->m_iVolume | (iLockLevel << 8);
    reg.reg_EDX     = iPermissions;
    reg.reg_Flags   = 1;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_IOCTL,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
unlockVolume_FAT32(wfeInfo *pwInfo)
{
    // mov ax, 440Dh      ; generic IOCTL
    // mov bl, DriveNum   ; Drive to lock. 0 for default, 1 for A,...
    // mov ch, DeviceCat  ; 48h for FAT32 drive
    // mov cl, 6Ah        ; Unlock Logical Volume
    // int 21h
    //
    // jc error_handler

    DIOC_REGISTERS  reg;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX     = 0x440D;
    reg.reg_ECX     = 0x486A;
    reg.reg_EBX     = pwInfo->m_iVolume;
    reg.reg_Flags   = 1;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_IOCTL,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static inline E_UINT32
item_FAT32(E_PUINT8 pBuffer, E_UINT32 uIndex)
{
    return (0x0FFFFFFF & *((E_UINT32*)&pBuffer[uIndex * 32 / 8]));
}

static inline void
getInfo_FAT32(wfeInfo *pwInfo, E_PUINT8 pBuffer)
{
    try {
        BOOTSECTOR32 *bs32 = (BOOTSECTOR32*)pBuffer;

        pwInfo->m_uSectorsPerCluster = bs32->bpb.A_BF_BPB_SectorsPerCluster;
        pwInfo->m_uSectorSize        = bs32->bpb.A_BF_BPB_BytesPerSector;
        pwInfo->m_uSectorsPerFAT     = (E_UINT16)((bs32->bpb.A_BF_BPB_BigSectorsPerFatHi << 16) +
                                                 bs32->bpb.A_BF_BPB_BigSectorsPerFat);
        pwInfo->m_uReservedSectors   = bs32->bpb.A_BF_BPB_ReservedSectors;
        pwInfo->m_uSectorsTotal      = bs32->bpb.A_BF_BPB_TotalSectors;
        if (pwInfo->m_uSectorsTotal == 0) {
            pwInfo->m_uSectorsTotal  = (bs32->bpb.A_BF_BPB_BigTotalSectorsHigh << 16) +
                                       bs32->bpb.A_BF_BPB_BigTotalSectors;
        }
        pwInfo->m_uFATDirectoryStart = (bs32->bpb.A_BF_BPB_RootDirStrtClusHi << 16) +
                                       bs32->bpb.A_BF_BPB_RootDirStrtClus;
        pwInfo->m_uStartSector       = pwInfo->m_uReservedSectors +
                                       pwInfo->m_uSectorsPerFAT * bs32->bpb.A_BF_BPB_NumberOfFATs;
    } catch (...) {
        ASSERT(0);
    }
}


// FAT specific functions
//
static DISKERROR
readSectors_FAT(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    // int 25h                          ; Absolute disk read
    //
    // al = logical drive number        ; 0 = A:, 1 = B:, ...
    // bx = pointer to data buffer
    //      pointer to control block
    // cx = number of sectors to
    //      read, -1 if bx is control
    //      block

    DIOC_REGISTERS  reg;
    DISKIO          di;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX      = pwInfo->m_iVolume - 1;
    reg.reg_EBX      = (E_UINT32)&di;
    reg.reg_ECX      = 0xFFFF;
    reg.reg_Flags    = 1;

    // control block
    di.diStartSector = uStartSector;
    di.diSectors     = uSectors;
    di.diBuffer      = (E_UINT32)pBuffer;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_INT25,
              &reg, sizeof(reg), &reg, sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
writeSectors_FAT(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    // int 26h                          ; Absolute disk write
    //
    // al = logical drive number        ; 0 = A:, 1 = B:, ...
    // bx = pointer to data buffer
    //      pointer to control block
    // cx = number of sectors to
    //      read, -1 if bx is control
    //      block

    BOOL            bResult;
    E_UINT32        cb;
    DIOC_REGISTERS  reg;
    DISKIO          di;

    reg.reg_EAX         = pwInfo->m_iVolume - 1;
    reg.reg_EBX         = (E_UINT32)&di;
    reg.reg_ECX         = 0xFFFF;
    reg.reg_Flags       = 1;

    // control block

    di.diStartSector    = uStartSector;
    di.diSectors        = uSectors;
    di.diBuffer         = (E_UINT32)pBuffer;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_INT26,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
lockState_FAT(wfeInfo *pwInfo)
{
    // mov ax, 440Dh        ; generic IOCTL
    // mov bl, DriveNum     ; Drive to poll. 1-based.
    // mov ch, DeviceCat    ; 08h for FAT drive
    // mov cl, 6Ch          ; Get Lock Flag State
    // int 21h
    //
    // jc error_handler
    // mov [AccessFlag], ax  ; state of access flag

    DIOC_REGISTERS  reg;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX     = 0x440D;
    reg.reg_ECX     = 0x086c;
    reg.reg_EBX     = pwInfo->m_iVolume;
    reg.reg_Flags   = 1;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_IOCTL,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return ((reg.reg_EAX == 0) ? DISK_NOP : DISK_WRITE);
}

static DISKERROR
lockVolume_FAT(wfeInfo *pwInfo, E_INT32 iLockLevel, E_INT32 iPermissions)
{
    // mov ax, 440Dh        ; generic IOCTL
    // mov bh, LockLevel    ; Level of the lock. 0, 1, 2 or 3
    // mov bl, DriveNum     ; Drive to lock. 0 for default, 1 for A,...
    // mov ch, DeviceCat    ; 48h for FAT32 drive, 08h for FAT drive
    // mov cl, 4Ah          ; Lock Logical Volume
    // mov dx, Permissions  ; Bit  Meaning
    //                        0    0 = Write operations are failed
    //                        0    1 = Write operations are allowed
    //                        1    0 = New file mapping are allowed
    //                        1    1 = New file mapping are failed
    //                        2    1 = The volume is locked for formatting
    //                                 (specified when a level 0 lock is
    //                                  obtained for the second time).
    // int 21h
    //
    // jc error_handler

    DIOC_REGISTERS  reg;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX     = 0x440D;
    reg.reg_ECX     = 0x084A;
    reg.reg_EBX     = pwInfo->m_iVolume | (iLockLevel << 8);
    reg.reg_EDX     = iPermissions;
    reg.reg_Flags   = 1;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_IOCTL,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
unlockVolume_FAT(wfeInfo *pwInfo)
{
    // mov ax, 440Dh      ; generic IOCTL
    // mov bl, DriveNum   ; Drive to lock. 0 for default, 1 for A,...
    // mov ch, DeviceCat  ; 48h for FAT32 drive, 08h for FAT drive
    // mov cl, 6Ah        ; Unlock Logical Volume
    // int 21h
    //
    // jc error_handler

    DIOC_REGISTERS  reg;
    BOOL            bResult;
    E_UINT32        cb;

    reg.reg_EAX     = 0x440D;
    reg.reg_ECX     = 0x086A;
    reg.reg_EBX     = pwInfo->m_iVolume;
    reg.reg_Flags   = 1;

    bResult = DeviceIoControl(pwInfo->m_hVolume, VWIN32_DIOC_DOS_IOCTL,
                              &reg, sizeof(reg), &reg,
                              sizeof(reg), &cb, 0);

    if (!bResult || bitSet(reg.reg_Flags, CARRY_FLAG)) {
        return DISK_FAILURE;
    }

    return DISK_SUCCESS;
}

static DISKERROR
lockDirectory_FAT(wfeInfo*, LPCTSTR)
{
    // not supported
    return DISK_FAILURE;
}

static DISKERROR
unlockDirectory_FAT(wfeInfo*)
{
    // not supported
    return DISK_FAILURE;
}

static inline E_UINT32
item_FAT16(E_PUINT8 pBuffer, E_UINT32 uIndex)
{
    return ((E_UINT32)*((E_PUINT16)&pBuffer[uIndex * 16 / 8]));
}

static inline E_UINT32
item_FAT12(E_PUINT8 pBuffer, E_UINT32 uIndex)
{
    E_UINT32  uValue;
    E_PUINT16 pwFAT = (E_PUINT16)(&pBuffer[uIndex * 12 / 8]);

    if (uIndex & 1) {
        uValue = (E_UINT32)(*pwFAT >> 4);
    } else {
        uValue = (E_UINT32)(0x0FFF & *pwFAT);
    }

    return uValue;
}

static inline void
getInfo_FAT(wfeInfo *pwInfo, E_PUINT8 pBuffer)
{
    try {
        BOOTSECTOR *bs = (BOOTSECTOR*)pBuffer;

        pwInfo->m_uSectorsPerCluster = bs->bsSecPerClust;
        pwInfo->m_uSectorSize        = bs->bsBytesPerSec;
        pwInfo->m_uSectorsPerFAT     = bs->bsFATsecs;
        pwInfo->m_uReservedSectors   = bs->bsResSectors;
        pwInfo->m_uSectorsTotal      = bs->bsSectors;
        if (pwInfo->m_uSectorsTotal == 0) {
            pwInfo->m_uSectorsTotal  = bs->bsHugeSectors;
        }
        pwInfo->m_uFATDirectorySize  = (E_UINT16)((32 * bs->bsRootDirEnts +
                                                  pwInfo->m_uSectorSize - 1) / pwInfo->m_uSectorSize);
        pwInfo->m_uFATDirectoryStart = pwInfo->m_uReservedSectors +
                                       pwInfo->m_uSectorsPerFAT * bs->bsFATs;
        pwInfo->m_uStartSector       = pwInfo->m_uFATDirectoryStart + pwInfo->m_uFATDirectorySize;
    } catch (...) {
        ASSERT(0);
    }
}

///////////////////////////////////////////////////////////////////////////////
// common read/write functions

static inline E_UINT32
readSectors(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    try {
        if (pwInfo->FS.readSectors(pwInfo, uStartSector, uSectors, pBuffer) == DISK_SUCCESS) {
            return WFE_SUCCESS;
        }
    } catch (...) {
        ASSERT(0);
    }

    return WFE_FAILURE;
}

static E_UINT32
writeSectors(wfeInfo *pwInfo, E_UINT32 uStartSector, E_UINT16 uSectors, E_PUINT8 pBuffer)
{
    try {
        E_UINT32 uReturn = WFE_FAILURE;

        if (pwInfo->FS.lockVolume(pwInfo, LEVEL2_LOCK, 0) == DISK_SUCCESS) {
            if (pwInfo->FS.lockVolume(pwInfo, LEVEL3_LOCK, 0) == DISK_SUCCESS) {
                if (pwInfo->FS.lockState(pwInfo) != DISK_NOP) {
                    uReturn = WFE_DISKMODIFIED;
                } else {
                    if (pwInfo->FS.writeSectors(pwInfo, uStartSector, uSectors, pBuffer) == DISK_SUCCESS) {
                        uReturn = WFE_SUCCESS;
                    }
                }
                pwInfo->FS.unlockVolume(pwInfo);
            }
            pwInfo->FS.unlockVolume(pwInfo);
        }

        return uReturn;
    } catch (...) {
        ASSERT(0);
    }

    return WFE_FAILURE;
}


///////////////////////////////////////////////////////////////////////////////
// common FAT functions

static inline BOOL
verifyFAT(wfeInfo *pwInfo)
{
    E_UINT32 uClusterValue;
    E_UINT32 uIndex;

    try {
        // two is the first valid cluster
        for (uIndex = 2; uIndex < pwInfo->m_uClustersTotal; uIndex++) {
            uClusterValue = pwInfo->FS.item(pwInfo->m_pFAT, uIndex);

            // check for valid cluster number, if it is not between
            // uFSLowValue and uFSHighValue, it must be smaller than
            // the number of total clusters

            if (!((uClusterValue >= pwInfo->FS.uLowValue &&
                   uClusterValue <= pwInfo->FS.uHighValue) ||
                  (uClusterValue <= pwInfo->m_uClustersTotal))) {
                return FALSE;
            }
        }

        return TRUE;
    } catch (...) {
        ASSERT(0);
    }

    return FALSE;
}

static E_UINT32
readFAT(wfeInfo *pwInfo)
{
    try {
        E_UINT32 uReturn     = WFE_FAILURE;
        E_UINT32 uBufferSize = pwInfo->m_uSectorSize *
                               pwInfo->m_uSectorsPerFAT;

        pwInfo->m_pFAT = new E_UINT8[uBufferSize];
        ZeroMemory(pwInfo->m_pFAT, uBufferSize);

        uReturn = readSectors(pwInfo, pwInfo->m_uReservedSectors,
                              pwInfo->m_uSectorsPerFAT,
                              pwInfo->m_pFAT);

        if (uReturn == WFE_SUCCESS) {
            if (!verifyFAT(pwInfo)) {
                uReturn = WFE_FATERROR;
            }
        }

        if (uReturn != WFE_SUCCESS) {
            delete[] pwInfo->m_pFAT;
            pwInfo->m_pFAT = 0;
        }

        return uReturn;
    } catch (...) {
        ASSERT(0);
    }

    return WFE_FAILURE;
}

///////////////////////////////////////////////////////////////////////////////
// common functions

static inline bool
isExactMatchingFolder(CEraserContext *context, const CString& strFolder)
{
    try {
        if (context->m_pstrlDirectories) {
            return (context->m_pstrlDirectories->Find(strFolder) != NULL);
        }
    } catch (...) {
        ASSERT(0);
    }

    return true;
}

static inline bool
isMatchingFolder(CEraserContext *context, const CString& strFolder)
{
    try {
        if (context->m_pstrlDirectories) {
            // see if given folder is in the list
            CString strItem;
            E_INT32 iLength = strFolder.GetLength();
            POSITION pos = context->m_pstrlDirectories->GetHeadPosition();

            while (pos != NULL) {
                strItem = context->m_pstrlDirectories->GetNext(pos);
                if (_tcsncmp((LPCTSTR)strItem, (LPCTSTR)strFolder, iLength) == 0) {
                    return true;
                }
            }

            return false;
        }
    } catch (...) {
        ASSERT(0);
    }

    return true;
}

static E_UINT32
clearDeletedEntries(wfeInfo *pwInfo, CEraserContext *context, E_PUINT8 pEntry, E_UINT32 uSize, const CString& strBase)
{
    E_UINT32  uReturn = WFE_SUCCESS;
    E_PUINT8  pEnd      = pEntry + uSize;
    PDIRENTRY pd        = 0;
    char      szDeName[9];
    char      szDeExtension[4];
    CString   strExtension;
    DIRINFO   diInfo;

    try {
        szDeName[8] = 0;
        szDeExtension[3] = 0;

        while (pEntry < pEnd) {
            pd = (PDIRENTRY)pEntry;

            if (pd->deName[0] == 0) {
                // filename is not used
            } else if (pd->deName[0] == 0xE5) {
                // deleted entry
                ZeroMemory(&(pEntry[1]), sizeof(DIRENTRY) - 1);
                uReturn = WFE_CHANGED;
            } else if (((PLONGDIRENTRY)pd)->leAttributes == 0xF) {
                // long entry
            } else if (bitSet(pd->deAttributes, 0x10)) {
                // directory
                if (strncmp(".       ", reinterpret_cast<char*>(pd->deName), 8) != 0 &&
                    strncmp("..      ", reinterpret_cast<char*>(pd->deName), 8) != 0) {

                    strncpy(szDeName, reinterpret_cast<char*>(pd->deName), 8);
                    strncpy(szDeExtension, reinterpret_cast<char*>(pd->deExtension), 3);

                    diInfo.strPath = strBase + CString(szDeName);
                    diInfo.strPath.TrimRight();

                    strExtension = szDeExtension;
                    strExtension.TrimRight();

                    if (!strExtension.IsEmpty()) {
                        diInfo.strPath += _T(".") + strExtension;
                    }

                    diInfo.strPath += _T("\\");
                    diInfo.strPath.MakeUpper();

                    if (isMatchingFolder(context, diInfo.strPath)) {
                        // subdirectory - store on stack for later processing
                        if (context->m_piCurrent.m_fsType != fsFAT32) {
                            diInfo.uCluster = ((E_UINT32)pd->deStartCluster) & 0xFFFFF;
                        } else {
                            diInfo.uCluster = ((E_UINT32)pd->deEAhandle) & 0xFFFF;
                            diInfo.uCluster <<= 16;
                            diInfo.uCluster |= ((E_UINT32)pd->deStartCluster) & 0xFFFF;
                        }
                        pwInfo->m_stDirectories.Push(diInfo);
                    }
                }
            }

            pEntry += sizeof(DIRENTRY);
        }

        // we must keep trailing zero to show end of directory,
        // unless of course cluster is completely full
        pEntry++;

        // clear off slack at end of directory chain unless full
        if (pEntry < pEnd) {
            ZeroMemory(pEntry, pEnd - pEntry);
        }

        return uReturn;
    } catch (CException *e) {
        handleException(e, context);
    } catch (...) {
        ASSERT(0);
    }

    return WFE_FAILURE;
}

static inline E_UINT32
getBufferSize(wfeInfo *pwInfo, E_UINT32 uIndexCluster, E_UINT32& uSize)
{
    try {
        uSize = 0;

        while (isClusterNotEnd(pwInfo, uIndexCluster)) {
            uSize += pwInfo->m_uClusterSize;
            uIndexCluster = pwInfo->FS.item(pwInfo->m_pFAT, uIndexCluster);
        }

        if (isClusterError(pwInfo, uIndexCluster) || uSize == 0) {
            return WFE_FAILURE;
        }

        return WFE_SUCCESS;
    } catch (...) {
        ASSERT(0);
    }

    return WFE_FAILURE;
}

static E_UINT32
readEntry(wfeInfo *pwInfo, E_UINT32 uIndexCluster, E_PUINT8 pBuffer)
{
    E_UINT32 uIndexBuffer = 0;
    E_UINT32 uReturn      = WFE_SUCCESS;

    try {
        while (isClusterNotEnd(pwInfo, uIndexCluster)) {
            uReturn = readSectors(pwInfo,
                                  clusterToSector(pwInfo, uIndexCluster),
                                  pwInfo->m_uSectorsPerCluster,
                                  &(pBuffer[uIndexBuffer]));

            if (uReturn != WFE_SUCCESS) {
                break;
            }

            uIndexBuffer += pwInfo->m_uClusterSize;
            uIndexCluster = pwInfo->FS.item(pwInfo->m_pFAT, uIndexCluster);
        }
    } catch (...) {
        ASSERT(0);
        uReturn = WFE_FAILURE;
    }

    return uReturn;
}

static E_UINT32
writeEntry(wfeInfo *pwInfo, E_UINT32 uIndexCluster, E_PUINT8 pBuffer)
{
    E_UINT32 uIndexBuffer = 0;
    E_UINT32 uReturn = WFE_SUCCESS;

    try {
        while (isClusterNotEnd(pwInfo, uIndexCluster)) {
            uReturn = writeSectors(pwInfo,
                                   clusterToSector(pwInfo, uIndexCluster),
                                   pwInfo->m_uSectorsPerCluster,
                                   &(pBuffer[uIndexBuffer]));

            if (uReturn != WFE_SUCCESS) {
                break;
            }

            uIndexBuffer += pwInfo->m_uClusterSize;
            uIndexCluster = pwInfo->FS.item(pwInfo->m_pFAT, uIndexCluster);
        }
    } catch (...) {
        ASSERT(0);
        uReturn = WFE_FAILURE;
    }

    return uReturn;
}

static E_UINT32
clearEntries(wfeInfo *pwInfo, CEraserContext *context)
{
    E_UINT32 uReturn     = WFE_SUCCESS;
    E_UINT32 uBufferSize = 0;
    E_PUINT8 pBuffer     = 0;
    DIRINFO  diInfo;
    CString  strDirectory;
    CString  strError;

    try {
        // reset progress
        context->m_uProgressWipedFiles = 0;

        if (context->m_piCurrent.m_fsType != fsFAT32) {
            // the non-standard FAT16/FAT12 root directory must be handled separately
            uBufferSize = pwInfo->m_uFATDirectorySize * pwInfo->m_uSectorSize;

            pBuffer = new E_UINT8[uBufferSize];
            ZeroMemory(pBuffer, uBufferSize);

            uReturn = readSectors(pwInfo, pwInfo->m_uFATDirectoryStart,
                                  pwInfo->m_uFATDirectorySize, pBuffer);

            if (uReturn == WFE_SUCCESS) {
                uReturn = clearDeletedEntries(pwInfo, context, pBuffer, uBufferSize, _T("\\"));

                if (uReturn == WFE_CHANGED && isExactMatchingFolder(context, _T("\\"))) {
                    uReturn = writeSectors(pwInfo, pwInfo->m_uFATDirectoryStart,
                                           pwInfo->m_uFATDirectorySize, pBuffer);
                }
            }

            delete[] pBuffer;
            pBuffer = 0;

            // progress information
            context->m_uProgressWipedFiles++;
            if (context->m_uProgressFolders > 0) {
                eraserSafeAssign(context, context->m_uProgressPercent,
                    (E_UINT8)((context->m_uProgressWipedFiles * 100) / context->m_uProgressFolders));
            }

            eraserUpdateNotify(context);
        } else {
            // the FAT32 root directory can be handled like any other directory
            diInfo.uCluster = pwInfo->m_uFATDirectoryStart;
            diInfo.strPath = "\\";

            pwInfo->m_stDirectories.Push(diInfo);
        }

        while (pwInfo->m_stDirectories.Pop(&diInfo) && (uReturn == WFE_SUCCESS || uReturn == WFE_CHANGED)) {

            if (eraserInternalTerminated(context) || !isClusterNotEnd(pwInfo, diInfo.uCluster)) {
                return WFE_FAILURE;
            }

            if (isWindowsNT && !isEntryLocked(pwInfo)) {
                strDirectory.Format(_T("%c:%s"), (TCHAR)(pwInfo->m_iVolume + 'A' - 1), (LPCTSTR)diInfo.strPath);
                pwInfo->FS.lockDirectory(pwInfo, (LPCTSTR)strDirectory);
            }

            uReturn = getBufferSize(pwInfo, diInfo.uCluster, uBufferSize);

            if (uReturn == WFE_SUCCESS) {
                pBuffer = new E_UINT8[uBufferSize];
                ZeroMemory(pBuffer, uBufferSize);

                uReturn = readEntry(pwInfo, diInfo.uCluster, pBuffer);

                if (uReturn == WFE_SUCCESS) {
                    uReturn = clearDeletedEntries(pwInfo, context, pBuffer, uBufferSize, diInfo.strPath);

                    if (uReturn == WFE_CHANGED && isExactMatchingFolder(context, diInfo.strPath)) {
                        if (isWindowsNT && !isEntryLocked(pwInfo)) {
                            eraserAddError1(context, IDS_ERROR_DIRENTRIES_LOCK, (LPCTSTR)strDirectory);
                        } else {
                            uReturn = writeEntry(pwInfo, diInfo.uCluster, pBuffer);
                        }
                    }
                }

                delete[] pBuffer;
                pBuffer = 0;
            }

            if (isWindowsNT) {
                pwInfo->FS.unlockDirectory(pwInfo);
            }

            // progress information
            context->m_uProgressWipedFiles++;
            if (context->m_uProgressFolders > 0) {
                eraserSafeAssign(context, context->m_uProgressPercent,
                    (E_UINT8)((context->m_uProgressWipedFiles * 100) / context->m_uProgressFolders));
            }
            setTotalProgress(context);

            eraserUpdateNotify(context);
        }
    } catch (CException *e) {
        handleException(e, context);
        uReturn = WFE_FAILURE;
    } catch (...) {
        ASSERT(0);
        uReturn = WFE_FAILURE;
    }

    return uReturn;
}

///////////////////////////////////////////////////////////////////////////////
// initialize file system specific read & write functions

static void
initReadWrite(wfeInfo *pwInfo, PARTITIONINFO& pi)
{
    try {
        if (isWindowsNT) {
            pwInfo->FS.readSectors     = readSectors_NT;
            pwInfo->FS.writeSectors    = writeSectors_NT;
            pwInfo->FS.lockVolume      = lockVolume_NT;
            pwInfo->FS.unlockVolume    = unlockVolume_NT;
            pwInfo->FS.lockState       = lockState_NT;
            pwInfo->FS.unlockDirectory = unlockDirectory_NT;
            pwInfo->FS.lockDirectory   = lockDirectory_NT;
            if (pi.m_fsType == fsFAT32) {
                pwInfo->FS.getInfo     = getInfo_FAT32;
            } else {
                pwInfo->FS.getInfo     = getInfo_FAT;
            }
        } else {
            if (pi.m_fsType == fsFAT32) {
                pwInfo->FS.readSectors     = readSectors_FAT32;
                pwInfo->FS.writeSectors    = writeSectors_FAT32;
                pwInfo->FS.lockVolume      = lockVolume_FAT32;
                pwInfo->FS.unlockVolume    = unlockVolume_FAT32;
                pwInfo->FS.lockState       = lockState_FAT32;
                pwInfo->FS.unlockDirectory = unlockDirectory_FAT;
                pwInfo->FS.lockDirectory   = lockDirectory_FAT;
                pwInfo->FS.getInfo         = getInfo_FAT32;
            } else {
                pwInfo->FS.readSectors     = readSectors_FAT;
                pwInfo->FS.writeSectors    = writeSectors_FAT;
                pwInfo->FS.lockVolume      = lockVolume_FAT;
                pwInfo->FS.unlockVolume    = unlockVolume_FAT;
                pwInfo->FS.lockState       = lockState_FAT;
                pwInfo->FS.unlockDirectory = unlockDirectory_FAT;
                pwInfo->FS.lockDirectory   = lockDirectory_FAT;
                pwInfo->FS.getInfo         = getInfo_FAT;
            }
        }
        pwInfo->m_uSectorSize = DEFAULT_SECTOR_SIZE;
    } catch (...) {
        ASSERT(0);
    }
}

///////////////////////////////////////////////////////////////////////////////
// initialize file system information and other specific stuff

static void
initFileSystemInfo(wfeInfo *pwInfo, PARTITIONINFO& pi)
{
    try {
        // information
        if (pwInfo->m_uSectorsPerCluster == 0) {
            pwInfo->m_uClustersTotal = 0;
        } else {
            pwInfo->m_uClustersTotal = (pwInfo->m_uSectorsTotal - pwInfo->m_uStartSector) /
                                       pwInfo->m_uSectorsPerCluster;
        }

        pwInfo->m_uClusterSize = pwInfo->m_uSectorsPerCluster * pwInfo->m_uSectorSize;

        // FAT type
        if (pi.m_fsType == fsFAT) {
            if (pwInfo->m_uClustersTotal <= FAT12_MAX_CLUSTERS) {
                pi.m_fsType = fsFAT12;
            } else {
                pi.m_fsType = fsFAT16;
            }
        }

        // specific
        if (pi.m_fsType == fsFAT32) {
            pwInfo->FS.item         = item_FAT32;
            pwInfo->FS.uHighValue   = FAT32_MAX_VALUE;
            pwInfo->FS.uLowValue    = FAT32_MAX_CLUSTERS;
            pwInfo->FS.uErrorValue  = FAT32_MAX_ERROR;
        } else if (pi.m_fsType == fsFAT16) {
            pwInfo->FS.item         = item_FAT16;
            pwInfo->FS.uHighValue   = FAT16_MAX_VALUE;
            pwInfo->FS.uLowValue    = FAT16_MAX_CLUSTERS;
            pwInfo->FS.uErrorValue  = FAT16_MAX_ERROR;
        } else {
            pwInfo->FS.item         = item_FAT12;
            pwInfo->FS.uHighValue   = FAT12_MAX_VALUE;
            pwInfo->FS.uLowValue    = FAT12_MAX_CLUSTERS;
            pwInfo->FS.uErrorValue  = FAT12_MAX_ERROR;
        }
    } catch (...) {
        ASSERT(0);
    }
}

static inline E_UINT32
openVolume(wfeInfo& wfe, PARTITIONINFO& pi)
{
    if (!isFileSystemFAT(pi)) {
        return WFE_NOT_SUPPORTED;
    }

    // one-based volume
    wfe.m_iVolume = toupper(pi.m_szDrive[0]) - 'A' + 1;

    if (wfe.m_hVolume != INVALID_HANDLE_VALUE) {
        CloseHandle(wfe.m_hVolume);
        wfe.m_hVolume = INVALID_HANDLE_VALUE;
    }

    if (isWindowsNT) {
        TCHAR szVolume[] = _T("\\\\.\\ :");
        szVolume[4] = pi.m_szDrive[0];

        wfe.m_hVolume = CreateFile(szVolume, GENERIC_READ | GENERIC_WRITE,
                                   FILE_SHARE_READ | FILE_SHARE_WRITE, NULL,
                                   OPEN_EXISTING, FILE_FLAG_NO_BUFFERING,
                                   NULL);
    } else {
        wfe.m_hVolume = CreateFile(szVWIN32, 0, 0, NULL, 0,
                                   FILE_FLAG_DELETE_ON_CLOSE, NULL);
    }

    if (wfe.m_hVolume != INVALID_HANDLE_VALUE) {
        return WFE_SUCCESS;
    } else {
        return WFE_FAILURE;
    }
}

///////////////////////////////////////////////////////////////////////////////
// functions exported from module

bool
getFATClusterAndSectorSize(LPCTSTR szDrive, E_UINT32& uCluster, E_UINT32& uSector)
{
    E_PUINT8 pBuffer = 0;

    uCluster = 0;
    uSector = DEFAULT_SECTOR_SIZE;

    try {
        PARTITIONINFO pi;
        wfeInfo wfe;

        pi.m_szDrive[0] = szDrive[0];

        if (!getPartitionType(pi)) {
            return false;
        }

        if (openVolume(wfe, pi) == WFE_SUCCESS) {
            bool bResult = false;

            pBuffer = new E_UINT8[MAX_SECTOR_BUFFER];
            ZeroMemory(pBuffer, MAX_SECTOR_BUFFER);

            // init read & write for the file system
            initReadWrite(&wfe, pi);

            for (E_UINT32 uRestarts = 0; uRestarts < ERASER_MAXIMUM_RESTARTS; uRestarts++) {
                bResult = (wfe.FS.lockVolume(&wfe, LEVEL1_LOCK, LOCK_MAX_PERMISSION) == DISK_SUCCESS);

                if (bResult) {
                    // read in the boot sector
                    bResult = (readSectors(&wfe, 0, 1, pBuffer) == WFE_SUCCESS);

                    wfe.FS.unlockVolume(&wfe);

                    if (bResult) {
                        // calculate cluster size
                        if (pi.m_fsType == fsFAT32) {
                            BOOTSECTOR32 *pbsBoot = (BOOTSECTOR32*)pBuffer;
                            uSector = pbsBoot->bpb.A_BF_BPB_BytesPerSector;
                            uCluster = uSector * pbsBoot->bpb.A_BF_BPB_SectorsPerCluster;
                        } else {
                            BOOTSECTOR *pbsBoot = (BOOTSECTOR*)pBuffer;
                            uSector = pbsBoot->bsBytesPerSec;
                            uCluster = uSector * pbsBoot->bsSecPerClust;
                        }
                    }

                    break;
                }

                Sleep(0);
            }

            delete[] pBuffer;
            pBuffer = 0;

            return (bResult && uCluster > 0);
        }
    }
    catch (...) {
        ASSERT(0);
        if (pBuffer) {
            try {
                delete[] pBuffer;
            } catch (...) {
            }
            pBuffer = 0;
        }
    }

    return false;
}

static E_UINT32
wipeEntries(CEraserContext *context)
{
    try {
        wfeInfo wfe;
        E_UINT32 uResult = openVolume(wfe, context->m_piCurrent);

        if (uResult == WFE_SUCCESS) {
            E_PUINT8 pBuffer = new E_UINT8[MAX_SECTOR_BUFFER];
            ZeroMemory(pBuffer, MAX_SECTOR_BUFFER);

            // init read & write for the file system
            initReadWrite(&wfe, context->m_piCurrent);

            if (wfe.FS.lockVolume(&wfe, LEVEL1_LOCK, LOCK_MAX_PERMISSION) == DISK_SUCCESS) {
                // read in the boot sector
                if (readSectors(&wfe, 0, 1, pBuffer) == WFE_SUCCESS) {
                    wfe.FS.getInfo(&wfe, pBuffer);
                    initFileSystemInfo(&wfe, context->m_piCurrent);

                    uResult = readFAT(&wfe);

                    if (uResult == WFE_SUCCESS) {
                        uResult = clearEntries(&wfe, context);
                    }
                }

                wfe.FS.unlockVolume(&wfe);
            }

            delete[] pBuffer;
            pBuffer = 0;
        }

        // done.
        return uResult;
    } catch (CException *e) {
        handleException(e, context);
    }

    return WFE_FAILURE;
}

E_UINT32
wipeFATFileEntries(CEraserContext *context, LPCTSTR szRetryMessage)
{
    E_UINT32 uError = WFE_SUCCESS;
    E_UINT32 uRestart = 0;
    bool     bStop = false;
    CString  strMessage;

    try {
        do {
            uRestart++;

            // send the start message
            eraserBeginNotify(context);

            // wipe directory entries
            uError = wipeEntries(context);

            // if successful - great, if failed - not so great, if a
            // write was detected - try again, up to cdwMaximumRestarts

            switch (uError) {
            case WFE_SUCCESS:
                bStop = true;
                break;
            case WFE_DISKMODIFIED:
                if (!eraserInternalTerminated(context)) {
                    strMessage.Format(szRetryMessage, uRestart);
                    eraserProgressSetMessage(context, strMessage);
                    Sleep(0);
                } else {
                    bStop = true;
                }
                break;
            default:
                if (uError == WFE_NOT_SUPPORTED) {
                    eraserAddError1(context, IDS_ERROR_DIRENTRIES_FS, (LPCTSTR)context->m_strData);
                } else if (uError == WFE_FATERROR) {
                    eraserAddError1(context, IDS_ERROR_DIRENTRIES_FAT, (LPCTSTR)context->m_strData);
                } else {
                    eraserAddError1(context, IDS_ERROR_DIRENTRIES, (LPCTSTR)context->m_strData);
                }
                bStop = true;
                break;
            }

            if (!eraserInternalTerminated(context) && !bStop && uRestart == ERASER_MAXIMUM_RESTARTS) {
                eraserAddError1(context, IDS_ERROR_DIRENTRIES_MAXRESTARTS, (LPCTSTR)context->m_strData);
            }
        } while (bStop == false && uRestart < ERASER_MAXIMUM_RESTARTS);

        // add some entropy to the pool - how many times we needed
        // to restart processing
        randomAddEntropy((E_PUINT8)&uRestart, sizeof(E_PUINT8));

        return uError;
    } catch (CException *e) {
        handleException(e, context);
    }

    return WFE_FAILURE;
}