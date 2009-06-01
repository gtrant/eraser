////////////////////////////////////////////////////////////////////////////
//  File:       CFlatHeaderCtrl.h
//  Version:    1.1.0.0
//
//  Author:     Maarten Hoeben
//  E-mail:     hoeben@nwn.com
//
//  Specification of the CFlatHeaderCtrl and associated classes.
//
//  You are free to use, distribute or modify this code
//  as long as the header is not removed or modified.
//
////////////////////////////////////////////////////////////////////////////

#if !defined(AFX_FLATHEADERCTRL_H__2162BEB4_A882_11D2_B18A_B294B34D6940__INCLUDED_)
#define AFX_FLATHEADERCTRL_H__2162BEB4_A882_11D2_B18A_B294B34D6940__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// FlatHeaderCtrl.h : header file
//
#ifdef DMARS
#define HDITEM HD_ITEM
#define HDHITTESTINFO  _HD_HITTESTINFO
#endif
#include <afxtempl.h>
#include <tchar.h>

#include "MemDC.h"

#define FLATHEADER_TEXT_MAX 80

/////////////////////////////////////////////////////////////////////////////
// CFlatHeaderCtrl window

class CFlatHeaderCtrl;
class CFHDragWnd;

#define FH_PROPERTY_SPACING         1
#define FH_PROPERTY_ARROW           2
#define FH_PROPERTY_STATICBORDER    3

typedef struct _HD_ITEMEX
{
    _HD_ITEMEX() :
        m_iMinWidth(0),
        m_iMaxWidth(-1)
    {}

    INT     m_iMinWidth;
    INT     m_iMaxWidth;

} HDITEMEX, FAR * LPHDITEMEX;

class CFlatHeaderCtrl : public CHeaderCtrl
{
    friend class CFHDragWnd;

    DECLARE_DYNCREATE(CFlatHeaderCtrl)

// Construction
public:
    CFlatHeaderCtrl();

// Attributes
public:
    BOOL    ModifyProperty(WPARAM wParam, LPARAM lParam);

    BOOL    GetItemEx(INT iPos, HDITEMEX* phditemex) const;
    BOOL    SetItemEx(INT iPos, HDITEMEX* phditemex);

    void    SetSortColumn(INT iPos, BOOL bSortAscending);
    INT     GetSortColumn(BOOL* pbSortAscending = NULL);

// Overrides
public:
    virtual ~CFlatHeaderCtrl();

    virtual void DrawItem(LPDRAWITEMSTRUCT);
    virtual void DrawItem(CDC* pDC, CRect rect, HDITEM hditem, BOOL bSort, BOOL bSortAscending);

    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CFlatHeaderCtrl)
    //}}AFX_VIRTUAL

// Implementation
protected:
    //BOOL          m_bNoFlicker;
    INT             m_iSpacing;
    SIZE            m_sizeArrow;
    BOOL            m_bStaticBorder;

    INT             m_iHotIndex;
    INT             m_iHotOrder;
    BOOL            m_bHotItemResizable;
    HDHITTESTINFO   m_hdhtiHotItem;
    HDITEM          m_hditemHotItem;
    TCHAR           m_szHotItemText[FLATHEADER_TEXT_MAX];

    BOOL            m_bResizing;

    INT             m_iHotDivider;
    COLORREF        m_crHotDivider;

    BOOL            m_bDragging;
    CFHDragWnd      *m_pDragWnd;

    UINT            m_nClickFlags;
    CPoint          m_ptClickPoint;

    BOOL            m_bSortAscending;
    INT             m_iSortColumn;
    CArray<HDITEMEX, HDITEMEX> m_arrayHdrItemEx;

    COLORREF        m_cr3DHighLight;
    COLORREF        m_cr3DShadow;
    COLORREF        m_cr3DFace;
    COLORREF        m_crText;

    void            DrawCtrl(CDC* pDC);
    INT             DrawImage(CDC* pDC, CRect rect, HDITEM hditem, BOOL bRight);
    INT             DrawBitmap(CDC* pDC, CRect rect, HDITEM hditem, CBitmap* pBitmap,
                               BITMAP* pBitmapInfo, BOOL bRight);
    INT             DrawText (CDC* pDC, CRect rect, HDITEM hditem);
    INT             DrawArrow(CDC* pDC, CRect rect, BOOL bSortAscending, BOOL bRight);

// Generated message map functions
protected:
    //{{AFX_MSG(CFlatHeaderCtrl)
    afx_msg LRESULT OnInsertItem(WPARAM wparam, LPARAM lparam);
    afx_msg LRESULT OnDeleteItem(WPARAM wparam, LPARAM lparam);
    afx_msg LRESULT OnSetHotDivider(WPARAM wparam, LPARAM lparam);
    afx_msg LRESULT OnLayout(WPARAM wparam, LPARAM lparam);
#if _MFC_VER <= 0x800
    afx_msg UINT OnNcHitTest(CPoint point);
#else
	afx_msg LRESULT OnNcHitTest(CPoint point);
#endif
    afx_msg BOOL OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message);
    afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
    afx_msg void OnLButtonDblClk(UINT nFlags, CPoint point);
    afx_msg void OnPaint();
    afx_msg void OnSysColorChange();
    afx_msg BOOL OnEraseBkgnd(CDC* pDC);
    afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
    afx_msg void OnMouseMove(UINT nFlags, CPoint point);
    //}}AFX_MSG

    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////
// CFHDragWnd window

#define FHDRAGWND_CLASSNAME _T("MFCFHDragWnd")

class CFHDragWnd : public CWnd
{
// Construction
public:
    CFHDragWnd();

// Attributes
public:

// Operations
public:

// Overrrides
protected:
    // Drawing
    virtual void OnDraw(CDC* pDC);

    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CFHDragWnd)
    protected:
    virtual void PostNcDestroy();
    //}}AFX_VIRTUAL

// Implementation
public:
    virtual ~CFHDragWnd();
    virtual BOOL Create(CRect rect, CFlatHeaderCtrl* pFlatHeaderCtrl, INT iItem);

protected:
    CFlatHeaderCtrl *m_pFlatHeaderCtrl;
    INT             m_iItem;

    // Generated message map functions
protected:
    //{{AFX_MSG(CFHDragWnd)
    afx_msg void OnPaint();
    afx_msg BOOL OnEraseBkgnd(CDC* pDC);
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_FLATHEADERCTRL_H__2162BEB4_A882_11D2_B18A_B294B34D6940__INCLUDED_)
