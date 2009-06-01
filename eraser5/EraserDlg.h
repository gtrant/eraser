// EraserDlg.h
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

#if !defined(AFX_ERASERDLG_H__52650794_F291_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_ERASERDLG_H__52650794_F291_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "EraserDll\EraserDll.h"
#include "EraserDll\FileLockResolver.h"
/////////////////////////////////////////////////////////////////////////////
// CEraserDlg dialog

class CEraserDlg : public CDialog
{
// Construction
public:
    BOOL Erase();
    BOOL Initialize(CPtrArray*);

    CEraserDlg(CWnd* pParent = NULL);   // standard constructor

    BOOL    m_bResultsForFiles;
    BOOL    m_bResultsForUnusedSpace;
    BOOL    m_bResultsOnlyWhenFailed;
	int     m_dwFinishAction;

// Dialog Data
    //{{AFX_DATA(CEraserDlg)
    enum { IDD = IDD_DIALOG_ERASER };
    CProgressCtrl   m_pcProgress;
    CProgressCtrl   m_pcProgressTotal;
    CString m_strData;
    CString m_strErasing;
    CString m_strMessage;
    CString m_strPass;
    CString m_strPercent;
    CString m_strPercentTotal;
    CString m_strTime;
    //}}AFX_DATA

	CFileLockResolver* m_pLockResolver;
// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CEraserDlg)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
	
protected:

    ERASER_HANDLE   m_ehContext;

    BOOL m_bShowResults;

    CStringArray    m_saFiles;
    CStringArray    m_saFolders;
    CStringArray    m_saDrives;
	

    BOOL EraserWipeBegin();
    BOOL EraserWipeUpdate();
    BOOL EraserWipeDone();


    // Generated message map functions
    //{{AFX_MSG(CEraserDlg)
    virtual void OnCancel();
    virtual BOOL OnInitDialog();
    afx_msg void OnDestroy();
    //}}AFX_MSG
    afx_msg LRESULT OnEraserNotify(WPARAM wParam, LPARAM lParam);
    DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_ERASERDLG_H__52650794_F291_11D2_BBF3_00105AAF62C4__INCLUDED_)
