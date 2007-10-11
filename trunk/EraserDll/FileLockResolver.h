// FileLockResolver.h
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2002-2006  Garrett Trant (gtrant@heidi.ie).
// Copyright © 2007 The Eraser Project
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

class ERASER_API CFileLockResolver
{
public:
	
	CFileLockResolver(BOOL = FALSE);
	~CFileLockResolver(void);
	void Close();
private:
	
	CFileLockResolver(ERASER_HANDLE, BOOL);
	inline void AskUser(BOOL val)
	{
		m_bAskUser = val;
	}
public:
	void SetHandle(ERASER_HANDLE);
	static void Resolve(LPCTSTR szFileName, CStringArray&);
private:
	BOOL m_bAskUser;	
	CString m_strLockFileList;
	ERASER_HANDLE m_hHandle;
private:
	void HandleError(LPCTSTR szFileName, DWORD dwErrorCode, int method, unsigned int passes);
	static DWORD ErrorHandler(LPCTSTR szFileName, DWORD dwErrorCode, void* ctx, void* param);
};

