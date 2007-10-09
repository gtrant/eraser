// PreferencesPage.cpp
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
#include "resource.h"
#include "EraserDoc.h"
#include "PreferencesPage.h"
#include "EraserDll\SecurityManager.h"
#include "HotKeyDlg.h"

#include <winsvc.h>


#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

IMPLEMENT_DYNCREATE(CEraserPreferencesPage, CPropertyPage)
IMPLEMENT_DYNCREATE(CSchedulerPreferencesPage, CPropertyPage)


/////////////////////////////////////////////////////////////////////////////
// CEraserPreferencesPage property page

CEraserPreferencesPage::CEraserPreferencesPage() :
CPropertyPage(CEraserPreferencesPage::IDD)
, m_bResolveLock(TRUE)
, m_bResolveAskUser(TRUE)
{
    //{{AFX_DATA_INIT(CEraserPreferencesPage)
    m_bClearSwap = FALSE;
    m_bShellextResults = FALSE;
    m_bResultsForFiles = FALSE;
    m_bResultsForUnusedSpace = FALSE;
    m_bResultsOnlyWhenFailed = FALSE;
	m_bErasextEnabled = FALSE;
	m_bEnableSlowPoll = FALSE;
	//}}AFX_DATA_INIT
}

CEraserPreferencesPage::~CEraserPreferencesPage()
{
}
void CEraserPreferencesPage::OnOK()
{
	//CFrameWnd* frame = (CFrameWnd*) AfxGetMainWnd();
	//CEraserDoc* doc = (CEraserDoc*) frame->GetActiveDocument();
	CPropertyPage::OnOK();
}
void CEraserPreferencesPage::DoDataExchange(CDataExchange* pDX)
{
	CPropertyPage::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CEraserPreferencesPage)
	DDX_Check(pDX, IDC_CHECK_CLEAR_SWAP, m_bClearSwap);
	DDX_Check(pDX, IDC_CHECK_SHELLEXT_RESULTS, m_bShellextResults);
	DDX_Check(pDX, IDC_CHECK_RESULTS_FOR_FILES, m_bResultsForFiles);
	DDX_Check(pDX, IDC_CHECK_RESULTS_FOR_UNUSED_SPACE, m_bResultsForUnusedSpace);
	DDX_Check(pDX, IDC_CHECK_RESULTSONLYWHENFAILED, m_bResultsOnlyWhenFailed);
	DDX_Check(pDX, IDC_CHECK_ERASEXT_ENABLE, m_bErasextEnabled);
	DDX_Check(pDX, IDC_CHECK_PRNG_SLOWPOLL, m_bEnableSlowPoll);
	//}}AFX_DATA_MAP
	DDX_Check(pDX, IDC_CHECK_RESOLVE_LOCK, m_bResolveLock);
	DDX_Check(pDX, IDC_CHECK_RESOLVE_ASK_USR, m_bResolveAskUser);
}


BEGIN_MESSAGE_MAP(CEraserPreferencesPage, CPropertyPage)
    //{{AFX_MSG_MAP(CEraserPreferencesPage)
    ON_BN_CLICKED(IDC_CHECK_RESULTS_FOR_UNUSED_SPACE, OnCheckResultsForUnusedSpace)
    ON_BN_CLICKED(IDC_CHECK_RESULTS_FOR_FILES, OnCheckResultsForFiles)
    //}}AFX_MSG_MAP
	ON_BN_CLICKED(IDC_BUTTON_PROTECTION, OnBnClickedButtonProtection)
	ON_BN_CLICKED(IDC_BUTTON_HOTKEYS, OnBnClickedButtonHotkeys)
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// CSchedulerPreferencesPage property page

CSchedulerPreferencesPage::CSchedulerPreferencesPage() : CPropertyPage(CSchedulerPreferencesPage::IDD)
{
    //{{AFX_DATA_INIT(CSchedulerPreferencesPage)
    m_bLog = FALSE;
    m_bStartup = FALSE;
    m_bNoVisualErrors = FALSE;
    m_bLogOnlyErrors = FALSE;
    m_dwMaxLogSize = 0;
    m_bNoTrayIcon = FALSE;
    m_bQueueTasks = FALSE;
	m_bEnabled = FALSE;
	m_bHideOnMinimize = FALSE;
	//}}AFX_DATA_INIT
}

CSchedulerPreferencesPage::~CSchedulerPreferencesPage()
{
}

void CSchedulerPreferencesPage::DoDataExchange(CDataExchange* pDX)
{
    CPropertyPage::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CSchedulerPreferencesPage)
    DDX_Control(pDX, IDC_SPIN_LIMIT, m_sbLimitSize);
    DDX_Check(pDX, IDC_CHECK_LOG, m_bLog);
    DDX_Check(pDX, IDC_CHECK_STARTUP, m_bStartup);
    DDX_Check(pDX, IDC_CHECK_NOVISUALERRORS, m_bNoVisualErrors);
    DDX_Check(pDX, IDC_CHECK_LOG_ONLYERRORS, m_bLogOnlyErrors);
    DDX_Text(pDX, IDC_EDIT_LIMIT, m_dwMaxLogSize);
    DDX_Check(pDX, IDC_CHECK_NOTRAYICON, m_bNoTrayIcon);
    DDX_Check(pDX, IDC_CHECK_QUEUE, m_bQueueTasks);
	DDX_Check(pDX, IDC_CHECK_ENABLE, m_bEnabled);
	DDX_Check(pDX, IDC_CHECK_HIDEONMINIMIZE, m_bHideOnMinimize);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CSchedulerPreferencesPage, CPropertyPage)
    //{{AFX_MSG_MAP(CSchedulerPreferencesPage)
    ON_BN_CLICKED(IDC_CHECK_LOG_LIMITSIZE, OnCheckLogLimitsize)
    ON_BN_CLICKED(IDC_CHECK_LOG, OnCheckLog)
	ON_BN_CLICKED(IDC_CHECK_NOTRAYICON, OnCheckNotrayicon)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()


static inline BOOL IsAdminOnNT()
{
    SC_HANDLE hSC;

    // Try an Admin Privileged API - if it works return
    // TRUE - else FALSE

    hSC = OpenSCManager(NULL, NULL,
                        GENERIC_READ | GENERIC_WRITE | GENERIC_EXECUTE);

    if (hSC == NULL)
        return FALSE;

    CloseServiceHandle(hSC);
    return TRUE;
}

BOOL CEraserPreferencesPage::OnInitDialog()
{
    CPropertyPage::OnInitDialog();

    // Clearing Paging file is a Windows NT security
    // feature and not available on Windows 9x

    OSVERSIONINFO ov;

    ZeroMemory(&ov, sizeof(OSVERSIONINFO));
    ov.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    VERIFY(GetVersionEx(&ov));

    CButton *pButtonSwap = (CButton*)GetDlgItem(IDC_CHECK_CLEAR_SWAP);

    if (ov.dwPlatformId == VER_PLATFORM_WIN32_NT)
    {
        if (IsAdminOnNT())
        {
            pButtonSwap->EnableWindow(TRUE);
        }
    }
    else
    {
        m_bClearSwap = FALSE;
        pButtonSwap->SetCheck(0);
    }


    BOOL bEnable = (m_bResultsForFiles || m_bResultsForUnusedSpace);

    GetDlgItem(IDC_CHECK_RESULTSONLYWHENFAILED)->EnableWindow(bEnable);
    GetDlgItem(IDC_CHECK_SHELLEXT_RESULTS)->EnableWindow(bEnable);
	

	CString strButtonTitle;
	strButtonTitle.LoadString(CSecurityManager::IsProtected() ? IDS_CLEAR_PROTECTION : IDS_SET_PROTECTION);

	GetDlgItem(IDC_BUTTON_PROTECTION)->SetWindowText(strButtonTitle);


    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CSchedulerPreferencesPage::OnCheckLogLimitsize()
{
    CButton *pCheck = static_cast<CButton*>(GetDlgItem(IDC_CHECK_LOG_LIMITSIZE));

    if (pCheck->GetCheck())
    {
        GetDlgItem(IDC_EDIT_LIMIT)->EnableWindow(TRUE);
        m_sbLimitSize.EnableWindow(TRUE);
    }
    else
    {
        GetDlgItem(IDC_EDIT_LIMIT)->EnableWindow(FALSE);
        m_sbLimitSize.EnableWindow(FALSE);
    }
}

void CSchedulerPreferencesPage::OnCheckLog()
{
    UpdateData(TRUE);
    GetDlgItem(IDC_CHECK_LOG_ONLYERRORS)->EnableWindow(m_bLog);

    CButton *pCheck = static_cast<CButton*>(GetDlgItem(IDC_CHECK_LOG_LIMITSIZE));

    GetDlgItem(IDC_EDIT_LIMIT)->EnableWindow(pCheck->GetCheck() && m_bLog);
    m_sbLimitSize.EnableWindow(pCheck->GetCheck() && m_bLog);

    pCheck->EnableWindow(m_bLog);
    GetDlgItem(IDC_STATIC_KB)->EnableWindow(m_bLog);
}

BOOL CSchedulerPreferencesPage::OnInitDialog()
{
    CPropertyPage::OnInitDialog();

    GetDlgItem(IDC_CHECK_LOG_ONLYERRORS)->EnableWindow(m_bLog);

    m_sbLimitSize.SetBase(10);
    m_sbLimitSize.SetRange(1, 1024);
    m_sbLimitSize.SetPos(m_dwMaxLogSize);
    m_sbLimitSize.SetBuddy(GetDlgItem(IDC_EDIT_LIMIT));

    CButton *pCheck = static_cast<CButton*>(GetDlgItem(IDC_CHECK_LOG_LIMITSIZE));

    if (m_dwMaxLogSize < 1)
    {
        m_dwMaxLogSize = 1;
        pCheck->SetCheck(0);
    }
    else
    {
        pCheck->SetCheck(1);
    }

    GetDlgItem(IDC_EDIT_LIMIT)->EnableWindow(pCheck->GetCheck() && m_bLog);
    m_sbLimitSize.EnableWindow(pCheck->GetCheck() && m_bLog);

    pCheck->EnableWindow(m_bLog);
    GetDlgItem(IDC_STATIC_KB)->EnableWindow(m_bLog);

    if (!m_bNoTrayIcon)
    {
        m_bHideOnMinimize = FALSE;
        GetDlgItem(IDC_CHECK_HIDEONMINIMIZE)->EnableWindow(FALSE);
    }


	
		
    UpdateData(FALSE);

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CSchedulerPreferencesPage::OnOK()
{
    CPropertyPage::OnOK();

    CButton *pCheck = static_cast<CButton*>(GetDlgItem(IDC_CHECK_LOG_LIMITSIZE));

    if (!pCheck->GetCheck())
        m_dwMaxLogSize = 0;
}

void CEraserPreferencesPage::OnCheckResultsForUnusedSpace()
{
    UpdateData(TRUE);
    BOOL bEnable = (m_bResultsForFiles || m_bResultsForUnusedSpace);

    GetDlgItem(IDC_CHECK_RESULTSONLYWHENFAILED)->EnableWindow(bEnable);
    GetDlgItem(IDC_CHECK_SHELLEXT_RESULTS)->EnableWindow(bEnable);
	if (!bEnable)
	{
		CButton* btn = (CButton*)GetDlgItem(IDC_CHECK_RESULTSONLYWHENFAILED);
		btn->SetCheck(BST_UNCHECKED);
		btn = (CButton*)GetDlgItem(IDC_CHECK_SHELLEXT_RESULTS);
		btn->SetCheck(BST_UNCHECKED);	
	}
}

void CEraserPreferencesPage::OnCheckResultsForFiles()
{
    UpdateData(TRUE);
    BOOL bEnable = (m_bResultsForFiles || m_bResultsForUnusedSpace);

    GetDlgItem(IDC_CHECK_RESULTSONLYWHENFAILED)->EnableWindow(bEnable);
    GetDlgItem(IDC_CHECK_SHELLEXT_RESULTS)->EnableWindow(bEnable);
	if (!bEnable)
	{
		CButton* btn = (CButton*)GetDlgItem(IDC_CHECK_RESULTSONLYWHENFAILED);
		btn->SetCheck(BST_UNCHECKED);
		btn = (CButton*)GetDlgItem(IDC_CHECK_SHELLEXT_RESULTS);
		btn->SetCheck(BST_UNCHECKED);
	}
}

void CSchedulerPreferencesPage::OnCheckNotrayicon() 
{
	UpdateData(TRUE);
    GetDlgItem(IDC_CHECK_HIDEONMINIMIZE)->EnableWindow(m_bNoTrayIcon);
}

void CEraserPreferencesPage::OnBnClickedButtonProtection()
{
	bool res;
	CString strButtonTitle;
		
	if (CSecurityManager::IsProtected())
	{
		res = ClearProtection();
		strButtonTitle.LoadString(IDS_SET_PROTECTION);
	}
	else
	{
		res = SetProtection();
		strButtonTitle.LoadString(IDS_CLEAR_PROTECTION);
	}

	if (res)
	{
		GetDlgItem(IDC_BUTTON_PROTECTION)->SetWindowText(strButtonTitle);
	}
}

void CEraserPreferencesPage::OnBnClickedButtonHotkeys()
{
	// TODO: Add your control notification handler code here
	CHotKeyDlg dlg;
	if (IDOK != dlg.DoModal()) return;
}
