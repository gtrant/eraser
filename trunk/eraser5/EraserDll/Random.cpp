// Random.cpp
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
//
//
//   Yes, all this only for overwriting. Call me crazy.
//
// A slightly modified version of cryptlib's cryptographically strong
// pseudorandom number generator generator described in Peter
// Gutmann's paper "Software generation of practically strong random
// numbers" (1998).
//
// This generator also includes another output PRNG for creating large
// amounts of random data - fast. It uses the ISAAC algorithm by Bob
// Jenkins.
//
// An implementation of the Tiger/192 hash function by Ross Anderson
// and Eli Biham is used for pool mixing and output instead of SHA-1
// or MD5 suggested in the paper.
//
// The code for entropy polling has been borrowed from GNU Privacy
// Guard 1.0.6 and cryptlib 3.0. The original source code contained
// this copyright notice:

/*
** The random pool handling code in this module and the misc/rnd*.c
** modules represent the cryptlib continuously seeded pseudorandom
** number generator (CSPRNG) as described in my 1998 Usenix Security
** Symposium paper "The generation of practically strong random
** numbers".
**
** The CSPRNG code is copyright Peter Gutmann (and various others)
** 1995-1999 all rights reserved.  Redistribution of the CSPRNG
** modules and use in source and binary forms, with or without
** modification, are permitted provided that the following conditions
** are met:
**
** 1. Redistributions of source code must retain the above copyright
** notice and this permission notice in its entirety.
**
** 2. Redistributions in binary form must reproduce the copyright
** notice in the documentation and/or other materials provided with
** the distribution.
**
** 3. A copy of any bugfixes or enhancements made must be provided
** to the author, <pgut001@cs.auckland.ac.nz> to allow them to be
** added to the baseline version of the code.
**
** ALTERNATIVELY, the code may be distributed under the terms of the
** GNU General Public License, version 2 or any later version
** published by the Free Software Foundation, in which case the
** provisions of the GNU GPL are required INSTEAD OF the above
** restrictions.
**
** Although not required under the terms of the GPL, it would still
** be nice if you could make any changes available to the author to
** allow a consistent code base to be maintained
*/


#include "stdafx.h"
#include "EraserDll.h"
#include "Common.h"
#include "Tiger.h"
#include "..\shared\UserInfo.h"
#include "..\shared\key.h"
#include "Random.h"
#include <winioctl.h>
#include <winperf.h>
#include <process.h>

/*
** Variables
*/

static CRITICAL_SECTION pageLock;

static E_PUINT8 poolPage = 0;

static ISAAC_STATE *isaacState = 0;
static E_PUINT32 isaacOutput = 0;
static E_PUINT8 entropyPool = 0;
static E_PUINT8 tempBuffer = 0;
static E_PUINT8 outputBuffer = 0;

static E_INT32 randomQuality = 0;
static E_INT32 poolMixes = 0;

static UINT_PTR isaacOutputPosition = 0;
static LARGE_INTEGER lastCall;

/*
** Reference counter
*/

static E_INT32 refCount = 0;
static CRITICAL_SECTION refLock;

/*
** Thread control
*/

static bool   enableSlowPoll = false;
static HANDLE stopThreads = NULL;
static HANDLE refreshThreadStopped = NULL;
static HANDLE pollThreadStopped = NULL;

/*
** Declarations
*/

static UINT refreshThread(LPVOID);
static UINT pollThread(LPVOID);


/*
** CryptoAPI function pointers and handles
*/

static HINSTANCE dllAdvAPI = NULL;
static HCRYPTPROV cryptoContext = NULL;

static CRYPTACQUIRECONTEXT pCryptAcquireContext = NULL;
static CRYPTGENRANDOM pCryptGenRandom = NULL;
static CRYPTRELEASECONTEXT pCryptReleaseContext = NULL;

/*
** CryptoAPI variables
*/

static E_UINT32 qualityCryptoAPI = qualityMicrosoft;

/*
** CryptoAPI initialization
*/

static void
releaseCryptoAPI()
{
    /*
    ** If we have acquired a context, try to release it
    */

    if (pCryptReleaseContext != NULL && cryptoContext != NULL) {
        try {
            pCryptReleaseContext(cryptoContext, 0);
        } catch (...) {
            ASSERT(0);
        }
    }
    cryptoContext = NULL;

    /*
    ** Release library
    */

    if (dllAdvAPI != NULL) {
        AfxFreeLibrary(dllAdvAPI);
        dllAdvAPI = NULL;
    }

    /*
    ** Clear function pointers and variables
    */

    pCryptAcquireContext = NULL;
    pCryptGenRandom = NULL;
    pCryptReleaseContext = NULL;
    qualityCryptoAPI = qualityMicrosoft;
}

static void
loadCryptoAPI()
{
    /*
    ** If library isn't loaded, load it now
    */

    if (dllAdvAPI == NULL) {
        dllAdvAPI = AfxLoadLibrary(RANDOM_MODULE_ADVAPI32);
    }

    /*
    ** If library was found, locate functions
    */

    if (dllAdvAPI != NULL) {
        pCryptAcquireContext = (CRYPTACQUIRECONTEXT)GetProcAddress(dllAdvAPI,
                                                        RANDOM_FUNCTION_CRYPTACQUIRECONTEXT);
        pCryptGenRandom = (CRYPTGENRANDOM)GetProcAddress(dllAdvAPI,
                                                        RANDOM_FUNCTION_CRYPTGENRANDOM);
        pCryptReleaseContext = (CRYPTRELEASECONTEXT)GetProcAddress(dllAdvAPI,
                                                        RANDOM_FUNCTION_CRYPTRELEASECONTEXT);

        if (pCryptAcquireContext != NULL && pCryptGenRandom != NULL &&
            pCryptReleaseContext != NULL) {
            try {
                /*
                ** Try to connect to Intel's hardware RNG if one is installed
                */

                if (pCryptAcquireContext(&cryptoContext, NULL, INTEL_DEF_PROV,
                                         PROV_INTEL_SEC, 0)) {
                    qualityCryptoAPI = qualityIntel;
                    return;
                }

                /*
                ** Default cryptographic service provider
                */

                qualityCryptoAPI = qualityMicrosoft;

                if (pCryptAcquireContext(&cryptoContext, NULL, NULL, PROV_RSA_FULL, 0)) {
                    return;
                } else if (GetLastError() == NTE_BAD_KEYSET) {
                    /*
                    ** Default keyset may not exist, attempt to create new
                    */

                    if (pCryptAcquireContext(&cryptoContext, NULL, NULL, PROV_RSA_FULL,
                                             CRYPT_NEWKEYSET)) {
                        return;
                    }
                }
            } catch (...) {
                ASSERT(0);
            }
        }

        /*
        ** CryptoAPI not present or couldn't connect to a CSP
        */

        releaseCryptoAPI();
    }
}

/*
** Windows ToolHelp32 API function pointers and handles
*/

static HINSTANCE dllKernel = NULL;

static CREATESNAPSHOT pCreateToolhelp32Snapshot = NULL;
static MODULEWALK pModule32First = NULL;
static MODULEWALK pModule32Next = NULL;
static PROCESSWALK pProcess32First = NULL;
static PROCESSWALK pProcess32Next = NULL;
static THREADWALK pThread32First = NULL;
static THREADWALK pThread32Next = NULL;
static HEAPLISTWALK pHeap32ListFirst = NULL;
static HEAPLISTWALK pHeap32ListNext = NULL;
static HEAPFIRST pHeap32First = NULL;
static HEAPNEXT pHeap32Next = NULL;

/*
** Windows ToolHelp32 API initialization
*/

static void
releaseToolHelpAPI()
{
    /*
    ** Free library
    */

    if (dllKernel != NULL) {
        AfxFreeLibrary(dllKernel);
        dllKernel = NULL;
    }

    /*
    ** Clear function pointers
    */

    pCreateToolhelp32Snapshot = NULL;
    pModule32First = NULL;
    pModule32Next = NULL;
    pProcess32First = NULL;
    pProcess32Next = NULL;
    pThread32First = NULL;
    pThread32Next = NULL;
    pHeap32ListFirst = NULL;
    pHeap32ListNext = NULL;
    pHeap32First = NULL;
    pHeap32Next = NULL;
}

static void
loadToolHelpAPI()
{
    /*
    ** Load library, kernel should at least be present
    */

    if (dllKernel == NULL) {
        dllKernel = AfxLoadLibrary(RANDOM_MODULE_KERNEL32);
    }

    /*
    ** Try to locate ToolHelp32 functions
    */

    if (dllKernel != NULL) {
        pModule32First   = (MODULEWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_MODULE32FIRST);
        pModule32Next    = (MODULEWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_MODULE32NEXT);
        pProcess32First  = (PROCESSWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_PROCESS32FIRST);
        pProcess32Next   = (PROCESSWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_PROCESS32NEXT);
        pThread32First   = (THREADWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_THREAD32FIRST);
        pThread32Next    = (THREADWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_THREAD32NEXT);
        pHeap32ListFirst = (HEAPLISTWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_HEAP32LISTFIRST);
        pHeap32ListNext  = (HEAPLISTWALK)GetProcAddress(dllKernel, RANDOM_FUNCTION_HEAP32LISTNEXT);
        pHeap32First     = (HEAPFIRST)GetProcAddress(dllKernel, RANDOM_FUNCTION_HEAPFIRST);
        pHeap32Next      = (HEAPNEXT)GetProcAddress(dllKernel, RANDOM_FUNCTION_HEAPNEXT);
        pCreateToolhelp32Snapshot = (CREATESNAPSHOT)GetProcAddress(dllKernel, RANDOM_FUNCTION_CREATESNAPSHOT);

        if (pModule32First   != NULL && pModule32Next   != NULL &&
            pProcess32First  != NULL && pProcess32Next  != NULL &&
            pThread32First   != NULL && pThread32Next   != NULL &&
            pHeap32ListFirst != NULL && pHeap32ListNext != NULL &&
            pHeap32First     != NULL && pHeap32Next     != NULL &&
            pCreateToolhelp32Snapshot != NULL) {
            /*
            ** ToolHelp32 loaded successfully
            */
            return;
        }
    }

    /*
    ** Failed to load all
    */

    releaseToolHelpAPI();
}

/*
** Windows NT NetAPI32 function pointers and handles
*/

static HINSTANCE dllNetAPI = NULL;

static NETSTATISTICSGET2 pNetStatisticsGet = NULL;
static NETAPIBUFFERSIZE pNetApiBufferSize = NULL;
static NETAPIBUFFERFREE pNetApiBufferFree = NULL;

/*
** Windows NT Native function pointers and handles
*/

static HINSTANCE dllNTAPI = NULL;
static NTQUERYSYSTEMINFO pNtQuerySystemInfo = NULL;

/*
** Windows NT-specific variables
*/

static bool isNTWorkstation = false;

/*
** Windows NT-specific initialization
*/

static void
releaseDependenciesNetAPI()
{
    /*
    ** Release library
    */

    if (dllNetAPI != NULL) {
        AfxFreeLibrary(dllNetAPI);
        dllNetAPI = NULL;
    }

    /*
    ** Clear function pointers
    */

    pNetStatisticsGet = NULL;
    pNetApiBufferSize = NULL;
    pNetApiBufferFree = NULL;
}

static void
releaseDependenciesNativeAPI()
{
    /*
    ** Release library for Native API and clean up
    */

    if (dllNTAPI != NULL) {
        AfxFreeLibrary(dllNTAPI);
        dllNTAPI = NULL;
    }
    pNtQuerySystemInfo = NULL;
}

static void
releaseDependenciesNT()
{
    releaseDependenciesNetAPI();
    releaseDependenciesNativeAPI();
}

static void
loadDependenciesNT()
{
    HKEY hKey;

    /*
    ** Attempt to load NetAPI32 and locate functions
    */

    if (dllNetAPI == NULL) {
        dllNetAPI = AfxLoadLibrary(RANDOM_MODULE_NETAPI);
    }

    if (dllNetAPI != NULL) {
        pNetStatisticsGet = (NETSTATISTICSGET2)GetProcAddress(dllNetAPI, RANDOM_FUNCTION_NETSTATISTICSGET2);
        pNetApiBufferSize = (NETAPIBUFFERSIZE)GetProcAddress(dllNetAPI, RANDOM_FUNCTION_NETAPIBUFFERSIZE);
        pNetApiBufferFree = (NETAPIBUFFERFREE)GetProcAddress(dllNetAPI, RANDOM_FUNCTION_NETAPIBUFFERFREE);

        if (pNetStatisticsGet == NULL || pNetApiBufferSize == NULL || pNetApiBufferFree == NULL) {
            releaseDependenciesNetAPI();
        }
    }

    /*
    ** Attempt to locate Native API and load functions
    */

    if (dllNTAPI == NULL) {
        dllNTAPI = AfxLoadLibrary(RANDOM_MODULE_NTDLL);
    }

    if (dllNTAPI != NULL) {
        pNtQuerySystemInfo = (NTQUERYSYSTEMINFO)GetProcAddress(dllNTAPI, RANDOM_FUNCTION_NTQUERYSYSTEMINFO);
        if (pNtQuerySystemInfo == NULL) {
            releaseDependenciesNativeAPI();
        }
    }

    /*
    ** Determine whether this is NT Workstation or NT Server (for querying network statistics)
    */

    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, RANDOM_KEY_PRODUCTOPTIONS,
                     0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        _TCHAR szValue[32];
        E_UINT32 uSize = sizeof(szValue);
        E_UINT32 status;

        isNTWorkstation = true;
        status = RegQueryValueEx(hKey, RANDOM_KEY_PRODUCTTYPE, 0,
                                 NULL, (LPBYTE)szValue, &uSize);

        if (status == ERROR_SUCCESS &&
            _tcsicmp(szValue, RANDOM_NTWORKSTATION_TOKEN)) {
            isNTWorkstation = false;
        }

        RegCloseKey(hKey);
    }
}

/*
** Helpers
*/

static void
clearBuffer(E_PUINT8 buffer, UINT_PTR size)
{
    if (size == 0) {
        return;
    }

    /*
    ** Fills given buffer with random, but not necessarily unpredictable data
    */

    ASSERT(hashSize >= sizeof(SYSTEMTIME));

    E_UINT32 copied = 0;
    E_UINT8 data[hashSize];
    E_UINT8 output[hashSize];

    /*
    ** Zero fill first
    */

    ZeroMemory(buffer, size);

    try {
        /*
        ** Hash current time N times to fill the buffer
        */

        GetSystemTime((LPSYSTEMTIME)data);

        while (copied < size) {
            tiger((E_PUINT64)data, hashSize, (E_PUINT64)output);

            memcpy(&buffer[copied], output, min(hashSize, (size - copied)));
            memcpy(data, output, hashSize);

            copied += hashSize;
        }
    } catch (...) {
        ASSERT(0);
    }

    ZeroMemory(data, hashSize);
    ZeroMemory(output, hashSize);
}

/*
** Pool management
*/

static void
setPoolPointers(E_PUINT8 page)
{
    isaacState   = (ISAAC_STATE*)(page + isaacStateOffset);
    isaacOutput  =    (E_PUINT32)(page + isaacOutputOffset);
    entropyPool  =     (E_PUINT8)(page + poolOffset);
    outputBuffer =     (E_PUINT8)(page + outputOffset);
    tempBuffer   =     (E_PUINT8)(page + tempOffset);
}

static E_PUINT8
allocatePoolPage()
{
    E_PUINT8 newPage = (E_PUINT8)VirtualAlloc(NULL, pageSize, MEM_COMMIT, PAGE_READWRITE);

    if (newPage != NULL) {
        /*
        ** VirtualLock doesn't work outside NT, and even there it is
        ** not respected when no threads are active. However, I suppose
        ** everything helps.
        */

        VirtualLock(newPage, pageSize);
        clearBuffer(newPage, pageSize);
    }

    return newPage;
}

static void
freePoolPage(E_PUINT8 *page)
{
    if (page != NULL && *page != NULL) {
        /*
        ** Clear page contents and unlock before releasing memory
        */

        clearBuffer(*page, pageSize);
        VirtualUnlock(*page, pageSize);

        VirtualFree(*page, 0, MEM_RELEASE);
        *page = NULL;
    }
}

inline static void
touchPool()
{
    static E_UINT32 index = 0;

    /*
    ** Try to keep the page in memory by touching it regularly.
    */

    /*
    ** This won't require page locking as no significant data
    ** is touched and the page cannot be released or moved.
    */

    try {
        if (index >= unusedSize) {
            index = 0;
        }
        poolPage[index++]--;
    } catch (...) {
        ASSERT(0);
    }
}

static bool
movePool()
{
    /*
    ** Try to make it more difficult to find the pool by changing page
    ** location in memory every now and then. May also help when trying
    ** to keep this out of swap file.
    */

    E_PUINT8 newPage = allocatePoolPage();

    if (newPage != NULL) {
        bool result = false;

        EnterCriticalSection(&pageLock);

        try {
            /*
            ** Copy only significant portion of the page
            */
            memcpy(&newPage[unusedSize], &poolPage[unusedSize], pageSize - unusedSize);
            freePoolPage(&poolPage);

            poolPage = newPage;
            newPage = NULL;

            setPoolPointers(poolPage);

            result = true;
        } catch (...) {
            if (newPage != NULL) {
                freePoolPage(&newPage);
            }
            ASSERT(0);
        }

        LeaveCriticalSection(&pageLock);

        return result;
    }
    return false;
}

/*
** Pool mixing and adding entropy
*/

inline static void
creditPool(const E_INT32 quality)
{
    if (randomQuality < requiredQuality) {
        randomQuality += quality;
    }
}

inline static void
mixPool(E_PUINT8 poolPtr)
{
    /*
    ** We use the temporary work area, so it cannot be mixed
    */

    ASSERT(poolPtr != tempBuffer);

    /*
    ** If no pool pointer is given, use the default entropy pool
    */

    if (poolPtr == NULL) {
        poolPtr = entropyPool;

        /*
        ** Mixing again
        */

        if (poolMixes < requiredMixes) {
            poolMixes++;
        }
    }

    /*
    ** Change something every time before generating output,
    ** increasing the counter will do (first 64 bits of the pool)
    */

    ((E_PUINT64)poolPtr)[0]++;

    /*
    ** Treat entropyPool as a circular buffer and mix (starting from n = 0) using
    ** hash function's compression function only:
    **
    ** [n] ... [n + hashSize - 1] = hash_compress([n - hashSize] ... [n + dataSize - 1])
    */

    E_UINT32 i;

    /*
    ** First hashSize bytes
    */

    memcpy(tempBuffer, &poolPtr[entropyPoolSize - hashSize], hashSize);
    memcpy(&tempBuffer[hashSize], poolPtr, dataSize);

    tiger_compress((E_PUINT64)&tempBuffer[0], (E_PUINT64)poolPtr);
    tiger_compress((E_PUINT64)&tempBuffer[blockSize], (E_PUINT64)poolPtr);

    /*
    ** Middle blocks
    */

    for (i = 0; i < (entropyPoolSize - mixSize); i += hashSize) {
        memcpy(tempBuffer, &poolPtr[i], mixSize);

        tiger_compress((E_PUINT64)&tempBuffer[0], (E_PUINT64)&poolPtr[i + hashSize]);
        tiger_compress((E_PUINT64)&tempBuffer[blockSize], (E_PUINT64)&poolPtr[i + hashSize]);
    }

    /*
    ** Last < mixSize bytes
    */

    for (; i < (entropyPoolSize - hashSize); i += hashSize) {
        memcpy(tempBuffer, &poolPtr[i], entropyPoolSize - i);
        memcpy(&tempBuffer[entropyPoolSize - i], poolPtr, mixSize - (entropyPoolSize - i));

        tiger_compress((E_PUINT64)&tempBuffer[0], (E_PUINT64)&poolPtr[i + hashSize]);
        tiger_compress((E_PUINT64)&tempBuffer[blockSize], (E_PUINT64)&poolPtr[i + hashSize]);
    }

    /*
    ** Clean used work area
    */

    clearBuffer(tempBuffer, mixSize);
}

inline static void
invertPool(E_PUINT8 poolPtr)
{
    if (poolPtr == NULL) {
        poolPtr = entropyPool;
    }

    /*
    ** Invert every bit in the pool
    */

    E_PUINT32 pool = (E_PUINT32)poolPtr;

    for (E_UINT32 i = 0; i < (entropyPoolSize / sizeof(E_UINT32)); i++) {
        pool[i] ^= (E_UINT32)-1;
    }
}

static void
addEntropyString(E_PUINT8 buffer, uintptr_t bytes)
{
    static int poolPosition = 0;

    if (entropyPool == NULL) {
        return;
    }

    /*
    ** Adds given data to the pool, when the end is reached,
    ** remix pools contents.
    */

    EnterCriticalSection(&pageLock);

    try {
        if (buffer == NULL) {
            mixPool(NULL);
        } else {
            while (bytes--) {
                if (poolPosition > entropyPoolSize - 1) {
                    mixPool(NULL);
                    poolPosition = 0;
                }
                entropyPool[poolPosition++] ^= *buffer++;
            }
        }
    } catch (...) {
        ASSERT(0);
    }

    LeaveCriticalSection(&pageLock);
}

#define addEntropy(pointer, size) \
    addEntropyString((E_PUINT8)(pointer), (size))

#define addEntropyValue(x) \
    entropyValue = (uintptr_t)(x); \
    addEntropy(&entropyValue, sizeof(uintptr_t))


/*
** Entropy polling
*/

#define checkStatus(x) \
    if (WaitForSingleObject(stopThreads, 0) == WAIT_OBJECT_0) { goto x; }

inline static void
fastPoll()
{
    static bool fixedItemsAdded = false;

    uintptr_t entropyValue;
    POINT point;
    LARGE_INTEGER uCounter;
    ULARGE_INTEGER uSpace;
    MEMORYSTATUS msStatus;
    FILETIME ftCreationTime;
    FILETIME ftExitTime;
    FILETIME ftKernelTime;
    FILETIME ftUserTime;
    SYSTEMTIME systemTime;
//	PSIZE_T lpMinimumWorkingSetSize;
//   PSIZE_T lpMaximumWorkingSetSize;

	
    try {
        checkStatus(ExitPoll_Fast);

        /*
        ** Free disk space
        */

        eraserGetFreeDiskSpace((E_IN LPVOID)"C:\\", 3, &uSpace.QuadPart);
        addEntropy(&uSpace, sizeof(ULARGE_INTEGER));

        /*
        ** Mouse and caret position
        */

        GetCaretPos(&point);
        addEntropy(&point, sizeof(POINT));
        GetCursorPos(&point);
        addEntropy(&point, sizeof(POINT));

        checkStatus(ExitPoll_Fast);

        /*
        ** Process and system information
        */

        addEntropyValue(GetActiveWindow());
        addEntropyValue(GetCapture());
        addEntropyValue(GetClipboardOwner());
        addEntropyValue(GetClipboardViewer());
        addEntropyValue(GetCurrentProcess());
        addEntropyValue(GetCurrentProcessId());
        addEntropyValue(GetCurrentThread());
        addEntropyValue(GetCurrentThreadId());
        addEntropyValue(GetDesktopWindow());
        addEntropyValue(GetFocus());
        addEntropyValue(GetForegroundWindow());
        addEntropyValue(GetInputState());
        addEntropyValue(GetMessagePos());
        addEntropyValue(GetMessageTime());
        addEntropyValue(GetOpenClipboardWindow());
        addEntropyValue(GetProcessHeap());
        addEntropyValue(GetProcessWindowStation());

        checkStatus(ExitPoll_Fast);

        /*
        ** Memory status
        */

        msStatus.dwLength = sizeof(MEMORYSTATUS);
        GlobalMemoryStatus(&msStatus);
        addEntropy(&msStatus, sizeof(MEMORYSTATUS));

        /*
        ** These exist on NT
        */

        if (GetThreadTimes(GetCurrentThread(), &ftCreationTime, &ftExitTime,
                &ftKernelTime, &ftUserTime)) {
            addEntropy(&ftCreationTime, sizeof(FILETIME));
            addEntropy(&ftExitTime,     sizeof(FILETIME));
            addEntropy(&ftKernelTime,   sizeof(FILETIME));
            addEntropy(&ftUserTime,     sizeof(FILETIME));
        }
        if (GetProcessTimes(GetCurrentProcess(), &ftCreationTime, &ftExitTime,
                &ftKernelTime, &ftUserTime)) {
            addEntropy(&ftCreationTime, sizeof(FILETIME));
            addEntropy(&ftExitTime,     sizeof(FILETIME));
            addEntropy(&ftKernelTime,   sizeof(FILETIME));
            addEntropy(&ftUserTime,     sizeof(FILETIME));
        }
        //if (GetProcessWorkingSetSize(GetCurrentProcess(), &uSpace.LowPart,
        //         &uSpace.HighPart)) {
        //    addEntropy(&uSpace, sizeof(ULARGE_INTEGER));
		//if (GetProcessWorkingSetSize(GetCurrentProcess(), &lpMinimumWorkingSetSize,
        //         lpMaximumWorkingSetSize)) {
        //    addEntropy(&lpMinimumWorkingSetSize, sizeof(PSIZE_T));
		//	addEntropy(&lpMaximumWorkingSetSize,  sizeof(PSIZE_T));
        //}


        if (!fixedItemsAdded) {
            STARTUPINFO startupInfo;
            TIME_ZONE_INFORMATION tzi;
            SYSTEM_INFO systemInfo;
            OSVERSIONINFO versionInfo;

            checkStatus(ExitPoll_Fast);

            /*
            ** User information
            */

            if (isWindowsNT) {
                CString strUser, strDomain;

                GetUserAndDomainName(strUser, strDomain);
                addEntropy((LPCTSTR)strUser, strUser.GetLength());
                addEntropy((LPCTSTR)strDomain, strDomain.GetLength());

                GetCurrentUserTextualSid(strUser);
                addEntropy((LPCTSTR)strUser, strUser.GetLength());
            }
            addEntropyValue(GetUserDefaultLangID());
            addEntropyValue(GetUserDefaultLCID());

            checkStatus(ExitPoll_Fast);

            /*
            ** Desktop geometry and colours
            */

            addEntropyValue(GetSystemMetrics(SM_CXSCREEN));
            addEntropyValue(GetSystemMetrics(SM_CYSCREEN));
            addEntropyValue(GetSystemMetrics(SM_CXHSCROLL));
            addEntropyValue(GetSystemMetrics(SM_CYHSCROLL));
            addEntropyValue(GetSystemMetrics(SM_CXMAXIMIZED));
            addEntropyValue(GetSystemMetrics(SM_CYMAXIMIZED));
            addEntropyValue(GetSysColor(COLOR_3DFACE));
            addEntropyValue(GetSysColor(COLOR_DESKTOP));
            addEntropyValue(GetSysColor(COLOR_INFOBK));
            addEntropyValue(GetSysColor(COLOR_WINDOW));
            addEntropyValue(GetDialogBaseUnits());

            checkStatus(ExitPoll_Fast);

            /*
            ** System information
            */

            if (GetTimeZoneInformation(&tzi) != TIME_ZONE_ID_INVALID) {
                addEntropy(&tzi, sizeof(TIME_ZONE_INFORMATION));
            }
            addEntropyValue(GetSystemDefaultLangID());
            addEntropyValue(GetSystemDefaultLCID());
            addEntropyValue(GetOEMCP());
            addEntropyValue(GetACP());
            addEntropyValue(GetKeyboardLayout(0));
            addEntropyValue(GetKeyboardType(0));
            addEntropyValue(GetKeyboardType(1));
            addEntropyValue(GetKeyboardType(2));
            addEntropyValue(GetDoubleClickTime());
            addEntropyValue(GetCaretBlinkTime());
            addEntropyValue(GetLogicalDrives());

            checkStatus(ExitPoll_Fast);

            versionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
            if (GetVersionEx(&versionInfo)) {
                addEntropy(&versionInfo, sizeof(OSVERSIONINFO));
            }

            GetSystemInfo(&systemInfo);
            addEntropy(&systemInfo, sizeof(SYSTEM_INFO));

            checkStatus(ExitPoll_Fast);


            /*
            ** Process startup info
            */

            startupInfo.cb = sizeof(STARTUPINFO);
            GetStartupInfo(&startupInfo);
            addEntropy(&startupInfo, sizeof(STARTUPINFO));

            /*
            ** Clear memory
            */

            ZeroMemory(&startupInfo, sizeof(STARTUPINFO));
            ZeroMemory(&tzi,         sizeof(TIME_ZONE_INFORMATION));
            ZeroMemory(&systemInfo,  sizeof(SYSTEM_INFO));
            ZeroMemory(&versionInfo, sizeof(OSVERSIONINFO));

            /*
            ** Don't add these again
            */

            fixedItemsAdded = true;
        }

        checkStatus(ExitPoll_Fast);

        /*
        ** Counters
        */

        if (QueryPerformanceCounter(&uCounter)) {
            addEntropy(&uCounter, sizeof(LARGE_INTEGER));

            /*
            ** Ticks since last call
            */

            lastCall.QuadPart = uCounter.QuadPart - lastCall.QuadPart;
            addEntropy(&lastCall, sizeof(LARGE_INTEGER));

            lastCall.QuadPart = uCounter.QuadPart;
        }

        addEntropyValue(GetTickCount());

        /*
        ** Time
        */

        GetSystemTime(&systemTime);
        addEntropy(&systemTime, sizeof(SYSTEMTIME));

        checkStatus(ExitPoll_Fast);

        /*
        ** Something from CryptoAPI
        */

        if (cryptoContext != NULL) {
            E_UINT8 bytes[fastPollSize];

            if (pCryptGenRandom(cryptoContext, fastPollSize, bytes)) {
                addEntropy(bytes, fastPollSize);
                ZeroMemory(bytes, fastPollSize);
            }
        }

        checkStatus(ExitPoll_Fast);

        /*
        ** Quality of randomness
        */

        creditPool(qualityFastPoll);
    } catch (...) {
        ASSERT(0);
    }


    /*
    ** Clear memory
    */

ExitPoll_Fast:

    entropyValue = 0;

    ZeroMemory(&point,          sizeof(POINT));
    ZeroMemory(&uSpace,         sizeof(ULARGE_INTEGER));
    ZeroMemory(&uCounter,       sizeof(LARGE_INTEGER));
    ZeroMemory(&msStatus,       sizeof(MEMORYSTATUS));
    ZeroMemory(&ftCreationTime, sizeof(FILETIME));
    ZeroMemory(&ftExitTime,     sizeof(FILETIME));
    ZeroMemory(&ftKernelTime,   sizeof(FILETIME));
    ZeroMemory(&ftUserTime,     sizeof(FILETIME));
    ZeroMemory(&systemTime,     sizeof(SYSTEMTIME));
}


static void
slowPollNT()
{
    static bool isInitialized = false;
    static bool isWorkstation = false;

    static E_UINT32 cbPerfData = PERFORMANCE_BUFFER_SIZE;

    E_PUINT8 buffer = NULL;
    E_UINT32 status;

    DISK_PERFORMANCE diskPerformance;
    HANDLE hDevice;
    CString strDevice;

    E_INT32 nDrive;
    E_INT32 iterations = 0;
    E_INT32 performanceResults = 0;

    PPERF_DATA_BLOCK pPerfData = NULL;

    try {
        pPerfData = (PPERF_DATA_BLOCK)malloc(cbPerfData);

        /*
        ** Get network statistics.
        */

        if (dllNetAPI != NULL &&
            pNetStatisticsGet(NULL, isNTWorkstation ? SERVICE_WORKSTATION : SERVICE_SERVER,
                              0, 0, &buffer) == 0) {
            DWORD uSize = 0;
            pNetApiBufferSize(buffer, &uSize);
            addEntropy(buffer, uSize);

            pNetApiBufferFree(buffer);
            buffer = NULL;
        }

        /*
        ** Get disk I/O statistics for all the hard drives
        */
        for (nDrive = 0; ; nDrive++) {
            /*
            ** If we are done, stop polling
            */

            checkStatus(ExitPoll_NT);

            /*
            ** Check whether we can access this device
            */

            strDevice.Format(_T("\\\\.\\PhysicalDrive%d"), nDrive);

            hDevice = CreateFile((LPCTSTR)strDevice, 0, FILE_SHARE_READ | FILE_SHARE_WRITE,
                                 NULL, OPEN_EXISTING, 0, NULL);

            if (hDevice == INVALID_HANDLE_VALUE) {
                break;
            }

            /*
            ** Note: This only works if the user has turned on the disk
            ** performance counters with 'diskperf -y'.  These counters are off
            ** by default
            */

            DWORD uSize;
            if (DeviceIoControl(hDevice, IOCTL_DISK_PERFORMANCE, NULL, 0,
                                &diskPerformance, sizeof(DISK_PERFORMANCE),
                                &uSize, NULL)) {
                addEntropy(&diskPerformance, uSize);
            }
            CloseHandle(hDevice);
        }

        /*
        ** Query performance data. Because the Win32 version of this API (through
        ** registry) may be buggy, try to use the NT Native API instead.
        */

        if (dllNTAPI != NULL && pPerfData != NULL) {
            E_UINT32 uType;
            performanceResults = 0;

            /*
            ** Scan the first 64 possible information types (we don't bother
            ** with increasing the buffer size as we do with the Win32
            ** version of the performance data read, we may miss a few classes
            ** but it's no big deal).  In addition the returned size value for
            ** some classes is wrong (eg 23 and 24 return a size of 0) so we
            ** miss a few more things, but again it's no big deal.  This scan
            ** typically yields around 20 pieces of data, there's nothing in
            ** the range 65...128 so chances are there won't be anything above
            ** there either
            */

            for (uType = 0; uType < 64; uType++) {
                ULONG uSize = cbPerfData;
                status = pNtQuerySystemInfo(uType, pPerfData, 32768, &uSize);

                if (status == ERROR_SUCCESS && uSize > 0) {
                    addEntropy(pPerfData, uSize);
                    performanceResults++;

                    /*
                    ** Quitting already?
                    */

                    checkStatus(ExitPoll_NT);
                }
            }
        }

        /*
        ** If we got enough data from Native API, we can leave now without
        ** having to try for a Win32-level performance information query
        */

        if (performanceResults < 15) {
            while (pPerfData != NULL && iterations++ < 10) {
                /*
                ** Done?
                */

                checkStatus(ExitPoll_NT);

                DWORD uSize = cbPerfData;
                status = RegQueryValueEx(HKEY_PERFORMANCE_DATA, _T("Global"), NULL,
                                         NULL, (E_PUINT8)pPerfData, &uSize);

                if (status == ERROR_SUCCESS) {
                    if (!memcmp(pPerfData->Signature, L"PERF", 8)) {
                        addEntropy(pPerfData, uSize);
                        /*
                        ** Quality of randomness
                        */

                        creditPool(qualitySlowPoll);
                        break;
                    }
                } else if (status == ERROR_MORE_DATA) {
                    ZeroMemory(pPerfData, cbPerfData);

                    cbPerfData += PERFORMANCE_BUFFER_STEP;
                    pPerfData = (PPERF_DATA_BLOCK)realloc(pPerfData, cbPerfData);
                }
            }

            RegCloseKey(HKEY_PERFORMANCE_DATA);
        } else {
            /*
            ** Quality of randomness
            */

            creditPool(qualitySlowPoll);
        }
    } catch (...) {
        ASSERT(0);
    }

    /*
    ** Make like a tree and leave.
    */

ExitPoll_NT:

    if (pPerfData != NULL) {
        ZeroMemory(pPerfData, sizeof(PPERF_DATA_BLOCK));
        free(pPerfData); pPerfData = NULL;
    }

    ZeroMemory(&diskPerformance, sizeof(DISK_PERFORMANCE));
}

static void
slowPollToolHelp()
{
    PROCESSENTRY32 pe32;
    THREADENTRY32 te32;
    MODULEENTRY32 me32;
    HEAPLIST32 hl32;
    HEAPENTRY32 he32;
    HANDLE hSnapshot = NULL;

    try {
        /*
        ** Make sure ToolHelp32 API is loaded
        */

        if (pCreateToolhelp32Snapshot == NULL) {
            return;
        }

        /*
        ** Take a snapshot of everything we can get to which is currently
        ** in the system
        */

        hSnapshot = pCreateToolhelp32Snapshot(TH32CS_SNAPALL, 0);

        if (!hSnapshot) {
            return;
        }

        /*
        ** Walk through the local heap
        */

        hl32.dwSize = sizeof(HEAPLIST32);
        he32.dwSize = sizeof(HEAPENTRY32);

        if (pHeap32ListFirst(hSnapshot, &hl32)) {
            do {
                /*
                ** First add the information from the basic Heaplist32
                ** structure
                */

                addEntropy(&hl32, sizeof(HEAPLIST32));

                /*
                ** Now walk through the heap blocks getting information
                ** on each of them
                */

                if (pHeap32First(&he32, hl32.th32ProcessID, hl32.th32HeapID)) {
                    do {
                        addEntropy(&he32, sizeof(HEAPENTRY32));
                        checkStatus(ExitPoll_ToolHelp);
                    } while (pHeap32Next(&he32));
                }

                checkStatus(ExitPoll_ToolHelp);

            } while (pHeap32ListNext(hSnapshot, &hl32));
        }

        /*
        ** Walk through all processes
        */

        pe32.dwSize = sizeof(PROCESSENTRY32);

        if (pProcess32First(hSnapshot, &pe32)) {
            do {
                addEntropy(&pe32, sizeof(PROCESSENTRY32));
                checkStatus(ExitPoll_ToolHelp);
            } while(pProcess32Next(hSnapshot, &pe32));
        }

        /*
        ** Walk through all threads
        */

        te32.dwSize = sizeof(THREADENTRY32);

        if (pThread32First(hSnapshot, &te32)) {
            do {
                addEntropy(&te32, sizeof(THREADENTRY32));
                checkStatus(ExitPoll_ToolHelp);
            } while (pThread32Next(hSnapshot, &te32));
        }

        /*
        ** Walk through all modules associated with the process
        */

        me32.dwSize = sizeof(MODULEENTRY32);

        if (pModule32First(hSnapshot, &me32)) {
            do {
                addEntropy(&me32, sizeof(MODULEENTRY32));
                checkStatus(ExitPoll_ToolHelp);
            } while (pModule32Next(hSnapshot, &me32));
        }

        /*
        ** Quality of randomness
        */

        creditPool(qualitySlowPoll);

    } catch (...) {
        ASSERT(0);
    }

ExitPoll_ToolHelp:

    /*
    ** Clean up the snapshot
    */

    CloseHandle(hSnapshot);

    ZeroMemory(&pe32, sizeof(PROCESSENTRY32));
    ZeroMemory(&te32, sizeof(THREADENTRY32));
    ZeroMemory(&me32, sizeof(MODULEENTRY32));
    ZeroMemory(&hl32, sizeof(HEAPLIST32));
    ZeroMemory(&he32, sizeof(HEAPENTRY32));
}

inline static void
slowPoll()
{
    try {
        /*
        ** Start OS specific poll
        */

        if (isWindowsNT) {
            slowPollNT();
        }

        /*
        ** Should we be stopping right about now?
        */

        checkStatus(ExitSlowPoll);

        /*
        ** See if ToolHelp API is available
        */

        slowPollToolHelp();

        /*
        ** Should we be stopping right about now?
        */

        checkStatus(ExitSlowPoll);

        /*
        ** CryptoAPI
        */

        if (cryptoContext != NULL) {
            E_UINT8 bytes[slowPollSize];

            if (pCryptGenRandom(cryptoContext, slowPollSize, bytes)) {
                addEntropy(bytes, slowPollSize);
                ZeroMemory(bytes, slowPollSize);

                /*
                ** Quality of randomness
                */

                creditPool(qualityCryptoAPI);
            }
        }

ExitSlowPoll:

        /*
        ** Process any unmixed bytes
        */

        addEntropy(NULL, 0);

    } catch (...) {
        ASSERT(0);
    }
}

/*
** Thread functions
*/

static UINT
refreshThread(LPVOID)
{
    E_UINT32 waitTime = 0;
    E_UINT64 counter = 0;
    E_UINT64 difference = 0;
    E_UINT64 previous = 0;

    ResetEvent(refreshThreadStopped);

    try {
        while (WaitForSingleObject(stopThreads, 0) != WAIT_OBJECT_0) {
            touchPool();

            if (waitTime >= poolMoveInterval) {
                movePool();

                /*
                ** Add some more entropy to the pool
                */

                waitTime -= poolMoveInterval;
                addEntropy(&waitTime, sizeof(E_UINT8));

                waitTime = 0;
            }

            /*
            ** Measure how much sleep time differs from the previous wait time
            ** and add some entropy to the pool.
            */

            QueryPerformanceCounter((PLARGE_INTEGER)&difference);

            /*
            ** Sleep for a while before retouching the pool
            */

            if (WaitForSingleObject(stopThreads, poolTouchInterval) == WAIT_OBJECT_0) {
                break;
            }

            QueryPerformanceCounter((PLARGE_INTEGER)&counter);
            counter -= difference;

            if (previous > 0) {
                if (counter > previous) {
                    difference = counter - previous;
                } else {
                    difference = previous - counter;
                }

                /*
                ** Add one byte to the entropy pool. According to ent, this data
                ** has ~7.24 bits of entropy for each byte, but it's not random.
                */

                addEntropy(&difference, sizeof(E_UINT8));
            }

            previous = counter;
            waitTime += poolTouchInterval;

            /*
            ** The waitTime is naturally an approximate, we may have to wait in
            ** addEntropy for a while and Sleep isn't obviously that accurate either.
            */
        }
    } catch (...) {
        ASSERT(0);
    }

    SetEvent(refreshThreadStopped);
    return 0;
}

static UINT
pollThread(LPVOID)
{
    ResetEvent(pollThreadStopped);

    try {
        fastPoll();
        slowPoll();
    } catch (...) {
        ASSERT(0);
    }

    SetEvent(pollThreadStopped);
    return 0;
}


/*
** Exported functions
*/

void
randomAddEntropy(E_PUINT8 buffer, E_UINT32 size)
{
    /*
    ** User-provided entropy, mmm.
    */

    if (buffer != NULL && !AfxIsValidAddress(buffer, size)) {
        return;
    }
    addEntropy(buffer, size);
}

bool
randomFill(E_PUINT8 buffer, E_UINT32 size)
{
    if (!AfxIsValidAddress(buffer, size)) {
        return false;
    }

    /*
    ** Make sure we have enough entropy in the pool
    */

    if (randomQuality < requiredQuality) {
        /*
        ** If not, poll for more
        */

        if (enableSlowPoll) {
            if (WaitForSingleObject(pollThreadStopped, slowPollWait) != WAIT_OBJECT_0) {
                slowPoll();
            }
        }

        /*
        ** If there is still not enough entropy (i.e. slow poll failed),
        ** run fast poll a couple of times
        */

        while (randomQuality < requiredQuality) {
            fastPoll();
        }
    }

    bool bResult = false;

    EnterCriticalSection(&pageLock);

    try {
        /*
        ** Do one final fast poll and mix the pool. If the pool hasn't been
        ** mixed at least requiredMixes times, continue until it has.
        */

        do {
            fastPoll();
            mixPool(NULL);
        } while (poolMixes < requiredMixes);

        /*
        ** Generate output
        */

        E_UINT32 bytesToCopy;
        E_UINT32 bytesCopied;

        for (bytesCopied = 0; bytesCopied < size; bytesCopied += outputSize) {

            bytesToCopy = min(outputSize, (size - bytesCopied));

            /*
            ** Copy pool contents to a temporary buffer and invert it
            */

            memcpy(outputBuffer, entropyPool, entropyPoolSize);
            invertPool(NULL);

            /*
            ** Mix the temporary buffer and the entropy pool
            */

            mixPool(outputBuffer);
            mixPool(NULL);

            /*
            ** The truly paranoid would at this point perform statistical
            ** tests to make sure the output really is random, and to catch
            ** catastrophical failures (e.g. no/wrong/repeated output).
            **
            ** Luckily, this generator seems to provide satisfactory results
            ** for me - and I am not that paranoid...
            */

            /*
            ** Fold output to hide even a hash of previous pool contents,
            ** and copy it to the output buffer
            */

            for (E_UINT32 i = 0; i < bytesToCopy; i++) {
                buffer[bytesCopied + i] = (E_UINT8)(outputBuffer[i] ^ outputBuffer[outputSize + i]);
            }
        }

        /*
        ** Clear work area
        */

        clearBuffer(outputBuffer, entropyPoolSize);

        bResult = true;
    } catch (...) {
        ASSERT(0);
        try {
            clearBuffer(outputBuffer, entropyPoolSize);
            ZeroMemory(buffer, size);
        } catch (...) {
        }
        bResult = false;
    }

    LeaveCriticalSection(&pageLock);

    return bResult;
}

/*
** ISAAC
*/

#define isaacInd(mm, x) \
  (* (E_PUINT32) ((E_PINT8) (mm) + ((x) & (RANDOM_ISAAC_WORDS - 1) * sizeof(E_UINT32))))

#define isaacStep(mix, a, b, mm, m, off, r) \
    ( a = ((a) ^ (mix)) + (m)[off], \
      x = *(m), \
      *(m) = y = isaacInd(mm, x) + (a) + (b), \
      *(r) = b = isaacInd(mm, (y) >> RANDOM_ISAAC_LOG) + x )

#define isaacMix(a,b,c,d,e,f,g,h) \
    (        a ^= b << 11, d += a, \
     b += c, b ^= c >>  2, e += b, \
     c += d, c ^= d <<  8, f += c, \
     d += e, d ^= e >> 16, g += d, \
     e += f, e ^= f << 10, h += e, \
     f += g, f ^= g >>  4, a += f, \
     g += h, g ^= h <<  8, b += g, \
     h += a, h ^= a >>  9, c += h, \
     a += b                        )

inline static void
isaacReload()
{
    E_UINT32 a, b;                  // Caches of a and b
    E_UINT32 x, y;                  // Temps needed by isaacStep macro
    E_PUINT32 m = isaacState->mm;   // Pointer into state array
    E_PUINT32 r = isaacOutput;

    a = isaacState->a;
    b = isaacState->b + (++isaacState->c);

    do {
        isaacStep(a << 13, a, b, isaacState->mm, m,     RANDOM_ISAAC_WORDS / 2, r);
        isaacStep(a >> 6,  a, b, isaacState->mm, m + 1, RANDOM_ISAAC_WORDS / 2, r + 1);
        isaacStep(a << 2,  a, b, isaacState->mm, m + 2, RANDOM_ISAAC_WORDS / 2, r + 2);
        isaacStep(a >> 16, a, b, isaacState->mm, m + 3, RANDOM_ISAAC_WORDS / 2, r + 3);
        r += 4;
    } while ((m += 4) < isaacState->mm + RANDOM_ISAAC_WORDS / 2);

    do {
        isaacStep(a << 13, a, b, isaacState->mm, m,     -RANDOM_ISAAC_WORDS / 2, r);
        isaacStep(a >> 6,  a, b, isaacState->mm, m + 1, -RANDOM_ISAAC_WORDS / 2, r + 1);
        isaacStep(a << 2,  a, b, isaacState->mm, m + 2, -RANDOM_ISAAC_WORDS / 2, r + 2);
        isaacStep(a >> 16, a, b, isaacState->mm, m + 3, -RANDOM_ISAAC_WORDS / 2, r + 3);
        r += 4;
    } while ((m += 4) < isaacState->mm + RANDOM_ISAAC_WORDS);

    isaacState->a = a;
    isaacState->b = b;

    a = b = x = y = 0;
    isaacOutputPosition = 0;
}

inline static void
isaacInit()
{
    E_INT32 i, j;

    E_UINT32 a = 0x1367df5a;
    E_UINT32 b = 0x95d90059;
    E_UINT32 c = 0xc3163e4b;
    E_UINT32 d = 0x0f421ad8;
    E_UINT32 e = 0xd92a4a78;
    E_UINT32 f = 0xa51a3c49;
    E_UINT32 g = 0xc4efea1b;
    E_UINT32 h = 0x30609119;

#if 0
    /*
    ** The values above come from this calculation
    */

    a = b = c = d = e = f = g = h = 0x9e3779b9;  /* the golden ratio */
    for (i = 0; i < 4; ++i) {
        isaacMix(a, b, c, d, e, f, g, h);   /* scramble it */
    }
#endif

    isaacState->a = isaacState->b = isaacState->c = 0;

    for (j = 0; j < 2; j++) {
        for (i = 0; i < RANDOM_ISAAC_WORDS; i += 8) {
            a += isaacState->mm[i    ];
            b += isaacState->mm[i + 1];
            c += isaacState->mm[i + 2];
            d += isaacState->mm[i + 3];
            e += isaacState->mm[i + 4];
            f += isaacState->mm[i + 5];
            g += isaacState->mm[i + 6];
            h += isaacState->mm[i + 7];

            isaacMix(a, b, c, d, e, f, g, h);

            isaacState->mm[i    ] = a;
            isaacState->mm[i + 1] = b;
            isaacState->mm[i + 2] = c;
            isaacState->mm[i + 3] = d;
            isaacState->mm[i + 4] = e;
            isaacState->mm[i + 5] = f;
            isaacState->mm[i + 6] = g;
            isaacState->mm[i + 7] = h;
        }
    }

    a = b = c = d = e = f = g = h = 0;
}

void
isaacSeed()
{
    EnterCriticalSection(&pageLock);
    try {
        if (!randomFill((E_PUINT8)isaacState->mm, RANDOM_ISAAC_SEED_BYTES)) {
            /*
            ** Quite unlikely, but we need a seed even if randomFill fails
            */
            clearBuffer((E_PUINT8)isaacState->mm, RANDOM_ISAAC_BYTES);
        }

        isaacInit();
        isaacReload();
    } catch (...) {
        ASSERT(0);
    }
    LeaveCriticalSection(&pageLock);
}

bool
isaacFill(E_PUINT8 puBuffer, UINT_PTR uSize)
{
    if (!AfxIsValidAddress(puBuffer, uSize)) {
        return false;
    }

    bool bResult = true;
    EnterCriticalSection(&pageLock);

    try {
        UINT_PTR uAvailableData, uPosition = 0;
        E_PUINT8 puRandom = (E_PUINT8)isaacOutput;
        while (uPosition < uSize) {
            /*
            ** Refill output buffer
            */

            if (isaacOutputPosition >= RANDOM_ISAAC_BYTES) {
                isaacReload();
            }

            /*
            ** Amount of data to copy
            */

            uAvailableData = min((RANDOM_ISAAC_BYTES - isaacOutputPosition),
                                 (uSize - uPosition));

            /*
            ** Copy random data to buffer
            */

            memcpy((LPVOID)&puBuffer[uPosition], (LPVOID)&puRandom[isaacOutputPosition],
                   uAvailableData);

            /*
            ** Increase counters
            */

            isaacOutputPosition += uAvailableData;
            uPosition += uAvailableData;
        }

        /*
        ** Clear used random data from output buffer
        */

        clearBuffer(puRandom, isaacOutputPosition);
    } catch (...) {
        ASSERT(0);
        bResult = false;
    }

    LeaveCriticalSection(&pageLock);
    return bResult;
}

/*
** Initialization
*/

void
randomInit()
{
    EnterCriticalSection(&refLock);

    try {
        /*
        ** Initialize if first reference
        */

        if (refCount == 0) {
            /*
            ** Load dynamically linked libraries and locate functions
            */

            loadCryptoAPI();

            if (isWindowsNT) {
                loadDependenciesNT();
            }

            /*
            ** ToolHelp32 exists on Windows 9x and NT >= 5.0
            */

            loadToolHelpAPI();

            /*
            ** Allocate page and set pointers
            */

            EnterCriticalSection(&pageLock);

            try {
                poolPage = allocatePoolPage();
                setPoolPointers(poolPage);
            } catch (...) {
                ASSERT(0);
            }

            LeaveCriticalSection(&pageLock);

            /*
            ** Start threads
            */

            ResetEvent(stopThreads);

            SetEvent(pollThreadStopped);
            SetEvent(refreshThreadStopped);

            if (enableSlowPoll) {
                /*
                ** It's OK to start entropy polling thread and another one
                ** to touch the allocated page every now and then
                */
                AfxBeginThread(refreshThread, 0, THREAD_PRIORITY_LOWEST);
                AfxBeginThread(pollThread, 0);
            }
        }

        /*
        ** Increase reference counter
        */

        refCount++;
    } catch (...) {
        ASSERT(0);
    }

    LeaveCriticalSection(&refLock);
}

void
randomEnd()
{
    EnterCriticalSection(&refLock);

    try {
        /*
        ** Decrease reference counter
        */

        if (refCount > 0) {
            refCount--;

            /*
            ** Clean up if nobody needs us anymore
            */

            if (refCount == 0) {
                /*
                ** Stop threads
                */

                SetEvent(stopThreads);
                WaitForSingleObject(refreshThreadStopped, threadTermination);

                /*
                ** Free page and clear pointers
                */

                EnterCriticalSection(&pageLock);

                try {
                    freePoolPage(&poolPage);

                    isaacState = 0;
                    isaacOutput = 0;
                    entropyPool = 0;
                    tempBuffer = 0;
                    outputBuffer = 0;
                } catch (...) {
                    ASSERT(0);
                }

                LeaveCriticalSection(&pageLock);

                /*
                ** Release loaded libraries
                */

                releaseCryptoAPI();

                if (isWindowsNT) {
                    releaseDependenciesNT();
                }

                releaseToolHelpAPI();
            }
        }
    } catch (...) {
        ASSERT(0);
    }

    LeaveCriticalSection(&refLock);
}

class __randomInit {
public:
    __randomInit() {
        /*
        ** Assumptions
        */

        /* We must be able to address the entropy pool as 32-bit words */
        ASSERT(entropyPoolSize % sizeof(E_UINT32) == 0);

        /* Tiger's compression function is called twice */
        ASSERT(mixSize == 2 * blockSize);

        /* Area that is hashed needs to be multiple of hash function's block size */
        ASSERT((hashSize + dataSize) % blockSize == 0);

        /* Entropy pool size must be multiple of hash size */
        ASSERT(entropyPoolSize % hashSize == 0);

        /* Must have enough room for work area */
        ASSERT((pageSize - tempOffset) >= entropyPoolSize);

        /*
        ** Initialize critical sections
        */

        InitializeCriticalSection(&refLock);
        InitializeCriticalSection(&pageLock);

        /*
        ** Initialize lastCall
        */
        QueryPerformanceCounter(&lastCall);

        /*
        ** Create events
        */

        stopThreads          = CreateEvent(NULL, TRUE, FALSE, NULL);
        refreshThreadStopped = CreateEvent(NULL, TRUE, TRUE,  NULL);
        pollThreadStopped    = CreateEvent(NULL, TRUE, TRUE,  NULL);

        /*
        ** Should we disable slow polling and memory refreshing?
        */

        enableSlowPoll = false;
		//Currently we have disabled the background threads for adding entropy. 



		/*

		CKey kReg_reg;
		CIniKey kReg_ini;
		CKey &kReg = no_registry ? kReg_ini : kReg_reg;
        if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE)) {
            E_UINT32 uValue;
            kReg.GetValue(uValue, ERASER_RANDOM_SLOW_POLL, 0);

            if (uValue) {
                enableSlowPoll = true;
            }

            kReg.Close();
        }
		*/

    }

    ~__randomInit() {

        /*
        ** Just in case...
        */

        randomEnd();

        /*
        ** Clean up events
        */

        CloseHandle(stopThreads);
        CloseHandle(refreshThreadStopped);
        CloseHandle(pollThreadStopped);

        stopThreads = NULL;
        refreshThreadStopped = NULL;
        pollThreadStopped = NULL;

        /*
        ** Destroy critical sections
        */

        DeleteCriticalSection(&pageLock);
        DeleteCriticalSection(&refLock);
    }
};

static __randomInit init;