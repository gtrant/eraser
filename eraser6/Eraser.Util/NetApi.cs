/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
	public static class NetApi
	{
		/// <summary>
		/// The NetStatisticsGet function retrieves operating statistics for a service.
		/// Currently, only the workstation and server services are supported.
		/// </summary>
		/// <param name="server">Pointer to a string that specifies the DNS or
		/// NetBIOS name of the server on which the function is to execute. If
		/// this parameter is NULL, the local computer is used.</param>
		/// <param name="service">Pointer to a string that specifies the name of
		/// the service about which to get the statistics. Only the values
		/// SERVICE_SERVER and SERVICE_WORKSTATION are currently allowed.</param>
		/// <param name="level">Specifies the information level of the data.
		/// This parameter must be 0.</param>
		/// <param name="options">This parameter must be zero.</param>
		/// <param name="bufptr">Pointer to the buffer that receives the data. The
		/// format of this data depends on the value of the level parameter.
		/// This buffer is allocated by the system and must be freed using the
		/// NetApiBufferFree function. For more information, see Network Management
		/// Function Buffers and Network Management Function Buffer Lengths.</param>
		/// <returns>If the function succeeds, the return value is NERR_Success.
		/// 
		/// If the function fails, the return value is a system error code. For
		/// a list of error codes, see System Error Codes.</returns>
		public static byte[] NetStatisticsGet(string server, NetApiService service,
			uint level, uint options)
		{
			IntPtr netAPIStats = IntPtr.Zero;
			string serviceName = "Lanman" + service.ToString();
			if (NativeMethods.NetStatisticsGet(server, serviceName, level, options, out netAPIStats) == 0)
			{
				try
				{
					//Get the size of the buffer
					uint size = 0;
					NativeMethods.NetApiBufferSize(netAPIStats, out size);
					byte[] result = new byte[size];

					//Copy the buffer
					Marshal.Copy(result, 0, netAPIStats, result.Length);

					//Return the result
					return result;
				}
				finally
				{
					//Free the statistics buffer
					NativeMethods.NetApiBufferFree(netAPIStats);
				}
			}

			return null;
		}

		internal static class NativeMethods
		{
			[DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
			public static extern uint NetStatisticsGet(string server, string service,
				uint level, uint options, out IntPtr bufptr);

			/// <summary>
			/// The NetApiBufferSize function returns the size, in bytes, of a buffer
			/// allocated by a call to the NetApiBufferAllocate function.
			/// </summary>
			/// <param name="Buffer">Pointer to a buffer returned by the NetApiBufferAllocate
			/// function.</param>
			/// <param name="ByteCount">Receives the size of the buffer, in bytes.</param>
			/// <returns>If the function succeeds, the return value is NERR_Success.
			/// 
			/// If the function fails, the return value is a system error code. For
			/// a list of error codes, see System Error Codes.</returns>
			[DllImport("Netapi32.dll")]
			public static extern uint NetApiBufferSize(IntPtr Buffer, out uint ByteCount);

			/// <summary>
			/// The NetApiBufferFree function frees the memory that the NetApiBufferAllocate
			/// function allocates. Call NetApiBufferFree to free the memory that other
			/// network management functions return.
			/// </summary>
			/// <param name="Buffer">Pointer to a buffer returned previously by another
			/// network management function.</param>
			/// <returns>If the function succeeds, the return value is NERR_Success.
			/// 
			/// If the function fails, the return value is a system error code. For
			/// a list of error codes, see System Error Codes.</returns>
			[DllImport("Netapi32.dll")]
			public static extern uint NetApiBufferFree(IntPtr Buffer);

			private const uint NERR_Success = 0;
		}
	}

	public enum NetApiService
	{
		Workstation,
		Server
	}
}
