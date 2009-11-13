// Common.h
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

#ifndef COMMON_H
#define COMMON_H

// global variable definition
//
#ifdef GLOBAL_VARIABLES_HERE
    #define GLOBALVAR
    #define GLOBALINIT1(x)    (x)
    #define GLOBALINIT2(x, y) (x, y)
#else
    #define GLOBALVAR extern
    #define GLOBALINIT1(x)
    #define GLOBALINIT2(x, y)
#endif

#include "random.h"
#include "eraserdllinternal.h"
// the context array
//
GLOBALVAR CEraserContext *eraserContextArray[ERASER_MAX_CONTEXT + 1];
// this is so the library can be accessed from multiple threads
GLOBALVAR CCriticalSection csContextArray;

// helpers for context access control
#define eraserContextArrayAccess() \
    eraserTraceLock("eraserContextArrayAccess\n"); \
    CSingleLock sl(&csContextArray, TRUE)
#define eraserContextArrayRelease() \
    eraserTraceLock("eraserContextArrayRelease\n"); \
    sl.Unlock()
#define eraserContextArrayRelock() \
    eraserTraceLock("eraserContextArrayRelock\n"); \
    sl.Lock()

// initialization control
//
GLOBALVAR CCriticalSection csReferenceCount;
GLOBALVAR E_UINT16 uReferenceCount GLOBALINIT1(0);
GLOBALVAR CEvent evLibraryInitialized GLOBALINIT2(FALSE, TRUE);
const LPTSTR strEraserMutex = _T("Eraser-D309F296-B70C-473d-B2DE-2A1F9C7C9FB1");

// helpers
#define eraserIsLibraryInit() \
    (WaitForSingleObject(evLibraryInitialized, 0) == WAIT_OBJECT_0)

#define eraserLibraryInit() \
    evLibraryInitialized.SetEvent(); \
    csReferenceCount.Lock(); \
    uReferenceCount++; \
	if (uReferenceCount == 1) \
	{ \
		SECURITY_DESCRIPTOR sc; \
		InitializeSecurityDescriptor(&sc, SECURITY_DESCRIPTOR_REVISION); \
		SetSecurityDescriptorDacl(&sc, TRUE, NULL, FALSE); \
		\
		SECURITY_ATTRIBUTES attr; \
		attr.nLength = sizeof(attr); \
		attr.lpSecurityDescriptor = &sc; \
		attr.bInheritHandle = FALSE; \
		\
		CreateMutex(&attr, TRUE, strEraserMutex); \
		CreateMutex(&attr, TRUE, CString(_T("Global\\")) + strEraserMutex); \
	} \
    csReferenceCount.Unlock()

#define eraserLibraryUninit() \
    csReferenceCount.Lock(); \
    if (uReferenceCount > 0) { \
        uReferenceCount--; \
    } \
    if (uReferenceCount == 0) { \
		CMutex localMutex(FALSE, _T("Eraser-D309F296-B70C-473d-B2DE-2A1F9C7C9FB1")); \
		CMutex globalMutex(FALSE, _T("Global\\Eraser-D309F296-B70C-473d-B2DE-2A1F9C7C9FB1")); \
		localMutex.Unlock(); \
		globalMutex.Unlock(); \
        evLibraryInitialized.ResetEvent(); \
    } \
    csReferenceCount.Unlock()
#define eraserLibraryUnlock() \
    csReferenceCount.Lock(); \
    uReferenceCount = 0; \
    evLibraryInitialized.ResetEvent(); \
    csReferenceCount.Unlock()


// other global variables
//
GLOBALVAR bool isWindowsNT;
bool ERASER_API IsWindowsNT();

const E_UINT16 uExceptionBufferSize = 127;
GLOBALVAR TCHAR szExceptionBuffer[uExceptionBufferSize];


// safe characters for filenames (excluded some that are not that so common)
//
const E_UINT16 ERASER_SAFEARRAY_SIZE = 36;
const LPCTSTR ERASER_SAFEARRAY = _T("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");

inline void createRandomFileName(LPTSTR szFileName, E_UINT16 uNameLength, E_UINT16 uExtLength, E_UINT16 uCounter)
{
    // szFileName needs to be at least (uNameLength + 1 + uExtLength + 1) bytes long
    try {
        E_UINT32 uLength = uNameLength;

        if (uExtLength > 0) {
            uLength += uExtLength + 1;
        }

        if (uLength >= MAX_PATH) {
            uLength = MAX_PATH - 1;
        }

        E_UINT8 uRandomArray[MAX_PATH];
        isaacFill(uRandomArray, MAX_PATH);

        for (E_UINT32 uIndex = 0; uIndex < uLength; uIndex++) {
            szFileName[uIndex] = ERASER_SAFEARRAY[uRandomArray[uIndex] % ERASER_SAFEARRAY_SIZE];
        }

        ZeroMemory(uRandomArray, MAX_PATH);

        if (uCounter > 0 && uNameLength >= 4) {
            _sntprintf(&szFileName[uNameLength - 4], 4, _T("%04X"), uCounter);
        }

        if (uExtLength > 0) {
            szFileName[uNameLength] = '.';
        }

        szFileName[uLength] = 0;
    } catch (...) {
        ASSERT(0);
    }
}

const E_UINT16 uShortFileNameLength = 8 + 1 + 3;
#define createRandomShortFileName(szFileName, uCounter) \
    createRandomFileName((szFileName), 8, 3, (uCounter))


// common helpers for all wipe functions to handle notification and
// statistics
//
inline void
countTotalProgressTasks(CEraserContext *context)
{
    // erasing unused disk space is divided to three steps when
    // it comes to showing total progress

    context->m_uProgressTasks = 0;
    if (bitSet(context->m_lsSettings.m_uItems, diskClusterTips)) {
        context->m_uProgressTasks++;
    }
    if (bitSet(context->m_lsSettings.m_uItems, diskFreeSpace)) {
        context->m_uProgressTasks++;
        if (isWindowsNT && isFileSystemNTFS(context->m_piCurrent)) {  // MFT records
            context->m_uProgressTasks++;
        }
    }
    if (bitSet(context->m_lsSettings.m_uItems, diskDirEntries)) {
        context->m_uProgressTasks++;
    }
    if (context->m_uProgressTasks < 1) {
        context->m_uProgressTasks = 1;
    }
}

#pragma warning(disable : 4244)

inline void
increaseTotalProgressPercent(CEraserContext *context)
{
    // one task has been completed, increase m_uProgressTaskPercent
    if (context->m_uProgressTasks > 1) {
        context->m_uProgressTaskPercent += 100 / context->m_uProgressTasks;
    }
}

#pragma warning(default : 4244)

inline void
setTotalProgress(CEraserContext *context)
{
    eraserContextAccess(context);

    if (context->m_edtDataType == ERASER_DATA_FILES) {
        context->m_uProgressTotalPercent =
            (E_UINT8)( ((context->m_uProgressWipedFiles * 100) / context->m_uProgressFiles) +
                       (context->m_uProgressPercent / context->m_uProgressFiles));
    } else {
        context->m_uProgressTotalPercent =
            (E_UINT8)( ((context->m_uProgressWipedDrives * 100 + context->m_uProgressTaskPercent) /
                            context->m_uProgressDrives) +
                       (context->m_uProgressPercent / (context->m_uProgressTasks * context->m_uProgressDrives)));
    }
}

inline void
postStartNotification(CEraserContext *context)
{
    // send update only when starting the overwriting
    if (!bitSet(context->m_uProgressFlags, progressCustom) && context->m_uProgressWiped == 0) {
        eraserBeginNotify(context);
    }
}

inline void
postUpdateNotification(CEraserContext *context, E_UINT16 passes)
{
    if (!bitSet(context->m_uProgressFlags, progressCustom)) {
        eraserContextAccess(context);

        context->m_uProgressPercent =
            (E_UINT8)(((E_UINT64)(context->m_uProgressWiped * 100)) /
                     ((E_UINT64)(context->m_uProgressSize * passes)));

        setTotalProgress(context);

        E_UINT32 uTickCount = GetTickCount();
        if (uTickCount > context->m_uProgressStartTime) {
            E_UINT64 uSpeed =
                context->m_uProgressWiped / (E_UINT64)(uTickCount - context->m_uProgressStartTime);

            if (uSpeed > 0) {
                context->m_uProgressTimeLeft =
                    (E_UINT32)(((context->m_uProgressSize * passes) - context->m_uProgressWiped) /
                             (uSpeed * 1000));
            } else {
                context->m_uProgressTimeLeft = 0;
            }
        }

        eraserUpdateNotifyNoAccess(context);
    }
}

inline void
setEndStatistics(CEraserContext *context, E_UINT64& uWiped, E_UINT32& uPrevTime)
{
    E_UINT32 uTickCount = GetTickCount();

    // wiped area
    context->m_uStatWiped += uWiped;

    // if the given area was completely overwritten at least once
    if (uWiped >= context->m_uiFileSize.QuadPart) {
        context->m_uStatErasedArea += context->m_uiFileSize.QuadPart;
        context->m_uStatTips += context->m_uClusterSpace;
    } else {
        // the whole area was not even once completely overwritten
        context->m_uStatErasedArea += uWiped;

        if (context->m_uClusterSpace > 0 &&
            uWiped > (context->m_uiFileSize.QuadPart - context->m_uClusterSpace)) {
            // this is how much of the cluster tips actually was overwritten
            context->m_uStatTips += context->m_uiFileSize.QuadPart -
                                    context->m_uClusterSpace;
        }
    }

    // time
    if (uTickCount > uPrevTime) {
        context->m_uStatTime += (uTickCount - uPrevTime);
    }
}


#endif