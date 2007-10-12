// EraserDoc.cpp
// $Id$
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
#include "resource.h"
#include "Eraser.h"
#include "Item.h"
#include "EraserDll\EraserDll.h"
#include "shared\key.h"
#include "shared\utils.h"
#include "PreferencesSheet.h"
#include "EraserUI\TimeOutMessageBox.h"
#include "EraserDoc.h"
#include "MainFrm.h"
#include "version.h"

#include <direct.h>

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CEraserDoc



CString findRecycledBinGUID()
{
	const LPCTSTR RBIN_NSPACE = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace";
	CKey kReg;
	BOOL bRes;
	HKEY hReg;
	TCHAR szSubKey[MAX_KEY_LENGTH];
	DWORD dwSubKeyLen = MAX_KEY_LENGTH;
	DWORD dwIndex=0;
	CString strValue=_T(""),strDef=_T(""),strFind=_T("");
	
	bRes = kReg.Open(HKEY_LOCAL_MACHINE,RBIN_NSPACE,FALSE);
	hReg = kReg.GetHandle();
	while (ERROR_SUCCESS == RegEnumKeyEx(hReg,dwIndex,szSubKey,&dwSubKeyLen,NULL,NULL,NULL,NULL))
	{		 
		CKey kTmpReg;
		CString strTmp=_T("");
		strTmp.Format("%s\\%s", RBIN_NSPACE, szSubKey);
		if (kTmpReg.Open(HKEY_LOCAL_MACHINE,strTmp,FALSE)) {
			kTmpReg.GetValue(strValue,strDef,"");
			if (strValue == "Recycle Bin") {
				strFind = szSubKey; 
			}
			dwIndex++;
			dwSubKeyLen = MAX_KEY_LENGTH;			
		}
		kTmpReg.Close();
	}
	kReg.Close();
	return strFind;
}


IMPLEMENT_DYNCREATE(CEraserDoc, CDocument)

BEGIN_MESSAGE_MAP(CEraserDoc, CDocument)
    //{{AFX_MSG_MAP(CEraserDoc)
    ON_UPDATE_COMMAND_UI(ID_FILE_EXPORT, OnUpdateFileExport)
    ON_COMMAND(ID_FILE_EXPORT, OnFileExport)
    ON_COMMAND(ID_FILE_IMPORT, OnFileImport)
    ON_COMMAND(ID_TRAY_ENABLE, OnTrayEnable)
    ON_UPDATE_COMMAND_UI(ID_TRAY_SHOW_WINDOW, OnUpdateTrayShowWindow)
    ON_UPDATE_COMMAND_UI(ID_TRAY_ENABLE, OnUpdateTrayEnable)
    ON_COMMAND(ID_TRAY_SHOW_WINDOW, OnTrayShowWindow)
    ON_COMMAND(ID_EDIT_PREFERENCES_GENERAL, OnEditPreferencesGeneral)
    ON_COMMAND(ID_EDIT_PREFERENCES_ERASER, OnEditPreferencesEraser)
    ON_COMMAND(ID_FILE_VIEW_LOG, OnFileViewLog)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CEraserDoc construction/destruction

CEraserDoc::CEraserDoc() :
m_bResolveLock(TRUE),
m_bResolveAskUser(TRUE),
m_bSchedulerEnabled(TRUE),
m_wProcessCount(0),
m_bResultsForFiles(TRUE),
m_bResultsForUnusedSpace(TRUE),
m_bResultsOnlyWhenFailed(FALSE),
m_bLog(TRUE),
m_bLogOnlyErrors(FALSE),
m_bStartup(TRUE),
m_bQueueTasks(TRUE),
m_bNoVisualErrors(FALSE),
m_dwMaxLogSize(10),
m_bClearSwap(FALSE),
m_bShellextResults(FALSE),
m_bErasextEnabled(TRUE),
m_bEnableSlowPoll(FALSE),
m_dwStartView((DWORD)-1),
m_bIconAnimation(FALSE),
m_bSmallIconView(FALSE),
m_dwOutbarWidth(0),
m_bNoTrayIcon(FALSE),
m_bHideOnMinimize(FALSE),
m_bViewInfoBar(FALSE),
m_smallImageList (NULL)
{
    TRACE("CEraserDoc::CEraserDoc\n");

    m_bAutoDelete = FALSE;
    ZeroMemory(&m_rWindowRect, sizeof(RECT));

    // find executable location for logging
    try
    {
		// Create the Application Data path to store the Default ers file
		if (!SUCCEEDED(SHGetFolderPath(NULL, CSIDL_LOCAL_APPDATA, NULL, 0, m_strAppDataPath.GetBuffer(MAX_PATH))))
			AfxMessageBox("Could not determine path to Application Data", MB_ICONERROR);
		m_strAppDataPath.ReleaseBuffer();
		CreateDirectory((m_strAppDataPath += "\\") += szAppDataPath, NULL);

		// read preferences
        if (!ReadPreferences())
        {
            AfxTimeOutMessageBox(IDS_ERROR_PREFERENCES_READ, MB_ICONERROR);

            if (m_bLog)
                LogAction(IDS_ERROR_PREFERENCES_READ);
        }

        // create task bar tray icon
        m_stIcon.Create(NULL, WM_TRAY_NOTIFY, "Starting...",
                        AfxGetApp()->LoadIcon(IDI_ICON_TRAY),
                        IDR_MENU_TRAY, !m_bNoTrayIcon);
		
        // create timers
        CalcNextAssignment();
        UpdateToolTip();

        if (m_bLog && !m_bLogOnlyErrors)
        {
            CString strStart;
            AfxFormatString1(strStart, IDS_ACTION_START, VERSION_NUMBER_STRING);
            LogAction(strStart);
        }

        VERIFY(m_ilHeader.Create(IDB_HEADER, 10, 1, RGB(255, 255, 255)));

        //image list setup
        HIMAGELIST hSystemSmallImageList;
        SHFILEINFO ssfi;

        //get a handle to the system small icon list
        hSystemSmallImageList =
            reinterpret_cast<HIMAGELIST>(SHGetFileInfo((LPCTSTR)_T("C:\\"),
                                      0, &ssfi, sizeof(SHFILEINFO),
                                      SHGFI_SYSICONINDEX | SHGFI_SMALLICON));

	
        //m_smallImageList.Attach(hSystemSmallImageList);
		m_smallImageList = CImageList::FromHandle(hSystemSmallImageList);
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

CEraserDoc::~CEraserDoc()
{
    TRACE("CEraserDoc::~CEraserDoc\n");

    try
    {
        m_smallImageList->Detach();
        m_stIcon.RemoveIcon();

        if (!SavePreferences() && m_bLog)
            LogAction(IDS_ERROR_PREFERENCES_SAVE);

        if (m_bLog && !m_bLogOnlyErrors)
            LogAction(IDS_ACTION_QUIT);

	//	delete m_smallImageList;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

BOOL CEraserDoc::OnNewDocument()
{
	TRACE("CEraserDoc::OnNewDocument\n");

	try
	{
		if (!CDocument::OnNewDocument())
			return FALSE;

		Import(m_strAppDataPath + szDefaultFile, FALSE);
		return TRUE;
	}
	catch (CException *e)
	{
		ASSERT(FALSE);
		REPORT_ERROR(e);
		e->Delete();
	}

	return FALSE;
}



/////////////////////////////////////////////////////////////////////////////
// CEraserDoc serialization

void CEraserDoc::Serialize(CArchive& ar)
{
    TRACE("CEraserDoc::Serialize\n");

    CItem           *piItem     = 0;
    CScheduleItem   *psiItem    = 0;

    if (ar.IsStoring())
    {
        int iSize;

        // remove invalid items
        CleanList(m_paTasks, sizeof(CItem));
        CleanList(m_paScheduledTasks, sizeof(CScheduleItem));

        ar << static_cast<DWORD>(m_paTasks.GetSize() + m_paScheduledTasks.GetSize());

        iSize = m_paTasks.GetSize();

        while (iSize--)
        {
            piItem = static_cast<CItem*>(m_paTasks[iSize]);

            ar << static_cast<WORD>(ITEMVERSION);
            ar << static_cast<WORD>(ITEM_ID);
            piItem->Serialize(ar);
        }

        iSize = m_paScheduledTasks.GetSize();

        while (iSize--)
        {
            psiItem = static_cast<CScheduleItem*>(m_paScheduledTasks[iSize]);

            ar << static_cast<WORD>(ITEMVERSION);
            ar << static_cast<WORD>(SCHEDULE_ID);
            psiItem->Serialize(ar);
        }
    }
    else
    {
        DWORD dwCount = 0;
        WORD  wID     = 0;
        WORD  wItemID = 0;

        ar >> dwCount;

        for (DWORD dwItem = 0; dwItem < dwCount; dwItem++)
        {
            ar >> wID;

            piItem = 0;
            psiItem = 0;

            if (wID >= ITEMVERSION_30 && wID <= ITEMVERSION)
            {
                ar >> wItemID;

                try
                {
                    if (wItemID == ITEM_ID)
                    {
                        piItem = new CItem();

                        switch (wID)
                        {
                        case ITEMVERSION:
                            piItem->Serialize(ar);
                            break;
						case ITEMVERSION_41:
							psiItem->Serialize(ar);
							break;
#ifdef SCHEDULER_IMPORT_COMPATIBLE
                        case ITEMVERSION_40:
                            piItem->Serialize40(ar);
                            break;
                        case ITEMVERSION_30:
                            piItem->Serialize30(ar);
                            break;
#endif
                        default:
                            AfxThrowArchiveException(CArchiveException::badIndex);
                        }

                        AddTask(piItem);
                    }
                    else if (wItemID == SCHEDULE_ID)
                    {
                        psiItem = new CScheduleItem();

                        switch (wID)
                        {
                        case ITEMVERSION:
                            psiItem->Serialize(ar);
                            break;
						case ITEMVERSION_41:
							psiItem->Serialize41(ar);
							break;
#ifdef SCHEDULER_IMPORT_COMPATIBLE
                        case ITEMVERSION_40:
                            psiItem->Serialize40(ar);
                            break;
                        case ITEMVERSION_30:
                            psiItem->Serialize30(ar);
                            break;
#endif
                        default:
                            AfxThrowArchiveException(CArchiveException::badIndex);
                        }

                        AddScheduledTask(psiItem);
                    }
                }
                catch (...)
                {
                    if (piItem)
                    {
                        delete piItem;
                        piItem = 0;
                    }

                    if (psiItem)
                    {
                        delete psiItem;
                        psiItem = 0;
                    }

                    ASSERT(FALSE);
                    throw;
                }
            }
#ifdef SCHEDULER_IMPORT_COMPATIBLE
            else if (wID == ITEMVERSION_21)
            {
                psiItem = new CScheduleItem();

                try
                {
                    psiItem->Serialize21(ar);
                    AddScheduledTask(psiItem);
                }
                catch (...)
                {
                    delete psiItem;
                    psiItem = 0;

                    ASSERT(FALSE);
                    throw;
                }
            }
#endif
            else
            {
                // unsupported format
                AfxThrowArchiveException(CArchiveException::badIndex);
            }
        }
    }
}

/////////////////////////////////////////////////////////////////////////////
// CEraserDoc diagnostics

#ifdef _DEBUG
void CEraserDoc::AssertValid() const
{
    CDocument::AssertValid();
}

void CEraserDoc::Dump(CDumpContext& dc) const
{
    CDocument::Dump(dc);
}
#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// CEraserDoc commands

BOOL CEraserDoc::OnOpenDocument(LPCTSTR lpszPathName)
{
    TRACE("CEraserDoc::OnOpenDocument\n");

    try
    {
        if (!CDocument::OnNewDocument())
            return FALSE;

        Import(lpszPathName);

        return TRUE;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    return FALSE;
}

void CEraserDoc::DeleteContents()
{
    TRACE("CEraserDoc::DeleteContents\n");

    FreeTasks();
    CDocument::DeleteContents();
}

void CEraserDoc::FreeTasks()
{
    TRACE("CEraserDoc::FreeTasks\n");

    try
    {
        CItem         *piItem  = 0;
        CScheduleItem *psiItem = 0;
        int           iSize    = 0;

        // tasks of type CItem
        iSize = m_paTasks.GetSize();

        while (iSize--)
        {
            piItem = static_cast<CItem*>(m_paTasks[iSize]);
            if (AfxIsValidAddress(piItem, sizeof(CItem)))
                delete piItem;

            piItem = 0;
        }

        m_paTasks.RemoveAll();

        // scheduled tasks of type CScheduleItem
        iSize = m_paScheduledTasks.GetSize();

        while (iSize--)
        {
            psiItem = static_cast<CScheduleItem*>(m_paScheduledTasks[iSize]);

            if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                delete psiItem;

            psiItem = 0;
        }

        m_paScheduledTasks.RemoveAll();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

BOOL CEraserDoc::AddTask(CItem *piItem)
{
    TRACE("CEraserDoc::AddTask\n");

    if (AfxIsValidAddress(piItem, sizeof(CItem)))
    {
        try
        {
            CString strNew;
            piItem->GetData(strNew);

            CItem   *piCurrent  = 0;
            int     iSize       = m_paTasks.GetSize();

            // duplicates are not accepted for on-demand eraser

            while (iSize--)
            {
                piCurrent = static_cast<CItem*>(m_paTasks[iSize]);
                if (AfxIsValidAddress(piCurrent, sizeof(CItem)))
                {
                    if (!strNew.CompareNoCase((LPCTSTR)(piCurrent->GetData())))
                        return FALSE;
                }
            }

            m_paTasks.Add(static_cast<void*>(piItem));
            return TRUE;
        }
        catch (CException *e)
        {
            ASSERT(FALSE);
            REPORT_ERROR(e);
            e->Delete();
        }
    }

    return FALSE;
}

BOOL CEraserDoc::AddScheduledTask(CScheduleItem *psiItem)
{
    TRACE("CEraserDoc::AddScheduledTask\n");

    if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
    {
        try
        {
            m_paScheduledTasks.Add(static_cast<void*>(psiItem));
            return TRUE;
        }
        catch (CException *e)
        {
            ASSERT(FALSE);
            REPORT_ERROR(e);
            e->Delete();
        }
    }

    return FALSE;
}


void CEraserDoc::OnUpdateFileExport(CCmdUI* pCmdUI)
{
    pCmdUI->Enable(m_paTasks.GetSize() > 0 || m_paScheduledTasks.GetSize() > 0);
}

void CEraserDoc::OnFileExport()
{
    TRACE("CEraserDoc::OnFileExport\n");
    CFileDialogEx fd(FALSE,
                     szFileExtension,
                     szFileWCard,
                     OFN_EXPLORER | OFN_PATHMUSTEXIST |
                     OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT,
                     szFileFilter,
                     AfxGetMainWnd());

	fd.m_ofn.lpstrTitle = szExportTitle;

    if (fd.DoModal() == IDOK)
    {
        CString strFile = fd.GetPathName();
        Export((LPCTSTR)strFile);
    }
}

void CEraserDoc::OnFileImport()
{
    TRACE("CEraserDoc::OnFileImport\n");
// Was CfileDialogEx now with MFC7 we can change back to MFC Class
    CFileDialog fd(TRUE,
                     szFileExtension,
                     szFileWCard,
                     OFN_EXPLORER | OFN_PATHMUSTEXIST |
                     OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT,
                     szFileFilter,
                     AfxGetMainWnd());

	fd.m_ofn.lpstrTitle = szImportTitle;

    if (fd.DoModal() == IDOK)
    {
        CString strFile = fd.GetPathName();
        Import((LPCTSTR)strFile);

        UpdateAllViews(NULL, SCHEDULER_SET_TIMERS);
    }
}

void CEraserDoc::OnUpdateTrayEnable(CCmdUI* pCmdUI)
{
    pCmdUI->SetCheck(m_bSchedulerEnabled);
}

void CEraserDoc::OnTrayEnable()
{
    TRACE("CEraserDoc::OnTrayEnable\n");

    m_bSchedulerEnabled = !m_bSchedulerEnabled;

    if (m_bLog && !m_bLogOnlyErrors)
    {
        if (m_bSchedulerEnabled)
            LogAction(IDS_ACTION_ENABLED);
        else
            LogAction(IDS_ACTION_DISABLED);
    }

    UpdateToolTip();
}

void CEraserDoc::OnUpdateTrayShowWindow(CCmdUI* pCmdUI)
{
    try
    {
        CFrameWnd *pwndMain = static_cast<CFrameWnd*>(AfxGetMainWnd());
        pCmdUI->Enable(!pwndMain->IsWindowVisible());
    }
    catch (...)
    {
        ASSERT(FALSE);
        pCmdUI->Enable(FALSE);
    }
}

void CEraserDoc::OnTrayShowWindow()
{
    TRACE("CEraserDoc::OnTrayShowWindow\n");

    try
    {
        CMainFrame *pwndMain = static_cast<CMainFrame*>(AfxGetMainWnd());

        if (!pwndMain->IsWindowVisible())
        {
            pwndMain->ShowWindow(SW_SHOW);

            if (pwndMain->m_pwndChild->m_iActiveViewID == VIEW_SCHEDULER)
            {
                // if there are processes running, update the window
                if (m_wProcessCount > 0)
                    pwndMain->m_pwndChild->m_pSchedulerView->EraserWipeBegin();
            }
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

BOOL CEraserDoc::LogAction(CString str)
{
    TRACE("CEraserDoc::LogAction(CString)\n");

    BOOL bResult = FALSE;
    CString strDate, strPath;

    CStdioFile sf;

    strPath = m_strExePath + szLogFile;

    if (sf.Open(strPath, CFile::modeReadWrite | CFile::modeCreate | CFile::modeNoTruncate | CFile::typeText))
    {
        strDate = GetTimeTimeZoneBased().Format();
        str = strDate + ": " + str + "\n";


        try
        {
            DWORD dwMax = m_dwMaxLogSize * 1024;
            ULONGLONG dwCurrent = sf.GetLength() + str.GetLength();

            if (m_dwMaxLogSize > 0 && dwMax < dwCurrent)
            {
                // must remove lines in order to keep the log
                // file under the maximum size

                CString strTmp;
                LONGLONG dwLen = dwMax - str.GetLength();

                sf.Seek(-1*dwLen, CFile::end);
                sf.ReadString(strTmp);

                LPTSTR lpszTmp = strTmp.GetBufferSetLength(dwLen);
                ZeroMemory(lpszTmp, dwLen);

                sf.Read((LPVOID)lpszTmp, dwLen);

                strTmp.ReleaseBuffer();
                strTmp += str;

                sf.SetLength(0);
                sf.SeekToBegin();
                sf.Write(strTmp, strTmp.GetLength());
            }
            else
            {
                // write to the end of the file
                sf.SeekToEnd();
                sf.WriteString(str);
            }

            bResult = TRUE;
        }
		catch (CException* e)
		{
			e->ReportError();			
		}
        catch (...)
        {
            ASSERT(FALSE);
        }

        sf.Close();
    }

    return bResult;
}

BOOL CEraserDoc::LogException(CException *e)
{
    TRACE("CEraserDoc::LogException\n");

    try
    {
        TCHAR szCause[255];
        e->GetErrorMessage(szCause, 255);

        return LogAction(szCause);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return FALSE;
}

BOOL CEraserDoc::LogAction(UINT nResourceID)
{
    TRACE("CEraserDoc::LogAction(UINT)\n");

    CString str;
    BOOL bResult = FALSE;

    try
    {
        if (str.LoadString(nResourceID))
            bResult = LogAction(str);
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        if (m_bLog)
            LogException(e);

        e->Delete();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return bResult;
}

void CEraserDoc::CalcNextAssignment()
{
    TRACE("CEraserDoc::CalcNextAssignment\n");

    try
    {
        int iSize = m_paScheduledTasks.GetSize();

        if (iSize > 0)
        {
            iSize--;

            CScheduleItem   *psiItem    = static_cast<CScheduleItem*>(m_paScheduledTasks[iSize]);
            COleDateTime    odtEarliest = psiItem->GetNextTime();

            while (iSize--)
            {
                psiItem = static_cast<CScheduleItem*>(m_paScheduledTasks[iSize]);
                if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                {
                    if (psiItem->GetNextTime() < odtEarliest)
                        odtEarliest = psiItem->GetNextTime();
                }
            }

            m_strNextAssignment = odtEarliest.Format();
        }
        else
            m_strNextAssignment.Empty();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        if (m_bLog)
            LogException(e);

        e->Delete();
    }
}

void CEraserDoc::UpdateToolTip()
{
    TRACE("CEraserDoc::UpdateToolTip\n");

    CString     strFormat;
    CString     str;

    try
    {
        if (m_wProcessCount == 0)
        {
            if (m_bSchedulerEnabled)
            {
                if (m_strNextAssignment.IsEmpty())
                {
                    strFormat.LoadString(IDS_TOOLTIP_WAITING);
                    m_stIcon.SetTooltipText(strFormat);
                }
                else
                {
                    str.LoadString(IDS_TOOLTIP_NEXT);
                    str += m_strNextAssignment;

                    m_stIcon.SetTooltipText(str);
                }

                m_stIcon.SetIcon(IDI_ICON_TRAY);
            }
            else
            {
                strFormat.LoadString(IDS_TOOLTIP_DISABLED);
                m_stIcon.SetTooltipText(strFormat);

                m_stIcon.SetIcon(IDI_ICON_TRAY_DISABLED);
            }
        }
        else
        {
            m_stIcon.SetIcon(IDI_ICON_TRAY_RUNNING);

            strFormat.LoadString(IDS_TOOLTIP_PROCESSING);
            str.Format(strFormat, m_wProcessCount);

            if (str.CompareNoCase(m_stIcon.GetTooltipText()) != 0)
                m_stIcon.SetTooltipText((LPCTSTR)str);
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        if (m_bLog)
            LogException(e);

        e->Delete();
    }
}

#define RESULT(result, statement) \
    ((result) = ((statement) ? (result) : FALSE))

__declspec(dllimport) bool no_registry;

BOOL CEraserDoc::SavePreferences()
{
    TRACE("CEraserDoc::SavePreferences\n");

    BOOL            bResult     = TRUE;
    CKey            kReg_reg;
	CIniKey         kReg_ini;
	CKey           &kReg = no_registry ? kReg_ini : kReg_reg;
    CString         strPath;
    OSVERSIONINFO   ov;

    strPath.Format("%s\\%s", ERASER_REGISTRY_BASE, szSettingsKey);

    ZeroMemory(&ov, sizeof(OSVERSIONINFO));
    ov.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    GetVersionEx(&ov);

    // Scheduler Preferences
    if (kReg.Open(HKEY_CURRENT_USER, strPath))
    {
        RESULT(bResult, kReg.SetValue(m_bSchedulerEnabled, szSchedulerEnabled));
        RESULT(bResult, kReg.SetValue(m_bLog, szLog));
        RESULT(bResult, kReg.SetValue(m_bLogOnlyErrors, szLogOnlyErrors));
        RESULT(bResult, kReg.SetValue(m_bStartup, szStartup));
        RESULT(bResult, kReg.SetValue(m_bQueueTasks, szQueueTasks));
        RESULT(bResult, kReg.SetValue(m_bNoVisualErrors, szNoVisualErrors));
        RESULT(bResult, kReg.SetValue(m_dwMaxLogSize, szMaxLogSize));

        kReg.Close();
    }
    else
        bResult = FALSE;

    // Scheduler Startup
	if (!no_registry) {
		if (m_bStartup)
		{
			if (kReg.Open(HKEY_CURRENT_USER, szStartupPath))
			{
				TCHAR szApplicationFileName[MAX_PATH + 1];

				if (GetModuleFileName(AfxGetInstanceHandle(), szApplicationFileName, MAX_PATH))
				{
					CString strStartupCmdLine(szApplicationFileName);
					strStartupCmdLine += " ";
					strStartupCmdLine += NOWINDOW_PARAMETER;

					RESULT(bResult, kReg.SetValue((LPCTSTR)strStartupCmdLine, szRunOnStartup));
				}
				else
					bResult = FALSE;

				kReg.Close();
			}
			else
				bResult = FALSE;
		}
		else if (kReg.Open(HKEY_CURRENT_USER, szStartupPath, FALSE))
		{
			kReg.DeleteValue(szRunOnStartup);
			kReg.Close();
		}
	}

    // General Preferences

	//cancel report on recycle bin clearance if m_bShellextResults is unchecked
	const LPCTSTR REC_BIN_PKEY = "CLSID\\";
	const LPCTSTR REC_BIN_SKEY = "Shell\\Eraserxt\\command";
	const CString strWthReport = "\\Eraserl.exe -recycled -results ";
	const CString strWoReport =  "\\Eraserl.exe -recycled ";

	CString strDef = _T(""), strCmd, strOld = _T("");
	CString strRecBinGUID = REC_BIN_PKEY + findRecycledBinGUID();
	strRecBinGUID = strRecBinGUID + "\\" + REC_BIN_SKEY;
	
	if (!no_registry && kReg.Open(HKEY_CLASSES_ROOT,strRecBinGUID,FALSE))
	{
		char buf[_MAX_PATH];
		_getcwd(buf,_MAX_PATH); //working directory
		strCmd = buf;
		if (m_bShellextResults == TRUE)	strCmd = strCmd + strWthReport;
		else strCmd = strCmd + strWoReport;
		kReg.GetValue(strOld,strDef,"");
		if (strOld != strCmd ) kReg.SetValue(strCmd,strDef);
		kReg.Close();
	}
	
	
	if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
    {
        if (m_dwStartView != (DWORD) -1)
            RESULT(bResult, kReg.SetValue(m_dwStartView, szStartView));

        RESULT(bResult, kReg.SetValue((LPVOID)&m_rWindowRect, szWindowRect, sizeof(RECT)));
        RESULT(bResult, kReg.SetValue(m_bResultsForFiles, ERASER_REGISTRY_RESULTS_FILES));
        RESULT(bResult, kReg.SetValue(m_bResultsForUnusedSpace, ERASER_REGISTRY_RESULTS_UNUSEDSPACE));
        RESULT(bResult, kReg.SetValue(m_bResultsOnlyWhenFailed, ERASER_REGISTRY_RESULTS_WHENFAILED));
        RESULT(bResult, kReg.SetValue(m_bShellextResults, ERASEXT_REGISTRY_RESULTS));
        RESULT(bResult, kReg.SetValue(m_bErasextEnabled, ERASEXT_REGISTRY_ENABLED));
        RESULT(bResult, kReg.SetValue(m_bEnableSlowPoll, ERASER_RANDOM_SLOW_POLL));
        RESULT(bResult, kReg.SetValue(m_bIconAnimation, szIconAnimation));
        RESULT(bResult, kReg.SetValue(m_bSmallIconView, szSmallIconView));
        RESULT(bResult, kReg.SetValue(m_dwOutbarWidth, szOutBarWidth));
        RESULT(bResult, kReg.SetValue(m_bNoTrayIcon, szNoTrayIcon));
        RESULT(bResult, kReg.SetValue(m_bHideOnMinimize, szHideOnMinimize));
        RESULT(bResult, kReg.SetValue(m_bViewInfoBar, szViewInfoBar));
		RESULT(bResult, kReg.SetValue(m_bResolveLock, szResolveLock));
		RESULT(bResult, kReg.SetValue(m_bResolveAskUser, szResolveAskUser));
		
        kReg.Close();

    }
    else
        bResult = FALSE;

    // Page File Clearing only on NT (works only if Admin so no errors here)
    if (ov.dwPlatformId == VER_PLATFORM_WIN32_NT)
    {
        if (kReg_reg.Open(HKEY_LOCAL_MACHINE, szClearSwapPath))
        {
            // need to set the registry key even if m_bClearSwap == false,
            // otherwise Windows 2000 overrides the setting when applying
            // local security policy

            kReg_reg.SetValue(m_bClearSwap, szClearSwapValue);
            kReg_reg.Close();
        }
    }

    return bResult;
}

BOOL CEraserDoc::ReadPreferences()
{
    TRACE("CEraserDoc::ReadPreferences\n");

    BOOL            bResult     = TRUE;
	CKey            kReg_reg;
	CIniKey         kReg_ini;
	CKey           &kReg = no_registry ? kReg_ini : kReg_reg;
    CString         strPath;
    OSVERSIONINFO   ov;

    strPath.Format("%s\\%s", ERASER_REGISTRY_BASE, szSettingsKey);

    ZeroMemory(&ov, sizeof(OSVERSIONINFO));
    ov.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    GetVersionEx(&ov);

    // Scheduler Preferences
    if (kReg.Open(HKEY_CURRENT_USER, strPath))
    {
        kReg.GetValue(m_bSchedulerEnabled, szSchedulerEnabled, TRUE);
        kReg.GetValue(m_bLog, szLog, TRUE);
        kReg.GetValue(m_bLogOnlyErrors, szLogOnlyErrors, FALSE);
        kReg.GetValue(m_bStartup, szStartup, TRUE);
        kReg.GetValue(m_bQueueTasks, szQueueTasks, TRUE);
        kReg.GetValue(m_bNoVisualErrors, szNoVisualErrors, FALSE);
        kReg.GetValue(m_dwMaxLogSize, szMaxLogSize, 10);

        kReg.Close();
    }
    else
        bResult = FALSE;

    // General Preferences
    if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
    {
        if (m_dwStartView == (DWORD) -1)
            kReg.GetValue(m_dwStartView, szStartView, VIEW_ERASER);

        ZeroMemory(&m_rWindowRect, sizeof(RECT));

        if (kReg.GetValueSize(szWindowRect) == sizeof(RECT))
            kReg.GetValue((LPVOID)&m_rWindowRect, szWindowRect);

        kReg.GetValue(m_bResultsForFiles, ERASER_REGISTRY_RESULTS_FILES, TRUE);
        kReg.GetValue(m_bResultsForUnusedSpace, ERASER_REGISTRY_RESULTS_UNUSEDSPACE, TRUE);
        kReg.GetValue(m_bResultsOnlyWhenFailed, ERASER_REGISTRY_RESULTS_WHENFAILED, FALSE);
        kReg.GetValue(m_bShellextResults, ERASEXT_REGISTRY_RESULTS, TRUE);
        kReg.GetValue(m_bErasextEnabled, ERASEXT_REGISTRY_ENABLED, TRUE);
        kReg.GetValue(m_bEnableSlowPoll, ERASER_RANDOM_SLOW_POLL, FALSE);
        kReg.GetValue(m_bIconAnimation, szIconAnimation, FALSE);
        kReg.GetValue(m_bSmallIconView, szSmallIconView, FALSE);
        kReg.GetValue(m_dwOutbarWidth, szOutBarWidth, 0);
        kReg.GetValue(m_bNoTrayIcon, szNoTrayIcon, FALSE);
        kReg.GetValue(m_bHideOnMinimize, szHideOnMinimize, FALSE);
        kReg.GetValue(m_bViewInfoBar, szViewInfoBar, FALSE);
		kReg.GetValue(m_bResolveLock, szResolveLock, TRUE);
		kReg.GetValue(m_bResolveAskUser, szResolveAskUser, TRUE);
		

        kReg.Close();
    }
    else
        bResult = FALSE;

    // Page File Clearing only on NT
    m_bClearSwap = FALSE;

    if (ov.dwPlatformId == VER_PLATFORM_WIN32_NT)
    {
        if (kReg_reg.Open(HKEY_LOCAL_MACHINE, szClearSwapPath))
        {
            kReg_reg.GetValue(m_bClearSwap, szClearSwapValue, FALSE);
            kReg_reg.Close();
        }
    }

    return bResult;
}

void CEraserDoc::OnEditPreferencesGeneral()
{
    TRACE("CEraserDoc::OnEditPreferencesGeneral\n");

    ReadPreferences();

    CPreferencesSheet propSheet;

    // General

	

    propSheet.m_pgEraser.m_bResultsForFiles         = m_bResultsForFiles;
    propSheet.m_pgEraser.m_bResultsForUnusedSpace   = m_bResultsForUnusedSpace;
    propSheet.m_pgEraser.m_bResultsOnlyWhenFailed   = m_bResultsOnlyWhenFailed;
    propSheet.m_pgEraser.m_bShellextResults         = m_bShellextResults;
    propSheet.m_pgEraser.m_bErasextEnabled          = m_bErasextEnabled;
    propSheet.m_pgEraser.m_bEnableSlowPoll          = m_bEnableSlowPoll;
    propSheet.m_pgEraser.m_bClearSwap               = m_bClearSwap;
	propSheet.m_pgEraser.m_bResolveLock				= m_bResolveLock;
	propSheet.m_pgEraser.m_bResolveAskUser			= m_bResolveAskUser;				


    // Scheduler

    propSheet.m_pgScheduler.m_bNoTrayIcon           = m_bNoTrayIcon;
    propSheet.m_pgScheduler.m_bHideOnMinimize       = m_bHideOnMinimize;
    propSheet.m_pgScheduler.m_bLog                  = m_bLog;
    propSheet.m_pgScheduler.m_bLogOnlyErrors        = m_bLogOnlyErrors;
    propSheet.m_pgScheduler.m_bStartup              = m_bStartup;
    propSheet.m_pgScheduler.m_bEnabled              = m_bSchedulerEnabled;
    propSheet.m_pgScheduler.m_bQueueTasks           = m_bQueueTasks;
    propSheet.m_pgScheduler.m_bNoVisualErrors       = m_bNoVisualErrors;
    propSheet.m_pgScheduler.m_dwMaxLogSize          = m_dwMaxLogSize;

    if (propSheet.DoModal())
    {
        // General

        m_bResultsForFiles          = propSheet.m_pgEraser.m_bResultsForFiles;
        m_bResultsForUnusedSpace    = propSheet.m_pgEraser.m_bResultsForUnusedSpace;
        m_bResultsOnlyWhenFailed    = propSheet.m_pgEraser.m_bResultsOnlyWhenFailed;
        m_bShellextResults          = propSheet.m_pgEraser.m_bShellextResults;
        m_bErasextEnabled           = propSheet.m_pgEraser.m_bErasextEnabled;
        m_bEnableSlowPoll           = propSheet.m_pgEraser.m_bEnableSlowPoll;
        m_bClearSwap                = propSheet.m_pgEraser.m_bClearSwap;
		m_bResolveLock				= propSheet.m_pgEraser.m_bResolveLock;
		m_bResolveAskUser			= propSheet.m_pgEraser.m_bResolveAskUser;

        // Scheduler

        m_bNoTrayIcon               = propSheet.m_pgScheduler.m_bNoTrayIcon;
        m_bHideOnMinimize           = propSheet.m_pgScheduler.m_bHideOnMinimize;
        m_bLog                      = propSheet.m_pgScheduler.m_bLog;
        m_bLogOnlyErrors            = propSheet.m_pgScheduler.m_bLogOnlyErrors;
        m_bStartup                  = propSheet.m_pgScheduler.m_bStartup;
        m_bSchedulerEnabled         = propSheet.m_pgScheduler.m_bEnabled;
        m_bQueueTasks               = propSheet.m_pgScheduler.m_bQueueTasks;
        m_bNoVisualErrors           = propSheet.m_pgScheduler.m_bNoVisualErrors;
        m_dwMaxLogSize              = propSheet.m_pgScheduler.m_dwMaxLogSize;

        if (!m_bNoTrayIcon && !m_stIcon.Visible())
            m_stIcon.ShowIcon();
        else if (m_bNoTrayIcon)
        {
            if (!m_bHideOnMinimize && !AfxGetMainWnd()->IsWindowVisible())
                AfxGetMainWnd()->ShowWindow(SW_SHOWMINIMIZED);
            else if (m_bHideOnMinimize && AfxGetMainWnd()->IsIconic())
                AfxGetMainWnd()->ShowWindow(SW_HIDE);

            m_stIcon.HideIcon();
        }

        UpdateToolTip();

        if (!SavePreferences())
        {
            AfxTimeOutMessageBox(IDS_ERROR_PREFERENCES_SAVE, MB_ICONERROR);

            if (m_bLog)
                LogAction(IDS_ERROR_PREFERENCES_SAVE);
        }
    }
}

void CEraserDoc::OnEditPreferencesEraser()
{
    TRACE("CEraserDoc::OnEditPreferencesEraser\n");
    eraserShowOptions(AfxGetMainWnd()->GetSafeHwnd(), ERASER_PAGE_FILES);
}

void CEraserDoc::OnFileViewLog()
{
    TRACE("CEraserDoc::OnFileViewLog\n");

    CFileStatus fs;
    CString strPath = m_strExePath + szLogFile;

    if (CFile::GetStatus((LPCTSTR)strPath, fs))
    {
        if (reinterpret_cast<int>(ShellExecute(NULL, "open", (LPCTSTR)strPath,
                                               NULL, NULL, SW_SHOWNORMAL)) <= 32)
        {
            AfxMessageBox(IDS_ERROR_VIEWLOG, MB_ICONERROR, 0);
        }
    }
    else
    {
        AfxMessageBox(IDS_INFO_NOLOG, MB_ICONINFORMATION, 0);
    }
}

BOOL CEraserDoc::Export(LPCTSTR szFile, BOOL bErrors)
{
    TRACE("CEraserDoc::Export\n");

    // export the present items into a file

    CFileException fe;
    CFile* pFile = NULL;

    pFile = GetFile(szFile, CFile::modeCreate |
                    CFile::modeReadWrite | CFile::shareExclusive, &fe);

    if (pFile == NULL)
    {
        if (bErrors)
            ReportSaveLoadException(szFile, &fe, TRUE, AFX_IDP_INVALID_FILENAME);
        return FALSE;
    }

    CArchive saveArchive(pFile, CArchive::store | CArchive::bNoFlushOnDelete);

    saveArchive.m_pDocument  = this;
    saveArchive.m_bForceFlat = FALSE;

    try
    {
        CWaitCursor wait;
        Serialize(saveArchive);

        saveArchive.Close();
        ReleaseFile(pFile, FALSE);
    }
    catch (CFileException* e)
    {
        ASSERT(FALSE);

        ReleaseFile(pFile, TRUE);

        if (bErrors)
        {
            try
            {
                ReportSaveLoadException(szFile, e, TRUE, AFX_IDP_FAILED_TO_SAVE_DOC);
            }
            catch (...)
            {
            }
        }

        if (m_bLog)
            LogException(e);

        e->Delete();
        return FALSE;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        ReleaseFile(pFile, TRUE);

        if (bErrors)
            REPORT_ERROR(e);

        if (m_bLog)
            LogException(e);

        e->Delete();
        return FALSE;
    }

    return TRUE;
}

BOOL CEraserDoc::Import(LPCTSTR szFile, BOOL bErrors)
{
    TRACE("CEraserDoc::Import\n");

    // import items from a file without removing
    // present items

    CFileException fe;
    CFile* pFile = NULL;

    pFile = GetFile(szFile, CFile::modeRead | CFile::shareDenyWrite, &fe);

    if (pFile == NULL)
    {
        if (bErrors)
            ReportSaveLoadException(szFile, &fe, TRUE, AFX_IDP_INVALID_FILENAME);
        return FALSE;
    }

    CArchive loadArchive(pFile, CArchive::load | CArchive::bNoFlushOnDelete);

    loadArchive.m_pDocument  = this;
    loadArchive.m_bForceFlat = FALSE;

    try
    {
        CWaitCursor wait;
        if (pFile->GetLength() != 0)
            Serialize(loadArchive);

        loadArchive.Close();
        ReleaseFile(pFile, FALSE);
    }
    catch (CFileException* e)
    {
        ASSERT(FALSE);

        ReleaseFile(pFile, TRUE);

        if (bErrors)
        {
            try
            {
                ReportSaveLoadException(szFile, e, TRUE, AFX_IDP_FAILED_TO_SAVE_DOC);
            }
            catch (...)
            {
            }
        }

        if (m_bLog)
            LogException(e);

        e->Delete();
        return FALSE;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        ReleaseFile(pFile, TRUE);

        if (bErrors)
            REPORT_ERROR(e);

        if (m_bLog)
            LogException(e);

        e->Delete();
        return FALSE;
    }

    return TRUE;
}

BOOL CEraserDoc::SaveTasksToDefault()
{
    return Export(m_strAppDataPath + szDefaultFile);
}

void CEraserDoc::OnCloseDocument()
{
    TRACE("CEraserDoc::OnCloseDocument\n");

    try
    {
        SaveTasksToDefault();
        CDocument::OnCloseDocument();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}


//DEL void CEraserDoc::OnHelpReward()
//DEL {
//DEL     AfxGetApp()->WinHelp(HID_BASE_COMMAND + ID_HELP_REWARD);
//DEL }

void CEraserDoc::CleanList(CPtrArray& rList, int iItemSize)
{
    int iSize = rList.GetSize();
    while (iSize--)
    {
        if (!AfxIsValidAddress(rList[iSize], iItemSize))
            rList.RemoveAt(iSize);
    }
    rList.FreeExtra();
}
