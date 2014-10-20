/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

namespace Eraser.Plugins
{
	/// <summary>
	/// A base class for storing persistent data.
	/// </summary>
	public abstract class PersistentStore
	{
		/// <summary>
		/// Gets a subsection Persistent Store to hold one group of settings.
		/// </summary>
		/// <param name="subsectionName"></param>
		/// <returns></returns>
		public abstract PersistentStore GetSubsection(string subsectionName);

		/// <summary>
		/// Gets the setting for the given name, coercing the object stored in the backend
		/// to the given type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the setting that is currently stored in the
		/// backend.</typeparam>
		/// <param name="name">The name of the setting that is used to uniquely refer
		/// to the value.</param>
		/// <param name="defaultValue">The default to return if the no data is assocated
		/// with the given setting.</param>
		/// <returns>The value stored in the backend, or null if none exists.</returns>
		public abstract T GetValue<T>(string name, T defaultValue);

		/// <summary>
		/// Overload for <see cref="GetValue"/> which returns a default for the given type.
		/// </summary>
		/// <typeparam name="T">The type of the setting that is currently stored in the
		/// backend.</typeparam>
		/// <param name="name">The name of the setting that is used to uniquely refer
		/// to the value.</param>
		/// <param name="defaultValue">The default to return if the no data is assocated
		/// with the given setting.</param>
		/// <returns>The value stored in the backend, or null if none exists.</returns>
		public T GetValue<T>(string name)
		{
			return GetValue<T>(name, default(T));
		}

		/// <summary>
		/// Sets the setting with the given name.
		/// </summary>
		/// <param name="name">The name of the setting.</param>
		/// <param name="value">The value to store in the backend. This may be serialised.</param>
		public abstract void SetValue(string name, object value);
	}
}
