/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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
#include "OpenHandle.h"

#pragma unmanaged

namespace {
	DWORD __stdcall nameResolutionThread(void* data)
	{
		//Get the thread parameters
		NameResolutionThreadParams& param = *static_cast<NameResolutionThreadParams*>(data);

		//Get the list of logical drives
		wchar_t drives[26 * 3];
		if (!GetLogicalDriveStrings(sizeof(drives) / sizeof(drives[0]), drives))
			return 1;
		for (wchar_t* i = drives; *i; i += 4)
			*(i + 2) = 0;

		//Get the file path
		char name[4096];
		PUNICODE_STRING nameStr = reinterpret_cast<PUNICODE_STRING>(name);

		for ( ; ; )
		{
			WaitForSingleObject(param.Semaphore, INFINITE);
			std::list<NameResult*>::iterator i = param.Input.begin();

			//If the iterator points to a NULL handle terminate us.
			if (*i == NULL)
				break;

			//Erase this entry from the queue
			NameResult& result = **i;
			param.Input.erase(i);

			//Query the name of the object
			if (NtQueryObject(result.Handle, static_cast<OBJECT_INFORMATION_CLASS>(ObjectNameInformation),
				name, sizeof(name), NULL) == STATUS_SUCCESS)
			{
				if (nameStr && nameStr->Length)
				{
					std::wstring& name = result.Name;

					//Resolve the file path into logical drives
					wchar_t path[MAX_PATH];
					name.assign(nameStr->Buffer);
					for (wchar_t* j = drives; *j; j += 4)
						if (QueryDosDevice(j, path, MAX_PATH))
						{
							size_t pathLen = wcslen(path);
							if (name.substr(0, pathLen) == path)
							{
								name.replace(0, pathLen, j);
								break;
							}
						}
				}
			}

			//Tell the waiting thread that we're done
			SetEvent(result.Event);
		}

		return 0;
	}

	void CreateNameThread(HANDLE& handle, NameResolutionThreadParams& params)
	{
		//If the handle is valid terminate the thread
		if (handle)
		{
			TerminateThread(handle, 1);
			CloseHandle(handle);
		}

		//Create the thread
		handle = CreateThread(NULL, 0, nameResolutionThread, &params, 0, NULL);
	}
}

std::wstring ResolveHandleName(HANDLE handle, int pid)
{
	static HANDLE thread = NULL;
	static NameResolutionThreadParams params;

	//Start a name resolution thread (in case one entry hangs)
	if (thread == NULL)
		CreateNameThread(thread, params);

	//Create a duplicate handle
	HANDLE localHandle;
	HANDLE processHandle = OpenProcess(PROCESS_DUP_HANDLE, false, pid);
	DuplicateHandle(processHandle, static_cast<void*>(handle), GetCurrentProcess(),
		&localHandle, 0, false, DUPLICATE_SAME_ACCESS);
	CloseHandle(processHandle);

	//We need a handle
	if (!localHandle)
		return std::wstring();

	//Send the handle to the secondary thread for name resolution
	NameResult result(localHandle);
	params.Input.push_back(&result);
	ReleaseSemaphore(params.Semaphore, 1, NULL);

	//Wait for the result
	if (WaitForSingleObject(result.Event, 50) != WAIT_OBJECT_0)
	{
		//The wait failed. Terminate the thread and recreate another.
		CreateNameThread(thread, params);
	}

	//Close the handle which we duplicated
	CloseHandle(localHandle);

	//Return the result
	return result.Name;
}
