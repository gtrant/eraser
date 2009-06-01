// key.h
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

#ifndef KEY_H
#define KEY_H

// registry definitions
#include <winreg.h>
#include <regstr.h>

#define MAX_KEY_LENGTH 256

/////////////////////////////////////////////////////////////////////////////
// CKey

class CKey
{
public:
    CKey();
    virtual ~CKey();

    virtual BOOL SetValue(LPCTSTR lpszValue, LPCTSTR lpszValueName = NULL);
    virtual BOOL SetValue(DWORD, LPCTSTR lpszValueName = NULL);
    virtual BOOL SetValue(BOOL, LPCTSTR lpszValueName = NULL);
    virtual BOOL SetValue(LPVOID, LPCTSTR lpszValueName = NULL, DWORD dwSize = 0);

    virtual BOOL GetValue(CString& str, LPCTSTR lpszValueName = NULL, CString strDefault = _T(""));
    virtual BOOL GetValue(DWORD&, LPCTSTR lpszValueName = NULL, DWORD dwDefault = 0);
    virtual BOOL GetValue(BOOL&, LPCTSTR lpszValueName = NULL, BOOL bDefault = FALSE);
    virtual BOOL GetValue(LPVOID, LPCTSTR lpszValueName = NULL);
	
	virtual BOOL GetNextValueName(CString& strValName, DWORD index = 0, LPDWORD valType = NULL);

    virtual BOOL IsEmpty();
    virtual DWORD GetValueSize(LPCTSTR lpszValueName = NULL);

    virtual BOOL DeleteValue(LPCTSTR lpszValueName);
    static BOOL DeleteKey(HKEY, LPCTSTR);

    virtual BOOL Open(HKEY hKey, LPCTSTR lpszKeyName, BOOL bCreate = TRUE);
	virtual HKEY GetHandle();
    virtual void Close();

protected:
    HKEY m_hKey;
};


class CIniKey : public CKey {
public:
	CIniKey();
	virtual ~CIniKey();

	virtual BOOL SetValue(LPCTSTR lpszValue, LPCTSTR lpszValueName = NULL);
	virtual BOOL SetValue(DWORD, LPCTSTR lpszValueName = NULL);
	virtual BOOL SetValue(BOOL, LPCTSTR lpszValueName = NULL);
	virtual BOOL SetValue(LPVOID, LPCTSTR lpszValueName = NULL, DWORD dwSize = 0);

	virtual BOOL GetValue(CString& str, LPCTSTR lpszValueName = NULL, CString strDefault = _T(""));
	virtual BOOL GetValue(DWORD&, LPCTSTR lpszValueName = NULL, DWORD dwDefault = 0);
	virtual BOOL GetValue(BOOL&, LPCTSTR lpszValueName = NULL, BOOL bDefault = FALSE);
	virtual BOOL GetValue(LPVOID, LPCTSTR lpszValueName = NULL);

	virtual BOOL GetNextValueName(CString& strValName, DWORD index = 0, LPDWORD valType = NULL);

	virtual BOOL IsEmpty();
	virtual DWORD GetValueSize(LPCTSTR lpszValueName = NULL);

	virtual BOOL DeleteValue(LPCTSTR lpszValueName);

	virtual BOOL Open(HKEY hKey, LPCTSTR lpszKeyName, BOOL bCreate = TRUE);
	virtual HKEY GetHandle();
	virtual void Close();

protected:
	CString section;
};

/////////////////////////////////////////////////////////////////////////////

#endif
