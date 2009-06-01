// TaskPropertySheet.cpp
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
#include "TaskPropertySheet.h"
#include "EraserDll\OptionPages.h"
#include <afxpriv.h>

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CTaskPropertySheet

//IMPLEMENT_DYNCREATE(COptionsForFiles, CPropertyPage)
IMPLEMENT_DYNAMIC(CTaskPropertySheet, CPropertySheet)

CTaskPropertySheet::CTaskPropertySheet(BOOL bSchedule, BOOL bCreate, CWnd* pWndParent) :
CPropertySheet(IDS_PROPSHT_CAPTION, pWndParent)
{
    // Add all of the property pages here.  Note that
    // the order that they appear in here will be
    // the order they appear in on screen.  By default,
    // the first page of the set is the active one.
    // One way to make a different property page the
    // active one is to call SetActivePage().

    m_psh.dwFlags |= PSH_NOAPPLYNOW;

    AddPage(&m_pgData);
	m_pgData.m_bShowPersistent = !bSchedule;
	m_pPageFileMethodOptions = 0;
    if (bSchedule)
    {
        AddPage(&m_pgSchedule);
		{
			
			m_pPageFileMethodOptions =  COptionsForFiles::create();
			AddPage(m_pPageFileMethodOptions );

		}
		

        if (!bCreate)
            AddPage(&m_pgStatistics);
    }
}

CTaskPropertySheet::~CTaskPropertySheet()
{
	//RemovePage(m_pPageFileMethodOptions);	
	if(!m_pPageFileMethodOptions) delete m_pPageFileMethodOptions;
}
BOOL CTaskPropertySheet::OnInitDialog( )
{
	BOOL ret = CPropertySheet::OnInitDialog();
	return ret;
}


BEGIN_MESSAGE_MAP(CTaskPropertySheet, CPropertySheet)
    //{{AFX_MSG_MAP(CTaskPropertySheet)
        // NOTE - the ClassWizard will add and remove mapping macros here.
		ON_MESSAGE(WM_KICKIDLE,OnKickIdle)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()
LRESULT CTaskPropertySheet::OnKickIdle(WPARAM /*wp*/, LPARAM /*lp*/)
{
	ASSERT_VALID(this);
	
	return 0;
}


/////////////////////////////////////////////////////////////////////////////
// CTaskPropertySheet message handlers
