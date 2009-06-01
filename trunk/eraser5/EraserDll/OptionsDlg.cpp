// OptionsDlg.cpp
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
#include "resource.h"
#include "Eraser.h"
#include "eraserdll.h"
#include "options.h"
#include "OptionsDlg.h"

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// COptionsDlg

IMPLEMENT_DYNAMIC(COptionsDlg, CPropertySheet)

COptionsDlg::COptionsDlg(CWnd* pWndParent) :
CPropertySheet(IDS_PROPSHT_CAPTION, pWndParent)
{
    // Add all of the property pages here.  Note that
    // the order that they appear in here will be
    // the order they appear in on screen.  By default,
    // the first page of the set is the active one.
    // One way to make a different property page the
    // active one is to call SetActivePage().

    m_psh.dwFlags |= PSH_NOAPPLYNOW;
    m_psh.dwFlags &= (~PSH_HASHELP);

    m_pgFiles.SetLibSettings(&m_lsSettings);
    m_pgFiles.SetFreeSpaceOpt(&m_pgFreeSpace);

    m_pgFreeSpace.m_plsSettings = &m_lsSettings;
    m_pgFreeSpace.m_ppgFiles    = &m_pgFiles;

    AddPage(&m_pgFiles);
    AddPage(&m_pgFreeSpace);
}

COptionsDlg::~COptionsDlg()
{

}


BEGIN_MESSAGE_MAP(COptionsDlg, CPropertySheet)
    //{{AFX_MSG_MAP(COptionsDlg)
        // NOTE - the ClassWizard will add and remove mapping macros here.
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// COptionsDlg message handlers
