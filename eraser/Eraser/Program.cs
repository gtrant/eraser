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
using System.Windows.Forms;

using System.IO;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Text;

using System.Reflection;
using System.Diagnostics;

using ComLib.Arguments;

using Eraser.Manager;
using Eraser.Util;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using Eraser.Plugins.Registrars;

namespace Eraser
{
	internal static partial class Program
	{
		/// <summary>
		/// The common program arguments shared between the GUI and console programs.
		/// </summary>
		class Arguments
		{
			/// <summary>
			/// True if the program should not be started with any user-visible interfaces.
			/// </summary>
			/// <remarks>Errors will also be silently ignored.</remarks>
			[Arg("quiet", "q", "The program should not be started with any user-visible " +
				"interfaces. Errors will be silently ignored.", typeof(bool), false, false)]
			public bool Quiet { get; set; }
		}

		/// <summary>
		/// Program arguments which only apply to the GUI program.
		/// </summary>
		class GuiArguments : Arguments
		{
			/// <summary>
			/// True if the command line specified atRestart, which should result in the
			/// queueing of tasks meant for running at restart.
			/// </summary>
			[Arg("atRestart",  "r", "The program should queue all tasks scheduled for " +
				"running at the system restart.", typeof(bool), false, false)]
			public bool AtRestart { get; set; }
		}

		class ConsoleArguments : Arguments
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			public ConsoleArguments()
			{
			}

			/// <summary>
			/// Copy constructor.
			/// </summary>
			/// <param name="arguments">The <see cref="ConsoleArguments"/> to use as a template
			/// for this instance.</param>
			protected ConsoleArguments(ConsoleArguments arguments)
			{
				Action = arguments.Action;
				PositionalArguments = arguments.PositionalArguments;
			}

			/// <summary>
			/// The Action which this handler is in charge of.
			/// </summary>
			[Arg(0, "The action this command line is stating.", typeof(string), true, null)]
			public string Action { get; set; }

			/// <summary>
			/// The list of command line parameters not placed in a switch.
			/// </summary>
			public List<string> PositionalArguments { get; set; }
		}
		
		class EraseArguments : ConsoleArguments
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			public EraseArguments()
			{
			}

			/// <summary>
			/// Copy constructor.
			/// </summary>
			/// <param name="arguments">The <see cref="EraseArguments"/> to use as a template
			/// for this instance.</param>
			protected EraseArguments(EraseArguments arguments)
				: base(arguments)
			{
				ErasureMethod = arguments.ErasureMethod;
			}

			/// <summary>
			/// The erasure method which the user specified on the command line.
			/// </summary>
			[Arg("method", "m", "The erasure method to use", typeof(string), false, null)]
			public string ErasureMethod { get; set; }
		}
		
		class TaskArguments : EraseArguments
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			public TaskArguments()
			{
			}

			/// <summary>
			/// Constructs Task arguments from Erase arguments.
			/// </summary>
			/// <param name="arguments">The <see cref="EraseArguments"/> to use as a template
			/// for this instance.</param>
			internal TaskArguments(EraseArguments arguments)
				: base(arguments)
			{
			}

			/// <summary>
			/// The schedule for the current set of targets.
			/// </summary>
			[Arg("schedule", "s", "The schedule to use", typeof(Schedule), false, null)]
			public string Schedule { get; set; }
		}
		
		class ShellArguments : TaskArguments
		{
			/// <summary>
			/// The parent HWND which can be used as a parent to display dialogs.
			/// </summary>
			[Arg("parent", "p", "The parent HWND which can be used as a parent to display " +
				"dialogues", typeof(string), false, null)]
			public string Parent { get; set; }

			/// <summary>
			/// Whether we should display a confirmation dialog.
			/// </summary>
			[Arg("confirm", "c", "Whether a confirmation dialog should be shown", typeof(bool),
				false, true)]
			public bool Confirm { get; set; }
		}

		public enum ShellActions
		{
			/// <summary>
			/// Erase the selected items now.
			/// </summary>
			EraseNow,

			/// <summary>
			/// Erase the selected items on restart.
			/// </summary>
			EraseOnRestart,

			/// <summary>
			/// Erase the unused space on the drive.
			/// </summary>
			EraseUnusedSpace,

			/// <summary>
			/// Securely moves a file from one drive to another (simple rename if the source and
			/// destination drives are the same)
			/// </summary>
			SecureMove
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] rawCommandLine)
		{
			//Immediately parse command line arguments. Start by substituting all
			//response files ("@filename") arguments with the arguments found in the
			//file
			List<string> commandLine = new List<string>(rawCommandLine.Length);
			foreach (string argument in rawCommandLine)
			{
				if (argument[0] == '@' && File.Exists(argument.Substring(1)))
				{
					//The current parameter is a response file, parse the file
					//for arguments and substitute it.
					using (TextReader reader = new StreamReader(argument.Substring(1)))
					{
						commandLine.AddRange(Shell.ParseCommandLine(reader.ReadToEnd()));
					}
				}
				else
					commandLine.Add(argument);
			}

			string[] finalCommandLine = commandLine.ToArray();
			ComLib.BoolMessageItem argumentParser = Args.Parse(finalCommandLine,
				CommandLinePrefixes, CommandLineSeparators);
			Args parsedArguments = (Args)argumentParser.Item;

			//Set application defaults for consistent behaviour across GUI and
			//Console instances
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			//Load the Eraser.Manager library
			using (ManagerLibrary library = new ManagerLibrary(Settings.Get()))
			{
				//Set our UI language
				EraserSettings settings = EraserSettings.Get();
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(settings.Language);

				//We default to a GUI if:
				// - The parser did not succeed.
				// - The parser resulted in an empty arguments list
				// - The parser's argument at index 0 is not equal to the first argument
				//   (this is when the user is passing GUI options -- command line options
				//   always start with the action, e.g. Eraser help, or Eraser addtask
				if (!argumentParser.Success || parsedArguments.IsEmpty ||
					parsedArguments.Positional.Count == 0 ||
					parsedArguments.Positional[0] != parsedArguments.Raw[0])
				{
					GUIMain(finalCommandLine);
				}
				else
				{
					return CommandMain(finalCommandLine);
				}
			}

			//Return zero to signify success
			return 0;
		}

		#region Console Program code
		/// <summary>
		/// Connects to the running Eraser instance for erasures.
		/// </summary>
		/// <returns>The connectin with the remote instance.</returns>
		private static RemoteExecutorClient CommandConnect()
		{
			try
			{
				RemoteExecutorClient result = new RemoteExecutorClient();
				result.Run();
				if (!result.IsConnected)
				{
					//The client cannot connect to the server. This probably means
					//that the server process isn't running. Start an instance.
					Process eraserInstance = Process.Start(
						Assembly.GetExecutingAssembly().Location, "/quiet");
					eraserInstance.WaitForInputIdle();

					//Wait for the server to be initialised.
					for (int i = 0; !result.IsConnected; ++i)
					{
						Thread.Sleep(100);
						result.Run();

						//After 10s, we should probably give up.
						if (i > 100)
							throw new IOException(S._("Eraser cannot connect to the running " +
								"instance for erasures."));
					}
				}

				return result;
			}
			catch (UnauthorizedAccessException e)
			{
				//We can't connect to the pipe because the other instance of Eraser
				//is running with higher privileges than this instance.
				throw new UnauthorizedAccessException(S._("Another instance of Eraser " +
					"is already running but it is running with higher privileges than " +
					"this instance of Eraser. Tasks cannot be added in this manner.\n\n" +
					"Close the running instance of Eraser and start it again without " +
					"administrator privileges, or run the command again as an " +
					"administrator.", e));
			}
		}

		/// <summary>
		/// Runs Eraser as a command-line application.
		/// </summary>
		/// <param name="commandLine">The command line parameters passed to Eraser.</param>
		private static int CommandMain(string[] commandLine)
		{
			using (ConsoleProgram program = new ConsoleProgram(commandLine))
				try
				{
					program.Handlers.Add("help",
						new ConsoleActionData(CommandHelp, new ConsoleArguments()));
					program.Handlers.Add("erase",
						new ConsoleActionData(CommandErase, new EraseArguments())); 
					program.Handlers.Add("addtask",
						new ConsoleActionData(CommandAddTask, new TaskArguments()));
					program.Handlers.Add("importtasklist",
						new ConsoleActionData(CommandImportTaskList, new ConsoleArguments()));
					program.Handlers.Add("shell",
						new ConsoleActionData(CommandShell, new ShellArguments()));
					program.Run();
					return 0;
				}
				catch (UnauthorizedAccessException)
				{
					return Win32ErrorCode.AccessDenied;
				}
				catch (Win32Exception e)
				{
					Console.WriteLine(e.Message);
					return e.ErrorCode;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					throw;
				}
		}

		/// <summary>
		/// Prints the command line help for Eraser.
		/// </summary>
		private static void PrintCommandHelp()
		{
			//Get the command-line help for every erasure target
			StringBuilder targets = new StringBuilder();
			foreach (IErasureTarget target in Host.Instance.ErasureTargetFactories)
			{
				//Replace all \r\n with \n, and split into lines
				string[] helpText = target.Configurer.Help().Replace("\r\n", "\n").Split('\r', '\n');

				//Pad the start of each line with spaces
				foreach (string line in helpText)
					targets.AppendLine(line.Insert(0, "    "));
			}

			//Get the list of registered Erasure Methods. First, output the header.
			const string methodFormat = "    {0,-2} {1,-35} {2}\n";
			StringBuilder methods = new StringBuilder();
			methods.AppendFormat(methodFormat, "", "Erasure Method", "GUID");
			methods.AppendLine("    " + new string('-', 75));

			//Generate the list of erasure methods.
			foreach (IErasureMethod method in Host.Instance.ErasureMethods)
			{
				methods.AppendFormat(methodFormat, (method is IDriveErasureMethod) ?
					"U" : "", method.Name, method.Guid);
			}

			//Print the message
			Console.WriteLine(S._(@"usage: Eraser <action> <arguments>
where action is
  help                Show this help message.
  erase               Erases items specified on the command line. This is
                      equivalent to addtask, with the schedule set to ""now"".
  addtask             Adds a task to the current task list.
  importtasklist      Imports an Eraser Task list to the current user's Task
                      List.

global parameters:
  /quiet              Do not create a Console window to display progress.

parameters for help:
  eraser help

  no parameters to set.

parameters for erase and addtask:
  eraser erase [/method=(<methodGUID>|<methodName>)] <target> [target [...]]
  eraser addtask [/method=(<methodGUID>|<methodName>)] [/schedule=(now|manually|restart)] <target> [target [...]]

  /method             The Erasure method to use.
    methodGUID and methodName can be any GUID/Name from the following list:
{0}
    Only erasure methods labelled ""U"" can be used to erase unused disk space.

  /schedule           The schedule the task will follow. The value must be one
                      of:
    now               The task will be queued for immediate execution.
    manually          The task will be created but not queued for execution.
    restart           The task will be queued for execution when the computer
                      is next restarted.

                      This parameter is only valid for use with ""addtask"".

  target is one or more of:
{1}
parameters for importtasklist:
  eraser importtasklist <file>[...]

    file               A list of one or more files to import.

All arguments are case sensitive.

Response files can be used for very long command lines (generally, anything
involving more than 32,000 characters.) Response files are used by prepending
""@"" to the path to the file, and passing it into the command line. The
contents of the response files' will be substituted at the same position into
the command line.", methods, targets));

			Console.Out.Flush();
		}

		/// <summary>
		/// Prints the help text for Eraser (with copyright)
		/// </summary>
		/// <param name="arguments">Not used.</param>
		private static void CommandHelp(ConsoleArguments arguments)
		{
			Console.WriteLine(S._(@"Eraser {0}
(c) 2008-2012 The Eraser Project
Eraser is Open-Source Software: see http://eraser.heidi.ie/ for details.
", BuildInfo.AssemblyFileVersion));

			PrintCommandHelp();
		}

		/// <summary>
		/// Parses the command line for tasks and adds them to run immediately
		/// using the <see cref="RemoveExecutor"/> class.
		/// </summary>
		/// <param name="arg">The command line parameters passed to the program.</param>
		private static void CommandErase(ConsoleArguments arg)
		{
			TaskArguments arguments = new TaskArguments((EraseArguments)arg);
			arguments.Schedule = "NOW";

			CommandAddTask(arguments);
		}

		/// <summary>
		/// Parses the command line for tasks and adds them using the
		/// <see cref="RemoteExecutor"/> class.
		/// </summary>
		/// <param name="arg">The command line parameters passed to the program.</param>
		private static void CommandAddTask(ConsoleArguments arg)
		{
			TaskArguments arguments = (TaskArguments)arg;
			Task task = TaskFromCommandLine(arguments);

			//Send the task out.
			using (eraserClient = CommandConnect())
				eraserClient.Tasks.Add(task);
		}

		/// <summary>
		/// Parses the command line for erasure targets and returns them as
		/// a Task object.
		/// </summary>
		/// <param name="arguments">The arguments specified on the command line.</param>
		/// <returns>The task represented on the command line.</returns>
		private static Task TaskFromCommandLine(TaskArguments arguments)
		{
			//Create the task
			Task task = new Task();

			//Get the erasure method the user wants to use
			IErasureMethod method = string.IsNullOrEmpty(arguments.ErasureMethod) ?
				ErasureMethodRegistrar.Default :
				ErasureMethodFromNameOrGuid(arguments.ErasureMethod);

			//Define the schedule
			if (!string.IsNullOrEmpty(arguments.Schedule))
				switch (arguments.Schedule.ToUpperInvariant())
				{
					case "NOW":
						task.Schedule = Schedule.RunNow;
						break;
					case "MANUALLY":
						task.Schedule = Schedule.RunManually;
						break;
					case "RESTART":
						task.Schedule = Schedule.RunOnRestart;
						break;
					default:
						throw new ArgumentException(
							S._("Unknown schedule type: {0}", arguments.Schedule), "/schedule");
				}

			//Parse the rest of the command line parameters as target expressions.
			foreach (string argument in arguments.PositionalArguments)
			{
				IErasureTarget selectedTarget = null;

				//Iterate over every defined erasure target
				foreach (IErasureTarget target in Host.Instance.ErasureTargetFactories)
				{
					//See if this argument can be handled by the target's configurer
					IErasureTargetConfigurer configurer = target.Configurer;
					if (configurer.ProcessArgument(argument))
					{
						//Check whether a target has been set (implicitly: check whether two
						//configurers can process the argument)
						if (selectedTarget == null)
						{
							configurer.SaveTo(target);
							selectedTarget = target;
						}
						else
						{
							//Yes, it is an ambiguity. Throw an error.
							throw new ArgumentException(S._("Ambiguous argument: {0} can be " +
								"handled by more than one erasure target.", argument));
						}
					}
				}

				//Check whether a target has been made from parsing the entry.
				if (selectedTarget == null)
				{
					Console.WriteLine(S._("Unknown argument: {0}, skipped.", argument));
				}
				else
				{
					selectedTarget.Method = method;
					task.Targets.Add(selectedTarget);
				}
			}

			//Check the number of tasks in the task.
			if (task.Targets.Count == 0)
				throw new ArgumentException(S._("Tasks must contain at least one erasure target."));

			return task;
		}

		private static IErasureMethod ErasureMethodFromNameOrGuid(string param)
		{
			try
			{
				return Host.Instance.ErasureMethods[new Guid(param)];
			}
			catch (FormatException)
			{
				//Invalid GUID. Check every registered erasure method for the name
				string upperParam = param.ToUpperInvariant();
				IErasureMethod result = null;
				foreach (IErasureMethod method in Host.Instance.ErasureMethods)
				{
					if (method.Name.ToUpperInvariant() == upperParam)
						if (result == null)
							result = method;
						else
							throw new ArgumentException(S._("Ambiguous erasure method name: {0} " +
								"identifies more than one erasure method.", param));
				}
			}

			throw new ArgumentException(S._("The provided Erasure Method '{0}' does not exist.",
				param));
		}

		/// <summary>
		/// Imports the given tasklists and adds them to the global Eraser instance.
		/// </summary>
		/// <param name="args">The list of files specified on the command line.</param>
		private static void CommandImportTaskList(ConsoleArguments args)
		{
			//Import the task list
			using (eraserClient = CommandConnect())
				foreach (string path in args.PositionalArguments)
					using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
						eraserClient.Tasks.LoadFromStream(stream);
		}

		/// <summary>
		/// Handles the files from the Shell extension.
		/// </summary>
		/// <param name="args">The command line parameters passed to the program.</param>
		private static void CommandShell(ConsoleArguments args)
		{
			ShellArguments arguments = (ShellArguments)args;
			Task task = TaskFromCommandLine(arguments);

			//Confirm that the user wants the erase.
			if (arguments.Confirm)
			{
				//Do we have a parent dialog?
				IWin32Window parent = null;
				if (!string.IsNullOrEmpty(arguments.Parent))
				{
					//We do. Get the handle
					parent = new Win32Window((IntPtr)(ulong)
						Convert.ChangeType(arguments.Parent, typeof(ulong)));

					//And find the top-level window of the handle.
					IWin32Window topLevel = UserApi.GetAncestor(parent,
						UserApi.GetAncestorFlags.GA_ROOT);
					if (topLevel != null)
						parent = topLevel;
				}

				using (Form dialog = new ShellConfirmationDialog(task))
				{
					if (dialog.ShowDialog(parent) != DialogResult.Yes)
						return;
				}
			}

			//Then queue for erasure.
			using (eraserClient = CommandConnect())
				eraserClient.Tasks.Add(task);
		}
		#endregion

		#region GUI Program code
		/// <summary>
		/// Runs Eraser as a GUI application.
		/// </summary>
		/// <param name="commandLine">The command line parameters passed to Eraser.</param>
		private static void GUIMain(string[] commandLine)
		{
			//Create a unique program instance ID for this user.
			string instanceId = "Eraser-BAD0DAC6-C9EE-4acc-8701-C9B3C64BC65E-GUI-" +
				WindowsIdentity.GetCurrent().User.ToString();

			//Then initialise the instance and initialise the Manager library.
			using (GuiProgram program = new GuiProgram(commandLine, instanceId))
			{
				program.InitInstance += OnGUIInitInstance;
				program.NextInstance += OnGUINextInstance;
				program.ExitInstance += OnGUIExitInstance;
				program.Run();
			}
		}

		/// <summary>
		/// Triggered when the Program is started for the first time.
		/// </summary>
		/// <param name="sender">The sender of the object.</param>
		/// <param name="e">Event arguments.</param>
		private static void OnGUIInitInstance(object sender, InitInstanceEventArgs e)
		{
			GuiProgram program = (GuiProgram)sender;
			eraserClient = new RemoteExecutorServer();
			Application.SafeTopLevelCaptionFormat = S._("Eraser");

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
			catch (InvalidDataException ex)
			{
				File.Delete(TaskListPath);
				MessageBox.Show(S._("Could not load task list. All task entries have " +
					"been lost. The error returned was: {0}", ex.Message), S._("Eraser"),
					MessageBoxButtons.OK, MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button1,
					Localisation.IsRightToLeft(null) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
			}

			//Decide whether to display any UI.
			GuiArguments arguments = new GuiArguments();
			Args.Parse(program.CommandLine, CommandLinePrefixes, CommandLineSeparators, arguments);
			e.ShowMainForm = !arguments.AtRestart && !arguments.Quiet;

			//Queue tasks meant for running at restart if we are given that command line.
			if (arguments.AtRestart)
				eraserClient.QueueRestartTasks();

			//Run the eraser client.
			eraserClient.Run();

			//Create the main form.
			program.MainForm = new MainForm();
		}

		/// <summary>
		/// Triggered when a second instance of Eraser is started.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">Event argument.</param>
		private static void OnGUINextInstance(object sender, NextInstanceEventArgs e)
		{
			//Another instance of the GUI Program has been started: show the main window
			//now as we still do not have a facility to handle the command line arguments.
			GuiProgram program = (GuiProgram)sender;

			//Invoke the function if we aren't on the main thread
			if (program.MainForm.InvokeRequired)
			{
				program.MainForm.Invoke(
					(GuiProgram.NextInstanceEventHandler)OnGUINextInstance,
					sender, e);
				return;
			}

			program.MainForm.Show();
		}

		/// <summary>
		/// Triggered when the first instance of Eraser is exited.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">Event argument.</param>
		private static void OnGUIExitInstance(object sender, EventArgs e)
		{
			//Save the task list
			if (!Directory.Exists(Program.AppDataPath))
				Directory.CreateDirectory(Program.AppDataPath);
			eraserClient.Tasks.SaveToFile(TaskListPath);

			//Dispose the eraser executor instance
			eraserClient.Dispose();
		}
		#endregion

		/// <summary>
		/// The acceptable list of command line prefixes we will accept.
		/// </summary>
		public const string CommandLinePrefixes = "^(/|-|--)";

		/// <summary>
		/// The acceptable list of command line separators we will accept.
		/// </summary>
		public const string CommandLineSeparators = "(:|=)";

		/// <summary>
		/// The global Executor instance.
		/// </summary>
		public static Executor eraserClient;

		/// <summary>
		/// Path to the Eraser application data path.
		/// </summary>
		public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(
			Environment.SpecialFolder.LocalApplicationData), @"Eraser 6");

		/// <summary>
		/// File name of the Eraser task list.
		/// </summary>
		private const string TaskListFileName = @"Task List.ersy";

		/// <summary>
		/// Path to the Eraser task list.
		/// </summary>
		public static readonly string TaskListPath = Path.Combine(AppDataPath, TaskListFileName);

		/// <summary>
		/// Path to the Eraser settings key (relative to HKCU)
		/// </summary>
		public const string SettingsPath = @"SOFTWARE\Eraser\Eraser 6";

		public static IEnumerable<IErasureTarget> ErasureTargetRegistrar { get; set; }
	}

	class Win32Window : IWin32Window
	{
		public Win32Window(IntPtr hwnd)
		{
			Hwnd = hwnd;
		}

		#region IWin32Window Members

		public IntPtr Handle
		{
			get { return Hwnd; }
		}

		#endregion

		private IntPtr Hwnd;
	}
}