// SchedulerView.cpp
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
#include "Eraser.h"
#include "EraserDoc.h"
#include "shared\key.h"
#include "SchedulerView.h"

#include "TaskPropertySheet.h"
#include "shared\FileHelper.h"
#include "EraserUI\DriveCombo.h"
#include "EraserUI\GfxPopupMenu.h"
#include "EraserUI\TimeOutMessageBox.h"

#include "EraserDll\OptionPages.h"
#include "EraserDll\options.h"
#include "EraserDll\EraserDllInternal.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

enum Columns
{
    ColumnName,
    ColumnType,
    ColumnLast,
    ColumnNext,
    ColumnSchedule
};

static const int iColumnCount = 5;

static const LPTSTR szColumnNames[iColumnCount] =
{
    "Name",
    "Type",
    "Last Run",
    "Next Run",
    "Schedule"
};

static const int iMinFirstColumnWidth = 100;
static const int iOtherColumnWidth = 370;

static const int iColumnWidths[] =
{
    -1,
    100,
    90,
    90,
    90
};

#define FIRST_TIMER (WM_USER + 42) // HHGTG

// for accessing the process counter
static CEvent evWaitForCounterAccess(TRUE, TRUE);

#define counterLock() \
    WaitForSingleObject(evWaitForCounterAccess, INFINITE); \
    evWaitForCounterAccess.ResetEvent()
#define counterUnlock() \
    evWaitForCounterAccess.SetEvent()

/////////////////////////////////////////////////////////////////////////////
// CSchedulerView

IMPLEMENT_DYNCREATE(CSchedulerView, CFlatListView)

CSchedulerView::CSchedulerView() :
m_uNextTimerID(FIRST_TIMER)
{
    TRACE("CSchedulerView::CSchedulerView\n");
}

CSchedulerView::~CSchedulerView()
{
    TRACE("CSchedulerView::~CSchedulerView\n");
}


BEGIN_MESSAGE_MAP(CSchedulerView, CFlatListView)
    ON_WM_CONTEXTMENU()
    //{{AFX_MSG_MAP(CSchedulerView)
    ON_WM_SIZE()
    ON_COMMAND(ID_FILE_NEW_TASK, OnFileNewTask)
    ON_UPDATE_COMMAND_UI(ID_EDIT_DELETE_TASK, OnUpdateEditDeleteTask)
    ON_COMMAND(ID_EDIT_DELETE_TASK, OnEditDeleteTask)
    ON_UPDATE_COMMAND_UI(ID_EDIT_PROPERTIES, OnUpdateEditProperties)
    ON_COMMAND(ID_EDIT_PROPERTIES, OnEditProperties)
    ON_WM_TIMER()
    ON_WM_DESTROY()
    ON_WM_LBUTTONDBLCLK()
    ON_UPDATE_COMMAND_UI(ID_PROCESS_RUN, OnUpdateProcessRun)
    ON_UPDATE_COMMAND_UI(ID_PROCESS_RUNALL, OnUpdateProcessRunAll)
    ON_COMMAND(ID_PROCESS_RUN, OnProcessRun)
    ON_COMMAND(ID_PROCESS_RUNALL, OnProcessRunAll)
    ON_UPDATE_COMMAND_UI(ID_PROCESS_STOP, OnUpdateProcessStop)
    ON_COMMAND(ID_PROCESS_STOP, OnProcessStop)
    ON_UPDATE_COMMAND_UI(ID_EDIT_SELECT_ALL, OnUpdateEditSelectAll)
    ON_COMMAND(ID_EDIT_SELECT_ALL, OnEditSelectAll)
    ON_COMMAND(ID_EDIT_REFRESH, OnEditRefresh)
    //}}AFX_MSG_MAP
    ON_MESSAGE(WM_ERASERNOTIFY, OnEraserNotify)
    ON_UPDATE_COMMAND_UI(ID_INDICATOR_ITEMS, OnUpdateItems)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CSchedulerView drawing

void CSchedulerView::OnDraw(CDC* /*pDC*/)
{

}

/////////////////////////////////////////////////////////////////////////////
// CSchedulerView diagnostics

#ifdef _DEBUG
void CSchedulerView::AssertValid() const
{
    CFlatListView::AssertValid();
}

void CSchedulerView::Dump(CDumpContext& dc) const
{
    CFlatListView::Dump(dc);
}
#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// CSchedulerView message handlers

BOOL CSchedulerView::Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext)
{
    TRACE("CSchedulerView::Create\n");

    dwStyle |= LVS_REPORT | LVS_NOSORTHEADER | LVS_SORTASCENDING | LVS_SHOWSELALWAYS;

    if (CFlatListView::Create(lpszClassName, lpszWindowName, dwStyle, rect, pParentWnd, nID, pContext))
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
        CListCtrl& lcList = GetListCtrl();

        try
        {
            CRect rClient;
            lcList.GetClientRect(&rClient);

            int iWidth = rClient.Width() - iOtherColumnWidth - GetSystemMetrics(SM_CXBORDER);

            if (iWidth < iMinFirstColumnWidth) iWidth = iMinFirstColumnWidth;

            LVCOLUMN lvc;
            ZeroMemory(&lvc, sizeof(LVCOLUMN));

            lvc.mask        = LVCF_FMT | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
            lvc.fmt         = LVCFMT_LEFT;
            lvc.pszText     = szColumnNames[ColumnName];
            lvc.cx          = iWidth;
            lvc.iSubItem    = ColumnName;
            lcList.InsertColumn(ColumnName, &lvc);

            for (int i = 1; i <= (iColumnCount - 1); i++)
            {
                lvc.pszText = szColumnNames[i];
                lvc.cx = iColumnWidths[i];
                lvc.iSubItem = i;
                lcList.InsertColumn(i, &lvc);
            }

            lcList.SetExtendedStyle(LVS_EX_HEADERDRAGDROP | LVS_EX_FULLROWSELECT | LVS_EX_GRIDLINES);
//            lcList.SetImageList(&pDoc->m_smallImageList, LVSIL_SMALL);
			lcList.SetImageList(pDoc->m_smallImageList, LVSIL_SMALL);

            CFlatHeaderCtrl *pfhFlatHeader = (CFlatHeaderCtrl*)lcList.GetHeaderCtrl();

            if (pfhFlatHeader != NULL)
            {
                pfhFlatHeader->SetImageList(&pDoc->m_ilHeader);

                HDITEMEX hie;

                hie.m_iMinWidth = iMinFirstColumnWidth;
                hie.m_iMaxWidth = -1;

                pfhFlatHeader->SetItemEx(ColumnName, &hie);

                HDITEM hditem;

                hditem.mask = HDI_FORMAT | HDI_IMAGE;
                pfhFlatHeader->GetItem(ColumnName, &hditem);

                hditem.fmt      |= HDF_IMAGE;
                hditem.iImage   = IconEraser;
                pfhFlatHeader->SetItem(ColumnName, &hditem);

                hditem.mask = HDI_FORMAT | HDI_IMAGE;
                pfhFlatHeader->GetItem(ColumnSchedule, &hditem);

                hditem.fmt      |= HDF_IMAGE;
                hditem.iImage   = IconClock;
                pfhFlatHeader->SetItem(ColumnSchedule, &hditem);
            }

            ModifyStyleEx(WS_EX_CLIENTEDGE, 0);

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

void CSchedulerView::OnSize(UINT nType, int cx, int cy)
{
    TRACE("CSchedulerView::OnSize\n");

    CFlatListView::OnSize(nType, cx, cy);
    ResizeColumns();
}

void CSchedulerView::ResizeColumns()
{
    TRACE("CSchedulerView::ResizeColumns\n");

    CListCtrl& lc = GetListCtrl();

    CRect rClient;
    lc.GetClientRect(&rClient);

    int iWidth;
    int iColumn;
    int iRest = 0;

    for (iColumn = (iColumnCount - 1); iColumn >= 1; iColumn--)
        iRest += lc.GetColumnWidth(iColumn);

    iWidth = rClient.Width() - iRest - GetSystemMetrics(SM_CXBORDER);

    if (iWidth < iMinFirstColumnWidth)
        iWidth = iMinFirstColumnWidth;

    lc.SetColumnWidth(0, iWidth);
}

void CSchedulerView::OnContextMenu(CWnd*, CPoint point)
{
    TRACE("CSchedulerView::OnContextMenu\n");

    try
    {
        if (point.x == -1 && point.y == -1)
        {
            CRect rect;
            GetClientRect(rect);
            ClientToScreen(rect);

            point = rect.TopLeft();
            point.Offset(5, 5);
        }

        CGfxPopupMenu menu;
        menu.LoadMenu(IDR_MENU_SCHEDULERVIEW, IDR_MAINFRAME, this);

        CWnd* pWndPopupOwner = this;

        while (pWndPopupOwner->GetStyle() & WS_CHILD)
            pWndPopupOwner = pWndPopupOwner->GetParent();

        menu.TrackPopupMenu(TPM_LEFTALIGN | TPM_RIGHTBUTTON, point.x, point.y,
            pWndPopupOwner);

        menu.DestroyMenu();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}


void CSchedulerView::OnInitialUpdate()
{
    TRACE("CSchedulerView::OnInitialUpdate\n");

    SetTimers();

    if (!IsWindow(m_pbProgress.GetSafeHwnd()))
        m_pbProgress.Create("", 30, 100, TRUE, 0);

    CFlatListView::OnInitialUpdate();
}

void CSchedulerView::OnFileNewTask()
{
    TRACE("CSchedulerView::OnFileNewTask\n");
	
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CTaskPropertySheet tps(TRUE, TRUE);
		LibrarySettings plsTmp;
		loadLibrarySettings(&plsTmp);		
		if (tps.DoModal() == IDOK)
        {
            if (tps.m_pgData.m_strSelectedDrive.IsEmpty() &&
                tps.m_pgData.m_strFolder.IsEmpty() &&
                tps.m_pgData.m_strFile.IsEmpty() &&
				tps.m_pgData.m_strMask.IsEmpty())
            {
                // no data
                return;
            }

            COleDateTime odtTime;
            Schedule scWhen;

            odtTime = tps.m_pgSchedule.m_odtTime;
            scWhen = static_cast<Schedule>(tps.m_pgSchedule.m_iWhen);

            // save the new scheduled task

            if (pDoc->m_bLog && !pDoc->m_bLogOnlyErrors)
            {
                CString strAction;
                CString strData;

                switch (tps.m_pgData.m_tType)
                {
                case Drive:
                    strData = tps.m_pgData.m_strSelectedDrive;
                    break;
                case Folder:
                    strData = tps.m_pgData.m_strFolder;
                    break;
                case File:
                    strData = tps.m_pgData.m_strFile;
                    break;
				case Mask:
					strData = tps.m_pgData.m_strMask;
					break;
                default:
                    NODEFAULT;
                };
	            AfxFormatString1(strAction, IDS_ACTION_NEW, strData);
                pDoc->LogAction(strAction);
            }

			CScheduleItem *psiItem = new CScheduleItem();
            psiItem->SetSchedule(scWhen);
            psiItem->SetTime((WORD)odtTime.GetHour(), (WORD)odtTime.GetMinute());

            switch (tps.m_pgData.m_tType)
            {
            case Drive:
                psiItem->SetDrive(tps.m_pgData.m_strSelectedDrive);
                break;
            case Folder:
                psiItem->SetFolder(tps.m_pgData.m_strFolder);
                psiItem->RemoveFolder(tps.m_pgData.m_bRemoveFolder);
                psiItem->Subfolders(tps.m_pgData.m_bSubfolders);
                psiItem->OnlySubfolders(tps.m_pgData.m_bRemoveOnlySub);
                break;
            case File:
                psiItem->SetFile(tps.m_pgData.m_strFile);
                psiItem->UseWildcards(tps.m_pgData.m_bUseWildCards);
                psiItem->WildcardsInSubfolders(tps.m_pgData.m_bWildCardsInSubfolders);
                break;
			case Mask:
				psiItem->SetMask(tps.m_pgData.m_strMask);
				break;
            default:
                NODEFAULT;
            };
    
            psiItem->CalcNextTime();

            if (!pDoc->AddScheduledTask(psiItem))
            {
                delete psiItem;
                psiItem = 0;
            }
            else
            {
                psiItem->m_uTimerID = GetNextTimerID();

                if (!SetTimer(psiItem->m_uTimerID, psiItem->GetTimeSpan(), NULL))
                {
                    AfxTimeOutMessageBox(IDS_ERROR_TIMER, MB_ICONERROR);

                    if (pDoc->m_bLog)
                        pDoc->LogAction(IDS_ERROR_TIMER);
                }
            }
// Here we just add it to the bootup sequence if it is on boot
				if (scWhen == Reboot) 
				{
				//Here we are setting the appropriate entry into the Registry Key :
  				//HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run
				//The entry should be in Name - Data pair, where Name is the name of the application and Data is the path of the executable
					CKey kReg;
					CString m_strExePath;
					CString m_strName;
					char Fullname[260];
	      		    char Filename[260];
					char Extension[5];
					char Pathname[260];
					char myDrive[10];
					char *buffer = new char[260];

					if (kReg.Open(HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
					{
						 GetModuleFileName(AfxGetInstanceHandle(),Fullname,sizeof Fullname);
					    _splitpath(Fullname,myDrive,Pathname,Filename,Extension); 
						strcpy(buffer,myDrive);
						strncat(buffer,Pathname,250);
						strncpy(Pathname,buffer,260);
						delete buffer;
						
						BOOL bNeed = TRUE;
						m_strExePath = '"';
						m_strExePath+=  CString(Pathname);
						m_strExePath+= "Eraserl.exe";
						m_strExePath+= '"';
						switch (tps.m_pgData.m_tType)
						{
							case Drive:
								m_strExePath+= " -disk " + tps.m_pgData.m_strSelectedDrive;
								break;
							case Folder:
								m_strExePath+= " -folder ";
								m_strExePath+= '"';
								m_strExePath+= tps.m_pgData.m_strFolder;
								m_strExePath+= '"';
								if (tps.m_pgData.m_bRemoveFolder==FALSE) {m_strExePath+= " -keepfolder ";}
								if (tps.m_pgData.m_bSubfolders) {m_strExePath+= " -subfolders ";}
								break;
							case File:
								m_strExePath+= " -file ";
								m_strExePath+= '"';
								m_strExePath+= tps.m_pgData.m_strFile;
								m_strExePath+= '"';
								break;
							case Mask:
								bNeed = FALSE;
								break;
							default:
								NODEFAULT;
					    };
						if (bNeed) kReg.SetValue(m_strExePath,psiItem->GetId());
						m_strExePath.ReleaseBuffer();
						kReg.Close();
					}
				}
            UpdateList();
			
			LibrarySettings* plsTmp1 = tps.m_pPageFileMethodOptions->GetLibSettings();
			psiItem->m_bMethod = plsTmp1->m_nFileMethodID;
			psiItem->m_uEraseItems = plsTmp1->m_uItems;
			psiItem->m_nRndPass = plsTmp1->m_nFileRandom;
			plsTmp1->m_nFileMethodID = plsTmp.m_nFileMethodID;
			plsTmp1->m_nFileRandom = plsTmp.m_nFileRandom;
			plsTmp1->m_uItems = plsTmp.m_uItems;
			saveLibrarySettings(plsTmp1);		
			
			pDoc->CalcNextAssignment();
            pDoc->UpdateToolTip();
            pDoc->SaveTasksToDefault();
			
			
					
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

void CSchedulerView::OnUpdateEditDeleteTask(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetSelectedCount() > 0);
}

void CSchedulerView::OnEditDeleteTask()
{
    TRACE("CSchedulerView::OnEditDeleteTask\n");
	CString strData;
	CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CListCtrl& lc = GetListCtrl();

        if (lc.GetSelectedCount() > 0)
        {
            int nItem, nIndex, iSize = pDoc->m_paScheduledTasks.GetSize();
            CScheduleItem *psiItem = 0;

            POSITION pos = lc.GetFirstSelectedItemPosition();

            while (pos)
            {
                nItem = lc.GetNextSelectedItem(pos);
                nIndex = lc.GetItemData(nItem);

                if (nIndex >= 0 && nIndex < iSize)
                {
                    psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[nIndex]);
                    if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                    {
                        if (pDoc->m_bLog && !pDoc->m_bLogOnlyErrors)
                        {
							CString strAction;
							
                            psiItem->GetData(strData);
							AfxFormatString1(strAction, IDS_ACTION_DELETE, strData);
                            pDoc->LogAction(strAction);
                        }

                        // kill the timer assigned to the task
                        KillTimer(psiItem->m_uTimerID);

                        // turn off the thread just in case it is running
                        TerminateThread(psiItem);
						if (psiItem->GetSchedule() == Reboot) 
								{
									CKey kReg;
									if (kReg.Open(HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
									{
										kReg.DeleteValue(psiItem->GetId());
									kReg.Close();
									}
								}
                            
                        // remove it from the queue just in case it happens to be there
                        RemoveTaskFromQueue(psiItem);

                        delete psiItem;
                        psiItem = 0;

                        pDoc->m_paScheduledTasks.SetAt(nIndex, 0);
                    }
                }
            }

            UpdateList();
            pDoc->CalcNextAssignment();
            pDoc->UpdateToolTip();
            pDoc->SaveTasksToDefault();
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

void CSchedulerView::OnUpdateEditProperties(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetSelectedCount() == 1);
}

void CSchedulerView::OnEditProperties()
{
    TRACE("CSchedulerView::OnEditProperties\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CListCtrl& lc = GetListCtrl();

        if (lc.GetSelectedCount() == 1)
        {
            POSITION pos = lc.GetFirstSelectedItemPosition();

            int nItem = lc.GetNextSelectedItem(pos);
            int nIndex = lc.GetItemData(nItem);

            if (nIndex >= 0 && nIndex < pDoc->m_paScheduledTasks.GetSize())
            {
                CScheduleItem *psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[nIndex]);
                if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                {
                    CString strData;
                    CTaskPropertySheet tps(TRUE, FALSE);

					LibrarySettings plsTmp;
					loadLibrarySettings(&plsTmp);
					if (psiItem->m_bMethod) {
						BOOL bExist = FALSE;
						for (int i = 0; i < plsTmp.m_nCMethods; i++) 
							bExist = (plsTmp.m_lpCMethods->m_nMethodID == psiItem->m_bMethod);
						if (bExist||(psiItem->m_bMethod == GUTMANN_METHOD_ID || psiItem->m_bMethod == DOD_METHOD_ID ||
							psiItem->m_bMethod == DOD_E_METHOD_ID || psiItem->m_bMethod == RANDOM_METHOD_ID ||
							psiItem->m_bMethod == FL2KB_METHOD_ID)){
							//tps.m_pPageFileMethodOptions->SetLibSettings(&plsTmp);
							tps.m_pPageFileMethodOptions->GetLibSettings()->m_nFileMethodID=psiItem->m_bMethod;
							tps.m_pPageFileMethodOptions->GetLibSettings()->m_uItems = psiItem->m_uEraseItems; 
							tps.m_pPageFileMethodOptions->GetLibSettings()->m_nFileRandom = psiItem->m_nRndPass;							
						}
						else {
							tps.m_pPageFileMethodOptions->GetLibSettings()->m_nFileMethodID=plsTmp.m_nFileMethodID;
							psiItem->m_bMethod = plsTmp.m_nFileMethodID;
							psiItem->m_nRndPass = plsTmp.m_nFileRandom;
							psiItem->m_uEraseItems = plsTmp.m_uItems;
						}					
					}

                    tps.m_pgData.m_tType = psiItem->GetType();
                    tps.m_pgData.m_bRemoveFolder = psiItem->RemoveFolder();
                    tps.m_pgData.m_bSubfolders = psiItem->Subfolders();
                    tps.m_pgData.m_bRemoveOnlySub = psiItem->OnlySubfolders();
                    tps.m_pgData.m_bUseWildCards = psiItem->UseWildcards();
                    tps.m_pgData.m_bWildCardsInSubfolders = psiItem->WildcardsInSubfolders();

                    psiItem->GetData(strData);

                    switch (tps.m_pgData.m_tType)
                    {
                    case Drive:
                        tps.m_pgData.m_strSelectedDrive = strData;
                        break;
                    case Folder:
                        tps.m_pgData.m_strFolder = strData;
                        break;
                    case File:
                        tps.m_pgData.m_strFile = strData;
                        break;
					case Mask:
						tps.m_pgData.m_strMask = strData;
						break;
                    default:
                        NODEFAULT;
                    };

                    tps.m_pgSchedule.m_odtTime.SetTime(psiItem->GetHour(), psiItem->GetMinute(), 0);
                    tps.m_pgSchedule.m_iWhen = static_cast<int>(psiItem->GetSchedule());
                    tps.m_pgStatistics.m_lpts = psiItem->GetStatistics();

                    if (tps.DoModal() == IDOK)
                    {
                        if (tps.m_pgData.m_strSelectedDrive.IsEmpty() &&
                            tps.m_pgData.m_strFolder.IsEmpty() &&
                            tps.m_pgData.m_strFile.IsEmpty() &&
							tps.m_pgData.m_strMask.IsEmpty())
                        {
                            // no data
                            return;
                        }

                        if (psiItem->IsRunning())
                        {
                            if (AfxTimeOutMessageBox(IDS_QUESTION_INTERRUPT, MB_ICONWARNING | MB_YESNO) != IDYES)
                                return;

                            TerminateThread(psiItem);
                        }

                        KillTimer(psiItem->m_uTimerID);

                        psiItem->SetSchedule(static_cast<Schedule>(tps.m_pgSchedule.m_iWhen));
                        psiItem->SetTime((WORD)tps.m_pgSchedule.m_odtTime.GetHour(),
                            (WORD)tps.m_pgSchedule.m_odtTime.GetMinute());

                        switch (tps.m_pgData.m_tType)
                        {
                        case Drive:
                            psiItem->SetDrive(tps.m_pgData.m_strSelectedDrive);
                            break;
                        case Folder:
                            psiItem->SetFolder(tps.m_pgData.m_strFolder);
                            psiItem->RemoveFolder(tps.m_pgData.m_bRemoveFolder);
                            psiItem->Subfolders(tps.m_pgData.m_bSubfolders);
                            psiItem->OnlySubfolders(tps.m_pgData.m_bRemoveOnlySub);
                            break;
                        case File:
                            psiItem->SetFile(tps.m_pgData.m_strFile);
                            psiItem->UseWildcards(tps.m_pgData.m_bUseWildCards);
                            psiItem->WildcardsInSubfolders(tps.m_pgData.m_bWildCardsInSubfolders);
                            break;
						case Mask:
							psiItem->SetMask(tps.m_pgData.m_strMask);
							break;
                        default:
                            NODEFAULT;
                        };

                        CString strTmp;
                        psiItem->GetData(strTmp);

                        if (strTmp != strData)
                        {
                            // data has changed -> reset statistics

                            psiItem->GetStatistics()->Reset();

                            COleDateTime odt;
                            odt.SetStatus(COleDateTime::null);

                            psiItem->SetLastTime(odt);
                        }

                        psiItem->CalcNextTime();

                        if (!SetTimer(psiItem->m_uTimerID, psiItem->GetTimeSpan(), NULL))
                        {
                            AfxTimeOutMessageBox(IDS_ERROR_TIMER, MB_ICONERROR);

                            if (pDoc->m_bLog)
                                pDoc->LogAction(IDS_ERROR_TIMER);
                        }
//Update Reboot Part
				if (tps.m_pgSchedule.m_iWhen == Reboot) 
				{
				//Here we are setting the appropriate entry into the Registry Key :
  				//HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run
				//The entry should be in Name - Data pair, where Name is the name of the application and Data is the path of the executable
					CKey kReg;
					CString m_strExePath;
					CString m_strName;
					char Fullname[260];
	      		    char Filename[260];
					char Extension[5];
					char Pathname[260];
					char myDrive[10];
					char *buffer = new char[260];

					if (kReg.Open(HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
					{
						 GetModuleFileName(AfxGetInstanceHandle(),Fullname,sizeof Fullname);
					    _splitpath(Fullname,myDrive,Pathname,Filename,Extension); 
						strcpy(buffer,myDrive);
						strncat(buffer,Pathname,250);
						strncpy(Pathname,buffer,260);
						delete buffer;
						BOOL bNeed = TRUE;	
						m_strExePath = '"';
						m_strExePath+=  CString(Pathname);
						m_strExePath+= "Eraserl.exe";
						m_strExePath+= '"';
						
						switch (tps.m_pgData.m_tType)
						{
							case Drive:
								m_strExePath+= " -disk " + tps.m_pgData.m_strSelectedDrive;
								break;
							case Folder:
								m_strExePath+= " -folder ";
								m_strExePath+= '"';
								m_strExePath+= tps.m_pgData.m_strFolder;
								m_strExePath+= '"';
								if (tps.m_pgData.m_bRemoveFolder==FALSE) {m_strExePath+= "-keepfolder ";}
								if (tps.m_pgData.m_bSubfolders) {m_strExePath+= "-subfolders ";}
								break;
							case File:
								m_strExePath+= " -file ";
								m_strExePath+= '"';
								m_strExePath+= tps.m_pgData.m_strFile;
								m_strExePath+= '"';
								break;
							case Mask:
								bNeed = FALSE;
								break;
							default:
								NODEFAULT;
					    };
						if (bNeed) kReg.SetValue(m_strExePath,psiItem->GetId());
						m_strExePath.ReleaseBuffer();
						kReg.Close();
					}
				}
//						
                        UpdateList();
                        
						LibrarySettings* plsTmp1 = tps.m_pPageFileMethodOptions->GetLibSettings();
						psiItem->m_bMethod = plsTmp1->m_nFileMethodID;
						psiItem->m_uEraseItems = plsTmp1->m_uItems;
						psiItem->m_nRndPass = plsTmp1->m_nFileRandom;
						plsTmp1->m_nFileMethodID = plsTmp.m_nFileMethodID;
						plsTmp1->m_nFileRandom = plsTmp.m_nFileRandom;
						plsTmp1->m_uItems = plsTmp.m_uItems;
						saveLibrarySettings(plsTmp1);	

						pDoc->CalcNextAssignment();
                        pDoc->UpdateToolTip();
                        pDoc->SaveTasksToDefault();	

										

                    }
					
                }
            }
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

void CSchedulerView::OnTimer(UINT_PTR nIDEvent)
{
    TRACE("CSchedulerView::OnTimer\n");
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        int iSize = pDoc->m_paScheduledTasks.GetSize();
        CScheduleItem *psiItem = 0;

        while (iSize--)
        {
            psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iSize]);
			if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)) & !(psiItem->GetSchedule() == Reboot))
            {
                if (psiItem->m_uTimerID == nIDEvent)
                {
                    KillTimer(psiItem->m_uTimerID);

                    if (!pDoc->m_bSchedulerEnabled)
                    {
                        // Scheduler not active at the moment, just
                        // calculate the next time

                        psiItem->CalcNextTime();

                        if (!SetTimer(psiItem->m_uTimerID, psiItem->GetTimeSpan(), NULL))
                        {
                            if (!pDoc->m_bNoVisualErrors)
                                AfxTimeOutMessageBox(IDS_ERROR_TIMER, MB_ICONERROR);

                            if (pDoc->m_bLog)
                                pDoc->LogAction(IDS_ERROR_TIMER);
                        }

                        UpdateList();
                        pDoc->CalcNextAssignment();
                    }
                    else
                    {
                        if (psiItem->IsRunning() || psiItem->IsQueued() || !psiItem->ScheduledNow())
                        {
                            // thread still running, queued or is not due anytime soon,
                            // skip procedure and calculate next time

                            psiItem->CalcNextTime();

                            if (!SetTimer(psiItem->m_uTimerID, psiItem->GetTimeSpan(), NULL))
                            {
                                if (!pDoc->m_bNoVisualErrors)
                                    AfxTimeOutMessageBox(IDS_ERROR_TIMER, MB_ICONERROR);

                                if (pDoc->m_bLog)
                                    pDoc->LogAction(IDS_ERROR_TIMER);
                            }
                        }
                        else if (pDoc->m_bQueueTasks)
                        {
                            counterLock();

                            if (pDoc->m_wProcessCount > 0)
                            {
                                QueueTask(psiItem);
                                UpdateList();
                                counterUnlock();
                            }
                            else
                            {
                                counterUnlock();
                                RunScheduledTask(psiItem);
                            }
                        }
                        else
                        {
                            RunScheduledTask(psiItem);
                        }
                    } // m_bEnabled

                    pDoc->UpdateToolTip();
                    return;
                } // == nIDEvent
            } // valid
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        counterUnlock();

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, MB_ICONERROR);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }

    // no such timer !? kill it anyway...
    KillTimer(nIDEvent);

    CFlatListView::OnTimer(nIDEvent);
}

LRESULT CSchedulerView::OnEraserNotify(WPARAM wParam, LPARAM)
{
    TRACE("CSchedulerView::OnEraserNotify\n");

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

BOOL CSchedulerView::EraserWipeDone()
{
    TRACE("CSchedulerView::EraserWipeDone\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        int iSize = pDoc->m_paScheduledTasks.GetSize();
        CScheduleItem *psiItem = 0;

        while (iSize--)
        {
            psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iSize]);
            if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
            {
                // if thread has been running lately but has stopped since.
                // that is, we have not destroyed the context yet

                if (eraserOK(eraserIsValidContext(psiItem->m_ehContext)) && !psiItem->IsRunning())
                {
                    E_UINT8 uValue = 0;
                    COleDateTime odt = GetTimeTimeZoneBased();
                    psiItem->SetLastTime(odt);

                    // not completed
                    if (eraserOK(eraserCompleted(psiItem->m_ehContext, &uValue)) && !uValue)
                    {
                        // not terminated
                        if (eraserOK(eraserTerminated(psiItem->m_ehContext, &uValue)) && !uValue)
                        {
                            // --> failed
                            if (pDoc->m_bLog)
                            {
                                CString str, strData;
                                psiItem->GetData(strData);

                                AfxFormatString1(str, IDS_ACTION_ERROR, strData);
                                pDoc->LogAction(str);

                                // log failed items
                                E_UINT32 uFailed = 0;
                                E_UINT16 uErrors = 0, uSize = 0;
                                eraserFailedCount(psiItem->m_ehContext, &uFailed);
                                eraserErrorStringCount(psiItem->m_ehContext, &uErrors);

                                if (uErrors > 0)
                                {
                                    for (E_UINT16 uIndex = 0; uIndex < uErrors; uIndex++)
                                    {
                                        if (eraserOK(eraserErrorString(psiItem->m_ehContext, uIndex, 0, &uSize)))
                                        {
                                            if (eraserOK(eraserErrorString(psiItem->m_ehContext, uIndex,
                                                    (LPVOID)strData.GetBuffer((int)uSize), &uSize)))
                                            {
                                                strData.ReleaseBuffer();
                                                pDoc->LogAction(strData);
                                            }
                                            else
                                                strData.ReleaseBuffer();
                                        }
                                    }
                                }

                                if (uFailed > 0)
                                {
                                    UINT nError;

                                    if (psiItem->GetType() == Drive)
                                        nError = IDS_ACTION_ERROR_UNUSED;
                                    else
                                        nError = IDS_ACTION_ERROR_FILE;

                                    for (E_UINT32 uIndex = 0; uIndex < uFailed; uIndex++)
                                    {
                                        if (eraserOK(eraserFailedString(psiItem->m_ehContext, uIndex, 0, &uSize)))
                                        {
                                            if (eraserOK(eraserFailedString(psiItem->m_ehContext, uIndex,
                                                    (LPVOID)strData.GetBuffer((int)uSize), &uSize)))
                                            {
                                                strData.ReleaseBuffer();
                                                AfxFormatString1(str, nError, strData);
                                                pDoc->LogAction(str);
                                            }
                                            else
                                                strData.ReleaseBuffer();
                                        }
                                    }
                                }
                            } // m_bLog

                            // update task statistics
                            psiItem->UpdateStatistics();
                        }
                    } // Done
                    else
                    {
                        if (pDoc->m_bLog && !pDoc->m_bLogOnlyErrors)
                        {
                            CString str, strData;
                            psiItem->GetData(strData);

                            AfxFormatString1(str, IDS_ACTION_DONE, strData);
                            pDoc->LogAction(str);
                        }

                        // update task statistics
                        psiItem->UpdateStatistics();
                        psiItem->GetStatistics()->m_dwTimesSuccess++;
                    }

                    // remove folders

                    if (psiItem->GetType() == Folder && psiItem->RemoveFolder())
                    {
                        CString strFolder;
                        CStringArray saFiles, saFolders;

                        psiItem->GetData(strFolder);

                        parseDirectory((LPCTSTR)strFolder,
                            saFiles,
                            saFolders,
                            psiItem->Subfolders());

                        if (psiItem->OnlySubfolders())
                        {
                            // remove the last folder from the list,
                            // since the user wishes it would not be
                            // removed

                            if (saFolders.GetSize() > 0)
                                saFolders.SetSize(saFolders.GetSize() - 1);
                        }

                        int iSize = saFolders.GetSize();
                        if (iSize > 0)
                        {
                            for (int i = 0; i < iSize; i++)
                            {
                                if (eraserOK(eraserRemoveFolder((LPVOID)(LPCTSTR)saFolders[i],
                                        (E_UINT16)saFolders[i].GetLength(), ERASER_REMOVE_FOLDERONLY)))
                                {
                                    SHChangeNotify(SHCNE_RMDIR, SHCNF_PATH, (LPCTSTR)saFolders[i], NULL);
                                }
                            }

                            saFolders.RemoveAll();
                        }
                    }

					//remove folders on mask clear
					if (psiItem->GetType() == Mask)
					{
						CString strMask;
						CStringArray saFiles, saFolders;

						psiItem->GetData(strMask);

						findMaskedElements(strMask,
							saFiles,
							saFolders);
						
						int iSize = saFolders.GetSize();
						if (iSize > 0)
						{
							for (int i = 0; i < iSize; i++)
							{
								if (eraserOK(eraserRemoveFolder((LPVOID)(LPCTSTR)saFolders[i],
									(E_UINT16)saFolders[i].GetLength(), ERASER_REMOVE_FOLDERONLY)))
								{
									SHChangeNotify(SHCNE_RMDIR, SHCNF_PATH, (LPCTSTR)saFolders[i], NULL);
								}
							}
							saFiles.RemoveAll();
							saFolders.RemoveAll();
						}
					}

                    uValue = 0;
                    eraserTerminated(psiItem->m_ehContext, &uValue);

                    counterLock();

                    if (!uValue && pDoc->m_wProcessCount > 0)
                        pDoc->m_wProcessCount--;

                    counterUnlock();

                    // destroy context
                    VERIFY(eraserOK(eraserDestroyContext(psiItem->m_ehContext)));
                    psiItem->m_ehContext = ERASER_INVALID_CONTEXT;

                } // not running
            } // valid
        }

        // run possible queued tasks
        psiItem = GetNextQueuedTask();

        if (psiItem)
            RunScheduledTask(psiItem);

        UpdateList();
        pDoc->CalcNextAssignment();
        pDoc->UpdateToolTip();

        return TRUE;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        counterUnlock();

        try
        {
            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }

    return FALSE;
}

BOOL CSchedulerView::EraserWipeBegin()
{
    TRACE("CSchedulerView::EraserWipeBegin\n");

    if (IsWindowVisible())
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

        try
        {
            CListCtrl&      lc          = GetListCtrl();
            int             iCount      = lc.GetItemCount();
            DWORD_PTR       iIndex      = 0;
            CScheduleItem   *psiItem    = 0;
            CString         str;
            CString         strOld;

            str.LoadString(IDS_INFO_RUNNING);

            // update information for all active threads
            for (int iItem = 0; iItem < iCount; iItem++)
            {
                iIndex = lc.GetItemData(iItem);

                if (iIndex >= 0 && iIndex < pDoc->m_paScheduledTasks.GetSize())
                {
                    psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iIndex]);
                    if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                    {
                        if (psiItem->IsRunning())
                        {
                            strOld = lc.GetItemText(iItem, 3);

                            if (str.CompareNoCase(strOld) != 0)
                            {
                                SetRedraw(FALSE);
                                lc.SetItemText(iItem, 3, (LPCTSTR)str);
                                SetRedraw(TRUE);
                            }
                        }
                    }
                }
            }

            pDoc->UpdateToolTip();

            return TRUE;
        }
        catch (CException *e)
        {
            ASSERT(FALSE);

            try
            {
                if (pDoc->m_bLog)
                    pDoc->LogException(e);
            }
            catch (...)
            {
            }

            e->Delete();
        }
    }

    return FALSE;
}

BOOL CSchedulerView::EraserWipeUpdate()
{
    TRACE("CSchedulerView::EraserWipeUpdate\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    CListCtrl&  lc              = GetListCtrl();
    BOOL        bSetProgress    = FALSE;

    if (IsWindowVisible())
    {
        SetRedraw(FALSE);

        int             iCount      = lc.GetItemCount();
        int             iIndex      = 0;
        CScheduleItem   *psiItem    = 0;
        CString         str;
        CString         strOld;
        CString         strFormat;

        try
        {
            // update information for all active threads
            for (int iItem = 0; iItem < iCount; iItem++)
            {
                iIndex = lc.GetItemData(iItem);

                if (iIndex >= 0 && iIndex < pDoc->m_paScheduledTasks.GetSize())
                {
                    psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iIndex]);
                    if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                    {
                        if (psiItem->IsRunning())
                        {
                            // if only one task (us) is selected, show the progress
                            // bar on the status bar

                            if (lc.GetSelectedCount() == 1 &&
                                lc.GetItemState(iItem, LVIS_SELECTED) == LVIS_SELECTED)
                            {
                                TCHAR szValue[255];
                                E_UINT16 uSize = 255;
                                E_UINT8 uValue = 0;

                                if (eraserOK(eraserProgGetTotalPercent(psiItem->m_ehContext, &uValue)))
                                    m_pbProgress.SetPos(uValue);

                                if (eraserOK(eraserProgGetMessage(psiItem->m_ehContext, (LPVOID)szValue, &uSize)))
                                    m_pbProgress.SetText((LPCTSTR)szValue);

                                bSetProgress = TRUE;
                            }
                        } // running
                    }
                }
            }
        }
        catch (CException *e)
        {
            ASSERT(FALSE);

            try
            {
                if (pDoc->m_bLog)
                    pDoc->LogException(e);
            }
            catch (...)
            {
            }

            e->Delete();
        }

        SetRedraw(TRUE);
    }

    // if there were no tasks selected, or the selected task
    // was not running, or the Scheduler window is not visible,
    // remove the progress bar

    if (!bSetProgress && m_pbProgress.IsWindowVisible())
        m_pbProgress.Clear();

    pDoc->UpdateToolTip();

    return TRUE;
}


void CSchedulerView::OnUpdate(CView* /*pSender*/, LPARAM lHint, CObject* /*pHint*/)
{
    TRACE("CSchedulerView::OnUpdate\n");

    if (lHint == SCHEDULER_SET_TIMERS)
    {
        SetTimers();
        lHint = 0L;
    }

    if (lHint == 0L)
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

        try
        {
            UpdateList();
            pDoc->CalcNextAssignment();
            pDoc->UpdateToolTip();
        }
        catch (...)
        {
            ASSERT(FALSE);
        }
    }
}

void CSchedulerView::OnDestroy()
{
    TRACE("CSchedulerView::OnDestroy\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());

    if (AfxIsValidAddress(pDoc, sizeof(CEraserDoc)))
    {
        try
        {
            CScheduleItem *psiItem = 0;
            int iSize = pDoc->m_paScheduledTasks.GetSize();

            while (iSize--)
            {
                psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iSize]);
                if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                    KillTimer(psiItem->m_uTimerID);
            }
        }
        catch (CException *e)
        {
            ASSERT(FALSE);

            if (pDoc->m_bLog)
                pDoc->LogException(e);

            e->Delete();
        }
    }

    CFlatListView::OnDestroy();
}

BOOL CSchedulerView::SetTimers()
{
    TRACE("CSchedulerView::SetTimers\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CScheduleItem *psiItem = 0;
        int iSize = pDoc->m_paScheduledTasks.GetSize();

        while (iSize--)
        {
            psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iSize]);
            if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
            {
                if (psiItem->m_uTimerID > 0)
                    KillTimer(psiItem->m_uTimerID);

                psiItem->m_uTimerID = GetNextTimerID();

                if (!psiItem->StillValid())
                    psiItem->CalcNextTime();

                if (!SetTimer(psiItem->m_uTimerID, psiItem->GetTimeSpan(), NULL))
                {
                    AfxTimeOutMessageBox(IDS_ERROR_TIMER, MB_ICONERROR);

                    if (pDoc->m_bLog)
                        pDoc->LogAction(IDS_ERROR_TIMER);

                    return FALSE;
                }
            }
        }

        return TRUE;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        try
        {
            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }

    return FALSE;
}

void CSchedulerView::UpdateList()
{
    TRACE("CSchedulerView::UpdateList\n");

    SetRedraw(FALSE);

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CListCtrl& lc = GetListCtrl();
        CUIntArray uaSelected;

        // get selected items
        POSITION pos = lc.GetFirstSelectedItemPosition();
        while (pos) uaSelected.Add((UINT)lc.GetNextSelectedItem(pos));

        lc.DeleteAllItems();

        CScheduleItem *psiItem = 0;
        int iSize = 0;
        int iItem = 0;

        // item information
        CString strData;
        COleDateTime odtLast;

        BOOL bExists = FALSE;
        WIN32_FIND_DATA findFileData;
        HANDLE hFind = NULL;
        SHFILEINFO sfi;

        LV_ITEM lvi;
        ZeroMemory(&lvi, sizeof(LV_ITEM));

        // remove the progress bar from the window
        m_pbProgress.Clear();

        // clean invalid objects from the task list
        pDoc->CleanList(pDoc->m_paScheduledTasks, sizeof(CScheduleItem));
        iSize = pDoc->m_paScheduledTasks.GetSize();

        // populate the list
        while (iSize--)
        {
            psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[iSize]);
            ASSERT(AfxIsValidAddress(psiItem, sizeof(CScheduleItem)));

            psiItem->GetData(strData);

            SHGetFileInfo((LPCTSTR)strData,
                      0,
                      &sfi,
                      sizeof(SHFILEINFO),
                      SHGFI_SYSICONINDEX |
                      SHGFI_SMALLICON |
                      SHGFI_TYPENAME |
                      SHGFI_DISPLAYNAME);

            switch (psiItem->GetType())
            {
            case File:
                if (psiItem->UseWildcards())
                {
                    bExists = FALSE;
                    break;
                }
                // no break!
            case Folder:
                {
                    if (strData.GetLength() <= _MAX_DRIVE &&
                        strData.Find(":\\") == 1)
                    {
                        // clear all data on a drive!
                        bExists = TRUE;
                    }
                    else
                    {
                        // file information

                        if (strData.GetLength()&&strData[strData.GetLength() - 1] == '\\')
                            strData = strData.Left(strData.GetLength() - 1);

                        hFind = FindFirstFile((LPCTSTR)strData, &findFileData);

                        bExists = (hFind != INVALID_HANDLE_VALUE);

                        if (bExists)
                            VERIFY(FindClose(hFind));
                    }

                    psiItem->GetData(strData);
                }
                break;
            case Drive:
                bExists = TRUE;
                if (strData == DRIVE_ALL_LOCAL)
                {
                    SHGetFileInfo((LPCTSTR)"C:\\",
                                  0,
                                  &sfi,
                                  sizeof(SHFILEINFO),
                                  SHGFI_SYSICONINDEX |
                                  SHGFI_SMALLICON |
                                  SHGFI_TYPENAME |
                                  SHGFI_DISPLAYNAME);
                }
                break;
			case Mask:
				bExists = TRUE;
				break;
            default:
                NODEFAULT;
            }

            // name

            lvi.mask = LVIF_TEXT | LVIF_PARAM | LVIF_IMAGE;

            lvi.iImage = (bExists) ? sfi.iIcon : -1;
            lvi.iItem = iItem;
            lvi.lParam = iSize;
            lvi.iSubItem = ColumnName;

            if (psiItem->GetType() == Drive)
            {
                if (strData == DRIVE_ALL_LOCAL)
                    strData = szLocalDrives;
                else
                    strData = sfi.szDisplayName;
            }

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lvi.iItem = lc.InsertItem(&lvi);

            strData.ReleaseBuffer();

            // type

            lvi.mask = LVIF_TEXT;
            lvi.iSubItem = ColumnType;

            if (psiItem->GetType() == Drive)
                strData = "Unused disk space";
            else if (psiItem->UseWildcards())
                strData = "Wildcard search";
            else
                strData = sfi.szTypeName;

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            // last run

            lvi.iSubItem = ColumnLast;

            odtLast = psiItem->GetLastTime();

            if (odtLast.GetStatus() == COleDateTime::valid)
                strData = odtLast.Format();
            else
                strData.Empty();

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            // next run

            lvi.iSubItem = ColumnNext;

            if (psiItem->IsRunning())
            {
                try
                {
                    strData.LoadString(IDS_INFO_RUNNING);
                }
                catch (CException *e)
                {
                    ASSERT(FALSE);
                    strData.Empty();

                    if (pDoc->m_bLog)
                        pDoc->LogException(e);

                    e->Delete();
                }
            }
            else if (psiItem->IsQueued())
            {
                try
                {
                    strData.LoadString(IDS_INFO_QUEUED);
                }
                catch (CException *e)
                {
                    ASSERT(FALSE);
                    strData.Empty();

                    if (pDoc->m_bLog)
                        pDoc->LogException(e);

                    e->Delete();
                }
            }
            else
            {
                strData = psiItem->GetNextTime().Format();
            }

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            // schedule

            lvi.iSubItem = ColumnSchedule;

            strData = "Every ";
            strData += szScheduleName[psiItem->GetSchedule()];

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            iItem++;
        }

        // set previously selected items as selected again
        int iListCount = lc.GetItemCount();
        int iSelectItem = -1;

        iSize = uaSelected.GetSize();
        for (iItem = 0; iItem < iSize; iItem++)
        {
            iSelectItem = (int)uaSelected[iItem];

            if (iSelectItem < iListCount)
                SelItemRange(TRUE, iSelectItem, iSelectItem);
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    SetRedraw(TRUE);
}

void CSchedulerView::OnLButtonDblClk(UINT /*nFlags*/, CPoint /*point*/)
{
    TRACE("CSchedulerView::OnLButtonDblClk\n");
    OnEditProperties();
}

void CSchedulerView::OnUpdateProcessRun(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetSelectedCount() > 0);
}

void CSchedulerView::OnUpdateProcessRunAll(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetItemCount() > 0);
}

void CSchedulerView::OnProcessRun()
{
    TRACE("CSchedulerView::OnProcessRun\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CListCtrl& lc = GetListCtrl();

        if (lc.GetSelectedCount() > 0)
        {
            POSITION pos = lc.GetFirstSelectedItemPosition();
            int nItem, nIndex;
            BOOL bQueued = FALSE;
            CScheduleItem *psiItem = 0;

            while (pos)
            {
                nItem = lc.GetNextSelectedItem(pos);
                nIndex = lc.GetItemData(nItem);

                if (nIndex >= 0 && nIndex < pDoc->m_paScheduledTasks.GetSize())
                {
                    psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[nIndex]);
                    if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                    {
                        if (psiItem->IsRunning())
                        {
                            // thread still running, skip procedure
                            continue;
                        }
                        else if (pDoc->m_bQueueTasks)
                        {
                            counterLock();

                            if (pDoc->m_wProcessCount > 0)
                            {
                                QueueTask(psiItem);
                                bQueued = TRUE;
                                counterUnlock();
                            }
                            else
                            {
                                counterUnlock();
                                RunScheduledTask(psiItem);
                            }
                        }
                        else
                        {
                            RunScheduledTask(psiItem);
                        }
                    }
                }
            }

            if (bQueued) UpdateList();
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        counterUnlock();

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, 255);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }
}

void CSchedulerView::OnProcessRunAll()
{
    if (AfxMessageBox(IDS_QUESTION_RUNALL, MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2, 0) == IDYES)
    {
        SelItemRange(TRUE, 0, -1);
        OnProcessRun();
    }
}


void CSchedulerView::OnUpdateProcessStop(CCmdUI* pCmdUI)
{
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CListCtrl& lc = GetListCtrl();

        if (lc.GetSelectedCount() == 1 && pDoc->m_wProcessCount > 0)
        {
            POSITION pos = lc.GetFirstSelectedItemPosition();

            int nItem = lc.GetNextSelectedItem(pos);
            int nIndex = lc.GetItemData(nItem);

            if (nIndex >= 0 && nIndex < pDoc->m_paScheduledTasks.GetSize())
            {
                CScheduleItem *psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[nIndex]);
                if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                {
                    if (psiItem->IsRunning() || psiItem->IsQueued())
                    {
                        pCmdUI->Enable();
                        return;
                    }
                }
            }
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    pCmdUI->Enable(FALSE);
}

void CSchedulerView::OnProcessStop()
{
    TRACE("CSchedulerView::OnProcessStop\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CListCtrl& lc = GetListCtrl();

        counterLock();

        if (lc.GetSelectedCount() == 1 && pDoc->m_wProcessCount > 0)
        {
            POSITION pos = lc.GetFirstSelectedItemPosition();

            int nItem = lc.GetNextSelectedItem(pos);
            int nIndex = lc.GetItemData(nItem);

            if (nIndex >= 0 && nIndex < pDoc->m_paScheduledTasks.GetSize())
            {
                CScheduleItem *psiItem = static_cast<CScheduleItem*>(pDoc->m_paScheduledTasks[nIndex]);
                if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                {
                    if (psiItem->IsQueued())
                    {
                        RemoveTaskFromQueue(psiItem);
                        UpdateList();
                    }
                    else
                    {
                        counterUnlock();
                        TerminateThread(psiItem);
                        return;
                    }
                }
            }
        }

        counterUnlock();
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        counterUnlock();

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, MB_ICONERROR);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }
}

void CSchedulerView::OnUpdateItems(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();

    CString str;
    int iCount = lc.GetItemCount();

    str.Format("%u Task", iCount);

    if (iCount != 1)
        str += "s";

    pCmdUI->SetText((LPCTSTR)str);
    pCmdUI->Enable();
}

void CSchedulerView::OnUpdateEditSelectAll(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetItemCount() > 0);
}

void CSchedulerView::OnEditSelectAll()
{
    SelItemRange(TRUE, 0, -1);
}

void CSchedulerView::OnEditRefresh()
{
    UpdateList();
}

BOOL CSchedulerView::TerminateThread(CScheduleItem *psiItem)
{
    // terminates the thread of a scheduled task

    if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
    {
        try
        {
            if (psiItem->IsRunning())
            {
                CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
                ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

                CWaitCursor wait;

                eraserDestroyContext(psiItem->m_ehContext);
                psiItem->m_ehContext = ERASER_INVALID_CONTEXT;

                // decrease process counter
                counterLock();

                if (pDoc->m_wProcessCount > 0)
                    pDoc->m_wProcessCount--;

                counterUnlock();

                // log action if desired
                if (pDoc->m_bLog && !pDoc->m_bLogOnlyErrors)
                {
                    CString strAction, strData;

                    psiItem->GetData(strData);
                    AfxFormatString1(strAction, IDS_ACTION_STOP, strData);

                    pDoc->LogAction(strAction);
                }

                // update task statistics
                psiItem->GetStatistics()->m_dwTimesInterrupted++;
            }

            return TRUE;
        }
        catch (...)
        {
            counterUnlock();
            ASSERT(FALSE);
        }
    }

    return FALSE;
}
E_UINT8 getMetodId(E_UINT8 old)
{
	E_UINT8 res;
	old ^= 0x80;
	for(E_UINT8 i = 0; i < 5; i++)
		if (1 << i == old)
			res = i;
	return res;
}

BOOL CSchedulerView::RunScheduledTask(CScheduleItem *psiItem)
{
    // starts a scheduled task

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CString strData;
        CStringArray saData;
        int iSize = 0, i;
        BOOL bResult = FALSE;

        // stop the timer
        KillTimer(psiItem->m_uTimerID);

        // remove from queue
        RemoveTaskFromQueue(psiItem);

        // just in case
        eraserDestroyContext(psiItem->m_ehContext);
        psiItem->m_ehContext = ERASER_INVALID_CONTEXT;

        // create context
		if (eraserOK(eraserCreateContextEx(&psiItem->m_ehContext,/*(ERASER_METHOD)getMetodId(psiItem->m_bMethod)*/psiItem->m_bMethod,psiItem->m_nRndPass,psiItem->m_uEraseItems)))
		{
            // get data
            psiItem->GetData(strData);

            // set parameters
            switch (psiItem->GetType())
            {
            case Drive:
                VERIFY(eraserOK(eraserSetDataType(psiItem->m_ehContext, ERASER_DATA_DRIVES)));
                if (strData == DRIVE_ALL_LOCAL)
                    GetLocalHardDrives(saData);
                else
                    saData.Add(strData);

                iSize = saData.GetSize();
                for (i = 0; i < iSize; i++)
                {
                    VERIFY(eraserOK(eraserAddItem(psiItem->m_ehContext,
                        (LPVOID)(LPCTSTR)saData[i], (E_UINT16)saData[i].GetLength())));
                }

                break;
            case Folder:
                {
                    CWaitCursor wait;
                    CStringArray saFolders;

                    VERIFY(eraserOK(eraserSetDataType(psiItem->m_ehContext, ERASER_DATA_FILES)));

                    parseDirectory((LPCTSTR)strData,
                                   saData,
                                   saFolders,
                                   psiItem->Subfolders());

                    iSize = saData.GetSize();
                    for (i = 0; i < iSize; i++)
                    {
                        VERIFY(eraserOK(eraserAddItem(psiItem->m_ehContext,
                            (LPVOID)(LPCTSTR)saData[i], (E_UINT16)saData[i].GetLength())));
                    }

                }
                break;
            case File:
                if (psiItem->UseWildcards())
                {
                    findMatchingFiles(strData, saData,
                                      psiItem->WildcardsInSubfolders());
//					CString temp;
//					for(int i = 0; i < saData.GetCount(); i++) {
//						temp += saData[i];
//						temp += "\n";
//					}
//					AfxMessageBox(temp);
//					return false;
                }
                else
                    saData.Add(strData);

                VERIFY(eraserOK(eraserSetDataType(psiItem->m_ehContext, ERASER_DATA_FILES)));

                iSize = saData.GetSize();
                for (i = 0; i < iSize; i++)
                {
                    VERIFY(eraserOK(eraserAddItem(psiItem->m_ehContext,
                        (LPVOID)(LPCTSTR)saData[i], (E_UINT16)saData[i].GetLength())));
                }
                break;
			case Mask:
				{
					CWaitCursor wait;
					CStringArray saFolders;
					VERIFY(eraserOK(eraserSetDataType(psiItem->m_ehContext, ERASER_DATA_FILES)));
					findMaskedElements(strData, saData, saFolders);
					iSize = saData.GetSize();
					for (i = 0; i < iSize; i++)
					{
						VERIFY(eraserOK(eraserAddItem(psiItem->m_ehContext,
							(LPVOID)(LPCTSTR)saData[i], (E_UINT16)saData[i].GetLength())));
					}					
				}
				break;
			default:
                NODEFAULT;
            };

            VERIFY(eraserOK(eraserSetWindow(psiItem->m_ehContext, GetSafeHwnd())));
            VERIFY(eraserOK(eraserSetWindowMessage(psiItem->m_ehContext, WM_ERASERNOTIFY)));

            // start the thread
            bResult = eraserOK(eraserStart(psiItem->m_ehContext));

            if (bResult)
            {
                // increase process counter
                counterLock();
                pDoc->m_wProcessCount++;
                counterUnlock();

                // task statistics
                psiItem->GetStatistics()->m_dwTimes++;
                psiItem->CalcNextTime();

                // log action
                if (pDoc->m_bLog && !pDoc->m_bLogOnlyErrors)
                {
                    CString strAction;

                    AfxFormatString1(strAction, IDS_ACTION_RUN, strData);
                    pDoc->LogAction(strAction);
                }
            }
        }

        // set timer for the next scheduled time
        if (!SetTimer(psiItem->m_uTimerID, psiItem->GetTimeSpan(), NULL))
        {
            if (!pDoc->m_bNoVisualErrors)
                AfxTimeOutMessageBox(IDS_ERROR_TIMER, MB_ICONERROR);

            if (pDoc->m_bLog)
                pDoc->LogAction(IDS_ERROR_TIMER);
        }

        return bResult;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        counterUnlock();

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, 255);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }

    return FALSE;
}

void CSchedulerView::QueueTask(CScheduleItem *psiItem)
{
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
        {
            if (!psiItem->IsQueued() && !psiItem->IsRunning())
            {
                pDoc->m_paQueuedTasks.Add((LPVOID)psiItem);
                psiItem->SetQueued(TRUE);
            }
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, 255);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }
}

CScheduleItem* CSchedulerView::GetNextQueuedTask()
{
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        if (pDoc->m_paQueuedTasks.GetSize() > 0)
        {
            CScheduleItem *psiItem = static_cast<CScheduleItem*>(pDoc->m_paQueuedTasks.GetAt(0));

            if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
                return psiItem;
            else
            {
                // remove this task from the list
                RemoveTaskFromQueue(psiItem);

                // recursive... until we find one or there are no more
                // items on the list
                return GetNextQueuedTask();
            }
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, 255);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }

    return 0;
}

void CSchedulerView::RemoveTaskFromQueue(CScheduleItem *psiItem)
{
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        if (AfxIsValidAddress(psiItem, sizeof(CScheduleItem)))
        {
            psiItem->SetQueued(FALSE);

            CScheduleItem *psiCurrentItem = 0;
            int iSize = pDoc->m_paQueuedTasks.GetSize();

            while (iSize--)
            {
                psiCurrentItem = static_cast<CScheduleItem*>(pDoc->m_paQueuedTasks[iSize]);

                if (psiCurrentItem == psiItem)
                {
                    pDoc->m_paQueuedTasks.RemoveAt(iSize);
                    pDoc->m_paQueuedTasks.FreeExtra();
                    break;
                }
            }
        }

        pDoc->CleanList(pDoc->m_paQueuedTasks, sizeof(CScheduleItem));
    }
    catch (CException *e)
    {
        ASSERT(FALSE);

        try
        {
            if (!pDoc->m_bNoVisualErrors)
            {
                TCHAR szError[255];
                e->GetErrorMessage(szError, 255);
                AfxTimeOutMessageBox(szError, 255);
            }

            if (pDoc->m_bLog)
                pDoc->LogException(e);
        }
        catch (...)
        {
        }

        e->Delete();
    }
}
