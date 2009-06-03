// ReportDialog.cpp
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
#include "ReportDialog.h"
#include "..\shared\Utils.h"
//#include "..\shared\FileDialogEx.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CReportDialog dialog


CReportDialog::CReportDialog(CWnd* pParent /*=NULL*/) :
CDialog(CReportDialog::IDD, pParent),
m_pstraErrorArray(0)
{
    //{{AFX_DATA_INIT(CReportDialog)
    m_strStatistics = _T("");
    m_strCompletion = _T("");
    //}}AFX_DATA_INIT
}


void CReportDialog::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CReportDialog)
    DDX_Control(pDX, IDC_LIST_ERRORS, m_listErrors);
    DDX_Text(pDX, IDC_EDIT_STATISTICS, m_strStatistics);
    DDX_Text(pDX, IDC_STATIC_COMPLETION, m_strCompletion);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CReportDialog, CDialog)
    //{{AFX_MSG_MAP(CReportDialog)
    ON_BN_CLICKED(IDC_BUTTON_SAVEAS, OnSaveAs)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CReportDialog message handlers

const LPCTSTR szFileExtension   = _T("txt");
const LPCTSTR szFileFilter      = _T("Text Files (*.txt)|*.txt||");
const LPCTSTR szSaveTitle       = _T("Save Report As");

const LPCTSTR szInformation     = _T("Information:");
const LPCTSTR szFailures        = _T("Failures:");

void CReportDialog::OnSaveAs()
{
    // Was CfileDialogEx now with MFC7 we can change back to MFC Class
	CFileDialog fd(FALSE,
                     _T("txt"),
                     _T("*.txt"),//NULL,
                     OFN_EXPLORER | OFN_PATHMUSTEXIST |OFN_ENABLESIZING |OFN_NODEREFERENCELINKS | OFN_FILEMUSTEXIST | OFN_SHOWHELP | OFN_OVERWRITEPROMPT,
                     szFileFilter,
                     AfxGetMainWnd());
    fd.m_ofn.lpstrTitle = szSaveTitle;

    if (fd.DoModal() == IDOK)
    {
        CString strFile = fd.GetPathName();
        CString strTemp;
		INT_PTR uIndex, uSize;
        CStdioFile file;

        if (file.Open((LPCTSTR)strFile, CFile::modeCreate | CFile::modeWrite))
        {
            try
            {
                // information
                strTemp.Format(_T("%s\n  "), szInformation);
                file.WriteString(strTemp);
                file.WriteString(m_strStatistics);

                // failures
                if (AfxIsValidAddress(m_pstraErrorArray, sizeof(CStringArray)) &&
                    m_pstraErrorArray->GetSize() > 0)
                {
                    strTemp.Format(_T("\n\n%s\n"), szFailures);
                    file.WriteString(strTemp);

                    uSize = m_pstraErrorArray->GetSize();
                    for (uIndex = 0; uIndex < uSize; uIndex++)
                    {
                        strTemp = _T("  ") + m_pstraErrorArray->GetAt(uIndex) + _T("\n");
                        file.WriteString(strTemp);
                    }
                }
            }
            catch (CException *e)
            {
                e->ReportError(MB_ICONERROR);
                e->Delete();
            }

            file.Close();
        }
    }
}

BOOL CReportDialog::OnInitDialog()
{
    CDialog::OnInitDialog();

    // create list
    CRect rClient;
    m_listErrors.GetClientRect(&rClient);

    int iColumnWidths[2];

    iColumnWidths[0] = 30;
    iColumnWidths[1] = rClient.Width() -
                       iColumnWidths[0];
                       // - 2 * GetSystemMetrics(SM_CXBORDER);

    LVCOLUMN lvc;
    ZeroMemory(&lvc, sizeof(LVCOLUMN));

    lvc.mask        = LVCF_FMT | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
    lvc.fmt         = LVCFMT_LEFT;
    lvc.pszText     = _T("#");
    lvc.cx          = iColumnWidths[0];
    lvc.iSubItem    = 0;
    m_listErrors.InsertColumn(0, &lvc);

    lvc.mask        = LVCF_FMT | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
    lvc.fmt         = LVCFMT_LEFT;
    lvc.pszText     = _T("Item");
    lvc.cx          = iColumnWidths[1];
    lvc.iSubItem    = 1;
    m_listErrors.InsertColumn(1, &lvc);

    m_listErrors.SetExtendedStyle(LVS_EX_FULLROWSELECT);

    // fill list
    if (AfxIsValidAddress(m_pstraErrorArray, sizeof(CStringArray)))
    {
        INT_PTR iSize = m_pstraErrorArray->GetSize();

        if (iSize > 0)
        {
            m_listErrors.SetRedraw(FALSE);

            try
            {
                m_listErrors.DeleteAllItems();

                CString strTemp;
                int i;
                int iMaxWidth = iColumnWidths[1];
                int iStringWidth;
                LV_ITEM lvi;
                ZeroMemory(&lvi, sizeof(LV_ITEM));

                for (i = 0; i < iSize; i++)
                {
                    strTemp.Format(_T("%i"), i + 1);
                    lvi.mask        = LVIF_TEXT;
                    lvi.iItem       = i;
                    lvi.iSubItem    = 0;
                    lvi.pszText     = strTemp.GetBuffer(strTemp.GetLength());
                    lvi.iItem       = m_listErrors.InsertItem(&lvi);
                    strTemp.ReleaseBuffer();

                    strTemp = m_pstraErrorArray->GetAt(i);

                    iStringWidth = m_listErrors.GetStringWidth(strTemp);
                    if (iStringWidth > iMaxWidth)
                        iMaxWidth = iStringWidth;

                    lvi.mask        = LVIF_TEXT;
                    lvi.iSubItem    = 1;
                    lvi.pszText     = strTemp.GetBuffer(strTemp.GetLength());
                    m_listErrors.SetItem(&lvi);
                    strTemp.ReleaseBuffer();
                }

                if (iMaxWidth > iColumnWidths[1])
                    m_listErrors.SetColumnWidth(1, iMaxWidth + GetSystemMetrics(SM_CXVSCROLL));
                else
                {
                    CRect rHeader;
                    CSize size = m_listErrors.ApproximateViewRect();
                    m_listErrors.GetHeaderCtrl()->GetClientRect(&rHeader);

                    if (size.cy > (rClient.Height() + rHeader.Height()))
                        m_listErrors.SetColumnWidth(1, iColumnWidths[1] - GetSystemMetrics(SM_CXVSCROLL));
                }
            }
            catch (...)
            {
                ASSERT(0);
            }

            m_listErrors.SetRedraw(TRUE);
        }
#ifdef HIDE_FAILURE_LIST_IF_EMPTY
        else
        {
            int iMove;
            CRect rectItem;

            CWnd *pWnd = GetDlgItem(IDC_EDIT_STATISTICS);

            // calc. distance between the error list and the info edit
            pWnd->GetWindowRect(&rectItem);
            ScreenToClient(&rectItem);
            iMove = rectItem.bottom;

            m_listErrors.GetWindowRect(&rectItem);
            ScreenToClient(&rectItem);
            iMove = rectItem.bottom - iMove;

            // hide & move the list
            m_listErrors.ShowWindow(SW_HIDE);
            m_listErrors.SetWindowPos(NULL, rectItem.left, rectItem.top - iMove, 0, 0,
                                      SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);

            // hide & move the header
            pWnd = GetDlgItem(IDC_STATIC_FAILURES_HEADER);
            pWnd->ShowWindow(SW_HIDE);

            pWnd->GetWindowRect(&rectItem);
            ScreenToClient(&rectItem);

            pWnd->SetWindowPos(NULL, rectItem.left, rectItem.top - iMove, 0, 0,
                               SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

            // and the buttons
            pWnd = GetDlgItem(IDCANCEL);
            pWnd->GetWindowRect(&rectItem);
            ScreenToClient(&rectItem);

            pWnd->SetWindowPos(NULL, rectItem.left, rectItem.top - iMove, 0, 0,
                               SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

            pWnd = GetDlgItem(IDC_BUTTON_SAVEAS);
            pWnd->GetWindowRect(&rectItem);
            ScreenToClient(&rectItem);

            pWnd->SetWindowPos(NULL, rectItem.left, rectItem.top - iMove, 0, 0,
                               SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

            // finally, resize the window
            GetWindowRect(&rectItem);
            SetWindowPos(NULL, 0, 0, rectItem.Width(), rectItem.Height() - iMove,
                         SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
#endif
    }

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}
