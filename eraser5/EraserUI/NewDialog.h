// DlgTestDlg.h : header file
//

#if !defined(AFX_DLGTESTDLG_H__B6986713_1552_4A05_B223_C49B60BF0D3C__INCLUDED_)
#define AFX_DLGTESTDLG_H__B6986713_1552_4A05_B223_C49B60BF0D3C__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "FileTreeCtrl.h"
#include "resource.h"

/////////////////////////////////////////////////////////////////////////////
// CNewDialog dialog

class CNewDialog : public CDialog
{
// Construction
public:
	CNewDialog(CWnd* pParent = NULL);	// standard constructor
CString m_sPath;
// Dialog Data
	//{{AFX_DATA(CNewDialog)
	enum { IDD = IDD_DLGNEW_DIALOG };
	CTreeFileCtrl	m_ctrlTree;
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CNewDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	HICON m_hIcon;
    

	// Generated message map functions
	//{{AFX_MSG(CNewDialog)
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnTvnSelchangedTree1(NMHDR *pNMHDR, LRESULT *pResult);
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_DLGTESTDLG_H__B6986713_1552_4A05_B223_C49B60BF0D3C__INCLUDED_)
