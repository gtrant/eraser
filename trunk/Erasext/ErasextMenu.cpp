// ErasextMenu.cpp
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
#include "resource.h"
#include "..\EraserDll\eraserdll.h"
#include "..\shared\FileHelper.h"
#include "..\shared\key.h"

#include "ConfirmDialog.h"
#include "ConfirmReplaceDlg.h"
#include "WipeProgDlg.h"

#include <shlobj.h>

#include "Erasext.h"
#include "ErasextMenu.h"

#define ResultFromShort(i)  MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, (i));

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

const LPCTSTR szAccelerKey = "Acceler";

/////////////////////////////////////////////////////////////////////////////
// CErasextMenu
enum
{
	CMD_ERASE = 0, CMD_MOVE = 1
};
IMPLEMENT_DYNCREATE(CErasextMenu, CCmdTarget)

static void DisplayError(DWORD dwError)
{
    LPVOID lpMsgBuf;

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
                  NULL,
                  dwError,
                  MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
                  (LPTSTR) &lpMsgBuf,
                  0,
                  NULL);

    AfxMessageBox((LPCTSTR)lpMsgBuf, MB_ICONERROR);

    // free the buffer
    LocalFree(lpMsgBuf);
}

CErasextMenu::CErasextMenu() :
m_bUseFiles(TRUE),
m_dwItems(0),
m_dwDirectories(0),
m_bDragMenu(FALSE)
{
    EnableAutomation();

    // To keep the application running as long as an OLE automation
    // object is active, the constructor calls AfxOleLockApp.

    AfxOleLockApp();

    // NT or not
    OSVERSIONINFO ov;

    ZeroMemory(&ov, sizeof(OSVERSIONINFO));
    ov.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    GetVersionEx(&ov);
    m_bNT = true;//(ov.dwPlatformId == VER_PLATFORM_WIN32_NT);
}

CErasextMenu::~CErasextMenu()
{
    // To terminate the application when all objects created with
    // with OLE automation, the destructor calls AfxOleUnlockApp.

    AfxOleUnlockApp();
}


void CErasextMenu::OnFinalRelease()
{
    // When the last reference for an automation object is released
    // OnFinalRelease is called.  The base class will automatically
    // deletes the object.  Add additional cleanup required for your
    // object before calling the base class.

    CCmdTarget::OnFinalRelease();
}


BEGIN_MESSAGE_MAP(CErasextMenu, CCmdTarget)
    //{{AFX_MSG_MAP(CErasextMenu)
        // NOTE - the ClassWizard will add and remove mapping macros here.
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

BEGIN_DISPATCH_MAP(CErasextMenu, CCmdTarget)
    //{{AFX_DISPATCH_MAP(CErasextMenu)
        // NOTE - the ClassWizard will add and remove mapping macros here.
    //}}AFX_DISPATCH_MAP
END_DISPATCH_MAP()

// Note: we add support for IID_IErasextMenu to support typesafe binding
//  from VBA.  This IID must match the GUID that is attached to the
//  dispinterface in the .ODL file.

// {8BE13461-936F-11D1-A87D-444553540000}
static const IID IID_IErasextMenu =
{ 0x8be13461, 0x936f, 0x11d1, { 0xa8, 0x7d, 0x44, 0x45, 0x53, 0x54, 0x0, 0x0 } };

IMPLEMENT_OLECREATE(CErasextMenu, "ErasextMenu", 0x8be13461, 0x936f, 0x11d1, 0xa8, 0x7d, 0x44, 0x45, 0x53, 0x54, 0x0, 0x0);

BEGIN_INTERFACE_MAP(CErasextMenu, CCmdTarget)
    INTERFACE_PART(CErasextMenu, IID_IErasextMenu, Dispatch)
    INTERFACE_PART(CErasextMenu, IID_IContextMenu, MenuExt)
    INTERFACE_PART(CErasextMenu, IID_IShellExtInit, ShellInit)
END_INTERFACE_MAP()

// IUnknown for IContextMenu
STDMETHODIMP CErasextMenu::XMenuExt::QueryInterface(REFIID riid, void** ppv)
{
    METHOD_PROLOGUE(CErasextMenu, MenuExt);
    TRACE("CErasextMenu::XMenuExt::QueryInterface\n");
    return pThis->ExternalQueryInterface(&riid, ppv);
}

STDMETHODIMP_(ULONG) CErasextMenu::XMenuExt::AddRef(void)
{
    METHOD_PROLOGUE(CErasextMenu, MenuExt);
    TRACE("CErasextMenu::XMenuExt::AddRef\n");
    return pThis->ExternalAddRef();
}

STDMETHODIMP_(ULONG) CErasextMenu::XMenuExt::Release(void)
{
    METHOD_PROLOGUE(CErasextMenu, MenuExt);
    TRACE("CErasextMenu::XMenuExt::Release\n");
    return pThis->ExternalRelease();
}

CString setShortcut(CString str)
{
	CKey kReg;
	CString strPath(""), strKey(""), strRes("");
	strPath.Format("%s\\%s", ERASER_REGISTRY_BASE, szAccelerKey);
	int iPos;
	if (kReg.Open(HKEY_CURRENT_USER, strPath,FALSE))
	{
		if ((iPos=str.Find('&'))!=-1) {
			str = str.Left(iPos) + str.Right(str.GetLength()-iPos-1);
		}
		if (kReg.GetValue(strKey,str))
		{			
			CString strTmp(str);
			strKey.MakeUpper();
			strTmp.MakeUpper();
			iPos=strTmp.Find(strKey[0]);
			strRes = str.Left(iPos)+"&"+str.Right(str.GetLength()-iPos);
		}
		kReg.Close();
	}	
	else{
		strRes = str;
	}
    return strRes;
}

STDMETHODIMP CErasextMenu::XMenuExt::QueryContextMenu(HMENU hMenu, UINT nIndex, UINT idCmdFirst, UINT /*idCmdLast*/, UINT uFlags)
{
    METHOD_PROLOGUE(CErasextMenu, MenuExt);

    // do not show menu for shortcuts or when the shell
    // wants only the default item, or if the user has disabled
    // the shell extension

    CKey kReg;
    BOOL bEnabled = TRUE;
	
    if (kReg.Open(HKEY_CURRENT_USER, ERASER_REGISTRY_BASE))
    {
        kReg.GetValue(bEnabled, ERASEXT_REGISTRY_ENABLED, TRUE);
        kReg.Close();
    }

    if (bEnabled && (uFlags & CMF_VERBSONLY) == 0 && (uFlags & CMF_DEFAULTONLY) == 0)
    {
        CString str;

        try
        {
            if (pThis->m_bDragMenu)
            {
                if (pThis->m_bUseFiles)
                    str.LoadString(IDS_MENU_TEXT_DRAG);
                else
                    return ResultFromShort(0);
            }
            else
            {
                if (!pThis->m_bUseFiles)
                    str.LoadString(IDS_MENU_TEXT_DRIVE);
                else
                    str.LoadString(IDS_MENU_TEXT_FILE);
            }

			if (!InsertMenu(hMenu, nIndex++, MF_SEPARATOR| MF_BYPOSITION, idCmdFirst, ""))
				return ResultFromShort(0);

			
			str = setShortcut(str);
			if (!InsertMenu(hMenu, nIndex++ , MF_STRING | MF_BYPOSITION , idCmdFirst + CMD_ERASE, str))
                return ResultFromShort(0);

			CString moveStr;
			//ASSERT(moveStr.LoadString(IDS_MENU_TEXT_DRAG));
			moveStr.LoadString(IDS_MENU_TEXT_DRAG);
			
			moveStr = setShortcut(moveStr);
			if (!InsertMenu(hMenu, nIndex++, MF_STRING | MF_BYPOSITION, idCmdFirst + CMD_MOVE, moveStr))
				return ResultFromShort(0);


			if (!InsertMenu(hMenu, nIndex++, MF_SEPARATOR| MF_BYPOSITION, idCmdFirst , ""))
				return ResultFromShort(0);

			return MAKE_HRESULT(SEVERITY_SUCCESS, 0, CMD_MOVE + 1);
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
    }

    return ResultFromShort(0);
}

BOOL GetFolder(HWND hParent, TCHAR* path)
{

	BROWSEINFO bi;
	memset(&bi, 0, sizeof (bi));
	bi.hwndOwner = hParent;
	bi.ulFlags = BIF_NEWDIALOGSTYLE | BIF_EDITBOX;
	bi.lpszTitle = "Select target folder";


	LPITEMIDLIST pidlFolder;
	pidlFolder = SHBrowseForFolder(&bi);
	if (!pidlFolder)
		return FALSE;

	//HRESULT hr = SHGetTargetFolderIDList(pidlFolder, &pidlTarget);



	SHGetPathFromIDList(pidlFolder, path);   // Make sure it is a path
	CoTaskMemFree(pidlFolder);

	return TRUE;
	
}
STDMETHODIMP CErasextMenu::XMenuExt::InvokeCommand(LPCMINVOKECOMMANDINFO lpici)
{
    METHOD_PROLOGUE(CErasextMenu, MenuExt);
    TRACE("CErasextMenu::XMenuExt::InvokeCommand\n");

    if (!HIWORD(lpici->lpVerb) &&
        CMD_ERASE == LOWORD((DWORD)lpici->lpVerb) || CMD_MOVE == LOWORD((DWORD)lpici->lpVerb) )
    {
        ASSERT(IsWindow(lpici->hwnd));

        CWnd parent;
        parent.Attach(lpici->hwnd);

		if (CMD_MOVE == LOWORD((DWORD)lpici->lpVerb))
		{
			pThis->m_bDragMenu = TRUE;
		}

        try
        {
            CConfirmDialog cd(&parent);

            cd.m_bSingleFile    = FALSE;
            cd.m_strData        = "?";
            cd.m_bUseFiles      = pThis->m_bUseFiles;
            cd.m_bMove          = pThis->m_bDragMenu;
            cd.m_strTarget      = pThis->m_szDropTarget;

            CString strTemp;

            if (pThis->m_bUseFiles)
            {
                CStringList strlSourceFolders;

                // get source folders if moving
                if (pThis->m_bDragMenu)
                {
                    CString strSourceFolder;
                    int iSize = pThis->m_saData.GetSize(), i;

                    for (i = 0; i < iSize; i++)
                    {
                        strSourceFolder = pThis->m_saData[i];
                        strSourceFolder = strSourceFolder.Left(strSourceFolder.ReverseFind('\\') + 1);

                        if (strlSourceFolders.Find(strSourceFolder) == NULL)
                            strlSourceFolders.AddTail(strSourceFolder);
                    }

                    iSize = pThis->m_saFolders.GetSize();

                    for (i = 0; i < iSize; i++)
                    {
                        strSourceFolder = pThis->m_saFolders[i];
                        if (strSourceFolder[strSourceFolder.GetLength() - 1] == '\\')
                            strSourceFolder.Left(strSourceFolder.GetLength() - 1);

                        strSourceFolder = strSourceFolder.Left(strSourceFolder.ReverseFind('\\') + 1);

                        if (strlSourceFolders.Find(strSourceFolder) == NULL)
                            strlSourceFolders.AddTail(strSourceFolder);
                    }

                    ASSERT(!strlSourceFolders.IsEmpty());
                }

                // parse files from the folders
                if (pThis->m_dwDirectories > 0)
                {
                    CStringArray saSubfolders;
                    int iSize = pThis->m_saFolders.GetSize(), i;

                    // parseDirectory will recount all directories for us
                    pThis->m_dwDirectories = 0;

                    for (i = 0; i < iSize; i++)
                    {
                        parseDirectory((LPCTSTR)pThis->m_saFolders[i],
                                       pThis->m_saData,
                                       saSubfolders,
                                       TRUE,
                                       &pThis->m_dwItems,
                                       &pThis->m_dwDirectories);
                    }

                    // add found subfolders to the list for removal
                    if (saSubfolders.GetSize() > 0)
                        pThis->m_saFolders.InsertAt(0, &saSubfolders);
                }

                // done parsing, continue if there is something to erase

                if (pThis->m_dwItems > 0 || pThis->m_dwDirectories > 0)
                {
                    // select which confirmation message to show

                    if (pThis->m_dwDirectories > 0)
                    {
                        // at least one folder and zero or more files
                        if (!pThis->m_bDragMenu)
                            strTemp.LoadString(IDS_CONFIRM_FILES_AND_FOLDERS);
                        else
                            strTemp.LoadString(IDS_CONFIRM_MOVE_FILES_AND_FOLDERS);

                        cd.m_strData.Format(strTemp, pThis->m_dwItems, pThis->m_dwDirectories);
                    }
                    else if (pThis->m_dwItems == 1)
                    {
                        // only one file
                        cd.m_strData = pThis->m_saData[0];
                        cd.m_bSingleFile = TRUE;
                    }
                    else if (pThis->m_dwItems > 1)
                    {
                        // more than one file and no folders
                        if (!pThis->m_bDragMenu)
                            strTemp.LoadString(IDS_CONFIRM_FILES);
                        else
                            strTemp.LoadString(IDS_CONFIRM_MOVE_FILES);

                        cd.m_strData.Format(strTemp, pThis->m_dwItems);
                    }

                    // ask for confirmation
					if (cd.m_bMove)
					{
						if ( !strlen(pThis->m_szDropTarget) 
							&& !GetFolder(lpici->hwnd, pThis->m_szDropTarget))
						{
							parent.Detach();
							return NOERROR;
						}
						cd.m_strTarget = pThis->m_szDropTarget;

					}

                    if (cd.DoModal() == IDOK)
                    {
                        if (pThis->m_bDragMenu)
                        {
                            // if there is data to copy
                            CStringArray saFolders;
                            if (pThis->m_saFolders.GetSize() > 0)
                                saFolders.Copy(pThis->m_saFolders);
							
							
								
							
                            if (!pThis->MoveFileList(&parent,                 // parent window
                                                     pThis->m_saData,         // source files
                                                     saFolders,               // source folders
                                                     strlSourceFolders,       // source folders
                                                     pThis->m_szDropTarget))  // destination folder
                            {
                                parent.Detach();
                                return E_FAIL;
                            }

                            // the amount of items to erase
                            pThis->m_dwItems = pThis->m_saData.GetSize();
                        }

                        if (pThis->m_dwItems > 0)
                        {
                            // if there are files to erase

                            CEraserDlg dlgEraser(&parent);

                            dlgEraser.m_bMove        = pThis->m_bDragMenu;
                            dlgEraser.m_bShowResults = TRUE;
                            dlgEraser.m_bUseFiles    = pThis->m_bUseFiles;
                            dlgEraser.m_saData.Copy(pThis->m_saData);

                            dlgEraser.DoModal();
                        }

                        if (pThis->m_dwDirectories > 0)
                        {
                            // if there are (empty) directories to remove

                            int iSize = pThis->m_saFolders.GetSize(), i;
                            for (i = 0; i < iSize; i++)
                            {
                                if (eraserOK(eraserRemoveFolder((LPVOID)(LPCTSTR)pThis->m_saFolders[i],
                                        (E_UINT16)pThis->m_saFolders[i].GetLength(), ERASER_REMOVE_FOLDERONLY)))
                                {
                                    SHChangeNotify(SHCNE_RMDIR, SHCNF_PATH, (LPCTSTR)pThis->m_saFolders[i], NULL);
                                }
                            }
                        }
                    }
                }
            }
            else if (!pThis->m_bDragMenu)
            {
                // drive(s)

                if (pThis->m_dwItems > 1)
                    cd.m_strData.LoadString(IDS_CONFIRM_MULTI_DRIVE);
                else if (pThis->m_dwItems == 1)
                {
                    strTemp.LoadString(IDS_CONFIRM_DRIVE);
                    cd.m_strData.Format((LPCTSTR)strTemp, pThis->m_saData[0]);
                }

                if (cd.DoModal() == IDOK)
                {
                    CEraserDlg dlgEraser(&parent);

                    dlgEraser.m_saData.Copy(pThis->m_saData);
                    dlgEraser.m_bShowResults = TRUE;
                    dlgEraser.m_bUseFiles = pThis->m_bUseFiles;

                    dlgEraser.DoModal();
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

        parent.Detach();

        return NOERROR;
    }

    return E_INVALIDARG;
}

void 
CErasextMenu::getstr_handle_erase(UINT /*nType*/, CString& cmdstr)
{
	if (m_bUseFiles)
	{
		if (m_dwDirectories > 0)
			cmdstr.LoadString(IDS_COMMAND_STRING_DIRECTORIES);
		else
			cmdstr.LoadString(IDS_COMMAND_STRING_FILE);
	}
	else
	{
		cmdstr.LoadString(IDS_COMMAND_STRING_DRIVE);
	}

}

void 
CErasextMenu::getstr_handle_move(UINT /*nType*/, CString& cmdstr)
{
	
	cmdstr = "Move";
}

STDMETHODIMP CErasextMenu::XMenuExt::GetCommandString(UINT_PTR  idCmd, UINT nType, UINT* /*pnReserved*/, LPSTR lpszName, UINT nMax)
{
    METHOD_PROLOGUE(CErasextMenu, MenuExt);
    TRACE("CErasextMenu::XMenuExt::GetCommandString\n");

	CString cmdstr;
	ZeroMemory(lpszName, nMax);
	try
	{
		if (0 == idCmd )
			pThis->getstr_handle_erase(nType, cmdstr);
		else
			if (1 == idCmd)
				pThis->getstr_handle_move(nType, cmdstr);
			
			
	}
	catch (CException *e)
	{
		ASSERT(FALSE);

		e->ReportError(MB_ICONERROR);
		e->Delete();

		return E_OUTOFMEMORY;
	}
	catch (...)
	{
		ASSERT(FALSE);
		return E_FAIL;
	}

	if (!pThis->m_bNT)
		lstrcpyn(lpszName, (LPCTSTR) cmdstr, nMax);
	else
	{
		MultiByteToWideChar(CP_ACP, 0, (LPCSTR)cmdstr, -1, (LPWSTR)lpszName,
			nMax / sizeof(WCHAR));
	}
    return NOERROR;
}

// IUnknown for IShellExtInit
STDMETHODIMP CErasextMenu::XShellInit::QueryInterface(REFIID riid, void** ppv)
{
    METHOD_PROLOGUE(CErasextMenu, ShellInit);
    TRACE("CErasextMenu::XShellInit::QueryInterface\n");
    return pThis->ExternalQueryInterface(&riid, ppv);
}

STDMETHODIMP_(ULONG) CErasextMenu::XShellInit::AddRef(void)
{
    METHOD_PROLOGUE(CErasextMenu, ShellInit);
    TRACE("CErasextMenu::XShellInit::AddRef\n");
    return pThis->ExternalAddRef();
}

STDMETHODIMP_(ULONG) CErasextMenu::XShellInit::Release(void)
{
    METHOD_PROLOGUE(CErasextMenu, ShellInit);
    TRACE("CErasextMenu::XShellInit::Release\n");
    return pThis->ExternalRelease();
}

STDMETHODIMP CErasextMenu::XShellInit::Initialize(LPCITEMIDLIST pidlFolder, LPDATAOBJECT lpdobj, HKEY /*hkeyProgID*/)
{
    METHOD_PROLOGUE(CErasextMenu, ShellInit);
    TRACE("CErasextMenu::XShellInit::Initialize\n");

    HRESULT hres = E_FAIL;
    STGMEDIUM medium;
    FORMATETC fmte = {CF_HDROP,
                      NULL,
                      DVASPECT_CONTENT,
                      -1,
                      TYMED_HGLOBAL};

    // duplicate the object pointer
    if (lpdobj == NULL)
    {
        TRACE("CErasextMenu::XShellInit::Initialize() no data object\n");
        return E_FAIL;
    }

    // Use the given IDataObject to get a list of filenames (CF_HDROP)
    hres = lpdobj->GetData(&fmte, &medium);

    if (FAILED(hres))
    {
        TRACE("CErasextMenu::XShellInit::Initialize() can't get data\n");
        return E_FAIL;
    }

    // clear members
    ZeroMemory(pThis->m_szDropTarget, MAX_PATH + 2);
    pThis->m_saData.RemoveAll();
    pThis->m_saFolders.RemoveAll();
    pThis->m_dwItems = 0;
    pThis->m_dwDirectories = 0;

    if (pidlFolder != NULL &&
        SHGetPathFromIDList(pidlFolder, pThis->m_szDropTarget))
    {
        TRACE1("Drop Target: %s\n", pThis->m_szDropTarget);
        pThis->m_bDragMenu = TRUE;
    }
    else
    {
        pThis->m_bDragMenu = FALSE;
        pThis->m_szDropTarget[0] = '\0';
    }


    BOOL bDrives = FALSE;
    BOOL bFiles = FALSE;

    UINT uAmount;
    UINT uCount;
    DWORD dwAttr;

    TCHAR szFileName[MAX_PATH + 1];
    ZeroMemory(szFileName, MAX_PATH + 1);

    // Get the number of items selected.
    uAmount = DragQueryFile(static_cast<HDROP>(medium.hGlobal),
              static_cast<UINT>(-1), NULL, 0);

    for (uCount = 0; uCount < uAmount; uCount++)
    {
        szFileName[0] = '\0';

        DragQueryFile(static_cast<HDROP>(medium.hGlobal), uCount, szFileName, MAX_PATH);

        if (!bFiles && lstrlen(szFileName) <= _MAX_DRIVE)
        {
            // drive
            dwAttr = GetDriveType((LPCTSTR) szFileName);

            if (dwAttr != DRIVE_UNKNOWN &&
                dwAttr != DRIVE_NO_ROOT_DIR &&
                dwAttr != DRIVE_CDROM &&
                dwAttr != DRIVE_REMOTE)
            {
                pThis->m_saData.Add(szFileName);
                bDrives = TRUE;

                pThis->m_dwItems++;
            }
        }
        else if (!bDrives)
        {
            dwAttr = GetFileAttributes(szFileName);

            if (dwAttr != (DWORD)-1)
            {
                if (dwAttr & FILE_ATTRIBUTE_DIRECTORY)
                {
                    // folder - read files later
                    pThis->m_saFolders.Add(szFileName);
                    bFiles = TRUE;

                    pThis->m_dwDirectories++;
                }
                else
                {
                    // file
                    pThis->m_saData.Add(szFileName);
                    bFiles = TRUE;

                    pThis->m_dwItems++;
                }
            }
        }
    }

    // Release the data.
    ReleaseStgMedium(&medium);

    if (bFiles)
        pThis->m_bUseFiles = TRUE;
    else if (bDrives)
        pThis->m_bUseFiles = FALSE;
    else
        return E_FAIL;

    return S_OK;
}


static inline void
GetSourceFolderFromList(CString& strFolder, CString strFile, CStringList& strlList)
{
    POSITION pos = NULL;
    int iPosition = -1;

    if (strFile.IsEmpty())
        return;

    if (strFile[strFile.GetLength() - 1] == '\\')
        strFile = strFile.Left(strFile.GetLength() - 1);

    iPosition = strFile.ReverseFind('\\');

    while (iPosition != -1)
    {
        strFile = strFile.Left(iPosition + 1);
        pos = strlList.Find(strFile);

        if (pos != NULL)
        {
            strFolder = strlList.GetAt(pos);
            break;
        }

        strFile = strFile.Left(strFile.GetLength() - 1);
        iPosition = strFile.ReverseFind('\\');
    }
}

BOOL CErasextMenu::MoveFileList(CWnd *pParent, CStringArray& saList, CStringArray& saFolders,
                                CStringList& strlSource, LPCTSTR szDestination)
{
    CStringArray saErase;
    CString strFile;
    CString strDestination(szDestination);
    CString strTemp;
    BOOL    bFailed = FALSE;
    BOOL    bNoToAll = FALSE, bYesToAll = FALSE;
    int     iSize, i;

    if (strDestination.IsEmpty() || strlSource.IsEmpty())
        return FALSE;

    // folders
    if (strDestination[strDestination.GetLength() - 1] != '\\')
        strDestination += "\\";

    // must reverse the order of folders on the list and
    // convert pathnames to destination folder

    iSize = saFolders.GetSize();
    for (i = 0; i < iSize; i++)
    {
        GetSourceFolderFromList(strTemp, saFolders[i], strlSource);

        // source and destination are same
        if (strTemp.CompareNoCase(strDestination) == 0)
        {
            AfxMessageBox(IDS_ERROR_MOVE_SAMEFOLDER, MB_ICONERROR);
            return FALSE;
        }

        strTemp = saFolders[i].Right(saFolders[i].GetLength() - strTemp.GetLength());
        strTemp = strDestination + strTemp;
        saErase.InsertAt(0, strTemp);
    }

    saFolders.RemoveAll();
    if (saErase.GetSize() > 0)
    {
        saFolders.Copy(saErase);
        saErase.RemoveAll();
    }

    // create destination folders

    iSize = saFolders.GetSize();
    for (i = 0; i < iSize; i++)
    {
        // don't care about the results; if the function fails,
        // the folder either exists or then we'll catch the error
        // when trying to copy files to this directory

        CreateDirectory((LPCTSTR)saFolders[i], NULL);
    }

    // copy files

    iSize = saList.GetSize();
    for (i = 0; i < iSize; i++)
    {
        GetSourceFolderFromList(strTemp, saList[i], strlSource);
        strTemp = saList[i].Right(saList[i].GetLength() - strTemp.GetLength());
        strTemp = strDestination + strTemp;

        if (!CopyFile((LPCTSTR)saList[i], (LPCTSTR)strTemp, TRUE))
        {
            if (GetLastError() == ERROR_FILE_EXISTS)
            {
                if (bNoToAll)
                {
                    // never overwrite the destination
                    continue;
                }
                else
                {
                    CConfirmReplaceDlg crd(pParent);

                    crd.SetExisting((LPCTSTR)strTemp);
                    crd.SetSource((LPCTSTR)saList[i]);

                    if (bYesToAll || crd.DoModal() == IDOK)
                    {
                        bFailed = (CopyFile((LPCTSTR)saList[i], (LPCTSTR)strTemp, FALSE) == FALSE);

                        if (!bYesToAll)
                            bYesToAll = crd.ApplyToAll();
                    }
                    else
                    {
                        if (!bYesToAll && !bNoToAll)
                            bNoToAll = crd.ApplyToAll();

                        // do not erase the source
                        continue;
                    }
                }
            }
            else
            {
                bFailed = TRUE;
            }
        }

        if (bFailed)
        {
            DisplayError(GetLastError());
            return FALSE;
        }

        // if file was copied, set the source to be
        // erased

        saErase.Add(saList[i]);
    }

    // set the file list
    saList.RemoveAll();
    if (saErase.GetSize() > 0)
        saList.Copy(saErase);

    return TRUE;
}
