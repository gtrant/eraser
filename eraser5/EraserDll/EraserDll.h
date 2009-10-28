// EraserDll.h
// Header file for the Eraser Library.
//
// Eraser. Secure data removal. For Windows.
// Copyright © 2002  Garrett Trant (gtrant@heidi.ie).
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

#ifndef ERASERDLL_H
#define ERASERDLL_H

#include "EraserExport.h"

// GUID
//
#ifndef _GUID_ERASER
    #define _GUID_ERASER "Eraser.{D5BBB6C1-64F1-11d1-A87C-444553540000}"
#endif


// Registry keys (HKEY_CURRENT_USER)
//
// base keys
const LPCTSTR ERASER_REGISTRY_AUTHOR
    = _T("Software\\Heidi Computers Ltd");
const LPCTSTR ERASER_REGISTRY_PROGRAM
    = _T("Software\\Heidi Computers Ltd\\Eraser");
const LPCTSTR ERASER_REGISTRY_BASE
    = _T("Software\\Heidi Computers Ltd\\Eraser\\5.8");
// settings for the library
const LPCTSTR ERASER_REGISTRY_LIBRARY
    = _T("Library");
const LPCTSTR ERASER_REGISTRY_LIBRARY_VERSION
    = _T("LibraryVersion");
// enable Shell Extension?
const LPCTSTR ERASEXT_REGISTRY_ENABLED
    = _T("ErasextEnabled");
// results for the Shell Extension
const LPCTSTR ERASEXT_REGISTRY_RESULTS
    = _T("ResultsErasext");
// enable slow entropy polling?
const LPCTSTR ERASER_RANDOM_SLOW_POLL
    = _T("EraserSlowPollEnabled");
// common results
const LPCTSTR ERASER_REGISTRY_RESULTS_WHENFAILED
    = _T("ResultsOnlyWhenFailed");
const LPCTSTR ERASER_REGISTRY_RESULTS_FILES
    = _T("ResultsForFiles");
const LPCTSTR ERASER_REGISTRY_RESULTS_UNUSEDSPACE
    = _T("ResultsForUnusedSpace");


// URLs
//
const LPCTSTR ERASER_URL_HOMEPAGE
    = _T("http://www.heidi.ie/eraser/");
const LPCTSTR ERASER_URL_EMAIL
    = _T("mailto:support@heidi.ie");

#ifdef DMARS
typedef unsigned __int64 ULONGLONG;
#endif

// Library basic types
//
typedef char        E_INT8,   *E_PINT8;
typedef short       E_INT16,  *E_PINT16;
typedef LONG        E_INT32,  *E_PINT32;
typedef LONGLONG    E_INT64,  *E_PINT64;

typedef BYTE        E_UINT8,  *E_PUINT8;
typedef WORD        E_UINT16, *E_PUINT16;
typedef ULONG       E_UINT32, *E_PUINT32;
typedef ULONGLONG   E_UINT64, *E_PUINT64;

#define E_IN        const
#define E_OUT
#define E_INOUT


// Window messages
//
#define WM_ERASERNOTIFY     (WM_USER + 10)

// wParam values
#define ERASER_WIPE_BEGIN   0
#define ERASER_WIPE_UPDATE  1
#define ERASER_WIPE_DONE    2
#define ERASER_TEST_PAUSED  3

// Library type definitions
//
typedef E_UINT32 ERASER_HANDLE;

typedef enum {
    ERASER_METHOD_LIBRARY,
    ERASER_METHOD_GUTMANN,
    ERASER_METHOD_DOD,
    ERASER_METHOD_DOD_E,
    ERASER_METHOD_PSEUDORANDOM,
	ERASER_METHOD_FIRST_LAST_2KB,
    ERASER_METHOD_SCHNEIER
} ERASER_METHOD;


#define eraserIsValidMethod(x)  (( (x) >= ERASER_METHOD_LIBRARY ) && \
                                 ( (x) <= ERASER_METHOD_SCHNEIER ))

typedef enum {
    ERASER_DATA_DRIVES,
    ERASER_DATA_FILES } ERASER_DATA_TYPE;

#define eraserIsValidDataType(x)  (( (x) >= ERASER_DATA_DRIVES ) && \
                                   ( (x) <= ERASER_DATA_FILES ))

typedef enum {
    ERASER_PAGE_DRIVE,
    ERASER_PAGE_FILES } ERASER_OPTIONS_PAGE;

// eraserRemoveFolder options
enum {
    ERASER_REMOVE_FOLDERONLY  = 0,
    ERASER_REMOVE_RECURSIVELY = 1
};

// display flags
enum {
    eraserDispPass     = (1 << 0),     // Show pass information
    eraserDispTime     = (1 << 1),     // Show estimated time
    eraserDispMessage  = (1 << 2),     // [UNUSED] Show message
    eraserDispProgress = (1 << 3),     // [UNUSED] Show progress bar
    eraserDispStop     = (1 << 4),     // [UNUSED] Allow termination
    eraserDispItem     = (1 << 5),     // [UNUSED] Show item name
    eraserDispInit     = (1 << 6),     // Set progress to 0 on ERASER_WIPE_BEGIN
    eraserDispReserved = (1 << 7)      // [UNUSED]
};

// bit masks for items to erase
enum {
    // files
    fileClusterTips      = (1 << 0),
    fileNames            = (1 << 1),
    fileAlternateStreams = (1 << 2),
    // unused disk space
    diskFreeSpace        = (1 << 5),
    diskClusterTips      = (1 << 6),
    diskDirEntries       = (1 << 7)
};


// Error messages
//
typedef E_INT32 ERASER_RESULT;

#define ERASER_OK                       0       // No error
#define ERASER_ERROR                    -1      // Unspecified error
#define ERASER_ERROR_PARAM1             -2      // Parameter 1 invalid
#define ERASER_ERROR_PARAM2             -3      // Parameter 2 invalid
#define ERASER_ERROR_PARAM3             -4      // Parameter 3 invalid
#define ERASER_ERROR_PARAM4             -5      // Parameter 4 invalid
#define ERASER_ERROR_PARAM5             -6      // Parameter 5 invalid
#define ERASER_ERROR_PARAM6             -7      // Parameter 6 invalid
#define ERASER_ERROR_MEMORY             -8      // Out of memory
#define ERASER_ERROR_THREAD             -9      // Failed to start thread
#define ERASER_ERROR_EXCEPTION          -10     // BUG!
#define ERASER_ERROR_CONTEXT            -11     // Context array full (ERASER_MAX_CONTEXT)
#define ERASER_ERROR_INIT               -12     // Library not initialized (eraserInit())
#define ERASER_ERROR_RUNNING            -13     // Failed because the thread is running
#define ERASER_ERROR_NOTRUNNING         -14     // Failed because the thread is not running
#define ERASER_ERROR_DENIED             -15     // Operation not permitted
#define ERASER_ERROR_NOTIMPLEMENTED     -32     // Function not implemented

#define eraserOK(x)     ((x) >= ERASER_OK)
#define eraserError(x)  (!eraserOK(x))

// only for marking a context invalid, use eraserIsValidContext to verify a handle!
#define ERASER_INVALID_CONTEXT          ((ERASER_HANDLE)-1)


// Export definitions
//

#if defined(__cplusplus)
extern "C" {
#endif




// Library initialization
//

// initializes the library, must be called before using
ERASER_EXPORT
eraserInit();
// cleans up after use
ERASER_EXPORT
eraserEnd();


// Context creation and destruction

//convert ERASER_METHOD to inner format
ERASER_API E_UINT8 convEraseMethod(ERASER_METHOD mIn);
// creates context with predefined settings
ERASER_EXPORT
eraserCreateContext(E_OUT ERASER_HANDLE*);
// creates context and sets an alternative method, pass count and items to erase
ERASER_EXPORT
eraserCreateContextEx(E_OUT ERASER_HANDLE*, E_IN E_UINT8, E_IN E_UINT16, E_IN E_UINT8);
// destroys a context
ERASER_EXPORT
eraserDestroyContext(E_IN ERASER_HANDLE);
// checks the validity of a context, return ERASER_OK if valid
ERASER_EXPORT
eraserIsValidContext(E_IN ERASER_HANDLE);


//error handler
typedef DWORD (*EraserErrorHandler) (LPCTSTR /*filename*/, DWORD /*dwErrorCode*/, void* /*ctx*/, void* /*param*/);
ERASER_EXPORT
eraserSetErrorHandler(E_IN ERASER_HANDLE, EraserErrorHandler pfn, void* fnParam);

// Data type
//

// sets context data type
ERASER_EXPORT
eraserSetDataType(E_IN ERASER_HANDLE, E_IN ERASER_DATA_TYPE);
// returns context data type
ERASER_EXPORT
eraserGetDataType(E_IN ERASER_HANDLE, E_OUT ERASER_DATA_TYPE*);


// Data
//

// adds item to the context data array
ERASER_EXPORT
eraserAddItem(E_IN ERASER_HANDLE, E_IN LPVOID, E_IN E_UINT16);

//set finish action - flags for ExitWindowsEx

ERASER_EXPORT
eraserSetFinishAction(E_IN ERASER_HANDLE param1, E_IN DWORD action);
// clears the context data array
ERASER_EXPORT
eraserClearItems(E_IN ERASER_HANDLE);


// Notification
//

// sets the window to notify
ERASER_EXPORT
eraserSetWindow(E_IN ERASER_HANDLE, E_IN HWND);
// returns the window
ERASER_EXPORT
eraserGetWindow(E_IN ERASER_HANDLE, E_OUT HWND*);
// sets the window message
ERASER_EXPORT
eraserSetWindowMessage(E_IN ERASER_HANDLE, E_IN E_UINT32);
// returns the window message
ERASER_EXPORT
eraserGetWindowMessage(E_IN ERASER_HANDLE, E_OUT E_PUINT32);


// Statistics
//

// returns the erased area
ERASER_EXPORT
eraserStatGetArea(E_IN ERASER_HANDLE, E_OUT E_PUINT64);
// returns the erased cluster tip area
ERASER_EXPORT
eraserStatGetTips(E_IN ERASER_HANDLE, E_OUT E_PUINT64);
// returns the amount of data written
ERASER_EXPORT
eraserStatGetWiped(E_IN ERASER_HANDLE, E_OUT E_PUINT64);
// returns the time used (ms)
ERASER_EXPORT
eraserStatGetTime(E_IN ERASER_HANDLE, E_OUT E_PUINT32);


// Display
//

// returns what the UI should show (see above for flag descriptions)
ERASER_EXPORT
eraserDispFlags(E_IN ERASER_HANDLE, E_OUT E_PUINT8);


// Progress information
//

// returns an estimate of how long the operation takes to complete
ERASER_EXPORT
eraserProgGetTimeLeft(E_IN ERASER_HANDLE, E_OUT E_PUINT32);
// returns the completion percent of current item
ERASER_EXPORT
eraserProgGetPercent(E_IN ERASER_HANDLE, E_OUT E_PUINT8);
// returns the completion percent of the operation
ERASER_EXPORT
eraserProgGetTotalPercent(E_IN ERASER_HANDLE, E_OUT E_PUINT8);
// returns the index of the current overwriting pass
ERASER_EXPORT
eraserProgGetCurrentPass(E_IN ERASER_HANDLE, E_OUT E_PUINT16);
// returns the amount of passes
ERASER_EXPORT
eraserProgGetPasses(E_IN ERASER_HANDLE, E_OUT E_PUINT16);
// returns a message UI can to show to the user telling what is going on
ERASER_EXPORT
eraserProgGetMessage(E_IN ERASER_HANDLE, E_OUT LPVOID, E_INOUT E_PUINT16);
// returns the name of the item that is being processed
ERASER_EXPORT
eraserProgGetCurrentDataString(E_IN ERASER_HANDLE, E_OUT LPVOID, E_INOUT E_PUINT16);



// Control
//

// starts overwriting in a new thread
ERASER_EXPORT
eraserStart(E_IN ERASER_HANDLE);
// starts overwriting
ERASER_EXPORT
eraserStartSync(E_IN ERASER_HANDLE);
// stops running task
ERASER_EXPORT
eraserStop(E_IN ERASER_HANDLE);
// checks whether task is being processed
ERASER_EXPORT
eraserIsRunning(E_IN ERASER_HANDLE, E_OUT E_PUINT8);


// Result
//

// checks whether the task was completed successfully
ERASER_EXPORT
eraserCompleted(E_IN ERASER_HANDLE, E_OUT E_PUINT8);
// checks whether the task failed
ERASER_EXPORT
eraserFailed(E_IN ERASER_HANDLE, E_OUT E_PUINT8);
// checks whether the task was terminated
ERASER_EXPORT
eraserTerminated(E_IN ERASER_HANDLE, E_OUT E_PUINT8);
// returns the amount of error messages in the context array
ERASER_EXPORT
eraserErrorStringCount(E_IN ERASER_HANDLE, E_OUT E_PUINT16);
// retrieves the given error message from the array
ERASER_EXPORT
eraserErrorString(E_IN ERASER_HANDLE, E_IN E_UINT16, E_OUT LPVOID, E_INOUT E_PUINT16);
// returns the amount of failed items in the context array
ERASER_EXPORT
eraserFailedCount(E_IN ERASER_HANDLE, E_OUT E_PUINT32);
// retrieves the given failed item from the array
ERASER_EXPORT
eraserFailedString(E_IN ERASER_HANDLE, E_IN E_UINT32, E_OUT LPVOID, E_INOUT E_PUINT16);


// Display report
//

// displays erasing report
ERASER_EXPORT
eraserShowReport(E_IN ERASER_HANDLE, E_IN HWND);


// Display library options
//

// displays the options window
ERASER_EXPORT
eraserShowOptions(E_IN HWND, E_IN ERASER_OPTIONS_PAGE);


// File / directory deletion
//

// removes a file
ERASER_EXPORT
eraserRemoveFile(E_IN LPVOID, E_IN E_UINT16);
// removes a folder
ERASER_EXPORT
eraserRemoveFolder(E_IN LPVOID, E_IN E_UINT16, E_IN E_UINT8);


// Helpers
//

// returns the amount of free disk space on a drive
ERASER_EXPORT
eraserGetFreeDiskSpace(E_IN LPVOID, E_IN E_UINT16, E_OUT E_PUINT64);

// returns the cluster size of a partition
ERASER_EXPORT
eraserGetClusterSize(E_IN LPVOID, E_IN E_UINT16, E_OUT E_PUINT32);


// Test mode
//

// enables test mode --> files will be opened with sharing enabled
// and erasing process will be paused after each overwriting pass
// until eraserTestContinueProcess(...) is called for the handle
ERASER_EXPORT
eraserTestEnable(E_IN ERASER_HANDLE);

// continues paused erasing process in test mode
ERASER_EXPORT
eraserTestContinueProcess(E_IN ERASER_HANDLE);


#if defined(__cplusplus)
}
#endif


// Helper definitions
//
#ifdef DEBUG
    #define NODEFAULT ASSERT(0)
#else
    #define NODEFAULT __assume(0)
#endif

// "." or ".."
#define ISNT_SUBFOLDER(lpsz) \
    ((lpsz)[0] == _T('.') && \
     ((lpsz)[1] == _T('\0') || \
      ((lpsz)[1] == _T('.') && \
       (lpsz)[2] == _T('\0'))))
#define IS_SUBFOLDER(lpsz) \
    (!ISNT_SUBFOLDER(lpsz))

// bit mask operations
//
#define bitSet(x, mask) \
    (((x) & (mask)) != 0)
#define setBit(x, mask) \
    (x) |= (mask)
#define unsetBit(x, mask) \
    (x) &= ~(mask)

// Library internal header file
//
#ifdef _DLL_ERASER
    #include "EraserDllInternal.h"
#endif

#endif // ERASERDLL_H