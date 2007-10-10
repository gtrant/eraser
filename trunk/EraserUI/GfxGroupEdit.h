// Copyright (c) Iuri Apollonio 1998
// Use & modify as you want & need, and leave those 4 lines.
// Strongly based on article "Inplace edit control" of Mario Contestabile and "Editable subitems" of Zafir
// http://www.codeguru.com

#if !defined(AFX_GFXGROUPEDIT_H__CA5BB022_1277_11D2_843A_0000B43382FE__INCLUDED_)
#define AFX_GFXGROUPEDIT_H__CA5BB022_1277_11D2_843A_0000B43382FE__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000
// GfxGroupEdit.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CGfxGroupEdit window

class CGfxGroupEdit : public CEdit
{
// Construction
public:
    CGfxGroupEdit();

// Attributes
public:
    BOOL    bEscapeKey;
    CString text;
    WPARAM  msgSend;
    bool    bNoDown;

// Operations
public:

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CGfxGroupEdit)
    public:
    virtual BOOL PreTranslateMessage(MSG* pMsg);
    protected:
    virtual void PostNcDestroy();
    //}}AFX_VIRTUAL

// Implementation
public:
    int iIndex;
    virtual ~CGfxGroupEdit();

    // Generated message map functions
protected:
    //{{AFX_MSG(CGfxGroupEdit)
    afx_msg void OnKillFocus(CWnd* pNewWnd);
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    afx_msg void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);
    //}}AFX_MSG

    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_GFXGROUPEDIT_H__CA5BB022_1277_11D2_843A_0000B43382FE__INCLUDED_)
