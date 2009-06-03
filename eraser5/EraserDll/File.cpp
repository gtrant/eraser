// File.cpp
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
#include "resource.h"
#include "EraserDll.h"
#include "Common.h"
#include "File.h"
#include "NTFS.h"
#include "io.h"


static inline void
formatError(CString& strError)
{
    strError.Empty();
    if (GetLastError() != NO_ERROR) {
        LPVOID lpMsgBuf;

        FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
                      NULL,
                      GetLastError(),
                      MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
                      (LPTSTR) &lpMsgBuf,
                      0,
                      NULL);

        strError = (LPCTSTR) lpMsgBuf;

        // remove CRLFs
        strError.TrimRight();
        E_INT32 iPos = strError.Find(_T("\r\n"));

        while (iPos != -1) {
            strError = strError.Left(iPos) + _T(" ") +
                       strError.Right(strError.GetLength() - iPos - 2);

            iPos = strError.Find(_T("\r\n"));
        }

        // Free the buffer.
        LocalFree(lpMsgBuf);
    }
}

/* returns 0 on error, 1 on success. this function set the MACE values based on 
the input from the FILE_BASIC_INFORMATION structure */
DWORD SetFileMACE(HANDLE file, FILE_BASIC_INFORMATION fbi) {

	HMODULE ntdll = NULL;
	IO_STATUS_BLOCK iostatus;
	pNtSetInformationFile NtSetInformationFile = NULL;

	ntdll = LoadLibrary(_T("ntdll.dll"));
	if (ntdll == NULL) {
		return 0;
	}

	NtSetInformationFile = (pNtSetInformationFile)GetProcAddress(ntdll, "NtSetInformationFile");
	if (NtSetInformationFile == NULL) {
		return 0;
	}

	if (NtSetInformationFile(file, &iostatus, &fbi, sizeof(FILE_BASIC_INFORMATION), FileBasicInformation) < 0) {
		return 0;
	}
	
	/* clean up */
	FreeLibrary(ntdll);

	return 1;
}

/* returns the handle on success or NULL on failure. this function opens a file and returns
the FILE_BASIC_INFORMATION on it. */
HANDLE RetrieveFileBasicInformation(TCHAR *filename, FILE_BASIC_INFORMATION *fbi) {
	
	HANDLE file = NULL;
	HMODULE ntdll = NULL;
	pNtQueryInformationFile NtQueryInformationFile = NULL;
	IO_STATUS_BLOCK iostatus;
	
	file = CreateFile(filename, FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, 0, NULL, OPEN_EXISTING, 0, NULL);
	if (file == INVALID_HANDLE_VALUE) {
		return 0;
	}

	/* load ntdll and retrieve function pointer */
	ntdll = LoadLibrary(_T("ntdll.dll"));
	if (ntdll == NULL) {
		CloseHandle(file);
		return 0;
	}

	/* retrieve current timestamps including file attributes which we want to preserve */
	NtQueryInformationFile = (pNtQueryInformationFile)GetProcAddress(ntdll, "NtQueryInformationFile");
	if (NtQueryInformationFile == NULL) {
		CloseHandle(file);
		return 0;
	}

	/* obtain the current file information including attributes */
	if (NtQueryInformationFile(file, &iostatus, fbi, sizeof(FILE_BASIC_INFORMATION), FileBasicInformation) < 0) {
		CloseHandle(file);
		return 0;
	}

	/* clean up */
	FreeLibrary(ntdll);

	return file;
}

static int RetrieveFileBasicInformationFromHandle(HANDLE file, FILE_BASIC_INFORMATION *fbi) {
	
	HMODULE ntdll = NULL;
	pNtQueryInformationFile NtQueryInformationFile = NULL;
	IO_STATUS_BLOCK iostatus;
	
	/* load ntdll and retrieve function pointer */
	ntdll = LoadLibrary(_T("ntdll.dll"));
	if (ntdll == NULL) {
		return 0;
	}

	/* retrieve current timestamps including file attributes which we want to preserve */
	NtQueryInformationFile = (pNtQueryInformationFile)GetProcAddress(ntdll, "NtQueryInformationFile");
	if (NtQueryInformationFile == NULL) {
		return 0;
	}

	/* obtain the current file information including attributes */
	if (NtQueryInformationFile(file, &iostatus, fbi, sizeof(FILE_BASIC_INFORMATION), FileBasicInformation) < 0) {
		return 0;
	}

	/* clean up */
	FreeLibrary(ntdll);

	return 0;
}


// returns 0 on error, 1 on success. this function converts a SYSTEMTIME structure to a LARGE_INTEGER
DWORD ConvertLocalTimeToLargeInteger(SYSTEMTIME localsystemtime, LARGE_INTEGER *largeinteger) {

	// the local time is stored in the system time structure argument which should be from the user
	// input. the user inputs the times in local time which is then converted to utc system time because
	// ntfs stores all timestamps in utc, which is then converted to a large integer
	
	// MSDN recommends converting SYSTEMTIME to FILETIME via SystemTimeToFileTime() and
	// then copying the values in FILETIME to a ULARGE_INTEGER structure.

	FILETIME filetime;
	FILETIME utcfiletime;

	// convert the SYSTEMTIME structure to a FILETIME structure
    if (SystemTimeToFileTime(&localsystemtime, &filetime) == 0) {
		return 0;
	}

	// convert the local file time to UTC
	if (LocalFileTimeToFileTime(&filetime, &utcfiletime) == 0) {
		return 0;
	}

	/* copying lowpart from a DWORD to DWORD, and copying highpart from a DWORD to a LONG.
	potential data loss of upper values 2^16, but acceptable bc we wouldn't be able to set 
	this high even if we wanted to because NtSetInformationFile() takes a max of what's
	provided in LARGE_INTEGER */
	largeinteger->LowPart = utcfiletime.dwLowDateTime;
	largeinteger->HighPart = utcfiletime.dwHighDateTime;	

	return 1;
}

/* returns 0 on error, 1 on success. this function converts a LARGE_INTEGER to a SYSTEMTIME structure */
DWORD ConvertLargeIntegerToLocalTime(SYSTEMTIME *localsystemtime, LARGE_INTEGER largeinteger) {

	FILETIME filetime;
	FILETIME localfiletime;

	filetime.dwLowDateTime = largeinteger.LowPart;
	filetime.dwHighDateTime = largeinteger.HighPart;

	if (FileTimeToLocalFileTime(&filetime, &localfiletime) == 0) {
		return 0;
	}

    if (FileTimeToSystemTime(&localfiletime, localsystemtime) == 0) {
		return 0;
	}

	return 1;
}
// takes a file a sets the time values to the minimum possible value, return 1 on success or 0 on failure
DWORD SetMinimumTimeValues(HANDLE file) {

	FILE_BASIC_INFORMATION fbi;
	SYSTEMTIME userinputtime;

	// open the file and retrieve information
	RetrieveFileBasicInformationFromHandle(file, &fbi);
	
	userinputtime.wYear = 1980;
	userinputtime.wMonth = 1;
	userinputtime.wDayOfWeek = 1;
	userinputtime.wDay = 1;
	userinputtime.wHour = 1;
	userinputtime.wMinute = 1;
	userinputtime.wSecond = 1;
	userinputtime.wMilliseconds = 1;
	if ((ConvertLocalTimeToLargeInteger(userinputtime, &fbi.ChangeTime) == 0) || (ConvertLocalTimeToLargeInteger(userinputtime, &fbi.CreationTime) == 0) ||
		(ConvertLocalTimeToLargeInteger(userinputtime, &fbi.LastAccessTime) == 0) || (ConvertLocalTimeToLargeInteger(userinputtime, &fbi.LastWriteTime) == 0)) {
		return 0;
	}	
	if (SetFileMACE(file, fbi) == 0) { return 0; }

	return 1;
}
bool
resetDate(HANDLE hFile)
{
    // changes all file dates to January 1st, 1980 0:00
    SYSTEMTIME  stTime;
    FILETIME    ftLocalTime;
    FILETIME    ftTime;
	
    stTime.wYear            = 1980;
    stTime.wMonth           = 1;
    stTime.wDayOfWeek       = 0;
    stTime.wDay             = 1;
    stTime.wHour            = 0;
    stTime.wMinute          = 0;
    stTime.wSecond          = 0;
    stTime.wMilliseconds    = 0;

    SystemTimeToFileTime(&stTime, &ftLocalTime);
    LocalFileTimeToFileTime(&ftLocalTime, &ftTime);
    SetFileTime(hFile, &ftTime, &ftTime, &ftTime);
	
	// flush to disk
    FlushFileBuffers(hFile);
	return (SetMinimumTimeValues(hFile)==0);
}

static inline bool
wipeDataStreams(CEraserContext *context, DataStreamArray& streams)
{
    bool bResult = false;
    E_INT32 lHigh = 0;
    INT_PTR iSize = streams.GetSize();

    for (E_INT32 i = 0; i < iSize; i++) {
        context->m_hFile = CreateFile((LPCTSTR)streams[i].m_strName,
                                      GENERIC_READ | GENERIC_WRITE,
                                      (context->m_uTestMode) ? FILE_SHARE_READ | FILE_SHARE_WRITE : 0,
                                      NULL,
                                      OPEN_EXISTING,
                                      FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH,
                                      NULL);

        bResult = (context->m_hFile != INVALID_HANDLE_VALUE);
        if (!bResult)
            context->HandleError(static_cast<LPCTSTR>(streams[i].m_strName));
        else {
            try {
                // set display name
                eraserSafeAssign(context, context->m_strData, streams[i].m_strName);

                // data stream size
                if (streams[i].m_bDefault) {
                    // we don't have the size for the default stream, get it
                    context->m_uiFileSize.LowPart =
                        GetFileSize(context->m_hFile, &context->m_uiFileSize.HighPart);

                    if (context->m_uiFileSize.LowPart == (E_UINT32)-1 &&
                        GetLastError() != NO_ERROR) {
                        // GetFileSize failed
                        bResult = false;
                    }
                } else {
                    context->m_uiFileSize.QuadPart = streams[i].m_uSize;
                }

                if (bResult) {
                    if (context->m_uiFileSize.QuadPart > 0) {
                        // mmm, entropy.
                        randomAddEntropy((E_PUINT8)&context->m_uiFileSize, sizeof(ULARGE_INTEGER));

                        // we will also wipe the slack space at the end of the last cluster or if
                        // cluster size isn't available, writes must at least be sector aligned
                        E_UINT64 uTotal = fileSizeToArea(context, context->m_uiFileSize.QuadPart);

                        context->m_uClusterSpace = (E_UINT32)(uTotal - context->m_uiFileSize.QuadPart);
                        context->m_uiFileSize.QuadPart = uTotal;

                        // set progress info
                        eraserProgressStartEstimate(context, context->m_uiFileSize.QuadPart);

                        // and overwrite
                        bResult = context->m_lpmMethod->m_pwfFunction(context);

                        if (bResult) {
                            // set stream length to zero so allocated clusters cannot be trailed
                            lHigh = 0L;
                            SetFilePointer(context->m_hFile, 0, &lHigh, FILE_BEGIN);
                            SetEndOfFile(context->m_hFile);

                            resetDate(context->m_hFile);
                        }
                    } else {
                        // nothing to erase
                        resetDate(context->m_hFile);
                    }
                }

                CloseHandle(context->m_hFile);

            } catch (CException *e) {
                handleException(e, context);

                bResult = false;
                SetLastError(ERROR_GEN_FAILURE);
                CloseHandle(context->m_hFile);
            }
        }

        if (!bResult) {
            // if we were terminated while erasing a stream, the stream
            // that wasn't completed will be added to the error list
            CString strError;
            formatError(strError);

            if (!strError.IsEmpty()) {
                strError = _T(" (") + strError + _T(")");
            }
            strError = streams[i].m_strName + strError;
            context->m_saFailed.Add(strError);

            if (iSize > 1) {
                eraserAddError1(context, IDS_ERROR_ADS, streams[i].m_strName);
            }

            break;
        }
    }

    return bResult;
}

bool
wipeFile(CEraserContext *context)
{
    try {
        E_UINT32 uAttributes;
        DataStreamArray streams;
        DataStream defaultStream;

		if (isWindowsNT) {
			DWORD cryptStatus = 0;
			FileEncryptionStatus (context->m_strData, &cryptStatus);
			if (cryptStatus == FILE_IS_ENCRYPTED) { 
				DecryptFile(context->m_strData,0);
			}
		}

        // default stream
        defaultStream.m_strName = context->m_strData;
        defaultStream.m_bDefault = true;

        // get file attributes
        uAttributes = GetFileAttributes(defaultStream.m_strName);

        // if the file does not exist or an error has occurred
        if (uAttributes == (E_UINT32)-1) {
            CString strError;
            formatError(strError);

            if (!strError.IsEmpty()) {
                strError = _T(" (") + strError + _T(")");
            }
            strError = defaultStream.m_strName + strError;
            context->m_saFailed.Add(strError);
            return false;
        }

        // ignore read-only
        SetFileAttributes(defaultStream.m_strName, FILE_ATTRIBUTE_NORMAL);

        if (isWindowsNT &&
            (bitSet(uAttributes, FILE_ATTRIBUTE_COMPRESSED) ||
             bitSet(uAttributes, FILE_ATTRIBUTE_ENCRYPTED)  ||
             bitSet(uAttributes, FILE_ATTRIBUTE_SPARSE_FILE))) {
            // requires special processing
            E_UINT32 uResult = wipeCompressedFile(context);

            if (uResult == WCF_FAILURE) {
                context->m_saFailed.Add(defaultStream.m_strName);
                return false;
            } else if (uResult == WCF_NOACCESS) {
                CString strError;
                strError.Format(_T("%s (Administrator privileges required)"), defaultStream.m_strName);

                context->m_saFailed.Add(strError);
                return false;
            } else if (uResult == WCF_SUCCESS) {
                return true;
            }

            // if file was not really compressed, erase normally
        }

        // search for alternate data streams (NTFS only)
        if (isWindowsNT && bitSet(context->m_lsSettings.m_uItems, fileAlternateStreams)) {
            findAlternateDataStreams(context, defaultStream.m_strName, streams);
        }

		// add the default (unnamed) data stream
        streams.Add(defaultStream);

        if (wipeDataStreams(context, streams)) {
            return eraserOK(eraserRemoveFile((LPVOID)(LPCTSTR)defaultStream.m_strName,
                                             (E_UINT16)defaultStream.m_strName.GetLength()));
        }
    } catch (CException *e) {
        handleException(e, context);
    }

    return false;
}