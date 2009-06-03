// LauncherDlg.cpp
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
#include "..\EraserDll\EraserDll.h"
#include "..\EraserUI\FitFileNameToScrn.h"
#include "..\EraserUI\DriveCombo.h"
#include "..\shared\FileHelper.h"
#include "Launcher.h"
#include "LauncherDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CLauncherDlg dialog

CLauncherDlg::CLauncherDlg(CWnd* pParent /*=NULL*/) :
CDialog(CLauncherDlg::IDD, pParent),
m_bUseFiles(FALSE),
m_bFolders(FALSE),
m_bSubFolders(FALSE),
m_bKeepFolder(FALSE),
m_bUseEmptySpace(FALSE),
m_bResults(FALSE),
m_bResultsOnError(FALSE),
m_bRecycled(FALSE),
m_emMethod(ERASER_METHOD_PSEUDORANDOM /*ERASER_METHOD_LIBRARY*/),
m_uPasses(1),
m_ehContext(ERASER_INVALID_CONTEXT)
{
    //{{AFX_DATA_INIT(CLauncherDlg)
    m_strData = _T("");
    m_strErasing = _T("");
    m_strMessage = _T("");
    m_strPass = _T("");
    m_strPercent = _T("0%");
    m_strPercentTotal = _T("0%");
    m_strTime = _T("");
    //}}AFX_DATA_INIT
    // Note that LoadIcon does not require a subsequent DestroyIcon in Win32
    m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CLauncherDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CLauncherDlg)
    DDX_Control(pDX, IDC_PROGRESS, m_pcProgress);
    DDX_Control(pDX, IDC_PROGRESS_TOTAL, m_pcProgressTotal);
    DDX_Text(pDX, IDC_STATIC_DATA, m_strData);
    DDX_Text(pDX, IDC_STATIC_ERASING, m_strErasing);
    DDX_Text(pDX, IDC_STATIC_MESSAGE, m_strMessage);
    DDX_Text(pDX, IDC_STATIC_PASS, m_strPass);
    DDX_Text(pDX, IDC_STATIC_PERCENT, m_strPercent);
    DDX_Text(pDX, IDC_STATIC_PERCENT_TOTAL, m_strPercentTotal);
    DDX_Text(pDX, IDC_STATIC_TIME, m_strTime);
    //}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CLauncherDlg, CDialog)
    //{{AFX_MSG_MAP(CLauncherDlg)
    ON_WM_PAINT()
    ON_WM_QUERYDRAGICON()
    ON_WM_DESTROY()
    //}}AFX_MSG_MAP
    ON_MESSAGE(WM_ERASERNOTIFY, OnEraserNotify)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CLauncherDlg message handlers

BOOL CLauncherDlg::OnInitDialog()
{
    CDialog::OnInitDialog();

    // Set the icon for this dialog.  The framework does this automatically
    //  when the application's main window is not a dialog
    SetIcon(m_hIcon, TRUE);         // Set big icon
    SetIcon(m_hIcon, FALSE);        // Set small icon

    m_pcProgress.SetRange(0, 100);
    m_pcProgress.SetStep(1);
    m_pcProgress.SetPos(0);

    m_pcProgressTotal.SetRange(0, 100);
    m_pcProgressTotal.SetStep(1);
    m_pcProgressTotal.SetPos(0);

    return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CLauncherDlg::OnPaint()
{
    if (IsIconic())
    {
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
    }
    else
    {
        CDialog::OnPaint();
    }
}

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CLauncherDlg::OnQueryDragIcon()
{
    return (HCURSOR) m_hIcon;
}

void CLauncherDlg::Options()
{
    eraserShowOptions(GetSafeHwnd(), ERASER_PAGE_FILES);
}

BOOL CLauncherDlg::Erase()
{
    BOOL bReturn = FALSE;

    if (eraserError(eraserIsValidContext(m_ehContext)))
    {
        if (eraserOK(eraserCreateContextEx(&m_ehContext, convEraseMethod(m_emMethod), m_uPasses, 0)))
        {
            if (m_bUseFiles)
            {
                VERIFY(eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_FILES)));

                if (m_bFolders)
                {
                    m_strMessage = "Searching...";
                    UpdateData(FALSE);

                    CString strTemp(m_saFiles[0]);
                    m_saFiles.RemoveAll();

                    parseDirectory((LPCTSTR)strTemp,
                                   m_saFiles,
                                   m_saFolders,
                                   m_bSubFolders);

                    if (m_bKeepFolder)
                    {
                        // remove the last folder from the list
                        // since the user does not want to remove it
                        if (m_saFolders.GetSize() > 0)
                            m_saFolders.SetSize(m_saFolders.GetSize() - 1);
                    }
                }
            }
            else
            {
                VERIFY(eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_DRIVES)));

                if (m_saFiles[0] == szDiskAll)
                    GetLocalHardDrives(m_saFiles);
            }

            INT_PTR iSize = m_saFiles.GetSize();

            if (iSize > 0)
            {
                for (int i = 0; i < iSize; i++)
                {
                    VERIFY(eraserOK(eraserAddItem(m_ehContext,
                        (LPVOID)(LPCTSTR)m_saFiles[i], (E_UINT16)m_saFiles[i].GetLength())));
                }
                m_saFiles.RemoveAll();

                // set notification window & message
                VERIFY(eraserOK(eraserSetWindow(m_ehContext, GetSafeHwnd())));
                VERIFY(eraserOK(eraserSetWindowMessage(m_ehContext, WM_ERASERNOTIFY)));

                // start erasing (the library will launch a new thread for this)
                bReturn = eraserOK(eraserStart(m_ehContext));
            }
            else
            {
                bReturn = TRUE;
                EraserWipeDone();
            }
        }
    }

    if (!bReturn)
        DestroyWindow();

    return bReturn;
}

void CLauncherDlg::OnCancel()
{
    m_strMessage = "Terminating...";
    m_strPercent.Empty();
    m_strPercentTotal.Empty();
    m_strPass.Empty();
    m_strTime.Empty();
    m_strData.Empty();
    m_pcProgress.SetPos(0);
    m_pcProgressTotal.SetPos(0);
    UpdateData(FALSE);

    GetDlgItem(IDCANCEL)->EnableWindow(FALSE);

    // this will take care of stopping the thread as well
    eraserDestroyContext(m_ehContext);
    m_ehContext = ERASER_INVALID_CONTEXT;
}

LRESULT CLauncherDlg::OnEraserNotify(WPARAM wParam, LPARAM)
{
    switch (wParam)
    {
    case ERASER_WIPE_BEGIN:
        EraserWipeBegin();
        break;
    case ERASER_WIPE_UPDATE:
        EraserWipeUpdate();
        break;
    case ERASER_WIPE_DONE:
        EraserWipeDone();
        break;
    }

    return TRUE;
}

BOOL CLauncherDlg::EraserWipeBegin()
{
    if (m_bUseFiles)
    {
        if (!m_bRecycled)
            m_strErasing = "Files";
        else
            m_strErasing = "Recycle Bin";
    }
    else
        m_strErasing = "Unused disk space";

    TCHAR    szValue[255];
    E_UINT16 uSize = 255;
    E_UINT8  uValue = 0;

    // data
    if (eraserOK(eraserProgGetCurrentDataString(m_ehContext, (LPVOID)szValue, &uSize)))
        m_strData = szValue;
    fitFileNameToScrn(GetDlgItem(IDC_STATIC_DATA), m_strData);

    // message
    if (eraserOK(eraserProgGetMessage(m_ehContext, (LPVOID)szValue, &uSize)))
        m_strMessage = szValue;

    // progress
    if (eraserOK(eraserDispFlags(m_ehContext, &uValue)))
    {
        if (bitSet(uValue, eraserDispInit))
        {
            m_pcProgress.SetPos(0);
            m_strPercent = "0%";
        }

        // pass
        if (!bitSet(uValue, eraserDispPass))
            m_strPass.Empty();

        // time
        if (!bitSet(uValue, eraserDispTime))
            m_strTime.Empty();
    }

    UpdateData(FALSE);

    return TRUE;
}

BOOL CLauncherDlg::EraserWipeUpdate()
{
    TCHAR    szValue[255];
    E_UINT16 uSize = 255;
    E_UINT8  uValue = 0;
    CString  strPercent, strPercentTotal, strTime, strPass, strMessage;

    // percent
    if (eraserOK(eraserProgGetPercent(m_ehContext, &uValue)))
    {
        strPercent.Format(_T("%u%%"), uValue);
        m_pcProgress.SetPos(uValue);
    }

    // total percent
    if (eraserOK(eraserProgGetTotalPercent(m_ehContext, &uValue)))
    {
        strPercentTotal.Format(_T("%u%%"), uValue);
        m_pcProgressTotal.SetPos(uValue);
    }

    // pass
    if (eraserOK(eraserDispFlags(m_ehContext, &uValue)))
    {
        if (bitSet(uValue, eraserDispPass))
        {
            E_UINT16 current = 0, passes = 0;
            if (eraserOK(eraserProgGetCurrentPass(m_ehContext, &current)) &&
                eraserOK(eraserProgGetPasses(m_ehContext, &passes)))
            {
                strPass.Format(_T("%u of %u"), current, passes);
            }
        }

        // show time?
        if (bitSet(uValue, eraserDispTime))
        {
            // time left
            E_UINT32 uTimeLeft = 0;
            if (eraserOK(eraserProgGetTimeLeft(m_ehContext, &uTimeLeft)))
            {
                if (uTimeLeft > 120)
                {
                    uTimeLeft = (uTimeLeft / 60) + 1;
                    strTime.Format(_T("%u minutes left"), uTimeLeft);
                }
                else if (uTimeLeft > 0)
                {
                    if (uTimeLeft % 5)
                        strTime = m_strTime;
                    else
                        strTime.Format(_T("%u seconds left"), uTimeLeft);
                }
            }
        }
    }

    // message
    if (eraserOK(eraserProgGetMessage(m_ehContext, (LPVOID)szValue, &uSize)))
        strMessage = szValue;

    // update only if necessary to minimize flickering
    if (m_strPercent != strPercent || m_strPercentTotal != strPercentTotal ||
        m_strPass != strPass || m_strTime != strTime || m_strMessage != strMessage)
    {
        m_strPercent = strPercent;
        m_strPercentTotal = strPercentTotal;
        m_strPass = strPass;
        m_strTime = strTime;
        m_strMessage = strMessage;

        UpdateData(FALSE);
    }

    return TRUE;
}

BOOL CLauncherDlg::EraserWipeDone()
{
    // clear display
    m_strMessage.Empty();
    m_strPercent.Empty();
    m_strPercentTotal.Empty();
    m_strPass.Empty();
    m_strTime.Empty();
    m_strData.Empty();
    m_pcProgress.SetPos(0);
    m_pcProgressTotal.SetPos(0);
    UpdateData(FALSE);

    // remove folders
    INT_PTR iSize = m_saFolders.GetSize();
    if (iSize > 0)
    {
        for (int i = 0; i < iSize; i++)
        {
            if (eraserOK(eraserRemoveFolder((LPVOID)(LPCTSTR)m_saFolders[i],
                    (E_UINT16)m_saFolders[i].GetLength(), ERASER_REMOVE_FOLDERONLY)))
            {
                SHChangeNotify(SHCNE_RMDIR, SHCNF_PATH, (LPCTSTR)m_saFolders[i], NULL);
            }
        }

        m_saFolders.RemoveAll();
    }

    // empty recycle bin
    E_UINT8 uTerminated = 0;
    if (eraserOK(eraserCompleted(m_ehContext, &uTerminated)) && m_bRecycled)
    {
        HINSTANCE hShell = AfxLoadLibrary(szShell32);

        if (hShell)
        {
            try
            {
                SHEMPTYRECYCLEBIN pSHEmptyRecycleBin =
                    (SHEMPTYRECYCLEBIN)GetProcAddress(hShell, szSHEmptyRecycleBin);

                if (pSHEmptyRecycleBin)
                    pSHEmptyRecycleBin(NULL, NULL,
                        SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND );
            }
            catch (CException *e)
            {
                e->ReportError(MB_ICONERROR);
                e->Delete();
            }

            AfxFreeLibrary(hShell);
        }
    }

    // show results
    E_UINT32 uFailed = 0;
    E_UINT16 uErrors = 0;
    eraserFailedCount(m_ehContext, &uFailed);
    eraserErrorStringCount(m_ehContext, &uErrors);

    if (m_bResults && (!m_bResultsOnError || (uFailed > 0 || uErrors > 0)))
        eraserShowReport(m_ehContext, GetSafeHwnd());

    // and we're done
    DestroyWindow();
    return TRUE;
}

void CLauncherDlg::OnDestroy()
{
    // this will take care of stopping the thread if needed
    eraserDestroyContext(m_ehContext);
    m_ehContext = ERASER_INVALID_CONTEXT;
    // shut down
    CDialog::OnDestroy();
    PostQuitMessage(0);
}