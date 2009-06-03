// PassEditDlg.cpp
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
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
#include "eraser.h"
#include "EraserDll.h"
#include "PassEditDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CPassEditDlg dialog


CPassEditDlg::CPassEditDlg(CWnd* pParent /*=NULL*/)
    : CDialog(CPassEditDlg::IDD, pParent)
{
    //{{AFX_DATA_INIT(CPassEditDlg)
    m_uPasses = 0;
    //}}AFX_DATA_INIT
    m_bIgnoreChange = true;
}


void CPassEditDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CPassEditDlg)
    DDX_Control(pDX, IDC_SPIN_PASSES, m_spinPasses);
    DDX_Text(pDX, IDC_EDIT_PASSES, m_uPasses);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CPassEditDlg, CDialog)
    //{{AFX_MSG_MAP(CPassEditDlg)
	ON_EN_CHANGE(IDC_EDIT_PASSES, OnChangeEditPasses)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CPassEditDlg message handlers

BOOL CPassEditDlg::OnInitDialog()
{
    m_bIgnoreChange = true;
    CDialog::OnInitDialog();

    m_spinPasses.SetRange32(1, PASSES_MAX);
    m_spinPasses.SetPos((int)m_uPasses);

    // the spin control fails to set the window text if m_uPasses
    // is greater than (2^16)/2 - 1, so we'll have to help it...
    CString strText;
    strText.Format(_T("%u"), m_uPasses);
    GetDlgItem(IDC_EDIT_PASSES)->SetWindowText((LPCTSTR)strText);

    m_bIgnoreChange = false;
    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CPassEditDlg::OnChangeEditPasses()
{
    if (!m_bIgnoreChange)
    {
	    UpdateData();
        GetDlgItem(IDOK)->EnableWindow(m_uPasses <= PASSES_MAX && m_uPasses >= 1);
    }
}