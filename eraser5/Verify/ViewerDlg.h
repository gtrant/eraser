// ViewerDlg.h
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
#if !defined(AFX_VIEWERDLG_H__037EC108_598D_413C_B5B7_A8270562C2E6__INCLUDED_)
#define AFX_VIEWERDLG_H__037EC108_598D_413C_B5B7_A8270562C2E6__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// ViewerDlg.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CViewerDlg dialog

class CViewerDlg : public CDialog
{
// Construction
public:
	CViewerDlg(CWnd* pParent = NULL);   // standard constructor
    CString m_strFileName;
    CString m_strMessage;

// Dialog Data
	//{{AFX_DATA(CViewerDlg)
	enum { IDD = IDD_DIALOG_VIEWER };
	CRichEditCtrl	m_recView;
	DWORD	m_dwCurrentCluster;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CViewerDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
    BOOL DisplayCluster(DWORD dwCluster, DWORD dwSpecial = 0);
    
    inline void AddDataLine(DWORD dwOffset, DWORD *pdwData, LPCTSTR szString);
    inline int  AppendText(LPCTSTR szString);
    inline void AppendFormattedText(LPCTSTR szString, CHARFORMAT& cf);

    HANDLE m_hFile;
    DWORD m_dwClusterSize;

	// Generated message map functions
	//{{AFX_MSG(CViewerDlg)
	afx_msg void OnButtonFirst();
	afx_msg void OnButtonGo();
	afx_msg void OnButtonLast();
	afx_msg void OnButtonNext();
	afx_msg void OnButtonPrevious();
	virtual BOOL OnInitDialog();
	afx_msg void OnDestroy();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_VIEWERDLG_H__037EC108_598D_413C_B5B7_A8270562C2E6__INCLUDED_)
