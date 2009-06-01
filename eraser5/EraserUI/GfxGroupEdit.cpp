// Copyright (c) Iuri Apollonio 1998
// Use & modify as you want & need, and leave those 4 lines.
// Strongly based on article "Inplace edit control" of Mario Contestabile and "Editable subitems" of Zafir
// http://www.codeguru.com

// GfxGroupEdit.cpp : implementation file
//

#include "stdafx.h"
#include "GfxGroupEdit.h"
#include "GfxOutBarCtrl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CGfxGroupEdit

CGfxGroupEdit::CGfxGroupEdit()
{
    bEscapeKey = FALSE;
    iIndex = -1;
    msgSend = NM_OB_ONGROUPENDEDIT;
    bNoDown = false;
}

CGfxGroupEdit::~CGfxGroupEdit()
{
}


BEGIN_MESSAGE_MAP(CGfxGroupEdit, CEdit)
    //{{AFX_MSG_MAP(CGfxGroupEdit)
    ON_WM_KILLFOCUS()
    ON_WM_CREATE()
    ON_WM_CHAR()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CGfxGroupEdit message handlers

void CGfxGroupEdit::OnKillFocus(CWnd* /*pNewWnd*/)
{
    PostMessage(WM_CLOSE, 0, 0);
    if (!bEscapeKey)
    {
        GetWindowText(text);
        if (text != "") GetOwner()->SendMessage(WM_OUTBAR_NOTIFY, msgSend, (LPARAM) this);
    }
}

BOOL CGfxGroupEdit::PreTranslateMessage(MSG* pMsg)
{
    if (pMsg->wParam == VK_RETURN)
    {
        PostMessage(WM_CLOSE, 0, 0);
        return TRUE;
    }
    else if (pMsg->wParam == VK_ESCAPE)
    {
        PostMessage(WM_CLOSE, 0, 0);
        return bEscapeKey = TRUE;
    }

    return CEdit::PreTranslateMessage(pMsg);
}

void CGfxGroupEdit::PostNcDestroy()
{
    CEdit::PostNcDestroy();
    delete this;
}

int CGfxGroupEdit::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
    if (CEdit::OnCreate(lpCreateStruct) == -1)
        return -1;

    SendMessage(WM_SETFONT,(WPARAM) GetStockObject(DEFAULT_GUI_FONT),MAKELPARAM(TRUE,0));
    return 0;
}

void CGfxGroupEdit::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    if (msgSend == NM_OB_ONGROUPENDEDIT)
    {
        CEdit::OnChar(nChar, nRepCnt, nFlags);
        return;
    }

    if (nChar == VK_ESCAPE || nChar == VK_RETURN)
    {
        if (nChar == VK_ESCAPE) bEscapeKey = TRUE;
        GetParent()->SetFocus();
        return;
    }
    CEdit::OnChar(nChar, nRepCnt, nFlags);
    CString str;
    CRect rect, parentrect;
    GetClientRect(&rect);
    GetParent()->GetClientRect(&parentrect);
    ClientToScreen(&rect);
    GetParent()->ScreenToClient(&rect);
    GetWindowText(str);
    CWindowDC dc(this);
    CFont *pFont = GetParent()->GetFont();
    CFont *pFontDC = dc.SelectObject(pFont);
    CRect szrc(rect);
    szrc.bottom = szrc.top;

    if (bNoDown == true)
    {
        dc.DrawText(str, szrc, DT_CALCRECT);
        if (szrc.right >= parentrect.right - 1) rect.right = parentrect.right - 1;
        else rect.right = szrc.right;
        MoveWindow(&rect);
        return;
    }

    dc.DrawText(str, szrc, DT_WORDBREAK|DT_CENTER|DT_CALCRECT);
    dc.SelectObject(pFontDC);
    CSize size = szrc.Size();

    if (size.cx > rect.Width())
    {
        if (size.cx + rect.left < parentrect.right) rect.right = rect.left + size.cx;
        else rect.right = parentrect.right;
        MoveWindow(&rect);
    }
    else if (size.cy > rect.Height())
    {
        if (size.cy + rect.bottom < parentrect.bottom) rect.bottom = rect.top + size.cy;
        else rect.bottom = parentrect.bottom;
        MoveWindow(&rect);
    }
}
