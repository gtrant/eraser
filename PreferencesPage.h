// PreferencesPage.h
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

#ifndef __PREFERENCESPAGE_H__
#define __PREFERENCESPAGE_H__

/////////////////////////////////////////////////////////////////////////////
// CEraserPreferencesPage dialog

class CEraserPreferencesPage : public CPropertyPage
{
    DECLARE_DYNCREATE(CEraserPreferencesPage)

// Construction
public:
    CEraserPreferencesPage();
    ~CEraserPreferencesPage();

// Dialog Data
    //{{AFX_DATA(CEraserPreferencesPage)
	enum { IDD = IDD_PROPPAGE_ERASER };
    BOOL    m_bClearSwap;
    BOOL    m_bShellextResults;
    BOOL    m_bResultsForFiles;
    BOOL    m_bResultsForUnusedSpace;
    BOOL    m_bResultsOnlyWhenFailed;
	BOOL	m_bErasextEnabled;
	BOOL	m_bEnableSlowPoll;
	BOOL	m_bResolveLock;
	BOOL	m_bResolveAskUser;
	//}}AFX_DATA


// Overrides
    // ClassWizard generate virtual function overrides
    //{{AFX_VIRTUAL(CEraserPreferencesPage)
    public:
		void OnOK();
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    // Generated message map functions
    //{{AFX_MSG(CEraserPreferencesPage)
    virtual BOOL OnInitDialog();
    afx_msg void OnCheckResultsForUnusedSpace();
    afx_msg void OnCheckResultsForFiles();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()


	
public:
	afx_msg void OnBnClickedButtonProtection();
	afx_msg void OnBnClickedButtonHotkeys();
};


/////////////////////////////////////////////////////////////////////////////
// CSchedulerPreferencesPage dialog

class CSchedulerPreferencesPage : public CPropertyPage
{
    DECLARE_DYNCREATE(CSchedulerPreferencesPage)

// Construction
public:
    CSchedulerPreferencesPage();
    ~CSchedulerPreferencesPage();

// Dialog Data
    //{{AFX_DATA(CSchedulerPreferencesPage)
	enum { IDD = IDD_PROPPAGE_SCHEDULER };
    CSpinButtonCtrl m_sbLimitSize;
    BOOL    m_bLog;
    BOOL    m_bStartup;
    BOOL    m_bNoVisualErrors;
    BOOL    m_bLogOnlyErrors;
    DWORD   m_dwMaxLogSize;
    BOOL    m_bNoTrayIcon;
    BOOL    m_bQueueTasks;
	BOOL	m_bEnabled;
	BOOL	m_bHideOnMinimize;
	//}}AFX_DATA


// Overrides
    // ClassWizard generate virtual function overrides
    //{{AFX_VIRTUAL(CSchedulerPreferencesPage)
    public:
    virtual void OnOK();
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    // Generated message map functions
    //{{AFX_MSG(CSchedulerPreferencesPage)
    afx_msg void OnCheckLogLimitsize();
    afx_msg void OnCheckLog();
    virtual BOOL OnInitDialog();
	afx_msg void OnCheckNotrayicon();
	//}}AFX_MSG
    DECLARE_MESSAGE_MAP()

};



#endif // __PREFERENCESPAGE_H__
