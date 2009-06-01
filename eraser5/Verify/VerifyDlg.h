// VerifyDlg.h
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2007 The Eraser Project
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
#if !defined(AFX_VERIFYDLG_H__1BD4386B_61DD_4477_AB19_A7676C3A94DF__INCLUDED_)
#define AFX_VERIFYDLG_H__1BD4386B_61DD_4477_AB19_A7676C3A94DF__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "..\EraserDll\EraserDll.h"

/////////////////////////////////////////////////////////////////////////////
// CVerifyDlg dialog

class CVerifyDlg : public CDialog
{
// Construction
public:
	CVerifyDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	//{{AFX_DATA(CVerifyDlg)
	enum { IDD = IDD_VERIFY_DIALOG };
	CString	m_strFileName;
	CString	m_strProgress;
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CVerifyDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	HICON m_hIcon;

    CFont *m_pfSave;
    CFont m_fBold;

    ERASER_HANDLE m_ehContext;
    BOOL m_bFileSelected;
    BOOL m_bTerminated;

	// Generated message map functions
	//{{AFX_MSG(CVerifyDlg)
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnButtonBrowse();
	afx_msg void OnButtonErase();
	afx_msg void OnButtonMethod();
	afx_msg void OnButtonStop();
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnDestroy();
	//}}AFX_MSG
    afx_msg LRESULT OnEraserNotify(WPARAM wParam, LPARAM lParam);
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_VERIFYDLG_H__1BD4386B_61DD_4477_AB19_A7676C3A94DF__INCLUDED_)
