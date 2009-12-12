// FileHelper.cpp
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
#include "..\EraserDll\EraserDll.h"
#include "FileHelper.h"
const LPCTSTR pprefix  = _T("\\\\?");

void
findMatchingFiles2(CString strSearch, CStringArray& saFiles, BOOL bSubFolders /*=FALSE*/)
{
    HANDLE          hFind, hFolder;
    WIN32_FIND_DATA wfdData, wfdFolder;

    CString         strFolder;
    CString         strBaseFolder;
    TCHAR           szDrive[_MAX_DRIVE];
    TCHAR           szFolder[_MAX_PATH];

    // get the root folder
    _tsplitpath((LPCTSTR)strSearch, szDrive, szFolder, NULL, NULL);

    strBaseFolder = szDrive;
    strBaseFolder += szFolder;

    if (strBaseFolder.IsEmpty()) {
        if (GetCurrentDirectory(_MAX_PATH, szFolder) != 0) {
            strBaseFolder = szFolder;
            if (strBaseFolder[strBaseFolder.GetLength() - 1] != '\\') {
                strBaseFolder += "\\";
            }
            strSearch = strBaseFolder + strSearch;
        }
    }
	//strBaseFolder = pprefix  + strBaseFolder;
    // the search pattern
    //if (strSearch.Find('\\') > 0) {
        
	if (strSearch.ReverseFind('\\') > 0) {
		//strSearch = strSearch.Left( strSearch.ReverseFind('\\')+1 ); -> this will return the path
		strSearch = strSearch.Right(strSearch.GetLength() - 1 -
                                    strSearch.ReverseFind('\\'));
    }

    // browse through all files (and directories)
    hFolder = FindFirstFile((LPCTSTR)(strBaseFolder + _T("*")), &wfdFolder);

    if (hFolder != INVALID_HANDLE_VALUE) {
        hFind = FindFirstFile((LPCTSTR)(strBaseFolder + strSearch), &wfdData);

        // process the current folder first
        if (hFind != INVALID_HANDLE_VALUE) {
            do {
                if (!bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY)) {
                    saFiles.Add(strBaseFolder + wfdData.cFileName);
                }
            } while (FindNextFile(hFind, &wfdData));

            VERIFY(FindClose(hFind));
        }

        // go through the subfolders then
        if (bSubFolders) {
            while (FindNextFile(hFolder, &wfdFolder)) {
                if (bitSet(wfdFolder.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY) &&
                    IS_SUBFOLDER(wfdFolder.cFileName)) {
                    // found one
                    strFolder = strBaseFolder + wfdFolder.cFileName;
                    strFolder += "\\";
                    // recursive
                    findMatchingFiles(strFolder + strSearch, saFiles, bSubFolders);
                }
            }
        }
        VERIFY(FindClose(hFolder));
    }
}

void
findMatchingFiles(CString strSearch, CStringArray& saFiles, BOOL bSubFolders /*=FALSE*/) {
	strSearch.Replace('/', '\\');
	int i = strSearch.FindOneOf(_T("*?"));
	int j = -1;
	if (i != -1) {
		j = strSearch.Find('\\', i);
	}
	if((i == -1) || (j == -1)) {
		// fallback if wildcard not found or if wildcard isn't in the middle of path
		findMatchingFiles2(strSearch, saFiles, bSubFolders);
		return;
	}
	int k = strSearch.Left(i).ReverseFind('\\');
	if(k == -1)
		k = 0;

	WIN32_FIND_DATA wfdData;
	HANDLE hFolder = FindFirstFile(strSearch.Left(j), &wfdData);
	if (hFolder != INVALID_HANDLE_VALUE) {
		do {
			if (!bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY))
				continue;
			if(!_tcscmp(wfdData.cFileName, _T(".")) || !_tcscmp(wfdData.cFileName, _T("..")))
				continue;
			CString newStrSearch = strSearch.Left(k) + _T("\\") + wfdData.cFileName + strSearch.Mid(j);
			findMatchingFiles(newStrSearch, saFiles, bSubFolders);
		} while(FindNextFile(hFolder, &wfdData));
		VERIFY(FindClose(hFolder));
	}
}

BOOL
parseDirectory(LPCTSTR szDirectory, CStringArray& saFiles, CStringArray& saDirectories,
               BOOL bSubDirectories, LPDWORD pdwFiles /*=0*/, LPDWORD pdwDirectories /*=0*/)
{
    HANDLE          hFind;
    WIN32_FIND_DATA wfdData;
    CString         strDirectory(szDirectory);

    if (!strDirectory.IsEmpty()) {
        if (pdwDirectories) {
            try {
                (*pdwDirectories)++;
            } catch (...) {
                ASSERT(0);
            }
        }

        if (strDirectory[strDirectory.GetLength() - 1] != '\\') {
            strDirectory += "\\";
        }

		TCHAR szFullPath[_MAX_PATH];
		GetFullPathName(strDirectory, sizeof(szFullPath) / sizeof(szFullPath[0]), szFullPath, NULL);
		strDirectory = szFullPath;

        // add current folder to the beginning of the list
        // --> subfolders will be removed first
        saDirectories.InsertAt(0, strDirectory);

        hFind = FindFirstFile((LPCTSTR)(strDirectory + _T("*")), &wfdData);

        if (hFind != INVALID_HANDLE_VALUE) {
            do {
                if (bitSet(wfdData.dwFileAttributes, FILE_ATTRIBUTE_DIRECTORY)) {
                    // skip "." and ".."
                    if (!bSubDirectories || ISNT_SUBFOLDER(wfdData.cFileName)) {
                        continue;
                    }

                    // recursive
                    parseDirectory((LPCTSTR)(strDirectory + wfdData.cFileName),
                                   saFiles,
                                   saDirectories,
                                   bSubDirectories,
                                   pdwFiles,
                                   pdwDirectories);
                } else {
                    saFiles.Add(strDirectory + wfdData.cFileName);
                    if (pdwFiles) {
                        try {
                            (*pdwFiles)++;
                        } catch (...) {
                            ASSERT(0);
                        }
                    }
                }
            } while (FindNextFile(hFind, &wfdData));

            VERIFY(FindClose(hFind));

            return TRUE;
        }
    }

    return FALSE;
}

CString findPattern(LPCTSTR szPath,CString& strBefore, CString& strAfter)
{
	CString strPath = szPath;
	CString strPattern = _T("");
	int iGearPos = 0;
	int iWhatPos = 0;
	int iStart = -1;
	strBefore=_T("");
	strAfter=_T("");
	if (strPath.IsEmpty()) return strPattern; 
	iGearPos=strPath.Find('*');
	iWhatPos=strPath.Find('?');
	iStart = iGearPos>-1?iGearPos:iStart;
	iStart = iWhatPos>-1?iWhatPos:iStart;
	if (iGearPos>-1&&iWhatPos>-1) iStart = iGearPos<iWhatPos?iGearPos:iWhatPos;
	if (iStart == -1) {
		strBefore = strPath;
		return _T("*");
	}
	iStart = (strPath.Left(iStart)).ReverseFind('\\');
	strBefore = strPath.Left(iStart) + _T("\\");
	strPattern=strPath.Right(strPath.GetLength() - iStart-1);
	if ((iStart = strPattern.Find('\\')) != -1)
	{
		strAfter = _T("\\") + strPattern.Right(strPattern.GetLength()-iStart-1);
		//if (strAfter.IsEmpty()) strAfter = "\\";
		strPattern = strPattern.Left(iStart);
	}
	return strPattern;
}


bool PatternMatch(const _TCHAR* s, const _TCHAR* mask)
{

	const _TCHAR* cp=0;
	const _TCHAR* mp=0;
	for (; *s && *mask!='*'; mask++,s++) if (*mask!=*s&&*mask!='?') return 0;
	for (;;) {
		if (!*s) { while (*mask=='*') mask++; return !*mask; }
		if (*mask=='*') { if (!*++mask) return 1; mp=mask; cp=s+1; continue; }
		if (*mask==*s||*mask=='?') { mask++, s++; continue; }
		mask=mp; s=cp++;
	}
}