/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	[Serializable]
	class EraseCustom : PassBasedErasureMethod
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="method">The erasure method definition for the custom method.</param>
		public EraseCustom(CustomErasureMethod method)
		{
			this.method = method;
		}

		/// <summary>
		/// Registers all defined custom methods with the method manager.
		/// </summary>
		internal static void RegisterAll()
		{
			if (DefaultPlugin.Settings.EraseCustom == null)
				return;

			Dictionary<Guid, CustomErasureMethod> methods =
				DefaultPlugin.Settings.EraseCustom;
			foreach (Guid guid in methods.Keys)
			{
				CustomErasureMethod method = methods[guid];
				Host.Instance.ErasureMethods.Add(new EraseCustom(method));
			}
		}

		public override string Name
		{
			get { return method.Name; }
		}

		public override Guid Guid
		{
			get { return method.Guid; }
		}

		protected override bool RandomizePasses
		{
			get { return method.RandomizePasses; }
		}

		protected override ErasureMethodPass[] PassesSet
		{
			get { return method.Passes; }
		}

		CustomErasureMethod method;
	}

	/// <summary>
	/// Contains information necessary to create user-defined erasure methods.
	/// </summary>
	[Serializable]
	internal class CustomErasureMethod : ISerializable
	{
		public CustomErasureMethod()
		{
			Name = string.Empty;
			Guid = Guid.Empty;
			RandomizePasses = true;
		}

		protected CustomErasureMethod(SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString("Name");
			Guid = (Guid)info.GetValue("GUID", Guid.GetType());
			RandomizePasses = info.GetBoolean("RandomizePasses");
			List<PassData> passes = (List<PassData>)
				info.GetValue("Passes", typeof(List<PassData>));

			Passes = new ErasureMethodPass[passes.Count];
			for (int i = 0; i != passes.Count; ++i)
				Passes[i] = passes[i];
		}

		public string Name { get; set; }
		public Guid Guid { get; set; }
		public bool RandomizePasses { get; set; }
		public ErasureMethodPass[] Passes { get; set; }

		#region ISerializable Members
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("GUID", Guid);
			info.AddValue("RandomizePasses", RandomizePasses);

			List<PassData> passes = new List<PassData>(Passes.Length);
			foreach (ErasureMethodPass pass in Passes)
				passes.Add(new PassData(pass));
			info.AddValue("Passes", passes);
		}

		[Serializable]
		private class PassData
		{
			public PassData(ErasureMethodPass pass)
			{
				if (pass.Function == PassBasedErasureMethod.WriteConstant)
				{
					Random = false;
					OpaqueValue = pass.OpaqueValue;
				}
				else if (pass.Function == PassBasedErasureMethod.WriteRandom)
				{
					Random = true;
				}
				else
					throw new ArgumentException(S._("The custom erasure method can only comprise " +
						"passes containing constant or random passes"));
			}

			public static implicit operator ErasureMethodPass(PassData pass)
			{
				return new ErasureMethodPass(pass.Random ?
					new ErasureMethodPass.ErasureMethodPassFunction(PassBasedErasureMethod.WriteRandom) :
						new ErasureMethodPass.ErasureMethodPassFunction(PassBasedErasureMethod.WriteConstant),
					pass.OpaqueValue);
			}

			object OpaqueValue;
			bool Random;
		}
		#endregion
	}
}
