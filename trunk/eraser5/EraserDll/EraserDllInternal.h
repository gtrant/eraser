// EraserDllInternal.h
// Internal header file for the Eraser Library
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

#ifndef ERASER_INTERNAL_H
#define ERASER_INTERNAL_H

#define USE_TRACE_OUTPUT
#define USE_TRACE_BASE


// debugging
//
#ifdef _DEBUG
    #ifndef USE_TRACE_OUTPUT
        #undef TRACE0
        #define TRACE0(x)
    #endif
#endif

#ifdef USE_TRACE_LOCK
    #define eraserTraceLock(x) TRACE0(x)
#else
    #define eraserTraceLock(x)
#endif
#ifdef USE_TRACE_QUERY
    #define eraserTraceQuery(x) TRACE0(x)
#else
    #define eraserTraceQuery(x)
#endif
#ifdef USE_TRACE_PROGRESS
    #define eraserTraceProgress(x) TRACE0(x)
#else
    #define eraserTraceProgress(x)
#endif
#ifdef USE_TRACE_BASE
    #define eraserTraceBase(x) TRACE0(x)
#else
    #define eraserTraceBase(x)
#endif

// constants
//
// buffer size for disk operations (divisible by 3, 4, 512 and 4096)
const E_UINT32 ERASER_DISK_BUFFER_SIZE
     = 10567680; // Added extra zero 09-04-2007
// maximum file size for overwriting the free disk space
const E_UINT32 ERASER_MAX_FILESIZE
    = 422707200; // Added extra 2 zero 09-04-2007

// how many times files renamed on NT
const E_UINT16 ERASER_FILENAME_PASSES
    = 7;
// how many times directory entry cleaning can be restarted on 9x
const E_UINT32 ERASER_MAXIMUM_RESTARTS
    = 10;

// sector size
const E_UINT16 DEFAULT_SECTOR_SIZE
    = 512;


// literal constants
//
// temporary folders
const LPCTSTR ERASER_TEMP_DIRECTORY
    = _T("~ERAFSWD.TMP");
const LPCTSTR ERASER_TEMP_DIRECTORY_NTFS_ENTRIES
    = _T("~ERAFEWD.");
// module names
const LPCTSTR ERASER_MODULENAME_KERNEL
    = _T("KERNEL32.DLL");
const LPCTSTR ERASER_MODULENAME_NTDLL
    = _T("NTDLL.DLL");
const LPCTSTR ERASER_MODULENAME_SFC
    = _T("SFC.DLL");
// function names
#if defined(_UNICODE)
const LPCSTR ERASER_FUNCTIONNAME_GETDISKFREESPACEEX
    = "GetDiskFreeSpaceExW";
#else
const LPCSTR ERASER_FUNCTIONNAME_GETDISKFREESPACEEX
    = "GetDiskFreeSpaceExA";
#endif

const LPCSTR ERASER_FUNCTIONNAME_NTFSCONTROLFILE
    = "NtFsControlFile";
const LPCSTR ERASER_FUNCTIONNAME_NTQUERYINFORMATIONFILE
    = "NtQueryInformationFile";
const LPCSTR ERASER_FUNCTIONNAME_RTLNTSTATUSTODOSERROR
    = "RtlNtStatusToDosError";
const LPCSTR ERASER_FUNCTIONNAME_SFCISFILEPROTECTED
    = "SfcIsFileProtected";
// progress messages
const LPCTSTR ERASER_MESSAGE_WORK
    = _T("Overwriting...");
const LPCTSTR ERASER_MESSAGE_SEARCH
    = _T("Searching...");
const LPCTSTR ERASER_MESSAGE_CLUSTER
    = _T("Cluster Tips...");
const LPCTSTR ERASER_MESSAGE_FILENAMES
    = _T("File Names...");
const LPCTSTR ERASER_MESSAGE_FILENAMES_RETRY
    = _T("File Names... Retrying %u");
const LPCTSTR ERASER_MESSAGE_DIRENTRY
    = _T("Directory Entries...");
const LPCTSTR ERASER_MESSAGE_DIRENTRY_RETRY
    = _T("Directory Entries... Retrying %u");
const LPCTSTR ERASER_MESSAGE_MFT
    = _T("Master File Table Records...");
const LPCTSTR ERASER_MESSAGE_MFT_WAIT
    = _T("Master File Table Records... File %u");
const LPCTSTR ERASER_MESSAGE_REMOVING
    = _T("Removing Temporary Files...");


// function type definitions
//
typedef BOOL (WINAPI *SFCISFILEPROTECTED)(HANDLE, LPCWSTR);
typedef BOOL (WINAPI *GETDISKFREESPACEEX)(LPCTSTR, PULARGE_INTEGER, PULARGE_INTEGER, PULARGE_INTEGER);

class CEraserContext;
typedef bool (*WIPEFUNCTION)(CEraserContext*);


// converts boolean to ERASER_RESULT
//
#define truthToResult(x)    ((x) ? ERASER_OK : ERASER_ERROR);


// settings
//
#include "Pass.h"

#pragma pack(1)

struct LibrarySettings
{
    LibrarySettings() {
        ZeroMemory(this, sizeof(LibrarySettings));
    }

    ~LibrarySettings() {
        if (m_lpCMethods) {
            delete[] m_lpCMethods;
            m_lpCMethods = 0;
        }

        ZeroMemory(this, sizeof(LibrarySettings));
    }

    LibrarySettings& operator=(LibrarySettings& rs) {
        if (this != &rs) {
            m_nFileMethodID     = rs.m_nFileMethodID;
            m_nUDSMethodID      = rs.m_nUDSMethodID;
            m_uItems            = rs.m_uItems;

            m_nFileRandom       = rs.m_nFileRandom;
            m_nUDSRandom        = rs.m_nUDSRandom;

            if (m_lpCMethods) {
                if (m_nCMethods > 0) {
                    ZeroMemory(m_lpCMethods, m_nCMethods * sizeof(METHOD));
                }
                delete[] m_lpCMethods;
                m_lpCMethods = 0;
            }

            m_nCMethods = rs.m_nCMethods;
            m_lpCMethods = new METHOD[rs.m_nCMethods];

            for (BYTE i = 0; i < rs.m_nCMethods; i++) {
                m_lpCMethods[i] = rs.m_lpCMethods[i];
            }
        }

        return *this;
    }

    // files
    E_UINT8     m_nFileMethodID;

    // unused disk space
    E_UINT8     m_nUDSMethodID;

    // what to erase
    E_UINT8     m_uItems;

    // built-in methods (req. settings)
    E_UINT16    m_nFileRandom;
    E_UINT16    m_nUDSRandom;

    // custom methods
    E_UINT8     m_nCMethods;
    LPMETHOD    m_lpCMethods;
};

#pragma pack()

// partition information
//
enum {
    fsUnknown,
    fsFAT,
    fsFAT12,
    fsFAT16,
    fsFAT32,
    fsNTFS
};

#define isFileSystemFAT(pi) \
    ((pi.m_fsType == fsFAT)   || (pi.m_fsType == fsFAT12) || \
     (pi.m_fsType == fsFAT16) || (pi.m_fsType == fsFAT32))

#define isFileSystemNTFS(pi) \
    (pi.m_fsType == fsNTFS)

typedef class _PartitionInfo {
public:
    _PartitionInfo() {
        clear();
    }

    ~_PartitionInfo() {
        clear();
    }

    TCHAR    m_szDrive[4];
    E_UINT8  m_fsType;
    E_UINT32 m_uCluster;
    E_UINT32 m_uSector;
    bool     m_bLastSuccess;

private:
    void clear() {
        _tcsncpy(m_szDrive, _T(" :\\"), 4);
        m_fsType = fsUnknown;
        m_uCluster = 0;
        m_uSector = DEFAULT_SECTOR_SIZE;
        m_bLastSuccess = false;
    }

} PARTITIONINFO;


// progress control
//
enum {
    // display flags take the first 8 bits
    progressCustom  = (1 << 8)      // use custom handling of progress
};


// the eraser context
//


class CEraserContext
{
public:
	
    CEraserContext() :
    m_evStart(FALSE, FALSE),        // non-signaled, automatic
    m_evDone(TRUE, TRUE),           // signaled, manual
    m_evKillThread(FALSE, TRUE),    // non-signaled, manual
    m_evThreadKilled(TRUE, TRUE),   // signaled, manual
    m_evTestContinue(FALSE, TRUE),  // non-signaled, manual
	m_dwFinishAction(0) {
        clear();
    }

    ~CEraserContext() {
        if (AfxIsValidAddress(m_pwtThread, sizeof(CWinThread))) {
            ::TerminateThread(m_pwtThread->m_hThread, 1);
            ASSERT(0);
        }
        clear();
    }

    // use synchronization when accessing while thread: killed (running)
    //   r = can be assigned from the calling process, LOCK BEFORE READING
    //   w = can be read from calling process, LOCK BEFORE WRITING
    //   - = no need to lock for reading or writing

    // Control
    CWinThread          *m_pwtThread;                       // Pointer to the thread that handles erasing
    CCriticalSection    m_csLock;                           // For synchronizing access to context variables
    CEvent              m_evStart;                          // Signaled if OK to start running the thread
    CEvent              m_evDone;                           // Signaled if operation completed
    CEvent              m_evKillThread;                     // Signaled when thread must stop
    CEvent              m_evThreadKilled;                   // Signaled when thread not running
    // context identification
    E_UINT32            m_uOwnerThreadID;                   // ID of the calling thread
    E_UINT16            m_uContextID;                       // Random ID
    // Data type
    ERASER_DATA_TYPE    m_edtDataType;          // rw (-w)  // Data type of items on the list
    // Data
    CString             m_strData;              // -- (-w)  // Current item being processed
    CStringArray        m_saData;               // rw (--)  // List of items to process
    // Internal data
    CStringList         *m_pstrlDirectories;                // Pointer to a list of directories to process when clearing file names
    // I/O
    // Buffer
    E_PUINT32           m_puBuffer;                         // Pointer to write buffer
    // File
    HANDLE              m_hFile;                            // Handle to the file being overwritten
    ULARGE_INTEGER		m_uiFileSize;                       // Size of the area to overwrite from the file
    ULARGE_INTEGER      m_uiFileStart;                      // File position where overwriting begins
    E_UINT32            m_uClusterSpace;                    // Size of the cluster tip area for the current item
    // Partition info
    PARTITIONINFO       m_piCurrent;                        // Information about the current partition
    // Overwriting method
    LPMETHOD            m_lpmMethod;                        // Pointer to the currently used overwriting method
    METHOD              m_mThreadLocalMethod;               // Possible local copy of a built-in method (if valid, m_lpmMethod points to this)
    // Notification
    HWND                m_hwndWindow;           // rw (rw)  // Recipient of notification messages
    E_UINT32            m_uWindowMessage;       // rw (rw)  // Window message to send
    // Statistics
    E_UINT64            m_uStatErasedArea;      // -w (--)  // Size of erased area
    E_UINT64            m_uStatTips;            // -w (--)  // Size of cluster tip area
    E_UINT64            m_uStatWiped;           // -w (--)  // Amount of data written
    E_UINT32            m_uStatTime;            // -w (--)  // Write time
    // Results
    CStringArray        m_saFailed;             // -w (--)  // List of failed items
    CStringArray        m_saError;              // -w (--)  // List of error messages
    // Progress
    E_UINT32            m_uProgressTimeLeft;    // -- (-w)  // Estimated time until current item is processed
    E_UINT16            m_uProgressCurrentPass; // -- (-w)  // Overwriting pass that is being written
    E_UINT16            m_uProgressPasses;      // -- (-w)  // Number of overwriting passes used
    E_UINT8             m_uProgressPercent;     // -- (-w)  // Progress for current item
    E_UINT8             m_uProgressTotalPercent;// -- (-w)  // Total progress
    CString             m_strProgressMessage;   // -- (-w)  // Message to show, updated on ERASER_WIPE_{BEGIN,UPDATE}
    // Internal progress
    E_UINT64            m_uProgressSize;                    // Size of the area being overwritten (for the current item)
    E_UINT64            m_uProgressWiped;                   // Amount of data written so far (for the current item)
    E_UINT32            m_uProgressFiles;                   // Number of files to process 
    E_UINT32            m_uProgressWipedFiles;              // Number of files processed
    E_UINT32            m_uProgressFolders;                 // Number of directories processed (when clearing directory entries)
    E_UINT32            m_uProgressDrives;                  // Number of drives to process
    E_UINT32            m_uProgressWipedDrives;             // Number of drives processed
    E_UINT8             m_uProgressTaskPercent;             // Total progress percentage based on the portion of the work completed
    E_UINT8             m_uProgressTasks;                   // For keeping track of total progress
    E_UINT32            m_uProgressStartTime;               // For estimating time left
    // Display and progress control
    E_UINT16            m_uProgressFlags;       // -- (-w)  // See above for flag descriptions, display flags in EraserDll.h
    // Settings
    LibrarySettings     m_lsSettings;                       // Context-specific settings
    // Test mode
    E_UINT8             m_uTestMode;            // rw (--)  // Not 0 = files opened with sharing and operation paused after each pass
    CEvent              m_evTestContinue;                   // Signal to continue after pause
	DWORD				m_dwFinishAction;					//finish action: shutdown system, reboot, none
	EraserErrorHandler  m_pfnErrorHandler;					// Error handler callback function. Called when "CreateFile" error is detected;
	void*				m_pErrorHandlerParam;				// Additional parameter for error handler callback

	inline DWORD HandleError(LPCTSTR szFileName, DWORD dwErrorCode = GetLastError())
	{
		if(!m_pfnErrorHandler)
			return 0;
		return m_pfnErrorHandler(szFileName, dwErrorCode, this, m_pErrorHandlerParam);
	}
private:
    void clear() {
        m_pwtThread = 0;
        m_evStart.ResetEvent();
        m_evDone.SetEvent();
        m_evKillThread.ResetEvent();
        m_evThreadKilled.SetEvent();
        m_uOwnerThreadID = 0;
        m_uContextID = 0;
        m_edtDataType = ERASER_DATA_FILES;
        m_strData.Empty();
        m_saData.RemoveAll();
        m_pstrlDirectories = 0;
        m_puBuffer = 0;
        m_hFile = INVALID_HANDLE_VALUE;
        m_uiFileSize.QuadPart = 0;
        m_uiFileStart.QuadPart = 0;
        m_uClusterSpace = 0;
        m_hwndWindow = NULL;
        m_uWindowMessage = WM_NULL;
        m_uStatErasedArea = 0;
        m_uStatTips = 0;
        m_uStatWiped = 0;
        m_uStatTime = 0;
        m_saFailed.RemoveAll();
        m_saError.RemoveAll();
        m_uProgressTimeLeft = 0;
        m_uProgressCurrentPass = 0;
        m_uProgressPasses = 0;
        m_uProgressPercent = 0;
        m_uProgressTotalPercent = 0;
        m_strProgressMessage.Empty();
        m_uProgressSize = 0;
        m_uProgressWiped = 0;
        m_uProgressFiles = 0;
        m_uProgressWipedFiles = 0;
        m_uProgressDrives = 0;
        m_uProgressWipedDrives = 0;
        m_uProgressTaskPercent = 0;
        m_uProgressTasks = 0;
        m_uProgressFolders = 0;
        m_uProgressStartTime = 0;
        m_uProgressFlags = 0;
        m_lpmMethod = 0;
        m_uTestMode = 0;
        m_evTestContinue.ResetEvent();
		m_pfnErrorHandler = NULL;
		m_pErrorHandlerParam = NULL;
    }


    // no copying, pass a pointer instead
    CEraserContext(const CEraserContext&);
    CEraserContext& operator=(const CEraserContext&);
};


// context range (ERASER_MAX_CONTEXT - ERASER_MIN_CONTEXT + 1)
//
#define ERASER_MIN_CONTEXT              0   // this one should probably be left alone
#define ERASER_MAX_CONTEXT              127 // max. 65534 to make sure ERASER_INVALID_CONTEXT is unique

// context helpers
#define eraserContextID(x)              ((E_UINT16)HIWORD((E_UINT32)(x)))
#define eraserContextIndex(x)           ((E_UINT16)LOWORD((E_UINT32)(x)))

#define eraserSetContextID(x, id)       ((x) = (ERASER_HANDLE)MAKELONG(eraserContextIndex(x), (E_UINT16)(id)))
#define eraserSetContextIndex(x, index) ((x) = (ERASER_HANDLE)MAKELONG((E_UINT16)(index), eraserContextID(x)))

// validity of the context index
#define eraserContextOK(x)              (( eraserContextIndex(x) >= ERASER_MIN_CONTEXT ) && \
                                         ( eraserContextIndex(x) <= ERASER_MAX_CONTEXT ))


// access control
#define eraserContextAccessName(x, name) \
    eraserTraceLock("eraserContextAccess\n"); \
    CSingleLock name(&(x)->m_csLock, TRUE)
#define eraserContextAccess(x) \
    eraserContextAccessName(x, slc)
#define eraserContextReleaseName(name) \
    do { \
        eraserTraceLock("eraserContextRelease\n"); \
        (name).Unlock(); \
    } while (0)
#define eraserContextRelease() \
    eraserContextReleaseName(slc)
#define eraserContextRelockName(name) \
    do { \
        eraserTraceLock("eraserContextRelock\n"); \
        (name).Lock(); \
    } while (0)
#define eraserContextRelock() \
    eraserContextRelockName(slc)
#define eraserContextLock(x) \
    do { \
        eraserTraceLock("eraserContextLock\n"); \
        (x)->m_csLock.Lock(); \
    } while (0)
#define eraserContextUnlock(x) \
    do { \
        eraserTraceLock("eraserContextUnlock\n"); \
        (x)->m_csLock.Unlock(); \
    } while (0)
#define eraserSafeAssign(x, y, z) \
    do { \
        eraserTraceLock("eraserSafeAssign\n"); \
        eraserContextLock(x); \
        (y) = (z); \
        eraserContextUnlock(x); \
    } while (0)


// status
#define eraserInternalCompleted(x) \
    (WaitForSingleObject((x)->m_evDone, 0) == WAIT_OBJECT_0)
#define eraserInternalFailed(x) \
    (!eraserInternalCompleted(x))
#define eraserInternalTerminated(x) \
    (WaitForSingleObject((x)->m_evKillThread, 0) == WAIT_OBJECT_0)
#define eraserInternalIsRunning(x) \
    (WaitForSingleObject((x)->m_evThreadKilled, 0) != WAIT_OBJECT_0)

// error - call only from running thread
inline void
eraserAddError(CEraserContext *context, E_UINT32 rid)
{
    eraserTraceBase("eraserAddError\n");
    CString strError;
    try {
        if (strError.LoadString(rid)) {
            context->m_saError.Add(strError);
        }
    } catch (...) {
        ASSERT(0);
    }
}

inline void
eraserAddError1(CEraserContext *context, E_UINT32 rid, LPCTSTR str)
{
    eraserTraceBase("eraserAddError\n");
    CString strError;
    try {
        AfxFormatString1(strError, rid, str);
        context->m_saError.Add(strError);
    } catch (...) {
        ASSERT(0);
    }
}

///////////////////////////////////////////////////////////////////////////////
// definitions

// for notifying UI window
#define eraserBeginNotifyNoAccess(x) \
    if ((x)->m_hwndWindow != NULL) { \
        PostMessage((x)->m_hwndWindow, (x)->m_uWindowMessage, ERASER_WIPE_BEGIN, 0); \
    }
#define eraserBeginNotify(x) \
    do { \
        eraserTraceProgress("eraserBeginNotify\n"); \
        eraserContextLock(x); \
        eraserBeginNotifyNoAccess(x); \
        eraserContextUnlock(x); \
    } while (0)

#define eraserUpdateNotifyNoAccess(x) \
    if ((x)->m_hwndWindow != NULL) { \
        PostMessage((x)->m_hwndWindow, (x)->m_uWindowMessage, ERASER_WIPE_UPDATE, 0); \
    }
#define eraserUpdateNotify(x) \
    do { \
        eraserTraceProgress("eraserUpdateNotify\n"); \
        eraserContextLock(x); \
        eraserUpdateNotifyNoAccess(x); \
        eraserContextUnlock(x); \
    } while (0)

#define eraserTestPausedNotifyNoAccess(x) \
    if ((x)->m_hwndWindow != NULL) { \
        PostMessage((x)->m_hwndWindow, (x)->m_uWindowMessage, ERASER_TEST_PAUSED, 0); \
    }
#define eraserTestPausedNotify(x) \
    do { \
        eraserTraceProgress("eraserTestPausedNotify\n"); \
        eraserContextLock(x); \
        eraserTestPausedNotifyNoAccess(x); \
        eraserContextUnlock(x); \
    } while (0)

#define eraserEndThread(x, return_value) \
    do { \
        eraserTraceBase("eraserEndThread\n"); \
        eraserContextLock(x); \
        (x)->m_evThreadKilled.SetEvent(); \
        if ((x)->m_hwndWindow != NULL) { \
            PostMessage((x)->m_hwndWindow, (x)->m_uWindowMessage, \
                        ERASER_WIPE_DONE, 0); \
        } \
        (x)->m_pwtThread = 0; \
        eraserContextUnlock(x); \
        return (return_value); \
    } while (0)

// for controlling what UI shows
#define eraserProgressSetMessage(x, message) \
    do { \
        eraserTraceProgress("eraserProgressSetMessage\n"); \
        eraserContextLock(x); \
        (x)->m_strProgressMessage = message; \
        eraserContextUnlock(x); \
    } while (0)

#define eraserProgressStartEstimate(x, size) \
    do { \
        eraserTraceProgress("eraserProgressStartEstimate\n"); \
        (x)->m_uProgressWiped = 0; \
        (x)->m_uProgressSize = (size); \
        (x)->m_uProgressStartTime = GetTickCount(); \
    } while (0)

// default configuration
#define eraserDispDefault(x) \
    do { \
        eraserTraceProgress("eraserDispDefault\n"); \
        eraserContextLock(x); \
        (x)->m_uProgressFlags = eraserDispPass | eraserDispTime | eraserDispInit; \
        (x)->m_strProgressMessage = ERASER_MESSAGE_WORK; \
        eraserContextUnlock(x); \
    } while (0)

// counting files (not overwriting)
#define eraserDispSearch(x) \
    do { \
        eraserTraceProgress("eraserDispSearch\n"); \
        eraserContextLock(x); \
        (x)->m_uProgressFlags = 0; \
        (x)->m_strProgressMessage = ERASER_MESSAGE_SEARCH; \
        eraserContextUnlock(x); \
    } while (0)

// erasing cluster tips
#define eraserDispClusterTips(x) \
    do { \
        eraserTraceProgress("eraserDispClusterTips\n"); \
        eraserContextLock(x); \
        (x)->m_uProgressFlags = progressCustom; \
        (x)->m_strProgressMessage = ERASER_MESSAGE_CLUSTER; \
        eraserContextUnlock(x); \
    } while (0)

// erasing file names
#define eraserDispFileNames(x) \
    do { \
        eraserTraceProgress("eraserDispFileNames\n"); \
        eraserContextLock(x); \
        (x)->m_uProgressFlags = eraserDispInit | progressCustom; \
        (x)->m_strProgressMessage = ERASER_MESSAGE_FILENAMES; \
        eraserContextUnlock(x); \
    } while (0)

// erasing directory entries
#define eraserDispDirEntries(x) \
    do { \
        eraserTraceProgress("eraserDispDirEntries\n"); \
        eraserContextLock(x); \
        (x)->m_uProgressFlags = eraserDispInit | progressCustom; \
        (x)->m_strProgressMessage = ERASER_MESSAGE_DIRENTRY; \
        eraserContextUnlock(x); \
    } while (0)

// erasing MFT records
#define eraserDispMFT(x) \
    do { \
        eraserTraceProgress("eraserDispMFT\n"); \
        eraserContextLock(x); \
        (x)->m_uProgressFlags = eraserDispInit | progressCustom; \
        (x)->m_strProgressMessage = ERASER_MESSAGE_MFT; \
        eraserContextUnlock(x); \
    } while (0)


// handling exceptions
#define handleException(e, context) \
    do { \
        ASSERT(0); \
        try { \
            ZeroMemory(szExceptionBuffer, uExceptionBufferSize * sizeof(TCHAR)); \
            (e)->GetErrorMessage(szExceptionBuffer, uExceptionBufferSize); \
            context->m_saError.Add((LPCTSTR)szExceptionBuffer); \
        } catch (...) {\
        } \
        try { \
            (e)->Delete(); \
        } catch (...) { \
        } \
    } while (0)


///////////////////////////////////////////////////////////////////////////////
// helper functions

#define eraserBool(x, y) \
    if (!(y)) { (x) = false; }

// round up to nearest multiple
//
#define roundUp(x, multiple) \
    (((x) + ((multiple) - 1)) & ~((multiple) - 1))

inline
void ansiToCString(LPCSTR ansi, CString& str) {
#if defined(_UNICODE)
	const int x = MultiByteToWideChar(
		CP_ACP,
		0,
		ansi,
		-1,
		NULL,
		0
		);
	wchar_t *buf = new wchar_t[x+1];
	if (MultiByteToWideChar(
		CP_ACP,
		0,
		ansi,
		-1,
		buf,
		x+1
		)
	)
	{
		str = buf;
	}
	delete [] buf;
#else
	str = ansi;
#endif
}

inline void
unicodeToCString(LPCWSTR uni, CString& str) {
#if defined(_UNICODE)
	str = uni;
#else
	const int x = WideCharToMultiByte(
		CP_ACP,
		0,
		uni,
		-1,
		NULL,
		0,
		NULL,
		NULL
		);

	if (x == 0) {
		str = "";
		return;
	}

	char *buf = new char[x+1];
	if (WideCharToMultiByte(
		CP_ACP,
		0,
		uni,
		-1,
		buf,
		x+1,
		NULL,
		NULL
		)
	)
	{
		str = buf;
	}
	delete [] buf;
#endif
}

void inline ansiToUnicode(const char *ansi, wchar_t *uni, int len) {
	MultiByteToWideChar(
		CP_ACP,
		0,
		ansi,
		-1,
		uni,
		len
		);
}


inline E_UINT64
fileSizeToArea(CEraserContext *context, const E_UINT64& uFileSize)
{
    E_UINT64 uBlockSize = max(context->m_piCurrent.m_uCluster,
        max(context->m_piCurrent.m_uSector, DEFAULT_SECTOR_SIZE));

    if (uFileSize % uBlockSize == 0)
        return uFileSize;
    return roundUp(uFileSize, uBlockSize);
}

inline void
fillPassData(E_PUINT32 puBuffer, E_UINT32 uSize, LPPASS pPass)
{
    // fills buffer with data
    try {
        if (pPass->bytes == 1) {
            if (pPass->byte1 != RND_DATA) {
                FillMemory((LPVOID)puBuffer, uSize, (E_UINT8)pPass->byte1);
            }
        } else {
            FillMemoryWith((LPVOID)puBuffer, uSize, pPass->bytes,
                (E_UINT8)pPass->byte1, (E_UINT8)pPass->byte2, (E_UINT8)pPass->byte3);
        }
    } catch (...) {
        ASSERT(0);
    }
}

inline void
shufflePassArray(PASS *pPassArray, E_UINT16 uSize)
{
    if (uSize < 2 || uSize > PASSES_MAX) {
        return;
    }

    E_UINT16 uLast;
    E_UINT16 uRandom;
    E_PUINT32 puRandomArray = 0;
    PASS pass;

    // shuffles pass array of size uSize
    try {
        uLast = uSize;

        // retrieve random bytes
        puRandomArray = new E_UINT32[uSize - 1];

        if (!randomFill((E_PUINT8)puRandomArray, (uSize - 1) * sizeof(E_UINT32))) {
            // in the unlikely case that randomFill fails, use ISAAC
            isaacFill((E_PUINT8)puRandomArray, (uSize - 1) * sizeof(E_UINT32));
        }

        // shuffle the array by swapping the last element with a
        // random element and the repeating without the last element

        while (uLast > 1) {
            // uRandom = [0, uLast - 1]
            uRandom = (E_UINT16)(puRandomArray[uLast - 2] % uLast);

            if (--uLast > uRandom) {
                pass                = pPassArray[uRandom];
                pPassArray[uRandom] = pPassArray[uLast];
                pPassArray[uLast]   = pass;
            }
        }

        ZeroMemory(puRandomArray, uSize - 1);
        delete[] puRandomArray;
        puRandomArray = 0;

    } catch (...) {
        ASSERT(0);
        try {
            if (puRandomArray) {
                ZeroMemory(puRandomArray, uSize - 1);
                delete[] puRandomArray;
                puRandomArray = 0;
            }
        } catch (...) {
        }
    }

    setPassOne(pass, 0);
    uRandom = 0;
}

inline void
setBufferSize(CEraserContext *context, E_UINT32& uUsedBufferSize)
{
    // sets minimum required buffer size
    try {
        if (context->m_uiFileSize.QuadPart < (E_UINT64)ERASER_DISK_BUFFER_SIZE) {
            uUsedBufferSize = context->m_uiFileSize.LowPart;
            return;
        }
    } catch (...) {
        ASSERT(0);
    }
    uUsedBufferSize = ERASER_DISK_BUFFER_SIZE;
}

#endif