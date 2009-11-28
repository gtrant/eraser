/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By:
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

#define WINVER 0x0600
#define _WIN32_WINNT 0x0600
#define _WIN32_WINDOWS 0x0410
#define _WIN32_IE 0x0700
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

// Windows Header Files:
#include <windows.h>
#include <commctrl.h>
#include <Shellapi.h>

#include <tchar.h>
#include <string>
#include <map>

//7-zip SDK
extern "C" {
	#include <C/7zCrc.h>
	#include <C/Archive/7z/7zIn.h>
	#include <C/Archive/7z/7zAlloc.h>
	#include <C/Archive/7z/7zDecode.h>
	#include <C/Archive/7z/7zExtract.h>
}
