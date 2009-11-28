/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Eraser.Util
{
	/// <summary>
	/// Provides an interface to MsCoree.dll
	/// </summary>
	public static class MsCorEEApi
	{
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
			bool wasVerified = false;
			return NativeMethods.StrongNameSignatureVerificationEx(assemblyPath, false,
				out wasVerified) && wasVerified;
		}

		/// <summary>
		/// Function, struct and constants imported from MsCorEE.dll
		/// </summary>
		internal static class NativeMethods
		{
			/// <summary>
			/// Gets a value indicating whether the assembly manifest at the supplied
			/// path contains a strong name signature. 
			/// </summary>
			/// <param name="wszFilePath">The path to the portable executable (.exe or
			/// .dll) file for the assembly to be verified.</param>
			/// <param name="fForceVerification">true to perform verification, even if
			/// it is necessary to override registry settings; otherwise, false.</param>
			/// <param name="pfWasVerified">True if the strong name signature was verified;
			/// otherwise, false. pfWasVerified is also set to false if the verification
			/// was successful due to registry settings.</param>
			/// <returns>True if the verification was successful; otherwise, false.</returns>
			/// <remarks>StrongNameSignatureVerificationEx provides a capability similar to
			/// the StrongNameSignatureVerification function. However, the second input
			/// parameter and the output parameter for StrongNameSignatureVerificationEx
			/// are of type BOOLEAN instead of DWORD.</remarks>
			[DllImport("MsCoree.dll", CharSet = CharSet.Unicode)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool StrongNameSignatureVerificationEx(
				string wszFilePath, [MarshalAs(UnmanagedType.Bool)] bool fForceVerification,
				[MarshalAs(UnmanagedType.Bool)] out bool pfWasVerified);

		}
	}
}
