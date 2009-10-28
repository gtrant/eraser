// Options.cpp
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
#include "EraserDll.h"
#include "Common.h"
#include "Options.h"
#include "..\shared\key.h"

#define LIBRARYSETTINGS_SIZE     (sizeof(LibrarySettings) - sizeof(LPMETHOD))
#define CMETHOD_SIZE             (sizeof(METHOD) - sizeof(LPPASS))
#define MAX_CMETHOD_SIZE         (CMETHOD_SIZE + PASSES_MAX * sizeof(PASS))
#define MAX_LIBRARYSETTINGS_SIZE (LIBRARYSETTINGS_SIZE + (MAX_CUSTOM_METHODS * MAX_CMETHOD_SIZE))
#define LIBRARYVERSION           1 //Unicode library version. 0 is the ASCII one

void
setLibraryDefaults(LibrarySettings *pls)
{
    try {
        ZeroMemory(pls, sizeof(LibrarySettings));

        pls->m_nFileMethodID     = DEFAULT_FILE_METHOD_ID;
        pls->m_nUDSMethodID      = DEFAULT_UDS_METHOD_ID;
        pls->m_uItems            = (E_UINT8)-1; // select all
        pls->m_nFileRandom       = PASSES_RND;
        pls->m_nUDSRandom        = PASSES_RND;
        pls->m_nCMethods         = 0;
        pls->m_lpCMethods        = 0;
    } catch (...) {
        ASSERT(0);
    }
}

extern bool no_registry;

bool
loadLibrarySettings(LibrarySettings *pls)
{
    try {
		CKey kReg_reg;
		CIniKey kReg_ini;
		CKey &kReg = no_registry ? kReg_ini : kReg_reg;
        bool    bResult = FALSE;
        DWORD   LibraryVersion = 0;
        E_UINT32  uSize;
        E_PUINT8  lpData;

        setLibraryDefaults(pls);

        if (!kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE)) {
            return false;
        }

        //Get the version of the library database
        kReg.GetValue(LibraryVersion, ERASER_REGISTRY_LIBRARY_VERSION, 0);

        uSize = kReg.GetValueSize(ERASER_REGISTRY_LIBRARY);

        if (uSize >= LIBRARYSETTINGS_SIZE && uSize <= MAX_LIBRARYSETTINGS_SIZE) {
            lpData = new E_UINT8[uSize];
            ZeroMemory(lpData, uSize);

            if (kReg.GetValue((LPVOID)lpData, ERASER_REGISTRY_LIBRARY)) {
                // basic fields
                MoveMemory((LPVOID)pls, (LPCVOID)lpData, LIBRARYSETTINGS_SIZE);

                // custom methods
                if (pls->m_nCMethods > 0 && pls->m_nCMethods <= MAX_CUSTOM_METHODS) {
                    pls->m_lpCMethods = new METHOD[pls->m_nCMethods];

                    E_UINT32 uPos = LIBRARYSETTINGS_SIZE;
#ifdef _UNICODE
					bool upgradeCustomMethods = LibraryVersion == LIBRARYVERSION ||
						AfxMessageBox(_T("An old version of Eraser settings have been found.\n\n")
						_T("Would you like to upgrade the settings to match the new format? Clicking no will likely ")
						_T("result in corrupted custom erase methods."), MB_YESNO) == IDYES;
#endif

                    for (E_UINT8 i = 0; i < pls->m_nCMethods; i++) {
                        // custom method fields
                        MoveMemory((LPVOID)(&pls->m_lpCMethods[i]),
                                   (LPCVOID)(&lpData[uPos]),
                                   CMETHOD_SIZE);

                        uPos += CMETHOD_SIZE;

#ifdef _UNICODE
						// upgrade the name of the method if this is an old library
						if (LibraryVersion < LIBRARYVERSION && upgradeCustomMethods)
						{
							// Cast the raw data to the old method base class
							MethodBaseA* ansiMethod = reinterpret_cast<MethodBaseA*>(&pls->m_lpCMethods[i]);
							_MethodBase unicodeMethod;

							// Migrate the data element by element
							memset(&unicodeMethod, 0, sizeof(_MethodBase));
							unicodeMethod.m_nMethodID = ansiMethod->m_nMethodID;
							unicodeMethod.m_nPasses = ansiMethod->m_nPasses;
							unicodeMethod.m_pwfFunction = ansiMethod->m_pwfFunction;
							unicodeMethod.m_bShuffle = ansiMethod->m_bShuffle;

							// Convert the method name to Unicode
							size_t convCount = 0;
							mbstowcs_s(&convCount, unicodeMethod.m_szDescription, DESCRIPTION_SIZE,
								ansiMethod->m_szDescription, DESCRIPTION_SIZE - 1);

							// Copy the new structure
							memcpy(ansiMethod, &unicodeMethod, sizeof(_MethodBase));

							// Move the buffer pointer backwards so that the passes loaded will be correct
							uPos -= (sizeof(wchar_t) - sizeof(char)) * DESCRIPTION_SIZE;
						}
#endif

                        // actual pass information
                        if (pls->m_lpCMethods[i].m_nPasses > 0 &&
                            pls->m_lpCMethods[i].m_nPasses <= PASSES_MAX) {
                            pls->m_lpCMethods[i].m_lpPasses = new PASS[pls->m_lpCMethods[i].m_nPasses];
                            ZeroMemory(pls->m_lpCMethods[i].m_lpPasses,
                                       pls->m_lpCMethods[i].m_nPasses * sizeof(PASS));

                            MoveMemory((LPVOID)(pls->m_lpCMethods[i].m_lpPasses),
                                       (LPCVOID)(&lpData[uPos]),
                                       pls->m_lpCMethods[i].m_nPasses * sizeof(PASS));

                            uPos += (pls->m_lpCMethods[i].m_nPasses * sizeof(PASS));
                        }
                    }

#ifdef _UNICODE
					if (LibraryVersion < LIBRARYVERSION && upgradeCustomMethods)
						saveLibrarySettings(pls);
#endif
                }

                bResult = true;
            }

            delete[] lpData;
            lpData = 0;
        }

        return bResult;
    } catch (CException *e) {
        ASSERT(0);
        e->ReportError(MB_ICONERROR);
        e->Delete();

        try {
			CKey kReg_reg;
			CIniKey kReg_ini;
			CKey &kReg = no_registry ? kReg_ini : kReg_reg;
            if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE)) {
                kReg.DeleteValue(ERASER_REGISTRY_LIBRARY);
            }
        } catch (...) {
            ASSERT(0);
        }
    }

    return false;
}

bool
saveLibrarySettings(LibrarySettings *pls)
{
    try {
		CKey kReg_reg;
		CIniKey kReg_ini;
		CKey &kReg = no_registry ? kReg_ini : kReg_reg;
        bool    bResult = FALSE;
        E_PUINT8  lpData;
        E_UINT8   i;
        E_UINT32  uSize;
        E_UINT32  uPos;

        if (!kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE)) {
            return FALSE;
        }

        // write the library version
        kReg.SetValue(LIBRARYVERSION, ERASER_REGISTRY_LIBRARY_VERSION);

        // calculate data size
        uSize = LIBRARYSETTINGS_SIZE + (pls->m_nCMethods * CMETHOD_SIZE);

        for (i = 0; i < pls->m_nCMethods; i++) {
            uSize += pls->m_lpCMethods[i].m_nPasses * sizeof(PASS);
        }

        // allocate memory
        lpData = new E_UINT8[uSize];
        ZeroMemory(lpData, uSize);

        // basic information
        MoveMemory((LPVOID)lpData, (LPCVOID)pls, LIBRARYSETTINGS_SIZE);
        uPos = LIBRARYSETTINGS_SIZE;

        // custom methods
        for (i = 0; i < pls->m_nCMethods; i++) {
            MoveMemory((LPVOID)(&lpData[uPos]),
                       (LPCVOID)(&pls->m_lpCMethods[i]),
                       CMETHOD_SIZE);

            uPos += CMETHOD_SIZE;

            // actual pass information
            if (pls->m_lpCMethods[i].m_nPasses > 0 &&
                pls->m_lpCMethods[i].m_nPasses <= PASSES_MAX) {
                MoveMemory((LPVOID)(&lpData[uPos]),
                           (LPCVOID)(pls->m_lpCMethods[i].m_lpPasses),
                           pls->m_lpCMethods[i].m_nPasses * sizeof(PASS));

                uPos += (pls->m_lpCMethods[i].m_nPasses * sizeof(PASS));
            }
        }

        bResult = (kReg.SetValue((LPVOID)lpData, ERASER_REGISTRY_LIBRARY, uSize) != 0);

        delete[] lpData;
        lpData = 0;

        return bResult;
    } catch (CException *e) {
        ASSERT(0);
        e->ReportError(MB_ICONERROR);
        e->Delete();
    }

    return false;
}
