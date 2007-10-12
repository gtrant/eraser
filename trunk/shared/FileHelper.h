// FileHelper.h
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

#ifndef __FILEHELPER_H__
#define __FILEHELPER_H__

BOOL
parseDirectory(LPCTSTR szDirectory, CStringArray& strFiles, CStringArray& strDirectories,
               BOOL bSubDirectories, LPDWORD dwFiles = 0, LPDWORD dwDirectories = 0);

void
findMatchingFiles(CString strSearch, CStringArray& strFiles, BOOL bSubFolders = FALSE);

#endif