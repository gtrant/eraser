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

#pragma once

#include "OpenHandle.NameResolver.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Collections::ObjectModel;
using namespace Microsoft::Win32::SafeHandles;

namespace Eraser {
namespace Util {
	/// Represents one open handle in the system.
	public ref class OpenHandle
	{
	internal:
		/// Constructor.
		/// 
		/// \param[in] handle The handle to wrap.
		/// \param[in] pid The Process ID of the handle.
		/// \param[in] path The path to the file.
		OpenHandle(IntPtr handle, int pid, String^ path)
		{
			this->handle = handle;
			this->processId = pid;
			this->path = path;
		}

	public:
		/// Retrieves all open handles on the system
		static property IList<OpenHandle^>^ Items
		{
			IList<OpenHandle^>^ get();
		}

		/// Force the handle to close.
		bool Close();

		/// The handle to the file, in the context of the owning process.
		property IntPtr Handle
		{
			IntPtr get()
			{
				return handle;
			}
		}

		/// The path to the file.
		property String^ Path
		{
			String^ get()
			{
				return path;
			}
		}

		/// The process ID of the process owning the handle.
		property int ProcessId
		{
			int get()
			{
				return processId;
			}
		};

	private:
		/// Resolves a handle to a file name.
		///
		/// \param[in] handle The file handle to resolve.
		/// \param[in] pid The process ID of the owning process.
		/// \return A string containing the path to the file, or null.
		static String^ ResolveHandlePath(IntPtr handle, int pid);

	private:
		static HANDLE* NameResolutionThread;
		static NameResolutionThreadParams* NameResolutionThreadParam;

		IntPtr handle;
		String^ path;
		int processId;
	};
}
}
