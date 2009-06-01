// Erasext.cpp
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
#include "Erasext.h"

#include "..\EraserDll\eraserdll.h"
#include "..\EraserUI\VisualStyles.h"
#include "ConfirmDialog.h"
#include "WipeProgDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CErasextApp

BEGIN_MESSAGE_MAP(CErasextApp, CWinApp)
    //{{AFX_MSG_MAP(CErasextApp)
        // NOTE - the ClassWizard will add and remove mapping macros here.
        //    DO NOT EDIT what you see in these blocks of generated code!
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CErasextApp construction

CErasextApp::CErasextApp()
{
    _set_se_translator(SeTranslator);
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CErasextApp object

CErasextApp theApp;

/////////////////////////////////////////////////////////////////////////////
// Special entry points required for inproc servers

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    TRACE("DllGetClassObject\n");
    return AfxDllGetClassObject(rclsid, riid, ppv);
}

STDAPI DllCanUnloadNow(void)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    TRACE("DllCanUnloadNow\n");
    return S_FALSE; //AfxDllCanUnloadNow();
}

// by exporting DllRegisterServer, you can use regsvr.exe
STDAPI DllRegisterServer(void)
{
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
    TRACE("DllRegisterServer\n");
    COleObjectFactory::UpdateRegistryAll();
    return S_OK;
}

BOOL CErasextApp::InitInstance()
{
    TRACE("CErasextApp::InitInstance\n");
    // Register all OLE server (factories) as running.  This enables the
    // OLE libraries to create objects from other applications.
    COleObjectFactory::RegisterAll();

    eraserInit();
    return CWinApp::InitInstance();
}

int CErasextApp::ExitInstance()
{
    eraserEnd();
	return CWinApp::ExitInstance();
}
