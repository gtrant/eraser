// GfxPopupMenu.cpp: implementation of the CGfxPopupMenu class.
// Copyright (c) Iuri Apollonio 1998

// Menu resource loading and cooler check marks.
// Copyright © 1998-2001 by Sami Tolvanen.
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "resource.h"
#include "GfxPopupMenu.h"

#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#define new DEBUG_NEW
#endif

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

CGfxPopupMenu::CGfxPopupMenu()
{
    crMenuText      = GetSysColor(COLOR_MENUTEXT);
    crMenuTextSel   = GetSysColor(COLOR_HIGHLIGHTTEXT);

    cr3dFace        = GetSysColor(COLOR_3DFACE);
    crMenu          = GetSysColor(COLOR_MENU);
    crHighlight     = GetSysColor(COLOR_HIGHLIGHT);
    cr3dHilight     = GetSysColor(COLOR_3DHILIGHT);
    cr3dShadow      = GetSysColor(COLOR_3DSHADOW);
    crGrayText      = GetSysColor(COLOR_GRAYTEXT);

    m_clrBtnFace    = GetSysColor(COLOR_BTNFACE);
    m_clrBtnHilight = GetSysColor(COLOR_BTNHILIGHT);
    m_clrBtnShadow  = GetSysColor(COLOR_BTNSHADOW);

    iSpawnItem      = 0;
    pSpawnItem      = NULL;

    iImageItem      = 0;
    pImageItem      = NULL;

    szImage         = CSize(18, 18);

    hMenuFont = NULL;

    ilOther.Create(17, 17, ILC_COLOR16 | ILC_MASK, 2, 0);

    // sami@tolvanen.com
    // fancy check marks MSVC++ has
    CBitmap     bmNormal;
    CBitmap     bmSelected;
    COLORMAP    cMap[3] =
    {
        { RGB(128, 128, 128), cr3dShadow    },
        { RGB(192, 192, 192), cr3dFace      },
        { RGB(255, 255, 255), cr3dHilight   }
    };

    bmNormal.LoadMappedBitmap(IDB_GFX_MENUCHECK, 0, cMap, 3);
    ilOther.Add(&bmNormal, cr3dFace);

    bmSelected.LoadMappedBitmap(IDB_GFX_MENUCHECK_SELECTED, 0, cMap, 3);
    ilOther.Add(&bmSelected, cr3dHilight);

    bmNormal.DeleteObject();
    bmSelected.DeleteObject();

    NONCLIENTMETRICS ncm;
    memset(&ncm, 0, sizeof(ncm));
    ncm.cbSize = sizeof(ncm);

    ::SystemParametersInfo(SPI_GETNONCLIENTMETRICS, 0, (PVOID) &ncm, 0);

    hGuiFont = ::CreateFontIndirect(&ncm.lfMenuFont);

    // David 08/04/98 - bold font handling
    hMenuBoldFont = NULL;
    CreateBoldFont();
}

CGfxPopupMenu::~CGfxPopupMenu()
{
    if (iSpawnItem > 0)
    {
        for (int t = 0; t < iSpawnItem; t++)
            if (pSpawnItem[t])
                delete pSpawnItem[t];

            GlobalFree((HGLOBAL) pSpawnItem);
    }

    if (iImageItem > 0)
        GlobalFree((HGLOBAL) pImageItem);

    if (hMenuFont)
        ::DeleteObject((HGDIOBJ)hMenuFont);
    if (hMenuBoldFont)
        ::DeleteObject((HGDIOBJ)hMenuBoldFont);
    if (hGuiFont)
        ::DeleteObject((HGDIOBJ)hGuiFont);
}

void CGfxPopupMenu::DrawItem(LPDRAWITEMSTRUCT lpDrawItemStruct)
{
    TRACE0("CGfxPopupMenu::DrawItem\n");

    if (lpDrawItemStruct->CtlType == ODT_MENU)
    {
        UINT state      = lpDrawItemStruct->itemState;
        bool bEnab      = !(state & ODS_DISABLED);
        bool bSelect    = (state & ODS_SELECTED) ? true : false;
        bool bChecked   = (state & ODS_CHECKED) ? true : false;

        // David 08/04/98 - bold font handling
        bool bBold      = (state & ODS_DEFAULT) ? true : false;

        SpawnItem *pItem = (SpawnItem *) lpDrawItemStruct->itemData;

        if (pItem)
        {
            CDC     dc;
            HGDIOBJ hOf = NULL;

            dc.Attach(lpDrawItemStruct->hDC);

            // David 08/04/98 - bold font handling
            if (!bBold)
                hOf = ::SelectObject(dc.GetSafeHdc(), (HGDIOBJ) (hMenuFont ? hMenuFont : hGuiFont));
            else
                hOf = ::SelectObject(dc.GetSafeHdc(), (HGDIOBJ) (hMenuBoldFont ? hMenuBoldFont : hGuiFont));

            CRect   rc(lpDrawItemStruct->rcItem);
            CRect   rcImage(rc);
            CRect   rcText(rc);

            rcImage.right   = rcImage.left + rc.Height();
            rcImage.bottom  = rc.bottom;

            if (pItem->iCmd == -3) // is a separator
            {
                CPen pnDk(PS_SOLID, 1, cr3dShadow);
                CPen pnLt(PS_SOLID, 1, cr3dHilight);

                CPen *opn = dc.SelectObject(&pnDk);
                dc.MoveTo(rc.left + 2, rc.top + 2);
                dc.LineTo(rc.right - 2, rc.top + 2);

                dc.SelectObject(&pnLt);
                dc.MoveTo(rc.left + 2, rc.top + 3);
                dc.LineTo(rc.right - 2, rc.top + 3);

                dc.SelectObject(opn);
            }
            /*else if (pItem->iCmd == -4) // is a title item
            {
                CString cs(pItem->cText);
                CRect   rcBdr(rcText);

                if (bSelect && bEnab)
                {
                    rcText.top++;
                    rcText.left += 2;
                }

                dc.FillSolidRect(rcText, crMenu);
                dc.DrawText(cs, rcText, DT_VCENTER | DT_CENTER | DT_SINGLELINE);

                if (bSelect && bEnab)
                    dc.Draw3dRect(rcBdr, cr3dShadow, cr3dHilight);
            }*/
            else
            {
                if (pItem->iCmd == -4)
                    rcText.left += 3;

                rcText.left += rcImage.right + 1;

                int         obk = dc.SetBkMode(TRANSPARENT);
                COLORREF    ocr;

                if (bSelect)
                {
                    if (pItem->iImageIdx >= 0 || bChecked)
                        dc.FillSolidRect(rcText, crHighlight);
                    else
                        dc.FillSolidRect(rc, crHighlight);

                    ocr = dc.SetTextColor(crMenuTextSel);
                }
                else
                {
                    if (pItem->iImageIdx >= 0 || bChecked)
                        dc.FillSolidRect(rcText, crMenu);
                    else
                        dc.FillSolidRect(rc, crMenu);

                    ocr = dc.SetTextColor(crMenuText);
                }

                if (pItem->iImageIdx >= 0)
                {
                    int ay = (rcImage.Height() - szImage.cy) / 2;
                    int ax = (rcImage.Width()  - szImage.cx) / 2;

                    if (bSelect && bEnab)
                        dc.Draw3dRect(rcImage, cr3dHilight, cr3dShadow);
                    else
                        dc.Draw3dRect(rcImage, crMenu, crMenu);


                    if (bEnab)
                        ilList.Draw(&dc, pItem->iImageIdx, CPoint(rcImage.left + ax, rcImage.top +ay), ILD_NORMAL);
                    else
                    {
                        HICON hIcon = ilList.ExtractIcon(pItem->iImageIdx);
                        dc.DrawState(CPoint(rcImage.left + ax, rcImage.top + ay), szImage, hIcon, DST_ICON | DSS_DISABLED, (CBrush *)NULL);
                        ::DestroyIcon(hIcon);
                    }
                }
                else
                {
                    if (bChecked)
                    {
                        int ay = (rcImage.Height() - szImage.cy) / 2;
                        int ax = (rcImage.Width()  - szImage.cx) / 2;

                        ilOther.Draw(&dc, (bSelect) ? 1 : 0, CPoint(rcImage.left + ax, rcImage.top + ay - 1), ILD_NORMAL);
                    }
                }

                CString cs(pItem->cText);
                CString cs1;
                CSize   sz                  = dc.GetTextExtent(cs);
                int     ay1                 = (rcText.Height() - sz.cy) / 2;

                rcText.top      += ay1;
                rcText.left     += 2;
                rcText.right    -= 15;

                int tf = cs.Find('\t');

                if (tf >= 0)
                {
                    cs1 = cs.Right(cs.GetLength() - tf - 1);
                    cs  = cs.Left(tf);

                    if (!bEnab)
                    {
                        if (!bSelect)
                        {
                            CRect rcText1(rcText);
                            rcText1.InflateRect(-1, -1);

                            dc.SetTextColor(cr3dHilight);
                            dc.DrawText(cs, rcText1, DT_VCENTER | DT_LEFT);
                            dc.DrawText(cs1, rcText1, DT_VCENTER | DT_RIGHT);

                            dc.SetTextColor(crGrayText);
                            dc.DrawText(cs, rcText, DT_VCENTER | DT_LEFT);
                            dc.DrawText(cs1, rcText, DT_VCENTER | DT_RIGHT);
                        }
                        else
                        {
                            dc.SetTextColor(crMenu);
                            dc.DrawText(cs, rcText, DT_VCENTER | DT_LEFT);
                            dc.DrawText(cs1, rcText, DT_VCENTER | DT_RIGHT);
                        }
                    }
                    else
                    {
                        dc.DrawText(cs, rcText, DT_VCENTER | DT_LEFT);
                        dc.DrawText(cs1, rcText, DT_VCENTER | DT_RIGHT);
                    }
                }
                else
                {
                    if (!bEnab)
                    {
                        if (!bSelect)
                        {
                            CRect rcText1(rcText);
                            rcText1.InflateRect(-1, -1);

                            dc.SetTextColor(cr3dHilight);
                            dc.DrawText(cs, rcText1, DT_VCENTER | DT_LEFT | DT_EXPANDTABS);

                            dc.SetTextColor(crGrayText);
                            dc.DrawText(cs, rcText, DT_VCENTER | DT_LEFT | DT_EXPANDTABS);
                        }
                        else
                        {
                            // sami@tolvanen.com
                            dc.SetTextColor(crMenu);
                            dc.DrawText(cs, rcText, DT_VCENTER | DT_LEFT | DT_EXPANDTABS);
                        }
                    }
                    else
                        dc.DrawText(cs, rcText, DT_VCENTER | DT_LEFT | DT_EXPANDTABS);
                }

                dc.SetTextColor(ocr);
                dc.SetBkMode(obk);
            }

            ::SelectObject(dc.GetSafeHdc(), hOf);
            dc.Detach();
        }
    }
}

void CGfxPopupMenu::MeasureItem(LPMEASUREITEMSTRUCT lpMeasureItemStruct)
{
    TRACE0("CGfxPopupMenu::MeasureItem\n");

    if (lpMeasureItemStruct->CtlType == ODT_MENU)
    {
        SpawnItem *pItem = (SpawnItem *) lpMeasureItemStruct->itemData;

        if (pItem)
        {
            if (pItem->iCmd == -3) // is a separator
            {
                lpMeasureItemStruct->itemWidth  = 10;
                lpMeasureItemStruct->itemHeight = 6;
            }
            else
            {
                CString cs(pItem->cText);

                if (!cs.IsEmpty())
                {
                    CClientDC   dc(AfxGetMainWnd());
                    CFont       *pft    = CFont::FromHandle(hMenuFont ? hMenuFont : hGuiFont);
                    CFont       *of     = dc.SelectObject(pft);
                    CSize       osz     = dc.GetOutputTabbedTextExtent(cs, 0, NULL);

                    // no special handling for title items - sami@tolvanen.com
                    /*if (pItem->iCmd == -4)
                    {
                        CRect rci(0, 0, 0, 0);

                        dc.DrawText(cs, rci, DT_CALCRECT | DT_TOP | DT_VCENTER | DT_SINGLELINE);
                        lpMeasureItemStruct->itemHeight = rci.Height();
                        lpMeasureItemStruct->itemWidth = rci.Width();
                    }
                    else
                    {*/
                        lpMeasureItemStruct->itemHeight = szImage.cy + 5;

                        if (osz.cy > (int) lpMeasureItemStruct->itemHeight)
                            lpMeasureItemStruct->itemHeight = (int) osz.cy;

                        lpMeasureItemStruct->itemWidth = osz.cx + 2 + 15;
                        lpMeasureItemStruct->itemWidth += lpMeasureItemStruct->itemHeight > (UINT) szImage.cx ? (UINT) lpMeasureItemStruct->itemHeight : (UINT) szImage.cx;
                    //}

                    dc.SelectObject(of);
                }
                else
                {
                    lpMeasureItemStruct->itemHeight = szImage.cy + 5;
                    lpMeasureItemStruct->itemWidth  = 100;
                }
            }
        }
    }
}

bool CGfxPopupMenu::CreateBoldFont()
{
    if (hMenuBoldFont)
        ::DeleteObject((HGDIOBJ)hMenuBoldFont);

    LOGFONT lgFont;
    ::GetObject (hMenuFont ? hMenuFont : hGuiFont, sizeof (lgFont), &lgFont);
    lgFont.lfWeight = FW_BOLD;

    hMenuBoldFont = CreateFontIndirect (&lgFont);
    return (hMenuBoldFont != NULL);
}

bool CGfxPopupMenu::AddToolBarResource(unsigned int resId)
{
    // David 08/04/98 - put CMenuSpawn in DLL
    HINSTANCE hInst = AfxFindResourceHandle (MAKEINTRESOURCE(resId), RT_TOOLBAR);

    if (!hInst)
        return false;

    HRSRC hRsrc = ::FindResource(/*AfxGetResourceHandle()*/hInst, MAKEINTRESOURCE(resId), RT_TOOLBAR);
    if (hRsrc == NULL)
        return false;

    HGLOBAL hGlb = ::LoadResource(/*AfxGetResourceHandle()*/hInst, hRsrc);
    if (hGlb == NULL)
        return false;


    ToolBarData* pTBData = (ToolBarData*) ::LockResource(hGlb);
    if (pTBData == NULL)
        return false;

    ASSERT(pTBData->wVersion == 1);

    CBitmap bmp;
    bmp.LoadBitmap(resId);
    int nBmpItems = ilList.Add(&bmp, RGB(192, 192, 192));
    bmp.DeleteObject();

    WORD* pItem = (WORD*)(pTBData + 1);

    for (int i = 0; i < pTBData->wItemCount; i++, pItem++)
    {
        if (*pItem != ID_SEPARATOR)
            AddImageItem(nBmpItems++, (WORD) *pItem);
    }

    // it seem that Windows doesn't free these resource (from Heitor Tome)
    ::UnlockResource(hGlb);
    ::FreeResource(hGlb);

    return true;
}

bool CGfxPopupMenu::LoadToolBarResource(unsigned int resId)
{
    // David 08/04/98 -  put CMenuSpawn in DLL
    HINSTANCE hInst = AfxFindResourceHandle (MAKEINTRESOURCE(resId), RT_TOOLBAR);
    if (!hInst)
        return false;

    HRSRC hRsrc = ::FindResource(hInst, MAKEINTRESOURCE(resId), RT_TOOLBAR);
    if (hRsrc == NULL)
        return false;

    HGLOBAL hGlb = ::LoadResource(hInst, hRsrc);
    if (hGlb == NULL)
        return false;

    ToolBarData* pTBData = (ToolBarData*) ::LockResource(hGlb);
    if (pTBData == NULL)
        return false;

    ASSERT(pTBData->wVersion == 1);

    szImage.cx = (int) pTBData->wWidth;
    szImage.cy = (int) pTBData->wHeight;

    if (ilList.Create(szImage.cx, szImage.cy, ILC_COLOR4 | ILC_MASK, pTBData->wItemCount, 0) == false)
        return false;

    ilList.SetBkColor(cr3dFace);

    CBitmap bmp;
    bmp.LoadBitmap(resId);
    ilList.Add(&bmp, RGB(192, 192, 192));
    bmp.DeleteObject();

    WORD* pItem = (WORD*)(pTBData + 1);
    int nBmpItems = 0;
    for (int i = 0; i < pTBData->wItemCount; i++, pItem++)
    {
        if (*pItem != ID_SEPARATOR)
            AddImageItem(nBmpItems++, (WORD) *pItem);
    }

    // it seem that Windows doesn't free these resource (from Heitor Tome)
    ::UnlockResource(hGlb);
    ::FreeResource(hGlb);

    return true;
}

void CGfxPopupMenu::AddImageItem(const int idx, WORD cmd)
{
    if (iImageItem == 0)
        pImageItem = (ImageItem *) GlobalAlloc(GPTR, sizeof(ImageItem));
    else
        pImageItem = (ImageItem *) GlobalReAlloc((HGLOBAL) pImageItem, sizeof(ImageItem) * (iImageItem + 1), GMEM_MOVEABLE | GMEM_ZEROINIT);

    ASSERT(pImageItem);
    pImageItem[iImageItem].iCmd = (int) cmd;
    pImageItem[iImageItem].iImageIdx = idx;
    iImageItem ++;
}

void CGfxPopupMenu::RemapMenu(CMenu * pMenu)
{
    static int iRecurse = 0;
    iRecurse++;

    ASSERT(pMenu);
    int nItem = pMenu->GetMenuItemCount();

    while ((--nItem) >= 0)
    {
        UINT itemId = pMenu->GetMenuItemID(nItem);

        if (itemId == (UINT) -1)
        {
            CMenu *pops = pMenu->GetSubMenu(nItem);

            if (pops)
                RemapMenu(pops);
            if (iRecurse > 0)
            {
                CString cs;

                pMenu->GetMenuString(nItem, cs, MF_BYPOSITION);

                if (!cs.IsEmpty())
                {
                    SpawnItem * sp = AddSpawnItem(cs, (iRecurse == 1) ? -4 : -2);
                    pMenu->ModifyMenu(nItem, MF_BYPOSITION | MF_OWNERDRAW, (UINT) -1, (LPCTSTR)sp);
                }
            }
        }
        else
        {
            if (itemId != 0)
            {
                UINT oldState = pMenu->GetMenuState(nItem, MF_BYPOSITION);

                if (!(oldState & MF_OWNERDRAW) && !(oldState & MF_BITMAP))
                {
                    ASSERT(oldState != (UINT)-1);

                    CString cs;
                    pMenu->GetMenuString(nItem, cs, MF_BYPOSITION);
                    SpawnItem * sp = AddSpawnItem(cs, itemId);
                    pMenu->ModifyMenu(nItem, MF_BYPOSITION | MF_OWNERDRAW | oldState, (LPARAM)itemId, (LPCTSTR)sp);
                }
            }
            else
            {
                UINT oldState = pMenu->GetMenuState(nItem, MF_BYPOSITION);

                if (!(oldState & MF_OWNERDRAW) && !(oldState & MF_BITMAP))
                {
                    ASSERT(oldState != (UINT)-1);
                    SpawnItem * sp = AddSpawnItem(_T("--"), -3);
                    pMenu->ModifyMenu(nItem, MF_BYPOSITION | MF_OWNERDRAW | oldState, (LPARAM)itemId, (LPCTSTR)sp);
                }
            }
        }
    }

    iRecurse --;
}

CGfxPopupMenu::SpawnItem * CGfxPopupMenu::AddSpawnItem(const TCHAR * txt, const int cmd)
{
    if (iSpawnItem == 0)
        pSpawnItem = (SpawnItem **) GlobalAlloc(GPTR, sizeof(SpawnItem));
    else
        pSpawnItem = (SpawnItem **) GlobalReAlloc((HGLOBAL) pSpawnItem, sizeof(SpawnItem) * (iSpawnItem + 1), GMEM_MOVEABLE | GMEM_ZEROINIT);

    ASSERT(pSpawnItem);

    SpawnItem * p = new SpawnItem;
    ASSERT(p);

    pSpawnItem[iSpawnItem] = p;
    lstrcpy(p->cText, txt);
    p->iCmd = cmd;

    if (cmd >= 0)
        p->iImageIdx = FindImageItem(cmd);
    else p->iImageIdx = cmd;

    iSpawnItem ++;
    return p;
}

int CGfxPopupMenu::FindImageItem(const int cmd)
{
    for (int t = 0; t < iImageItem; t++)
        if (pImageItem[t].iCmd == cmd)
            return pImageItem[t].iImageIdx;

        return -1;
}

void CGfxPopupMenu::EnableMenuItems(CMenu * pMenu, CWnd * pParent)
{
    ASSERT(pMenu);
    ASSERT(pParent);

    int nItem = pMenu->GetMenuItemCount();

    CCmdUI state;
    state.m_pMenu       = pMenu;
    state.m_nIndex      = nItem-1;
    state.m_nIndexMax   = nItem;

    while ((--nItem) >= 0)
    {
        UINT itemId = pMenu->GetMenuItemID(nItem);

        if (itemId == (UINT) -1)
        {
            CMenu *pops = pMenu->GetSubMenu(nItem);
            if (pops)
                EnableMenuItems(pops, pParent);
        }
        else
        {
            if (itemId != 0)
            {
                state.m_nID = itemId;
                pParent->OnCmdMsg(itemId, CN_UPDATE_COMMAND_UI, &state, NULL);
                state.DoUpdate(pParent, true);
            }
        }

        state.m_nIndex = nItem-1;
    }
}

BOOL CGfxPopupMenu::LoadMenu(UINT nIDResource)
{
    // sami@tolvanen.com
    // - creates a popup-menu from a resource information
    // - menu must be destroyed after use by calling DestroyMenu
    // - ignores popup items

    CMenu menuTmp;
    CMenu *pPopup = 0;

    if (!menuTmp.LoadMenu(nIDResource))
        return FALSE;

    pPopup = menuTmp.GetSubMenu(0);

    if (pPopup)
    {
        UINT nCount = pPopup->GetMenuItemCount();

        if (nCount != (UINT) -1)
        {
            CreatePopupMenu();

            CString str;
            UINT    nFlags = 0;
            UINT    nID    = 0;

            for (UINT nItem = 0; nItem < nCount; nItem++)
            {
                nID = pPopup->GetMenuItemID(nItem);

                if (nID != (UINT) -1)
                {
                    if (nID == 0)
                    {
                        // separator

                        AppendMenu(MF_SEPARATOR);
                    }
                    else
                    {
                        // string

                        nFlags = pPopup->GetMenuState(nID, MF_BYCOMMAND);
                        pPopup->GetMenuString(nID, str, MF_BYCOMMAND);

                        AppendMenu(MF_STRING | nFlags, nID, (LPCTSTR)str);
                    }
                }
                else
                {
                    CGfxPopupMenu menuSub;
                    CMenu *pSubMenu = pPopup->GetSubMenu(nItem);

                    if (pSubMenu)
                    {
                        UINT    nSubCount = pSubMenu->GetMenuItemCount();

                        if (nSubCount != (UINT) -1)
                        {
                            menuSub.CreatePopupMenu();

                            for (UINT nSubItem = 0; nSubItem < nSubCount; nSubItem++)
                            {
                                nID = pSubMenu->GetMenuItemID(nSubItem);

                                if (nID != (UINT) -1)
                                {
                                    if (nID == 0)
                                    {
                                        // separator

                                        menuSub.AppendMenu(MF_SEPARATOR);
                                    }
                                    else
                                    {
                                        // string

                                        nFlags = pSubMenu->GetMenuState(nID, MF_BYCOMMAND);
                                        pSubMenu->GetMenuString(nID, str, MF_BYCOMMAND);

                                        menuSub.AppendMenu(MF_STRING | nFlags, nID, (LPCTSTR)str);
                                    }
                                }
                            }
                        }
                    }

                    pPopup->GetMenuString(nItem, str, MF_BYPOSITION);
                    AppendMenu(MF_STRING | MF_POPUP, (UINT)menuSub.GetSafeHmenu(), (LPCTSTR)str);
                }
            }
        }
    }

    menuTmp.DestroyMenu();

    return TRUE;
}

BOOL CGfxPopupMenu::LoadMenu(UINT nIDResource, UINT nToolBar, CWnd *pParent)
{
    if (LoadMenu(nIDResource) && LoadToolBarResource(nToolBar))
    {
        RemapMenu(this);
        EnableMenuItems(this, pParent);

        return TRUE;
    }

    return FALSE;
}