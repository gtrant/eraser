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
using System.Text;
using System.Runtime.InteropServices;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	[Guid("D1583631-702E-4dbf-A0E9-C35DBA481702")]
	sealed class DoD_EcE : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("US DoD 5220.22-M (8-306./E, C & E)"); }
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
				//Set passes 1, 4 and 5 to be a random value
				IPrng prng = Host.Instance.Prngs.ActivePrng;
				int rand = prng.Next();

				ErasureMethodPass[] result = new ErasureMethodPass[]
				{
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)(rand & 0xFF) }),
					new ErasureMethodPass(WriteConstant, new byte[] { 0 }),
					new ErasureMethodPass(WriteRandom, null),
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)((rand >> 8) & 0xFF) }),
					new ErasureMethodPass(WriteConstant, new byte[] { (byte)((rand >> 16) & 0xFF) }),
					new ErasureMethodPass(WriteConstant, new byte[] { 0 }),
					new ErasureMethodPass(WriteRandom, null)
				};

				//Set passes 2 and 6 to be complements of 1 and 5
				result[1] = new ErasureMethodPass(WriteConstant, new byte[] {
					(byte)(~((byte[])result[0].OpaqueValue)[0]) });
				result[5] = new ErasureMethodPass(WriteConstant, new byte[] {
					(byte)(~((byte[])result[4].OpaqueValue)[0]) });
				return result;
			}
		}
	}

	[Guid("ECBF4998-0B4F-445c-9A06-23627659E419")]
	sealed class DoD_E : PassBasedErasureMethod
	{
		public override string Name
		{
			get { return S._("US DoD 5220.22-M (8-306./E)"); }
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
					new ErasureMethodPass(WriteConstant, new byte[] { 0 }),
					new ErasureMethodPass(WriteConstant, new byte[] { 0xFF }),
					new ErasureMethodPass(WriteRandom, null)
				};
			}
		}
	}
}
