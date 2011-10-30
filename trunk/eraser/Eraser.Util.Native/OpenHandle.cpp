/* 
 * $Id$
 * Copyright 2008-2010 The Eraser Project
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

namespace Eraser {
namespace Util {
	IList<OpenHandle^>^ OpenHandle::Items::get()
	{
		List<OpenHandle^>^ handles = gcnew List<OpenHandle^>();

		//Get the number of handles on the system then load up the complete list.
		std::auto_ptr<SYSTEM_HANDLES> handlesList(new SYSTEM_HANDLES);
		{
			DWORD bufferSize = 0;
			NtQuerySystemInformation(static_cast<SYSTEM_INFORMATION_CLASS>(SystemHandleInformation),
				handlesList.get(), sizeof(SYSTEM_HANDLES), &bufferSize);

			//Then get the whole list
			handlesList.reset(reinterpret_cast<PSYSTEM_HANDLES>(new char[bufferSize]));
			NtQuerySystemInformation(static_cast<SYSTEM_INFORMATION_CLASS>(SystemHandleInformation),
				handlesList.get(), bufferSize, &bufferSize);

			if (bufferSize == 0)
				throw gcnew InvalidOperationException(S::_(L"The list of open system handles could not be retrieved."));
		}

		//Iterate over the handles
		for (ULONG i = 0; i != handlesList->NumberOfHandles; ++i)
		{
			//Only consider files
			SYSTEM_HANDLE_INFORMATION handleInfo = handlesList->Information[i];
			if (handleInfo.ObjectTypeNumber != 25 && handleInfo.ObjectTypeNumber != 23 &&
				handleInfo.ObjectTypeNumber != 28)
			{
				continue;
			}

			//Try to resolve the path of the handle, continue if we can't get it
			String^ handlePath = ResolveHandlePath(IntPtr(handleInfo.Handle),
				handleInfo.ProcessId);
			if (String::IsNullOrEmpty(handlePath))
				continue;

			//Store the entry
			OpenHandle^ listItem = gcnew OpenHandle(IntPtr(handleInfo.Handle),
				handleInfo.ProcessId, handlePath);
			handles->Add(listItem);
		}

		return handles->AsReadOnly();
	}

	List<OpenHandle^>^ OpenHandle::Close(String^ path)
	{
		List<OpenHandle^>^ result = gcnew List<OpenHandle^>();
		for each (OpenHandle^ handle in Items)
		{
			if (handle->Path == path)
				if (!handle->Close())
					result->Add(handle);
		}
		
		return result;
	}

	String^ OpenHandle::ResolveHandlePath(IntPtr handle, int pid)
	{
		std::wstring result(ResolveHandleName(static_cast<void*>(handle), pid));
		return result.empty() ? nullptr :
			gcnew String(result.c_str(), 0, static_cast<int>(result.length()));
	}

	bool OpenHandle::Close()
	{
		//Open a handle to the owning process
		HANDLE processHandle = OpenProcess(PROCESS_DUP_HANDLE, false, processId);

		//Forcibly close the handle
		HANDLE duplicateHandle = NULL;
		DuplicateHandle(processHandle, static_cast<void*>(Handle), GetCurrentProcess(),
			&duplicateHandle, 0, false, DUPLICATE_SAME_ACCESS | DUPLICATE_CLOSE_SOURCE);
		CloseHandle(duplicateHandle);

		//Check if the handle is closed
		bool result = true;
		if (DuplicateHandle(processHandle, static_cast<void*>(Handle), GetCurrentProcess(),
			&duplicateHandle, 0, false, DUPLICATE_SAME_ACCESS))
		{
			result = false;
			CloseHandle(duplicateHandle);
		}

		//Close the process handle
		CloseHandle(processHandle);

		//Return the result
		return result;
	}
}
}
