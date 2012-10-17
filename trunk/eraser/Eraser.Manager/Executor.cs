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
using System.IO; 
 
namespace Eraser.Manager
{
	/// <summary>
	/// Executor base class. This class will manage the tasks currently scheduled
	/// to be run and will run them when they are set to be run. This class is
	/// abstract as they each will have their own ways of dealing with tasks.
	/// </summary>
	public abstract class Executor : IDisposable
	{
		#region IDisposable members
		~Executor()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		/// <summary>
		/// Starts the execution of tasks queued.
		/// </summary>
		public abstract void Run();

		/// <summary>
		/// Queues the task for execution.
		/// </summary>
		/// <param name="task">The task to queue.</param>
		public abstract void QueueTask(Task task);

		/// <summary>
		/// Schedules the given task for execution.
		/// </summary>
		/// <param name="task">The task to schedule</param>
		public abstract void ScheduleTask(Task task);

		/// <summary>
		/// Removes the given task from the execution queue.
		/// </summary>
		/// <remarks>If the task given runs a recurring schedule, the task will only
		/// remove requested tasks and not the scheduled ones</remarks>
		/// <param name="task">The task to cancel.</param>
		public abstract void UnqueueTask(Task task);

		/// <summary>
		/// Gets whether a task is currently queued for execution, outside of the
		/// scheduled time.
		/// </summary>
		/// <param name="task">The task to query.</param>
		/// <returns>True if the task is currently queued, false otherwise.</returns>
		internal abstract bool IsTaskQueued(Task task);

		/// <summary>
		/// Queues all tasks in the task list which are meant for restart execution.
		/// This is a separate function rather than just running them by default on
		/// task load because creating a new instance and loading the task list
		/// may just be a program restart and may not necessarily be a system
		/// restart. Therefore this fuction has to be explicitly called by clients.
		/// </summary>
		public abstract void QueueRestartTasks();

		/// <summary>
		/// Retrieves the current task list for the executor.
		/// </summary>
		/// <returns>A list of tasks which the executor has registered.</returns>
		public abstract ExecutorTasksCollection Tasks { get; }

		/// <summary>
		/// The task added event object.
		/// </summary>
		public EventHandler<TaskEventArgs> TaskAdded { get; set; }

		/// <summary>
		/// Helper function for the task added event.
		/// </summary>
		internal void OnTaskAdded(TaskEventArgs e)
		{
			if (TaskAdded != null)
				TaskAdded(this, e);
		}

		/// <summary>
		/// The task added event object.
		/// </summary>
		public EventHandler<TaskEventArgs> TaskDeleted { get; set; }

		/// <summary>
		/// Helper function for the task deleted event.
		/// </summary>
		internal void OnTaskDeleted(TaskEventArgs e)
		{
			if (TaskDeleted != null)
				TaskDeleted(this, e);
		}
	}

	public abstract class ExecutorTasksCollection : IList<Task>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="executor">The <seealso cref="Executor"/> object owning
		/// this task list.</param>
		protected ExecutorTasksCollection(Executor executor)
		{
			Owner = executor;
		}

		#region IList<Task> Members
		public abstract int IndexOf(Task item);
		public abstract void Insert(int index, Task item);
		public abstract void RemoveAt(int index);
		public abstract Task this[int index] { get; set; }
		#endregion

		#region ICollection<Task> Members
		public abstract void Add(Task item);
		public abstract void Clear();
		public abstract bool Contains(Task item);
		public abstract void CopyTo(Task[] array, int arrayIndex);
		public abstract int Count { get; }
		public bool IsReadOnly { get { return false; } }
		public abstract bool Remove(Task item);
		#endregion

		#region IEnumerable<Task> Members
		public abstract IEnumerator<Task> GetEnumerator();
		#endregion

		#region IEnumerable Members
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion

		/// <summary>
		/// Saves a task list to the given stream.
		/// </summary>
		/// <param name="stream">The stream to save to.</param>
		public abstract void SaveToStream(Stream stream);

		/// <summary>
		/// Saves the task list to file.
		/// </summary>
		/// <param name="file">The path to the task list.</param>
		public virtual void SaveToFile(string file)
		{
			using (FileStream stream = new FileStream(file, FileMode.Create,
				FileAccess.Write, FileShare.None))
			{
				SaveToStream(stream);
			}
		}

		/// <summary>
		/// Loads the task list from the given stream.
		/// </summary>
		/// <remarks>This will append the tasks in the given stream to the current list of
		/// tasks instead of overwriting it.</remarks>
		/// <param name="stream">The stream to save to.</param>
		/// <exception cref="InvalidDataException">Thrown when the data in the stream is
		/// invalid or unrecognised.</exception>
		public abstract void LoadFromStream(Stream stream);

		/// <summary>
		/// The owner of this task list.
		/// </summary>
		protected Executor Owner { get; private set; }
	}
}