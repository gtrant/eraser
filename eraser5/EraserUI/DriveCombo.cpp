// DriveCombo.cpp
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

#include "stdafx.h"
#include "DriveCombo.h"

#include <shlobj.h>

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CDriveCombo

CDriveCombo::CDriveCombo()
{
}

CDriveCombo::~CDriveCombo()
{
}


BEGIN_MESSAGE_MAP(CDriveCombo, CComboBox)
    //{{AFX_MSG_MAP(CDriveCombo)
        // NOTE - the ClassWizard will add and remove mapping macros here.
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CDriveCombo message handlers

void CDriveCombo::DrawItem(LPDRAWITEMSTRUCT lpDrawItemStruct)
{
    if (lpDrawItemStruct->itemID != LB_ERR)
    {
        // window's DC
        CDC *pDC = CDC::FromHandle(lpDrawItemStruct->hDC);

        CRect rc(lpDrawItemStruct->rcItem);

        BOOL bSelected  = (lpDrawItemStruct->itemState & ODS_SELECTED);
        BOOL bEnabled   = !(lpDrawItemStruct->itemState & ODS_DISABLED);

        if (lpDrawItemStruct->itemAction & (ODA_DRAWENTIRE | ODA_SELECT | ODA_FOCUS))
        {
            // colors
            COLORREF crBack = GetSysColor((bSelected) ? COLOR_HIGHLIGHT : COLOR_WINDOW);
            COLORREF crText = GetSysColor((bSelected) ? COLOR_HIGHLIGHTTEXT : COLOR_WINDOWTEXT);

            if (!bEnabled)
            {
                crBack = GetSysColor(COLOR_3DFACE);
                crText = GetSysColor(COLOR_GRAYTEXT);
            }

            pDC->SetTextColor(crText);

            // background
            pDC->FillSolidRect(rc, crBack);

            // won't modify the background
            pDC->SetBkMode(TRANSPARENT);

            // drive name & icon
            SHFILEINFO shfi;
            ZeroMemory(&shfi, sizeof(shfi));

            HIMAGELIST hSysImageList = NULL;

#ifdef ALLOW_ALL_DRIVES
            if (m_straDrives[lpDrawItemStruct->itemID].Compare(DRIVE_ALL_LOCAL) == 0)
            {
                hSysImageList = (HIMAGELIST) SHGetFileInfo(TEXT("C:\\"),
                                    0, &shfi, sizeof(shfi),
                                    SHGFI_SMALLICON | SHGFI_SYSICONINDEX | SHGFI_DISPLAYNAME);

                shfi.szDisplayName[0] = 0;
                lstrcpyn(shfi.szDisplayName, szLocalDrives, MAX_PATH);
            }
            else
            {
#endif
                hSysImageList = (HIMAGELIST) SHGetFileInfo(
                                    (LPCTSTR)m_straDrives[lpDrawItemStruct->itemID],
                                    0, &shfi, sizeof(shfi),
                                    SHGFI_SMALLICON | SHGFI_SYSICONINDEX | SHGFI_DISPLAYNAME);
#ifdef ALLOW_ALL_DRIVES
            }
#endif

            int cx = 0, cy = 0;
            int px = rc.TopLeft().x, py = rc.TopLeft().y;

            // icon
            if (hSysImageList)
            {
                ImageList_GetIconSize(hSysImageList, &cx, &cy);

#ifdef DRAW_DISABLED_ICON
                if (!bEnabled)
                {
                    HICON hIcon = ImageList_ExtractIcon(0, hSysImageList, shfi.iIcon);

                    if (hIcon)
                    {
                        pDC->DrawState(CPoint(px + 2, py), CSize(cx, cy), hIcon,
                            DST_ICON | DSS_DISABLED, (CBrush *)NULL);
                        DestroyIcon(hIcon);
                    }
                }
                else
                {
#endif
                    ImageList_Draw(hSysImageList, shfi.iIcon, pDC->GetSafeHdc(),
                                   px+2, py, ILD_TRANSPARENT);
#ifdef DRAW_DISABLED_ICON
                }
#endif
            }

            // name
            pDC->TextOut(px + cx + 4, py + 2, shfi.szDisplayName);

            // does the item have focus?
            if ((lpDrawItemStruct->itemState & ODS_FOCUS) && bEnabled)
                pDC->DrawFocusRect(rc);
        }
    }
}

int CDriveCombo::AddString(LPCTSTR lpszString)
{
    int iReturn = 0;
    CString str(lpszString);

    iReturn = CComboBox::AddString(lpszString);

    if ((iReturn != CB_ERR) && (iReturn != CB_ERRSPACE))
    {
        try
        {
            m_straDrives.Add(str);
        }
        catch (CMemoryException *e)
        {
            ASSERT(FALSE);
            e->ReportError(MB_ICONSTOP);
            e->Delete();

            iReturn = 0;
        }
        catch (...)
        {
            ASSERT(FALSE);
            iReturn = 0;
        }

        SetCurSel(0);
    }

    return iReturn;
}

void CDriveCombo::GetSelectedDrive(CString& str)
{
    if (GetCurSel() <= (m_straDrives.GetSize() - 1))
        str = m_straDrives[GetCurSel()];
    else
        str.Empty();
}

void CDriveCombo::FillDrives()
{
    // bit 0 = drive A:
    // bit 1 = drive B:
    // ...
    DWORD dwDriveMask = GetLogicalDrives();

    if (dwDriveMask)
    {
        CString strDrive;
        TCHAR cRoot;
        UINT uType;

        for (cRoot = TEXT('A'); cRoot <= TEXT('Z'); cRoot++)
        {
            if (dwDriveMask & 1)
            {
                strDrive = cRoot;
                strDrive += TEXT(":\\");

                uType = GetDriveType((LPCTSTR) strDrive);

                if (uType != DRIVE_UNKNOWN &&
                    uType != DRIVE_NO_ROOT_DIR &&
                    uType != DRIVE_CDROM &&
                    uType != DRIVE_REMOTE)
                {
                    AddString(strDrive);
                }
            }

            dwDriveMask >>= 1;
        }

#ifdef ALLOW_ALL_DRIVES
        AddString(DRIVE_ALL_LOCAL);
#endif
    }
}

int CDriveCombo::SelectDrive(LPCTSTR szDrive)
{
    int iCount  = m_straDrives.GetSize();
    int iResult = CB_ERR;

    for (int iDrive = 0; iDrive < iCount; iDrive++)
    {
        if (m_straDrives[iDrive].CompareNoCase(szDrive) == 0)
        {
            SetCurSel(iDrive);
            iResult = iDrive;
            break;
        }
    }

    return iResult;
}

#ifdef ALLOW_ALL_DRIVES
void GetLocalHardDrives(CStringArray& saDrives)
{
    DWORD dwDriveMask = GetLogicalDrives();

    if (dwDriveMask)
    {
        CString strDrive;
        TCHAR cRoot;
        UINT uType;

        saDrives.RemoveAll();

        for (cRoot = 'A'; cRoot <= 'Z'; cRoot++)
        {
            if (dwDriveMask & 1)
            {
                strDrive = cRoot;
                strDrive += ":\\";

                uType = GetDriveType((LPCTSTR) strDrive);

                if (uType != DRIVE_UNKNOWN &&
                    uType != DRIVE_NO_ROOT_DIR &&
                    uType != DRIVE_CDROM &&
                    uType != DRIVE_REMOTE &&
                    uType != DRIVE_REMOVABLE)
                {
                    saDrives.Add(strDrive);
                }
            }

            dwDriveMask >>= 1;
        }
    }
}
#endif
