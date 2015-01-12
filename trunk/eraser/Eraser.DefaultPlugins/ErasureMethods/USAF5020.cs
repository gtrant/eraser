/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By:
 * 
 * The algorithm in this file is implemented using the description in EMIShredder
 * (http://www.codeplex.com/EMISecurityShredder)
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
using System.Runtime.InteropServices;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	[Guid("7BF5B185-8EA5-4e12-83F1-F6C2EFB3D2C2")]
	sealed class USAF5020 : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("US Air Force 5020"); }
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		protected override bool RandomizePasses
		{
			get { return false; }
		}

		protected override ErasureMethodPass[] PassesSet
		{
			get
			{
				IPrng prng = Host.Instance.Prngs.ActivePrng;
				int rand = prng.Next();

				return new ErasureMethodPass[]
				{
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)(rand & 0xFF) }),
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)((rand >> 8) & 0xFF) }),
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)((rand >> 16) & 0xFF) })
				};
			}
		}
	}
}
