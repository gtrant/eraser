// MainFrm.h
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

#if !defined(AFX_MAINFRM_H__70E9C858_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_MAINFRM_H__70E9C858_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "EraserUI\GfxSplitterWnd.h"
#include "EraserUI\GfxOutBarCtrl.h"
#include "EraserUI\ShellPidl.h"
#include "EraserUI\AlphaToolBar.h"
#include "ShellTree.h"
#include "ChildFrame.h"

class CMainFrame : public CFrameWnd
{

protected: // create from serialization only
    CMainFrame();
    DECLARE_DYNCREATE(CMainFrame)

    CChildFrame     *m_pwndChild;

    CGfxSplitterWnd m_wndSplitter;
    CGfxOutBarCtrl  m_wndBar;

    CImageList      m_imaLarge;
    CImageList      m_imaSmall;
	CBitmap			m_bmToolbarHi;

    CShellTreeCtrl  m_wndTree;

    int m_iLastActiveItem;

// Attributes
public:

// Operations
public:
    void SetInfoText(LPCTSTR info, BOOL setTitle = TRUE);

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CMainFrame)
	public:
    virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
	protected:
    virtual BOOL OnCreateClient(LPCREATESTRUCT lpcs, CCreateContext* pContext);
	//}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CMainFrame();


#ifdef _DEBUG
    virtual void AssertValid() const;
    virtual void Dump(CDumpContext& dc) const;
#endif

protected:  // control bar embedded members
    CStatusBar  m_wndStatusBar;
    CAlphaToolBar    m_wndToolBar;
    CReBar      m_wndReBar;


// Generated message map functions
protected:
    //{{AFX_MSG(CMainFrame)
    afx_msg void OnSysCommand( UINT nID, LPARAM lParam );
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    afx_msg void OnViewInfoBar();
    afx_msg void OnUpdateViewInfoBar(CCmdUI* pCmdUI);
    afx_msg void OnDestroy();
    //}}AFX_MSG
    afx_msg LRESULT OnOutbarNotify(WPARAM wParam, LPARAM lParam);
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_MAINFRM_H__70E9C858_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_)
