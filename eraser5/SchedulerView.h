// SchedulerView.h
// $Id$
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

#if !defined(AFX_SCHEDULERVIEW_H__44195820_F0FC_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_SCHEDULERVIEW_H__44195820_F0FC_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "EraserUI\FlatListView.h"
#include "EraserUI\ProgressBar.h"

/////////////////////////////////////////////////////////////////////////////
// CSchedulerView view

class CSchedulerView : public CFlatListView
{
protected:
    DECLARE_DYNCREATE(CSchedulerView)

// Attributes
public:
    CSchedulerView();           // protected constructor used by dynamic creation

// Operations
public:
    BOOL EraserWipeBegin();
    BOOL SetTimers();

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CSchedulerView)
    public:
    virtual BOOL Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext = NULL);
    virtual void OnInitialUpdate();
    protected:
    virtual void OnDraw(CDC* pDC);      // overridden to draw this view
    virtual void OnUpdate(CView* pSender, LPARAM lHint, CObject* pHint);
    //}}AFX_VIRTUAL

// Implementation
protected:
    virtual ~CSchedulerView();
#ifdef _DEBUG
    virtual void AssertValid() const;
    virtual void Dump(CDumpContext& dc) const;
#endif

    // Generated message map functions
protected:
    void            QueueTask(CScheduleItem *psiItem);
    CScheduleItem*  GetNextQueuedTask();
    void            RemoveTaskFromQueue(CScheduleItem *psiItem);
    BOOL            RunScheduledTask(CScheduleItem *psiItem);

    void ResizeColumns();
    void UpdateList();

    BOOL TerminateThread(CScheduleItem *);
    UINT GetNextTimerID()   { return m_uNextTimerID++; }

    BOOL EraserWipeUpdate();
    BOOL EraserWipeDone();

    CProgressBar m_pbProgress;
    UINT m_uNextTimerID;

    //{{AFX_MSG(CSchedulerView)
    afx_msg void OnSize(UINT nType, int cx, int cy);
    afx_msg void OnFileNewTask();
    afx_msg void OnUpdateEditDeleteTask(CCmdUI* pCmdUI);
    afx_msg void OnEditDeleteTask();
    afx_msg void OnUpdateEditProperties(CCmdUI* pCmdUI);
    afx_msg void OnEditProperties();
    afx_msg void OnTimer(UINT_PTR nIDEvent);
    afx_msg void OnDestroy();
    afx_msg void OnLButtonDblClk(UINT nFlags, CPoint point);
    afx_msg void OnUpdateProcessRun(CCmdUI* pCmdUI);
    afx_msg void OnUpdateProcessRunAll(CCmdUI* pCmdUI);
    afx_msg void OnProcessRun();
    afx_msg void OnProcessRunAll();
    afx_msg void OnUpdateProcessStop(CCmdUI* pCmdUI);
    afx_msg void OnProcessStop();
    afx_msg void OnUpdateEditSelectAll(CCmdUI* pCmdUI);
    afx_msg void OnEditSelectAll();
    afx_msg void OnEditRefresh();
    //}}AFX_MSG
    afx_msg void OnContextMenu(CWnd*, CPoint point);
    afx_msg void OnUpdateItems(CCmdUI* pCmdUI);
    afx_msg LRESULT OnEraserNotify(WPARAM wParam, LPARAM lParam);
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_SCHEDULERVIEW_H__44195820_F0FC_11D2_BBF3_00105AAF62C4__INCLUDED_)
