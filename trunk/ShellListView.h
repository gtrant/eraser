// ShellList.h
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

#if !defined(AFX_SHELLLIST_H__F6A0A301_8F85_11D1_B10E_40F603C10000__INCLUDED_)
#define AFX_SHELLLIST_H__F6A0A301_8F85_11D1_B10E_40F603C10000__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include <shlobj.h>
#include "EraserUI\FlatListView.h"
#include "EraserUI\ShellPidl.h"

/////////////////////////////////////////////////////////////////////////////
// CShellListView window

class CShellListView : public CFlatListView, public CShellPidl
{
public:

// Construction
public:
    CShellListView();

// Attributes
public:
    HWND                m_hwndTree;
    CEvent              m_evKillThread;
    CEvent              m_evNotRunning;

// Operations
public:
    BOOL                PopulateListView(LPITEMIDLIST lpidl, LPSHELLFOLDER lpsf, int&);
    int                 GetDoubleClickedItem();
    void                GetContextMenu();
    void                ShellOpenItem(int iItem);
    void                ShellOpenItem(LPLVITEMDATA lplvid);
    DWORD               GetCurrentView();
    BOOL                GetItemPath(int iItem, CString &szFolderPath);
    void                ResizeColumns();
    void                OnDragDrop(NMHDR* pNMHDR, LRESULT* pResult);
// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CShellListView)
    public:
    virtual BOOL Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext = NULL);
    virtual BOOL OnDrop(COleDataObject* pDataObject, DROPEFFECT dropEffect, CPoint point);
    virtual DROPEFFECT OnDropEx(COleDataObject* pDataObject, DROPEFFECT dropDefault, DROPEFFECT dropList, CPoint point);
    virtual DROPEFFECT OnDragOver(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point);
    virtual DROPEFFECT OnDragEnter(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point);
    virtual void OnDragLeave();
    protected:
    virtual void OnUpdate(CView* pSender, LPARAM lHint, CObject* pHint);
    //}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CShellListView();

// Private Parts
private:

    // Generated message map functions
protected:
    HANDLE              m_hChangeNotification;
    LPTVITEMDATA        m_ptvCurrent;

    BOOL                m_bDragRight;
    COleDropTarget      m_odtTarget;

    BOOL                CopyTVItemData(LPTVITEMDATA ptv, LPTVITEMDATA *pptv);
    void                DeleteTVItemData(LPTVITEMDATA *pptv);

    BOOL                InitListViewItems(LPITEMIDLIST lpidl, LPSHELLFOLDER lpsf, int&);
    void                GetNormalAndSelectedIcons(LPITEMIDLIST lpifq, LPTVITEM lptvitem);

    static int CALLBACK ListViewCompareProc(LPARAM, LPARAM, LPARAM);
    static UINT         RefreshThread(LPVOID);

    //{{AFX_MSG(CShellListView)
    afx_msg void OnSize(UINT nType, int cx, int cy);
    afx_msg void OnRclick(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnDblclk(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnBeginDrag(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnBeginRDrag(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnDeleteItem(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnEditRefresh();
    afx_msg void OnEditSelectAll();
    //}}AFX_MSG
    afx_msg void OnUpdateItems(CCmdUI* pCmdUI);
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_SHELLLIST_H__F6A0A301_8F85_11D1_B10E_40F603C10000__INCLUDED_)
