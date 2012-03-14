/* 
 * $Id$
 * Copyright 2008-2012 The Eraser Project
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
using System.Runtime.InteropServices;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	[Guid("B1BFAB4A-31D3-43a5-914C-E9892C78AFD8")]
	sealed class Schneier : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("Schneier 7 pass"); }
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
					new ErasureMethodPass(WriteConstant, new byte[] { 1 }),
					new ErasureMethodPass(WriteConstant, new byte[] { 0 }),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteRandom, null)
				};
			}
		}
	}
}
