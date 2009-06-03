// WipeProgDlg.cpp
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
#include "Erasext.h"
#include "..\EraserDll\eraserdll.h"
#include "..\EraserUI\FitFileNameToScrn.h"
#include "..\shared\key.h"


#include "resource.h"
#include "WipeProgDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CEraserDlg dialog

// General Preferences
static BOOL getAskUserParam()
{
	BOOL bResolveAskUser = TRUE;
	CKey kReg;
	if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
	{
		kReg.GetValue(bResolveAskUser, _T("EraserResolveLockAskUser"), TRUE);	
		kReg.Close();
	}	
	return bResolveAskUser; 
}

CEraserDlg::CEraserDlg(CWnd* pParent /*=NULL*/) :
CDialog(CEraserDlg::IDD, pParent),
m_hAccel(NULL),
m_bUseFiles(TRUE),
m_bMove(FALSE),
m_bShowResults(TRUE),
m_ehContext(ERASER_INVALID_CONTEXT),
m_LockResolver(getAskUserParam())
{
    //{{AFX_DATA_INIT(CEraserDlg)
    m_strPercent = _T("0%");
    m_strPercentTotal = _T("0%");
    m_strData = _T("");
    m_strErasing = _T("");
    m_strPass = _T("");
    m_strTime = _T("");
    m_strMessage = _T("");
    m_bResults = FALSE;
    //}}AFX_DATA_INIT

}


void CEraserDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CEraserDlg)
    DDX_Control(pDX, IDC_PROGRESS, m_pcProgress);
    DDX_Control(pDX, IDC_PROGRESS_TOTAL, m_pcProgressTotal);
    DDX_Text(pDX, IDC_STATIC_PERCENT, m_strPercent);
    DDX_Text(pDX, IDC_STATIC_PERCENT_TOTAL, m_strPercentTotal);
    DDX_Text(pDX, IDC_STATIC_DATA, m_strData);
    DDX_Text(pDX, IDC_STATIC_ERASING, m_strErasing);
    DDX_Text(pDX, IDC_STATIC_PASS, m_strPass);
    DDX_Text(pDX, IDC_STATIC_TIME, m_strTime);
    DDX_Text(pDX, IDC_STATIC_MESSAGE, m_strMessage);
    DDX_Check(pDX, IDC_CHECK_RESULTS, m_bResults);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CEraserDlg, CDialog)
    //{{AFX_MSG_MAP(CEraserDlg)
    ON_WM_DESTROY()
	ON_BN_CLICKED(IDC_CHECK_RESULTS, OnCheckResults)
	//}}AFX_MSG_MAP
    ON_MESSAGE(WM_ERASERNOTIFY, OnEraserNotify)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CEraserDlg message handlers

BOOL CEraserDlg::OnInitDialog()
{
    CDialog::OnInitDialog();

    CKey kReg;

    if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
    {
        kReg.GetValue(m_bResults, ERASEXT_REGISTRY_RESULTS, TRUE);
        kReg.GetValue(m_bResultsForFiles, ERASER_REGISTRY_RESULTS_FILES, TRUE);
        kReg.GetValue(m_bResultsForUnusedSpace, ERASER_REGISTRY_RESULTS_UNUSEDSPACE, TRUE);
        kReg.GetValue(m_bResultsOnlyWhenFailed, ERASER_REGISTRY_RESULTS_WHENFAILED, TRUE);
        kReg.Close();
    }

    if (!m_bShowResults)
    {
        m_bResults = FALSE;
        GetDlgItem(IDC_CHECK_RESULTS)->ShowWindow(SW_HIDE);
    }

    UpdateData(FALSE);

    m_hAccel = LoadAccelerators(AfxGetInstanceHandle(), MAKEINTRESOURCE(IDR_ACCELERATOR_PROG));

    m_pcProgress.SetRange(0, 100);
    m_pcProgress.SetStep(1);
    m_pcProgress.SetPos(0);

    m_pcProgressTotal.SetRange(0, 100);
    m_pcProgressTotal.SetStep(1);
    m_pcProgressTotal.SetPos(0);

    // starts the thread
    Erase();

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CEraserDlg::Erase()
{
    if (eraserError(eraserIsValidContext(m_ehContext)) && m_saData.GetSize() > 0)
    {
        if (eraserOK(eraserCreateContext(&m_ehContext)))
        {
			m_LockResolver.SetHandle(m_ehContext);

            if (m_bUseFiles)
                VERIFY(eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_FILES)));
            else
                VERIFY(eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_DRIVES)));

            INT_PTR iSize = m_saData.GetSize();
            for (int i = 0; i < iSize; i++)
            {
                VERIFY(eraserOK(eraserAddItem(m_ehContext,
                    (LPVOID)(LPCTSTR)m_saData[i], (E_UINT16)m_saData[i].GetLength())));
            }
            m_saData.RemoveAll();

            // set notification window & message
            VERIFY(eraserOK(eraserSetWindow(m_ehContext, GetSafeHwnd())));
            VERIFY(eraserOK(eraserSetWindowMessage(m_ehContext, WM_ERASERNOTIFY)));

            // start erasing (the library will launch a new thread for this)
            VERIFY(eraserOK(eraserStart(m_ehContext)));
        }
    }
}

LRESULT CEraserDlg::OnEraserNotify(WPARAM wParam, LPARAM)
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

BOOL CEraserDlg::EraserWipeBegin()
{
    UpdateData(TRUE);

    if (m_bUseFiles)
    {
        if (!m_bMove)
            m_strErasing = "Files";
        else
            m_strErasing = "Source Files";
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

BOOL CEraserDlg::EraserWipeUpdate()
{
    TCHAR    szValue[255];
    E_UINT16 uSize = 255;
    E_UINT8  uValue = 0;
    CString  strPercent, strPercentTotal, strTime, strPass, strMessage;

    UpdateData(TRUE);

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

BOOL CEraserDlg::EraserWipeDone()
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

    // show results
    E_UINT32 uFailed = 0;
    E_UINT16 uErrors = 0;
    eraserFailedCount(m_ehContext, &uFailed);
    eraserErrorStringCount(m_ehContext, &uErrors);

    if (m_bResults)
    {
        if (((m_bUseFiles && m_bResultsForFiles) ||
             (!m_bUseFiles && m_bResultsForUnusedSpace)) &&
            (!m_bResultsOnlyWhenFailed || (uFailed > 0 || uErrors > 0)))
        {
            eraserShowReport(m_ehContext, GetSafeHwnd());
        }
    }

    // success
    E_UINT8 uSuccess = 0;
    if (eraserOK(eraserCompleted(m_ehContext, &uSuccess)) && uSuccess)
        CDialog::OnOK();
    else
        CDialog::OnCancel();

    return TRUE;
}


void CEraserDlg::OnDestroy()
{
    UpdateData(TRUE);

    if (m_bShowResults)
    {
        CKey kReg;

        if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
        {
            kReg.SetValue(m_bResults, ERASEXT_REGISTRY_RESULTS);
            kReg.Close();
        }
    }

	m_LockResolver.Close();

    eraserDestroyContext(m_ehContext);
    m_ehContext = ERASER_INVALID_CONTEXT;

    CDialog::OnDestroy();
}

BOOL CEraserDlg::PreTranslateMessage(MSG* pMsg)
{
    if (TranslateAccelerator(GetSafeHwnd(), m_hAccel, pMsg))
        return TRUE;

    return CDialog::PreTranslateMessage(pMsg);
}

void CEraserDlg::OnCancel()
{
    m_strMessage = _T("Terminating...");
    m_strPercent.Empty();
    m_strPercentTotal.Empty();
    m_strPass.Empty();
    m_strTime.Empty();
    m_strData.Empty();
    m_pcProgress.SetPos(0);
    m_pcProgressTotal.SetPos(0);
    UpdateData(FALSE);

    GetDlgItem(IDCANCEL)->EnableWindow(FALSE);
    eraserStop(m_ehContext);
}

void CEraserDlg::OnCheckResults()
{
	UpdateData(TRUE);
}