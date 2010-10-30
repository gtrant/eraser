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
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser.Manager
{
	/// <summary>
	/// Deals with an erase task.
	/// </summary>
	[Serializable]
	public class Task : ISerializable
	{
		#region Serialization code
		protected Task(SerializationInfo info, StreamingContext context)
		{
			Name = (string)info.GetValue("Name", typeof(string));
			Executor = context.Context as Executor;
			Targets = (ErasureTargetsCollection)info.GetValue("Targets", typeof(ErasureTargetsCollection));
			Targets.Owner = this;
			Log = (List<LogSink>)info.GetValue("Log", typeof(List<LogSink>));
			Canceled = false;

			Schedule schedule = (Schedule)info.GetValue("Schedule", typeof(Schedule));
			if (schedule.GetType() == Schedule.RunManually.GetType())
				Schedule = Schedule.RunManually;
			else if (schedule.GetType() == Schedule.RunNow.GetType())
				Schedule = Schedule.RunNow;
			else if (schedule.GetType() == Schedule.RunOnRestart.GetType())
				Schedule = Schedule.RunOnRestart;
			else if (schedule is RecurringSchedule)
				Schedule = schedule;
			else
				throw new InvalidDataException(S._("An invalid type was found when loading " +
					"the task schedule"));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("Schedule", Schedule);
			info.AddValue("Targets", Targets);
			info.AddValue("Log", Log);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public Task()
		{
			Name = string.Empty;
			Targets = new ErasureTargetsCollection(this);
			Schedule = Schedule.RunNow;
			Canceled = false;
			Log = new List<LogSink>();
		}

		/// <summary>
		/// Cancels the task from running, or, if the task is queued for running,
		/// removes the task from the queue.
		/// </summary>
		public void Cancel()
		{
			Executor.UnqueueTask(this);
			Canceled = true;
		}

		/// <summary>
		/// The Executor object which is managing this task.
		/// </summary>
		public Executor Executor { get; internal set; }

		/// <summary>
		/// The name for this task. This is just an opaque value for the user to
		/// recognize the task.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The name of the task, used for display in UI elements.
		/// </summary>
		public string UIText
		{
			get
			{
				//Simple case, the task name was given by the user.
				if (!string.IsNullOrEmpty(Name))
					return Name;

				string result = string.Empty;
				if (Targets.Count == 0)
					return result;
				else if (Targets.Count < 5)
				{
					//Simpler case, small set of data.
					foreach (ErasureTarget tgt in Targets)
						result += S._("{0}, ", tgt.UIText);

					return result.Remove(result.Length - 2);
				}
				else
				{
					//Ok, we've quite a few entries, get the first, the mid and the end.
					result = S._("{0}, ", Targets[0].UIText);
					result += S._("{0}, ", Targets[Targets.Count / 2].UIText);
					result += Targets[Targets.Count - 1].UIText;

					return S._("{0} and {1} other targets", result, Targets.Count - 3);
				}
			}
		}

		/// <summary>
		/// Gets the status of the task - whether it is being executed.
		/// </summary>
		public bool Executing { get; private set; }

		/// <summary>
		/// Gets whether this task is currently queued to run. This is true only
		/// if the queue it is in is an explicit request, i.e will run when the
		/// executor is idle.
		/// </summary>
		public bool Queued
		{
			get
			{
				return Executor.IsTaskQueued(this);
			}
		}

		/// <summary>
		/// Gets whether the task has been cancelled from execution.
		/// </summary>
		public bool Canceled
		{
			get;
			internal set;
		}

		/// <summary>
		/// The set of data to erase when this task is executed.
		/// </summary>
		public ErasureTargetsCollection Targets { get; private set; }

		/// <summary>
		/// The schedule for running the task.
		/// </summary>
		public Schedule Schedule
		{
			get
			{
				return schedule;
			}
			set
			{
				if (value.Owner != null)
					throw new ArgumentException("The schedule provided can only " +
						"belong to one task at a time");

				if (schedule is RecurringSchedule)
					((RecurringSchedule)schedule).Owner = null;
				schedule = value;
				if (schedule is RecurringSchedule)
					((RecurringSchedule)schedule).Owner = this;
				OnTaskEdited();
			}
		}

		/// <summary>
		/// The log entries which this task has accumulated.
		/// </summary>
		public List<LogSink> Log { get; private set; }

		/// <summary>
		/// The progress manager object which manages the progress of this task.
		/// </summary>
		public SteppedProgressManager Progress
		{
			get
			{
				if (!Executing)
					throw new InvalidOperationException("The progress of an erasure can only " +
						"be queried when the task is being executed.");

				return progress;
			}
			private set
			{
				progress = value;
			}
		}

		private Schedule schedule;
		private SteppedProgressManager progress;

		#region Events
		/// <summary>
		/// The task has been edited.
		/// </summary>
		public EventHandler TaskEdited { get; set; }

		/// <summary>
		/// The start of the execution of a task.
		/// </summary>
		public EventHandler TaskStarted { get; set; }

		/// <summary>
		/// The event object holding all event handlers.
		/// </summary>
		public EventHandler<ProgressChangedEventArgs> ProgressChanged { get; set; }

		/// <summary>
		/// The completion of the execution of a task.
		/// </summary>
		public EventHandler TaskFinished { get; set; }

		/// <summary>
		/// Broadcasts the task edited event.
		/// </summary>
		internal void OnTaskEdited()
		{
			if (TaskEdited != null)
				TaskEdited(this, EventArgs.Empty);
		}

		/// <summary>
		/// Broadcasts the task execution start event.
		/// </summary>
		internal void OnTaskStarted()
		{
			if (TaskStarted != null)
				TaskStarted(this, EventArgs.Empty);
			Executing = true;
			Progress = new SteppedProgressManager();
		}

		/// <summary>
		/// Broadcasts a ProgressChanged event. The sender will be the erasure target
		/// which broadcast this event; e.UserState will contain extra information
		/// about the progress which is stored as a TaskProgressChangedEventArgs
		/// object.
		/// </summary>
		/// <param name="sender">The <see cref="ErasureTarget"/> which is reporting
		/// progress.</param>
		/// <param name="e">The new progress value.</param>
		/// <exception cref="ArgumentException">e.UserState must be of the type
		/// <see cref="TaskProgressEventargs"/></exception>
		/// <exception cref="ArgumentNullException">Both sender and e cannot be null.</exception>
		internal void OnProgressChanged(ErasureTarget sender, ProgressChangedEventArgs e)
		{
			if (sender == null)
				throw new ArgumentNullException("sender");
			if (e == null)
				throw new ArgumentNullException("sender");
			if (e.UserState.GetType() != typeof(TaskProgressChangedEventArgs))
				throw new ArgumentException("The Task.OnProgressChanged event expects a " +
					"TaskProgressEventArgs argument for the ProgressChangedEventArgs' UserState " +
					"object.", "e");

			if (ProgressChanged != null)
				ProgressChanged(sender, e);
		}

		/// <summary>
		/// Broadcasts the task execution completion event.
		/// </summary>
		internal void OnTaskFinished()
		{
			Progress = null;
			Executing = false;
			if (TaskFinished != null)
				TaskFinished(this, EventArgs.Empty);
		}
		#endregion
	}

	/// <summary>
	/// A base event class for all event arguments involving a task.
	/// </summary>
	public class TaskEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="task">The task being referred to by this event.</param>
		public TaskEventArgs(Task task)
		{
			Task = task;
		}

		/// <summary>
		/// The executing task.
		/// </summary>
		public Task Task { get; private set; }
	}

	/// <summary>
	/// Stores extra information in the <see cref="ProgressChangedEventArgs"/>
	/// structure that is not conveyed in the ProgressManagerBase classes.
	/// </summary>
	public class TaskProgressChangedEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="itemName">The item whose erasure progress is being erased.</param>
		/// <param name="itemPass">The current pass number for this item.</param>
		/// <param name="itemTotalPasses">The total number of passes to complete erasure
		/// of this item.</param>
		public TaskProgressChangedEventArgs(string itemName, int itemPass,
			int itemTotalPasses)
		{
			ItemName = itemName;
			ItemPass = itemPass;
			ItemTotalPasses = itemTotalPasses;
		}

		/// <summary>
		/// The file name of the item being erased.
		/// </summary>
		public string ItemName { get; private set; }

		/// <summary>
		/// The pass number of a multi-pass erasure method.
		/// </summary>
		public int ItemPass { get; private set; }

		/// <summary>
		/// The total number of passes to complete before this erasure method is
		/// completed.
		/// </summary>
		public int ItemTotalPasses { get; private set; }
	}
}