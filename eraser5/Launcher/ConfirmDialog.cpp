// ConfirmDialog.cpp
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
#include "..\EraserDll\EraserDll.h"
#include "Launcher.h"
#include "ConfirmDialog.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CConfirmDialog dialog


CConfirmDialog::CConfirmDialog(CWnd* pParent /*=NULL*/)
    : CDialog(CConfirmDialog::IDD, pParent)
{
    //{{AFX_DATA_INIT(CConfirmDialog)
        // NOTE: the ClassWizard will add member initialization here
    //}}AFX_DATA_INIT
}


void CConfirmDialog::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CConfirmDialog)
        // NOTE: the ClassWizard will add DDX and DDV calls here
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CConfirmDialog, CDialog)
    //{{AFX_MSG_MAP(CConfirmDialog)
    ON_BN_CLICKED(IDOPTIONS, OnOptions)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CConfirmDialog message handlers

void CConfirmDialog::OnOptions()
{
    eraserShowOptions(GetSafeHwnd(), ERASER_PAGE_FILES);
}
