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
        E_INT32 iPos = strError.Find("\r\n");

        while (iPos != -1) {
            strError = strError.Left(iPos) + " " +
                       strError.Right(strError.GetLength() - iPos - 2);

            iPos = strError.Find("\r\n");
        }

        // Free the buffer.
        LocalFree(lpMsgBuf);
    }
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
    
    return (SystemTimeToFileTime(&stTime, &ftLocalTime) &&
            LocalFileTimeToFileTime(&ftLocalTime, &ftTime) &&
            SetFileTime(hFile, &ftTime, &ftTime, &ftTime));
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
                strError = " (" + strError + ")";
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
                strError = " (" + strError + ")";
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
                strError.Format("%s (Administrator privileges required)", defaultStream.m_strName);

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
