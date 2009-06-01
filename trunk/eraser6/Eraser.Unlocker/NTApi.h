/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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

#define STATUS_SUCCESS                          ((NTSTATUS)0x00000000L) // ntsubauth
#define STATUS_BUFFER_OVERFLOW                  ((NTSTATUS)0x80000005L)
#define STATUS_WAIT_0                           ((DWORD   )0x00000000L)

typedef struct _UNICODE_STRING {
	USHORT Length;
	USHORT MaximumLength;
	PWSTR  Buffer;
} UNICODE_STRING;
typedef UNICODE_STRING *PUNICODE_STRING;
typedef const UNICODE_STRING *PCUNICODE_STRING;

typedef struct _SYSTEM_HANDLE_INFORMATION {
	ULONG  ProcessId;
	UCHAR  ObjectTypeNumber;
	UCHAR  Flags;
	USHORT  Handle;
	PVOID  Object;
	ACCESS_MASK  GrantedAccess;
} SYSTEM_HANDLE_INFORMATION, *PSYSTEM_HANDLE_INFORMATION;

typedef struct _SYSTEM_HANDLES {
	ULONG NumberOfHandles;
	SYSTEM_HANDLE_INFORMATION Information[1];
} SYSTEM_HANDLES, *PSYSTEM_HANDLES;

typedef struct _FILE_NAME_INFORMATION {
	ULONG  FileNameLength;
	WCHAR  FileName[1];
} FILE_NAME_INFORMATION, *PFILE_NAME_INFORMATION;

typedef struct _IO_STATUS_BLOCK {
	union {
		NTSTATUS Status;
		PVOID Pointer;
	} DUMMYUNIONNAME;

	ULONG_PTR Information;
} IO_STATUS_BLOCK, *PIO_STATUS_BLOCK;

typedef enum _POOL_TYPE {
	NonPagedPool,
	PagedPool,
	NonPagedPoolMustSucceed,
	DontUseThisType,
	NonPagedPoolCacheAligned,
	PagedPoolCacheAligned,
	NonPagedPoolCacheAlignedMustS,
	MaxPoolType,
	NonPagedPoolSession = 32,
	PagedPoolSession,
	NonPagedPoolMustSucceedSession,
	DontUseThisTypeSession,
	NonPagedPoolCacheAlignedSession,
	PagedPoolCacheAlignedSession,
	NonPagedPoolCacheAlignedMustSSession
} POOL_TYPE;

typedef struct _OBJECT_TYPE_INFORMATION {
	UNICODE_STRING  Name;
	ULONG  ObjectCount;
	ULONG  HandleCount;
	ULONG  Reserved1[4];
	ULONG  PeakObjectCount;
	ULONG  PeakHandleCount;
	ULONG  Reserved2[4];
	ULONG  InvalidAttributes;
	GENERIC_MAPPING  GenericMapping;
	ULONG  ValidAccess;
	UCHAR  Unknown;
	BOOLEAN  MaintainHandleDatabase;
	POOL_TYPE  PoolType;
	ULONG  PagedPoolUsage;
	ULONG  NonPagedPoolUsage;
} OBJECT_TYPE_INFORMATION, *POBJECT_TYPE_INFORMATION;

typedef enum _FILE_INFORMATION_CLASS {
	FileDirectoryInformation=1,
	FileFullDirectoryInformation,
	FileBothDirectoryInformation,
	FileBasicInformation,
	FileStandardInformation,
	FileInternalInformation,
	FileEaInformation,
	FileAccessInformation,
	FileNameInformation,
	FileRenameInformation,
	FileLinkInformation,
	FileNamesInformation,
	FileDispositionInformation,
	FilePositionInformation,
	FileFullEaInformation,
	FileModeInformation,
	FileAlignmentInformation,
	FileAllInformation,
	FileAllocationInformation,
	FileEndOfFileInformation,
	FileAlternateNameInformation,
	FileStreamInformation,
	FilePipeInformation,
	FilePipeLocalInformation,
	FilePipeRemoteInformation,
	FileMailslotQueryInformation,
	FileMailslotSetInformation,
	FileCompressionInformation,
	FileCopyOnWriteInformation,
	FileCompletionInformation,
	FileMoveClusterInformation,
	FileQuotaInformation,
	FileReparsePointInformation,
	FileNetworkOpenInformation,
	FileObjectIdInformation,
	FileTrackingInformation,
	FileOleDirectoryInformation,
	FileContentIndexInformation,
	FileInheritContentIndexInformation,
	FileOleInformation,
	FileMaximumInformation
} FILE_INFORMATION_CLASS, *PFILE_INFORMATION_CLASS;

typedef enum _SYSTEM_INFORMATION_CLASS {
	SystemBasicInformation = 0,
	SystemPerformanceInformation = 2,
	SystemTimeOfDayInformation = 3,
	SystemProcessInformation = 5,
	SystemProcessorPerformanceInformation = 8,
	SystemHandleInformation = 16,
	SystemInterruptInformation = 23,
	SystemExceptionInformation = 33,
	SystemRegistryQuotaInformation = 37,
	SystemLookasideInformation = 45
} SYSTEM_INFORMATION_CLASS;

typedef enum _OBJECT_INFORMATION_CLASS {
	ObjectBasicInformation,
	ObjectNameInformation,
	ObjectTypeInformation,
	ObjectAllTypesInformation,
	ObjectHandleInformation
} OBJECT_INFORMATION_CLASS;

typedef NTSTATUS (__stdcall *fNtQuerySystemInformation)(
	__in       SYSTEM_INFORMATION_CLASS,
	__inout    PVOID,
	__in       ULONG,
	__out_opt  PULONG);

typedef NTSTATUS (__stdcall *fNtQueryInformationFile)(
	IN HANDLE  FileHandle,
	OUT PIO_STATUS_BLOCK  IoStatusBlock,
	OUT PVOID  FileInformation,
	IN ULONG  Length,
	IN FILE_INFORMATION_CLASS FileInformationClass);

typedef NTSTATUS (__stdcall *fNtQueryObject)(
	IN HANDLE   OPTIONAL,
	IN OBJECT_INFORMATION_CLASS  ,
	OUT PVOID   OPTIONAL,
	IN ULONG  ,
	OUT PULONG   OPTIONAL);

extern fNtQuerySystemInformation NtQuerySystemInformation;
extern fNtQueryInformationFile NtQueryInformationFile;
extern fNtQueryObject NtQueryObject;
