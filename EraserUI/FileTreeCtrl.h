/*
Module : FileTreeCtrl.h
Purpose: Interface for an MFC class which provides a tree control similiar 
         to the left hand side of explorer

Copyright (c) 1999 - 2001 by PJ Naughter.  (Web: www.naughter.com, Email: pjna@naughter.com)

All rights reserved.

Copyright / Usage Details:

You are allowed to include the source code in any product (commercial, shareware, freeware or otherwise) 
when your product is released in binary form. You are allowed to modify the source code in any way you want 
except you cannot modify the copyright details at the top of each module. If you want to distribute source 
code with your application, then you are only allowed to distribute versions released by the author. This is 
to maintain a single distribution point for the source code. 

*/



////////////////////////////////// Macros / Defines  ///////////////////////

#ifndef __FILETREECTRL_H__
#define __FILETREECTRL_H__



#ifndef __AFXMT_H__
#pragma message("To avoid this message, put afxmt.h in your PCH (normally stdafx.h)")
#include <afxmt.h>
#endif

#ifndef _SHLOBJ_H_
#pragma message("To avoid this message, put shlobj.h in your PCH (normally stdafx.h)")
#include <shlobj.h>
#endif

#ifndef _LM_
#pragma message("To avoid this message, put lm.h in your PCH (normally stdafx.h)")
#include <lm.h>
#endif

#ifndef __AFXMT_H__
#pragma message("To avoid this message, put afxmt.h in your PCH (normally stdafx.h)")
#include <afxmt.h>
#endif



//flags used to control how the DDX_FileTreeControl and SetFlags routine works

const DWORD TFC_SHOWFILES       = 0x0001;   //Control will show files aswell as show folders
const DWORD TFC_ALLOWDRAGDROP   = 0x0002;   //Control allows drag / drop
const DWORD TFC_ALLOWRENAME     = 0x0004;   //Control allows renaming of items
const DWORD TFC_ALLOWOPEN       = 0x0008;   //Control allows items to be "opened" by the shell
const DWORD TFC_ALLOWPROPERTIES = 0x0010;   //Control allows the "Properties" dialog to be shown
const DWORD TFC_ALLOWDELETE     = 0x0020;   //Control allows items to be deleted



//Allowable bit mask flags in SetDriveHideFlags / GetDriveHideFlags

const DWORD DRIVE_ATTRIBUTE_REMOVABLE   = 0x00000001;
const DWORD DRIVE_ATTRIBUTE_FIXED       = 0x00000002;
const DWORD DRIVE_ATTRIBUTE_REMOTE      = 0x00000004;
const DWORD DRIVE_ATTRIBUTE_CDROM       = 0x00000010;
const DWORD DRIVE_ATTRIBUTE_RAMDISK     = 0x00000020;



/////////////////////////// Classes /////////////////////////////////


//Class which gets stored int the item data on the tree control

class CTreeFileCtrlItemInfo
{
public:
//Constructors / Destructors
  CTreeFileCtrlItemInfo();
  CTreeFileCtrlItemInfo(const CTreeFileCtrlItemInfo& ItemInfo);
  ~CTreeFileCtrlItemInfo();

//Member variables
  CString       m_sFQPath;          //Fully qualified path for this item
  CString       m_sRelativePath;    //The relative bit of the path
  NETRESOURCE*  m_pNetResource;     //Used if this item is under Network Neighborhood
  BOOL          m_bNetworkNode;     //Item is "Network Neighborhood" or is underneath it
  BOOL          m_bExtensionHidden; //Is the extension being hidden for this item
};



//Class which encapsulates access to the System image list which contains
//all the icons used by the shell to represent the file system

class CSystemImageList
{
public:
//Constructors / Destructors
  CSystemImageList();
  ~CSystemImageList();

protected:
//Data
  CImageList m_ImageList;          //The MFC image list wrapper
  static int sm_nRefCount;         //Reference count for the imagelist

  friend class CTreeFileCtrl;      //Allow the FileTreeCtrl access to our internals
};




//Struct taken from svrapi.h as we cannot mix Win9x and Win NT net headers in one program
#pragma pack(1)
struct CTreeFile_share_info_50 
{
	char		shi50_netname[LM20_NNLEN+1];    /* share name */
	unsigned char 	shi50_type;                 /* see below */
  unsigned short	shi50_flags;                /* see below */
	char FAR *	shi50_remark;                   /* ANSI comment string */
	char FAR *	shi50_path;                     /* shared resource */
	char		shi50_rw_password[SHPWLEN+1];   /* read-write password (share-level security) */
	char		shi50_ro_password[SHPWLEN+1];   /* read-only password (share-level security) */
};	/* share_info_50 */
#pragma pack()



//class which manages enumeration of shares. This is used for determining 
//if an item is shared or not
class CShareEnumerator
{
public:
//Constructors / Destructors
  CShareEnumerator();
  ~CShareEnumerator();

//Methods
  void Refresh(); //Updates the internal enumeration list
  BOOL IsShared(const CString& sPath);

protected:
//Defines
  typedef NET_API_STATUS (WINAPI NT_NETSHAREENUM)(LPWSTR, DWORD, LPBYTE*, DWORD, LPDWORD, LPDWORD, LPDWORD);
  typedef NET_API_STATUS (WINAPI NT_NETAPIBUFFERFREE)(LPVOID);
  typedef NET_API_STATUS (WINAPI WIN9X_NETSHAREENUM)(const char FAR *, short, char FAR *, unsigned short, unsigned short FAR *, unsigned short FAR *);

//Data
  BOOL                     m_bWinNT;          //Are we running on NT
  HMODULE                  m_hNetApi;         //Handle to the net api dll
  NT_NETSHAREENUM*         m_pNTShareEnum;    //NT function pointer for NetShareEnum
  NT_NETAPIBUFFERFREE*     m_pNTBufferFree;   //NT function pointer for NetAPIBufferFree
  SHARE_INFO_502*          m_pNTShareInfo;    //NT share info
  WIN9X_NETSHAREENUM*      m_pWin9xShareEnum; //Win9x function pointer for NetShareEnum
  CTreeFile_share_info_50* m_pWin9xShareInfo; //Win9x share info
  DWORD                    m_dwShares;        //The number of shares enumerated
};



//Class which is used for passing info to and from the change notification
//threads

class CTreeFileCtrlThreadInfo
{
public:
//Constructors / Destructors
  CTreeFileCtrlThreadInfo();
  ~CTreeFileCtrlThreadInfo();

//Member variables
  CString        m_sPath;          //The path we are monitoring
  CWinThread*    m_pThread;        //The thread pointer
  CTreeFileCtrl* m_pTree;          //The tree control
  int            m_nIndex;         //Index of this item into CTreeFileCtrl::m_ThreadInfo
  CEvent         m_TerminateEvent; //Event using to terminate the thread
};



//Class which implements the tree control representation of the file system

class CTreeFileCtrl : public CTreeCtrl
{
public:
//Enums
enum HideFileExtension
{
  HideExtension,
  DoNoHideExtension,
  UseTheShellSetting
};

//Constructors / Destructors
	CTreeFileCtrl();
	virtual ~CTreeFileCtrl();

//Public methods
  void              SetRootFolder(const CString& sPath);
  CString           GetRootFolder() const { return m_sRootFolder; };
  CString           GetSelectedPath();
  HTREEITEM         SetSelectedPath(const CString& sPath, BOOL bExpanded=FALSE);
  void              SetShowFiles(BOOL bFiles);
  BOOL              GetShowFiles() const { return m_bShowFiles; };
  void              SetDisplayNetwork(BOOL bDisplayNetwork);
  BOOL              GetDisplayNetwork() const { return m_bDisplayNetwork; };
  void              SetUsingDifferentIconForSharedFolders(BOOL bShowSharedUsingDifferentIcon);
  BOOL              GetUsingDifferentIconForSharedFolders() const { return m_bShowSharedUsingDifferentIcon; };
  void              SetAllowDragDrop(BOOL bAllowDragDrop) { m_bAllowDragDrop = bAllowDragDrop; };
  BOOL              GetAllowDragDrop() const { return m_bAllowDragDrop; };
  void              SetAllowRename(BOOL bAllowRename) { m_bAllowRename = bAllowRename; };
  BOOL              GetAllowRename() const { return m_bAllowRename; };
  void              SetAllowOpen(BOOL bAllowOpen) { m_bAllowOpen = bAllowOpen; };
  BOOL              GetAllowOpen() const { return m_bAllowOpen; };
  void              SetAllowProperties(BOOL bAllowProperties) { m_bAllowProperties = bAllowProperties; };
  BOOL              GetAllowProperties() const { return m_bAllowProperties; };
  void              SetAllowDelete(BOOL bAllowDelete) { m_bAllowDelete = bAllowDelete; };
  BOOL              GetAllowDelete() const { return m_bAllowDelete; };
  void              SetFlags(DWORD dwFlags);
  void              SetDriveHideFlags(DWORD dwDriveHideFlags);
  DWORD             GetDriveHideFlags() const { return m_dwDriveHideFlags; };
  void              SetFileHideFlags(DWORD dwFileHideFlags);
  DWORD             GetFileHideFlags() const { return m_dwFileHideFlags; };
  void              SetFileNameMask(const CString& sFileNameMask);
  CString           GetFileNameMask() const { return m_sFileNameMask; };
  BOOL              GetChecked(HTREEITEM hItem) const;
  BOOL              SetChecked(HTREEITEM hItem, BOOL fCheck);
  void              SetNetworkItemTypes(DWORD dwTypes); 
  DWORD             GetNetworkItemTypes() const { return m_dwNetworkItemTypes; };
  void              SetShowDriveLabels(BOOL bShowDriveLabels); 
  BOOL              GetShowDriveLabels() const { return m_bShowDriveLabels; };
  COLORREF          GetCompressedColor() const { return m_rgbCompressed; };
  void              SetCompressedColor(COLORREF rgbCompressed);
  BOOL              GetUsingDifferentColorForCompressed() const { return m_bShowCompressedUsingDifferentColor; };
  void              SetUsingDifferentColorForCompressed(BOOL bShowCompressedUsingDifferentColor);
  COLORREF          GetEncryptedColor() const { return m_rgbEncrypted; };
  void              SetEncryptedColor(COLORREF rgbEncrypted);
  BOOL              GetUsingDifferentColorForEncrypted() const { return m_bShowEncryptedUsingDifferentColor; };
  void              SetUsingDifferentColorForEncrypted(BOOL bShowEncryptedUsingDifferentColor);
  HideFileExtension GetShowFileExtensions() const { return m_FileExtensions; };
  void              SetShowFileExtensions(HideFileExtension FileExtensions);
  BOOL              GetShowMyComputer() const { return m_bShowMyComputer; };
  void              SetShowMyComputer(BOOL bShowMyComputer);
  BOOL              GetShowRootedFolder() const { return m_bShowRootedFolder; };
  void              SetShowRootedFolder(BOOL bShowRootedFolder);
  void              SetAutoRefresh(BOOL bAutoRefresh);
  BOOL              GetAutoRefresh() const { return m_bAutoRefresh; };
  virtual CString   ItemToPath(HTREEITEM hItem) const;
  virtual HTREEITEM PathToItem(const CString& sPath) const;
  virtual BOOL      IsFile(HTREEITEM hItem);
  virtual BOOL      IsFolder(HTREEITEM hItem);
  virtual BOOL      IsDrive(HTREEITEM hItem);
  virtual BOOL      IsCompressed(HTREEITEM hItem);
  virtual BOOL      IsEncrypted(HTREEITEM hItem);
  virtual BOOL      IsShared(const CString& sPath);
  virtual BOOL      IsFile(const CString& sPath);
  virtual BOOL      IsFolder(const CString& sPath);
  virtual BOOL      IsDrive(const CString& sPath);
  virtual BOOL      IsCompressed(const CString& sPath);
  virtual BOOL      IsEncrypted(const CString& sPath);
  virtual BOOL      Rename(HTREEITEM hItem);
  virtual BOOL      ShowProperties(HTREEITEM hItem);
  virtual BOOL      Delete(HTREEITEM hItem);
  virtual BOOL      Open(HTREEITEM hItem);
  virtual void      PopulateTree(); 
  virtual void      UpOneLevel();
  virtual void      Refresh();
  virtual BOOL      GoBack();
  virtual BOOL      CanGoBack() const { return m_PrevItems.GetSize() != 0; };
  virtual BOOL      GoForward();
  virtual BOOL      CanGoForward() const { return m_NextItems.GetSize() != 0; };
  virtual int       GetMaxHistory() const { return m_nMaxHistory; };
  virtual void      SetMaxHistory(int nMaxHistory);
  int               GetBackSize() const;
  CString           GetBackItemText(int nBack) const;
  int               GetForwardSize() const;
  CString           GetForwardItemText(int nForward) const;
  void              CollapseExpandBranch(HTREEITEM hti, int nAction);
  void              Collapseall();
  void              Expandall();
  void              Clear();

//Debug / Assert help
#ifdef _DEBUG
	virtual void AssertValid() const;
	virtual void Dump(CDumpContext& dc) const;
#endif

protected:
	//{{AFX_MSG(CTreeFileCtrl)
	afx_msg void OnProperties();
	afx_msg void OnUpdateProperties(CCmdUI* pCmdUI);
	afx_msg void OnRename();
	afx_msg void OnUpdateRename(CCmdUI* pCmdUI);
	afx_msg void OnOpen();
	afx_msg void OnUpdateOpen(CCmdUI* pCmdUI);
	afx_msg void OnDelete();
	afx_msg void OnUpdateDelete(CCmdUI* pCmdUI);
	afx_msg void OnRefresh();
  afx_msg void OnUpOneLevel();
  afx_msg void OnUpdateUpOneLevel(CCmdUI* pCmdUI);
	afx_msg void OnContextMenu(CWnd* pWnd, CPoint point);
	afx_msg void OnInitMenuPopup(CMenu* pPopupMenu, UINT nIndex, BOOL bSysMenu);
	afx_msg void OnMouseMove(UINT nFlags, CPoint point);
	afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnBack();
	afx_msg void OnUpdateBack(CCmdUI* pCmdUI);
	afx_msg void OnForward();
	afx_msg void OnUpdateForward(CCmdUI* pCmdUI);
	afx_msg void OnDestroy();
	//}}AFX_MSG
	afx_msg BOOL OnDblclk(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg BOOL OnItemExpanding(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg BOOL OnBeginLabelEdit(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg BOOL OnEndLabelEdit(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg BOOL OnRclick(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg BOOL OnBeginDrag(NMHDR* pNMHDR, LRESULT* pResult);
  afx_msg BOOL OnCustomDraw(NMHDR* pNMHDR, LRESULT* pResult);
 	afx_msg BOOL OnSelChanged(NMHDR* pNMHDR, LRESULT* pResult);
  afx_msg BOOL OnDeleteItem(NMHDR* pNMHDR, LRESULT* pResult);
  afx_msg LRESULT OnChange(WPARAM wParam, LPARAM lParam);

	DECLARE_MESSAGE_MAP()

  DECLARE_DYNCREATE(CTreeFileCtrl)

	//{{AFX_VIRTUAL(CTreeFileCtrl)
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	virtual void PreSubclassWindow();
	//}}AFX_VIRTUAL

//Methods
  virtual void      DisplayPath(const CString& sPath, HTREEITEM hParent, BOOL bUseSetRedraw=TRUE);
  virtual void      DisplayDrives(HTREEITEM hParent, BOOL bUseSetRedraw=TRUE);
  virtual int       GetIconIndex(const CString& sFilename);
  virtual int       GetIconIndex(HTREEITEM hItem);
  virtual int       GetSelIconIndex(const CString& sFilename);
  virtual int       GetSelIconIndex(HTREEITEM hItem);
  virtual int       GetIconIndex(LPITEMIDLIST lpPIDL);
  virtual int       GetSelIconIndex(LPITEMIDLIST lpPIDL);
  virtual BOOL      HasGotSubEntries(const CString& sDirectory);
  virtual BOOL      HasChildWithText(HTREEITEM hParent, const CString& sText);
  virtual HTREEITEM InsertFileItem(HTREEITEM hParent, CTreeFileCtrlItemInfo* pItem, BOOL bShared, int nIcon, int nSelIcon, BOOL bCheckForChildren);
  virtual HTREEITEM FindSibling(HTREEITEM hParent, const CString& sItem) const;
  virtual BOOL      DriveHasRemovableMedia(const CString& sPath);
  virtual BOOL      IsDropSource(HTREEITEM hItem);
  BOOL              IsDragging();
  BOOL              IsExpanded(HTREEITEM hItem);
  virtual HTREEITEM GetDropTarget(HTREEITEM hItem);
  void              EndDragging(BOOL bCancel);
  virtual HTREEITEM CopyItem(HTREEITEM hItem, HTREEITEM htiNewParent, HTREEITEM htiAfter = TVI_LAST);
  virtual HTREEITEM CopyBranch(HTREEITEM htiBranch, HTREEITEM htiNewParent, HTREEITEM htiAfter = TVI_LAST);
  virtual BOOL      CanDisplayFile(const CFileFind& find);
  virtual BOOL      CanDisplayFolder(const CFileFind& find);
  virtual BOOL      CanDisplayDrive(const CString& sDrive);
  virtual BOOL      CanDisplayNetworkItem(CTreeFileCtrlItemInfo* pItem);
  virtual BOOL      CanHandleChangeNotifications(const CString& sPath);
  static int        CompareByFilenameNoCase(CString& element1, CString& element2);
  virtual void      CreateMonitoringThread(const CString& sPath);
  static UINT       MonitoringThread(LPVOID pParam);
  virtual void      KillNotificationThreads();
  virtual void      KillNotificationThread(const CString& sPath);
  virtual BOOL      GetSerialNumber(const CString& sDrive, DWORD& dwSerialNumber);
  virtual BOOL      IsMediaValid(const CString& sDrive);
  virtual int       NumberOfChildItems(HTREEITEM hItem);
  virtual int       DeleteChildren(HTREEITEM hItem, BOOL bUpdateChildIndicator);
  virtual void      OnSelectionChanged(NM_TREEVIEW*, const CString&);
  virtual BOOL      EnumNetwork(HTREEITEM hParent);
  virtual CString   GetDriveLabel(const CString& sDrive);
  CString           GetCorrectedLabel(CTreeFileCtrlItemInfo* pItem);
  HTREEITEM         FindServersNode(HTREEITEM hFindFrom) const;
  BOOL              HasPlusButton(HTREEITEM hItem);
  void              SetHasPlusButton(HTREEITEM hItem, BOOL bHavePlus);
  void              DoExpand(HTREEITEM hItem);


//Member variables
  CImageList                                                  m_ilNetwork;          //The image list to use for network items
  CString                                                     m_sRootFolder;
  BOOL                                                        m_bShowFiles;
  HTREEITEM                                                   m_hItemDrag;
  HTREEITEM                                                   m_hItemDrop;
  HTREEITEM                                                   m_hNetworkRoot;
  HTREEITEM                                                   m_hMyComputerRoot;
  HTREEITEM                                                   m_hRootedFolder;
  BOOL                                                        m_bShowMyComputer;
  CImageList*                                                 m_pilDrag;
  UINT                                                        m_nTimerID;
  HCURSOR                                                     m_DropCopyCursor;
  HCURSOR                                                     m_NoDropCopyCursor;
  HCURSOR                                                     m_DropMoveCursor;
  HCURSOR                                                     m_NoDropMoveCursor;
  UINT                                                        m_TimerTicks;
  BOOL                                                        m_bAllowDragDrop;
  BOOL                                                        m_bAllowRename;
  BOOL                                                        m_bAllowOpen;
  BOOL                                                        m_bAllowProperties;
  BOOL                                                        m_bAllowDelete;
  DWORD                                                       m_dwDriveHideFlags;
  DWORD                                                       m_dwFileHideFlags;
  CString                                                     m_sFileNameMask;
  COLORREF                                                    m_rgbCompressed;
  BOOL                                                        m_bShowCompressedUsingDifferentColor;  
  COLORREF                                                    m_rgbEncrypted;
  BOOL                                                        m_bShowEncryptedUsingDifferentColor;  
  CArray<LPVOID, LPVOID>                                      m_PrevItems;
  CArray<LPVOID, LPVOID>                                      m_NextItems;
  int                                                         m_nMaxHistory;            
  BOOL                                                        m_bUpdatingHistorySelection;
  CArray<CTreeFileCtrlThreadInfo*, CTreeFileCtrlThreadInfo*&> m_ThreadInfo;
  CEvent                                                      m_TerminateEvent;
  BOOL                                                        m_bAutoRefresh;
  BOOL                                                        m_bDisplayNetwork;
  BOOL                                                        m_bShowSharedUsingDifferentIcon;
  HideFileExtension                                           m_FileExtensions;
  DWORD                                                       m_dwMediaID[26];
  IMalloc*                                                    m_pMalloc; 
  IShellFolder*                                               m_pShellFolder;
  DWORD                                                       m_dwNetworkItemTypes;
  BOOL                                                        m_bShowDriveLabels;
  BOOL                                                        m_bShowRootedFolder;
};



//MFC Data exchange routines

void DDX_FileTreeValue(CDataExchange* pDX, CTreeFileCtrl& ctrlFileTree, CString& sItem);



#endif //__FILETREECTRL_H__
