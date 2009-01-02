/*
Module : FileTreeCtrl.cpp
Purpose: Implementation for an MFC class which provides a tree control similiar 
         to the left hand side of explorer
Created: PJN / 25-12-1999
History: PJN / 11-01-2000 1. Added some asserts to HasGotSubEntries
                          2. Fixed a problem with calling OnDblclk when no tree item is selected
                          3. Removed an unused variable from SetSelectedPath
         PJN / 25-01-2000 1. Minor update to the code in CTreeFileCtrl::OnDblclk to only allow 
                          selected items to be opened.
         PJN / 31-01-2000 1. Fixed a problem when you right mouse click over a non - selected item.
                          The control now implements the same behavior as Explorer for this.
                          2. Removed check for keyboard invocation of the context menu in OnContextMenu
                          3. Now displays the context menu over the selected item when invoked via
                          the keyboard. Again this is the same bahavior as Explorer has.
                          4. Optimized the code in PreTranslateMessage
                          5. Fixed a bug in CTreeFileCtrl::OnEndlabeledit
         PJN / 02-04-2000 1. Fixed a small bug in CTreeFileCtrl::SetRootFolder
                          2. Fixed the problem with initialisation errors in the code. Client code must not 
                          explicitly call PopulateTree when the window is created. When used in a dialog
                          resource this is not necessary as it is called for you in the DDX functions.
         PJN / 13-05-2000 1. Fixed a problem where items on the context menu were being invoked for the 
                          wrong item when you right mouse click over an item that is not the selected item.
                          Behaviour now is that the item is selected prior to showing the context menu. 
                          Now, this is same behaviour as Explorer.
         PJN / 12-07-2000 1. Now uses ON_NOTIFY_REFLECT_EX instead of ON_NOTIFY_REFLECT for handling reflected
                          messages. This allows derived classes to handle these messages also. Thanks to 
                          Christian Dahl for this.
                          2. Sample app now allows drag drop behaviour to be toggled
                          3. Fixed a problem whereby two items were left selected after you did a drap /
                          drop operation. Thanks to Jonathon Ralston for this.
                          4. Removed function declaration for unused function "InsertDriveItem".
                          5. Removed an unreferenced variable in InsertFileItem.
                          6. Tidied up the UpOneLevel functions and made it public.
                          7. Removed all the message handlers in the sample code which reflect straight
                          down to the tree control. Instead the OnCmdMsg first routes the message to this
                          class.
                          8. Renamed all menu items which CTreeFileCtrl uses to include the prefix TREEFILECTRL
                          9. Renamed all the methods to more generic names
                          10. PreTranslateMessage now uses PostMessage instead of calling functions directly. 
                          This allows up to function correctly for derived classes in addition to correctly
                          disabling items through the OnUpdate mechanism
                          11. Removed an unreferrenced variable in OnRclick
                          12. Removed the unreferrenced variable m_hSelItem
                          13. Optimized a number of expressions by putting the boolean comparisons first
                          14. m_bAllowRename parameter is now observed for in place editing of an item
                          15. Now supports hiding of Drive types via the SetDriveHideFlags function. See the 
                          menu options on the Tools menu in the sample program for its usage.
                          16. Filename masks can now be specifed via the SetFileNameMask method. See the 
                          menu options on the Tools menu in the sample program for its usage.
                          17. File types can now be specified via the GetFileHideFlags function. See the 
                          menu options on the Tools menu in the sample program for its usage.
                          18. Fixed a small issue in one of my calls to ::GetKeyState
                          19. Fixed a bug where programs was crashing if an icon index for it could not
                          be found.
                          20. Made many of the methods of CTreeFileCtrl virtual, thus allowable better
                          use in end user derived classes.
                          21. Fixed problem where SetSelectedPath(_T("C:\\"), FALSE) was resulting 
                          in the drive being expanded even through FALSE was being sent in to specify
                          that the item should not be expanded.
                          22. A virtual "CanDisplayFile" has been added to allow you to decide at runtime
                          whether or not a certain file is to be displayed.
                          23. A virtual "CanDisplayFolder" has been added to allow you to decide at
                          runtime whether or not a certain folder is to be displayed
                          24. Now optionally displays compressed files in a different color, similiar to
                          explorer. The color is customizable through the class API.
                          25. Code has been made smarter so that it does not have to spin up the floppy
                          disk to determine if there are files on it. It now initially displays a "+"
                          and only when you try to expand it will it do the actual scan.
         PJN / 23-07-2000 1. Fixed a bug where the expansion state of the selected item was not being
                          preserved across refreshes.
                          2. Now includes full support for Next / Prev support similiar to Windows 
                          Explorer with the Desktop Update.
                          3. Updated sample app to have some useful toolbars.
                          4. Changing any tree settings which can affect its appearance now force
                          a refresh of its contents.
                          5. ItemToPath method has been made const.
                          6. Addition of PathToItem method
                          7. Auto refresh of items is now provided for by means of change notification
                          threads. This is configurable via the SetAutoRefresh method.
                          8. The root folder of the tree control can now be changed from the sample app
                          9. Fixed a bug in SetRootFolder when passed an empty folder
                          10. Fixed a bug where the system image list was not being initialized correctly
                          if the user did not have a "C:\\" drive. This could occur on NT/Windows2000
                          11. Fixed a bug in IsFile and IsFolder which was causing invalid files or folders
                          to appear valid.
                          12. Deleted items are now removed upon expansion. Also if the item being expanded was
                          deleted and it was the only child, then its parent has the "-" expansion button removed
                          13. Removable drive nodes are collapsed back to their root nodes if their media is 
                          changed in the intervening time when a node expansion occurs.
                          14. Wait cursor is now displayed while a refresh is taking place.
                          15. A "OnSelectionChanged" virtual function has now been provided
                          16. Sample app's icon has been made the same as Explorers.
                          17. Sample app now displays the path name of the currently selected item in the tree control.
                          18. Fixed a bug in IsCompressed
                          19. items are now deleted when selected if they do not exist.
         PJN / 05-09-2000 1. Fixed a bug in CTreeFileCtrl::IsFile and CTreeFileCtrl::IsFolder
         PJN / 20-09-2000 1. Control now includes DECLARE_DYNCREATE thereby allowing it to be used
                          in code which requires this such as Stingray's SEC3DTabWnd. Thanks to Petter Nilsen for
                          pointing out this omission
         PJN / 02-10-2000 1. Fixed a stack overwrite problem in CSystemImageList::CSystemImageList
                          2. Removed an unreferrenced variable in CTreeFileCtrl::OnSelChanged
                          3. Removed an unreferrenced variable in CTreeFileCtrl::OnItemExpanding
                          4. Changed the SendMessage in CTreeFileCtrl::OnDblClk to prevent a crash 
                          which was occurring when the open call sometimes caused a refresh call 
                          which changed the tree items at times. When the double click message handler
                          continued it then caused item expand notifications for items already deleted
                          and of course crashes.
                          5. Removed an unreferrenced variable in CTreeFileCtrl::EndDragging
                          6. Removed an unreferrenced variable in CTreeFileCtrl::KillNotificationThread
                          7. Sample app now remembers the selected path and its expansion state across
                          invocations.
         PJN / 05-05-2001 1. Updated copright message.
                          2. Fixed a resource leak where icon resources were not being released. Thanks to Jay Kohler for
                          spotting this problem
         PJN / 05-08-2001 1. You can now optionally display Network Neighborhood
                          2. You can now turn on / off display of file extensions.
                          3. You can now display shared items with a different icon
                          4. Friendly names can now be displayed for drives.
         PJN / 11-08-2001 1. Improved checking to see if action is allowed in Rename and Delete
                          2. Fixed a bug in OnBeginLabelEdit
                          3. Fixed a problem in OnEndLabelEdit which was causing renames to fail when filename extensions 
                          were not being shown.
         PJN / 11-08-2001 1. Fixed a bug in OnSelChanged which was causing a crash when you right click on an empty area of the control.
                          Thanks to Eli Fenton for spotting this one.
                          2. The control now by default shows drives as children of "My Computer" just like in Explorer.
                          3. When you display a rooted directory in the control, you now have the option of displaying the root
                          folder in the control as the root item.  Thanks to Eli Fenton for suggesting this.
         PJN / 26-10-2001 1. Fixed some stability problems with the code. This was due to adding items to the system image list.
                          This is normally a very bad thing. Instead now the code uses TreeView custom draw (just like the blue color
                          for compresed items) to draw the icons for the Network Neighborhood items. Thanks to Darken Screamer and
                          Timo Haberkern for spotting this problem.
         PJN / 24-12-2001 1. Fixed a copy and paste bug in GoForward. Thanks to Michael T. Luongo for this fix.
                          2. Now allows encrypted files to be displayed in a different color
                          3. Fixed memory leak which was occuring when control was being used in a dialog
                          4. Fixed a problem with the calculation of idents when the style "TVS_LINESATROOT" is used.
         PJN / 16-02-2002 1. Updated copyright message
                          2. Fixed a drag/drop problem which caused the tree state to be inconsistent after the file was dropped.
                          3. Fixed a bug in the refresh code which was causing it to not reselect the selected node
                          after the refresh occurs. Thanks to John Noël for this fix.
                          4. Fixed a problem where the custom draw icons for network nodes were not being drawn in the correct
                          positions when scrollbars were present in the control. Again thanks to John Noël for this fix.
                          5. Fixed a bug in SetSelectedPath which would not display the correct selection if the node we 
                          want to select has been deleted due to the node becoming deleted when it was previously collapsed.
                          Thanks to Franz Fackelmann and John Noël for spotting this problem.
         PJN / 05-06-2002 1. Implemented function "SetUsingDifferentColorForEncrypted" which was declared but had no
                          implementation.
                          2. Fixed report of uninitialized member variable "m_nTimerID". Thanks to Emil Isberg for spoting this.
         PJN / 07-08-2002 1. Fixed a bug in the sample app which ships with the class which was occuring when you compiled
                          the code in Visual Studio.Net. This was due to MS changing the location oleimpl2.h header file. Thanks
                          to Darren Schroeder for spotting this problem.
         PJN / 22-09-2002 1. Removed a number of unreferrenced variables from the code, as highlighted by Visual Studio.Net. Thanks
                          to Bill Johnson for spotting this.




Copyright (c) 1999 - 2002 by PJ Naughter.  (Web: www.naughter.com, Email: pjna@naughter.com)

All rights reserved.

Copyright / Usage Details:

You are allowed to include the source code in any product (commercial, shareware, freeware or otherwise) 
when your product is released in binary form. You are allowed to modify the source code in any way you want 
except you cannot modify the copyright details at the top of each module. If you want to distribute source 
code with your application, then you are only allowed to distribute versions released by the author. This is 
to maintain a single distribution point for the source code. 

*/

/////////////////////////////////  Includes  //////////////////////////////////
#include "stdafx.h"
#include "resource.h"
#include "FileTreeCtrl.h"
#include <afxpriv.h>
#include "Shared/SortedArray.h"




//////////////////////////////// Defines / Locals /////////////////////////////

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

#define WM_TREEUPDATE_CHANGE WM_USER

//To avoid having to have the latest Platform SDK installed to compile CTreeFileCtrl
#ifndef FILE_ATTRIBUTE_ENCRYPTED
#define FILE_ATTRIBUTE_ENCRYPTED            0x00004000  
#endif

int CSystemImageList::sm_nRefCount = 0; //Initialise the reference count
CSystemImageList theSystemImageList;    //The one and only system image list instance
CShareEnumerator theSharedEnumerator;   //The one and only share enumerator

//Pull in the WNet Lib automatically
#pragma comment(lib, "mpr.lib")



////////////////////////////// Implementation /////////////////////////////////

CTreeFileCtrlItemInfo::CTreeFileCtrlItemInfo()
{
  m_pNetResource = NULL;
  m_bNetworkNode = FALSE;
  m_bExtensionHidden = FALSE;
}

CTreeFileCtrlItemInfo::CTreeFileCtrlItemInfo(const CTreeFileCtrlItemInfo& ItemInfo)
{
  m_sFQPath       = ItemInfo.m_sFQPath;
  m_sRelativePath = ItemInfo.m_sRelativePath;
  m_bNetworkNode  = ItemInfo.m_bNetworkNode;
  m_pNetResource = new NETRESOURCE;
  if (ItemInfo.m_pNetResource)
  {
    //Copy the direct member variables of NETRESOURCE
    CopyMemory(m_pNetResource, ItemInfo.m_pNetResource, sizeof(NETRESOURCE)); 

    //Duplicate the strings which are stored in NETRESOURCE as pointers
    if (ItemInfo.m_pNetResource->lpLocalName)
		  m_pNetResource->lpLocalName	= _tcsdup(ItemInfo.m_pNetResource->lpLocalName);
    if (ItemInfo.m_pNetResource->lpRemoteName)
		  m_pNetResource->lpRemoteName = _tcsdup(ItemInfo.m_pNetResource->lpRemoteName);
    if (ItemInfo.m_pNetResource->lpComment)
		  m_pNetResource->lpComment	= _tcsdup(ItemInfo.m_pNetResource->lpComment);
    if (ItemInfo.m_pNetResource->lpProvider)
		  m_pNetResource->lpProvider	= _tcsdup(ItemInfo.m_pNetResource->lpProvider);
  }
  else
    ZeroMemory(m_pNetResource, sizeof(NETRESOURCE)); 
}

CTreeFileCtrlItemInfo::~CTreeFileCtrlItemInfo()
{
}


CSystemImageList::CSystemImageList()
{
  ASSERT(sm_nRefCount == 0); //Should only every be one instance of CSystemImageList declared
  ++sm_nRefCount;

  //Get the temp directory. This is used to then bring back the system image list
  TCHAR pszTempDir[_MAX_PATH];
  VERIFY(GetTempPath(_MAX_PATH, pszTempDir));
  TCHAR pszDrive[_MAX_DRIVE + 1];
  _tsplitpath(pszTempDir, pszDrive, NULL, NULL, NULL);
  size_t nLen = _tcslen(pszDrive);
  if (pszDrive[nLen-1] != _T('\\'))
    _tcscat(pszDrive, _T("\\"));

  //Attach to the system image list
  SHFILEINFO sfi;
  HIMAGELIST hSystemImageList = (HIMAGELIST) SHGetFileInfo(pszTempDir, 0, &sfi, sizeof(SHFILEINFO),
                                                           SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
  VERIFY(m_ImageList.Attach(hSystemImageList));
}

CSystemImageList::~CSystemImageList()
{
  //Decrement the reference count
  --sm_nRefCount;

  //Detach from the image list to prevent problems on 95/98 where
  //the system image list is shared across processes
  m_ImageList.Detach();
}

CShareEnumerator::CShareEnumerator()
{
  //Set out member variables to defaults
  m_pNTShareEnum = NULL;
  m_pWin9xShareEnum = NULL;
  m_pNTBufferFree = NULL;
  m_pNTShareInfo = NULL;
  m_pWin9xShareInfo = NULL;
  m_pWin9xShareInfo = NULL;
  m_hNetApi = NULL;
  m_dwShares = 0;

  //Determine if we are running Windows NT or Win9x
  OSVERSIONINFO osvi;
  osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
  m_bWinNT = (GetVersionEx(&osvi) && osvi.dwPlatformId == VER_PLATFORM_WIN32_NT);
  if (m_bWinNT)
  {
    //Load up the NETAPI dll
    m_hNetApi = LoadLibrary(_T("NETAPI32.dll"));
    if (m_hNetApi)
    {
      //Get the required function pointers
      m_pNTShareEnum = (NT_NETSHAREENUM*) GetProcAddress(m_hNetApi, "NetShareEnum");
      m_pNTBufferFree = (NT_NETAPIBUFFERFREE*) GetProcAddress(m_hNetApi, "NetApiBufferFree");
    }
  }
  else
  {
    //Load up the NETAPI dll
    m_hNetApi = LoadLibrary(_T("SVRAPI.dll"));
    if (m_hNetApi)
    {
      //Get the required function pointer
      m_pWin9xShareEnum = (WIN9X_NETSHAREENUM*) GetProcAddress(m_hNetApi, "NetShareEnum");
    }
  }

  //Update the array of shares we know about
  Refresh();
}

CShareEnumerator::~CShareEnumerator()
{
  if (m_bWinNT)
  {
    //Free the buffer if valid
    if (m_pNTShareInfo)
      m_pNTBufferFree(m_pNTShareInfo);
  }
  else
    //Free up the heap memory we have used
		delete [] m_pWin9xShareInfo;      

  //Free the dll now that we are finished with it
  if (m_hNetApi)
  {
    FreeLibrary(m_hNetApi);
    m_hNetApi = NULL;
  }
}

void CShareEnumerator::Refresh()
{
  m_dwShares = 0;
  if (m_bWinNT)
  {
    //Free the buffer if valid
    if (m_pNTShareInfo)
      m_pNTBufferFree(m_pNTShareInfo);

    //Call the function to enumerate the shares
    if (m_pNTShareEnum)
    {
      DWORD dwEntriesRead = 0;
      m_pNTShareEnum(NULL, 502, (LPBYTE*) &m_pNTShareInfo, MAX_PREFERRED_LENGTH, &dwEntriesRead, &m_dwShares, NULL);
    }
  }
  else
  {
    //Free the buffer if valid
    if (m_pWin9xShareInfo)
      delete [] m_pWin9xShareInfo;

    //Call the function to enumerate the shares
    if (m_pWin9xShareEnum)
    {
      //Start with a reasonably sized buffer
      unsigned short cbBuffer = 1024;
      BOOL bNeedMoreMemory = TRUE;
      BOOL bSuccess = FALSE;
      while (bNeedMoreMemory && !bSuccess)
      {
        unsigned short nTotalRead = 0;
        m_pWin9xShareInfo = (CTreeFile_share_info_50*) new BYTE[cbBuffer];
        ZeroMemory(m_pWin9xShareInfo, cbBuffer);
        unsigned short nShares = 0;
        NET_API_STATUS nStatus = m_pWin9xShareEnum(NULL, 50, (char FAR *)m_pWin9xShareInfo, cbBuffer, (unsigned short FAR *)&nShares, (unsigned short FAR *)&nTotalRead);
        if (nStatus == ERROR_MORE_DATA)
		    {            
          //Free up the heap memory we have used
		      delete [] m_pWin9xShareInfo;      

          //And double the size, ready for the next loop around
          cbBuffer *= 2;
		    }
        else if (nStatus == NERR_Success)
        {
          m_dwShares = nShares;
          bSuccess = TRUE;
        }
        else
          bNeedMoreMemory = FALSE;
      }
    }
  }
}

BOOL CShareEnumerator::IsShared(const CString& sPath)
{
  //Assume the item is not shared
  BOOL bShared = FALSE;

  if (m_bWinNT)
  {
    if (m_pNTShareInfo)
    {
      for (DWORD i=0; i<m_dwShares && !bShared; i++)
      {
        CString sShare((LPWSTR) m_pNTShareInfo[i].shi502_path);
        bShared = (sPath.CompareNoCase(sShare) == 0) && ((m_pNTShareInfo[i].shi502_type == STYPE_DISKTREE) || ((m_pNTShareInfo[i].shi502_type == STYPE_PRINTQ)));
      }
    }
  }  
  else
  {
    if (m_pWin9xShareInfo)
    {
      for (DWORD i=0; i<m_dwShares && !bShared; i++)
      {
        CString sShare(m_pWin9xShareInfo[i].shi50_path);
        bShared = (sPath.CompareNoCase(sShare) == 0) && 
                   ((m_pWin9xShareInfo[i].shi50_type == STYPE_DISKTREE) || ((m_pWin9xShareInfo[i].shi50_type == STYPE_PRINTQ)));
      }
    }
  }

  return bShared;
}







CTreeFileCtrlThreadInfo::CTreeFileCtrlThreadInfo() : m_TerminateEvent(FALSE, TRUE)
{
  m_pThread = NULL;
  m_pTree   = NULL;
  m_nIndex  = -1;
}

CTreeFileCtrlThreadInfo::~CTreeFileCtrlThreadInfo()
{
  delete m_pThread;
}




IMPLEMENT_DYNCREATE(CTreeFileCtrl, CTreeCtrl)

BEGIN_MESSAGE_MAP(CTreeFileCtrl, CTreeCtrl)
	//{{AFX_MSG_MAP(CTreeFileCtrl)
	ON_COMMAND(ID_TREEFILECTRL_PROPERTIES, OnProperties)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_PROPERTIES, OnUpdateProperties)
	ON_COMMAND(ID_TREEFILECTRL_RENAME, OnRename)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_RENAME, OnUpdateRename)
	ON_COMMAND(ID_TREEFILECTRL_OPEN, OnOpen)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_OPEN, OnUpdateOpen)
	ON_COMMAND(ID_TREEFILECTRL_DELETE, OnDelete)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_DELETE, OnUpdateDelete)
	ON_COMMAND(ID_TREEFILECTRL_REFRESH, OnRefresh)
	ON_COMMAND(ID_TREEFILECTRL_UPONELEVEL, OnUpOneLevel)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_UPONELEVEL, OnUpdateUpOneLevel)
	ON_WM_CONTEXTMENU()
	ON_WM_INITMENUPOPUP()
	ON_WM_MOUSEMOVE()
	ON_WM_LBUTTONUP()
	ON_WM_TIMER()
	ON_COMMAND(ID_TREEFILECTRL_BACK, OnBack)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_BACK, OnUpdateBack)
	ON_COMMAND(ID_TREEFILECTRL_FORWARD, OnForward)
	ON_UPDATE_COMMAND_UI(ID_TREEFILECTRL_FORWARD, OnUpdateForward)
	ON_WM_DESTROY()
	//}}AFX_MSG_MAP
	ON_WM_CONTEXTMENU()
	ON_NOTIFY_REFLECT_EX(NM_DBLCLK, OnDblclk)
	ON_NOTIFY_REFLECT_EX(TVN_ITEMEXPANDING, OnItemExpanding)
	ON_NOTIFY_REFLECT_EX(TVN_BEGINLABELEDIT, OnBeginLabelEdit)
	ON_NOTIFY_REFLECT_EX(TVN_ENDLABELEDIT, OnEndLabelEdit)
	ON_NOTIFY_REFLECT_EX(NM_RCLICK, OnRclick)
	ON_NOTIFY_REFLECT_EX(TVN_BEGINDRAG, OnBeginDrag)
    ON_NOTIFY_REFLECT_EX(NM_CUSTOMDRAW, OnCustomDraw)
 	ON_NOTIFY_REFLECT_EX(TVN_SELCHANGED, OnSelChanged)
	ON_NOTIFY_REFLECT_EX(TVN_DELETEITEM, OnDeleteItem)
  ON_MESSAGE(WM_TREEUPDATE_CHANGE, OnChange)
END_MESSAGE_MAP()

CTreeFileCtrl::CTreeFileCtrl() : CTreeCtrl()
{
  m_sFileNameMask = _T("*.*");
  m_dwFileHideFlags = FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM | 
                      FILE_ATTRIBUTE_OFFLINE | FILE_ATTRIBUTE_TEMPORARY;
  m_bShowCompressedUsingDifferentColor = TRUE;
  m_rgbCompressed = RGB(0, 0, 255);
  m_bShowEncryptedUsingDifferentColor = TRUE;
  m_rgbEncrypted = RGB(255, 0, 0);
  m_dwDriveHideFlags = 0;
  m_bShowFiles = TRUE;
  m_pilDrag = NULL;
  m_hItemDrag = NULL;
  m_hItemDrop = NULL;
  m_hNetworkRoot = NULL;
  m_TimerTicks = 0;
  m_bAllowDragDrop = FALSE;
  m_bAllowRename = FALSE;
  m_bAllowOpen = TRUE;
  m_bAllowProperties = TRUE;
  m_bAllowDelete = FALSE;
  m_nMaxHistory = 20;
  m_bUpdatingHistorySelection = FALSE;
  m_bAutoRefresh = TRUE;
  m_bShowSharedUsingDifferentIcon = TRUE;
  m_nTimerID = 0;
  for (int i=0; i<26; i++)
    m_dwMediaID[i] = 0xFFFFFFFF;

  m_pMalloc = NULL;
  m_pShellFolder = NULL;
  m_bDisplayNetwork = TRUE;
  m_FileExtensions = UseTheShellSetting;
  m_dwNetworkItemTypes = RESOURCETYPE_ANY;
  m_hMyComputerRoot = NULL;
  m_bShowMyComputer = TRUE;
  m_bShowRootedFolder = FALSE;
  m_hRootedFolder = NULL;
  m_bShowDriveLabels = TRUE;
}

CTreeFileCtrl::~CTreeFileCtrl()
{
}

int CTreeFileCtrl::CompareByFilenameNoCase(CString& element1, CString& element2) 
{
  return element1.CompareNoCase(element2);
}

#ifdef _DEBUG
void CTreeFileCtrl::AssertValid() const
{
	CTreeCtrl::AssertValid();
}

void CTreeFileCtrl::Dump(CDumpContext& dc) const
{
	CTreeCtrl::Dump(dc);
}
#endif //_DEBUG

void CTreeFileCtrl::SetShowFiles(BOOL bFiles) 
{ 
  m_bShowFiles = bFiles; 

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetRootFolder(const CString& sPath)
{
  m_sRootFolder = sPath;

  //Ensure it is terminated with a "\"
  int nLength = m_sRootFolder.GetLength();
  if (nLength && m_sRootFolder.GetAt(nLength-1) != _T('\\'))
    m_sRootFolder += _T('\\');

  //Force a refresh
  Refresh();
}

int CTreeFileCtrl::GetIconIndex(HTREEITEM hItem)
{
  TV_ITEM tvi;
  ZeroMemory(&tvi, sizeof(TV_ITEM));
  tvi.mask = TVIF_IMAGE;
  tvi.hItem = hItem;
  if (GetItem(&tvi))
    return tvi.iImage;
  else
    return -1;
}

int CTreeFileCtrl::GetIconIndex(const CString& sFilename)
{
  //Retreive the icon index for a specified file/folder
  SHFILEINFO sfi;
  ZeroMemory(&sfi, sizeof(SHFILEINFO));
  SHGetFileInfo(sFilename, 0, &sfi, sizeof(SHFILEINFO), SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
  return sfi.iIcon; 
}

int CTreeFileCtrl::GetSelIconIndex(const CString& sFilename)
{
  //Retreive the icon index for a specified file/folder
  SHFILEINFO sfi;
  ZeroMemory(&sfi, sizeof(SHFILEINFO));
  SHGetFileInfo(sFilename, 0, &sfi, sizeof(SHFILEINFO), SHGFI_SYSICONINDEX | SHGFI_OPENICON | SHGFI_SMALLICON);
  return sfi.iIcon; 
}

int CTreeFileCtrl::GetIconIndex(LPITEMIDLIST lpPIDL)
{
  SHFILEINFO sfi;
  ZeroMemory(&sfi, sizeof(SHFILEINFO));
  SHGetFileInfo((LPCTSTR)lpPIDL, 0, &sfi, sizeof(sfi), SHGFI_PIDL | SHGFI_SYSICONINDEX | SHGFI_SMALLICON | SHGFI_LINKOVERLAY);
  return sfi.iIcon; 
}

int CTreeFileCtrl::GetSelIconIndex(LPITEMIDLIST lpPIDL)
{
  SHFILEINFO sfi;
  ZeroMemory(&sfi, sizeof(SHFILEINFO));
  SHGetFileInfo((LPCTSTR)lpPIDL, 0, &sfi, sizeof(sfi), SHGFI_PIDL | SHGFI_SYSICONINDEX | SHGFI_SMALLICON | SHGFI_OPENICON);
  return sfi.iIcon; 
}

int CTreeFileCtrl::GetSelIconIndex(HTREEITEM hItem)
{
  TV_ITEM tvi;
  ZeroMemory(&tvi, sizeof(TV_ITEM));
  tvi.mask = TVIF_SELECTEDIMAGE;
  tvi.hItem = hItem;
  if (GetItem(&tvi))
    return tvi.iSelectedImage;
  else
    return -1;
}

HTREEITEM CTreeFileCtrl::FindSibling(HTREEITEM hParent, const CString& sItem) const
{
  HTREEITEM hChild = GetChildItem(hParent);
  while (hChild)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hChild);
    ASSERT(pItem);
    if (pItem->m_sRelativePath.CompareNoCase(sItem) == 0)
      return hChild;
    hChild = GetNextItem(hChild, TVGN_NEXT);
  }
  return NULL;
}

CString CTreeFileCtrl::GetSelectedPath()
{
  HTREEITEM hItem = GetSelectedItem();
  if (hItem)
    return ItemToPath(hItem);
  else
    return CString();
}

HTREEITEM CTreeFileCtrl::FindServersNode(HTREEITEM hFindFrom) const
{
  if (m_bDisplayNetwork)
  {
    //Try to find some "servers" in the child items of hFindFrom
    HTREEITEM hChild = GetChildItem(hFindFrom);
    while (hChild)
    {
      CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hChild);
      ASSERT(pItem);
      if (pItem->m_pNetResource)
      {
        //Found a share 
        if (pItem->m_pNetResource->dwDisplayType == RESOURCEDISPLAYTYPE_SERVER)
          return hFindFrom;
      }
 
      //Get the next sibling for the next loop around
      hChild = GetNextSiblingItem(hChild);
    }

    //Ok, since we got here, we did not find any servers in any of the child nodes of this 
    //item. In this case we need to call ourselves recursively to find one
    hChild = GetChildItem(hFindFrom);
    while (hChild)
    {
      HTREEITEM hFound = FindServersNode(hChild);
      if (hFound)
        return hFound;
       
      //Get the next sibling for the next loop around
      hChild = GetNextSiblingItem(hChild);
    }
  }

  //If we got as far as here then no servers were found.
  return NULL;
}

void CTreeFileCtrl::SetHasPlusButton(HTREEITEM hItem, BOOL bHavePlus)
{
  //Remove all the child items from the parent
  TV_ITEM tvItem;
  tvItem.hItem = hItem;
  tvItem.mask = TVIF_CHILDREN;  
  tvItem.cChildren = bHavePlus;
  SetItem(&tvItem);
}

BOOL CTreeFileCtrl::HasPlusButton(HTREEITEM hItem)
{
  TVITEM tvItem;
  tvItem.hItem = hItem;
  tvItem.mask = TVIF_HANDLE | TVIF_CHILDREN;
  return GetItem(&tvItem) && (tvItem.cChildren != 0);
}

HTREEITEM CTreeFileCtrl::SetSelectedPath(const CString& sPath, BOOL bExpanded)
{
  CString sSearch(sPath);
  int nSearchLength = sSearch.GetLength();
  if (nSearchLength == 0)
  {
    TRACE(_T("Cannot select a empty path\n"));
    return NULL;
  }

  //Remove trailing "\" from the path
  if (nSearchLength > 3 && sSearch.GetAt(nSearchLength-1) == _T('\\'))
    sSearch = sSearch.Left(nSearchLength-1);
  
  //Remove initial part of path if the root folder is setup
  int nRootLength = m_sRootFolder.GetLength();
  if (nRootLength)
  {
    if (sSearch.Find(m_sRootFolder) != 0)
    {
      TRACE(_T("Could not select the path %s as the root has been configued as %s\n"), sPath, m_sRootFolder);
      return NULL;
    }
    sSearch = sSearch.Right(sSearch.GetLength() - nRootLength);
  }

  if (sSearch.IsEmpty())
    return NULL;

  SetRedraw(FALSE);

  HTREEITEM hItemFound = TVI_ROOT;
  if (nRootLength && m_hRootedFolder)
    hItemFound = m_hRootedFolder;
  BOOL bDriveMatch = m_sRootFolder.IsEmpty();
  BOOL bNetworkMatch = m_bDisplayNetwork && ((sSearch.GetLength() > 2) && sSearch.Find(_T("\\\\")) == 0);
  if (bNetworkMatch)
  {
    bDriveMatch = FALSE;

    //Working here
    BOOL bHasPlus = HasPlusButton(m_hNetworkRoot);
    BOOL bHasChildren = (GetChildItem(m_hNetworkRoot) != NULL);

    if (bHasPlus && !bHasChildren)
      DoExpand(m_hNetworkRoot);
    else
      Expand(m_hNetworkRoot, TVE_EXPAND);
    hItemFound = FindServersNode(m_hNetworkRoot);
    sSearch = sSearch.Right(sSearch.GetLength() - 2);
  }
  if (bDriveMatch)
  {
    if (m_hMyComputerRoot)
    {
      //Working here
      BOOL bHasPlus = HasPlusButton(m_hMyComputerRoot);
      BOOL bHasChildren = (GetChildItem(m_hMyComputerRoot) != NULL);

      if (bHasPlus && !bHasChildren)
        DoExpand(m_hMyComputerRoot);
      else
        Expand(m_hMyComputerRoot, TVE_EXPAND);
      hItemFound = m_hMyComputerRoot;
    }
  }

  int nFound = sSearch.Find(_T('\\'));
  while (nFound != -1)
  {
    CString sMatch;
    if (bDriveMatch)
    {
      sMatch = sSearch.Left(nFound + 1);
      bDriveMatch = FALSE;
    }
    else
      sMatch = sSearch.Left(nFound);
    
    hItemFound = FindSibling(hItemFound, sMatch);
    if (hItemFound == NULL)
      break;
    else if (!IsDrive(sPath))
    {
      SelectItem(hItemFound);

      //Working here
      BOOL bHasPlus = HasPlusButton(hItemFound);
      BOOL bHasChildren = (GetChildItem(hItemFound) != NULL);

      if (bHasPlus && !bHasChildren)
        DoExpand(hItemFound);
      else
        Expand(hItemFound, TVE_EXPAND);
    }

    sSearch = sSearch.Right(sSearch.GetLength() - nFound - 1);
    nFound = sSearch.Find(_T('\\'));
  };

  //The last item 
  if (hItemFound)
  {
    if (sSearch.GetLength())
      hItemFound = FindSibling(hItemFound, sSearch);
    if (hItemFound)
      SelectItem(hItemFound);

    if (bExpanded)
    {
      //Working here
      BOOL bHasPlus = HasPlusButton(hItemFound);
      BOOL bHasChildren = (GetChildItem(hItemFound) != NULL);

      if (bHasPlus && !bHasChildren)
        DoExpand(hItemFound);
      else
        Expand(hItemFound, TVE_EXPAND);
    }
  }

  //Turn back on the redraw flag
  SetRedraw(TRUE);

  return hItemFound;
}

BOOL CTreeFileCtrl::Rename(HTREEITEM hItem)
{
  if (hItem)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hItem);
    ASSERT(pItem);
	  if (m_bAllowRename && !IsDrive(hItem) && !pItem->m_bNetworkNode)
      return (EditLabel(hItem) != NULL);
    else
      return FALSE;
  }
  else
    return FALSE;
}

BOOL CTreeFileCtrl::ShowProperties(HTREEITEM hItem)
{
  BOOL bSuccess = FALSE;
  if (m_bAllowProperties && hItem)
  {
    //Show the "properties" for the selected file
    CString sFile = ItemToPath(hItem);
    SHELLEXECUTEINFO sei;
    ZeroMemory(&sei,sizeof(sei));
    sei.cbSize = sizeof(sei);
    sei.hwnd = AfxGetMainWnd()->GetSafeHwnd();
    sei.nShow = SW_SHOW;
    sei.lpFile = sFile.GetBuffer(sFile.GetLength());
    sei.lpVerb = _T("properties");
    sei.fMask  = SEE_MASK_INVOKEIDLIST;
    bSuccess = ShellExecuteEx(&sei);
    sFile.ReleaseBuffer();
  }
  return bSuccess;
}

BOOL CTreeFileCtrl::Delete(HTREEITEM hItem)
{
  BOOL bSuccess = FALSE;

  if (hItem)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hItem);
    ASSERT(pItem);
    if (m_bAllowDelete && !IsDrive(hItem) && !pItem->m_bNetworkNode)
    {
      //Create a Multi SZ string with the filename to delete
      CString sFileToDelete = ItemToPath(hItem);
      int nChars = sFileToDelete.GetLength() + 1;
      nChars++;
      SHFILEOPSTRUCT shfo;
      ZeroMemory(&shfo, sizeof(SHFILEOPSTRUCT));
      shfo.hwnd = AfxGetMainWnd()->GetSafeHwnd();
      shfo.wFunc = FO_DELETE;

      //Undo is not allowed if the SHIFT key is held down
      if (!(GetKeyState(VK_SHIFT) & 0x8000))
        shfo.fFlags = FOF_ALLOWUNDO;

      TCHAR* pszFrom = new TCHAR[nChars];
      TCHAR* pszCur = pszFrom;
      _tcscpy(pszCur, sFileToDelete);
      pszCur[nChars-1] = _T('\0');
      shfo.pFrom = pszFrom;

      BOOL bOldAutoRefresh = m_bAutoRefresh;
      m_bAutoRefresh = FALSE; //Prevents us from getting thread notifications

      //Let the shell perform the actual deletion
      if (SHFileOperation(&shfo) == 0 && shfo.fAnyOperationsAborted == FALSE)
      {
        //Delete the item from the view
        bSuccess = DeleteItem(hItem);
      }

      m_bAutoRefresh = bOldAutoRefresh;

      //Free up the memory we had allocated
      delete [] pszFrom;
    }
  }
  return bSuccess;
}

BOOL CTreeFileCtrl::Open(HTREEITEM hItem)
{
  BOOL bSuccess = FALSE;
  if (m_bAllowOpen && hItem)
  {
    //Show the "properties" for the selected file
    CString sFile = ItemToPath(hItem);
    SHELLEXECUTEINFO sei;
    ZeroMemory(&sei,sizeof(sei));
    sei.cbSize = sizeof(sei);
    sei.hwnd = AfxGetMainWnd()->GetSafeHwnd();
    sei.nShow = SW_SHOW;
    sei.lpFile = sFile.GetBuffer(sFile.GetLength());
    sei.fMask  = SEE_MASK_INVOKEIDLIST;
    bSuccess = ShellExecuteEx(&sei);
    sFile.ReleaseBuffer();
  }
  return bSuccess;
}

void CTreeFileCtrl::SetFlags(DWORD dwFlags)
{
  SetShowFiles((dwFlags & TFC_SHOWFILES) != 0);
  SetAllowDragDrop((dwFlags & TFC_ALLOWDRAGDROP) != 0);
  SetAllowRename((dwFlags & TFC_ALLOWRENAME) != 0);  
  SetAllowOpen((dwFlags & TFC_ALLOWOPEN) != 0);    
  SetAllowProperties((dwFlags & TFC_ALLOWPROPERTIES) != 0);
  SetAllowDelete((dwFlags & TFC_ALLOWDELETE) != 0);
}

void CTreeFileCtrl::SetDriveHideFlags(DWORD dwDriveHideFlags)
{
  m_dwDriveHideFlags = dwDriveHideFlags;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetFileHideFlags(DWORD dwFileHideFlags)
{
  m_dwFileHideFlags = dwFileHideFlags;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetFileNameMask(const CString& sFileNameMask)
{
  m_sFileNameMask = sFileNameMask;

  //Force a refresh
  Refresh();
}


void CTreeFileCtrl::SetCompressedColor(COLORREF rgbCompressed)
{
  m_rgbCompressed = rgbCompressed;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetDisplayNetwork(BOOL bDisplayNetwork)
{
  m_bDisplayNetwork = bDisplayNetwork;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetUsingDifferentColorForCompressed(BOOL bShowCompressedUsingDifferentColor)
{
  m_bShowCompressedUsingDifferentColor = bShowCompressedUsingDifferentColor;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetUsingDifferentIconForSharedFolders(BOOL bShowSharedUsingDifferentIcon)
{
  m_bShowSharedUsingDifferentIcon = bShowSharedUsingDifferentIcon;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetUsingDifferentColorForEncrypted(BOOL bShowEncryptedUsingDifferentColor)
{
  m_bShowEncryptedUsingDifferentColor = bShowEncryptedUsingDifferentColor;

  //Force a refresh
  Refresh();
};

void CTreeFileCtrl::SetShowFileExtensions(HideFileExtension FileExtensions)
{
  m_FileExtensions = FileExtensions;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetNetworkItemTypes(DWORD dwTypes)
{
  m_dwNetworkItemTypes = dwTypes;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetShowDriveLabels(BOOL bShowDriveLabels)
{
  m_bShowDriveLabels = bShowDriveLabels;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetShowMyComputer(BOOL bShowMyComputer)
{
  m_bShowMyComputer = bShowMyComputer;

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::SetShowRootedFolder(BOOL bShowRootedFolder)
{
  m_bShowRootedFolder = bShowRootedFolder;

  //Force a refresh
  Refresh();
}

BOOL CTreeFileCtrl::CanDisplayFile(const CFileFind& find)
{
  //Derived classes can decide dynamically whether or not a 
  //certain file are to be displayed. CTreeFileCtrl by default
  //displays all files which do not have attributes as set in m_dwFileHideFlags

  return (m_bShowFiles && !find.IsDirectory() && !find.MatchesMask(m_dwFileHideFlags));
}

BOOL CTreeFileCtrl::CanDisplayFolder(const CFileFind& find)
{
  //Derived classes can decide dynamically whether or not a 
  //certain folder are to be displayed. CTreeFileCtrl by default
  //displays all folders excluding the ".." and "." entries

  return (find.IsDirectory() && !find.IsDots());
}

BOOL CTreeFileCtrl::CanDisplayNetworkItem(CTreeFileCtrlItemInfo* /*pItem*/)
{
  //Derived classes can decide dynamically whether or not a 
  //certain network items are to be displayed. CTreeFileCtrl by default
  //displays all network items

  return TRUE;
}

BOOL CTreeFileCtrl::CanHandleChangeNotifications(const CString& sPath)
{
  //check if this drive is one of the types which can issue notification changes
  CString sDrive(sPath);
  if (!IsDrive(sDrive))
    sDrive = sPath.Left(3);

  UINT nDrive = GetDriveType(sDrive);
  return ((nDrive != DRIVE_REMOVABLE) && nDrive != DRIVE_CDROM);
}

BOOL CTreeFileCtrl::CanDisplayDrive(const CString& sDrive)
{
  //Derived classes can decide dynamically whether or not a 
  //certain drive is to be displayed. CTreeFileCtrl by default
  //displays all drives which do not have attributes as set in
  //m_dwDriveHideFlags

  //check if this drive is one of the types to hide
  BOOL bDisplay = TRUE;
  UINT nDrive = GetDriveType(sDrive);
  switch (nDrive)
  {
    case DRIVE_REMOVABLE:
    {
      if (m_dwDriveHideFlags & DRIVE_ATTRIBUTE_REMOVABLE)
        bDisplay = FALSE;
      break;
    }
    case DRIVE_FIXED:
    {
      if (m_dwDriveHideFlags & DRIVE_ATTRIBUTE_FIXED)
        bDisplay = FALSE;
      break;
    }
    case DRIVE_REMOTE:
    {
      if (m_dwDriveHideFlags & DRIVE_ATTRIBUTE_REMOTE)
        bDisplay = FALSE;
      break;
    }
    case DRIVE_CDROM:
    {
      if (m_dwDriveHideFlags & DRIVE_ATTRIBUTE_CDROM)
        bDisplay = FALSE;
      break;
    }
    case DRIVE_RAMDISK:
    {
      if (m_dwDriveHideFlags & DRIVE_ATTRIBUTE_RAMDISK)
        bDisplay = FALSE;
      break;
    }
    default:
    {
      break;
    }
  }

  return bDisplay;
}

void CTreeFileCtrl::OnRename() 
{
  Rename(GetSelectedItem());
}

void CTreeFileCtrl::OnUpdateRename(CCmdUI* pCmdUI) 
{
  HTREEITEM hSelItem = GetSelectedItem();
  if (hSelItem)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hSelItem);
    ASSERT(pItem);
	  pCmdUI->Enable(m_bAllowRename && !IsDrive(hSelItem) && !pItem->m_bNetworkNode);
  }
  else
    pCmdUI->Enable(FALSE);
}

void CTreeFileCtrl::OnProperties() 
{
  ShowProperties(GetSelectedItem());
}

void CTreeFileCtrl::OnUpdateProperties(CCmdUI* pCmdUI) 
{
  HTREEITEM hSelItem = GetSelectedItem();
  if (hSelItem)
  {
    if (m_bAllowProperties)
    {
      CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hSelItem);
      ASSERT(pItem);
      if (pItem->m_bNetworkNode)
      { 
        if (pItem->m_pNetResource)
          pCmdUI->Enable(pItem->m_pNetResource->dwDisplayType == RESOURCEDISPLAYTYPE_SERVER ||
                         pItem->m_pNetResource->dwDisplayType == RESOURCEDISPLAYTYPE_SHARE);
        else
          pCmdUI->Enable(FALSE);
      }
      else
        pCmdUI->Enable(TRUE);
    }
    else
      pCmdUI->Enable(FALSE);
  }
  else
    pCmdUI->Enable(FALSE);
}

void CTreeFileCtrl::OnOpen() 
{
  Open(GetSelectedItem());
}

void CTreeFileCtrl::OnUpdateOpen(CCmdUI* pCmdUI) 
{
  HTREEITEM hSelItem = GetSelectedItem();
  if (hSelItem)
  {
    if (m_bAllowOpen)
    {
      CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hSelItem);
      ASSERT(pItem);
      if (pItem->m_bNetworkNode)
      { 
        if (pItem->m_pNetResource)
          pCmdUI->Enable(pItem->m_pNetResource->dwDisplayType == RESOURCEDISPLAYTYPE_SERVER ||
                         pItem->m_pNetResource->dwDisplayType == RESOURCEDISPLAYTYPE_SHARE);
        else
          pCmdUI->Enable(FALSE);
      }
      else
        pCmdUI->Enable(TRUE);
    }
    else
      pCmdUI->Enable(FALSE);
  }
  else
    pCmdUI->Enable(FALSE);

}

void CTreeFileCtrl::OnDelete() 
{
  Delete(GetSelectedItem());
}

void CTreeFileCtrl::OnUpdateDelete(CCmdUI* pCmdUI) 
{
  HTREEITEM hSelItem = GetSelectedItem();
  if (hSelItem)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hSelItem);
    ASSERT(pItem);
	  pCmdUI->Enable(m_bAllowDelete && !IsDrive(hSelItem) && !pItem->m_bNetworkNode);
  }
  else
    pCmdUI->Enable(FALSE);
}

void CTreeFileCtrl::OnContextMenu(CWnd*, CPoint point)
{
	CMenu menu;
	VERIFY(menu.LoadMenu(IDR_TREEFILECTRL_POPUP));
	CMenu* pPopup = menu.GetSubMenu(0);
	ASSERT(pPopup != NULL);
	pPopup->TrackPopupMenu(TPM_LEFTALIGN | TPM_RIGHTBUTTON, point.x, point.y,	this);
}

HTREEITEM CTreeFileCtrl::InsertFileItem(HTREEITEM hParent, CTreeFileCtrlItemInfo* pItem, BOOL bShared, int nIcon, int nSelIcon, BOOL bCheckForChildren)
{
  CString sLabel;

  //Correct the label if need be
  if (IsDrive(pItem->m_sFQPath) && m_bShowDriveLabels)
    sLabel = GetDriveLabel(pItem->m_sFQPath);
  else
    sLabel = GetCorrectedLabel(pItem);

  //Add the actual item
	TV_INSERTSTRUCT tvis;
  ZeroMemory(&tvis, sizeof(TV_INSERTSTRUCT));
	tvis.hParent = hParent;
	tvis.hInsertAfter = TVI_LAST;
	tvis.item.mask = TVIF_CHILDREN | TVIF_IMAGE | TVIF_SELECTEDIMAGE | TVIF_TEXT | TVIF_PARAM;
  tvis.item.lParam = (LPARAM) pItem;
  tvis.item.pszText = sLabel.GetBuffer(sLabel.GetLength());
  tvis.item.iImage = nIcon;
  tvis.item.iSelectedImage = nSelIcon;
  if (bCheckForChildren)
	  tvis.item.cChildren = HasGotSubEntries(pItem->m_sFQPath);
  else
    tvis.item.cChildren = TRUE;
  if (bShared)
  {
    tvis.item.mask |= TVIF_STATE;
    tvis.item.stateMask |= TVIS_OVERLAYMASK;
    tvis.item.state |= INDEXTOOVERLAYMASK(1); //1 is the index for the shared overlay image
  }

  HTREEITEM hItem = InsertItem(&tvis);
  sLabel.ReleaseBuffer();
  return hItem;
}

BOOL CTreeFileCtrl::GetChecked(HTREEITEM hItem) const
{
	ASSERT(::IsWindow(m_hWnd));
	TVITEM item;
	item.mask = TVIF_HANDLE | TVIF_STATE;
	item.hItem = hItem;
	item.stateMask = TVIS_STATEIMAGEMASK;
	VERIFY(::SendMessage(m_hWnd, TVM_GETITEM, 0, (LPARAM)&item));
	// Return zero if it's not checked, or nonzero otherwise.
	return ((BOOL)(item.state >> 12) -1);
}

BOOL CTreeFileCtrl::SetChecked(HTREEITEM hItem, BOOL fCheck)
{
	ASSERT(::IsWindow(m_hWnd));
	TVITEM item;
	item.mask = TVIF_HANDLE | TVIF_STATE;
	item.hItem = hItem;
	item.stateMask = TVIS_STATEIMAGEMASK;

	/*
	Since state images are one-based, 1 in this macro turns the check off, and
	2 turns it on.
	*/
	item.state = INDEXTOSTATEIMAGEMASK((fCheck ? 2 : 1));

	return (BOOL)::SendMessage(m_hWnd, TVM_SETITEM, 0, (LPARAM)&item);
}


void CTreeFileCtrl::Refresh()
{
  if (GetSafeHwnd())
    OnRefresh();
}

BOOL CTreeFileCtrl::IsExpanded(HTREEITEM hItem)
{
  TVITEM tvItem;
  tvItem.hItem = hItem;
  tvItem.mask = TVIF_HANDLE | TVIF_STATE;
  return GetItem(&tvItem) && (tvItem.state & TVIS_EXPANDED);
}

CString CTreeFileCtrl::GetCorrectedLabel(CTreeFileCtrlItemInfo* pItem)
{
  CString sLabel(pItem->m_sRelativePath);

  switch (m_FileExtensions)
  {
    case UseTheShellSetting:
    {
      TCHAR pszLabel[_MAX_PATH];
      if (IsFile(pItem->m_sFQPath) && GetFileTitle(pItem->m_sRelativePath, pszLabel, _MAX_PATH) == 0)
      {
        pItem->m_bExtensionHidden = (sLabel.CompareNoCase(pszLabel) != 0);
        sLabel = pszLabel;
      }
      break;
    }
    case HideExtension:
    {
      //Remove the extension if the item is a file
      if (IsFile(pItem->m_sFQPath))
      {
        TCHAR szPath[_MAX_PATH];
        TCHAR szDrive[_MAX_DRIVE];
        TCHAR szDir[_MAX_DIR];
        TCHAR szFname[_MAX_FNAME];
        _tsplitpath(pItem->m_sRelativePath, szDrive, szDir, szFname, NULL);
        _tmakepath(szPath, szDrive, szDir, szFname, NULL);
        sLabel = szPath;
        pItem->m_bExtensionHidden = TRUE;
      }
      break;
    }
    default:
    {
      pItem->m_bExtensionHidden = FALSE;
      break;
    }
  }

  return sLabel;
}

void CTreeFileCtrl::OnRefresh() 
{
  //Just in case this will take some time
  CWaitCursor wait;

  SetRedraw(FALSE);

  //Get the item which is currently selected
  HTREEITEM hSelItem = GetSelectedItem();
  CString sItem;
  BOOL bExpanded = FALSE;
  if (hSelItem)
  {
    sItem = ItemToPath(hSelItem);
    bExpanded = IsExpanded(hSelItem); 
  }

  theSharedEnumerator.Refresh();

  KillNotificationThreads();

  //Remove all nodes that currently exist
  Clear();

  //Display the folder items in the tree
  if (m_sRootFolder.IsEmpty())
  {
    //Should we insert a "My Computer" node
    if (m_bShowMyComputer)
    {
      CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
      pItem->m_bNetworkNode = FALSE;
      int nIcon = 0;
      int nSelIcon = 0;

      //Get the localized name and correct icons for "My Computer"
      LPITEMIDLIST lpMCPidl;
      if (SUCCEEDED(SHGetSpecialFolderLocation(NULL, CSIDL_DRIVES, &lpMCPidl)))
      {
        SHFILEINFO sfi;
        if (SHGetFileInfo((LPCTSTR)lpMCPidl, 0, &sfi, sizeof(sfi), SHGFI_PIDL | SHGFI_DISPLAYNAME))
        {
          pItem->m_sRelativePath = sfi.szDisplayName;
          pItem->m_sFQPath = pItem->m_sRelativePath;
        }
        nIcon = GetIconIndex(lpMCPidl);
        nSelIcon = GetSelIconIndex(lpMCPidl);

        //Free up the pidl now that we are finished with it
        ASSERT(m_pMalloc);
        m_pMalloc->Free(lpMCPidl);
        m_pMalloc->Release();
      }

      //Add it to the tree control
      m_hMyComputerRoot = InsertFileItem(TVI_ROOT, pItem, FALSE, nIcon, nSelIcon, FALSE);
    }

    //Display all the drives
    if (!m_bShowMyComputer)
      DisplayDrives(TVI_ROOT, FALSE);

    //Also add network neighborhood if requested to do so
    if (m_bDisplayNetwork)
    {
      CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
      pItem->m_bNetworkNode = TRUE;
      int nIcon = 0;
      int nSelIcon = 0;

      //Get the localized name and correct icons for "Network Neighborhood"
      LPITEMIDLIST lpNNPidl;
      if (SUCCEEDED(SHGetSpecialFolderLocation(NULL, CSIDL_NETWORK, &lpNNPidl)))
      {
        SHFILEINFO sfi;
        if (SHGetFileInfo((LPCTSTR)lpNNPidl, 0, &sfi, sizeof(sfi), SHGFI_PIDL | SHGFI_DISPLAYNAME))
        {
          pItem->m_sRelativePath = sfi.szDisplayName;
          pItem->m_sFQPath = pItem->m_sRelativePath;
        }
        nIcon = GetIconIndex(lpNNPidl);
        nSelIcon = GetSelIconIndex(lpNNPidl);

        //Free up the pidl now that we are finished with it
        ASSERT(m_pMalloc);
        m_pMalloc->Free(lpNNPidl);
        m_pMalloc->Release();
      }

      //Add it to the tree control
      m_hNetworkRoot = InsertFileItem(TVI_ROOT, pItem, FALSE, nIcon, nSelIcon, FALSE);
    }
  }
  else
  {
    DisplayPath(m_sRootFolder, TVI_ROOT, FALSE);
    if (CanHandleChangeNotifications(m_sRootFolder))
      CreateMonitoringThread(m_sRootFolder);
  }
  
  //Reselect the initially selected item
  if (hSelItem)
    SetSelectedPath(sItem, bExpanded);

  //Turn back on the redraw flag
  SetRedraw(TRUE);
}

BOOL CTreeFileCtrl::OnBeginLabelEdit(NMHDR* pNMHDR, LRESULT* pResult) 
{
	TV_DISPINFO* pDispInfo = (TV_DISPINFO*)pNMHDR;

  if (pDispInfo->item.hItem)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(pDispInfo->item.hItem);
    ASSERT(pItem);
	  if (m_bAllowRename && !IsDrive(pDispInfo->item.hItem) && !pItem->m_bNetworkNode)
      *pResult = FALSE;
    else
    	*pResult = TRUE;
  }
  else
    *pResult = TRUE;

  return TRUE; //Allow the message to be reflected again
}

BOOL CTreeFileCtrl::OnCustomDraw(NMHDR* pNMHDR, LRESULT* pResult) 
{
  NMTVCUSTOMDRAW* pCustomDraw = (NMTVCUSTOMDRAW*) pNMHDR;
  switch (pCustomDraw->nmcd.dwDrawStage) 
  {
    case CDDS_PREPAINT:
    {
      *pResult = CDRF_NOTIFYITEMDRAW; //Tell the control that we are interested in item notifications
      break;
		}	
    case CDDS_ITEMPREPAINT:
    {
      //Check to see if this item is compressed and if it it is, change its
      //color just like explorer does
      if (m_bShowCompressedUsingDifferentColor && 
          ((pCustomDraw->nmcd.uItemState & CDIS_SELECTED) == 0) && 
          IsCompressed((HTREEITEM) pCustomDraw->nmcd.dwItemSpec))
        pCustomDraw->clrText = m_rgbCompressed;
      //also check for encrypted files
      else if (m_bShowEncryptedUsingDifferentColor && 
          ((pCustomDraw->nmcd.uItemState & CDIS_SELECTED) == 0) && 
          IsEncrypted((HTREEITEM) pCustomDraw->nmcd.dwItemSpec))
        pCustomDraw->clrText = m_rgbEncrypted;

      //Let it know that we want post paint notifications
      *pResult = CDRF_NOTIFYPOSTPAINT;

      break;
    }
    case CDDS_ITEMPOSTPAINT:
    {
      HTREEITEM hItem = (HTREEITEM) pCustomDraw->nmcd.dwItemSpec;

      CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hItem);
      ASSERT(pItem);
      if (pItem->m_pNetResource)
      {
        //Determine if we should custom draw the bitmap
        BOOL bDrawIcon = FALSE;
        int nIndexToDraw = -1;
        switch (pItem->m_pNetResource->dwDisplayType)
        {
          case RESOURCEDISPLAYTYPE_DOMAIN:
          {
            bDrawIcon = TRUE;
            nIndexToDraw = 0;
            break;
          }
          case RESOURCEDISPLAYTYPE_SERVER:
          {
            bDrawIcon = TRUE;
            nIndexToDraw = 3;
            break;
          }
          case RESOURCEDISPLAYTYPE_SHARE:
          {
            //Deliberately do nothing
            break;
          }
          case RESOURCEDISPLAYTYPE_ROOT:
          {
            bDrawIcon = TRUE;
            nIndexToDraw = 2;
            break;
          }
          default:
          { 
            bDrawIcon = TRUE;
            nIndexToDraw = 1;
            break;
          }
        }

        if (bDrawIcon)
        {  
          //Draw the icon of the tree view item using the specified bitmap
          CDC dc;
          dc.Attach(pCustomDraw->nmcd.hdc);

          //First work out the position of the icon
          CRect rItemRect;
          if (GetItemRect(hItem, rItemRect, FALSE))
		  {
            CPoint point(rItemRect.left, rItemRect.top);
            UINT nFlags = 0;
            CPoint testPoint(pCustomDraw->nmcd.rc.left, rItemRect.top+2);
            BOOL bFound = FALSE;
            do
            {
              HitTest(testPoint, &nFlags);  

              //Prepare for the next time around
              bFound  = (nFlags & TVHT_ONITEMICON) || (nFlags & TVHT_ONITEMSTATEICON);
              if (!bFound)
                testPoint.x++;
            }
            while (!bFound);
            point.x = testPoint.x;
            CRect r(point.x, point.y, point.x+16, point.y+16);

            //Draw it using the IL
            dc.FillSolidRect(&r, RGB(255, 255, 255));
            m_ilNetwork.Draw(&dc, nIndexToDraw, point, ILD_NORMAL);
		  }

          //Release the DC
          dc.Detach();
        }
      }

      *pResult = CDRF_DODEFAULT;
      break;
    }
    default:
    {
      break;
    }
  }
  
  return TRUE; //Allow the message to be reflected again
}

BOOL CTreeFileCtrl::OnSelChanged(NMHDR* pNMHDR, LRESULT* pResult) 
{
	NM_TREEVIEW* pNMTreeView = (NM_TREEVIEW*)pNMHDR;

  //Nothing selected
  if (pNMTreeView->itemNew.hItem == NULL)
    return FALSE;

  //Check to see if the current item is valid, if not then delete it (Exclude network items from this check)
  CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(pNMTreeView->itemNew.hItem);
  ASSERT(pItem);
  CString sPath = pItem->m_sFQPath;
  if ((pNMTreeView->itemNew.hItem != m_hNetworkRoot) && (pItem->m_pNetResource == NULL) && 
      (pNMTreeView->itemNew.hItem != m_hMyComputerRoot) && !IsDrive(sPath) && (GetFileAttributes(sPath) == 0xFFFFFFFF))
  {
    //Before we delete it see if we are the only child item
    HTREEITEM hParent = GetParentItem(pNMTreeView->itemNew.hItem);

    //Delete the item
    DeleteItem(pNMTreeView->itemNew.hItem);

    //Remove all the child items from the parent
    SetHasPlusButton(hParent, FALSE);

    *pResult = 1;

    return FALSE; //Allow the message to be reflected again
  }

  //Add to the prev array the item we were just at
  if (pNMTreeView->itemOld.hItem && !m_bUpdatingHistorySelection)
  {
    if (m_PrevItems.GetSize() > m_nMaxHistory)
      m_PrevItems.RemoveAt(0);
    m_PrevItems.Add(pNMTreeView->itemOld.hItem);
  }

  //Remeber the serial number for this item (if it is a drive)
  if (IsDrive(sPath))
  {
    int nDrive = sPath.GetAt(0) - _T('A');
    GetSerialNumber(sPath, m_dwMediaID[nDrive]); 
  }

  //call the virtual function
  OnSelectionChanged(pNMTreeView, sPath);
	
	*pResult = 0;

  return FALSE; //Allow the message to be reflected again
}

void CTreeFileCtrl::OnSelectionChanged(NM_TREEVIEW*, const CString&)
{
}

BOOL CTreeFileCtrl::OnEndLabelEdit(NMHDR* pNMHDR, LRESULT* pResult) 
{
	TV_DISPINFO* pDispInfo = (TV_DISPINFO*)pNMHDR;
  if (pDispInfo->item.pszText)
  {
    SHFILEOPSTRUCT shfo;
    ZeroMemory(&shfo, sizeof(SHFILEOPSTRUCT));
    shfo.hwnd = AfxGetMainWnd()->GetSafeHwnd();
    shfo.wFunc = FO_RENAME;
    shfo.fFlags = FOF_ALLOWUNDO;

    //Work out the "From" string
    CString sFrom = ItemToPath(pDispInfo->item.hItem);
    int nFromLength = sFrom.GetLength();
    TCHAR* pszFrom = new TCHAR[nFromLength + 2];
    _tcscpy(pszFrom, sFrom);
    pszFrom[nFromLength+1] = _T('\0');
    shfo.pFrom = pszFrom;
    HTREEITEM hParent = GetParentItem(pDispInfo->item.hItem);
    CString sParent = ItemToPath(hParent);

    //Work out the "To" string
    CString sTo;
    CString sToRelative(pDispInfo->item.pszText);
    if (IsDrive(sParent))
      sTo = sParent + pDispInfo->item.pszText;
    else
      sTo = sParent + _T("\\") + pDispInfo->item.pszText;
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(pDispInfo->item.hItem);
    ASSERT(pItem);
    if (pItem->m_bExtensionHidden)
    {
      TCHAR szExt[_MAX_EXT];
      _tsplitpath(sFrom, NULL, NULL, NULL, szExt);
      sTo += szExt;
      sToRelative += szExt;
    }
    size_t nToLength = _tcslen(sTo);
    TCHAR* pszTo = new TCHAR[nToLength + 2];
    _tcscpy(pszTo, sTo);
    pszTo[nToLength+1] = _T('\0');
    shfo.pTo = pszTo;

    BOOL bOldAutoRefresh = m_bAutoRefresh;
    m_bAutoRefresh = FALSE; //Prevents us from getting thread notifications

    //Let the shell perform the actual rename
    if (SHFileOperation(&shfo) == 0 && shfo.fAnyOperationsAborted == FALSE)
    {
      *pResult = TRUE;

      //Update its text  
      SetItemText(pDispInfo->item.hItem, pDispInfo->item.pszText);

      //Update the item data
      pItem->m_sFQPath = sTo;
      pItem->m_sRelativePath = sToRelative;

      //Also update the icons for it (if need be)
      if (!pItem->m_bExtensionHidden)
      {
        CString sPath = ItemToPath(pDispInfo->item.hItem);
        SetItemImage(pDispInfo->item.hItem, GetIconIndex(sPath), GetSelIconIndex(sPath));
      }
    }

    m_bAutoRefresh = bOldAutoRefresh;

    //Don't forget to free up the memory we allocated
    delete [] pszFrom;
    delete [] pszTo;
  }
	
	*pResult = 0;

  return FALSE; //Allow the message to be reflected again
}

BOOL CTreeFileCtrl::PreTranslateMessage(MSG* pMsg) 
{
  // When an item is being edited make sure the edit control
  // receives certain important key strokes
  if (GetEditControl())
  {
    ::TranslateMessage(pMsg);
    ::DispatchMessage(pMsg);
    return TRUE; // DO NOT process further
  }

  //Context menu via the keyboard
	if ((((pMsg->message == WM_KEYDOWN || pMsg->message == WM_SYSKEYDOWN) &&   // If we hit a key and
    	(pMsg->wParam == VK_F10) && (GetKeyState(VK_SHIFT) & 0x8000)) != 0) || // it's Shift+F10 OR
		  (pMsg->message == WM_CONTEXTMENU))						                   	     // Natural keyboard key
	{
		CRect rect;
		GetItemRect(GetSelectedItem(), rect, TRUE);
		ClientToScreen(rect);
		OnContextMenu(NULL, rect.CenterPoint());
		return TRUE;
	}
  //Hitting the Escape key, Cancelling drag & drop
	else if (pMsg->message == WM_KEYDOWN && pMsg->wParam == VK_ESCAPE && IsDragging())
  {
    EndDragging(TRUE);
    return TRUE;
  }
  //Hitting the Alt-Enter key combination, show the properties sheet 
	else if (pMsg->message == WM_SYSKEYDOWN && pMsg->wParam == VK_RETURN)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_PROPERTIES);
    return TRUE;
  }
  //Hitting the Enter key, open the item
	else if (pMsg->message == WM_KEYDOWN && pMsg->wParam == VK_RETURN)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_OPEN);
    return TRUE;
  }
  //Hitting the delete key, delete the item
  else if (pMsg->message == WM_KEYDOWN && pMsg->wParam == VK_DELETE)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_DELETE);
    return TRUE;
  }
  //hitting the backspace key, go to the parent folder
  else if (pMsg->message == WM_KEYDOWN && pMsg->wParam == VK_BACK)
  {
    UpOneLevel();
    return TRUE;
  }
  //hitting the F2 key, being in-place editing of an item
  else if (pMsg->message == WM_KEYDOWN && pMsg->wParam == VK_F2)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_RENAME);
    return TRUE;
  }
  //hitting the F5 key, force a refresh of the whole tree
  else if (pMsg->message == WM_KEYDOWN && pMsg->wParam == VK_F5)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_REFRESH);
    return TRUE;
  }
  //Hitting the Alt-Left Arrow key combination, move to the previous item
	else if (pMsg->message == WM_SYSKEYDOWN && pMsg->wParam == VK_LEFT)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_BACK);
    return TRUE;
  }
  //Hitting the Alt-Right Arrow key combination, move to the next item
	else if (pMsg->message == WM_SYSKEYDOWN && pMsg->wParam == VK_RIGHT)
  {
    PostMessage(WM_COMMAND, ID_TREEFILECTRL_FORWARD);
    return TRUE;
  }

  //Let the parent class do its thing
	return CTreeCtrl::PreTranslateMessage(pMsg);
}

void CTreeFileCtrl::OnUpOneLevel() 
{
  HTREEITEM hItem = GetSelectedItem();
  if (hItem)
  {
    HTREEITEM hParent = GetParentItem(hItem);
    if (hParent)
      Select(hParent, TVGN_CARET);
  }
}

void CTreeFileCtrl::UpOneLevel()
{
  SendMessage(WM_COMMAND, ID_TREEFILECTRL_UPONELEVEL);
}

void CTreeFileCtrl::OnUpdateUpOneLevel(CCmdUI* pCmdUI)
{
  HTREEITEM hItem = GetSelectedItem();
  if (hItem)
    pCmdUI->Enable(GetParentItem(hItem) != NULL);
  else
    pCmdUI->Enable(FALSE);
}

BOOL CTreeFileCtrl::IsFile(HTREEITEM hItem)
{
  return IsFile(ItemToPath(hItem));
}

BOOL CTreeFileCtrl::IsFolder(HTREEITEM hItem)
{
  return IsFolder(ItemToPath(hItem));
}

BOOL CTreeFileCtrl::IsDrive(HTREEITEM hItem)
{
  return IsDrive(ItemToPath(hItem));
}

BOOL CTreeFileCtrl::DriveHasRemovableMedia(const CString& sPath)
{
  BOOL bRemovableMedia = FALSE;
  if (IsDrive(sPath))
  {
    UINT nDriveType = GetDriveType(sPath);
    bRemovableMedia = ((nDriveType == DRIVE_REMOVABLE) ||
                       (nDriveType == DRIVE_CDROM)); 
  }

  return bRemovableMedia;
}

BOOL CTreeFileCtrl::IsCompressed(HTREEITEM hItem)
{
  return IsCompressed(ItemToPath(hItem));
}

BOOL CTreeFileCtrl::IsEncrypted(HTREEITEM hItem)
{
  return IsEncrypted(ItemToPath(hItem));
}

BOOL CTreeFileCtrl::IsFile(const CString& sPath)
{
  DWORD dwAttributes = GetFileAttributes(sPath);
  return ((dwAttributes != 0xFFFFFFFF) && ((dwAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0));
}

BOOL CTreeFileCtrl::IsFolder(const CString& sPath)
{
  DWORD dwAttributes = GetFileAttributes(sPath);
  return ((dwAttributes != 0xFFFFFFFF) && (dwAttributes & FILE_ATTRIBUTE_DIRECTORY));
}

BOOL CTreeFileCtrl::IsDrive(const CString& sPath)
{
  return (sPath.GetLength() == 3 && sPath.GetAt(1) == _T(':') && sPath.GetAt(2) == _T('\\'));
}

BOOL CTreeFileCtrl::IsCompressed(const CString& sPath)
{
  BOOL bCompressed = FALSE;
  if (!IsDrive(sPath))
  {
    DWORD dwAttributes = GetFileAttributes(sPath);
    bCompressed = ((dwAttributes != 0xFFFFFFFF) && (dwAttributes & FILE_ATTRIBUTE_COMPRESSED));
  }

  return bCompressed;
}

BOOL CTreeFileCtrl::IsEncrypted(const CString& sPath)
{
  BOOL bEncrypted = FALSE;
  if (!IsDrive(sPath))
  {
    DWORD dwAttributes = GetFileAttributes(sPath);
    bEncrypted = ((dwAttributes != 0xFFFFFFFF) && (dwAttributes & FILE_ATTRIBUTE_ENCRYPTED));
  }

  return bEncrypted;
}


BOOL CTreeFileCtrl::HasGotSubEntries(const CString& sDirectory)
{
  ASSERT(sDirectory.GetLength());

  if (DriveHasRemovableMedia(sDirectory))
  {
    return TRUE; //we do not bother searching for files on drives 
                 //which have removable media as this would cause 
                 //the drive to spin up, which for the case of a 
                 //floppy is annoying
  }
  else
  {
    //First check to see if there is any sub directories  
    CFileFind find1;
    CString sFile;
    if (sDirectory.GetAt(sDirectory.GetLength()-1) == _T('\\'))
      sFile = sDirectory + _T("*.*");
    else
      sFile = sDirectory + _T("\\*.*");
    BOOL bFind = find1.FindFile(sFile);  
    while (bFind)
    {
      bFind = find1.FindNextFile();
      if (CanDisplayFolder(find1))
        return TRUE;
    }

    //Now check to see if there is any files of the specfied file mask  
    CFileFind find2;
    if (sDirectory.GetAt(sDirectory.GetLength()-1) == _T('\\'))
      sFile = sDirectory + m_sFileNameMask;
    else
      sFile = sDirectory + _T("\\") + m_sFileNameMask;
    bFind = find2.FindFile(sFile);  
    while (bFind)
    {
      bFind = find2.FindNextFile();
      if (CanDisplayFile(find2))
        return TRUE;
    }
  }

  return FALSE;
}

void CTreeFileCtrl::PopulateTree()
{
  ASSERT(GetSafeHwnd()); //Should only call this function after the creation of it on screen

  //attach the image list to the tree control
  SetImageList(&theSystemImageList.m_ImageList, TVSIL_NORMAL);

  //Force a refresh
  Refresh();
}

void CTreeFileCtrl::CreateMonitoringThread(const CString& sPath)
{
  //Setup the structure we will be passing to the thread function
  CTreeFileCtrlThreadInfo* pInfo = new CTreeFileCtrlThreadInfo;
  pInfo->m_sPath = sPath;
  int nLength = pInfo->m_sPath.GetLength();
  ASSERT(nLength);
  if (nLength && pInfo->m_sPath.GetAt(nLength-1) != _T('\\'))
    pInfo->m_sPath += _T('\\');
  pInfo->m_pTree = this;

  TRACE(_T("Creating monitoring thread for %s\n"), pInfo->m_sPath);

  CWinThread* pThread = AfxBeginThread(MonitoringThread, pInfo, THREAD_PRIORITY_IDLE, 0, CREATE_SUSPENDED);
  ASSERT(pThread);
  pThread->m_bAutoDelete = FALSE;
  pInfo->m_pThread = pThread;

  //Add the info struct to the thread array
  int nIndex = m_ThreadInfo.Add(pInfo);
  m_ThreadInfo.GetAt(nIndex)->m_nIndex = nIndex;

  //Resume the thread now that everything is ready to go
  pThread->ResumeThread();
}

UINT CTreeFileCtrl::MonitoringThread(LPVOID pParam)
{
  //Validate our parameters
  ASSERT(pParam);
  CTreeFileCtrlThreadInfo* pInfo = (CTreeFileCtrlThreadInfo*) pParam;
  ASSERT(pInfo->m_pTree);

  //Form the notification flag to use
  DWORD dwNotifyFilter = FILE_NOTIFY_CHANGE_DIR_NAME;
  if (pInfo->m_pTree->m_bShowFiles)
    dwNotifyFilter |= FILE_NOTIFY_CHANGE_FILE_NAME;

  //Get a handle to a file change notification object
  HANDLE hChange = ::FindFirstChangeNotification(pInfo->m_sPath, TRUE, dwNotifyFilter);
  if (hChange != INVALID_HANDLE_VALUE)
  {
    HANDLE handles[2];
    handles[0] = hChange;
    handles[1] = pInfo->m_TerminateEvent.m_hObject;

    //Sleep until a file change notification wakes this thread or m_TerminateEvent becomes
    //set indicating it's time for the thread to end
    BOOL bContinue = TRUE;
    while (bContinue)
    {
      if (::WaitForMultipleObjects(2, handles, FALSE, INFINITE) - WAIT_OBJECT_0 == 0)
      {
        //Respond to the change notification by posting a user defined message 
        //back to the GUI thread
        if (!pInfo->m_pTree->m_bAutoRefresh)
          bContinue = FALSE;
        else
          pInfo->m_pTree->PostMessage(WM_TREEUPDATE_CHANGE, (WPARAM) pInfo->m_nIndex);
        
        //Move onto the next notification
        ::FindNextChangeNotification(hChange);
      }
      else
      {
        //Kill the thread
        bContinue = FALSE;
      }
    }

    //Close the handle we have open
    ::FindCloseChangeNotification(hChange);
  }

  return 0;
}

BOOL CTreeFileCtrl::GetSerialNumber(const CString& sDrive, DWORD& dwSerialNumber)
{
  return GetVolumeInformation(sDrive, NULL, 0, &dwSerialNumber, NULL, NULL, NULL, 0);
}

BOOL CTreeFileCtrl::IsMediaValid(const CString& sDrive)
{
  //return TRUE if the drive does not support removable media
  UINT nDriveType = GetDriveType(sDrive);
  if ((nDriveType != DRIVE_REMOVABLE) && (nDriveType != DRIVE_CDROM))
    return TRUE;

  //Return FALSE if the drive is empty (::GetVolumeInformation fails)
  DWORD dwSerialNumber;
  int nDrive = sDrive.GetAt(0) - _T('A');
  if (GetSerialNumber(sDrive, dwSerialNumber))
    m_dwMediaID[nDrive] = dwSerialNumber;
  else
  {
    m_dwMediaID[nDrive] = 0xFFFFFFFF;
    return FALSE;
  }

  //Also return FALSE if the disk's serial number has changed
  if ((m_dwMediaID[nDrive] != dwSerialNumber) &&
      (m_dwMediaID[nDrive] != 0xFFFFFFFF))
  {
    m_dwMediaID[nDrive] = 0xFFFFFFFF;
    return FALSE;
  }

  return TRUE;
}

BOOL CTreeFileCtrl::EnumNetwork(HTREEITEM hParent)
{
  //What will be the return value from this function
	BOOL bGotChildren = FALSE;

	//Check if the item already has a network resource and use it.
  CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hParent);
  ASSERT(pItem);
	NETRESOURCE* pNetResource = pItem->m_pNetResource;

	//Setup for the network enumeration
	HANDLE hEnum;      
	DWORD dwResult = WNetOpenEnum(pNetResource ? RESOURCE_GLOBALNET : RESOURCE_CONTEXT, m_dwNetworkItemTypes,
      								          0, pNetResource ? pNetResource : NULL, &hEnum);

	//Was the read sucessful
	if (dwResult != NO_ERROR)      
	{
		TRACE(_T("Cannot enumerate network drives, Error:%d\n"), dwResult);
		return FALSE;
	} 
	
  //Do the network enumeration
	DWORD cbBuffer = 16384;

  BOOL bNeedMoreMemory = TRUE;
  BOOL bSuccess = FALSE;
  LPNETRESOURCE lpnrDrv = NULL;
	DWORD cEntries = 0;      
  while (bNeedMoreMemory && !bSuccess)
  {
    //Allocate the memory and enumerate
  	lpnrDrv = (LPNETRESOURCE) new BYTE[cbBuffer];
    cEntries = 0xFFFFFFFF;
		dwResult = WNetEnumResource(hEnum, &cEntries, lpnrDrv, &cbBuffer);

    if (dwResult == ERROR_MORE_DATA)
		{            
      //Free up the heap memory we have used
		  delete [] lpnrDrv;      

      cbBuffer *= 2;
		}
    else if (dwResult == NO_ERROR)
      bSuccess = TRUE;
    else
      bNeedMoreMemory = FALSE;
	}

  //Enumeration successful?
  if (bSuccess)
  {
		//Scan through the results
		for (DWORD i=0; i<cEntries; i++)            
		{
			CString sNameRemote = lpnrDrv[i].lpRemoteName;
			if (sNameRemote.IsEmpty())
				sNameRemote = lpnrDrv[i].lpComment;

			//Remove leading back slashes 
			if (sNameRemote.GetLength() > 0 && sNameRemote[0] == _T('\\'))
				sNameRemote = sNameRemote.Mid(1);
			if (sNameRemote.GetLength() > 0 && sNameRemote[0] == _T('\\'))
				sNameRemote = sNameRemote.Mid(1);

      //Setup the item data for the new item
      CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
			pItem->m_pNetResource = new NETRESOURCE;
      ZeroMemory(pItem->m_pNetResource, sizeof(NETRESOURCE));
			*pItem->m_pNetResource = lpnrDrv[i];
      if (lpnrDrv[i].lpLocalName)
				pItem->m_pNetResource->lpLocalName	= _tcsdup(lpnrDrv[i].lpLocalName);
      if (lpnrDrv[i].lpRemoteName)
				pItem->m_pNetResource->lpRemoteName = _tcsdup(lpnrDrv[i].lpRemoteName);
      if (lpnrDrv[i].lpComment)
				pItem->m_pNetResource->lpComment	= _tcsdup(lpnrDrv[i].lpComment);
      if (lpnrDrv[i].lpProvider)
				pItem->m_pNetResource->lpProvider	= _tcsdup(lpnrDrv[i].lpProvider);
      if (lpnrDrv[i].lpRemoteName)
        pItem->m_sFQPath = lpnrDrv[i].lpRemoteName;
      else
        pItem->m_sFQPath = sNameRemote;
      pItem->m_sRelativePath = sNameRemote;
      pItem->m_bNetworkNode = TRUE;

			//Display a share or the appropiate icon
			if (lpnrDrv[i].dwDisplayType == RESOURCEDISPLAYTYPE_SHARE)
			{
				//Display only the share name
				int nPos = pItem->m_sRelativePath.Find(_T('\\'));
				if (nPos >= 0)
					pItem->m_sRelativePath = pItem->m_sRelativePath.Mid(nPos+1);

        //Now add the item into the control
        if (CanDisplayNetworkItem(pItem))
          InsertFileItem(hParent, pItem, m_bShowSharedUsingDifferentIcon, GetIconIndex(pItem->m_sFQPath), 
                         GetSelIconIndex(pItem->m_sFQPath), TRUE);
        else
          delete pItem;
			}
			else
			{
        //Now add the item into the control
        if (CanDisplayNetworkItem(pItem))
          InsertFileItem(hParent, pItem, FALSE, 0, 0, FALSE);  //Indexes for the network icons do not matter here as we will be drawing over them in OnCustomDraw
        else
          delete pItem;
			}
			bGotChildren = TRUE;
		}
  }
  else
	  TRACE(_T("Cannot complete network drive enumeration, Error:%d\n"), dwResult);

	//Clean up the enumeration handle
	WNetCloseEnum(hEnum);   

  //Free up the heap memory we have used
	delete [] lpnrDrv;      

  //Return whether or not we added any items
	return bGotChildren;
}

void CTreeFileCtrl::DisplayDrives(HTREEITEM hParent, BOOL bUseSetRedraw)
{
  CWaitCursor c;

  //Speed up the job by turning off redraw
  if (bUseSetRedraw)
    SetRedraw(FALSE);

  //Enumerate the drive letters and add them to the tree control
  DWORD dwDrives = GetLogicalDrives();
  DWORD dwMask = 1;
  for (int i=0; i<32; i++)
  {
    if (dwDrives & dwMask)
    {
      CString sDrive;
      sDrive.Format(_T("%c:\\"), i + _T('A'));

      //check if this drive is one of the types to hide
      if (CanDisplayDrive(sDrive))
      {
        CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
        pItem->m_sFQPath = sDrive;
        pItem->m_sRelativePath = sDrive;

        //Insert the item into the view
        InsertFileItem(hParent, pItem, m_bShowSharedUsingDifferentIcon && IsShared(sDrive), GetIconIndex(sDrive), GetSelIconIndex(sDrive), TRUE);
      }
    }
    dwMask <<= 1;
  }

  if (bUseSetRedraw)
    SetRedraw(TRUE);
}

CString CTreeFileCtrl::GetDriveLabel(const CString& sDrive)
{
  USES_CONVERSION;

  //Let's start with the drive letter
  CString sLabel(sDrive);

  //Try to find the item directory using ParseDisplayName
  LPITEMIDLIST lpItem;
  HRESULT hr = m_pShellFolder->ParseDisplayName(NULL, NULL, T2W((LPTSTR) (LPCTSTR)sDrive), NULL, &lpItem, NULL);
  if (SUCCEEDED(hr))
  {
    SHFILEINFO sfi;
    if (SHGetFileInfo((LPCTSTR)lpItem, 0, &sfi, sizeof(sfi), SHGFI_PIDL | SHGFI_DISPLAYNAME))
      sLabel = sfi.szDisplayName;

    //Free the pidl now that we are finished with it
    m_pMalloc->Free(lpItem);
  }

  return sLabel;
}

BOOL CTreeFileCtrl::IsShared(const CString& sPath)
{
  //Defer all the work to the share enumerator class
  return theSharedEnumerator.IsShared(sPath);
}

void CTreeFileCtrl::DisplayPath(const CString& sPath, HTREEITEM hParent, BOOL bUseSetRedraw)
{
  CWaitCursor c;

  //Speed up the job by turning off redraw
  if (bUseSetRedraw)
    SetRedraw(FALSE);

  //Remove all the items currently under hParent
  HTREEITEM hChild = GetChildItem(hParent);
  while (hChild)
  {
    DeleteItem(hChild);
    hChild = GetChildItem(hParent);
  }

  //Should we display the root folder
  if (m_bShowRootedFolder && (hParent == TVI_ROOT)) 
  {
    CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
    pItem->m_sFQPath = m_sRootFolder;
    pItem->m_sRelativePath = m_sRootFolder;
    m_hRootedFolder = InsertFileItem(TVI_ROOT, pItem, FALSE, GetIconIndex(m_sRootFolder), GetSelIconIndex(m_sRootFolder), TRUE);
    Expand(m_hRootedFolder, TVE_EXPAND);
    return;
  }

  //First find all the directories underneath sPath
  CSortedArray<CString, CString&> DirectoryPaths;
  CFileFind find1;
  CString sFile;
  if (sPath.GetAt(sPath.GetLength()-1) == _T('\\'))
    sFile = sPath + _T("*.*");
  else
    sFile = sPath + _T("\\*.*");
  BOOL bFind = find1.FindFile(sFile);  
  while (bFind)
  {
    bFind = find1.FindNextFile();
    if (CanDisplayFolder(find1))
    {
      CString sPath = find1.GetFilePath();
      DirectoryPaths.Add(sPath);
    }
  }

  //Now check to see if there is any files of the specfied file mask  
  CSortedArray<CString, CString&> FilePaths;
  CFileFind find2;
  if (sPath.GetAt(sPath.GetLength()-1) == _T('\\'))
    sFile = sPath + m_sFileNameMask;
  else
    sFile = sPath + _T("\\") + m_sFileNameMask;
  bFind = find2.FindFile(sFile);  
  while (bFind)
  {
    bFind = find2.FindNextFile();
    if (CanDisplayFile(find2))
    {
      CString sPath = find2.GetFilePath();
      FilePaths.Add(sPath);
    }  
  }

  //Now sort the 2 arrays prior to added to the tree control
  DirectoryPaths.SetCompareFunction(CompareByFilenameNoCase);
  FilePaths.SetCompareFunction(CompareByFilenameNoCase);
  DirectoryPaths.Sort();
  FilePaths.Sort();

  //Now add all the directories to the tree control
  int nDirectories = DirectoryPaths.GetSize();
  for (int i=0; i<nDirectories; i++)
  {
    CString& sPath = DirectoryPaths.ElementAt(i);
    TCHAR szPath[_MAX_PATH];
    TCHAR szFname[_MAX_FNAME];
    TCHAR szExt[_MAX_EXT];
    _tsplitpath(sPath, NULL, NULL, szFname, szExt);
    _tmakepath(szPath, NULL, NULL, szFname, szExt);

    CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
    pItem->m_sFQPath = sPath;
    pItem->m_sRelativePath = szPath;
    InsertFileItem(hParent, pItem, m_bShowSharedUsingDifferentIcon && IsShared(sPath), GetIconIndex(sPath), GetSelIconIndex(sPath), TRUE);
  }

  //And the files to the tree control (if required)
  int nFiles = FilePaths.GetSize();
  int ii;
  for (ii=0; ii<nFiles; ii++)
  {
    CString& sPath = FilePaths.ElementAt(ii);
    TCHAR szPath[_MAX_PATH];
    TCHAR szFname[_MAX_FNAME];
    TCHAR szExt[_MAX_EXT];
    _tsplitpath(sPath, NULL, NULL, szFname, szExt);
    _tmakepath(szPath, NULL, NULL, szFname, szExt);

    CTreeFileCtrlItemInfo* pItem = new CTreeFileCtrlItemInfo;
    pItem->m_sFQPath = sPath;
    pItem->m_sRelativePath = szPath;
    InsertFileItem(hParent, pItem, FALSE, GetIconIndex(sPath), GetSelIconIndex(sPath), TRUE);
  }

  //If no items were added then remove the "+" indicator from hParent
  if (nFiles == 0 && nDirectories == 0)
    SetHasPlusButton(hParent, FALSE);

  //Turn back on the redraw flag
  if (bUseSetRedraw)
    SetRedraw(TRUE);
}

void CTreeFileCtrl::DoExpand(HTREEITEM hItem)
{
  CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hItem);
  ASSERT(pItem);

  //Reset the drive node if the drive is empty or the media has changed
  if (IsMediaValid(pItem->m_sFQPath))
  {
    //Delete the item if the path is no longer valid
    if (IsFolder(pItem->m_sFQPath))
    {
      //Add the new items to the tree if it does not have any child items
      //already
      if (!GetChildItem(hItem))
        DisplayPath(pItem->m_sFQPath, hItem);

      //Create a thread to monitor file changes
      if (m_bAutoRefresh && IsDrive(pItem->m_sFQPath))
        CreateMonitoringThread(pItem->m_sFQPath);
    }
    else if (hItem == m_hMyComputerRoot)
    {
      //Display an hour glass as this may take some time
      CWaitCursor wait;

      //Enumerate the local drive letters
      DisplayDrives(m_hMyComputerRoot, FALSE);
    }
    else if ((hItem == m_hNetworkRoot) || (pItem->m_pNetResource))
    {
      //Display an hour glass as this may take some time
      CWaitCursor wait;

      //Enumerate the network resources
      EnumNetwork(hItem);
    }
    else
    {
      //Before we delete it see if we are the only child item
      HTREEITEM hParent = GetParentItem(hItem);

      //Delete the item
      DeleteItem(hItem);

      //Remove all the child items from the parent
      SetHasPlusButton(hParent, FALSE);
    }
  }
  else
  {
    //Display an hour glass as this may take some time
    CWaitCursor wait;

    //Collapse the drive node and remove all the child items from it
    Expand(hItem, TVE_COLLAPSE);
    DeleteChildren(hItem, TRUE);
  }
}

BOOL CTreeFileCtrl::OnItemExpanding(NMHDR* pNMHDR, LRESULT* pResult) 
{
	NM_TREEVIEW* pNMTreeView = (NM_TREEVIEW*)pNMHDR;
  if (pNMTreeView->action == TVE_EXPAND)
  {
    BOOL bHasPlus = HasPlusButton(pNMTreeView->itemNew.hItem);
    BOOL bHasChildren = (GetChildItem(pNMTreeView->itemNew.hItem) != NULL);

    if (bHasPlus && !bHasChildren)
      DoExpand(pNMTreeView->itemNew.hItem);
  }
  else if (pNMTreeView->action == TVE_COLLAPSE)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(pNMTreeView->itemNew.hItem);
    ASSERT(pItem);

    //Display an hour glass as this may take some time
    CWaitCursor wait;

    CString sPath = ItemToPath(pNMTreeView->itemNew.hItem);
    if (IsDrive(sPath))
      KillNotificationThread(sPath);

    //Collapse the node and remove all the child items from it
    Expand(pNMTreeView->itemNew.hItem, TVE_COLLAPSE);

    //Never uppdate the child indicator for a network node which is not a share

    BOOL bUpdateChildIndicator = TRUE;
    if (pItem->m_bNetworkNode)
    {
      if (pItem->m_pNetResource)
        bUpdateChildIndicator = (pItem->m_pNetResource->dwDisplayType == RESOURCEDISPLAYTYPE_SHARE);
      else
        bUpdateChildIndicator = FALSE;                              
    }  
    DeleteChildren(pNMTreeView->itemNew.hItem, bUpdateChildIndicator);
  }

  *pResult = 0;

  return FALSE; //Allow the message to be reflected again
}

int CTreeFileCtrl::DeleteChildren(HTREEITEM hItem, BOOL bUpdateChildIndicator)
{
  int nCount = 0;
  HTREEITEM hChild = GetChildItem(hItem);
  while (hChild)
  {
    //Get the next sibling before we delete the current one
    HTREEITEM hNextItem = GetNextSiblingItem(hChild);

    //Delete the current child
    DeleteItem(hChild);

    //Get ready for the next loop
    hChild = hNextItem;
    ++nCount;
  }

  //Also update its indicator to suggest that their is children
  if (bUpdateChildIndicator)
    SetHasPlusButton(hItem, (nCount != 0));

  return nCount;
}

HTREEITEM CTreeFileCtrl::PathToItem(const CString& sPath) const
{
  CString sSearch(sPath);
  int nSearchLength = sSearch.GetLength();
  if (nSearchLength == 0)
    return NULL;

  //Remove trailing "\" from the path
  if (nSearchLength > 3 && sSearch.GetAt(nSearchLength-1) == _T('\\'))
    sSearch = sSearch.Left(nSearchLength-1);
  
  //Remove initial part of path if the root folder is setup
  int nRootLength = m_sRootFolder.GetLength();
  if (nRootLength)
  {
    if (sSearch.Find(m_sRootFolder) != 0)
    {
      TRACE(_T("Could not find the path %s as the root has been configued as %s\n"), sPath, m_sRootFolder);
      return NULL;
    }
    sSearch = sSearch.Right(sSearch.GetLength() - 1 - nRootLength);
  }

  if (sSearch.IsEmpty())
    return NULL;

  HTREEITEM hItemFound = TVI_ROOT;
  if (nRootLength && m_hRootedFolder)
    hItemFound = m_hRootedFolder;
  BOOL bDriveMatch = m_sRootFolder.IsEmpty();
  BOOL bNetworkMatch = m_bDisplayNetwork && ((sSearch.GetLength() > 2) && sSearch.Find(_T("\\\\")) == 0);
  if (bNetworkMatch)
  {
    bDriveMatch = FALSE;
    hItemFound = FindServersNode(m_hNetworkRoot);
    sSearch = sSearch.Right(sSearch.GetLength() - 2);
  }
  if (bDriveMatch)
  {
    if (m_hMyComputerRoot)
      hItemFound = m_hMyComputerRoot;
  }
  int nFound = sSearch.Find(_T('\\'));
  while (nFound != -1)
  {
    CString sMatch;
    if (bDriveMatch)
    {
      sMatch = sSearch.Left(nFound + 1);
      bDriveMatch = FALSE;
    }
    else
      sMatch = sSearch.Left(nFound);
    hItemFound = FindSibling(hItemFound, sMatch);
    if (hItemFound == NULL)
      break;

    sSearch = sSearch.Right(sSearch.GetLength() - nFound - 1);
    nFound = sSearch.Find(_T('\\'));
  };

  //The last item 
  if (hItemFound)
  {
    if (sSearch.GetLength())
      hItemFound = FindSibling(hItemFound, sSearch);
  }

  return hItemFound;
}

CString CTreeFileCtrl::ItemToPath(HTREEITEM hItem) const
{
  CString sPath;
  if (hItem)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) GetItemData(hItem);
    ASSERT(pItem);
    sPath = pItem->m_sFQPath;
  }
  return sPath;
}

BOOL CTreeFileCtrl::OnRclick(NMHDR* /*pNMHDR*/, LRESULT* pResult) 
{
  //Work out the position of where the context menu should be
  CPoint p(GetCurrentMessage()->pt);
  CPoint pt(p);
  ScreenToClient(&pt);
  Select(HitTest(pt), TVGN_CARET);
	OnContextMenu(NULL, p);

	*pResult = 0;

  return FALSE; //Allow the message to be reflected again
}

BOOL CTreeFileCtrl::OnDblclk(NMHDR* /*pNMHDR*/, LRESULT* pResult) 
{
  HTREEITEM hItem = GetSelectedItem();
  CPoint pt = GetCurrentMessage()->pt;
  ScreenToClient(&pt);

	if (hItem && (hItem == HitTest(pt)))
	{
		if (!HasPlusButton(hItem))
			PostMessage(WM_COMMAND, ID_TREEFILECTRL_OPEN);
	}
	
	*pResult = 0;

  return FALSE; //Allow the message to be reflected again
}

//Copied from CFrameWnd::OnInitMenuPopup to provide OnUpdateCmdUI functionality
//in the tree control
void CTreeFileCtrl::OnInitMenuPopup(CMenu* pMenu, UINT /*nIndex*/, BOOL bSysMenu) 
{
	//AfxCancelModes(m_hWnd);

	if (bSysMenu)
		return;     // don't support system menu

	ASSERT(pMenu != NULL);
	// check the enabled state of various menu items

	CCmdUI state;
	state.m_pMenu = pMenu;
	ASSERT(state.m_pOther == NULL);
	ASSERT(state.m_pParentMenu == NULL);

	// determine if menu is popup in top-level menu and set m_pOther to
	//  it if so (m_pParentMenu == NULL indicates that it is secondary popup)
	HMENU hParentMenu;
	if (AfxGetThreadState()->m_hTrackingMenu == pMenu->m_hMenu)
		state.m_pParentMenu = pMenu;    // parent == child for tracking popup
	else if ((hParentMenu = ::GetMenu(m_hWnd)) != NULL)
	{
		CWnd* pParent = GetTopLevelParent();
			// child windows don't have menus -- need to go to the top!
		if (pParent != NULL &&
			(hParentMenu = ::GetMenu(pParent->m_hWnd)) != NULL)
		{
			int nIndexMax = ::GetMenuItemCount(hParentMenu);
			for (int nIndex = 0; nIndex < nIndexMax; nIndex++)
			{
				if (::GetSubMenu(hParentMenu, nIndex) == pMenu->m_hMenu)
				{
					// when popup is found, m_pParentMenu is containing menu
					state.m_pParentMenu = CMenu::FromHandle(hParentMenu);
					break;
				}
			}
		}
	}

	state.m_nIndexMax = pMenu->GetMenuItemCount();
	for (state.m_nIndex = 0; state.m_nIndex < state.m_nIndexMax;
	  state.m_nIndex++)
	{
		state.m_nID = pMenu->GetMenuItemID(state.m_nIndex);
		if (state.m_nID == 0)
			continue; // menu separator or invalid cmd - ignore it

		ASSERT(state.m_pOther == NULL);
		ASSERT(state.m_pMenu != NULL);
		if (state.m_nID == (UINT)-1)
		{
			// possibly a popup menu, route to first item of that popup
			state.m_pSubMenu = pMenu->GetSubMenu(state.m_nIndex);
			if (state.m_pSubMenu == NULL ||
				(state.m_nID = state.m_pSubMenu->GetMenuItemID(0)) == 0 ||
				state.m_nID == (UINT)-1)
			{
				continue;       // first item of popup can't be routed to
			}
			state.DoUpdate(this, FALSE);    // popups are never auto disabled
		}
		else
		{
			// normal menu item
			// Auto enable/disable if frame window has 'm_bAutoMenuEnable'
			//    set and command is _not_ a system command.
			state.m_pSubMenu = NULL;
			//state.DoUpdate(this, m_bAutoMenuEnable && state.m_nID < 0xF000);
      state.DoUpdate(this, TRUE && state.m_nID < 0xF000);
		}

		// adjust for menu deletions and additions
		UINT nCount = pMenu->GetMenuItemCount();
		if (nCount < state.m_nIndexMax)
		{
			state.m_nIndex -= (state.m_nIndexMax - nCount);
			while (state.m_nIndex < nCount &&
				pMenu->GetMenuItemID(state.m_nIndex) == state.m_nID)
			{
				state.m_nIndex++;
			}
		}
		state.m_nIndexMax = nCount;
	}
}

BOOL CTreeFileCtrl::OnBeginDrag(NMHDR* pNMHDR, LRESULT* pResult) 
{
	NM_TREEVIEW* pNMTreeView = (NM_TREEVIEW*)pNMHDR;
	*pResult = 0;

  if (!m_bAllowDragDrop || !IsDropSource(pNMTreeView->itemNew.hItem))
    return FALSE; //Allow the message to be reflected again

  m_pilDrag = CreateDragImage(pNMTreeView->itemNew.hItem);
  if (!m_pilDrag)
    return FALSE; //Allow the message to be reflected again

  m_hItemDrag = pNMTreeView->itemNew.hItem;
  m_hItemDrop = NULL;

  // Calculate the offset to the hotspot
  CPoint offsetPt(8,8);   // Initialize a default offset

  CPoint dragPt = pNMTreeView->ptDrag;    // Get the Drag point
  UINT nHitFlags = 0;
  HTREEITEM htiHit = HitTest(dragPt, &nHitFlags);
  if (htiHit != NULL)
  {
    // The drag point has Hit an item in the tree
    CRect itemRect;
    if (GetItemRect(htiHit, &itemRect, FALSE))
    {
      // Count indent levels
      HTREEITEM htiParent = htiHit;
      int nIndentCnt = 0;
      while (htiParent != NULL)
      {
        htiParent = GetParentItem(htiParent);
        nIndentCnt++;
      }

      if (!(GetStyle() & TVS_LINESATROOT)) 
        nIndentCnt--;

      // Calculate the new offset
      offsetPt.y = dragPt.y - itemRect.top;
      offsetPt.x = dragPt.x - (nIndentCnt * GetIndent()) + GetScrollPos(SB_HORZ);
    }
  }

  //Begin the dragging  
  m_pilDrag->BeginDrag(0, offsetPt);
  POINT pt = pNMTreeView->ptDrag;
  ClientToScreen(&pt);
  m_pilDrag->DragEnter(NULL, pt);
  SetCapture();

  //Create the timer which is used for auto expansion
  m_nTimerID = SetTimer(1, 300, NULL);

  return FALSE; //Allow the message to be reflected again
}

void CTreeFileCtrl::OnMouseMove(UINT nFlags, CPoint point) 
{
	if (IsDragging())
  {
    CRect clientRect;
    GetClientRect(&clientRect);

    //Draw the drag
    POINT pt = point;
    ClientToScreen(&pt);
    CImageList::DragMove(pt);

    //Only select the drop item if we are in the client area
    HTREEITEM hItem = NULL;
    if (clientRect.PtInRect(point))
    {
      UINT flags;
      hItem = HitTest(point, &flags);
      if (m_hItemDrop != hItem)
      {
        CImageList::DragShowNolock(FALSE);
        SelectDropTarget(hItem);
        m_hItemDrop = hItem;
        CImageList::DragShowNolock(TRUE);
      }
    }
    
    if (hItem)
      hItem = GetDropTarget(hItem);

    //Change the cursor to give feedback
    if (hItem)
    {
      if ((GetKeyState(VK_CONTROL) & 0x8000))
        SetCursor(m_DropCopyCursor);
      else
        SetCursor(m_DropMoveCursor);
    }
    else
    {
      if ((GetKeyState(VK_CONTROL) & 0x8000))
        SetCursor(m_NoDropCopyCursor);
      else
        SetCursor(m_NoDropMoveCursor);
    }
  }

  //Let the parent class do its thing	
	CTreeCtrl::OnMouseMove(nFlags, point);
}

HTREEITEM CTreeFileCtrl::GetDropTarget(HTREEITEM hItem)
{
  if (!IsFile(hItem) && (hItem != m_hItemDrag) && (hItem != GetParentItem(m_hItemDrag)) && IsFolder(hItem))
  {
    HTREEITEM htiParent = hItem;
    while ((htiParent = GetParentItem(htiParent)) != NULL)
    {
      if (htiParent == m_hItemDrag)
        return NULL;
    }
    return hItem;
  }
  return NULL;
}

int CTreeFileCtrl::NumberOfChildItems(HTREEITEM hItem)
{
  int nChildren = 0;
  HTREEITEM hChild = GetChildItem(hItem);
  while (hChild)
  {
    ++nChildren;
    hChild = GetNextSiblingItem(hChild);
  }
  return nChildren;
}

BOOL CTreeFileCtrl::IsDropSource(HTREEITEM hItem)
{
  return !IsDrive(hItem) && IsFile(hItem);
}

BOOL CTreeFileCtrl::IsDragging()
{
  return (m_pilDrag != NULL);
}

void CTreeFileCtrl::OnLButtonUp(UINT nFlags, CPoint point) 
{
  CRect clientRect;
  GetClientRect(&clientRect);

  if (clientRect.PtInRect(point))
    EndDragging(FALSE);
  else
    EndDragging(TRUE);
	 
  //Let the parent class do its thing	
	CTreeCtrl::OnLButtonUp(nFlags, point);
}

void CTreeFileCtrl::EndDragging(BOOL bCancel)
{
  if (IsDragging())
  {
    //Kill the timer that is being used
    KillTimer(m_nTimerID);

    CImageList::DragLeave(this);
    CImageList::EndDrag();
    ReleaseCapture();

    //Delete the drag image list
    delete m_pilDrag;
    m_pilDrag = NULL;

    //Remove drop target highlighting
    SelectDropTarget(NULL);

    //Find out where we are dropping
    m_hItemDrop = GetDropTarget(m_hItemDrop);
    if (m_hItemDrop == NULL)
      return;

    if (!bCancel)
    {
      //Also need to make the change on disk
      CString sFromPath = ItemToPath(m_hItemDrag);
      CString sToPath = ItemToPath(m_hItemDrop);

      int nFromLength = sFromPath.GetLength();
      int nToLength = sToPath.GetLength();
      SHFILEOPSTRUCT shfo;
      ZeroMemory(&shfo, sizeof(SHFILEOPSTRUCT));
      shfo.hwnd = GetSafeHwnd();

      if ((GetKeyState(VK_CONTROL) & 0x8000))
        shfo.wFunc = FO_COPY;
      else
        shfo.wFunc = FO_MOVE;

      shfo.fFlags = FOF_SILENT | FOF_NOCONFIRMMKDIR;
      //Undo is not allowed if the SHIFT key is held down
      if (!(GetKeyState(VK_SHIFT) & 0x8000))
        shfo.fFlags |= FOF_ALLOWUNDO;

      TCHAR* pszFrom = new TCHAR[nFromLength + 2];
      _tcscpy(pszFrom, sFromPath);
      pszFrom[nFromLength+1] = _T('\0');
      shfo.pFrom = pszFrom;

      TCHAR* pszTo = new TCHAR[nToLength + 2];
      _tcscpy(pszTo, sToPath);
      pszTo[nToLength+1] = _T('\0');
      shfo.pTo = pszTo;

      BOOL bOldAutoRefresh = m_bAutoRefresh;
      m_bAutoRefresh = FALSE; //Prevents us from getting thread notifications

      //Let the shell perform the actual deletion
      BOOL bSuccess = ((SHFileOperation(&shfo) == 0) && (shfo.fAnyOperationsAborted == FALSE));

      m_bAutoRefresh = bOldAutoRefresh;

      //Free up the memory we had allocated
      delete [] pszFrom;
      delete [] pszTo;

      if (bSuccess)
      {
        //Only copy the item in the tree if there is not an item with the same
        //text under m_hItemDrop
        CString sText = GetItemText(m_hItemDrag);
        if (!HasChildWithText(m_hItemDrop, sText))
        {
          Expand(m_hItemDrop, TVE_COLLAPSE);
          DeleteChildren(m_hItemDrop, FALSE);
          SelectItem(m_hItemDrop);

          //Update the children indicator for the folder we just dropped into
          SetHasPlusButton(m_hItemDrop, TRUE);
        }

        if (shfo.wFunc == FO_MOVE)
        {
          //Get the parent of the item we moved prior to deleting it
          HTREEITEM hParent = GetParentItem(m_hItemDrag);

          //Delete the item we just moved
          DeleteItem(m_hItemDrag);

          //Update the children indicator for the item we just dragged from
          BOOL bHasChildren = (GetChildItem(hParent) != NULL);
          if (hParent && !bHasChildren)
            SetHasPlusButton(hParent, FALSE);
        }
      }
    }
  }
}

BOOL CTreeFileCtrl::HasChildWithText(HTREEITEM hParent, const CString& sText)
{
  HTREEITEM hChild = GetChildItem(hParent);
  while (hChild)
  {
    CString sItemText = GetItemText(hChild);
    if (sItemText.CompareNoCase(sText) == 0)
      return TRUE;
    hChild = GetNextSiblingItem(hChild);
  }
  return FALSE;
}

HTREEITEM CTreeFileCtrl::CopyItem(HTREEITEM hItem, HTREEITEM htiNewParent, HTREEITEM htiAfter)
{
  //Get the details of the item to copy
  TV_INSERTSTRUCT tvstruct;
  tvstruct.item.hItem = hItem;
  tvstruct.item.mask = TVIF_CHILDREN | TVIF_HANDLE | TVIF_IMAGE | TVIF_SELECTEDIMAGE | TVIF_PARAM;
  GetItem(&tvstruct.item);
  CString sText = GetItemText(hItem);
  tvstruct.item.cchTextMax = sText.GetLength();
  tvstruct.item.pszText = sText.GetBuffer(tvstruct.item.cchTextMax);

  //Make a copy of the item data we are carying around
  CTreeFileCtrlItemInfo* pOldInfo = (CTreeFileCtrlItemInfo*) tvstruct.item.lParam;
  tvstruct.item.lParam = (LPARAM) new CTreeFileCtrlItemInfo(*pOldInfo);

  //Insert the item at the proper location
  tvstruct.hParent = htiNewParent;
  tvstruct.hInsertAfter = htiAfter;
  tvstruct.item.mask |= TVIF_TEXT;
  HTREEITEM hNewItem = InsertItem(&tvstruct);

  //Don't forget to release the CString buffer  
  sText.ReleaseBuffer();

  return hNewItem;
}

HTREEITEM CTreeFileCtrl::CopyBranch(HTREEITEM htiBranch, HTREEITEM htiNewParent, HTREEITEM htiAfter)
{
  HTREEITEM hNewItem = CopyItem(htiBranch, htiNewParent, htiAfter);
  HTREEITEM hChild = GetChildItem(htiBranch);
  while (hChild != NULL)
  {
    //recursively transfer all the items
    CopyBranch(hChild, hNewItem);
    hChild = GetNextSiblingItem(hChild);
  }
  return hNewItem;
}

void CTreeFileCtrl::OnTimer(UINT_PTR nIDEvent) 
{
	if (nIDEvent != m_nTimerID)
  {
	  CTreeCtrl::OnTimer(nIDEvent);
    return;
  }

  //Show the dragging effect
  POINT pt;
  GetCursorPos(&pt);
  RECT rect;
  GetClientRect(&rect);
  ClientToScreen(&rect);
  CImageList::DragMove(pt);

  HTREEITEM hFirstItem = GetFirstVisibleItem();
  CRect ItemRect;
  GetItemRect(hFirstItem, &ItemRect, FALSE);
  if (pt.y < (rect.top + (ItemRect.Height()*2)) && pt.y > rect.top)
  {
    //we need to scroll up
    CImageList::DragShowNolock(FALSE);
    SendMessage(WM_VSCROLL, SB_LINEUP);
    EnsureVisible(hFirstItem);
    SelectDropTarget(hFirstItem);
    m_hItemDrop = hFirstItem;
    CImageList::DragShowNolock(TRUE);
  }
  else if (pt.y > (rect.bottom - (ItemRect.Height()*2)) && pt.y < rect.bottom)
  {
    //we need to scroll down
    CImageList::DragShowNolock(FALSE);
    SendMessage(WM_VSCROLL, SB_LINEDOWN);
    HTREEITEM hLastItem = hFirstItem;
    int nCount = GetVisibleCount();
    for (int i=0; i<(nCount-1); i++)
      hLastItem = GetNextVisibleItem(hLastItem);
    SelectDropTarget(hLastItem);
    EnsureVisible(hLastItem);
    m_hItemDrop = hLastItem;
    CImageList::DragShowNolock(TRUE);
  }

  //Expand the item if the timer ticks has expired
  if (m_TimerTicks == 3)
  {
    m_TimerTicks = 0;
    Expand(m_hItemDrop, TVE_EXPAND);
  }

  //Expand the selected item if it is collapsed and
  //the timeout has occurred
  TV_ITEM tvItem;
  tvItem.hItem = m_hItemDrop;
  tvItem.mask = TVIF_HANDLE | TVIF_CHILDREN | TVIF_STATE;
  tvItem.stateMask = TVIS_EXPANDED;
  GetItem(&tvItem);
  if (tvItem.cChildren && ((tvItem.state & TVIS_EXPANDED) == 0))
  {
    m_TimerTicks++;
  }
}

void CTreeFileCtrl::OnBack() 
{
  int nSize = m_PrevItems.GetSize();
  if (nSize)
  {
    HTREEITEM hOldItem = GetSelectedItem();
    HTREEITEM hNewItem = (HTREEITEM) m_PrevItems.GetAt(nSize - 1);

    //Select the previous item
    m_bUpdatingHistorySelection = TRUE;
    m_PrevItems.RemoveAt(nSize - 1);
    SelectItem(hNewItem);
    EnsureVisible(hNewItem);
    m_bUpdatingHistorySelection = FALSE;

    //Add the old item to the next stack
    m_NextItems.Add(hOldItem);
  }
}

void CTreeFileCtrl::OnUpdateBack(CCmdUI* pCmdUI) 
{
	pCmdUI->Enable(CanGoBack());
}

void CTreeFileCtrl::OnForward() 
{
  int nSize = m_NextItems.GetSize();
  if (nSize)
  {
    HTREEITEM hOldItem = GetSelectedItem();
    HTREEITEM hNewItem = (HTREEITEM) m_NextItems.GetAt(nSize - 1);

    //Select the previous item
    m_bUpdatingHistorySelection = TRUE;
    m_NextItems.RemoveAt(nSize - 1);
    SelectItem(hNewItem);
    EnsureVisible(hNewItem);
    m_bUpdatingHistorySelection = FALSE;

    //Add the old item to the prev stack
    m_PrevItems.Add(hOldItem);
  }
}

void CTreeFileCtrl::OnUpdateForward(CCmdUI* pCmdUI) 
{
	pCmdUI->Enable(CanGoForward());	
}

BOOL CTreeFileCtrl::GoBack()
{
  BOOL bSuccess = FALSE;
  if (m_PrevItems.GetSize())
  {
    SendMessage(WM_COMMAND, ID_TREEFILECTRL_BACK);
    bSuccess = TRUE;
  }
  return bSuccess;
}

BOOL CTreeFileCtrl::GoForward()
{
  BOOL bSuccess = FALSE;
  if (m_NextItems.GetSize())
  {
    SendMessage(WM_COMMAND, ID_TREEFILECTRL_FORWARD);
    bSuccess = TRUE;
  }
  return bSuccess;
}

void CTreeFileCtrl::SetMaxHistory(int nMaxHistory)
{
  m_nMaxHistory = nMaxHistory;

  //Shrink the prev array if necessary
  INT_PTR nCurItems = m_PrevItems.GetSize();
  if (nCurItems > m_nMaxHistory)
  {
    int nItemsToDelete = nCurItems - m_nMaxHistory;
    for (int i=0; i<nItemsToDelete; i++)
      m_PrevItems.RemoveAt(nCurItems - i - 1);
  }

  //Shrink the next array if necessary
  nCurItems = m_NextItems.GetSize();
  if (nCurItems > m_nMaxHistory)
  {
    int nItemsToDelete = nCurItems - m_nMaxHistory;
    for (int i=0; i<nItemsToDelete; i++)
      m_NextItems.RemoveAt(nCurItems - i - 1);
  }
}

int CTreeFileCtrl::GetBackSize() const
{
  return m_PrevItems.GetSize();
}

CString CTreeFileCtrl::GetBackItemText(int nBack) const
{
  ASSERT(nBack < GetBackSize());
  HTREEITEM hItem = (HTREEITEM) m_PrevItems.GetAt(nBack);
  return ItemToPath(hItem);
}

int CTreeFileCtrl::GetForwardSize() const
{
  return m_NextItems.GetSize();
}

CString CTreeFileCtrl::GetForwardItemText(int nForward) const
{
  ASSERT(nForward < GetForwardSize());
  HTREEITEM hItem = (HTREEITEM) m_NextItems.GetAt(nForward);
  return ItemToPath(hItem);
}

void CTreeFileCtrl::KillNotificationThread(const CString& sPath)
{
	//Kill all the running file change notification threads
  int nThreads = m_ThreadInfo.GetSize();
  for (int i=0; i<nThreads; i++)
  {
    CTreeFileCtrlThreadInfo* pInfo = m_ThreadInfo.GetAt(i);
    if (pInfo->m_sPath.CompareNoCase(sPath) == 0)
    {
      TRACE(_T("Killing monitoring thread for %s\n"), sPath);

      //Signal the worker thread to exit and wait for it to return
      pInfo->m_TerminateEvent.SetEvent();
      WaitForSingleObject(pInfo->m_pThread->m_hThread, INFINITE);

      delete pInfo;
      m_ThreadInfo.RemoveAt(i);
      return;
    }
  }
}

void CTreeFileCtrl::KillNotificationThreads()
{
	//Kill all the running file change notification threads
  int nThreads = m_ThreadInfo.GetSize();
  if (nThreads)
  {
    HANDLE* pThreads = new HANDLE[nThreads];
    for (int i=0; i<nThreads; i++)
    {
      CTreeFileCtrlThreadInfo* pInfo = m_ThreadInfo.GetAt(i);
      pThreads[i] = pInfo->m_pThread->m_hThread;
      pInfo->m_TerminateEvent.SetEvent();
    }

    //wait for the threads to exit
    ::WaitForMultipleObjects(nThreads, pThreads, TRUE, INFINITE);

    //Free up all the objects we have
    delete [] pThreads;
	
    for (int i=0; i<nThreads; i++)
      delete m_ThreadInfo.GetAt(i);
    m_ThreadInfo.RemoveAll();

    //Reset the event
    m_TerminateEvent.ResetEvent();
  }
}

void CTreeFileCtrl::OnDestroy() 
{
  KillNotificationThreads();

  //Remove all the items from the tree control 
  //This ensures that all the heap memory we
  //have used in the item datas is freed
  Clear();

  //Let the parent class do its thing
	CTreeCtrl::OnDestroy();
}

LRESULT CTreeFileCtrl::OnChange(WPARAM wParam, LPARAM /*lParam*/)
{
  //Return immediately if auto refresh is turned of
  if (!m_bAutoRefresh)
    return 0L;

  //Validate our parameters
  CTreeFileCtrlThreadInfo* pInfo = m_ThreadInfo.GetAt(wParam);
  ASSERT(pInfo);

  //Trace message which is helpful for diagnosing autorefresh
  TRACE(_T("Refreshing %s due to change\n"), pInfo->m_sPath), 

  SetRedraw(FALSE);

  //Remember what was selected
  HTREEITEM hSelItem = GetSelectedItem();
  CString sItem;
  BOOL bExpanded = FALSE;
  if (hSelItem)
  {
    sItem  = ItemToPath(hSelItem);
    bExpanded = IsExpanded(hSelItem); 
  }

  //Cause the redisplay
  HTREEITEM hItem = PathToItem(pInfo->m_sPath);
  DisplayPath(pInfo->m_sPath, hItem, TRUE);

  //Reselect the initially selected item
  if (sItem.GetLength())
    hSelItem = SetSelectedPath(sItem, bExpanded);

  //Turn back on the redraw flag
  SetRedraw(TRUE);

  return 0L;
}

void CTreeFileCtrl::SetAutoRefresh(BOOL bAutoRefresh) 
{
  //Since it can be touched by more than one thead
  InterlockedExchange((LPLONG) &m_bAutoRefresh, bAutoRefresh); 

  Refresh(); //Force the monitoring threads to be recreated
}

void CTreeFileCtrl::CollapseExpandBranch( HTREEITEM hti, int nAction)
{
  if (ItemHasChildren(hti))
  {
    Expand(hti, nAction);
    hti = GetChildItem(hti);                
    while (hti)
    {
      CollapseExpandBranch(hti, nAction);
      hti = GetNextSiblingItem(hti);
    }
  }
}

void CTreeFileCtrl::Collapseall() 
{
  HTREEITEM hti = GetRootItem();        
  while (hti)
  {
    CollapseExpandBranch(hti, TVE_COLLAPSE);
    hti = GetNextSiblingItem(hti);
  }   
}

void CTreeFileCtrl::Expandall() 
{
  HTREEITEM hti = GetRootItem();        
  while (hti)
  {
    CollapseExpandBranch(hti, TVE_EXPAND);
    hti = GetNextSiblingItem(hti);
  }
}

void CTreeFileCtrl::Clear()
{
  //Delete all the items
  DeleteAllItems();

  //Reset the member variables we have
  m_hMyComputerRoot = NULL;
  m_hNetworkRoot = NULL;
  m_hRootedFolder = NULL;
}

void CTreeFileCtrl::PreSubclassWindow() 
{
  //Let the base class do its thing
	CTreeCtrl::PreSubclassWindow();

  //Get a pointer to IShellFolder and IMalloc
  ASSERT(m_pShellFolder == NULL);
  VERIFY(SUCCEEDED(SHGetDesktopFolder(&m_pShellFolder)));
  VERIFY(SUCCEEDED(SHGetMalloc(&m_pMalloc)));

  //Load up the cursors we need
  CWinApp* pApp = AfxGetApp();
  ASSERT(pApp);
  m_NoDropCopyCursor = pApp->LoadCursor(IDR_TREEFILECTRL_NO_DROPCOPY);
  VERIFY(m_NoDropCopyCursor);
  m_DropCopyCursor = pApp->LoadCursor(IDR_TREEFILECTRL_DROPCOPY);
  VERIFY(m_DropCopyCursor);
  m_NoDropMoveCursor = pApp->LoadCursor(IDR_TREEFILECTRL_NO_DROPMOVE);
  VERIFY(m_NoDropMoveCursor);
  m_DropMoveCursor = pApp->LoadStandardCursor(IDC_ARROW);
  VERIFY(m_DropMoveCursor);

  //Load up the bitmaps used to supplement the system image list
  VERIFY(m_ilNetwork.Create(IDB_TREEFILECTRL_NETWORK, 16, 1, RGB(255, 0, 255)));
}

BOOL CTreeFileCtrl::OnDeleteItem(NMHDR* pNMHDR, LRESULT* pResult) 
{
	NM_TREEVIEW* pNMTreeView = (NM_TREEVIEW*)pNMHDR;
  if (pNMTreeView->itemOld.hItem != TVI_ROOT)
  {
    CTreeFileCtrlItemInfo* pItem = (CTreeFileCtrlItemInfo*) pNMTreeView->itemOld.lParam;
    if (pItem->m_pNetResource)
    {
      free(pItem->m_pNetResource->lpLocalName);
      free(pItem->m_pNetResource->lpRemoteName);
      free(pItem->m_pNetResource->lpComment);
      free(pItem->m_pNetResource->lpProvider);
      delete pItem->m_pNetResource;
    }
    delete pItem;
  }

	*pResult = 0;

  return FALSE; //Allow the message to be reflected again
}

void DDX_FileTreeValue(CDataExchange* pDX, CTreeFileCtrl& ctrlFileTree, CString& sItem)
{
  if (pDX->m_bSaveAndValidate)
    sItem = ctrlFileTree.GetSelectedPath();
  else
    ctrlFileTree.SetSelectedPath(sItem);
}


