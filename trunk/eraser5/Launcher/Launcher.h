// Launcher.h
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

#if !defined(AFX_LAUNCHER_H__DC6635C8_F67B_11D2_BBF6_00105AAF62C4__INCLUDED_)
#define AFX_LAUNCHER_H__DC6635C8_F67B_11D2_BBF6_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef __AFXWIN_H__
    #error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"       // main symbols

// SHEmptyRecycleBin
typedef HRESULT (STDAPICALLTYPE *SHEMPTYRECYCLEBIN)(HWND, LPCTSTR, DWORD);

const LPCTSTR szShell32             = _T("SHELL32.dll");
#if defined(_UNICODE)
	const LPCSTR szSHEmptyRecycleBin   = "SHEmptyRecycleBinW";
#else
	const LPCSTR szSHEmptyRecycleBin   = "SHEmptyRecycleBinA";
#endif

// constants
const LPCTSTR szFile            = _T("-file");
const LPCTSTR szFolder          = _T("-folder");
const LPCTSTR szSubFolders      = _T("-subfolders");
const LPCTSTR szKeepFolder      = _T("-keepfolder");
const LPCTSTR szDisk            = _T("-disk");
const LPCTSTR szDiskAll         = _T("all");
const LPCTSTR szSilent          = _T("-silent");
const LPCTSTR szRecycled        = _T("-recycled");

const LPCTSTR szMethod          = _T("-method");
const LPCTSTR szMethodLibrary   = _T("Library");
const LPCTSTR szMethodGutmann   = _T("Gutmann");
const LPCTSTR szMethodDoD       = _T("DoD");
const LPCTSTR szMethodDoD_E     = _T("DoD_E");
const LPCTSTR szMethodFL2K      = _T("First_Last2k");
const LPCTSTR szMethodRandom    = _T("Random");
const LPCTSTR szSchneier        = _T("Schneier");

const LPCTSTR szOptions         = _T("-options");
const LPCTSTR szResults         = _T("-results");
const LPCTSTR szResultsOnError  = _T("-resultsonerror");
const LPCTSTR szQueue           = _T("-queue");
const LPCTSTR szResolveLock     = _T("-rl");

const LPCTSTR szQueueGUID       = _T("EraserL.{F0D19C73-EF5F-422a-9F0C-524C7F76E090}.%u");
const DWORD   ERASERL_MAX_QUEUE = 100;

/////////////////////////////////////////////////////////////////////////////
// CLauncherApp:
// See Launcher.cpp for the implementation of this class
//

class CLauncherDlg;

class CLauncherApp : public CWinApp
{
public:
    CLauncherApp();
    BOOL GetNextParameter(CString& strCmdLine, CString& strNextParameter) const;

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CLauncherApp)
    public:
    virtual BOOL InitInstance();
    virtual int ExitInstance();
    //}}AFX_VIRTUAL

// Implementation

    //{{AFX_MSG(CLauncherApp)
        // NOTE - the ClassWizard will add and remove member functions here.
        //    DO NOT EDIT what you see in these blocks of generated code !
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()

protected:
	void HandleQueue(BOOL bQueue);
    CLauncherDlg *m_pdlgEraser;
    HANDLE m_hQueue;
};


/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_LAUNCHER_H__DC6635C8_F67B_11D2_BBF6_00105AAF62C4__INCLUDED_)