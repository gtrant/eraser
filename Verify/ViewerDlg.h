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
