/* 
 * $Id: ErasureMethod.cs 2085 2010-05-09 10:00:15Z lowjoel $
 * Copyright 2008-2010 The Eraser Project
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

using System.IO;
using Eraser.Util;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// An interface class representing the method for erasure. If classes only
	/// inherit this class, then the method can only be used to erase abstract
	/// streams, not unused drive space.
	/// </summary>
	public interface IErasureMethod : IRegisterable
	{
		/// <summary>
		/// Returns a string that represents the current IErasureMethod. The suggested
		/// template is {name} ({pass count} passes)
		/// </summary>
		/// <returns>The string that represents the current IErasureMethod.</returns>
		string ToString();

		/// <summary>
		/// The name of this erase pass, used for display in the UI
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// The number of erase passes for this erasure method.
		/// </summary>
		int Passes
		{
			get;
		}

		/// <summary>
		/// Calculates the total size of the erasure data that needs to be written.
		/// This is mainly for use by the Manager to determine how much data needs
		/// to be written to disk.
		/// </summary>
		/// <param name="paths">The list containing the file paths to erase. This
		/// may be null if the list of paths are unknown.</param>
		/// <param name="targetSize">The precomputed value of the total size of
		/// the files to be erased.</param>
		/// <returns>The total size of the files that need to be erased.</returns>
		/// <remarks>This function MAY be slow. Most erasure methods can
		/// calculate this amount fairly quickly as the number of files and the
		/// total size of the files (the ones that take most computation time)
		/// are already provided. However some exceptional cases may take a
		/// long time if the data set is large.</remarks>
		long CalculateEraseDataSize(ICollection<StreamInfo> paths, long targetSize);

		/// <summary>
		/// The main bit of the class! This function is called whenever data has
		/// to be erased. Erase the stream passed in, using the given PRNG for
		/// randomness where necessary.
		/// 
		/// This function should be implemented thread-safe as using the same
		/// instance, this function may be called across different threads.
		/// </summary>
		/// <param name="stream">The stream which needs to be erased.</param>
		/// <param name="erasureLength">The length of the stream to erase. If all
		/// data in the stream should be overwritten, then pass in the maximum
		/// value for long, the function will take the minimum.</param>
		/// <param name="prng">The PRNG source for random data.</param>
		/// <param name="callback">The progress callback function.</param>
		void Erase(Stream stream, long erasureLength, IPrng prng,
			ErasureMethodProgressFunction callback);
	}

	/// <summary>
	/// A simple callback for clients to retrieve progress information from
	/// the erase method.
	/// </summary>
	/// <param name="lastWritten">The amount of data written to the stream since
	/// the last call to the delegate.</param>
	/// <param name="totalData">The total amount of data that must be written to
	/// complete the erasure.</param>
	/// <param name="currentPass">The current pass number. The total number
	/// of passes can be found from the Passes property.</param>
	public delegate void ErasureMethodProgressFunction(long lastWritten,
		long totalData, int currentPass);

	/// <summary>
	/// This class adds functionality to the ErasureMethod class to erase
	/// unused drive space.
	/// </summary>
	public interface IUnusedSpaceErasureMethod : IErasureMethod
	{
		/// <summary>
		/// This function will allow clients to erase a file in a set of files
		/// used to fill the disk, thus achieving disk unused space erasure.
		/// 
		/// By default, this function will simply call the Erase method inherited
		/// from the ErasureMethod class.
		/// 
		/// This function should be implemented thread-safe as using the same
		/// instance, this function may be called across different threads.
		/// </summary>
		/// <param name="strm">The stream which needs to be erased.</param>
		/// <param name="prng">The PRNG source for random data.</param>
		/// <param name="callback">The progress callback function.</param>
		void EraseUnusedSpace(Stream stream, IPrng prng, ErasureMethodProgressFunction callback);
	}
}
