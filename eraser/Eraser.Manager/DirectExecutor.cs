/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net> @17/10/2008
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
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.IO;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using System.Xml;
using System.Xml.Serialization;

namespace Eraser.Manager
{
	/// <summary>
	/// The DirectExecutor class is used by the Eraser GUI directly when the program
	/// is run without the help of a Service.
	/// </summary>
	public class DirectExecutor : Executor
	{
		public DirectExecutor()
		{
			TaskAdded += OnTaskAdded;
			TaskDeleted += OnTaskDeleted;
			tasks = new DirectExecutorTasksCollection(this);
			thread = new Thread(Main);
		}

		protected override void Dispose(bool disposing)
		{
			if (thread == null || schedulerInterrupt == null)
				return;

			if (disposing)
{
    thread.Abort();
    schedulerInterrupt.Set();
    //Wait for the executor thread to exit -- we call some event functions
    //and these events may need invocation on the main thread. So,
    //pump messages from the main thread until the thread exits.
    if (System.Windows.Forms.Application.MessageLoop)
    {
        if (!thread.Join(new TimeSpan(0, 0, 0, 0, 100)))
            System.Windows.Forms.Application.DoEvents();
    }
    //If we are disposing on a secondary thread, or a thread without
    //a message loop, just wait for the thread to exit indefinitely
    else
        thread.Join();
    schedulerInterrupt.Close();
    if (schedulerInterrupt != null)
    {
        schedulerInterrupt.Dispose();
        schedulerInterrupt = null;
    }
}

			thread = null;
			schedulerInterrupt = null;
			base.Dispose(disposing);
		}

		public override void Run()
		{
			thread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;
			thread.Start();
		}

		public override void QueueTask(Task task)
		{
			lock (tasksLock)
			{
				//Queue the task to be run immediately.
				DateTime executionTime = DateTime.Now;
				if (!scheduledTasks.ContainsKey(executionTime))
					scheduledTasks.Add(executionTime, new List<Task>());
				scheduledTasks[executionTime].Add(task);
				schedulerInterrupt.Set();
			}
		}

		public override void ScheduleTask(Task task)
		{
			RecurringSchedule schedule = task.Schedule as RecurringSchedule;
			if (schedule == null)
				return;

			DateTime executionTime = (schedule.MissedPreviousSchedule &&
				ManagerLibrary.Instance.Settings.ExecuteMissedTasksImmediately) ?
					DateTime.Now : schedule.NextRun;

			lock (tasksLock)
			{
				if (!scheduledTasks.ContainsKey(executionTime))
					scheduledTasks.Add(executionTime, new List<Task>());
				scheduledTasks[executionTime].Add(task);
			}
		}

		public override void QueueRestartTasks()
		{
			lock (tasksLock)
			{
				foreach (Task task in Tasks)
					if (task.Schedule == Schedule.RunOnRestart)
						QueueTask(task);
			}
		}

		public override void UnqueueTask(Task task)
		{
			lock (tasksLock)
				for (int i = 0; i != scheduledTasks.Count; ++i)
					for (int j = 0; j < scheduledTasks.Values[i].Count; )
					{
						Task currentTask = scheduledTasks.Values[i][j];
						if (currentTask == task &&
							(!(currentTask.Schedule is RecurringSchedule) ||
								((RecurringSchedule)currentTask.Schedule).NextRun != scheduledTasks.Keys[i]))
						{
							scheduledTasks.Values[i].RemoveAt(j);
						}
						else
						{
							++j;
						}
					}
		}

		internal override bool IsTaskQueued(Task task)
		{
			lock (tasksLock)
				foreach (KeyValuePair<DateTime, List<Task>> tasks in scheduledTasks)
					foreach (Task i in tasks.Value)
						if (task == i)
							if (task.Schedule is RecurringSchedule)
							{
								if (((RecurringSchedule)task.Schedule).NextRun != tasks.Key)
									return true;
							}
							else
								return true;

			return false;
		}

		private void OnTaskAdded(object sender, TaskEventArgs e)
		{
			e.Task.TaskEdited += OnTaskEdited;
		}

		private void OnTaskEdited(object sender, EventArgs e)
		{
			//Find all schedule entries containing the task - since the user cannot make
			//edits to the task when it is queued (only if it is scheduled) remove
			//all task references and add them back
			Task task = (Task)sender;
			lock (tasksLock)
				for (int i = 0; i != scheduledTasks.Count; ++i)
					for (int j = 0; j < scheduledTasks.Values[i].Count; )
					{
						Task currentTask = scheduledTasks.Values[i][j];
						if (currentTask == task)
							scheduledTasks.Values[i].RemoveAt(j);
						else
							j++;
					}

			//Then reschedule the task
			if (task.Schedule is RecurringSchedule)
				ScheduleTask(task);
		}

		private void OnTaskDeleted(object sender, TaskEventArgs e)
		{
			e.Task.TaskEdited -= OnTaskEdited;
		}

		public override ExecutorTasksCollection Tasks
		{
			get
			{
				return tasks;
			}
		}

		/// <summary>
		/// The thread entry point for this object. This object operates on a queue
		/// and hence the thread will sequentially execute tasks.
		/// </summary>
		private void Main()
		{
			//The waiting thread will utilize a polling loop to check for new
			//scheduled tasks. This will be checked every 30 seconds. However,
			//when the thread is waiting for a new task, it can be interrupted.
			while (thread.ThreadState != ThreadState.AbortRequested)
			{
				//Check for a new task
				Task task = null;
				lock (tasksLock)
				{
					while (scheduledTasks.Count != 0)
						if (scheduledTasks.Values[0].Count == 0)
						{
							//Clean all all time slots at the start of the queue which are
							//empty
							scheduledTasks.RemoveAt(0);
						}
						else
						{
							if (scheduledTasks.Keys[0] <= DateTime.Now)
							{
								List<Task> tasks = scheduledTasks.Values[0];
								task = tasks[0];
								tasks.RemoveAt(0);
							}

							//Do schedule queue maintenance: clean up all empty timeslots
							if (task == null)
							{
								for (int i = 0; i < scheduledTasks.Count; )
									if (scheduledTasks.Values[i].Count == 0)
										scheduledTasks.RemoveAt(i);
									else
										++i;
							}

							break;
						}
				}

				if (task != null)
				{
					LogSink sessionLog = new LogSink();
					task.Log.Add(sessionLog);

					//Start a new log session to separate this session's events
					//from previous ones.
					try
					{
						using (new LogSession(sessionLog))
						{
							//Set the currently executing task.
							currentTask = task;

							//Prevent the system from sleeping.
							Power.ExecutionState = ExecutionState.Continuous |
								ExecutionState.SystemRequired;

							task.Execute();
						}
					}
					finally
					{
						//Allow the system to sleep again.
						Power.ExecutionState = ExecutionState.Continuous;

						//If the task is a recurring task, reschedule it for the next execution
						//time since we are done with this one.
						if (task.Schedule is RecurringSchedule)
						{
							ScheduleTask(task);
						}

						//If the task is an execute on restart task or run immediately task, it is
						//only run once and can now be restored to a manually run task
						else if (task.Schedule == Schedule.RunOnRestart ||
							task.Schedule == Schedule.RunNow)
						{
							task.Schedule = Schedule.RunManually;
						}

						//Remove the actively executing task from our instance variable
						currentTask = null;
					}
				}

				//Wait for half a minute to check for the next scheduled task.
				schedulerInterrupt.WaitOne(30000, false);
			}
		}

		/// <summary>
		/// The thread object.
		/// </summary>
		private Thread thread;

		/// <summary>
		/// The lock preventing concurrent access for the tasks list and the
		/// tasks queue.
		/// </summary>
		private object tasksLock = new object();

		/// <summary>
		/// The queue of tasks. This queue is executed when the first element's
		/// timestamp (the key) has been past. This list assumes that all tasks
		/// are sorted by timestamp, smallest one first.
		/// </summary>
		private SortedList<DateTime, List<Task>> scheduledTasks =
			new SortedList<DateTime, List<Task>>();

		/// <summary>
		/// The task list associated with this executor instance.
		/// </summary>
		private DirectExecutorTasksCollection tasks;

		/// <summary>
		/// The currently executing task.
		/// </summary>
		Task currentTask;

		/// <summary>
		/// An automatically reset event allowing the addition of new tasks to
		/// interrupt the thread's sleeping state waiting for the next recurring
		/// task to be due.
		/// </summary>
		AutoResetEvent schedulerInterrupt = new AutoResetEvent(true);

		private class DirectExecutorTasksCollection : ExecutorTasksCollection
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="executor">The <see cref="DirectExecutor"/> object owning
			/// this list.</param>
			public DirectExecutorTasksCollection(DirectExecutor executor)
				: base(executor)
			{
			}
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="executor">The <seealso cref="Executor"/> object owning
            /// this task list.</param>
            protected DirectExecutorTasksCollection(Executor executor)
                : base(executor)
            {
                
            }

			#region IList<Task> Members
			public override int IndexOf(Task item)
			{
				return list.IndexOf(item);
			}

			public override void Insert(int index, Task item)
			{
				item.Executor = Owner;
				lock (list)
					list.Insert(index, item);

				//Call all the event handlers who registered to be notified of tasks
				//being added.
				Owner.OnTaskAdded(new TaskEventArgs(item));

				//If the task is scheduled to run now, break the waiting thread and
				//run it immediately
				if (item.Schedule == Schedule.RunNow)
				{
					Owner.QueueTask(item);
				}
				//If the task is scheduled, add the next execution time to the list
				//of schduled tasks.
				else if (item.Schedule != Schedule.RunOnRestart)
				{
					Owner.ScheduleTask(item);
				}
			}

			public override void RemoveAt(int index)
			{
				lock (list)
				{
					Task task = list[index];
					task.Cancel();
					task.Executor = null;
					list.RemoveAt(index);

					//Call all event handlers registered to be notified of task deletions.
					Owner.OnTaskDeleted(new TaskEventArgs(task));
				}
			}

			public override Task this[int index]
			{
				get
				{
					lock (list)
						return list[index];
				}
				set
				{
					lock (list)
						list[index] = value;
				}
			}
			#endregion

			#region ICollection<Task> Members
			public override void Add(Task item)
			{
				Insert(Count, item);
			}

			public override void Clear()
			{
				foreach (Task task in list)
					Remove(task);
			}

			public override bool Contains(Task item)
			{
				lock (list)
					return list.Contains(item);
			}

			public override void CopyTo(Task[] array, int arrayIndex)
			{
				lock (list)
					list.CopyTo(array, arrayIndex);
			}

			public override int Count
			{
				get
				{
					lock (list)
						return list.Count;
				}
			}

			public override bool Remove(Task item)
			{
				lock (list)
				{
					int index = list.IndexOf(item);
					if (index < 0)
						return false;

					RemoveAt(index);
				}

				return true;
			}
			#endregion

			#region IEnumerable<Task> Members
			public override IEnumerator<Task> GetEnumerator()
			{
				return list.GetEnumerator();
			}
			#endregion

			public override void SaveToStream(Stream stream)
			{
				lock (list)
				{
					XmlRootAttribute root = new XmlRootAttribute("TaskList");
					XmlSerializer serializer = new XmlSerializer(list.GetType(), root);
					serializer.Serialize(stream, list);
				}
			}

			public override void SaveToFile(string file)
			{
                XmlWriterSettings settings = new XmlWriterSettings() { Indent = true };
				lock (list)
				using (XmlWriter writer = XmlWriter.Create(file, settings))
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("TaskList");

					string logFolderPath = Path.Combine(Path.GetDirectoryName(file), "Logs");
					if (!Directory.Exists(logFolderPath))
						Directory.CreateDirectory(logFolderPath);

					foreach (Task task in list)
					{
						writer.WriteStartElement("Task");
						task.WriteSeparatedXml(writer, logFolderPath);
						writer.WriteEndElement();
					}

					writer.WriteEndElement();
					writer.WriteEndDocument();
				}
			}

			public override void LoadFromStream(Stream stream)
			{
				//Load the list into the dictionary
				XmlRootAttribute root = new XmlRootAttribute("TaskList");
				XmlSerializer serializer = new XmlSerializer(list.GetType(), root);

				try
				{
					List<Task> deserialised = (List<Task>)serializer.Deserialize(stream);
					list.AddRange(deserialised);

					foreach (Task task in deserialised)
					{
						task.Executor = Owner;
						Owner.OnTaskAdded(new TaskEventArgs(task));
						if (task.Schedule == Schedule.RunNow)
							Owner.QueueTask(task);
						else if (task.Schedule is RecurringSchedule)
							Owner.ScheduleTask(task);
					}
				}
				catch (InvalidOperationException e)
				{
					throw new InvalidDataException(e.Message, e);
				}
				catch (FileLoadException e)
				{
					throw new InvalidDataException(e.Message, e);
				}
			}

			/// <summary>
			/// The data store for this object.
			/// </summary>
			private List<Task> list = new List<Task>();
		}
	}
}