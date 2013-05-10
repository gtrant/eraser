/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Garrett Trant <gtrant@users.sourceforge.net>
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
using System.Security.Cryptography;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	[Guid("6BF35B8E-F37F-476e-B6B2-9994A92C3B0C")]
	class RngCrypto : PrngBase
	{
		public override string Name
		{
			get { return S._("RNGCryptoServiceProvider"); }
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override void NextBytes(byte[] buffer)
		{
			rand.GetBytes(buffer);
		}

		public override void Reseed(byte[] seed)
		{
			//No-op. RNGCryptoServiceProviders can't be reseeded.
		}

		readonly RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
	}
}
