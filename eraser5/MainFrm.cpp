// MainFrm.cpp
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
#include "resource.h"
#include "Eraser.h"

#include "EraserDoc.h"
#include "EraserView.h"
#include "SchedulerView.h"

#include "ChildFrame.h"
#include "MainFrm.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

#define DEFAULT_OUTBAR_WIDTH    100
#define FOLDERANIMATION_DELAY   10
#define ICONANIMATION_DELAY     200

/////////////////////////////////////////////////////////////////////////////
// CMainFrame

IMPLEMENT_DYNCREATE(CMainFrame, CFrameWnd)

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
    //{{AFX_MSG_MAP(CMainFrame)
    ON_WM_SYSCOMMAND()
    ON_WM_CREATE()
    ON_COMMAND(ID_VIEW_INFO_BAR, OnViewInfoBar)
    ON_UPDATE_COMMAND_UI(ID_VIEW_INFO_BAR, OnUpdateViewInfoBar)
    ON_WM_DESTROY()
    //}}AFX_MSG_MAP
    // Global help commands
    ON_COMMAND(ID_HELP_FINDER, CFrameWnd::OnHelpFinder)
    ON_COMMAND(ID_HELP, CFrameWnd::OnHelp)
    ON_COMMAND(ID_CONTEXT_HELP, CFrameWnd::OnContextHelp)
    ON_COMMAND(ID_DEFAULT_HELP, CFrameWnd::OnHelpFinder)
    ON_MESSAGE(WM_OUTBAR_NOTIFY, OnOutbarNotify)
END_MESSAGE_MAP()

static UINT indicators[] =
{
    ID_SEPARATOR,           // status line indicator
    ID_INDICATOR_ITEMS
};

/////////////////////////////////////////////////////////////////////////////
// CMainFrame construction/destruction

CMainFrame::CMainFrame() :
m_pwndChild(0),
m_iLastActiveItem(-1)
{
    TRACE("CMainFrame::CMainFrame\n");
}

CMainFrame::~CMainFrame()
{
    TRACE("CMainFrame::~CMainFrame\n");
}

int CMainFrame::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
    TRACE("CMainFrame::OnCreate\n");

    if (CFrameWnd::OnCreate(lpCreateStruct) == -1)
        return -1;

	if (!m_wndToolBar.CreateEx(this, TBSTYLE_FLAT | TBSTYLE_TRANSPARENT, WS_CHILD |  CBRS_TOP
		| CBRS_TOOLTIPS | CBRS_FLYBY | CBRS_SIZE_DYNAMIC) ||
		!m_wndToolBar.LoadToolBar(IDR_MAINFRAME))
		{
			TRACE0("Failed to create toolbar\n");
			return -1;      // fail to create
		}

/*    if (!m_wndToolBar.CreateEx(this) ||
        !m_wndToolBar.LoadToolBar(IDR_MAINFRAME))
    {
        TRACE0("Failed to create toolbar\n");
        return -1;      // fail to create
    }*/
// ===========================================================
    m_bmToolbarHi.LoadBitmap( IDR_MAINFRAME );
    m_wndToolBar.SetBitmap( (HBITMAP)m_bmToolbarHi );
// ===========================================================

    if (!m_wndReBar.Create(this) ||
        !m_wndReBar.AddBar(&m_wndToolBar))
    {
        TRACE0("Failed to create rebar\n");
        return -1;      // fail to create
    }

    if (!m_wndStatusBar.Create(this) ||
        !m_wndStatusBar.SetIndicators(indicators,
          sizeof(indicators)/sizeof(UINT)))
    {
        TRACE0("Failed to create status bar\n");
        return -1;      // fail to create
    }

    m_wndToolBar.SetBarStyle(m_wndToolBar.GetBarStyle() |
                             CBRS_TOOLTIPS | CBRS_FLYBY);

    return 0;
}

BOOL CMainFrame::PreCreateWindow(CREATESTRUCT& cs)
{
    TRACE("CMainFrame::PreCreateWindow\n");

    cs.lpszClass = szEraserClassName;
    cs.style &= ~(LONG) FWS_ADDTOTITLE;

    if (!CFrameWnd::PreCreateWindow(cs))
        return FALSE;

    return TRUE;
}

/////////////////////////////////////////////////////////////////////////////
// CMainFrame diagnostics

#ifdef _DEBUG
void CMainFrame::AssertValid() const
{
    CFrameWnd::AssertValid();
}

void CMainFrame::Dump(CDumpContext& dc) const
{
    CFrameWnd::Dump(dc);
}

#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// CMainFrame message handlers


BOOL CMainFrame::OnCreateClient(LPCREATESTRUCT /*lpcs*/, CCreateContext* pContext)
{
    TRACE("CMainFrame::OnCreateClient\n");

    try
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(pContext->m_pCurrentDoc);
        static_cast<CEraserApp*>(AfxGetApp())->m_pDoc = pDoc;
        
        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));
        CRect r = pDoc->m_rWindowRect;

        pContext->m_pCurrentFrame = this;

        // restore window size - more error checking perhaps?
        if (r.top >= 0 && r.left >= 0 && r.Width() > 0 && r.Height() > 0)
            SetWindowPos(&wndTop, r.top, r.left, r.Width(), r.Height(), SWP_NOZORDER);

        // create splitter window
        if (!m_wndSplitter.CreateStatic(this, 1, 2)) return false;

        GetClientRect(&r);

        // restore outbar width
        if (pDoc->m_dwOutbarWidth == 0)
            pDoc->m_dwOutbarWidth = DEFAULT_OUTBAR_WIDTH;

        // create child frame
        if (!m_wndSplitter.CreateView(0, 1, RUNTIME_CLASS(CChildFrame),
                CSize(r.Width() - pDoc->m_dwOutbarWidth, r.Height()), pContext))
        {
            return false;
        }

        // set outbar properties and create it
        DWORD dwFlags = CGfxOutBarCtrl::fAnimation;

        if (pDoc->m_bSmallIconView)
            dwFlags |= CGfxOutBarCtrl::fSmallIcon;

        m_wndBar.Create(WS_CHILD|WS_VISIBLE, CRect(0,0,0,0), &m_wndSplitter,
                        m_wndSplitter.IdFromRowCol(0, 0), dwFlags);
        m_wndBar.SetOwner(this);

        m_imaLarge.Create(IDB_IMAGELIST_LARGE, 32, 0, RGB(0, 128, 128));
        m_imaSmall.Create(IDB_IMAGELIST_SMALL, 16, 0, RGB(0, 128, 128));

        m_wndBar.SetImageList(&m_imaLarge, CGfxOutBarCtrl::fLargeIcon);
        m_wndBar.SetImageList(&m_imaSmall, CGfxOutBarCtrl::fSmallIcon);

        m_wndBar.SetAnimationTickCount(FOLDERANIMATION_DELAY);

        if (pDoc->m_bIconAnimation)
            m_wndBar.SetAnimSelHighlight(ICONANIMATION_DELAY);

        // create tree view for explorer view
        m_wndTree.Create(WS_CHILD | TVS_HASLINES | TVS_LINESATROOT | TVS_HASBUTTONS |
                         TVS_SHOWSELALWAYS, CRect(0,0,0,0), &m_wndBar, 1010);
//        m_wndTree.SetImageList(&pDoc->m_smallImageList, TVSIL_NORMAL);
		m_wndTree.SetImageList(pDoc->m_smallImageList, TVSIL_NORMAL);

        // add folders
        m_wndBar.AddFolder("Eraser", 0);
        m_wndBar.AddFolderBar("Explorer", &m_wndTree);

        // add folder items
        m_wndBar.InsertItem(FolderEraser, ViewEraser, "On-Demand",
                            ViewEraser, ViewEraser);

        m_wndBar.InsertItem(FolderEraser, ViewScheduler, "Scheduler",
                            ViewScheduler, ViewScheduler);

        m_wndBar.SetSelFolder(0);

        // set windows widths
        m_wndSplitter.SetColumnInfo(0, pDoc->m_dwOutbarWidth, 0);
        m_wndSplitter.SetColumnInfo(1, r.Width() - pDoc->m_dwOutbarWidth, 0);
        m_wndSplitter.RecalcLayout();

        // and active view
        m_wndSplitter.SetActivePane(0, 1);
        m_pwndChild = static_cast<CChildFrame*>(m_wndSplitter.GetPane(0, 1));

        if (pDoc->m_dwStartView == VIEW_SCHEDULER)
        {
            SetInfoText(m_wndBar.GetItemText(1), !pDoc->m_bViewInfoBar);
            SetActiveView(m_pwndChild->m_pSchedulerView);
            m_iLastActiveItem = ViewScheduler;
        }
        else
        {
            SetInfoText(m_wndBar.GetItemText(0), !pDoc->m_bViewInfoBar);
            SetActiveView(m_pwndChild->m_pEraserView);
            m_iLastActiveItem = ViewEraser;
        }

        // give explorer view a handle to the tree
        m_pwndChild->m_pExplorerView->m_hwndTree = m_wndTree.GetSafeHwnd();

        // choose whether to display InfoBar
        if (!pDoc->m_bViewInfoBar)
            ShowControlBar(&m_pwndChild->m_InfoBar, FALSE, FALSE);

        // and we're done
        return true;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    return false;
}

LRESULT CMainFrame::OnOutbarNotify(WPARAM wParam, LPARAM lParam)
{
    try
    {
        CEraserDoc  *pDoc   = static_cast<CEraserApp*>(AfxGetApp())->m_pDoc;
        int         iFolder = (int)lParam;

        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

        if (m_pwndChild == 0)
            return 0;

        switch (wParam)
        {
        case NM_OB_ITEMCLICK:
            {
                int iItem = m_wndBar.GetItemData(iFolder);

                if (iItem == ViewScheduler)
                {
                    m_pwndChild->SwitchToForm(VIEW_SCHEDULER);

                    if (pDoc->m_wProcessCount > 0)
                        m_pwndChild->m_pSchedulerView->EraserWipeBegin();
                }
                else
                {
                    m_pwndChild->SwitchToForm(VIEW_ERASER);
                }

                SetInfoText(m_wndBar.GetItemText(iItem));
                m_iLastActiveItem = iItem;
            }
            break;
        case NM_PREFOLDERCHANGE:
            if (iFolder == FolderExplorer && m_wndTree.GetCount() == 0)
            {
                CWaitCursor wait;
                m_wndTree.PopulateTree();
            }
            break;
        case NM_FOLDERCHANGE:
            switch (iFolder)
            {
            case FolderEraser:
            default:
                if (m_iLastActiveItem == ViewEraser)
                    m_pwndChild->SwitchToForm(VIEW_ERASER);
                else
                {
                    m_pwndChild->SwitchToForm(VIEW_SCHEDULER);

                    if (pDoc->m_wProcessCount > 0)
                        m_pwndChild->m_pSchedulerView->EraserWipeBegin();
                }

                SetInfoText(m_wndBar.GetItemText(m_iLastActiveItem));

                // delete all other tree items except for the selected one
                // to save memory
                m_wndTree.DeleteInActiveItems();

                break;
            case FolderExplorer:
                m_iLastActiveItem =
                    (m_pwndChild->m_iActiveViewID == VIEW_ERASER) ? ViewEraser : ViewScheduler;

                m_pwndChild->SwitchToForm(VIEW_EXPLORER);
                SetInfoText("Explorer");
                break;
            }
            break;
        case NM_ICONANIMATION:
            // if the animation was off, set it on and vice versa
            pDoc->m_bIconAnimation = ((int)lParam == 0);
            m_wndBar.SetAnimSelHighlight(((pDoc->m_bIconAnimation) ? ICONANIMATION_DELAY : 0));
            break;
        case NM_ICONVIEWCHANGE:
            pDoc->m_bSmallIconView = m_wndBar.IsSmallIconView();
            break;
        default:
            break;
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    return 0;
}

void CMainFrame::SetInfoText(LPCTSTR info, BOOL setTitle /*=TRUE*/)
{
    if (info && m_pwndChild)
        m_pwndChild->m_InfoBar.SetText(info);

    if (setTitle)
    {
        try
        {
            CString strTitle;
            strTitle.LoadString(AFX_IDS_APP_TITLE);

            if (!m_pwndChild->m_InfoBar.IsWindowVisible())
            {
                strTitle += "  [";
                strTitle += m_pwndChild->m_InfoBar.GetText();
                strTitle += "]";
            }

            SetWindowText(strTitle);
        }
        catch (CException *e)
        {
            ASSERT(FALSE);
            REPORT_ERROR(e);
            e->Delete();
        }
    }
}

void CMainFrame::OnViewInfoBar()
{
    try
    {
        if (m_pwndChild)
        {
            ShowControlBar(&m_pwndChild->m_InfoBar, !m_pwndChild->m_InfoBar.IsWindowVisible(), FALSE);
            // update title
            SetInfoText(NULL);

            CEraserDoc *pDoc = static_cast<CEraserApp*>(AfxGetApp())->m_pDoc;
            if (pDoc)
                pDoc->m_bViewInfoBar = m_pwndChild->m_InfoBar.IsWindowVisible();
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

void CMainFrame::OnUpdateViewInfoBar(CCmdUI* pCmdUI)
{
    try
    {
        if (m_pwndChild)
            pCmdUI->SetCheck(m_pwndChild->m_InfoBar.IsWindowVisible());
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

void CMainFrame::OnSysCommand(UINT nID, LPARAM lParam)
{
    TRACE("CMainFrame::OnSysCommand\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(static_cast<CEraserApp*>(AfxGetApp())->m_pDoc);
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        if (nID == SC_MINIMIZE)
        {
            if (!pDoc->m_bNoTrayIcon || pDoc->m_bHideOnMinimize)
            {
                ShowWindow(SW_HIDE);
                return;
            }
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    CFrameWnd::OnSysCommand(nID, lParam);
}

void CMainFrame::OnDestroy()
{
    TRACE("CMainFrame::OnDestroy\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(static_cast<CEraserApp*>(AfxGetApp())->m_pDoc);
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        // save program state
        int cxCur = 0, cxMin = 0;

        // outbar width
        m_wndSplitter.GetColumnInfo(0, cxCur, cxMin);
        pDoc->m_dwOutbarWidth = cxCur;

        // start view
        if (m_iLastActiveItem == ViewScheduler)
            pDoc->m_dwStartView = VIEW_SCHEDULER;
        else
            pDoc->m_dwStartView = VIEW_ERASER;

        // window size
        GetWindowRect(&pDoc->m_rWindowRect);

		// ===========================================================
		//m_bmToolbarHi.DeleteObject();
		// ===========================================================

        // set active view to NULL, cause our
        // views were be destroyed in the child frame

        SetActiveView(NULL, FALSE);
        CFrameWnd::OnDestroy();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}