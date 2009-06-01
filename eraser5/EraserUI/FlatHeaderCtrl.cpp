////////////////////////////////////////////////////////////////////////////
//  File:       CFlatHeaderCtrl.cpp
//  Version:    1.0.1.0
//
//  Author:     Maarten Hoeben
//  E-mail:     hoeben@nwn.com
//
//  Implementation of the CFlatHeaderCtrl and associated classes.
//
//  You are free to use, distribute or modify this code
//  as long as the header is not removed or modified.
//
//  Version history
//
//  1.0.0.1 - Initial release
//  1.0.1.0 - Fixed FHDragWnd destroy warning (thanks Philippe Terrier)
//          - Fixed double sent HDN_ITEMCLICK
//          - Added a property that adjusts for ListCtrls that use a static
//            border for flat look.
//  ?       - Fixed some problems with resizing (sami@tolvanen.com). See below.
//
////////////////////////////////////////////////////////////////////////////


// FlatHeaderCtrl.cpp : implementation file
//

#include "stdafx.h"
#include "FlatHeaderCtrl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CFHDragWnd

CFHDragWnd::CFHDragWnd()
{
    // Register the window class if it has not already been registered.
    WNDCLASS wndclass;
    HINSTANCE hInst = AfxGetInstanceHandle();

    if (!(::GetClassInfo(hInst, FHDRAGWND_CLASSNAME, &wndclass)))
    {
        // otherwise we need to register a new class
        wndclass.style          = CS_SAVEBITS ;
        wndclass.lpfnWndProc    = ::DefWindowProc;
        wndclass.cbClsExtra     = wndclass.cbWndExtra = 0;
        wndclass.hInstance      = hInst;
        wndclass.hIcon          = NULL;
        wndclass.hCursor        = LoadCursor(hInst, IDC_ARROW);
        wndclass.hbrBackground  = (HBRUSH)(COLOR_3DFACE + 1);
        wndclass.lpszMenuName   = NULL;
        wndclass.lpszClassName  = FHDRAGWND_CLASSNAME;

        if (!AfxRegisterClass(&wndclass))
            AfxThrowResourceException();
    }
}

CFHDragWnd::~CFHDragWnd()
{
}


BEGIN_MESSAGE_MAP(CFHDragWnd, CWnd)
    //{{AFX_MSG_MAP(CFHDragWnd)
    ON_WM_PAINT()
    ON_WM_ERASEBKGND()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// CFHDragWnd message handlers

BOOL CFHDragWnd::Create(CRect rect, CFlatHeaderCtrl* pFlatHeaderCtrl, INT iItem)
{
    ASSERT_VALID(pFlatHeaderCtrl);
    ASSERT(pFlatHeaderCtrl->IsKindOf(RUNTIME_CLASS(CFlatHeaderCtrl)));

    m_pFlatHeaderCtrl = pFlatHeaderCtrl;
    m_iItem = iItem;

    DWORD dwStyle = WS_POPUP|WS_DISABLED;
    DWORD dwExStyle = WS_EX_TOPMOST|WS_EX_TOOLWINDOW ;

    return CreateEx(dwExStyle, FHDRAGWND_CLASSNAME, NULL, dwStyle,
                    rect.left, rect.top, rect.Width(), rect.Height(),
                    NULL, NULL, NULL);
}


void CFHDragWnd::OnPaint()
{
    CPaintDC dc(this);

    /*if (m_pFlatHeaderCtrl->m_bNoFlicker)
    {*/
        CMemDC MemDC(&dc);
        OnDraw(&MemDC);
    /*}
    else
        OnDraw(&dc);*/
}

BOOL CFHDragWnd::OnEraseBkgnd(CDC* /*pDC*/)
{
    return TRUE;
}

void CFHDragWnd::OnDraw(CDC* pDC)
{
    CRect rect;
    GetClientRect(rect);

    pDC->FillSolidRect(rect, m_pFlatHeaderCtrl->m_cr3DFace);
    pDC->Draw3dRect(rect, m_pFlatHeaderCtrl->m_cr3DHighLight, m_pFlatHeaderCtrl->m_cr3DShadow);

    CPen* pPen = pDC->GetCurrentPen();
    CFont* pFont = pDC->SelectObject(m_pFlatHeaderCtrl->GetFont());

    pDC->SetBkColor(m_pFlatHeaderCtrl->m_cr3DFace);
    pDC->SetTextColor(m_pFlatHeaderCtrl->m_crText);

    rect.DeflateRect(m_pFlatHeaderCtrl->m_iSpacing, 0);
    m_pFlatHeaderCtrl->DrawItem(pDC, rect,
                                m_pFlatHeaderCtrl->m_hditemHotItem,
                                (m_pFlatHeaderCtrl->m_iSortColumn == m_iItem),
                                m_pFlatHeaderCtrl->m_bSortAscending);

    pDC->SelectObject(pFont);
    pDC->SelectObject(pPen);
}

void CFHDragWnd::PostNcDestroy()
{
    CWnd::PostNcDestroy();
    delete this;
}

/////////////////////////////////////////////////////////////////////////////
// CFlatHeaderCtrl

IMPLEMENT_DYNCREATE(CFlatHeaderCtrl, CHeaderCtrl)

CFlatHeaderCtrl::CFlatHeaderCtrl()
{
//  m_bNoFlicker        = TRUE;
    m_iSpacing          = 6;
    m_sizeArrow.cx      = 8;
    m_sizeArrow.cy      = 8;
    m_bStaticBorder     = FALSE;

    m_iHotIndex         = -1;
    m_bHotItemResizable = TRUE;

    m_bResizing         = FALSE;

    m_iHotDivider       = -1;
    m_crHotDivider      = 0x000000FF;

    m_bDragging         = FALSE;
    m_pDragWnd          = NULL;

    m_nClickFlags       = 0;

    m_bSortAscending    = FALSE;
    m_iSortColumn       = -1;
    m_arrayHdrItemEx.SetSize(0, 8);

    m_cr3DHighLight     = ::GetSysColor(COLOR_3DHIGHLIGHT);
    m_cr3DShadow        = ::GetSysColor(COLOR_3DSHADOW);
    m_cr3DFace          = ::GetSysColor(COLOR_3DFACE);
    m_crText            = ::GetSysColor(COLOR_BTNTEXT);
}

CFlatHeaderCtrl::~CFlatHeaderCtrl()
{
    if (m_pDragWnd != NULL)
    {
        m_pDragWnd->DestroyWindow();
        m_pDragWnd = NULL;
    }
}
#if _MFC_VER == 0x0800 
//Hack for VS beta version
#undef ON_WM_NCHITTEST
#define ON_WM_NCHITTEST() \
{ WM_NCHITTEST, 0, 0, 0, AfxSig_l_p, \
	(AFX_PMSG)(AFX_PMSGW) \
	(static_cast< UINT (AFX_MSG_CALL CWnd::*)(CPoint) > (&ThisClass :: OnNcHitTest)) },
#endif
BEGIN_MESSAGE_MAP(CFlatHeaderCtrl, CHeaderCtrl)
    //{{AFX_MSG_MAP(CFlatHeaderCtrl)
    ON_MESSAGE(HDM_INSERTITEMA, OnInsertItem)
    ON_MESSAGE(HDM_INSERTITEMW, OnInsertItem)
    ON_MESSAGE(HDM_DELETEITEM, OnDeleteItem)
    ON_MESSAGE(HDM_SETHOTDIVIDER, OnSetHotDivider)
    ON_MESSAGE(HDM_LAYOUT, OnLayout)
    ON_WM_NCHITTEST()
    ON_WM_SETCURSOR()
    ON_WM_LBUTTONDOWN()
    ON_WM_LBUTTONDBLCLK()
    ON_WM_PAINT()
    ON_WM_SYSCOLORCHANGE()
    ON_WM_ERASEBKGND()
    ON_WM_LBUTTONUP()
    ON_WM_MOUSEMOVE()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CFlatHeaderCtrl attributes

BOOL CFlatHeaderCtrl::ModifyProperty(WPARAM wParam, LPARAM lParam)
{
    switch (wParam)
    {
    case FH_PROPERTY_SPACING:
        m_iSpacing = (INT)lParam;
        break;
    case FH_PROPERTY_ARROW:
        m_sizeArrow.cx = LOWORD(lParam);
        m_sizeArrow.cy = HIWORD(lParam);
        break;
    case FH_PROPERTY_STATICBORDER:
        m_bStaticBorder = (BOOL)lParam;
        break;
    default:
        return FALSE;
    }

    Invalidate();
    return TRUE;
}

BOOL CFlatHeaderCtrl::GetItemEx(INT iPos, HDITEMEX* phditemex) const
{
    if (iPos >= m_arrayHdrItemEx.GetSize())
        return FALSE;

    *phditemex = m_arrayHdrItemEx[iPos];
    return TRUE;
}

BOOL CFlatHeaderCtrl::SetItemEx(INT iPos, HDITEMEX* phditemex)
{
    if (iPos >= m_arrayHdrItemEx.GetSize())
        return FALSE;

    if (phditemex->m_iMinWidth <= phditemex->m_iMaxWidth)
    {
        HDITEM hditem;
        hditem.mask = HDI_WIDTH;

        if (!GetItem(iPos, &hditem))
            return FALSE;

        if (hditem.cxy < phditemex->m_iMinWidth)
            hditem.cxy = phditemex->m_iMinWidth;

        if (hditem.cxy > phditemex->m_iMaxWidth)
            hditem.cxy = phditemex->m_iMaxWidth;

        SetItem(iPos, &hditem);
    }

    m_arrayHdrItemEx.SetAt(iPos, *phditemex);

    return TRUE;
}

void CFlatHeaderCtrl::SetSortColumn(INT iPos, BOOL bSortAscending)
{
    ASSERT(iPos < GetItemCount());

    m_bSortAscending = bSortAscending;
    m_iSortColumn = iPos;

    Invalidate();
}

INT CFlatHeaderCtrl::GetSortColumn(BOOL* pbSortAscending /*= NULL*/)
{
    if (pbSortAscending)
        *pbSortAscending = m_bSortAscending;

    return m_iSortColumn;
}

/////////////////////////////////////////////////////////////////////////////
// CFlatHeaderCtrl implementation

void CFlatHeaderCtrl::DrawCtrl(CDC* pDC)
{
    CRect rectClip;

    if (pDC->GetClipBox(&rectClip) == ERROR)
        return;

    CRect rectClient, rectItem;
    GetClientRect(&rectClient);

    pDC->FillSolidRect(rectClip, m_cr3DFace);

    INT iItems = GetItemCount();
    ASSERT(iItems >= 0);

    CPen penHighLight(PS_SOLID, 1, m_cr3DHighLight);
    CPen penShadow(PS_SOLID, 1, m_cr3DShadow);
    CPen* pPen = pDC->GetCurrentPen();

    CFont* pFont = pDC->SelectObject(GetFont());

    pDC->SetBkColor(m_cr3DFace);
    pDC->SetTextColor(m_crText);

    INT iWidth = 0;

    for (INT i = 0; i < iItems; i++)
    {
        INT iItem = OrderToIndex(i);

        TCHAR szText[FLATHEADER_TEXT_MAX];

        HDITEM hditem;
        hditem.mask         = HDI_WIDTH | HDI_FORMAT | HDI_TEXT | HDI_IMAGE | HDI_BITMAP;
        hditem.pszText      = szText;
        hditem.cchTextMax   = sizeof(szText);

        VERIFY(GetItem(iItem, &hditem));
        VERIFY(GetItemRect(iItem, rectItem));

        if (rectItem.right >= rectClip.left || rectItem.left <= rectClip.right)
        {
            if (hditem.fmt & HDF_OWNERDRAW)
            {
                DRAWITEMSTRUCT disItem;

                disItem.CtlType     = ODT_BUTTON;
                disItem.CtlID       = GetDlgCtrlID();
                disItem.itemID      = iItem;
                disItem.itemAction  = ODA_DRAWENTIRE;
                disItem.itemState   = 0;
                disItem.hwndItem    = m_hWnd;
                disItem.hDC         = pDC->m_hDC;
                disItem.rcItem      = rectItem;
                disItem.itemData    = 0;

                DrawItem(&disItem);
            }
            else
            {
                rectItem.DeflateRect(m_iSpacing, 0);
                DrawItem(pDC, rectItem, hditem, (iItem == m_iSortColumn), m_bSortAscending);
                rectItem.InflateRect(m_iSpacing, 0);

                if (m_nClickFlags & MK_LBUTTON &&
                    m_iHotIndex == iItem &&
                    m_hdhtiHotItem.flags & HHT_ONHEADER)
                {
                    pDC->InvertRect(rectItem);
                }
            }

            if (i < iItems-1)
            {
                pDC->SelectObject(&penShadow);
                pDC->MoveTo(rectItem.right - 1, rectItem.top + 2);
                pDC->LineTo(rectItem.right - 1, rectItem.bottom - 2);

                pDC->SelectObject(&penHighLight);
                pDC->MoveTo(rectItem.right, rectItem.top + 2);
                pDC->LineTo(rectItem.right, rectItem.bottom - 2);
            }
        }

        iWidth += hditem.cxy;
    }

    if (iWidth > 0)
    {
        rectClient.right = rectClient.left + iWidth + 1;
        pDC->Draw3dRect(rectClient, m_cr3DHighLight, m_cr3DShadow);
    }

    if (m_iHotDivider >= 0)
    {
        INT iOffset;

        if (m_iHotDivider < iItems)
        {
            GetItemRect(OrderToIndex(m_iHotDivider), rectItem);
            iOffset = rectItem.left - 1;
        }
        else
        {
            GetItemRect(OrderToIndex(iItems - 1), rectItem);
            iOffset = rectItem.right;
        }

        CPoint points[3];

        CPen penDivider(PS_SOLID, 1, m_crHotDivider);
        pDC->SelectObject(&penDivider);

        CBrush brushDivider(m_crHotDivider);
        pDC->SelectObject(&brushDivider);

        points[0] = CPoint(iOffset - 4, rectClient.bottom - 1);
        points[1] = CPoint(iOffset, rectClient.bottom - 5);
        points[2] = CPoint(iOffset + 4, rectClient.bottom - 1);

        pDC->Polygon(points, 3);

        points[0] = CPoint(iOffset - 4, 0);
        points[1] = CPoint(iOffset, 4);
        points[2] = CPoint(iOffset + 4, 0);

        pDC->Polygon(points, 3);
    }


    pDC->SelectObject(pFont);
    pDC->SelectObject(pPen);
}

void CFlatHeaderCtrl::DrawItem(LPDRAWITEMSTRUCT)
{
    ASSERT(FALSE);  // must override for self draw header controls
}

void CFlatHeaderCtrl::DrawItem(CDC* pDC, CRect rect, HDITEM hditem, BOOL bSort, BOOL bSortAscending)
{
    ASSERT(hditem.mask & HDI_FORMAT);

    INT iWidth = 0;

    CBitmap* pBitmap = NULL;
    BITMAP BitmapInfo;

    if (hditem.fmt & HDF_BITMAP)
    {
        ASSERT(hditem.mask & HDI_BITMAP);
        ASSERT(hditem.hbm);

        pBitmap = CBitmap::FromHandle(hditem.hbm);

        if (pBitmap)
            VERIFY(pBitmap->GetObject(sizeof(BITMAP), &BitmapInfo));
    }

    switch (hditem.fmt & HDF_JUSTIFYMASK)
    {
    case HDF_LEFT:
        iWidth = DrawImage(pDC, rect, hditem, FALSE);

        if (iWidth)
            rect.left += (iWidth + m_iSpacing);

//        if (hditem.fmt & HDF_IMAGE && !iWidth)
//            break;

        if (bSort)
            rect.right -= m_iSpacing + m_sizeArrow.cx;

        iWidth = DrawText(pDC, rect, hditem);

        if (iWidth)
            rect.left += (iWidth + m_iSpacing);

        if (bSort)
        {
            rect.right += m_iSpacing + m_sizeArrow.cx;
            rect.left += DrawArrow(pDC, rect, bSortAscending, FALSE) + m_iSpacing;
        }

        DrawBitmap(pDC, rect, hditem, pBitmap, &BitmapInfo, TRUE);
        break;
    case HDF_CENTER:
        iWidth = DrawImage(pDC, rect, hditem, FALSE);

        if (iWidth)
            rect.left += (iWidth + m_iSpacing);

//        if (hditem.fmt & HDF_IMAGE && !iWidth)
//            break;

        if (bSort)
            rect.left += (m_iSpacing + m_sizeArrow.cx);

        iWidth = DrawBitmap(pDC, rect, hditem, pBitmap, &BitmapInfo, TRUE);

        if (iWidth)
            rect.right -= (iWidth + m_iSpacing);

        if (bSort)
        {
            rect.left -= (m_iSpacing + m_sizeArrow.cx);
            rect.right -= (DrawArrow(pDC, rect, bSortAscending, TRUE) + (2 * m_iSpacing));
        }

        DrawText(pDC, rect, hditem);
        break;
    case HDF_RIGHT:
        if (!(hditem.fmt & HDF_BITMAP_ON_RIGHT))
        {
            iWidth = DrawBitmap(pDC, rect, hditem, pBitmap, &BitmapInfo, FALSE);

            if (iWidth)
                rect.left += (iWidth + m_iSpacing);
        }

        iWidth = DrawImage(pDC, rect, hditem, FALSE);

        if (iWidth)
            rect.left += (iWidth + m_iSpacing);

//        if (hditem.fmt & HDF_IMAGE && !iWidth)
//            break;

        if (bSort && hditem.fmt & HDF_BITMAP_ON_RIGHT)
            rect.left += (m_iSpacing + m_sizeArrow.cx);

        if (hditem.fmt & HDF_BITMAP_ON_RIGHT)
        {
            iWidth = DrawBitmap(pDC, rect, hditem, pBitmap, &BitmapInfo, TRUE);

            if (iWidth)
                rect.right -= (iWidth + m_iSpacing);
        }

        if (bSort)
        {
            if (hditem.fmt & HDF_BITMAP_ON_RIGHT)
                rect.left -= (m_iSpacing + m_sizeArrow.cx);

            rect.right -= (DrawArrow(pDC, rect, bSortAscending, TRUE) + (2 * m_iSpacing));
        }

        DrawText(pDC, rect, hditem);
        break;
    }
}

INT CFlatHeaderCtrl::DrawImage(CDC* pDC, CRect rect, HDITEM hditem, BOOL bRight)
{
    CImageList* pImageList = GetImageList();
    INT iWidth = 0;

    if (hditem.fmt & HDF_IMAGE)
    {
        ASSERT(hditem.mask & HDI_IMAGE);
        ASSERT(pImageList);
        ASSERT(hditem.iImage >= 0 && hditem.iImage < pImageList->GetImageCount());

        IMAGEINFO info;

        if (pImageList->GetImageInfo(hditem.iImage, &info))
        {
            iWidth = info.rcImage.right - info.rcImage.left;

            if (iWidth <= rect.Width() && rect.Width() > 0)
            {
                POINT point;
                point.y = rect.CenterPoint().y - ((info.rcImage.bottom - info.rcImage.top) >> 1);

                if (bRight)
                    point.x = rect.right - iWidth;
                else
                    point.x = rect.left;

                pImageList->Draw(pDC, hditem.iImage, point, ILD_NORMAL);
            }
            else
                iWidth = 0;
        }
    }

    return iWidth;
}

INT CFlatHeaderCtrl::DrawBitmap(CDC* pDC, CRect rect, HDITEM /*hditem*/, CBitmap* pBitmap, BITMAP* pBitmapInfo, BOOL bRight)
{
    INT iWidth = 0;

    if (pBitmap)
    {
        iWidth = pBitmapInfo->bmWidth;

        if (iWidth <= rect.Width() && rect.Width() > 0)
        {
            POINT point;
            point.y = rect.CenterPoint().y - (pBitmapInfo->bmHeight >> 1);

            if (bRight)
                point.x = rect.right - iWidth;
            else
                point.x = rect.left;

            CDC dc;

            if (dc.CreateCompatibleDC(pDC))
            {
                VERIFY(dc.SelectObject(pBitmap));

                if (!pDC->BitBlt(point.x, point.y,
                                 pBitmapInfo->bmWidth,
                                 pBitmapInfo->bmHeight,
                                 &dc, 0, 0, SRCCOPY))
                {
                    iWidth = 0;
                }
            }
            else
                iWidth = 0;
        }
        else
            iWidth = 0;
    }

    return iWidth;
}

INT CFlatHeaderCtrl::DrawText(CDC* pDC, CRect rect, HDITEM hditem)
{
    CSize size;

    if (rect.Width() > 0 && hditem.mask & HDI_TEXT && hditem.fmt & HDF_STRING)
    {
        size = pDC->GetTextExtent(hditem.pszText);

        switch (hditem.fmt & HDF_JUSTIFYMASK)
        {
        case HDF_LEFT:
        case HDF_LEFT | HDF_RTLREADING:
            pDC->DrawText(hditem.pszText, -1, rect, DT_LEFT | DT_END_ELLIPSIS |
                          DT_SINGLELINE | DT_VCENTER);
            break;
        case HDF_CENTER:
        case HDF_CENTER | HDF_RTLREADING:
            pDC->DrawText(hditem.pszText, -1, rect, DT_CENTER | DT_END_ELLIPSIS |
                          DT_SINGLELINE | DT_VCENTER);
            break;
        case HDF_RIGHT:
        case HDF_RIGHT | HDF_RTLREADING:
            pDC->DrawText(hditem.pszText, -1, rect, DT_RIGHT | DT_END_ELLIPSIS |
                          DT_SINGLELINE | DT_VCENTER);
            break;
        }
    }

    size.cx = (rect.Width() > size.cx) ? size.cx : rect.Width();

    return ((size.cx > 0) ? size.cx : 0);
}

INT CFlatHeaderCtrl::DrawArrow(CDC* pDC, CRect rect, BOOL bSortAscending, BOOL bRight)
{
    INT iWidth = 0;

    if (rect.Width() > 0 && m_sizeArrow.cx <= rect.Width())
    {
        iWidth = m_sizeArrow.cx;

        rect.top += (rect.Height() - m_sizeArrow.cy - 1) >> 1;
        rect.bottom = rect.top + m_sizeArrow.cy - 1;

        rect.left = bRight ? (rect.right - m_sizeArrow.cy) : rect.left;

        // Set up pens to use for drawing the triangle
        CPen penLight(PS_SOLID, 1, m_cr3DHighLight);
        CPen penShadow(PS_SOLID, 1, m_cr3DShadow);
        CPen *pPen = pDC->SelectObject(&penLight);

        if (bSortAscending)
        {
            // Draw triangle pointing upwards
            pDC->MoveTo(rect.left + ((m_sizeArrow.cx - 1) >> 1) + 1, rect.top);
            pDC->LineTo(rect.left +  (m_sizeArrow.cx - 1),           rect.top + m_sizeArrow.cy - 1);
            pDC->LineTo(rect.left,                                   rect.top + m_sizeArrow.cy - 1);

            pDC->SelectObject(&penShadow);
            pDC->MoveTo(rect.left + ((m_sizeArrow.cx - 1) >> 1),     rect.top);
            pDC->LineTo(rect.left,                                   rect.top + m_sizeArrow.cy - 1);
        }
        else
        {
            // Draw triangle pointing downwards
            pDC->MoveTo(rect.left + ((m_sizeArrow.cx - 1) >> 1) + 1, rect.top + m_sizeArrow.cy - 1);
            pDC->LineTo(rect.left +  (m_sizeArrow.cx - 1),           rect.top);

            pDC->SelectObject(&penShadow);
            pDC->MoveTo(rect.left + ((m_sizeArrow.cx - 1) >> 1),     rect.top + m_sizeArrow.cy - 1);
            pDC->LineTo(rect.left,                                   rect.top);
            pDC->LineTo(rect.left + m_sizeArrow.cx,                  rect.top);
        }

        // Restore the pen
        pDC->SelectObject(pPen);
    }

    return iWidth;
}

/////////////////////////////////////////////////////////////////////////////
// CHeaderCtrl message handlers

LRESULT CFlatHeaderCtrl::OnInsertItem(WPARAM wParam, LPARAM /*lParam*/)
{
    HDITEMEX hditemex;

    hditemex.m_iMinWidth = 0;
    hditemex.m_iMaxWidth = -1;

    ASSERT((INT)wParam <= m_arrayHdrItemEx.GetSize());
    m_arrayHdrItemEx.InsertAt(wParam, hditemex);

    return Default();
}

LRESULT CFlatHeaderCtrl::OnDeleteItem(WPARAM wParam, LPARAM /*lParam*/)
{
    ASSERT((INT)wParam < m_arrayHdrItemEx.GetSize());
    m_arrayHdrItemEx.RemoveAt(wParam);

    return Default();
}

LRESULT CFlatHeaderCtrl::OnSetHotDivider(WPARAM wParam, LPARAM lParam)
{
    if (wParam)
    {
        HDHITTESTINFO hdhti;

        hdhti.pt.x = LOWORD(lParam);
        hdhti.pt.y = HIWORD(lParam);
        ScreenToClient(&hdhti.pt);

        m_iHotDivider = SendMessage(HDM_HITTEST, 0, (LPARAM)(&hdhti));

        if (m_iHotDivider >= 0)
        {
            CRect rectItem;
            VERIFY(GetItemRect(m_iHotDivider, rectItem));

            if (hdhti.pt.x > rectItem.CenterPoint().x)
                m_iHotDivider++;
        }
    }
    else
        m_iHotDivider = (INT)lParam;

    Invalidate();

    return (LRESULT)m_iHotDivider;
}

LRESULT CFlatHeaderCtrl::OnLayout(WPARAM /*wParam*/, LPARAM lParam)
{
    LPHDLAYOUT lphdlayout = (LPHDLAYOUT)lParam;

    if (m_bStaticBorder)
        lphdlayout->prc->right += (GetSystemMetrics(SM_CXBORDER) * 2);

    return CHeaderCtrl::DefWindowProc(HDM_LAYOUT, 0, lParam);
}

/////////////////////////////////////////////////////////////////////////////
// CFlatHeaderCtrl message handlers

BOOL CFlatHeaderCtrl::OnEraseBkgnd(CDC* /*pDC*/)
{
    return TRUE;
}

void CFlatHeaderCtrl::OnPaint()
{
    CPaintDC dc(this);

    /*if (m_bNoFlicker)
    {*/
        CMemDC MemDC(&dc);
        DrawCtrl(&MemDC);
    /*}
    else
        DrawCtrl(&dc);*/
}

#if _MFC_VER <= 0x800
UINT
#else
LRESULT
#endif
CFlatHeaderCtrl::OnNcHitTest(CPoint point)
{
    m_hdhtiHotItem.pt = point;
    ScreenToClient(&m_hdhtiHotItem.pt);

    m_iHotIndex = (int)SendMessage(HDM_HITTEST, 0, (LPARAM)(&m_hdhtiHotItem));

    if (m_iHotIndex >= 0)
    {
        HDITEM hditem;
        HDITEMEX hditemex;

        hditem.mask = HDI_ORDER;
        VERIFY(GetItem(m_iHotIndex, &hditem));

        m_iHotOrder = hditem.iOrder;

        if (GetItemEx(m_iHotIndex, &hditemex))
            m_bHotItemResizable = (hditemex.m_iMinWidth != hditemex.m_iMaxWidth);
    }

    return (UINT)CHeaderCtrl::OnNcHitTest(point);
}


BOOL CFlatHeaderCtrl::OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message)
{
    if (m_iHotIndex >= 0 &&
        m_hdhtiHotItem.flags & (HHT_ONDIVIDER | HHT_ONDIVOPEN) &&
        !m_bHotItemResizable)
    {
        SetCursor(AfxGetApp()->LoadStandardCursor(IDC_ARROW));
        return TRUE;
    }

    return CHeaderCtrl::OnSetCursor(pWnd, nHitTest, message);
}

void CFlatHeaderCtrl::OnLButtonDown(UINT nFlags, CPoint point)
{
    m_nClickFlags = nFlags;
    m_ptClickPoint = point;

    if (m_iHotIndex >= 0)
    {
        m_hditemHotItem.mask        = HDI_WIDTH | HDI_FORMAT | HDI_TEXT | HDI_IMAGE |
                                      HDI_BITMAP | HDI_ORDER;
        m_hditemHotItem.pszText     = m_szHotItemText;
        m_hditemHotItem.cchTextMax  = sizeof(m_szHotItemText);

        VERIFY(GetItem(m_iHotIndex, &m_hditemHotItem));

        if (m_hdhtiHotItem.flags & HHT_ONHEADER)
        {
            RECT rectItem;
            VERIFY(GetItemRect(m_iHotIndex, &rectItem));
            InvalidateRect(&rectItem);
        }

        if (m_hdhtiHotItem.flags & (HHT_ONDIVIDER | HHT_ONDIVOPEN))
        {
            if (!m_bHotItemResizable)
                return;

            HDITEMEX hditemex;

            if (GetItemEx(m_iHotIndex, &hditemex))
            {
                CRect rectItem;
                GetItemRect(m_iHotIndex, rectItem);
                ClientToScreen(rectItem);

                // sami@tolvanen.com
                if (hditemex.m_iMinWidth > 0 ||
                   (hditemex.m_iMaxWidth > 0 &&
                    hditemex.m_iMinWidth <= hditemex.m_iMaxWidth))
                {
                    CRect rectClip;
                    GetClipCursor(rectClip);

                    POINT point;
                    GetCursorPos(&point);

                    INT iOffset = point.x - rectItem.right;

                    if (hditemex.m_iMinWidth > 0)
                        rectClip.left = rectItem.left + hditemex.m_iMinWidth + iOffset;

                    if (hditemex.m_iMaxWidth > 0)
                        rectClip.right = rectItem.left + hditemex.m_iMaxWidth + iOffset;

                    ClipCursor(rectClip);
                }
            }

            m_bResizing = TRUE;
        }
    }

    CHeaderCtrl::OnLButtonDown(nFlags, point);
}


void CFlatHeaderCtrl::OnLButtonDblClk(UINT nFlags, CPoint point)
{
    if (m_iHotIndex >= 0 &&
        m_hdhtiHotItem.flags & (HHT_ONDIVIDER | HHT_ONDIVOPEN) &&
        !m_bHotItemResizable)
    {
        return;
    }

    CHeaderCtrl::OnLButtonDblClk(nFlags, point);
}

void CFlatHeaderCtrl::OnLButtonUp(UINT nFlags, CPoint point)
{
    m_nClickFlags = nFlags;
    m_ptClickPoint = point;

    if (m_iHotIndex >= 0)
    {
        CWnd* pWnd = GetParent();

        // sami@tolvanen.com - uncommenting these lines may result
        // in cursor clipbox not to be set NULL after resizing...

//        if (m_hdhtiHotItem.flags & (HHT_ONDIVIDER | HHT_ONDIVOPEN))
//        {
            if (m_bResizing)
            {
                ClipCursor(NULL);
                m_bResizing = FALSE;
            }
//        }

        if (m_hdhtiHotItem.flags & HHT_ONHEADER)
        {
            if (m_bDragging)
            {
                NMHEADER nmhdr;

                nmhdr.hdr.hwndFrom  = m_hWnd;
                nmhdr.hdr.idFrom    = GetDlgCtrlID();
                nmhdr.hdr.code      = HDN_ENDDRAG;
                nmhdr.iItem         = m_iHotIndex;
                nmhdr.iButton       = 0;
                nmhdr.pitem         = &m_hditemHotItem;

                if (!pWnd->SendMessage(WM_NOTIFY, 0, (LPARAM)&nmhdr) &&
                    m_iHotDivider >= 0)
                {
                    try
                    {
                        INT iCount = GetItemCount();

                        ASSERT(m_iHotOrder < iCount);
                        ASSERT(m_iHotDivider <= iCount);

                        LPINT piArray = new INT[iCount * 2];

                        GetOrderArray((LPINT)piArray, iCount);

                        for (INT i = 0, j = 0; i < iCount; i++)
                        {
                            if (j == m_iHotOrder)
                                j++;

                            if ((m_iHotOrder<m_iHotDivider && i == m_iHotDivider - 1) ||
                                (m_iHotOrder>=m_iHotDivider && i == m_iHotDivider))
                                piArray[iCount+i] = piArray[m_iHotOrder];
                            else
                                piArray[iCount+i] = piArray[j++];
                        }

                        SetOrderArray(iCount, (LPINT)&piArray[iCount]);
                        delete piArray;
                    }
                    catch (CException *e)
                    {
                        ASSERT(FALSE);
                        e->Delete();
                    }
                    catch (...)
                    {
                        ASSERT(FALSE);
                    }
                }

                if (m_pDragWnd != NULL)
                {
                    delete m_pDragWnd;
                    m_pDragWnd = NULL;
                }

                if (GetCapture()->GetSafeHwnd() == GetSafeHwnd())
                    ReleaseCapture();

                m_bDragging = FALSE;
                m_iHotDivider = -1;

                Invalidate();
            }
            else
            {
                RECT rectItem;
                VERIFY(GetItemRect(m_iHotIndex, &rectItem));
                InvalidateRect(&rectItem);
            }
        }
    }


    CHeaderCtrl::OnLButtonUp(nFlags, point);
}

void CFlatHeaderCtrl::OnSysColorChange()
{
    CHeaderCtrl::OnSysColorChange();

    m_cr3DHighLight = ::GetSysColor(COLOR_3DHIGHLIGHT);
    m_cr3DShadow    = ::GetSysColor(COLOR_3DSHADOW);
    m_cr3DFace      = ::GetSysColor(COLOR_3DFACE);
    m_crText        = ::GetSysColor(COLOR_BTNTEXT);
}

void CFlatHeaderCtrl::OnMouseMove(UINT nFlags, CPoint point)
{
    if (m_nClickFlags & MK_LBUTTON && m_iHotIndex >= 0)
    {
        if (m_bResizing)
        {
            CHeaderCtrl::OnMouseMove(nFlags, point);
            return;
        }

        if (m_hdhtiHotItem.flags & HHT_ONHEADER)
        {
            if (m_bDragging)
            {
                if (m_pDragWnd != NULL)
                {
                    CRect rect;
                    m_pDragWnd->GetWindowRect(&rect);

                    CPoint pt = point;
                    ClientToScreen(&pt);

                    pt.Offset(-(rect.Width() >> 1), -(rect.Height() >> 1));

                    m_pDragWnd->SetWindowPos(&wndTop, pt.x, pt.y,
                                             0, 0, SWP_NOSIZE | SWP_SHOWWINDOW |
                                             SWP_NOACTIVATE);

                    HDHITTESTINFO hdhti;
                    hdhti.pt.x = point.x;
                    hdhti.pt.y = point.y;

                    INT iHotOrder = -1;
                    INT iHotIndex = (INT)SendMessage(HDM_HITTEST, 0, (LPARAM)(&hdhti));

                    if (iHotIndex >= 0)
                    {
                        HDITEM hditem;

                        hditem.mask = HDI_ORDER;
                        VERIFY(GetItem(iHotIndex, &hditem));

                        iHotOrder = hditem.iOrder;

                        CRect rectItem;
                        VERIFY(GetItemRect(iHotIndex, rectItem));

                        if (hdhti.pt.x > rectItem.CenterPoint().x)
                            iHotOrder++;
                    }

                    if (iHotOrder == m_iHotOrder || iHotOrder == m_iHotOrder+1)
                        iHotOrder = -1;

                    if (iHotOrder != m_iHotDivider)
                    {
                        m_iHotDivider = iHotOrder;
                        Invalidate();
                    }
                }

                return;
            }
            else if (GetStyle() & HDS_DRAGDROP)
            {
                INT iDragCX = GetSystemMetrics(SM_CXDRAG);
                INT iDragCY = GetSystemMetrics(SM_CYDRAG);

                CRect rectDrag(m_ptClickPoint.x - iDragCX, m_ptClickPoint.y - iDragCY,
                               m_ptClickPoint.x + iDragCX, m_ptClickPoint.y + iDragCY);

                if (!rectDrag.PtInRect(point))
                {
                    NMHEADER nmhdr;

                    nmhdr.hdr.hwndFrom  = m_hWnd;
                    nmhdr.hdr.idFrom    = GetDlgCtrlID();
                    nmhdr.hdr.code      = HDN_BEGINDRAG;
                    nmhdr.iItem         = m_iHotIndex;
                    nmhdr.iButton       = 1;
                    nmhdr.pitem         = &m_hditemHotItem;

                    BOOL bBeginDrag = TRUE;
                    CWnd* pWnd = GetParent();

                    if (pWnd != NULL)
                        bBeginDrag = !(pWnd->SendMessage(WM_NOTIFY, 0, (LPARAM)&nmhdr));

                    if (bBeginDrag)
                    {
                        try
                        {
                            ASSERT(m_pDragWnd == NULL);
                            m_pDragWnd = new CFHDragWnd;

                            CRect rectItem;
                            VERIFY(GetItemRect(m_iHotIndex, rectItem));
                            ClientToScreen(&rectItem);

                            m_pDragWnd->Create(rectItem, this, m_iHotIndex);
                        }
                        catch (CException *e)
                        {
                            ASSERT(FALSE);
                            e->Delete();
                        }
                        catch (...)
                        {
                            ASSERT(FALSE);
                        }
                    }

                    SetCapture();
                    m_bDragging = TRUE;
                }
            }
        }
    }
}
