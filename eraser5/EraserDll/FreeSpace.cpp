// FreeSpace.cpp
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2007 The Eraser Project
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
#include "resource.h"
#include "EraserDll.h"
#include "..\shared\FileHelper.h"
#include "..\shared\key.h"
#include "Common.h"
#include "File.h"
#include "FAT.h"
#include "NTFS.h"
#include "FreeSpace.h"
#include <winioctl.h>
#include <windows.h>
#include <stdio.h> 
#include <winbase.h>
#include <winnt.h>

// Windows 98 - Q188074
static const LPCTSTR ERASER_REGISTRY_FILESYSTEM
    = _T("System\\CurrentControlSet\\Control\\FileSystem");
static const LPCTSTR ERASER_REGISTRY_LOWDISKSPACE
    = _T("DisableLowDiskSpaceBroadcast");

#undef MAX_PATH
#define MAX_PATH 2048 //HACK: Some filenames under Vista can exceed the 260
                      //char limit. This will have to do for now.

static inline E_UINT32
disableLowDiskSpaceNotification(TCHAR szDrive)
{
    // disables the annoying warning Windows 98 displays when disk space is low
    if (!isWindowsNT) {
        CKey kReg;
        E_UINT32 uOldValue = 0;
        E_UINT32 uNewValue = (1 << (toupper((E_INT32)szDrive) - (E_INT32)'A'));

        if (kReg.Open(HKEY_LOCAL_MACHINE, ERASER_REGISTRY_FILESYSTEM)) {
            // save the previous value (if it exists)
            kReg.GetValue(uOldValue, ERASER_REGISTRY_LOWDISKSPACE, 0);
            kReg.SetValue(uNewValue, ERASER_REGISTRY_LOWDISKSPACE);

            kReg.Close();
        }
        return uOldValue;
    }

    return 0;
}

static inline void
restoreLowDiskSpaceNotification(E_UINT32 uOldValue)
{
    // restores the old value for the low disk space notification key
    if (!isWindowsNT) {
        CKey kReg;
        if (kReg.Open(HKEY_LOCAL_MACHINE, ERASER_REGISTRY_FILESYSTEM)) {
            if (uOldValue != 0) {
                kReg.SetValue(uOldValue, ERASER_REGISTRY_LOWDISKSPACE);
            } else {
                kReg.DeleteValue(ERASER_REGISTRY_LOWDISKSPACE);
            }
            kReg.Close();
        }
    }
}

static inline BOOL
uncompressFolder(LPCTSTR szFolder)
{
    // if folder is compressed, this uncompresses it
    if (isWindowsNT) {
        HANDLE hHandle = INVALID_HANDLE_VALUE;

        hHandle = CreateFile(szFolder, GENERIC_READ | GENERIC_WRITE, 0, NULL,
                             OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);

        if (hHandle != INVALID_HANDLE_VALUE) {
            BOOL   bSuccess    = FALSE;
            E_UINT16 uCompressed = COMPRESSION_FORMAT_NONE;
            E_UINT32 uReturned   = 0;

            bSuccess = DeviceIoControl(hHandle, FSCTL_GET_COMPRESSION, NULL, 0,
                                       &uCompressed, sizeof(E_UINT16), &uReturned,
                                       NULL);

            if (bSuccess && (uCompressed != COMPRESSION_FORMAT_NONE)) {
                // it is compressed - change attributes
                uCompressed = COMPRESSION_FORMAT_NONE;

                bSuccess = DeviceIoControl(hHandle, FSCTL_SET_COMPRESSION,
                                           &uCompressed, sizeof(E_UINT16),
                                           NULL, 0, &uReturned, NULL);
            }

            CloseHandle(hHandle);
            return bSuccess;
        }
    }

    return FALSE;
}

bool
getClusterAndSectorSize(LPCTSTR szDrive, E_UINT32& uCluster, E_UINT32& uSector)
{
    uCluster = 0;
    uSector = DEFAULT_SECTOR_SIZE;

    try {
        if (isWindowsNT) {
            // GetDiskFreeSpace will return correct values
            E_UINT32 uSecPerClus = 0;
            E_UINT32 uBytPerSec  = 0;
            E_UINT32 uFreeClus   = 0;
            E_UINT32 uClus       = 0;

            if (GetDiskFreeSpace(szDrive, &uSecPerClus, &uBytPerSec,
                                 &uFreeClus, &uClus)) {
                uSector = uBytPerSec;
                uCluster = (uSector * uSecPerClus);
            }
        } else {
            // GetDiskFreeSpace returns false results on drives larger than 2 GB
            return getFATClusterAndSectorSize(szDrive, uCluster, uSector);
        }
    } catch (...) {
        ASSERT(0);
    }

    return (uCluster > 0);
}

bool
getClusterSize(LPCTSTR szDrive, E_UINT32& uCluster)
{
    E_UINT32 uSector = 0;
    return getClusterAndSectorSize(szDrive, uCluster, uSector);
}

bool
getPartitionType(PARTITIONINFO& pi)
{
    TCHAR szFS[MAX_PATH];

    if (GetVolumeInformation(pi.m_szDrive, NULL, 0, NULL, NULL, NULL, szFS, MAX_PATH)) {
        if (_tcsicmp(szFS, _T("FAT32")) == 0) {
            pi.m_fsType = fsFAT32;
        } else if (_tcsicmp(szFS, _T("FAT")) == 0) {
            pi.m_fsType = fsFAT;
        } else if (_tcsicmp(szFS, _T("NTFS")) == 0) {
            pi.m_fsType = fsNTFS;
        } else {
            pi.m_fsType = fsUnknown;
        }
        return true;
    } else {
        return false;
    }
}

bool
getPartitionInformation(CEraserContext *context, TCHAR cDrive)
{
    cDrive = (TCHAR)toupper(cDrive);

    if (cDrive != context->m_piCurrent.m_szDrive[0] ||
        context->m_piCurrent.m_bLastSuccess == false) {

        // partition information wasn't cached
        context->m_piCurrent.m_szDrive[0] = cDrive;

        if (context->m_piCurrent.m_szDrive[0] < 'A' ||
            context->m_piCurrent.m_szDrive[0] > 'Z') {

            context->m_piCurrent.m_szDrive[0] = ' ';
            context->m_piCurrent.m_bLastSuccess = false;
        } else {
            // determine file system type
            context->m_piCurrent.m_bLastSuccess = getPartitionType(context->m_piCurrent);

            // cluster and sector size, but not if erasing files and user
            // doesn't want to touch cluster tip area

            if (context->m_edtDataType != ERASER_DATA_FILES ||
                bitSet(context->m_lsSettings.m_uItems, fileClusterTips)) {

                if (!getClusterAndSectorSize(context->m_piCurrent.m_szDrive,
                                             context->m_piCurrent.m_uCluster,
                                             context->m_piCurrent.m_uSector)) {
                    eraserAddError1(context, IDS_ERROR_CLUSTER, context->m_piCurrent.m_szDrive);
                    context->m_piCurrent.m_bLastSuccess = false;
                }
            }
        }
    }

    return context->m_piCurrent.m_bLastSuccess;
}

__declspec(dllexport) bool IsProcessElevated(HANDLE process)
{
	// under Vista if the user is not elevated write an error to the log and return
	OSVERSIONINFO verInfo;
	::ZeroMemory(&verInfo, sizeof(verInfo));
	verInfo.dwOSVersionInfoSize = sizeof(verInfo);
	if (GetVersionEx(&verInfo) && verInfo.dwMajorVersion >= 6)
	{
		HANDLE hToken = 0;
		TOKEN_ELEVATION_TYPE elevationType;
		DWORD returnSize = 0;
		OpenProcessToken(process, TOKEN_QUERY, &hToken);

		if (hToken)
		{
			bool infoResult = GetTokenInformation(hToken, TokenElevationType,
				&elevationType, sizeof(elevationType), &returnSize) != FALSE;
			CloseHandle(hToken);

			if (infoResult && elevationType == TokenElevationTypeLimited)	
				return false;
		}
	}

	return true;
}

static bool hasPrivileges(CEraserContext *context)
{
	if (!IsProcessElevated(GetCurrentProcess()))
	{
		context->m_saError.Add(_T("Erasing the Free Space of a drive requires elevation"));
		return false;
	}

	return true;
}

void
countFilesOnDrive(CEraserContext *context, const CString& strDrive, E_UINT32& uFiles, E_UINT32& uFolders)
{
    // counts the amount of files on drive (including subfolders)
    HANDLE          hFind;
    WIN32_FIND_DATA wfdData;
    CString         strRoot(strDrive);

    // make sure that the directory name ends with a backslash

    if (strRoot[strRoot.GetLength() - 1] != '\\') {
        strRoot += "\\";
    }

    hFind = FindFirstFile((LPCTSTR) (strRoot + _T("*")), &wfdData);

    if (hFind != INVALID_HANDLE_VALUE) {
        try {
            do {
                if (eraserInternalTerminated(context)) {
                    break;
                }

                // skip volume mount point
                if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_REPARSE_POINT)) {
                    continue;
                }
                if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY)) {
                    // skip "." and ".."
                    if (ISNT_SUBFOLDER(wfdData.cFileName)) {
                        continue;
                    }

                    uFolders++;

                    // recursive for subfolders
                    countFilesOnDrive(context, (strRoot + wfdData.cFileName), uFiles, uFolders);
                } else {
                    uFiles++;
                }
            } while (FindNextFile(hFind, &wfdData));
        } catch (...) {
            ASSERT(0);
        }

        VERIFY(FindClose(hFind));
    }
}

static bool
wipeClusterTipsRecursive(CEraserContext *context, SFCISFILEPROTECTED pSfcIsFileProtected)
{
    // wipes unused cluster tips of each file on the drive given by context->m_strData

    // the cluster size must have been set before calling this function
    if (context->m_piCurrent.m_uCluster == 0) {
        return false;
    }

    HANDLE hFind;
    WIN32_FIND_DATA wfdData;
    bool bCompleted = true;
    CString strDirectory = context->m_strData;

    // the directory name must end with a backslash
    if (strDirectory[strDirectory.GetLength() - 1] != '\\') {
        strDirectory += "\\";
    }

    hFind = FindFirstFile((LPCTSTR) (strDirectory + _T("*")), &wfdData);

    if (hFind != INVALID_HANDLE_VALUE) {
        WCHAR    szWideName[MAX_PATH];
        CString  strFile;
        E_UINT32 uAttributes;
        E_UINT64 ulTotalSize;
        FILETIME ftCreation, ftLastAccess, ftLastWrite;
        bool     bSuccess;
        bool     bIgnoreFile;

        do {
            if (eraserInternalTerminated(context)) {
                bCompleted = false;
                break;
            }

            if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY)) {
                // skip volume mount point
                if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_REPARSE_POINT)) {
                    strFile = strDirectory + wfdData.cFileName;
                    context->m_saFailed.Add(strFile + _T(" (Reparse point)"));
                    continue;
                }
                // skip "." and ".."
                if (ISNT_SUBFOLDER(wfdData.cFileName)) {
                    continue;
                }
                eraserSafeAssign(context, context->m_strData, strDirectory + wfdData.cFileName);

                // recursive for subfolders
                eraserBool(bCompleted, wipeClusterTipsRecursive(context, pSfcIsFileProtected));
            } else {
                // wipe slack space on the file
                strFile = strDirectory + wfdData.cFileName;
                eraserSafeAssign(context, context->m_strData, strFile);

                bIgnoreFile = false;

                // System File Protection - use only for Windows 2000 and later
                if (isWindowsNT && pSfcIsFileProtected) {
                    try {
#if defined(_UNICODE)
						wcscpy(szWideName, strFile);
#else

                        ansiToUnicode((LPCSTR)strFile, (LPWSTR)szWideName, 260);
#endif

                        // we skip protected files to avoid confusing the user
                        if (pSfcIsFileProtected(NULL, (LPCWSTR)szWideName)) {
                            context->m_saFailed.Add(strFile + _T(" (Protected File)"));

                            bIgnoreFile = true;
                            bCompleted = false;
                        }
                    } catch (...) {
                        ASSERT(0);
                    }
                }

                // wanna skip other files? add the code here then...

                if (!bIgnoreFile) {
                    eraserBeginNotify(context);

                    // save file attributes
                    uAttributes = GetFileAttributes((LPCTSTR)strFile);

                    // skip compressed (and encrypted) files as overwriting the cluster tips
                    // on them (or even opening) causes fragmentation and is useless anyway
                    bIgnoreFile = (isWindowsNT && uAttributes != (E_UINT32)-1 &&
                                   (bitSet(uAttributes, FILE_ATTRIBUTE_COMPRESSED) ||
                                    bitSet(uAttributes, FILE_ATTRIBUTE_ENCRYPTED)  ||
                                    bitSet(uAttributes, FILE_ATTRIBUTE_SPARSE_FILE)));

                    if (!bIgnoreFile) {
                        // change file attributes temporarily
                        SetFileAttributes((LPCTSTR)strFile, FILE_ATTRIBUTE_NORMAL);

                        context->m_hFile = CreateFile((LPCTSTR)strFile,
                                                      GENERIC_READ | GENERIC_WRITE,
                                                      (context->m_uTestMode) ?
                                                        FILE_SHARE_READ | FILE_SHARE_WRITE : 0,
                                                      NULL, OPEN_EXISTING,
                                                      FILE_FLAG_WRITE_THROUGH,
                                                      NULL);

                        bSuccess = (context->m_hFile != INVALID_HANDLE_VALUE);

                        if (bSuccess) {
                            // save dates
                            GetFileTime(context->m_hFile, &ftCreation, &ftLastAccess, &ftLastWrite);

                            // file size
                            context->m_uiFileSize.LowPart = GetFileSize(context->m_hFile, &context->m_uiFileSize.HighPart);

                            if (context->m_uiFileSize.LowPart == (E_UINT32)-1 && GetLastError() != NO_ERROR) {
                                bSuccess = false;
                            } else {
                                ulTotalSize = fileSizeToArea(context, context->m_uiFileSize.QuadPart);

                                // continue if there is something to overwrite
                                if (ulTotalSize > context->m_uiFileSize.QuadPart) {
                                    context->m_uiFileStart.QuadPart = context->m_uiFileSize.QuadPart;
                                    context->m_uiFileSize.QuadPart = ulTotalSize - context->m_uiFileSize.QuadPart;

                                    context->m_uClusterSpace = context->m_uiFileSize.LowPart;

                                    try {
                                        // overwrite
                                        bSuccess = context->m_lpmMethod->m_pwfFunction(context);
                                    } catch (...) {
                                        bSuccess = false;
                                    }

                                    // restore size
                                    SetFilePointer(context->m_hFile, context->m_uiFileStart.LowPart,
                                                   (LPLONG)&context->m_uiFileStart.HighPart, FILE_BEGIN);
                                    SetEndOfFile(context->m_hFile);
                                }
                            }

                            // restore dates
                            SetFileTime(context->m_hFile, &ftCreation, &ftLastAccess, &ftLastWrite);
                            CloseHandle(context->m_hFile);
                        }

                        // restore attributes
                        SetFileAttributes((LPCTSTR)strFile, uAttributes);

                        if (!bSuccess) {
                            context->m_saFailed.Add(strFile);
                            bCompleted = false;
                        }
                    }
                }

                // next file
                context->m_uProgressWipedFiles++;

                // set progress
                eraserSafeAssign(context, context->m_uProgressPercent,
                    (E_UINT8)((context->m_uProgressWipedFiles * 100) / context->m_uProgressFiles));
                setTotalProgress(context);
                eraserUpdateNotify(context);
            }
        } while (FindNextFile(hFind, &wfdData));

        VERIFY(FindClose(hFind));
    }

    return bCompleted;
}

bool
wipeClusterTips(CEraserContext *context)
{
	if (!hasPrivileges(context))
		return false;
	if (context->m_lpmMethod->m_nMethodID == FL2KB_METHOD_ID)
		return false;

    bool               bReturn = false;
    SFCISFILEPROTECTED pSfcIsFileProtected = 0;
    HINSTANCE          hInst = AfxLoadLibrary(ERASER_MODULENAME_SFC);

    if (hInst != NULL) {
        pSfcIsFileProtected = (SFCISFILEPROTECTED)GetProcAddress(hInst,
                                    ERASER_FUNCTIONNAME_SFCISFILEPROTECTED);
    }

    // reset progress
    context->m_uProgressWipedFiles = 0;

    try {
        bReturn = wipeClusterTipsRecursive(context, pSfcIsFileProtected);
    } catch (CException *e) {
        handleException(e, context);
        bReturn = false;
    }

    if (hInst != NULL) {
        AfxFreeLibrary(hInst);
    }

    return bReturn;
}


bool
wipeFreeSpace(CEraserContext *context)
{
	if (!hasPrivileges(context))
		return false;
	if (context->m_lpmMethod->m_nMethodID == FL2KB_METHOD_ID)
		return false;

    CString strFolder;

    try {
        strFolder.Format(_T("%s%s"), context->m_piCurrent.m_szDrive, ERASER_TEMP_DIRECTORY);

        // remove possibly existing folder
        eraserRemoveFolder((LPVOID)(LPCTSTR)strFolder, (E_UINT16)strFolder.GetLength(),
                           ERASER_REMOVE_RECURSIVELY);

        // create temporary directory for temporary files
        if (CreateDirectory((LPCTSTR)strFolder, NULL)) {
            bool bResult = false;

            try {
                // make sure NTFS folder isn't compressed
                uncompressFolder((LPCTSTR)strFolder);

                bResult = eraserOK(eraserGetFreeDiskSpace((LPVOID)context->m_piCurrent.m_szDrive,
                                   (E_UINT16)lstrlen(context->m_piCurrent.m_szDrive),
                                   &context->m_uiFileSize.QuadPart));

                if (bResult) {
                    CString  strTempFile;
                    TCHAR    szFileName[uShortFileNameLength + 1];
                    E_UINT32 uFileSize = (E_UINT32)fileSizeToArea(context, ERASER_MAX_FILESIZE);
                    E_UINT32 uFiles;
                    E_UINT32 uAccessFlags = FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING;

                    // calculate the amount of fixed size files needed
                    if (context->m_uiFileSize.QuadPart > (E_UINT64)uFileSize) {
                        uFiles = (E_UINT32)(context->m_uiFileSize.QuadPart / (E_UINT64)uFileSize);
                    } else {
                        uFiles = 1;
                    }

                    // disable "Low Disk Space Notification" - Q188074, I am not so
                    // convinced this works as advertized...
                    E_UINT32 uWarningValue =
                        disableLowDiskSpaceNotification(context->m_piCurrent.m_szDrive[0]);

                    // show continuous progress info as if we were wiping only one file
                    eraserProgressStartEstimate(context, context->m_uiFileSize.QuadPart);

                    // set file size
                    context->m_uiFileStart.QuadPart = 0;
                    context->m_uiFileSize.QuadPart = uFileSize;
                    context->m_uClusterSpace = 0;

                    for (E_UINT32 uCurrent = 1; uCurrent <= uFiles; uCurrent++) {
                        if (eraserInternalTerminated(context)) {
                            bResult = false;
                            break;
                        }

                        createRandomShortFileName(szFileName, (E_UINT16)uCurrent);
                        strTempFile.Format(_T("%s\\%s"), strFolder, szFileName);

                        // cannot disable buffering for the last file, its size may not be sector aligned
                        if (uCurrent == uFiles) {
                            uAccessFlags = FILE_ATTRIBUTE_NORMAL | FILE_FLAG_WRITE_THROUGH;
                        }

                        context->m_hFile = CreateFile(strTempFile,
                                                     GENERIC_WRITE, (context->m_uTestMode) ?
                                                        FILE_SHARE_READ | FILE_SHARE_WRITE : 0,
                                                     NULL, OPEN_ALWAYS, uAccessFlags, NULL);

                        if (context->m_hFile != INVALID_HANDLE_VALUE) {
                            // the size of the last file is the amount of space available
                            try {
                                if (uCurrent == uFiles) {
                                    if (eraserOK(eraserGetFreeDiskSpace((LPVOID)context->m_piCurrent.m_szDrive,
                                            (E_UINT16)lstrlen(context->m_piCurrent.m_szDrive),
                                            &context->m_uiFileSize.QuadPart))) {

                                        context->m_uProgressSize = UInt32x32To64((uFiles - 1), uFileSize) +
                                                                   context->m_uiFileSize.QuadPart;

                                        // set end of file
                                        SetFilePointer(context->m_hFile, context->m_uiFileSize.LowPart,
                                                       (LPLONG)&context->m_uiFileSize.HighPart, FILE_BEGIN);
                                        SetEndOfFile(context->m_hFile);

                                        // overwrite
                                        bResult = context->m_lpmMethod->m_pwfFunction(context);
                                    } else {
                                        bResult = false;
                                        eraserAddError1(context, IDS_ERROR_FREESPACE,
                                            (LPCTSTR)context->m_piCurrent.m_szDrive);
                                        context->m_saFailed.Add(context->m_strData);
                                    }
                                } else {
                                    // overwrite
                                    bResult = context->m_lpmMethod->m_pwfFunction(context);
                                }
                            } catch (CException *e) {
                                handleException(e, context);
                                bResult = false;
                            }

                            resetDate(context->m_hFile);
                            CloseHandle(context->m_hFile);

                            // don't add an error to the list if we were terminated
                            if (!bResult && !eraserInternalTerminated(context)) {
                                context->m_saFailed.Add(strTempFile);
                            }
                        } else {
                            eraserAddError(context, IDS_ERROR_TEMPFILE);
                            bResult = false;
                        }

                        // if something failed, give up
                        if (!bResult) {
                            break;
                        }
                    }

                    // wipe unused space from MFT if an NTFS drive
                    if (bResult && isWindowsNT && isFileSystemNTFS(context->m_piCurrent)) {
                        increaseTotalProgressPercent(context);
                        eraserBool(bResult, wipeMFTRecords(context));
                    }

                    // restore the "Low Disk Space Notification" value
                    restoreLowDiskSpaceNotification(uWarningValue);
                } else {
                    eraserAddError1(context, IDS_ERROR_FREESPACE,
                                    (LPCTSTR)context->m_piCurrent.m_szDrive);
                    context->m_saFailed.Add(context->m_strData);
                }
            } catch (CException *e) {
                handleException(e, context);
                bResult = false;
            }

            // remove temporary directory and set error if failed
            if (eraserError(eraserRemoveFolder((LPVOID)(LPCTSTR)strFolder,
                    (E_UINT16)strFolder.GetLength(), ERASER_REMOVE_RECURSIVELY))) {

                eraserAddError1(context, IDS_ERROR_DIRECTORY_REMOVE,
                                (LPCTSTR)context->m_piCurrent.m_szDrive);
                context->m_saFailed.Add(strFolder);
                bResult = false;
            }

            return bResult;
        } else {
            eraserAddError1(context, IDS_ERROR_DIRECTORY,
                            (LPCTSTR)context->m_piCurrent.m_szDrive);
            context->m_saFailed.Add(context->m_strData);
        }
    } catch (CException *e) {
        handleException(e, context);
    }

    return false;
}