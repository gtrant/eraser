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
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.Manager
{
	/// <summary>
	/// Deals with an erase task.
	/// </summary>
	[Serializable]
	public class Task : ITask
	{
		#region Serialization code
		protected Task(SerializationInfo info, StreamingContext context)
		{
			Name = (string)info.GetValue("Name", typeof(string));
			Executor = context.Context as Executor;
			Targets = (ErasureTargetCollection)info.GetValue("Targets", typeof(ErasureTargetCollection));
			Targets.Owner = this;
			Log = (List<LogSinkBase>)info.GetValue("Log", typeof(List<LogSinkBase>));
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

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			Canceled = false;
			Name = reader.GetAttribute("name");
			bool empty = reader.IsEmptyElement;
			reader.ReadStartElement("Task");

			if (!empty)
			{
				while (reader.NodeType != XmlNodeType.EndElement)
				{
					switch (reader.Name)
					{
						case "Schedule":
							ReadSchedule(reader);
							break;

						case "ErasureTargetCollection":
							ReadTargets(reader);
							break;

						case "Logs":
							ReadLog(reader);
							break;

						default:
							System.Diagnostics.Debug.Assert(false);
							break;
					}
				}

				reader.ReadEndElement();
			}
		}

		private void ReadSchedule(XmlReader reader)
		{
			//Get the type of the schedule.
			string type = reader.GetAttribute("type");
			bool empty = reader.IsEmptyElement;

			//Consume the <Schedule> element
			reader.ReadStartElement("Schedule");

			switch (type)
			{
				case "Now":
					Schedule = Schedule.RunNow;
					break;

				case "Restart":
					Schedule = Schedule.RunOnRestart;
					break;

				case "Recurring":
					XmlSerializer serializer = new XmlSerializer(typeof(RecurringSchedule));
					schedule = (RecurringSchedule)serializer.Deserialize(reader);
					break;

				case "Manual":
				default:
					Schedule = Schedule.RunManually;
					break;
			}

			if (!empty)
				//Consume the </Schedule> element, if there is one
				reader.ReadEndElement();
		}

		private void ReadTargets(XmlReader reader)
		{
			XmlSerializer targetsSerializer = new XmlSerializer(Targets.GetType());
			Targets = (ErasureTargetCollection)targetsSerializer.Deserialize(reader);
			Targets.Owner = this;
		}

		private void ReadLog(XmlReader reader)
		{
			//Consume the <Logs> element.
			bool empty = reader.IsEmptyElement;
			reader.ReadStartElement("Logs");
			if (empty)
				return;

			//We can either have a ArrayOfLogEntry or LogRef element as children.
			Log = new List<LogSinkBase>();
			while (reader.NodeType != XmlNodeType.EndElement)
			{
				if (reader.IsEmptyElement)
				{
					reader.ReadStartElement();
				}
				else if (reader.Name == "LogRef")
				{
					reader.ReadStartElement();
					Log.Add(new LazyLogSink(reader.ReadString()));
					reader.ReadEndElement();
				}
				else if (reader.Name == "ArrayOfLogEntry")
				{
					XmlSerializer logSerializer = new XmlSerializer(typeof(LogSink));
					Log.Add((LogSink)logSerializer.Deserialize(reader));
				}
			}

			reader.ReadEndElement();
		}

		/// <summary>
		/// Writes the common part of the Task XML Element.
		/// </summary>
		/// <param name="writer">The XML Writer instance to write to.</param>
		private void WriteXmlCommon(XmlWriter writer)
		{
			writer.WriteAttributeString("name", Name);

			writer.WriteStartElement("Schedule");
			if (schedule.GetType() == Schedule.RunManually.GetType())
				writer.WriteAttributeString("type", "Manual");
			else if (schedule.GetType() == Schedule.RunNow.GetType())
				writer.WriteAttributeString("type", "Now");
			else if (schedule.GetType() == Schedule.RunOnRestart.GetType())
				writer.WriteAttributeString("type", "Restart");
			else if (schedule is RecurringSchedule)
			{
				writer.WriteAttributeString("type", "Recurring");
				XmlSerializer serializer = new XmlSerializer(schedule.GetType());
				serializer.Serialize(writer, schedule);
			}
			writer.WriteEndElement();

			XmlSerializer targetsSerializer = new XmlSerializer(Targets.GetType());
			targetsSerializer.Serialize(writer, Targets);
		}

		public void WriteXml(XmlWriter writer)
		{
			WriteXmlCommon(writer);

			writer.WriteStartElement("Logs");
			foreach (LogSinkBase log in Log)
			{
				XmlSerializer logSerializer = new XmlSerializer(log.GetType());
				logSerializer.Serialize(writer, log);
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Writes an XML element with the Log entries linked instead of embedded
		/// in the task list.
		/// </summary>
		/// <param name="writer">The XML Writer instance to write to.</param>
		/// <param name="logPaths">The path to a folder to store logs in.</param>
		public void WriteSeparatedXml(XmlWriter writer, string logPaths)
		{
			WriteXmlCommon(writer);

			writer.WriteStartElement("Logs");
			foreach (LogSinkBase log in Log)
			{
				//If we have a file-backed log, retain that.
				if (log is LazyLogSink)
				{
					writer.WriteElementString("LogRef", ((LazyLogSink)log).SavePath);
				}
				
				//Otherwise, decide if we want to store the log inline (if small) or
				//link to the log file.
				else if (log.Count < 5)
				{
					//Small log, keep it inline.
					XmlSerializer logSerializer = new XmlSerializer(log.GetType());
					logSerializer.Serialize(writer, log);
				}
				else
				{
					string savePath;
					do
					{
						savePath = Path.Combine(logPaths, Guid.NewGuid().ToString() + ".log");
					}
					while (File.Exists(savePath));

					log.Save(savePath);
					writer.WriteElementString("LogRef", savePath);
				}
			}
			writer.WriteEndElement();
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public Task()
		{
			Name = string.Empty;
			Targets = new ErasureTargetCollection(this);
			Schedule = Schedule.RunNow;
			Canceled = false;
			Log = new List<LogSinkBase>();
		}

		/// <summary>
		/// The Executor object which is managing this task.
		/// </summary>
		public Executor Executor
		{
			get
			{
				return executor;
			}
			internal set
			{
				if (executor != null && value != null)
					throw new InvalidOperationException("A task can only belong to one " +
						"executor at any one time");

				executor = value;
			}
		}

		/// <summary>
		/// The name for this task. This is just an opaque value for the user to
		/// recognize the task.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The name of the task, used for display in UI elements.
		/// </summary>
		public override string ToString()
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
				foreach (IErasureTarget tgt in Targets)
					result += S._("{0}, ", tgt);

				return result.Remove(result.Length - 2);
			}
			else
			{
				//Ok, we've quite a few entries, get the first, the mid and the end.
				result = S._("{0}, ", Targets[0]);
				result += S._("{0}, ", Targets[Targets.Count / 2]);
				result += Targets[Targets.Count - 1];

				return S._("{0} and {1} other targets", result, Targets.Count - 3);
			}
		}

		/// <summary>
		/// The set of data to erase when this task is executed.
		/// </summary>
		public ErasureTargetCollection Targets { get; private set; }

		/// <summary>
		/// <see cref="Targets"/>
		/// </summary>
		ICollection<IErasureTarget> ITask.Targets
		{
			get { return Targets; }
		}

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
		public List<LogSinkBase> Log { get; private set; }

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
				if (Executor == null)
					throw new InvalidOperationException();

				return Executor.IsTaskQueued(this);
			}
		}

		/// <summary>
		/// Gets whether the task has been cancelled from execution.
		/// </summary>
		public bool Canceled
		{
			get;
			private set;
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
		/// Executes the task in the context of the calling thread.
		/// </summary>
		public void Execute()
		{
			OnTaskStarted();
			Executing = true;
			Canceled = false;
			Progress = new SteppedProgressManager();

			try
			{
				//Run the task
				foreach (IErasureTarget target in Targets)
					try
					{
						Progress.Steps.Add(new ErasureTargetProgressManagerStep(
							target, Targets.Count));
						target.Execute();
					}
					catch (FatalException)
					{
						throw;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (NotSupportedException e)
					{
						//This is thrown whenever we try to erase files on an unsupported
						//filesystem.
						Logger.Log(e.Message, LogLevel.Error);
					}
					catch (PathTooLongException e)
					{
						//Until we have code to deal with paths using NT names, we can't
						//do much about it.
						Logger.Log(e.Message, LogLevel.Error);
					}
					catch (SharingViolationException)
					{
					}
			}
			catch (FatalException e)
			{
				Logger.Log(e.Message, LogLevel.Fatal);
			}
			catch (OperationCanceledException e)
			{
				Logger.Log(e.Message, LogLevel.Fatal);
			}
			catch (SharingViolationException)
			{
			}
			finally
			{
				//If the task is a recurring task, reschedule it since we are done.
				if (Schedule is RecurringSchedule)
				{
					((RecurringSchedule)Schedule).Reschedule(DateTime.Now);
				}

				Progress = null;
				Executing = false;
				OnTaskFinished();
			}
		}

		private Executor executor;
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
		private void OnTaskStarted()
		{
			if (TaskStarted != null)
				TaskStarted(this, EventArgs.Empty);
		}

		/// <summary>
		/// Broadcasts the task execution completion event.
		/// </summary>
		private void OnTaskFinished()
		{
			if (TaskFinished != null)
				TaskFinished(this, EventArgs.Empty);
		}
		#endregion
	}

	/// <summary>
	/// Returns the progress of an erasure target, since that comprises the
	/// steps of the Task Progress.
	/// </summary>
	public class ErasureTargetProgressManagerStep : SteppedProgressManagerStepBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="target">The erasure target represented by this object.</param>
		/// <param name="steps">The number of targets in the task.</param>
		public ErasureTargetProgressManagerStep(IErasureTarget target, int targets)
			: base(1.0f / targets)
		{
			Target = target;
		}

		public override ProgressManagerBase Progress
		{
			get
			{
				ProgressManagerBase targetProgress = Target.Progress;
				if (targetProgress != null)
					TargetProgress = targetProgress;

				return TargetProgress;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// The erasure target represented by this step.
		/// </summary>
		public IErasureTarget Target
		{
			get;
			private set;
		}

		/// <summary>
		/// Caches a copy of the progress object for the Target. This is so that
		/// for as long we our object is alive we can give valid information
		/// (as required by the SteppedProgressManager class)
		/// </summary>
		private ProgressManagerBase TargetProgress;
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
}