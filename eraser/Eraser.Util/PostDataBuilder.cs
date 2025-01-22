/* 
 * $Id: PostDataBuilder.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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
using System.IO;

namespace Eraser.Util
{
	/// <summary>
	/// Constructs a multipart/form-data encoded POST request.
	/// </summary>
	public class PostDataBuilder
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public PostDataBuilder()
		{
			FileName = Path.GetTempFileName();
		}

		public void AddPart(PostDataField field)
		{
			//Generate a random part boundary
			if (Boundary == null)
			{
				Random rand = new Random();
				for (int i = 0, j = 20 + rand.Next(40); i < j; ++i)
					Boundary += ValidBoundaryChars[rand.Next(ValidBoundaryChars.Length)];
			}

			using (FileStream stream = new FileStream(FileName, FileMode.Open, FileAccess.Write,
				FileShare.Read))
			{
				//Append data!
				stream.Seek(0, SeekOrigin.End);

				StringBuilder currentBoundary = new StringBuilder();
				currentBoundary.AppendFormat("--{0}\r\n", Boundary);
				if (field is PostDataFileField)
				{
					currentBoundary.AppendFormat(
						"Content-Disposition: file; name=\"{0}\"; filename=\"{1}\"\r\n",
						field.FieldName, ((PostDataFileField)field).FileName);
					currentBoundary.AppendLine("Content-Type: application/octet-stream");
				}
				else
				{
					currentBoundary.AppendFormat("Content-Disposition: form-data; name=\"{0}\"\r\n",
						field.FieldName);
				}

				currentBoundary.AppendLine();
				byte[] boundary = Encoding.UTF8.GetBytes(currentBoundary.ToString());
				stream.Write(boundary, 0, boundary.Length);

				int lastRead = 0;
				byte[] buffer = new byte[524288];
				while ((lastRead = field.Stream.Read(buffer, 0, buffer.Length)) != 0)
					stream.Write(buffer, 0, lastRead);

				currentBoundary = new StringBuilder();
				currentBoundary.AppendFormat("\r\n--{0}--\r\n", Boundary);
				boundary = Encoding.UTF8.GetBytes(currentBoundary.ToString());
				stream.Write(boundary, 0, boundary.Length);
			}
		}

		public void AddParts(ICollection<PostDataField> fields)
		{
			foreach (PostDataField field in fields)
				AddPart(field);
		}

		/// <summary>
		/// Gets a stream with which to read the data from.
		/// </summary>
		public Stream Stream
		{
			get
			{
				return new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
		}

		/// <summary>
		/// Gets the Content Type of this builder. This is suitable for use in a HTTP
		/// Content-Type header.
		/// </summary>
		public string ContentType
		{
			get
			{
				return "multipart/form-data; boundary=" + Boundary;
			}
		}

		/// <summary>
		/// The Multipart/Form-Data boundary in use. If this is NULL, WritePostData will generate one
		/// and store it here.
		/// </summary>
		public string Boundary
		{
			get
			{
				return boundary;
			}
			set
			{
				using (Stream stream = Stream)
					if (stream.Length != 0)
						throw new InvalidOperationException("The boundary cannot be set as data " +
							"already exists in the buffer.");
				boundary = value;
			}
		}

		/// <summary>
		/// Stores the temporary file we use as a buffer to store the parts before the
		/// stream is requested to store the parts.
		/// </summary>
		private string FileName;

		/// <summary>
		/// The backing variable for the <see cref="Boundary"/> property.
		/// </summary>
		private string boundary;

		/// <summary>
		/// Characters valid for use in the multipart boundary.
		/// </summary>
		private static readonly string ValidBoundaryChars =
			"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
	}

	public class PostDataField
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="stream">The stream containing the field data.</param>
		public PostDataField(string fieldName, Stream stream)
		{
			FieldName = fieldName;
			Stream = stream;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="stream">The content of the field.</param>
		public PostDataField(string fieldName, string content)
			: this(fieldName, new MemoryStream(Encoding.UTF8.GetBytes(content)))
		{
		}

		/// <summary>
		/// The name of the field.
		/// </summary>
		public string FieldName;

		/// <summary>
		/// The stream containing the data for this field.
		/// </summary>
		public Stream Stream;
	}

	public class PostDataFileField : PostDataField
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fieldName">The name of the form field.</param>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="stream">The stream containing the field data.</param>
		public PostDataFileField(string fieldName, string fileName, Stream stream)
			: base(fieldName, stream)
		{
			FileName = fileName;
		}

		/// <summary>
		/// The name of the file.
		/// </summary>
		public string FileName;
	}
}
