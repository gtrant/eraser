// NTFS.cpp
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
//
// SDelete - Secure Delete
// Copyright (C) 1999 Mark Russinovich
// Systems Internals - http://www.sysinternals.com
//
// This program implements a secure delete function for
// Windows NT/2K. It even works on WinNT compressed, encrypted
// and sparse files.
//
// This program is copyrighted. You may not use the source, or
// derived version of the source, in a secure delete application.
// You may use the source or techniques herein in applications
// with a purpose other than secure delete.

#include "stdafx.h"
#include "resource.h"
#include "EraserDll.h"
#include "Common.h"
#include "File.h"
#include "NTFS.h"
#include "winioctl.h"
// Invalid longlong number

#define LLINVALID       ((E_UINT64) -1)

// Size of the buffer we read file mapping information into.
// The buffer is big enough to hold the 16 bytes that
// come back at the head of the buffer (the number of entries
// and the starting virtual cluster), as well as 512 pairs
// of [virtual cluster, logical cluster] pairs.

#define FILEMAPSIZE     (16384 + 2)


static bool
initEntryPoints(NTFSContext& ntc)
{
    // load the NTDLL entry point we need
    ntc.m_hNTDLL = AfxLoadLibrary(ERASER_MODULENAME_NTDLL);

    if (ntc.m_hNTDLL != NULL) {
        ntc.NtFsControlFile =
            (NTFSCONTROLFILE) GetProcAddress(ntc.m_hNTDLL, ERASER_FUNCTIONNAME_NTFSCONTROLFILE);
        ntc.NtQueryInformationFile =
            (NTQUERYINFORMATIONFILE) GetProcAddress(ntc.m_hNTDLL, ERASER_FUNCTIONNAME_NTQUERYINFORMATIONFILE);
        ntc.RtlNtStatusToDosError =
            (RTLNTSTATUSTODOSERROR) GetProcAddress(ntc.m_hNTDLL, ERASER_FUNCTIONNAME_RTLNTSTATUSTODOSERROR);

        if (ntc.NtFsControlFile == NULL || ntc.RtlNtStatusToDosError == NULL) {
            AfxFreeLibrary(ntc.m_hNTDLL);
            ntc.m_hNTDLL = NULL;
        }
    }

    return (ntc.m_hNTDLL != NULL);
}

static CString
formatNTError(NTFSContext& ntc, NTSTATUS dwStatus)
{
    CString strMessage;
    LPTSTR szMessage;

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
                  NULL,
                  ntc.RtlNtStatusToDosError(dwStatus),
                  MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
                  (LPTSTR)&szMessage, 0, NULL);

    strMessage = szMessage;
    LocalFree(szMessage);

    return strMessage;
}

static bool
wipeClusters(NTFSContext& ntc, CEraserContext *context, bool& bCompressed)
{
    if (context->m_piCurrent.m_uCluster == 0) {
        return false;
    }

    NTSTATUS                  status = STATUS_INVALID_PARAMETER;
    E_INT32                   i;
    IO_STATUS_BLOCK           ioStatus;
    E_UINT64                  startVcn;
    PGET_RETRIEVAL_DESCRIPTOR fileMappings;
    E_UINT64                  fileMap[FILEMAPSIZE];
    HANDLE                    hFile;
	
    // set the handle passed to the wipe function
    hFile = context->m_hFile;
    context->m_hFile = ntc.m_hVolume;

    // assume file is in an MFT record.
    bCompressed = false;

    startVcn = 0;
    fileMappings = (PGET_RETRIEVAL_DESCRIPTOR) fileMap;

    status = ntc.NtFsControlFile(hFile, NULL, NULL, 0, &ioStatus,
                              FSCTL_GET_RETRIEVAL_POINTERS,
                              &startVcn, sizeof(startVcn),
                              fileMappings,
                              FILEMAPSIZE * sizeof(ULONGLONG));

    while (status == STATUS_SUCCESS || status == STATUS_BUFFER_OVERFLOW ||
           status == STATUS_PENDING) {
        // if the operation is pending, wait for it to finish
        if (status == STATUS_PENDING) {
            WaitForSingleObject(hFile, INFINITE);

            // get the status from the status block
            if (ioStatus.Status != STATUS_SUCCESS &&
                ioStatus.Status != STATUS_BUFFER_OVERFLOW) {
                context->m_hFile = hFile;
                return false;
            }
        }

        // progress information
        context->m_uProgressSize = 0;

        startVcn = fileMappings->StartVcn;

        for (i = 0; i < (E_UINT64) fileMappings->NumberOfPairs; i++) {
            if (fileMappings->Pair[i].Lcn != LLINVALID) {
                context->m_uProgressSize += (fileMappings->Pair[i].Vcn - startVcn) *
                                            (E_UINT64)context->m_piCurrent.m_uCluster;
            }

            startVcn = fileMappings->Pair[i].Vcn;
        }

        eraserProgressStartEstimate(context, context->m_uProgressSize);

        // loop through the buffer of number/cluster pairs, printing them out.
        startVcn = fileMappings->StartVcn;

        for (i = 0; i < (E_UINT64)fileMappings->NumberOfPairs; i++) {
            // On NT 4.0, a compressed virtual run (0-filled) is
            // identified with a cluster offset of -1

            if (fileMappings->Pair[i].Lcn != LLINVALID) {
                // its compressed and outside the zone
                bCompressed = true;

                // Overwrite the clusters if we were able to open the volume
                // for write access.
                context->m_uiFileStart.QuadPart = fileMappings->Pair[i].Lcn * context->m_piCurrent.m_uCluster;
                context->m_uiFileSize.QuadPart = (fileMappings->Pair[i].Vcn - startVcn) *
                                                 (E_UINT64)context->m_piCurrent.m_uCluster;

				if (context->m_lpmMethod->m_pwfFunction == bmMethods[4].m_pwfFunction)
				{
					context->m_hFile = hFile;
					context->m_saError.Add(_T("The file could not be erased with the first/last ")
						_T("2kb erasure because the file is compressed, encrypted or a sparse file."));
					return false;
				}

				if (!context->m_lpmMethod->m_pwfFunction(context)) {
                    context->m_hFile = hFile;
                    return false;
                }

            }

            startVcn = fileMappings->Pair[i].Vcn;
        }

        // if the buffer wasn't overflowed, then we're done
        if (NT_SUCCESS(status)) {
            break;
        }

        status = ntc.NtFsControlFile(hFile, NULL, NULL, 0, &ioStatus,
                                  FSCTL_GET_RETRIEVAL_POINTERS,
                                  &startVcn, sizeof(startVcn),
                                  fileMappings,
                                  FILEMAPSIZE * sizeof(E_UINT64));
    }

    if (status != STATUS_SUCCESS && status != STATUS_INVALID_PARAMETER &&
		ntc.RtlNtStatusToDosError(status) != ERROR_HANDLE_EOF) {
        context->m_saError.Add(formatNTError(ntc, status));
    }

    // restore the file handle
    context->m_hFile = hFile;

    // if we made through with no errors we've overwritten all the file's clusters.
    return NT_SUCCESS(status) || ntc.RtlNtStatusToDosError(status) == ERROR_HANDLE_EOF;
}

static bool
initAndOpenVolume(NTFSContext& ntc, TCHAR cDrive)
{
    TCHAR szVolumeName[] = _T("\\\\.\\ :");

    if (initEntryPoints(ntc)) {
        // open the volume for direct access
        szVolumeName[4] = cDrive;
        ntc.m_hVolume = CreateFile(szVolumeName, GENERIC_READ | GENERIC_WRITE,
                                   FILE_SHARE_READ | FILE_SHARE_WRITE, NULL,
                                   OPEN_EXISTING, FILE_FLAG_WRITE_THROUGH, 0);

        return (ntc.m_hVolume != INVALID_HANDLE_VALUE);
    }
    return false;
}

// exported functions
//

E_UINT32
wipeCompressedFile(CEraserContext *context)
{
    if (!isFileSystemNTFS(context->m_piCurrent)) {
        return WCF_NOTCOMPRESSED;
    }

    NTFSContext ntc;
    E_UINT32 uResult = WCF_FAILURE;
    bool bCompressed = false;

    if (initAndOpenVolume(ntc, context->m_strData[0])) {
        // open the file exclusively
        context->m_hFile = CreateFile((LPCTSTR)context->m_strData, GENERIC_READ,
                                      (context->m_uTestMode) ?
                                        FILE_SHARE_READ | FILE_SHARE_WRITE : 0,
                                      NULL, OPEN_EXISTING,
                                      FILE_FLAG_WRITE_THROUGH | FILE_FLAG_NO_BUFFERING, NULL);

        if (context->m_hFile != INVALID_HANDLE_VALUE) {
            try {
                // scan the location of the file
                if (wipeClusters(ntc, context, bCompressed)) {
                    // done with the file handle
                    CloseHandle(context->m_hFile);
                    context->m_hFile = INVALID_HANDLE_VALUE;

                    if (!bCompressed) {
                        // if the file wasn't really compressed, erase normally
                        uResult = WCF_NOTCOMPRESSED;
                    } else {
                        if (eraserOK(eraserRemoveFile((LPVOID)(LPCTSTR)context->m_strData,
                                (E_UINT16)context->m_strData.GetLength()))) {
                            uResult = WCF_SUCCESS;
                        }
                    }
                } else {
                    // close the handle
                    CloseHandle(context->m_hFile);
                    context->m_hFile = INVALID_HANDLE_VALUE;
                }
            } catch (CException *e) {
                handleException(e, context);
                uResult = WCF_FAILURE;

                if (context->m_hFile != INVALID_HANDLE_VALUE) {
                    CloseHandle(context->m_hFile);
                    context->m_hFile = INVALID_HANDLE_VALUE;
                }
            }
        }
    } else {
        // the user does not have privileges for low-level access
        uResult = WCF_NOACCESS;
    }

    return uResult;
}


#define mftFastWriteTest(x) \
    WriteFile((x)->m_hFile, uTestBuffer, (x)->m_uiFileSize.LowPart, &uTemp, NULL)

bool
wipeMFTRecords(CEraserContext *context)
{
    // On NTFS file system small files can be resident on the MFT record so we
    // will need to overwrite empty records by creating as many of the largest
    // sized files as possible (if there is space in the MFT, we'll be able to
    // create non-zero sized files, where the data is resident in the MFT record)

    if (!isFileSystemNTFS(context->m_piCurrent)) {
        return false;
    }

    E_UINT64 uFreeSpace = 0;
    eraserGetFreeDiskSpace((LPVOID)context->m_piCurrent.m_szDrive,
                           (E_UINT16)lstrlen(context->m_piCurrent.m_szDrive),
                           &uFreeSpace);

    if (uFreeSpace == 0) {
        const E_UINT16 maxMFTRecordSize = 4096;

        TCHAR        szFileName[uShortFileNameLength + 1];
        E_UINT16     uCounter = 1;
        E_UINT32     uTestBuffer[maxMFTRecordSize];
        E_UINT32     uTemp;
        E_UINT64     ulPrevSize = maxMFTRecordSize;
        CString      strTemp;
        CStringArray saList;
        bool         bCreatedFile;

        // fill test buffer with random data
        isaacFill((E_PUINT8)uTestBuffer, maxMFTRecordSize);

        // do something with the progress bar to entertain the user
        eraserDispMFT(context);
        eraserBeginNotify(context);

        context->m_uClusterSpace = 0;

        do {
            createRandomShortFileName(szFileName, uCounter++);
            strTemp.Format(_T("Eraser%s%s"), context->m_piCurrent.m_szDrive, szFileName);

            context->m_hFile = CreateFile((LPCTSTR)strTemp,
                                         GENERIC_WRITE,
                                         (context->m_uTestMode) ?
                                            FILE_SHARE_READ | FILE_SHARE_WRITE : 0,
                                         NULL,
                                         CREATE_NEW,
                                         FILE_ATTRIBUTE_HIDDEN | FILE_FLAG_WRITE_THROUGH,
                                         NULL);

            if (context->m_hFile == INVALID_HANDLE_VALUE) {
                break;
            }

            saList.Add(strTemp);

            try {
                context->m_uiFileStart.QuadPart = 0;
                context->m_uiFileSize.QuadPart = ulPrevSize;
                bCreatedFile = false;

                while (context->m_uiFileSize.QuadPart) {
                    if (eraserInternalTerminated(context)) {
                        bCreatedFile = false;
                        break;
                    }

                    // trying simple write first is much faster than calling the wipe function
                    if (!mftFastWriteTest(context) || !context->m_lpmMethod->m_pwfFunction(context)) {
                        eraserProgressSetMessage(context, ERASER_MESSAGE_MFT);
                        context->m_uiFileSize.QuadPart--;
                    } else {
                        strTemp.Format(ERASER_MESSAGE_MFT_WAIT, uCounter);
                        eraserProgressSetMessage(context, strTemp);
                        eraserUpdateNotify(context);

                        ulPrevSize = context->m_uiFileSize.QuadPart;
                        bCreatedFile = true;
                        break;
                    }

                    eraserSafeAssign(context, context->m_uProgressPercent,
                        (E_UINT8)(((maxMFTRecordSize - context->m_uiFileSize.QuadPart) * 100) / maxMFTRecordSize));
                    setTotalProgress(context);
                    eraserUpdateNotify(context);
                }
            } catch (CException *e) {
                handleException(e, context);
                bCreatedFile = false;
            }

            resetDate(context->m_hFile);
            CloseHandle(context->m_hFile);

        } while (bCreatedFile);

        eraserSafeAssign(context, context->m_uProgressPercent, 100);
        setTotalProgress(context);

        eraserProgressSetMessage(context, ERASER_MESSAGE_REMOVING);
        eraserUpdateNotify(context);

        E_INT32 iSize = static_cast<E_INT32>(saList.GetSize());
        for (E_INT32 i = 0; i < iSize; i++) {
            eraserSafeAssign(context, context->m_uProgressPercent, (E_UINT8)((i * 100) / iSize));
            eraserUpdateNotify(context);

            // file names are already random, no need to use slower eraserRemoveFile
            DeleteFile((LPCTSTR)saList[i]);
        }

        // add entropy to the pool, number of files created
        randomAddEntropy((E_PUINT8)&iSize, sizeof(E_INT32));

        eraserSafeAssign(context, context->m_uProgressPercent, 100);
        eraserUpdateNotify(context);

        // clean up
        ZeroMemory(uTestBuffer, maxMFTRecordSize);

        return true;
    }

    return false;
}

bool
wipeNTFSFileEntries(CEraserContext *context)
{
    if (!isFileSystemNTFS(context->m_piCurrent)) {
        return false;
    }

    NTFSContext ntc;
    bool bResult = false;

    if (initAndOpenVolume(ntc, context->m_strData[0])) {
        IO_STATUS_BLOCK ioStatus;
        NTSTATUS status;
        NTFS_VOLUME_DATA_BUFFER nvd;

        // find out MFT size
        status = ntc.NtFsControlFile(ntc.m_hVolume, NULL, NULL, 0, &ioStatus,
                                     FSCTL_GET_VOLUME_INFORMATION,
                                     NULL, 0, &nvd,
                                     sizeof(NTFS_VOLUME_DATA_BUFFER));

        if (status == STATUS_SUCCESS) {
            const E_UINT32  uMaxFilesPerFolder = 3000;
            const E_UINT32  uMFTPollInterval = 20;
            const E_UINT32  uFileNameLength = _MAX_FNAME - 14 /*strFolder.GetLength() + 1*/ - 8 - 1;

            HANDLE          hFile, hFind;
            WIN32_FIND_DATA wfdData;
            E_UINT32        uSpeed, uTickCount;
            E_UINT32        i, j;
            E_UINT32        uFiles = 0, uFolders = 0;
            E_UINT32        uEstimate;
            LARGE_INTEGER   uOriginalMFTSize = nvd.MftValidDataLength;
            CString         strPath, strFolder;
            CStringArray    saFolders;
            TCHAR           szPrefix[uFileNameLength + 1];

            try {
                // prefix each name with a couple of zeros
                for (i = 0; i < uFileNameLength; i++) {
                    szPrefix[i] = '0';
                }
                szPrefix[uFileNameLength] = 0;

                // approximate the number of files we need to create (at least 1)
				uEstimate = max(1, (E_UINT32)(nvd.MftValidDataLength.QuadPart / nvd.BytesPerFileRecordSegment));

                if (uEstimate > context->m_uProgressFiles) {
                    uEstimate -= context->m_uProgressFiles;
                }

                // this may take a while, so we'll try to estimate the time
                context->m_uProgressFlags |= eraserDispTime;
                context->m_uProgressStartTime = GetTickCount();

                eraserBeginNotify(context);

                do {
                    if (uFiles % uMaxFilesPerFolder == 0) {
                        strFolder.Format(_T("%c:\\%s%04X"), context->m_strData[0],
                                    ERASER_TEMP_DIRECTORY_NTFS_ENTRIES, uFolders++);

                        // remove possibly existing folder
                        eraserRemoveFolder((LPVOID)(LPCTSTR)strFolder, (E_UINT16)strFolder.GetLength(),
                                           ERASER_REMOVE_RECURSIVELY);

                        // create new directory
                        if (CreateDirectory((LPCTSTR)strFolder, NULL)) {
                            saFolders.Add(strFolder + _T("\\"));
                        } else {
                            eraserAddError(context, IDS_ERROR_TEMPFILE);
                            break;
                        }
                    }

                    strPath.Format(_T("%s\\%s%08X"), strFolder, szPrefix, uFiles);

                    hFile = CreateFile((LPCTSTR)strPath, GENERIC_WRITE, 0, NULL, CREATE_NEW, 0, 0);

                    if (hFile != INVALID_HANDLE_VALUE) {
                        uFiles++;
                        CloseHandle(hFile);
                    } else {
                        eraserAddError(context, IDS_ERROR_TEMPFILE);
                        break;
                    }

                    if (uFiles % uMFTPollInterval == 0 || eraserInternalTerminated(context)) {
                        status = ntc.NtFsControlFile(ntc.m_hVolume, NULL, NULL, 0, &ioStatus,
                                                     FSCTL_GET_VOLUME_INFORMATION,
                                                     NULL, 0, &nvd,
                                                     sizeof(NTFS_VOLUME_DATA_BUFFER));

                        if (eraserInternalTerminated(context)) {
                            break;
                        } else {
                            uTickCount = GetTickCount();
                            if (uTickCount > context->m_uProgressStartTime) {
                                uSpeed = (uFiles * 1000) / (uTickCount - context->m_uProgressStartTime);

                                if (uSpeed > 0) {
                                    context->m_uProgressTimeLeft = ((uEstimate - uFiles) / uSpeed);
                                } else {
                                    context->m_uProgressTimeLeft = 0;
                                }
                            }

                            eraserSafeAssign(context, context->m_uProgressPercent,
                                (E_UINT8)min(100, (uFiles * 100) / uEstimate));
                            setTotalProgress(context);
                            eraserUpdateNotify(context);
                        }
                    }
				} while (status == STATUS_SUCCESS && nvd.MftValidDataLength.QuadPart <= uOriginalMFTSize.QuadPart);
					

                // if we managed to increase MFT size, slack space was filled
				if (nvd.MftValidDataLength.QuadPart > uOriginalMFTSize.QuadPart) {
                    bResult = true;
                    eraserSafeAssign(context, context->m_uProgressPercent, 100);
                    setTotalProgress(context);

                    // add entropy to the pool, number of files created
                    randomAddEntropy((E_PUINT8)&uFiles, sizeof(E_UINT32));
                }

                if (!eraserInternalTerminated(context)) {
                    // show progress bar while removing files - may take a while
                    eraserProgressSetMessage(context, ERASER_MESSAGE_REMOVING);
                    eraserBeginNotify(context);
                }

                // remove temporary files
                for (i = 0, j = 0; i < uFolders; i++) {
                    strFolder = saFolders[i];
                    hFind = FindFirstFile((LPCTSTR)(strFolder + _T("*")), &wfdData);

                    if (hFind != INVALID_HANDLE_VALUE) {
                        do {
                            if (!bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY)) {
                                // eraserRemoveFile is way too slow
                                DeleteFile((LPCTSTR)(strFolder + wfdData.cFileName));

                                if (!eraserInternalTerminated(context)) {
                                    eraserSafeAssign(context, context->m_uProgressPercent, (E_UINT8)((++j * 100) / uFiles));
                                    eraserUpdateNotify(context);
                                }
                            }
                        }
                        while (FindNextFile(hFind, &wfdData));

                        VERIFY(FindClose(hFind));
                    }

                    // remove the folder
                    if (eraserError(eraserRemoveFolder((LPVOID)(LPCTSTR)strFolder,
                            (E_UINT16)strFolder.GetLength(), ERASER_REMOVE_RECURSIVELY))) {
                        context->m_saFailed.Add(strFolder);
                    }
                }

                if (!eraserInternalTerminated(context)) {
                    // and we're done
                    eraserSafeAssign(context, context->m_uProgressPercent, 100);
                    eraserUpdateNotify(context);
                }
            } catch (CException *e) {
                handleException(e, context);
                bResult = false;
            }
        }
    }

    if (!bResult) {
        eraserAddError1(context, IDS_ERROR_DIRENTRIES, (LPCTSTR)context->m_strData);
    }

    return bResult;
}

bool
findAlternateDataStreams(CEraserContext *context, LPCTSTR szFile, DataStreamArray& streams)
{
    if (!isFileSystemNTFS(context->m_piCurrent)) {
        return false;
    }

    NTFSContext ntc;
    if (initEntryPoints(ntc)) {
        bool bResult = false;
        HANDLE hFile;
        PFILE_STREAM_INFORMATION psi = 0;
        NTSTATUS status = STATUS_INVALID_PARAMETER;
        WCHAR wszStreamName[MAX_PATH];
        IO_STATUS_BLOCK ioStatus;
        DataStream ads;

        hFile = CreateFile(szFile,
                           GENERIC_READ,
                           FILE_SHARE_READ | FILE_SHARE_WRITE,
                           NULL,
                           OPEN_EXISTING,
                           0, 0);

        if (hFile != INVALID_HANDLE_VALUE) {
            // use write buffer, should be large enough
            status = ntc.NtQueryInformationFile(hFile, &ioStatus,
                                                (PFILE_STREAM_INFORMATION)context->m_puBuffer,
                                                ERASER_DISK_BUFFER_SIZE,
                                                FileStreamInformation);

            if (NT_SUCCESS(status)) {
                try {
                    psi = (PFILE_STREAM_INFORMATION)context->m_puBuffer;

                    do {
                        memcpy(wszStreamName, psi->Name, psi->NameLength);
                        wszStreamName[psi->NameLength / sizeof(WCHAR)] = 0;

                        if (_wcsicmp(wszStreamName, L"::$DATA")) {
                            // name of the alternate data stream
                            unicodeToCString(wszStreamName, ads.m_strName);
                            ads.m_strName = szFile + ads.m_strName;

                            ads.m_uSize = psi->Size.QuadPart;
                            streams.Add(ads);
                        }

                        if (psi->NextEntry) {
                            psi = (PFILE_STREAM_INFORMATION)((E_PUINT8)psi + psi->NextEntry);
                        } else {
                            psi = 0;
                        }
                    } while (psi);

                    bResult = true;
                } catch (...) {
                    ASSERT(0);
                    bResult = false;
                }
            }

            CloseHandle(hFile);
            return bResult;
        }
    }
    return false;
}