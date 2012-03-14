/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
using namespace System::Diagnostics;
using namespace Microsoft::Win32::SafeHandles;

namespace Eraser {
namespace Util {
	/// <summary>
	/// Represents one open handle in the system.
	/// </summary>
	public ref class OpenHandle
	{
	internal:
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="handle">The handle to wrap.</param>
		/// <param name="pid">The Process ID of the handle.</param>
		/// <param name="path">The path to the file.</param>
		OpenHandle(IntPtr handle, int pid, String^ path)
		{
			this->handle = handle;
			this->processId = pid;
			this->path = path;
		}

	public:
		/// <summary>
		/// Retrieves all open handles on the system
		/// </summary>
		static property IList<OpenHandle^>^ Items
		{
			IList<OpenHandle^>^ get();
		}

		/// <summary>
		/// Closes all handles matching the given path.
		/// </summary>
		/// <param name="path">The path to close handles for.</param>
		/// <returns>A list of handles which could not be closed</returns>
		static List<OpenHandle^>^ Close(String^ path);

		/// <summary>
		/// Force the handle to close.
		/// </summary>
		bool Close();

		/// <summary>
		/// The handle to the file, in the context of the owning process.
		/// </summary>
		property IntPtr Handle
		{
			IntPtr get()
			{
				return handle;
			}
		}

		/// <summary>
		/// The path to the file.
		/// </summary>
		property String^ Path
		{
			String^ get()
			{
				return path;
			}
		}

		/// <summary>
		/// The process ID of the process owning the handle.
		/// </summary>
		property Process^ Process
		{
			System::Diagnostics::Process^ get()
			{
				return System::Diagnostics::Process::GetProcessById(processId);
			}
		};

	private:
		/// <summary>
		/// Resolves a handle to a file name.
		/// </summary>
		/// <param name="handle">The file handle to resolve.</param>
		/// <param name="pid">The process ID of the owning process.</param>
		/// <returns>A string containing the path to the file, or null.</returns>
		static String^ ResolveHandlePath(IntPtr handle, int pid);

	private:
		IntPtr handle;
		String^ path;
		int processId;
	};
}
}
