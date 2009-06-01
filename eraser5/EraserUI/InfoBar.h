/*
 Written by Steve Bryndin (fishbed@tezcat.com, steveb@gvsi.com).

 This code may be used in compiled form in any way you wish. This
 file may be redistributed unmodified by any means PROVIDING it is
 not sold for profit without the authors written consent, and
 providing that this notice and the authors name is included.
 An email letting me know that you are using it would be
 nice as well.

 This software is provided "as is" without express or implied warranty.
 Use it at you own risk! The author accepts no liability for any damages
 to your computer or data these products may cause.
*/

// From http://www.codeguru.com/submission_guide.shtml :
//
// "While we are talking about copyrights, you retain copyright of
//  your article and code but by submitting it to CodeGuru you give it
//  permission to use it in a fair manner and also permit all developers
//  to freely use the code in their own applications - even if they are
//  commercial."


#if !defined(AFX_INFOBAR_H__C789E26C_DA4B_11D2_BF44_006008085F93__INCLUDED_)
#define AFX_INFOBAR_H__C789E26C_DA4B_11D2_BF44_006008085F93__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// InfoBar.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CInfoBar window

class CInfoBar : public CControlBar
{
// Construction
public:
    CInfoBar();

// Attributes
public:

// Operations
public:

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CInfoBar)
    //}}AFX_VIRTUAL

// Implementation
public:
    void SetTextColor(COLORREF crNew);
    void SetBackgroundColor(COLORREF cr);
    BOOL SetTextFont(LPCTSTR lpFontName);
    BOOL SetBitmap(UINT nResID);
    void SetText(LPCTSTR lpszNew);
    LPCTSTR GetText();
    virtual ~CInfoBar();

    // Generated message map functions
protected:
    //{{AFX_MSG(CInfoBar)
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    afx_msg BOOL OnEraseBkgnd(CDC* pDC);
    afx_msg void OnPaint();
    afx_msg void OnSysColorChange();
    //}}AFX_MSG
    afx_msg LRESULT OnSizeParent(WPARAM, LPARAM lParam);
    DECLARE_MESSAGE_MAP()

    BOOL m_bCustomBkgnd;
    int m_cxAvailable;
    CFont m_font;
    CString m_caption;
    CBitmap m_bm;

    virtual void OnUpdateCmdUI(CFrameWnd* pTarget, BOOL bDisableIfNoHndler);
    virtual CSize CalcFixedLayout(BOOL bStretch, BOOL bHorz);
private:
    COLORREF    m_crBackgroundColor;
    COLORREF    m_crTextColor;
    CSize       m_sizeBitmap;
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_INFOBAR_H__C789E26C_DA4B_11D2_BF44_006008085F93__INCLUDED_)
