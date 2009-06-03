// ConfirmDialog.cpp
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
#include "erasext.h"
#include "..\EraserDll\eraserdll.h"
#include "..\EraserUI\FitFileNameToScrn.h"
#include "ConfirmDialog.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CConfirmDialog dialog


CConfirmDialog::CConfirmDialog(CWnd* pParent /*=NULL*/) :
CDialog(CConfirmDialog::IDD, pParent),
m_bSingleFile(FALSE),
m_bUseFiles(TRUE),
m_bMove(FALSE),
m_hAccel(NULL)
{
    //{{AFX_DATA_INIT(CConfirmDialog)
    m_strLineOne = _T("");
    m_strLineTwo = _T("");
    //}}AFX_DATA_INIT
}


void CConfirmDialog::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CConfirmDialog)
    DDX_Text(pDX, IDC_STATIC_LINEONE, m_strLineOne);
    DDX_Text(pDX, IDC_STATIC_LINETWO, m_strLineTwo);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CConfirmDialog, CDialog)
    //{{AFX_MSG_MAP(CConfirmDialog)
    ON_BN_CLICKED(IDOPTIONS, OnOptions)
    ON_BN_CLICKED(IDOK, OnYes)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CConfirmDialog message handlers


BOOL CConfirmDialog::OnInitDialog()
{
    try
    {
        if (!m_bMove)
            m_strLineOne.LoadString(IDS_CONFIRM);
        else
        {
            CString strTitle;

            strTitle.LoadString(IDS_MOVE_TITLE);
            m_strLineOne.LoadString(IDS_CONFIRM_MOVE);

            SetWindowText(strTitle);
        }

        CWnd  *pWnd = GetDlgItem(IDC_STATIC_LINEONE);
        CWnd  *pWnd2 = GetDlgItem(IDC_STATIC_LINETWO);
        CRect rectWnd;
        CSize sizeText;
        int   iSaved;

        // create a context for the window
        CClientDC dc(pWnd);
        iSaved = dc.SaveDC();

        // select the used font
        dc.SelectObject(pWnd->GetFont());

        if (m_bSingleFile)
        {
            if (!m_bMove)
            {
                // the width required to display the whole string
                sizeText = dc.GetTextExtent(m_strLineOne + _T("\'") + m_strData + _T("\'?"));

                // the width of the window
                pWnd->GetClientRect(&rectWnd);

                if (rectWnd.Width() >= sizeText.cx)
                    m_strLineOne += _T("\'") + m_strData + _T("\'?");
                else
                {
                    fitFileNameToScrn(pWnd2, m_strData, _T("\'"), _T("\'?"));
                    m_strLineTwo = _T("\'") + m_strData + _T("\'?");
                }
            }
            else
            {
                CString strTo;
                strTo.LoadString(IDS_CONFIRM_MOVE_FILE);

                // the width required to display the whole string
                sizeText = dc.GetTextExtent(m_strLineOne + _T("\'") + m_strData + _T("\' ") + strTo);

                // the width of the window
                pWnd->GetClientRect(&rectWnd);

                if (rectWnd.Width() >= sizeText.cx)
                {
                    m_strLineOne += _T("\'") + m_strData + _T("\' ") + strTo;

                    sizeText = dc.GetTextExtent(m_strLineOne + _T(" \'") + m_strTarget + _T("\'?"));

                    if (rectWnd.Width() >= sizeText.cx)
                        m_strLineOne += _T(" \'") + m_strTarget + _T("\'?");
                    else
                    {
                        fitFileNameToScrn(pWnd2, m_strTarget, _T("\'"), _T("\'?"));
                        m_strLineTwo = _T("\'") + m_strTarget + _T("\'?");
                    }
                }
                else
                {
                    fitFileNameToScrn(pWnd, m_strData, m_strLineOne + _T("\'"), _T("\'"));
                    m_strLineOne += _T("\'") + m_strData + _T("\'");

                    fitFileNameToScrn(pWnd2, m_strTarget, strTo + _T(" \'"), _T("\'?"));
                    m_strLineTwo = strTo + _T(" \'") + m_strTarget + _T("\'?");
                }
            }
        }
        else
        {
            m_strLineOne += m_strData;

            if (m_bMove)
            {
                // the width required to display the whole string
				sizeText = dc.GetTextExtent(m_strLineOne + _T("\'") + m_strTarget + _T("\'?"));

                // the width of the window
                pWnd->GetClientRect(&rectWnd);

                if (rectWnd.Width() >= sizeText.cx)
                {
                    m_strLineOne += _T("\'") + m_strTarget + _T("\'?");
                }
                else
                {
                    fitFileNameToScrn(pWnd2, m_strTarget, _T("\'"), _T("\'?"));
                    m_strLineTwo = _T("\'") + m_strTarget + _T("\'?");
                }
            }
        }

        dc.RestoreDC(iSaved);

        CDialog::OnInitDialog();

        m_hAccel = LoadAccelerators(AfxGetInstanceHandle(), MAKEINTRESOURCE(IDR_ACCELERATOR_CONFIRM));
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        e->ReportError(MB_ICONERROR);
        e->Delete();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

BOOL CConfirmDialog::PreTranslateMessage(MSG* pMsg)
{
    if (TranslateAccelerator(GetSafeHwnd(), m_hAccel, pMsg))
        return TRUE;

    return CDialog::PreTranslateMessage(pMsg);
}

void CConfirmDialog::OnOptions()
{
    if (m_bUseFiles)
        eraserShowOptions(GetSafeHwnd(), ERASER_PAGE_FILES);
    else
        eraserShowOptions(GetSafeHwnd(), ERASER_PAGE_DRIVE);
}

void CConfirmDialog::OnYes()
{
    CDialog::OnOK();
}

void CConfirmDialog::OnCancel()
{
    CDialog::OnCancel();
}