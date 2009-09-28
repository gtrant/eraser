/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
#include "Eraser.Unlocker.h"

#pragma managed(push)
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
}

#pragma managed(pop)

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
