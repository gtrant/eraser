/* 
 * $Id: Handle.h 2993 2021-09-25 17:23:27Z gtrant $
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

#pragma once

#include <windows.h>

/// Manages the lifetime of a Windows handle. This class will destroy the handle
/// based on its type upon destruction of this object.
template<typename handleType> class Handle
{
public:
	/// Constructor.
	/// 
	/// \param[in] handle The handle to manage.
	Handle(handleType handle = NULL)
	{
		thisHandle = handle;
	}

	~Handle();

	/// Converts the handle back to an unmanaged handle.
	operator handleType()
	{
		return thisHandle;
	}

	/// Returns a mutable reference to the internal value. This allows this object
	/// to be used when a function takes a pointer to a handle (e.g. for Out parameters)
	operator handleType*()
	{
		return &thisHandle;
	}

private:
	/// The handle this object is managing.
	handleType thisHandle;
};

Handle<HANDLE>::~Handle()
{
	CloseHandle(thisHandle);
}

Handle<HKEY>::~Handle()
{
	RegCloseKey(thisHandle);
}
