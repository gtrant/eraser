// ErasextMenu.h
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

#ifndef ERASEXTMENU_H
#define ERASEXTMENU_H

/////////////////////////////////////////////////////////////////////////////
// CErasextMenu command target

class CErasextMenu : public CCmdTarget
{
    DECLARE_DYNCREATE(CErasextMenu)

    CErasextMenu();           // protected constructor used by dynamic creation

// Attributes
public:

// Operations
public:
    BOOL MoveFileList(CWnd *pParent, CStringArray& saList, CStringArray& saFolders,
                      CStringList& strlSource, LPCTSTR szDestination);

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CErasextMenu)
    public:
    virtual void OnFinalRelease();
    //}}AFX_VIRTUAL

// Implementation
private:
	void getstr_handle_erase(UINT nType, CString& );
	void getstr_handle_move(UINT nType, CString& );
protected:
    BOOL        m_bNT;
    DWORD       m_dwItems;
    DWORD       m_dwDirectories;
    BOOL        m_bUseFiles;

    BOOL        m_bDragMenu;
    TCHAR       m_szDropTarget[MAX_PATH + 2];
	
    CStringArray m_saData;
    CStringArray m_saFolders;

    virtual ~CErasextMenu();


    // Generated message map functions
    //{{AFX_MSG(CErasextMenu)
        // NOTE - the ClassWizard will add and remove member functions here.
    //}}AFX_MSG

    DECLARE_MESSAGE_MAP()
    // Generated OLE dispatch map functions
    //{{AFX_DISPATCH(CErasextMenu)
        // NOTE - the ClassWizard will add and remove member functions here.
    //}}AFX_DISPATCH
    DECLARE_DISPATCH_MAP()
    DECLARE_INTERFACE_MAP()

    DECLARE_OLECREATE(CErasextMenu)

    // IContextMenu Interface 
	BEGIN_INTERFACE_PART(MenuExt, IContextMenu)
        STDMETHOD(QueryContextMenu)(HMENU hMenu, UINT nIndex, UINT idCmdFirst,
            UINT idCmdLast, UINT uFlags);
        STDMETHOD(InvokeCommand)(LPCMINVOKECOMMANDINFO lpici);
        STDMETHOD(GetCommandString)(UINT_PTR  idCmd, UINT nType, UINT* pnReserved,
            LPSTR lpszName, UINT nMax);
    END_INTERFACE_PART(MenuExt)

    // IShellExtInit interface
    BEGIN_INTERFACE_PART(ShellInit, IShellExtInit)
        STDMETHOD(Initialize)(LPCITEMIDLIST pidlFolder, LPDATAOBJECT lpdobj,
            HKEY hkeyProgID);
    END_INTERFACE_PART(ShellInit)
};


/////////////////////////////////////////////////////////////////////////////

#endif
