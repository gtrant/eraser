// KeyComboDlg.cpp
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

#include "stdafx.h"
#include "Eraser.h"
#include "KeyComboDlg.h"
#include <shared/key.h>
#include <commctrl.h>

const LPCTSTR szAccelerKey = "Acceler";
// CKeyComboDlg dialog

IMPLEMENT_DYNAMIC(CKeyComboDlg, CDialog)
CKeyComboDlg::CKeyComboDlg(CString wValue /* ="" */, CString strValName /* ="" */, CWnd* pParent /*=NULL*/)
: CDialog(CKeyComboDlg::IDD, pParent), m_strValue(wValue),m_strRegKey(strValName)
{
}

CKeyComboDlg::~CKeyComboDlg()
{
}

void CKeyComboDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDITTMP, m_eKey);
}


BEGIN_MESSAGE_MAP(CKeyComboDlg, CDialog)
	ON_BN_CLICKED(IDOK, OnBnClickedOk)
	ON_WM_ACTIVATE()
	ON_EN_CHANGE(IDC_EDITTMP, OnEnChangeEdittmp)
END_MESSAGE_MAP()


// CKeyComboDlg message handlers

BOOL CKeyComboDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	m_eKey.SetLimitText(1);
	m_eKey.SetWindowText(m_strValue);

	// TODO:  Add extra initialization here

	return TRUE;  // return TRUE unless you set the focus to a control
	// EXCEPTION: OCX Property Pages should return FALSE
}

void CKeyComboDlg::OnBnClickedOk()
{
	char ch[8];	
	memset(ch, 0, sizeof(ch));
	if (m_eKey.GetLine(0, ch, sizeof(ch)))
	{
		m_strValue = ch[0];
		m_strValue.MakeUpper();
	}

	OnOK();
}

void CKeyComboDlg::OnActivate(UINT nState, CWnd* pWndOther, BOOL bMinimized)
{
	CDialog::OnActivate(nState, pWndOther, bMinimized);
	m_eKey.SetFocus();
}


void CKeyComboDlg::OnEnChangeEdittmp()
{
	// TODO:  If this is a RICHEDIT control, the control will not
	// send this notification unless you override the CDialog::OnInitDialog()
	// function and call CRichEditCtrl().SetEventMask()
	// with the ENM_CHANGE flag ORed into the mask.

	//Recursion guard
	static bool busy = false;
	if (busy)
		return;
	busy = true;

	char ch[8];
	memset(ch, 0, sizeof(ch));
	if (!m_eKey.GetLine(0, ch, sizeof(ch)))
		return;

	CString strLine(ch);
	strLine.MakeUpper();
	if (!strLine.Trim().IsEmpty())
	{
		CString strTmp(m_strRegKey.MakeUpper());
		if (strTmp.Find(strLine[0]) == -1) {
			//Invalid selection, clear the entry
			m_eKey.Undo();

			//TODO: This works only with XP/Vista. What about others?
			EDITBALLOONTIP ebtt;
			ZeroMemory(&ebtt, sizeof(ebtt));
			ebtt.cbStruct = sizeof(ebtt);
			ebtt.pszTitle = L"Invalid shortcut";
			ebtt.ttiIcon = TTI_ERROR;

			strTmp = "The shortcut value must be one of the characters " + strTmp;
			ebtt.pszText = new wchar_t[strTmp.GetLength() + 1];
			mbstowcs((wchar_t*)ebtt.pszText, strTmp.GetBuffer(), strTmp.GetLength() + 1);
			
			m_eKey.SendMessage(EM_SHOWBALLOONTIP, 0, (LPARAM)&ebtt);
			delete[] ebtt.pszText;
		}
	}
	busy = false;
}
