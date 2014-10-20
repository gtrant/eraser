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

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Eraser.Util
{
	/// <summary>
	/// Stores Mpr.dll functions, structs and constants.
	/// </summary>
	internal static partial class NativeMethods
	{
		/// <summary>
		/// The WNetOpenEnum function starts an enumeration of network resources or
		/// existing connections. You can continue the enumeration by calling the
		/// WNetEnumResource function.
		/// </summary>
		/// <param name="dwScope">Scope of the enumeration. This parameter can be one
		/// of the following values.
		/// 
		/// Value						Meaning
		/// RESOURCE_CONNECTED			Enumerate all currently connected resources. The
		///								function ignores the dwUsage parameter. For more
		///								information, see the following Remarks section.
		///	RESOURCE_CONTEXT			Enumerate only resources in the network context
		///								of the caller. Specify this value for a Network
		///								Neighborhood view. The function ignores the dwUsage
		///								parameter.
		///	RESOURCE_GLOBALNET			Enumerate all resources on the network.
		///	RESOURCE_REMEMBERED			Enumerate all remembered (persistent)
		///								connections. The function ignores the
		///								dwUsage parameter.</param>
		/// <param name="dwType">Resource types to be enumerated. This parameter can be
		/// a combination of the following values. If a network provider cannot distinguish
		/// between print and disk resources, it can enumerate all resources.
		/// This parameter is ignored unless the dwScope parameter is equal to
		/// RESOURCE_GLOBALNET. For more information, see the following Remarks section.
		/// 
		/// Value						Meaning
		/// RESOURCETYPE_ANY			All resources. This value cannot be combined
		///								with RESOURCETYPE_DISK or RESOURCETYPE_PRINT.
		///	RESOURCETYPE_DISK			All disk resources.
		///	RESOURCETYPE_PRINT			All print resources.</param>
		/// <param name="dwUsage">Resource usage type to be enumerated. This parameter
		/// can be a combination of the following values.
		/// 
		/// Value						Meaning
		/// 0							All resources.
		/// RESOURCEUSAGE_CONNECTABLE	All connectable resources.
		/// RESOURCEUSAGE_CONTAINER		All container resources.
		/// RESOURCEUSAGE_ATTACHED		Setting this value forces WNetOpenEnum to fail if
		///								the user is not authenticated. The function fails
		///								even if the network allows enumeration without
		///								authentication.
		/// RESOURCEUSAGE_ALL			Setting this value is equivalent to setting
		///								RESOURCEUSAGE_CONNECTABLE, RESOURCEUSAGE_CONTAINER,
		///								and RESOURCEUSAGE_ATTACHED.</param>
		/// <param name="lpNetResource">Pointer to a NETRESOURCE structure that specifies
		/// the container to enumerate. If the dwScope parameter is not RESOURCE_GLOBALNET,
		/// this parameter must be NULL.
		/// 
		/// If this parameter is NULL, the root of the network is assumed. (The system
		/// organizes a network as a hierarchy; the root is the topmost container in the
		/// network.)
		/// 
		/// If this parameter is not NULL, it must point to a NETRESOURCE structure. This
		/// structure can be filled in by the application or it can be returned by a call
		/// to the WNetEnumResource function. The NETRESOURCE structure must specify a
		/// container resource; that is, the RESOURCEUSAGE_CONTAINER value must be
		/// specified in the dwUsage parameter.
		/// 
		/// To enumerate all network resources, an application can begin the enumeration
		/// by calling WNetOpenEnum with the lpNetResource parameter set to NULL, and
		/// then use the returned handle to call WNetEnumResource to enumerate resources.
		/// If one of the resources in the NETRESOURCE array returned by the
		/// WNetEnumResource function is a container resource, you can call WNetOpenEnum
		/// to open the resource for further enumeration.</param>
		/// <param name="lphEnum">Pointer to an enumeration handle that can be used
		/// in a subsequent call to WNetEnumResource.</param>
		/// <returns>If the function succeeds, the return value is NO_ERROR.
		/// 
		/// If the function fails, the return value is a system error code.</returns>
		[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
		public static extern uint WNetOpenEnum(uint dwScope, uint dwType, uint dwUsage,
			IntPtr lpNetResource, out IntPtr lphEnum);

		/// <summary>
		/// The WNetEnumResource function continues an enumeration of network resources
		/// that was started by a call to the WNetOpenEnum function.
		/// </summary>
		/// <param name="hEnum">Handle that identifies an enumeration instance. This
		/// handle must be returned by the WNetOpenEnum function.</param>
		/// <param name="lpcCount">Pointer to a variable specifying the number of
		/// entries requested. If the number requested is –1, the function returns
		/// as many entries as possible.
		/// 
		/// If the function succeeds, on return the variable pointed to by this
		/// parameter contains the number of entries actually read.</param>
		/// <param name="lpBuffer">Pointer to the buffer that receives the enumeration
		/// results. The results are returned as an array of NETRESOURCE
		/// structures. Note that the buffer you allocate must be large enough to
		/// hold the structures, plus the strings to which their members point.
		/// For more information, see the following Remarks section.
		/// 
		/// The buffer is valid until the next call using the handle specified by
		/// the hEnum parameter. The order of NETRESOURCE structures in the array
		/// is not predictable.</param>
		/// <param name="lpBufferSize">Pointer to a variable that specifies the size
		/// of the lpBuffer parameter, in bytes. If the buffer is too small to receive
		/// even one entry, this parameter receives the required size of the buffer.</param>
		/// <returns>If the function succeeds, the return value is one of the
		/// following values.
		/// Return code				Description
		/// NO_ERROR				The enumeration succeeded, and the buffer
		///							contains the requested data. The calling application
		///							can continue to call WNetEnumResource to complete
		///							the enumeration.
		///	ERROR_NO_MORE_ITEMS		There are no more entries. The buffer contents are undefined.
		///	
		/// If the function fails, the return value is a system error code.</returns>
		[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
		public static extern uint WNetEnumResource(IntPtr hEnum, ref uint lpcCount,
			IntPtr lpBuffer, ref uint lpBufferSize);

		/// <summary>
		/// The WNetCloseEnum function ends a network resource enumeration started
		/// by a call to the WNetOpenEnum function.
		/// </summary>
		/// <param name="hEnum">Handle that identifies an enumeration instance. This
		/// handle must be returned by the WNetOpenEnum function.</param>
		/// <returns>If the function succeeds, the return value is NO_ERROR.
		/// 
		/// If the function fails, the return value is a system error code.</returns>
		[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
		public static extern uint WNetCloseEnum(IntPtr hEnum);

		/// <summary>
		/// The NETRESOURCE structure contains information about a network resource.
		/// The structure is returned during an enumeration of network resources.
		/// The NETRESOURCE structure is also specified when making or querying a
		/// network connection with calls to various Windows Networking functions.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct NETRESOURCE
		{
			/// <summary>
			/// The scope of the enumeration. This member can be one of the following
			/// values.
			/// Value				Meaning
			/// RESOURCE_CONNECTED	Enumerate currently connected resources. The dwUsage
			///						member cannot be specified.
			///	RESOURCE_GLOBALNET	Enumerate all resources on the network. The dwUsage
			///						member is specified.
			///	RESOURCE_REMEMBERED	Enumerate remembered (persistent) connections. The
			///						dwUsage member cannot be specified.
			/// </summary>
			public uint dwScope;

			/// <summary>
			/// The type of resource. This member can be one of the following values
			/// defined in the Winnetwk.h header file.
			/// Value				Meaning
			/// RESOURCETYPE_ANY	All resources.
			/// RESOURCETYPE_DISK	Disk resources.
			/// RESOURCETYPE_PRINT	Print resources.
			/// 
			/// The WNetEnumResource function can also return the value
			/// RESOURCETYPE_UNKNOWN if a resource is neither a disk nor a print resource.
			/// </summary>
			public uint dwType;

			/// <summary>
			/// The display options for the network object in a network browsing
			/// user interface. This member can be one of the following values
			/// defined in the Winnetwk.h header file.
			/// 
			/// Value								Meaning
			/// RESOURCEDISPLAYTYPE_GENERIC			The method used to display the object
			/// 0x00000000							does not matter.
			/// RESOURCEDISPLAYTYPE_DOMAIN			The object should be displayed as a domain.
			/// 0x00000001
			/// RESOURCEDISPLAYTYPE_SERVER			The object should be displayed as a server.
			/// 0x00000002
			/// RESOURCEDISPLAYTYPE_SHARE			The object should be displayed as a share.
			/// 0x00000003
			/// RESOURCEDISPLAYTYPE_FILE			The object should be displayed as a file.
			/// 0x00000004
			/// RESOURCEDISPLAYTYPE_GROUP			The object should be displayed as a group.
			/// 0x00000005
			/// RESOURCEDISPLAYTYPE_NETWORK			The object should be displayed as a network.
			/// 0x00000006
			/// RESOURCEDISPLAYTYPE_ROOT			The object should be displayed as a logical
			/// 0x00000007							root for the entire network.
			/// RESOURCEDISPLAYTYPE_SHAREADMIN		The object should be displayed as an
			/// 0x00000008							administrative share.
			/// RESOURCEDISPLAYTYPE_DIRECTORY		The object should be displayed as a directory.
			/// 0x00000009
			/// RESOURCEDISPLAYTYPE_TREE			The object should be displayed as a tree.
			///	0x0000000A              			This display type was used for a NetWare
			///										Directory Service (NDS) tree by the NetWare
			///										Workstation service supported on Windows XP
			///										and earlier.
			///	RESOURCEDISPLAYTYPE_NDSCONTAINER	The object should be displayed as a
			///	0x0000000A							Netware Directory Service container.
			///										This display type was used by the NetWare
			///										Workstation service supported on Windows XP
			///										and earlier.
			/// </summary>
			public uint dwDisplayType;

			/// <summary>
			/// A set of bit flags describing how the resource can be used.
			/// Note that this member can be specified only if the dwScope member is
			/// equal to RESOURCE_GLOBALNET. This member can be one of the following
			/// values.
			/// Value						Meaning
			/// RESOURCEUSAGE_CONNECTABLE	The resource is a connectable resource; the
			/// 0x00000001					name pointed to by the lpRemoteName member
			///								can be passed to the WNetAddConnection function
			///								to make a network connection.
			///	RESOURCEUSAGE_CONTAINER		The resource is a container resource; the
			///	0x00000002					name pointed to by the lpRemoteName member can
			///								be passed to the WNetOpenEnum function to
			///								enumerate the resources in the container.
			///	RESOURCEUSAGE_NOLOCALDEVICE	The resource is not a local device.
			///	0x00000004
			///	RESOURCEUSAGE_SIBLING		The resource is a sibling. This value is not
			///	0x00000008					used by Windows.
			///	RESOURCEUSAGE_ATTACHED		The resource must be attached. This value
			///	0x00000010					specifies that a function to enumerate resource
			///								this should fail if the caller is not
			///								authenticated, even if the network permits
			///								enumeration without authentication.
			/// </summary>
			public uint dwUsage;

			/// <summary>
			/// If the dwScope member is equal to RESOURCE_CONNECTED or RESOURCE_REMEMBERED,
			/// this member is a pointer to a null-terminated character string that
			/// specifies the name of a local device. This member is NULL if the
			/// connection does not use a device.
			/// </summary>
			public string lpLocalName;

			/// <summary>
			/// If the entry is a network resource, this member is a pointer to a
			/// null-terminated character string that specifies the remote network name.
			/// 
			/// If the entry is a current or persistent connection, lpRemoteName member
			/// points to the network name associated with the name pointed to by the
			/// lpLocalName member.
			/// 
			/// The string can be MAX_PATH characters in length, and it must follow the
			/// network provider's naming conventions.
			/// </summary>
			public string lpRemoteName;

			/// <summary>
			/// A pointer to a NULL-terminated string that contains a comment supplied
			/// by the network provider.
			/// </summary>
			public string lpComment;

			/// <summary>
			/// A pointer to a NULL-terminated string that contains the name of the
			/// provider that owns the resource. This member can be NULL if the
			/// provider name is unknown. To retrieve the provider name, you can
			/// call the WNetGetProviderName function.
			/// </summary>
			public string lpProvider;
		}

		public const int RESOURCE_CONNECTED = 0x00000001;
		public const int RESOURCETYPE_DISK = 0x00000001;

		/// <summary>
		/// The WNetGetConnection function retrieves the name of the network
		/// resource associated with a local device.
		/// </summary>
		/// <param name="lpLocalName">Pointer to a constant null-terminated string
		/// that specifies the name of the local device to get the network name
		/// for.</param>
		/// <param name="lpRemoteName">Pointer to a null-terminated string that
		/// receives the remote name used to make the connection.</param>
		/// <param name="lpnLength">Pointer to a variable that specifies the
		/// size of the buffer pointed to by the lpRemoteName parameter, in
		/// characters. If the function fails because the buffer is not large
		/// enough, this parameter returns the required buffer size.</param>
		/// <returns>If the function succeeds, the return value is NO_ERROR.
		/// 
		/// If the function fails, the return value is a system error code.</returns>
		[DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
		public static extern uint WNetGetConnection(string lpLocalName,
			StringBuilder lpRemoteName, ref uint lpnLength);
	}
}
