// TaskDataPage.h
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

#ifndef __TASKDATAPAGE_H__
#define __TASKDATAPAGE_H__

#include "EraserUI\Masked.h"
#include "EraserUI\DriveCombo.h"
#include "Item.h"

/////////////////////////////////////////////////////////////////////////////
// CTaskDataPage dialog

class CTaskDataPage : public CPropertyPage
{
    DECLARE_DYNCREATE(CTaskDataPage)

// Construction
public:
    CTaskDataPage();
    ~CTaskDataPage();

    CString m_strSelectedDrive;
    Type    m_tType;

    BOOL    m_bShowPersistent;

// Dialog Data
    //{{AFX_DATA(CTaskDataPage)
	enum { IDD = IDD_PROPPAGE_TASKDATA };
    CDriveCombo m_comboDrives;
    CString m_strFolder;
    CString m_strFile;
	CString m_strMask;
    BOOL    m_bRemoveOnlySub;
    BOOL    m_bSubfolders;
    BOOL    m_bRemoveFolder;
    CButton m_buRadioDisk;
    CButton m_buRadioFiles;
    CButton m_buRadioFile;
	CButton m_buRadioMask;
    BOOL    m_bPersistent;
    BOOL    m_bUseWildCards;
	BOOL	m_bWildCardsInSubfolders;
	
	//}}AFX_DATA


// Overrides
    // ClassWizard generate virtual function overrides
    //{{AFX_VIRTUAL(CTaskDataPage)
    public:
    virtual void OnOK();
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    // Generated message map functions
    //{{AFX_MSG(CTaskDataPage)
    afx_msg void OnBrowse();
    afx_msg void OnBrowseFiles();
    afx_msg void OnRemoveFolder();
    afx_msg void OnRadioDisk();
    afx_msg void OnRadioFiles();
    afx_msg void OnRadioFile();
	afx_msg void OnRadioMask();
    virtual BOOL OnInitDialog();
    afx_msg void OnCheckWildcards();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()

public:
	DWORD m_dwFinishAction;
	int m_iFinishActionInd;
//	afx_msg void OnBnClickedRadio1();
};


/////////////////////////////////////////////////////////////////////////////
// CTaskSchedulePage dialog

class CTaskSchedulePage : public CPropertyPage
{
    DECLARE_DYNCREATE(CTaskSchedulePage)

// Construction
public:
    CTaskSchedulePage();
    ~CTaskSchedulePage();

    COleDateTime m_odtTime;

// Dialog Data
    //{{AFX_DATA(CTaskSchedulePage)
    enum { IDD = IDD_PROPPAGE_TASKSCHEDULE };
    CTimeEdit   m_editTime;
    BOOL    m_bPM;
	int     m_iWhen;
    //}}AFX_DATA


// Overrides
    // ClassWizard generate virtual function overrides
    //{{AFX_VIRTUAL(CTaskSchedulePage)
    public:
    virtual void OnOK();
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    BOOL m_b24Hour;

    // Generated message map functions
    //{{AFX_MSG(CTaskSchedulePage)
    virtual BOOL OnInitDialog();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()

};

/////////////////////////////////////////////////////////////////////////////
// CTaskSchedulePage dialog

class CTaskStatisticsPage : public CPropertyPage
{
    DECLARE_DYNCREATE(CTaskStatisticsPage)

// Construction
public:
    CTaskStatisticsPage();
    ~CTaskStatisticsPage();

    LPTASKSTATISTICS m_lpts;


// Dialog Data
    //{{AFX_DATA(CTaskStatisticsPage)
	enum { IDD = IDD_PROPPAGE_TASKSTATISTICS };
	CString	m_strStatistics;
	//}}AFX_DATA


// Overrides
    // ClassWizard generate virtual function overrides
    //{{AFX_VIRTUAL(CTaskStatisticsPage)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
	void UpdateStatistics();
    // Generated message map functions
    //{{AFX_MSG(CTaskStatisticsPage)
    virtual BOOL OnInitDialog();
	afx_msg void OnButtonReset();
	//}}AFX_MSG
    DECLARE_MESSAGE_MAP()

};




#endif // __TASKDATAPAGE_H__
