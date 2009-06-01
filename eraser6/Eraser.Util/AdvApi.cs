/* 
 * $Id$
 * Copyright 2009 The Eraser Project
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

namespace Eraser.Util
{
	public static class AdvApi
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
		/// Checks whether the current process is running with administrative privileges.
		/// </summary>
		/// <returns>Returns true if UAC is enabled under Vista. Will return false
		/// under pre-Vista OSes</returns>
		public static bool UacEnabled()
		{
			//Check whether we're on Vista
			if (Environment.OSVersion.Platform != PlatformID.Win32NT ||
				Environment.OSVersion.Version < new Version(6, 0))
			{
				//UAC doesn't exist on these platforms.
				return false;
			}

			//Get the process token.
			SafeTokenHandle hToken = new SafeTokenHandle();
			bool result = NativeMethods.OpenProcessToken(KernelApi.NativeMethods.GetCurrentProcess(),
				NativeMethods.TOKEN_QUERY, out hToken);
			if (!result || hToken.IsInvalid)
				throw KernelApi.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

			IntPtr pElevationType = Marshal.AllocHGlobal(Marshal.SizeOf(
				typeof(NativeMethods.TOKEN_ELEVATION_TYPE)));
			try
			{
				//Get the token information for our current process.
				uint returnSize = 0;
				result = NativeMethods.GetTokenInformation(hToken,
					NativeMethods.TOKEN_INFORMATION_CLASS.TokenElevationType,
					pElevationType, sizeof(NativeMethods.TOKEN_ELEVATION_TYPE),
					out returnSize);

				//Check the return code
				if (!result)
					throw KernelApi.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

				NativeMethods.TOKEN_ELEVATION_TYPE elevationType =
					(NativeMethods.TOKEN_ELEVATION_TYPE)Marshal.PtrToStructure(
						pElevationType, typeof(NativeMethods.TOKEN_ELEVATION_TYPE));
				return elevationType != NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
			}
			finally
			{
				Marshal.FreeHGlobal(pElevationType);
			}
		}

		/// <summary>
		/// Stores functions, structs and constants from Advapi32.dll
		/// </summary>
		internal static class NativeMethods
		{
			/// <summary>
			/// The CryptAcquireContext function is used to acquire a handle to a
			/// particular key container within a particular cryptographic service
			/// provider (CSP). This returned handle is used in calls to CryptoAPI
			/// functions that use the selected CSP.
			/// 
			/// This function first attempts to find a CSP with the characteristics
			/// described in the dwProvType and pszProvider parameters. If the CSP
			/// is found, the function attempts to find a key container within the
			/// CSP that matches the name specified by the pszContainer parameter.
			/// To acquire the context and the key container of a private key
			/// associated with the public key of a certificate, use
			/// CryptAcquireCertificatePrivateKey.
			/// 
			/// With the appropriate setting of dwFlags, this function can also create
			/// and destroy key containers and can provide access to a CSP with a
			/// temporary key container if access to a private key is not required.
			/// </summary>
			/// <param name="phProv">A pointer to a handle of a CSP. When you have
			/// finished using the CSP, release the handle by calling the
			/// CryptReleaseContext function.</param>
			/// <param name="pszContainer">The key container name. This is a
			/// null-terminated string that identifies the key container to the CSP.
			/// This name is independent of the method used to store the keys.
			/// Some CSPs store their key containers internally (in hardware),
			/// some use the system registry, and others use the file system. When
			/// dwFlags is set to CRYPT_VERIFYCONTEXT, pszContainer must be set to NULL.
			/// 
			/// When pszContainer is NULL, a default key container name is used. For
			/// example, the Microsoft Base Cryptographic Provider uses the logon name
			/// of the currently logged on user as the key container name. Other CSPs
			/// can also have default key containers that can be acquired in this way.
			/// 
			/// Applications must not use the default key container to store private
			/// keys. When multiple applications use the same container, one application
			/// can change or destroy the keys that another application needs to have
			/// available. If applications use key containers linked to the application,
			/// the risk is reduced of other applications tampering with keys necessary
			/// for proper function.
			/// 
			/// An application can obtain the name of the key container in use by
			/// reading the PP_CONTAINER value with the CryptGetProvParam function.</param>
			/// <param name="pszProvider">A null-terminated string that specifies the
			/// name of the CSP to be used.
			/// 
			/// If this parameter is NULL, the user default provider is used. For more
			/// information, see Cryptographic Service Provider Contexts. For a list
			/// of available cryptographic providers, see Cryptographic Provider Names.
			/// 
			/// An application can obtain the name of the CSP in use by using the
			/// CryptGetProvParam function to read the PP_NAME CSP value in the dwParam
			/// parameter.
			/// 
			/// Due to changing export control restrictions, the default CSP can change
			/// between operating system releases. To ensure interoperability on
			/// different operating system platforms, the CSP should be explicitly
			/// set by using this parameter instead of using the default CSP.</param>
			/// <param name="dwProvType">Specifies the type of provider to acquire.
			/// Defined provider types are discussed in Cryptographic Provider Types.</param>
			/// <param name="dwFlags">Flag values. This parameter is usually set to zero,
			/// but some applications set one or more flags.</param>
			/// <returns> If the function succeeds, the function returns nonzero (TRUE).
			/// If the function fails, it returns zero (FALSE). For extended error
			/// information, call Marshal.GetLastWin32Error.</returns>
			[DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CryptAcquireContext(out SafeCryptHandle phProv,
				string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);

			/// <summary>
			/// The CryptGenRandom function fills a buffer with cryptographically random bytes.
			/// </summary>
			/// <param name="hProv">Handle of a cryptographic service provider (CSP)
			/// created by a call to CryptAcquireContext.</param>
			/// <param name="dwLen">Number of bytes of random data to be generated.</param>
			/// <param name="pbBuffer">Buffer to receive the returned data. This buffer
			/// must be at least dwLen bytes in length.
			/// 
			/// Optionally, the application can fill this buffer with data to use as
			/// an auxiliary random seed.</param>
			[DllImport("Advapi32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CryptGenRandom(SafeCryptHandle hProv, uint dwLen,
				byte[] pbBuffer);

			/// <summary>
			/// The CryptReleaseContext function releases the handle of a cryptographic
			/// service provider (CSP) and a key container. At each call to this function,
			/// the reference count on the CSP is reduced by one. When the reference
			/// count reaches zero, the context is fully released and it can no longer
			/// be used by any function in the application.
			/// 
			/// An application calls this function after finishing the use of the CSP.
			/// After this function is called, the released CSP handle is no longer
			/// valid. This function does not destroy key containers or key pairs</summary>
			/// <param name="hProv">Handle of a cryptographic service provider (CSP)
			/// created by a call to CryptAcquireContext.</param>
			/// <param name="dwFlags">Reserved for future use and must be zero. If
			/// dwFlags is not set to zero, this function returns FALSE but the CSP
			/// is released.</param>
			/// <returns>If the function succeeds, the return value is nonzero (TRUE).
			/// 
			/// If the function fails, the return value is zero (FALSE). For extended
			/// error information, call Marshal.GetLastWin32Error.</returns>
			[DllImport("Advapi32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);

			public const uint PROV_RSA_FULL = 1;
			public const uint PROV_RSA_SIG = 2;
			public const uint PROV_DSS = 3;
			public const uint PROV_FORTEZZA = 4;
			public const uint PROV_MS_EXCHANGE = 5;
			public const uint PROV_SSL = 6;
			public const uint PROV_RSA_SCHANNEL = 12;
			public const uint PROV_DSS_DH = 13;
			public const uint PROV_EC_ECDSA_SIG = 14;
			public const uint PROV_EC_ECNRA_SIG = 15;
			public const uint PROV_EC_ECDSA_FULL = 16;
			public const uint PROV_EC_ECNRA_FULL = 17;
			public const uint PROV_DH_SCHANNEL = 18;
			public const uint PROV_SPYRUS_LYNKS = 20;
			public const uint PROV_RNG = 21;
			public const uint PROV_INTEL_SEC = 22;

			public const int NTE_BAD_KEYSET = unchecked((int)0x80090016);

			public const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
			public const uint CRYPT_NEWKEYSET = 0x00000008;
			public const uint CRYPT_DELETEKEYSET = 0x00000010;
			public const uint CRYPT_MACHINE_KEYSET = 0x00000020;
			public const uint CRYPT_SILENT = 0x00000040;

			/// <summary>
			/// The GetTokenInformation function retrieves a specified type of information
			/// about an access token. The calling process must have appropriate access
			/// rights to obtain the information.
			/// </summary>
			/// <param name="TokenHandle">A handle to an access token from which
			/// information is retrieved. If TokenInformationClass specifies TokenSource,
			/// the handle must have TOKEN_QUERY_SOURCE access. For all other
			/// TokenInformationClass values, the handle must have TOKEN_QUERY access.</param>
			/// <param name="TokenInformationClass">Specifies a value from the
			/// TOKEN_INFORMATION_CLASS enumerated type to identify the type of
			/// information the function retrieves.</param>
			/// <param name="TokenInformation">A pointer to a buffer the function
			/// fills with the requested information. The structure put into this
			/// buffer depends upon the type of information specified by the
			/// TokenInformationClass parameter.</param>
			/// <param name="TokenInformationLength">Specifies the size, in bytes,
			/// of the buffer pointed to by the TokenInformation parameter.
			/// If TokenInformation is NULL, this parameter must be zero.</param>
			/// <param name="ReturnLength">A pointer to a variable that receives the
			/// number of bytes needed for the buffer pointed to by the TokenInformation
			/// parameter. If this value is larger than the value specified in the
			/// TokenInformationLength parameter, the function fails and stores no
			/// data in the buffer.
			/// 
			/// If the value of the TokenInformationClass parameter is TokenDefaultDacl
			/// and the token has no default DACL, the function sets the variable pointed
			/// to by ReturnLength to sizeof(TOKEN_DEFAULT_DACL) and sets the
			/// DefaultDacl member of the TOKEN_DEFAULT_DACL structure to NULL.</param>
			/// <returns> If the function succeeds, the return value is true. To get
			/// extended error information, call Marshal.GetLastWin32Error().</returns>
			[DllImport("Advapi32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool GetTokenInformation(SafeTokenHandle TokenHandle,
				TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation,
				uint TokenInformationLength, out uint ReturnLength);

			/// <summary>
			/// The OpenProcessToken function opens the access token associated with a process.
			/// </summary>
			/// <param name="ProcessHandle">A handle to the process whose access token
			/// is opened. The process must have the PROCESS_QUERY_INFORMATION access
			/// permission.</param>
			/// <param name="DesiredAccess">Specifies an access mask that specifies
			/// the requested types of access to the access token. These requested
			/// access types are compared with the discretionary access control
			/// list (DACL) of the token to determine which accesses are granted or
			/// denied.</param>
			/// <param name="TokenHandle">A pointer to a handle that identifies the
			/// newly opened access token when the function returns.</param>
			/// <returns> If the function succeeds, the return value is true. To get
			/// extended error information, call Marshal.GetLastWin32Error().</returns>
			[DllImport("Advapi32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool OpenProcessToken(IntPtr ProcessHandle,
				UInt32 DesiredAccess, out SafeTokenHandle TokenHandle);

			public const uint STANDARD_RIGHTS_REQUIRED = 0xF0000;
			public const uint TOKEN_ASSIGN_PRIMARY = 0x00001;
			public const uint TOKEN_DUPLICATE = 0x00002;
			public const uint TOKEN_IMPERSONATE = 0x00004;
			public const uint TOKEN_QUERY = 0x00008;
			public const uint TOKEN_QUERY_SOURCE = 0x00010;
			public const uint TOKEN_ADJUST_PRIVILEGES = 0x00020;
			public const uint TOKEN_ADJUST_GROUPS = 0x00040;
			public const uint TOKEN_ADJUST_DEFAULT = 0x00080;
			public const uint TOKEN_ADJUST_SESSIONID = 0x00100;
			public const uint TOKEN_ALL_ACCESS_P = (STANDARD_RIGHTS_REQUIRED |
				TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY |
				TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS |
				TOKEN_ADJUST_DEFAULT);

			public enum TOKEN_INFORMATION_CLASS
			{
				TokenUser = 1,
				TokenGroups = 2,
				TokenPrivileges = 3,
				TokenOwner = 4,
				TokenPrimaryGroup = 5,
				TokenDefaultDacl = 6,
				TokenSource = 7,
				TokenType = 8,
				TokenImpersonationLevel = 9,
				TokenStatistics = 10,
				TokenRestrictedSids = 11,
				TokenSessionId = 12,
				TokenGroupsAndPrivileges = 13,
				TokenSessionReference = 14,
				TokenSandBoxInert = 15,
				TokenAuditPolicy = 16,
				TokenOrigin = 17,
				TokenElevationType = 18,
				TokenLinkedToken = 19,
				TokenElevation = 20,
				TokenHasRestrictions = 21,
				TokenAccessInformation = 22,
				TokenVirtualizationAllowed = 23,
				TokenVirtualizationEnabled = 24,
				TokenIntegrityLevel = 25,
				TokenUIAccess = 26,
				TokenMandatoryPolicy = 27,
				TokenLogonSid = 28,
				MaxTokenInfoClass = 29  // MaxTokenInfoClass should always be the last enum
			}

			public enum TOKEN_ELEVATION_TYPE
			{
				TokenElevationTypeDefault = 1,
				TokenElevationTypeFull = 2,
				TokenElevationTypeLimited = 3,
			}
		}
	}

	public sealed class CryptApi : IDisposable
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		private CryptApi()
		{
			/* Intel i8xx (82802 Firmware Hub Device) hardware random number generator */
			const string IntelDefaultProvider = "Intel Hardware Cryptographic Service Provider";

			handle = new SafeCryptHandle();
			if (AdvApi.NativeMethods.CryptAcquireContext(out handle, string.Empty,
				IntelDefaultProvider, AdvApi.NativeMethods.PROV_INTEL_SEC, 0))
			{
				return;
			}
			else if (AdvApi.NativeMethods.CryptAcquireContext(out handle, string.Empty,
				string.Empty, AdvApi.NativeMethods.PROV_RSA_FULL, 0))
			{
				return;
			}
			else if (Marshal.GetLastWin32Error() == AdvApi.NativeMethods.NTE_BAD_KEYSET)
			{
				//Default keyset doesn't exist, attempt to create a new one
				if (AdvApi.NativeMethods.CryptAcquireContext(out handle, string.Empty,
					string.Empty, AdvApi.NativeMethods.PROV_RSA_FULL,
					AdvApi.NativeMethods.CRYPT_NEWKEYSET))
				{
					return;
				}
			}

			throw new NotSupportedException("Unable to acquire a cryptographic service provider.");
		}

		#region IDisposable Members
		~CryptApi()
		{
			Dispose(false);
		}

		public void Dispose(bool disposing)
		{
			if (disposing)
				handle.Close();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		/// <summary>
		/// The GenRandom function fills a buffer with cryptographically random bytes.
		/// </summary>
		/// <param name="buffer">Buffer to receive the returned data. This buffer
		/// must be at least dwLen bytes in length.
		/// 
		/// Optionally, the application can fill this buffer with data to use as
		/// an auxiliary random seed.</param>
		public static bool CryptGenRandom(byte[] buffer)
		{
			return AdvApi.NativeMethods.CryptGenRandom(instance.handle,
				(uint)buffer.Length, buffer);
		}

		/// <summary>
		/// The HCRYPTPROV handle.
		/// </summary>
		private SafeCryptHandle handle;

		/// <summary>
		/// The global CryptAPI instance.
		/// </summary>
		private static CryptApi instance = new CryptApi();
	}

	internal class SafeCryptHandle : SafeHandle
	{
		public SafeCryptHandle()
			: base(IntPtr.Zero, true)
		{
		}

		public override bool IsInvalid
		{
			get { return handle == IntPtr.Zero; }
		}

		protected override bool ReleaseHandle()
		{
			AdvApi.NativeMethods.CryptReleaseContext(handle, 0u);
			handle = IntPtr.Zero;
			return true;
		}
	}

	internal class SafeTokenHandle : SafeHandle
	{
		public SafeTokenHandle()
			: base(IntPtr.Zero, true)
		{
		}

		public override bool IsInvalid
		{
			get { return handle == IntPtr.Zero; }
		}

		protected override bool ReleaseHandle()
		{
			KernelApi.NativeMethods.CloseHandle(handle);
			handle = IntPtr.Zero;
			return true;
		}
	}
}