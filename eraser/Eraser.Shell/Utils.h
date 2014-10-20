/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
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

/// Displays a busy cursor for the lifetime of this object
class BusyCursor
{
public:
	BusyCursor()
	{
		if (++Count == 1)
			Cursor = SetCursor(LoadCursor(NULL, IDC_WAIT));
		else
			Cursor = NULL;
	}

	~BusyCursor()
	{
		if (Count == 0)
			return;

		if (--Count == 0)
			SetCursor(Cursor);
	}

private:
	static HCURSOR Cursor;
	static unsigned Count;
};
