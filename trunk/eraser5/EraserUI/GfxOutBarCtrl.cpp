// Copyright (c) Iuri Apollonio 1998
// Use & modify as you want & need, and leave those 3 lines.
// http://www.codeguru.com

// Animation improvements and drag & drop support.
// Copyright © 1998-2001 by Sami Tolvanen.


// GfxOutBarCtrl.cpp : implementation file
//

#include "stdafx.h"
#include "resource.h"
#include "GfxOutBarCtrl.h"
#include "GfxGroupEdit.h"

#include "GfxPopupMenu.h"

#define _ID_GFX_SMALLICON   _T("Small Icons")
#define _ID_GFX_LARGEICON   _T("Large Icons")
#define _ID_GFX_REMOVEITEM  _T("Remove")
#define _ID_GFX_RENAMEITEM  _T("Rename")

// sami@tolvanen.com
#define _ID_GFX_ANIMATION   _T("Animation")

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CGfxOutBarCtrl

IMPLEMENT_DYNCREATE(CGfxOutBarCtrl, CDropTargetWnd)

CGfxOutBarCtrl::CGfxOutBarCtrl()
{
    crBackGroundColor = GetSysColor(COLOR_3DSHADOW);
    crBackGroundColor1 = GetSysColor(COLOR_3DSHADOW);
    crTextColor = GetSysColor(COLOR_WINDOW);
    cr3dFace = GetSysColor(COLOR_3DFACE);
    crLightBorder = GetSysColor(COLOR_3DLIGHT);
    crHilightBorder = GetSysColor(COLOR_3DHILIGHT);
    crShadowBorder = GetSysColor(COLOR_3DSHADOW);
    crDkShadowBorder = GetSysColor(COLOR_3DDKSHADOW);

    pBlackPen = new CPen(PS_SOLID, 1, RGB(0,0,0));

    iFolderHeight = 22;

    dwFlags = fDragItems|fEditGroups|fEditItems|fRemoveGroups|fRemoveItems|fAddGroups|fAnimation|fSelHighlight;

    iSelFolder = 0;

    iLastFolderHighlighted = -1;
    iLastSelectedFolder = -1;

    pLargeImageList = NULL;
    pSmallImageList = NULL;

    iFirstItem = 0;

    xSmallIconLabelOffset = 4;
    yLargeIconLabelOffset = 3;

    ySmallIconSpacing = 10;
    yLargeIconSpacing = 8;

    xLeftMargin = 5;
    yTopMargin = 5;

    bUpArrow = bDownArrow = false;

    hHandCursor = AfxGetApp()->LoadCursor(IDC_HANDCUR);
    rcUpArrow = rcDownArrow = CRect(0,0,0,0);

    bUpPressed = bDownPressed = false;

    bLooping = false;

    iLastItemHighlighted = -1;
    bPressedHighlight = false;

    iLastDragItemDraw = -1;

    lAnimationTickCount = -1;

    hDragCursor = AfxGetApp()->LoadCursor(IDC_DRAGGING);
    hNoDragCursor = AfxGetApp()->LoadCursor(IDC_NODRAGGING);

    iLastSel = -1;
    iSelAnimCount = 0;

    iSelAnimTiming = 0;

    m_bDragOver = FALSE;
}

CGfxOutBarCtrl::~CGfxOutBarCtrl()
{
    for (int t = 0; t < arFolder.GetSize(); t++)
    {
        if (arFolder.GetAt(t)) delete (CBarFolder*) arFolder.GetAt(t);
    }

    arFolder.RemoveAll();

    delete pBlackPen;
}


BEGIN_MESSAGE_MAP(CGfxOutBarCtrl, CDropTargetWnd)
    //{{AFX_MSG_MAP(CGfxOutBarCtrl)
    ON_WM_CREATE()
    ON_WM_TIMER()
    ON_WM_PAINT()
    ON_WM_ERASEBKGND()
    ON_WM_MOUSEMOVE()
    ON_WM_LBUTTONDOWN()
    ON_WM_SETCURSOR()
    ON_WM_SIZE()
    ON_WM_RBUTTONDOWN()
    ON_COMMAND(ID_GFX_LARGEICON, OnGfxLargeicon)
    ON_UPDATE_COMMAND_UI(ID_GFX_LARGEICON, OnUpdateGfxLargeicon)
    ON_COMMAND(ID_GFX_SMALLICON, OnGfxSmallicon)
    ON_UPDATE_COMMAND_UI(ID_GFX_SMALLICON, OnUpdateGfxSmallicon)
    ON_COMMAND(ID_GFX_REMOVEITEM, OnGfxRemoveitem)
    ON_UPDATE_COMMAND_UI(ID_GFX_REMOVEITEM, OnUpdateGfxRemoveitem)
    ON_COMMAND(ID_GFX_RENAMEITEM, OnGfxRenameitem)
    ON_UPDATE_COMMAND_UI(ID_GFX_RENAMEITEM, OnUpdateGfxRenameitem)
    ON_WM_SYSCOLORCHANGE()
    ON_COMMAND(ID_GFX_ANIMATION, OnGfxAnimation)
    //}}AFX_MSG_MAP
    ON_MESSAGE(WM_OUTBAR_NOTIFY, OnEndLabelEdit)
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// CGfxOutBarCtrl message handlers

LRESULT CGfxOutBarCtrl::OnEndLabelEdit(WPARAM wParam, LPARAM lParam)
{
    if (wParam == NM_OB_ONGROUPENDEDIT)
    {
        CGfxGroupEdit * pEdit = (CGfxGroupEdit *) lParam;
        OUTBAR_INFO ob;
        ob.index = pEdit->iIndex;
        ob.cText = pEdit->text;

        if (GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_OB_ONGROUPENDEDIT, (LPARAM) &ob))
        {
            SetFolderText(pEdit->iIndex, pEdit->text);
            CRect rc;
            GetFolderRect(pEdit->iIndex, rc);
            InvalidateRect(rc, false);
        }
    }
    if (wParam == NM_OB_ONLABELENDEDIT)
    {
        CGfxGroupEdit * pEdit = (CGfxGroupEdit *) lParam;
        OUTBAR_INFO ob;
        ob.index = pEdit->iIndex;
        ob.cText = pEdit->text;

        if (GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_OB_ONLABELENDEDIT, (LPARAM) &ob))
        {
            SetItemText(pEdit->iIndex, pEdit->text);
            CRect rc;
            GetInsideRect(rc);
            InvalidateRect(rc, true);
        }
    }
    return 0;
}

void CGfxOutBarCtrl::SetFolderText(const int index, const TCHAR * text)
{
    ASSERT(index >= 0 && index < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(index);
    if (pbf->cName)
    {
        delete [] pbf->cName;
        pbf->cName = NULL;
    }
    pbf->cName = new TCHAR[lstrlen(text)+1];
    lstrcpy(pbf->cName, text);
}

void CGfxOutBarCtrl::SetItemText(const int index, const TCHAR * text)
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(index);
    if (pi->cItem)
    {
        delete [] pi->cItem;
        pi->cItem = NULL;
    }
    pi->cItem = new TCHAR [lstrlen(text)+1];
    lstrcpy(pi->cItem, text);
}

BOOL CGfxOutBarCtrl::Create(DWORD dwStyle, const RECT & rect, CWnd * pParentWnd, UINT nID, const DWORD dwFlag)
{
    dwFlags = dwFlag;
    return CDropTargetWnd::CreateEx(WS_EX_STATICEDGE, NULL, NULL, dwStyle|WS_CHILD, rect, pParentWnd, nID);
    //GFXOUTBARCTRL_CLASSNAME
}

bool CGfxOutBarCtrl::IsSmallIconView() const
{
    return (dwFlags & fSmallIcon);
}

void CGfxOutBarCtrl::SetSmallIconView(const bool bSet)
{
    iFirstItem = 0;

    if (bSet)
    {
        if (!IsSmallIconView())
        {
            dwFlags |= fSmallIcon;
            CRect rc;
            GetInsideRect(rc);
            InvalidateRect(rc, false);
        }
    }
    else
    {
        if (IsSmallIconView())
        {
            dwFlags &= ~fSmallIcon;
            CRect rc;
            GetInsideRect(rc);
            InvalidateRect(rc, false);
        }
    }
}

DWORD CGfxOutBarCtrl::GetFlag() const
{
    return dwFlags;
}

void CGfxOutBarCtrl::ModifyFlag(const DWORD & dwRemove, const DWORD & dwAdd, const UINT redraw)
{
    if (dwRemove)   dwFlags &= ~(dwRemove);
    if (dwAdd)      dwFlags |= dwAdd;

    if (GetSafeHwnd())
    {
        if (redraw != 0) SetWindowPos(0,0,0,0,0,SWP_NOZORDER|SWP_NOSIZE|SWP_NOMOVE|redraw);
        else Invalidate();
    }
}

int CGfxOutBarCtrl::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
    if (CDropTargetWnd::OnCreate(lpCreateStruct) == -1)
        return -1;

    Register();
    return 0;
}

void CGfxOutBarCtrl::OnTimer(UINT_PTR nIDEvent)
{
    if (nIDEvent == 1)
    {
        DWORD pos = GetMessagePos();
        CPoint pt(MAKEPOINTS(pos).x, MAKEPOINTS(pos).y);

        ScreenToClient(&pt);

        CRect rc;
        GetClientRect(&rc);
        if (!(rc.PtInRect(pt)))
        {
            HighlightFolder(-1);
            HighlightItem(-1);
            KillTimer(1);
        }
    }
    else if (nIDEvent == 2)
    {
        if (m_bDragOver)
        {
            TRACE("CGfxOutBarCtrl::OnTimer(2)\n");

            DWORD pos = GetMessagePos();
            CPoint pt(MAKEPOINTS(pos).x, MAKEPOINTS(pos).y);
            ScreenToClient(&pt);

            int idx, ht = HitTestEx(pt, idx);

            if (ht == htFolder && idx != iSelFolder)
                SetSelFolder(idx);
            else if (ht == htItem && idx != iLastSel)
            {
                if (iSelAnimTiming > 0 && iLastSel >= 0)
                    DrawAnimItem(0, 0, iLastSel);

                if (dwFlags & fSelHighlight && iLastSel >= 0)
                {
                    CRect rc;
                    GetIconRect(iSelFolder, iLastSel, rc);
                    rc.InflateRect(1,1);
                    InvalidateRect(rc);
                }

                iLastSel = idx;

                if (dwFlags & fSelHighlight && iLastSel >= 0)
                {
                    CRect rc;
                    GetIconRect(iSelFolder, iLastSel, rc);
                    rc.InflateRect(1,1);
                    InvalidateRect(rc);
                }

                GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_OB_ITEMCLICK, idx);
            }
        }
    }
    else if (nIDEvent == 3 && iLastSel >= 0)
    {
        iSelAnimCount ++;
        if (iSelAnimCount > 10) iSelAnimCount = -1;
        if (iSelAnimCount == 0)
            DrawAnimItem(-1, 1, iLastSel);
        if (iSelAnimCount == 1)
            DrawAnimItem(0, 0, iLastSel);
        if (iSelAnimCount == 2)
            DrawAnimItem(1, 1, iLastSel);
        if (iSelAnimCount == 3)
            DrawAnimItem(0, 0, iLastSel);
    }
    CDropTargetWnd::OnTimer(nIDEvent);
}

void CGfxOutBarCtrl::DrawAnimItem(const int xoffset, const int yoffset, const int index)
{
    CImageList * ima = GetFolderImageList(iSelFolder, IsSmallIconView());

    CRect rc, irc;
    GetInsideRect(irc);
    GetItemRect(iSelFolder, iLastSel, rc);

    // sami@tolvanen.com    added IsWindowVisible

    if (IsWindowVisible() && iLastSel >= iFirstItem && irc.bottom > rc.bottom && irc.top < rc.top)
    {
        ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
        CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);

        ASSERT(index >= 0 && index < pbf->GetItemCount());
        CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(index);

        ASSERT(pi && ima);

        CClientDC dc(this);

        if (IsSmallIconView())
        {
            if (ima)
            {
                IMAGEINFO ii;
                ima->GetImageInfo(pi->iImageIndex, &ii);
                CSize szImage = CRect(ii.rcImage).Size();
                CPoint pt;
                pt.x = rc.left + 2;
                pt.y = rc.top + (rc.Height() - szImage.cy) / 2;

                CRect rcBck(pt.x-1, pt.y-1, pt.x + szImage.cx+2, pt.y + szImage.cy+2);
                dc.FillSolidRect(rcBck, crBackGroundColor);

                pt.x += xoffset;
                pt.y += yoffset;

                ima->Draw(&dc, pi->iImageIndex, pt, ILD_NORMAL);

            }
        }
        else
        {
            if (ima)
            {
                IMAGEINFO ii;
                ima->GetImageInfo(pi->iImageIndex, &ii);
                CSize szImage = CRect(ii.rcImage).Size();
                CPoint pt;
                pt.x = rc.left + (rc.Width() - szImage.cx) / 2;
                pt.y = rc.top;

                CRect rcBck(pt.x-1, pt.y-1, pt.x + szImage.cx+2, pt.y + szImage.cy+2);
                dc.FillSolidRect(rcBck, crBackGroundColor);

                pt.x += xoffset;
                pt.y += yoffset;

                ima->Draw(&dc, pi->iImageIndex, pt, ILD_NORMAL);
            }
        }
    }
}

void CGfxOutBarCtrl::OnPaint()
{
    CRect clientRc;
    GetClientRect(&clientRc);

    CPaintDC dc(this);
    CDC memDC;
    memDC.CreateCompatibleDC(&dc);

    CBitmap bmp;
    bmp.CreateCompatibleBitmap(&dc, clientRc.Width(), clientRc.Height());

    CBitmap *obmp = memDC.SelectObject(&bmp);
    CDC *pDC = &memDC;

    CRect rc;
    GetInsideRect(rc);

    if (!GetFolderChild())
        pDC->FillSolidRect(rc, crBackGroundColor);


    int max = arFolder.GetSize();
    CRect frc;

    for (int t = 0; t < max; t++)
    {
        GetFolderRect(t, frc);
        DrawFolder(pDC, t, frc, false);
    }

    if (!GetFolderChild())
    {
        int f = 0, l = 0;

        GetVisibleRange(iSelFolder, f, l);

        rcUpArrow.SetRect(0, 0, GetSystemMetrics(SM_CXVSCROLL), GetSystemMetrics(SM_CXVSCROLL));
        rcDownArrow = rcUpArrow;

        rcUpArrow.OffsetRect(rc.right - 5 - GetSystemMetrics(SM_CXVSCROLL), rc.top + 5);
        rcDownArrow.OffsetRect(rc.right - 5 - GetSystemMetrics(SM_CXVSCROLL), rc.bottom -
                               5 - GetSystemMetrics(SM_CXVSCROLL));

        if (f > 0)
        {
            if (bUpPressed)
                pDC->DrawFrameControl(rcUpArrow, DFC_SCROLL, DFCS_SCROLLUP|DFCS_PUSHED);
            else
                pDC->DrawFrameControl(rcUpArrow, DFC_SCROLL, DFCS_SCROLLUP);

            bUpArrow = true;
            pDC->ExcludeClipRect(rcUpArrow);
        }
        else
            bUpArrow = false;

        if (l < GetItemCount() - 1)
        {
            if (bDownPressed)
                pDC->DrawFrameControl(rcDownArrow, DFC_SCROLL, DFCS_SCROLLDOWN|DFCS_PUSHED);
            else
                pDC->DrawFrameControl(rcDownArrow, DFC_SCROLL, DFCS_SCROLLDOWN);

            bDownArrow = true;
            pDC->ExcludeClipRect(rcDownArrow);
        }
        else
            bDownArrow = false;

        PaintItems(pDC, iSelFolder, rc);
    }
    else
    {
        // 24.5.1999 sami@tolvanen.com
        // if we do not draw anything, it causes flickering,
        // so let the child window draw its contents here.
        // I know this will result in sending the WM_PAINT twice
        // to the child window, but hey, anything to get rid of
        // that nasty flicker! right?

        CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
        CWnd *pwnd = pbf->pChild;

        CPoint ovpt = pDC->SetViewportOrg(rc.left, rc.top);
        pwnd->SendMessage(WM_PAINT, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);

        pDC->SetViewportOrg(ovpt);
    }

    dc.BitBlt(clientRc.left, clientRc.top, clientRc.Width(), clientRc.Height(), &memDC, 0,0, SRCCOPY);

    memDC.SelectObject(obmp);

    if (iLastFolderHighlighted >= 0)
    {
        int i = iLastFolderHighlighted;
        iLastFolderHighlighted = -1;
        HighlightFolder(i);
    }
    if (iLastItemHighlighted >= 0)
    {
        int i = iLastItemHighlighted;
        iLastItemHighlighted = -1;
        HighlightItem(i, bPressedHighlight);
    }
}

BOOL CGfxOutBarCtrl::OnEraseBkgnd(CDC* /*pDC*/)
{
    return true;
}

bool CGfxOutBarCtrl::GetFolderRect(const int iIndex, CRect & rect) const
{
    int max = arFolder.GetSize();
    ASSERT(iIndex >= 0 && iIndex < max);

    if (iIndex >= 0 && iIndex < max)
    {
        CRect rc;
        GetClientRect(rc);
        if (iIndex > iSelFolder)
            rect.SetRect(rc.left, rc.bottom - ((max - iIndex)) * iFolderHeight, rc.right,
                         rc.bottom - (max - iIndex - 1) * iFolderHeight);
        else
            rect.SetRect(rc.left, rc.top + iIndex * iFolderHeight - 1, rc.right,
                         rc.top + (1 + iIndex) * iFolderHeight - 1);
        return true;
    }
    return false;
}

void CGfxOutBarCtrl::GetItemRect(const int iFolder, const int iIndex, CRect & rect)
{
    CRect rc;
    GetInsideRect(rc);
    int top = rc.top;
    CSize sz(0,0);
    int y = 0;
    for (int t = 0; t < iIndex; t++)
    {
        sz = GetItemSize(iFolder, t, ircAll);
        top += sz.cy;

        if (IsSmallIconView())
        {
            top += ySmallIconSpacing;
        }
        else
        {
            top += yLargeIconSpacing;
        }
        if (t == iFirstItem - 1) y = top - rc.top;
    }
    sz = GetItemSize(iFolder, iIndex, ircAll);
    rect.SetRect(rc.left, top, rc.left + sz.cx, top + sz.cy);

    rect.top -= y;
    rect.bottom -= y;

    rect.left += xLeftMargin;
    rect.top  += yTopMargin;
    rect.bottom += yTopMargin;

    if (!IsSmallIconView())
    {
        rect.left = rc.left;
        rect.right = rc.right;
    }
}

void CGfxOutBarCtrl::DrawFolder(CDC * pDC, const int iIdx, CRect rect, const bool bSelected)
{
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iIdx);

    if (!bSelected)
    {
        CPen pn1(PS_SOLID, 1, crBackGroundColor1);
        CPen * op = pDC->SelectObject(&pn1);
        pDC->MoveTo(rect.left, rect.top);
        pDC->LineTo(rect.right, rect.top);
        pDC->SelectObject(op);

        rect.top ++;

        pDC->Draw3dRect(rect, crHilightBorder, crBackGroundColor1);
        rect.InflateRect(-1,-1);
        pDC->FillSolidRect(rect, cr3dFace);
        int obk = pDC->SetBkMode(TRANSPARENT);
        CFont * of = pDC->SelectObject(CFont::FromHandle((HFONT)GetStockObject(DEFAULT_GUI_FONT)));
        pDC->DrawText(CString(pbf->cName), rect, DT_CENTER|DT_VCENTER|DT_SINGLELINE);
        pDC->SetBkMode(obk);
        pDC->SelectObject(of);
    }
    else
    {
        CPen pn1(PS_SOLID, 1, crBackGroundColor1);
        CPen * op = pDC->SelectObject(&pn1);
        pDC->MoveTo(rect.left+1, rect.top);
        pDC->LineTo(rect.right, rect.top);
        pDC->SelectObject(op);

        rect.top ++;

        pDC->Draw3dRect(rect, crDkShadowBorder, crHilightBorder);
        rect.InflateRect(-1,-1);
        pDC->Draw3dRect(rect, crBackGroundColor1, cr3dFace);
        rect.InflateRect(-1,-1);

        pDC->FillSolidRect(rect, cr3dFace);

        int obk = pDC->SetBkMode(TRANSPARENT);
        CFont * of = pDC->SelectObject(CFont::FromHandle((HFONT)GetStockObject(DEFAULT_GUI_FONT)));
        pDC->DrawText(CString(pbf->cName), rect, DT_CENTER|DT_VCENTER|DT_SINGLELINE);
        pDC->SetBkMode(obk);
        pDC->SelectObject(of);
    }
}

int CGfxOutBarCtrl::AddFolder(const TCHAR * cFolderName, const DWORD exData)
{
    CBarFolder * pbf = new CBarFolder(cFolderName, exData);
    ASSERT(pbf);
    arFolder.Add((void *)pbf);

    return arFolder.GetSize() - 1;
}

CGfxOutBarCtrl::CBarFolder::CBarFolder(const TCHAR * name, DWORD exData)
{
    cName = NULL;
    dwData = exData;

    if (name)
    {
        cName = new TCHAR[lstrlen(name)+1];
        ASSERT(cName);
        lstrcpy(cName, name);
    }
    pLargeImageList = NULL;
    pSmallImageList = NULL;
    pChild = NULL;
}

CGfxOutBarCtrl::CBarFolder::~CBarFolder()
{
    if (cName) delete [] cName;
    for (int t = 0; t < arItems.GetSize(); t++)
        if (arItems.GetAt(t)) delete (CBarFolder*) arItems.GetAt(t);

    arItems.RemoveAll();
}

void CGfxOutBarCtrl::GetInsideRect(CRect & rect) const
{
    GetClientRect(rect);
    if (arFolder.GetSize() > 0)
    {
        int max = arFolder.GetSize();
        rect.top += iFolderHeight * (iSelFolder + 1) - 1;//+ 2;
        rect.bottom -= (max - iSelFolder - 1)*iFolderHeight;
        return;
    }
}

void CGfxOutBarCtrl::OnMouseMove(UINT nFlags, CPoint point)
{
    if (AfxGetMainWnd()!=GetActiveWindow())
        return;

    int index, ht = HitTestEx(point, index);

    if (ht != htFolder && iLastFolderHighlighted >= 0) HighlightFolder(-1);
    if (ht != htItem   && iLastItemHighlighted   >= 0) HighlightItem(-1);

    if (ht == htFolder)
    {
        HighlightFolder(index);
        SetTimer(1,100,NULL);
    }
    else if (ht == htItem)
    {
        HighlightItem(index);
        SetTimer(1,100,NULL);
    }

    CDropTargetWnd::OnMouseMove(nFlags, point);
}

int CGfxOutBarCtrl::HitTestEx(const CPoint & point, int &index)
{
    if (bUpArrow && rcUpArrow.PtInRect(point)) return htUpScroll;
    if (bDownArrow && rcDownArrow.PtInRect(point)) return htDownScroll;

    int max = arFolder.GetSize(), t;

    CRect rc;
    for (t = 0; t < max; t++)
    {
        GetFolderRect(t, rc);
        if (rc.PtInRect(point))
        {
            index = t;
            return htFolder;
        }
    }
    GetInsideRect(rc);
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    max = pbf->GetItemCount();
    for (t = iFirstItem; t < max; t++)
    {
//      CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(t);
        CRect itemRc;
        if (!IsSmallIconView())
        {
            GetIconRect(iSelFolder, t, itemRc);
            if (itemRc.PtInRect(point))
            {
                index = t;
                return htItem;
            }
            else if (itemRc.top > rc.bottom) break;
            GetLabelRect(iSelFolder, t, itemRc);
            itemRc.top -= yLargeIconLabelOffset;

            if (itemRc.PtInRect(point))
            {
                index = t;
                return htItem;
            }
            else if (itemRc.top > rc.bottom) break;
        }
        else
        {
            GetItemRect(iSelFolder, t, itemRc);
            if (itemRc.PtInRect(point))
            {
                index = t;
                return htItem;
            }
            else if (itemRc.top > rc.bottom) break;
        }
    }
    return htNothing;
}

void CGfxOutBarCtrl::HighlightFolder(const int index)
{
    CWnd * pf = GetFocus();
    if (pf != NULL && pf != this && IsChild(pf)) return;

    if (iLastFolderHighlighted == index) return;

    if (iLastFolderHighlighted >= 0)
    {
        CRect rc;
        if (GetFolderRect(iLastFolderHighlighted, rc))
        {
            CClientDC dc(this);
            CPen pn(PS_SOLID, 1, crBackGroundColor1);//crShadowBorder);
            CPen * op = dc.SelectObject(&pn);
            dc.MoveTo(rc.left, rc.bottom-1);
            dc.LineTo(rc.right-1, rc.bottom-1);
            dc.LineTo(rc.right-1, rc.top);
            CPen pn1(PS_SOLID, 1, cr3dFace);
            dc.SelectObject(&pn1);
            dc.MoveTo(rc.left+1, rc.bottom-2);
            dc.LineTo(rc.right-2, rc.bottom-2);
            dc.LineTo(rc.right-2, rc.top+1);
            dc.SelectObject(op);
        }
    }
    iLastFolderHighlighted = index;
    if (iLastFolderHighlighted >= 0)
    {
        CRect rc;
        if (GetFolderRect(iLastFolderHighlighted, rc))
        {
            CClientDC dc(this);
            CPen pn(PS_SOLID, 1, crDkShadowBorder);
            CPen * op = dc.SelectObject(&pn);
            dc.MoveTo(rc.left, rc.bottom-1);
            dc.LineTo(rc.right-1, rc.bottom-1);
            dc.LineTo(rc.right-1, rc.top);
            CPen pn1(PS_SOLID, 1, crBackGroundColor1);
            dc.SelectObject(&pn1);
            dc.MoveTo(rc.left+1, rc.bottom-2);
            dc.LineTo(rc.right-2, rc.bottom-2);
            dc.LineTo(rc.right-2, rc.top+1);
            dc.SelectObject(op);
        }
    }
}

void CGfxOutBarCtrl::OnLButtonDown(UINT nFlags, CPoint point)
{
    if (GetFocus() != this) SetFocus();

    int index, ht = HitTestEx(point, index);
    iLastDragItemDrawType = -1;

    CRect inRc;
    GetInsideRect(inRc);

    if (ht == htFolder)
    {
        bool bHigh = true;
        CRect rc;
        GetFolderRect(index, rc);

        if (::GetCapture() == NULL)
        {
            SetCapture();
            ASSERT(this == GetCapture());
            CClientDC dc(this);
            DrawFolder(&dc, index, rc, true);
            AfxLockTempMaps();
            for (;;)
            {
                MSG msg;
                VERIFY(::GetMessage(&msg, NULL, 0, 0));

                if (CDropTargetWnd::GetCapture() != this) break;

                switch (msg.message)
                {
                case WM_MOUSEMOVE:
                    {
                        CPoint pt(msg.lParam);
                        int idx, ht1 = HitTestEx(pt, idx);
                        if (ht1 == htFolder && idx == index)
                        {
                            if (!bHigh)
                            {
                                DrawFolder(&dc, index, rc, true);
                                bHigh = true;
                            }
                        }
                        else
                        {
                            if (bHigh)
                            {
                                DrawFolder(&dc, index, rc, false);
                                bHigh = false;
                            }
                        }
                    }
                    break;

                case WM_LBUTTONUP:
                    {
                        if (bHigh)
                        {
                            DrawFolder(&dc, index, rc, false);
                            bHigh = false;
                        }
                        CPoint pt(msg.lParam);
                        int idx, ht1 = HitTestEx(pt, idx);
                        if (ht1 == htFolder && idx != iSelFolder)
                            SetSelFolder(idx);
                    }

                    goto ExitLoop2;

                case WM_KEYDOWN:
                    if (msg.wParam != VK_ESCAPE)
                        break;

                default:
                    DispatchMessage(&msg);
                    break;
                }
            }

        ExitLoop2:
            ReleaseCapture();
            AfxUnlockTempMaps(FALSE);
        }
        if (bHigh) InvalidateRect(rc, false);
    }
    else iLastSelectedFolder = -1;

    if (ht == htItem)
    {
        iLastDragItemDraw = -1;
        bool bHigh = true, bDragging = false;
        CRect rc;
        GetItemRect(iSelFolder, index, rc);

        HCURSOR hCur = GetCursor();

        if (::GetCapture() == NULL)
        {
            SetCapture();
            ASSERT(this == GetCapture());
            CClientDC dc(this);
            HighlightItem(index, true);
            AfxLockTempMaps();
            for (;;)
            {
                MSG msg;
                VERIFY(::GetMessage(&msg, NULL, 0, 0));

                if (CDropTargetWnd::GetCapture() != this) break;

                switch (msg.message)
                {
                case WM_MOUSEMOVE:
                    {
                        CPoint pt(msg.lParam);
                        int idx, ht1 = HitTestEx(pt, idx);
                        if (bDragging)
                        {
                            if (ht1 == htItem)
                            {
                                DrawDragArrow(&dc, index, idx);
                                SetCursor(hDragCursor);
                                hCur = hDragCursor;
                            }
                            else
                            {
                                CRect rcItem;
                                GetItemRect(iSelFolder, GetItemCount() - 1, rcItem);
                                if (pt.y > rcItem.bottom && pt.y < inRc.bottom)
                                {
                                    DrawDragArrow(&dc, index, GetItemCount());
                                    SetCursor(hDragCursor);
                                    hCur = hDragCursor;
                                }
                                else
                                {
                                    DrawDragArrow(&dc, index, -1);
                                    SetCursor(hNoDragCursor);
                                    hCur = hNoDragCursor;
                                }
                            }
                        }
                        else
                        {
                            if (ht1 == htItem && idx == index)
                            {
                                if (!bHigh)
                                {
                                    HighlightItem(index, true);
                                    bHigh = true;
                                    bPressedHighlight = true;
                                }
                            }
                            else
                            {
                                if (ht1 == htItem)
                                {
                                    if (bHigh)
                                    {
                                        HighlightItem(index, false);
                                        bHigh = false;
                                        bPressedHighlight = false;
                                    }
                                }
                                else
                                {
                                    if (dwFlags&fDragItems)
                                    {
                                        HighlightItem(index, true);
                                        bHigh = true;
                                        bDragging = true;

                                        SetCursor(hDragCursor);
                                        hCur = hDragCursor;

                                        bPressedHighlight = true;
                                    }
                                }
                            }
                        }
                    }
                    break;

                case WM_SETCURSOR:
                    SetCursor(hCur);
                    break;

                case WM_LBUTTONUP:
                    {
                        if (bHigh)
                        {
                            HighlightItem(-1);
                            bHigh = false;
                        }
                        CPoint pt(msg.lParam);
                        int idx, ht1 = HitTestEx(pt, idx);
                        if (!bDragging)
                        {
                            if (ht1 == htItem && idx == index)
                            {
                                if (iSelAnimTiming > 0 && index != iLastSel && iLastSel >= 0)
                                {
                                    DrawAnimItem(0, 0, iLastSel);
                                }

                                if (dwFlags&fSelHighlight && iLastSel >= 0)
                                {
                                    CRect rc;
                                    GetIconRect(iSelFolder, iLastSel, rc);
                                    rc.InflateRect(1,1);
                                    InvalidateRect(rc);
                                }

                                iLastSel = index;

                                if (dwFlags&fSelHighlight && iLastSel >= 0)
                                {
                                    CRect rc;
                                    GetIconRect(iSelFolder, iLastSel, rc);
                                    rc.InflateRect(1,1);
                                    InvalidateRect(rc);
                                }

                                GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_OB_ITEMCLICK, idx);
                            }
                        }
                        else
                        {
                            if (ht1 == htItem)
                            {
                                if (idx != index)
                                {
                                    OUTBAR_INFO ob;
                                    ob.iDragFrom = iSelFolder;
                                    ob.iDragTo = index;
                                    if (GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_OB_DRAGITEM, (LPARAM) &ob))
                                    {
                                        CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
                                        CBarItem * piFrom = (CBarItem *) pbf->arItems.GetAt(index);
                                        pbf->arItems.RemoveAt(index);
                                        if (idx > index) idx --;
                                        pbf->arItems.InsertAt(idx, piFrom);
                                    }
                                }
                            }
                            else
                            {
                                CRect rcItem;
                                GetItemRect(iSelFolder, GetItemCount() - 1, rcItem);
                                if (pt.y > rcItem.bottom && pt.y < inRc.bottom)
                                {
                                    OUTBAR_INFO ob;
                                    ob.iDragFrom = iSelFolder;
                                    ob.iDragTo = index;
                                    if (GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_OB_DRAGITEM, (LPARAM) &ob))
                                    {
                                        CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
                                        CBarItem * piFrom = (CBarItem *) pbf->arItems.GetAt(index);
                                        pbf->arItems.RemoveAt(index);
                                        pbf->arItems.Add((void *) piFrom);
                                    }
                                }
                            }
                        }
                    }

                    goto ExitLoop4;

                default:
                    DispatchMessage(&msg);
                    break;
                }
            }

        ExitLoop4:
            ReleaseCapture();
            AfxUnlockTempMaps(FALSE);
            if (bDragging)
            {
                Invalidate();
            }
        }
        bPressedHighlight = false;
        if (bHigh) InvalidateRect(rc, false);
    }
    else iLastItemHighlighted = -1;

    if (ht == htDownScroll)
    {
        bLooping = true;
        bool bHigh = true;

        if (::GetCapture() == NULL)
        {
            SetCapture();
            ASSERT(this == GetCapture());
            CClientDC dc(this);
            dc.DrawFrameControl(rcDownArrow, DFC_SCROLL, DFCS_SCROLLDOWN|DFCS_PUSHED);
//          SetTimer(2,300,NULL);
            AfxLockTempMaps();
            for (;;)
            {
                MSG msg;
                VERIFY(::GetMessage(&msg, NULL, 0, 0));

                if (CDropTargetWnd::GetCapture() != this) break;

                switch (msg.message)
                {
                case WM_MOUSEMOVE:
                    {
                        CPoint pt(msg.lParam);
                        if (rcDownArrow.PtInRect(pt))
                        {
                            if (bHigh == false)
                            {
                                dc.DrawFrameControl(rcDownArrow, DFC_SCROLL, DFCS_SCROLLDOWN|DFCS_PUSHED);
                                bHigh = true;
                                bDownArrow = true;
                                bDownPressed = true;
                            }
                        }
                        else
                        {
                            if (bHigh == true)
                            {
                                dc.DrawFrameControl(rcDownArrow, DFC_SCROLL, DFCS_SCROLLDOWN);
                                bHigh = false;
                                bDownArrow = false;
                                bDownPressed = false;
                            }
                        }
                    }
                    break;

                case WM_LBUTTONUP:
                    {
                        if (bHigh)
                        {
                            dc.DrawFrameControl(rcDownArrow, DFC_SCROLL, DFCS_SCROLLDOWN);
                            bHigh = false;
                        }
                        bDownArrow = false;
                        CPoint pt(msg.lParam);
                        if (rcDownArrow.PtInRect(pt))
                        {
                            CRect itrc;
                            GetItemRect(iSelFolder, GetItemCount() - 1, itrc);
                            CRect crc;
                            GetInsideRect(crc);
                            if (itrc.bottom > crc.bottom)
                            {
                                iFirstItem ++;
                                InvalidateRect(crc, true);
                            }
                        }
                    }
                    goto ExitLoop3;


                case WM_TIMER:
                    {
                        if (msg.wParam == 2)
                        {
                            if (bHigh)
                            {
                                CPoint pt(msg.pt);
                                ScreenToClient(&pt);
                                if (rcDownArrow.PtInRect(pt))
                                {
                                    bDownPressed = true;
                                    CRect itrc;
                                    GetItemRect(iSelFolder, GetItemCount() - 1, itrc);
                                    CRect crc;
                                    GetInsideRect(crc);
                                    if (itrc.bottom > crc.bottom)
                                    {
                                        iFirstItem ++;
                                        InvalidateRect(crc, true);
                                    }
                                    else goto ExitLoop3;
                                }
                                else bDownPressed = false;
                            }
                        }
                        break;
                    }

                case WM_KEYDOWN:
                    if (msg.wParam != VK_ESCAPE)
                        break;

                default:
                    DispatchMessage(&msg);
                    break;
                }
            }

        ExitLoop3:
//          KillTimer(2);
            ReleaseCapture();
            AfxUnlockTempMaps(FALSE);
            bLooping = false;
            bDownPressed = false;
            bDownArrow = false;
            CRect crc;
            GetInsideRect(crc);
            InvalidateRect(crc, true);
        }
    }

    if (ht == htUpScroll)
    {
        bLooping = true;
        bool bHigh = true;

        if (::GetCapture() == NULL)
        {
            SetCapture();
            ASSERT(this == GetCapture());
            CClientDC dc(this);
            dc.DrawFrameControl(rcUpArrow, DFC_SCROLL, DFCS_SCROLLUP|DFCS_PUSHED);
//          SetTimer(2,300,NULL);
            AfxLockTempMaps();
            for (;;)
            {
                MSG msg;
                VERIFY(::GetMessage(&msg, NULL, 0, 0));

                if (CDropTargetWnd::GetCapture() != this) break;

                switch (msg.message)
                {
                case WM_MOUSEMOVE:
                    {
                        CPoint pt(msg.lParam);
                        if (rcUpArrow.PtInRect(pt))
                        {
                            if (bHigh == false)
                            {
                                dc.DrawFrameControl(rcUpArrow, DFC_SCROLL, DFCS_SCROLLUP|DFCS_PUSHED);
                                bHigh = true;
                                bUpArrow = true;
                                bUpPressed = true;
                            }
                        }
                        else
                        {
                            if (bHigh == true)
                            {
                                dc.DrawFrameControl(rcUpArrow, DFC_SCROLL, DFCS_SCROLLUP);
                                bHigh = false;
                                bUpArrow = false;
                                bUpPressed = false;
                            }
                        }
                    }
                    break;

                case WM_LBUTTONUP:
                    {
                        if (bHigh)
                        {
                            dc.DrawFrameControl(rcUpArrow, DFC_SCROLL, DFCS_SCROLLUP);
                            bHigh = false;
                        }
                        bUpArrow = false;
                        CPoint pt(msg.lParam);
                        if (rcUpArrow.PtInRect(pt))
                        {
                            if (iFirstItem > 0 )
                            {
                                iFirstItem --;
                                CRect crc;
                                GetInsideRect(crc);
                                InvalidateRect(crc, true);
                            }
                        }
                    }

                    goto ExitLoop;

                case WM_TIMER:
                    {
                        if (msg.wParam == 2)
                        {
                            if (bHigh)
                            {
                                CPoint pt(msg.pt);
                                ScreenToClient(&pt);
                                if (rcUpArrow.PtInRect(pt))
                                {
                                    bUpPressed = true;
                                    if (iFirstItem > 0)
                                    {
                                        iFirstItem --;
                                        CRect crc;
                                        GetInsideRect(crc);
                                        InvalidateRect(crc, true);
                                    }
                                    else goto ExitLoop;
                                }
                                else bUpPressed = false;
                            }
                        }
                        break;
                    }
                case WM_KEYDOWN:
                    if (msg.wParam != VK_ESCAPE) break;

                default:
                    DispatchMessage(&msg);
                    break;
                }
            }

        ExitLoop:
//          KillTimer(2);
            ReleaseCapture();
            AfxUnlockTempMaps(FALSE);
            bLooping = false;
            bUpPressed = false;
            bUpArrow = false;
            CRect crc;
            GetInsideRect(crc);
            InvalidateRect(crc, true);
        }
    }

    CDropTargetWnd::OnLButtonDown(nFlags, point);
}


int CGfxOutBarCtrl::InsertItem(const int folder, const int index, const TCHAR * text, const int image, const DWORD exData)
{
    ASSERT(folder >= 0 && folder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(folder);
    return pbf->InsertItem(index, text, image, exData);
}

int CGfxOutBarCtrl::GetItemCount() const
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    return  pbf->GetItemCount();
}

void CGfxOutBarCtrl::SetSelFolder(const int index)
{
    ASSERT(index >= 0 && index < GetFolderCount());
    if (index == iSelFolder) return;

    // sami@tolvanen.com
    GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_PREFOLDERCHANGE, (LPARAM) index);

    CWnd * pc = GetFolderChild();
    if (pc) pc->ShowWindow(SW_HIDE);

    if (index != iSelFolder)
    {
        if (dwFlags & fAnimation &&
            lAnimationTickCount >= 0)
        {
            AnimateFolderScroll(iSelFolder, index);
        }
    }

    iSelFolder = index;
    iFirstItem = 0;
    iLastSel = -1;

    pc = GetFolderChild();

    if (pc)
    {
        CRect rc;
        GetInsideRect(rc);
        pc->MoveWindow(rc);
        pc->ShowWindow(SW_SHOW);
    }

    GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_FOLDERCHANGE, (LPARAM) iSelFolder);

    Invalidate();
}

int CGfxOutBarCtrl::GetFolderCount() const
{
    return arFolder.GetSize();
}

int CGfxOutBarCtrl::GetSelFolder() const
{
    return iSelFolder;
}

void CGfxOutBarCtrl::RemoveFolder(const int index)
{
    ASSERT(index >= 0 && index < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(index);
    delete pbf;
    arFolder.RemoveAt(index);
    if (iSelFolder >= index) iSelFolder = index - 1;
    if (iSelFolder < 0 && GetFolderCount() > 0) iSelFolder = 0;
    Invalidate();
}

int CGfxOutBarCtrl::CBarFolder::InsertItem(int index, const TCHAR * text, const int image, const DWORD exData)
{
    if (index < 0 || index > GetItemCount()) index = GetItemCount();

    int c = GetItemCount();

    CBarItem * pbf = new CBarItem(text, image, exData);
    ASSERT(pbf);
    if (index < GetItemCount())
        arItems.InsertAt(index, (void *) pbf);
    else
        arItems.Add((void *) pbf);

    c = GetItemCount();

    return index;
}

int CGfxOutBarCtrl::GetCountPerPage() const
{
    return 0;
}

CImageList * CGfxOutBarCtrl::SetImageList(CImageList * pImageList, int nImageList)
{
    CImageList * o = NULL;
    if (nImageList == fSmallIcon)
    {
        o = pSmallImageList;
        pSmallImageList = pImageList;
    }
    else if (nImageList == fLargeIcon)
    {
        o = pLargeImageList;
        pLargeImageList = pImageList;
    }
    return o;
}

CImageList * CGfxOutBarCtrl::GetImageList(CImageList * /*pImageList*/, int nImageList)
{
    if (nImageList == fSmallIcon) return pSmallImageList;
    else if (nImageList == fLargeIcon) return pLargeImageList;
    return NULL;
}

CImageList * CGfxOutBarCtrl::SetFolderImageList(const int folder, CImageList * pImageList, int nImageList)
{
    ASSERT(folder >= 0 && folder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(folder);
    CImageList * o = NULL;
    if (nImageList == fSmallIcon)
    {
        o = pbf->pSmallImageList;
        pbf->pSmallImageList = pImageList;
    }
    else if (nImageList == fLargeIcon)
    {
        o = pbf->pLargeImageList;
        pbf->pLargeImageList = pImageList;
    }
    return o;
}


CGfxOutBarCtrl::CBarItem::CBarItem(const TCHAR * name, const int image, DWORD exData)
{
    cItem = NULL;
    iImageIndex = image;
    dwData = exData;
    if (name)
    {
        cItem = new TCHAR[lstrlen(name)+1];
        ASSERT(cItem);
        lstrcpy(cItem, name);
    }
}

CGfxOutBarCtrl::CBarItem::~CBarItem()
{
    if (cItem) delete [] cItem;
}

int CGfxOutBarCtrl::CBarFolder::GetItemCount()
{
    return arItems.GetSize();
}

void CGfxOutBarCtrl::PaintItems(CDC * pDC, const int iFolder, CRect rc)
{
    ASSERT(iFolder >= 0 && iFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iFolder);
    int max = pbf->GetItemCount(), t;
    CRect irc;
    GetInsideRect(irc);
    int isdc = pDC->SaveDC();
    CRgn itemRegion;
    itemRegion.CreateRectRgnIndirect(&irc);
    pDC->SelectClipRgn(&itemRegion);
    itemRegion.DeleteObject();
    for (t = iFirstItem; t < max; t++)
    {
//      CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(t);
        CRect itemRc;
        GetItemRect(iFolder, t, itemRc);
        if (itemRc.top > rc.bottom) break;
        else DrawItem(pDC, iFolder, itemRc, t);
    }
    pDC->RestoreDC(isdc);
}


CSize CGfxOutBarCtrl::GetItemSize(const int iFolder, const int index, const int type)
{
    ASSERT(iFolder >= 0 && iFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iFolder);

    ASSERT(index >= 0 && index < pbf->GetItemCount());
    CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(index);

    CSize szImage(0,0);
    CSize szLabel(0,0);
    CSize szAll(0,0);

    if (pi->iImageIndex >= 0)
    {
        if (type != ircLabel)
        {
            CImageList * il = GetFolderImageList(iFolder, IsSmallIconView());
            ASSERT(il);
            if (il)
            {
                IMAGEINFO ii;
                il->GetImageInfo(pi->iImageIndex, &ii);
                szImage = CRect(ii.rcImage).Size();
            }
        }
    }
    if (pi->cItem)
    {
        if (type != ircIcon)
        {
            CClientDC dc(this);
            CFont * oft = (CFont *) dc.SelectObject(CFont::FromHandle((HFONT)GetStockObject(DEFAULT_GUI_FONT)));
            if (IsSmallIconView())
            {
                szLabel = dc.GetTextExtent(pi->cItem, lstrlen(pi->cItem));
            }
            else
            {
                CRect rc;
                GetInsideRect(rc);
                rc.bottom = rc.top;
                dc.DrawText(pi->cItem, lstrlen(pi->cItem), rc, DT_CALCRECT|DT_CENTER|DT_WORDBREAK);
                szLabel = rc.Size();
            }
            dc.SelectObject(oft);
        }
    }
    if (IsSmallIconView())
    {
        if (type == ircIcon) szAll = szImage;
        else if (type == ircLabel) szAll = szLabel;
        else if (type == ircAll) szAll = CSize(szImage.cx + szLabel.cx + xSmallIconLabelOffset, szImage.cy > szLabel.cy ? szImage.cy : szLabel.cy);
    }
    else
    {
        if (type == ircIcon) szAll = szImage;
        else if (type == ircLabel) szAll = szLabel;
        else if (type == ircAll)
        {
            szAll = CSize(szImage.cx > szLabel.cx ? szImage.cx : szLabel.cx, szLabel.cy + szImage.cy + yLargeIconLabelOffset + yLargeIconSpacing);
        }
    }
    return szAll;
}

CImageList * CGfxOutBarCtrl::GetFolderImageList(const int index, const bool bSmall) const
{
    ASSERT(index >= 0 && index < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(index);
    if (bSmall)
    {
        if (pbf->pSmallImageList) return pbf->pSmallImageList; else return pSmallImageList;
    }
    if (pbf->pLargeImageList) return pbf->pLargeImageList; else return pLargeImageList;
}

void CGfxOutBarCtrl::DrawItem(CDC * pDC, const int iFolder, CRect rc, const int index, const bool bOnlyImage)
{
    CImageList * ima = GetFolderImageList(iFolder, IsSmallIconView());

    ASSERT(iFolder >= 0 && iFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iFolder);

    ASSERT(index >= 0 && index < pbf->GetItemCount());
    CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(index);

    ASSERT(pi && ima);

    CFont * oft = (CFont *) pDC->SelectObject(CFont::FromHandle((HFONT)GetStockObject(DEFAULT_GUI_FONT)));
    COLORREF ocr = pDC->SetTextColor(crTextColor);
    int obk = pDC->SetBkMode(TRANSPARENT);

    if (IsSmallIconView())
    {
        if (ima)
        {
            IMAGEINFO ii;
            ima->GetImageInfo(pi->iImageIndex, &ii);
            CSize szImage = CRect(ii.rcImage).Size();
            CPoint pt;
            pt.x = rc.left + 2;
            pt.y = rc.top + (rc.Height() - szImage.cy) / 2;
            ima->Draw(pDC, pi->iImageIndex, pt, ILD_NORMAL);

            if (!bOnlyImage)
            {
                rc.left += szImage.cx + xSmallIconLabelOffset;
                pDC->TextOut(rc.left, rc.top, CString(pi->cItem));
            }

        }
    }
    else
    {
        if (ima)
        {
            IMAGEINFO ii;
            ima->GetImageInfo(pi->iImageIndex, &ii);
            CSize szImage = CRect(ii.rcImage).Size();
            CPoint pt;
            pt.x = rc.left + (rc.Width() - szImage.cx) / 2;
            pt.y = rc.top;// + (rc.Height() - szImage.cy) / 2;
            ima->Draw(pDC, pi->iImageIndex, pt, ILD_NORMAL);
            if (!bOnlyImage)
            {
                rc.top += szImage.cy + yLargeIconLabelOffset;
                pDC->DrawText(pi->cItem, lstrlen(pi->cItem), rc, DT_CENTER|DT_WORDBREAK);
            }
        }
    }

    if (dwFlags&fSelHighlight && iLastSel == index && iLastSel >= 0)
    {
        CRect rc;
        GetIconRect(iSelFolder, iLastSel, rc);
        rc.InflateRect(1,1);
        pDC->Draw3dRect(rc, crDkShadowBorder, cr3dFace);
    }

    pDC->SetTextColor(ocr);
    pDC->SelectObject(oft);
    pDC->SetBkMode(obk);

}


BOOL CGfxOutBarCtrl::OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message)
{
    DWORD pos = GetMessagePos();
    CPoint pt(MAKEPOINTS(pos).x, MAKEPOINTS(pos).y);
    ScreenToClient(&pt);
    int index, ht = HitTestEx(pt, index);
    if (ht == htFolder)
    {
        SetCursor(hHandCursor);
        return true;
    }

    return CDropTargetWnd::OnSetCursor(pWnd, nHitTest, message);
}

void CGfxOutBarCtrl::GetVisibleRange(const int iFolder, int & first, int & last)
{
    first = iFirstItem;
    CRect rc;
    GetInsideRect(rc);

    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iFolder);
    int max = pbf->GetItemCount(), t;
    for (t = iFirstItem; t < max; t++)
    {
//      CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(t);
        CRect itemRc;
        GetItemRect(iFolder, t, itemRc);
        if (itemRc.bottom > rc.bottom)
        {
            last = t - 1;
            break;
        }
        else last = t;
    }
}

void CGfxOutBarCtrl::OnSize(UINT nType, int cx, int cy)
{
    bUpArrow = bDownArrow = false;

    CDropTargetWnd::OnSize(nType, cx, cy);

    int t, max = GetFolderCount();
    CRect rc;
    GetInsideRect(rc);

    for (t = 0; t < max; t++)
    {
//      CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(t);
        CWnd * pc = GetFolderChild(t);
        if (pc) pc->SetWindowPos(0, rc.left, rc.top, rc.Width(), rc.Height(), SWP_NOZORDER);
    }
}

void CGfxOutBarCtrl::HighlightItem(const int index, const bool bPressed)
{
    CWnd * pf = GetFocus();
    if (pf != NULL && pf != this && IsChild(pf)) return;

    static bool bOldPressed = false;
    if (iLastItemHighlighted == index && bOldPressed == bPressed) return;

    bOldPressed = bPressed;

    CClientDC dc(this);

    CRect irc;
    GetInsideRect(irc);
    int isdc = dc.SaveDC();
    CRgn itemRegion;
    itemRegion.CreateRectRgnIndirect(&irc);
    dc.SelectClipRgn(&itemRegion);
    itemRegion.DeleteObject();


    if (iLastItemHighlighted >= 0 && IsValidItem(iLastItemHighlighted))
    {
        CRect rc;
        GetIconRect(iSelFolder, iLastItemHighlighted, rc);
        rc.InflateRect(1,1);
        dc.Draw3dRect(rc, crBackGroundColor, crBackGroundColor);
    }
    if (iSelAnimTiming > 0 && index == iLastSel)
    {
        iLastItemHighlighted = -1;
        return;
    }
    if (dwFlags&fSelHighlight && iLastSel == index)
    {
        iLastItemHighlighted = -1;
        return;
    }

    iLastItemHighlighted = index;
    if (iLastItemHighlighted >= 0 && IsValidItem(iLastItemHighlighted))
    {
        CRect rc;
        GetIconRect(iSelFolder, iLastItemHighlighted, rc);
        rc.InflateRect(1,1);
        if (bPressed)
            dc.Draw3dRect(rc, crDkShadowBorder, cr3dFace);
        else
            dc.Draw3dRect(rc, cr3dFace, crDkShadowBorder);
    }
    dc.RestoreDC(isdc);
}

void CGfxOutBarCtrl::GetIconRect(const int iFolder, const int iIndex, CRect & rect)
{
    CRect rc;
    GetInsideRect(rc);
    int top = rc.top;
    CSize sz(0,0);
    int y = 0;
    int spacing = IsSmallIconView() ? ySmallIconSpacing : yLargeIconSpacing;

    for (int t = 0; t < iIndex; t++)
    {
        sz = GetItemSize(iFolder, t, ircAll);
        top += sz.cy;
        top += spacing;
        if (t == iFirstItem - 1) y = top - rc.top;
    }
    top += spacing;

    sz = GetItemSize(iFolder, iIndex, ircIcon);
    if (IsSmallIconView())
    {
        rect.SetRect(rc.left, top, rc.left + sz.cx, top + sz.cy);
        rect.left += xLeftMargin + 2;
        rect.right += xLeftMargin + 2;
        rect.top -= yTopMargin;
        rect.bottom -= yTopMargin;

        rect.top -= y;
        rect.bottom -= y;
    }
    else
    {
        rect.SetRect(rc.left + (rc.Width() - sz.cx)/2, top, rc.left + (rc.Width() - sz.cx)/2 + sz.cx, top + sz.cy);
        rect.top -= y + 3;
        rect.bottom -= y + 2;
    }
}

void CGfxOutBarCtrl::GetLabelRect(const int iFolder, const int iIndex, CRect & rect)
{
    CRect rc;
    GetInsideRect(rc);
    int top = rc.top;
    CSize sz(0,0);
    int y = 0;
    int spacing = IsSmallIconView() ? ySmallIconSpacing : yLargeIconSpacing;

    for (int t = 0; t < iIndex; t++)
    {
        sz = GetItemSize(iFolder, t, ircAll);
        top += sz.cy;
        top += spacing;
        if (t == iFirstItem - 1) y = top - rc.top;
    }
    top += spacing;

    sz = GetItemSize(iFolder, iIndex, ircAll);

    CSize szLabel = GetItemSize(iFolder, iIndex, ircLabel);

    if (IsSmallIconView())
    {
        rect.SetRect(rc.left, top, rc.left + sz.cx, top + sz.cy);
        rect.left += xLeftMargin + 2;
        rect.right += xLeftMargin + 2;
        rect.top  += yTopMargin - 5;
        rect.bottom += yTopMargin - 5;
    }
    else
    {
        rect.SetRect(rc.left + (rc.Width() - sz.cx)/2, top, rc.left + (rc.Width() - sz.cx)/2 + sz.cx, top + sz.cy);
        rect.top -= y + 3;
        rect.bottom -= y + 2;

        rect.bottom -=  yLargeIconSpacing;
        rect.top    = rect.bottom - szLabel.cy;
    }
}


void CGfxOutBarCtrl::StartGroupEdit(const int index)
{
    CGfxGroupEdit * pEdit = new CGfxGroupEdit;
    pEdit->iIndex = index;
    CRect rc;
    GetFolderRect(index, rc);
    rc.InflateRect(-2,-2);
    pEdit->Create(WS_CHILD|WS_VISIBLE|ES_CENTER|ES_MULTILINE,rc,this,1);
    pEdit->msgSend = NM_OB_ONGROUPENDEDIT;
    pEdit->ModifyStyleEx(0, WS_EX_CLIENTEDGE, SWP_FRAMECHANGED);
    ASSERT(index >= 0 && index < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(index);
    pEdit->SetWindowText(pbf->cName);
    pEdit->SetFocus();
}

void CGfxOutBarCtrl::StartItemEdit(const int index)
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    ASSERT(index >= 0 && index < pbf->GetItemCount());
    CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(index);

    CGfxGroupEdit * pEdit = new CGfxGroupEdit;
    pEdit->iIndex = index;
    CRect rc, crc;
    GetLabelRect(iSelFolder, index, rc);
    GetInsideRect(crc);

    DWORD dwFlag = WS_CHILD|WS_VISIBLE|WS_BORDER;

    if (!IsSmallIconView())
    {
        rc.left = crc.left + 5;
        rc.right = crc.right - 5;
        rc.bottom += 5;
        dwFlag |= ES_CENTER|ES_MULTILINE;
    }
    else
    {
        CRect irc;
        GetIconRect(iSelFolder, index, irc);
        dwFlag |= ES_AUTOHSCROLL;
        rc.OffsetRect(irc.Width()+1, -6);
        pEdit->bNoDown = true;
    }

    pEdit->Create(dwFlag, rc, this, 1);
    pEdit->msgSend = NM_OB_ONLABELENDEDIT;

    pEdit->SetWindowText(pi->cItem);
    pEdit->SetFocus();

}

void CGfxOutBarCtrl::OnRButtonDown(UINT nFlags, CPoint point)
{
    iHitInternal1 = HitTestEx(point, iHitInternal2);

    CGfxPopupMenu cMenu;
    cMenu.CreatePopupMenu();

    cMenu.AppendMenu(MF_STRING, ID_GFX_SMALLICON, _ID_GFX_SMALLICON);
    cMenu.AppendMenu(MF_STRING, ID_GFX_LARGEICON, _ID_GFX_LARGEICON);

    if (iHitInternal1 == htItem)
    {
        if (dwFlags & fRemoveItems || dwFlags & fEditItems)
        {
            cMenu.AppendMenu(MF_SEPARATOR);
            if (dwFlags&fRemoveItems)
                cMenu.AppendMenu(MF_STRING, ID_GFX_REMOVEITEM, _ID_GFX_REMOVEITEM);

            if (dwFlags&fEditItems)
                cMenu.AppendMenu(MF_STRING, ID_GFX_RENAMEITEM, _ID_GFX_RENAMEITEM);
        }
    }
    else if (iHitInternal1 == htFolder)
    {
        if (dwFlags & fRemoveGroups || dwFlags & fEditGroups)
        {
            cMenu.AppendMenu(MF_SEPARATOR);

            if (dwFlags&fRemoveGroups)
                cMenu.AppendMenu(MF_STRING, ID_GFX_REMOVEITEM, _ID_GFX_REMOVEITEM);

            if (dwFlags&fEditGroups)
                cMenu.AppendMenu(MF_STRING, ID_GFX_RENAMEITEM, _ID_GFX_RENAMEITEM);
        }
    }

    cMenu.AppendMenu(MF_SEPARATOR);
    cMenu.AppendMenu(MF_STRING, ID_GFX_ANIMATION, _ID_GFX_ANIMATION);

    if (iSelAnimTiming > 0)
        cMenu.CheckMenuItem(ID_GFX_ANIMATION, MF_CHECKED | MF_BYCOMMAND);

    cMenu.LoadToolBarResource(IDR_GFXMENU_TOOLBAR);
    cMenu.RemapMenu(&cMenu);
    cMenu.EnableMenuItems(&cMenu, this);

    CPoint pt(point);
    ClientToScreen(&pt);
    cMenu.TrackPopupMenu(TPM_LEFTALIGN|TPM_RIGHTBUTTON, pt.x, pt.y, this);

    cMenu.DestroyMenu();

    CDropTargetWnd::OnRButtonDown(nFlags, point);
}

// sami@tolvanen.com
void CGfxOutBarCtrl::OnGfxAnimation()
{
    GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_ICONANIMATION, iSelAnimTiming);
}

void CGfxOutBarCtrl::OnGfxLargeicon()
{
    SetSmallIconView(false);
    Invalidate();
    GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_ICONVIEWCHANGE);
}

void CGfxOutBarCtrl::OnUpdateGfxLargeicon(CCmdUI* pCmdUI)
{
    pCmdUI->Enable(IsSmallIconView());
}

void CGfxOutBarCtrl::OnGfxSmallicon()
{
    SetSmallIconView(true);
    Invalidate();
    GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, NM_ICONVIEWCHANGE);
}

void CGfxOutBarCtrl::OnUpdateGfxSmallicon(CCmdUI* pCmdUI)
{
    pCmdUI->Enable(!IsSmallIconView());
}

void CGfxOutBarCtrl::OnGfxRemoveitem()
{
    if (iHitInternal1 == htFolder)
    {
        RemoveFolder(iHitInternal2);
    }
    else if (iHitInternal1 == htItem)
    {
        RemoveItem(iHitInternal2);
    }
}

void CGfxOutBarCtrl::OnUpdateGfxRemoveitem(CCmdUI* /*pCmdUI*/)
{

}

void CGfxOutBarCtrl::OnGfxRenameitem()
{
    if (iHitInternal1 == htFolder)
    {
        StartGroupEdit(iHitInternal2);
    }
    else if (iHitInternal1 == htItem)
    {
        StartItemEdit(iHitInternal2);
    }
}

void CGfxOutBarCtrl::OnUpdateGfxRenameitem(CCmdUI* /*pCmdUI*/)
{
}

void CGfxOutBarCtrl::RemoveItem(const int index)
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    ASSERT(index >= 0 && index < pbf->GetItemCount());
    if (IsValidItem(index))
    {
        CBarItem * i = (CBarItem *) pbf->arItems.GetAt(index);
        delete i;

        pbf->arItems.RemoveAt(index);
        CRect rc;
        GetInsideRect(rc);
        InvalidateRect(rc);
    }
}

bool CGfxOutBarCtrl::IsValidItem(const int index) const
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    return (index >= 0 && index < pbf->GetItemCount());
}

DWORD CGfxOutBarCtrl::GetItemData(const int index) const
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    if (IsValidItem(index))
    {
        CBarItem * i = (CBarItem *) pbf->arItems.GetAt(index);
        return i->dwData;
    }
    return 0;
}

int CGfxOutBarCtrl::GetItemImage(const int index) const
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    if (IsValidItem(index))
    {
        CBarItem * i = (CBarItem *) pbf->arItems.GetAt(index);
        return i->iImageIndex;
    }
    return 0;
}

void CGfxOutBarCtrl::SetItemData(const int index, const DWORD dwData)
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    if (IsValidItem(index))
    {
        CBarItem * i = (CBarItem *) pbf->arItems.GetAt(index);
        i->dwData = dwData;
    }
}

void CGfxOutBarCtrl::SetItemImage(const int index, const int iImage)
{
    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    if (IsValidItem(index))
    {
        CBarItem * i = (CBarItem *) pbf->arItems.GetAt(index);
        i->iImageIndex = iImage;
    }
}

void CGfxOutBarCtrl::DrawDragArrow(CDC * pDC, const int /*iFrom*/, const int iTo)
{
    if (iTo == iLastDragItemDraw) return;
    CRect rc1;
    if (iLastDragItemDraw >= 0)
    {
        GetDragItemRect(iLastDragItemDraw, rc1);
        if (iLastDragItemDrawType == 0) rc1.bottom = rc1.top + 5;
        else if (iLastDragItemDrawType == 1) { rc1.top -= 4; rc1.bottom = rc1.top + 9; }
        else if (iLastDragItemDrawType == 2) { rc1.top -= 4; rc1.bottom = rc1.top + 5; }
        InvalidateRect(rc1, true);
        UpdateWindow();
    }
    iLastDragItemDraw = iTo;

    if (iLastDragItemDraw >= 0)
    {
        iLastDragItemDrawType = GetDragItemRect(iLastDragItemDraw, rc1);

        CPen * op = pDC->SelectObject(pBlackPen);

        pDC->MoveTo(rc1.left, rc1.top);
        pDC->LineTo(rc1.right, rc1.top);

        if (iLastDragItemDrawType != 2)
        {
            pDC->MoveTo(rc1.left, rc1.top+1);
            pDC->LineTo(rc1.left + 7, rc1.top+1);
            pDC->MoveTo(rc1.left, rc1.top+2);
            pDC->LineTo(rc1.left + 5, rc1.top+2);
            pDC->MoveTo(rc1.left, rc1.top+3);
            pDC->LineTo(rc1.left + 3, rc1.top+3);
            pDC->MoveTo(rc1.left, rc1.top+4);
            pDC->LineTo(rc1.left + 1, rc1.top+4);

            pDC->MoveTo(rc1.right-1, rc1.top+1);
            pDC->LineTo(rc1.right - 7, rc1.top+1);
            pDC->MoveTo(rc1.right-1, rc1.top+2);
            pDC->LineTo(rc1.right - 5, rc1.top+2);
            pDC->MoveTo(rc1.right-1, rc1.top+3);
            pDC->LineTo(rc1.right - 3, rc1.top+3);
        }
        if (iLastDragItemDrawType != 0)
        {
            pDC->MoveTo(rc1.left, rc1.top-1);
            pDC->LineTo(rc1.left + 7, rc1.top-1);
            pDC->MoveTo(rc1.left, rc1.top-2);
            pDC->LineTo(rc1.left + 5, rc1.top-2);
            pDC->MoveTo(rc1.left, rc1.top-3);
            pDC->LineTo(rc1.left + 3, rc1.top-3);
            pDC->MoveTo(rc1.left, rc1.top-4);
            pDC->LineTo(rc1.left + 1, rc1.top-4);

            pDC->MoveTo(rc1.right-1, rc1.top-1);
            pDC->LineTo(rc1.right - 7, rc1.top-1);
            pDC->MoveTo(rc1.right-1, rc1.top-2);
            pDC->LineTo(rc1.right - 5, rc1.top-2);
            pDC->MoveTo(rc1.right-1, rc1.top-3);
            pDC->LineTo(rc1.right - 3, rc1.top-3);
            pDC->MoveTo(rc1.right-1, rc1.top-4);
            pDC->LineTo(rc1.right - 1, rc1.top-4);
        }

        pDC->SelectObject(op);
    }
}

int CGfxOutBarCtrl::GetDragItemRect(const int index, CRect & rect)
{
    CRect rc, crc;
    GetInsideRect(crc);
    crc.InflateRect(-2, 0);

    GetItemRect(iSelFolder, index < GetItemCount() ? index : index - 1, rc);
    int line = 0;
    if (index < GetItemCount())
    {
        line = rc.top - 4;
    }
    else
    {
        line = rc.bottom + 6;
    }
    int tpe;
    if (index == 0)
    {
        rect.SetRect(crc.left, line+2, crc.right, line + 7);
        tpe = 0;
    }
    else if (index < GetItemCount())
    {
        rect.SetRect(crc.left, line - 9, crc.right, line);
        tpe = 1;
        if (IsSmallIconView())
        {
            rect.top += 8;
            rect.bottom += 8;
        }
    }
    else
    {
        rect.SetRect(crc.left, line, crc.right, line + 5);
        tpe = 2;
    }
    return tpe;
}

void CGfxOutBarCtrl::AnimateFolderScroll(const int iFrom, const int iTo)
{
    // changes:
    //
    // 6.4.1999     sami@tolvanen.com:
    //              enhanced scrolling, fixed original bugs
    //
    // 3.7.1999     sami@tolvanen.com
    //              fixed small problem when scrolling down (too small bitmap)
    //              added comments...

    ASSERT(iFrom >= 0 && iFrom < GetFolderCount());
    ASSERT(iTo >= 0 && iTo < GetFolderCount());

    int     iFoldersToMove = (iTo > iFrom) ? (iTo - iFrom) : (iFrom - iTo);

    CRect   rc;     // the "inside" rect (control area minus folders)
    CRect   rc1;    // same as above, positioned in origo
    CRect   frc;    // folder rectangle
    CRect   frc1;   // same as above

    GetInsideRect(rc);
    rc1.SetRect(0, 0, rc.Width(), rc.Height());

    GetFolderRect(iTo, frc);
    frc1 = frc;

    CClientDC   dc(this);
    CDC         memDC;
    CBitmap     bmpFrom;    // image of the "from" or the  previous folder
    CBitmap     bmpTo;      // image of the "to" or the new folder

    memDC.CreateCompatibleDC(&dc);
    bmpFrom.CreateCompatibleBitmap(&dc, rc1.Width(),
                                   rc1.Height() + frc.Height() * (iFoldersToMove + 1));
    bmpTo.CreateCompatibleBitmap(&dc, rc1.Width(),
                                 rc1.Height() + frc.Height() * (iFoldersToMove + 1));

    CDC     *pDC = &memDC;

    CWnd    *pWnd1 = GetFolderChild(iFrom); // possible child window of "from" folder
    CWnd    *pWnd2 = GetFolderChild(iTo);   // possible child window of "to" folder

    CBitmap *obmp = pDC->SelectObject(&bmpFrom);

    if (iTo > iFrom)
    {
        // up

        pDC->SelectObject(&bmpTo);

        frc.SetRect(0, 0, frc.Width(), frc.Height());
        rc1.SetRect(0, frc.Height(), rc.Width(), rc.Height() + frc.Height() * iFoldersToMove);
        pDC->FillSolidRect(rc1, crBackGroundColor);

        if (pWnd2)
        {
            // if the new folder has a child window,
            // let it handle the painting

            CPoint  ovpt  = pDC->SetViewportOrg(0, frc.Height() * iFoldersToMove);
            BOOL    bPrev = pWnd2->ShowWindow(SW_SHOW);

            pWnd2->SendMessage(WM_ERASEBKGND, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);
            pWnd2->SendMessage(WM_PAINT, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);

            if (!bPrev)
                pWnd2->ShowWindow(SW_HIDE);

            pDC->SetViewportOrg(ovpt);
        }
        else
        {
            // otherwise, paint the folder contents
            PaintItems(pDC, iTo, rc1);
        }

        // draw the folders to the top
        for (int iTmp = iTo - iFoldersToMove + 1; iTmp <= (iFrom + iFoldersToMove); iTmp++)
        {
            DrawFolder(pDC, iTmp, frc, false);
            frc.OffsetRect(0, frc.Height());
        }
    }
    else
    {
        // down

        frc.SetRect(0, 0, frc.Width(), frc.Height());
        rc1.bottom += frc.Height() * (iFoldersToMove + 1);

        pDC->FillSolidRect(rc1, crBackGroundColor);

        if (pWnd1)
        {
            // if the previous folder has a child window,
            // let it handle the painting

            CPoint  ovpt  = pDC->SetViewportOrg(0, frc.Height() * iFoldersToMove);
            BOOL    bPrev = pWnd1->ShowWindow(SW_SHOW);

            pWnd1->SendMessage(WM_ERASEBKGND, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);
            pWnd1->SendMessage(WM_PAINT, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);

            if (!bPrev)
                pWnd1->ShowWindow(SW_HIDE);

            pDC->SetViewportOrg(ovpt);
        }
        else
        {
            // otherwise, paint the folder contents
            PaintItems(pDC, iFrom, rc1);
        }

        // paint the folders to the top of the "from" bitmap
        for (int iTmp = iFrom - iFoldersToMove + 1; iTmp <= (iTo + iFoldersToMove); iTmp++)
        {
            DrawFolder(pDC, iTmp, frc, false);
            frc.OffsetRect(0, frc.Height());
        }

        pDC->SelectObject(&bmpTo);

        pDC->FillSolidRect(rc1, crBackGroundColor);

        if (pWnd2)
        {
            // if the new folder has a child window,
            // let it handle the painting

            BOOL bPrev = pWnd2->ShowWindow(SW_SHOW);

            pWnd2->SendMessage(WM_ERASEBKGND, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);
            pWnd2->SendMessage(WM_PAINT, (WPARAM) pDC->GetSafeHdc(), (LPARAM) 0);

            if (!bPrev)
                pWnd2->ShowWindow(SW_HIDE);
        }
        else
        {
            // otherwise, paint the folder contents
            PaintItems(pDC, iTo, rc1);
        }
    }

    if (iTo > iFrom)
    {
        // up

        CRect rcFrom;
        CRect rcTo;

        GetFolderRect(iFrom, rcFrom);
        GetFolderRect(iTo, rcTo);

        int iSmooth = 1;
        int iFolderHeight = rcFrom.Height();
        int y = rcTo.top - iFolderHeight * (iFoldersToMove - 1);

        int iWidth = rc.Width();
        int iHeight = rc.bottom + iFolderHeight * iFoldersToMove;

        pDC->SelectObject(&bmpTo);

        for (; iSmooth < iFolderHeight && y > rcFrom.bottom; y -= iSmooth)
        {
            dc.BitBlt(rc.left, y, iWidth, iHeight - y, pDC, 0,0, SRCCOPY);
            Sleep(lAnimationTickCount);

            if (y % 2) iSmooth++;
        }

        for (; y > rcFrom.bottom; y -= iFolderHeight)
        {
            dc.BitBlt(rc.left, y, iWidth, iHeight - y, pDC, 0,0, SRCCOPY);
            Sleep(lAnimationTickCount);
        }
    }
    else
    {
        // i.e. down

        CRect rcFrom;
        CRect rcTo;
        CRect rcTo2;

        GetFolderRect(iFrom, rcFrom);
        GetFolderRect(iTo, rcTo2);

        rcTo2.OffsetRect(0, 1);
        rcTo.SetRect(rc.left, rc.bottom, rc.right, rc.bottom - iFolderHeight * iFoldersToMove);

        int iFolderHeight = rcFrom.Height();

        int iWidth = rc.Width();
        int iFolders = iFolderHeight * iFoldersToMove;
        int iSmooth = 1;
        int y = rcTo2.bottom;

        for (; (iSmooth < iFolderHeight) && (y < (rcTo.top - iFolders)); y += iSmooth)
        {
            pDC->SelectObject(&bmpTo);
            dc.BitBlt(rc.left, rcTo2.bottom, iWidth, y - rcTo2.bottom, pDC, 0,
                      rcTo2.bottom + iFolders, SRCCOPY);

            pDC->SelectObject(&bmpFrom);
            dc.BitBlt(rc.left, y, iWidth, rc.bottom - y, pDC, 0,0, SRCCOPY);

            Sleep(lAnimationTickCount);

            if (y % 2) iSmooth++;
        }

        for (; y < (rcTo.top - iFolders); y += iFolderHeight)
        {
            pDC->SelectObject(&bmpTo);
            dc.BitBlt(rc.left, rcTo2.bottom, iWidth, y - rcTo2.bottom, pDC, 0,
                      rcTo2.bottom + iFolders, SRCCOPY);

            pDC->SelectObject(&bmpFrom);
            dc.BitBlt(rc.left, y, iWidth, rcTo.top - y, pDC, 0,0, SRCCOPY);

            Sleep(lAnimationTickCount);
        }
    }

    pDC->SelectObject(obmp);
}


CString CGfxOutBarCtrl::GetItemText(const int index)
{
    CString item;

    ASSERT(iSelFolder >= 0 && iSelFolder < GetFolderCount());
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iSelFolder);
    if (IsValidItem(index))
    {
        CBarItem * pi = (CBarItem *) pbf->arItems.GetAt(index);
        if (pi->cItem) item = pi->cItem;
    }
    return item;
}

int CGfxOutBarCtrl::AddFolderBar(const TCHAR * pFolder, CWnd * pSon, const DWORD exData)
{
    CBarFolder * pbf = new CBarFolder(pFolder, exData);
    ASSERT(pbf);
    pbf->pChild = pSon;

    arFolder.Add((void *)pbf);

    return arFolder.GetSize() - 1;
}

CWnd * CGfxOutBarCtrl::GetFolderChild(int iFolder)
{
    if (GetFolderCount())
    {
        if (iFolder < 0) iFolder = iSelFolder;

        CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iFolder);
        return pbf->pChild;
    }
    return NULL;
}

DWORD CGfxOutBarCtrl::GetFolderData(int iFolder)
{
    if (iFolder < 0) iFolder = iSelFolder;
    CBarFolder * pbf = (CBarFolder *) arFolder.GetAt(iFolder);
    return pbf->dwData;
}

void CGfxOutBarCtrl::SetAnimSelHighlight(const int iTime)
{
    if (iTime <= 0) KillTimer(3);
    else SetTimer(3, iTime, NULL);
    iSelAnimTiming = iTime;
}

void CGfxOutBarCtrl::OnSysColorChange()
{
    CDropTargetWnd::OnSysColorChange();

    crBackGroundColor = GetSysColor(COLOR_3DSHADOW);
    crBackGroundColor1 = GetSysColor(COLOR_3DSHADOW);
    crTextColor = GetSysColor(COLOR_WINDOW);
    cr3dFace = GetSysColor(COLOR_3DFACE);
    crLightBorder = GetSysColor(COLOR_3DLIGHT);
    crHilightBorder = GetSysColor(COLOR_3DHILIGHT);
    crShadowBorder = GetSysColor(COLOR_3DSHADOW);
    crDkShadowBorder = GetSysColor(COLOR_3DDKSHADOW);
}

// for drag open support - sami@tolvanen.com
DROPEFFECT CGfxOutBarCtrl::OnDragEnter(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    TRACE("CGfxOutBarCtrl::OnDragEnter\n");

    m_bDragOver = TRUE;
    SetTimer(2, 700, NULL);

    return CDropTargetWnd::OnDragEnter(pDataObject, dwKeyState, point);
}

// for drag open support - sami@tolvanen.com
void CGfxOutBarCtrl::OnDragLeave()
{
    TRACE("CGfxOutBarCtrl::OnDragLeave\n");

    m_bDragOver = FALSE;
    KillTimer(2);

    CDropTargetWnd::OnDragLeave();
}