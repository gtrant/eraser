// InPlaceEdit.cpp
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
#include "FlatListCtrl.h"
#include "InPlaceEdit.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CInPlaceEdit

CInPlaceEdit::CInPlaceEdit(int iItem, int iSubItem, CString sInitText) :
m_sInitText(sInitText)
{
    m_iItem = iItem;
    m_iSubItem = iSubItem;
    m_bESC = FALSE;
    m_bNext = FALSE;
    m_iNextItem = iItem;
    m_iNextSubItem = iSubItem;
}

CInPlaceEdit::~CInPlaceEdit()
{
}


BEGIN_MESSAGE_MAP(CInPlaceEdit, CEdit)
    //{{AFX_MSG_MAP(CInPlaceEdit)
    ON_WM_KILLFOCUS()
    ON_WM_NCDESTROY()
    ON_WM_CHAR()
    ON_WM_CREATE()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CInPlaceEdit message handlers

BOOL CInPlaceEdit::PreTranslateMessage(MSG* pMsg)
{
    if (pMsg->message == WM_KEYDOWN)
    {
        if (pMsg->wParam == VK_DOWN ||
            pMsg->wParam == VK_UP)
        {
            if (pMsg->wParam == VK_DOWN)
            {
                m_bNext = TRUE;
                m_iNextItem++;
            }
            else if (pMsg->wParam == VK_UP)
            {
                if (m_iItem > 0)
                {
                    m_bNext = TRUE;
                    m_iNextItem--;
                }
            }

            GetParent()->SetFocus();

            ::TranslateMessage(pMsg);
            ::DispatchMessage(pMsg);
            return TRUE;
        }
        else if (pMsg->wParam == VK_RETURN  ||
                 pMsg->wParam == VK_DELETE  ||
                 pMsg->wParam == VK_ESCAPE  ||
                 pMsg->wParam == VK_TAB     ||
                 GetKeyState(VK_CONTROL))
        {
            ::TranslateMessage(pMsg);
            ::DispatchMessage(pMsg);
            return TRUE;                    // DO NOT process further
        }

    }

    return CEdit::PreTranslateMessage(pMsg);
}


void CInPlaceEdit::OnKillFocus(CWnd* pNewWnd)
{
    CEdit::OnKillFocus(pNewWnd);

    CString str;
    GetWindowText(str);

    // Send Notification to parent of ListView ctrl
    LV_DISPINFO dispinfo;
    dispinfo.hdr.hwndFrom = GetParent()->m_hWnd;
    dispinfo.hdr.idFrom = GetDlgCtrlID();
    dispinfo.hdr.code = LVN_ENDLABELEDIT;

    dispinfo.item.mask = LVIF_TEXT;
    dispinfo.item.iItem = m_iItem;
    dispinfo.item.iSubItem = m_iSubItem;
    dispinfo.item.pszText = m_bESC ? NULL : LPTSTR((LPCTSTR)str);
    dispinfo.item.cchTextMax = str.GetLength();

    int iNewIndex = static_cast<int>(GetParent()->GetParent()->
                                        SendMessage(WM_NOTIFY, GetParent()->GetDlgCtrlID(),
                                                    (LPARAM)&dispinfo));

    if (iNewIndex != m_iItem)
        m_iNextItem += (iNewIndex - m_iItem);

    if (m_bNext)
    {
        CFlatListCtrl *pParent = static_cast<CFlatListCtrl*>(GetParent());

        str = pParent->GetItemText(m_iNextItem, m_iNextSubItem);
        dispinfo.hdr.hwndFrom = pParent->m_hWnd;
        dispinfo.hdr.idFrom = GetDlgCtrlID();
        dispinfo.hdr.code = LVN_BEGINLABELEDIT;

        dispinfo.item.mask = LVIF_TEXT;
        dispinfo.item.iItem = m_iNextItem;
        dispinfo.item.iSubItem = m_iNextSubItem;
        dispinfo.item.pszText = (LPTSTR)((LPCTSTR)str);
        dispinfo.item.cchTextMax = str.GetLength();

        if (pParent->GetParent()->SendMessage(WM_NOTIFY, GetDlgCtrlID(), (LPARAM)&dispinfo) == FLC_EDIT)
            pParent->EditSubLabel(m_iNextItem, m_iNextSubItem);
    }

    DestroyWindow();
}

void CInPlaceEdit::OnNcDestroy()
{
    CEdit::OnNcDestroy();
    delete this;
}


void CInPlaceEdit::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    if (nChar == VK_ESCAPE  ||
        nChar == VK_RETURN  ||
        nChar == VK_TAB)
    {
        if (nChar == VK_ESCAPE)
            m_bESC = TRUE;
        else if (nChar == VK_TAB)
        {
            m_bNext = TRUE;
            m_iNextSubItem++;
        }

        GetParent()->SetFocus();
        return;
    }


    CEdit::OnChar(nChar, nRepCnt, nFlags);

    // Resize edit control if needed

    // Get text extent
    CString str;
    GetWindowText(str);

    CWindowDC dc(this);
    CFont *pFont = GetParent()->GetFont();
    CFont *pFontDC = dc.SelectObject( pFont );
    CSize size = dc.GetTextExtent( str );

    dc.SelectObject(pFontDC);
    size.cx += 5;                           // add some extra buffer

    // Get client rect
    CRect rect, parentrect;
    GetClientRect(&rect);
    GetParent()->GetClientRect(&parentrect);

    // Transform rect to parent coordinates
    ClientToScreen(&rect);
    GetParent()->ScreenToClient(&rect);

    // Check whether control needs to be resized
    // and whether there is space to grow
    if (size.cx > rect.Width())
    {
        if (size.cx + rect.left < parentrect.right)
            rect.right = rect.left + size.cx;
        else
            rect.right = parentrect.right;

        MoveWindow(&rect);
    }
}

int CInPlaceEdit::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
    if (CEdit::OnCreate(lpCreateStruct) == -1)
        return -1;

    // Set the proper font
    CFont* pFont = GetParent()->GetFont();
    SetFont(pFont);

    CWindowDC dc(this);
    CFont *pFontDC = dc.SelectObject(pFont);
    CSize size = dc.GetTextExtent(m_sInitText);

    dc.SelectObject(pFontDC);
    size.cx += 20;                           // add some extra buffer

    // Get client rect
    CRect rect, parentrect;
    GetClientRect(&rect);

    GetParent()->GetClientRect(&parentrect);

    // Transform rect to parent coordinates
    ClientToScreen(&rect);
    GetParent()->ScreenToClient(&rect);

    // Check whether control needs to be resized
    // and whether there is space to grow
    if (size.cx > rect.Width())
    {
        if (size.cx + rect.left < parentrect.right)
            rect.right = rect.left + size.cx;
        else
            rect.right = parentrect.right;

        MoveWindow(&rect);
    }

    SetWindowText(m_sInitText);
    SetFocus();
    SetSel(0, -1);

    return 0;
}
