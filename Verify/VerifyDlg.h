// VerifyDlg.h : header file
//

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
