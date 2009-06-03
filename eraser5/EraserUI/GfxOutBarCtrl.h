// Copyright (c) Iuri Apollonio 1998
// Use & modify as you want & need, and leave those 3 lines.
// http://www.codeguru.com
//

// Animation improvements and drag & drop support.
// Copyright © 1997-2001 by Sami Tolvanen.

#if !defined(AFX_GFXOUTBARCTRL_H__28FA2CA4_11B7_11D2_8437_0000B43382FE__INCLUDED_)
#define AFX_GFXOUTBARCTRL_H__28FA2CA4_11B7_11D2_8437_0000B43382FE__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000
// GfxOutBarCtrl.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CGfxOutBarCtrl window

#include <afxcoll.h>
#include "DropTargetWnd.h"

#define WM_OUTBAR_NOTIFY        WM_USER + 1
#define NM_OB_ITEMCLICK         1
#define NM_OB_ONLABELENDEDIT    2
#define NM_OB_ONGROUPENDEDIT    3
#define NM_OB_DRAGITEM          4
#define NM_FOLDERCHANGE         5

// sami@tolvanen.com
#define NM_PREFOLDERCHANGE      6
#define NM_ICONANIMATION        7
#define NM_ICONVIEWCHANGE       8

struct OUTBAR_INFO
{
    int             index;
    const TCHAR *    cText;
    int             iDragFrom;
    int             iDragTo;
};

class CGfxOutBarCtrl : public CDropTargetWnd
{
// Construction
    DECLARE_DYNCREATE(CGfxOutBarCtrl)
public:
    CGfxOutBarCtrl();

    enum {  fSmallIcon = 1 << 0, fLargeIcon = 1 << 1, fEditGroups = 1 << 2, fEditItems = 1 << 3,
            fRemoveGroups = 1 << 4, fRemoveItems = 1 << 5, fAddGroups = 1 << 6,
            fDragItems = 1 << 7, fAnimation = 1 << 8, fSelHighlight = 1 << 9 };

    enum { ircIcon = 1, ircLabel = 2, ircAll = 3 };

// Attributes
public:
    COLORREF    crBackGroundColor, crBackGroundColor1;
    COLORREF    crTextColor;
    COLORREF    cr3dFace, crLightBorder, crHilightBorder, crShadowBorder, crDkShadowBorder;
    int         iFolderHeight;

    int         xSmallIconLabelOffset, yLargeIconLabelOffset;
    int         ySmallIconSpacing, yLargeIconSpacing;

    int         xLeftMargin, yTopMargin;
    bool        bUpArrow, bDownArrow, bUpPressed, bDownPressed;
    CRect       rcUpArrow, rcDownArrow;
    bool        bLooping;

    int         iHitInternal1, iHitInternal2;

    long        lAnimationTickCount;

    int         iLastSel, iSelAnimTiming;
    int         iSelAnimCount;


    DWORD       dwFlags;

    CPtrArray   arFolder;

    int         iLastFolderHighlighted;
    int         iLastSelectedFolder;
    int         iFirstItem;

    int         iLastItemHighlighted;
    bool        bPressedHighlight;

    int         iLastDragItemDraw, iLastDragItemDrawType;

    class CBarItem
    {
    public:
        CBarItem(const TCHAR * name, const int image, DWORD exData);
        virtual  ~CBarItem();
        int             iImageIndex;
        TCHAR *          cItem;
        DWORD           dwData;
    };

    class CBarFolder
    {
    public:
        CBarFolder(const TCHAR * name, DWORD exData);
        virtual  ~CBarFolder();
        int GetItemCount();
        int InsertItem(int index, const TCHAR * text, const int image, const DWORD exData);
        TCHAR *          cName;
        DWORD           dwData;
        CImageList *    pLargeImageList;
        CImageList *    pSmallImageList;
        CPtrArray       arItems;
        CWnd *          pChild;
    };

    int iSelFolder;

    CImageList *    pLargeImageList;
    CImageList *    pSmallImageList;

    HCURSOR         hHandCursor;
    HCURSOR         hDragCursor;
    HCURSOR         hNoDragCursor;

    CPen *          pBlackPen;

    // for drag open support - sami@tolvanen.com
    BOOL            m_bDragOver;

// Operations
public:
    enum { htNothing = -1, htFolder, htItem, htUpScroll, htDownScroll};

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CGfxOutBarCtrl)
    //}}AFX_VIRTUAL

// Implementation
public:
    void DrawAnimItem(const int xoffset, const int yoffset, const int index);
    void SetAnimSelHighlight(const int iTime);
    int GetAnimSelHighligt() { return iSelAnimTiming; } // sami@tolvanen.com
    DWORD GetFolderData(int iFolder = -1);
    CWnd * GetFolderChild(int iFolder = -1);
    int AddFolderBar(const TCHAR * pFolder, CWnd * pSon, const DWORD exData = 0);
    CString GetItemText(const int index);
    void SetAnimationTickCount(const long value) { lAnimationTickCount = value; };
    long GetAnimationTickCount() { return lAnimationTickCount; };

    void AnimateFolderScroll(const int iFrom, const int iTo);
    int GetDragItemRect(const int index, CRect &rect);
    void DrawDragArrow(CDC * pDC, const int iFrom, const int iTo);
    void SetItemImage(const int index, const int iImage);
    void SetItemData(const int index, const DWORD dwData);
    int  GetItemImage(const int index) const;
    DWORD GetItemData(const int index) const;
    bool IsValidItem(const int index) const;
    void RemoveItem(const int index);
    void SetItemText(const int index, const TCHAR * text);
    void StartItemEdit(const int index);
    void SetFolderText(const int index, const TCHAR * text);
    void StartGroupEdit(const int index);
    void GetLabelRect(const int iFolder, const int iIndex, CRect &rect);
    void GetIconRect(const int iFolder, const int iIndex, CRect &rect);
    void HighlightItem(const int index, const bool bPressed = false);
    void GetVisibleRange(const int iFolder, int &first, int &last);
    void DrawItem(CDC * pDC, const int iFolder, CRect rc, const int index, const bool bOnlyImage = false);
    CImageList * GetFolderImageList(const int index, const bool bSmall) const;
    CSize GetItemSize(const int iFolder, const int index, const int type);
    void PaintItems(CDC * pDC, const int iFolder, CRect rc);
    CImageList * GetImageList(CImageList * pImageList, int nImageList);
    CImageList * SetFolderImageList(const int folder, CImageList * pImageList, int nImageList);
    CImageList * SetImageList(CImageList * pImageList, int nImageList);
    int GetCountPerPage() const;
    void RemoveFolder(const int index);
    int GetSelFolder() const;
    int GetFolderCount() const;
    void SetSelFolder(const int index);
    int GetItemCount() const;
    int InsertItem(const int folder, const int index, const TCHAR * text, const int image = -1, const DWORD exData = 0);
    void HighlightFolder(const int index);
    int HitTestEx(const CPoint &point, int &index);
    void GetInsideRect(CRect &rect) const;
    int AddFolder(const TCHAR * cFolderName, const DWORD exData);
    void GetItemRect(const int iFolder, const int iIndex, CRect &rect);
    bool GetFolderRect(const int iIndex, CRect &rect) const;
    void ModifyFlag(const DWORD &dwRemove, const DWORD &dwAdd, const UINT redraw = 0);
    DWORD GetFlag() const;
    void SetSmallIconView(const bool bSet);
    bool IsSmallIconView() const;
    BOOL Create(DWORD dwStyle, const RECT& rect, CWnd * pParentWnd, UINT nID, const DWORD dwFlag = fDragItems|fEditGroups|fEditItems|fRemoveGroups|fRemoveItems|fAddGroups|fAnimation|fSelHighlight);
    virtual ~CGfxOutBarCtrl();

    // for drag open support - sami@tolvanen.com
    virtual DROPEFFECT OnDragEnter(COleDataObject* pDataObject,
                                   DWORD dwKeyState, CPoint point);
    virtual void OnDragLeave();

    // Generated message map functions
protected:
    void DrawFolder(CDC * pDC, const int iIdx, CRect rect, const bool bSelected);
    //{{AFX_MSG(CGfxOutBarCtrl)
    afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
    afx_msg void OnTimer(UINT_PTR nIDEvent);
    afx_msg void OnPaint();
    afx_msg BOOL OnEraseBkgnd(CDC* pDC);
    afx_msg void OnMouseMove(UINT nFlags, CPoint point);
    afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
    afx_msg BOOL OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message);
    afx_msg void OnSize(UINT nType, int cx, int cy);
    afx_msg void OnRButtonDown(UINT nFlags, CPoint point);
    afx_msg void OnGfxLargeicon();
    afx_msg void OnUpdateGfxLargeicon(CCmdUI* pCmdUI);
    afx_msg void OnGfxSmallicon();
    afx_msg void OnUpdateGfxSmallicon(CCmdUI* pCmdUI);
    afx_msg void OnGfxRemoveitem();
    afx_msg void OnUpdateGfxRemoveitem(CCmdUI* pCmdUI);
    afx_msg void OnGfxRenameitem();
    afx_msg void OnUpdateGfxRenameitem(CCmdUI* pCmdUI);
    afx_msg void OnSysColorChange();
    afx_msg void OnGfxAnimation();
    //}}AFX_MSG
    afx_msg LRESULT OnEndLabelEdit(WPARAM wParam, LPARAM lParam);
    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_GFXOUTBARCTRL_H__28FA2CA4_11B7_11D2_8437_0000B43382FE__INCLUDED_)