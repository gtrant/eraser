// FlatListView.h
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

#if !defined(AFX_FLATLISTVIEW_H__DA8B78C0_F1B2_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_FLATLISTVIEW_H__DA8B78C0_F1B2_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "FlatHeaderCtrl.h"

#define FLC_NOEDIT  1
#define FLC_EDIT    2

/////////////////////////////////////////////////////////////////////////////
// CFlatListView view

class CFlatListView : public CListView
{
protected:
    CFlatListView();           // protected constructor used by dynamic creation
    DECLARE_DYNCREATE(CFlatListView)

// Attributes
public:

// Operations
public:
    CEdit* EditSubLabel(int nItem, int nCol);

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CFlatListView)
    public:
    virtual BOOL PreTranslateMessage(MSG* pMsg);
    virtual BOOL Create(LPCTSTR lpszClassName, LPCTSTR lpszWindowName, DWORD dwStyle, const RECT& rect, CWnd* pParentWnd, UINT nID, CCreateContext* pContext = NULL);
    protected:
    virtual void OnDraw(CDC* pDC);      // overridden to draw this view
    virtual void PreSubclassWindow();
    virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
    //virtual int OnToolHitTest( CPoint point, TOOLINFO* pTI ) const;
	virtual INT_PTR OnToolHitTest(CPoint point, TOOLINFO* pTI) const;
    //}}AFX_VIRTUAL

// Implementation
protected:
    virtual ~CFlatListView();

    int HitTestEx(CPoint &point, int *col, RECT *rectCell = NULL) const;
    BOOL OnToolTipText(UINT id, NMHDR * pNMHDR, LRESULT * pResult);

    CFlatHeaderCtrl m_wndFlatHeader;
    UINT m_nMenuID;
    int m_iLastIndex;

#ifdef _DEBUG
    virtual void AssertValid() const;
    virtual void Dump(CDumpContext& dc) const;
#endif

    // Generated message map functions
protected:
    int SelItemRange(BOOL bSelect, int nFirstItem, int nLastItem);
    void SetRedraw(BOOL);
    //{{AFX_MSG(CFlatListView)
    afx_msg void OnColumnclick(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
    afx_msg void OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
    afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_FLATLISTVIEW_H__DA8B78C0_F1B2_11D2_BBF3_00105AAF62C4__INCLUDED_)
