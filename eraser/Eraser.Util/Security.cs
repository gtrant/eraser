/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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

using System.ComponentModel;
using System.Security.Principal;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;
using Microsoft.Runtime.Hosting;
using Microsoft.Runtime.Hosting.Interop;

namespace Eraser.Util
{
	public static class Security
	{
		/// <summary>
		/// Checks whether the current process is running with administrative
		/// privileges.
		/// </summary>
		/// <returns>True if the user is an administrator. This only returns
		/// true under Vista when UAC is enabled and the process is elevated.</returns>
		public static bool IsAdministrator()
		{
			WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		/// <summary>
		/// Verifies the Authenticode signature in a file.
		/// </summary>
		/// <param name="pathToFile">The file to verify.</param>
		/// <returns>True if the file contains a valid Authenticode certificate.</returns>
		public static bool VerifyAuthenticode(string pathToFile)
		{
			IntPtr unionPointer = IntPtr.Zero;

			try
			{
				NativeMethods.WINTRUST_FILE_INFO fileinfo = new NativeMethods.WINTRUST_FILE_INFO();
				fileinfo.cbStruct = (uint)Marshal.SizeOf(typeof(NativeMethods.WINTRUST_FILE_INFO));
				fileinfo.pcwszFilePath = pathToFile;

				NativeMethods.WINTRUST_DATA data = new NativeMethods.WINTRUST_DATA();
				data.cbStruct = (uint)Marshal.SizeOf(typeof(NativeMethods.WINTRUST_DATA));
				data.dwUIChoice = NativeMethods.WINTRUST_DATA.UIChoices.WTD_UI_NONE;
				data.fdwRevocationChecks = NativeMethods.WINTRUST_DATA.RevocationChecks.WTD_REVOKE_NONE;
				data.dwUnionChoice = NativeMethods.WINTRUST_DATA.UnionChoices.WTD_CHOICE_FILE;
				unionPointer = data.pUnion = Marshal.AllocHGlobal((int)fileinfo.cbStruct);
				Marshal.StructureToPtr(fileinfo, data.pUnion, false);

				Guid guid = NativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2;
				return NativeMethods.WinVerifyTrust(IntPtr.Zero, ref guid, ref data) == 0;
			}
			finally
			{
				if (unionPointer != IntPtr.Zero)
					Marshal.FreeHGlobal(unionPointer);
			}
		}

		/// <summary>
		/// Gets a value indicating whether the assembly manifest at the supplied
		/// path contains a strong name signature. 
		/// </summary>
		/// <param name="assemblyPath">The path to the portable executable (.exe or
		/// .dll) file for the assembly to be verified.</param>
		/// <returns>True if the verification was successful; otherwise, false.</returns>
		/// <remarks>VerifyStrongName is a utility function to check the validity
		/// of an assembly, taking into account registry settings.</remarks>
		public static bool VerifyStrongName(string assemblyPath)
		{
			ICLRStrongName strongName = ClrMetaHost.CurrentRuntime.GetInterface<ICLRStrongName>(
				new Guid(0xB79B0ACD, 0xF5CD, 0x409b, 0xB5, 0xA5, 0xA1, 0x62, 0x44, 0x61, 0x0B, 0x92));
			return strongName.StrongNameSignatureVerificationEx(assemblyPath, 1) != 0;
		}
	}
}
