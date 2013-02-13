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
using System.Linq;
using System.Text;
using Eraser.Util;

namespace System.IO
{
	/// <summary>
	/// A file sharing violation exception.
	/// </summary>
	public class SharingViolationException : FileLoadException
	{
		/// <summary>
		/// Constructor. This sets the <see cref="FilePath"/> property to null and
		/// initialises the exception with the default message.
		/// </summary>
		public SharingViolationException()
			: this(null)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filePath">The file which triggered this exception.</param>
		public SharingViolationException(string filePath)
			: this(null, filePath)
		{
		}

		/// <summary>
		/// Constructor. This sets the message for the exception and specifies the
		/// file whcih triggered this exception.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="filePath">The file which triggered this exception.</param>
		public SharingViolationException(string message, string filePath)
			: this(message, filePath, null)
		{
		}

		/// <summary>
		/// Constructor. This sets the message for the exception and specifies the
		/// file whcih triggered this exception as well as the exception which
		/// is the cause of this exception.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="filePath">The file which triggered this exception.</param>
		/// <param name="innerException">The exception that is the cause of the
		/// current exception. If the innerException parameter is not null, the
		/// current exception is raised in a catch block that handles the inner exception.</param>
		public SharingViolationException(string message, string filePath, Exception innerException)
			: base(message == null ?
				Win32ErrorCode.GetSystemErrorMessage(Win32ErrorCode.SharingViolation) : message,
			innerException)
		{
		}

		/// <summary>
		/// The path to the file which triggered this exception.
		/// </summary>
		public string FilePath { get; private set; }
	}
}
