// MaskEd.cpp
//
// Orginially written by : DunnoWho
// Modified by : Jeremy Davis, 24/07/1998
//     Added CTimeEdit::SetMins and CTimeEdit::SetHours
// Modified by : sami@tolvanen.com
//     Added handler to right mouse click to stop the popup menu from showing
//     Miscellanious bug fixes

#include "stdafx.h"
#include "MaskEd.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// COleDateTime read /write

static COleDateTime ReadCOleDateTime(LPCTSTR lpszData)
{
    COleDateTime DateTime;
    DateTime.ParseDateTime(lpszData);

    return DateTime;
}

static void FormatCOleDateTime(CString& strData, COleDateTime DateTime, int len)
{
    strData.Empty();

    if (DateTime.m_dt == 0)
    {
        if(len == 5)
            strData = "00:00";

        return;
    }

    if (len == 8)
        strData = DateTime.Format(TEXT("%d/%m/%y"));
    else if (len == 5) // added these two
        strData = DateTime.Format(TEXT("%H:%M"));
    else
        strData = DateTime.Format(TEXT("%d/%m/%Y"));
}

/////////////////////////////////////////////////////////////////////////////
// CMaskEdit class

IMPLEMENT_DYNAMIC(CMaskEdit, CEdit)

BEGIN_MESSAGE_MAP(CMaskEdit, CEdit)
//{{AFX_MSG_MAP(CMaskEdit)
ON_WM_CHAR()
ON_WM_KEYDOWN()
//}}AFX_MSG_MAP
END_MESSAGE_MAP()

CMaskEdit::CMaskEdit()
{
    m_bUseMask              = FALSE;
    m_strMask               = TEXT("");
    m_strLiteral            = TEXT("");
    m_strValid              = TEXT("");
    m_strHours              = TEXT("23");
    m_strMins               = TEXT("59");
    m_strMinHours           = TEXT("00");
    m_strMinMins            = TEXT("00");
    m_bMaskKeyInProgress    = FALSE;
    m_strMaskLiteral        = TEXT("");
}

void CMaskEdit::SetMask(LPCSTR lpMask, LPCSTR lpLiteral, LPCSTR lpValid)
{
    m_bUseMask = FALSE;

    if (lpMask == NULL)
        return;

    m_strMask = lpMask;

    if (m_strMask.IsEmpty())
        return;

    if (lpLiteral != NULL)
    {
        m_strLiteral = lpLiteral;

        if (m_strLiteral.GetLength() != m_strMask.GetLength())
            m_strLiteral.Empty();
    }
    else
        m_strLiteral.Empty();

    if (lpValid != NULL)
        m_strValid = lpValid;
    else
        m_strValid.Empty();

    m_bUseMask = TRUE;
}

void CMaskEdit::SendChar(UINT nChar)
{
    m_bMaskKeyInProgress = TRUE;

    AfxCallWndProc(this, m_hWnd, WM_CHAR, nChar, 1);

    m_bMaskKeyInProgress = FALSE;
}

void CMaskEdit::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    if (!m_bMaskKeyInProgress)
    {
        if (!CheckChar(nChar))
            return;
    }

    if (m_bUseMask)
    {
        if (isprint(nChar))
        {
            // si un masque existe, on est en insert mode
            int startPos, endPos;

            GetSel(startPos, endPos);
            SetSel(startPos, endPos + 1);
            Clear();

            m_str.SetAt(endPos, (TCHAR)nChar); // added this
        }
        else if (nChar == VK_BACK)
        {
            int startPos, endPos;
            GetSel(startPos, endPos);

            // sanity range check
            if ((startPos == endPos) && (startPos >= 1) && (startPos <= m_str.GetLength()))
            {
                // get the masked literal representation
                TRACE(TEXT("m_strMaskLiteral = [%s](%s)\n"), m_strMaskLiteral, m_str);

                // back space the cursor
                SendMessage(WM_KEYDOWN, VK_LEFT, 0);

                if (!m_strMaskLiteral.IsEmpty())
                {
                    // update the char backspacing over
                    SendChar(m_strMaskLiteral[startPos-1]);

                    // back space the cursor again
                    SendMessage(WM_KEYDOWN, VK_LEFT, 0);
                }
            }
            else // out of range or have more than one char selected
                MessageBeep(static_cast < UINT>(-1));

            return;
        }
    }

    CEdit::OnChar(nChar, nRepCnt, nFlags);

    if (!m_bMaskKeyInProgress && m_bUseMask && !m_strLiteral.IsEmpty())
    {
        int startPos, endPos;
        GetSel(startPos, endPos);

        // make sure the string is not longer than the mask
        if (endPos < m_strLiteral.GetLength())
        {
            UINT c = m_strLiteral.GetAt(endPos);

            if (c != TEXT('_'))
                SendChar(c);
        }
    }
}

BOOL CMaskEdit::CheckChar(UINT nChar)
{
    UINT c;

    // do not use mask
    if (!m_bUseMask)
        return TRUE;

    // control character, OK
    if (!isprint(nChar))
        return TRUE;

    // unselect all selections, if any
    int startPos, endPos;

    GetSel(startPos, endPos);
    SetSel(-1, 0);
    SetSel(startPos, startPos);

    // check the key against the mask
    GetSel(startPos, endPos);

    // make sure the string is not longer than the mask
    if (endPos >= m_strMask.GetLength())
    {
        MessageBeep(static_cast < UINT>(-1));
        return FALSE;
    }

    // check to see if a literal is in this position
    c = TEXT('_');

    if (!m_strLiteral.IsEmpty())
        c = m_strLiteral.GetAt(endPos);

    if (c != TEXT('_'))
    {
        SendChar(c);
        GetSel(startPos, endPos);
    }

    // check the valid string character
    if (m_strValid.Find((TCHAR)nChar) != -1)
        return TRUE;

    // check the key against the mask
    c = m_strMask.GetAt(endPos);

    switch (c)
    {
    case TEXT('0'):       // digit only // completely changed this
        {
            BOOL doit = TRUE;

            if (isdigit(nChar))
            {
                if (m_isdate)
                {
                    if (endPos == 0)
                    {
                        if (nChar > TEXT('3'))
                            doit = FALSE;
                    }

                    if (endPos == 1)
                    {
                        if (m_str.GetAt(0) == TEXT('3'))
                        {
                            if (nChar > TEXT('1'))
                                doit = FALSE;
                        }
                    }

                    if (endPos == 3)
                    {
                        if (nChar > TEXT('1'))
                            doit = FALSE;
                    }

                    if (endPos == 4)
                    {
                        if (m_str.GetAt(3) == TEXT('1'))
                        {
                            if (nChar > TEXT('2'))
                                doit = FALSE;
                        }
                    }
                }
                else if (m_bisTime)
                {
                    if (endPos == 0)
                    {
                        if (nChar > static_cast < UINT>(m_strHours[0]) ||
                            nChar < static_cast < UINT>(m_strMinHours[0]))
                            doit = FALSE;
                    }

                    if (endPos == 1)
                    {
                        if (m_str.GetAt(0) == m_strHours[0])
                        {
                            if (nChar > static_cast < UINT>(m_strHours[1]))
                                doit = FALSE;
                        }
                        else if (m_str.GetAt(0) == m_strMinHours[0])
                        {
                            if (nChar < static_cast < UINT>(m_strMinHours[1]))
                                doit = FALSE;
                        }
                    }

                    if (endPos == 3)
                    {
                        if (nChar > static_cast < UINT>(m_strMins[0]) ||
                            nChar < static_cast < UINT>(m_strMinMins[0]))
                            doit = FALSE;
                    }

                    if (endPos == 4)
                    {
                        if (m_str.GetAt(3) == m_strMins[0])
                        {
                            if (nChar > static_cast < UINT>(m_strMins[1]))
                                doit = FALSE;
                        }
                        else if (m_str.GetAt(3) == m_strMinMins[0])
                        {
                            if (nChar < static_cast < UINT>(m_strMinMins[1]))
                                doit = FALSE;
                        }
                    }
                }

                return doit;
            }
            break;
        }
    case TEXT('9'):       // digit or space
        {
            if (isdigit(nChar) || nChar == VK_SPACE)
                return TRUE;

            break;
        }
    case TEXT('#'):       // digit or space or '+' or '-'
        {
            if (isdigit(nChar) || nChar == VK_SPACE ||
                nChar == VK_ADD || nChar == VK_SUBTRACT)
                return TRUE;

            break;
        }
    case TEXT('L'):       // alpha only
        {
            if (isalpha(nChar))
                return TRUE;

            break;
        }
    case TEXT('?'):       // alpha or space
        {
            if (isalpha(nChar) || nChar == VK_SPACE)
                return TRUE;

            break;
        }
    case TEXT('A'):       // alpha numeric only
        {
            if (isalnum(nChar))
                return TRUE;

            break;
        }
    case TEXT('a'):       // alpha numeric or space
        {
            if (isalnum(nChar) || nChar == VK_SPACE)
                return TRUE;

            break;
        }
    case TEXT('&'):       // all print character only
        {
            if (isprint(nChar))
                return TRUE;

            break;
        }
    default:
        break;
    }

    MessageBeep(static_cast < UINT>(-1));
    return FALSE;
}

void CMaskEdit::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    // si un masque existe, tester les touches spéciales
    if (m_bUseMask)
    {
        if (nChar == VK_DELETE || nChar == VK_INSERT)
            return;
    }

    CEdit::OnKeyDown(nChar, nRepCnt, nFlags);
}


/////////////////////////////////////////////////////////////////////////////
// CTimeEdit class completely new

IMPLEMENT_DYNAMIC(CTimeEdit, CMaskEdit)

BEGIN_MESSAGE_MAP(CTimeEdit, CMaskEdit)
//{{AFX_MSG_MAP(CTimeEdit)
    ON_WM_RBUTTONDOWN()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

CTimeEdit::CTimeEdit()
{
    m_bUseMask = TRUE;
    m_isdate = FALSE;
    m_strMask = TEXT("00:00");
    m_strLiteral = TEXT("__:__");
}

void CTimeEdit::SetTime(COleDateTime& Date)
{
    CString strText;
    FormatCOleDateTime(strText, Date, 5);
    m_str = m_strMaskLiteral = strText;
    SetWindowText(strText);
}

void CTimeEdit::SetTime(CString Date)
{
    m_str = m_strMaskLiteral = Date;
    SetWindowText(Date);
}

COleDateTime CTimeEdit::GetTime()
{
    CString strText;
    GetWindowText(strText);
    return ReadCOleDateTime(strText);
}

CString CTimeEdit::GetTimeStr()
{
    CString strText;
    GetWindowText(strText);
    return strText;
}

void CTimeEdit::SetHours(int hrs)
{
    m_strHours.Format(TEXT("%.2u"), hrs);
}

void CTimeEdit::SetMins(int hrs)
{
    m_strMins.Format(TEXT("%.2u"), hrs);
}

void CTimeEdit::SetMinHours(int hrs)
{
    m_strMinHours.Format(TEXT("%.2u"), hrs);
}

void CTimeEdit::SetMinMins(int hrs)
{
    m_strMinMins.Format(TEXT("%.2u"), hrs);
}

void CTimeEdit::OnRButtonDown(UINT /*nFlags*/, CPoint /*point*/)
{
    return;
}