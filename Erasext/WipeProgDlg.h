// WipeProgDlg.h
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

#ifndef WIPEPROGDLG_H
#define WIPEPROGDLG_H

/////////////////////////////////////////////////////////////////////////////
// CEraserDlg dialog
#include "..\EraserDll\FileLockResolver.h"
class CEraserDlg : public CDialog
{
// Construction
public:
    CEraserDlg(CWnd* pParent = NULL);   // standard constructor

    BOOL    m_bShowResults;
    BOOL    m_bUseFiles;
    BOOL    m_bMove;
    CStringArray m_saData;

    BOOL    m_bResultsForFiles;
    BOOL    m_bResultsForUnusedSpace;
    BOOL    m_bResultsOnlyWhenFailed;
	CFileLockResolver m_LockResolver;

// Dialog Data
    //{{AFX_DATA(CEraserDlg)
    enum { IDD = IDD_DIALOG_WIPEPROG };
    CProgressCtrl   m_pcProgress;
    CProgressCtrl   m_pcProgressTotal;
    CString m_strPercent;
    CString m_strPercentTotal;
    CString m_strData;
    CString m_strErasing;
    CString m_strPass;
    CString m_strTime;
    CString m_strMessage;
    BOOL    m_bResults;
    //}}AFX_DATA


// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CEraserDlg)
    public:
    virtual BOOL PreTranslateMessage(MSG* pMsg);
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    ERASER_HANDLE   m_ehContext;
    HACCEL          m_hAccel;

    // Generated message map functions
    //{{AFX_MSG(CEraserDlg)
    virtual BOOL OnInitDialog();
    afx_msg void OnDestroy();
    virtual void OnCancel();
	afx_msg void OnCheckResults();
	//}}AFX_MSG
    afx_msg LRESULT OnEraserNotify(WPARAM wParam, LPARAM lParam);
    DECLARE_MESSAGE_MAP()

    void Erase();

    BOOL EraserWipeBegin();
    BOOL EraserWipeUpdate();
    BOOL EraserWipeDone();
};

#endif
