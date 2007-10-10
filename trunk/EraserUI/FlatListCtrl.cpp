// FlatListCtrl.cpp
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
#include "FlatListCtrl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CFlatListCtrl

CFlatListCtrl::CFlatListCtrl() :
m_nMenuID(0),
m_iLastIndex(-1)
{
}

CFlatListCtrl::~CFlatListCtrl()
{
}


BEGIN_MESSAGE_MAP(CFlatListCtrl, CListCtrl)
    ON_WM_CONTEXTMENU()
    //{{AFX_MSG_MAP(CFlatListCtrl)
    ON_NOTIFY_REFLECT(LVN_COLUMNCLICK, OnColumnClick)
    ON_WM_VSCROLL()
    ON_WM_HSCROLL()
    ON_WM_LBUTTONDOWN()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CFlatListCtrl message handlers

void CFlatListCtrl::PreSubclassWindow()
{
    CListCtrl::PreSubclassWindow();
    VERIFY(m_wndFlatHeader.SubclassWindow(::GetDlgItem(m_hWnd,0)));
}


void CFlatListCtrl::OnColumnClick(NMHDR* pNMHDR, LRESULT* pResult)
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

void CFlatListCtrl::OnContextMenu(CWnd*, CPoint point)
{

    // CG: This block was added by the Pop-up Menu component
    if (m_nMenuID > 0)
    {
        if (point.x == -1 && point.y == -1){
            //keystroke invocation
            CRect rect;
            GetClientRect(rect);
            ClientToScreen(rect);

            point = rect.TopLeft();
            point.Offset(5, 5);
        }

        CMenu menu;
        VERIFY(menu.LoadMenu(m_nMenuID));

        CMenu* pPopup = menu.GetSubMenu(0);
        ASSERT(pPopup != NULL);
        CWnd* pWndPopupOwner = this;

        while (pWndPopupOwner->GetStyle() & WS_CHILD)
            pWndPopupOwner = pWndPopupOwner->GetParent();

        pPopup->TrackPopupMenu(TPM_LEFTALIGN | TPM_RIGHTBUTTON, point.x, point.y,
            pWndPopupOwner);

        menu.DestroyMenu();
    }
}

// HitTestEx    - Determine the row index and column index for a point
// Returns      - the row index or -1 if point is not over a row
// point        - point to be tested.
// col          - to hold the column index
int CFlatListCtrl::HitTestEx(CPoint &point, int *col) const
{
    int colnum = 0;
    int row = HitTest( point, NULL );

    if (col) *col = 0;

    // Make sure that the ListView is in LVS_REPORT
    if ((GetWindowLong(m_hWnd, GWL_STYLE) & LVS_TYPEMASK) != LVS_REPORT)
        return row;

    // Get the top and bottom row visible
    row = GetTopIndex();

    int bottom = row + GetCountPerPage();
    if (bottom > GetItemCount())
        bottom = GetItemCount();

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
        GetItemRect( row, &rect, LVIR_BOUNDS );
        if( rect.PtInRect(point) )
        {
                // Now find the column
            for( colnum = 0; colnum < nColumnCount; colnum++ )
            {
                int colwidth = GetColumnWidth(piColumnArray[colnum]);
                if( point.x >= rect.left
                        && point.x <= (rect.left + colwidth ) )
                {
                    if( col ) *col = piColumnArray[colnum];
                    TRACE("Hittestex = %d\n",*col);
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

// EditSubLabel         - Start edit of a sub item label
// Returns              - Temporary pointer to the new edit control
// nItem                - The row index of the item to edit
// nCol                 - The column of the sub item.
CEdit* CFlatListCtrl::EditSubLabel(int nItem, int nCol)
{
    // The returned pointer should not be saved

    // Make sure that the item is visible
    if (!EnsureVisible(nItem, TRUE)) return NULL;

    // Make sure that nCol is valid
    int nColumnCount = m_wndFlatHeader.GetItemCount();

    if (nCol >= nColumnCount || GetColumnWidth(nCol) < 5)
        return NULL;

    // Get the column offset
    int offset = 0;

    // Array of Column prder by Sang-il, Lee
    INT *piColumnArray = new INT[nColumnCount];
    ((CListCtrl*)this)->GetColumnOrderArray(piColumnArray);

    for (int i = 0; nCol != piColumnArray[i]; i++)
        offset += GetColumnWidth(piColumnArray[i]);

    // delete Array
    delete[] piColumnArray;

    CRect rect;
    GetItemRect(nItem, &rect, LVIR_BOUNDS);

    // Now scroll if we need to expose the column
    CRect rcClient;
    GetClientRect(&rcClient);

    if (offset + rect.left < 0 || offset + rect.left > rcClient.right)
    {
        CSize size;
        size.cx = offset + rect.left;
        size.cy = 0;

        Scroll(size);
        rect.left -= size.cx;
    }

    // Get Column alignment
    LV_COLUMN lvcol;
    lvcol.mask = LVCF_FMT;

    GetColumn(nCol, &lvcol);

    DWORD dwStyle;

    if ((lvcol.fmt & LVCFMT_JUSTIFYMASK) == LVCFMT_LEFT)
        dwStyle = ES_LEFT;
    else if ((lvcol.fmt & LVCFMT_JUSTIFYMASK) == LVCFMT_RIGHT)
        dwStyle = ES_RIGHT;
    else
        dwStyle = ES_CENTER;

    rect.left += offset + 4;
    rect.right = rect.left + GetColumnWidth(nCol);

    if (rect.right > rcClient.right) rect.right = rcClient.right;

    dwStyle |= WS_BORDER | WS_CHILD | WS_VISIBLE | ES_AUTOHSCROLL;

    CEdit *pEdit = new CInPlaceEdit(nItem, nCol, GetItemText(nItem, nCol));
    pEdit->Create(dwStyle, rect, this, IDC_IPEDIT);

    return pEdit;
}
void CFlatListCtrl::OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar)
{
    if (GetFocus() != this) SetFocus();

    CListCtrl::OnVScroll(nSBCode, nPos, pScrollBar);
}

void CFlatListCtrl::OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar)
{
    if (GetFocus() != this) SetFocus();

    CListCtrl::OnHScroll(nSBCode, nPos, pScrollBar);
}


void CFlatListCtrl::OnLButtonDown(UINT nFlags, CPoint point)
{
    int index;
    int colnum;

    CListCtrl::OnLButtonDown(nFlags, point);

    if ((index = HitTestEx(point, &colnum)) != -1)
    {
        UINT flag = LVIS_FOCUSED;

        if ((GetItemState(index, flag) & flag) == flag)
        {
            // Check first click(Gendoh)
            if ((GetExtendedStyle() & LVS_EX_FULLROWSELECT) &&
                index != m_iLastIndex)
            {
                m_iLastIndex = index;
                SetItemState(index, LVIS_SELECTED | flag,
                             LVIS_SELECTED | flag);

            }
            else
            {
                // Second Click (Original code)
                // Add check for LVS_EDITLABELS
                if (GetWindowLong(m_hWnd, GWL_STYLE) & LVS_EDITLABELS)
                {
                    CString str;
                    str = GetItemText(index, colnum);
                    LV_DISPINFO dispinfo;
                    dispinfo.hdr.hwndFrom = m_hWnd;
                    dispinfo.hdr.idFrom = GetDlgCtrlID();
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
            SetItemState(index, LVIS_SELECTED | LVIS_FOCUSED,
                         LVIS_SELECTED | flag);
    }
}


BOOL CFlatListCtrl::PreTranslateMessage(MSG* pMsg)
{
    if (pMsg->message == WM_KEYDOWN)
    {
        if (pMsg->wParam == VK_F2)
        {
            int iItem = GetNextItem(-1, LVNI_FOCUSED);

            CString str;
            str = GetItemText(iItem, 0);
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

    return CListCtrl::PreTranslateMessage(pMsg);
}
