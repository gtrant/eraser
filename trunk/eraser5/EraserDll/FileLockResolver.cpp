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
#define RUNONCE  _T("Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
#define LAUNCHER _T("Eraserl.exe")

CFileLockResolver::CFileLockResolver(BOOL askUser)
: m_bAskUser(askUser), m_hHandle(ERASER_INVALID_CONTEXT),
  m_iMethod(0), m_defaultAction(-1), m_iPasses(0)
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
	TCHAR fullname[MAX_PATH];
	GetModuleFileName(AfxGetInstanceHandle(), fullname, sizeof(fullname));

	// Then separate the path into its constituent parts
	TCHAR filename[MAX_PATH];
	TCHAR extension[MAX_PATH];
	TCHAR pathname[MAX_PATH];
	TCHAR drive[10];
	_tsplitpath(fullname, drive, pathname, filename, extension); 

	// Then generate the path which we want
	CString result;
	if (path_only)
		result.Format(_T("%s%s"), drive, pathname);
	else
		result.Format(_T("%s%s%d.%s"), drive, pathname, time(0), LOCKED_FILE_LIST_NAME);
	return result;
}

typedef std::basic_string<_TCHAR, std::char_traits<_TCHAR>, std::allocator<_TCHAR> > tstring;
typedef std::basic_istream<_TCHAR, std::char_traits<_TCHAR> > tistream;
typedef std::basic_ostream<_TCHAR, std::char_traits<_TCHAR> > tostream;
typedef std::basic_ifstream<_TCHAR, std::char_traits<_TCHAR> > tifstream;
typedef std::basic_ofstream<_TCHAR, std::char_traits<_TCHAR> > tofstream;
struct FileData
{
	tstring name;
	int method;
	unsigned int passes;

	FileData()
	{
	}
	FileData(const tstring& fname, int m, unsigned int pass)
		:name(fname), method(m), passes(pass)
	{
	}

	void read(tistream& is)
	{
		std::getline(is, name);
		is >> method >> passes;
	}

	void write(tostream& os) const
	{
		os << std::noskipws;
		os << name << std::endl;
		os << method << std::endl << passes;
	}
};

tostream& operator<< (tostream& os, const FileData& data)
{
	data.write(os);
	return os;
}

tistream& operator>> (tistream& is, FileData& data)
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
		int eraseOnRestart = !m_bAskUser || m_defaultAction == 1;
		if (m_bAskUser && m_defaultAction == -1)
		{
			int dlgCode = AfxMessageBox(CString(_T("The file ")) + szFileName +
				_T("\nis locked by another process. Do you want to Erase the file ")
				_T("after you restart your computer?"), MB_YESNO | MB_ICONQUESTION);

			eraseOnRestart = dlgCode == IDYES;
			if (AfxMessageBox(_T("Remember this decision for the rest of this erase?"),
				MB_YESNO | MB_ICONQUESTION) == IDYES)
			{
				m_defaultAction = eraseOnRestart;
			}
		}

		if (eraseOnRestart)
		{
			if (m_strLockFileList.IsEmpty())
				m_strLockFileList = GetLockFilePath();
			tofstream os(m_strLockFileList, std::ios_base::out | std::ios_base::app);		
			os << FileData(szFileName, method, passes);

			ASSERT(m_iMethod == 0 || m_iMethod == method);
			ASSERT(m_iPasses == 0 || m_iPasses == passes);
			m_iMethod = method;
			m_iPasses = passes;
		}
	}
}

void CFileLockResolver::Resolve(LPCTSTR szFileName, CStringArray& ar)
{
	tifstream is(szFileName);
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
		method = _T("Gutmann");
		break;
	case DOD_METHOD_ID:
		method = _T("DoD");
		break;
	case DOD_E_METHOD_ID:
		method = _T("DoD_E");
		break;
	case RANDOM_METHOD_ID:
		method.Format(_T("Random %d"), m_iPasses);
		break;
	case FL2KB_METHOD_ID:
		method = _T("First_Last2k");
		break;
	case SCHNEIER_METHOD_ID:
		method = _T("Schneier");
		break;
	}

	CString cmdLine(CString(_T("\"")) + LAUNCHER + _T("\" ") + szResolveLock + _T(" \"") +
		m_strLockFileList + _T("\" -method ") + method);

	extern bool no_registry;
	if (!no_registry)
	{
		CRegKey key;
		if (ERROR_SUCCESS == key.Open(HKEY_LOCAL_MACHINE, RUNONCE))
		{
			// Find an unused eraser launcher ID
			int i = 0;
			ULONG bufSiz = 0;
			const TCHAR* KeyName = _T("EraserRestartErase (%i)");
			TCHAR KeyNameBuf[64];
			do {
				_stprintf(KeyNameBuf, KeyName, ++i);
			}
			while (key.QueryStringValue(KeyNameBuf, NULL, &bufSiz) == ERROR_SUCCESS);

			// Then save to registry
			key.SetStringValue(KeyNameBuf, cmdLine);
			m_strLockFileList = _T("");
			m_iMethod = 0;
			m_iPasses = 0;
		}
	}
}