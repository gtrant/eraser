// Eraser.cpp
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2001-2006  Garrett Trant (support@heidi.ie).
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
#include "Eraser.h"

#include "MainFrm.h"
#include "EraserDoc.h"
#include "EraserView.h"
#include "Windows.h"
#include "version.h"

#include "EraserDll\EraserDll.h"
#include "EraserDll\SecurityManager.h"
#include "EraserUI\HyperLink.h"
#include "EraserUI\VisualStyles.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CEraserApp

BEGIN_MESSAGE_MAP(CEraserApp, CWinApp)
    //{{AFX_MSG_MAP(CEraserApp)
    ON_COMMAND(ID_APP_ABOUT, OnAppAbout)
        // NOTE - the ClassWizard will add and remove mapping macros here.
        //    DO NOT EDIT what you see in these blocks of generated code!
    //}}AFX_MSG_MAP
    // Standard file based document commands
    ON_COMMAND(ID_FILE_NEW, CWinApp::OnFileNew)
    ON_COMMAND(ID_FILE_OPEN, CWinApp::OnFileOpen)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CEraserApp construction

CEraserApp::CEraserApp() :
m_pDoc(0)
{
    setlocale(LC_ALL, "");
    _set_se_translator(SeTranslator);
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CEraserApp object

CEraserApp theApp;

/////////////////////////////////////////////////////////////////////////////
// CEraserApp initialization

// Add a static BOOL that indicates whether the class was
// registered so that we can unregister it in ExitInstance
static BOOL bClassRegistered = FALSE;
__declspec(dllimport) bool IsProcessElevated(HANDLE process);

BOOL CEraserApp::FirstInstance()
{
	CWnd *pWndPrev = CWnd::FindWindow(szEraserClassName, NULL);
    CWnd *pWndChild;

    // Determine if another window with our class name exists...
    if (pWndPrev)
    {
		// Determine the elevation status of both processes.
		unsigned elevationStatus = 0;
		{
			DWORD pid = 0;
			GetWindowThreadProcessId(pWndPrev->m_hWnd, &pid);
			HANDLE process = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
			if (IsProcessElevated(process))
				elevationStatus |= 2;
			if (IsProcessElevated(GetCurrentProcess()))
				elevationStatus |= 1;
		}

		if (!(elevationStatus & 2) && (elevationStatus & 1))
		{
			switch (AfxMessageBox(_T("Another instance of Eraser is still running, but this ")
				_T("new instance is running at a higher privilege level than the other.\n\n")
				_T("Do you want to close the previous instance? ")
				_T("All active erasing tasks will be terminated"), MB_YESNO))
			{
			case IDYES:
				pWndPrev->SendMessage(WM_CLOSE);
				break;
			}

			return TRUE;
		}

        // if so, does it have any popups?
        pWndChild = pWndPrev->GetLastActivePopup();

        // If iconic, restore the main window
        if (!pWndPrev->IsWindowVisible() || pWndPrev->IsIconic())
            pWndPrev->ShowWindow(SW_RESTORE);

        // Bring the main window or its popup to
        // the foreground
        pWndChild->SetForegroundWindow();

        // and we are done activating the previous one.
        return FALSE;
    }
    // First instance. Proceed as normal.
    else
        return TRUE;
}

__declspec(dllimport) bool no_registry;

BOOL CEraserApp::InitInstance()
{
    // If a previous instance of the application is already running,
    // then activate it and return FALSE from InitInstance to
    // end the execution of this instance.

#ifndef ERASER_STANDALONE
	{
		TCHAR temp[512];
		::GetModuleFileName(NULL, temp, sizeof(temp));
		::PathStripToRoot(temp);
		switch(::GetDriveType(temp)) {
		case DRIVE_UNKNOWN:
		case DRIVE_NO_ROOT_DIR:
		case DRIVE_FIXED:
		default:
			no_registry = false;
			break;
		case DRIVE_REMOVABLE:
		case DRIVE_REMOTE:
		case DRIVE_CDROM:
		case DRIVE_RAMDISK:
			no_registry = true;
			break;
		}
	}
#else
	no_registry = true;
#endif

	if (no_registry) {
		TCHAR temp[512];
		::GetModuleFileName(NULL, temp, 512);
		::PathRemoveFileSpec(temp);
		::PathAppend(temp, _T("eraser.ini"));
		m_pszProfileName = _tcsdup(temp);
	}

	if (!CheckAccess())
		return false;

    if (!FirstInstance())
        return FALSE;

    eraserInit();

    // Register our unique class name that we wish to use
    WNDCLASS wndcls;
    ZeroMemory(&wndcls, sizeof(WNDCLASS));   // start with NULL

    // defaults
    wndcls.style            = CS_DBLCLKS | CS_HREDRAW | CS_VREDRAW;
    wndcls.lpfnWndProc      = ::DefWindowProc;
    wndcls.hInstance        = AfxGetInstanceHandle();
    wndcls.hIcon            = LoadIcon(IDR_MAINFRAME); // or load a different icon
    wndcls.hCursor          = LoadCursor(IDC_ARROW);
    wndcls.hbrBackground    = (HBRUSH) (COLOR_WINDOW + 1);
    wndcls.lpszMenuName     = NULL;

    // Specify our own class name for using FindWindow later
    wndcls.lpszClassName = szEraserClassName;

    // Register new class and exit if it fails
    if (!AfxRegisterClass(&wndcls))
    {
        AfxMessageBox(_T("Class Registration Failed"));
        return FALSE;
    }

    bClassRegistered = TRUE;

    // Initialize OLE libraries
    if (!AfxOleInit())
    {
        AfxMessageBox(_T("OLE initialization failed."));
        return FALSE;
    }

    // Standard initialization
    // If you are not using these features and wish to reduce the size
    // of your final executable, you should remove from the following
    // the specific initialization routines you do not need.
    // Change the registry key under which our settings are stored.
	if (!no_registry)
	    SetRegistryKey(_T("Heidi Computers Ltd\\Eraser\\5.8"));

    LoadStdProfileSettings(0);  // Load standard INI file options (including MRU)

    // Register the application's document templates.  Document templates
    //  serve as the connection between documents, frame windows and views.
    CSingleDocTemplate* pDocTemplate;
    pDocTemplate = new CSingleDocTemplate(
        IDR_MAINFRAME,
        RUNTIME_CLASS(CEraserDoc),
        RUNTIME_CLASS(CMainFrame),       // main SDI frame window
        NULL);  // we create views by ourselves; no default view here
    AddDocTemplate(pDocTemplate);

#ifndef ERASER_STANDALONE
    // Enable DDE Execute open
    EnableShellOpen();
    RegisterShellFileTypes(TRUE);
#endif

    CString str(m_lpCmdLine);
    BOOL bHide = (str.Find(NOWINDOW_PARAMETER) != -1);

    if (bHide)
        m_nCmdShow = SW_HIDE;

    // Parse command line for standard shell commands, DDE, file open
    CCommandLineInfo cmdInfo;
    ParseCommandLine(cmdInfo);

    // Dispatch commands specified on the command line
    if (!ProcessShellCommand(cmdInfo))
        return FALSE;

    EnableHtmlHelp();
    size_t helpfilelen = _tcslen(m_pszHelpFilePath);
    if((helpfilelen >= 4) && !_tcsicmp(&m_pszHelpFilePath[helpfilelen - 4], _T(".hlp")))
        _tcscpy((TCHAR *)&m_pszHelpFilePath[helpfilelen - 4], _T(".chm"));

    // The one and only window has been initialized, so show and update it.
    m_pMainWnd->ShowWindow((bHide) ? SW_HIDE : SW_SHOW);
    m_pMainWnd->UpdateWindow();

    return TRUE;
}


/////////////////////////////////////////////////////////////////////////////
// CAboutDlg dialog used for App About

class CAboutDlg : public CDialog
{
public:
    CAboutDlg();

// Dialog Data
    //{{AFX_DATA(CAboutDlg)
    enum { IDD = IDD_ABOUTBOX };
    CHyperLink  m_hlMail;
    CHyperLink  m_hlLink;
    CString m_strVersion;
    //}}AFX_DATA

    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CAboutDlg)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    //{{AFX_MSG(CAboutDlg)
    virtual BOOL OnInitDialog();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
    //{{AFX_DATA_INIT(CAboutDlg)
    m_strVersion = _T("Eraser Version 5.8.1");
    //}}AFX_DATA_INIT
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CAboutDlg)
    DDX_Control(pDX, IDC_HYPERLINK_MAIL, m_hlMail);
    DDX_Control(pDX, IDC_HYPERLINK, m_hlLink);
    DDX_Text(pDX, IDC_STATIC_VERSION, m_strVersion);
    //}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
    //{{AFX_MSG_MAP(CAboutDlg)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

// App command to run the dialog
void CEraserApp::OnAppAbout()
{
    CAboutDlg aboutDlg;
	//aboutDlg.m_strVersion.Format("Eraser Version %s",GetVersionInfoFromModule( TRUE )); // Show four-digit version info
	aboutDlg.m_strVersion.Format(_T("Eraser Version %s"), _T(VERSION_NUMBER_STRING));
    aboutDlg.DoModal();
}
LPCTSTR  CEraserApp::GetVersionInfoFromModule( BOOL boolFourDigitString )
{
 // From: Chris Copenhaver <ccopenhaver@documentsolutions.com> 
 
 // This method reads information from the Version resource.

    TCHAR szFullPath[MAX_PATH];
    DWORD dwVerHnd;
    DWORD dwVerInfoSize;

    // Get version information from the application
    ::GetModuleFileName(NULL, szFullPath, sizeof(szFullPath));

    dwVerInfoSize = ::GetFileVersionInfoSize(szFullPath, &dwVerHnd);
    if (dwVerInfoSize)
    {
        char* pVersionInfo = new char[dwVerInfoSize];
        if(pVersionInfo)
        {
            BOOL bRet = ::GetFileVersionInfo((LPTSTR)szFullPath,
                                             (DWORD)dwVerHnd,
                                             (DWORD)dwVerInfoSize,
                                             (LPVOID)pVersionInfo);
            TCHAR* szVer = NULL;
            UINT uVerLength;
            if(bRet)
            {
                bRet = ::VerQueryValue(pVersionInfo,

TEXT("\\StringFileInfo\\040904b0\\FileVersion"),
                                       (LPVOID*)&szVer,
                                       &uVerLength);
                if (bRet)
                {
                    // Now return the file version...
                 CString strVersion = szVer;
                 if ( !boolFourDigitString )
                 {
   int posComma = strVersion.ReverseFind( ',' );
   strVersion = strVersion.Left( posComma );
                 }
                 
                    return strVersion;
                }
            }
            delete pVersionInfo;
        }
    }

 return _T("");
}

/////////////////////////////////////////////////////////////////////////////
// CEraserApp message handlers


int CEraserApp::ExitInstance()
{
    try
    {
        if (AfxIsValidAddress(m_pDoc, sizeof(CEraserDoc)))
            delete m_pDoc;

        m_pDoc = 0;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }

    if (bClassRegistered)
        ::UnregisterClass(szEraserClassName, AfxGetInstanceHandle());

    eraserEnd();

    return CWinApp::ExitInstance();
}

BOOL CAboutDlg::OnInitDialog()
{
    //m_strVersion.Format("Eraser %u.%u  (Build %u)", MAJOR_NUMBER, MINOR_NUMBER, BUILD_NUMBER);
    
    m_hlLink.SetURL(ERASER_URL_HOMEPAGE);
    m_hlMail.SetURL(ERASER_URL_EMAIL);

    CDialog::OnInitDialog();

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}
