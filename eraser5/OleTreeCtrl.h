// OleTreeCtrl.h
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

#if !defined(AFX_OLETREECTRL_H__42B73434_1782_11D3_81F0_000000000000__INCLUDED_)
#define AFX_OLETREECTRL_H__42B73434_1782_11D3_81F0_000000000000__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CDropTargetTreeCtrl window

class CDropTargetTreeCtrl : public CTreeCtrl
{
// Construction
public:
    CDropTargetTreeCtrl();

// Attributes
public:

// Operations
public:
    BOOL Register();
    virtual void Revoke();  // virtual for implementation

// Overridables
    virtual DROPEFFECT OnDragEnter(COleDataObject* pDataObject,
                                   DWORD dwKeyState, CPoint point);
    virtual DROPEFFECT OnDragOver(COleDataObject* pDataObject,
                                  DWORD dwKeyState, CPoint point);
    virtual BOOL OnDrop(COleDataObject* pDataObject,
                        DROPEFFECT dropEffect, CPoint point);
    virtual DROPEFFECT OnDropEx(COleDataObject* pDataObject, DROPEFFECT dropDefault,
                                DROPEFFECT dropList, CPoint point);
    virtual void OnDragLeave();
    virtual DROPEFFECT OnDragScroll(DWORD dwKeyState, CPoint point);
// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CDropTargetTreeCtrl)
    //}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CDropTargetTreeCtrl();

    // Generated message map functions
protected:
    //{{AFX_MSG(CDropTargetTreeCtrl)
    afx_msg void OnDestroy();
    //}}AFX_MSG

    DECLARE_MESSAGE_MAP()

protected:
    LPDATAOBJECT m_lpDataObject;    // != NULL between OnDragEnter, OnDragLeave
    BOOL m_bRegistered;

// Interface Maps
public:
    BEGIN_INTERFACE_PART(DropTarget, IDropTarget)
        INIT_INTERFACE_PART(CDropTargetTreeCtrl, DropTarget)
        STDMETHOD(DragEnter)(LPDATAOBJECT, DWORD, POINTL, LPDWORD);
        STDMETHOD(DragOver)(DWORD, POINTL, LPDWORD);
        STDMETHOD(DragLeave)();
        STDMETHOD(Drop)(LPDATAOBJECT, DWORD, POINTL pt, LPDWORD);
    END_INTERFACE_PART(DropTarget)

    DECLARE_INTERFACE_MAP()

};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_OLETREECTRL_H__42B73434_1782_11D3_81F0_000000000000__INCLUDED_)
