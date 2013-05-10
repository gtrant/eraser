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

using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;

using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

using System.ComponentModel;

using Microsoft.VisualBasic.Devices;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;

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
		public static void CreateReport(Exception e)
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
		public static BlackBoxReport[] GetDumps()
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
		private static void WriteMemoryDump(string dumpFolder, Exception e)
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
		private static void WriteDebugLog(string dumpFolder, Exception exception)
		{
			using (FileStream file = new FileStream(Path.Combine(dumpFolder, DebugLogFileName),
				FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			using (StreamWriter stream = new StreamWriter(file))
			{
				//Application information
				string separator = new string('-', 100);
				const string lineFormat = "{0,20}: {1}";
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

                //Disk Drives
                stream.WriteLine();
                stream.WriteLine("Logical Drives");
                stream.WriteLine(separator);
                foreach (System.IO.DriveInfo DriveInfo1 in System.IO.DriveInfo.GetDrives())
                {
                    try
                    {
                        stream.WriteLine("\t Drive: {0}\n\t\t VolumeLabel: {1}\n\t\t DriveType: {2}\n\t\t DriveFormat: {3}\n\t\t TotalSize: {4}\n\t\t AvailableFreeSpace: {5}\n",
                            DriveInfo1.Name, DriveInfo1.VolumeLabel, DriveInfo1.DriveType, DriveInfo1.DriveFormat, DriveInfo1.TotalSize, DriveInfo1.AvailableFreeSpace);
                    }
                    catch
                    {
                    }
                }
                stream.WriteLine("SystemPageSize:  {0}\n", Environment.SystemPageSize);
                stream.WriteLine("Version:  {0}", Environment.Version);
            
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
		private static void WriteScreenshot(string dumpFolder)
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
}