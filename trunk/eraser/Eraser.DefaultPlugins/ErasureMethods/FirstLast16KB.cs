/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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

using System.IO;
using System.Threading;
using System.Windows.Forms;

using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	[Guid("0C2E07BF-0207-49a3-ADE8-46F9E1499C01")]
	sealed class FirstLast16KB : ErasureMethodBase
	{
		public FirstLast16KB()
		{
			try
			{
				//Try to retrieve the set erasure method
				if (DefaultPlugin.Settings.FL16Method != Guid.Empty)
					method = Host.Instance.ErasureMethods[DefaultPlugin.Settings.FL16Method];
				else if (Host.Instance.Settings.DefaultFileErasureMethod != Guid)
					method = Host.Instance.ErasureMethods[
						Host.Instance.Settings.DefaultFileErasureMethod];
				else
					method = Host.Instance.ErasureMethods[new Gutmann().Guid];
			}
			catch (ErasureMethodNotFoundException)
			{
				MessageBox.Show(S._("The First/last 16KB erasure method " +
					"requires another erasure method to erase the file.\n\nThis must " +
					"be set in the Plugin Settings dialog."), Name, MessageBoxButtons.OK,
					MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
					Localisation.IsRightToLeft(null) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}
		}

		public override string Name
		{
			get { return S._("First/last 16KB Erasure"); }
		}

		public override int Passes
		{
			get { return 0; } //Variable number, depending on defaults.
		}

		public override Guid Guid
		{
			get { return GetType().GUID; }
		}

		public override long CalculateEraseDataSize(ICollection<StreamInfo> paths, long targetSize)
		{
			//If we have no default or we are the default then throw an exception
			if (method == null || method.Guid == Guid)
				throw new InvalidOperationException(S._("The First/last 16KB erasure method " +
					"requires another erasure method to erase the file.\n\nThis must " +
					"be set in the Plugin Settings dialog."));

			//Amount of data required to be written.
			long amountToWrite = 0;
			if (paths == null)
			{
				if (targetSize <= DataSize)
					amountToWrite = targetSize;
				else
					amountToWrite = DataSize * 2;
			}
			else
				amountToWrite = paths.Count * DataSize * 2;

			//The final amount has to be multiplied by the number of passes.
			return amountToWrite * method.Passes;
		}

		public override void Erase(Stream strm, long erasureLength, IPrng prng,
			ErasureMethodProgressFunction callback)
		{
			//If we have no default or we are the default then throw an exception
			if (method == null || method.Guid == Guid)
				throw new InvalidOperationException(S._("The First/last 16KB erasure method " +
					"requires another erasure method to erase the file.\n\nThis must " +
					"be set in the Plugin Settings dialog."));

			//Make sure that the erasureLength passed in here is the maximum value
			//for the size of long, since we don't want to write extra or write
			//less.
			if (erasureLength != long.MaxValue)
				throw new ArgumentException("The amount of data erased should not be " +
					"limited, since this is a self-limiting erasure method.");

			//If the target stream is shorter than or equal to 32kb, just forward it to
			//the default function.
			if (strm.Length < DataSize * 2)
			{
				method.Erase(strm, erasureLength, prng, callback);
				return;
			}

			//We need to intercept the callback function as we run the erasure method
			//twice on two parts of the file.
			long dataSize = method.CalculateEraseDataSize(null, DataSize * 2);
			ErasureMethodProgressFunction customCallback =
				delegate(long lastWritten, long totalData, int currentPass)
				{
					callback(lastWritten, dataSize, currentPass);
				};

			//Seek to the beginning and write 16kb.
			strm.Seek(0, SeekOrigin.Begin);
			method.Erase(strm, dataSize, prng, callback == null ? null: customCallback);

			//Seek to the end - 16kb, and write.
			strm.Seek(-dataSize, SeekOrigin.End);
			method.Erase(strm, long.MaxValue, prng, callback == null ? null : customCallback);
		}

		/// <summary>
		/// The amount of data to be erased from the header and the end of the file.
		/// </summary>
		private const long DataSize = 16 * 1024;

		private readonly IErasureMethod method;
	}
}