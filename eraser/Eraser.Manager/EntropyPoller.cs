/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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
using Eraser.Util;

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
			//Create the pool and its complement.
			Pool = new byte[sizeof(uint) << 7];
			PoolInvert = new byte[Pool.Length];
			for (uint i = 0, j = (uint)PoolInvert.Length; i < j; ++i)
				PoolInvert[i] = byte.MaxValue;

			//Handle the Entropy Source Registered event.
			Host.Instance.EntropySources.Registered += OnEntropySourceRegistered;

			//Meanwhile, add all entropy sources already registered.
			foreach (IEntropySource source in Host.Instance.EntropySources)
				AddEntropySource(source);

			//Then start the thread which maintains the pool.
			Thread = new Thread(Main);
			Thread.Priority = ThreadPriority.Lowest;
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

				//Send entropy to the PRNGs for new seeds.
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
		/// Handles the OnEntropySourceRegistered event so we can register them with
		/// ourselves.
		/// </summary>
		/// <param name="sender">The object which was registered.</param>
		/// <param name="e">Event argument.</param>
		private void OnEntropySourceRegistered(object sender, EventArgs e)
		{
			AddEntropySource((IEntropySource)sender);
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
			HashAlgorithm secondaryHash = GetSecondaryHash();
			if (secondaryHash != null)
				MixPool(secondaryHash);
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
		/// Inverts the contents of the pool.
		/// </summary>
		private void InvertPool()
		{
			lock (PoolLock)
			{
				MemoryXor(PoolInvert, 0, Pool, 0, Pool.Length);
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
						Math.Min(mixBlockSize, Pool.Length - i)), 0, Pool, i,
						Math.Min(hashSize, Pool.Length - i));

				//Mix the remaining blocks which require copying from the front
				byte[] combinedBuffer = new byte[mixBlockSize];
				for (; i < Pool.Length; i += hashSize)
				{
					int remainder = Pool.Length - i;
					Buffer.BlockCopy(Pool, i, combinedBuffer, 0, remainder);
					Buffer.BlockCopy(Pool, 0, combinedBuffer, remainder,
						mixBlockSize - remainder);

					Buffer.BlockCopy(hash.ComputeHash(combinedBuffer, 0, mixBlockSize), 0,
						Pool, i, Math.Min(hashSize, remainder));
				}
			}
		}

		/// <summary>
		/// Mixes the contents of the entropy pool using the currently specified default
		/// algorithm.
		/// </summary>
		private void MixPool()
		{
			MixPool(GetPrimaryHash());
		}

		/// <summary>
		/// Adds data which is random to the pool
		/// </summary>
		/// <param name="entropy">An array of data which will be XORed with pool
		/// contents.</param>
		public void AddEntropy(byte[] entropy)
		{
			lock (PoolLock)
			{
				for (int i = entropy.Length, j = 0; i > 0; )
				{
					//Bring the pool position back to the front if we are at our end
					if (PoolPosition >= Pool.Length)
					{
						PoolPosition = 0;
						MixPool();
					}

					int amountToMix = Math.Min(i, Pool.Length - PoolPosition);
					MemoryXor(entropy, j, Pool, PoolPosition, amountToMix);
					i -= amountToMix;
					j += amountToMix;
					PoolPosition += amountToMix;
				}
			}
		}

		/// <summary>
		/// Copies a specified number of bytes from a source array starting at a particular
		/// offset to a destination array starting at a particular offset.
		/// </summary>
		/// <param name="src">The source buffer.</param>
		/// <param name="srcOffset">The zero-based byte offset into src.</param>
		/// <param name="dst">The destination buffer.</param>
		/// <param name="dstOffset">The zero-based byte offset into dst.</param>
		/// <param name="count">The number of bytes to copy.</param>
		/// 
		/// <exception cref="System.ArgumentNullException"><paramref name="src"/> or
		/// <paramref name="dst"/> is null.</exception>
		/// <exception cref="System.ArgumentException"><paramref name="src"/> or
		/// <paramref name="dst"/> is not an array of primitives or the length of
		/// <paramref name="src"/> is less than <paramref name="srcOffset"/> +
		/// <paramref name="count"/> or the length of <paramref name="dst"/>
		/// is less than <paramref name="dstOffset"/> + <paramref name="count"/>.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="srcOffset"/>,
		/// <paramref name="dstOffset"/>, or <paramref name="count"/> is less than 0.</exception>
		private static void MemoryXor(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
		{
			if (src == null || dst == null)
				throw new ArgumentNullException();
			if (src.Length < srcOffset + count ||
				dst.Length < dstOffset + count)
				throw new ArgumentException();
			if (srcOffset < 0 || dstOffset < 0 || count < 0)
				throw new ArgumentOutOfRangeException();
			
			unsafe
			{
				fixed (byte* pSrc = src)
				fixed (byte* pDst = dst)
					MemoryXor64(pDst + dstOffset, pSrc + srcOffset, (uint)count);
			}
		}

		/// <summary>

		/// XOR's <paramref name="source"/> onto <paramref name="destination"/>, at the
		/// natural word alignment of the current processor.
		/// </summary>
		/// <typeparam name="T">An integral type indicating the natural word of the
		/// processor.</typeparam>
		/// <param name="destination">The destination buffer to XOR to.</param>
		/// <param name="source">The source buffer to XOR with.</param>
		/// <param name="length">The amount of data, in bytes, to XOR.</param>
		private static unsafe void MemoryXor64(byte* destination, byte* source, uint length)
		{
			//XOR the buffers using a processor word
			{
				ulong* wDestination = (ulong*)destination;
				ulong* wSource = (ulong*)source;
				for (uint i = 0, j = (uint)(length / sizeof(ulong)); i < j; ++i)
					*wDestination++ ^= *wSource++;
			}

			//XOR the remaining bytes
			{
				uint i = length - (length % sizeof(ulong));
				destination += i;
				source += i;
				for (; i < length; ++i)
					*destination++ ^= *source++;
			}
		}

		/// <summary>
		/// Gets the primary hash algorithm used for pool mixing.
		/// </summary>
		/// <returns>A hash algorithm suitable for the current platform serving as the
		/// primary hash algorithm for pool mixing.</returns>
		/// <remarks>The instance returned need not be freed as it is cached.</remarks>
		private static HashAlgorithm GetPrimaryHash()
		{
			if (PrimaryHashAlgorithmCache != null)
				return PrimaryHashAlgorithmCache;

			HashFactoryDelegate[] priorityList = new HashFactoryDelegate[] {
				delegate() { return new SHA512Cng(); },
				delegate() { return new SHA512CryptoServiceProvider(); },
				delegate() { return new SHA512Managed(); },
				delegate() { return new SHA256Cng(); },
				delegate() { return new SHA256CryptoServiceProvider(); },
				delegate() { return new SHA256Managed(); },
				delegate() { return new SHA1Cng(); },
				delegate() { return new SHA1CryptoServiceProvider(); },
				delegate() { return new SHA1Managed(); }
			};

			foreach (HashFactoryDelegate func in priorityList)
			{
				try
				{
					return PrimaryHashAlgorithmCache = func();
				}
				catch (PlatformNotSupportedException)
				{
				}
				catch (InvalidOperationException)
				{
				}
			}

			throw new InvalidOperationException(S._("No suitable hash algorithms were found " +
				"on this computer."));
		}

		/// <summary>
		/// Gets the secondary hash algorithm used for pool mixing, serving roughly analogous
		/// to key whitening.
		/// </summary>
		/// <returns>A hash algorithm suitable for the current platform serving as the
		/// secondary hash algorithm for pool mixing, or null if no secondary hash
		/// algorithm can be used (e.g. due to FIPS algorithm restrictions)</returns>
		/// <remarks>The instance returned need not be freed as it is cached.</remarks>
		private static HashAlgorithm GetSecondaryHash()
		{
			if (HasSecondaryHashAlgorithm)
				return SecondaryHashAlgorithmCache;

			HashFactoryDelegate[] priorityList = new HashFactoryDelegate[] {
				delegate() { return new RIPEMD160Managed(); }
			};

			foreach (HashFactoryDelegate func in priorityList)
			{
				try
				{
					SecondaryHashAlgorithmCache = func();
					break;
				}
				catch (PlatformNotSupportedException)
				{
				}
				catch (InvalidOperationException)
				{
				}
			}

			HasSecondaryHashAlgorithm = true;
			return SecondaryHashAlgorithmCache;
		}

		/// <summary>
		/// The function prototype for factory delegates in the Primary and Secondary hash
		/// priority lists.
		/// </summary>
		/// <returns></returns>
		private delegate HashAlgorithm HashFactoryDelegate();

		/// <summary>
		/// The pool of data which we currently maintain.
		/// </summary>
		private byte[] Pool;

		/// <summary>
		/// A pool, the same size as <see cref="Pool"/>, but containing all bitwise 1's
		/// for XOR for pool inversion
		/// </summary>
		private byte[] PoolInvert;

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

		/// <summary>
		/// Cache object for <see cref="GetPrimaryHash"/>
		/// </summary>
		private static HashAlgorithm PrimaryHashAlgorithmCache;

		/// <summary>
		/// Cache object for <see cref="GetSecondaryHash"/>
		/// </summary>
		private static HashAlgorithm SecondaryHashAlgorithmCache;

		/// <summary>
		/// Cache for whether construction for a <see cref="SecondaryHashAlgorithmCache"/>
		/// has been attempted.
		/// </summary>
		private static bool HasSecondaryHashAlgorithm;
	}
}