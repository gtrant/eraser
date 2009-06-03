// ShellPidl.cpp
//
// Originally based on CShellTree version 1.02 by Selom Ofori.
// Heavily altered since then...
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
#include "ShellPidl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

LPITEMIDLIST CShellPidl::Next(LPCITEMIDLIST pidl)
{
    ASSERT(pidl != NULL);

    try
    {
        // If the size is zero, it is the end of the list.
        if (pidl->mkid.cb == 0)
            return NULL;

        // Add cb to pidl (casting to increment by bytes).
        LPITEMIDLIST pidlNext = (LPITEMIDLIST) (((LPBYTE) pidl) + pidl->mkid.cb);

        // Return NULL if it is null-terminating, or a pidl otherwise.
        return (pidl->mkid.cb == 0) ? NULL : pidlNext;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return NULL;
}

UINT CShellPidl::GetSize(LPCITEMIDLIST pidl)
{
    try
    {
        UINT cbTotal = 0;

        if (pidl)
        {
            cbTotal += sizeof(pidl->mkid.cb);       // Null terminator

            while (pidl->mkid.cb)
            {
                cbTotal += pidl->mkid.cb;
                pidl = Next(pidl);
            }
        }

        return cbTotal;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return 0;
}

LPITEMIDLIST CShellPidl::CreatePidl(UINT cbSize)
{
    LPMALLOC        lpMalloc;
    LPITEMIDLIST    pidl = NULL;

    if (SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        pidl = (LPITEMIDLIST)lpMalloc->Alloc(cbSize);

        if (pidl)
            ZeroMemory(pidl, cbSize);

        lpMalloc->Release();
    }

    return pidl;
}

LPITEMIDLIST CShellPidl::ConcatPidls(LPCITEMIDLIST pidl1, LPCITEMIDLIST pidl2)
{
    ASSERT(pidl2 != NULL);

    try
    {
        LPITEMIDLIST    pidlNew;
        UINT            cb1 = 0;
        UINT            cb2;

        if (pidl1)
            cb1 = GetSize(pidl1) - sizeof(pidl1->mkid.cb);

        cb2 = GetSize(pidl2);

        pidlNew = CreatePidl(cb1 + cb2);

        if (pidlNew)
        {
            if (pidl1)
                CopyMemory(pidlNew, pidl1, cb1);

            CopyMemory(((LPSTR)pidlNew) + cb1, pidl2, cb2);
        }

        return pidlNew;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return NULL;
}

LPITEMIDLIST CShellPidl::CopyITEMID(LPMALLOC lpMalloc, LPITEMIDLIST pidl)
{
    ASSERT(lpMalloc != NULL);
    ASSERT(pidl != NULL);

    try
    {
        // Allocate a new item identifier list.
        LPITEMIDLIST pidlNew = (LPITEMIDLIST) lpMalloc->Alloc(pidl->mkid.cb + sizeof(USHORT));

        if (pidlNew == NULL)
            return NULL;

        // Copy the specified item identifier.
        CopyMemory(pidlNew, pidl, pidl->mkid.cb);

        // Append a terminating zero.
        *((USHORT *) (((LPBYTE) pidlNew) + pidl->mkid.cb)) = 0;

        return pidlNew;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return NULL;
}

BOOL CShellPidl::GetName(LPSHELLFOLDER lpsf,
                         LPITEMIDLIST  lpi,
                         DWORD         dwFlags,
                         LPTSTR         lpFriendlyName)
{
    try
    {
        BOOL   bSuccess = TRUE;
        STRRET str;

        if (NOERROR == lpsf->GetDisplayNameOf(lpi, dwFlags, &str))
        {
            switch (str.uType)
            {
            case STRRET_WSTR:
#if defined(_UNICODE)
				lstrcpy(lpFriendlyName, (LPTSTR)str.pOleStr);
#else
                WideCharToMultiByte(CP_ACP,         // CodePage
                                    0,              // dwFlags
                                    str.pOleStr,    // lpWideCharStr
                                    -1,             // cchWideChar
                                    lpFriendlyName, // lpMultiByteStr
                                    MAX_PATH,       // cchMultiByte
                                    NULL,           // lpDefaultChar,
                                    NULL);          // lpUsedDefaultChar
#endif
                break;
            case STRRET_OFFSET:
#if defined(_UNICODE)
				::MultiByteToWideChar(
					CP_ACP,
					0,
					(LPCSTR)str.cStr + str.uOffset,
					-1,
					lpFriendlyName,
					MAX_PATH
					);
#else
                lstrcpy(lpFriendlyName, (LPSTR)lpi + str.uOffset);
#endif
                break;
            case STRRET_CSTR:
#if defined(_UNICODE)
				::MultiByteToWideChar(
					CP_ACP,
					0,
					(LPCSTR)str.cStr,
					-1,
					lpFriendlyName,
					MAX_PATH
					);
#else
                lstrcpy(lpFriendlyName, (LPSTR)str.cStr);
#endif
                break;
            default:
                bSuccess = FALSE;
                break;
            }
        }
        else
            bSuccess = FALSE;

        return bSuccess;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return FALSE;
}

LPITEMIDLIST CShellPidl::GetFullyQualPidl(LPSHELLFOLDER lpsf, LPITEMIDLIST lpi)
{
    ASSERT(lpsf != NULL);
    ASSERT(lpi != NULL);

    try
    {
        TCHAR           szBuffer[MAX_PATH];
        OLECHAR         szOleChar[MAX_PATH];
        LPSHELLFOLDER   lpsfDeskTop;
        LPITEMIDLIST    lpifq = NULL;
        ULONG           ulEaten, ulAttribs;

        if (GetName(lpsf, lpi, SHGDN_FORPARSING, szBuffer))
        {
            if (SUCCEEDED(SHGetDesktopFolder(&lpsfDeskTop)))
            {
#if defined(_UNICODE)
				lstrcpy(szOleChar, szBuffer);
#else
                MultiByteToWideChar(CP_ACP,
                                    MB_PRECOMPOSED,
                                    szBuffer,
                                    -1,
                                    (LPWSTR )szOleChar, //gt
                                    sizeof(szOleChar));
#endif

                if (FAILED(lpsfDeskTop->ParseDisplayName(NULL, NULL, szOleChar,
                                                         &ulEaten, &lpifq, &ulAttribs)))
                {
                    lpifq = NULL;
                }

                lpsfDeskTop->Release();
            }
        }

        return lpifq;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return NULL;
}

BOOL CShellPidl::DoTheMenuThing(HWND hwnd, LPSHELLFOLDER lpsfParent,
                                LPITEMIDLIST  *ppidlArray, LPPOINT lppt,
                                int iCount /*= 1*/)
{
    TRACE("CShellPidl::DoTheMenuThing\n");

    ASSERT(IsWindow(hwnd));
    ASSERT(lpsfParent != NULL);
    ASSERT(ppidlArray != NULL);
    ASSERT(lppt != NULL);
    ASSERT(iCount > 0);

    try
    {
        HMENU               hMenu;
        LPCONTEXTMENU       lpcm;
        CMINVOKECOMMANDINFOEX cmi;
        int                 idCmd;
        HRESULT             hr;
        BOOL                bSuccess = FALSE;

        hr = lpsfParent->GetUIObjectOf(hwnd,
                                       iCount,  // Number of objects to get attributes for
                                       (LPCITEMIDLIST*)ppidlArray,
                                       IID_IContextMenu,
                                       0,
                                       (LPVOID *)&lpcm);

        if (SUCCEEDED(hr))
        {
            hMenu = CreatePopupMenu();

            if (hMenu)
            {
                hr = lpcm->QueryContextMenu(hMenu, 0, 1, 0x7FFF, CMF_EXPLORE);

                if (SUCCEEDED(hr))
                {
                    idCmd = TrackPopupMenu(hMenu,
                                           TPM_LEFTALIGN | TPM_RETURNCMD |
                                           TPM_RIGHTBUTTON,
                                           lppt->x, lppt->y, 0, hwnd, NULL);

                    if (idCmd)
                    {
                        cmi.cbSize          = sizeof(CMINVOKECOMMANDINFOEX);
                        cmi.hwnd            = hwnd;
#if defined(_UNICODE)
                        cmi.fMask           = CMIC_MASK_UNICODE;
                        cmi.lpVerbW          = MAKEINTRESOURCE(idCmd - 1);
                        cmi.lpParametersW    = NULL;
                        cmi.lpDirectoryW     = NULL;
#else
                        cmi.fMask           = 0;
                        cmi.lpVerb          = MAKEINTRESOURCE(idCmd - 1);
                        cmi.lpParameters    = NULL;
                        cmi.lpDirectory     = NULL;
#endif
                        cmi.nShow           = SW_SHOWNORMAL;
                        cmi.dwHotKey        = 0;
                        cmi.hIcon           = NULL;

                        hr = lpcm->InvokeCommand((LPCMINVOKECOMMANDINFO)&cmi);

                        if (SUCCEEDED(hr))
                            bSuccess = TRUE;
                        else
                        {
                            CString strError;
                            strError.Format(_T("InvokeCommand failed. hr = %lx"), hr);
                            AfxMessageBox(strError);
                        }
                    }
                    else
                        bSuccess = TRUE;
                }

                DestroyMenu(hMenu);
            }

            lpcm->Release();
        }
        else
        {
            CString strError;
            strError.Format(_T("GetUIObjectOf failed! hr = %lx"), hr);
            AfxMessageBox(strError);
        }

        return bSuccess;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return FALSE;
}

BOOL CShellPidl::DoTheDefaultThing(HWND hwnd, LPSHELLFOLDER lpsfParent,
                                   LPITEMIDLIST  *ppidlArray, int iCount /*= 1*/)
{
    TRACE("CShellPidl::DoTheDefaultThing\n");

    ASSERT(IsWindow(hwnd));
    ASSERT(lpsfParent != NULL);
    ASSERT(ppidlArray != NULL);
    ASSERT(iCount > 0);

    try
    {
        HMENU               hMenu;
        LPCONTEXTMENU       lpcm;
        CMINVOKECOMMANDINFOEX cmi;
        int                 idCmd;
        HRESULT             hr;
        BOOL                bSuccess = FALSE;

        hr = lpsfParent->GetUIObjectOf(hwnd,
                                       iCount,  // Number of objects to get attributes for
                                       (LPCITEMIDLIST*)ppidlArray,
                                       IID_IContextMenu,
                                       0,
                                       (LPVOID *)&lpcm);

        if (SUCCEEDED(hr))
        {
            hMenu = CreatePopupMenu();

            if (hMenu)
            {
                hr = lpcm->QueryContextMenu(hMenu, 0, 1, 0x7fff, CMF_EXPLORE);

                if (SUCCEEDED(hr))
                {
                    // get the default menu item
                    idCmd = GetMenuDefaultItem(hMenu, FALSE, 0);

                    // if there was a default item, do the thing
                    if (idCmd != -1)
                    {
                        cmi.cbSize          = sizeof(CMINVOKECOMMANDINFOEX);
                        cmi.hwnd            = hwnd;
#if defined(_UNICODE)
                        cmi.fMask           = CMIC_MASK_UNICODE;
                        cmi.lpVerbW          = MAKEINTRESOURCE(idCmd - 1);
                        cmi.lpParametersW    = NULL;
                        cmi.lpDirectoryW     = NULL;
#else
                        cmi.fMask           = 0;
                        cmi.lpVerb          = MAKEINTRESOURCE(idCmd - 1);
                        cmi.lpParameters    = NULL;
                        cmi.lpDirectory     = NULL;
#endif
                        cmi.nShow           = SW_SHOWNORMAL;
                        cmi.dwHotKey        = 0;
                        cmi.hIcon           = NULL;

                        hr = lpcm->InvokeCommand((LPCMINVOKECOMMANDINFO)&cmi);

                        if (SUCCEEDED(hr))
                            bSuccess = TRUE;
                        else
                        {
                            CString strError;
                            strError.Format(_T("InvokeCommand failed. hr = %lx"), hr);
                            AfxMessageBox(strError);
                        }
                    }
                    else
                        bSuccess = TRUE;
                }

                DestroyMenu(hMenu);
            }

            lpcm->Release();
        }
        else
        {
            CString strError;
            strError.Format(_T("GetUIObjectOf failed! hr = %lx"), hr);
            AfxMessageBox(strError);
        }

        return bSuccess;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return FALSE;
}

int CShellPidl::GetItemIcon(LPITEMIDLIST lpi, UINT uFlags)
{
    try
    {
        SHFILEINFO    sfi;

        SHGetFileInfo((LPCTSTR)lpi,
                      0,
                      &sfi,
                      sizeof(SHFILEINFO),
                      uFlags);

        return sfi.iIcon;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return -1;
}