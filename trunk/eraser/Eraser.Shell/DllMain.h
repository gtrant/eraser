/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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

#pragma once

class CEraserShellExtModule : public CAtlDllModuleT< CEraserShellExtModule >
{
public :
	DECLARE_LIBID(LIBID_EraserShellExtLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_ERASERSHELLEXT, "{92BDCDEA-D98E-49C2-9851-A4AD15B847EA}")
};

class CEraserShellExtApp : public CWinApp
{
public:
	virtual BOOL InitInstance();
	virtual int ExitInstance();

	DECLARE_MESSAGE_MAP()
};

extern CEraserShellExtModule _AtlModule;
extern CEraserShellExtApp theApp;
