// FreeSpace.h
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

#ifndef FREESPACE_H
#define FREESPACE_H

void
countFilesOnDrive(CEraserContext *context, const CString& strDrive, E_UINT32& uFiles, E_UINT32& uFolders);
bool
getClusterSize(LPCTSTR szDrive, E_UINT32& uCluster);
bool
getClusterAndSectorSize(LPCTSTR szDrive, E_UINT32& uCluster, E_UINT32& uSector);

bool
getPartitionType(PARTITIONINFO& pi);
bool
getPartitionInformation(CEraserContext *context, TCHAR cDrive);

bool
wipeMFTRecords(CEraserContext *context);
bool
wipeClusterTips(CEraserContext *context);
bool
wipeFreeSpace(CEraserContext *context);

#endif
