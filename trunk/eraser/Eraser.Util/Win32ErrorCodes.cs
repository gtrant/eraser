/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;

namespace Eraser.Util
{
	public static class Win32ErrorCode
	{
		/// <summary>
		/// Gets the error message associated with the provided system error code.
		/// </summary>
		/// <param name="code">The system error code which should have the message queried.</param>
		/// <returns>The string describing the error with the given error code.</returns>
		public static string GetSystemErrorMessage(int code)
		{
			return new System.ComponentModel.Win32Exception(code).Message;
		}

		/// <summary>
		/// Converts a Win32 Error code to a HRESULT.
		/// </summary>
		/// <param name="errorCode">The error code to convert.</param>
		/// <returns>A HRESULT value representing the error code.</returns>
		private static int GetHRForWin32Error(int errorCode)
		{
			const uint FACILITY_WIN32 = 7;
			return errorCode <= 0 ? errorCode :
				(int)((((uint)errorCode) & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000);
		}

		/// <summary>
		/// Gets a Exception for the given Win32 error code.
		/// </summary>
		/// <param name="errorCode">The error code.</param>
		/// <returns>An exception object representing the error code.</returns>
		internal static Exception GetExceptionForWin32Error(int errorCode)
		{
			switch (errorCode)
			{
				case NoError:			return null;
				case DeviceNotExist:
				case NotReady:			return new IOException();
				case RequestAborted:	return new OperationCanceledException();
				case SharingViolation:	return new SharingViolationException();
				case BadCommand:		return new NotSupportedException();
				case BadArguments:		return new ArgumentException();
			}

			int HR = GetHRForWin32Error(errorCode);
			Exception exception = Marshal.GetExceptionForHR(HR);
			if (exception.GetType() == typeof(COMException))
				throw new Win32Exception(errorCode);
			else
				throw exception;
		}

		public const int NoError = Success;
		public const int Success = 0;
		public const int InvalidFunction = 1;
		public const int FileNotFound = 2;
		public const int PathNotFound = 3;
		public const int AccessDenied = 5;
		public const int NoMoreFiles = 18;
		public const int NotReady = 21;
		public const int SharingViolation = 32;
		public const int DeviceNotExist = 55;
		public const int InvalidParameter = 87;
		public const int DiskFull = 112;
		public const int InsufficientBuffer = 122;
		public const int MoreData = 234;
		public const int NoMoreItems = 259;
		public const int UnrecognizedVolume = 1005;
		public const int BadDevice = 1200;
		public const int RequestAborted = 1235;
		public const int NotAReparsePoint = 4390;
		public const int BadCommand = 22;
		public const int NotSupported = 50;
		public const int BadArguments = 160;
	}
}
