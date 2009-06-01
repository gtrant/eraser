// UserInfo.cpp
//
// Functions adapted from MS Knowledge Base article Q155698
// and MSDN sample TEXTSID.C.
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
#include "UserInfo.h"
#include <lmcons.h>

PTOKEN_USER
GetCurrentUserToken(E_UINT32& uSize)
{
    HANDLE      hToken;
    PTOKEN_USER pTokenUser = 0;

    if (!OpenThreadToken(GetCurrentThread(), TOKEN_QUERY, TRUE, &hToken)) {
        if (GetLastError() == ERROR_NO_TOKEN) {
            // attempt to open the process token, since no thread token exists
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
                return false;
            }
        } else {
            // error trying to get thread token
            return false;
        }
    }

    GetTokenInformation(hToken, TokenUser, 0, 0, &uSize);

    if (GetLastError() == ERROR_INSUFFICIENT_BUFFER && uSize > 0) {
        pTokenUser = (PTOKEN_USER)malloc(uSize);

        if (pTokenUser) {
            if (!GetTokenInformation(hToken, TokenUser, (LPVOID)pTokenUser, uSize, &uSize)) {
                free(pTokenUser);
                pTokenUser = 0;
            }
        }
    }
    CloseHandle(hToken);
    return pTokenUser;
}

bool
GetUserAndDomainName(CString& strUserName, CString& strDomainName)
{
    bool        bSuccess = false;
    E_UINT32    uSize = 0;
    PTOKEN_USER pTokenUser = 0;

    try {
        pTokenUser = GetCurrentUserToken(uSize);

        if (pTokenUser) {
            SID_NAME_USE snu;
            E_UINT32 uUserName   = UNLEN;
            E_UINT32 uDomainName = DNLEN;

            LPTSTR pszUserName   = strUserName.GetBufferSetLength(UNLEN + 1);
            LPTSTR pszDomainName = strDomainName.GetBufferSetLength(DNLEN + 1);

            bSuccess = LookupAccountSid(NULL, pTokenUser->User.Sid,
                                        pszUserName, &uUserName,
                                        pszDomainName, &uDomainName,
                                        &snu) != 0;

            free(pTokenUser);
            pTokenUser = 0;
        }
    } catch (...) {
        ASSERT(0);
        if (pTokenUser) {
            free(pTokenUser);
        }
        bSuccess = false;
    }

    strUserName.ReleaseBuffer();
    strDomainName.ReleaseBuffer();

    return bSuccess;
}

bool
GetCurrentUserTextualSid(CString& strSID)
{
    bool        bSuccess = false;
    E_UINT32    uSize = 0;
    PTOKEN_USER pTokenUser = 0;

    try {
        pTokenUser = GetCurrentUserToken(uSize);

        if (pTokenUser) {
            bSuccess = GetTextualSid(pTokenUser->User.Sid, strSID);

            free(pTokenUser);
            pTokenUser = 0;
        }
    } catch (...) {
        ASSERT(0);
        if (pTokenUser) {
            free(pTokenUser);
        }
        bSuccess = false;
    }

    return bSuccess;
}

bool
GetTextualSid(PSID pSid, CString& strSID)
{
    PSID_IDENTIFIER_AUTHORITY psia;
    E_UINT32 uSubAuthorities;
    E_UINT32 uCounter;
    E_UINT32 uSidCopy;
    LPTSTR   pszSID = 0;
    bool     bSuccess = false;

    try {
        // test if Sid passed in is valid
        if (!IsValidSid(pSid)) {
            return false;
        }

        // obtain SidIdentifierAuthority
        psia = GetSidIdentifierAuthority(pSid);

        // obtain sidsubauthority count
        uSubAuthorities = *GetSidSubAuthorityCount(pSid);

        // compute approximate buffer length
        // S-SID_REVISION- + identifierauthority- + subauthorities- + NULL
        uSidCopy = (15 + 12 + (12 * uSubAuthorities) + 1) * sizeof(TCHAR);

        pszSID = strSID.GetBufferSetLength(uSidCopy);

        // prepare S-SID_REVISION-
        uSidCopy = wsprintf(pszSID, TEXT("S-%lu-"), SID_REVISION);

        // prepare SidIdentifierAuthority
        if ((psia->Value[0] != 0) || (psia->Value[1] != 0)) {
            uSidCopy += wsprintf(pszSID + uSidCopy,
                                 TEXT("0x%02hx%02hx%02hx%02hx%02hx%02hx"),
                                 (USHORT)psia->Value[0],
                                 (USHORT)psia->Value[1],
                                 (USHORT)psia->Value[2],
                                 (USHORT)psia->Value[3],
                                 (USHORT)psia->Value[4],
                                 (USHORT)psia->Value[5]);
        } else {
            uSidCopy += wsprintf(pszSID + uSidCopy,
                                 TEXT("%lu"),
                                 (ULONG)(psia->Value[5]      )   +
                                 (ULONG)(psia->Value[4] <<  8)   +
                                 (ULONG)(psia->Value[3] << 16)   +
                                 (ULONG)(psia->Value[2] << 24));
        }

        // loop through SidSubAuthorities
        for (uCounter = 0; uCounter < uSubAuthorities; uCounter++) {
            uSidCopy += wsprintf(pszSID + uSidCopy, TEXT("-%lu"),
                                 *GetSidSubAuthority(pSid, uCounter));
        }

        bSuccess = true;

    } catch (...) {
        ASSERT(0);
        bSuccess = false;
    }

    strSID.ReleaseBuffer();
    return bSuccess;
}
