/* 
 * $Id: EntropySource.cs 2055 2010-05-04 05:51:04Z lowjoel $
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
using System.Runtime.InteropServices;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// Provides an abstract interface to allow multiple sources of entropy into
	/// the EntropyPoller class.
	/// </summary>
	public interface IEntropySource : IRegisterable
	{
		/// <summary>
		/// The name of the entropy source
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// Gets a primer to add to the pool when this source is first initialised, to
		/// further add entropy to the pool.
		/// </summary>
		/// <returns>A byte array containing the entropy.</returns>
		byte[] GetPrimer();

		/// <summary>
		/// Retrieve entropy from a source which will have slow rate of
		/// entropy polling.
		/// </summary>
		/// <returns></returns>
		byte[] GetSlowEntropy();

		/// <summary>
		/// Retrieve entropy from a soruce which will have a fast rate of 
		/// entropy polling.
		/// </summary>
		/// <returns></returns>
		byte[] GetFastEntropy();

		/// <summary>
		/// Gets entropy from the entropy source. This will be called repetitively.
		/// </summary>
		/// <returns>A byte array containing the entropy, both slow rate and fast rate.</returns>
		byte[] GetEntropy();
	}
}
