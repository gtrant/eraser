// HotKeyDlg.h
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2001-2006  Garrett Trant (support@heidi.ie).
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
#pragma once

#include "HotKeyListCtrl.h"
#include <CommCtrl.h>

static const int iCommandCount = 2;


class CHotKeyDlg : public CDialog
{
	DECLARE_DYNAMIC(CHotKeyDlg)

public:
	CHotKeyDlg(CWnd* pParent = NULL);   // standard constructor
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
