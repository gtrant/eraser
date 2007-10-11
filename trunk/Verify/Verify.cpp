// Verify.cpp
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
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

#include "stdafx.h"
#include "Verify.h"
#include "VerifyDlg.h"
#include "..\EraserUI\VisualStyles.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CVerifyApp

BEGIN_MESSAGE_MAP(CVerifyApp, CWinApp)
	//{{AFX_MSG_MAP(CVerifyApp)
		// NOTE - the ClassWizard will add and remove mapping macros here.
		//    DO NOT EDIT what you see in these blocks of generated code!
	//}}AFX_MSG
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CVerifyApp construction

CVerifyApp::CVerifyApp()
{
	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CVerifyApp object

CVerifyApp theApp;

/////////////////////////////////////////////////////////////////////////////
// CVerifyApp initialization

BOOL CVerifyApp::InitInstance()
{
	// Standard initialization
	// If you are not using these features and wish to reduce the size
	//  of your final executable, you should remove from the following
	//  the specific initialization routines you do not need.
    AfxInitRichEdit();

    // initialize Eraser library
    eraserInit();

    CVerifyDlg dlg;
	m_pMainWnd = &dlg;

    try {
	    dlg.DoModal();
    } catch (CException *e) {
        e->ReportError(MB_ICONERROR);
        e->Delete();
    }

	// Since the dialog has been closed, return FALSE so that we exit the
	//  application, rather than start the application's message pump.
	return FALSE;
}

int CVerifyApp::ExitInstance() 
{
    // free allocated resources
	eraserEnd();
	return CWinApp::ExitInstance();
}
