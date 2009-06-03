// key.cpp
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
#include "key.h"

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CKey

CKey::CKey() :
m_hKey(NULL)
{

}

CKey::~CKey()
{
    Close();
}

void CKey::Close()
{
    if (m_hKey != NULL)
    {
        VERIFY(RegCloseKey(m_hKey) == ERROR_SUCCESS);
        m_hKey = NULL;
    }
}

BOOL CKey::Open(HKEY hKey, LPCTSTR lpszKeyName, BOOL bCreate /*=TRUE*/)
{
    ASSERT(hKey != NULL);
    DWORD dwDisp = 0;
    LONG lReturn = 0;

    if (bCreate)
    {
        lReturn = RegCreateKeyEx(hKey, lpszKeyName, 0,
                                _T(""), REG_OPTION_NON_VOLATILE,
                                KEY_ALL_ACCESS, NULL, &m_hKey, &dwDisp);
    }
    else
    {
        lReturn = RegOpenKeyEx(hKey, lpszKeyName, 0,KEY_ALL_ACCESS,
                               &m_hKey);
    }

    SetLastError(static_cast<DWORD>(lReturn));

    return (lReturn == ERROR_SUCCESS);
}

BOOL CKey::SetValue(LPCTSTR lpszValue, LPCTSTR lpszValueName)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(lpszValue));
    ASSERT(AfxIsValidString(lpszValueName));

    LONG lReturn = RegSetValueEx(m_hKey, lpszValueName, 0, REG_SZ,
                                reinterpret_cast<CONST BYTE*>(lpszValue),
                                (lstrlen(lpszValue) + 1) * sizeof(TCHAR));

    SetLastError(static_cast<DWORD>(lReturn));
    return (lReturn == ERROR_SUCCESS);
}

BOOL CKey::SetValue(BOOL bValue, LPCTSTR lpszValueName)
{
    ASSERT(AfxIsValidString(lpszValueName));

    return (SetValue(static_cast<DWORD>(bValue), lpszValueName));
}

BOOL CKey::SetValue(DWORD dwValue, LPCTSTR lpszValueName)
{
    ASSERT(AfxIsValidString(lpszValueName));
    ASSERT(m_hKey != NULL);

    LONG lReturn = RegSetValueEx(m_hKey, lpszValueName, 0, REG_DWORD,
                                 reinterpret_cast<CONST BYTE*>(&dwValue),
                                 sizeof(dwValue));

    SetLastError(static_cast<DWORD>(lReturn));
    return (lReturn == ERROR_SUCCESS);
}

BOOL CKey::SetValue(LPVOID pValue, LPCTSTR lpszValueName, DWORD dwSize)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(lpszValueName));

    LONG lReturn = RegSetValueEx(m_hKey, lpszValueName, 0, REG_BINARY,
                                 reinterpret_cast<CONST BYTE*>(pValue), dwSize);

    SetLastError(static_cast<DWORD>(lReturn));
    return (lReturn == ERROR_SUCCESS);
}

BOOL CKey::GetValue(CString& str, LPCTSTR lpszValueName, CString strDefault)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(str));
    ASSERT(AfxIsValidString(lpszValueName));

    str.Empty();

    DWORD dw = 0;
    DWORD dwType = 0;
    LONG lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                                NULL, &dw);

    if (lRes == ERROR_SUCCESS)
    {
        ASSERT(dwType == REG_SZ);

        try
        {
            LPTSTR lpsz = str.GetBufferSetLength(dw);
            lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                                   reinterpret_cast<BYTE*>(lpsz), &dw);

            str.ReleaseBuffer();

            SetLastError(static_cast<DWORD>(lRes));
            return (lRes == ERROR_SUCCESS);
        }
        catch (CMemoryException *e)
        {
            ASSERT(FALSE);
            e->ReportError(MB_ICONERROR);
            e->Delete();
        }
        catch (...)
        {
            ASSERT(FALSE);
        }
    }
    else
    {
        SetLastError(static_cast<DWORD>(lRes));
    }

    str = strDefault;
    return FALSE;
}

BOOL CKey::GetValue(BOOL& bValue, LPCTSTR lpszValueName, BOOL bDefault)
{
    ASSERT(AfxIsValidString(lpszValueName));

    BOOL bRes;
    DWORD dwValue = static_cast<DWORD>(bValue);

    bRes = GetValue(dwValue, lpszValueName, static_cast<DWORD>(bDefault));
    bValue = static_cast<BOOL>(dwValue);

    return bRes;
}

BOOL CKey::GetValue(DWORD& dwValue, LPCTSTR lpszValueName, DWORD dwDefault)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(lpszValueName));

    DWORD dw = 0;
    DWORD dwType = 0;
    LONG lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                                NULL, &dw);

    if (lRes == ERROR_SUCCESS &&
        dwType == REG_DWORD)
    {
        lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                               reinterpret_cast<BYTE*>(&dwValue), &dw);

        SetLastError(static_cast<DWORD>(lRes));
        return (lRes == ERROR_SUCCESS);
    }
    else
    {
        dwValue = dwDefault;

        SetLastError(static_cast<DWORD>(lRes));
        return FALSE;
    }
}

BOOL CKey::GetNextValueName(CString& strValName, DWORD index /*=0*/, LPDWORD valType /*=NULL*/)
{
     if (m_hKey == NULL) return FALSE;
     DWORD dwNameLen = MAX_KEY_LENGTH;
     _TCHAR lpszValName[MAX_KEY_LENGTH];     
     
     try{
          LONG lRes = RegEnumValue(m_hKey, index, (LPTSTR)lpszValName, &dwNameLen, NULL, valType, NULL, NULL);
          
          if (lRes == ERROR_SUCCESS) {
               strValName = lpszValName;
               return TRUE;
          }
     }
     catch (...) {
          ASSERT(FALSE);
     }
    return FALSE;
}


BOOL CKey::GetValue(LPVOID pValue, LPCTSTR lpszValueName)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(lpszValueName));

    DWORD dw = 0;
    DWORD dwType = 0;
    LONG lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                                NULL, &dw);

    if (lRes == ERROR_SUCCESS &&
        dwType == REG_BINARY)
    {
        lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                               static_cast<BYTE*>(pValue), &dw);

        SetLastError(static_cast<DWORD>(lRes));
        return (lRes == ERROR_SUCCESS);
    }
    else
    {
        SetLastError(static_cast<DWORD>(lRes));
        return FALSE;
    }
}

DWORD CKey::GetValueSize(LPCTSTR lpszValueName)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(lpszValueName));

    DWORD dw = 0;
    DWORD dwType = 0;

    LONG lRes = RegQueryValueEx(m_hKey, const_cast<LPTSTR>(lpszValueName), NULL, &dwType,
                                NULL, &dw);

    SetLastError(static_cast<DWORD>(lRes));

    if (lRes != ERROR_SUCCESS) dw = 0;

    return dw;
}


BOOL CKey::DeleteValue(LPCTSTR lpszValueName)
{
    ASSERT(m_hKey != NULL);
    ASSERT(AfxIsValidString(lpszValueName));

    LONG lReturn = RegDeleteValue(m_hKey, lpszValueName);
    SetLastError(static_cast<DWORD>(lReturn));

    return (lReturn == ERROR_SUCCESS);
}

// delete recursively for NT4
BOOL CKey::DeleteKey(HKEY hStartKey, LPCTSTR pKeyName)
{
    ASSERT(AfxIsValidString(pKeyName));

    DWORD dwRtn, dwSubKeyLength;
    TCHAR szSubKey[MAX_KEY_LENGTH]; // (256) this should be dynamic.
    HKEY hKey;

    // do not allow NULL or empty key name
    if (pKeyName && lstrlen(pKeyName))
    {
        // are we allowed to delete and enumerate keys?
        if((dwRtn = RegOpenKeyEx(hStartKey,pKeyName, 0,
                                 KEY_ENUMERATE_SUB_KEYS | DELETE, &hKey )) == ERROR_SUCCESS)
        {
            while (dwRtn == ERROR_SUCCESS )
            {
                dwSubKeyLength = MAX_KEY_LENGTH;

                dwRtn=RegEnumKeyEx(hKey,
                    0,       // always index zero
                    szSubKey,
                    &dwSubKeyLength,
                    NULL,
                    NULL,
                    NULL,
                    NULL);

                if (dwRtn == ERROR_SUCCESS)
                {
                    DeleteKey(hKey, szSubKey);
                    continue;
                }
                else if (dwRtn == ERROR_NO_MORE_ITEMS)
                    break;
            }

            // finally, delete the main key
            dwRtn = RegDeleteKey(hStartKey, pKeyName);

            RegCloseKey(hKey);

            // Do not save return code because error
            // has already occurred
        }
    }
    // empty key name
    else
        dwRtn = ERROR_BADKEY;


    SetLastError(dwRtn);
    return (dwRtn == ERROR_SUCCESS);
}

BOOL CKey::IsEmpty()
{
    if (m_hKey == NULL)
        return FALSE;

    TCHAR szSubKey[MAX_KEY_LENGTH]; // (256) this should be dynamic.
    DWORD dwRtn, dwSubKeyLength = MAX_KEY_LENGTH;

    dwRtn = RegEnumKeyEx(m_hKey, 0, szSubKey, &dwSubKeyLength, NULL, NULL, NULL, NULL);

    SetLastError(dwRtn);
    return (dwRtn == ERROR_NO_MORE_ITEMS);
}
HKEY CKey::GetHandle()
{
	return m_hKey; 
}



#include <assert.h>

CIniKey::CIniKey() {
}
CIniKey::~CIniKey() {
}

BOOL CIniKey::Open(HKEY /*hKey*/, LPCTSTR lpszKeyName, BOOL /*bCreate*/) {
	section = lpszKeyName;
	section.Replace('/', '\\');
	int i = section.ReverseFind('\\');
	if(i != -1)
		section = section.Mid(i + 1);
	if(!section.GetLength())
		section = "default";
	return true;
}
void CIniKey::Close() {
}
HKEY CIniKey::GetHandle() {
	assert(("CIniKey::GetHandle() not implemented!", false));
	return NULL;
}

BOOL CIniKey::SetValue(LPCTSTR lpszValue, LPCTSTR lpszValueName) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	AfxGetApp()->WriteProfileString(section, lpszValueName, lpszValue);
	::WritePrivateProfileString(NULL, NULL, NULL, AfxGetApp()->m_pszProfileName);
	return true;
}
BOOL CIniKey::SetValue(DWORD lpszValue, LPCTSTR lpszValueName) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	AfxGetApp()->WriteProfileInt(section, lpszValueName, lpszValue);
	::WritePrivateProfileString(NULL, NULL, NULL, AfxGetApp()->m_pszProfileName);
	return true;
}
BOOL CIniKey::SetValue(BOOL lpszValue, LPCTSTR lpszValueName) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	AfxGetApp()->WriteProfileString(section, lpszValueName, lpszValue ? _T("yes") : _T("no"));
	::WritePrivateProfileString(NULL, NULL, NULL, AfxGetApp()->m_pszProfileName);
	return true;
}
BOOL CIniKey::SetValue(LPVOID lpszValue, LPCTSTR lpszValueName, DWORD dwSize) {
	if(!lpszValue)
		return false;
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	AfxGetApp()->WriteProfileBinary(section, lpszValueName, (LPBYTE)lpszValue, dwSize);
	::WritePrivateProfileString(NULL, NULL, NULL, AfxGetApp()->m_pszProfileName);
	return true;
}

BOOL CIniKey::GetValue(CString &str, LPCTSTR lpszValueName, CString strDefault) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	str = AfxGetApp()->GetProfileString(section, lpszValueName, strDefault);
	return true;
}
BOOL CIniKey::GetValue(DWORD &value, LPCTSTR lpszValueName, DWORD dwDefault) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	value = AfxGetApp()->GetProfileInt(section, lpszValueName, dwDefault);
	return true;
}
BOOL CIniKey::GetValue(BOOL &value, LPCTSTR lpszValueName, BOOL bDefault) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	CString str = AfxGetApp()->GetProfileString(section, lpszValueName, bDefault ? _T("yes") : _T("no"));
	value = !_tcsicmp(str, _T("yes"));
	return true;
}
BOOL CIniKey::GetValue(LPVOID data, LPCTSTR lpszValueName) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	LPBYTE data2 = NULL;
	UINT bytes = 0;
	BOOL ret = AfxGetApp()->GetProfileBinary(section, lpszValueName, &data2, &bytes);
	if(ret && data) {
		memcpy(data, data2, bytes);
		delete[] data2;
	}
	return ret;
}

BOOL CIniKey::DeleteValue(LPCTSTR /*lpszValueName*/) {
	return false;
}

BOOL CIniKey::IsEmpty() {
	assert(("CIniKey::IsEmpty() not implemented!", false));
	return false;
}

DWORD CIniKey::GetValueSize(LPCTSTR lpszValueName) {
	if(!lpszValueName || !lpszValueName[0])
		lpszValueName = _T("__default");
	LPBYTE data = NULL;
	UINT bytes = 0;
	BOOL ret = AfxGetApp()->GetProfileBinary(section, lpszValueName, &data, &bytes);
	if(ret && data)
		delete[] data;
	return bytes;
}

BOOL CIniKey::GetNextValueName(CString& strValName, DWORD /*index*/, LPDWORD /*valType*/) {
	assert(("CIniKey::GetNextValueName() not implemented!", false));
	strValName = "";
	return false;
}
