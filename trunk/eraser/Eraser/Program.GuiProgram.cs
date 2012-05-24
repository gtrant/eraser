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
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Security.Principal;
using System.Security.AccessControl;

using Eraser.Util;

namespace Eraser
{
	internal static partial class Program
	{
		/// <summary>
		/// Manages a global single instance of a Windows Form application.
		/// </summary>
		class GuiProgram : IDisposable
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="commandLine">The command line arguments associated with
			/// this program launch</param>
			/// <param name="instanceID">The instance ID of the program, used to group
			/// instances of the program together.</param>
			public GuiProgram(string[] commandLine, string instanceID)
			{
				InstanceID = instanceID;
				CommandLine = commandLine;

				//Check if there already is another instance of the program.
				try
				{
					bool isFirstInstance = false;
					GlobalMutex = new Mutex(true, instanceID, out isFirstInstance);
					IsFirstInstance = isFirstInstance;
				}
				catch (UnauthorizedAccessException)
				{
					//If we get here, the mutex exists but we cannot modify it. That
					//would imply that this is not the first instance.
					//See http://msdn.microsoft.com/en-us/library/bwe34f1k.aspx
					IsFirstInstance = false;
				}
			}

			#region IDisposable Interface
			~GuiProgram()
			{
				Dispose(false);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (GlobalMutex == null)
					return;

				if (disposing)
					GlobalMutex.Close();
				GlobalMutex = null;
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			#endregion

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
			public void Run()
			{
				//If no other instances are running, set up our pipe server so clients
				//can connect and give us subsequent command lines.
				if (IsFirstInstance)
				{
					//Initialise and run the program.
					InitInstanceEventArgs eventArgs = new InitInstanceEventArgs();
					OnInitInstance(this, eventArgs);
					if (MainForm == null)
						return;

					try
					{
						//Create the pipe server which will handle connections to us
						PipeServer = new Thread(ServerMain);
						PipeServer.Start();

						//Handle the exit instance event. This will occur when the main form
						//has been closed.
						Application.ApplicationExit += OnExitInstance;
						MainForm.FormClosed += OnExitInstance;

						if (eventArgs.ShowMainForm)
							Application.Run(MainForm);
						else
							Application.Run();
					}
					finally
					{
						if (PipeServer != null)
							PipeServer.Abort();
					}
				}

				//Another instance of the program is running. Connect to it and transfer
				//the command line arguments
				else
				{
					try
					{
						NamedPipeClientStream client = new NamedPipeClientStream(".", InstanceID,
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
						MessageBox.Show(S._("Another instance of Eraser is already running but it " +
							"is running with higher privileges than this instance of Eraser.\n\n" +
							"Eraser will now exit."), S._("Eraser"), MessageBoxButtons.OK,
							MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
							Localisation.IsRightToLeft(null) ?
								MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
					}
					catch (IOException ex)
					{
						MessageBox.Show(S._("Another instance of Eraser is already running but " +
							"cannot be connected to.\n\nThe error returned was: {0}", ex.Message),
							S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Error,
							MessageBoxDefaultButton.Button1,
							Localisation.IsRightToLeft(null) ?
								MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
					}
					catch (TimeoutException)
					{
						//Can't do much: half a second is a reasonably long time to wait.
					}
				}
			}

			#region Next instance processing
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
				while (PipeServer.ThreadState != System.Threading.ThreadState.AbortRequested)
				{
					PipeSecurity security = new PipeSecurity();
					security.AddAccessRule(new PipeAccessRule(
						WindowsIdentity.GetCurrent().User,
						PipeAccessRights.FullControl,
						AccessControlType.Allow));

					using (NamedPipeServerStream server = new NamedPipeServerStream(InstanceID,
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
						string[] commandLine = null;
						string message = string.Empty;

						do
						{
							int lastRead = async.Server.Read(buffer, 0, buffer.Length);
							message += Encoding.UTF8.GetString(buffer, 0, lastRead);
						}
						while (!async.Server.IsMessageComplete);

						//Let the event handler process the message.
						OnNextInstance(this, new NextInstanceEventArgs(commandLine, message));
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
			#endregion

			/// <summary>
			/// Gets the command line arguments this instance was started with.
			/// </summary>
			public string[] CommandLine { get; private set; }

			/// <summary>
			/// Gets whether another instance of the program is already running.
			/// </summary>
			public bool IsFirstInstance { get; private set; }

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
			/// <param name="e">Event arguments.</param>
			/// <returns>True if the MainForm property holds a valid reference to
			/// a form, and the form should be displayed to the user.</returns>
			public delegate void InitInstanceEventHandler(object sender, InitInstanceEventArgs e);

			/// <summary>
			/// The event object managing listeners to the instance initialisation event.
			/// This event is raised when the first instance of the program is started
			/// and this is where the program initialisation code should be. When this
			/// event is raised, the program should set the <see cref="GUIProgram.MainForm"/>
			/// property to the program's main form. If the property remains null at the
			/// end of the event, the program instance will quit. If the
			/// <see cref="GUIProgram.MainForm.Visible"/> property is set to false,
			/// the main form will not be shown to the user by default.
			/// </summary>
			public event InitInstanceEventHandler InitInstance;

			/// <summary>
			/// Broadcasts the InitInstance event.
			/// </summary>
			/// <param name="sender">The sender of the event.</param>
			/// <param name="e">Event arguments.</param>
			/// <returns>True if the MainForm object should be shown.</returns>
			private void OnInitInstance(object sender, InitInstanceEventArgs e)
			{
				if (InitInstance != null)
					InitInstance(sender, e);
			}

			/// <summary>
			/// The prototype of event handlers procesing the NextInstance event.
			/// </summary>
			/// <param name="sender">The sender of the event</param>
			/// <param name="e">Event arguments.</param>
			public delegate void NextInstanceEventHandler(object sender, NextInstanceEventArgs e);

			/// <summary>
			/// The event object managing listeners to the next instance event. This
			/// event is raised when a second instance of the program is started.
			/// </summary>
			public event NextInstanceEventHandler NextInstance;

			/// <summary>
			/// Broadcasts the NextInstance event.
			/// </summary>
			/// <param name="sender">The sender of the event.</param>
			/// <param name="e">Event arguments.</param>
			private void OnNextInstance(object sender, NextInstanceEventArgs e)
			{
				if (NextInstance != null)
					NextInstance(sender, e);
			}

			/// <summary>
			/// The prototype of event handlers procesing the ExitInstance event.
			/// </summary>
			/// <param name="sender">The sender of the event.</param>
			/// <param name="e">Event arguments.</param>
			public delegate void ExitInstanceEventHandler(object sender, EventArgs e);

			/// <summary>
			/// The event object managing listeners to the exit instance event. This
			/// event is raised when the first instance of the program is exited.
			/// </summary>
			public event ExitInstanceEventHandler ExitInstance;

			/// <summary>
			/// Broadcasts the ExitInstance event after getting the notification that the
			/// application is exiting. This event is broadcast only if the program
			/// completed initialisation.
			/// </summary>
			/// <seealso cref="InitInstance"/>
			/// <param name="sender">The sender of the event.</param>
			/// <param name="e">Event arguments.</param>
			private void OnExitInstance(object sender, EventArgs e)
			{
				//If the exit event has been broadcast don't repeat.
				if (Exited)
					return;

				Exited = true;
				if (ExitInstance != null)
					ExitInstance(sender, e);

				if (!MainForm.Disposing)
					MainForm.Dispose();
			}
			#endregion

			#region Instance variables
			/// <summary>
			/// The Instance ID of this program, used to group program instances together.
			/// </summary>
			private string InstanceID;

			/// <summary>
			/// The named mutex ensuring that only one instance of the application runs
			/// at a time.
			/// </summary>
			private Mutex GlobalMutex;

			/// <summary>
			/// The thread maintaining the pipe server for secondary instances to connect to.
			/// </summary>
			private Thread PipeServer;

			/// Tracks whether the Exit event has been broadcast. It should only be broadcast
			/// once in the lifetime of the application.
			private bool Exited;
			#endregion
		}

		/// <summary>
		/// Holds event data for the <see cref="GUIProgram.InitInstance"/> event.
		/// </summary>
		class InitInstanceEventArgs : EventArgs
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			public InitInstanceEventArgs()
			{
				ShowMainForm = true;
			}

			/// <summary>
			/// Gets or sets whether the main form should be shown when the program
			/// is initialised.
			/// </summary>
			public bool ShowMainForm { get; set; }
		}

		/// <summary>
		/// Holds event data for the <see cref="GUIProgram.NextInstance"/> event.
		/// </summary>
		class NextInstanceEventArgs : EventArgs
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="commandLine">The command line that the next instance was
			/// started with.</param>
			/// <param name="message">The message that the next instance wanted
			/// displayed.</param>
			public NextInstanceEventArgs(string[] commandLine, string message)
			{
				CommandLine = commandLine;
				Message = message;
			}

			/// <summary>
			/// The command line that the next instance was executed with.
			/// </summary>
			public string[] CommandLine { get; private set; }

			/// <summary>
			/// The message that the next instance wanted to display, but since a first
			/// instance already started, it was suppressed.
			/// </summary>
			public string Message { get; private set; }
		}
	}
}
