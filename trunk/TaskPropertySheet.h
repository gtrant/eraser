// TaskPropertySheet.h
//
// This class defines custom modal property sheet
// CTaskPropertySheet.
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

#ifndef __TASKPROPERTYSHEET_H__
#define __TASKPROPERTYSHEET_H__

#include "TaskDataPage.h"



/////////////////////////////////////////////////////////////////////////////
// CTaskPropertySheet
class COptionsForFiles;
class CTaskPropertySheet : public CPropertySheet
{
    DECLARE_DYNAMIC(CTaskPropertySheet)

// Construction
public:
    CTaskPropertySheet(BOOL bSchedule = TRUE, BOOL bCreate = FALSE, CWnd* pWndParent = NULL);

// Attributes
public:
    CTaskDataPage       m_pgData;
    CTaskSchedulePage   m_pgSchedule;
    CTaskStatisticsPage m_pgStatistics;
	COptionsForFiles*    m_pPageFileMethodOptions;

// Operations
public:

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CTaskPropertySheet)
	virtual BOOL OnInitDialog( );

    //}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CTaskPropertySheet();

// Generated message map functions
protected:
    //{{AFX_MSG(CTaskPropertySheet)
        // NOTE - the ClassWizard will add and remove member functions here.
	LRESULT OnKickIdle(WPARAM wp, LPARAM lp);
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

#endif  // __TASKPROPERTYSHEET_H__
