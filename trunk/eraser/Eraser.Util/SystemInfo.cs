/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Garrett Trant <gtrant@users.sourceforge.net>
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
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;

namespace Eraser.Util
{
	public static class SystemInfo
	{
		/// <summary>
		/// Retrieves the current value of the high-resolution performance counter.
		/// </summary>
		public static long PerformanceCounter
		{
			get
			{
				long result = 0;
				if (NativeMethods.QueryPerformanceCounter(out result))
					return result;
				return 0;
			}
		}

		/// <summary>
		/// Gets the current CPU type of the system.
		/// </summary>
		/// <returns>One of the <see cref="ProcessorTypes"/> enumeration values.</returns>
		public static ProcessorArchitecture ProcessorArchitecture
		{
			get
			{
				NativeMethods.SYSTEM_INFO info = new NativeMethods.SYSTEM_INFO();
				NativeMethods.GetSystemInfo(out info);

				switch (info.processorArchitecture)
				{
					case NativeMethods.SYSTEM_INFO.ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64:
						return ProcessorArchitecture.Amd64;
					case NativeMethods.SYSTEM_INFO.ProcessorArchitecture.PROCESSOR_ARCHITECTURE_IA64:
						return ProcessorArchitecture.IA64;
					case NativeMethods.SYSTEM_INFO.ProcessorArchitecture.PROCESSOR_ARCHITECTURE_INTEL:
						return ProcessorArchitecture.X86;
					default:
						return ProcessorArchitecture.None;
				}
			}
		}

		/// <summary>
		/// Gets the edition of Windows that is currently running. This will only
		/// return a valid result for operating systems later than or equal to
		/// Windows Vista (RTM). All other operating systems will return
		/// <see cref="WindowsEditions.Undefined"/>.
		/// </summary>
		public static WindowsEditions WindowsEdition
		{
			get
			{
				if (Environment.OSVersion.Platform != PlatformID.Win32NT ||
					Environment.OSVersion.Version.Major < 6)
				{
					return WindowsEditions.Undefined;
				}

				WindowsEditions result;
				NativeMethods.GetProductInfo(6u, 1u, 0u, 0u, out result);
				return result;
			}
		}
	}

	public enum WindowsEditions
	{
		/// <summary>
		/// Business
		/// </summary>
		Business = 0x00000006,

		/// <summary>
		/// Business N
		/// </summary>
		BusinessN = 0x00000010,

		/// <summary>
		/// HPC Edition
		/// </summary>
		ClusterServer = 0x00000012,

		/// <summary>
		/// Server Datacenter (full installation)
		/// </summary>
		DatacenterServer = 0x00000008,

		/// <summary>
		/// Server Datacenter (core installation)
		/// </summary>
		DatacenterServerCore = 0x0000000C,

		/// <summary>
		/// Server Datacenter without Hyper-V (core installation)
		/// </summary>
		DatacenterServerCoreV = 0x00000027,

		/// <summary>
		/// Server Datacenter without Hyper-V (full installation)
		/// </summary>
		DatacenterServerV = 0x00000025,

		/// <summary>
		/// Enterprise
		/// </summary>
		Enterprise = 0x00000004,

		/// <summary>
		/// Enterprise E
		/// </summary>
		EnterpriseE = 0x00000046,

		/// <summary>
		/// Enterprise N
		/// </summary>
		EnterpriseN = 0x0000001B,

		/// <summary>
		/// Server Enterprise (full installation)
		/// </summary>
		EnterpriseServer = 0x0000000A,

		/// <summary>
		/// Server Enterprise (core installation)
		/// </summary>
		EnterpriseServerCore = 0x0000000E,

		/// <summary>
		/// Server Enterprise without Hyper-V (core installation)
		/// </summary>
		EnterpriseServerCoreV = 0x00000029,

		/// <summary>
		/// Server Enterprise for Itanium-based Systems
		/// </summary>
		EnterpriseServerIA64 = 0x0000000F,

		/// <summary>
		/// Server Enterprise without Hyper-V (full installation)
		/// </summary>
		EnterpriseServerV = 0x00000026,

		/// <summary>
		/// Home Basic
		/// </summary>
		HomeBasic = 0x00000002,

		/// <summary>
		/// Home Basic E
		/// </summary>
		HomeBasicE = 0x00000043,

		/// <summary>
		/// Home Basic N
		/// </summary>
		HomeBasicN = 0x00000005,

		/// <summary>
		/// Home Premium
		/// </summary>
		HomePremium = 0x00000003,

		/// <summary>
		/// Home Premium E
		/// </summary>
		HomePremiumE = 0x00000044,

		/// <summary>
		/// Home Premium N
		/// </summary>
		HomePremiumN = 0x0000001A,

		/// <summary>
		/// Microsoft Hyper-V Server
		/// </summary>
		HyperV = 0x0000002A,

		/// <summary>
		/// Windows Essential Business Server Management Server
		/// </summary>
		MediumBusinessServerManagement = 0x0000001E,

		/// <summary>
		/// Windows Essential Business Server Messaging Server
		/// </summary>
		MediumBusinessServerMessaging = 0x00000020,

		/// <summary>
		/// Windows Essential Business Server Security Server
		/// </summary>
		MediumBusinessServerSecurity = 0x0000001F,

		/// <summary>
		/// Professional
		/// </summary>
		Professional = 0x00000030,

		/// <summary>
		/// Professional E
		/// </summary>
		ProfessionalE = 0x00000045,

		/// <summary>
		/// Professional N
		/// </summary>
		ProfessionalN = 0x00000031,

		/// <summary>
		/// Windows Server 2008 for Windows Essential Server Solutions
		/// </summary>
		ServerForSmallBusiness = 0x00000018,

		/// <summary>
		/// Windows Server 2008 without Hyper-V for Windows Essential Server Solutions
		/// </summary>
		ServerForSmallBusinessV = 0x00000023,

		/// <summary>
		/// Server Foundation
		/// </summary>
		ServerFoundation = 0x00000021,

		/// <summary>
		/// Windows Small Business Server
		/// </summary>
		SmallBusinessServer = 0x00000009,

		/// <summary>
		/// Server Standard (full installation)
		/// </summary>
		StandardServer = 0x00000007,

		/// <summary>
		/// Server Standard (core installation)
		/// </summary>
		StandardServerCore = 0x0000000D,

		/// <summary>
		/// Server Standard without Hyper-V (core installation)
		/// </summary>
		StandardServerCoreV = 0x00000028,

		/// <summary>
		/// Server Standard without Hyper-V (full installation)
		/// </summary>
		StandardServerV = 0x00000024,

		/// <summary>
		/// Starter
		/// </summary>
		Starter = 0x0000000B,

		/// <summary>
		/// Starter E
		/// </summary>
		StarterE = 0x00000042,

		/// <summary>
		/// Starter N
		/// </summary>
		StarterN = 0x0000002F,

		/// <summary>
		/// Storage Server Enterprise
		/// </summary>
		StorageEnterpriseServer = 0x00000017,

		/// <summary>
		/// Storage Server Express
		/// </summary>
		StorageExpressServer = 0x00000014,

		/// <summary>
		/// Storage Server Standard
		/// </summary>
		StorageStandardServer = 0x00000015,

		/// <summary>
		/// Storage Server Workgroup
		/// </summary>
		StorageWorkgroupServer = 0x00000016,

		/// <summary>
		/// An unknown product
		/// </summary>
		Undefined = 0x00000000,

		/// <summary>
		/// Ultimate
		/// </summary>
		Ultimate = 0x00000001,

		/// <summary>
		/// Ultimate E
		/// </summary>
		UltimateE = 0x00000047,

		/// <summary>
		/// Ultimate N
		/// </summary>
		UltimateN = 0x0000001C,

		/// <summary>
		/// Web Server (full installation)
		/// </summary>
		WebServer = 0x00000011,

		/// <summary>
		/// Web Server (core installation)
		/// </summary>
		WebServerCore = 0x0000001D
	}
}