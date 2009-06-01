// Eraser.h
// $Id$
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

#if !defined(AFX_ERASER_H__70E9C854_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_ERASER_H__70E9C854_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef __AFXWIN_H__
    #error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"       // main symbols
#include "EraserDoc.h"

const LPCTSTR NOWINDOW_PARAMETER    = "-hide";
const LPCTSTR szEraserClassName     = "Eraser.{73F5BCF6-F36C-11d2-BBF3-00105AAF62C4}";

/////////////////////////////////////////////////////////////////////////////
// CEraserApp:
// See Eraser.cpp for the implementation of this class
//

class CEraserApp : public CWinApp
{
public:
    CEraserApp();
    CEraserDoc *m_pDoc;
// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CEraserApp)
    public:
    virtual BOOL InitInstance();
    virtual int ExitInstance();
    //}}AFX_VIRTUAL

// Implementation
    //{{AFX_MSG(CEraserApp)
    afx_msg void OnAppAbout();
        // NOTE - the ClassWizard will add and remove member functions here.
        //    DO NOT EDIT what you see in these blocks of generated code !
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()

protected:
    BOOL FirstInstance();
	
public:
	LPCTSTR  GetVersionInfoFromModule( BOOL boolFourDigitString );
};


/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_ERASER_H__70E9C854_F0D1_11D2_BBF3_00105AAF62C4__INCLUDED_)
