// EraserDlg.cpp
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
#include "Eraser.h"

#include "Item.h"
#include "EraserUI\FitFileNameToScrn.h"
#include "EraserUI\DriveCombo.h"
#include "shared\FileHelper.h"
#include "EraserDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CEraserDlg dialog


CEraserDlg::CEraserDlg(CWnd* pParent /*=NULL*/) :
CDialog(CEraserDlg::IDD, pParent),
m_bResultsForFiles(TRUE),
m_bResultsForUnusedSpace(TRUE),
m_bResultsOnlyWhenFailed(FALSE),
m_bShowResults(FALSE),
m_ehContext(ERASER_INVALID_CONTEXT),
m_pLockResolver(NULL)
{
    //{{AFX_DATA_INIT(CEraserDlg)
    m_strData = _T("");
    m_strErasing = _T("");
    m_strMessage = _T("");
    m_strPass = _T("");
    m_strPercent = _T("0%");
    m_strPercentTotal = _T("0%");
    m_strTime = _T("");
    //}}AFX_DATA_INIT
}


void CEraserDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CEraserDlg)
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


BEGIN_MESSAGE_MAP(CEraserDlg, CDialog)
    //{{AFX_MSG_MAP(CEraserDlg)
    ON_WM_DESTROY()
    //}}AFX_MSG_MAP
    ON_MESSAGE(WM_ERASERNOTIFY, OnEraserNotify)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CEraserDlg message handlers

void CEraserDlg::OnCancel()
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
    eraserStop(m_ehContext);
}

BOOL CEraserDlg::OnInitDialog()
{
    CDialog::OnInitDialog();

    m_pcProgress.SetRange(0, 100);
    m_pcProgress.SetStep(1);
    m_pcProgress.SetPos(0);

    m_pcProgressTotal.SetRange(0, 100);
    m_pcProgressTotal.SetStep(1);
    m_pcProgressTotal.SetPos(0);

    if (!Erase())
        DestroyWindow();

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

BOOL CEraserDlg::Initialize(CPtrArray *ppaTasks)
{
    try
    {
        if (AfxIsValidAddress(ppaTasks, sizeof(CPtrArray)))
        {
            m_saFiles.RemoveAll();
            m_saFolders.RemoveAll();
            m_saDrives.RemoveAll();
			
            CItem   *piItem = 0;
            CString strData;

            int iSize = ppaTasks->GetSize();

            while (iSize--)
            {
                piItem = static_cast<CItem*>(ppaTasks->GetAt(iSize));
                piItem->GetData(strData);

                switch (piItem->GetType())
                {
                case File:
                    if (piItem->UseWildcards())
                    {
                        findMatchingFiles(strData, m_saFiles,
                                          piItem->WildcardsInSubfolders());
//						CString temp;
//						for(int i = 0; i < m_saFiles.GetCount(); i++) {
//							temp += m_saFiles[i];
//							temp += "\n";
//							AfxMessageBox(temp);
//						}
//						return false;
                    }
                    else
                    {
                        m_saFiles.Add(strData);
                    }
                    break;
                case Folder:
                    {
                        CStringArray saFolders;
                        parseDirectory((LPCTSTR)strData,
                                       m_saFiles,
                                       saFolders,
                                       piItem->Subfolders());

                        if (piItem->RemoveFolder())
                        {
                            if (piItem->OnlySubfolders())
                            {
                                // remove the last folder from the list
                                // since the user does not want to remove
                                // it

                                if (saFolders.GetSize() > 0)
                                    saFolders.SetSize(saFolders.GetSize() - 1);
                            }

                            m_saFolders.InsertAt(0, &saFolders);
                        }
                    }
                    break;
                case Drive:
                    if (strData == DRIVE_ALL_LOCAL)
                        GetLocalHardDrives(m_saDrives);
                    else
                        m_saDrives.Add(strData);
                    break;
				case Mask:
					findMaskedElements(strData, m_saFiles, m_saFolders);
					break;
                default:
                    NODEFAULT;
                }
            }

            return TRUE;
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    return FALSE;
}

BOOL CEraserDlg::Erase()
{
    BOOL bReturn = FALSE;

    if (eraserError(eraserIsValidContext(m_ehContext)))
    {
        if (eraserError(eraserCreateContext(&m_ehContext)))
            return FALSE;
    }

	if (m_pLockResolver)
	{
		m_pLockResolver->SetHandle(m_ehContext);		
	}
	
    // set notification window & message
    VERIFY(eraserOK(eraserSetWindow(m_ehContext, GetSafeHwnd())));
    VERIFY(eraserOK(eraserSetWindowMessage(m_ehContext, WM_ERASERNOTIFY)));
	eraserSetFinishAction(m_ehContext, m_dwFinishAction);


    // clear possible previous items
    VERIFY(eraserOK(eraserClearItems(m_ehContext)));

    // even if we wouldn't have any files to erase, call eraserStart
    // and the user will see an error message when the Erasing Report
    // is shown

    if (m_saDrives.GetSize() == 0 || m_saFiles.GetSize() > 0)
    {
        // we either have files to erase, or we have nothing
        // to erase...

        if (m_bResultsForFiles)
            m_bShowResults = TRUE;

        VERIFY(eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_FILES)));

        int iSize = m_saFiles.GetSize();
        for (int i = 0; i < iSize; i++)
        {
            VERIFY(eraserOK(eraserAddItem(m_ehContext,
                (LPVOID)(LPCTSTR)m_saFiles[i], (E_UINT16)m_saFiles[i].GetLength())));
        }
        m_saFiles.RemoveAll();

        bReturn = eraserOK(eraserStart(m_ehContext));
    }
    else if (m_saDrives.GetSize() > 0)
    {
        if (m_bResultsForUnusedSpace)
            m_bShowResults = TRUE;

        VERIFY(eraserOK(eraserSetDataType(m_ehContext, ERASER_DATA_DRIVES)));

        int iSize = m_saDrives.GetSize();
        for (int i = 0; i < iSize; i++)
        {
            VERIFY(eraserOK(eraserAddItem(m_ehContext,
                (LPVOID)(LPCTSTR)m_saDrives[i], (E_UINT16)m_saDrives[i].GetLength())));
        }
        m_saDrives.RemoveAll();

		
        bReturn = eraserOK(eraserStart(m_ehContext));
    }

    return bReturn;
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
    ERASER_DATA_TYPE edt;
    VERIFY(eraserOK(eraserGetDataType(m_ehContext, &edt)));

    if (edt == ERASER_DATA_FILES)
        m_strErasing = "Files";
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

    // percent
    if (eraserOK(eraserProgGetPercent(m_ehContext, &uValue)))
    {
        strPercent.Format("%u%%", uValue);
        m_pcProgress.SetPos(uValue);
    }

    // total percent
    if (eraserOK(eraserProgGetTotalPercent(m_ehContext, &uValue)))
    {
        strPercentTotal.Format("%u%%", uValue);
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
                strPass.Format("%u of %u", current, passes);
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
                    strTime.Format("%u minutes left", uTimeLeft);
                }
                else if (uTimeLeft > 0)
                {
                    if (uTimeLeft % 5)
                        strTime = m_strTime;
                    else
                        strTime.Format("%u seconds left", uTimeLeft);
                }
            }
        }
    }

    // message
    if (eraserOK(eraserProgGetMessage(m_ehContext, (LPVOID)szValue, &uSize)))
        strMessage = szValue;

    // update only if necessary to minimize flickering
    if (m_strPercent != strPercent || strPercentTotal != m_strPercentTotal ||
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

	
    // remove folders
    int iSize = m_saFolders.GetSize();
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

    E_UINT8 uTerminated = 0;
    BOOL bTerminated = eraserOK(eraserTerminated(m_ehContext, &uTerminated)) && uTerminated;

    // continue with unused disk space?
    if (!bTerminated && m_saDrives.GetSize() > 0)
        Erase();
    else
    {
        E_UINT32 uFailed = 0;
        E_UINT16 uErrors = 0;
        eraserFailedCount(m_ehContext, &uFailed);
        eraserErrorStringCount(m_ehContext, &uErrors);

        if (m_bShowResults && (!m_bResultsOnlyWhenFailed || (uFailed > 0 || uErrors > 0)))
            eraserShowReport(m_ehContext, GetSafeHwnd());

        if (bTerminated)
            CDialog::OnCancel();
        else
            CDialog::OnOK();
    }

    return TRUE;
}

void CEraserDlg::OnDestroy()
{
	if (m_pLockResolver)
	{
		m_pLockResolver->Close();
		m_pLockResolver = NULL;
	}
    eraserDestroyContext(m_ehContext);
    m_ehContext = ERASER_INVALID_CONTEXT;
    CDialog::OnDestroy();
}
