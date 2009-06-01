// ShellTree.cpp
//
// Originally based on CShellTree version 1.02 by Selom Ofori.
// Heavily altered since then...
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
#include "ShellTree.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif


CShellTreeCtrl::CShellTreeCtrl() :
m_bDragRight(FALSE)
{
    TRACE("CShellTreeCtrl::CShellTreeCtrl\n");
}

CShellTreeCtrl::~CShellTreeCtrl()
{
    TRACE("CShellTreeCtrl::~CShellTreeCtrl\n");
}


BEGIN_MESSAGE_MAP(CShellTreeCtrl, CDropTargetTreeCtrl)
    //{{AFX_MSG_MAP(CShellTreeCtrl)
    ON_NOTIFY_REFLECT(TVN_ITEMEXPANDING, OnItemexpanding)
    ON_NOTIFY_REFLECT(NM_RCLICK, OnRclick)
    ON_NOTIFY_REFLECT(TVN_DELETEITEM, OnDeleteitem)
    ON_NOTIFY_REFLECT(TVN_SELCHANGING, OnSelchanging)
    ON_NOTIFY_REFLECT(TVN_BEGINDRAG, OnBeginDrag)
    ON_NOTIFY_REFLECT(TVN_BEGINRDRAG, OnBeginRDrag)
    ON_WM_CREATE()
    //}}AFX_MSG_MAP
    ON_MESSAGE(ID_SHELLLIST_FOLDERCOUNT, OnFolderCount)
    ON_MESSAGE(ID_SHELLLIST_SELECTCHILD, OnSelectChild)
    ON_MESSAGE(ID_SHELLLIST_REFRESHLIST, OnRefreshList)
END_MESSAGE_MAP()

void CShellTreeCtrl::PopulateTree()
{
    TRACE("CShellTreeCtrl::PopulateTree\n");

    LPSHELLFOLDER   lpsf = NULL;
    TVSORTCB       tvscb;

    // Get a pointer to the desktop folder.
    if (SUCCEEDED(SHGetDesktopFolder(&lpsf)))
    {
        // Initialize the tree view to be empty.
        DeleteAllItems();

        // Fill in the tree view from the root.
        FillTreeView(lpsf, NULL, TVI_ROOT);

        // Release the folder pointer.
        lpsf->Release();
    }

    tvscb.hParent       = TVI_ROOT;
    tvscb.lParam        = 0;
    tvscb.lpfnCompare   = TreeViewCompareProc;

    // Sort the items in the tree view
    SortChildrenCB(&tvscb);

    HTREEITEM hItem = GetRootItem();

    Expand(hItem, TVE_EXPAND);
    Select(hItem, TVGN_CARET);
}

void CShellTreeCtrl::PopulateTree(int nFolder)
{
    LPSHELLFOLDER   lpsf = NULL, lpsf2 = NULL;
    LPITEMIDLIST    lpi = NULL;
    HRESULT         hr;
    TVSORTCB       tvscb;

    // Get a pointer to the desktop folder.

    if (SUCCEEDED(SHGetDesktopFolder(&lpsf)))
    {
        // Initialize the tree view to be empty.
        DeleteAllItems();

        if (FAILED(SHGetSpecialFolderLocation(m_hWnd, nFolder, &lpi)))
        {
            lpi = NULL;
            FillTreeView(lpsf, NULL, TVI_ROOT);
        }
        else
        {
            hr = lpsf->BindToObject(lpi, 0, IID_IShellFolder,(LPVOID *)&lpsf2);

            if (SUCCEEDED(hr))
            {
                // Fill in the tree view from the root.
                FillTreeView(lpsf2, lpi, TVI_ROOT);
                lpsf2->Release();
            }
            else
                FillTreeView(lpsf, NULL, TVI_ROOT);
        }

        // Release the folder pointer.
        lpsf->Release();
    }

    tvscb.hParent       = TVI_ROOT;
    tvscb.lParam        = 0;
    tvscb.lpfnCompare   = TreeViewCompareProc;

    // Sort the items in the tree view
    SortChildrenCB(&tvscb);

    HTREEITEM hItem = GetRootItem();

    Expand(hItem, TVE_EXPAND);
    Select(hItem, TVGN_CARET);
}

int CShellTreeCtrl::FillTreeView(LPSHELLFOLDER lpsf, LPITEMIDLIST lpifq, HTREEITEM hParent)
{
    TRACE("CShellTreeCtrl::FillTreeView\n");

    ASSERT(lpsf != NULL);
    ASSERT(hParent != NULL);

    TVITEM          tvi;                          // TreeView Item.
    TVINSERTSTRUCT  tvins;                        // TreeView Insert Struct.
    HTREEITEM       hPrev           = NULL;       // Previous Item Added.

    LPITEMIDLIST    lpi             = NULL;
    LPITEMIDLIST    lpifqThisItem   = NULL;
    LPTVITEMDATA    lptvid          = NULL;
    LPMALLOC        lpMalloc        = NULL;
    HRESULT         hr;

    LPENUMIDLIST    lpe             = NULL;

    ULONG           ulFetched;
    TCHAR           szBuffer[MAX_PATH];
    HWND            hwnd            = ::GetParent(m_hWnd);
    int             iChildren       = 0;

    SetRedraw(FALSE);

    // Allocate a shell memory object.

    if (SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        // Get the IEnumIDList object for the given folder.
        hr = lpsf->EnumObjects(hwnd, SHCONTF_FOLDERS | SHCONTF_NONFOLDERS | SHCONTF_INCLUDEHIDDEN, &lpe);

        if (SUCCEEDED(hr))
        {
            // Enumerate throught the list of folder and non-folder objects.
            while (S_OK == lpe->Next(1, &lpi, &ulFetched))
            {
                //Create a fully qualified path to the current item
                //The SH* shell api's take a fully qualified path pidl,
                //(see GetIcon above where I call SHGetFileInfo) whereas the
                //interface methods take a relative path pidl.
                ULONG ulAttrs = SFGAO_HASSUBFOLDER | SFGAO_FOLDER;

                // Determine what type of object we have.
                lpsf->GetAttributesOf(1, (LPCITEMIDLIST*)&lpi, &ulAttrs);

                if (ulAttrs & (SFGAO_HASSUBFOLDER | SFGAO_FOLDER))
                {
                    //We need this next if statement so that we don't add things like
                    //the MSN to our tree.  MSN is not a folder, but according to the
                    //shell it has subfolders.
                    if (ulAttrs & SFGAO_FOLDER)
                    {
                        tvi.mask = TVIF_TEXT | TVIF_IMAGE | TVIF_SELECTEDIMAGE | TVIF_PARAM;

                        if (ulAttrs & SFGAO_HASSUBFOLDER)
                        {
                            //This item has sub-folders, so let's put the + in the TreeView.
                            //The first time the user clicks on the item, we'll populate the
                            //sub-folders.
                            tvi.cChildren = 1;
                            tvi.mask |= TVIF_CHILDREN;
                        }

                        //OK, let's get some memory for our ITEMDATA struct
                        lptvid = (LPTVITEMDATA)lpMalloc->Alloc(sizeof(TVITEMDATA));

                        if (!lptvid)
                            break;

                        //Now get the friendly name that we'll put in the treeview.
                        if (!GetName(lpsf, lpi, SHGDN_NORMAL, szBuffer))
                        {
                            lpMalloc->Free(lptvid);
                            break;
                        }

                        tvi.pszText    = szBuffer;
                        tvi.cchTextMax = MAX_PATH;

                        lpifqThisItem = ConcatPidls(lpifq, lpi);

                        //Now, make a copy of the ITEMIDLIST
                        lptvid->lpi = CopyITEMID(lpMalloc, lpi);

                        GetNormalAndSelectedIcons(lpifqThisItem, &tvi);

                        lptvid->lpsfParent = lpsf;    //Store the parent folders SF
                        lpsf->AddRef();

                        lptvid->lpifq = ConcatPidls(lpifq, lpi);

                        tvi.lParam = (LPARAM)lptvid;

                        // Populate the TreeVeiw Insert Struct
                        // The item is the one filled above.
                        // Insert it after the last item inserted at this level.
                        // And indicate this is a root entry.
                        tvins.item         = tvi;
                        tvins.hInsertAfter = hPrev;
                        tvins.hParent      = hParent;

                        // Add the item to the tree
                        hPrev = InsertItem(&tvins);
                        iChildren++;
                    }

                    // Free this items task allocator.
                    lpMalloc->Free(lpifqThisItem);
                    lpifqThisItem = 0;
                }

                lpMalloc->Free(lpi);  //Free the pidl that the shell gave us.
                lpi = 0;
            }

            lpe->Release();
        }
        else
        {
            // folder not accessible
            iChildren = -1;
        }

        lpMalloc->Release();
    }

    SetRedraw(TRUE);

    return iChildren;
}

void CShellTreeCtrl::GetNormalAndSelectedIcons(LPITEMIDLIST lpifq,
                                           LPTVITEM lptvitem)
{
    ASSERT(lpifq != NULL);
    ASSERT(lptvitem != NULL);

    //Note that we don't check the return value here because if GetIcon()
    //fails, then we're in big trouble...

    lptvitem->iImage = GetItemIcon(lpifq,
                                   SHGFI_PIDL | SHGFI_SYSICONINDEX |
                                   SHGFI_SMALLICON);

    lptvitem->iSelectedImage = GetItemIcon(lpifq,
                                           SHGFI_PIDL | SHGFI_SYSICONINDEX |
                                           SHGFI_SMALLICON | SHGFI_OPENICON);

    return;
}



int CALLBACK CShellTreeCtrl::TreeViewCompareProc(LPARAM lparam1,
                                             LPARAM lparam2, LPARAM /*lparamSort*/)
{
    ASSERT(lparam1 != NULL);
    ASSERT(lparam2 != NULL);

    try
    {
        LPTVITEMDATA    lptvid1 = (LPTVITEMDATA)lparam1;
        LPTVITEMDATA    lptvid2 = (LPTVITEMDATA)lparam2;
        HRESULT         hr;

        hr = lptvid1->lpsfParent->CompareIDs(0, lptvid1->lpi, lptvid2->lpi);

        if (FAILED(hr)) return 0;

        return (short)SCODE_CODE(GetScode(hr));
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return 0;
}

void CShellTreeCtrl::OnFolderExpanding(NMHDR* pNMHDR, LRESULT* pResult)
{
    TRACE("CShellTreeCtrl::OnFolderExpanding\n");

    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    HRESULT         hr;
    LPSHELLFOLDER   lpsf = NULL;
    TVSORTCB       tvscb;

    NMTREEVIEW* pnmtv = (NMTREEVIEW*)pNMHDR;

    if ((pnmtv->itemNew.state & TVIS_EXPANDEDONCE))
        return;

    lptvid = (LPTVITEMDATA)pnmtv->itemNew.lParam;

    if (lptvid)
    {
        hr = lptvid->lpsfParent->BindToObject(lptvid->lpi,
                                              0,
                                              IID_IShellFolder,
                                              (LPVOID *)&lpsf);

        if (SUCCEEDED(hr))
        {
            int iFolders = FillTreeView(lpsf,
                                        lptvid->lpifq,
                                        pnmtv->itemNew.hItem);

            pnmtv->itemNew.mask         |= TVIF_CHILDREN;
            pnmtv->itemNew.cChildren    = iFolders;
            SetItem(&pnmtv->itemNew);

            lpsf->Release();
        }

        tvscb.hParent     = pnmtv->itemNew.hItem;
        tvscb.lParam      = 0;
        tvscb.lpfnCompare = TreeViewCompareProc;

        SortChildrenCB(&tvscb);
    }

    *pResult = 0;
}

void CShellTreeCtrl::GetContextMenu(NMHDR* /*pNMHDR*/, LRESULT* pResult)
{
    TRACE("CShellTreeCtrl::GetContextMenu\n");

    // since we can select only one item on the tree,
    // this code is sufficient; otherwise (like in the list),
    // we would need to get all selected items and allocate
    // a list for the pointers to LPITEMIDLIST

    POINT           pt;
    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    TVHITTESTINFO   tvhti;
    TVITEM          tvi;

    ::GetCursorPos((LPPOINT)&pt);
    ScreenToClient(&pt);

    tvhti.pt = pt;
    HitTest(&tvhti);

    if (tvhti.flags & (TVHT_ONITEMLABEL | TVHT_ONITEMICON))
    {
        ClientToScreen(&pt);

        tvi.mask = TVIF_PARAM;
        tvi.hItem = tvhti.hItem;

        if (!GetItem(&tvi)) return;


        lptvid = (LPTVITEMDATA)tvi.lParam;

        DoTheMenuThing(::GetParent(m_hWnd),
                       lptvid->lpsfParent,
                       &lptvid->lpi,
                       &pt);
    }

    *pResult = 0;
}

BOOL CShellTreeCtrl::OnFolderSelect(NMHDR* pNMHDR, LRESULT* pResult, CString &szFolderPath)
{
    TRACE("CShellTreeCtrl::OnFolderSelect\n");

    NMTREEVIEW      *pnmtv = (NMTREEVIEW*)pNMHDR;

    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    LPSHELLFOLDER   lpsf = NULL;
    HRESULT         hr;

    TVSORTCB        tvscb;
    HTREEITEM       hItem = NULL;

    TCHAR           szBuffer[MAX_PATH];
    BOOL            bResult = FALSE;

    *pResult = FALSE;
    hItem = pnmtv->itemNew.hItem;//GetSelectedItem();

    if (hItem != NULL)
    {
        lptvid = (LPTVITEMDATA)GetItemData(hItem);

        if (lptvid && lptvid->lpsfParent && lptvid->lpi)
        {
            hr = lptvid->lpsfParent->BindToObject(lptvid->lpi,
                                                  0,
                                                  IID_IShellFolder,
                                                  (LPVOID *)&lpsf);

            if (SUCCEEDED(hr))
            {
                ULONG ulAttrs = SFGAO_FILESYSTEM;

                // Determine what type of object we have.
                lptvid->lpsfParent->GetAttributesOf(1,
                                                    (LPCITEMIDLIST*)&lptvid->lpi,
                                                    &ulAttrs);

                if (ulAttrs & SFGAO_FILESYSTEM)
                {
                    if (SHGetPathFromIDList(lptvid->lpifq, szBuffer))
                    {
                        szFolderPath = szBuffer;
                        bResult = TRUE;
                    }
                }

                if (ItemHasChildren(pnmtv->itemNew.hItem) &&
                    !(pnmtv->itemNew.state & TVIS_EXPANDEDONCE))
                {
                    int iFolders = FillTreeView(lpsf,
                                                lptvid->lpifq,
                                                pnmtv->itemNew.hItem);

                    if (iFolders >= 0)
                    {
                        pnmtv->itemNew.cChildren    = iFolders;
                        pnmtv->itemNew.state        |= TVIS_EXPANDEDONCE;
                        pnmtv->itemNew.stateMask    |= TVIS_EXPANDEDONCE;
                        pnmtv->itemNew.mask         |= (TVIF_STATE | TVIF_CHILDREN);
                        SetItem(&pnmtv->itemNew);

                        tvscb.hParent               = pnmtv->itemNew.hItem;
                        tvscb.lParam                = 0;
                        tvscb.lpfnCompare           = TreeViewCompareProc;
                        SortChildrenCB(&tvscb);
                    }
                    else
                    {
                        // folder not accessible; prevent the selection from changing

                        bResult = FALSE;
                        *pResult = TRUE;
                    }
                }

                lpsf->Release();
            }
        }
    }

    return bResult;
}

void CShellTreeCtrl::OnDeleteShellItem(NMHDR* pNMHDR, LRESULT* pResult)
{
    TRACE("CShellTreeCtrl::OnDeleteShellItem\n");

    LPTVITEMDATA    lptvid = NULL;
    LPMALLOC        lpMalloc;

    NMTREEVIEW* pNMTreeView = (NMTREEVIEW*)pNMHDR;
    lptvid = (LPTVITEMDATA)pNMTreeView->itemOld.lParam;

    if (lptvid && SUCCEEDED(SHGetMalloc(&lpMalloc)))
    {
        lptvid->lpsfParent->Release();

        lpMalloc->Free(lptvid->lpi);
        lpMalloc->Free(lptvid->lpifq);
        lpMalloc->Free(lptvid);

        lpMalloc->Release();
        *pResult = 0;
    }
}

BOOL CShellTreeCtrl::GetSelectedFolderPath(CString& strFolderPath)
{
    TRACE("CShellTreeCtrl::GetSelectedFolderPath\n");

    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    LPSHELLFOLDER   lpsf = NULL;
    HRESULT         hr;

    HTREEITEM       hItem = NULL;
    BOOL            bResult = FALSE;

    hItem = GetSelectedItem();

    if (hItem != NULL)
    {
        lptvid = (LPTVITEMDATA)GetItemData(hItem);

        if (lptvid && lptvid->lpsfParent && lptvid->lpi)
        {
            hr = lptvid->lpsfParent->BindToObject(lptvid->lpi,
                                                  0,
                                                  IID_IShellFolder,
                                                  (LPVOID *)&lpsf);

            if (SUCCEEDED(hr))
            {
                ULONG ulAttrs = SFGAO_FILESYSTEM;

                // Determine what type of object we have.
                lptvid->lpsfParent->GetAttributesOf(1,
                                                    (LPCITEMIDLIST*)&lptvid->lpi,
                                                    &ulAttrs);

                if (ulAttrs & SFGAO_FILESYSTEM)
                {
                    try
                    {
                        if (SHGetPathFromIDList(lptvid->lpifq,
                                                strFolderPath.GetBuffer(MAX_PATH)))
                        {
                            bResult = TRUE;
                        }

                        strFolderPath.ReleaseBuffer();
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

                lpsf->Release();
            }
        }
    }

    return bResult;
}

LPSHELLFOLDER CShellTreeCtrl::GetParentShellFolder(HTREEITEM folderNode)
{
    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    lptvid = (LPTVITEMDATA)GetItemData(folderNode);

    return ((lptvid) ? lptvid->lpsfParent : NULL);
}

LPITEMIDLIST CShellTreeCtrl::GetRelativeIDLIST(HTREEITEM folderNode)
{
    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    lptvid = (LPTVITEMDATA)GetItemData(folderNode);

    return ((lptvid) ? lptvid->lpifq : NULL);
}

LPITEMIDLIST CShellTreeCtrl::GetFullyQualifiedID(HTREEITEM folderNode)
{
    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    lptvid = (LPTVITEMDATA)GetItemData(folderNode);

    return ((lptvid) ? lptvid->lpifq : NULL);
}

void CShellTreeCtrl::FindTreePidl(HTREEITEM nextNode, LPLVITEMDATA lplvid, BOOL& valid)
{
    LPTVITEMDATA    lptvid;  //Long pointer to TreeView item data
    HTREEITEM       subNode;
    HRESULT         hr;

    valid = FALSE;

    while (nextNode && !valid)
    {
        lptvid = (LPTVITEMDATA)GetItemData(nextNode);

        if (lptvid && lplvid)
        {
            hr = lplvid->lpsfParent->CompareIDs(0, lplvid->lpi, lptvid->lpi);

            if ((short)SCODE_CODE(GetScode(hr)) == 0)
            {
                EnsureVisible(nextNode);
                SelectItem(nextNode);
                valid = TRUE;

                return;
            }
        }

        subNode = GetChildItem(nextNode);

        if (subNode)
            FindTreePidl(subNode, lplvid, valid);

        nextNode = GetNextSiblingItem(nextNode);
    }
}


void CShellTreeCtrl::OnItemexpanding(NMHDR* pNMHDR, LRESULT* pResult)
{
    OnFolderExpanding(pNMHDR,pResult);
}

void CShellTreeCtrl::OnRclick(NMHDR* pNMHDR, LRESULT* pResult)
{
    GetContextMenu(pNMHDR, pResult);
}

void CShellTreeCtrl::OnDeleteitem(NMHDR* pNMHDR, LRESULT* pResult)
{
    OnDeleteShellItem(pNMHDR, pResult);
}

LRESULT CShellTreeCtrl::OnRefreshList(WPARAM /*wParam*/, LPARAM /*lParam*/)
{
    TRACE("CShellTreeCtrl::OnRefreshList\n");

    HTREEITEM htSelected = GetSelectedItem();

    if (htSelected != NULL)
    {
        // if the selected item is a directory, make sure it still exists
        if (m_strCurrentPath.GetLength() > _MAX_DRIVE)
        {
            if (GetFileAttributes((LPCTSTR)m_strCurrentPath) == (DWORD) -1)
            {
                // selects the parent item which causes update request
                // to be sent to the list; so we are not going to send it
                // twice

                HTREEITEM htParent = GetParentItem(htSelected);

                if (htParent != NULL)
                    RefreshChildren(htParent);

                return TRUE;
            }
        }

        // send update for the list

        CEraserApp *pApp = static_cast<CEraserApp*>(AfxGetApp());
        pApp->m_pDoc->UpdateAllViews(NULL, ID_SHELLTREE_SELCHANGED, (CObject*)GetItemData(htSelected));
    }

    return TRUE;
}


LRESULT CShellTreeCtrl::OnSelectChild(WPARAM wParam, LPARAM /*lParam*/)
{
    TRACE("CShellTreeCtrl::OnSelectChild\n");

    LPLVITEMDATA    lplvid = NULL;
    BOOL            bFound = FALSE;

    lplvid = (LPLVITEMDATA)wParam;
    ASSERT(lplvid);

    FindTreePidl(GetSelectedItem(), lplvid, bFound);

    if (!bFound)
    {
        //the folder was not found so we send back a message
        //to the listview to execute the itemid
        CEraserApp *pApp = static_cast<CEraserApp*>(AfxGetApp());
        pApp->m_pDoc->UpdateAllViews(NULL, ID_SHELLTREE_SHELLEXECUTE, (CObject*)lplvid);
    }

    return (LRESULT)bFound;
}

LRESULT CShellTreeCtrl::OnFolderCount(WPARAM wParam, LPARAM /*lParam*/)
{
    TRACE("CShellTreeCtrl::OnFolderCount\n");

    HTREEITEM   htItem = GetSelectedItem();
    int         iFolderCount = (int) wParam;
    int         iCurrentCount = 0;

    // count child items

    HTREEITEM htChild = GetChildItem(htItem);
    HTREEITEM htNext = htChild;

    while (htNext != NULL)
    {
        htNext = GetNextSiblingItem(htNext);
        iCurrentCount++;
    }

    // if values do not match, will need to refresh

    if (iCurrentCount != iFolderCount)
        RefreshChildren(htItem);

    return 0;
}

// refreshes child items for the given tree item
void CShellTreeCtrl::RefreshChildren(HTREEITEM htItem)
{
    TRACE("CShellTreeCtrl::RefreshChildren\n");

    ASSERT(htItem != NULL);

    // select the item to prevent selection changes
    // while removing possible children

    SelectItem(htItem);

    HTREEITEM htChild = GetChildItem(htItem);
    HTREEITEM htNext;

    // first clear possible children

    while (htChild != NULL)
    {
        htNext = GetNextSiblingItem(htChild);
        DeleteItem(htChild);
        htChild = htNext;
    }

    // refill

    HRESULT         hr;
    LPTVITEMDATA    lptvid = (LPTVITEMDATA)GetItemData(htItem);
    LPSHELLFOLDER   lpsf = NULL;

    if (lptvid)
    {
        hr = lptvid->lpsfParent->BindToObject(lptvid->lpi,
                                              0,
                                              IID_IShellFolder,
                                              (LPVOID *)&lpsf);

        if (SUCCEEDED(hr))
        {
            int iFolders = FillTreeView(lpsf,
                                        lptvid->lpifq,
                                        htItem);

            TVITEM tvItem;
            tvItem.mask = TVIF_HANDLE;
            tvItem.hItem = htItem;
            GetItem(&tvItem);

            tvItem.mask |= TVIF_CHILDREN;
            tvItem.cChildren = iFolders;
            SetItem(&tvItem);

            lpsf->Release();
        }

        TVSORTCB    tvscb;

        tvscb.hParent     = htItem;
        tvscb.lParam      = 0;
        tvscb.lpfnCompare = TreeViewCompareProc;

        SortChildrenCB(&tvscb);
    }
}

void CShellTreeCtrl::OnSelchanging(NMHDR* pNMHDR, LRESULT* pResult)
{
    NMTREEVIEW* pNMTreeView = (NMTREEVIEW*)pNMHDR;

    // sets *pResult to non-zero if the selection change
    // should be prevented

    if (!OnFolderSelect(pNMHDR, pResult, m_strCurrentPath))
        m_strCurrentPath.Empty();

    if (*pResult == 0)
    {
        CEraserApp *pApp = static_cast<CEraserApp*>(AfxGetApp());
        pApp->m_pDoc->UpdateAllViews(NULL, ID_SHELLTREE_SELCHANGED,
                                     (CObject*)GetItemData(pNMTreeView->itemNew.hItem));
    }
}

/////////////////////////////////////////////////////////////////////////////
// CDropTargetTreeCtrl drop/ drop query handling

DROPEFFECT CShellTreeCtrl::OnDragEnter(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    // whether the right mouse button was pressed when the dragging started
    m_bDragRight = (dwKeyState & MK_RBUTTON);

    return CDropTargetTreeCtrl::OnDragEnter(pDataObject, dwKeyState, point);
}

DROPEFFECT CShellTreeCtrl::OnDragOver(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    if (pDataObject->IsDataAvailable(CF_HDROP))
    {
        TVHITTESTINFO   tvhti;
        TVITEM          tvi;
        HTREEITEM       htItem = GetDropHilightItem();

        DROPEFFECT      dropEffect;
        LPTVITEMDATA    lptvid;
        LPDROPTARGET    lpdt;
        HRESULT         hr;

        // determine the default operation

        if (dwKeyState & MK_RBUTTON || dwKeyState & MK_ALT)
            dropEffect = DROPEFFECT_MOVE;
        else
            dropEffect = DROPEFFECT_COPY;

        // determine whether the cursor is over an item
        tvhti.pt = point;
        HitTest(&tvhti);

        if (tvhti.flags & (TVHT_ONITEMLABEL | TVHT_ONITEMICON))
        {
            // the cursor IS over an item; determine whether the
            // target supports dropping

            tvi.mask = TVIF_PARAM;
            tvi.hItem = tvhti.hItem;

            if (GetItem(&tvi))
            {
                lptvid = (LPTVITEMDATA)tvi.lParam;

                hr = lptvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                                       1,
                                                       (LPCITEMIDLIST*)&lptvid->lpi,
                                                       IID_IDropTarget,
                                                       0,
                                                       (LPVOID*)&lpdt);

                if (SUCCEEDED(hr))
                {
                    // supports dropping; return the default effect
                    lpdt->Release();

                    // select the item
                    if (htItem != tvhti.hItem)
                        SelectDropTarget(tvi.hItem);
                }
                else
                {
                    if (htItem != NULL)
                        SelectDropTarget(NULL);

                    // does not support dropping; return no effect
                    dropEffect = DROPEFFECT_NONE;
                }
            }
        }
        else
        {
            // if the cursor is not over any item
            if (htItem != NULL)
                SelectDropTarget(NULL);

            // does not support dropping; return no effect
            dropEffect = DROPEFFECT_NONE;
        }

        // done
        return dropEffect;
    }

    return CDropTargetTreeCtrl::OnDragOver(pDataObject, dwKeyState, point);
}

BOOL CShellTreeCtrl::OnDrop(COleDataObject* pDataObject, DROPEFFECT dropEffect, CPoint point)
{
    // the right button was not down

    LPTVITEMDATA    lptvid;
    TVHITTESTINFO   tvhti;
    TVITEM          tvi;

    tvhti.pt = point;
    HitTest(&tvhti);

    LPDROPTARGET    lpdt;
    HRESULT         hr;
    DWORD           dwEffect = (DWORD) dropEffect;

    ClientToScreen(&point);
    POINTL          pt = { point.x, point.y };

    if (tvhti.flags & (TVHT_ONITEMLABEL | TVHT_ONITEMICON))
    {
        tvi.mask = TVIF_PARAM;
        tvi.hItem = tvhti.hItem;

        if (GetItem(&tvi))
        {
            lptvid = (LPTVITEMDATA)tvi.lParam;

            hr = lptvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                                   1,
                                                   (LPCITEMIDLIST*)&lptvid->lpi,
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

    return CDropTargetTreeCtrl::OnDrop(pDataObject, dropEffect, point);
}

DROPEFFECT CShellTreeCtrl::OnDropEx(COleDataObject* pDataObject, DROPEFFECT dropEffect, DROPEFFECT dropEffectList, CPoint point)
{
    SelectDropTarget(NULL);

    // if the right mouse button is down, show a popup menu for
    // drop options

    if (m_bDragRight)
    {
        // the right mouse button is down

        LPTVITEMDATA    lptvid;
        TVHITTESTINFO   tvhti;
        TVITEM          tvi;

        tvhti.pt = point;
        HitTest(&tvhti);

        if (tvhti.flags & (TVHT_ONITEMLABEL | TVHT_ONITEMICON))
        {
            LPDROPTARGET    lpdt;
            HRESULT         hr;
            DWORD           dwEffect = (DWORD) dropEffect;

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
                // get IDropTarget that handles the drop

                tvi.mask = TVIF_PARAM;
                tvi.hItem = tvhti.hItem;

                if (GetItem(&tvi))
                {
                    lptvid = (LPTVITEMDATA)tvi.lParam;

                    hr = lptvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                                           1,
                                                           (LPCITEMIDLIST*)&lptvid->lpi,
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

            // return what happened
            return (DROPEFFECT)dwEffect;
        }
    }

    return CDropTargetTreeCtrl::OnDropEx(pDataObject, dropEffect, dropEffectList, point);
}

void CShellTreeCtrl::OnDragLeave()
{
    SelectDropTarget(NULL);

    // reset the right mouse button state
    m_bDragRight = FALSE;

    CDropTargetTreeCtrl::OnDragLeave();
}

int CShellTreeCtrl::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
    if (CDropTargetTreeCtrl::OnCreate(lpCreateStruct) == -1)
        return -1;

    Register();
    return 0;
}

void CShellTreeCtrl::OnBeginDrag(NMHDR* pNMHDR, LRESULT* pResult)
{
    OnDragDrop(pNMHDR, pResult);
    *pResult = 0;
}

void CShellTreeCtrl::OnBeginRDrag(NMHDR* pNMHDR, LRESULT* pResult)
{
    OnDragDrop(pNMHDR, pResult);
    *pResult = 0;
}

void CShellTreeCtrl::OnDragDrop(NMHDR* pNMHDR, LRESULT* pResult)
{
    TRACE("CShellListView::OnDragDrop\n");

    NMTREEVIEW      *pNMTree = (NMTREEVIEW*)pNMHDR;
    ASSERT(pNMTree);

    // OLE
    LPDATAOBJECT    lpdo;
    LPTVITEMDATA    lptvid;
    HRESULT         hr;

    lptvid = (LPTVITEMDATA)GetItemData(pNMTree->itemNew.hItem);

    if (lptvid != NULL)
    {
        hr = lptvid->lpsfParent->GetUIObjectOf(m_hWnd,
                                               1,
                                               (LPCITEMIDLIST*)&lptvid->lpi,
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
    }
}

void CShellTreeCtrl::DeleteInActiveItems()
{
    HTREEITEM htItem    = GetSelectedItem();
    HTREEITEM htParent  = htItem;
    HTREEITEM htSibling;
    HTREEITEM htNext;

    while (htParent != NULL)
    {
        htItem = htParent;

        // collapse siblings
        htSibling = GetNextSiblingItem(htItem);
        htNext = NULL;

        while (htSibling != NULL)
        {
            htNext = GetNextSiblingItem(htSibling);
            Expand(htSibling, TVE_COLLAPSE | TVE_COLLAPSERESET);
            htSibling = htNext;
        }

        htSibling = GetPrevSiblingItem(htItem);

        while (htSibling != NULL)
        {
            htNext = GetPrevSiblingItem(htSibling);
            Expand(htSibling, TVE_COLLAPSE | TVE_COLLAPSERESET);
            htSibling = htNext;
        }

        htParent = GetParentItem(htItem);
    }
}
