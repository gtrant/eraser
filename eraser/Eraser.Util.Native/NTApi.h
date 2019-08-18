/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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
#include <winternl.h>

typedef struct _SYSTEM_HANDLE_INFORMATION {
	ULONG  ProcessId;
	UCHAR  ObjectTypeNumber;
	UCHAR  Flags;
	USHORT  Handle;
	PVOID  Object;
	ACCESS_MASK  GrantedAccess;
} SYSTEM_HANDLE_INFORMATION, *PSYSTEM_HANDLE_INFORMATION;

typedef struct _SYSTEM_HANDLES {
	ULONG NumberOfHandles;
	SYSTEM_HANDLE_INFORMATION Information[1];
} SYSTEM_HANDLES, *PSYSTEM_HANDLES;

const int SystemHandleInformation = 16;
const int ObjectNameInformation = 1;
const int ObjectAllTypesInformation = 3;
const int ObjectHandleInformation = 4;
