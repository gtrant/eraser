/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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
	[Guid("9ACDBD78-0406-4116-87E5-263E5E3B2E0D")]
	sealed class HMGIS5Baseline : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("British HMG IS5 (Baseline)"); }
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
				return new ErasureMethodPass[]
				{
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)0 })
				};
			}
		}
	}

	[Guid("45671DA4-9401-46e4-9C0D-89B94E89C8B5")]
	sealed class HMGIS5Enhanced : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("British HMG IS5 (Enhanced)"); }
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
				return new ErasureMethodPass[]
				{
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)0 }),
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)0x01 }),
					new ErasureMethodPass(WriteRandom, null),
				};
			}
		}
	}
}
