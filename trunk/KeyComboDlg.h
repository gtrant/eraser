#pragma once

// CKeyComboDlg dialog

class CKeyComboDlg : public CDialog
{
	DECLARE_DYNAMIC(CKeyComboDlg)

public:
	CEdit	m_eKey;
	CString	m_strValue;
	CString m_strRegKey;
	CKeyComboDlg(CString wValue = "", CString strValName = "", CWnd* pParent = NULL) ;   // standard constructor
	virtual ~CKeyComboDlg();

// Dialog Data
	enum { IDD = IDD_DIALOG_KEYCOMBO };
	

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	virtual BOOL OnInitDialog();
	afx_msg void OnBnClickedOk();
	afx_msg void OnActivate(UINT nState, CWnd* pWndOther, BOOL bMinimized);
	afx_msg void OnEnChangeEdittmp();
};
