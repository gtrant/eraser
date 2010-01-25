/* 
 * $Id$
 * Copyright 2008-2010 The Eraser Project
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
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Eraser.Util
{
	/// <summary>
	/// Handles application exceptions, stores minidumps and uploads them to the
	/// Eraser server.
	/// </summary>
	public class BlackBox
	{
		/// <summary>
		/// Stores DLL references for this class.
		/// </summary>
		private static class NativeMethods
		{
			/// <summary>
			/// Writes user-mode minidump information to the specified file.
			/// </summary>
			/// <param name="hProcess">A handle to the process for which the information
			/// is to be generated.</param>
			/// <param name="ProcessId">The identifier of the process for which the information
			/// is to be generated.</param>
			/// <param name="hFile">A handle to the file in which the information is to be
			/// written.</param>
			/// <param name="DumpType">The type of information to be generated. This parameter
			/// can be one or more of the values from the MINIDUMP_TYPE enumeration.</param>
			/// <param name="ExceptionParam">A pointer to a MiniDumpExceptionInfo structure
			/// describing the client exception that caused the minidump to be generated.
			/// If the value of this parameter is NULL, no exception information is included
			/// in the minidump file.</param>
			/// <param name="UserStreamParam">Not supported. Use IntPtr.Zero</param>
			/// <param name="CallbackParam">Not supported. Use IntPtr.Zero</param>
			/// <returns>If the function succeeds, the return value is true; otherwise, the
			/// return value is false. To retrieve extended error information, call GetLastError.
			/// Note that the last error will be an HRESULT value.</returns>
			[DllImport("dbghelp.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId,
				SafeFileHandle hFile, MiniDumpType DumpType,
				ref MiniDumpExceptionInfo ExceptionParam, IntPtr UserStreamParam,
				IntPtr CallbackParam);

			/// <summary>
			/// Identifies the type of information that will be written to the minidump file
			/// by the MiniDumpWriteDump function.
			/// </summary>
			public enum MiniDumpType
			{
				/// <summary>
				/// Include just the information necessary to capture stack traces for all
				/// existing threads in a process.
				/// </summary>
				MiniDumpNormal = 0x00000000,

				/// <summary>
				/// Include the data sections from all loaded modules. This results in the
				/// inclusion of global variables, which can make the minidump file significantly
				/// larger. For per-module control, use the ModuleWriteDataSeg enumeration
				/// value from MODULE_WRITE_FLAGS.
				/// </summary>
				MiniDumpWithDataSegs = 0x00000001,

				/// <summary>
				/// Include all accessible memory in the process. The raw memory data is
				/// included at the end, so that the initial structures can be mapped directly
				/// without the raw memory information. This option can result in a very large
				/// file.
				/// </summary>
				MiniDumpWithFullMemory = 0x00000002,

				/// <summary>
				/// Include high-level information about the operating system handles that are
				/// active when the minidump is made.
				/// </summary>
				MiniDumpWithHandleData = 0x00000004,

				/// <summary>
				/// Stack and backing store memory written to the minidump file should be
				/// filtered to remove all but the pointer values necessary to reconstruct a
				/// stack trace. Typically, this removes any private information.
				/// </summary>
				MiniDumpFilterMemory = 0x00000008,

				/// <summary>
				/// Stack and backing store memory should be scanned for pointer references
				/// to modules in the module list. If a module is referenced by stack or backing
				/// store memory, the ModuleWriteFlags member of the MINIDUMP_CALLBACK_OUTPUT
				/// structure is set to ModuleReferencedByMemory.
				/// </summary>
				MiniDumpScanMemory = 0x00000010,

				/// <summary>
				/// Include information from the list of modules that were recently unloaded,
				/// if this information is maintained by the operating system.
				/// </summary>
				MiniDumpWithUnloadedModules = 0x00000020,

				/// <summary>
				/// Include pages with data referenced by locals or other stack memory.
				/// This option can increase the size of the minidump file significantly.
				/// </summary>
				MiniDumpWithIndirectlyReferencedMemory = 0x00000040,

				/// <summary>
				/// Filter module paths for information such as user names or important
				/// directories. This option may prevent the system from locating the image
				/// file and should be used only in special situations.
				/// </summary>
				MiniDumpFilterModulePaths = 0x00000080,

				/// <summary>
				/// Include complete per-process and per-thread information from the operating
				/// system.
				/// </summary>
				MiniDumpWithProcessThreadData = 0x00000100,

				/// <summary>
				/// Scan the virtual address space for PAGE_READWRITE memory to be included.
				/// </summary>
				MiniDumpWithPrivateReadWriteMemory = 0x00000200,

				/// <summary>
				/// Reduce the data that is dumped by eliminating memory regions that are not
				/// essential to meet criteria specified for the dump. This can avoid dumping
				/// memory that may contain data that is private to the user. However, it is
				/// not a guarantee that no private information will be present.
				/// </summary>
				MiniDumpWithoutOptionalData = 0x00000400,

				/// <summary>
				/// Include memory region information. For more information, see
				/// MINIDUMP_MEMORY_INFO_LIST.
				/// </summary>
				MiniDumpWithFullMemoryInfo = 0x00000800,

				/// <summary>
				/// Include thread state information. For more information, see
				/// MINIDUMP_THREAD_INFO_LIST.
				/// </summary>
				MiniDumpWithThreadInfo = 0x00001000,

				/// <summary>
				/// Include all code and code-related sections from loaded modules to capture
				/// executable content. For per-module control, use the ModuleWriteCodeSegs
				/// enumeration value from MODULE_WRITE_FLAGS. 
				/// </summary>
				MiniDumpWithCodeSegs = 0x00002000,

				/// <summary>
				/// Turns off secondary auxiliary-supported memory gathering.
				/// </summary>
				MiniDumpWithoutAuxiliaryState = 0x00004000,

				/// <summary>
				/// Requests that auxiliary data providers include their state in the dump
				/// image; the state data that is included is provider dependent. This option
				/// can result in a large dump image.
				/// </summary>
				MiniDumpWithFullAuxiliaryState = 0x00008000,

				/// <summary>
				/// Scans the virtual address space for PAGE_WRITECOPY memory to be included.
				/// </summary>
				MiniDumpWithPrivateWriteCopyMemory = 0x00010000,

				/// <summary>
				/// If you specify MiniDumpWithFullMemory, the MiniDumpWriteDump function will
				/// fail if the function cannot read the memory regions; however, if you include
				/// MiniDumpIgnoreInaccessibleMemory, the MiniDumpWriteDump function will
				/// ignore the memory read failures and continue to generate the dump. Note that
				/// the inaccessible memory regions are not included in the dump.
				/// </summary>
				MiniDumpIgnoreInaccessibleMemory = 0x00020000,

				/// <summary>
				/// Adds security token related data. This will make the "!token" extension work
				/// when processing a user-mode dump. 
				/// </summary>
				MiniDumpWithTokenInformation = 0x00040000
			}

			/// <summary>
			/// Contains the exception information written to the minidump file by the
			/// MiniDumpWriteDump function.
			/// </summary>
			[StructLayout(LayoutKind.Sequential, Pack = 4)]
			public struct MiniDumpExceptionInfo
			{
				/// <summary>
				/// The identifier of the thread throwing the exception.
				/// </summary>
				public uint ThreadId;

				/// <summary>
				///  A pointer to an EXCEPTION_POINTERS structure specifying a
				///  computer-independent description of the exception and the processor
				///  context at the time of the exception.
				/// </summary>
				public IntPtr ExceptionPointers;

				/// <summary>
				/// Determines where to get the memory regions pointed to by the
				/// ExceptionPointers member. Set to TRUE if the memory resides in the
				/// process being debugged (the target process of the debugger). Otherwise,
				/// set to FALSE if the memory resides in the address space of the calling
				/// program (the debugger process). If you are accessing local memory (in
				/// the calling process) you should not set this member to TRUE.
				/// </summary>
				[MarshalAs(UnmanagedType.Bool)]
				public bool ClientPointers;
			}
		}

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
			Directory.CreateDirectory(currentCrashReport);

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
					Directory.Delete(currentCrashReport);
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
				NativeMethods.MiniDumpExceptionInfo exception =
					new NativeMethods.MiniDumpExceptionInfo();
				exception.ClientPointers = false;
				exception.ExceptionPointers = Marshal.GetExceptionPointers();
				exception.ThreadId = (uint)AppDomain.GetCurrentThreadId();

				NativeMethods.MiniDumpWriteDump(Process.GetCurrentProcess().Handle,
					(uint)Process.GetCurrentProcess().Id, stream.SafeFileHandle,
					NativeMethods.MiniDumpType.MiniDumpWithFullMemory,
					ref exception, IntPtr.Zero, IntPtr.Zero);
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
				string separator = new string('-', 76);
				string lineFormat = "{0,15}: {1}";
				stream.WriteLine("Application Information");
				stream.WriteLine(separator);
				stream.WriteLine(string.Format(lineFormat, "Version",
					Assembly.GetEntryAssembly().GetName().Version));
				StringBuilder commandLine = new StringBuilder();
				foreach (string param in Environment.GetCommandLineArgs())
				{
					commandLine.Append(param);
					commandLine.Append(' ');
				}
				stream.WriteLine(string.Format(lineFormat, "Command Line",
					commandLine.ToString().Trim()));

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
						stream.WriteLine(string.Format("Exception {0}:", i));
						stream.WriteLine(string.Format(lineFormat, "Message", currentException.Message));
						stream.WriteLine(string.Format(lineFormat, "Exception Type",
							currentException.GetType().FullName));
						stackTraceLog.WriteLine(string.Format("Exception {0}: {1}", i,
							currentException.GetType().FullName));

						//Parse the stack trace
						string[] stackTrace = currentException.StackTrace.Split(new char[] { '\n' });
						for (uint j = 0; j < stackTrace.Length; ++j)
						{
							stream.WriteLine(string.Format(lineFormat,
								string.Format("Stack Trace [{0}]", j), stackTrace[j].Trim()));
							stackTraceLog.WriteLine(string.Format("{0}", stackTrace[j].Trim()));
						}

						uint k = 0;
						foreach (System.Collections.DictionaryEntry value in currentException.Data)
							stream.WriteLine(string.Format(lineFormat, string.Format("Data[{0}]", ++k),
								string.Format("{0} {1}", value.Key.ToString(), value.Value.ToString())));

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
			bitmap.CopyFromScreen(0, 0, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);

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
			if (!System.IO.File.Exists(stackTracePath))
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
				return DateTime.ParseExact(Name, BlackBox.CrashReportName,
					CultureInfo.InvariantCulture).ToLocalTime();
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
					FileMode.Open, FileAccess.Read);
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
}