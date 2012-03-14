/* 
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

using System.Runtime.Serialization;
using System.Security.Permissions;

using Eraser.Util;
using Eraser.Plugins.Registrars;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// Represents a generic target of erasure
	/// </summary>
	public interface IErasureTarget : ISerializable, IRegisterable
	{
		/// <summary>
		/// Retrieves the text to display representing this target.
		/// </summary>
		string ToString();

		/// <summary>
		/// The name of the type of the Erasure target.
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// The task owning this Erasure Target.
		/// </summary>
		ITask Task
		{
			get;
			set;
		}

		/// <summary>
		/// The method used for erasing the file.
		/// </summary>
		IErasureMethod Method
		{
			get;
			set;
		}

		/// <summary>
		/// Checks whether the provided erasure method is supported by this current
		/// target.
		/// </summary>
		/// <param name="method">The erasure method to check.</param>
		/// <returns>True if the erasure method is supported, false otherwise.</returns>
		bool SupportsMethod(IErasureMethod method);

		/// <summary>
		/// Gets an <see cref="IErasureTargetConfigurer"/> which contains settings for
		/// configuring this task, or null if this erasure target has no settings to be set.
		/// </summary>
		/// <remarks>The result should be able to be passed to the <see cref="Configure"/>
		/// function, and settings for this task will be according to the returned
		/// control.</remarks>
		IErasureTargetConfigurer Configurer
		{
			get;
		}

		/// <summary>
		/// Executes the given target.
		/// </summary>
		void Execute();

		/// <summary>
		/// Gets the progress manager for this Erasure Target.
		/// </summary>
		SteppedProgressManager Progress
		{
			get;
		}
	}

	/// <summary>
	/// Represents an interface for an abstract erasure target configuration
	/// object.
	/// </summary>
	public interface IErasureTargetConfigurer : ICliConfigurer<IErasureTarget>
	{
	}
}
