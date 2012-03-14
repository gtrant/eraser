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
using System.Runtime.Serialization;
using Eraser.Util;

namespace Eraser.Plugins
{
	/// <summary>
	/// Fatal exception class.
	/// </summary>
	[Serializable]
	public class FatalException : Exception
	{
		public FatalException()
		{
		}

		public FatalException(string message)
			: base(message)
		{
		}

		protected FatalException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public FatalException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	[Serializable]
	public class EntropySourceNotFoundException : FatalException
	{
		public EntropySourceNotFoundException()
		{
		}

		public EntropySourceNotFoundException(string message)
			: base(message)
		{
		}

		public EntropySourceNotFoundException(Guid value)
			: this(S._("EntropySource GUID not found: {0}", value.ToString()))
		{
		}

		protected EntropySourceNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public EntropySourceNotFoundException(Guid value, Exception innerException)
			: this(S._("EntropySource GUID not found: {0}", value.ToString()),
				innerException)
		{
		}

		public EntropySourceNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	[Serializable]
	public class ErasureMethodNotFoundException : FatalException
	{
		public ErasureMethodNotFoundException()
		{
		}

		public ErasureMethodNotFoundException(string message)
			: base(message)
		{
		}
		
		public ErasureMethodNotFoundException(Guid value)
			: this(S._("Erasure method not found: {0}", value.ToString()))
		{
		}

		protected ErasureMethodNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public ErasureMethodNotFoundException(Guid value, Exception innerException)
			: this(S._("Erasure method not found: {0}", value.ToString()),
				innerException)
		{
		}

		public ErasureMethodNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	[Serializable]
	public class PrngNotFoundException : FatalException
	{
		public PrngNotFoundException()
		{
		}

		public PrngNotFoundException(string message)
			: base(message)
		{
		}
		
		public PrngNotFoundException(Guid value)
			: this(S._("PRNG not found: {0}", value.ToString()))
		{
		}

		protected PrngNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public PrngNotFoundException(Guid value, Exception innerException)
			: this(S._("PRNG not found: {0}", value.ToString()),
				innerException)
		{
		}

		public PrngNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
