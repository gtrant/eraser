// FlatListCtrl.h
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

#if !defined(AFX_FLATLISTCTRL_H__2162BEB5_A882_11D2_B18A_B294B34D6940__INCLUDED_)
#define AFX_FLATLISTCTRL_H__2162BEB5_A882_11D2_B18A_B294B34D6940__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "FlatHeaderCtrl.h"

#define FLC_NOEDIT  1
#define FLC_EDIT    2

/////////////////////////////////////////////////////////////////////////////
// CFlatListCtrl window

class CFlatListCtrl : public CListCtrl
{
// Construction
public:
    CFlatListCtrl();
    void SetMenu(UINT nMenu)            { m_nMenuID = nMenu; }

// Attributes
public:

// Operations
public:
    CEdit* EditSubLabel(int nItem, int nCol);

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CFlatListCtrl)
    public:
    virtual BOOL PreTranslateMessage(MSG* pMsg);
    protected:
    virtual void PreSubclassWindow();
    //}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CFlatListCtrl();

protected:
    int HitTestEx(CPoint &point, int *col) const;

    CFlatHeaderCtrl m_wndFlatHeader;
    UINT m_nMenuID;
    int m_iLastIndex;

    // Generated message map functions
protected:
    afx_msg void OnContextMenu(CWnd*, CPoint point);
    //{{AFX_MSG(CFlatListCtrl)
    afx_msg void OnColumnClick(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
    afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
    afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
    //}}AFX_MSG

    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_FLATLISTCTRL_H__2162BEB5_A882_11D2_B18A_B294B34D6940__INCLUDED_)
