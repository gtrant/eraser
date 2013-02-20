/* 
 * $Id: Program.cs 2770 2012-07-03 03:35:31Z lowjoel $
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
using System.Linq;
using System.Text;

using System.IO;

using ComLib.Arguments;

using Eraser.Manager;
using Eraser.Plugins;
using System.Windows.Forms;

namespace Eraser.Service
{
	/// <summary>
	/// Program arguments which only apply to the Eraser Service.
	/// </summary>
	class ServiceArguments
	{
		/// <summary>
		/// True if the command line specified atRestart, which should result in the
		/// queueing of tasks meant for running at restart.
		/// </summary>
		[Arg("atRestart", "r", "The program should queue all tasks scheduled for " +
			"running at the system restart.", typeof(bool), false, false)]
		public bool AtRestart { get; set; }
	}

	public class Program
	{
		static void Main(string[] args)
		{
			//Parse the command line arguments
			ServiceArguments arguments = new ServiceArguments();
			Args.Parse(args, CommandLinePrefixes, CommandLineSeparators, arguments);
			
			using (ManagerLibrary library = new ManagerLibrary(Settings.Get()))
			{
				RemoteExecutorServer eraserClient = null;
				try
				{
					eraserClient = new RemoteExecutorServiceServer();
				}
				catch (InvalidOperationException)
				{
					//We already have another instance running.
					return;
				}

				try
				{
					//Load the task list
					try
					{
						if (File.Exists(TaskListPath))
						{
							using (FileStream stream = new FileStream(TaskListPath, FileMode.Open,
								FileAccess.Read, FileShare.Read))
							{
								eraserClient.Tasks.LoadFromStream(stream);
							}
						}
					}
					catch (InvalidDataException)
					{
						File.Delete(TaskListPath);
					}

					//Queue tasks meant for running at restart if we are given that command line.
					if (arguments.AtRestart)
						eraserClient.QueueRestartTasks();

					//Run the eraser client.
					eraserClient.Run();
					Application.Run();

					//Save the task list
					if (!Directory.Exists(Program.AppDataPath))
						Directory.CreateDirectory(Program.AppDataPath);
					eraserClient.Tasks.SaveToFile(TaskListPath);
				}
				finally
				{
					//Dispose the Eraser Executor instance
					eraserClient.Dispose();
				}
			}
		}

		/// <summary>
		/// Path to the Eraser application data path.
		/// </summary>
		public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(
			Environment.SpecialFolder.LocalApplicationData), @"Eraser 6");

		/// <summary>
		/// File name of the Eraser task list.
		/// </summary>
		public const string TaskListFileName = @"Task List.ersy";

		/// <summary>
		/// Path to the Eraser task list.
		/// </summary>
		public static readonly string TaskListPath = Path.Combine(AppDataPath, TaskListFileName);

		/// <summary>
		/// Path to the Eraser settings key (relative to HKCU)
		/// </summary>
		public const string SettingsPath = @"SOFTWARE\Eraser\Eraser 6";

		/// <summary>
		/// The acceptable list of command line prefixes we will accept.
		/// </summary>
		public const string CommandLinePrefixes = "^(/|-|--)";

		/// <summary>
		/// The acceptable list of command line separators we will accept.
		/// </summary>
		public const string CommandLineSeparators = "(:|=)";
	}

	/// <summary>
	/// A subclass of RemoteExecutorServer so that we will quit when the client requests Exit.
	/// </summary>
	class RemoteExecutorServiceServer : RemoteExecutorServer
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				Application.Exit();
		}
	}
}
