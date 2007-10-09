#pragma once

#include "HotKeyListCtrl.h"
#include <CommCtrl.h>

static const int iCommandCount = 2;


class CHotKeyDlg : public CDialog
{
	DECLARE_DYNAMIC(CHotKeyDlg)

public:
	CHotKeyDlg(CWnd* pParent = NULL, int iValCnt = iCommandCount);   // standard constructor
	virtual ~CHotKeyDlg();

// Dialog Data
	enum { IDD = IDD_DIALOG_HOTKEYS };
	//CHotKeyListCtrl m_lcHotKeys;
	CListCtrl m_lcHotKeys;
	CMapStringToString m_arKeyValues;

protected:
	virtual BOOL OnInitDialog();
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	void CHotKeyDlg::UpdateHotKeyList(CListCtrl& lcHKey);

	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedChange();
	afx_msg void OnActivate(UINT nState, CWnd* pWndOther, BOOL bMinimized);
	afx_msg void OnNMClickListHotkeys(NMHDR *pNMHDR, LRESULT *pResult);

	void LoadValuesFromRegistry();
	void saveListToRegistry();
	afx_msg void OnBnClickedButtonOk();
};
