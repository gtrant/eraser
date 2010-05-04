using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32.SafeHandles;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Provides means of generating random entropy from the system or user space
	/// randomness.
	/// This class is hardcoded into the Manager Library as we need at least one
	/// instance of such behaviour within our system. The other classes could be
	/// implemented as plugins, managed by EntropySourceManager.
	/// </summary>
	public class KernelEntropySource : EntropySource
	{
		public override byte[] GetPrimer()
		{
			List<byte> result = new List<byte>();

			//Process information
			result.AddRange(StructToBuffer(Process.GetCurrentProcess().Id));
			result.AddRange(StructToBuffer(Process.GetCurrentProcess().StartTime.Ticks));

			result.AddRange(GetFastEntropy());
			result.AddRange(GetSlowEntropy());
			return result.ToArray();
		}

		public override Guid Guid
		{
			get
			{
				return new Guid("{11EDCECF-AD81-4e50-A73D-B9CF1F813093}");
			}
		}

		public override string Name
		{
			get
			{
				return S._("Kernel Entropy Source");
			}
		}

		public override byte[] GetEntropy()
		{
			List<byte> result = new List<byte>();
			result.AddRange(GetFastEntropy());
			result.AddRange(GetSlowEntropy());

			return result.ToArray();
		}

		/// <summary>
		/// Retrieves entropy from quick sources.
		/// </summary>
		public override byte[] GetFastEntropy()
		{
			List<byte> result = new List<byte>();

			//Add the free disk space to the pool
			result.AddRange(StructToBuffer(new DriveInfo(new DirectoryInfo(Environment.SystemDirectory).
				Root.FullName).TotalFreeSpace));

			//Miscellaneous window handles
			result.AddRange(StructToBuffer(UserApi.MessagePos));
			result.AddRange(StructToBuffer(UserApi.MessageTime));

			//The caret and cursor positions
			result.AddRange(StructToBuffer(UserApi.CaretPos));
			result.AddRange(StructToBuffer(Cursor.Position));

			//Currently running threads (dynamic, but not very)
			Process currProcess = Process.GetCurrentProcess();
			foreach (ProcessThread thread in currProcess.Threads)
				result.AddRange(StructToBuffer(thread.Id));

			//Various process statistics
			result.AddRange(StructToBuffer(currProcess.VirtualMemorySize64));
			result.AddRange(StructToBuffer(currProcess.MaxWorkingSet));
			result.AddRange(StructToBuffer(currProcess.MinWorkingSet));
			result.AddRange(StructToBuffer(currProcess.NonpagedSystemMemorySize64));
			result.AddRange(StructToBuffer(currProcess.PagedMemorySize64));
			result.AddRange(StructToBuffer(currProcess.PagedSystemMemorySize64));
			result.AddRange(StructToBuffer(currProcess.PeakPagedMemorySize64));
			result.AddRange(StructToBuffer(currProcess.PeakVirtualMemorySize64));
			result.AddRange(StructToBuffer(currProcess.PeakWorkingSet64));
			result.AddRange(StructToBuffer(currProcess.PrivateMemorySize64));
			result.AddRange(StructToBuffer(currProcess.WorkingSet64));
			result.AddRange(StructToBuffer(currProcess.HandleCount));

			//Amount of free memory
			ComputerInfo computerInfo = new ComputerInfo();
			result.AddRange(StructToBuffer(computerInfo.AvailablePhysicalMemory));
			result.AddRange(StructToBuffer(computerInfo.AvailableVirtualMemory));

			//Process execution times
			result.AddRange(StructToBuffer(currProcess.TotalProcessorTime));
			result.AddRange(StructToBuffer(currProcess.UserProcessorTime));
			result.AddRange(StructToBuffer(currProcess.PrivilegedProcessorTime));

			//Thread execution times
			foreach (ProcessThread thread in currProcess.Threads)
			{
				try
				{
					result.AddRange(StructToBuffer(thread.TotalProcessorTime));
					result.AddRange(StructToBuffer(thread.UserProcessorTime));
					result.AddRange(StructToBuffer(thread.PrivilegedProcessorTime));
				}
				catch (InvalidOperationException)
				{
					//Caught when the thread has exited in the middle of the foreach.
				}
				catch (System.ComponentModel.Win32Exception e)
				{
					if (e.NativeErrorCode != Win32ErrorCode.AccessDenied)
						throw;
				}
			}

			//Current system time
			result.AddRange(StructToBuffer(DateTime.Now.Ticks));

			//The high resolution performance counter
			result.AddRange(StructToBuffer(SystemInfo.PerformanceCounter));

			//Ticks since start up
			result.AddRange(StructToBuffer(Environment.TickCount));
			return result.ToArray();
		}

		/// <summary>
		/// Retrieves entropy from sources which are relatively slower than those from
		/// the FastAddEntropy function.
		/// </summary>
		public override byte[] GetSlowEntropy()
		{
			List<byte> result = new List<byte>();

			//NetAPI statistics
			byte[] netApiStats = NetApi.NetStatisticsGet(null, NetApiService.Workstation, 0, 0);
			if (netApiStats != null)
				result.AddRange(netApiStats);

			foreach (VolumeInfo info in VolumeInfo.Volumes)
			{
				DiskPerformanceInfo performance = info.Performance;
				if (performance == null)
					continue;

				result.AddRange(StructToBuffer(performance.BytesRead));
				result.AddRange(StructToBuffer(performance.BytesWritten));
				result.AddRange(StructToBuffer(performance.IdleTime));
				result.AddRange(StructToBuffer(performance.QueryTime));
				result.AddRange(StructToBuffer(performance.QueueDepth));
				result.AddRange(StructToBuffer(performance.ReadCount));
				result.AddRange(StructToBuffer(performance.ReadTime));
				result.AddRange(StructToBuffer(performance.SplitCount));
				result.AddRange(StructToBuffer(performance.WriteCount));
				result.AddRange(StructToBuffer(performance.WriteTime));
			}

			return result.ToArray();
		}
	}
}
