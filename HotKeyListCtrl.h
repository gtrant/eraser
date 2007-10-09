#pragma once


// CHotKeyListCtrl

class CHotKeyListCtrl : public CListCtrl
{
	DECLARE_DYNAMIC(CHotKeyListCtrl)

public:
	CHotKeyListCtrl();
	virtual ~CHotKeyListCtrl();

protected:
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnLvnItemActivate(NMHDR *pNMHDR, LRESULT *pResult);
};


