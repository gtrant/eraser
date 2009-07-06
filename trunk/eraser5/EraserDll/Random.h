// Random.h
//
// Constants and function definitions.
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

#ifndef RANDOM_H
#define RANDOM_H

#include <wincrypt.h>   /* CryptoAPI */
#include <tlhelp32.h>   /* ToolHelp  */

/*
** CryptoAPI definitions and constants
*/

/* Module name */
const LPCTSTR RANDOM_MODULE_ADVAPI32 = _T("ADVAPI32.DLL");
#ifdef DMARS
#define ULONG_PTR DWORD
#endif
/* Function definitions */
typedef BOOL (WINAPI *CRYPTACQUIRECONTEXT)(HCRYPTPROV*, LPCTSTR, LPCTSTR, DWORD, DWORD);
typedef BOOL (WINAPI *CRYPTGENRANDOM)(HCRYPTPROV, DWORD, BYTE*);
typedef BOOL (WINAPI *CRYPTRELEASECONTEXT)(HCRYPTPROV, ULONG_PTR);

/* Function names */
#if defined(_UNICODE)
const LPCSTR RANDOM_FUNCTION_CRYPTACQUIRECONTEXT = "CryptAcquireContextW";
#else
const LPCSTR RANDOM_FUNCTION_CRYPTACQUIRECONTEXT = "CryptAcquireContextA";
#endif
const LPCSTR RANDOM_FUNCTION_CRYPTGENRANDOM      = "CryptGenRandom";
const LPCSTR RANDOM_FUNCTION_CRYPTRELEASECONTEXT = "CryptReleaseContext";

/* Constants */
const E_UINT32 fastPollSize = 20;   /* 160 bits */
const E_UINT32 slowPollSize = 64;   /* 512 bits */

/* Intel i8xx (82802 Firmware Hub Device) hardware random number generator */
#ifndef INTEL_DEF_PROV
#define INTEL_DEF_PROV  _T("Intel Hardware Cryptographic Service Provider")
#endif


/*
** Windows ToolHelp32 API definitions and constants
*/

/* Module name */
const LPCTSTR RANDOM_MODULE_KERNEL32 = _T("KERNEL32.DLL");

/* Function definitions */
typedef BOOL (WINAPI *MODULEWALK)(HANDLE hSnapshot, LPMODULEENTRY32 lpme);
typedef BOOL (WINAPI *THREADWALK)(HANDLE hSnapshot, LPTHREADENTRY32 lpte);
typedef BOOL (WINAPI *PROCESSWALK)(HANDLE hSnapshot, LPPROCESSENTRY32 lppe);
typedef BOOL (WINAPI *HEAPLISTWALK)(HANDLE hSnapshot, LPHEAPLIST32 lphl);
typedef BOOL (WINAPI *HEAPFIRST)(LPHEAPENTRY32 lphe, DWORD th32ProcessID, ULONG_PTR th32HeapID);
typedef BOOL (WINAPI *HEAPNEXT)(LPHEAPENTRY32 lphe);
typedef HANDLE (WINAPI *CREATESNAPSHOT)(DWORD dwFlags, DWORD th32ProcessID);

/* Function names */
const LPCSTR RANDOM_FUNCTION_MODULE32FIRST   = "Module32First";
const LPCSTR RANDOM_FUNCTION_MODULE32NEXT    = "Module32Next";
const LPCSTR RANDOM_FUNCTION_THREAD32FIRST   = "Thread32First";
const LPCSTR RANDOM_FUNCTION_THREAD32NEXT    = "Thread32Next";
const LPCSTR RANDOM_FUNCTION_PROCESS32FIRST  = "Process32First";
const LPCSTR RANDOM_FUNCTION_PROCESS32NEXT   = "Process32Next";
const LPCSTR RANDOM_FUNCTION_HEAP32LISTFIRST = "Heap32ListFirst";
const LPCSTR RANDOM_FUNCTION_HEAP32LISTNEXT  = "Heap32ListNext";
const LPCSTR RANDOM_FUNCTION_HEAPFIRST       = "Heap32First";
const LPCSTR RANDOM_FUNCTION_HEAPNEXT        = "Heap32Next";
const LPCSTR RANDOM_FUNCTION_CREATESNAPSHOT  = "CreateToolhelp32Snapshot";


/*
** Windows NT NetAPI32 definitions and constants
*/

/* Module name */
const LPCTSTR RANDOM_MODULE_NETAPI = _T("NETAPI32.DLL");

/* Function definitions */
typedef DWORD (WINAPI *NETSTATISTICSGET2)(LPWSTR szServer, LPWSTR szService,
                                          DWORD dwLevel, DWORD dwOptions,
                                          LPBYTE *lpBuffer);
typedef DWORD (WINAPI *NETAPIBUFFERSIZE)(LPVOID lpBuffer, LPDWORD cbBuffer);
typedef DWORD (WINAPI *NETAPIBUFFERFREE)(LPVOID lpBuffer);

/* Function names */
const LPCSTR RANDOM_FUNCTION_NETSTATISTICSGET2 = "NetStatisticsGet2";
const LPCSTR RANDOM_FUNCTION_NETAPIBUFFERSIZE  = "NetApiBufferSize";
const LPCSTR RANDOM_FUNCTION_NETAPIBUFFERFREE  = "NetApiBufferFree";

/* Constants */
const LPCTSTR RANDOM_KEY_PRODUCTOPTIONS  = _T("SYSTEM\\CurrentControlSet\\Control\\ProductOptions");
const LPCTSTR RANDOM_KEY_PRODUCTTYPE     = _T("ProductType");
const LPCTSTR RANDOM_NTWORKSTATION_TOKEN = _T("WinNT");

#undef SERVICE_WORKSTATION
#undef SERVICE_SERVER
const LPWSTR SERVICE_WORKSTATION = L"LanmanWorkstation";
const LPWSTR SERVICE_SERVER      = L"LanmanServer";

/*
** Windows NT Native functions
*/

/* Module name */
const LPCTSTR RANDOM_MODULE_NTDLL = _T("NTDLL.DLL");

/* Function definitions */
typedef DWORD (WINAPI *NTQUERYSYSTEMINFO)(DWORD dwType, PVOID dwData,
                                          ULONG dwMaxSize, PULONG dwDataSize);

/* Function names */
const LPCSTR RANDOM_FUNCTION_NTQUERYSYSTEMINFO = "NtQuerySystemInformation";

/* Constants */
#define PERFORMANCE_BUFFER_SIZE     65536   /* Start at 64K */
#define PERFORMANCE_BUFFER_STEP     16384   /* Step by 16K */


/*
** ISAAC pseudorandom number generator definitions and constants
*/

#define RANDOM_ISAAC_LOG        8
#define RANDOM_ISAAC_WORDS      (1 << RANDOM_ISAAC_LOG)
#define RANDOM_ISAAC_BYTES      (RANDOM_ISAAC_WORDS * sizeof(E_UINT32))

#pragma pack(4)

typedef struct __isaacState {
    E_UINT32 mm[RANDOM_ISAAC_WORDS];
    E_UINT32 a;
    E_UINT32 b;
    E_UINT32 c;
} ISAAC_STATE;

#pragma pack()

/*
** Page allocation and pool management constants
*/

/*
** Page structure:
**
**  [52 bytes][1036 bytes][1024 bytes][480 bytes][480 bytes][1024 bytes] = 4096 bytes
**       1          2           3          4          5           6
**
**  1. Unused (non-random data)
**  2. ISAAC_STATE
**  3. ISAAC output buffer
**  4. Entropy pool
**  5. Used as a work buffer when creating output
**  6. Used as temporary work buffer
*/

const E_UINT32 entropyPoolSize = 480;
const E_UINT32 outputSize = entropyPoolSize / 2;

const E_UINT32 unusedSize = 52;
const E_UINT32 isaacStateOffset = unusedSize;
const E_UINT32 isaacOutputOffset = isaacStateOffset + sizeof(ISAAC_STATE);
const E_UINT32 poolOffset = isaacOutputOffset + RANDOM_ISAAC_BYTES;
const E_UINT32 outputOffset = poolOffset + entropyPoolSize;
const E_UINT32 tempOffset = outputOffset + entropyPoolSize;

/*
** Constants
*/

const E_UINT32 qualityFastPoll = 34;
const E_UINT32 qualitySlowPoll = 100;
const E_UINT32 qualityMicrosoft = qualityFastPoll;
const E_UINT32 qualityIntel = 90;

const E_UINT32 requiredQuality = 100;
const E_UINT32 requiredMixes = 10;

const E_UINT32 slowPollWait = 30000;
const E_UINT32 threadTermination = 5000;

const E_UINT32 pageSize = 4096;

/*
** Hash function properties
*/

const E_UINT32 hashSize = 24;          // hash function's digest size
const E_UINT32 blockSize = 64;         // hash function's block size

const E_UINT32 dataSize = 104;         // additional data hashed when mixing
const E_UINT32 mixSize = 2* blockSize; // = hashSize + dataSize

/*
** Pool refresh
*/

const E_UINT32 poolTouchInterval = 2000;  // milliseconds
const E_UINT32 poolMoveInterval = 300000;

/*
** ISAAC seed size
*/

const E_UINT32 RANDOM_ISAAC_SEED_BYTES = outputSize;



/*
** Exported functions
*/

void
randomInit();
void
randomEnd();

void
randomAddEntropy(E_PUINT8, E_UINT32);
bool
randomFill(E_PUINT8, E_UINT32);

void
isaacSeed();
bool
isaacFill(E_PUINT8, UINT_PTR);

#endif