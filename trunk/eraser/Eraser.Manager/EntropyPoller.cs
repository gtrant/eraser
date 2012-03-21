/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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

using System.Threading;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.Manager
{
	/// <summary>
	/// A class which uses EntropyPoll class to fetch system data as a source of
	/// randomness at "regular" but "random" intervals
	/// </summary>
	public class EntropyPoller
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public EntropyPoller()
		{
			//Create the pool.
			Pool = new byte[sizeof(uint) << 7];

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
			//Maintain the time we last provided entropy to the PRNGs. We will only
			//provide entropy every 10 minutes.
			DateTime lastAddedEntropy = DateTime.Now;
			TimeSpan managerEntropySpan = new TimeSpan(0, 10, 0);

			while (Thread.ThreadState != System.Threading.ThreadState.AbortRequested)
			{
				lock (EntropySources)
					foreach (IEntropySource src in EntropySources)
					{
						byte[] entropy = src.GetEntropy();
						AddEntropy(entropy);
					}

				//Sleep for a "random" period between roughly [2, 5) seconds from now
				Thread.Sleep(2000 + (int)(DateTime.Now.Ticks % 2999));

				// Send entropy to the PRNGs for new seeds.
				DateTime now = DateTime.Now;
				if (now - lastAddedEntropy > managerEntropySpan)
				{
					Host.Instance.Prngs.AddEntropy(GetPool());
					lastAddedEntropy = now;
				}
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
		public void AddEntropySource(IEntropySource source)
		{
			lock (EntropySources)
				EntropySources.Add(source);

			AddEntropy(source.GetPrimer());
			MixPool();

			//Apply "whitening" effect. Try to mix the pool using RIPEMD-160 to strengthen
			//the cryptographic strength of the pool.
			//There is a need to catch the InvalidOperationException because if Eraser is
			//running under an OS with FIPS-compliance mode the RIPEMD-160 algorithm cannot
			//be used.
			try
			{
				using (HashAlgorithm hash = new RIPEMD160Managed())
					MixPool(hash);
			}
			catch (InvalidOperationException)
			{
			}
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
			lock (PoolLock)
			{
				byte[] result = new byte[Pool.Length];
				Pool.CopyTo(result, 0);

				return result;
			}
		}

		/// <summary>
		/// Inverts the contents of the pool
		/// </summary>
		private void InvertPool()
		{
			lock (PoolLock)
				unsafe
				{
					fixed (byte* fPool = Pool)
					{
						uint* pPool = (uint*)fPool;
						uint poolLength = (uint)(Pool.Length / sizeof(uint));
						while (poolLength-- != 0)
							*pPool = (uint)(*pPool++ ^ uint.MaxValue);
					}
				}
		}

		/// <summary>
		/// Mixes the contents of the pool.
		/// </summary>
		private void MixPool(HashAlgorithm hash)
		{
			lock (PoolLock)
			{
				//Mix the last 128 bytes first.
				const int mixBlockSize = 128;
				int hashSize = hash.HashSize / 8;
				hash.ComputeHash(Pool, Pool.Length - mixBlockSize, mixBlockSize).CopyTo(Pool, 0);

				//Then mix the following bytes until wraparound is required
				int i = 0;
				for (; i < Pool.Length - hashSize; i += hashSize)
					Buffer.BlockCopy(hash.ComputeHash(Pool, i,
						i + mixBlockSize >= Pool.Length ? Pool.Length - i : mixBlockSize),
						0, Pool, i, i + hashSize >= Pool.Length ? Pool.Length - i : hashSize);

				//Mix the remaining blocks which require copying from the front
				byte[] combinedBuffer = new byte[mixBlockSize];
				for (; i < Pool.Length; i += hashSize)
				{
					Buffer.BlockCopy(Pool, i, combinedBuffer, 0, Pool.Length - i);

					Buffer.BlockCopy(Pool, 0, combinedBuffer, Pool.Length - i,
								mixBlockSize - (Pool.Length - i));

					Buffer.BlockCopy(hash.ComputeHash(combinedBuffer, 0, mixBlockSize), 0,
						Pool, i, Pool.Length - i > hashSize ? hashSize : Pool.Length - i);
				}
			}
		}

		/// <summary>
		/// Mixes the contents of the entropy pool using the currently specified default
		/// algorithm.
		/// </summary>
		private void MixPool()
		{
			using (HashAlgorithm hash = new SHA1CryptoServiceProvider())
				MixPool(hash);
		}

		/// <summary>
		/// Adds data which is random to the pool
		/// </summary>
		/// <param name="entropy">An array of data which will be XORed with pool
		/// contents.</param>
		public unsafe void AddEntropy(byte[] entropy)
		{
			lock (PoolLock)
				fixed (byte* pEntropy = entropy)
				fixed (byte* pPool = Pool)
				{
					int size = entropy.Length;
					byte* mpEntropy = pEntropy;
					while (size > 0)
					{
						//Bring the pool position back to the front if we are at our end
						if (PoolPosition >= Pool.Length)
							PoolPosition = 0;

						int amountToMix = Math.Min(size, Pool.Length - PoolPosition);
						MemoryXor(pPool + PoolPosition, mpEntropy, amountToMix);
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
		/// The pool of data which we currently maintain.
		/// </summary>
		private byte[] Pool;

		/// <summary>
		/// The next position where entropy will be added to the pool.
		/// </summary>
		private int PoolPosition;

		/// <summary>
		/// The lock guarding the pool array and the current entropy addition index.
		/// </summary>
		private object PoolLock = new object();

		/// <summary>
		/// The thread object.
		/// </summary>
		private Thread Thread;

		/// <summary>
		/// The list of entropy sources registered with the Poller.
		/// </summary>
		private List<IEntropySource> EntropySources = new List<IEntropySource>();
	}
}