// ShellPidl.h
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

#if !defined(AFX_SHELLPIDL_H__INCLUDED_)
#define AFX_SHELLPIDL_H__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000
// ShellPidl.h : header file
//

#include <shlobj.h>

#define ID_SHELLTREE_SELCHANGED     (WM_USER + 200)
#define ID_SHELLLIST_FOLDERCOUNT    (WM_USER + 201)
#define ID_SHELLLIST_SELECTCHILD    (WM_USER + 202)
#define ID_SHELLTREE_SHELLEXECUTE   (WM_USER + 203)
#define ID_SHELLLIST_REFRESHLIST    (WM_USER + 204)

class CShellPidl
{
public:
    // STRUCTURES
    typedef struct tagLVID
    {
        LPSHELLFOLDER lpsfParent;
        LPITEMIDLIST  lpi;
        ULONG         ulAttribs;
    } LVITEMDATA, *LPLVITEMDATA;

    typedef struct tagID
    {
        LPSHELLFOLDER lpsfParent;
        LPITEMIDLIST  lpi;
        LPITEMIDLIST  lpifq;
    } TVITEMDATA, *LPTVITEMDATA;

public:

    // Functions that deal with PIDLs
    LPITEMIDLIST    ConcatPidls(LPCITEMIDLIST pidl1, LPCITEMIDLIST pidl2);
    LPITEMIDLIST    GetFullyQualPidl(LPSHELLFOLDER lpsf, LPITEMIDLIST lpi);
    LPITEMIDLIST    CopyITEMID(LPMALLOC lpMalloc, LPITEMIDLIST lpi);
    BOOL            GetName(LPSHELLFOLDER lpsf, LPITEMIDLIST lpi, DWORD dwFlags, LPTSTR lpFriendlyName);
    LPITEMIDLIST    CreatePidl(UINT cbSize);
    UINT            GetSize(LPCITEMIDLIST pidl);
    LPITEMIDLIST    Next(LPCITEMIDLIST pidl);

    // Utility Functions
    BOOL            DoTheMenuThing(HWND hwnd, LPSHELLFOLDER lpsfParent, LPITEMIDLIST *ppidlArray,
                                   LPPOINT lppt, int iCount = 1);
    BOOL            DoTheDefaultThing(HWND hwnd, LPSHELLFOLDER lpsfParent, LPITEMIDLIST *ppidlArray,
                                   int iCount = 1);
    int             GetItemIcon(LPITEMIDLIST lpi, UINT uFlags);
};

#endif // !defined(AFX_SHELLTREE_H__INCLUDED_)