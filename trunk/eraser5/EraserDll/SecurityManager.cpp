// SecurityManager.cpp
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
#include "stdafx.h"
#include "SecurityManager.h"
#include "SecManDlg.h"

#include <vector>
#include <atlbase.h>
#include <atlsecurity.h>
#define REG_PROTECT_KEY _T("Software\\Heidi Computers Ltd\\Eraser\\PROTECT")
#define REG_PRODUCT_KEY _T("Software\\Heidi Computers Ltd\\Eraser")

#define PROTECTION _T("protection")
#define SECRET _T("sec")

#pragma comment( lib, "Crypt32" )

enum
{
	PROTECTION_ON = 1, PROTECTION_OFF = 0
};

__declspec(dllexport) bool no_registry;

CSecurityManager::CSecurityManager(void)
{
}

CSecurityManager::~CSecurityManager(void)
{
}
static void init_sa(CSecurityAttributes& sa)
{
	TCHAR tcUser[1024];
	DWORD dwSize = sizeof (tcUser);
	if (FALSE == GetUserName(tcUser, &dwSize))
	{
		throw std::runtime_error("User detection error");
	}

	CDacl ac;	
	ac.AddAllowedAce(CSid(tcUser), MAXIMUM_ALLOWED|GENERIC_ALL);
	ac.AddAllowedAce(Sids::Users(), GENERIC_READ);
	ac.AddAllowedAce(Sids::World(), GENERIC_READ);
	ac.AddAllowedAce(Sids::Admins(), GENERIC_ALL);


	CSecurityDesc sd;
	sd.SetDacl(ac);	
	sa.Set(sd);

}
void 
CSecurityManager::Protect(const TCHAR* szSecret )
{
	extern bool no_registry;
	if (no_registry)
		return;

	CRegKey protect;		
	SetLastError(0);
	DWORD dwErrorCode = protect.Create(HKEY_LOCAL_MACHINE, REG_PRODUCT_KEY);
	if (dwErrorCode != ERROR_SUCCESS)
	{
		SetLastError(dwErrorCode);
		throw std::runtime_error("Unable to create key");
	}

	CSecurityAttributes sa;
	init_sa(sa);

	dwErrorCode = protect.Create(HKEY_LOCAL_MACHINE, 
		REG_PROTECT_KEY, REG_NONE, REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, &sa);

	if (ERROR_SUCCESS != dwErrorCode)
	{
		SetLastError(dwErrorCode);
		if (ERROR_ACCESS_DENIED == dwErrorCode)
			throw std::runtime_error("Access denied");			
		throw std::runtime_error("Unable to create key");
	}

	DATA_BLOB data_in;
	data_in.cbData = static_cast<DWORD>((_tcslen(szSecret) + 1) * sizeof(TCHAR));
	data_in.pbData = reinterpret_cast<BYTE*>(const_cast<LPTSTR>(szSecret));
	CRYPTPROTECT_PROMPTSTRUCT promt;
	ZeroMemory(&promt, sizeof(promt));
	promt.cbSize = sizeof(promt);
	promt.dwPromptFlags = 0;
	DATA_BLOB data_out;
	DATA_BLOB data_entropy = data_in;


	if(!CryptProtectData(
		&data_in,
		L"Eraser's protection",	// A description string. 
		&data_entropy,			// Optional entropy
		NULL,					// Reserved.
		&promt,					// Pass a PromptStruct.
		0,
		&data_out))
	{
		throw std::runtime_error("Encryption error");
	}

	dwErrorCode = protect.SetBinaryValue(SECRET, data_out.pbData, data_out.cbData);

	LocalFree(data_out.pbData);

	if (ERROR_SUCCESS != dwErrorCode )
	{

		throw std::runtime_error("Unable set protection off");
	}

	protect.Close();

}

void 
CSecurityManager::Unprotect()
{
	extern bool no_registry;
	if (no_registry)
		return;

	CRegKey protect;

	DWORD dwErrorCode = protect.Open(HKEY_LOCAL_MACHINE, REG_PRODUCT_KEY);
	if (ERROR_SUCCESS != dwErrorCode )
	{
		if (ERROR_ACCESS_DENIED == dwErrorCode)
			throw std::runtime_error("Access denied");	

		return;
	}

	dwErrorCode = protect.DeleteSubKey(_T("PROTECT"));
	/*
	if (ERROR_SUCCESS != dwErrorCode )	
	throw std::runtime_error("Unable to unprotect");	
	*/
}

bool 
CSecurityManager::Check(const TCHAR* szSecret )
{
	extern bool no_registry;
	if (no_registry)
		return true;

	CRegKey protect;

	DWORD dwErrorCode = protect.Open(HKEY_LOCAL_MACHINE, REG_PROTECT_KEY, KEY_READ);
	if (ERROR_SUCCESS != dwErrorCode )
	{
		if (ERROR_ACCESS_DENIED == dwErrorCode )
			throw std::runtime_error("Access denied");
		return true;
	}	

	ULONG blob_len;
	if (ERROR_SUCCESS != protect.QueryBinaryValue(SECRET, NULL, &blob_len))
		throw std::runtime_error("No data");

	std::vector<unsigned char> blob;
	blob.resize(blob_len);	

	if (ERROR_SUCCESS != protect.QueryBinaryValue(SECRET, &blob[0], &blob_len))
		throw std::runtime_error("No data");

	LPWSTR pDescrOut = NULL;

	DATA_BLOB data_verify;
	DATA_BLOB data_enc;
	DATA_BLOB data_entropy;
	data_entropy.cbData = static_cast<DWORD>((_tcslen(szSecret) + 1) * sizeof(TCHAR));
	data_entropy.pbData = reinterpret_cast<BYTE*>(const_cast<LPTSTR>(szSecret));
	data_enc.cbData = blob_len;
	data_enc.pbData = reinterpret_cast<BYTE*>(&blob[0]);

	CRYPTPROTECT_PROMPTSTRUCT promt;
	ZeroMemory(&promt, sizeof(promt));
	promt.cbSize = sizeof(promt);
	promt.dwPromptFlags = 0;

	if (!CryptUnprotectData(
		&data_enc,
		&pDescrOut,
		&data_entropy,                 // Optional entropy
		NULL,                 // Reserved
		&promt,        // Optional PromptStruct
		0,
		&data_verify))
	{

		return false;
	}
	bool ret = (0 == memcmp(data_verify.pbData, szSecret, data_verify.cbData));
	LocalFree(pDescrOut);
	LocalFree(data_verify.pbData);
	return ret;
}
bool
CSecurityManager::IsProtected()
{
	extern bool no_registry;
	if (no_registry)
		return false;

	CRegKey protect;

	DWORD dwErrorCode = protect.Open(HKEY_LOCAL_MACHINE, REG_PROTECT_KEY, KEY_READ);
	if (ERROR_SUCCESS != dwErrorCode )			
		return ERROR_ACCESS_DENIED == dwErrorCode;		


	ULONG blob_len(1);
	dwErrorCode  = protect.QueryBinaryValue(SECRET, NULL, &blob_len);
	if (ERROR_ACCESS_DENIED == dwErrorCode  )
		return true;

	return ERROR_SUCCESS == dwErrorCode;

}

bool CheckAccess(DWORD dwMaxError /*= 3*/)
{

	bool is_ok(false);
	try
	{
		if (!CSecurityManager::IsProtected())	
			return true;
		AFX_MANAGE_STATE(AfxGetStaticModuleState( ))

	
		CSecManDlg dlg;
		dlg.SetMode(CSecManDlg::CHECKUP);
		for (DWORD  i = 0; i <dwMaxError;)
		{
			dlg.Clear();
			if (IDOK  != dlg.DoModal())
				return false;
			
			is_ok = CSecurityManager::Check(dlg.GetSecret());

			if (is_ok)							
				return true;
			++i;						
			AfxGetApp()->GetMainWnd()->MessageBox(_T("Password incorrect"), _T("Security error"), MB_OK|MB_ICONERROR);
		}
	}
	catch (const CAtlException& )
	{
		is_ok = false;
	}
	catch (CException* ee)
	{
		ee->ReportError();
		ee->Delete();
		is_ok = false;
	}
	catch (const std::exception& e)
	{
		CString str;
		ansiToCString(e.what(), str);
		AfxGetApp()->GetMainWnd()->MessageBox(str, _T("Security error"), MB_OK|MB_ICONERROR);
		is_ok = false;
	}
	
	return is_ok;
}

bool SetProtection()
{
	try
	{	
		if (!CheckAccess(1))
			return false;

		AFX_MANAGE_STATE(AfxGetStaticModuleState())

		CSecManDlg dlg;
		dlg.SetMode(CSecManDlg::SETUP);
		if (IDOK != dlg.DoModal())
			return false;

		CSecurityManager::Protect(dlg.GetSecret());
		return true;
	}
	catch (const CAtlException&)
	{
	}
	catch (CException* ee)
	{
		ee->ReportError();
		ee->Delete();
	}
	catch (const std::exception& e)
	{
		CString message(e.what());
		switch (GetLastError())
		{
		case ERROR_ACCESS_DENIED:
			{
				message = _T("Setting password protection requires your user account to be an Administrator");
				OSVERSIONINFO info;
				info.dwOSVersionInfoSize = sizeof(info);
				if (GetVersionEx(&info) && info.dwMajorVersion >= 6)
					message += _T(" and for Eraser to be elevated");
				message += _T(".");
			}
		}

		AfxGetApp()->GetMainWnd()->MessageBox(message, _T("Security error"), MB_OK | MB_ICONERROR);
	}

	return false;
}

bool ClearProtection()
{
	bool is_ok(false);
	try
	{
		if (!CheckAccess(1))
			return false;

		CSecurityManager::Unprotect();
		is_ok = true;
	}
	catch (const CAtlException& )
	{
		is_ok = false;
	}
	catch (CException* ee)
	{
		ee->ReportError();
		ee->Delete();
		is_ok = false;
	}
	catch (const std::exception& e)
	{
		CString str;
		ansiToCString(e.what(), str);
		AfxGetApp()->GetMainWnd()->MessageBox(str, _T("Security error"), MB_OK|MB_ICONERROR);
		is_ok = false;
	}
	return is_ok;
}