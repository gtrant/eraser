/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By:
 * 
 * The Gutmann Lite algorithm in this file is implemented using the description
 * in EMIShredder (http://www.codeplex.com/EMISecurityShredder)
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
	[Guid("1407FC4E-FEFF-4375-B4FB-D7EFBB7E9922")]
	sealed class Gutmann : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("Gutmann"); }
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		protected override bool RandomizePasses
		{
			get { return true; }
		}

		protected override ErasureMethodPass[] PassesSet
		{
			get
			{
				return new ErasureMethodPass[]
				{
					new ErasureMethodPass(WriteRandom, null),                                   // 1
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteConstant, new byte[] {0x55}),                    // 5
					new ErasureMethodPass(WriteConstant, new byte[] {0xAA}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x92, 0x49, 0x24}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x49, 0x24, 0x92}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x24, 0x92, 0x49}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x00}),                    // 10
					new ErasureMethodPass(WriteConstant, new byte[] {0x11}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x22}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x33}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x44}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x55}),                    // 15
					new ErasureMethodPass(WriteConstant, new byte[] {0x66}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x77}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x88}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x99}),
					new ErasureMethodPass(WriteConstant, new byte[] {0xAA}),                    // 20
					new ErasureMethodPass(WriteConstant, new byte[] {0xBB}),
					new ErasureMethodPass(WriteConstant, new byte[] {0xCC}),
					new ErasureMethodPass(WriteConstant, new byte[] {0xDD}),
					new ErasureMethodPass(WriteConstant, new byte[] {0xEE}),
					new ErasureMethodPass(WriteConstant, new byte[] {0xFF}),                    // 25
					new ErasureMethodPass(WriteConstant, new byte[] {0x92, 0x49, 0x24}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x49, 0x24, 0x92}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x24, 0x92, 0x49}),
					new ErasureMethodPass(WriteConstant, new byte[] {0x6D, 0xB6, 0xDB}),
					new ErasureMethodPass(WriteConstant, new byte[] {0xB6, 0xDB, 0x6D}),        // 30
					new ErasureMethodPass(WriteConstant, new byte[] {0xDB, 0x6D, 0xB6}),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null)                                    // 35
				};
			}
		}
	}
}
