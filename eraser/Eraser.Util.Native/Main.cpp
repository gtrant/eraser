/* 
 * $Id: Main.cpp 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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

#include "stdafx.h"

#pragma unmanaged

namespace {
	typedef NTSTATUS (__stdcall *fNtQuerySystemInformation)(
		__in       SYSTEM_INFORMATION_CLASS,
		__inout    PVOID,
		__in       ULONG,
		__out_opt  PULONG);

	typedef NTSTATUS (__stdcall *fNtQueryObject)(
		IN HANDLE   OPTIONAL,
		IN OBJECT_INFORMATION_CLASS  ,
		OUT PVOID   OPTIONAL,
		IN ULONG  ,
		OUT PULONG   OPTIONAL);

	fNtQuerySystemInformation pNtQuerySystemInformation = NULL;
	fNtQueryObject pNtQueryObject = NULL;

	class DllLoader
	{
	public:
		DllLoader()
		{
			HINSTANCE ntDll = LoadLibrary(L"NtDll.dll");
			pNtQuerySystemInformation = reinterpret_cast<fNtQuerySystemInformation>(
				GetProcAddress(ntDll, "NtQuerySystemInformation"));
			pNtQueryObject = reinterpret_cast<fNtQueryObject>(
				GetProcAddress(ntDll, "NtQueryObject"));
		}

	};

	DllLoader loader;
}

NTSTATUS __stdcall NtQuerySystemInformation(
		__in       SYSTEM_INFORMATION_CLASS sic,
		__inout    PVOID data,
		__in       ULONG length,
		__out_opt  PULONG outLength)
{
	return pNtQuerySystemInformation(sic, data, length, outLength);
}

NTSTATUS __stdcall NtQueryObject(
		IN HANDLE handle OPTIONAL,
		IN OBJECT_INFORMATION_CLASS oic,
		OUT PVOID data OPTIONAL,
		IN ULONG length,
		OUT PULONG outLength OPTIONAL)
{
	return pNtQueryObject(handle, oic, data, length, outLength);
}
