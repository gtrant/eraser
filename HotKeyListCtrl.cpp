// HotKeyListCtrl.cpp : implementation file
//

#include "stdafx.h"
#include "Eraser.h"
#include "HotKeyListCtrl.h"
#include ".\hotkeylistctrl.h"


// CHotKeyListCtrl

IMPLEMENT_DYNAMIC(CHotKeyListCtrl, CListCtrl)
CHotKeyListCtrl::CHotKeyListCtrl()
{
}

CHotKeyListCtrl::~CHotKeyListCtrl()
{
}


BEGIN_MESSAGE_MAP(CHotKeyListCtrl, CListCtrl)
	ON_NOTIFY_REFLECT(LVN_ITEMACTIVATE, OnLvnItemActivate)
END_MESSAGE_MAP()



// CHotKeyListCtrl message handlers


void CHotKeyListCtrl::OnLvnItemActivate(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMITEMACTIVATE pNMIA = reinterpret_cast<LPNMITEMACTIVATE>(pNMHDR);
	// TODO: Add your control notification handler code here
	*pResult = 0;
}
