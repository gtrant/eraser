﻿/* 
 * $Id$
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

namespace Eraser.Manager
{
	/// <summary>
	/// Represents an object which is able to configure a given instance of
	/// <typeparamref name="T"/> with some sort of user interaction.
	/// </summary>
	/// <typeparam name="T">The type to configure</typeparam>
	public interface IConfigurer<T>
	{
		/// <summary>
		/// Loads the configuration from the provided object.
		/// </summary>
		/// <param name="target">The object to load the configuration from.</param>
		void LoadFrom(T target);

		/// <summary>
		/// Configures the provided object.
		/// </summary>
		/// <param name="target">The object to configure.</param>
		/// <returns>True if the configuration was valid and the save operation
		/// succeeded.</returns>
		bool SaveTo(T target);
	}
}
