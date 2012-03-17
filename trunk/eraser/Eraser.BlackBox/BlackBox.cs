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

using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;
using System.Globalization;

using System.Net;
using System.Xml;
using System.ComponentModel;

using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Win32.SafeHandles;
using Microsoft.VisualBasic.Devices;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using ProgressChangedEventHandler = Eraser.Plugins.ProgressChangedEventHandler;
using ProgressChangedEventArgs = Eraser.Plugins.ProgressChangedEventArgs;

namespace Eraser.BlackBox
{
	/// <summary>
	/// Handles application exceptions, stores minidumps and uploads them to the
	/// Eraser server.
	/// </summary>
	public class BlackBox
	{
		/// <summary>
		/// Initialises the BlackBox handler. Call this initialiser once throughout
		/// the lifespan of the application.
		/// </summary>
		/// <returns>The global BlackBox instance.</returns>
		public static BlackBox Get()
		{
			if (Instance == null)
				Instance = new BlackBox();
			return Instance;
		}

		/// <summary>
		/// Creates a new BlackBox report based on the exception provided.
		/// </summary>
		/// <param name="e">The exception which triggered this dump.</param>
		public void CreateReport(Exception e)
		{
			if (e == null)
				throw new ArgumentNullException("e");

			//Generate a unique identifier for this report.
			string crashName = DateTime.Now.ToUniversalTime().ToString(
				CrashReportName, CultureInfo.InvariantCulture);
			string currentCrashReport = Path.Combine(CrashReportsPath, crashName);

			//Create the report folder. If we can't create the report folder, we can't
			//create the report contents.
			Directory.CreateDirectory(currentCrashReport);
			if (!Directory.Exists(currentCrashReport))
				return;

			//Store the steps which we have completed.
			int currentStep = 0;

			try
			{
				//First, write a user-readable summary
				WriteDebugLog(currentCrashReport, e);
				++currentStep;

				//Take a screenshot
				WriteScreenshot(currentCrashReport);
				++currentStep;

				//Write a memory dump to the folder
				WriteMemoryDump(currentCrashReport, e);
				++currentStep;
			}
			catch
			{
				//If an exception was caught while creating the report, we should just
				//abort as that may cause a cascade. However, we need to remove the
				//report folder if the crash report is empty.
				if (currentStep == 0)
					Directory.Delete(currentCrashReport, true);
			}
		}

		/// <summary>
		/// Enumerates the list of crash dumps waiting for upload.
		/// </summary>
		/// <returns>A string array containing the list of dumps waiting for upload.</returns>
		public BlackBoxReport[] GetDumps()
		{
			DirectoryInfo dirInfo = new DirectoryInfo(CrashReportsPath);
			List<BlackBoxReport> result = new List<BlackBoxReport>();
			if (dirInfo.Exists)
				foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
					try
					{
						result.Add(new BlackBoxReport(Path.Combine(CrashReportsPath, subDir.Name)));
					}
					catch (InvalidDataException)
					{
						//Do nothing: invalid reports are automatically deleted.
					}

			return result.ToArray();
		}

		/// <summary>
		/// Constructor. Use the <see cref="Initialise"/> function to use this class.
		/// </summary>
		private BlackBox()
		{
			//If we have a debugger attached we shouldn't bother with exceptions.
			if (Debugger.IsAttached)
				return;

			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
		}

		/// <summary>
		/// Called when an unhandled exception is raised in the application.
		/// </summary>
		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			CreateReport(e.ExceptionObject as Exception);
		}

		/// <summary>
		/// Dumps the contents of memory to a dumpfile.
		/// </summary>
		/// <param name="dumpFolder">Path to the folder to store the dump file.</param>
		/// <param name="e">The exception which is being handled.</param>
		private void WriteMemoryDump(string dumpFolder, Exception e)
		{
			//Open a file stream
			using (FileStream stream = new FileStream(Path.Combine(dumpFolder, MemoryDumpFileName),
				FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			{
				//Store the exception information
				MiniDump.Dump(stream);
			}
		}

		/// <summary>
		/// Writes a debug log to the given directory.
		/// </summary>
		/// <param name="screenshotPath">The path to store the screenshot into.</param>
		/// <param name="exception">The exception to log about.</param>
		private void WriteDebugLog(string dumpFolder, Exception exception)
		{
			using (FileStream file = new FileStream(Path.Combine(dumpFolder, DebugLogFileName),
				FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			using (StreamWriter stream = new StreamWriter(file))
			{
				//Application information
				string separator = new string('-', 100);
				string lineFormat = "{0,20}: {1}";
				stream.WriteLine("Application Information");
				stream.WriteLine(separator);
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Version", Assembly.GetEntryAssembly().GetFileVersion()));
				StringBuilder commandLine = new StringBuilder();
				foreach (string param in Environment.GetCommandLineArgs())
				{
					commandLine.Append(param);
					commandLine.Append(' ');
				}
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Command Line", commandLine.ToString().Trim()));
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Current Directory", Environment.CurrentDirectory));

				//System Information
				ComputerInfo info = new ComputerInfo();
				stream.WriteLine();
				stream.WriteLine("System Information");
				stream.WriteLine(separator);
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Operating System",
					string.Format(CultureInfo.InvariantCulture, "{0} {1}{2} {4}",
						info.OSFullName.Trim(), info.OSVersion.Trim(),
						string.IsNullOrEmpty(Environment.OSVersion.ServicePack) ?
							string.Empty :
							string.Format(CultureInfo.InvariantCulture, "({0})", Environment.OSVersion.ServicePack),
						SystemInfo.WindowsEdition == WindowsEditions.Undefined ?
							string.Empty : SystemInfo.WindowsEdition.ToString(),
						SystemInfo.ProcessorArchitecture)));
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					".NET Runtime version", Environment.Version));
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Processor Count", Environment.ProcessorCount));
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Physical Memory", string.Format(CultureInfo.InvariantCulture,
						"{0}/{1}", info.AvailablePhysicalMemory, info.TotalPhysicalMemory)));
				stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
					"Virtual Memory", string.Format(CultureInfo.InvariantCulture,
						"{0}/{1}", info.AvailableVirtualMemory, info.TotalVirtualMemory)));

				//Running processes
				stream.WriteLine();
				stream.WriteLine("Running Processes");
				stream.WriteLine(separator);
				{
					int i = 0;
					foreach (Process process in Process.GetProcesses())
					{
						try
						{
							ProcessModule mainModule = process.MainModule;
							stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
								string.Format(CultureInfo.InvariantCulture, "Process[{0}]", ++i),
								string.Format(CultureInfo.InvariantCulture, "{0} [{1}.{2}.{3}.{4}{5}]",
									mainModule.FileName,
									mainModule.FileVersionInfo.FileMajorPart,
									mainModule.FileVersionInfo.FileMinorPart,
									mainModule.FileVersionInfo.FileBuildPart,
									mainModule.FileVersionInfo.FilePrivatePart,
									string.IsNullOrEmpty(mainModule.FileVersionInfo.FileVersion) ?
										string.Empty :
										string.Format(CultureInfo.InvariantCulture, " <{0}>",
											mainModule.FileVersionInfo.FileVersion))));
						}
						catch (Win32Exception)
						{
						}
						catch (InvalidOperationException)
						{
						}
					}
				}

				//Exception Information
				stream.WriteLine();
				stream.WriteLine("Exception Information (Outermost to innermost)");
				stream.WriteLine(separator);

				//Open a stream to the Stack Trace Log file. We want to separate the stack
				//trace do we can check against the server to see if the crash is a new one
				using (StreamWriter stackTraceLog = new StreamWriter(
					Path.Combine(dumpFolder, BlackBoxReport.StackTraceFileName)))
				{
					Exception currentException = exception;
					for (uint i = 1; currentException != null; ++i)
					{
						stream.WriteLine(string.Format(CultureInfo.InvariantCulture,
							"Exception {0}:", i));
						stream.WriteLine(string.Format(CultureInfo.InvariantCulture,
							lineFormat, "Message", currentException.Message));
						stream.WriteLine(string.Format(CultureInfo.InvariantCulture,
							lineFormat, "Exception Type", currentException.GetType().FullName));
						stackTraceLog.WriteLine(string.Format(CultureInfo.InvariantCulture,
							"Exception {0}: {1}", i, currentException.GetType().FullName));

						//Parse the stack trace
						string[] stackTrace = currentException.StackTrace.Split(new char[] { '\n' });
						for (uint j = 0; j < stackTrace.Length; ++j)
						{
							stream.WriteLine(string.Format(CultureInfo.InvariantCulture, lineFormat,
								string.Format(CultureInfo.InvariantCulture,
									"Stack Trace [{0}]", j), stackTrace[j].Trim()));
							stackTraceLog.WriteLine(string.Format(CultureInfo.InvariantCulture,
								"{0}", stackTrace[j].Trim()));
						}

						uint k = 0;
						foreach (System.Collections.DictionaryEntry value in currentException.Data)
							stream.WriteLine(
								string.Format(CultureInfo.InvariantCulture, lineFormat,
									string.Format(CultureInfo.InvariantCulture, "Data[{0}]", ++k),
									string.Format(CultureInfo.InvariantCulture, "{0} {1}",
										value.Key.ToString(), value.Value.ToString())));

						//End the exception and get the inner exception.
						stream.WriteLine();
						currentException = currentException.InnerException;
					}
				}
			}
		}

		/// <summary>
		/// Writes a screenshot to the given directory
		/// </summary>
		/// <param name="dumpFolder">The path to save the screenshot to.</param>
		private void WriteScreenshot(string dumpFolder)
		{
			//Get the size of the screen
			Rectangle rect = new Rectangle(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
			foreach (Screen screen in Screen.AllScreens)
				rect = Rectangle.Union(rect, screen.Bounds);

			//Copy a screen DC to the screenshot bitmap
			Bitmap screenShot = new Bitmap(rect.Width, rect.Height);
			Graphics bitmap = Graphics.FromImage(screenShot);
			bitmap.CopyFromScreen(rect.Location, Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);

			//Place the mouse pointer
			Cursor.Current.Draw(bitmap, new Rectangle(Cursor.Position, Cursor.Current.Size));

			//Save the bitmap to disk
			screenShot.Save(Path.Combine(dumpFolder, ScreenshotFileName), ImageFormat.Png);
		}

		/// <summary>
		/// The global BlackBox instance.
		/// </summary>
		private static BlackBox Instance;

		/// <summary>
		/// The path to all Eraser crash reports.
		/// </summary>
		private static readonly string CrashReportsPath = Path.Combine(Environment.GetFolderPath(
			Environment.SpecialFolder.LocalApplicationData), @"Eraser 6\Crash Reports");

		/// <summary>
		/// The report name format.
		/// </summary>
		internal static readonly string CrashReportName = "yyyyMMdd HHmmss.FFF";

		/// <summary>
		/// The file name of the memory dump.
		/// </summary>
		/// 
		internal static readonly string MemoryDumpFileName = "Memory.dmp";

		/// <summary>
		/// The file name of the debug log.
		/// </summary>
		internal static readonly string DebugLogFileName = "Debug.log";

		/// <summary>
		/// The file name of the screenshot.
		/// </summary>
		internal static readonly string ScreenshotFileName = "Screenshot.png";
	}

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
				Delete();
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
		/// Gets or sets whether the given report has been uploaded to the server.
		/// </summary>
		public bool Submitted
		{
			get
			{
				byte[] buffer = new byte[1];
				using (FileStream stream = new FileStream(System.IO.Path.Combine(Path, StatusFileName),
					FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
				{
					stream.Read(buffer, 0, buffer.Length);
				}

				return buffer[0] == 1;
			}

			set
			{
				byte[] buffer = { Convert.ToByte(value) };
				using (FileStream stream = new FileStream(System.IO.Path.Combine(Path, StatusFileName),
					FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				{
					stream.Write(buffer, 0, buffer.Length);
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
	/// Uploads <see cref="BlackBoxReport"/>s to the Eraser server.
	/// </summary>
	public class BlackBoxReportUploader
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="report">The report to upload.</param>
		public BlackBoxReportUploader(BlackBoxReport report)
		{
			Report = report;
			if (!Directory.Exists(UploadTempDir))
				Directory.CreateDirectory(UploadTempDir);

			ReportBaseName = Path.Combine(UploadTempDir, Report.Name);
		}

		/// <summary>
		/// Gets from the server based on the stack trace whether this report is
		/// new.
		/// </summary>
		public bool IsNew
		{
			get
			{
				PostDataBuilder builder = new PostDataBuilder();
				builder.AddPart(new PostDataField("action", "status"));
				AddStackTraceToRequest(Report.StackTrace, builder);

				WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
				reportRequest.ContentType = builder.ContentType;
				reportRequest.Method = "POST";
				using (Stream formStream = builder.Stream)
				{
					reportRequest.ContentLength = formStream.Length;
					using (Stream requestStream = reportRequest.GetRequestStream())
					{
						int lastRead = 0;
						byte[] buffer = new byte[32768];
						while ((lastRead = formStream.Read(buffer, 0, buffer.Length)) != 0)
							requestStream.Write(buffer, 0, lastRead);
					}
				}

				try
				{
					HttpWebResponse response = reportRequest.GetResponse() as HttpWebResponse;
					using (Stream responseStream = response.GetResponseStream())
					{
						XmlReader reader = XmlReader.Create(responseStream);
						reader.ReadToFollowing("crashReport");
						string reportStatus = reader.GetAttribute("status");
						switch (reportStatus)
						{
							case "exists":
								Report.Submitted = true;
								return false;

							case "new":
								return true;

							default:
								throw new InvalidDataException(
									"Unknown crash report server response.");
						}
					}
				}
				catch (WebException e)
				{
					if (e.Response == null)
						throw;

					using (Stream responseStream = e.Response.GetResponseStream())
					{
						try
						{
							XmlReader reader = XmlReader.Create(responseStream);
							reader.ReadToFollowing("error");
							throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture,
								"The server encountered a problem while processing the request: {0}",
								reader.ReadString()));
						}
						catch (XmlException)
						{
						}
					}

					throw new InvalidDataException(((HttpWebResponse)e.Response).StatusDescription);
				}
			}
		}

		/// <summary>
		/// Compresses the report for uploading.
		/// </summary>
		/// <param name="progress">The <see cref="ProgressManager"/> instance that the
		/// Upload function is using.</param>
		/// <param name="progressChanged">The progress changed event handler that should
		/// be called for upload progress updates.</param>
		private void Compress(SteppedProgressManager progress,
			ProgressChangedEventHandler progressChanged)
		{
			using (FileStream archiveStream = new FileStream(ReportBaseName + ".tar",
					FileMode.Create, FileAccess.Write))
			{
				//Add the report into a tar file
				TarArchive archive = TarArchive.CreateOutputTarArchive(archiveStream);
				foreach (FileInfo file in Report.Files)
				{
					TarEntry entry = TarEntry.CreateEntryFromFile(file.FullName);
					entry.Name = Path.GetFileName(entry.Name);
					archive.WriteEntry(entry, false);
				}
				archive.Close();
			}

			ProgressManager step = new ProgressManager();
			progress.Steps.Add(new SteppedProgressManagerStep(step, 0.5f, "Compressing"));
			using (FileStream bzipFile = new FileStream(ReportBaseName + ".tbz",
				FileMode.Create))
			using (FileStream tarStream = new FileStream(ReportBaseName + ".tar",
				FileMode.Open, FileAccess.Read, FileShare.Read, 262144, FileOptions.DeleteOnClose))
			using (BZip2OutputStream bzipStream = new BZip2OutputStream(bzipFile, 262144))
			{
				//Compress the tar file
				int lastRead = 0;
				byte[] buffer = new byte[524288];
				while ((lastRead = tarStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					bzipStream.Write(buffer, 0, lastRead);
					step.Total = tarStream.Length;
					step.Completed = tarStream.Position;

					if (progressChanged != null)
						progressChanged(this, new ProgressChangedEventArgs(progress, null));
				}
			}
		}

		/// <summary>
		/// Compresses the report, then uploads it to the server.
		/// </summary>
		/// <param name="progressChanged">The progress changed event handler that should
		/// be called for upload progress updates.</param>
		public void Submit(ProgressChangedEventHandler progressChanged)
		{
			SteppedProgressManager overallProgress = new SteppedProgressManager();
			Compress(overallProgress, progressChanged);

			using (FileStream bzipFile = new FileStream(ReportBaseName + ".tbz",
				FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.DeleteOnClose))
			using (Stream logFile = Report.DebugLog)
			{
				//Build the POST request
				PostDataBuilder builder = new PostDataBuilder();
				builder.AddPart(new PostDataField("action", "upload"));
				builder.AddPart(new PostDataFileField("crashReport", "Report.tbz", bzipFile));
				AddStackTraceToRequest(Report.StackTrace, builder);

				//Upload the POST request
				WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
				reportRequest.ContentType = builder.ContentType;
				reportRequest.Method = "POST";
				reportRequest.Timeout = int.MaxValue;
				using (Stream formStream = builder.Stream)
				{
					ProgressManager progress = new ProgressManager();
					overallProgress.Steps.Add(new SteppedProgressManagerStep(
						progress, 0.5f, "Uploading"));
					reportRequest.ContentLength = formStream.Length;

					using (Stream requestStream = reportRequest.GetRequestStream())
					{
						int lastRead = 0;
						byte[] buffer = new byte[32768];
						while ((lastRead = formStream.Read(buffer, 0, buffer.Length)) != 0)
						{
							requestStream.Write(buffer, 0, lastRead);

							progress.Total = formStream.Length;
							progress.Completed = formStream.Position;
							progressChanged(this, new ProgressChangedEventArgs(overallProgress, null));
						}
					}
				}

				try
				{
					reportRequest.GetResponse();
					Report.Submitted = true;
				}
				catch (WebException e)
				{
					if (e.Response == null)
						throw;

					using (Stream responseStream = e.Response.GetResponseStream())
					{
						try
						{
							XmlReader reader = XmlReader.Create(responseStream);
							reader.ReadToFollowing("error");
							throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture,
								"The server encountered a problem while processing the request: {0}",
								reader.ReadString()));
						}
						catch (XmlException)
						{
						}
					}

					throw new InvalidDataException(((HttpWebResponse)e.Response).StatusDescription);
				}
			}
		}

		/// <summary>
		/// Adds the stack trace to the given form request.
		/// </summary>
		/// <param name="stackTrace">The stack trace to add.</param>
		/// <param name="builder">The Form request builder to add the stack trace to.</param>
		private static void AddStackTraceToRequest(IList<BlackBoxExceptionEntry> stackTrace,
			PostDataBuilder builder)
		{
			int exceptionIndex = 0;
			foreach (BlackBoxExceptionEntry exceptionStack in stackTrace)
			{
				foreach (string stackFrame in exceptionStack.StackTrace)
					builder.AddPart(new PostDataField(
						string.Format(CultureInfo.InvariantCulture, "stackTrace[{0}][]", exceptionIndex), stackFrame));
				builder.AddPart(new PostDataField(string.Format(CultureInfo.InvariantCulture, 
					"stackTrace[{0}][exception]", exceptionIndex), exceptionStack.ExceptionType));
				++exceptionIndex;
			}
		}

		/// <summary>
		/// The path to where the temporary files are stored before uploading.
		/// </summary>
		private static readonly string UploadTempDir =
			Path.Combine(Path.GetTempPath(), "Eraser Crash Reports");

		/// <summary>
		/// The URI to the BlackBox server.
		/// </summary>
		private static readonly Uri BlackBoxServer =
			new Uri("http://eraser.heidi.ie/scripts/blackbox/upload.php");

		/// <summary>
		/// The report being uploaded.
		/// </summary>
		private BlackBoxReport Report;

		/// <summary>
		/// The base name of the report.
		/// </summary>
		private readonly string ReportBaseName;
	}
}