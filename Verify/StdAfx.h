// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
//
#if !defined(AFX_STDAFX_H__23AAFC39_A230_4F47_9D92_63F8181F183B__INCLUDED_)
#define AFX_STDAFX_H__23AAFC39_A230_4F47_9D92_63F8181F183B__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN	// Exclude rarely-used items from Windows headers.
#endif

// Modify the following defines if you have to target an OS before the ones
// specified in the following code. See MSDN for the latest information
// about corresponding values for different operating systems.
#ifndef WINVER		// Permit use of features specific to Windows 95 and Windows NT 4.0 or later.
#define WINVER 0x0400	// Change this to the appropriate value to target
#endif                     // Windows 98 and Windows 2000 or later.

#ifndef _WIN32_WINNT	// Permit use of features specific to Windows NT 4.0 or later.
#define _WIN32_WINNT 0x0400	// Change this to the appropriate value to target
#endif		         // Windows 98 and Windows 2000 or later.

#ifndef _WIN32_WINDOWS	      // Permit use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0410    // Change this to the appropriate value to target
#endif			      // Windows Millennium Edition or later.

#ifndef _WIN32_IE		// Permit use of features specific to Internet Explorer 4.0 or later.
#define _WIN32_IE 0x0400   // Change this to the appropriate value to target
#endif			// Internet Explorer 5.0 or later.

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS  // Some CString constructors will be explicit.

// Turns off MFC feature that hides of some common warning messages
// that are frequently and safely ignored .
#define _AFX_ALL_WARNINGS
#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions
//#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>			// MFC support for Windows Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT
#include <afxdisp.h>
#include <afxtempl.h>
#include <afxmt.h>
#include <shlobj.h>
#include <lm.h>
#include <eh.h>             // structured exception
#include "..\shared\SeException.h"    // handling
#include "..\shared\FileDialogEx.h"

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_STDAFX_H__23AAFC39_A230_4F47_9D92_63F8181F183B__INCLUDED_)
