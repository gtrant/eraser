// FitFileNameToScrn.cpp
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
#include "FitFileNameToScrn.h"

inline static BOOL doesItFit(CWnd* pWnd, const CString& strText)
{
    try {
        if (strText.IsEmpty()) {
            return TRUE;
        }

        CRect rectWnd;
        CSize sizeText;
        int iSaved;

        // create a context for the window
        CClientDC dc(pWnd);

        iSaved = dc.SaveDC();

        // select the used font
        dc.SelectObject(pWnd->GetFont());

        // the width required to display the whole string
        sizeText = dc.GetTextExtent(strText);

        dc.RestoreDC(iSaved);

        // the width of the window
        pWnd->GetClientRect(&rectWnd);

        return (rectWnd.Width() >= sizeText.cx);
    } catch (...) {
        ASSERT(FALSE);
    }

    return FALSE;
}

BOOL fitFileNameToScrn(CWnd *pWnd, CString& strText, LPCTSTR szPrefix, LPCTSTR szPostfix)
{
    // must be a valid window
    if (!AfxIsValidAddress(pWnd, sizeof(CWnd)) ||
        !IsWindow(pWnd->GetSafeHwnd())) {
        return FALSE;
    }

    if (!doesItFit(pWnd, szPrefix + strText + szPostfix)) {
        // doesn't fit - must cut something off

        BOOL bCutFileName = FALSE;

        CString strDrive;
        CString strPath;
        CString strFile;
        CString strExt;

        try {
            _tsplitpath((LPCTSTR) strText,
                       strDrive.GetBuffer(_MAX_DRIVE),
                       strPath.GetBuffer(_MAX_DIR),
                       strFile.GetBuffer(_MAX_FNAME),
                       strExt.GetBuffer(_MAX_EXT));
        } catch (CException *e) {
            ASSERT(FALSE);

            e->ReportError(MB_ICONERROR);
            e->Delete();

            strDrive.ReleaseBuffer();
            strPath.ReleaseBuffer();
            strFile.ReleaseBuffer();
            strExt.ReleaseBuffer();

            return FALSE;
        } catch (...) {
            ASSERT(FALSE);
            return FALSE;
        }

        strDrive.ReleaseBuffer();
        strPath.ReleaseBuffer();
        strFile.ReleaseBuffer();
        strExt.ReleaseBuffer();

        if (!strDrive.IsEmpty()) {
            strDrive += TEXT("\\");
        }

        strFile += strExt;
        strExt.Empty();

        if (!strPath.IsEmpty()) {
            // there are directories to remove...

            int iPos;
            BOOL bUseLong = FALSE;
            CString strLeft;

            // remove the first backslash
            strPath = strPath.Right(strPath.GetLength() - 1);

            do {
                iPos = strPath.Find(TEXT('\\'));

                if (iPos != -1) {
                    // remove a folder
                    strPath = strPath.Right(strPath.GetLength() - iPos - 1);

                    if (bUseLong) {
                        strLeft = TEXT("....\\");
                    } else {
                        strLeft = TEXT("...\\");
                        bUseLong = TRUE;
                    }
                } else {
                    // must shorten the file name
                    strPath.Empty();

                    bCutFileName = TRUE;
                    break;
                }
            } while (!doesItFit(pWnd, (szPrefix + strDrive + strLeft + strPath + strFile + szPostfix)));

            strPath = strLeft + strPath;
        } else {
            // must shorten the file name
            bCutFileName = TRUE;
        }

        if (bCutFileName) {
            // must shorten the file name

            // the path is always "...\\" or empty if we need to
            // concentrate to the file name itself

            BOOL bCutFromLeft = FALSE;

            CString strLeft;
            CString strMid = TEXT("..");
            CString strRight;

            DWORD dwMid = strFile.GetLength() / 2;

            strLeft = strFile.Left(dwMid);
            strRight = strFile.Right(strFile.GetLength() - dwMid);

            while (!doesItFit(pWnd, (szPrefix + strDrive + strPath + strLeft + strMid + strRight + szPostfix))) {
                if (strLeft.IsEmpty() && strRight.IsEmpty()) {
                    break;
                }

                if (bCutFromLeft) {
                    if (!strLeft.IsEmpty()) {
                        strLeft = strLeft.Left(strLeft.GetLength() - 1);
                    }
                    bCutFromLeft = FALSE;
                } else {
                    if (!strRight.IsEmpty()) {
                        strRight = strRight.Right(strRight.GetLength() - 1);
                    }
                    bCutFromLeft = TRUE;
                }
            }

            // if there is still something left show it,
            // otherwise we'll just use the long file name
            if (!strLeft.IsEmpty() || !strRight.IsEmpty()) {
                strFile = strLeft + strMid + strRight;
            }
        }

        // C:\....\Too long to..letely.txt
        strText = strDrive + strPath + strFile;
    }

    return TRUE;
}