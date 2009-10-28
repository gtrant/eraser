// Eraser.cpp
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
#include "Eraser.h"
#include "EraserDll.h"
#include "Common.h"

#include "Options.h"
#include "OptionsDlg.h"
#include "ReportDialog.h"

#include "RND.h"
#include "DOD.h"
#include "Gutmann.h"
#include "Custom.h"

#include "File.h"
#include "NTFS.h"
#include "FreeSpace.h"
#include "FAT.h"

#include "..\EraserUI\VisualStyles.h"
#include "..\shared\FileHelper.h"
#include "..\shared\key.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

#undef MAX_PATH
#define MAX_PATH 2048 //HACK: Some filenames under Vista can exceed the 260
                      //char limit. This will have to do for now.

/////////////////////////////////////////////////////////////////////////////
// CEraserApp

BEGIN_MESSAGE_MAP(CEraserDll, CWinApp)
    //{{AFX_MSG_MAP(CEraserApp)
        // NOTE - the ClassWizard will add and remove mapping macros here.
        //    DO NOT EDIT what you see in these blocks of generated code!
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CEraserApp construction

CEraserDll::CEraserDll()
{
    _set_se_translator(SeTranslator);
}

BOOL CEraserDll::InitInstance()
{
    // determine the operating system
    OSVERSIONINFO ov;
    ZeroMemory(&ov, sizeof(OSVERSIONINFO));
    ov.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
    GetVersionEx(&ov);

    //isWindowsNT = (ov.dwPlatformId == VER_PLATFORM_WIN32_NT);
	//isWindowsNT =  (ov.dwPlatformId == VER_PLATFORM_WIN32_NT  && (ov.dwMajorVersion > 5 || (ov.dwMajorVersion == 5 && ov.dwMinorVersion >= 1)));
	isWindowsNT = (ov.dwPlatformId == VER_PLATFORM_WIN32_NT  && (ov.dwMajorVersion >= 4));

    // initialize the context array
    eraserContextArrayAccess();
    ZeroMemory(eraserContextArray, sizeof(CEraserContext*) * (ERASER_MAX_CONTEXT + 1));

    // initialize reference counter
    eraserLibraryUnlock();

    return CWinApp::InitInstance();
}

int CEraserDll::ExitInstance()
{
    // clean up
    eraserLibraryUnlock();
    eraserEnd();

    return CWinApp::ExitInstance();
}


/////////////////////////////////////////////////////////////////////////////
// The one and only CEraserApp object

CEraserDll theApp;

/////////////////////////////////////////////////////////////////////////////
// definitions

UINT eraserThread(LPVOID);

/////////////////////////////////////////////////////////////////////////////
// misc. helpers

static void mySleep(UINT second) 
{ 
    MSG  msg; 
    while( PeekMessage( &msg, NULL/*(HWND)this*/, 0, 0, PM_REMOVE ) ) 
    { 
        GetMessage( &msg, NULL, 0, 0 ); 
        TranslateMessage(&msg); 
        DispatchMessage(&msg); 
    } 
    clock_t start, finish; 
    double  duration; 
    start = clock(); 
    for(;;) 
    { 
        finish = clock(); 
        duration = (double)(finish - start) / CLOCKS_PER_SEC; 
        if(duration > second) 
        break; 
    } 
} 

static inline bool
overwriteFileName(LPCTSTR szFile, LPTSTR szLastFileName)
{
    TCHAR szNewName[MAX_PATH];
    PTCHAR pszLastSlash;
    size_t index, i, j, length;

    try {
        _tcsncpy(szLastFileName, szFile, MAX_PATH);
        pszLastSlash = _tcsrchr(szLastFileName, '\\');

        if (pszLastSlash == NULL) {
            return false;
        }

        index = (pszLastSlash - szLastFileName) / sizeof(TCHAR);

        _tcsncpy(szNewName, szLastFileName, MAX_PATH);
        length = (E_UINT32)_tcslen(szLastFileName);

        for (i = 0; i < ERASER_FILENAME_PASSES; i++) {
            // replace each non-'.' character with a random letter
            isaacFill((E_PUINT8)(szNewName + index + 1), (length - index - 1) * sizeof(TCHAR));

            for (j = index + 1; j < length; j++) {
                if (szLastFileName[j] != '.') {
                    szNewName[j] = ERASER_SAFEARRAY[((E_UINT16)szNewName[j]) % ERASER_SAFEARRAY_SIZE];
                } else {
                    szNewName[j] = '.';
                }
            }

            if (MoveFile(szLastFileName, szNewName)) {
                _tcsncpy(szLastFileName, szNewName, MAX_PATH);
            } else
            {
                Sleep(50); // Allow for Anti-Virus applications to stop looking at the file
                if (MoveFile(szLastFileName, szNewName)) {
                _tcsncpy(szLastFileName, szNewName, MAX_PATH);
                }
            }
        }

        return true;
    }
    catch (...) {
        ASSERT(0);
    }

    ZeroMemory(szNewName, MAX_PATH * sizeof(TCHAR));

    return false;
}

static inline bool
isFolderEmpty(LPCTSTR szFolder)
{
    bool            bEmpty = true;
    HANDLE          hFind;
    WIN32_FIND_DATA wfdData;
    CString         strFolder(szFolder);

    // make sure that the folder name ends with a backslash
    if (strFolder[strFolder.GetLength() - 1] != '\\') {
        strFolder += "\\";
    }

    hFind = FindFirstFile((LPCTSTR)(strFolder + _T("*")), &wfdData);

    if (hFind != INVALID_HANDLE_VALUE) {
        do {
            if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY) &&
                ISNT_SUBFOLDER(wfdData.cFileName)) {
                continue;
            }

            bEmpty = false;
            break;
        }
        while (FindNextFile(hFind, &wfdData));

        VERIFY(FindClose(hFind));
    }
    return bEmpty;
}

// removes all files and subfolders from the given folder, use with caution
static inline bool
emptyFolder(LPCTSTR szFolder)
{
    bool            bEmpty = true;
    HANDLE          hFind;
    WIN32_FIND_DATA wfdData;
    CString         strFolder(szFolder);
    CString         strFile;

    // make sure that the folder name ends with a backslash
    if (strFolder[strFolder.GetLength() - 1] != '\\') {
        strFolder += "\\";
    }

    hFind = FindFirstFile((LPCTSTR)(strFolder + _T("*")), &wfdData);

    if (hFind != INVALID_HANDLE_VALUE) {
        do {
            strFile = strFolder + wfdData.cFileName;

            if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY)) {
                if (IS_SUBFOLDER(wfdData.cFileName)) {
                    if (eraserError(eraserRemoveFolder((LPVOID)(LPCTSTR)strFile,
                            (E_UINT16)strFile.GetLength(), ERASER_REMOVE_RECURSIVELY))) {
                        bEmpty = false;
                    }
                }
            } else {
                if (eraserError(eraserRemoveFile((LPVOID)(LPCTSTR)strFile,
                        (E_UINT16)strFile.GetLength()))) {
                    bEmpty = false;
                }
            }
        }
        while (FindNextFile(hFind, &wfdData));

        VERIFY(FindClose(hFind));
    } else {
        return false;
    }
    return bEmpty;
}

/////////////////////////////////////////////////////////////////////////////
// context helpers

static inline ERASER_RESULT
contextToAddress(E_IN ERASER_HANDLE param1, E_OUT CEraserContext **pointer)
{
    // if you don't count all the error checking, this is quite fast, O(1)
    if (!eraserContextOK(param1)) {
        return ERASER_ERROR_PARAM1;
    } else if (!AfxIsValidAddress(pointer, sizeof(CEraserContext*))) {
        return ERASER_ERROR_PARAM2;
    } else {
        try {
            E_UINT16 index = eraserContextIndex(param1);
            eraserContextArrayAccess();
            *pointer = eraserContextArray[index];
            if (*pointer == 0) {
                return ERASER_ERROR_PARAM1;
            } else if (!AfxIsValidAddress(*pointer, sizeof(CEraserContext))) {
                eraserContextArray[index] = 0;
                *pointer = 0;
                return ERASER_ERROR_PARAM1;
            } else if ((*pointer)->m_uContextID != eraserContextID(param1) ||
                       (*pointer)->m_uOwnerThreadID != ::GetCurrentThreadId()) {
                // invalid context id or attempt to access from another thread
                *pointer = 0;
                return ERASER_ERROR_DENIED;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

/////////////////////////////////////////////////////////////////////////////
// exported functions

// Library initialization
//
ERASER_EXPORT
eraserInit()
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserInit\n");

    try {
        // increase the reference counter
        eraserLibraryInit();
        randomInit();

        return ERASER_OK;
    } catch (...) {
        ASSERT(0);
        return ERASER_ERROR_EXCEPTION;
    }
}

ERASER_EXPORT
eraserEnd()
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserEnd\n");

    ERASER_RESULT result = ERASER_OK;

    // decrease the reference counter
    eraserLibraryUninit();

    eraserContextArrayAccess();

    try {
        // if nobody else is using this instance of the library, clean up
        if (!eraserIsLibraryInit()) {
            for (ERASER_HANDLE i = ERASER_MIN_CONTEXT; i <= ERASER_MAX_CONTEXT; i++) {
                if (eraserContextArray[i] != 0) {
                    if (AfxIsValidAddress(eraserContextArray[i], sizeof(CEraserContext))) {
                        try {
                            // this will stop unsynchronized access to the context
                            VERIFY(eraserOK(eraserStop(i)));
                            eraserContextLock(eraserContextArray[i]);
                            delete eraserContextArray[i];
                        } catch (...) {
                            ASSERT(0);
                            result = ERASER_ERROR_EXCEPTION;
                        }
                    }
                    eraserContextArray[i] = 0;
                }
            }
        }

        // decrease prng reference counter
        randomEnd();
    } catch (...) {
        ASSERT(0);
        result = ERASER_ERROR_EXCEPTION;
    }

    return result;
}


// Context creation and destruction
//
ERASER_EXPORT
eraserCreateContext(E_OUT ERASER_HANDLE *param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserCreateContext\n");

    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    } else if (!AfxIsValidAddress(param1, sizeof(ERASER_HANDLE))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            *param1 = ERASER_INVALID_CONTEXT;
        } catch (...) {
            return ERASER_ERROR_PARAM1;
        }
    }

    eraserContextArrayAccess();

    // find first available context
    for (E_UINT16 i = ERASER_MAX_CONTEXT; i >= ERASER_MIN_CONTEXT; i--) {
        if (eraserContextArray[i] == 0) {
            try {
                eraserContextArray[i] = new CEraserContext();
            } catch (...) {
                eraserContextArray[i] = 0;
                return ERASER_ERROR_MEMORY;
            }

            try {
                if (!loadLibrarySettings(&eraserContextArray[i]->m_lsSettings)) {
                    setLibraryDefaults(&eraserContextArray[i]->m_lsSettings);
                }

                // reseed the prng
                isaacSeed();

                // context identification
                isaacFill((E_PUINT8)&eraserContextArray[i]->m_uContextID, sizeof(E_UINT16));
                eraserContextArray[i]->m_uOwnerThreadID = ::GetCurrentThreadId();

                // context handle is a combination of eraserContextArray
                // index and the number of times this function is called
                eraserSetContextID(*param1, eraserContextArray[i]->m_uContextID);
                eraserSetContextIndex(*param1, i);
            } catch (...) {
                ASSERT(0);
                if (AfxIsValidAddress(eraserContextArray[i], sizeof(CEraserContext))) {
                    delete eraserContextArray[i];
                    eraserContextArray[i] = 0;
                }
                return ERASER_ERROR_EXCEPTION;
            }
            return ERASER_OK;
        }
    }

    return ERASER_ERROR_CONTEXT;
}

E_UINT8 convEraseMethod(ERASER_METHOD mIn)
{
	switch(mIn){
		case ERASER_METHOD_LIBRARY:
			return GUTMANN_METHOD_ID;
			break;
		case ERASER_METHOD_GUTMANN:
			return GUTMANN_METHOD_ID;
			break;
		case ERASER_METHOD_DOD:
			return DOD_METHOD_ID;
			break;
		case ERASER_METHOD_DOD_E:
			return DOD_E_METHOD_ID;
			break;
		case ERASER_METHOD_PSEUDORANDOM:
			return RANDOM_METHOD_ID;
			break;
		case ERASER_METHOD_FIRST_LAST_2KB:
			return FL2KB_METHOD_ID;
			break;
        case ERASER_METHOD_SCHNEIER:
			return SCHNEIER_METHOD_ID;
			break;
		default:
			return (E_UINT8)mIn;
	}
}

ERASER_EXPORT
eraserCreateContextEx(E_OUT ERASER_HANDLE *param1, E_IN E_UINT8 param2, E_IN E_UINT16 param3, E_IN E_UINT8 param4)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserCreateContextEx\n");
    LONG lRetStatus = ERASER_OK;
	if (param2==0) {
        return ERASER_ERROR_PARAM2;
    } else if (param3 > PASSES_MAX || param3 < 1) {
        return ERASER_ERROR_PARAM3;
    }

    // this one does all our basic sanity checks
    ERASER_RESULT result = eraserCreateContext(param1);
    if (eraserError(result)) {
        return result;
    } else {
        try {
            CEraserContext *context = 0;
            if (eraserOK(contextToAddress(*param1, &context))) {
                eraserContextAccess(context);
                if (param4) {
                    context->m_lsSettings.m_uItems = param4;
                }
                switch (param2) {
                case GUTMANN_METHOD_ID:
                    context->m_lsSettings.m_nFileMethodID = GUTMANN_METHOD_ID;
                    context->m_lsSettings.m_nUDSMethodID  = GUTMANN_METHOD_ID;
                    break;
                case DOD_METHOD_ID:
                    context->m_lsSettings.m_nFileMethodID = DOD_METHOD_ID;
                    context->m_lsSettings.m_nUDSMethodID  = DOD_METHOD_ID;
                    break;
                case DOD_E_METHOD_ID:
                    context->m_lsSettings.m_nFileMethodID = DOD_E_METHOD_ID;
                    context->m_lsSettings.m_nUDSMethodID  = DOD_E_METHOD_ID;
                    break;
                case RANDOM_METHOD_ID:
                    context->m_lsSettings.m_nFileMethodID = RANDOM_METHOD_ID;
                    context->m_lsSettings.m_nUDSMethodID  = RANDOM_METHOD_ID;
                    context->m_lsSettings.m_nFileRandom   = param3;
                    context->m_lsSettings.m_nUDSRandom    = param3;
                    break;
				case FL2KB_METHOD_ID:
                    context->m_lsSettings.m_nFileMethodID = FL2KB_METHOD_ID;
                    context->m_lsSettings.m_nUDSMethodID  = FL2KB_METHOD_ID;
					break;
				case SCHNEIER_METHOD_ID:
                    context->m_lsSettings.m_nFileMethodID = SCHNEIER_METHOD_ID;
                    context->m_lsSettings.m_nUDSMethodID  = SCHNEIER_METHOD_ID;
					break;
                default:
					{
						LibrarySettings lsTemp;
						BOOL bExist = FALSE;
						if (loadLibrarySettings(&lsTemp))
						{
							for(int i = 0; i < lsTemp.m_nCMethods; i++)
								if (lsTemp.m_lpCMethods->m_nMethodID == param2) bExist = TRUE;
							if (bExist) {
								context->m_lsSettings.m_nFileMethodID = param2;
								context->m_lsSettings.m_nUDSMethodID  = param2;
							}
							else { 
								//eraserDestroyContext(*param1);
								lRetStatus = ERASER_ERROR_PARAM2;
								//break;
							}
						}
						else{
							//eraserDestroyContext(*param1);
							lRetStatus = ERASER_ERROR_PARAM2;
							//break;
						}
					}
                }

                return lRetStatus;
            }
            return ERASER_ERROR_CONTEXT;

        } catch (...) {
            ASSERT(0);
            try {
                eraserDestroyContext(*param1);
            } catch (...) {
            }
            return ERASER_ERROR_EXCEPTION;
        }
    }
}

ERASER_EXPORT
eraserDestroyContext(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserDestroyContext\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            // make sure we are not running this one
            VERIFY(eraserOK(eraserStop(param1)));
            // remove from array
            eraserContextArrayAccess();
            eraserContextArray[eraserContextIndex(param1)] = 0;
            eraserContextArrayRelease();
            // free the memory
            eraserContextLock(context);
            delete context;
            context = 0;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserIsValidContext(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserIsValidContext\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        return ERASER_OK;
    }
}
//Error handler
ERASER_EXPORT
eraserSetErrorHandler(E_IN ERASER_HANDLE param1, EraserErrorHandler pfn, void* fnParam)
{
	CEraserContext *context = 0;
	if (eraserError(contextToAddress(param1, &context))) 
	{
		return ERASER_ERROR_PARAM1;
	} 
	else if (eraserInternalIsRunning(context)) 
	{
		return ERASER_ERROR_RUNNING;
	} 
	else 
	{
		try 
		{

			eraserContextAccess(context);
			context->m_pfnErrorHandler = pfn;
			context->m_pErrorHandlerParam = fnParam;


		} 
		catch (...) 
		{
			ASSERT(0);
			return ERASER_ERROR_EXCEPTION;
		}
		return ERASER_OK;
	}

}

// Data type
//
ERASER_EXPORT
eraserSetDataType(E_IN ERASER_HANDLE param1, E_IN ERASER_DATA_TYPE param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserSetDataType\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!eraserIsValidDataType(param2)) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            if (context->m_saData.GetSize() == 0) {
                context->m_edtDataType = param2;
            } else {
                // cannot change data type after adding items to erase
                return ERASER_ERROR_DENIED;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserGetDataType(E_IN ERASER_HANDLE param1, E_OUT ERASER_DATA_TYPE *param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserGetDataType\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(ERASER_DATA_TYPE))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_edtDataType;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Assign data
//
ERASER_EXPORT
eraserAddItem(E_IN ERASER_HANDLE param1, E_IN LPVOID param2, E_IN E_UINT16 param3)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserAddItem\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (param3 < _MAX_DRIVE || param3 > _MAX_PATH) {
        return ERASER_ERROR_PARAM3;
    } else if (!AfxIsValidString((LPCTSTR)param2, param3)) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            LPCTSTR szItem = (LPCTSTR)param2;

            // if the item is a file, a fully qualified name is required,
            // drive must be given as "X:\\"

            if (!(isalpha(szItem[0]) && szItem[1] == ':' && szItem[2] == '\\') &&
            	!(_tcsncmp(_T("\\\\"), szItem, 2) == 0 && _tcschr(szItem + 2, '\\') != NULL)) {
                return ERASER_ERROR_PARAM2;
            }

            eraserContextAccess(context);

            if ((context->m_edtDataType == ERASER_DATA_FILES  && _tcslen(szItem) > _MAX_PATH) ||
                (context->m_edtDataType == ERASER_DATA_DRIVES && _tcslen(szItem) > _MAX_DRIVE)) {
                return ERASER_ERROR_PARAM2;
            } else {
                context->m_saData.Add(szItem);
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserSetFinishAction(E_IN ERASER_HANDLE param1, E_IN DWORD action)
{
	CEraserContext *context = 0;
	if (eraserError(contextToAddress(param1, &context))) {
		return ERASER_ERROR_PARAM1;
	} else if (eraserInternalIsRunning(context)) {
		return ERASER_ERROR_RUNNING;
	} else {
		try {
			
			eraserContextAccess(context);
			context->m_dwFinishAction = action;

			
		} catch (...) {
			ASSERT(0);
			return ERASER_ERROR_EXCEPTION;
		}
		return ERASER_OK;
	}

}
ERASER_EXPORT
eraserClearItems(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserClearItems\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            context->m_saData.RemoveAll();
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Notification
//
ERASER_EXPORT
eraserSetWindow(E_IN ERASER_HANDLE param1, E_IN HWND param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserSetWindow\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!IsWindow(param2)) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            eraserContextAccess(context);
            context->m_hwndWindow = param2;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserGetWindow(E_IN ERASER_HANDLE param1, E_OUT HWND* param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserGetWindow\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(HWND))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_hwndWindow;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserSetWindowMessage(E_IN ERASER_HANDLE param1, E_IN E_UINT32 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserSetWindowMessage\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            eraserContextAccess(context);
            context->m_uWindowMessage = param2;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserGetWindowMessage(E_IN ERASER_HANDLE param1, E_OUT E_PUINT32 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserGetWindowMessage\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT32))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uWindowMessage;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Statistics
//
ERASER_EXPORT
eraserStatGetArea(E_IN ERASER_HANDLE param1, E_OUT E_PUINT64 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserStatGetArea\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT64))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uStatErasedArea;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserStatGetTips(E_IN ERASER_HANDLE param1, E_OUT E_PUINT64 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserStatGetTips\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT64))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uStatTips;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserStatGetWiped(E_IN ERASER_HANDLE param1, E_OUT E_PUINT64 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserStatGetWiped\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT64))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uStatWiped;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserStatGetTime(E_IN ERASER_HANDLE param1, E_OUT E_PUINT32 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserStatGetTime\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT32))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uStatTime;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Display
//
ERASER_EXPORT
eraserDispFlags(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserDispFlags\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT8))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            // low-order byte
            *param2 = (E_UINT8)context->m_uProgressFlags;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Progress information
//
ERASER_EXPORT
eraserProgGetTimeLeft(E_IN ERASER_HANDLE param1, E_OUT E_PUINT32 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetTimeLeft\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT32))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uProgressTimeLeft;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserProgGetPercent(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetPercent\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT8))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = min(context->m_uProgressPercent, (E_UINT8)100);
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserProgGetTotalPercent(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetTotalPercent\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT8))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = min(context->m_uProgressTotalPercent, (E_UINT8)100);
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserProgGetCurrentPass(E_IN ERASER_HANDLE param1, E_OUT E_PUINT16 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetCurrentPass\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uProgressCurrentPass;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserProgGetPasses(E_IN ERASER_HANDLE param1, E_OUT E_PUINT16 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetPasses\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = context->m_uProgressPasses;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserProgGetMessage(E_IN ERASER_HANDLE param1, E_OUT LPVOID param2, E_INOUT E_PUINT16 param3)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetMessage\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param3, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM3;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            LPTSTR pszError = (LPTSTR)param2;
            if (pszError == 0) {
                *param3 = (E_UINT16)(context->m_strProgressMessage.GetLength() + 1);
                return ERASER_OK;
            } else if (*param3 < 1) {
                return ERASER_ERROR_PARAM3;
            } else if (!AfxIsValidAddress((LPCTSTR)pszError, *param3)) {
                return ERASER_ERROR_PARAM2;
            }
            ZeroMemory(pszError, *param3);
            lstrcpyn(pszError, (LPCTSTR)context->m_strProgressMessage, *param3);
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserProgGetCurrentDataString(E_IN ERASER_HANDLE param1, E_OUT LPVOID param2, E_INOUT E_PUINT16 param3)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceProgress("eraserProgGetCurrentDataString\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param3, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM3;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            LPTSTR pszError = (LPTSTR)param2;
            if (pszError == 0) {
                *param3 = (E_UINT16)(context->m_strData.GetLength() + 1);
                return ERASER_OK;
            } else if (*param3 < 1) {
                return ERASER_ERROR_PARAM3;
            } else if (!AfxIsValidAddress((LPCTSTR)pszError, *param3)) {
                return ERASER_ERROR_PARAM2;
            }
            ZeroMemory(pszError, *param3);
            lstrcpyn(pszError, (LPCTSTR)context->m_strData, *param3);
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Control
//
ERASER_EXPORT
eraserStart(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserStart\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            // preset flags
            context->m_evDone.SetEvent();
            context->m_evKillThread.ResetEvent();
            // create the thread
            context->m_pwtThread = AfxBeginThread(eraserThread, (LPVOID)context);
            if (context->m_pwtThread == NULL) {
                return ERASER_ERROR_THREAD;
            }
            // start operation
            eraserContextRelease();
            context->m_evStart.SetEvent();
        } catch (...) {
            ASSERT(0);
            eraserStop(param1);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserStartSync(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserStartSync\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            // preset flags
            context->m_evDone.SetEvent();
            context->m_evKillThread.ResetEvent();
            context->m_evStart.SetEvent();
            eraserContextRelease();
            // start erasing
            if (eraserThread((LPVOID)context) == EXIT_SUCCESS) {
                return ERASER_OK;
            } else {
                return ERASER_ERROR;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
    }
}


ERASER_EXPORT
eraserStop(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserStop\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            // set the kill flag
            context->m_evKillThread.SetEvent();
            context->m_evStart.SetEvent();
            // if in test mode, enable execution again
            eraserContextAccess(context);
            if (context->m_uTestMode) {
                context->m_evTestContinue.SetEvent();
            }
            eraserContextRelease();
            // two minutes should be enough for any thread (don't quote me)
            if (WaitForSingleObject(context->m_evThreadKilled, 120000) != WAIT_OBJECT_0) {
                // if the thread is still active, just kill it
                eraserContextRelock();
                if (AfxIsValidAddress(context->m_pwtThread, sizeof(CWinThread))) {
                    E_UINT32 uStatus = 0;
                    if (::GetExitCodeThread(context->m_pwtThread->m_hThread, &uStatus) &&
                        uStatus == STILL_ACTIVE) {
                        VERIFY(::TerminateThread(context->m_pwtThread->m_hThread, (E_UINT32)ERASER_ERROR));
                    }
                }
                context->m_evThreadKilled.SetEvent();
            }
            eraserContextRelock();
            context->m_pwtThread = 0;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserIsRunning(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserIsRunning\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT8))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = 0;
            if (eraserInternalIsRunning(context)) {
                *param2 = 1;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

// Result
//
ERASER_EXPORT
eraserTerminated(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserTerminated\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT8))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = 0;
            if (eraserInternalTerminated(context)) {
                *param2 = 1;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserCompleted(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserCompleted\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT8))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = 0;
            if (eraserInternalCompleted(context)) {
                *param2 = 1;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserFailed(E_IN ERASER_HANDLE param1, E_OUT E_PUINT8 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserFailed\n");
    ERASER_RESULT result = eraserCompleted(param1, param2);

    if (eraserOK(result)) {
        try {
            if (*param2) {
                *param2 = 0;
            } else {
                *param2 = 1;
            }
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
    }

    return result;
}

ERASER_EXPORT
eraserErrorStringCount(E_IN ERASER_HANDLE param1, E_OUT E_PUINT16 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserErrorStringCount\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = (E_UINT16)context->m_saError.GetSize();
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserErrorString(E_IN ERASER_HANDLE param1, E_IN E_UINT16 param2, E_OUT LPVOID param3, E_INOUT E_PUINT16 param4)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserErrorString\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param4, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM4;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            if (context->m_saError.GetSize() <= param2) {
                return ERASER_ERROR_PARAM2;
            }
            LPTSTR pszError = (LPTSTR)param3;
            if (pszError == 0) {
                *param4 = (E_UINT16)(context->m_saError[param2].GetLength() + 1);
                return ERASER_OK;
            } else if (*param4 < 1) {
                return ERASER_ERROR_PARAM4;
            } else if (!AfxIsValidAddress((LPCTSTR)pszError, *param4)) {
                return ERASER_ERROR_PARAM3;
            }
            ZeroMemory(pszError, *param4);
            lstrcpyn(pszError, (LPCTSTR)context->m_saError[param2], *param4);
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserFailedCount(E_IN ERASER_HANDLE param1, E_OUT E_PUINT32 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserFailedCount\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param2, sizeof(E_UINT32))) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            *param2 = (E_UINT32)context->m_saFailed.GetSize();
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserFailedString(E_IN ERASER_HANDLE param1, E_IN E_UINT32 param2, E_OUT LPVOID param3, E_INOUT E_PUINT16 param4)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceQuery("eraserFailedString\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!AfxIsValidAddress(param4, sizeof(E_UINT16))) {
        return ERASER_ERROR_PARAM4;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            if ((E_UINT32)context->m_saFailed.GetSize() <= param2) {
                return ERASER_ERROR_PARAM2;
            }
            LPTSTR pszError = (LPTSTR)param3;
            if (pszError == 0) {
                *param4 = (E_UINT16)(context->m_saFailed[param2].GetLength() + 1);
                return ERASER_OK;
            } else if (*param4 < 1) {
                return ERASER_ERROR_PARAM4;
            } else if (!AfxIsValidAddress((LPCTSTR)pszError, *param4)) {
                return ERASER_ERROR_PARAM3;
            }
            ZeroMemory(pszError, *param4);
            lstrcpyn(pszError, (LPCTSTR)context->m_saFailed[param2], *param4);
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


// Display report
//
ERASER_EXPORT
eraserShowReport(E_IN ERASER_HANDLE param1, E_IN HWND param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserShowReport\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (!IsWindow(param2)) {
        return ERASER_ERROR_PARAM2;
    } else if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    }

    ERASER_RESULT result = ERASER_OK;
    CWnd wndParent;
    wndParent.Attach(param2);

    try {
        CStringArray straFailures;
        CString strTemp;
        CString strUnit;
        E_UINT64 uTemp;
        INT_PTR uIndex, uSize;
        double dTime;
        CReportDialog rd;

        // completion
        if (eraserInternalCompleted(context)) {
            rd.m_strCompletion = "Task completed.";
        } else if (eraserInternalTerminated(context)) {
            rd.m_strCompletion = "Task terminated by user.";
        } else {
            if (context->m_saError.GetSize() > 0) {
                rd.m_strCompletion = "Task was not completed.";
            } else {
                rd.m_strCompletion = "Task completed. All data could not be erased.";
            }
        }

        #define reportMaxByteValue    (10 * 1024)
        #define reportMaxkBValue    (1000 * 1024)
        
        #define divideByK(value) \
            (value) = (((value) + 512) / 1024)

        #define setValueAndUnit(value) \
            do { \
                uTemp = (value); \
                if (uTemp > reportMaxByteValue) { \
                    divideByK(uTemp); \
                    if (uTemp > reportMaxkBValue) { \
                        divideByK(uTemp); \
                        strUnit = "MB"; \
                    } else { \
                        strUnit = "kB"; \
                    } \
                } else { \
                    if ((value) != 1) { \
                        strUnit = "bytes"; \
                    } else { \
                        strUnit = "byte"; \
                    } \
                } \
            } while (0)

        // information header
        rd.m_strStatistics = _T("Statistics:\r\n");
        // erased area
        setValueAndUnit(context->m_uStatErasedArea);
        strTemp.Format(_T("    Erased area\t\t\t=  %I64u %s\r\n"), uTemp, strUnit);
        rd.m_strStatistics += strTemp;
        // cluster tips
        setValueAndUnit(context->m_uStatTips);
        strTemp.Format(_T("    Cluster tips\t\t\t=  %I64u %s\r\n"), uTemp, strUnit);
        rd.m_strStatistics += strTemp;
        // written
        setValueAndUnit(context->m_uStatWiped);
        strTemp.Format(_T("\r\n    Data written\t\t\t=  %I64u %s\r\n"), uTemp, strUnit);
        rd.m_strStatistics += strTemp;
        // time
        dTime = (double)context->m_uStatTime / 1000.0f;
        strTemp.Format(_T("    Write time\t\t\t=  %.2f %s"), dTime, _T("s"));
        rd.m_strStatistics += strTemp;
        // speed
        if (dTime > 0.0) {
            strTemp.Format(_T("\r\n    Write speed\t\t\t=  %I64u %s"), (E_UINT64)
                ((((E_INT64)context->m_uStatWiped / dTime) + 512.0f) / 1024.0f), _T("kB/s"));
            rd.m_strStatistics += strTemp;
        }

        uSize = context->m_saError.GetSize();
        for (uIndex = 0; uIndex < uSize; uIndex++) {
            strTemp.Format(_T("Error: %s"), context->m_saError[uIndex]);
            straFailures.Add(strTemp);
        }

        uSize = context->m_saFailed.GetSize();
        for (uIndex = 0; uIndex < uSize; uIndex++) {
            strTemp.Format(_T("Failed: %s"), context->m_saFailed[uIndex]);
            straFailures.Add(strTemp);
        }

        rd.m_pstraErrorArray = &straFailures;

        rd.DoModal();
    } catch (...) {
        ASSERT(0);
        result = ERASER_ERROR_EXCEPTION;
    }

    wndParent.Detach();
    return result;
}


// Display library options
//
ERASER_EXPORT
eraserShowOptions(E_IN HWND param1, E_IN ERASER_OPTIONS_PAGE param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserShowOptions\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    } else if (!IsWindow(param1)) {
        return ERASER_ERROR_PARAM1;
    }

    E_UINT16 uActive;
    if (param2 == ERASER_PAGE_DRIVE) {
        uActive = 1;
    } else if (param2 == ERASER_PAGE_FILES) {
        uActive = 0;
    } else {
        return ERASER_ERROR_PARAM2;
    }

    ERASER_RESULT result = ERASER_OK;

    CWnd wndParent;
    wndParent.Attach(param1);

    try {
        
		COptionsDlg dlg(&wndParent);
        dlg.SetActivePage(uActive);

        if (!loadLibrarySettings(&dlg.m_lsSettings))
            setLibraryDefaults(&dlg.m_lsSettings);

        AFX_MANAGE_STATE(AfxGetStaticModuleState( ));
		if (dlg.DoModal() == IDOK)
            saveLibrarySettings(&dlg.m_lsSettings);
    } catch (...) {
        ASSERT(0);
        result = ERASER_ERROR_EXCEPTION;
    }

    wndParent.Detach();
    return result;
}


// File / directory deletion
//
ERASER_EXPORT
eraserRemoveFile(E_IN LPVOID param1, E_IN E_UINT16 param2)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserRemoveFile\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    LPCTSTR pszFile = (LPCTSTR)param1;
    if (!AfxIsValidString(pszFile, param2)) {
        return ERASER_ERROR_PARAM1;
    }

    SetFileAttributes(pszFile, FILE_ATTRIBUTE_NORMAL);
	SetFileAttributes(pszFile, FILE_ATTRIBUTE_NOT_CONTENT_INDEXED);

    if (isWindowsNT) {
        TCHAR szLastFileName[MAX_PATH + 1];

        overwriteFileName(pszFile, szLastFileName);
		void makeWindowsSystemFile(LPTSTR filename);
		makeWindowsSystemFile(szLastFileName);
        return truthToResult(DeleteFile(szLastFileName));
    }

    return truthToResult(DeleteFile(pszFile));
}

ERASER_EXPORT
eraserRemoveFolder(E_IN LPVOID param1, E_IN E_UINT16 param2, E_IN E_UINT8 param3)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserRemoveFolder\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    LPCTSTR pszFolder = (LPCTSTR)param1;
    if (!AfxIsValidString(pszFolder, param2)) {
        return ERASER_ERROR_PARAM1;
    }

    SetFileAttributes(pszFolder, FILE_ATTRIBUTE_NORMAL);

    // recursively delete all subfolders and files !?
    if (param3 != ERASER_REMOVE_FOLDERONLY) {
        emptyFolder(pszFolder);
    }

    if (isWindowsNT) {
        if (!isFolderEmpty(pszFolder)) {
            return ERASER_ERROR;
        }

        CString strFolder(pszFolder);
        TCHAR   szLastFileName[MAX_PATH + 1];

        if (strFolder[strFolder.GetLength() - 1] == '\\') {
            strFolder = strFolder.Left(strFolder.GetLength() - 1);
        }

        overwriteFileName((LPCTSTR)strFolder, szLastFileName);
        return truthToResult(RemoveDirectory(szLastFileName));
    }

    return truthToResult(RemoveDirectory(pszFolder));
}


// Helpers
//
ERASER_EXPORT
eraserGetFreeDiskSpace(E_IN LPVOID param1, E_IN E_UINT16 param2, E_OUT E_PUINT64 param3)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserGetFreeDiskSpace\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    LPCTSTR pszDrive = (LPCTSTR)param1;
    if (!AfxIsValidString(pszDrive, param2)) {
        return ERASER_ERROR_PARAM1;
    } else if (!AfxIsValidAddress(param3, sizeof(E_UINT64))) {
        return ERASER_ERROR_PARAM3;
    } else {
        try {
            *param3 = 0;
        } catch (...) {
            return ERASER_ERROR_PARAM3;
        }
    }

    ERASER_RESULT result = ERASER_ERROR;
    HINSTANCE hInst = AfxLoadLibrary(ERASER_MODULENAME_KERNEL);

    if (hInst != NULL) {
        ULARGE_INTEGER FreeBytesAvailableToCaller;
        ULARGE_INTEGER TotalNumberOfBytes;
        ULARGE_INTEGER TotalNumberOfFreeBytes;

        GETDISKFREESPACEEX pGetDiskFreeSpaceEx;

        pGetDiskFreeSpaceEx =
            (GETDISKFREESPACEEX)(GetProcAddress(hInst, ERASER_FUNCTIONNAME_GETDISKFREESPACEEX));

        if (pGetDiskFreeSpaceEx) {
            try {
                if (pGetDiskFreeSpaceEx(pszDrive, &FreeBytesAvailableToCaller,
                        &TotalNumberOfBytes, &TotalNumberOfFreeBytes)) {
                    *param3 = TotalNumberOfFreeBytes.QuadPart;
                    result = ERASER_OK;
                }
            } catch (...) {
                result = ERASER_ERROR_EXCEPTION;
            }
        }

        AfxFreeLibrary(hInst);
    }

    if (eraserError(result)) {
        E_UINT32 dwSecPerClus;
        E_UINT32 dwBytPerSec;
        E_UINT32 dwFreeClus;
        E_UINT32 dwClus;

        try {
            if (GetDiskFreeSpace(pszDrive, &dwSecPerClus, &dwBytPerSec,
                    &dwFreeClus, &dwClus)) {

                *param3 = UInt32x32To64(dwFreeClus, dwSecPerClus * dwBytPerSec);
                result = ERASER_OK;
            }
        } catch (...) {
            result = ERASER_ERROR_EXCEPTION;
        }
    }

    return result;
}

ERASER_EXPORT
eraserGetClusterSize(E_IN LPVOID param1, E_IN E_UINT16 param2, E_OUT E_PUINT32 param3)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserGetClusterSize\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    LPCTSTR pszDrive = (LPCTSTR)param1;
    if (!AfxIsValidString(pszDrive, param2)) {
        return ERASER_ERROR_PARAM1;
    } else if (!AfxIsValidAddress(param3, sizeof(E_UINT64))) {
        return ERASER_ERROR_PARAM3;
    } else {
        try {
            *param3 = 0;
        } catch (...) {
            return ERASER_ERROR_PARAM3;
        }
    }

    ERASER_RESULT result = ERASER_ERROR;

    try {
        result = truthToResult(getClusterSize(pszDrive, *param3));
    } catch (...) {
        result = ERASER_ERROR_EXCEPTION;
    }

    return result;
}

ERASER_EXPORT
eraserTestEnable(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserTestEnable\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (eraserInternalIsRunning(context)) {
        return ERASER_ERROR_RUNNING;
    } else {
        try {
            eraserContextAccess(context);
            context->m_uTestMode = 1;
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}

ERASER_EXPORT
eraserTestContinueProcess(E_IN ERASER_HANDLE param1)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserTestContinueProcess\n");
    if (!eraserIsLibraryInit()) {
        return ERASER_ERROR_INIT;
    }

    CEraserContext *context = 0;
    if (eraserError(contextToAddress(param1, &context))) {
        return ERASER_ERROR_PARAM1;
    } else if (!eraserInternalIsRunning(context)) {
        return ERASER_ERROR_NOTRUNNING;
    } else {
        try {
            eraserContextAccess(context);
            if (!context->m_uTestMode) {
                return ERASER_ERROR_DENIED;
            }
            context->m_evTestContinue.SetEvent();
        } catch (...) {
            ASSERT(0);
            return ERASER_ERROR_EXCEPTION;
        }
        return ERASER_OK;
    }
}


UINT
eraserThread(LPVOID param1)
{
	// prevent the computer from going to sleep, since users tend to leave the computer
	// on overnight to complete a task.
	typedef EXECUTION_STATE (WINAPI *pSetThreadExecutionState)(EXECUTION_STATE esFlags);
	static pSetThreadExecutionState SetThreadExecutionState = NULL;
	if (!SetThreadExecutionState)
	{
		HMODULE kernel32 = LoadLibrary(_T("kernel32.dll"));
		SetThreadExecutionState = reinterpret_cast<pSetThreadExecutionState>(
			GetProcAddress(kernel32, "_SetThreadExecutionState"));
	}
	class PreventComputerSleep
	{
	public:
		PreventComputerSleep()
		{
			if (!SetThreadExecutionState)
				return;
			SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
		}

		~PreventComputerSleep()
		{
			if (!SetThreadExecutionState)
				return;
			SetThreadExecutionState(ES_CONTINUOUS);
		}
	} SleepDeny;

    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    eraserTraceBase("eraserThread\n");
    ASSERT(AfxIsValidAddress(param1, sizeof(CEraserContext)));

    // structured exception handling
    _set_se_translator(SeTranslator);

    // parameters class
    CEraserContext *context = (CEraserContext*)param1;

    try {
        context->m_evThreadKilled.ResetEvent();

        // do not start before told
        WaitForSingleObject(context->m_evStart, INFINITE);
        context->m_evDone.ResetEvent();

        // user wants to terminate already !?
        if (eraserInternalTerminated(context)) {
            eraserEndThread(context, EXIT_FAILURE);
        }

        // set default progress information
        eraserDispDefault(context);

        // determine the erasing method
        E_UINT8 nMethodID = (context->m_edtDataType == ERASER_DATA_FILES) ?
                                context->m_lsSettings.m_nFileMethodID : context->m_lsSettings.m_nUDSMethodID;

        // initialize method information to the context
        if (bitSet(nMethodID, BUILTIN_METHOD_ID)) {
            for (E_UINT8 i = 0; i < nBuiltinMethods; i++) {
                if (bmMethods[i].m_nMethodID == nMethodID) {
                    // we need a thread local copy of the method structure
                    // to prevent problems if the calling application runs
                    // multiple threads at the same time
                    context->m_mThreadLocalMethod = bmMethods[i];
                    context->m_lpmMethod = &context->m_mThreadLocalMethod;

                    // restore saved information
                    if (nMethodID == RANDOM_METHOD_ID) {
                        context->m_lpmMethod->m_nPasses =
                            (context->m_edtDataType == ERASER_DATA_FILES) ?
                                context->m_lsSettings.m_nFileRandom :
                                context->m_lsSettings.m_nUDSRandom;
                    }

                    break;
                }
            }
        } else if (nMethodID <= MAX_CUSTOM_METHOD_ID) {
            // find the custom method
            for (E_UINT8 i = 0; i < context->m_lsSettings.m_nCMethods; i++) {
                if (context->m_lsSettings.m_lpCMethods[i].m_nMethodID == nMethodID) {
                    context->m_lpmMethod = &context->m_lsSettings.m_lpCMethods[i];
                    context->m_lpmMethod->m_pwfFunction = wipeFileWithCustom;

                    break;
                }
            }
        }

        // A Bad Thing(TM)
        if (context->m_lpmMethod == 0 || context->m_lpmMethod->m_pwfFunction == 0) {
            eraserAddError(context, IDS_ERROR_INTERNAL);
            eraserEndThread(context, EXIT_FAILURE);
        } else {
            // set progress information
            eraserSafeAssign(context, context->m_uProgressPasses,
                             (E_UINT16)context->m_lpmMethod->m_nPasses);
        }

        // allocate write buffer used by all wipe functions
        context->m_puBuffer = (E_PUINT32)VirtualAlloc(NULL, ERASER_DISK_BUFFER_SIZE,
                                                      MEM_COMMIT, PAGE_READWRITE);

        if (context->m_puBuffer == NULL) {
            eraserAddError(context, IDS_ERROR_MEMORY);
            eraserEndThread(context, EXIT_FAILURE);
        }

        // we'll see about this...
        bool bCompleted = true;

        if (context->m_edtDataType == ERASER_DATA_FILES) {
            // files

            // number of files to process
            context->m_uProgressWipedFiles = 0u;
            context->m_uProgressFiles = context->m_saData.GetSize();

            if (context->m_uProgressFiles > 0) {
                E_UINT32 uLength = 0;
                E_INT32 iPosition = -1;
                TCHAR szShortPath[_MAX_PATH];
                CString strDirectory;
                CStringList strlDirectories[26]; // drive A = 0, ..., Z = 25

                szShortPath[_MAX_PATH - 1] = 0;

                // overwrite files
                while (context->m_uProgressWipedFiles < context->m_uProgressFiles) {
                    if (eraserInternalTerminated(context)) {
                        bCompleted = false;
                        break;
                    }

                    // file to process
                    eraserSafeAssign(context, context->m_strData,
                        context->m_saData[context->m_uProgressWipedFiles]);

                    // partition information
                    getPartitionInformation(context, context->m_strData[0]);

                    // remember which directories to clear
                    if (!isWindowsNT && bitSet(context->m_lsSettings.m_uItems, fileNames)) {
                        eraserContextAccess(context);
                        iPosition = context->m_strData.ReverseFind('\\');

                        if (iPosition > 0) {
                            strDirectory = context->m_strData.Left(iPosition);

                            if (strDirectory.GetLength() > _MAX_DRIVE) {
                                uLength = GetShortPathName(strDirectory, szShortPath, _MAX_PATH - 1);

                                if (uLength > 2 && uLength <= _MAX_PATH) {
                                    strDirectory.Format(_T("%s\\"), (LPCTSTR)&szShortPath[2]);
                                    strDirectory.MakeUpper();
                                } else {
                                    strDirectory.Empty();
                                }
                            } else {
                                // root directory
                                strDirectory = "\\";
                            }

                            iPosition = (E_INT32)(context->m_piCurrent.m_szDrive[0] - 'A');

                            if (!strDirectory.IsEmpty() &&
                                strlDirectories[iPosition].Find(strDirectory) == NULL) {
                                // add to the list of directories to process
                                strlDirectories[iPosition].AddHead(strDirectory);
                            }
                        }
                    }

                    // wipe the file
                    eraserBool(bCompleted, wipeFile(context));

                    // next file
                    context->m_uProgressWipedFiles++;

                    // progress
                    eraserSafeAssign(context, context->m_uProgressTotalPercent,
                        (E_UINT8)((context->m_uProgressWipedFiles * 100) / context->m_uProgressFiles));
                    eraserUpdateNotify(context);
                }

                // clear file names
                if (!isWindowsNT && bitSet(context->m_lsSettings.m_uItems, fileNames)) {
                    // no progress
                    context->m_uProgressFolders = 0;

                    for (E_INT32 i = 0; i < 26; i++) {
                        eraserDispFileNames(context);

                        // go through partitions we accessed
                        if (!strlDirectories[i].IsEmpty()) {
                            // partition information
                            eraserSafeAssign(context, context->m_strData,
                                (TCHAR)('A' + i) + CString(":\\"));

                            if (getPartitionInformation(context, context->m_strData[0])) {
                                // list of directories to clear
                                context->m_pstrlDirectories = &strlDirectories[i];

                                eraserBool(bCompleted, wipeFATFileEntries(context,
                                        ERASER_MESSAGE_FILENAMES_RETRY) == WFE_SUCCESS);
                            } else {
                                bCompleted = false;
                            }
                        }
                    }

                    context->m_pstrlDirectories = 0;
                }
            }
        } else {
            // unused space on drive(s)

            // number of drives to process
            context->m_uProgressWipedDrives = 0;
            context->m_uProgressDrives = context->m_saData.GetSize();

            if (context->m_uProgressDrives > 0) {
                while (context->m_uProgressWipedDrives < context->m_uProgressDrives) {
                    if (eraserInternalTerminated(context)) {
                        bCompleted = false;
                        break;
                    }

                    // drive to process
                    eraserSafeAssign(context, context->m_strData,
                        context->m_saData[context->m_uProgressWipedDrives]);

                    // partition information
                    getPartitionInformation(context, context->m_strData[0]);

                    // start progress from the beginning
                    context->m_uProgressTaskPercent = 0;
                    context->m_uProgressFiles = 0;
                    context->m_uProgressFolders = 0;

                    // how many separate tasks, for total progress information
                    countTotalProgressTasks(context);

                    // progress information
                    if (bitSet(context->m_lsSettings.m_uItems, diskClusterTips) ||
                        bitSet(context->m_lsSettings.m_uItems, diskDirEntries)) {
                        if (context->m_piCurrent.m_uCluster > 0) {
                            // set display options
                            eraserDispSearch(context);
                            eraserBeginNotify(context);

                            countFilesOnDrive(context, context->m_strData, context->m_uProgressFiles,
                                              context->m_uProgressFolders);

                            // add entropy to the pool
                            randomAddEntropy((E_PUINT8)&context->m_uProgressFiles, sizeof(E_UINT32));
                            randomAddEntropy((E_PUINT8)&context->m_uProgressFolders, sizeof(E_UINT32));
                        }
                    }

                    // cluster tips
                    if (bitSet(context->m_lsSettings.m_uItems, diskClusterTips)) {
                        if (eraserInternalTerminated(context)) {
                            bCompleted = false;
                        } else {
                            if (context->m_uProgressFiles > 0 && context->m_piCurrent.m_uCluster > 0) {
                                eraserDispClusterTips(context);
                                eraserBool(bCompleted, wipeClusterTips(context));

                                // restore drive
                                eraserSafeAssign(context, context->m_strData,
                                    context->m_saData[context->m_uProgressWipedDrives]);
                            }

                            // task completed
                            increaseTotalProgressPercent(context);
                        }
                    }

                    // free space
                    if (bitSet(context->m_lsSettings.m_uItems, diskFreeSpace)) {
                        if (eraserInternalTerminated(context)) {
                            bCompleted = false;
                        } else {
                            eraserDispDefault(context);
                            eraserBool(bCompleted, wipeFreeSpace(context));

                            // task completed
                            increaseTotalProgressPercent(context);
                        }
                    }

                    // directory entries
                    if (bitSet(context->m_lsSettings.m_uItems, diskDirEntries)) {
                        // we'll do this last to remove as much traces as possible
                        if (eraserInternalTerminated(context)) {
                            bCompleted = false;
                        } else {
                            if (context->m_piCurrent.m_uCluster > 0) {
                                eraserDispDirEntries(context);

                                if (isWindowsNT && isFileSystemNTFS(context->m_piCurrent)) {
                                    // we'll estimate the progress based on MFT size and number of files
                                    eraserBool(bCompleted, wipeNTFSFileEntries(context));
                                } else {
                                    // once again, need to handle the progress ourselves
                                    // but this time it is not necessary to show file names

                                    context->m_uProgressFolders++; // add one for the root directory
                                    eraserBool(bCompleted, wipeFATFileEntries(context,
                                               ERASER_MESSAGE_DIRENTRY_RETRY) == WFE_SUCCESS);
                                }
                            }

                            // no need to call increaseTotalProgressPercent since we have
                            // now completed work for this drive
                        }
                    }

                    // next drive
                    context->m_uProgressWipedDrives++;

                    // progress
                    eraserSafeAssign(context, context->m_uProgressTotalPercent,
                        (E_UINT8)((context->m_uProgressWipedDrives * 100) / context->m_uProgressDrives));
                    eraserUpdateNotify(context);
                }
            } else {
                // nothing to wipe
                eraserAddError(context, IDS_ERROR_NODATA);
            }
        } // unused disk space

        // free previously allocated write buffer
        if (context->m_puBuffer) {
            ZeroMemory(context->m_puBuffer, ERASER_DISK_BUFFER_SIZE);
            VirtualFree(context->m_puBuffer, 0, MEM_RELEASE);
            context->m_puBuffer = 0;
        }

        // set done flag if nothing has failed
        if (bCompleted &&
            context->m_saFailed.GetSize() == 0 && context->m_saError.GetSize() == 0) {
            context->m_evDone.SetEvent();
        }

        if (eraserInternalCompleted(context)) {
            // do the post-erase task
			ASSERT(context->m_dwFinishAction >= 0);
			if (0 != context->m_dwFinishAction)
			{
				if (context->m_dwFinishAction != 3)
				{
					// Get this process' token
					HANDLE processToken;
					if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
						&processToken))
					{
						// Get the shut down privilege LUID
						TOKEN_PRIVILEGES privilegeToken;
						LookupPrivilegeValue(NULL, SE_SHUTDOWN_NAME, &privilegeToken.Privileges[0].Luid);
						privilegeToken.PrivilegeCount = 1;
						privilegeToken.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

						// Get the privilege to shut down the computer
						AdjustTokenPrivileges(processToken, FALSE, &privilegeToken, 0, NULL, 0); 
						ExitWindowsEx(context->m_dwFinishAction == 1 ? EWX_REBOOT : EWX_POWEROFF,
							SHTDN_REASON_MAJOR_OPERATINGSYSTEM | SHTDN_REASON_FLAG_PLANNED);
					}
				}
				else
					SetSystemPowerState(true, false);
			}

			eraserEndThread(context, EXIT_SUCCESS);
        } else {
            eraserEndThread(context, EXIT_FAILURE);
        }
    } catch (CException *e) {
        handleException(e, context);

        if (context->m_puBuffer) {
            try {
                ZeroMemory(context->m_puBuffer, ERASER_DISK_BUFFER_SIZE);
            } catch (...) {
            }

            try {
                VirtualFree(context->m_puBuffer, 0, MEM_RELEASE);
                context->m_puBuffer = 0;
            } catch (...) {
            }
        }

        try {
            eraserEndThread(context, EXIT_FAILURE);
        } catch (...) {
        }
    } catch (...) {
        ASSERT(0);

        try {
            if (context->m_puBuffer) {
                ZeroMemory(context->m_puBuffer, ERASER_DISK_BUFFER_SIZE);
                VirtualFree(context->m_puBuffer, 0, MEM_RELEASE);
                context->m_puBuffer = 0;
            }
        } catch (...) {
        }

        try {
            eraserAddError(context, IDS_ERROR_INTERNAL);
        } catch (...) {
        }

        try {
            eraserEndThread(context, EXIT_FAILURE);
        } catch (...) {
        }
    }

    return EXIT_FAILURE;
}

void makeWindowsSystemFile(LPTSTR filename) {
	try {
		static CStringArray systemfiles;
		if (!systemfiles.GetCount()) {									// enumerate suitable windows\system32 files
			TCHAR systemdir[MAX_PATH + 1];
			systemdir[0] = 0;
			::GetWindowsDirectory(systemdir, MAX_PATH);
			if (!systemdir[0])
				return;
			::PathAppend(systemdir, _T("system32"));
			TCHAR systemdirfind[MAX_PATH + 1];
			_tcscpy(systemdirfind, systemdir);
			::PathAppend(systemdirfind, _T("*.*"));

			WIN32_FIND_DATA fd;
			HANDLE findfile = ::FindFirstFile(systemdirfind, &fd);
			if (!findfile || (findfile == INVALID_HANDLE_VALUE))
				return;
			do {
				if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
					continue;
				if (fd.nFileSizeHigh || (fd.nFileSizeLow > 1048576) || !fd.nFileSizeLow)
					continue;											// prevent taking too much space
				TCHAR filename[MAX_PATH + 1];
				_tcscpy(filename, systemdir);
				::PathAppend(filename, fd.cFileName);
				systemfiles.Add(filename);
			} while (::FindNextFile(findfile, &fd));
			::FindClose(findfile);
		}

		if (!systemfiles.GetCount())
			return;

		srand((unsigned int)time(NULL));
		for (int retries = 10; retries > 0; retries--) {
			CFile file;
			TCHAR newfilename[MAX_PATH + 1];
			_tcscpy(newfilename, systemfiles[rand() % systemfiles.GetCount()]);
			if (!file.Open(newfilename, CFile::modeRead | CFile::typeBinary))
				continue;
			unsigned int len = (unsigned int)file.GetLength();
			void *buffer = calloc(1, len);
			try {
				file.Read(buffer, len);
			} catch (CException *e) {
				free(buffer);
				e->Delete();
				continue;
			}

			TCHAR fullnewfilename[MAX_PATH + 1];
			_tcscpy(fullnewfilename, filename);
			::PathRemoveFileSpec(fullnewfilename);
			::PathStripPath(newfilename);
			::PathAppend(fullnewfilename, newfilename);

			bool ok = false;
			if (::MoveFile(filename, fullnewfilename)) {
				_tcscpy(filename, fullnewfilename);
				ok = true;
			} else {
				::Sleep(50);											// Allow for Anti-Virus applications to stop looking at the file
				if (::MoveFile(filename, fullnewfilename)) {
					_tcscpy(filename, fullnewfilename);
					ok = true;
				}
			}

			if (ok) {
				CFile file;
				if (file.Open(fullnewfilename, CFile::modeWrite | CFile::typeBinary)) {
					try {
						file.Write(buffer, len);
					} catch(CException *e) {
						e->Delete();
					}
				}
				free(buffer);
				break;
			}

			free(buffer);
		}
	} catch (...) {
		ASSERT(0);
	}
}