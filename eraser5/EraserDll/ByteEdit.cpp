// ByteEdit.cpp
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
#include "eraser.h"
#include "ByteEdit.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CByteEdit

CByteEdit::CByteEdit()
{
}

CByteEdit::~CByteEdit()
{
}


BEGIN_MESSAGE_MAP(CByteEdit, CEdit)
    //{{AFX_MSG_MAP(CByteEdit)
    ON_WM_CHAR()
    ON_WM_KEYDOWN()
    ON_WM_RBUTTONDOWN()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CByteEdit message handlers

void CByteEdit::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    if (nChar == TEXT('0') || nChar == TEXT('1'))
    {
        CString strText;
        int     startPos;
        int     endPos;

        GetSel(startPos, endPos);
        GetWindowText(strText);

        if (startPos >= 0 && startPos < 8)
        {
            strText.SetAt(startPos, (TCHAR)nChar);
            SetWindowText((LPCTSTR)strText);
            SetSel(startPos + 1, startPos + 1);
        }
    }
    else if (nChar == VK_LEFT || nChar == VK_RIGHT)
        CEdit::OnChar(nChar, nRepCnt, nFlags);
    else
        MessageBeep(MB_ICONASTERISK);
}

BYTE CByteEdit::GetByte()
{
    BYTE    byte = 0;
    CString str;

    GetWindowText(str);

    for (BYTE i = 0; i < 8; i++)
    {
        if (str[(int)i] == TEXT('1'))
            byte |= (1 << (7 - i));
    }

    return byte;
}

void CByteEdit::SetByte(BYTE byte)
{
    CString str = TEXT("00000000");

    for (BYTE i = 0; i < 8; i++)
        str.SetAt(7 - i, (byte & (1 << i)) ? TEXT('1') : TEXT('0'));

    SetWindowText(str);
}

BOOL CByteEdit::Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext)
{
    if (CWnd::Create(lpszClassName, lpszWindowName, dwStyle, rect, pParentWnd, nID, pContext))
    {
        SetLimitText(8);
        SetByte(0);

        return TRUE;
    }

    return FALSE;
}

void CByteEdit::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    if (IsCharAlphaNumeric((TCHAR)nChar) || nChar == VK_LEFT ||
        nChar == VK_RIGHT || nChar == VK_HOME || nChar == VK_END)
    {
        CEdit::OnKeyDown(nChar, nRepCnt, nFlags);
    }
}

void CByteEdit::OnRButtonDown(UINT /*nFlags*/, CPoint /*point*/)
{
    return;
}
