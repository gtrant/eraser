/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Security.Principal;
using System.Security.AccessControl;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] commandLine)
		{
			//Trivial case: no command parameters
			if (commandLine.Length == 0)
				GUIMain(commandLine);

			//Determine if the sole parameter is --restart; if it is, start the GUI
			//passing isRestart as true. Otherwise, we're a console application.
			else if (commandLine.Length == 1)
			{
				if (commandLine[0] == "--atRestart" || commandLine[0] == "--quiet")
				{
					GUIMain(commandLine);
				}
				else
				{
					return CommandMain(commandLine);
				}
			}

			//The other trivial case: definitely a console application.
			else
				return CommandMain(commandLine);

			//No error.
			return 0;
		}

		/// <summary>
		/// Runs Eraser as a command-line application.
		/// </summary>
		/// <param name="commandLine">The command line parameters passed to Eraser.</param>
		private static int CommandMain(string[] commandLine)
		{
			//True if the user specified a quiet command.
			bool isQuiet = false;

			try
			{
				CommandLineProgram program = new CommandLineProgram(commandLine);
				isQuiet = program.Arguments.Quiet;

				using (ManagerLibrary library = new ManagerLibrary(new Settings()))
					program.Run();

				return 0;
			}
			catch (UnauthorizedAccessException)
			{
				return 5; //ERROR_ACCESS_DENIED
			}
			catch (Win32Exception e)
			{
				Console.WriteLine(e.Message);
				return e.ErrorCode;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return 1;
			}
			finally
			{
				//Flush the buffered output to the console
				Console.Out.Flush();

				//Don't ask for a key to press if the user specified Quiet
				if (!isQuiet)
				{
					Console.Write("\nPress enter to continue . . . ");
					Console.Out.Flush();
					Console.ReadLine();
				}

				KernelApi.FreeConsole();
			}
		}

		/// <summary>
		/// Runs Eraser as a GUI application.
		/// </summary>
		/// <param name="commandLine">The command line parameters passed to Eraser.</param>
		private static void GUIMain(string[] commandLine)
		{
			//Create the program instance
			using (GUIProgram program = new GUIProgram(commandLine, "Eraser-BAD0DAC6-C9EE-4acc-" +
				"8701-C9B3C64BC65E-GUI-" +
				System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString()))

			//Then run the program instance.
			using (ManagerLibrary library = new ManagerLibrary(new Settings()))
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
		/// <returns>True if the user did not specify --quiet, false otherwise.</returns>
		private static bool OnGUIInitInstance(object sender)
		{
			GUIProgram program = (GUIProgram)sender;
			eraserClient = new RemoteExecutorServer();

			//Set our UI language
			EraserSettings settings = EraserSettings.Get();
			System.Threading.Thread.CurrentThread.CurrentUICulture =
				new CultureInfo(settings.Language);
			Application.SafeTopLevelCaptionFormat = S._("Eraser");

			//Load the task list
			SettingsCompatibility.Execute();
			try
			{
				if (System.IO.File.Exists(TaskListPath))
				{
					using (FileStream stream = new FileStream(TaskListPath, FileMode.Open,
						FileAccess.Read, FileShare.Read))
					{
						eraserClient.Tasks.LoadFromStream(stream);
					}
				}
			}
			catch (SerializationException e)
			{
				System.IO.File.Delete(TaskListPath);
				MessageBox.Show(S._("Could not load task list. All task entries have " +
					"been lost. The error returned was: {0}", e.Message), S._("Eraser"),
					MessageBoxButtons.OK, MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button1,
					S.IsRightToLeft(null) ? MessageBoxOptions.RtlReading : 0);
			}

			//Create the main form
			program.MainForm = new MainForm();
			program.MainForm.CreateControl();
			bool showMainForm = true;
			foreach (string param in program.CommandLine)
			{
				//Run tasks which are meant to be run on restart
				switch (param)
				{
					case "--atRestart":
						eraserClient.QueueRestartTasks();
						goto case "--quiet";

					//Hide the main form if the user specified the quiet command
					//line
					case "--quiet":
						showMainForm = false;
						break;
				}
			}

			//Run the eraser client.
			eraserClient.Run();
			return showMainForm;
		}

		/// <summary>
		/// Triggered when a second instance of Eraser is started.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="message">The message from the source application.</param>
		private static void OnGUINextInstance(object sender, string message)
		{
			//Another instance of the GUI Program has been started: show the main window
			//now as we still do not have a facility to handle the command line arguments.
			GUIProgram program = (GUIProgram)sender;

			//Invoke the function if we aren't on the main thread
			if (program.MainForm.InvokeRequired)
			{
				program.MainForm.Invoke(new GUIProgram.NextInstanceFunction(
					OnGUINextInstance), new object[] { sender, message });
				return;
			}

			program.MainForm.Visible = true;
		}

		/// <summary>
		/// Triggered when the first instance of Eraser is exited.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		private static void OnGUIExitInstance(object sender)
		{
			//Save the task list
			if (!Directory.Exists(Program.AppDataPath))
				Directory.CreateDirectory(Program.AppDataPath);
			using (FileStream stream = new FileStream(TaskListPath, FileMode.Create,
				FileAccess.Write, FileShare.None))
			{
				eraserClient.Tasks.SaveToStream(stream);
			}

			//Dispose the eraser executor instance
			eraserClient.Dispose();
		}

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
		private const string TaskListFileName = @"Task List.ersx";

		/// <summary>
		/// Path to the Eraser task list.
		/// </summary>
		public static readonly string TaskListPath = Path.Combine(AppDataPath, TaskListFileName);

		/// <summary>
		/// Path to the Eraser settings key (relative to HKCU)
		/// </summary>
		public const string SettingsPath = @"SOFTWARE\Eraser\Eraser 6";
	}

	class GUIProgram : IDisposable
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="commandLine">The command line arguments associated with
		/// this program launch</param>
		/// <param name="instanceID">The instance ID of the program, used to group
		/// instances of the program together.</param>
		public GUIProgram(string[] commandLine, string instanceID)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			this.instanceID = instanceID;
			this.CommandLine = commandLine;

			//Check if there already is another instance of the program.
			globalMutex = new Mutex(true, instanceID, out isFirstInstance);
		}

		~GUIProgram()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				globalMutex.Close();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Runs the event loop of the GUI program, returning true if the program
		/// was started as there were no other instances of the program, or false
		/// if other instances were found.
		/// </summary>
		/// <remarks>
		/// This function must always be called in your program, regardless
		/// of the value of <see cref="IsAlreadyRunning"/>. If this function is not
		/// called, the first instance will never be notified that another was started.
		/// </remarks>
		/// <returns>True if the application was started, or false if another instance
		/// was detected.</returns>
		public bool Run()
		{
			//If no other instances are running, set up our pipe server so clients
			//can connect and give us subsequent command lines.
			if (IsFirstInstance)
			{
				try
				{
					//Create the pipe server which will handle connections to us
					pipeServer = new Thread(ServerMain);
					pipeServer.Start();

					//Initialise and run the program.
					bool ShowMainForm = OnInitInstance(this);
					if (MainForm == null)
						return false;

					//Handle the exit instance event. This will occur when the main form
					//has been closed.
					Application.ApplicationExit += OnExitInstance;

					if (ShowMainForm)
						Application.Run(MainForm);
					else
						Application.Run();

					return true;
				}
				finally
				{
					pipeServer.Abort();
				}
			}

			//Another instance of the program is running. Connect to it and transfer
			//the command line arguments
			else
			{
				try
				{
					NamedPipeClientStream client = new NamedPipeClientStream(".", instanceID,
						PipeDirection.Out);
					client.Connect(500);

					StringBuilder commandLineStr = new StringBuilder(CommandLine.Length * 64);
					foreach (string param in CommandLine)
						commandLineStr.Append(string.Format(
							CultureInfo.InvariantCulture, "{0}\0", param));

					byte[] buffer = new byte[commandLineStr.Length];
					int count = Encoding.UTF8.GetBytes(commandLineStr.ToString(), 0,
						commandLineStr.Length, buffer, 0);
					client.Write(buffer, 0, count);
				}
				catch (UnauthorizedAccessException)
				{
					//We can't connect to the pipe because the other instance of Eraser
					//is running with higher privileges than this instance. Tell the
					//user this is the case and show him how to resolve the issue.
					MessageBox.Show(S._("Another instance of Eraser is already running but it is " +
						"running with higher privileges than this instance of Eraser.\n\n" +
						"Eraser will now exit."), S._("Eraser"), MessageBoxButtons.OK,
						MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
						S.IsRightToLeft(null) ? MessageBoxOptions.RtlReading : 0);
				}
				catch (TimeoutException)
				{
					//Can't do much: half a second is a reasonably long time to wait.
				}
				return false;
			}
		}

		/// <summary>
		/// Holds information required for an asynchronous call to
		/// NamedPipeServerStream.BeginWaitForConnection.
		/// </summary>
		private struct ServerAsyncInfo
		{
			public NamedPipeServerStream Server;
			public AutoResetEvent WaitHandle;
		}

		/// <summary>
		/// Runs a background thread, monitoring for new connections to the server.
		/// </summary>
		private void ServerMain()
		{
			while (pipeServer.ThreadState != System.Threading.ThreadState.AbortRequested)
			{
				PipeSecurity security = new PipeSecurity();
				security.AddAccessRule(new PipeAccessRule(
					WindowsIdentity.GetCurrent().User,
					PipeAccessRights.FullControl, AccessControlType.Allow));
				using (NamedPipeServerStream server = new NamedPipeServerStream(instanceID,
					PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
					128, 128, security))
				{
					ServerAsyncInfo async = new ServerAsyncInfo();
					async.Server = server;
					async.WaitHandle = new AutoResetEvent(false);
					IAsyncResult result = server.BeginWaitForConnection(WaitForConnection, async);

					//Wait for the operation to complete.
					if (result.AsyncWaitHandle.WaitOne())
						//It completed. Wait for the processing to complete.
						async.WaitHandle.WaitOne();
				}
			}
		}

		/// <summary>
		/// Waits for new connections to be made to the server.
		/// </summary>
		/// <param name="result"></param>
		private void WaitForConnection(IAsyncResult result)
		{
			ServerAsyncInfo async = (ServerAsyncInfo)result.AsyncState;

			try
			{
				//We're done waiting for the connection
				async.Server.EndWaitForConnection(result);

				//Process the connection if the server was successfully connected.
				if (async.Server.IsConnected)
				{
					//Read the message from the secondary instance
					byte[] buffer = new byte[8192];
					string message = string.Empty;

					do
					{
						int lastRead = async.Server.Read(buffer, 0, buffer.Length);
						message += Encoding.UTF8.GetString(buffer, 0, lastRead);
					}
					while (!async.Server.IsMessageComplete);

					//Let the event handler process the message.
					OnNextInstance(this, message);
				}
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				//Reset the wait event
				async.WaitHandle.Set();
			}
		}

		/// <summary>
		/// Gets the command line arguments this instance was started with.
		/// </summary>
		public string[] CommandLine { get; private set; }

		/// <summary>
		/// Gets whether another instance of the program is already running.
		/// </summary>
		public bool IsFirstInstance
		{
			get
			{
				return isFirstInstance;
			}
		}

		/// <summary>
		/// The main form for this program instance. This form will be shown when
		/// run is called if it is non-null and if its Visible property is true.
		/// </summary>
		public Form MainForm { get; set; }

		#region Events
		/// <summary>
		/// The prototype of event handlers procesing the InitInstance event.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <returns>True if the MainForm property holds a valid reference to
		/// a form, and the form should be displayed to the user.</returns>
		public delegate bool InitInstanceFunction(object sender);

		/// <summary>
		/// The event object managing listeners to the instance initialisation event.
		/// This event is raised when the first instance of the program is started
		/// and this is where the program initialisation code should be.
		/// </summary>
		public event InitInstanceFunction InitInstance;

		/// <summary>
		/// Broadcasts the InitInstance event.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <returns>True if the MainForm object should be shown.</returns>
		private bool OnInitInstance(object sender)
		{
			if (InitInstance != null)
				return InitInstance(sender);
			return true;
		}

		/// <summary>
		/// The prototype of event handlers procesing the NextInstance event.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		public delegate void NextInstanceFunction(object sender, string message);

		/// <summary>
		/// The event object managing listeners to the next instance event. This
		/// event is raised when a second instance of the program is started.
		/// </summary>
		public event NextInstanceFunction NextInstance;

		/// <summary>
		/// Broadcasts the NextInstance event.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="message">The message sent by the secondary instance.</param>
		private void OnNextInstance(object sender, string message)
		{
			if (NextInstance != null)
				NextInstance(sender, message);
		}

		/// <summary>
		/// The prototype of event handlers procesing the ExitInstance event.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		public delegate void ExitInstanceFunction(object sender);

		/// <summary>
		/// The event object managing listeners to the exit instance event. This
		/// event is raised when the first instance of the program is exited.
		/// </summary>
		public event ExitInstanceFunction ExitInstance;

		/// <summary>
		/// Broadcasts the ExitInstance event.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		private void OnExitInstance(object sender)
		{
			if (ExitInstance != null)
				ExitInstance(sender);
		}

		/// <summary>
		/// Broadcasts the ExitInstance event after getting the notification that the
		/// application is exiting.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		private void OnExitInstance(object sender, EventArgs e)
		{
			if (ExitInstance != null)
				ExitInstance(sender);
		}
		#endregion

		#region Instance variables
		/// <summary>
		/// The Instance ID of this program, used to group program instances together.
		/// </summary>
		private string instanceID;

		/// <summary>
		/// The named mutex ensuring that only one instance of the application runs
		/// at a time.
		/// </summary>
		private Mutex globalMutex;

		/// <summary>
		/// The thread maintaining the pipe server for secondary instances to connect to.
		/// </summary>
		private Thread pipeServer;

		/// <summary>
		/// Holds whether this instance of the program is the first instance.
		/// </summary>
		private bool isFirstInstance;
		#endregion
	}

	class CommandLineProgram
	{
		#region Command Line parsing classes
		/// <summary>
		/// Manages a command line.
		/// </summary>
		public abstract class CommandLine
		{
			/// <summary>
			/// Gets the CommandLine-derived object for the given command line.
			/// </summary>
			/// <param name="cmdParams">The raw arguments passed to the program.</param>
			/// <returns>A processed CommandLine Object.</returns>
			public static CommandLine Get(string[] cmdParams)
			{
				//Get the action.
				if (cmdParams.Length < 1)
					throw new ArgumentException("An action must be specified.");
				string action = cmdParams[0];

				CommandLine result = null;
				switch (action)
				{
					case "help":
						result = new HelpCommandLine();
						break;
					case "querymethods":
						result = new QueryMethodsCommandLine();
						break;
					case "importtasklist":
						result = new ImportTaskListCommandLine();
						break;
					case "addtask":
						result = new AddTaskCommandLine();
						break;
				}

				if (result != null)
				{
					result.Parse(cmdParams);
					return result;
				}
				else
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
						"Unknown action: {0}", action));
			}

			/// <summary>
			/// Constructor.
			/// </summary>
			protected CommandLine()
			{
			}

			/// <summary>
			/// Parses the given command line arguments to the respective properties of
			/// the class.
			/// </summary>
			/// <param name="cmdParams">The raw arguments passed to the program.</param>
			/// <returns></returns>
			public bool Parse(string[] cmdParams)
			{
				//Iterate over each argument, resolving them ourselves and letting
				//subclasses resolve them if we don't know how to.
				for (int i = 1; i != cmdParams.Length; ++i)
				{
					if (IsParam(cmdParams[i], "quiet", "q"))
						Quiet = true;
					else if (!ResolveParameter(cmdParams[i]))
						throw new ArgumentException("Unknown argument: " + cmdParams[i]);
				}

				return true;
			}

			/// <summary>
			/// Called when a parameter is not used by the current CommandLine object
			/// for subclasses to handle their parameters.
			/// </summary>
			/// <param name="param">The parameter to resolve.</param>
			/// <returns>Return true if the parameter was resolved and accepted.</returns>
			protected virtual bool ResolveParameter(string param)
			{
				return false;
			}

			/// <summary>
			/// Checks if the given <paramref name="parameter"/> refers to the
			/// <paramref name="expectedParameter"/>, regardless of whether it is specified
			/// with -, --, or /
			/// </summary>
			/// <param name="parameter">The parameter on the command line.</param>
			/// <param name="expectedParameter">The parameter the program is looking for, without
			/// the - or / prefix.</param>
			/// <param name="shortParameter">The short parameter when used with a single hyphen,
			/// without the - or / prefix.</param>
			/// <returns>True if the parameter references the given expected parameter.</returns>
			protected static bool IsParam(string parameter, string expectedParameter,
				string shortParameter)
			{
				//Trivial case
				if (parameter.Length < 1)
					return false;

				//Extract the bits before the equal sign.
				{
					int equalPos = parameter.IndexOf('=');
					if (equalPos != -1)
						parameter = parameter.Substring(0, equalPos);
				}

				//Get the first letter.
				switch (parameter[0])
				{
					case '-':
						//Can be a - or a --. Check for the second parameter
						if (parameter.Length < 2)
							//Nothing specified at the end... it's invalid.
							return false;

						if (parameter[1] == '-')
							return parameter.Substring(2) == expectedParameter;
						else if (string.IsNullOrEmpty(shortParameter))
							return parameter.Substring(1) == expectedParameter;
						else
							return parameter.Substring(1) == shortParameter;

					case '/':
						//The / can be used with both long and short parameters.
						parameter = parameter.Substring(1);
						return parameter == expectedParameter || (
							!string.IsNullOrEmpty(shortParameter) && parameter == shortParameter
						);

					default:
						return false;
				}
			}

			/// <summary>
			/// Gets the list of subparameters of the parameter. Subparameters are text
			/// after the first =, separated by commas.
			/// </summary>
			/// <param name="param">The subparameter text to parse.</param>
			/// <returns>The list of subparameters in the parameter.</returns>
			protected static List<KeyValuePair<string, string>> GetSubParameters(string param)
			{
				List<KeyValuePair<string, string>> result =
					new List<KeyValuePair<string, string>>();
				int lastPos = 0;
				int commaPos = (param += ',').IndexOf(',');

				while (commaPos != -1)
				{
					//Check that the first parameter is not a \ otherwise this comma
					//is escaped
					if (commaPos == 0 ||									//No possibility of escaping
						(commaPos >= 1 && param[commaPos - 1] != '\\') ||	//Second character
						(commaPos >= 2 && param[commaPos - 2] == '\\'))		//Cannot be a \\ which is an escape
					{
						//Extract the current subparameter, and dissect the subparameter
						//at the first =.
						string subParam = param.Substring(lastPos, commaPos - lastPos);
						int equalPos = -1;

						do
						{
							equalPos = subParam.IndexOf('=', equalPos + 1);
							if (equalPos == -1)
							{
								result.Add(new KeyValuePair<string, string>(
									UnescapeCommandLine(subParam), null));
							}
							else if (equalPos == 0 ||								//No possibility of escaping
								(equalPos >= 1 && subParam[equalPos - 1] != '\\') ||//Second character
								(equalPos >= 2 && subParam[equalPos - 2] == '\\'))	//Double \\ which is an escape
							{
								result.Add(new KeyValuePair<string, string>(
									UnescapeCommandLine(subParam.Substring(0, equalPos)),
									UnescapeCommandLine(subParam.Substring(equalPos + 1))));
								break;
							}
						}
						while (equalPos != -1);
						lastPos = ++commaPos;
					}
					else
						++commaPos;

					//Find the next ,
					commaPos = param.IndexOf(',', commaPos);
				}

				return result;
			}

			/// <summary>
			/// Unescapes a subparameter command line, removing the extra 
			/// </summary>
			/// <param name="param"></param>
			/// <returns></returns>
			private static string UnescapeCommandLine(string param)
			{
				StringBuilder result = new StringBuilder(param.Length);
				for (int i = 0; i < param.Length; ++i)
					if (param[i] == '\\' && i < param.Length - 1)
						result.Append(param[++i]);
					else
						result.Append(param[i]);
				return result.ToString();
			}

			/// <summary>
			/// True if no console window should be created.
			/// </summary>
			public bool Quiet { get; private set; }
		}

		/// <summary>
		/// Manages a help query command line.
		/// </summary>
		class HelpCommandLine : CommandLine
		{
			public HelpCommandLine()
			{
			}
		}

		class QueryMethodsCommandLine : CommandLine
		{
			public QueryMethodsCommandLine()
			{
			}
		}

		/// <summary>
		/// Manages a command line for adding tasks to the global DirectExecutor
		/// </summary>
		class AddTaskCommandLine : CommandLine
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			public AddTaskCommandLine()
			{
				Schedule = Schedule.RunNow;
				Targets = new List<ErasureTarget>();
			}

			protected override bool ResolveParameter(string param)
			{
				int equalPos = param.IndexOf('=');
				if (IsParam(param, "method", "m"))
				{
					if (equalPos == -1)
						throw new ArgumentException("--method must be specified with an Erasure " +
							"method GUID.");

					List<KeyValuePair<string, string>> subParams =
						GetSubParameters(param.Substring(equalPos + 1));
					ErasureMethod = new Guid(subParams[0].Key);
				}
				else if (IsParam(param, "schedule", "s"))
				{
					if (equalPos == -1)
						throw new ArgumentException("--schedule must be specified with a Schedule " +
							"type.");

					List<KeyValuePair<string, string>> subParams =
						GetSubParameters(param.Substring(equalPos + 1));
					switch (subParams[0].Key)
					{
						case "now":
							Schedule = Schedule.RunNow;
							break;
						case "manually":
							Schedule = Schedule.RunManually;
							break;
						case "restart":
							Schedule = Schedule.RunOnRestart;
							break;
						default:
							throw new ArgumentException("Unknown schedule type: " + subParams[0].Key);
					}
				}
				else if (IsParam(param, "recycled", "r"))
				{
					Targets.Add(new RecycleBinTarget());
				}
				else if (IsParam(param, "unused", "u"))
				{
					if (equalPos == -1)
						throw new ArgumentException("--unused must be specified with the Volume " +
							"to erase.");

					//Create the UnusedSpace target for inclusion into the task.
					UnusedSpaceTarget target = new UnusedSpaceTarget();

					//Determine if cluster tips should be erased.
					target.EraseClusterTips = false;
					List<KeyValuePair<string, string>> subParams =
						GetSubParameters(param.Substring(equalPos + 1));
					foreach (KeyValuePair<string, string> kvp in subParams)
						if (kvp.Value == null && target.Drive == null)
							target.Drive = Path.GetFullPath(kvp.Key);
						else if (kvp.Key == "clusterTips")
							target.EraseClusterTips = true;
						else
							throw new ArgumentException("Unknown subparameter: " + kvp.Key);
					Targets.Add(target);
				}
				else if (IsParam(param, "dir", "d") || IsParam(param, "directory", null))
				{
					if (equalPos == -1)
						throw new ArgumentException("--directory must be specified with the " +
							"directory to erase.");

					//Create the base target
					FolderTarget target = new FolderTarget();

					//Parse the subparameters.
					List<KeyValuePair<string, string>> subParams =
						GetSubParameters(param.Substring(equalPos + 1));
					foreach (KeyValuePair<string, string> kvp in subParams)
						if (kvp.Value == null && target.Path == null)
							target.Path = Path.GetFullPath(kvp.Key);
						else if (kvp.Key == "excludeMask")
						{
							if (kvp.Value == null)
								throw new ArgumentException("The exclude mask must be specified " +
									"if the excludeMask subparameter is specified");
							target.ExcludeMask = kvp.Value;
						}
						else if (kvp.Key == "includeMask")
						{
							if (kvp.Value == null)
								throw new ArgumentException("The include mask must be specified " +
									"if the includeMask subparameter is specified");
							target.IncludeMask = kvp.Value;
						}
						else if (kvp.Key == "delete")
							target.DeleteIfEmpty = true;
						else
							throw new ArgumentException("Unknown subparameter: " + kvp.Key);

					//Add the target to the list of targets
					Targets.Add(target);
				}
				else if (IsParam(param, "file", "f"))
				{
					if (equalPos == -1)
						throw new ArgumentException("--file must be specified with the " +
							"file to erase.");

					//It's just a file!
					FileTarget target = new FileTarget();

					//Parse the subparameters.
					List<KeyValuePair<string, string>> subParams =
						GetSubParameters(param.Substring(equalPos + 1));
					foreach (KeyValuePair<string, string> kvp in subParams)
						if (kvp.Value == null && target.Path == null)
							target.Path = Path.GetFullPath(kvp.Key);
						else
							throw new ArgumentException("Unknown subparameter: " + kvp.Key);

					Targets.Add(target);
				}
				else
					return false;

				return true;
			}

			/// <summary>
			/// The erasure method which the user specified on the command line.
			/// </summary>
			public Guid ErasureMethod { get; private set; }

			/// <summary>
			/// The schedule for the current set of targets.
			/// </summary>
			public Schedule Schedule { get; private set; }

			/// <summary>
			/// The list of targets which was specified on the command line.
			/// </summary>
			public List<ErasureTarget> Targets { get; private set; }
		}

		/// <summary>
		/// Manages a command line for importing a task list into the global
		/// DirectExecutor.
		/// </summary>
		class ImportTaskListCommandLine : CommandLine
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			public ImportTaskListCommandLine()
			{
			}

			protected override bool ResolveParameter(string param)
			{
				if (!System.IO.File.Exists(param))
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
						"The file {0} does not exist.", param));
				
				files.Add(param);
				return true;
			}

			public ICollection<string> Files
			{
				get
				{
					return files.AsReadOnly();
				}
			}

			private List<string> files = new List<string>();
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cmdParams">The raw command line arguments passed to the program.</param>
		public CommandLineProgram(string[] cmdParams)
		{
			try
			{
				Arguments = CommandLine.Get(cmdParams);

				//If the user did not specify the quiet command line, then create the console.
				if (!Arguments.Quiet)
					CreateConsole();

				//Map actions to their handlers
				actionHandlers.Add(typeof(AddTaskCommandLine), AddTask);
				actionHandlers.Add(typeof(ImportTaskListCommandLine), ImportTaskList);
				actionHandlers.Add(typeof(QueryMethodsCommandLine), QueryMethods);
				actionHandlers.Add(typeof(HelpCommandLine), Help);
			}
			finally
			{
				if (Arguments == null || !Arguments.Quiet)
					CreateConsole();
			}
		}

		/// <summary>
		/// Runs the program, analogous to System.Windows.Forms.Application.Run.
		/// </summary>
		public void Run()
		{
			//Call the function handling the current command line.
			actionHandlers[Arguments.GetType()]();
		}

		/// <summary>
		/// Creates a console for our application, setting the input/output streams to the
		/// defaults.
		/// </summary>
		private static void CreateConsole()
		{
			if (KernelApi.AllocConsole())
			{
				Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
				Console.SetIn(new StreamReader(Console.OpenStandardInput()));
			}
		}

		/// <summary>
		/// Prints the command line help for Eraser.
		/// </summary>
		private static void CommandUsage()
		{
			Console.WriteLine(@"usage: Eraser <action> <arguments>
where action is
    help                    Show this help message.
    addtask                 Adds tasks to the current task list.
    querymethods            Lists all registered Erasure methods.

global parameters:
    --quiet, -q	            Do not create a Console window to display progress.

parameters for help:
    eraser help

    no parameters to set.

parameters for addtask:
    eraser addtask [--method=<methodGUID>] [--schedule=(now|manually|restart)] (--recycled " +
@"| --unused=<volume> | --dir=<directory> | --file=<file>)[...]

    --method, -m            The Erasure method to use.
    --schedule, -s          The schedule the task will follow. The value must
                            be one of:
            now             The task will be queued for immediate execution.
            manually        The task will be created but not queued for execution.
            restart         The task will be queued for execution when the
                            computer is next restarted.
                            This parameter defaults to now.
    --recycled, -r          Erases files and folders in the recycle bin
    --unused, -u            Erases unused space in the volume.
        optional arguments: --unused=<drive>[,clusterTips]
            clusterTips     If specified, the drive's files will have their
                            cluster tips erased.
    --dir, --directory, -d  Erases files and folders in the directory
        optional arguments: --dir=<directory>[,e=excludeMask][,i=includeMask][,delete]
            excludeMask     A wildcard expression for files and folders to
                            exclude.
            includeMask     A wildcard expression for files and folders to
                            include.
                            The include mask is applied before the exclude
                            mask.
            delete          Deletes the folder at the end of the erasure if
                            specified.
    --file, -f              Erases the specified file

parameters for querymethods:
    eraser querymethods

    no parameters to set.

All arguments are case sensitive.");
			Console.Out.Flush();
		}

		#region Action Handlers
		/// <summary>
		/// The command line arguments passed to the program.
		/// </summary>
		public CommandLine Arguments { get; private set; }

		/// <summary>
		/// Prints the help text for Eraser (with copyright)
		/// </summary>
		private void Help()
		{
			Console.WriteLine(@"Eraser {0}
(c) 2008 The Eraser Project
Eraser is Open-Source Software: see http://eraser.heidi.ie/ for details.
", Assembly.GetExecutingAssembly().GetName().Version);

			Console.Out.Flush();
			CommandUsage();
		}

		/// <summary>
		/// Lists all registered erasure methods.
		/// </summary>
		/// <param name="commandLine">The command line parameters passed to the program.</param>
		private void QueryMethods()
		{
			//Output the header
			const string methodFormat = "{0,-2} {1,-39} {2}";
			Console.WriteLine(methodFormat, "", "Method", "GUID");
			Console.WriteLine(new string('-', 79));

			//Refresh the list of erasure methods
			Dictionary<Guid, ErasureMethod> methods = ErasureMethodManager.Items;
			foreach (ErasureMethod method in methods.Values)
			{
				Console.WriteLine(methodFormat, (method is UnusedSpaceErasureMethod) ?
					"U" : "", method.Name, method.Guid.ToString());
			}
		}

		/// <summary>
		/// Parses the command line for tasks and adds them using the
		/// <see cref="RemoteExecutor"/> class.
		/// </summary>
		/// <param name="commandLine">The command line parameters passed to the program.</param>
		private void AddTask()
		{
			AddTaskCommandLine taskArgs = (AddTaskCommandLine)Arguments;
			
			//Create the task, and set the method to use.
			Task task = new Task();
			ErasureMethod method = taskArgs.ErasureMethod == Guid.Empty ? 
				ErasureMethodManager.Default :
				ErasureMethodManager.GetInstance(taskArgs.ErasureMethod);

			foreach (ErasureTarget target in taskArgs.Targets)
			{
				target.Method = method;
				task.Targets.Add(target);
			}

			//Check the number of tasks in the task.
			if (task.Targets.Count == 0)
				throw new ArgumentException("Tasks must contain at least one erasure target.");

			//Set the schedule for the task.
			task.Schedule = taskArgs.Schedule;

			//Send the task out.
			try
			{
				using (RemoteExecutorClient client = new RemoteExecutorClient())
				{
					client.Run();
					if (!client.IsConnected)
					{
						//The client cannot connect to the server. This probably means
						//that the server process isn't running. Start an instance.
						Process eraserInstance = Process.Start(
							Assembly.GetExecutingAssembly().Location, "--quiet");
						eraserInstance.WaitForInputIdle();

						client.Run();
						if (!client.IsConnected)
							throw new IOException("Eraser cannot connect to the running " +
								"instance for erasures.");
					}

					client.Tasks.Add(task);
				}
			}
			catch (UnauthorizedAccessException e)
			{
				//We can't connect to the pipe because the other instance of Eraser
				//is running with higher privileges than this instance.
				throw new UnauthorizedAccessException("Another instance of Eraser " +
					"is already running but it is running with higher privileges than " +
					"this instance of Eraser. Tasks cannot be added in this manner.\n\n" +
					"Close the running instance of Eraser and start it again without " +
					"administrator privileges, or run the command again as an " +
					"administrator.", e);
			}
		}

		/// <summary>
		/// Imports the given tasklists and adds them to the global Eraser instance.
		/// </summary>
		private void ImportTaskList()
		{
			ImportTaskListCommandLine cmdLine = (ImportTaskListCommandLine)Arguments;

			//Import the task list
			try
			{
				using (RemoteExecutorClient client = new RemoteExecutorClient())
				{
					client.Run();
					if (!client.IsConnected)
					{
						//The client cannot connect to the server. This probably means
						//that the server process isn't running. Start an instance.
						Process eraserInstance = Process.Start(
							Assembly.GetExecutingAssembly().Location, "--quiet");
						eraserInstance.WaitForInputIdle();

						client.Run();
						if (!client.IsConnected)
							throw new IOException("Eraser cannot connect to the running " +
								"instance for erasures.");
					}

					foreach (string path in cmdLine.Files)
						using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
							client.Tasks.LoadFromStream(stream);
				}
			}
			catch (UnauthorizedAccessException e)
			{
				//We can't connect to the pipe because the other instance of Eraser
				//is running with higher privileges than this instance.
				throw new UnauthorizedAccessException("Another instance of Eraser " +
					"is already running but it is running with higher privileges than " +
					"this instance of Eraser. Tasks cannot be added in this manner.\n\n" +
					"Close the running instance of Eraser and start it again without " +
					"administrator privileges, or run the command again as an " +
					"administrator.", e);
			}
		}
		#endregion

		/// <summary>
		/// The prototype of an action handler in the class which executes an
		/// action as specified in the command line.
		/// </summary>
		private delegate void ActionHandler();

		/// <summary>
		/// Matches an action handler to a function in the class.
		/// </summary>
		private Dictionary<Type, ActionHandler> actionHandlers =
			new Dictionary<Type, ActionHandler>();
	}
}
