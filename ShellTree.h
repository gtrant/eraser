// ShellTree.h
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

#if !defined(AFX_SHELLTREE_H__6B1818E3_8ADA_11D1_B10E_40F603C10000__INCLUDED_)
#define AFX_SHELLTREE_H__6B1818E3_8ADA_11D1_B10E_40F603C10000__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000
// ShellTree.h : header file
//

#include <shlobj.h>
#include "EraserUI\ShellPidl.h"
#include "OleTreeCtrl.h"

class CShellTreeCtrl : public CDropTargetTreeCtrl, public CShellPidl
{
public:
    enum FindAttribs{type_drive, type_folder};

    // Construction
public:
    CShellTreeCtrl();

    virtual DROPEFFECT OnDragEnter(COleDataObject* pDataObject,
                                   DWORD dwKeyState, CPoint point);
    virtual DROPEFFECT OnDragOver(COleDataObject* pDataObject,
                                  DWORD dwKeyState, CPoint point);
    virtual BOOL OnDrop(COleDataObject* pDataObject,
                        DROPEFFECT dropEffect, CPoint point);
    virtual DROPEFFECT OnDropEx(COleDataObject* pDataObject, DROPEFFECT dropDefault,
                                DROPEFFECT dropList, CPoint point);
    virtual void OnDragLeave();

    // Attributes
public:

    // Operations
public:
    void            PopulateTree();
    void            PopulateTree(int nFolder);
    void            OnFolderExpanding(NMHDR* pNMHDR, LRESULT* pResult);
    void            GetContextMenu(NMHDR* pNMHDR, LRESULT* pResult);
    BOOL            OnFolderSelect(NMHDR* pNMHDR, LRESULT* pResult, CString &szFolderPath);
    void            OnDeleteShellItem(NMHDR* pNMHDR, LRESULT* pResult);
    BOOL            GetSelectedFolderPath(CString &szFolderPath);
    LPSHELLFOLDER   GetParentShellFolder(HTREEITEM folderNode);
    LPITEMIDLIST    GetRelativeIDLIST(HTREEITEM folderNode);
    LPITEMIDLIST    GetFullyQualifiedID(HTREEITEM folderNode);
    void            FindTreePidl(HTREEITEM nextNode, LPLVITEMDATA lplvid, BOOL& valid);
    void            RefreshChildren(HTREEITEM htItem);
    void            OnDragDrop(NMHDR* pNMHDR, LRESULT* pResult);

    // Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CShellTreeCtrl)
    public:
    //}}AFX_VIRTUAL

    // Implementation
public:
    void DeleteInActiveItems();
    virtual ~CShellTreeCtrl();

    // Generated message map functions
protected:
    CString             m_strCurrentPath;
    BOOL                m_bDragRight;

    int                 FillTreeView(LPSHELLFOLDER lpsf, LPITEMIDLIST lpifq, HTREEITEM hParent);
    void                GetNormalAndSelectedIcons(LPITEMIDLIST lpifq, LPTVITEM lptvitem);
    static int CALLBACK TreeViewCompareProc(LPARAM, LPARAM, LPARAM);

    //{{AFX_MSG(CShellTreeCtrl)
    afx_msg void OnItemexpanding(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnRclick(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnDeleteitem(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnSelchanging(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnBeginDrag(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnBeginRDrag(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    afx_msg void OnEditCopy();
    //}}AFX_MSG
    afx_msg LRESULT OnSelectChild(WPARAM, LPARAM);
    afx_msg LRESULT OnFolderCount(WPARAM, LPARAM);
    afx_msg LRESULT OnRefreshList(WPARAM, LPARAM);

    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_SHELLTREE_H__6B1818E3_8ADA_11D1_B10E_40F603C10000__INCLUDED_)
