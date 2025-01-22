/* 
 * $Id: BlackBoxReport.cs 2993 2021-09-25 17:23:27Z gtrant $
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
using System.Linq;
using System.Text;

using System.IO;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Eraser.BlackBox
{
	/// <summary>
	/// Represents one BlackBox crash report.
	/// </summary>
	public class BlackBoxReport
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="path">Path to the folder containing the memory dump, screenshot and
		/// debug log.</param>
		internal BlackBoxReport(string path)
		{
			Path = path;

			string stackTracePath = System.IO.Path.Combine(Path, StackTraceFileName);
			if (!File.Exists(stackTracePath))
			{
				try
				{
					Delete();
				}
				catch (UnauthorizedAccessException)
				{
					//Swallow. We may not be able to access the report; let the user
					//who owns the report upload it. But in this case, we still need
					//to still raise InvalidDataException otherwise we will operate
					//on a report we cannot modify.
				}

				throw new InvalidDataException("The BlackBox report is corrupt.");
			}

			string[] stackTrace = null;
			using (StreamReader reader = new StreamReader(stackTracePath))
				stackTrace = reader.ReadToEnd().Split(new char[] { '\n' });

			//Parse the lines in the file.
			StackTraceCache = new List<BlackBoxExceptionEntry>();
			List<string> currentException = new List<string>();
			string exceptionType = null;
			foreach (string str in stackTrace)
			{
				if (str.StartsWith("Exception "))
				{
					//Add the current exception to the list of exceptions.
					if (currentException.Count != 0)
					{
						StackTraceCache.Add(new BlackBoxExceptionEntry(exceptionType,
							new List<string>(currentException)));
						currentException.Clear();
					}

					//Set the exception type for the next exception.
					exceptionType = str.Substring(str.IndexOf(':') + 1).Trim();
				}
				else if (!string.IsNullOrEmpty(str.Trim()))
				{
					currentException.Add(str.Trim());
				}
			}

			if (currentException.Count != 0)
				StackTraceCache.Add(new BlackBoxExceptionEntry(exceptionType, currentException));
		}

		/// <summary>
		/// Deletes the report and its contents.
		/// </summary>
		public void Delete()
		{
			Directory.Delete(Path, true);
		}

		/// <summary>
		/// The name of the report.
		/// </summary>
		public string Name
		{
			get
			{
				return System.IO.Path.GetFileName(Path);
			}
		}

		/// <summary>
		/// The timestamp of the report.
		/// </summary>
		public DateTime Timestamp
		{
			get
			{
				try
				{
					return DateTime.ParseExact(Name, BlackBox.CrashReportName,
						CultureInfo.InvariantCulture).ToLocalTime();
				}
				catch (FormatException)
				{
					return DateTime.MinValue;
				}
			}
		}

		/// <summary>
		/// The path to the folder containing the report.
		/// </summary>
		public string Path
		{
			get;
			private set;
		}

		/// <summary>
		/// The files which comprise the error report.
		/// </summary>
		public ReadOnlyCollection<FileInfo> Files
		{
			get
			{
				List<FileInfo> result = new List<FileInfo>();
				DirectoryInfo directory = new DirectoryInfo(Path);
				foreach (FileInfo file in directory.GetFiles())
					if (!InternalFiles.Contains(file.Name))
						result.Add(file);

				return result.AsReadOnly();
			}
		}

		/// <summary>
		/// Gets a read-only stream which reads the Debug log.
		/// </summary>
		public Stream DebugLog
		{
			get
			{
				return new FileStream(System.IO.Path.Combine(Path, BlackBox.DebugLogFileName),
					FileMode.Open, FileAccess.Read, FileShare.Read);
			}
		}

		/// <summary>
		/// Gets the stack trace for this crash report.
		/// </summary>
		public ReadOnlyCollection<BlackBoxExceptionEntry> StackTrace
		{
			get
			{
				return StackTraceCache.AsReadOnly();
			}
		}

		/// <summary>
		/// Gets the status of the report.
		/// </summary>
		public BlackBoxReportStatus Status
		{
			get
			{
				return (BlackBoxReportStatus)StatusData[0];
			}

			internal set
			{
				byte[] status = StatusData;
				status[0] = Convert.ToByte(value);
				StatusData = status;
			}
		}

		/// <summary>
		/// Gets the ID of the current report returned by the server during upload.
		/// This will be 0 if <see cref="Submitted"/> is false.
		/// </summary>
		public int ID
		{
			get
			{
				return BitConverter.ToInt32(StatusData, 1);
			}

			internal set
			{
				byte[] bytes = BitConverter.GetBytes(value);
				byte[] status = StatusData;
				Buffer.BlockCopy(bytes, 0, status, 1, bytes.Length);
				StatusData = status;
			}
		}

		/// <summary>
		/// Gets or sets the status of the report.
		/// </summary>
		private byte[] StatusData
		{
			get
			{
				byte[] buffer = new byte[5];
				using (FileStream stream = new FileStream(System.IO.Path.Combine(Path, StatusFileName),
					FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
				{
					stream.Read(buffer, 0, buffer.Length);
				}

				return buffer;
			}

			set
			{
				using (FileStream stream = new FileStream(System.IO.Path.Combine(Path, StatusFileName),
					FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				{
					stream.Write(value, 0, Math.Min(value.Length, 5));
				}
			}
		}

		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// The backing variable for the <see cref="StackTrace"/> field.
		/// </summary>
		private List<BlackBoxExceptionEntry> StackTraceCache;

		/// <summary>
		/// The file name for the status file.
		/// </summary>
		private static readonly string StatusFileName = "Status.txt";

		/// <summary>
		/// The file name of the stack trace.
		/// </summary>
		internal static readonly string StackTraceFileName = "Stack Trace.log";

		/// <summary>
		/// The list of files internal to the report.
		/// </summary>
		private static readonly List<string> InternalFiles = new List<string>(
			new string[] {
				 StackTraceFileName,
				 StatusFileName
			}
		);
	}

	/// <summary>
	/// Represents one exception which can be chained <see cref="InnerException"/>
	/// to represent the exception handled by BlackBox
	/// </summary>
	public class BlackBoxExceptionEntry
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="exceptionType">The type of the exception.</param>
		/// <param name="stackTrace">The stack trace for this exception.</param>
		internal BlackBoxExceptionEntry(string exceptionType, List<string> stackTrace)
		{
			ExceptionType = exceptionType;
			StackTraceCache = stackTrace;
		}

		/// <summary>
		/// The type of the exception.
		/// </summary>
		public string ExceptionType
		{
			get;
			private set;
		}

		/// <summary>
		/// The stack trace for this exception.
		/// </summary>
		public ReadOnlyCollection<string> StackTrace
		{
			get
			{
				return StackTraceCache.AsReadOnly();
			}
		}

		/// <summary>
		/// The backing variable for the <see cref="StackTrace"/> property.
		/// </summary>
		private List<string> StackTraceCache;
	}

	/// <summary>
	/// Statuses of reports on the server.
	/// </summary>
	public enum BlackBoxReportStatus
	{
		/// <summary>
		/// The report has not been reported before.
		/// </summary>
		New,

		/// <summary>
		/// The report has been reported before and pending a resolution.
		/// </summary>
		Uploaded,

		/// <summary>
		/// The report has been resolved.
		/// </summary>
		Resolved
	}
}
