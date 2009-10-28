// EraserView.cpp
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

#include "EraserDll\EraserDll.h"

#include "EraserDoc.h"
#include "EraserView.h"

#include "EraserUI\GfxPopupMenu.h"
#include "TaskPropertySheet.h"
#include "EraserDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

enum Columns
{
    ColumnName,
    ColumnSize,
    ColumnType,
    ColumnModified,
    ColumnAttributes
};

static const int iColumnCount = 5;

static const LPTSTR szColumnNames[] =
{
    _T("Name"),
    _T("Size"),
    _T("Type"),
    _T("Modified"),
    _T("Attributes")
};

static const int iMinFirstColumnWidth = 100;
static const int iOtherColumnWidth = 360;

static const int iColumnWidths[] =
{
    -1,
    80,
    110,
    100,
    70
};

/////////////////////////////////////////////////////////////////////////////
// CEraserView

IMPLEMENT_DYNCREATE(CEraserView, CFlatListView)

BEGIN_MESSAGE_MAP(CEraserView, CFlatListView)
    ON_WM_CONTEXTMENU()
    //{{AFX_MSG_MAP(CEraserView)
    ON_WM_SIZE()
    ON_COMMAND(ID_FILE_NEW_TASK, OnFileNewTask)
    ON_WM_DROPFILES()
    ON_UPDATE_COMMAND_UI(ID_EDIT_PROPERTIES, OnUpdateEditProperties)
    ON_COMMAND(ID_EDIT_PROPERTIES, OnEditProperties)
    ON_UPDATE_COMMAND_UI(ID_EDIT_DELETE_TASK, OnUpdateEditDeleteTask)
    ON_COMMAND(ID_EDIT_DELETE_TASK, OnEditDeleteTask)
    ON_WM_LBUTTONDBLCLK()
    ON_UPDATE_COMMAND_UI(ID_EDIT_SELECT_ALL, OnUpdateEditSelectAll)
    ON_COMMAND(ID_EDIT_SELECT_ALL, OnEditSelectAll)
    ON_UPDATE_COMMAND_UI(ID_PROCESS_RUN, OnUpdateProcessRun)
    ON_UPDATE_COMMAND_UI(ID_PROCESS_RUNALL, OnUpdateProcessRunAll)
    ON_COMMAND(ID_PROCESS_RUN, OnProcessRun)
    ON_COMMAND(ID_PROCESS_RUNALL, OnProcessRunAll)
    ON_UPDATE_COMMAND_UI(ID_EDIT_PASTE, OnUpdateEditPaste)
    ON_COMMAND(ID_EDIT_PASTE, OnEditPaste)
    ON_COMMAND(ID_EDIT_REFRESH, OnEditRefresh)
    //}}AFX_MSG_MAP
    ON_UPDATE_COMMAND_UI(ID_INDICATOR_ITEMS, OnUpdateItems)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CEraserView construction/destruction

CEraserView::CEraserView()
{
    TRACE("CEraserView::CEraserView\n");
}

CEraserView::~CEraserView()
{
    TRACE("CEraserView::~CEraserView\n");
}

/////////////////////////////////////////////////////////////////////////////
// CEraserView drawing

void CEraserView::OnDraw(CDC* /*pDC*/)
{

}

/////////////////////////////////////////////////////////////////////////////
// CEraserView diagnostics

#ifdef _DEBUG
void CEraserView::AssertValid() const
{
    CFlatListView::AssertValid();
}

void CEraserView::Dump(CDumpContext& dc) const
{
    CFlatListView::Dump(dc);
}

CEraserDoc* CEraserView::GetDocument() // non-debug version is inline
{
    ASSERT(m_pDocument->IsKindOf(RUNTIME_CLASS(CEraserDoc)));
    return (CEraserDoc*)m_pDocument;
}
#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// CEraserView message handlers

BOOL CEraserView::Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext)
{
    TRACE("CEraserView::Create\n");

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
                lvc.pszText     = szColumnNames[i];
                lvc.iSubItem    = i;
                lvc.cx          = iColumnWidths[i];
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
            }

            ModifyStyleEx(WS_EX_CLIENTEDGE, 0);

            DragAcceptFiles();
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

void CEraserView::OnInitialUpdate()
{
    TRACE("CEraserView::OnInitialUpdate\n");
    CFlatListView::OnInitialUpdate();
}

void CEraserView::OnContextMenu(CWnd*, CPoint point)
{
    if (point.x == -1 && point.y == -1)
    {
        CRect rect;
        GetClientRect(rect);
        ClientToScreen(rect);

        point = rect.TopLeft();
        point.Offset(5, 5);
    }

    try
    {
        CGfxPopupMenu menu;
        menu.LoadMenu(IDR_MENU_ERASERVIEW, IDR_MAINFRAME, this);

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

void CEraserView::OnSize(UINT nType, int cx, int cy)
{
    CFlatListView::OnSize(nType, cx, cy);
    ResizeColumns();
}

void CEraserView::ResizeColumns()
{
    TRACE("CEraserView::ResizeColumns\n");

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

void CEraserView::OnFileNewTask()
{
    TRACE("CEraserView::OnFileNewTask\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    try
    {
        CTaskPropertySheet tps(FALSE);

        if (tps.DoModal() == IDOK)
        {
            if (tps.m_pgData.m_strSelectedDrive.IsEmpty() &&
                tps.m_pgData.m_strFolder.IsEmpty() &&
                tps.m_pgData.m_strFile.IsEmpty())
            {
                // no data
                return;
            }

            CItem *piItem = new CItem();
			piItem->FinishAction(tps.m_pgData.m_dwFinishAction);
			//saveLibrarySettings(tps.m_pPageFileMethodOptions->GetLibSettings());

            switch (tps.m_pgData.m_tType)
            {
            case File:
                piItem->SetFile(tps.m_pgData.m_strFile);
                piItem->UseWildcards(tps.m_pgData.m_bUseWildCards);
                piItem->WildcardsInSubfolders(tps.m_pgData.m_bWildCardsInSubfolders);
                break;
            case Folder:
                piItem->SetFolder(tps.m_pgData.m_strFolder);
                piItem->Subfolders(tps.m_pgData.m_bSubfolders);
                piItem->RemoveFolder(tps.m_pgData.m_bRemoveFolder);
                piItem->OnlySubfolders(tps.m_pgData.m_bRemoveOnlySub);
                break;
            case Drive:
                piItem->SetDrive(tps.m_pgData.m_strSelectedDrive);
                break;
            default:
                delete piItem;
                piItem = 0;
                return;
            }

            piItem->SetPersistent(tps.m_pgData.m_bPersistent);

            if (!pDoc->AddTask(piItem))
            {
                delete piItem;
                AfxMessageBox(IDS_ERROR_NEW_TASK, MB_ICONWARNING, 0);
            }
            else
            {
                UpdateList();
                pDoc->SaveTasksToDefault();
            }

            piItem = 0;
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
}

void CEraserView::OnDropFiles(HDROP hDropInfo)
{
    TRACE("CEraserView::OnDropFiles\n");

    try
    {
        DoPaste(hDropInfo);
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    DragFinish(hDropInfo);
    UpdateList();
}

void CEraserView::DoPaste(HDROP hDrop)
{
    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    CItem *piItem = 0;
    DWORD dwAttributes = 0;
    TCHAR szFile[MAX_PATH];

    ZeroMemory(szFile, MAX_PATH);

    UINT nCount = DragQueryFile(hDrop, (UINT) -1, NULL, 0);

    for (UINT nFile = 0; nFile < nCount; nFile++)
    {
        DragQueryFile(hDrop, nFile, szFile, MAX_PATH);

        dwAttributes = GetFileAttributes(szFile);

        if (dwAttributes != (DWORD) -1)
        {
            piItem = new CItem();

            if (dwAttributes & FILE_ATTRIBUTE_DIRECTORY)
            {
                piItem->SetFolder(szFile);

                // for folders, erase everything
                piItem->RemoveFolder(TRUE);
                piItem->Subfolders(TRUE);
            }
            else
                piItem->SetFile(szFile);

            if (!pDoc->AddTask(piItem))
                delete piItem;

            piItem = 0;
        }
    }
}

void CEraserView::UpdateList()
{
    TRACE("CEraserView::UpdateList\n");

    // if non-existent file or folder is found, it will not
    // be displayed and it will be removed from the task
    // array

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

        CItem           *piItem = 0;
        INT_PTR         iSize   = 0;
        int             iItem   = 0;

        // item information

        CString         strData;
        ULARGE_INTEGER  uiSize;
        DWORD           dwAttributes;
        COleDateTime    odtModified;
        BOOL            bExists = FALSE;

        WIN32_FIND_DATA findFileData;
        HANDLE          hFind   = NULL;
        SHFILEINFO      sfi;

        LV_ITEM         lvi;
        ZeroMemory(&lvi, sizeof(LV_ITEM));

        // clean all invalid entries from the task list
        pDoc->CleanList(pDoc->m_paTasks, sizeof(CItem));
        iSize = pDoc->m_paTasks.GetSize();

        // populate the list
        while (iSize--)
        {
            uiSize.QuadPart = 0;
            dwAttributes    = 0;

            piItem = static_cast<CItem*>(pDoc->m_paTasks[iSize]);
            ASSERT(AfxIsValidAddress(piItem, sizeof(CItem)));

            piItem->GetData(strData);

            SHGetFileInfo((LPCTSTR)strData, 0, &sfi, sizeof(SHFILEINFO),
                      SHGFI_SYSICONINDEX | SHGFI_SMALLICON | SHGFI_TYPENAME |
                      SHGFI_DISPLAYNAME);

            switch (piItem->GetType())
            {
            case File:
                if (piItem->UseWildcards())
                {
                    bExists = FALSE;
                    break;
                }
                // no break!
            case Folder:
                {
                    // file information
                    if (strData.GetLength() <= _MAX_DRIVE &&
                        strData.Find(_T(":\\")) == 1)
                    {
                        // clear all data on a drive!
                        bExists = TRUE;
                    }
                    else
                    {
                        if (strData[strData.GetLength() - 1] == '\\')
                            strData = strData.Left(strData.GetLength() - 1);

                        hFind = FindFirstFile((LPCTSTR)strData, &findFileData);

                        if (hFind != INVALID_HANDLE_VALUE)
                        {
                            VERIFY(FindClose(hFind));

                            dwAttributes = findFileData.dwFileAttributes;

                            if (dwAttributes & FILE_ATTRIBUTE_COMPRESSED)
                            {
                                uiSize.LowPart = GetCompressedFileSize((LPCTSTR)strData,
                                                                       &uiSize.HighPart);
                            }
                            else
                            {
                                uiSize.HighPart = findFileData.nFileSizeHigh;
                                uiSize.LowPart = findFileData.nFileSizeLow;
                            }

                            odtModified = COleDateTime(findFileData.ftLastWriteTime);
                            bExists = TRUE;
                        }
                        else
                        {
                            // does not exist, if not persistent, remove from the list
                            if (!piItem->IsPersistent())
                            {
                                delete piItem;
                                piItem = 0;
                                // we will clean up this later
                                pDoc->m_paTasks.SetAt(iSize, 0);
                                continue;
                            }
                            else
                            {
                                bExists = FALSE;
                            }
                        }
                    }

                    piItem->GetData(strData);
                }
                break;
            case Drive:
                eraserGetFreeDiskSpace((LPVOID)(LPCTSTR)strData, (E_UINT16)strData.GetLength(),
                                       (E_PUINT64)&uiSize.QuadPart);

                bExists = TRUE;
                if (strData == DRIVE_ALL_LOCAL)
                {
                    SHGetFileInfo((LPCTSTR)"C:\\", 0, &sfi, sizeof(SHFILEINFO),
                                  SHGFI_SYSICONINDEX | SHGFI_SMALLICON | SHGFI_TYPENAME |
                                  SHGFI_DISPLAYNAME);
                }
                break;
            default:
                NODEFAULT;
            }


            // name
            lvi.mask        = LVIF_IMAGE | LVIF_TEXT | LVIF_PARAM;
            lvi.iImage      = (bExists) ? sfi.iIcon : -1;
            lvi.iItem       = iItem;
            lvi.lParam      = iSize;
            lvi.iSubItem    = ColumnName;

            if (piItem->GetType() == Drive)
            {
                if (strData == DRIVE_ALL_LOCAL)
                    strData = szLocalDrives;
                else
                    strData = sfi.szDisplayName;
            }

            lvi.pszText     = strData.GetBuffer(strData.GetLength());
            lvi.iItem       = lc.InsertItem(&lvi);

            strData.ReleaseBuffer();

            // size
            lvi.mask        = LVIF_TEXT;
            lvi.iSubItem    = ColumnSize;

            strData.Empty();

            if (piItem->GetType() != Folder && !piItem->UseWildcards())
            {
                // this is how Windows Explorer seems to round file size
                uiSize.QuadPart += 1024;
                uiSize.QuadPart /= 1024;

                if (uiSize.QuadPart > 100000) // > 100 000 KB ~ 97,7 MB
                    strData.Format(_T("%I64u MB"), (uiSize.QuadPart / 1024));
                else if (!(piItem->GetType() == Drive) || uiSize.QuadPart > 0) // > 0 KB
                    strData.Format(_T("%I64u KB"), uiSize.QuadPart);
            }

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            // type
            lvi.iSubItem    = ColumnType;

            if (piItem->GetType() == Drive)
                strData = _T("Unused disk space");
            else if (piItem->UseWildcards())
                strData = _T("Wildcard search");
            else
                strData = sfi.szTypeName;

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            // modified
            if (piItem->GetType() != Drive && bExists)
                strData = odtModified.Format();
            else
                strData.Empty();

            lvi.iSubItem = ColumnModified;

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            // attributes
            strData.Empty();

            if (piItem->GetType() != Drive && bExists)
            {
                if (!(dwAttributes & FILE_ATTRIBUTE_NORMAL))
                {
                    if (dwAttributes & FILE_ATTRIBUTE_READONLY)
                        strData += "R";
                    if (dwAttributes & FILE_ATTRIBUTE_HIDDEN)
                        strData += "H";
                    if (dwAttributes & FILE_ATTRIBUTE_SYSTEM)
                        strData += "S";
                    if (dwAttributes & FILE_ATTRIBUTE_ARCHIVE)
                        strData += "A";
                    if (dwAttributes & FILE_ATTRIBUTE_COMPRESSED)
                        strData += "C";
                    if (dwAttributes & FILE_ATTRIBUTE_ENCRYPTED)
                        strData += "E";
                    if (dwAttributes & FILE_ATTRIBUTE_TEMPORARY)
                        strData += "T";
                }
            }

            lvi.iSubItem = ColumnAttributes;

            lvi.pszText = strData.GetBuffer(strData.GetLength());
            lc.SetItem(&lvi);

            strData.ReleaseBuffer();

            iItem++;
        }

        // no items shown --> no valid tasks
        int iListCount = lc.GetItemCount();

        if (iListCount == 0)
            pDoc->m_paTasks.RemoveAll();
        else
        {
            // set previously selected items as selected again
            int iSelectItem = -1;

            iSize = uaSelected.GetSize();
            for (iItem = 0; iItem < iSize; iItem++)
            {
                iSelectItem = (int)uaSelected[iItem];

                if (iSelectItem < iListCount)
                    SelItemRange(TRUE, iSelectItem, iSelectItem);
            }
        }
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    SetRedraw(TRUE);
}

void CEraserView::OnUpdate(CView* /*pSender*/, LPARAM lHint, CObject* /*pHint*/)
{
    TRACE("CEraserView::OnUpdate\n");

    if (lHint == 0L || lHint == SCHEDULER_SET_TIMERS)
        UpdateList();
}

void CEraserView::OnUpdateEditProperties(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetSelectedCount() == 1);
}

void CEraserView::OnEditProperties()
{
    TRACE("CEraserView::OnEditProperties\n");

    CListCtrl& lc = GetListCtrl();

    if (lc.GetSelectedCount() == 1)
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

        POSITION pos = lc.GetFirstSelectedItemPosition();

        int       nItem  = lc.GetNextSelectedItem(pos);
        INT_PTR   nIndex = static_cast<INT_PTR>(lc.GetItemData(nItem));

        if (nIndex >= 0 && nIndex < pDoc->m_paTasks.GetSize())
        {
            CItem *piItem = static_cast<CItem*>(pDoc->m_paTasks[nIndex]);
            if (AfxIsValidAddress(piItem, sizeof(CItem)))
            {
                CTaskPropertySheet tps(FALSE);

                tps.m_pgData.m_tType = piItem->GetType();

                switch (tps.m_pgData.m_tType)
                {
                case File:
                    piItem->GetData(tps.m_pgData.m_strFile);
                    tps.m_pgData.m_bUseWildCards = piItem->UseWildcards();
                    tps.m_pgData.m_bWildCardsInSubfolders = piItem->WildcardsInSubfolders();
                    break;
                case Folder:
                    piItem->GetData(tps.m_pgData.m_strFolder);
                    tps.m_pgData.m_bSubfolders = piItem->Subfolders();
                    tps.m_pgData.m_bRemoveFolder = piItem->RemoveFolder();
                    tps.m_pgData.m_bRemoveOnlySub = piItem->OnlySubfolders();
                    break;
                case Drive:
                    piItem->GetData(tps.m_pgData.m_strSelectedDrive);
                    break;
                default:
                    return;
                }

				tps.m_pgData.m_dwFinishAction = piItem->FinishAction();
                tps.m_pgData.m_bPersistent = piItem->IsPersistent();

                if (tps.DoModal() == IDOK)
                {
                    if (tps.m_pgData.m_strSelectedDrive.IsEmpty() &&
                        tps.m_pgData.m_strFolder.IsEmpty() &&
                        tps.m_pgData.m_strFile.IsEmpty())
                    {
                        // no data
                        return;
                    }
					
					piItem->FinishAction(tps.m_pgData.m_dwFinishAction);

                    switch (tps.m_pgData.m_tType)
                    {
                    case File:
                        piItem->SetFile(tps.m_pgData.m_strFile);
                        piItem->UseWildcards(tps.m_pgData.m_bUseWildCards);
                        piItem->WildcardsInSubfolders(tps.m_pgData.m_bWildCardsInSubfolders);
                        break;
                    case Folder:
                        piItem->SetFolder(tps.m_pgData.m_strFolder);
                        piItem->Subfolders(tps.m_pgData.m_bSubfolders);
                        piItem->RemoveFolder(tps.m_pgData.m_bRemoveFolder);
                        piItem->OnlySubfolders(tps.m_pgData.m_bRemoveOnlySub);
                        break;
                    case Drive:
                        piItem->SetDrive(tps.m_pgData.m_strSelectedDrive);
                        break;
                    default:
                        return;
                    }

                    piItem->SetPersistent(tps.m_pgData.m_bPersistent);

                    UpdateList();
                    pDoc->SaveTasksToDefault();

                    piItem = 0;					
                }
            }
        }
    }
}

void CEraserView::OnUpdateEditDeleteTask(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetSelectedCount() > 0);
}

void CEraserView::OnEditDeleteTask()
{
    TRACE("CEraserView::OnEditDeleteTask\n");

    CListCtrl& lc = GetListCtrl();

	if (lc.GetSelectedCount() > 0 && AfxMessageBox(_T("Are you sure you want to ")
		_T("delete the selected tasks?"), MB_YESNO | MB_ICONQUESTION) == IDYES)
    {
        CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
        ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

        int       nItem;
        INT_PTR   nIndex;
        INT_PTR   iSize   = pDoc->m_paTasks.GetSize();
        CItem     *piItem = 0;

        POSITION pos = lc.GetFirstSelectedItemPosition();

        while (pos)
        {
            nItem   = lc.GetNextSelectedItem(pos);
            nIndex  = static_cast<INT_PTR>(lc.GetItemData(nItem));

            if (nIndex >= 0 && nIndex < iSize)
            {
                piItem = static_cast<CItem*>(pDoc->m_paTasks[nIndex]);
                if (AfxIsValidAddress(piItem, sizeof(CItem)))
                {
                    delete piItem;
                    piItem = 0;

                    pDoc->m_paTasks.SetAt(nIndex, 0);
                }
            }
        }

        UpdateList();
        pDoc->SaveTasksToDefault();
    }
}

void CEraserView::OnLButtonDblClk(UINT /*nFlags*/, CPoint /*point*/)
{
    OnEditProperties();
}

void CEraserView::OnUpdateEditSelectAll(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetItemCount() > 0);
}

void CEraserView::OnEditSelectAll()
{
    TRACE("CEraserView::OnEditSelectAll\n");
    SelItemRange(TRUE, 0, -1);
}

void CEraserView::OnUpdateItems(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();

    CString str;
    int     iCount = lc.GetItemCount();

    str.Format(_T("%u Item"), iCount);

    if (iCount != 1)
        str += _T("s");

    pCmdUI->SetText((LPCTSTR)str);
    pCmdUI->Enable();
}

void CEraserView::OnUpdateProcessRun(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetSelectedCount() > 0);
}

void CEraserView::OnUpdateProcessRunAll(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();
    pCmdUI->Enable(lc.GetItemCount() > 0);
}

void CEraserView::OnProcessRunAll()
{
    SelItemRange(TRUE, 0, -1);
    OnProcessRun();
}

void CEraserView::OnProcessRun()
{
    TRACE("CEraserView::OnProcessRun\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));
    CListCtrl& lc = GetListCtrl();

    if (lc.GetSelectedCount() > 0)
    {
        if (AfxMessageBox(IDS_QUESTION_CONFIRMATION, MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2, 0) == IDYES)
        {
            CFileLockResolver resolver(pDoc->m_bResolveAskUser);
			CWaitCursor wait;			
            CEraserDlg ed(this);
			if (pDoc->m_bResolveLock)
				ed.m_pLockResolver = &resolver;
			

            ed.m_bResultsForFiles       = pDoc->m_bResultsForFiles;
            ed.m_bResultsForUnusedSpace = pDoc->m_bResultsForUnusedSpace;
            ed.m_bResultsOnlyWhenFailed = pDoc->m_bResultsOnlyWhenFailed;
			

            // array of selected tasks
            CPtrArray   paSelected;
            CDWordArray daRemovedIfComplete;
            CItem       *piItem = 0;
            int         nItem = -1;
            DWORD_PTR   nIndex = 0;
            INT_PTR     iSize = pDoc->m_paTasks.GetSize();
            POSITION    pos = lc.GetFirstSelectedItemPosition();

            // see below - need to remove items whose existence won't
            // be verified in UpdateList
			DWORD finish_action(0);
            while (pos)
            {
                nItem = lc.GetNextSelectedItem(pos);
				nIndex = lc.GetItemData(nItem);

                if (nIndex >= 0 && nIndex < iSize)
                {
                    piItem = static_cast<CItem*>(pDoc->m_paTasks[nIndex]);
                    if (AfxIsValidAddress(piItem, sizeof(CItem)))
                    {
                        paSelected.Add((LPVOID)piItem);

                        if (!piItem->IsPersistent())
                        {
                            // item exists if it is a drive, a wildcard search, or a folder
                            // that won't be removed after erasing
                            if (piItem->GetType() == Drive ||
                                (piItem->GetType() == File && piItem->UseWildcards()) ||
                                (piItem->GetType() == Folder && (!piItem->RemoveFolder() || piItem->OnlySubfolders())))
                            {
                                daRemovedIfComplete.Add(static_cast<DWORD>(nIndex));
                            }
                        }
						if (piItem->FinishAction())
							finish_action = piItem->FinishAction();
                    }
                }
            }

			ed.m_dwFinishAction = finish_action;
            if (ed.Initialize(&paSelected))
            {
                if (ed.DoModal() != IDCANCEL)
                {
                    try
                    {
                        // remove all drives and wildcard searches from the list,
                        // unless the task is persistent - UpdateList will take care
                        // of everything else

                        INT_PTR iSize = daRemovedIfComplete.GetSize(), nIndex;
                        CItem *piItem = 0;

                        while (iSize--)
                        {
                            nIndex = daRemovedIfComplete[iSize];
                            piItem = static_cast<CItem*>(pDoc->m_paTasks[nIndex]);

                            if (AfxIsValidAddress(piItem, sizeof(CItem)))
                            {
                                pDoc->m_paTasks.SetAt(nIndex, 0);
                                delete piItem;
                                piItem = 0;
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

                UpdateList();
            }
        }
    }
}

void CEraserView::OnUpdateEditPaste(CCmdUI* pCmdUI)
{
    COleDataObject odj;

    if (odj.AttachClipboard())
    {
        if (odj.IsDataAvailable(CF_HDROP))
        {
            pCmdUI->Enable();
            return;
        }
    }

    pCmdUI->Enable(FALSE);
}

void CEraserView::OnEditPaste()
{
    TRACE("CEraserView::OnEditPaste\n");

    CEraserDoc *pDoc = static_cast<CEraserDoc*>(GetDocument());
    ASSERT(AfxIsValidAddress(pDoc, sizeof(CEraserDoc)));

    COleDataObject odj;

    if (odj.AttachClipboard())
    {
        if (odj.IsDataAvailable(CF_HDROP))
        {
            STGMEDIUM StgMed;
            FORMATETC fmte = { CF_HDROP,
                (DVTARGETDEVICE FAR *)NULL,
                DVASPECT_CONTENT,
                -1,
                TYMED_HGLOBAL };

            if (odj.GetData(CF_HDROP, &StgMed, &fmte))
            {
                try
                {
                    DoPaste((HDROP)StgMed.hGlobal);
                }
                catch (CException *e)
                {
                    ASSERT(FALSE);
                    REPORT_ERROR(e);
                    e->Delete();
                }

                if (StgMed.pUnkForRelease)
                    StgMed.pUnkForRelease->Release();
                else
                {
                    ::GlobalFree(StgMed.hGlobal);
                }

                UpdateList();
                pDoc->SaveTasksToDefault();
            }
        }
    }
}



void CEraserView::OnEditRefresh()
{
    UpdateList();
}