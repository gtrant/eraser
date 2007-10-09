// PreferencesSheet.h
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

#ifndef __PREFERENCESSHEET_H__
#define __PREFERENCESSHEET_H__

#include "PreferencesPage.h"

/////////////////////////////////////////////////////////////////////////////
// CPreferencesSheet

class CPreferencesSheet : public CPropertySheet
{
    DECLARE_DYNAMIC(CPreferencesSheet)

// Construction
public:
    CPreferencesSheet(CWnd* pWndParent = NULL);

// Attributes
public:
    CEraserPreferencesPage      m_pgEraser;
    CSchedulerPreferencesPage   m_pgScheduler;

// Operations
public:

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CPreferencesSheet)
    //}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CPreferencesSheet();

// Generated message map functions
protected:
    //{{AFX_MSG(CPreferencesSheet)
        // NOTE - the ClassWizard will add and remove member functions here.
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

#endif  // __PREFERENCESSHEET_H__
