// EraserDoc.h
// $Id$
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


#if !defined(AFX_ERASERDOC_H__70E9C85A_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_ERASERDOC_H__70E9C85A_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "Item.h"
#include "SystemTray.h"

// constants
const LPCTSTR szSettingsKey      = "Scheduler";
const LPCTSTR szScheduleKey      = "Scheduled Assignments";

const LPCTSTR szSchedulerEnabled = "SchedulerEnabled";
const LPCTSTR szLog              = "SchedulerLog";
const LPCTSTR szLogOnlyErrors    = "SchedulerLogOnlyErrors";
const LPCTSTR szStartup          = "SchedulerStartup";
const LPCTSTR szQueueTasks       = "SchedulerQueueTasks";
const LPCTSTR szNoVisualErrors   = "SchedulerNoVisualErrors";
const LPCTSTR szMaxLogSize       = "SchedulerMaxLogFileSize";

const LPCTSTR szStartView        = "EraserStartView";
const LPCTSTR szIconAnimation    = "EraserOutbarIconAnimation";
const LPCTSTR szSmallIconView    = "EraserOutbarSmallIconView";
const LPCTSTR szOutBarWidth      = "EraserOutbarWidth";
const LPCTSTR szNoTrayIcon       = "EraserNoTrayIcon";
const LPCTSTR szHideOnMinimize   = "EraserHideOnMinimize";
const LPCTSTR szWindowRect       = "EraserWindowRect";

const LPCTSTR szViewInfoBar      = "EraserViewInfoBar";
const LPCTSTR szResolveLock      = "EraserResolveLock";
const LPCTSTR szResolveAskUser   = "EraserResolveLockAskUser";

const LPCTSTR szStartupPath      = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
const LPCTSTR szLogFile          = "schedlog.txt";
const LPCTSTR szRunOnStartup     = "Eraser";

const LPCTSTR szClearSwapPath    = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management";
const LPCTSTR szClearSwapValue   = "ClearPageFileAtShutdown";

const LPCTSTR szFileExtension    = "ers";
const LPCTSTR szFileWCard		 = "*.ers";
const LPCTSTR szFileFilter       = "Eraser Files (*.ers)|*.ers||";
const LPCTSTR szExportTitle      = "Select Export File";
const LPCTSTR szImportTitle      = "Select Import File";

const LPCTSTR szDefaultFile      = "default.ers";

// definitions
#define VIEW_ERASER     1
#define VIEW_SCHEDULER  2
#define VIEW_EXPLORER   3

// messages
#define SCHEDULER_SET_TIMERS 1L

// enumerations
enum HeaderIcon
{
    IconExclamation,
    IconEraser,
    IconClock
};

enum OutbarFolders
{
    FolderEraser,
    FolderExplorer
};

enum OutbarEraserViews
{
    ViewEraser,
    ViewScheduler
};


class CEraserDoc : public CDocument
{
private:
CEraserDoc(const CEraserDoc&) ;
CEraserDoc& operator = (const CEraserDoc&);

protected: // create from serialization only
    CEraserDoc();
    DECLARE_DYNCREATE(CEraserDoc)


// Operations
public:
    virtual ~CEraserDoc();

    BOOL LogAction(UINT);
    BOOL LogException(CException*);
    BOOL LogAction(CString);

    void CalcNextAssignment();
    void UpdateToolTip();

    BOOL SavePreferences();
    BOOL ReadPreferences();

    BOOL Import(LPCTSTR szFile, BOOL bErrors = TRUE);
    BOOL Export(LPCTSTR szFile, BOOL bErrors = TRUE);
    BOOL SaveTasksToDefault();

    BOOL AddScheduledTask(CScheduleItem*);
    BOOL AddTask(CItem*);
    void CleanList(CPtrArray&, int);
    void FreeTasks();

    CSystemTray m_stIcon;

    CString m_strNextAssignment;
    CString m_strExePath;

	//Setting. resolving file lock
	BOOL m_bResolveLock;
	BOOL m_bResolveAskUser;
    // Scheduler process counting
    WORD m_wProcessCount;
    BOOL m_bSchedulerEnabled;

    // task arrays
    CPtrArray m_paTasks;
    CPtrArray m_paScheduledTasks;

    // task queue
    CPtrArray m_paQueuedTasks;

    // settings (start view)
    DWORD   m_dwStartView;

    // settings (results)
    BOOL    m_bResultsForFiles;
    BOOL    m_bResultsForUnusedSpace;
    BOOL    m_bResultsOnlyWhenFailed;
    BOOL    m_bShellextResults;

    // settings (shell extension)
    BOOL    m_bErasextEnabled;

    // settings (PRNG)
    BOOL    m_bEnableSlowPoll;

    // settings (paging file on NT)
    BOOL    m_bClearSwap;

    // settings (Scheduler)
    BOOL    m_bLog;
    BOOL    m_bLogOnlyErrors;
    BOOL    m_bStartup;
    BOOL    m_bQueueTasks;
    BOOL    m_bNoVisualErrors;
    DWORD   m_dwMaxLogSize;
    BOOL    m_bNoTrayIcon;
    BOOL    m_bHideOnMinimize;

    // settings (Folder Bar)
    BOOL    m_bIconAnimation;
    BOOL    m_bSmallIconView;
    DWORD   m_dwOutbarWidth;
    RECT    m_rWindowRect;

    // settings (View)
    BOOL    m_bViewInfoBar;

    CImageList* m_smallImageList;
    CImageList m_ilHeader;

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CEraserDoc)
    public:
    virtual BOOL OnNewDocument();
    virtual void Serialize(CArchive& ar);
    virtual BOOL OnOpenDocument(LPCTSTR lpszPathName);
    virtual void DeleteContents();
    virtual void OnCloseDocument();
    //}}AFX_VIRTUAL



#ifdef _DEBUG
    virtual void AssertValid() const;
    virtual void Dump(CDumpContext& dc) const;
#endif

protected:

// Generated message map functions
protected:
    //{{AFX_MSG(CEraserDoc)
    afx_msg void OnUpdateFileExport(CCmdUI* pCmdUI);
    afx_msg void OnFileExport();
    afx_msg void OnFileImport();
    afx_msg void OnTrayEnable();
    afx_msg void OnUpdateTrayShowWindow(CCmdUI* pCmdUI);
    afx_msg void OnUpdateTrayEnable(CCmdUI* pCmdUI);
    afx_msg void OnTrayShowWindow();
    afx_msg void OnEditPreferencesGeneral();
    afx_msg void OnEditPreferencesEraser();
    afx_msg void OnFileViewLog();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_ERASERDOC_H__70E9C85A_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_)
