/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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
	/// Loading policies applicable for a given plugin.
	/// </summary>
	public enum PluginLoadingPolicy
	{
		/// <summary>
		/// The host decides the best policy for loading the plugin.
		/// </summary>
		None,

		/// <summary>
		/// The host will enable the plugin by default.
		/// </summary>
		DefaultOn,

		/// <summary>
		/// The host will disable the plugin by default.
		/// </summary>
		DefaultOff,

		/// <summary>
		/// The host must always load the plugin.
		/// </summary>
		/// <remarks>This policy does not have an effect when declared in the
		/// <see cref="PluginLoadingPolicyAttribute"/> attribute and will be equivalent
		/// to <see cref="None"/>.</remarks>
		Core
	}

	/// <summary>
	/// Declares the loading policy for the assembly containing the plugin. Only
	/// plugins signed with an Authenticode signature will be trusted and have
	/// this attribute checked at initialisation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class PluginLoadingPolicyAttribute : Attribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="policy">The policy used for loading the plugin.</param>
		public PluginLoadingPolicyAttribute(PluginLoadingPolicy policy)
		{
			Policy = policy;
		}

		/// <summary>
		/// The loading policy to be applied to the assembly.
		/// </summary>
		public PluginLoadingPolicy Policy
		{
			get;
			set;
		}
	}
}
