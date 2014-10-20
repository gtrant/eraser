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

using System.Windows.Forms;

namespace Eraser.Plugins
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

	/// <summary>
	/// Represents an object which is able to configure a given instance of
	/// <typeparamref name="T"/> from the Command Line.
	/// </summary>
	/// <typeparam name="T">The type to configure</typeparam>
	public interface ICliConfigurer<T> : IConfigurer<T>
	{
		/// <summary>
		/// Gets the help string for the current configurer.
		/// </summary>
		string Help();

		/// <summary>
		/// Sets the configuration of the current configurer from the provided
		/// command line argument.
		/// </summary>
		/// <param name="argument">The argument on the command line.</param>
		/// <returns>True if the argument is accepted by the configurer.</returns>
		bool ProcessArgument(string argument);
	}

	/// <summary>
	/// Represents an object which is able to transform the contents of
	/// a drag-and-drop operation into program logic.
	/// </summary>
	/// <typeparam name="T">The type to configure</typeparam>
	public interface IDragAndDropConfigurerFactory<T>
	{
		/// <summary>
		/// Retrieves the transformed collection of objects based on the
		/// contents of the provided drag-and-drop operation.
		/// </summary>
		/// <param name="e">The event argument.</param>
		/// <returns>A collection of T based on the drag-and-drop event.</returns>
		ICollection<T> ProcessArgument(DragEventArgs e);
	}
}
