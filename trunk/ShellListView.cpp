// ShellList.cpp
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
#include "resource.h"
#include "EraserDoc.h"
#include "ShellListView.h"

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
    "Name",
    "Size",
    "Type",
    "Modified",
    "Attributes"
};

static const int iMinFirstColumnWidth = 100;
static const int iOtherColumnWidth = 410;

static const int iColumnWidths[] =
{
    -1,
    80,
    150,
    110,
    70
};

/////////////////////////////////////////////////////////////////////////////
// CShellListView

CShellListView::CShellListView() :
m_hwndTree(NULL),
m_hChangeNotification(INVALID_HANDLE_VALUE),
m_evKillThread(TRUE, TRUE),
m_evNotRunning(TRUE, TRUE),
m_ptvCurrent(NULL),
m_bDragRight(FALSE)
{
    TRACE("CShellListView::CShellListView\n");
}

CShellListView::~CShellListView()
{
    TRACE("CShellListView::~CShellListView\n");

     // close the possible thread
    m_evKillThread.SetEvent();
    WaitForSingleObject(m_evNotRunning, INFINITE);

    // close the notification handle
    if (m_hChangeNotification != INVALID_HANDLE_VALUE)
    {
        VERIFY(FindCloseChangeNotification(m_hChangeNotification));
        m_hChangeNotification = INVALID_HANDLE_VALUE;
    }

    // release the item data for the current folder
    DeleteTVItemData(&m_ptvCurrent);
}


BEGIN_MESSAGE_MAP(CShellListView, CFlatListView)
    //{{AFX_MSG_MAP(CShellListView)
    ON_WM_SIZE()
    ON_NOTIFY_REFLECT(NM_RCLICK, OnRclick)
    ON_NOTIFY_REFLECT(NM_DBLCLK, OnDblclk)
    ON_NOTIFY_REFLECT(LVN_BEGINDRAG, OnBeginDrag)
    ON_NOTIFY_REFLECT(LVN_BEGINRDRAG, OnBeginRDrag)
    ON_NOTIFY_REFLECT(LVN_DELETEITEM, OnDeleteItem)
    ON_COMMAND(ID_EDIT_REFRESH, OnEditRefresh)
    ON_COMMAND(ID_EDIT_SELECT_ALL, OnEditSelectAll)
    //}}AFX_MSG_MAP
    ON_UPDATE_COMMAND_UI(ID_INDICATOR_ITEMS, OnUpdateItems)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CShellListView operations

BOOL CShellListView::PopulateListView(LPITEMIDLIST lpidl, LPSHELLFOLDER lpsf, int& iFolderCount)
{
    TRACE("CShellListView::PopulateListView\n");

    SetRedraw(FALSE);

    CListCtrl&  lc = GetListCtrl();
    BOOL        bResult = FALSE;
    TCHAR       szPath[MAX_PATH];

    // close the possible thread
    m_evKillThread.SetEvent();
    WaitForSingleObject(m_evNotRunning, INFINITE);

    // close the old file notification handle
    if (m_hChangeNotification != INVALID_HANDLE_VALUE)
    {
        VERIFY(FindCloseChangeNotification(m_hChangeNotification));
        m_hChangeNotification = INVALID_HANDLE_VALUE;
    }

    // open a new file notification handle for the current
    // directory
    if (SHGetPathFromIDList(lpidl, szPath))
    {
        m_hChangeNotification =
            FindFirstChangeNotification(szPath, FALSE,
                                        FILE_NOTIFY_CHANGE_FILE_NAME |
                                        FILE_NOTIFY_CHANGE_DIR_NAME);

        m_evKillThread.ResetEvent();

        // start the refresh thread
        AfxBeginThread(RefreshThread, (LPVOID)this);
    }

    lc.DeleteAllItems();

    if (InitListViewItems(lpidl, lpsf, iFolderCount))
    {
        lc.SortItems(ListViewCompareProc, 0);
        bResult = TRUE;
    }

    SetRedraw(TRUE);
    return bResult;
}

void CShellListView::ResizeColumns()
{
    TRACE("CShellListView::ResizeColumns\n");

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

BOOL CShellListView::InitListViewItems(LPITEMIDLIST lpidl, LPSHELLFOLDER lpsf, int& iFolderCount)
{
    TRACE("CShellListView::InitListViewitems\n");

    LVITEM          lvi;
    int             iCtr            = 0;
    int             idx;

    HRESULT         hr;
    LPMALLOC        lpMalloc;
    LPITEMIDLIST    lpifqThisItem;
    LPITEMIDLIST    lpi             = NULL;
    LPLVITEMDATA    lplvid;
    ULONG           ulFetched;
    ULONG           ulAttrs;

    LPENUMIDLIST    lpe             = NULL;

    HWND            hwnd            = ::GetParent(m_hWnd);
    UINT            uFlags;
    SHFILEINFO      sfi;
    TCHAR           szBuffer[MAX_PATH];

    HANDLE          hFind           = INVALID_HANDLE_VALUE;
    WIN32_FIND_DATA wfdFind;

    BOOL            bResult         = TRUE;
    CListCtrl&      clc             = GetListCtrl();

    CWaitCursor     wait;

    iFolderCount = 0;
    lvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;

    if (SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        hr = lpsf->EnumObjects(hwnd, SHCONTF_FOLDERS | SHCONTF_NONFOLDERS | SHCONTF_INCLUDEHIDDEN, &lpe);

        if (SUCCEEDED(hr))
        {
            while (S_OK == lpe->Next(1, &lpi, &ulFetched))
            {
                //OK, let's get some memory for our ITEMDATA struct
                lplvid = (LPLVITEMDATA)lpMalloc->Alloc(sizeof(LVITEMDATA));

                if (!lplvid)
                {
                    bResult = FALSE;
                    break;
                }

                //Now get the friendly name that we'll put in the list...
                GetName(lpsf, lpi, SHGDN_NORMAL, szBuffer);

                //Note that since we are interested in the display attributes as well as
                //the other attributes, we need to set ulAttrs to SFGAO_DISPLAYATTRMASK
                //before calling GetAttributesOf();

                ulAttrs = SFGAO_DISPLAYATTRMASK;

                lpsf->GetAttributesOf(1, (LPCITEMIDLIST*)&lpi, &ulAttrs);
                lplvid->ulAttribs = ulAttrs;

                lpifqThisItem   = ConcatPidls(lpidl, lpi);

                lvi.iItem       = iCtr++;
                lvi.iSubItem    = ColumnName;
                lvi.pszText     = szBuffer;
                lvi.cchTextMax  = MAX_PATH;
                lvi.iImage      = GetItemIcon(lpifqThisItem,
                                              SHGFI_PIDL | SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
                uFlags          = SHGFI_PIDL | SHGFI_SYSICONINDEX | SHGFI_SMALLICON;

                if (lplvid->ulAttribs & SFGAO_LINK)
                {
                    lvi.mask |= LVIF_STATE;
                    lvi.state = INDEXTOOVERLAYMASK(2);
                }
                else
                {
                    lvi.mask &= ~(LVIF_STATE);
                    lvi.state = 0;
                }

                lplvid->lpsfParent = lpsf;
                lpsf->AddRef();

                // now, make a copy of the ITEMIDLIST
                lplvid->lpi = CopyITEMID(lpMalloc, lpi);

                lvi.lParam = (LPARAM)lplvid;

                // Add the item to the listview;
                idx = clc.InsertItem(&lvi);

                if (idx >= 0)
                {
                    if (ulAttrs & SFGAO_FILESYSTEM)
                    {
                        if (ulAttrs & SFGAO_FOLDER)
                            iFolderCount++;

                        if (SHGetPathFromIDList(lpifqThisItem, szBuffer))
                        {
                            hFind = FindFirstFile((LPCTSTR)szBuffer, &wfdFind);

                            if (hFind != INVALID_HANDLE_VALUE)
                            {
                                VERIFY(FindClose(hFind));

                                CString strData;

                                if (!(ulAttrs & SFGAO_FOLDER))
                                {
                                    ULARGE_INTEGER uiSize;
                                    uiSize.HighPart = wfdFind.nFileSizeHigh;
                                    uiSize.LowPart = wfdFind.nFileSizeLow;

                                    strData.Format("%I64uKB", (uiSize.QuadPart + 1024) / 1024);
                                    clc.SetItemText(idx, ColumnSize, strData);
                                    strData.Empty();
                                }

                                if (!(wfdFind.dwFileAttributes & FILE_ATTRIBUTE_NORMAL))
                                {
                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_READONLY)
                                        strData += "R";

                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN)
                                        strData += "H";

                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_SYSTEM)
                                        strData += "S";

                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_ARCHIVE)
                                        strData += "A";

                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_COMPRESSED)
                                        strData += "C";

                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_ENCRYPTED)
                                        strData += "E";

                                    if (wfdFind.dwFileAttributes & FILE_ATTRIBUTE_TEMPORARY)
                                        strData += "T";
                                }

                                clc.SetItemText(idx, ColumnAttributes, strData);

                                COleDateTime odtCreation(wfdFind.ftLastWriteTime);
                                clc.SetItemText(idx, ColumnModified, odtCreation.Format());
                            }
                        }
                    }

                    SHGetFileInfo((LPCSTR)lpifqThisItem,
                                  0,
                                  &sfi,
                                  sizeof(SHFILEINFO),
                                  SHGFI_PIDL | SHGFI_TYPENAME);

                    clc.SetItemText(idx, ColumnType, sfi.szTypeName);
                }
                else
                {
                    bResult = FALSE;
                    break;
                }

                lpMalloc->Free(lpifqThisItem);
                lpifqThisItem = 0;

                lpMalloc->Free(lpi);  //Finally, free the pidl that the shell gave us...
                lpi = 0;
            }

            lpe->Release();
        }

        lpMalloc->Release();
    }
    else
    {
        // SHGetMalloc failed
        bResult = FALSE;
    }

    return bResult;
}

int CALLBACK CShellListView::ListViewCompareProc(LPARAM lparam1, LPARAM lparam2, LPARAM /*lparamSort*/)
{
    LPLVITEMDATA    lplvid1 = (LPLVITEMDATA)lparam1;
    LPLVITEMDATA    lplvid2 = (LPLVITEMDATA)lparam2;
    HRESULT         hr;

    hr = lplvid1->lpsfParent->CompareIDs(0,lplvid1->lpi,lplvid2->lpi);

    if (FAILED(hr))
       return 0;

    return (short)SCODE_CODE(GetScode(hr));
}

DWORD CShellListView::GetCurrentView()
{
    TRACE("CShellListView::GetCurrentView\n");

    DWORD dwStyle = GetWindowLong(m_hWnd, GWL_STYLE);
    return (dwStyle & LVS_TYPEMASK);
}

void CShellListView::GetNormalAndSelectedIcons(LPITEMIDLIST lpifq, LPTVITEM lptvitem)
{
    ASSERT(lpifq != NULL);
    ASSERT(lptvitem != NULL);

    // Note that we don't check the return value here because if GetIcon()
    // fails, then we're in big trouble...

    lptvitem->iImage = GetItemIcon(lpifq,
                                   SHGFI_PIDL | SHGFI_SYSICONINDEX |
                                   SHGFI_SMALLICON);

    lptvitem->iSelectedImage = GetItemIcon(lpifq,
                                           SHGFI_PIDL | SHGFI_SYSICONINDEX |
                                           SHGFI_SMALLICON | SHGFI_OPENICON);
    return;
}

/////////////////////////////////////////////////////////////////////////////
// CShellListView message handlers

int CShellListView::GetDoubleClickedItem()
{
    POINT           pt;
    LVHITTESTINFO   lvhti;
    CListCtrl&      lc = GetListCtrl();

    ::GetCursorPos((LPPOINT)&pt);
    lc.ScreenToClient(&pt);

    lvhti.pt = pt;
    lc.HitTest(&lvhti);

    if (!(lvhti.flags & LVHT_ONITEM))
        lvhti.iItem = -1;

    return lvhti.iItem;
}

void CShellListView::GetContextMenu()
{
    TRACE("CShellListView::GetContextMenu\n");

    LPMALLOC        lpMalloc;
    LPLVITEMDATA    lplvid;
    LPITEMIDLIST    *ppidlArray = 0;
    int             iCount = 0;
    int             iItem = 0;
    CListCtrl&      lc = GetListCtrl();
    POSITION        pos = lc.GetFirstSelectedItemPosition();
    POINT           pt;

    if (lc.GetSelectedCount() == 0)
        return;

    // get position where to show the menu
    ::GetCursorPos((LPPOINT)&pt);

    // get access to the task allocator
    if (FAILED(SHGetMalloc(&lpMalloc)))
        return;

    // allocate memory for the array consisting of pointers to
    // ITEMIDLIST structures

    ppidlArray = (LPITEMIDLIST*) lpMalloc->Alloc(lc.GetSelectedCount() *
                                                 sizeof(LPITEMIDLIST));

    if (ppidlArray)
    {
        // fill the array

        while (pos != NULL)
        {
            iItem = lc.GetNextSelectedItem(pos);
            lplvid = (LPLVITEMDATA) lc.GetItemData(iItem);

            ppidlArray[iCount] = lplvid->lpi;
            iCount++;
        }

        // show the menu and execute commands
        DoTheMenuThing(m_hWnd, lplvid->lpsfParent, ppidlArray, &pt, iCount);

        // the the pointer array
        lpMalloc->Free(ppidlArray);
    }

    // release task allocator
    lpMalloc->Release();
}

void CShellListView::ShellOpenItem(int iItem)
{
    CListCtrl& lc = GetListCtrl();
    ShellOpenItem((LPLVITEMDATA)lc.GetItemData(iItem));
}


void CShellListView::ShellOpenItem(LPLVITEMDATA lplvid)
{
    TRACE("CShellListView::ShellOpenItem\n");

    // try to do the default thing first; if no default menu item is
    // specified, try using the ShellExecuteEx (works only on files)

    if (!DoTheDefaultThing(m_hWnd, lplvid->lpsfParent, &lplvid->lpi))
    {
        if (!(lplvid->ulAttribs & (SFGAO_FOLDER | SFGAO_FILESYSANCESTOR | SFGAO_REMOVABLE)))
        {
            SHELLEXECUTEINFO sei =
            {
                sizeof(SHELLEXECUTEINFO),
                SEE_MASK_INVOKEIDLIST,  // fMask
                ::GetParent(m_hWnd),    // hwnd of parent
                "",                     // lpVerb
                NULL,                   // lpFile
                "",
                "",                     // lpDirectory
                SW_SHOWNORMAL,          // nShow
                AfxGetInstanceHandle(), // hInstApp
                (LPVOID)NULL,           // lpIDList...will set below
                NULL,                   // lpClass
                0,                      // hkeyClass
                0,                      // dwHotKey
                NULL                    // hIcon
            };

            sei.lpIDList = GetFullyQualPidl(lplvid->lpsfParent, lplvid->lpi);
            ShellExecuteEx(&sei);
        }
    }
}

BOOL CShellListView::GetItemPath(int iItem, CString &strItemPath)
{
    TRACE("CShellListView::GetItemPath\n");

    LPLVITEMDATA    lplvid;  //Long pointer to TreeView item data
    BOOL            bReturn = FALSE;
    CListCtrl&      lc = GetListCtrl();

    if (iItem >= 0)
    {
        lplvid = (LPLVITEMDATA) lc.GetItemData(iItem);

        if (lplvid)
        {
            try
            {
                bReturn =
                    SHGetPathFromIDList(GetFullyQualPidl(lplvid->lpsfParent, lplvid->lpi),
                                        strItemPath.GetBuffer(MAX_PATH));

                strItemPath.ReleaseBuffer();
            }
            catch (CException *e)
            {
                ASSERT(FALSE);
                REPORT_ERROR(e);
                e->Delete();
            }
            catch (...)
            {
                ASSERT(FALSE);
            }
        }
    }

    return bReturn;
}

void CShellListView::OnDragDrop(NMHDR* /*pNMHDR*/, LRESULT* pResult)
{
    TRACE("CShellListView::OnDragDrop\n");

    // OLE
    LPMALLOC        lpMalloc;
    LPDATAOBJECT    lpdo;
    LPLVITEMDATA    lplvid;
    LPITEMIDLIST    *ppidlArray = 0;
    HRESULT         hr;

    if (FAILED(SHGetMalloc(&lpMalloc)))
        return;

    // list view
    int             iCount = 0;
    int             iItem = 0;
    CListCtrl&      lc = GetListCtrl();
    POSITION        pos = lc.GetFirstSelectedItemPosition();


    // allocate memory for the array consisting of pointers to
    // ITEMIDLIST structures

    ppidlArray = (LPITEMIDLIST*) lpMalloc->Alloc(lc.GetSelectedCount() *
                                                 sizeof(LPITEMIDLIST));

    if (ppidlArray)
    {
        // fill the array

        while (pos != NULL)
        {
            iItem = lc.GetNextSelectedItem(pos);
            lplvid = (LPLVITEMDATA) lc.GetItemData(iItem);

            ppidlArray[iCount] = lplvid->lpi;
            iCount++;
        }

        hr = lplvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                               iCount,
                                               (LPCITEMIDLIST*)ppidlArray,
                                               IID_IDataObject,
                                               0,
                                               (LPVOID *)&lpdo);

        if (SUCCEEDED(hr))
        {
            // data object & source
            COleDataObject odo;
            COleDataSource ods;

            STGMEDIUM stgMedium;
            FORMATETC fmte = { CF_HDROP, (DVTARGETDEVICE FAR *)NULL,
                               DVASPECT_CONTENT, -1, TYMED_HGLOBAL };

            // attach retrieved IDataObject to the object
            odo.Attach(lpdo);

            // get data
            odo.GetData(CF_HDROP, &stgMedium, &fmte);

            // set data to the source
            ods.CacheData(CF_HDROP, &stgMedium, &fmte);

            // perform the drag (and drop)
            ods.DoDragDrop();

            // yeah, we made it
            *pResult = 0;
        }

        lpMalloc->Free(ppidlArray);
    }

    lpMalloc->Release();
}

BOOL CShellListView::Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext)
{
    TRACE("CShellListView::Create\n");

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
            lvc.pszText     = szColumnNames[0];
            lvc.cx          = iWidth;
            lvc.iSubItem    = 0;
            lcList.InsertColumn(0, &lvc);

            for (int i = 1; i <= (iColumnCount - 1); i++)
            {
                lvc.pszText     = szColumnNames[i];
                lvc.iSubItem    = i;
                lvc.cx          = iColumnWidths[i];
                lcList.InsertColumn(i, &lvc);
            }

            lcList.SetExtendedStyle(LVS_EX_HEADERDRAGDROP | LVS_EX_FULLROWSELECT/* | LVS_EX_GRIDLINES*/);
//            lcList.SetImageList(&pDoc->m_smallImageList, LVSIL_SMALL);
			lcList.SetImageList(pDoc->m_smallImageList, LVSIL_SMALL);

            CFlatHeaderCtrl *pfhFlatHeader = (CFlatHeaderCtrl*)lcList.GetHeaderCtrl();

            if (pfhFlatHeader != NULL)
            {
                pfhFlatHeader->SetImageList(&pDoc->m_ilHeader);

                HDITEMEX hie;

                hie.m_iMinWidth = iMinFirstColumnWidth;
                hie.m_iMaxWidth = -1;

                pfhFlatHeader->SetItemEx(0, &hie);

                HDITEM hditem;

                hditem.mask = HDI_FORMAT | HDI_IMAGE;
                pfhFlatHeader->GetItem(0, &hditem);

                hditem.fmt      |= HDF_IMAGE;
                hditem.iImage   = IconEraser;
                pfhFlatHeader->SetItem(0, &hditem);
            }

            ModifyStyleEx(WS_EX_CLIENTEDGE, 0);
            m_odtTarget.Register(this);

            return TRUE;
        }
        catch (CException *e)
        {
            ASSERT(FALSE);
            REPORT_ERROR(e);
            e->Delete();
        }
        catch (...)
        {
            ASSERT(FALSE);
        }
    }

    return FALSE;
}

void CShellListView::OnSize(UINT nType, int cx, int cy)
{
    TRACE("CShellListView::OnSize\n");

    CFlatListView::OnSize(nType, cx, cy);
    ResizeColumns();
}

void CShellListView::OnUpdate(CView* /*pSender*/, LPARAM lHint, CObject* pHint)
{
    TRACE("CShellListView::OnUpdate\n");

    HRESULT         hr;                 //result of operation
    LPSHELLFOLDER   lpsf = NULL;       //Long pointer to ISHELLFOLDER interface
    LPTVITEMDATA    lptvid;             //Long pointer to TreeView item data
    LPLVITEMDATA    lplvid;             //Long pointer to ListView item data

    switch (lHint)
    {
    case ID_SHELLTREE_SELCHANGED:
        lptvid = (LPTVITEMDATA)pHint;
        ASSERT(lptvid);

        // release possible previous item data
        DeleteTVItemData(&m_ptvCurrent);

        // and save a copy of the new one
        CopyTVItemData(lptvid, &m_ptvCurrent);

        hr = lptvid->lpsfParent->BindToObject(lptvid->lpi,
                                              0,
                                              IID_IShellFolder,
                                              (LPVOID *)&lpsf);
        if (SUCCEEDED(hr))
        {
            int iFolders = 0;

            PopulateListView(lptvid->lpifq, lpsf, iFolders);
            lpsf->Release();

            ::PostMessage(m_hwndTree, ID_SHELLLIST_FOLDERCOUNT, (WPARAM)iFolders, 0);
        }

        break;
    case ID_SHELLTREE_SHELLEXECUTE:
        lplvid = (LPLVITEMDATA)pHint;
        ASSERT(lplvid);

        ShellOpenItem(lplvid);
        break;
    default:
        break;
    }
}

BOOL CShellListView::CopyTVItemData(LPTVITEMDATA ptv, LPTVITEMDATA *pptv)
{
    ASSERT(ptv != NULL);
    ASSERT(pptv != NULL);
    ASSERT(*pptv == NULL);

    LPMALLOC lpMalloc;

    if (SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        try
        {
            *pptv = (LPTVITEMDATA) lpMalloc->Alloc(sizeof(TVITEMDATA));

            if (*pptv != NULL)
            {
                (*pptv)->lpsfParent = ptv->lpsfParent;
                (*pptv)->lpsfParent->AddRef();

                (*pptv)->lpi = CopyITEMID(lpMalloc, ptv->lpi);
                (*pptv)->lpifq = CopyITEMID(lpMalloc, ptv->lpifq);

                lpMalloc->Release();
                return TRUE;
            }
        }
        catch (...)
        {
            ASSERT(FALSE);
        }

        lpMalloc->Release();
    }


    return FALSE;
}

void CShellListView::DeleteTVItemData(LPTVITEMDATA* pptv)
{
    ASSERT(pptv != NULL);

    LPMALLOC lpMalloc;

    if (SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        try
        {
            if (*pptv != NULL)
            {
                (*pptv)->lpsfParent->Release();

                lpMalloc->Free((*pptv)->lpi);
                lpMalloc->Free((*pptv)->lpifq);
                lpMalloc->Free(*pptv);

                *pptv = NULL;
            }
        }
        catch (...)
        {
            ASSERT(FALSE);
        }

        lpMalloc->Release();
    }
}

void CShellListView::OnRclick(NMHDR* /*pNMHDR*/, LRESULT* pResult)
{
    GetContextMenu();
    *pResult = 0;
}

void CShellListView::OnDblclk(NMHDR* /*pNMHDR*/, LRESULT* pResult)
{
    int         lvIndex = GetDoubleClickedItem();
    CListCtrl&  lc = GetListCtrl();

    if (lvIndex >= 0)
        ::PostMessage(m_hwndTree, ID_SHELLLIST_SELECTCHILD, (WPARAM)lc.GetItemData(lvIndex), 0);

    *pResult = 0;
}

void CShellListView::OnBeginDrag(NMHDR* pNMHDR, LRESULT* pResult)
{
    OnDragDrop(pNMHDR, pResult);
    *pResult = 0;
}

void CShellListView::OnBeginRDrag(NMHDR* pNMHDR, LRESULT* pResult)
{
    OnDragDrop(pNMHDR, pResult);
    *pResult = 0;
}

void CShellListView::OnEditRefresh()
{
    TRACE("CShellListView::OnEditRefresh\n");

    // refresh on demand, otherwise the refresh thread
    // takes care of the list automagically

    ::PostMessage(m_hwndTree, ID_SHELLLIST_REFRESHLIST, 0, 0);
}

void CShellListView::OnDeleteItem(NMHDR* pNMHDR, LRESULT* pResult)
{
    TRACE("CShellListView::OnDeleteItem\n");

    // free the memory of the list view item data

    LPNMLISTVIEW lpnm = (LPNMLISTVIEW) pNMHDR;

    CListCtrl&      lc = GetListCtrl();
    LPLVITEMDATA    lplvid = NULL;
    LPMALLOC        lpMalloc;

    lplvid = (LPLVITEMDATA)lc.GetItemData(lpnm->iItem);

    if (lplvid && SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        lplvid->lpsfParent->Release();

        lpMalloc->Free(lplvid->lpi);
        lpMalloc->Free(lplvid);

        lpMalloc->Release();
        *pResult = 0;
    }
}

UINT CShellListView::RefreshThread(LPVOID lpParam)
{
    TRACE("CShellListView::RefreshThread\n");

    _set_se_translator(SeTranslator);

    CShellListView *pThis = (CShellListView*)lpParam;

    // do not run if a thread is already running or we have set to
    // stop or if the change notification handle is invalid

    try
    {
        if (pThis->m_hChangeNotification != INVALID_HANDLE_VALUE &&
            WaitForSingleObject(pThis->m_evKillThread, 0) != WAIT_OBJECT_0 &&
            WaitForSingleObject(pThis->m_evNotRunning, 0) == WAIT_OBJECT_0)
        {
            // running again
            pThis->m_evNotRunning.ResetEvent();

            // handle array
            HANDLE hHandles[2] = { pThis->m_hChangeNotification,
                                   (HANDLE)pThis->m_evKillThread };

            // wait for either of the events to signal
            WaitForMultipleObjects(2, hHandles, FALSE, INFINITE);

            // if it was the change notification, refresh
            if (WaitForSingleObject(pThis->m_hChangeNotification, 0) == WAIT_OBJECT_0)
                ::PostMessage(pThis->m_hwndTree, ID_SHELLLIST_REFRESHLIST, 0, 0);

            // not running
            pThis->m_evNotRunning.SetEvent();

            return EXIT_SUCCESS;
        }
    }
    catch (...)
    {
        ASSERT(FALSE);

        try
        {
            pThis->m_evNotRunning.SetEvent();
        }
        catch (...)
        {
        }
    }

    return EXIT_FAILURE;
}

void CShellListView::OnEditSelectAll()
{
    TRACE("CShellListView::OnEditSelectAll\n");
    SelItemRange(TRUE, 0, -1);
}

BOOL CShellListView::OnDrop(COleDataObject* pDataObject, DROPEFFECT dropEffect, CPoint point)
{
    // the right button was not down

    LPLVITEMDATA    lplvid;
    LVHITTESTINFO   lvhti;
    LVITEM          lvi;
    CListCtrl&      lc = GetListCtrl();

    lvhti.pt = point;
    lc.HitTest(&lvhti);

    LPDROPTARGET    lpdt;
    HRESULT         hr;
    DWORD           dwEffect = (DWORD) dropEffect;

    ClientToScreen(&point);
    POINTL          pt = { point.x, point.y };

    if (lvhti.flags & (LVHT_ONITEMLABEL | LVHT_ONITEMICON))
    {
        lvi.mask = LVIF_PARAM;
        lvi.iItem = lvhti.iItem;

        if (lc.GetItem(&lvi))
        {
            lplvid = (LPLVITEMDATA)lvi.lParam;

            hr = lplvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                                   1,
                                                   (LPCITEMIDLIST*)&lplvid->lpi,
                                                   IID_IDropTarget,
                                                   0,
                                                   (LPVOID*)&lpdt);

            if (SUCCEEDED(hr))
            {
                // pass the drop handling to the file or folder
                // beneath the cursor

                lpdt->DragEnter(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
                lpdt->Drop(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
                lpdt->DragLeave();

                lpdt->Release();

                return TRUE;
            }
        }
    }
    else if (m_ptvCurrent != NULL)
    {
        hr = m_ptvCurrent->lpsfParent->GetUIObjectOf(m_hWnd,
                                                     1,
                                                     (LPCITEMIDLIST*)&m_ptvCurrent->lpi,
                                                     IID_IDropTarget,
                                                     0,
                                                     (LPVOID*)&lpdt);

        if (SUCCEEDED(hr))
        {
            // pass the drop handling to the current folder

            lpdt->DragEnter(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
            lpdt->Drop(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
            lpdt->DragLeave();

            lpdt->Release();

            return TRUE;
        }
    }

    // cannot drop
    return CFlatListView::OnDrop(pDataObject, dropEffect, point);
}

DROPEFFECT CShellListView::OnDropEx(COleDataObject* pDataObject, DROPEFFECT dropDefault, DROPEFFECT dropList, CPoint point)
{
    // if the right mouse button is down, show a popup menu for
    // drop options

    int             iItem;
    CListCtrl&      lc = GetListCtrl();

    // remove hiliting from the possible previous item
    iItem = lc.GetNextItem(-1, LVNI_DROPHILITED);

    if (iItem != -1)
        lc.SetItemState(iItem, 0, LVIS_DROPHILITED);

    if (m_bDragRight)
    {
        // the right mouse button is down

        LPLVITEMDATA    lplvid;
        LVHITTESTINFO   lvhti;
        LVITEM          lvi;

        lvhti.pt = point;
        lc.HitTest(&lvhti);

        LPDROPTARGET    lpdt;
        HRESULT         hr;
        DWORD           dwEffect = (DWORD) dropDefault;

        ClientToScreen(&point);
        POINTL          pt = { point.x, point.y };

        // load the menu

        CMenu cMenu, *pPop = 0;
        cMenu.LoadMenu(IDR_MENU_RDROP);

        pPop = cMenu.GetSubMenu(0);
        ASSERT(pPop);

        // set "Move Here" as the default option
        SetMenuDefaultItem(pPop->m_hMenu, ID_DRAG_MOVE, FALSE);

        int iCmd = pPop->TrackPopupMenu(TPM_LEFTALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON,
                                        point.x, point.y, this);

        // determine desired action
        switch (iCmd)
        {
        case ID_DRAG_MOVE:
            dwEffect = (DWORD) DROPEFFECT_MOVE;
            break;
        case ID_DRAG_COPY:
            dwEffect = (DWORD) DROPEFFECT_COPY;
            break;
        default:
            dwEffect = (DWORD) DROPEFFECT_NONE;
            break;
        }

        // done with the menu
        cMenu.DestroyMenu();

        // do what the user asked to
        if (dwEffect != (DWORD) DROPEFFECT_NONE)
        {
            if (lvhti.flags & (LVHT_ONITEMLABEL | LVHT_ONITEMICON))
            {
                // get IDropTarget that handles the drop

                lvi.mask = LVIF_PARAM;
                lvi.iItem = lvhti.iItem;

                if (lc.GetItem(&lvi))
                {
                    lplvid = (LPLVITEMDATA)lvi.lParam;

                    hr = lplvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                                           1,
                                                           (LPCITEMIDLIST*)&lplvid->lpi,
                                                           IID_IDropTarget,
                                                           0,
                                                           (LPVOID*)&lpdt);

                    if (SUCCEEDED(hr))
                    {
                        // pass the drop handling to the file or folder
                        // beneath the cursor

                        lpdt->DragEnter(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
                        lpdt->Drop(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
                        lpdt->DragLeave();

                        lpdt->Release();
                    }
                }
            }
            else if (m_ptvCurrent != NULL)
            {
                hr = m_ptvCurrent->lpsfParent->GetUIObjectOf(m_hWnd,
                                                             1,
                                                             (LPCITEMIDLIST*)&m_ptvCurrent->lpi,
                                                             IID_IDropTarget,
                                                             0,
                                                             (LPVOID*)&lpdt);

                if (SUCCEEDED(hr))
                {
                    // pass the drop handling to the current folder

                    lpdt->DragEnter(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
                    lpdt->Drop(pDataObject->m_lpDataObject, 0, pt, &dwEffect);
                    lpdt->DragLeave();

                    lpdt->Release();
                }
            }
        }

        // return what happened
        return (DROPEFFECT)dwEffect;
    }

    // the right button was not down, continue to OnDrop
    return CFlatListView::OnDropEx(pDataObject, dropDefault, dropList, point);
}


DROPEFFECT CShellListView::OnDragOver(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    if (pDataObject->IsDataAvailable(CF_HDROP))
    {
        LVHITTESTINFO   lvhti;
        LVITEM          lvi;
        int             iItem;
        CListCtrl&      lc = GetListCtrl();

        DROPEFFECT      dropEffect;
        LPLVITEMDATA    lplvid;
        LPDROPTARGET    lpdt;
        HRESULT         hr;

        // determine the default operation

        if (dwKeyState & MK_RBUTTON || dwKeyState & MK_ALT)
            dropEffect = DROPEFFECT_MOVE;
        else
            dropEffect = DROPEFFECT_COPY;

        // halt window updating
        SetRedraw(FALSE);

        // determine whether the cursor is over an item
        lvhti.pt = point;
        lc.HitTest(&lvhti);

        // remove hiliting from the possible previous item
        iItem = lc.GetNextItem(-1, LVNI_DROPHILITED);

        if (iItem != -1 && iItem != lvhti.iItem)
            lc.SetItemState(iItem, 0, LVIS_DROPHILITED);


        if (lvhti.flags & (LVHT_ONITEMLABEL | LVHT_ONITEMICON))
        {
            // the cursor IS over an item; determine whether the
            // target supports dropping

            lvi.mask = LVIF_PARAM;
            lvi.iItem = lvhti.iItem;

            if (lc.GetItem(&lvi))
            {
                lplvid = (LPLVITEMDATA)lvi.lParam;

                hr = lplvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                                       1,
                                                       (LPCITEMIDLIST*)&lplvid->lpi,
                                                       IID_IDropTarget,
                                                       0,
                                                       (LPVOID*)&lpdt);

                if (SUCCEEDED(hr))
                {
                    // supports dropping; return the default effect
                    lpdt->Release();

                    // select the item
                    lc.SetItemState(lvi.iItem, LVIS_DROPHILITED, LVIS_DROPHILITED);
                }
                else
                {
                    // does not support dropping; return no effect
                    dropEffect = DROPEFFECT_NONE;
                }
            }
        }

        // if the cursor is not over any item, the drop target is the current folder
        // and we'll support dropping into it

        // update the window
        SetRedraw(TRUE);

        // done
        return dropEffect;
    }

    return CFlatListView::OnDragOver(pDataObject, dwKeyState, point);
}

DROPEFFECT CShellListView::OnDragEnter(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    // whether the right mouse button was pressed when the dragging started
    m_bDragRight = (dwKeyState & MK_RBUTTON);

    return CFlatListView::OnDragEnter(pDataObject, dwKeyState, point);
}

void CShellListView::OnDragLeave()
{
    // reset the right mouse button state
    m_bDragRight = FALSE;

    int             iItem;
    CListCtrl&      lc = GetListCtrl();

    // remove hiliting from the possible previous item
    iItem = lc.GetNextItem(-1, LVNI_DROPHILITED);

    if (iItem != -1)
        lc.SetItemState(iItem, 0, LVIS_DROPHILITED);

    CFlatListView::OnDragLeave();
}

void CShellListView::OnUpdateItems(CCmdUI* pCmdUI)
{
    CListCtrl& lc = GetListCtrl();

    CString str;
    int iCount = lc.GetItemCount();

    str.Format("%u Item", iCount);

    if (iCount != 1)
        str += "s";

    pCmdUI->SetText((LPCTSTR)str);
    pCmdUI->Enable();
}
