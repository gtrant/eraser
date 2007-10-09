// ChildFrame.h
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


#if !defined(AFX_CHILDFRAME_H__55C18AF0_F175_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_CHILDFRAME_H__55C18AF0_F175_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "InfoBar.h"
#include "EraserView.h"
#include "SchedulerView.h"
#include "ShellListView.h"

/////////////////////////////////////////////////////////////////////////////
// CChildFrame frame

class CChildFrame : public CFrameWnd
{
    DECLARE_DYNCREATE(CChildFrame)
protected:
    CChildFrame();           // protected constructor used by dynamic creation

// Attributes
public:

// Operations
public:
    CEraserView     *m_pEraserView;
    CSchedulerView  *m_pSchedulerView;
    CShellListView  *m_pExplorerView;

    int             m_iActiveViewID;
    CInfoBar        m_InfoBar;

    void            SwitchToForm(int nForm);

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CChildFrame)
    protected:
    virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
    virtual BOOL OnCreateClient(LPCREATESTRUCT lpcs, CCreateContext* pContext);
    //}}AFX_VIRTUAL

// Implementation
protected:
    virtual ~CChildFrame();

    // Generated message map functions
    //{{AFX_MSG(CChildFrame)
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    afx_msg BOOL OnEraseBkgnd(CDC* pDC);
    afx_msg void OnDestroy();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_CHILDFRAME_H__55C18AF0_F175_11D2_BBF3_00105AAF62C4__INCLUDED_)
