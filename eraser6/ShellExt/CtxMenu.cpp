/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
 * Original Author: Kasra Nassiri <cjax@users.sourceforge.net>
 * Modified By: Joel Low <lowjoel@users.sourceforge.net>
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

#include "stdafx.h"
#include "CtxMenu.h"
#include "DllMain.h"
#include <sstream>

extern "C"
{
	typedef LONG NTSTATUS;
	enum KEY_INFORMATION_CLASS
	{
		KeyBasicInformation,
		KeyNodeInformation,
		KeyFullInformation,
		KeyNameInformation,
		KeyCachedInformation,
		KeyVirtualizationInformation
	};

	struct KEY_BASIC_INFORMATION
	{
		LARGE_INTEGER LastWriteTime;
		ULONG TitleIndex;
		ULONG NameLength;
		WCHAR Name[1];
	};

	struct KEY_NODE_INFORMATION
	{
		LARGE_INTEGER LastWriteTime;
		ULONG TitleIndex;
		ULONG ClassOffset;
		ULONG ClassLength;
		ULONG NameLength;
		WCHAR Name[1];
	};

	typedef NTSTATUS (__stdcall *pNtQueryKey)(HANDLE KeyHandle, KEY_INFORMATION_CLASS KeyInformationClass,
		PVOID KeyInformation, ULONG Length, PULONG ResultLength);
	pNtQueryKey NtQueryKey = NULL;
}

template<typename handleType> class Handle
{
public:
	Handle()
	{
		Object = NULL;
	}

	Handle(handleType handle)
	{
		Object = handle;
	}

	~Handle()
	{
		DeleteObject(Object);
	}

	operator handleType&()
	{
		return Object;
	}

private:
	handleType Object;
};

Handle<HICON>::~Handle()
{
	DestroyIcon(Object);
}

Handle<HANDLE>::~Handle()
{
	CloseHandle(Object);
}

Handle<HKEY>::~Handle()
{
	CloseHandle(Object);
}

namespace Eraser {
	HRESULT CCtxMenu::FinalConstruct()
	{
		//Initialise member variables.
		MenuID = 0;
		std::wstring menuTitle(LoadString(IDS_ERASER));
		MenuTitle = new wchar_t[menuTitle.length() + 1];
		wcscpy_s(MenuTitle, menuTitle.length() + 1, menuTitle.c_str());

		//Check if the shell extension has been disabled.
		Handle<HKEY> eraserKey;
		LONG openKeyResult = RegOpenKeyEx(HKEY_CURRENT_USER,
			L"Software\\Eraser\\Eraser 6\\3460478d-ed1b-4ecc-96c9-2ca0e8500557\\", 0,
			KEY_READ, &static_cast<HKEY&>(eraserKey));

		switch (openKeyResult)
		{
		case ERROR_FILE_NOT_FOUND:
			//No settings defined: we default to enabling the shell extension.
			return S_OK;

		case ERROR_SUCCESS:
			break;
			
		default:
			return E_FAIL;
		}

		//Check the value of the IntegrateWithShell value.
		DWORD value = 0;
		DWORD valueType = 0;
		DWORD valueSize = sizeof(value);
		DWORD error = RegQueryValueEx(eraserKey, L"IntegrateWithShell", NULL, &valueType,
			reinterpret_cast<BYTE*>(&value), &valueSize);
		if (error == ERROR_SUCCESS && value == 0)
		{
			return E_FAIL;
		}

		return S_OK;
	}

	HRESULT CCtxMenu::FinalRelease()
	{
		delete[] MenuTitle;
		return S_OK;
	}

	HRESULT CCtxMenu::Initialize(LPCITEMIDLIST pidlFolder, LPDATAOBJECT pDataObj,
	                             HKEY hProgID)
	{
		//Determine where the shell extension was invoked from.
		if (GetHKeyPath(hProgID) == L"{645FF040-5081-101B-9F08-00AA002F954E}")
		{
			InvokeReason = INVOKEREASON_RECYCLEBIN;

			//We can't do much other processing: the LPDATAOBJECT parameter contains
			//data that is a private type so we don't know how to query for it.
			return S_OK;
		}

		//Check pidlFolder for the drop path, if it exists. This is for drag-and-drop
		//context menus.
		else if (pidlFolder != NULL)
		{
			InvokeReason = INVOKEREASON_DRAGDROP;

			//Translate the drop path to a location on the filesystem.
			wchar_t dropTargetPath[MAX_PATH];
			if (!SHGetPathFromIDList(pidlFolder, dropTargetPath))
				return E_FAIL;

			DragDropDestinationDirectory = dropTargetPath;
		}

		//Okay, everything else is a simple context menu for a set of selected files/
		//folders/drives.
		else
			InvokeReason = INVOKEREASON_FILEFOLDER;

		//Look for CF_HDROP data in the data object.
		FORMATETC fmt = { CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
		STGMEDIUM stg = { TYMED_HGLOBAL };
		if (FAILED(pDataObj->GetData(&fmt, &stg)))
			//Nope! Return an "invalid argument" error back to Explorer.
			return E_INVALIDARG;

		//Get a pointer to the actual data.
		HDROP hDrop = static_cast<HDROP>(GlobalLock(stg.hGlobal));
		if (hDrop == NULL)
			return E_INVALIDARG;

		//Sanity check - make sure there is at least one filename.
		UINT uNumFiles = DragQueryFile(hDrop, 0xFFFFFFFF, NULL, 0);
		if (!uNumFiles)
		{
			GlobalUnlock(stg.hGlobal);
			ReleaseStgMedium(&stg);
			return E_INVALIDARG;
		}

		//Collect all the files which have been selected.
		HRESULT hr = S_OK;
		WCHAR buffer[MAX_PATH] = {0};
		for (UINT i = 0; i < uNumFiles; i++)
		{
			UINT charsWritten = DragQueryFile(hDrop, i, buffer, sizeof(buffer) / sizeof(buffer[0]));
			if (!charsWritten)
			{
				hr = E_INVALIDARG;
				continue;
			}

			SelectedFiles.push_back(std::wstring(buffer, charsWritten));
		}

		//Clean up.
		GlobalUnlock(stg.hGlobal);
		ReleaseStgMedium(&stg);
		return hr;
	}

	HRESULT CCtxMenu::QueryContextMenu(HMENU hmenu, UINT uMenuIndex, UINT uidFirstCmd,
	                                   UINT /*uidLastCmd*/, UINT uFlags)
	{
		//First check if we're running on Vista or later
		bool isVistaOrLater = false;
		{
			//Set the bitmap for the registered item. Vista machines will be set using a DIB,
			//older machines will be ownerdrawn.
			OSVERSIONINFO osvi;
			ZeroMemory(&osvi, sizeof(osvi));
			osvi.dwOSVersionInfoSize = sizeof(osvi);

			isVistaOrLater = GetVersionEx(&osvi) && osvi.dwPlatformId == VER_PLATFORM_WIN32_NT &&
				osvi.dwMajorVersion >= 6;
		}

		//If the flags include CMF_DEFAULTONLY then we shouldn't do anything.
		if (uFlags & CMF_DEFAULTONLY)
			return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);

		//First, create and populate a submenu.
		UINT uID = uidFirstCmd;
		HMENU hSubmenu = CreatePopupMenu();

		//Create the submenu, following the order defined in the CEraserLPVERB enum, creating
		//only items which are applicable.
		Actions applicableActions = GetApplicableActions();
		VerbMenuIndices.clear();
		if (applicableActions & ACTION_ERASE)
		{
			InsertMenu(hSubmenu, ACTION_ERASE, MF_BYPOSITION, uID++,
				LoadString(IDS_ACTION_ERASE).c_str());				//Erase
			VerbMenuIndices.push_back(ACTION_ERASE);
		}
		if (applicableActions & ACTION_ERASE_ON_RESTART)
		{
			InsertMenu(hSubmenu, ACTION_ERASE_ON_RESTART, MF_BYPOSITION, uID++,
				LoadString(IDS_ACTION_ERASERESTART).c_str());		//Erase on Restart
			VerbMenuIndices.push_back(ACTION_ERASE_ON_RESTART);
		}
		if (applicableActions & ACTION_ERASE_UNUSED_SPACE)
		{
			MENUITEMINFO mii = { sizeof(MENUITEMINFO) };
			mii.wID = uID++;
			mii.fMask = MIIM_STRING | MIIM_ID;

			std::wstring str(LoadString(IDS_ACTION_ERASEUNUSEDSPACE));
			std::vector<wchar_t> buffer(str.length() + 1);
			wcscpy_s(&buffer.front(), str.length() + 1, str.c_str());
			mii.dwTypeData = &buffer.front();

			if (isVistaOrLater)
			{
				SHSTOCKICONINFO sii;
				::ZeroMemory(&sii, sizeof(sii));
				sii.cbSize = sizeof(sii);

				static HMODULE shellAPI = LoadLibrary(L"Shell32.dll");
				typedef HRESULT (__stdcall *pSHGetStockIconInfo)(SHSTOCKICONID siid, UINT uFlags,
					SHSTOCKICONINFO* psii);
				pSHGetStockIconInfo SHGetStockIconInfo = reinterpret_cast<pSHGetStockIconInfo>(
					GetProcAddress(shellAPI, "SHGetStockIconInfo"));

				unsigned dimensions = GetSystemMetrics(SM_CXSMICON);
				if (SUCCEEDED(SHGetStockIconInfo(SIID_SHIELD, SHGSI_ICON | SHGSI_SMALLICON, &sii)))
				{
					Handle<HICON> icon(sii.hIcon);
					static HBITMAP dib = NULL;

					if (dib == NULL)
					{
						dib = CreateDIB(dimensions, dimensions, NULL);
						Handle<HDC> hdc(CreateCompatibleDC(NULL));
						SelectObject(hdc, dib);

						DrawIconEx(hdc, 0, 0, icon, dimensions, dimensions, 0, NULL, DI_NORMAL);
						SelectObject(hdc, NULL);
					}

					mii.hbmpItem = dib;
					mii.fMask |= MIIM_BITMAP;
				}
			}
			
			InsertMenuItem(hSubmenu, ACTION_ERASE_UNUSED_SPACE, MF_BYPOSITION, &mii);
			VerbMenuIndices.push_back(ACTION_ERASE_UNUSED_SPACE);
		}
		//-------------------------------------------------------------------------
		if (applicableActions & ACTION_SECURE_MOVE)
		{
			InsertSeparator(hSubmenu);
			InsertMenu(hSubmenu, ACTION_SECURE_MOVE, MF_BYPOSITION, uID++,
				LoadString(IDS_ACTION_SECUREMOVE).c_str());			//Secure Move
			VerbMenuIndices.push_back(ACTION_SECURE_MOVE);
		}

		//Insert the submenu into the Context menu provided by Explorer.
		{
			MENUITEMINFO mii = { sizeof(MENUITEMINFO) };
			mii.wID = uID++;
			mii.fMask = MIIM_SUBMENU | MIIM_STRING | MIIM_ID;
			mii.hSubMenu = hSubmenu;
			mii.dwTypeData = const_cast<wchar_t*>(MenuTitle);

			//Set the bitmap for the registered item. Vista machines will be set using a DIB,
			//older machines will be ownerdrawn.
			if (isVistaOrLater)
			{
				mii.fMask |= MIIM_BITMAP;
				Handle<HICON> icon(GetMenuIcon());
				mii.hbmpItem = GetMenuBitmapFromIcon(icon);
			}
			else if (InvokeReason != INVOKEREASON_DRAGDROP)
			{
				mii.fMask |= MIIM_FTYPE;
				mii.fType = MFT_OWNERDRAW;
			}

			MenuID = uMenuIndex++;
			InsertMenuItem(hmenu, MenuID, TRUE, &mii);

			//Disable the menu item - IF the user selected the recycle bin AND the
			//recycle bin is empty
			if (InvokeReason == INVOKEREASON_RECYCLEBIN)
			{
				SHQUERYRBINFO sqrbi;
				::ZeroMemory(&sqrbi, sizeof(sqrbi));
				sqrbi.cbSize = sizeof(sqrbi);
				if (SUCCEEDED(SHQueryRecycleBin(NULL, &sqrbi)))
				{
					EnableMenuItem(hmenu, MenuID, MF_BYPOSITION |
						((sqrbi.i64NumItems != 0) ? MF_ENABLED : MF_DISABLED));
				}
			}
		}

		return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, uID - uidFirstCmd);
	}

	HRESULT CCtxMenu::HandleMenuMsg(UINT uMsg, WPARAM wParam, LPARAM lParam)
	{
		return HandleMenuMsg2(uMsg, wParam, lParam, NULL);
	}

	HRESULT CCtxMenu::HandleMenuMsg2(UINT uMsg, WPARAM /*wParam*/, LPARAM lParam,
	                                 LRESULT* result)
	{
		//Skip if we aren't handling our own.
		bool handleResult = false;
		switch (uMsg)
		{
		case WM_MEASUREITEM:
			{
				MEASUREITEMSTRUCT* mis = reinterpret_cast<MEASUREITEMSTRUCT*>(lParam);
				if (mis->CtlID == MenuID)
					handleResult = OnMeasureItem(mis->itemWidth, mis->itemHeight);
				else
					handleResult = false;
				break;
			}

		case WM_DRAWITEM:
			{
				DRAWITEMSTRUCT* dis = reinterpret_cast<DRAWITEMSTRUCT*>(lParam);
				if (dis->CtlID == MenuID)
					handleResult = OnDrawItem(dis->hDC, dis->rcItem, dis->itemAction, dis->itemState);
				else
					handleResult = false;
			}
		}

		if (result)
			*result = handleResult;
		return S_OK;
	}

	bool CCtxMenu::OnMeasureItem(UINT& itemWidth, UINT& itemHeight)
	{
		LOGFONT logFont;
		if (!SystemParametersInfo(SPI_GETICONTITLELOGFONT, sizeof(logFont), &logFont, 0))
			return false;

		//Measure the size of the text.
		Handle<HDC> screenDC = GetDC(NULL);
		Handle<HFONT> font = CreateFontIndirect(&logFont);
		SelectObject(screenDC, font);
		SIZE textSize;
		if (!GetTextExtentPoint32(screenDC, MenuTitle, static_cast<DWORD>(wcslen(MenuTitle)), &textSize))
			return false;

		itemWidth = textSize.cx;
		itemHeight = textSize.cy;

		//Account for the size of the bitmap.
		UINT iconWidth = GetSystemMetrics(SM_CXMENUCHECK);
		itemWidth += iconWidth;
		itemHeight = std::max(iconWidth, itemHeight);

		//And remember the minimum size for menu items.
		itemHeight = std::max((int)itemHeight, GetSystemMetrics(SM_CXMENUSIZE));
		return true;
	}

	bool CCtxMenu::OnDrawItem(HDC hdc, RECT rect, UINT /*action*/, UINT state)
	{
		//Draw the background.
		LOGBRUSH logBrush = { BS_SOLID,
			(state & ODS_SELECTED) ?
				GetSysColor(COLOR_HIGHLIGHT) : GetSysColor(COLOR_MENU),
			0
		};
		Handle<HBRUSH> bgBrush = CreateBrushIndirect(&logBrush);
		FillRect(hdc, &rect, bgBrush);

		//Then the bitmap.
		{
			//Draw the icon with alpha and all first.
			Handle<HICON> icon(GetMenuIcon());
			int iconSize = GetSystemMetrics(SM_CXMENUCHECK);
			int iconMargin = GetSystemMetrics(SM_CXEDGE);
			DrawState(hdc, NULL, NULL, reinterpret_cast<LPARAM>(static_cast<HICON>(icon)),
				NULL, rect.left + iconMargin, rect.top + (rect.bottom - rect.top - iconSize) / 2,
				0, 0, DST_ICON | ((state & ODS_DISABLED) ? DSS_DISABLED : 0));
			
			//Move the rectangle's left bound to the text starting position
			rect.left += iconMargin * 2 + iconSize;
		}
		
		//Draw the text.
		SetBkMode(hdc, TRANSPARENT);
		LOGFONT logFont;
		if (!SystemParametersInfo(SPI_GETICONTITLELOGFONT, sizeof(logFont), &logFont, 0))
			return false;

		SIZE textSize;
		if (!GetTextExtentPoint32(hdc, MenuTitle, static_cast<DWORD>(wcslen(MenuTitle)), &textSize))
			return false;

		COLORREF oldColour = SetTextColor(hdc, 
			(state & ODS_DISABLED) ? GetSysColor(COLOR_GRAYTEXT) :			//Disabled menu item
			(state & ODS_SELECTED) ? GetSysColor(COLOR_HIGHLIGHTTEXT) :		//Highlighted menu item
				GetSysColor(COLOR_MENUTEXT));								//Normal menu item
		UINT flags = DST_PREFIXTEXT;
		if (state & ODS_NOACCEL)
			flags |= DSS_HIDEPREFIX;
		::DrawState(hdc, NULL, NULL, reinterpret_cast<LPARAM>(MenuTitle), wcslen(MenuTitle),
			rect.left, rect.top + (rect.bottom - rect.top - textSize.cy) / 2, textSize.cx, textSize.cy, flags);
		SetTextColor(hdc, oldColour);
		return true;
	}

	HRESULT CCtxMenu::GetCommandString(UINT_PTR idCmd, UINT uFlags, UINT* /*pwReserved*/,
	                                   LPSTR pszName, UINT cchMax)
	{
		//We only know how to handle help string requests.
		if (!(uFlags & GCS_HELPTEXT))
			return E_INVALIDARG;

		//Get the command string for the given id
		if (idCmd >= VerbMenuIndices.size())
			return E_INVALIDARG;

		std::wstring commandString;
		switch (VerbMenuIndices[idCmd])
		{
		case ACTION_ERASE:
			commandString = LoadString(IDS_HELPSTRING_ERASE);
			break;
		case ACTION_ERASE_ON_RESTART:
			commandString = LoadString(IDS_HELPSTRING_ERASEONRESTART);
			break;
		case ACTION_ERASE_UNUSED_SPACE:
			commandString = LoadString(IDS_HELPSTRING_ERASEUNUSEDSPACE);
			break;
		case ACTION_SECURE_MOVE:
			commandString = LoadString(IDS_HELPSTRING_SECUREMOVE);
			break;
		default:
			//We don't know what action this is: return E_INVALIDARG.
			return E_INVALIDARG;
		}

		//Return the help string to Explorer.
		if (uFlags & GCS_UNICODE)
			wcscpy_s(reinterpret_cast<wchar_t*>(pszName), cchMax, commandString.c_str());
		else
		{
			size_t convCount = 0;
			wcstombs_s(&convCount, pszName, cchMax, commandString.c_str(), commandString.length());
		}

		return S_OK;
	}

	HRESULT CCtxMenu::InvokeCommand(LPCMINVOKECOMMANDINFO pCmdInfo)
	{
		//If lpVerb really points to a string, ignore this function call and bail out.
		if (HIWORD(pCmdInfo->lpVerb) != 0)
			return E_INVALIDARG;

		//If the verb index refers to an item outside the bounds of our VerbMenuIndices
		//vector, exit.
		if (LOWORD(pCmdInfo->lpVerb) >= VerbMenuIndices.size())
			return E_INVALIDARG;

		//Build the command line
		std::wstring commandAction;
		std::wstring commandLine;
		bool commandElevate = false;
		HRESULT result = E_INVALIDARG;
		switch (VerbMenuIndices[LOWORD(pCmdInfo->lpVerb)])
		{
		case ACTION_ERASE_ON_RESTART:
			commandLine += L"-s=restart ";

		case ACTION_ERASE:
			{
				//Add Task command.
				commandAction = L"addtask";

				//See the invocation context: if it is executed from the recycle bin
				//then the list of selected files will be empty.
				if (InvokeReason == INVOKEREASON_RECYCLEBIN)
				{
					commandLine += L"-r";
				}
				else
				{
					//Okay, we were called from an item right-click; iterate over every
					//selected file
					for (std::list<std::wstring>::const_iterator i = SelectedFiles.begin();
						i != SelectedFiles.end(); ++i)
					{
						//Check if the current item is a file or folder.
						std::wstring item(*i);
						if (item.length() > 2 && item[item.length() - 1] == '\\')
							item.erase(item.end() - 1);
						DWORD attributes = GetFileAttributes(item.c_str());

						//Escape the string
						std::wstring escapedItem(EscapeString(item));

						//Add the correct command line for the file type.
						if (attributes & FILE_ATTRIBUTE_DIRECTORY)
							commandLine += L"\"-d=" + escapedItem + L"\" ";
						else
							commandLine += L"\"-f=" + escapedItem + L"\" ";
					}
				}

				break;
			}

		case ACTION_ERASE_UNUSED_SPACE:
			{
				//We want to add a new task
				commandAction = L"addtask";

				//Erasing unused space requires elevation
				commandElevate = true;

				//Add every item onto the command line
				for (std::list<std::wstring>::const_iterator i = SelectedFiles.begin();
					i != SelectedFiles.end(); ++i)
				{
					std::wstring escapedItem(EscapeString(*i));
					commandLine += L"\"-u=" + escapedItem + L",clusterTips\" ";
				}
				
				break;
			}

		default:
			if (!(pCmdInfo->fMask & CMIC_MASK_FLAG_NO_UI))
			{
				MessageBox(pCmdInfo->hwnd, FormatString(LoadString(IDS_ERROR_UNKNOWNACTION),
					VerbMenuIndices[LOWORD(pCmdInfo->lpVerb)]).c_str(),
					LoadString(IDS_ERASERSHELLEXT).c_str(), MB_OK | MB_ICONERROR);
			}
		}

		try
		{
			RunEraser(commandAction, commandLine, commandElevate, pCmdInfo->hwnd,
				pCmdInfo->nShow);
		}
		catch (const std::wstring& e)
		{
			if (!(pCmdInfo->fMask & CMIC_MASK_FLAG_NO_UI))
			{
				MessageBox(pCmdInfo->hwnd, e.c_str(), LoadString(IDS_ERASERSHELLEXT).c_str(),
					MB_OK | MB_ICONERROR);
			}
		}

		return result;
	}

	CCtxMenu::Actions CCtxMenu::GetApplicableActions()
	{
		unsigned result = 0;
		
		//First decide the actions which are applicable to the current invocation
		//reason.
		switch (InvokeReason)
		{
		case INVOKEREASON_RECYCLEBIN:
			result |= ACTION_ERASE | ACTION_ERASE_ON_RESTART;
			break;
		case INVOKEREASON_FILEFOLDER:
			result |= ACTION_ERASE | ACTION_ERASE_ON_RESTART | ACTION_ERASE_UNUSED_SPACE;
#if 0
		case INVOKEREASON_DRAGDROP:
			result |= ACTION_SECURE_MOVE;
#endif
		}

		//Remove actions that don't apply to the current invocation reason.
		for (std::list<std::wstring>::const_iterator i = SelectedFiles.begin();
			i != SelectedFiles.end(); ++i)
		{
			//Remove trailing slashes if they are directories.
			std::wstring item(*i);

			//Check if the path is a path to a volume, if it is not, remove the
			//erase unused space verb.
			wchar_t volumeName[MAX_PATH];
			if (!GetVolumeNameForVolumeMountPoint(item.c_str(), volumeName,
				sizeof(volumeName) / sizeof(volumeName[0])))
			{
				result &= ~ACTION_ERASE_UNUSED_SPACE;
			}
		}

		return static_cast<Actions>(result);
	}

	std::wstring CCtxMenu::LoadString(UINT stringID)
	{
		//Get a pointer to the buffer containing the string (we're copying it anyway)
		wchar_t* buffer = NULL;
		DWORD lastCount = ::LoadString(theApp.m_hInstance, stringID,
			reinterpret_cast<wchar_t*>(&buffer), 0);

		if (lastCount > 0)
			return std::wstring(buffer, lastCount);
		return std::wstring();
	}

	std::wstring CCtxMenu::EscapeString(const std::wstring& string)
	{
		//Escape the command line (= and , are special characters)
		std::wstring escapedItem;
		escapedItem.reserve(string.length());
		for (std::wstring::const_iterator i = string.begin(); i != string.end(); ++i)
		{
			if (wcschr(L"\\=,", *i))
				escapedItem += '\\';
			escapedItem += *i;
		}

		return escapedItem;
	}

	std::wstring CCtxMenu::FormatString(const std::wstring& formatString, ...)
	{
		std::vector<wchar_t> formatStr(formatString.length() + 1);
		wcscpy_s(&formatStr.front(), formatStr.size(), formatString.c_str());

		std::vector<wchar_t> buffer(formatStr.size());
		for ( ; ; )
		{
			buffer.resize(buffer.size() * 2);
			va_list arguments;
			va_start(arguments, formatString);
			int result = vswprintf_s(&buffer.front(), buffer.size(), &formatStr.front(),
				arguments);
			va_end(arguments);

			if (result > 0 && static_cast<unsigned>(result) < buffer.size())
			{
				break;
			}
		}

		//Return the result as a wstring
		std::wstring result;
		if (buffer.size() > 0)
			result = &buffer.front();
		return result;
	}

	std::wstring CCtxMenu::FormatError(DWORD lastError)
	{
		if (lastError == static_cast<DWORD>(-1))
			lastError = GetLastError();

		LPTSTR messageBuffer = NULL;
		if (FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM, 0,
			lastError, 0, reinterpret_cast<LPWSTR>(&messageBuffer), 0, NULL) == 0)
		{
			return L"";
		}

		std::wstring result(messageBuffer);
		LocalFree(messageBuffer);
		return result;
	}

	std::wstring CCtxMenu::GetHKeyPath(HKEY handle)
	{
		if (NtQueryKey == NULL)
			NtQueryKey = reinterpret_cast<pNtQueryKey>(GetProcAddress(
				LoadLibrary(L"Ntdll.dll"), "NtQueryKey"));

		//Keep querying for the key information until enough buffer space has been allocated.
		std::vector<char> buffer(sizeof(KEY_NODE_INFORMATION));
		NTSTATUS queryResult = STATUS_BUFFER_TOO_SMALL;
		ULONG keyInfoSize = 0;

		while (queryResult == STATUS_BUFFER_TOO_SMALL || queryResult == STATUS_BUFFER_OVERFLOW)
		{
			buffer.resize(buffer.size() + keyInfoSize);
			ZeroMemory(&buffer.front(), buffer.size());
			queryResult = NtQueryKey(handle, KeyNodeInformation, &buffer.front(),
				static_cast<ULONG>(buffer.size()), &keyInfoSize);
		}

		if (queryResult != STATUS_SUCCESS)
			return std::wstring();

		KEY_NODE_INFORMATION* keyInfo = reinterpret_cast<KEY_NODE_INFORMATION*>(
			&buffer.front());
		return keyInfo->Name;
	}

	bool CCtxMenu::IsUserAdmin()
	{
		SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
		PSID AdministratorsGroup;
		if (AllocateAndInitializeSid(&NtAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID,
			DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &AdministratorsGroup))
		{
			BOOL result = false;
			if (!CheckTokenMembership(NULL, AdministratorsGroup, &result))
				result = false;

			FreeSid(AdministratorsGroup);
			return result != FALSE;
		}

		return false;
	}

	void CCtxMenu::RunEraser(const std::wstring& action, const std::wstring& parameters,
		bool elevated, HWND parent, int show)
	{
		//Get the path to this DLL so we can look for Eraser.exe
		wchar_t fileName[MAX_PATH];
		DWORD fileNameLength = GetModuleFileName(theApp.m_hInstance, fileName,
			sizeof(fileName) / sizeof(fileName[0]));
		if (!fileNameLength || fileNameLength >= sizeof(fileName) / sizeof(fileName[0]))
			throw LoadString(IDS_ERROR_CANNOTFINDERASER);
		
		//Trim to the last \, then append Eraser.exe
		std::wstring eraserPath(fileName, fileNameLength);
		std::wstring::size_type lastBackslash = eraserPath.rfind('\\');
		if (lastBackslash == std::wstring::npos)
			throw LoadString(IDS_ERROR_CANNOTFINDERASER);

		eraserPath.erase(eraserPath.begin() + lastBackslash + 1, eraserPath.end());
		if (eraserPath.empty())
			throw LoadString(IDS_ERROR_CANNOTFINDERASER);

		eraserPath += L"Eraser.exe";

		std::wstring finalParameters;
		{
			std::wostringstream parametersStrm;
			parametersStrm << "\"" << action << L"\" -q " << parameters;
			finalParameters = parametersStrm.str();
		}

		//If the process must be elevated we use ShellExecute with the runas verb
		//to elevate the new process.
		if (elevated && !IsUserAdmin())
		{
			int result = reinterpret_cast<int>(ShellExecute(parent, L"runas",
				eraserPath.c_str(), finalParameters.c_str(), NULL, show));
			if (result <= 32)
				switch (result)
				{
				case SE_ERR_ACCESSDENIED:
					throw LoadString(IDS_ERROR_ACCESSDENIED);
				default:
					throw LoadString(IDS_ERROR_UNKNOWN);
				}
		}

		//If the process isn't to be elevated, we use CreateProcess so we can get
		//read the output from the child process
		else
		{
			//Create the process.
			STARTUPINFO startupInfo;
			ZeroMemory(&startupInfo, sizeof(startupInfo));
			startupInfo.cb = sizeof(startupInfo);
			startupInfo.dwFlags = STARTF_USESHOWWINDOW;
			startupInfo.wShowWindow = static_cast<WORD>(show);
			startupInfo.hStdInput = startupInfo.hStdOutput = startupInfo.hStdError =
				INVALID_HANDLE_VALUE;

			//Create handles for output redirection
			Handle<HANDLE> readPipe;
			HANDLE writePipe;
			SECURITY_ATTRIBUTES security;
			ZeroMemory(&security, sizeof(security));
			security.nLength = sizeof(security);
			security.lpSecurityDescriptor = NULL;
			security.bInheritHandle = true;

			if (CreatePipe(&static_cast<HANDLE&>(readPipe), &writePipe, &security, 0))
			{
				startupInfo.dwFlags |= STARTF_USESTDHANDLES;
				startupInfo.hStdOutput = startupInfo.hStdError =
					writePipe;
			}

			PROCESS_INFORMATION processInfo;
			ZeroMemory(&processInfo, sizeof(processInfo));
			std::vector<wchar_t> buffer(eraserPath.length() + finalParameters.length() + 4);
			wcscpy_s(&buffer.front(), buffer.size(), (L"\"" + eraserPath + L"\" " +
				finalParameters).c_str());

			if (!CreateProcess(NULL, &buffer.front(), NULL, NULL, true, CREATE_NO_WINDOW,
				NULL, NULL, &startupInfo, &processInfo))
			{
				if (GetLastError() == ERROR_FILENAME_EXCED_RANGE)
					throw FormatString(LoadString(IDS_ERROR_TOO_MANY_FILES));
				else
					throw FormatString(LoadString(IDS_ERROR_MISC), FormatError().c_str());
			}

			//Wait for the process to finish.
			Handle<HANDLE> hProcess(processInfo.hProcess),
						   hThread(processInfo.hThread);
			CloseHandle(writePipe);
			WaitForSingleObject(hProcess, static_cast<DWORD>(-1));
			DWORD exitCode = 0;
			
			if (GetExitCodeProcess(processInfo.hProcess, &exitCode))
				if (exitCode == ERROR_ACCESS_DENIED)
				{
					//The spawned instance could not connect with the master instance
					//because it is running as an administrator. Spawn the new instance
					//again, this time as an administrator
					RunEraser(action, parameters, true, parent, show);
				}
				else if (exitCode)
				{
					char buffer[8192];
					DWORD lastRead = 0;
					std::wstring output;

					while (ReadFile(readPipe, buffer, sizeof(buffer), &lastRead, NULL) && lastRead != 0)
					{
						size_t lastConvert = 0;
						wchar_t wBuffer[8192];
						if (!mbstowcs_s(&lastConvert, wBuffer, sizeof(wBuffer) / sizeof(wBuffer[0]),
							buffer, lastRead))
						{
							output += std::wstring(wBuffer, lastConvert);
						}
					}

					//Show the error message.
					throw output;
				}
		}
	}

	void CCtxMenu::InsertSeparator(HMENU menu)
	{
		MENUITEMINFO mii;
		mii.cbSize = sizeof(MENUITEMINFO);
		mii.fMask = MIIM_TYPE;
		mii.fType = MF_SEPARATOR;
		InsertMenuItem(menu, 0, false, &mii);
	}

	HICON CCtxMenu::GetMenuIcon()
	{
		int smIconSize = GetSystemMetrics(SM_CXMENUCHECK);
		return static_cast<HICON>(LoadImage(theApp.m_hInstance, L"Eraser",
			IMAGE_ICON, smIconSize, smIconSize, LR_DEFAULTCOLOR));
	}

	HBITMAP CCtxMenu::GetMenuBitmapFromIcon(HICON icon)
	{
		BITMAP bitmap;
		ICONINFO iconInfo;
		ZeroMemory(&bitmap, sizeof(bitmap));
		ZeroMemory(&iconInfo, sizeof(iconInfo));

		//Try to get the icon's size, bitmap and bit depth. We will try to convert
		//the bitmap into a DIB for display on Vista if it contains an alpha channel.
		if (!GetIconInfo(icon, &iconInfo))
			return NULL;

		Handle<HBITMAP> iconMask(iconInfo.hbmMask);
		if (!GetObject(iconInfo.hbmColor, sizeof(BITMAP), &bitmap) ||
			bitmap.bmBitsPixel < 32)
			return iconInfo.hbmColor;

		//Draw the icon onto the DIB which will preseve its alpha values
		Handle<HDC> hdcDest = CreateCompatibleDC(NULL);
		HBITMAP dib = CreateDIB(bitmap.bmWidth, bitmap.bmHeight, NULL);
		SelectObject(hdcDest, dib);

		Handle<HBITMAP> iconBitmap(iconInfo.hbmColor);
		DrawIconEx(hdcDest, 0, 0, icon, bitmap.bmWidth, bitmap.bmHeight, 0, NULL, DI_NORMAL);
		return dib;
	}

	HBITMAP CCtxMenu::CreateDIB(LONG width, LONG height, char** bitmapBits)
	{
		BITMAPINFO info;
		ZeroMemory(&info, sizeof(info));
		info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		info.bmiHeader.biWidth = width;
		info.bmiHeader.biHeight = height;
		info.bmiHeader.biPlanes = 1;
		info.bmiHeader.biBitCount = 32;

		Handle<HDC> screenDC(GetDC(NULL));
		return ::CreateDIBSection(screenDC, &info, DIB_RGB_COLORS,
			reinterpret_cast<void**>(bitmapBits), NULL, 0);
	}
}
