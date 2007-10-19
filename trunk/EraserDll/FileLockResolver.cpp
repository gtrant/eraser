// FileLockResolver.cpp
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2001-2006  Garrett Trant (support@heidi.ie).
// Copyright © 2007 The Eraser Project.
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
#include "FileLockResolver.h"
#include "..\Launcher\Launcher.h"
#include <fstream>
#include <string>
#include <iterator>
#include <atlbase.h>

#define LOCKED_FILE_LIST_NAME _T("lock")
#define RUNONCE  "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce"
#define LAUNCHER "Eraserl.exe"

CFileLockResolver::CFileLockResolver(BOOL askUser)
: m_bAskUser(askUser), m_hHandle(ERASER_INVALID_CONTEXT),
  m_iMethod(0), m_iPasses(0)
{

}

CFileLockResolver::CFileLockResolver(ERASER_HANDLE h, BOOL askUser)
: m_bAskUser(askUser), m_iMethod(0), m_iPasses(0)
{
	SetHandle(h);
}

CFileLockResolver::~CFileLockResolver(void)
{
	Close();
}

void CFileLockResolver::SetHandle(ERASER_HANDLE h)
{
	m_hHandle = h;
	eraserSetErrorHandler(h, ErrorHandler, this);
}

CString CFileLockResolver::GetLockFilePath(bool path_only)
{
	// Retrieve the path to the current binary
	char fullname[MAX_PATH];
	GetModuleFileName(AfxGetInstanceHandle(), fullname, sizeof(fullname));

	// Then separate the path into its constituent parts
	char filename[MAX_PATH];
	char extension[MAX_PATH];
	char pathname[MAX_PATH];
	char drive[10];
	_splitpath(fullname, drive, pathname, filename, extension); 

	// Then generate the path which we want
	CString result;
	if (path_only)
		result.Format("%s%s", drive, pathname);
	else
		result.Format("%s%s%d.%s", drive, pathname, time(0), LOCKED_FILE_LIST_NAME);
	return result;
}

struct FileData
{
	std::string name;
	int method;
	unsigned int passes;

	FileData()
	{
	}

	FileData(const std::string& fname, int m, unsigned int pass)
		: name(fname), method(m), passes(pass)
	{
	}

	void read(std::istream& is)
	{
		std::getline(is, name);
		is >> method >> passes;
	}

	void write(std::ostream& os) const
	{
		os << std::noskipws;
		os << name << std::endl << method << std::endl << passes;
	}
};

std::ostream& operator<< (std::ostream& os, const FileData& data)
{
	data.write(os);
	return os;
}

std::istream& operator>> (std::istream& is, FileData& data)
{
	data.read(is);
	return is;
}

void CFileLockResolver::HandleError(LPCTSTR szFileName, DWORD dwErrorCode, int method, unsigned int passes)
{
	if (ERROR_LOCK_VIOLATION == dwErrorCode 
		|| ERROR_DRIVE_LOCKED == dwErrorCode
		|| ERROR_LOCKED == dwErrorCode
		|| ERROR_SHARING_VIOLATION == dwErrorCode)
	{
		if (TRUE == m_bAskUser)
		{
			if (IDYES == AfxMessageBox(CString("The file ") +
				szFileName + "\nis locked by another process. Do you want to Erase the file after " +
				"you restart your computer?", MB_YESNO | MB_ICONQUESTION))
			{
				if (m_strLockFileList.IsEmpty())
					m_strLockFileList = GetLockFilePath();
				std::ofstream os(m_strLockFileList, std::ios_base::out | std::ios_base::app);		
				os << FileData(szFileName, method, passes);

				ASSERT(m_iMethod == 0 || m_iMethod == method);
				ASSERT(m_iPasses == 0 || m_iPasses == passes);
				m_iMethod = method;
				m_iPasses = passes;
			}
		}
	}
}

void CFileLockResolver::Resolve(LPCTSTR szFileName, CStringArray& ar)
{
	std::ifstream is(szFileName);
	if (is.fail())
		throw std::runtime_error("Unable to resolve locked files. Can't open file list");

	while (!is.eof()) 
	{
		FileData data;
		is >> data;
		if (!data.name.empty())
			ar.Add(data.name.c_str());
	}
	is.close();
	DeleteFile(szFileName);
}

DWORD CFileLockResolver::ErrorHandler(LPCTSTR szFileName, DWORD dwErrorCode, void* ctx, void* param)
{
	CFileLockResolver* self(static_cast<CFileLockResolver*>(param));
	CEraserContext* ectx(static_cast<CEraserContext* >(ctx));
	self->HandleError(szFileName, dwErrorCode, ectx->m_lpmMethod->m_nMethodID, ectx->m_lpmMethod->m_nPasses);
	return 0UL;
}

void CFileLockResolver::Close()
{
	eraserSetErrorHandler(m_hHandle, NULL, NULL);
	if (m_strLockFileList.IsEmpty())
		return;

	//Using the method and the passes, generate a command line that will do the same thing as it does now.
	CString method;
	switch (m_iMethod)
	{
	case GUTMANN_METHOD_ID:
		method = "Gutmann";
		break;
	case DOD_METHOD_ID:
		method = "DoD";
		break;
	case DOD_E_METHOD_ID:
		method = "DoD_E";
		break;
	case RANDOM_METHOD_ID:
		method.Format("Random %d", m_iPasses);
		break;
	case FL2KB_METHOD_ID:
		method = "First_Last2k";
		break;
	case SCHNEIER_METHOD_ID:
		method = "Schneier";
		break;
	}

	CString cmdLine(CString("\"") + LAUNCHER + "\" " + szResolveLock + " \"" +
		m_strLockFileList + "\" -method " + method);

	extern bool no_registry;
	if (!no_registry)
	{
		CRegKey key;
		if (ERROR_SUCCESS == key.Open(HKEY_LOCAL_MACHINE, RUNONCE))
		{
			// Find an unused eraser launcher ID
			int i = 0;
			ULONG bufSiz = 0;
			const char* KeyName = "EraserRestartErase (%i)";
			char KeyNameBuf[64];
			do
				sprintf(KeyNameBuf, KeyName, ++i);
			while (key.QueryStringValue(KeyNameBuf, NULL, &bufSiz) == ERROR_SUCCESS);

			// Then save to registry
			key.SetStringValue(KeyNameBuf, cmdLine);
			m_strLockFileList = "";
			m_iMethod = 0;
			m_iPasses = 0;
		}
	}
}
