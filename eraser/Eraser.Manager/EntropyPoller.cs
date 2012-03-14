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
					foreach (IEntropySource src in EntropySources)
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
					Host.Instance.Prngs.AddEntropy(GetPool());
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
		private List<IEntropySource> EntropySources = new List<IEntropySource>();
	}
}