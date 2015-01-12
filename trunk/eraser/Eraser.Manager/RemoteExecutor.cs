/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
 * Original Author: Kasra Nassiri <cjax@users.sourceforge.net>
 * Modified By: Joel Low <lowjoel@users.sourceforge.net>
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
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Eraser.Manager
{
	/// <summary>
	/// Represents a request to the RemoteExecutorServer instance
	/// </summary>
	[Serializable]
	internal class RemoteExecutorRequest
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="func">The function this command is wanting to execute.</param>
		/// <param name="data">The parameters for the command, serialised using a
		/// BinaryFormatter</param>
		public RemoteExecutorRequest(RemoteExecutorFunction func, params object[] data)
		{
			Func = func;
			Data = data;
		}

		/// <summary>
		/// The function that this request is meant to call.
		/// </summary>
		public RemoteExecutorFunction Func { get; set; }

		/// <summary>
		/// The parameters associated with the function call.
		/// </summary>
		public object[] Data { get; private set; }
	};

	/// <summary>
	/// List of supported functions
	/// </summary>
	public enum RemoteExecutorFunction
	{
		QueueTask,
		ScheduleTask,
		UnqueueTask,

		AddTask,
		DeleteTask,
		//UpdateTask,
		GetTaskCount,
		GetTask
	}

	/// <summary>
	/// The RemoteExecutorServer class is the server half required for remote execution
	/// of tasks.
	/// </summary>
	public class RemoteExecutorServer : DirectExecutor
	{
		/// <summary>
		/// Our Remote Server name, prevent collisions!
		/// </summary>
		public static readonly string ServerName =
			"Eraser-FB6C5A7D-E47F-475f-ABA4-58F4D24BB67E-RemoteExecutor-" +
			WindowsIdentity.GetCurrent().User.ToString();

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteExecutorServer()
		{
			thread = new Thread(Main);
			serverLock = new Semaphore(maxServerInstances, maxServerInstances);
		}

		protected override void Dispose(bool disposing)
		{
			if (thread == null || serverLock == null)
				return;

			if (disposing)
			{
				//Close the polling thread that creates new server instances
				thread.Abort();
				thread.Join();

				//Close all waiting streams
				lock (servers)
					foreach (NamedPipeServerStream server in servers)
						server.Close();

				//Acquire all available locks to ensure no more server instances exist,
				//then destroy the semaphore
				for (int i = 0; i < maxServerInstances; ++i)
					serverLock.WaitOne();
				serverLock.Close();
			}

			thread = null;
			serverLock = null;
			base.Dispose(disposing);
		}

		public override void Run()
		{
			thread.Start();
			Thread.Sleep(0);
			base.Run();
		}

		/// <summary>
		/// The polling loop dealing with every server connection.
		/// </summary>
		private void Main()
		{
			while (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
			{
				//Wait for a new server instance to be available.
				if (!serverLock.WaitOne())
					continue;

				PipeSecurity security = new PipeSecurity();
				security.AddAccessRule(new PipeAccessRule(
					WindowsIdentity.GetCurrent().User,
					PipeAccessRights.FullControl, AccessControlType.Allow));

				//Otherwise, a new instance can be created. Create it and wait for connections.
				NamedPipeServerStream server = new NamedPipeServerStream(ServerName,
					PipeDirection.InOut, maxServerInstances, PipeTransmissionMode.Message,
					PipeOptions.Asynchronous, 128, 128, security);
				server.BeginWaitForConnection(EndWaitForConnection, server);
				
				lock (servers)
					servers.Add(server);
			}
		}

		/// <summary>
		/// Handles the arguments passed to the server and calls the real function.
		/// </summary>
		/// <param name="arguments">The arguments to the function.</param>
		/// <returns>Te result of the function call.</returns>
		private delegate object RequestHandler(object[] arguments);

		/// <summary>
		/// Waits for a connection from a client.
		/// </summary>
		/// <param name="result">The AsyncResult object associated with this asynchronous
		/// operation.</param>
		private void EndWaitForConnection(IAsyncResult result)
		{
			NamedPipeServerStream server = (NamedPipeServerStream)result.AsyncState;

			try
			{
				//We're done waiting for the connection
				server.EndWaitForConnection(result);

				while (server.IsConnected)
				{
					//Read the request into the buffer.
					RemoteExecutorRequest request = null;
					using (MemoryStream mstream = new MemoryStream())
					{
						byte[] buffer = new byte[65536];

						do
						{
							int lastRead = server.Read(buffer, 0, buffer.Length);
							mstream.Write(buffer, 0, lastRead);
						}
						while (!server.IsMessageComplete);

						//Ignore the request if the client disconnected from us.
						if (!server.IsConnected)
							return;

						//Deserialise the header of the request.
						mstream.Position = 0;
						try
						{
							request = (RemoteExecutorRequest)new BinaryFormatter().Deserialize(mstream);
						}
						catch (SerializationException)
						{
							//We got a unserialisation issue but we can't do anything about it.
							return;
						}
					}

					//Map the deserialisation function to a real function
					Dictionary<RemoteExecutorFunction, RequestHandler> functionMap =
						new Dictionary<RemoteExecutorFunction, RequestHandler>();
					functionMap.Add(RemoteExecutorFunction.QueueTask,
						delegate(object[] args) { QueueTask((Task)args[0]); return null; });
					functionMap.Add(RemoteExecutorFunction.ScheduleTask,
						delegate(object[] args) { ScheduleTask((Task)args[0]); return null; });
					functionMap.Add(RemoteExecutorFunction.UnqueueTask,
						delegate(object[] args) { UnqueueTask((Task)args[0]); return null; });

					functionMap.Add(RemoteExecutorFunction.AddTask,
						delegate(object[] args)
						{
							Tasks.Add((Task)args[0]);
							return null;
						});
					functionMap.Add(RemoteExecutorFunction.DeleteTask,
						delegate(object[] args)
						{
							Tasks.Remove((Task)args[0]);
							return null;
						});
					functionMap.Add(RemoteExecutorFunction.GetTaskCount,
						delegate(object[] args) { return Tasks.Count; });
					functionMap.Add(RemoteExecutorFunction.GetTask,
						delegate(object[] args) { return Tasks[(int)args[0]]; });

					//Execute the function
					object returnValue = functionMap[request.Func](request.Data);

					//Return the result of the invoked function.
					using (MemoryStream mStream = new MemoryStream())
					{
						if (returnValue != null)
						{
							byte[] header = BitConverter.GetBytes((Int32)1);
							byte[] buffer = null;
							new BinaryFormatter().Serialize(mStream, returnValue);

							server.Write(header, 0, header.Length);
							server.Write(buffer, 0, buffer.Length);
						}
						else
						{
							byte[] header = BitConverter.GetBytes((Int32)0);
							server.Write(header, 0, header.Length);
						}
					}

					server.WaitForPipeDrain();
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				lock (servers)
					servers.Remove(server);
				server.Close();
				serverLock.Release();
			}
		}

		/// <summary>
		/// The thread which will answer pipe connections
		/// </summary>
		private Thread thread;

		/// <summary>
		/// Counts the number of available server instances.
		/// </summary>
		private Semaphore serverLock;

		/// <summary>
		/// The list storing all available created server instances.
		/// </summary>
		private List<NamedPipeServerStream> servers = new List<NamedPipeServerStream>();

		/// <summary>
		/// The maximum number of server instances existing concurrently.
		/// </summary>
		private const int maxServerInstances = 4;
	}

	/// <summary>
	/// The RemoteExecutorServer class is the client half required for remote execution
	/// of tasks, sending requests to the server running on the local computer.
	/// </summary>
	public class RemoteExecutorClient : Executor
	{
		public RemoteExecutorClient()
		{
			client = new NamedPipeClientStream(".", RemoteExecutorServer.ServerName,
				PipeDirection.InOut);
			tasks = new RemoteExecutorClientTasksCollection(this);
		}

		protected override void Dispose(bool disposing)
		{
			if (client == null)
				return;

			if (disposing)
			{
				client.Close();
			}

			client = null;
			base.Dispose(disposing);
		}

		public override void Run()
		{
			try
			{
				client.Connect(0);
			}
			catch (TimeoutException)
			{
			}
		}

		/// <summary>
		/// Sends a request to the executor server.
		/// </summary>
		/// <typeparam name="ReturnType">The expected return type of the request.</typeparam>
		/// <param name="function">The requested operation.</param>
		/// <param name="args">The arguments for the operation.</param>
		/// <returns>The return result from the object as if it were executed locally.</returns>
		internal ReturnType SendRequest<ReturnType>(RemoteExecutorFunction function, params object[] args)
		{
			//Connect to the server
			object result = null;

			using (MemoryStream mStream = new MemoryStream())
			{
				//Serialise the request
				new BinaryFormatter().Serialize(mStream, new RemoteExecutorRequest(function, args));

				//Write the request to the pipe
				byte[] buffer = mStream.ToArray();
				client.Write(buffer, 0, buffer.Length);

				//Read the response from the pipe
				mStream.Position = 0;
				buffer = new byte[65536];
				client.ReadMode = PipeTransmissionMode.Message;
				do
				{
					int lastRead = client.Read(buffer, 0, buffer.Length);
					mStream.Write(buffer, 0, lastRead);
				}
				while (!client.IsMessageComplete);

				//Check if the server says there is a response. If so, read it.
				if (BitConverter.ToInt32(mStream.ToArray(), 0) == 1)
				{
					mStream.Position = 0;
					do
					{
						int lastRead = client.Read(buffer, 0, buffer.Length);
						mStream.Write(buffer, 0, lastRead);
					}
					while (!client.IsMessageComplete);

					//Deserialise the response
					mStream.Position = 0;
					if (mStream.Length > 0)
						result = new BinaryFormatter().Deserialize(mStream);
				}
			}

			return (ReturnType)result;
		}

		public override void QueueTask(Task task)
		{
			SendRequest<object>(RemoteExecutorFunction.QueueTask, task);
		}

		public override void ScheduleTask(Task task)
		{
			SendRequest<object>(RemoteExecutorFunction.ScheduleTask, task);
		}

		public override void UnqueueTask(Task task)
		{
			SendRequest<object>(RemoteExecutorFunction.UnqueueTask, task);
		}

		public override void QueueRestartTasks()
		{
			throw new NotImplementedException();
		}

		internal override bool IsTaskQueued(Task task)
		{
			throw new NotImplementedException();
		}

		public override ExecutorTasksCollection Tasks
		{
			get
			{
				return tasks;
			}
		}

		/// <summary>
		/// Checks whether the executor instance has connected to a server.
		/// </summary>
		public bool IsConnected 
		{
			get { return client.IsConnected; }
		}

		/// <summary>
		/// The list of tasks belonging to this executor instance.
		/// </summary>
		private RemoteExecutorClientTasksCollection tasks;

		/// <summary>
		/// The named pipe used to connect to another running instance of Eraser.
		/// </summary>
		private NamedPipeClientStream client;

		private class RemoteExecutorClientTasksCollection : ExecutorTasksCollection
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="executor">The <see cref="RemoteExecutor"/> object owning
			/// this list.</param>
			public RemoteExecutorClientTasksCollection(RemoteExecutorClient executor)
				: base(executor)
			{
			}

			/// <summary>
			/// Sends a request to the executor server.
			/// </summary>
			/// <typeparam name="ReturnType">The expected return type of the request.</typeparam>
			/// <param name="function">The requested operation.</param>
			/// <param name="args">The arguments for the operation.</param>
			/// <returns>The return result from the object as if it were executed locally.</returns>
			private ReturnType SendRequest<ReturnType>(RemoteExecutorFunction function, params object[] args)
			{
				RemoteExecutorClient client = (RemoteExecutorClient)Owner;
				return client.SendRequest<ReturnType>(function, args);
			}

			#region IList<Task> Members
			public override int IndexOf(Task item)
			{
				throw new NotSupportedException();
			}

			public override void Insert(int index, Task item)
			{
				throw new NotSupportedException();
			}

			public override void RemoveAt(int index)
			{
				throw new NotSupportedException();
			}

			public override Task this[int index]
			{
				get
				{
					return SendRequest<Task>(RemoteExecutorFunction.GetTask, index);
				}
				set
				{
					throw new NotSupportedException();
				}
			}
			#endregion

			#region ICollection<Task> Members
			public override void Add(Task item)
			{
				item.Executor = Owner;
				SendRequest<object>(RemoteExecutorFunction.AddTask, item);

				//Call all the event handlers who registered to be notified of tasks
				//being added.
				Owner.OnTaskAdded(new TaskEventArgs(item));
			}

			public override void Clear()
			{
				throw new NotSupportedException();
			}

			public override bool Contains(Task item)
			{
				throw new NotSupportedException();
			}

			public override void CopyTo(Task[] array, int arrayIndex)
			{
				throw new NotSupportedException();
			}

			public override int Count
			{
				get { return SendRequest<int>(RemoteExecutorFunction.GetTaskCount); }
			}

			public override bool Remove(Task item)
			{
				item.Cancel();
				item.Executor = null;
				SendRequest<object>(RemoteExecutorFunction.DeleteTask, item);

				//Call all event handlers registered to be notified of task deletions.
				Owner.OnTaskDeleted(new TaskEventArgs(item));
				return true;
			}
			#endregion

			#region IEnumerable<Task> Members
			public override IEnumerator<Task> GetEnumerator()
			{
				throw new NotSupportedException();
			}
			#endregion

			public override void SaveToStream(Stream stream)
			{
				throw new NotSupportedException();
			}

			public override void LoadFromStream(Stream stream)
			{
				throw new NotSupportedException();
			}
		}
	}
}