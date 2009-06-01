// DirDialog.cpp
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
#include "dirdialog.h"

#include <shlobj.h>

IMPLEMENT_DYNCREATE(CDirectoryDialog, CObject)

BOOL CDirectoryDialog::DoModal(CString& strBuffer, CString strInfo /*=""*/, int nFolder /*=0*/)
{
    ASSERT(AfxIsValidString(strBuffer));

    // Retrieve the task memory allocator.
    LPMALLOC pIMalloc;

    if (SUCCEEDED(SHGetMalloc(&pIMalloc)))
    {
        BOOL            bResult         = FALSE;
        LPITEMIDLIST    pidlDestination = NULL;
        LPITEMIDLIST    pidlRoot        = NULL;
        HWND            hParent         = AfxGetMainWnd()->GetSafeHwnd();

        TCHAR szTemp[MAX_PATH + 1];
        ZeroMemory(szTemp, MAX_PATH + 1);

        // special startup folder?
        if (nFolder)
            SHGetSpecialFolderLocation(hParent, nFolder, &pidlRoot);

        BROWSEINFO biInfo;
        ZeroMemory(&biInfo, sizeof(biInfo));

        biInfo.hwndOwner        = hParent;
        biInfo.pidlRoot         = pidlRoot;
        biInfo.pszDisplayName   = szTemp;
        biInfo.lpszTitle        = (LPCTSTR) strInfo;
        biInfo.ulFlags          = BIF_DONTGOBELOWDOMAIN |
                                  BIF_RETURNFSANCESTORS |
                                  BIF_RETURNONLYFSDIRS;

        //use the shell's folder browser
        pidlDestination = SHBrowseForFolder(&biInfo);

        //did the user select the cancel button
        bResult = (pidlDestination != NULL);

        if (bResult)
        {
            try
            {
                SHGetPathFromIDList(pidlDestination, strBuffer.GetBuffer(MAX_PATH));
                strBuffer.ReleaseBuffer();
            }
            catch (CException *e)
            {
                ASSERT(FALSE);

                e->ReportError(MB_ICONERROR);
                e->Delete();
                bResult = FALSE;
            }
            catch (...)
            {
                ASSERT(FALSE);
                bResult = FALSE;
            }

            pIMalloc->Free(pidlDestination);
        }

        // Cleanup and release the stuff we used
        pIMalloc->Release();

        return bResult;
    }

    return FALSE;
}
