// FileLockResolver.h
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2001-2006  Garrett Trant (support@heidi.ie).
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
#pragma once
#include "EraserDll.h"
#include "EraserDoc.h"
class ERASER_API CFileLockResolver
{
public:
	CFileLockResolver(BOOL = FALSE);
	CFileLockResolver(ERASER_HANDLE, BOOL);
	~CFileLockResolver(void);
	void Close();
	inline void AskUser(BOOL val)
	{
		m_bAskUser = val;
	}
	void SetHandle(ERASER_HANDLE);
	static void Resolve(LPCTSTR szFileName, CStringArray&);
	static void Resolve(LPCTSTR szFileName);
private:
	BOOL m_bAskUser;	
	CString m_strLockFileList;
	ERASER_HANDLE m_hHandle;
	CEraserDoc m_Doc;
private:
	void HandleError(LPCTSTR szFileName, DWORD dwErrorCode, int method, unsigned int passes);
	static DWORD ErrorHandler(LPCTSTR szFileName, DWORD dwErrorCode, void* ctx, void* param);
};
