// KeyComboDlg.h
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

// CKeyComboDlg dialog

class CKeyComboDlg : public CDialog
{
	DECLARE_DYNAMIC(CKeyComboDlg)

public:
	CEdit	m_eKey;
	CString	m_strValue;
	CString m_strRegKey;
	CKeyComboDlg(CString wValue = _T(""), CString strValName = _T(""), CWnd* pParent = NULL) ;   // standard constructor
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