// NTFS.h
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
//
// Copyright (C) 1997 Mark Russinovich
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

#ifndef NTFS_H
#define NTFS_H

// Header file for defragmentation demonstration program. This file
// includes definitions for defragmentation File System Control
// commands, as well as the undocumented NtFsControl call.

//--------------------------------------------------------------------
//                     D E F I N E S
//--------------------------------------------------------------------


//
// File System Control commands related to defragging
//
#define FSCTL_GET_VOLUME_INFORMATION    0x90064
#define FSCTL_READ_MFT_RECORD           0x90068
//GT#define FSCTL_GET_VOLUME_BITMAP         0x9006F
//GT#define FSCTL_GET_RETRIEVAL_POINTERS    0x90073
//GT#define FSCTL_MOVE_FILE                 0x90074

//
// return code type
//
typedef LONG NTSTATUS;

//
// Check for success
//
#define NT_SUCCESS(Status) ((NTSTATUS)(Status) >= 0)


//
// Error codes returned by NtFsControlFile (see NTSTATUS.H)
//
#define STATUS_SUCCESS                   ((NTSTATUS)0x00000000L)
#define STATUS_BUFFER_OVERFLOW           ((NTSTATUS)0x80000005L)
#ifndef STATUS_INVALID_PARAMETER
	#define STATUS_INVALID_PARAMETER         ((NTSTATUS)0xC000000DL)
#endif
#define STATUS_BUFFER_TOO_SMALL          ((NTSTATUS)0xC0000023L)
#define STATUS_ACCESS_DENIED             ((NTSTATUS)0xC0000011L)
#define STATUS_ALREADY_COMMITTED         ((NTSTATUS)0xC0000021L)
#define STATUS_INVALID_DEVICE_REQUEST    ((NTSTATUS)0xC0000010L)


//--------------------------------------------------------------------
//       F S C T L  S P E C I F I C   T Y P E D E F S
//--------------------------------------------------------------------


//
// This is the definition for a VCN/LCN (virtual cluster/logical cluster)
// mapping pair that is returned in the buffer passed to
// FSCTL_GET_RETRIEVAL_POINTERS
//
typedef struct {
    ULONGLONG           Vcn;
    ULONGLONG           Lcn;
} MAPPING_PAIR, *PMAPPING_PAIR;

//
// This is the definition for the buffer that FSCTL_GET_RETRIEVAL_POINTERS
// returns. It consists of a header followed by mapping pairs
//
typedef struct {
    ULONG               NumberOfPairs;
    ULONGLONG           StartVcn;
    MAPPING_PAIR        Pair[1];
} GET_RETRIEVAL_DESCRIPTOR, *PGET_RETRIEVAL_DESCRIPTOR;


//
// This is the definition of the buffer that FSCTL_GET_VOLUME_BITMAP
// returns. It consists of a header followed by the actual bitmap data
//
typedef struct {
    ULONGLONG           StartLcn;
    ULONGLONG           ClustersToEndOfVol;
    BYTE                Map[1];
} BITMAP_DESCRIPTOR, *PBITMAP_DESCRIPTOR;


//
// This is the definition for the data structure that is passed in to
// FSCTL_MOVE_FILE
//
typedef struct {
     HANDLE            FileHandle;
     ULONG             Reserved;
     ULONGLONG         StartVcn;
     ULONGLONG         TargetLcn;
     ULONG             NumVcns;
     ULONG             Reserved1;
} MOVEFILE_DESCRIPTOR, *PMOVEFILE_DESCRIPTOR;


//
// NTFS volume information
//
/* GT
typedef struct {
    ULONGLONG       SerialNumber;
    ULONGLONG       NumberOfSectors;
    ULONGLONG       TotalClusters;
    ULONGLONG       FreeClusters;
    ULONGLONG       Reserved;
    ULONG           BytesPerSector;
    ULONG           BytesPerCluster;
    ULONG           BytesPerMFTRecord;
    ULONG           ClustersPerMFTRecord;
    ULONGLONG       MFTLength;
    ULONGLONG       MFTStart;
    ULONGLONG       MFTMirrorStart;
    ULONGLONG       MFTZoneStart;
    ULONGLONG       MFTZoneEnd;
} NTFS_VOLUME_DATA_BUFFER, *PNTFS_VOLUME_DATA_BUFFER; */



//--------------------------------------------------------------------
//     N T F S C O N T R O L F I L E   D E F I N I T I O N S
//--------------------------------------------------------------------

//
// Prototype for NtFsControlFile and data structures
// used in its definition
//

//
// Io Status block (see NTDDK.H)
//
typedef struct _IO_STATUS_BLOCK {
    NTSTATUS Status;
    ULONG_PTR Information;
} IO_STATUS_BLOCK, *PIO_STATUS_BLOCK;


//
// Apc Routine (see NTDDK.H)
//
typedef VOID (*PIO_APC_ROUTINE) (
                PVOID ApcContext,
                PIO_STATUS_BLOCK IoStatusBlock,
                ULONG Reserved
            );


//
// The undocumented NtFsControlFile
//
// This function is used to send File System Control (FSCTL)
// commands into file system drivers. Its definition is
// in ntdll.dll (ntdll.lib), a file shipped with the NTDDK.
//
typedef NTSTATUS (__stdcall *NTFSCONTROLFILE)(
                    HANDLE FileHandle,
                    HANDLE Event,                   // optional
                    PIO_APC_ROUTINE ApcRoutine,     // optional
                    PVOID ApcContext,               // optional
                    PIO_STATUS_BLOCK IoStatusBlock,
                    ULONG FsControlCode,
                    PVOID InputBuffer,              // optional
                    ULONG InputBufferLength,
                    PVOID OutputBuffer,             // optional
                    ULONG OutputBufferLength
            );


typedef ULONG (__stdcall *RTLNTSTATUSTODOSERROR) (
        IN NTSTATUS Status
        );


//
// File information classes (see NTDDK.H)
//
typedef enum _FILE_INFORMATION_CLASS {
// end_wdm
    FileDirectoryInformation       = 1,
    FileFullDirectoryInformation, // 2
    FileBothDirectoryInformation, // 3
    FileBasicInformation,         // 4  wdm
    FileStandardInformation,      // 5  wdm
    FileInternalInformation,      // 6
    FileEaInformation,            // 7
    FileAccessInformation,        // 8
    FileNameInformation,          // 9
    FileRenameInformation,        // 10
    FileLinkInformation,          // 11
    FileNamesInformation,         // 12
    FileDispositionInformation,   // 13
    FilePositionInformation,      // 14 wdm
    FileFullEaInformation,        // 15
    FileModeInformation,          // 16
    FileAlignmentInformation,     // 17
    FileAllInformation,           // 18
    FileAllocationInformation,    // 19
    FileEndOfFileInformation,     // 20 wdm
    FileAlternateNameInformation, // 21
    FileStreamInformation,        // 22
    FilePipeInformation,          // 23
    FilePipeLocalInformation,     // 24
    FilePipeRemoteInformation,    // 25
    FileMailslotQueryInformation, // 26
    FileMailslotSetInformation,   // 27
    FileCompressionInformation,   // 28
    FileObjectIdInformation,      // 29
    FileCompletionInformation,    // 30
    FileMoveClusterInformation,   // 31
    FileQuotaInformation,         // 32
    FileReparsePointInformation,  // 33
    FileNetworkOpenInformation,   // 34
    FileAttributeTagInformation,  // 35
    FileTrackingInformation,      // 36
    FileMaximumInformation
// begin_wdm
} FILE_INFORMATION_CLASS, *PFILE_INFORMATION_CLASS;


// #######################################################################
// ############ DEFINITIONS
// #######################################################################
#define OBJ_EXCLUSIVE           0x00000020L
#define OBJ_KERNEL_HANDLE       0x00000200L
#define FILE_NON_DIRECTORY_FILE 0x00000040

typedef LONG NTSTATUS;

typedef struct _FILE_BASIC_INFORMATION {                    
    LARGE_INTEGER CreationTime;							// Created             
    LARGE_INTEGER LastAccessTime;                       // Accessed    
    LARGE_INTEGER LastWriteTime;                        // Modifed
    LARGE_INTEGER ChangeTime;                           // Entry Modified
    ULONG FileAttributes;                                   
} FILE_BASIC_INFORMATION, *PFILE_BASIC_INFORMATION;

typedef NTSTATUS (WINAPI *pNtQueryInformationFile)(HANDLE, PIO_STATUS_BLOCK, PVOID, ULONG, FILE_INFORMATION_CLASS);
typedef NTSTATUS (WINAPI *pNtSetInformationFile)(HANDLE, PIO_STATUS_BLOCK, PVOID, ULONG, FILE_INFORMATION_CLASS);


typedef NTSTATUS (__stdcall *NTQUERYINFORMATIONFILE)(
            IN HANDLE FileHandle,
            OUT PIO_STATUS_BLOCK IoStatusBlock,
            OUT PVOID FileInformation,
            IN ULONG Length,
            IN FILE_INFORMATION_CLASS FileInformationClass
            );


//
// Streams information
//
#pragma pack(4)
typedef struct {
    ULONG               NextEntry;
    ULONG               NameLength;
    LARGE_INTEGER       Size;
    LARGE_INTEGER       AllocationSize;
    USHORT              Name[1];
} FILE_STREAM_INFORMATION, *PFILE_STREAM_INFORMATION;
#pragma pack()



// error codes
#define WCF_FAILURE         0
#define WCF_SUCCESS         1
#define WCF_NOTCOMPRESSED   2
#define WCF_NOACCESS        3

// structure for function pointers
typedef struct _NTFSContext {
    _NTFSContext() :
        NtFsControlFile(0),
        NtQueryInformationFile(0),
        RtlNtStatusToDosError(0),
        m_hNTDLL(NULL),
        m_hVolume(INVALID_HANDLE_VALUE)
    {}

    ~_NTFSContext() {
        NtFsControlFile = 0;
        NtQueryInformationFile = 0;
        RtlNtStatusToDosError = 0;

        if (m_hNTDLL != NULL) {
            AfxFreeLibrary(m_hNTDLL);
            m_hNTDLL = NULL;
        }
        if (m_hVolume != INVALID_HANDLE_VALUE) {
            CloseHandle(m_hVolume);
            m_hVolume = INVALID_HANDLE_VALUE;
        }
    }

    // functions
    NTFSCONTROLFILE NtFsControlFile;
    NTQUERYINFORMATIONFILE NtQueryInformationFile;
    RTLNTSTATUSTODOSERROR RtlNtStatusToDosError;

    // handles
    HINSTANCE m_hNTDLL;
    HANDLE m_hVolume;
} NTFSContext;


// functions exported from this module
bool
findAlternateDataStreams(CEraserContext *context, LPCTSTR szFile, DataStreamArray& streams);

bool
wipeMFTRecords(CEraserContext *context);

bool
wipeNTFSFileEntries(CEraserContext *context);

E_UINT32
wipeCompressedFile(CEraserContext *context);

#endif