// ReportDialog.h
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

#if !defined(AFX_REPORTDIALOG_H__E04E0B5A_267B_48B4_9EFF_A4321F890EC3__INCLUDED_)
#define AFX_REPORTDIALOG_H__E04E0B5A_267B_48B4_9EFF_A4321F890EC3__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CReportDialog dialog

class CReportDialog : public CDialog
{
// Construction
public:
	CReportDialog(CWnd* pParent = NULL);   // standard constructor
    CStringArray *m_pstraErrorArray;

// Dialog Data
	//{{AFX_DATA(CReportDialog)
	enum { IDD = IDD_DIALOG_REPORT };
	CListCtrl	m_listErrors;
	CString	m_strStatistics;
	CString	m_strCompletion;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CReportDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:

	// Generated message map functions
	//{{AFX_MSG(CReportDialog)
	afx_msg void OnSaveAs();
	virtual BOOL OnInitDialog();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_REPORTDIALOG_H__E04E0B5A_267B_48B4_9EFF_A4321F890EC3__INCLUDED_)
