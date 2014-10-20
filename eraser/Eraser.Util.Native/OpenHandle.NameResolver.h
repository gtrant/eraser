/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

struct NameResult
{
	NameResult(HANDLE handle)
	{
		Handle = handle;
		Event = CreateEvent(NULL, false, false, NULL);
	}

	~NameResult()
	{
		CloseHandle(Event);
	}

	HANDLE Handle;
	std::wstring Name;
	HANDLE Event;
};

struct NameResolutionThreadParams
{
	NameResolutionThreadParams()
	{
		Semaphore = CreateSemaphore(NULL, 0, 1, NULL);
	}

	~NameResolutionThreadParams()
	{
		CloseHandle(Semaphore);
	}

	/// The input/output queue.
	std::list<NameResult*> Input;

	/// Wait queue
	HANDLE Semaphore;
};

struct AutoHandle
{
public:
	AutoHandle(HANDLE handle)
	{
		Handle = handle;
	}

	~AutoHandle()
	{
		CloseHandle(Handle);
	}

	operator HANDLE&()
	{
		return Handle;
	}

private:
	HANDLE Handle;
};

std::wstring ResolveHandleName(HANDLE handle, int pid);
