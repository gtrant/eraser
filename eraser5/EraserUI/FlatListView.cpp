// FlatListView.cpp
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
#include "InPlaceEdit.h"
#include "FlatListView.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CFlatListView

IMPLEMENT_DYNCREATE(CFlatListView, CListView)

CFlatListView::CFlatListView() :
m_nMenuID(0),
m_iLastIndex(-1)
{
}

CFlatListView::~CFlatListView()
{
}


BEGIN_MESSAGE_MAP(CFlatListView, CListView)
    //{{AFX_MSG_MAP(CFlatListView)
    ON_NOTIFY_REFLECT(LVN_COLUMNCLICK, OnColumnclick)
    ON_WM_HSCROLL()
    ON_WM_VSCROLL()
    ON_WM_LBUTTONDOWN()
    //}}AFX_MSG_MAP
    ON_NOTIFY_EX_RANGE(TTN_NEEDTEXTW, 0, 0xFFFF, OnToolTipText)
    ON_NOTIFY_EX_RANGE(TTN_NEEDTEXTA, 0, 0xFFFF, OnToolTipText)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CFlatListView drawing

void CFlatListView::OnDraw(CDC* /*pDC*/)
{

}

/////////////////////////////////////////////////////////////////////////////
// CFlatListView diagnostics

#ifdef _DEBUG
void CFlatListView::AssertValid() const
{
    CListView::AssertValid();
}

void CFlatListView::Dump(CDumpContext& dc) const
{
    CListView::Dump(dc);
}
#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// CFlatListView message handlers

void CFlatListView::PreSubclassWindow()
{
    CListView::PreSubclassWindow();
    EnableToolTips();
}

BOOL CFlatListView::PreTranslateMessage(MSG* pMsg)
{
    if (pMsg->message == WM_KEYDOWN)
    {
        if (pMsg->wParam == VK_F2)
        {
            CListCtrl& lc = GetListCtrl();

            int iItem = lc.GetNextItem(-1, LVNI_FOCUSED);

            CString str;
            str = lc.GetItemText(iItem, 0);
            LV_DISPINFO dispinfo;
            dispinfo.hdr.hwndFrom = m_hWnd;
            dispinfo.hdr.idFrom = GetDlgCtrlID();
            dispinfo.hdr.code = LVN_BEGINLABELEDIT;

            dispinfo.item.mask = LVIF_TEXT;
            dispinfo.item.iItem = iItem;
            dispinfo.item.iSubItem = 0;
            dispinfo.item.pszText = (LPTSTR)((LPCTSTR)str);
            dispinfo.item.cchTextMax = str.GetLength();

            if (GetParent()->SendMessage(WM_NOTIFY, GetDlgCtrlID(), (LPARAM)&dispinfo) == FLC_EDIT)
                EditSubLabel(iItem, 0);
        }
    }

    return CListView::PreTranslateMessage(pMsg);
}

void CFlatListView::OnColumnclick(NMHDR* pNMHDR, LRESULT* pResult)
{
    NM_LISTVIEW* pNMListView = (NM_LISTVIEW*)pNMHDR;

    BOOL bSortAscending;

    // If clicked on already sorted column, reverse sort order
    if(pNMListView->iSubItem == m_wndFlatHeader.GetSortColumn(&bSortAscending))
        bSortAscending = !bSortAscending;
    else
        bSortAscending = TRUE;

    m_wndFlatHeader.SetSortColumn(pNMListView->iSubItem, bSortAscending);

    // Do some sort of sorting...

    *pResult = 0;
}

void CFlatListView::OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar)
{
    if (GetFocus() != this) SetFocus();

    CListView::OnHScroll(nSBCode, nPos, pScrollBar);
}

void CFlatListView::OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar)
{
    if (GetFocus() != this) SetFocus();

    CListView::OnVScroll(nSBCode, nPos, pScrollBar);
}

void CFlatListView::OnLButtonDown(UINT nFlags, CPoint point)
{
    CListCtrl& lc = GetListCtrl();
    int index;
    int colnum;

    CListView::OnLButtonDown(nFlags, point);

    if ((index = HitTestEx(point, &colnum)) != -1)
    {
        UINT flag = LVIS_FOCUSED;

        if ((lc.GetItemState(index, flag) & flag) == flag)
        {
            // Check first click(Gendoh)
            if ((lc.GetExtendedStyle() & LVS_EX_FULLROWSELECT) &&
                index != m_iLastIndex)
            {
                m_iLastIndex = index;
                lc.SetItemState(index, LVIS_SELECTED | flag,
                             LVIS_SELECTED | flag);

            }
            else
            {
                // Second Click (Original code)
                // Add check for LVS_EDITLABELS
                if (GetWindowLong(m_hWnd, GWL_STYLE) & LVS_EDITLABELS)
                {
                    CString str;
                    str = lc.GetItemText(index, colnum);
                    LV_DISPINFO dispinfo;
                    dispinfo.hdr.hwndFrom = m_hWnd;
                    dispinfo.hdr.idFrom = lc.GetDlgCtrlID();
                    dispinfo.hdr.code = LVN_BEGINLABELEDIT;

                    dispinfo.item.mask = LVIF_TEXT;
                    dispinfo.item.iItem = index;
                    dispinfo.item.iSubItem = colnum;
                    dispinfo.item.pszText = (LPTSTR)((LPCTSTR)str);
                    dispinfo.item.cchTextMax = str.GetLength();

                    if (GetParent()->SendMessage(WM_NOTIFY, GetDlgCtrlID(), (LPARAM)&dispinfo) == FLC_EDIT)
                        EditSubLabel(index, colnum);
                }
            }
        }
        else
            lc.SetItemState(index, LVIS_SELECTED | LVIS_FOCUSED,
                         LVIS_SELECTED | flag);
    }
}

// EditSubLabel         - Start edit of a sub item label
// Returns              - Temporary pointer to the new edit control
// nItem                - The row index of the item to edit
// nCol                 - The column of the sub item.
CEdit* CFlatListView::EditSubLabel(int nItem, int nCol)
{
    // The returned pointer should not be saved

    CListCtrl& lc = GetListCtrl();

    // Make sure that the item is visible
    if (!lc.EnsureVisible(nItem, TRUE)) return NULL;

    // Make sure that nCol is valid
    int nColumnCount = m_wndFlatHeader.GetItemCount();

    if (nCol >= nColumnCount || lc.GetColumnWidth(nCol) < 5)
        return NULL;

    // Get the column offset
    int offset = 0;

    // Array of Column prder by Sang-il, Lee
    INT *piColumnArray = new INT[nColumnCount];
    ((CListCtrl*)this)->GetColumnOrderArray(piColumnArray);

    for (int i = 0; nCol != piColumnArray[i]; i++)
        offset += lc.GetColumnWidth(piColumnArray[i]);

    // delete Array
    delete[] piColumnArray;

    CRect rect;
    lc.GetItemRect(nItem, &rect, LVIR_BOUNDS);

    // Now scroll if we need to expose the column
    CRect rcClient;
    GetClientRect(&rcClient);

    if (offset + rect.left < 0 || offset + rect.left > rcClient.right)
    {
        CSize size;
        size.cx = offset + rect.left;
        size.cy = 0;

        lc.Scroll(size);
        rect.left -= size.cx;
    }

    // Get Column alignment
    LV_COLUMN lvcol;
    lvcol.mask = LVCF_FMT;

    lc.GetColumn(nCol, &lvcol);

    DWORD dwStyle;

    if ((lvcol.fmt & LVCFMT_JUSTIFYMASK) == LVCFMT_LEFT)
        dwStyle = ES_LEFT;
    else if ((lvcol.fmt & LVCFMT_JUSTIFYMASK) == LVCFMT_RIGHT)
        dwStyle = ES_RIGHT;
    else
        dwStyle = ES_CENTER;

    rect.left += offset + 4;
    rect.right = rect.left + lc.GetColumnWidth(nCol);

    if (rect.right > rcClient.right) rect.right = rcClient.right;

    dwStyle |= WS_BORDER | WS_CHILD | WS_VISIBLE | ES_AUTOHSCROLL;

    CEdit *pEdit = new CInPlaceEdit(nItem, nCol, lc.GetItemText(nItem, nCol));
    pEdit->Create(dwStyle, rect, this, IDC_IPEDIT);

    return pEdit;
}

// HitTestEx    - Determine the row index and column index for a point
// Returns      - the row index or -1 if point is not over a row
// point        - point to be tested.
// col          - to hold the column index
int CFlatListView::HitTestEx(CPoint &point, int *col, RECT *rectCell /*=NULL*/) const
{
    CListCtrl& lc = GetListCtrl();

    int colnum = 0;
    int row = lc.HitTest( point, NULL );

    if (col) *col = 0;

    // Make sure that the ListView is in LVS_REPORT
    if ((GetWindowLong(m_hWnd, GWL_STYLE) & LVS_TYPEMASK) != LVS_REPORT)
        return row;

    // Get the top and bottom row visible
    row = lc.GetTopIndex();

    int bottom = row + lc.GetCountPerPage();
    if (bottom > lc.GetItemCount())
        bottom = lc.GetItemCount();

    // Get the number of columns
    int nColumnCount = m_wndFlatHeader.GetItemCount();

    // Loop through the visible rows

    // Array of Column prder by Sang-il, Lee
    INT* piColumnArray = new INT[nColumnCount];
    ((CListCtrl*)this)->GetColumnOrderArray(piColumnArray);

    for( ;row <=bottom;row++)
    {
        // Get bounding rect of item and check whether point falls in it.
        CRect rect;
        lc.GetItemRect( row, &rect, LVIR_BOUNDS );
        if( rect.PtInRect(point) )
        {
            // Now find the column
            for( colnum = 0; colnum < nColumnCount; colnum++ )
            {
                int colwidth = lc.GetColumnWidth(piColumnArray[colnum]);
                if( point.x >= rect.left
                    && point.x <= (rect.left + colwidth ) )
                {
                    if (col) *col = piColumnArray[colnum];
//                    TRACE("Hittestex = %d\n",*col);

                    if (rectCell)
                    {
                        CRect rectClient;
                        GetClientRect(&rectClient);

                        rect.right = rect.left + colwidth;

                        // Make sure that the right extent does not exceed
                        // the client area
                        if (rect.right > rectClient.right)
                            rect.right = rectClient.right;

                        *rectCell = rect;
                    }

                    delete [] piColumnArray;
                    return row;
                }
                rect.left += colwidth;
            }
        }
    }

    delete [] piColumnArray;

    return -1;
}


BOOL CFlatListView::Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext)
{
    if (CWnd::Create(lpszClassName, lpszWindowName, dwStyle, rect, pParentWnd, nID, pContext))
    {
        CListCtrl& lcList = GetListCtrl();
        CHeaderCtrl *phc = lcList.GetHeaderCtrl();

        return (m_wndFlatHeader.SubclassWindow(phc->GetSafeHwnd()));
    }

    return FALSE;
}

BOOL CFlatListView::PreCreateWindow(CREATESTRUCT& cs)
{
    cs.style |= WS_CLIPCHILDREN;
    return CListView::PreCreateWindow(cs);
}

/*int CFlatListView::OnToolHitTest(CPoint point, TOOLINFO * pTI) const*/
INT_PTR CFlatListView::OnToolHitTest(CPoint point, TOOLINFO* pTI) const
{
    int row, col;
    RECT cellrect;
    row = HitTestEx(point, &col, &cellrect);

    if (row == -1)
        return -1;

    pTI->hwnd = m_hWnd;
    pTI->uId = (UINT)((row << 10) + (col & 0x3ff) + 1);
    pTI->lpszText = LPSTR_TEXTCALLBACK;

    pTI->rect = cellrect;

    return pTI->uId;
}

BOOL CFlatListView::OnToolTipText(UINT /*id*/, NMHDR * pNMHDR, LRESULT * pResult)
{
    CListCtrl& lc = GetListCtrl();

    // need to handle both ANSI and UNICODE versions of the message
    TOOLTIPTEXTA* pTTTA = (TOOLTIPTEXTA*)pNMHDR;
    TOOLTIPTEXTW* pTTTW = (TOOLTIPTEXTW*)pNMHDR;
    CString strTipText;
    UINT nID = pNMHDR->idFrom;

    if (nID == 0)          // Notification in NT from automatically
        return FALSE;      // created tooltip

    int row = ((nID-1) >> 10) & 0x3fffff ;
    int col = (nID-1) & 0x3ff;
    strTipText = lc.GetItemText(row, col);

    // if there is no text and therefore nothing
    // to show, let other windows have a chance to
    // make something out of it

    if (strTipText.IsEmpty())
        return FALSE;

#ifndef _UNICODE
    if (pNMHDR->code == TTN_NEEDTEXTA)
        lstrcpyn(pTTTA->szText, strTipText, 80);
    else
        _mbstowcsz(pTTTW->szText, strTipText, 80);
#else
    if (pNMHDR->code == TTN_NEEDTEXTA)
        _wcstombsz(pTTTA->szText, strTipText, 80);
    else
        lstrcpyn(pTTTW->szText, strTipText, 80);
#endif
    *pResult = 0;

    return TRUE;    // message was handled
}

void CFlatListView::SetRedraw(BOOL bRedraw)
{
    static int iRedrawCount = 0;

    if (!bRedraw)
    {
        if (iRedrawCount++ <= 0)
            CListView::SetRedraw(FALSE);
    }
    else
    {
        if (--iRedrawCount <= 0)
        {
            CListView::SetRedraw(TRUE);
            iRedrawCount = 0;
//            Invalidate();
        }
    }
}

int CFlatListView::SelItemRange(BOOL bSelect, int nFirstItem, int nLastItem)
{
    CListCtrl& lc = GetListCtrl();

    if (nLastItem < 0)
        nLastItem = lc.GetItemCount() - 1;

    // make sure nFirstItem and nLastItem are valid
    if (nFirstItem >= lc.GetItemCount() || nLastItem >= lc.GetItemCount())
        return 0;

    SetRedraw(FALSE);

    int nItemsSelected = 0;
    int nFlags = bSelect ? 0 : LVNI_SELECTED;
    int nItem = nFirstItem - 1;

    while ((nItem = lc.GetNextItem(nItem, nFlags)) >= 0 && nItem <= nLastItem)
    {
        nItemsSelected++;
        lc.SetItemState(nItem, bSelect ? LVIS_SELECTED : 0, LVIS_SELECTED );
    }

    SetRedraw(TRUE);

    return nItemsSelected;
}
