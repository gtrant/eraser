// Launcher.cpp
// $Id$
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2001-2006  Garrett Trant (support@heidi.ie).
// Copyright © 2007 The Eraser Project.
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
#include "..\EraserDll\FileLockResolver.h"
#include "..\EraserUI\DriveCombo.h"
#include "..\EraserUI\VisualStyles.h"
#include "..\shared\FileHelper.h"
#include "..\shared\UserInfo.h"
#include "..\shared\Key.h"

#include "Launcher.h"
#include "ConfirmDialog.h"
#include "LauncherDlg.h"

#include <exception>
#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CLauncherApp

BEGIN_MESSAGE_MAP(CLauncherApp, CWinApp)
    //{{AFX_MSG_MAP(CLauncherApp)
        // NOTE - the ClassWizard will add and remove mapping macros here.
        //    DO NOT EDIT what you see in these blocks of generated code!
    //}}AFX_MSG
    ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CLauncherApp construction

CLauncherApp::CLauncherApp() :
m_pdlgEraser(0),
m_hQueue(NULL)
{
    _set_se_translator(SeTranslator);
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CLauncherApp object

CLauncherApp theApp;

static BOOL ParseRecycledDirectory(LPCTSTR szDirectory, CStringArray& saFiles,
                                   CStringArray& saDirectories, BOOL bNoINFO)
{
    static int      iRecursion = 0;

    BOOL            bResult = FALSE;
    HANDLE          hFind;
    WIN32_FIND_DATA wfdData;

    iRecursion++;

    CString         strTemp;
    CString         strDirectory(szDirectory);

    // do not include the base directory
    if (iRecursion > 1)
        saDirectories.InsertAt(0, strDirectory);

    if (!strDirectory.IsEmpty())
    {
        if (strDirectory[strDirectory.GetLength() - 1] != '\\')
            strDirectory += "\\";

        strTemp = strDirectory + "*";

        hFind = FindFirstFile((LPCTSTR) strTemp, &wfdData);

        if (hFind != INVALID_HANDLE_VALUE)
        {
            do
            {
                if (wfdData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
                {
                    // skip "." and ".."
                    if (ISNT_SUBFOLDER(wfdData.cFileName))
                        continue;

                    strTemp = strDirectory + wfdData.cFileName;

                    // recursive
                    ParseRecycledDirectory((LPCTSTR) strTemp,
                                           saFiles, saDirectories,
                                           bNoINFO);
                }
                else
                {
                    if (_stricmp(wfdData.cFileName, "desktop.ini") == 0)
                        continue;

                    if (bNoINFO && _strnicmp(wfdData.cFileName, "INFO", 4) == 0)
                        continue;

                    strTemp = strDirectory + wfdData.cFileName;
                    saFiles.Add(strTemp);
                }
            }
            while (FindNextFile(hFind, &wfdData));

            VERIFY(FindClose(hFind));

            bResult = TRUE;
        }
    }

    iRecursion--;
    return bResult;
}

struct VersionHelper
{
	bool isVista;
	VersionHelper()
	{
		isVista = false;

		OSVERSIONINFOEX osvi;
		BOOL bOsVersionInfoEx;

		ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));

		// Try calling GetVersionEx using the OSVERSIONINFOEX structure.
		// If that fails, try using the OSVERSIONINFO structure.

		osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

		if ( (bOsVersionInfoEx = GetVersionEx((OSVERSIONINFO *) &osvi)) != TRUE )
		{
			osvi.dwOSVersionInfoSize = sizeof (OSVERSIONINFO);
			if ( !GetVersionEx ((OSVERSIONINFO *) &osvi) ) 
				return ;
		}
		if ( VER_PLATFORM_WIN32_NT == osvi.dwPlatformId )
		{
			if ( osvi.dwMajorVersion == 6 && osvi.dwMinorVersion == 0 )
			{

				/*if( osvi.wProductType == VER_NT_WORKSTATION )
				printf ("Windows Vista ");
				else printf ("Windows Server \"Longhorn\" " );
			
			*/
				isVista = true;
			}
		}
	}
};
static void LocateRecycledItems(CStringArray& saRecycled, CStringArray& saRecycledDirectories)
{
    CStringArray    straDrives;
    CStringArray    lstraDrives;
    TCHAR           szFS[MAX_PATH];
    DWORD           dwFileSystem;
    DWORD           dwAttributes;
    int             iSize;
    CString         strSID;
	static VersionHelper version;

    saRecycled.RemoveAll();
    saRecycledDirectories.RemoveAll();

    // find all hard drives installed to the system

    GetLocalHardDrives(lstraDrives);

    // determine user SID (works on NT only)

    GetCurrentUserTextualSid(strSID);

    // determine the location of the Recycle Bin (depending on the file system)

    iSize = lstraDrives.GetSize();


    while (iSize--)
    {
        if (GetVolumeInformation((LPCTSTR)lstraDrives[iSize], NULL, 0, NULL, NULL,
            &dwFileSystem, szFS, MAX_PATH))
        {
            if (_stricmp(szFS, "NTFS") == 0)
            {
				straDrives.Add(lstraDrives[iSize]+ "RECYCLER\\NPROTECT");
                straDrives.Add(lstraDrives[iSize] + "$Recycle.Bin\\");
                straDrives.Add(lstraDrives[iSize] + "$Recycle.Bin\\NPROTECT");
                if (!strSID.IsEmpty())
				{
                    //straDrives.SetAt(iSize, straDrives[iSize] + "RECYCLER\\" + strSID);					
                    straDrives.Add(lstraDrives[iSize] + "RECYCLER\\" + strSID);		
                    straDrives.Add(lstraDrives[iSize] + "$Recycle.Bin\\" + strSID);
				}
                else
				{
                    //straDrives.SetAt(iSize, straDrives[iSize] + "RECYCLER");
                    straDrives.Add(lstraDrives[iSize] + "RECYCLER");
                    straDrives.Add(lstraDrives[iSize] + "$Recycle.Bin\\");
				}
            }
            else
                //straDrives.SetAt(iSize, straDrives[iSize] + "RECYCLED");
                straDrives.Add(lstraDrives[iSize] + "RECYCLED");
            
        }
    }

	
    // parse contents of the Recycle Bin folders except for the "desktop.ini"
    // files

    CString strTemp;
    BOOL    bNoINFO = FALSE;
    iSize = straDrives.GetSize();

    HINSTANCE         hShell = AfxLoadLibrary(szShell32);
    SHEMPTYRECYCLEBIN pSHEmptyRecycleBin = 0;

    if (hShell)
    {
        pSHEmptyRecycleBin =
            (SHEMPTYRECYCLEBIN)GetProcAddress(hShell, szSHEmptyRecycleBin);

        bNoINFO = (pSHEmptyRecycleBin != NULL);

        AfxFreeLibrary(hShell);
    }

    while (iSize--)
    {
        strTemp = straDrives[iSize];
        dwAttributes = GetFileAttributes((LPCTSTR)straDrives[iSize]);

        if (dwAttributes != (DWORD) -1 && dwAttributes & FILE_ATTRIBUTE_SYSTEM)
        {
            strTemp = straDrives[iSize];

            ParseRecycledDirectory(strTemp,
               saRecycled,
               saRecycledDirectories,
               bNoINFO);
        }
    }

#if 0
    CString strRecycled, strRecycledDirectories;
    int i = 0;

    for (i = 0, iSize = saRecycled.GetSize(); i < iSize; i++) {
        strRecycled += saRecycled[i] + "\r\n";
    }

    for (i = 0, iSize = saRecycledDirectories.GetSize(); i < iSize; i++) {
        strRecycledDirectories += saRecycledDirectories[i] + "\r\n";
    }

    AfxMessageBox(strSID);
    AfxMessageBox(strRecycled);
    AfxMessageBox(strRecycledDirectories);
#endif
}


/////////////////////////////////////////////////////////////////////////////
// CLauncherApp initialization

BOOL CLauncherApp::InitInstance()
{
	// Standard initialization
	// If you are not using these features and wish to reduce the size
	//  of your final executable, you should remove from the following
	//  the specific initialization routines you do not need.
	eraserInit();

	CString strCmdLine(m_lpCmdLine);
	CString strCurrentParameter;

	BOOL    bIncorrectParameter = FALSE;
	BOOL    bSilent             = FALSE;
	BOOL    bResults            = -1;
	BOOL    bResultsOnError     = -1;
	BOOL    bOptions            = FALSE;
	BOOL    bQueue              = FALSE;

	CString strData;
	CStringArray saFiles;
	BOOL    bFiles              = FALSE;
	BOOL    bFolders            = FALSE;
	BOOL    bSubFolders         = FALSE;
	BOOL    bKeepFolder         = FALSE;
	BOOL    bDrive              = FALSE;
	BOOL    bRecycled           = FALSE;
	BOOL	bResolveLock		= FALSE;

	ERASER_METHOD emMethod      = ERASER_METHOD_PSEUDORANDOM /*ERASER_METHOD_LIBRARY*/;
	E_UINT16 uPasses            = 1;

	if (!strCmdLine.IsEmpty())
	{
		while (GetNextParameter(strCmdLine, strCurrentParameter))
		{
			if (strCurrentParameter.CompareNoCase(szFile) == 0 &&
				strData.IsEmpty())
			{
				// file

				if (!GetNextParameter(strCmdLine, strCurrentParameter))
					bIncorrectParameter = TRUE;
				else
				{
					strData = strCurrentParameter;
					bFiles = TRUE;
				}
			}
			else if (strCurrentParameter.CompareNoCase(szResolveLock) == 0 &&
				strData.IsEmpty())
			{
				if (!GetNextParameter(strCmdLine, strCurrentParameter))
					bIncorrectParameter = TRUE;
				else
				{
					strData = strCurrentParameter;
					bResolveLock = TRUE;
				}
			}
			else if (strCurrentParameter.CompareNoCase(szFolder) == 0 &&
					 strData.IsEmpty())
			{
				// folder

				if (!GetNextParameter(strCmdLine, strCurrentParameter))
					bIncorrectParameter = TRUE;
				else
				{
					strData = strCurrentParameter;
					bFiles = TRUE;
					bFolders = TRUE;

					if (strData[strData.GetLength() - 1] != '\\')
						strData += "\\";
				}
			}
			else if (strCurrentParameter.CompareNoCase(szDisk) == 0 &&
					 strData.IsEmpty())
			{
				// unused disk space

				if (!GetNextParameter(strCmdLine, strCurrentParameter))
					bIncorrectParameter = TRUE;
				else
				{
					bDrive = TRUE;

					if (strCurrentParameter != szDiskAll)
						strData.Format("%c:\\", strCurrentParameter[0]);
					else
						strData = strCurrentParameter;
				}
			}
			else if (strCurrentParameter.CompareNoCase(szRecycled) == 0)
			{
				bRecycled   = TRUE;
				bFiles      = TRUE;
				bFolders    = FALSE;
			}
			else if (strCurrentParameter.CompareNoCase(szMethod) == 0)
			{
				if (!GetNextParameter(strCmdLine, strCurrentParameter))
					bIncorrectParameter = TRUE;
				else
				{
					if (strCurrentParameter.CompareNoCase(szMethodLibrary) == 0)
						emMethod = ERASER_METHOD_LIBRARY;
					else if (strCurrentParameter.CompareNoCase(szMethodGutmann) == 0)
						emMethod = ERASER_METHOD_GUTMANN;
					else if (strCurrentParameter.CompareNoCase(szMethodDoD) == 0)
						emMethod = ERASER_METHOD_DOD;
					else if (strCurrentParameter.CompareNoCase(szMethodDoD_E) == 0)
						emMethod = ERASER_METHOD_DOD_E;
					else if (strCurrentParameter.CompareNoCase(szMethodFL2K) == 0)
						emMethod = ERASER_METHOD_FIRST_LAST_2KB;
					else if (strCurrentParameter.CompareNoCase(szSchneier) == 0)
						emMethod = ERASER_METHOD_SCHNEIER;
					else if (strCurrentParameter.CompareNoCase(szMethodRandom) == 0)
					{
						emMethod = ERASER_METHOD_PSEUDORANDOM;

						if (!GetNextParameter(strCmdLine, strCurrentParameter))
							bIncorrectParameter = TRUE;
						else
						{
							char *sztmp = 0;
							E_UINT32 uCurrentParameter = strtoul((LPCTSTR)strCurrentParameter, &sztmp, 10);

							if (*sztmp != '\0' || uCurrentParameter > (E_UINT16)-1) {
								bIncorrectParameter = TRUE;
							} else {
								uPasses = (E_UINT16)uCurrentParameter;
							}
						}
					}
					else
						bIncorrectParameter = TRUE;
				}
			}
			else if (strCurrentParameter.CompareNoCase(szSubFolders) == 0)
				bSubFolders = TRUE;
			else if (strCurrentParameter.CompareNoCase(szKeepFolder) == 0)
				bKeepFolder = TRUE;
			else if (strCurrentParameter.CompareNoCase(szSilent) == 0)
				bSilent = TRUE;
			else if (strCurrentParameter.CompareNoCase(szResults) == 0)
				bResults = TRUE;
			else if (strCurrentParameter.CompareNoCase(szResultsOnError) == 0)
			{
				bResults = TRUE;
				bResultsOnError = TRUE;
			}
			else if (strCurrentParameter.CompareNoCase(szOptions) == 0)
				bOptions = TRUE;
			else if (strCurrentParameter.CompareNoCase(szQueue) == 0)
				bQueue = TRUE;
			else
				bIncorrectParameter = TRUE;
		}
	}
	else
	{
		bIncorrectParameter = TRUE;
	}

	// conflicting command line parameters ?
	if (((!bOptions && !bRecycled) && strData.IsEmpty()) || // no data!
		(!bFolders && bKeepFolder) ||                       // data not a folder
		(bSilent && bResults) ||                            // no windows
		(bOptions && bQueue) ||                             // why queue the options?
		bIncorrectParameter)
	{
		AfxMessageBox(IDS_CMDLINE_INCORRECT, MB_ICONERROR, 0);
		return FALSE;
	}

	// is the user naive enough to select the first/last 2KB pass with free space?
	if (emMethod == ERASER_METHOD_FIRST_LAST_2KB && bDrive)
	{
		AfxMessageBox("The first/last 2KB erase cannot be used with Free Space erases.", MB_ICONERROR);
		return FALSE;
	}

	//Now that the command line has been passed, check if we should display the
	//results dialog (because it may not be overridde by the user)
	CKey kReg;
	if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
	{
		if (bResults == -1)
			kReg.GetValue(bResults, ERASER_REGISTRY_RESULTS_FILES, TRUE);
		if (bResultsOnError == -1)
			kReg.GetValue(bResultsOnError, ERASER_REGISTRY_RESULTS_WHENFAILED, FALSE);
		kReg.Close();
	}

	try
	{
		m_pdlgEraser = new CLauncherDlg();
		m_pMainWnd   = m_pdlgEraser;

		if (!m_pdlgEraser->Create(IDD_LAUNCHER_DIALOG))
		{
			AfxMessageBox(IDS_ERROR_DIALOG, MB_ICONERROR, 0);
			return FALSE;
		}

		if (bOptions)
		{
			m_pdlgEraser->Options();
			return FALSE;
		}
		else
		{
			HandleQueue(bQueue);
			if (bResolveLock)
			{
				try
				{
					CFileLockResolver::Resolve(strData, saFiles);
				}
				catch (const std::exception& ee)
				{
					AfxMessageBox(ee.what(), MB_ICONERROR);
					return FALSE;
				}
			}
			
			if (bFiles && !bFolders)
			{
				if (!bRecycled)
					findMatchingFiles(strData, saFiles, bSubFolders);
				else
				{
					LocateRecycledItems(saFiles, m_pdlgEraser->m_saFolders);

					if (saFiles.GetSize() > 0 && !bSilent)
					{
						CConfirmDialog cd(m_pdlgEraser);

						if (cd.DoModal() != IDOK)
							return FALSE;
					}
				}
			}
			else
			{
				if (bDrive || GetFileAttributes((LPCTSTR)strData) != (DWORD)-1)
					saFiles.Add(strData);
			}

			if (saFiles.GetSize() > 0 || m_pdlgEraser->m_saFolders.GetSize() > 0)
			{
				if (!bSilent)
					m_pdlgEraser->ShowWindow(SW_SHOW);
				else
					m_pdlgEraser->GetDlgItem(IDCANCEL)->EnableWindow(FALSE);

				m_pdlgEraser->m_saFiles.Copy(saFiles);
				m_pdlgEraser->m_bResults        = bResults;
				m_pdlgEraser->m_bResultsOnError = bResultsOnError;
				m_pdlgEraser->m_bUseFiles       = bFiles || bResolveLock;
				m_pdlgEraser->m_bUseEmptySpace  = bDrive;
				m_pdlgEraser->m_bFolders        = bFolders;
				m_pdlgEraser->m_bSubFolders     = bSubFolders;
				m_pdlgEraser->m_bKeepFolder     = bKeepFolder;
				m_pdlgEraser->m_bRecycled       = bRecycled;
				m_pdlgEraser->m_emMethod        = emMethod;
				m_pdlgEraser->m_uPasses         = uPasses;

				return m_pdlgEraser->Erase();
			}
			else if (!bSilent)
			{
				if (bRecycled)
					AfxMessageBox("Recycle Bin is empty.", MB_ICONERROR);
				else
					AfxMessageBox("File not found. Nothing to erase. (" + strData + ")", MB_ICONERROR);
			}
		}
	}
	catch (CException *e)
	{
		ASSERT(FALSE);
		e->ReportError(MB_ICONERROR);
		e->Delete();
	}
	catch (...)
	{
		ASSERT(FALSE);
	}

	return FALSE;
}

BOOL CLauncherApp::GetNextParameter(CString& strCmdLine, CString& strNextParameter) const
{
    strCmdLine.TrimRight();
    strCmdLine.TrimLeft();

    int iPos = strCmdLine.Find(' ');

    if (iPos != -1)
    {
        strNextParameter = strCmdLine.Left(iPos);
        strCmdLine = strCmdLine.Right(strCmdLine.GetLength() - iPos - 1);

        if (!strNextParameter.IsEmpty() && strNextParameter[0] == '\"')
        {
            iPos = strCmdLine.Find('\"');

            if (iPos != -1)
            {
                strNextParameter = strNextParameter.Right(strNextParameter.GetLength() - 1);
                strNextParameter += " ";
                strNextParameter += strCmdLine.Left(iPos);

                strCmdLine = strCmdLine.Right(strCmdLine.GetLength() - iPos - 1);
            }
            else
            {
                strNextParameter = strNextParameter.Right(strNextParameter.GetLength() - 1);

                iPos = strNextParameter.Find('\"');

                if (iPos != -1)
                    strNextParameter = strNextParameter.Left(iPos);
            }
        }

        return (!strNextParameter.IsEmpty());
    }
    else if (!strCmdLine.IsEmpty())
    {
        strNextParameter = strCmdLine;
        strCmdLine.Empty();

        if (strNextParameter[0] == '\"')
            strNextParameter = strNextParameter.Right(strNextParameter.GetLength() - 1);

        if (strNextParameter[strNextParameter.GetLength() - 1] == '\"')
            strNextParameter = strNextParameter.Left(strNextParameter.GetLength() - 1);

        return TRUE;
    }

    return FALSE;
}

int CLauncherApp::ExitInstance()
{
    if (m_pdlgEraser)
    {
        m_pdlgEraser->DestroyWindow();

        delete m_pdlgEraser;
        m_pdlgEraser = 0;
    }

    if (m_hQueue != NULL)
    {
        ReleaseMutex(m_hQueue);
        CloseHandle(m_hQueue);
        m_hQueue = NULL;
    }

    // clean up the library
    eraserEnd();

    return CWinApp::ExitInstance();
}

void CLauncherApp::HandleQueue(BOOL bQueue)
{
    ASSERT(m_hQueue == NULL);

    DWORD dwQueue = 0;
    CString strName;

    // find our position in the queue
    for (DWORD dwNumber = 0; dwNumber < ERASERL_MAX_QUEUE; dwNumber++)
    {
        strName.Format(szQueueGUID, dwNumber);
        m_hQueue = CreateMutex(NULL, TRUE, (LPCTSTR)strName);

        if (m_hQueue == NULL)
        {
            // mutex creation failed!
            return;
        }
        else if (GetLastError() == ERROR_ALREADY_EXISTS)
        {
            CloseHandle(m_hQueue);
            m_hQueue = NULL;
        }
        else
        {
            // found our position in the queue!
            dwQueue = dwNumber;
            break;
        }

        if (dwNumber == ERASERL_MAX_QUEUE - 1)
            AfxMessageBox(IDS_ERROR_MAX_INSTANCE, MB_ICONWARNING, 0);
    }

    // if we were ordered to wait until other instances have finished,
    // why don't we then
    if (m_hQueue != NULL && bQueue)
    {
        HANDLE m_hPrevInstance = NULL;
        HANDLE m_hTemp = NULL;

        while (dwQueue > 0)
        {
            strName.Format(szQueueGUID, --dwQueue);

            m_hPrevInstance = OpenMutex(MUTEX_ALL_ACCESS, FALSE, (LPCTSTR)strName);

            if (m_hPrevInstance != NULL)
            {
                WaitForSingleObject(m_hPrevInstance, INFINITE);

                CloseHandle(m_hPrevInstance);
                m_hPrevInstance = NULL;

                m_hTemp = CreateMutex(NULL, TRUE, (LPCTSTR)strName);

                if (m_hTemp == NULL)
                    return;

                ReleaseMutex(m_hQueue);
                CloseHandle(m_hQueue);

                m_hQueue = m_hTemp;
                m_hTemp = NULL;
            }
        }
    }
}
