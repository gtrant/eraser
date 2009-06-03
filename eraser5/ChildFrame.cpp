// ChildFrame.cpp
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

#include "stdafx.h"
#include "Eraser.h"

#include "EraserDoc.h"
#include "EraserView.h"
#include "SchedulerView.h"
#include "ChildFrame.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CChildFrame

IMPLEMENT_DYNCREATE(CChildFrame, CFrameWnd)

CChildFrame::CChildFrame() :
m_iActiveViewID(-1),
m_pEraserView(0),
m_pSchedulerView(0),
m_pExplorerView(0)
{
    TRACE("CChildFrame::CChildFrame\n");
    m_nIDHelp = IDR_CHILDFRAME;
}

CChildFrame::~CChildFrame()
{
    TRACE("CChildFrame::~CChildFrame\n");
}


BEGIN_MESSAGE_MAP(CChildFrame, CFrameWnd)
    //{{AFX_MSG_MAP(CChildFrame)
    ON_WM_CREATE()
    ON_WM_ERASEBKGND()
    ON_WM_DESTROY()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CChildFrame message handlers

void CChildFrame::SwitchToForm(int nForm)
{
    TRACE("CChildFrame::SwitchToForm\n");

    if (nForm == m_iActiveViewID)
        return;

    try
    {
        CFrameWnd   *pwndMain       = static_cast<CFrameWnd*>(AfxGetMainWnd());
        CView       *pOldActiveView = 0;
        CView       *pNewActiveView = 0;

        switch (m_iActiveViewID)
        {
        case VIEW_ERASER:
        default:
            pOldActiveView = static_cast<CView*>(m_pEraserView);
            break;
        case VIEW_SCHEDULER:
            pOldActiveView = static_cast<CView*>(m_pSchedulerView);
            break;
        case VIEW_EXPLORER:
            pOldActiveView = static_cast<CView*>(m_pExplorerView);
            break;
        }

        switch (nForm)
        {
        case VIEW_ERASER:
        default:
            pNewActiveView = static_cast<CView*>(m_pEraserView);
            m_nIDHelp = IDR_MENU_ERASERVIEW;
            break;
        case VIEW_SCHEDULER:
            pNewActiveView = static_cast<CView*>(m_pSchedulerView);
            m_nIDHelp = IDR_MENU_SCHEDULERVIEW;
            break;
        case VIEW_EXPLORER:
            pNewActiveView = static_cast<CView*>(m_pExplorerView);
            m_nIDHelp = IDR_ERASEREXPLORER;
            break;
        }

        SetActiveView(pNewActiveView);

        // set as the main frame's active view to enable message
        // routing

        if (pwndMain) pwndMain->SetActiveView(pNewActiveView);

        pNewActiveView->SetDlgCtrlID(AFX_IDW_PANE_FIRST);
        if (pOldActiveView) pOldActiveView->SetDlgCtrlID(m_iActiveViewID);

        RecalcLayout();

        pNewActiveView->ShowWindow(SW_SHOW);
        if (pOldActiveView) pOldActiveView->ShowWindow(SW_HIDE);

        m_iActiveViewID = nForm;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

int CChildFrame::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
    TRACE("CChildFrame::OnCreate\n");

    if (CFrameWnd::OnCreate(lpCreateStruct) == -1)
        return -1;

    m_InfoBar.Create(NULL, NULL, WS_VISIBLE | WS_CHILD | WS_CLIPSIBLINGS | CBRS_TOP,
                     CRect(0,0,0,0), this, IDW_INFO_BAR);

    m_InfoBar.SetBarStyle(CBRS_ALIGN_TOP);
    m_InfoBar.SetTextFont(_T("Tahoma"));

    ModifyStyleEx(WS_EX_CLIENTEDGE, 0);

    return 0;
}

BOOL CChildFrame::PreCreateWindow(CREATESTRUCT& cs)
{
    TRACE("CChildFrame::PreCreateWindow\n");
    cs.dwExStyle |= WS_EX_STATICEDGE;

    return CFrameWnd::PreCreateWindow(cs);
}

BOOL CChildFrame::OnEraseBkgnd(CDC* /*pDC*/)
{
    return FALSE;
}

void CChildFrame::OnDestroy()
{
    TRACE("CChildFrame::OnDestroy\n");

    try
    {
        // set view to null because we will take care
        // of our views all by ourselves

        SetActiveView(NULL, FALSE);

        // do the same for the main window
        CFrameWnd *pWnd = static_cast<CFrameWnd*>(AfxGetMainWnd());

        if (AfxIsValidAddress(pWnd, sizeof(CFrameWnd)))
            pWnd->SetActiveView(NULL, FALSE);

        // now destroy the views
        if (AfxIsValidAddress(m_pEraserView, sizeof(CEraserView)))
            m_pEraserView->DestroyWindow();
        if (AfxIsValidAddress(m_pSchedulerView, sizeof(CSchedulerView)))
            m_pSchedulerView->DestroyWindow();
        if (AfxIsValidAddress(m_pExplorerView, sizeof(CShellListView)))
            m_pExplorerView->DestroyWindow();

        CFrameWnd::OnDestroy();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}



BOOL CChildFrame::OnCreateClient(LPCREATESTRUCT lpcs, CCreateContext* pContext)
{
    TRACE("CChildFrame::OnCreateClient\n");

    if (CFrameWnd::OnCreateClient(lpcs, pContext))
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(pContext->m_pCurrentDoc);
        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

        try
        {
            // create CEraserView

            m_pEraserView = new CEraserView();
            m_pEraserView->Create(NULL, NULL, AFX_WS_DEFAULT_VIEW,
                                  CFrameWnd::rectDefault, this, VIEW_ERASER, pContext);

            // create CSchedulerView

            m_pSchedulerView = new CSchedulerView();
            m_pSchedulerView->Create(NULL, NULL, AFX_WS_DEFAULT_VIEW,
                                     CFrameWnd::rectDefault, this, VIEW_SCHEDULER, pContext);

            // create CShellListView

            m_pExplorerView = new CShellListView();
            m_pExplorerView->Create(NULL, NULL, AFX_WS_DEFAULT_VIEW,
                                    CFrameWnd::rectDefault, this, VIEW_EXPLORER, pContext);

            // set active view

            CView *pActiveView      = 0;
            CView *pInactiveView    = 0;
            int iInactiveViewID     = 0;

            if (pDoc->m_dwStartView == VIEW_SCHEDULER)
            {
                pActiveView     = m_pSchedulerView;
                pInactiveView   = m_pEraserView;

                m_iActiveViewID = VIEW_SCHEDULER;
                iInactiveViewID = VIEW_ERASER;

                m_nIDHelp       = IDR_MENU_SCHEDULERVIEW;
            }
            else
            {
                pActiveView     = m_pEraserView;
                pInactiveView   = m_pSchedulerView;

                m_iActiveViewID = VIEW_ERASER;
                iInactiveViewID = VIEW_SCHEDULER;

                m_nIDHelp       = IDR_MENU_ERASERVIEW;
            }

            SetActiveView(pActiveView);

            pActiveView->SetDlgCtrlID(AFX_IDW_PANE_FIRST);
            pInactiveView->SetDlgCtrlID(iInactiveViewID);
            m_pExplorerView->SetDlgCtrlID(VIEW_EXPLORER);

            RecalcLayout();

            pActiveView->ShowWindow(SW_SHOW);
            pInactiveView->ShowWindow(SW_HIDE);
            m_pExplorerView->ShowWindow(SW_HIDE);

            return TRUE;
        }
        catch (CException *e)
        {
            ASSERT(FALSE);
            REPORT_ERROR(e);

            try
            {
                pDoc->LogException(e);
            }
            catch (...)
            {
            }

            e->Delete();
        }
    }

    return FALSE;
}