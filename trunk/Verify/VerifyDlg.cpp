// VerifyDlg.cpp
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2007 The Eraser Project
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
#include "..\EraserDll\EraserDll.h"
#include "..\EraserUI\NewDialog.h"
#include "Verify.h"
#include "VerifyDlg.h"
#include "ViewerDlg.h"
#include "resource.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

#define TIMER_CONTINUE  100
const DWORD uWait = 2; // seconds

const LPCTSTR szProgress = "Now overwriting pass %u / %u (%u%% completed)";

/////////////////////////////////////////////////////////////////////////////
// CVerifyDlg dialog

CVerifyDlg::CVerifyDlg(CWnd* pParent /*=NULL*/) :
CDialog(CVerifyDlg::IDD, pParent),
m_pfSave(0),
m_ehContext(ERASER_INVALID_CONTEXT),
m_bFileSelected(FALSE),
m_bTerminated(FALSE)
{
	//{{AFX_DATA_INIT(CVerifyDlg)
	m_strFileName = _T("");
	m_strProgress = _T("");
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CVerifyDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CVerifyDlg)
	DDX_Text(pDX, IDC_EDIT_FILE, m_strFileName);
	DDX_Text(pDX, IDC_STATIC_PROGRESS, m_strProgress);
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CVerifyDlg, CDialog)
	//{{AFX_MSG_MAP(CVerifyDlg)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnButtonBrowse)
	ON_BN_CLICKED(IDC_BUTTON_ERASE, OnButtonErase)
	ON_BN_CLICKED(IDC_BUTTON_METHOD, OnButtonMethod)
	ON_BN_CLICKED(IDC_BUTTON_STOP, OnButtonStop)
	ON_WM_TIMER()
	ON_WM_DESTROY()
	//}}AFX_MSG_MAP
    ON_MESSAGE(WM_ERASERNOTIFY, OnEraserNotify)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CVerifyDlg message handlers

BOOL CVerifyDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

    // create and set bold font
    LOGFONT lgFont;
    ZeroMemory(&lgFont, sizeof(LOGFONT));

    m_pfSave = GetDlgItem(IDC_STATIC_STEP1)->GetFont();
    m_pfSave->GetLogFont(&lgFont);
    lgFont.lfWeight = FW_BOLD;

    m_fBold.CreateFontIndirect(&lgFont);
    GetDlgItem(IDC_STATIC_STEP1)->SetFont(&m_fBold);
    GetDlgItem(IDC_STATIC_PROGRESS)->SetFont(&m_fBold);


	GetDlgItem(IDC_BUTTON_ERASE)->EnableWindow(FALSE);
    GetDlgItem(IDC_BUTTON_STOP)->EnableWindow(FALSE);

	return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CVerifyDlg::OnPaint()
{
	if (IsIconic()) {
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
    } else {
		CDialog::OnPaint();
	}
}

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CVerifyDlg::OnQueryDragIcon()
{
	return (HCURSOR) m_hIcon;
}

void CVerifyDlg::OnButtonBrowse()
{
   /* CFileDialogEx fdlg(TRUE, NULL, NULL,
                       OFN_PATHMUSTEXIST |OFN_NODEREFERENCELINKS | OFN_FILEMUSTEXIST | OFN_SHOWHELP |OFN_EXPLORER,
					   "All Files (*.*) | *.*||", this);
	fdlg.m_ofn.lpstrTitle = "Select File to be Erased";*/
UpdateData(TRUE);
	CNewDialog fdlg;
    if (fdlg.DoModal() == IDOK) {
        m_strFileName = fdlg.m_sPath;		//fdlg.GetPathName();

        if (!m_bFileSelected) {
            // move to step 2
            GetDlgItem(IDC_STATIC_STEP1)->SetFont(m_pfSave);
            m_pfSave = GetDlgItem(IDC_STATIC_STEP2)->GetFont();
            GetDlgItem(IDC_STATIC_STEP2)->SetFont(&m_fBold);
            // enable "Erase" button
            GetDlgItem(IDC_BUTTON_ERASE)->EnableWindow(TRUE);
            // clear possible message
            m_strProgress.Empty();
        }

        UpdateData(FALSE);
        m_bFileSelected = TRUE;
    }
}

void CVerifyDlg::OnButtonErase()
{
    // only one click, thanks
	GetDlgItem(IDC_BUTTON_ERASE)->EnableWindow(FALSE);

    // no changing preferences at this point
    GetDlgItem(IDC_BUTTON_METHOD)->EnableWindow(FALSE);

    BOOL bSuccess = FALSE;

    // start erasing
    if (eraserError(eraserIsValidContext(m_ehContext)) && !m_strFileName.IsEmpty()) {
        if (eraserOK(eraserCreateContext(&m_ehContext))) {
            // set data type
            if (eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_FILES))) {
                // add file to erase
                if (eraserOK(eraserAddItem(m_ehContext, (LPVOID)(LPCTSTR)m_strFileName,
                        (E_UINT16)m_strFileName.GetLength()))) {

                    // set notification window & message and enable test API notifications
                    if (eraserOK(eraserSetWindow(m_ehContext, GetSafeHwnd())) &&
                        eraserOK(eraserSetWindowMessage(m_ehContext, WM_ERASERNOTIFY)) &&
                        eraserOK(eraserTestEnable(m_ehContext))) {

                        // before...
                        CViewerDlg viewer(this);
                        viewer.m_strFileName = m_strFileName;
                        viewer.m_strMessage = "Before Erasing";
                        viewer.DoModal();

                        // start erasing (the library will launch a new thread for this)
                        bSuccess = eraserOK(eraserStart(m_ehContext));
                    }
                }
            }
        }
    }

    if (bSuccess) {
        // move to step 3
        GetDlgItem(IDC_STATIC_STEP2)->SetFont(m_pfSave);
        m_pfSave = GetDlgItem(IDC_STATIC_STEP3)->GetFont();
        GetDlgItem(IDC_STATIC_STEP3)->SetFont(&m_fBold);

        // enable "Stop" button
        GetDlgItem(IDC_BUTTON_STOP)->EnableWindow(TRUE);

        // disable "Close" button
        GetDlgItem(IDCANCEL)->EnableWindow(FALSE);
    } else {
        // back to step 1
        GetDlgItem(IDC_STATIC_STEP2)->SetFont(m_pfSave);
        m_pfSave = GetDlgItem(IDC_STATIC_STEP1)->GetFont();
        GetDlgItem(IDC_STATIC_STEP1)->SetFont(&m_fBold);

        // re-enable preferences button
        GetDlgItem(IDC_BUTTON_METHOD)->EnableWindow(TRUE);

        // clear file name
        m_strFileName.Empty();
        m_bFileSelected = FALSE;

        // destroy possibly created context
        eraserDestroyContext(m_ehContext);
        m_ehContext = ERASER_INVALID_CONTEXT;

        // notify the user
        m_strProgress = "Failed to start erasing.";
        UpdateData(FALSE);
    }
}

void CVerifyDlg::OnButtonMethod()
{
	eraserShowOptions(GetSafeHwnd(), ERASER_PAGE_FILES);
}

void CVerifyDlg::OnButtonStop()
{
    if (eraserOK(eraserIsValidContext(m_ehContext))) {
        GetDlgItem(IDC_BUTTON_STOP)->EnableWindow(FALSE);
        m_bTerminated = TRUE;
        eraserStop(m_ehContext);
    }
}

LRESULT CVerifyDlg::OnEraserNotify(WPARAM wParam, LPARAM)
{
    if (eraserError(eraserIsValidContext(m_ehContext))) {
        return 0;
    }

    switch (wParam) {
    case ERASER_WIPE_BEGIN:
        m_strProgress = "Erasing started...";
        break;
    case ERASER_WIPE_UPDATE:
        {
            E_UINT16 uCurrent = 0, uPasses = 0;
            E_UINT8 uPercent = 0;

            // percent
            if (eraserOK(eraserProgGetPercent(m_ehContext, &uPercent)) &&
                eraserOK(eraserProgGetCurrentPass(m_ehContext, &uCurrent)) &&
                eraserOK(eraserProgGetPasses(m_ehContext, &uPasses))) {
                if (uPasses > 0 && uCurrent > 0) {
                    m_strProgress.Format(szProgress, uCurrent, uPasses,
                        (uPercent - ((uCurrent - 1) * 100 / uPasses)) * uPasses );
                }
            } else {
                m_strProgress = "Erasing...";
            }
        }
        break;
    case ERASER_WIPE_DONE:
        if (!m_bTerminated) {
            m_strProgress = "Erasing completed.";
        } else {
            m_strProgress = "Erasing terminated.";
        }

        // and we're back to step 1
        GetDlgItem(IDC_STATIC_STEP3)->SetFont(m_pfSave);
        m_pfSave = GetDlgItem(IDC_STATIC_STEP1)->GetFont();
        GetDlgItem(IDC_STATIC_STEP1)->SetFont(&m_fBold);

        // re-enable preferences button
        GetDlgItem(IDC_BUTTON_METHOD)->EnableWindow(TRUE);

        // and "Close" button
        GetDlgItem(IDCANCEL)->EnableWindow(TRUE);

        // no more stopping
        GetDlgItem(IDC_BUTTON_STOP)->EnableWindow(FALSE);
        m_bTerminated = FALSE;

        // clear file name
        m_strFileName.Empty();
        m_bFileSelected = FALSE;

        // destroy context
        eraserDestroyContext(m_ehContext);
        m_ehContext = ERASER_INVALID_CONTEXT;

        break;
    case ERASER_TEST_PAUSED:
        {
            TCHAR szCurrentData[_MAX_PATH];
            m_strProgress = "Viewing File Contents...";
            UpdateData(FALSE);

            E_UINT16 uLength = _MAX_PATH, uCurrent = 0, uPasses = 0;

            eraserProgGetCurrentPass(m_ehContext, &uCurrent);
            eraserProgGetPasses(m_ehContext, &uPasses);

            // if the file has alternate data streams and user has opted to
            // erase them, the file name will change at some point
            eraserProgGetCurrentDataString(m_ehContext, (LPVOID)szCurrentData, &uLength);

            try {
                CViewerDlg viewer(this);
                viewer.m_strFileName = szCurrentData;

                if (uCurrent > 0) {
                    viewer.m_strMessage.Format("After Pass %u", uCurrent);
                }
                viewer.DoModal();
            } catch (...) {
                ASSERT(0);
            }

            if (uCurrent < uPasses) {
                // and continue after uWait seconds
                if (!SetTimer(TIMER_CONTINUE, uWait * 1000, NULL)) {
                    eraserTestContinueProcess(m_ehContext);
                }
                m_strProgress = "Starting next overwriting pass...";
            } else {
                eraserTestContinueProcess(m_ehContext);
            }
        }
        break;
    default:
        break;
    }

    UpdateData(FALSE);
    return 1;
}

void CVerifyDlg::OnTimer(UINT_PTR nIDEvent)
{
    if (nIDEvent == TIMER_CONTINUE && eraserOK(eraserIsValidContext(m_ehContext))) {
        KillTimer(TIMER_CONTINUE);
        eraserTestContinueProcess(m_ehContext);
    }
	CDialog::OnTimer(nIDEvent);
}

void CVerifyDlg::OnDestroy()
{
    eraserDestroyContext(m_ehContext);
	CDialog::OnDestroy();
}
