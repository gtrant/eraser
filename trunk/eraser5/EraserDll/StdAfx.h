// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
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

#undef NOCRYPT
#pragma once

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN	// Exclude rarely-used items from Windows headers.
#endif

// Modify the following defines if you have to target an OS before the ones 
// specified in the following code. See MSDN for the latest information
// about corresponding values for different operating systems.
#ifndef WINVER
#define WINVER 0x0501  //Windows XP and later
#endif

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0501	 // Windows XP and later
#endif

#ifndef _WIN32_WINDOWS
#define _WIN32_WINDOWS 0x0501    // Windows XP and later
#endif

#ifndef _WIN32_IE
#define _WIN32_IE 0x0600   // IE6+
#endif

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS  // Some CString constructors will be explicit.

// Turns off MFC feature that hides of some common warning messages
// that are frequently and safely ignored .
#define _AFX_ALL_WARNINGS
#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions

#define _AFX_NO_OLE_SUPPORT

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxole.h>         // MFC OLE classes
#include <afxodlgs.h>       // MFC OLE dialog classes
#include <afxdisp.h>        // MFC OLE automation classes
#endif // _AFX_NO_OLE_SUPPORT


#define _AFX_NO_DB_SUPPORT

#ifndef _AFX_NO_DB_SUPPORT
#include <afxdb.h>          // MFC ODBC database classes
#endif // _AFX_NO_DB_SUPPORT

#define _AFX_NO_DAO_SUPPORT

#ifndef _AFX_NO_DAO_SUPPORT
#include <afxdao.h>         // MFC DAO database classes
#endif // _AFX_NO_DAO_SUPPORT

#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>         // MFC support for Windows 95 Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT

#include <afxmt.h>
#include <afxtempl.h>
#include <afxcoll.h>
#include <eh.h>                       // structured exception
#include "..\shared\SeException.h"    // handling
#include <Shlwapi.h>
#include <vector>

#ifndef _AFXDLL
#define AfxLoadLibrary  ::LoadLibrary
#define AfxFreeLibrary  ::FreeLibrary
#endif

// use intrinsic memory functions
#pragma intrinsic(memset, memcpy)