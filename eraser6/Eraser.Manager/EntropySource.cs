/* 
 * $Id$
 * Copyright 2008-2010 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net>
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

using System.Globalization;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.IO;

using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32.SafeHandles;
using Eraser.Util;

namespace Eraser.Manager
{
	/// <summary>
	/// Provides an abstract interface to allow multiple sources of entropy into
	/// the EntropyPoller class.
	/// </summary>
	public abstract class EntropySource : IRegisterable
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		protected EntropySource()
		{
		}

		/// <summary>
		/// The name of the entropy source
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// The guid representing this entropy source
		/// </summary>
		public abstract Guid Guid
		{
			get;
		}

		/// <summary>
		/// Gets a primer to add to the pool when this source is first initialised, to
		/// further add entropy to the pool.
		/// </summary>
		/// <returns>A byte array containing the entropy.</returns>
		public abstract byte[] GetPrimer();

		/// <summary>
		/// Retrieve entropy from a source which will have slow rate of
		/// entropy polling.
		/// </summary>
		/// <returns></returns>
		public abstract byte[] GetSlowEntropy();

		/// <summary>
		/// Retrieve entropy from a soruce which will have a fast rate of 
		/// entropy polling.
		/// </summary>
		/// <returns></returns>
		public abstract byte[] GetFastEntropy();

		/// <summary>
		/// Gets entropy from the entropy source. This will be called repetitively.
		/// </summary>
		/// <returns>A byte array containing the entropy, both slow rate and fast rate.</returns>
		public abstract byte[] GetEntropy();

		/// <summary>
		/// Converts value types into a byte array. This is a helper function to allow
		/// inherited classes to convert value types into byte arrays which can be
		/// returned to the EntropyPoller class.
		/// </summary>
		/// <typeparam name="T">Any value type</typeparam>
		/// <param name="entropy">A value which will be XORed with pool contents.</param>
		protected static byte[] StructToBuffer<T>(T entropy) where T : struct
		{
			int sizeofObject = Marshal.SizeOf(entropy);
			IntPtr memory = Marshal.AllocHGlobal(sizeofObject);
			try
			{
				Marshal.StructureToPtr(entropy, memory, false);
				byte[] dest = new byte[sizeofObject];

				//Copy the memory
				Marshal.Copy(memory, dest, 0, sizeofObject);
				return dest;
			}
			finally
			{
				Marshal.FreeHGlobal(memory);
			}
		}
	}

	/// <summary>
	/// A class which manages all of the instances of the EntropySources
	/// available. Plugins could register their entropy sources via this class.
	/// </summary>
	public class EntropySourceRegistrar : Registrar<EntropySource>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		internal EntropySourceRegistrar()
		{
			Poller = new EntropyPoller();
			Poller.AddEntropySource(new KernelEntropySource());
		}

		/// <summary>
		/// Gets the entropy poller instance associated with this manager.
		/// </summary>
		public EntropyPoller Poller { get; private set; }
		
		/// <summary>
		/// The list of currently registered Entropy Sources.
		/// </summary>
		private Dictionary<Guid, EntropySource> sources = new Dictionary<Guid, EntropySource>();
	};
		
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
				return "Kernel Entropy Source";
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

			//CryptGenRandom
			byte[] cryptGenRandom = new byte[160];
			if (Security.Randomise(cryptGenRandom))
				result.AddRange(cryptGenRandom);

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

#if false
			//Get disk I/O statistics for all the hard drives
			try
			{
				for (int drive = 0; ; ++drive)
				{
					//Try to open the drive.
					StreamInfo info = new StreamInfo(string.Format(CultureInfo.InvariantCulture,
						"\\\\.\\PhysicalDrive{0}", drive));
					using (FileStream stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						SafeFileHandle device = stream.SafeFileHandle;
						KernelApi.DiskPerformanceInfo diskPerformance =
							KernelApi.QueryDiskPerformanceInfo(device);
						if (diskPerformance != null)
						{
							result.AddRange(StructToBuffer(diskPerformance.BytesRead));
							result.AddRange(StructToBuffer(diskPerformance.BytesWritten));
							result.AddRange(StructToBuffer(diskPerformance.IdleTime));
							result.AddRange(StructToBuffer(diskPerformance.QueryTime));
							result.AddRange(StructToBuffer(diskPerformance.QueueDepth));
							result.AddRange(StructToBuffer(diskPerformance.ReadCount));
							result.AddRange(StructToBuffer(diskPerformance.ReadTime));
							result.AddRange(StructToBuffer(diskPerformance.SplitCount));
							result.AddRange(StructToBuffer(diskPerformance.StorageDeviceNumber));
							result.AddRange(Encoding.UTF8.GetBytes(diskPerformance.StorageManagerName));
							result.AddRange(StructToBuffer(diskPerformance.WriteCount));
							result.AddRange(StructToBuffer(diskPerformance.WriteTime));
						}
					}
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
#endif
			//Finally, our good friend CryptGenRandom()
			byte[] cryptGenRandom = new byte[1536];
			if (Security.Randomise(cryptGenRandom))
				result.AddRange(cryptGenRandom);

			return result.ToArray();
		}
	}

	/// <summary>
	/// A class which uses EntropyPoll class to fetch system data as a source of
	/// randomness at "regular" but "random" intervals
	/// </summary>
	public class EntropyPoller
	{
		/// <summary>
		/// The algorithm used for mixing
		/// </summary>
		private enum PRFAlgorithms
		{
			Md5,
			Sha1,
			Ripemd160,
			Sha256,
			Sha384,
			Sha512,
		};

		/// <summary>
		/// Constructor.
		/// </summary>
		public EntropyPoller()
		{
			//Create the pool.
			pool = new byte[sizeof(uint) << 7];

			//Then start the thread which maintains the pool.
			Thread = new Thread(Main);
			Thread.Start();
		}

		/// <summary>
		/// The PRNG entropy thread. This thread will run in the background, getting
		/// random data to be used for entropy. This will maintain the integrity
		/// of generated data from the PRNGs.
		/// </summary>
		private void Main()
		{
			//This entropy thread will utilize a polling loop.
			DateTime lastAddedEntropy = DateTime.Now;
			TimeSpan managerEntropySpan = new TimeSpan(0, 10, 0);
			Stopwatch st = new Stopwatch();

			while (Thread.ThreadState != System.Threading.ThreadState.AbortRequested)
			{
				st.Start();
				lock (EntropySources)
					foreach (EntropySource src in EntropySources)
					{
						byte[] entropy = src.GetEntropy();
						AddEntropy(entropy);
					}

				st.Stop();
				// 2049 = bin '100000000001' ==> great avalanche
				Thread.Sleep(2000 + (int)(st.ElapsedTicks % 2049L));
				st.Reset();

				// Send entropy to the PRNGs for new seeds.
				if (DateTime.Now - lastAddedEntropy > managerEntropySpan)
					ManagerLibrary.Instance.PrngRegistrar.AddEntropy(GetPool());
			}
		}

		/// <summary>
		/// Stops the execution of the thread.
		/// </summary>
		public void Abort()
		{
			Thread.Abort();
		}

		/// <summary>
		/// Adds a new Entropy Source to the Poller.
		/// </summary>
		/// <param name="source">The EntropySource object to add.</param>
		public void AddEntropySource(EntropySource source)
		{
			lock (EntropySources)
				EntropySources.Add(source);

			AddEntropy(source.GetPrimer());
			MixPool();

			//Apply whitening effect
			PRFAlgorithm = PRFAlgorithms.Ripemd160;
			MixPool();
			PRFAlgorithm = PRFAlgorithms.Sha512;
		}

		/// <summary>
		/// Retrieves the current contents of the entropy pool.
		/// </summary>
		/// <returns>A byte array containing all the randomness currently found.</returns>
		public byte[] GetPool()
		{
			//Mix and invert the pool
			MixPool();
			InvertPool();

			//Return a safe copy
			lock (poolLock)
			{
				byte[] result = new byte[pool.Length];
				pool.CopyTo(result, 0);

				return result;
			}
		}

		/// <summary>
		/// Inverts the contents of the pool
		/// </summary>
		private void InvertPool()
		{
			lock (poolLock)
				unsafe
				{
					fixed (byte* fPool = pool)
					{
						uint* pPool = (uint*)fPool;
						uint poolLength = (uint)(pool.Length / sizeof(uint));
						while (poolLength-- != 0)
							*pPool = (uint)(*pPool++ ^ uint.MaxValue);
					}
				}
		}

		/// <summary>
		/// Mixes the contents of the pool.
		/// </summary>
		private void MixPool()
		{
			lock (poolLock)
			{
				//Mix the last 128 bytes first.
				const int mixBlockSize = 128;
				int hashSize = PRF.HashSize / 8;
				PRF.ComputeHash(pool, pool.Length - mixBlockSize, mixBlockSize).CopyTo(pool, 0);

				//Then mix the following bytes until wraparound is required
				int i = 0;
				for (; i < pool.Length - hashSize; i += hashSize)
					Buffer.BlockCopy(PRF.ComputeHash(pool, i,
						i + mixBlockSize >= pool.Length ? pool.Length - i : mixBlockSize),
						0, pool, i, i + hashSize >= pool.Length ? pool.Length - i : hashSize);

				//Mix the remaining blocks which require copying from the front
				byte[] combinedBuffer = new byte[mixBlockSize];
				for (; i < pool.Length; i += hashSize)
				{
					Buffer.BlockCopy(pool, i, combinedBuffer, 0, pool.Length - i);

					Buffer.BlockCopy(pool, 0, combinedBuffer, pool.Length - i,
								mixBlockSize - (pool.Length - i));

					Buffer.BlockCopy(PRF.ComputeHash(combinedBuffer, 0, mixBlockSize), 0,
						pool, i, pool.Length - i > hashSize ? hashSize : pool.Length - i);
				}
			}
		}

		/// <summary>
		/// Adds data which is random to the pool
		/// </summary>
		/// <param name="entropy">An array of data which will be XORed with pool
		/// contents.</param>
		public unsafe void AddEntropy(byte[] entropy)
		{
			lock (poolLock)
				fixed (byte* pEntropy = entropy)
				fixed (byte* pPool = pool)
				{
					int size = entropy.Length;
					byte* mpEntropy = pEntropy;
					while (size > 0)
					{
						//Bring the pool position back to the front if we are at our end
						if (poolPosition >= pool.Length)
							poolPosition = 0;

						int amountToMix = Math.Min(size, pool.Length - poolPosition);
						MemoryXor(pPool + poolPosition, mpEntropy, amountToMix);
						mpEntropy = mpEntropy + amountToMix;
						size -= amountToMix;
					}
				}
		}

		/// <summary>
		/// XOR's memory a DWORD at a time.
		/// </summary>
		/// <param name="destination">The destination buffer to be XOR'ed</param>
		/// <param name="source">The source buffer to XOR with</param>
		/// <param name="size">The size of the source buffer</param>
		private static unsafe void MemoryXor(byte* destination, byte* source, int size)
		{
			// XXX: Further optomisation
			// check the memory bus frame
			// use BYTE / WORD / DWORD as required			
			
			int wsize = size / sizeof(uint);
			size -= wsize * sizeof(uint);
			uint* d = (uint*)destination;
			uint* s = (uint*)source;

			while (wsize-- > 0)
				*d++ ^= *s++;

			if (size > 0)
			{
				byte* db = (byte*)d,
				      ds = (byte*)s;
				while (size-- > 0)
					*db++ ^= *ds++;
			}
		}

		/// <summary>
		/// PRF algorithm handle
		/// </summary>
		private HashAlgorithm PRF
		{
			get
			{
				Type type = null;
				switch (PRFAlgorithm)
				{
					case PRFAlgorithms.Md5:
						type = typeof(MD5CryptoServiceProvider);
						break;
					case PRFAlgorithms.Sha1:
						type = typeof(SHA1Managed);
						break;
					case PRFAlgorithms.Ripemd160:
						type = typeof(RIPEMD160Managed);
						break;
					case PRFAlgorithms.Sha256:
						type = typeof(SHA256Managed);
						break;
					case PRFAlgorithms.Sha384:
						type = typeof(SHA384Managed);
						break;
					default:
						type = typeof(SHA512Managed);
						break;
				}

				if (type.IsInstanceOfType(prfCache))
					return prfCache;
				ConstructorInfo hashConstructor = type.GetConstructor(Type.EmptyTypes);
				return prfCache = (HashAlgorithm)hashConstructor.Invoke(null);
			}
		}

		/// <summary>
		/// The last created PRF algorithm handle.
		/// </summary>
		private HashAlgorithm prfCache;

		/// <summary>
		/// PRF algorithm identifier
		/// </summary>
		private PRFAlgorithms PRFAlgorithm = PRFAlgorithms.Sha512;

		/// <summary>
		/// The pool of data which we currently maintain.
		/// </summary>
		private byte[] pool;

		/// <summary>
		/// The next position where entropy will be added to the pool.
		/// </summary>
		private int poolPosition;

		/// <summary>
		/// The lock guarding the pool array and the current entropy addition index.
		/// </summary>
		private object poolLock = new object();

		/// <summary>
		/// The thread object.
		/// </summary>
		private Thread Thread;

		/// <summary>
		/// The list of entropy sources registered with the Poller.
		/// </summary>
		private List<EntropySource> EntropySources = new List<EntropySource>();
	}
}