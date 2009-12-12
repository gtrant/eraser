/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.ComponentModel;
using Eraser.Util;
using System.Security.Permissions;
using System.Threading;

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
			Log = (Logger)info.GetValue("Log", typeof(Logger));
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
			Log = new Logger();
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
				if (Name.Length != 0)
					return Name;

				string result = string.Empty;
				if (Targets.Count < 3)
					//Simpler case, small set of data.
					foreach (ErasureTarget tgt in Targets)
						result += tgt.UIText + ", ";
				else
					//Ok, we've quite a few entries, get the first, the mid and the end.
					for (int i = 0; i < Targets.Count; i += Targets.Count / 3)
						result += Targets[i].UIText + ", ";
				return result.Substring(0, result.Length - 2);
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
			get
			{
				return canceled;
			}

			internal set
			{
				canceled = value;
			}
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
					throw new ArgumentException(S._("The schedule provided can only " +
						"belong to one task at a time"));

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
		public Logger Log { get; private set; }

		private Schedule schedule;

		/// <see cref="Canceled"/>
		private volatile bool canceled;

		#region Events
		/// <summary>
		/// The task has been edited.
		/// </summary>
		public EventHandler<TaskEventArgs> TaskEdited { get; set; }

		/// <summary>
		/// The start of the execution of a task.
		/// </summary>
		public EventHandler<TaskEventArgs> TaskStarted { get; set; }

		/// <summary>
		/// The event object holding all event handlers.
		/// </summary>
		public EventHandler<TaskProgressEventArgs> ProgressChanged { get; set; }

		/// <summary>
		/// The completion of the execution of a task.
		/// </summary>
		public EventHandler<TaskEventArgs> TaskFinished { get; set; }

		/// <summary>
		/// Broadcasts the task edited event.
		/// </summary>
		internal void OnTaskEdited()
		{
			if (TaskEdited != null)
				TaskEdited(this, new TaskEventArgs(this));
		}

		/// <summary>
		/// Broadcasts the task execution start event.
		/// </summary>
		/// <param name="e"></param>
		internal void OnTaskStarted(TaskEventArgs e)
		{
			if (TaskStarted != null)
				TaskStarted(this, e);
			Executing = true;
		}

		/// <summary>
		/// Broadcasts a ProgressChanged event.
		/// </summary>
		/// <param name="e">The new progress value.</param>
		internal void OnProgressChanged(TaskProgressEventArgs e)
		{
			if (ProgressChanged != null)
				ProgressChanged(this, e);
		}

		/// <summary>
		/// Broadcasts the task execution completion event.
		/// </summary>
		/// <param name="e"></param>
		internal void OnTaskFinished(TaskEventArgs e)
		{
			if (TaskFinished != null)
				TaskFinished(this, e);
			Executing = false;
		}
		#endregion
	}

	/// <summary>
	/// Represents a generic target of erasure
	/// </summary>
	[Serializable]
	public abstract class ErasureTarget : ISerializable
	{
		#region Serialization code
		protected ErasureTarget(SerializationInfo info, StreamingContext context)
		{
			Guid methodGuid = (Guid)info.GetValue("Method", typeof(Guid));
			if (methodGuid == Guid.Empty)
				method = ErasureMethodManager.Default;
			else
				method = ErasureMethodManager.GetInstance(methodGuid);
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Method", method.Guid);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected ErasureTarget()
		{
		}

		/// <summary>
		/// The method used for erasing the file. If the variable is equal to
		/// ErasureMethodManager.Default then the default is queried for the
		/// task type. Check the <see cref="MethodDefined"/> property to see if
		/// this variable was set on deliberately or if the result of the get
		/// call is from the inferred default.
		/// </summary>
		public virtual ErasureMethod Method
		{
			get
			{
				return method;
			}
			set
			{
				method = value;
				MethodDefined = method != ErasureMethodManager.Default;
			}
		}

		/// <summary>
		/// Checks whether a method has been selected for this target. This is
		/// because the Method property will return non-default erasure methods
		/// only.
		/// </summary>
		public bool MethodDefined { get; private set; }

		/// <summary>
		/// The task which owns this target.
		/// </summary>
		public Task Task { get; internal set; }

		/// <summary>
		/// Retrieves the text to display representing this task.
		/// </summary>
		public abstract string UIText
		{
			get;
		}

		/// <summary>
		/// Retrieves the amount of data that needs to be written in order to
		/// complete the erasure.
		/// </summary>
		public abstract long TotalData
		{
			get;
		}

		/// <summary>
		/// Erasure method to use for the target.
		/// </summary>
		private ErasureMethod method;
	}

	/// <summary>
	/// Class representing a tangible object (file/folder) to be erased.
	/// </summary>
	[Serializable]
	public abstract class FileSystemObjectTarget : ErasureTarget
	{
		#region Serialization code
		protected FileSystemObjectTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Path = (string)info.GetValue("Path", typeof(string));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Path", Path);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected FileSystemObjectTarget()
			: base()
		{
			Method = ErasureMethodManager.Default;
		}

		/// <summary>
		/// Retrieves the list of files/folders to erase as a list.
		/// </summary>
		/// <param name="totalSize">Returns the total size in bytes of the
		/// items.</param>
		/// <returns>A list containing the paths to all the files to be erased.</returns>
		internal abstract List<string> GetPaths(out long totalSize);

		/// <summary>
		/// Adds ADSes of the given file to the list.
		/// </summary>
		/// <param name="list">The list to add the ADS paths to.</param>
		/// <param name="file">The file to look for ADSes</param>
		protected void GetPathADSes(ICollection<string> list, out long totalSize, string file)
		{
			totalSize = 0;

			try
			{
				//Get the ADS names
				ICollection<string> adses = Util.File.GetADSes(new FileInfo(file));

				//Then prepend the path.
				foreach (string adsName in adses)
				{
					string adsPath = file + ':' + adsName;
					list.Add(adsPath);
					Util.StreamInfo info = new Util.StreamInfo(adsPath);
					totalSize += info.Length;
				}
			}
			catch (FileLoadException)
			{
				//The system cannot open the file, try to force the file handle to close.
				if (!ManagerLibrary.Settings.ForceUnlockLockedFiles)
					throw;

				foreach (OpenHandle handle in OpenHandle.Items)
					if (handle.Path == file && handle.Close())
					{
						GetPathADSes(list, out totalSize, file);
						return;
					}
			}
			catch (UnauthorizedAccessException e)
			{
				//The system cannot read the file, assume no ADSes for lack of
				//more information.
				Task.Log.LastSessionEntries.Add(new LogEntry(e.Message, LogLevel.Error));
			}
		}

		/// <summary>
		/// The path to the file or folder referred to by this object.
		/// </summary>
		public string Path { get; set; }

		public sealed override ErasureMethod Method
		{
			get
			{
				if (base.MethodDefined)
					return base.Method;
				return ErasureMethodManager.GetInstance(
					ManagerLibrary.Settings.DefaultFileErasureMethod);
			}
			set
			{
				base.Method = value;
			}
		}

		public override string UIText
		{
			get { return Path; }
		}

		public override long TotalData
		{
			get
			{
				long totalSize = 0;
				List<string> paths = GetPaths(out totalSize);
				return Method.CalculateEraseDataSize(paths, totalSize);
			}
		}
	}

	/// <summary>
	/// Class representing a unused space erase.
	/// </summary>
	[Serializable]
	public class UnusedSpaceTarget : ErasureTarget
	{
		#region Serialization code
		protected UnusedSpaceTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Drive = (string)info.GetValue("Drive", typeof(string));
			EraseClusterTips = (bool)info.GetValue("EraseClusterTips", typeof(bool));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Drive", Drive);
			info.AddValue("EraseClusterTips", EraseClusterTips);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public UnusedSpaceTarget()
			: base()
		{
			Method = ErasureMethodManager.Default;
		}

		public override sealed ErasureMethod Method
		{
			get
			{
				if (base.MethodDefined)
					return base.Method;
				return ErasureMethodManager.GetInstance(
					ManagerLibrary.Settings.DefaultUnusedSpaceErasureMethod);
			}
			set
			{
				base.Method = value;
			}
		}

		public override string UIText
		{
			get { return S._("Unused disk space ({0})", Drive); }
		}

		public override long TotalData
		{
			get
			{
				VolumeInfo info = VolumeInfo.FromMountpoint(Drive);
				return Method.CalculateEraseDataSize(null, info.AvailableFreeSpace);
			}
		}

		/// <summary>
		/// The drive to erase
		/// </summary>
		public string Drive { get; set; }

		/// <summary>
		/// Whether cluster tips should be erased.
		/// </summary>
		public bool EraseClusterTips { get; set; }
	}

	/// <summary>
	/// Class representing a file to be erased.
	/// </summary>
	[Serializable]
	public class FileTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected FileTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FileTarget()
		{
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<string> result = new List<string>();
			FileInfo fileInfo = new FileInfo(Path);

			if (fileInfo.Exists)
			{
				GetPathADSes(result, out totalSize, Path);
				totalSize += fileInfo.Length;
			}

			result.Add(Path);
			return result;
		}
	}

	/// <summary>
	/// Represents a folder and its files which are to be erased.
	/// </summary>
	[Serializable]
	public class FolderTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected FolderTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			IncludeMask = (string)info.GetValue("IncludeMask", typeof(string));
			ExcludeMask = (string)info.GetValue("ExcludeMask", typeof(string));
			DeleteIfEmpty = (bool)info.GetValue("DeleteIfEmpty", typeof(bool));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("IncludeMask", IncludeMask);
			info.AddValue("ExcludeMask", ExcludeMask);
			info.AddValue("DeleteIfEmpty", DeleteIfEmpty);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FolderTarget()
		{
			IncludeMask = string.Empty;
			ExcludeMask = string.Empty;
			DeleteIfEmpty = true;
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			//Get a list to hold all the resulting paths.
			List<string> result = new List<string>();

			//Open the root of the search, including every file matching the pattern
			DirectoryInfo dir = new DirectoryInfo(Path);

			//List recursively all the files which match the include pattern.
			FileInfo[] files = GetFiles(dir);

			//Then exclude each file and finalize the list and total file size
			totalSize = 0;
			if (ExcludeMask.Length != 0)
			{
				string regex = Regex.Escape(ExcludeMask).Replace("\\*", ".*").
					Replace("\\?", ".");
				Regex excludePattern = new Regex(regex, RegexOptions.IgnoreCase);
				foreach (FileInfo file in files)
					if ((file.Attributes & FileAttributes.ReparsePoint) == 0 &&
						excludePattern.Matches(file.FullName).Count == 0)
					{
						totalSize += file.Length;
						GetPathADSes(result, out totalSize, file.FullName);
						result.Add(file.FullName);
					}
			}
			else
				foreach (FileInfo file in files)
				{
					if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
						continue;

					//Get the size of the file and its ADSes
					totalSize += file.Length;
					long adsesSize = 0;
					GetPathADSes(result, out adsesSize, file.FullName);
					totalSize += adsesSize;

					//Append this file to the list of files to erase.
					result.Add(file.FullName);
				}

			//Return the filtered list.
			return result;
		}

		/// <summary>
		/// Gets all files in the provided directory.
		/// </summary>
		/// <param name="info">The directory to look files in.</param>
		/// <returns>A list of files found in the directory matching the IncludeMask
		/// property.</returns>
		private FileInfo[] GetFiles(DirectoryInfo info)
		{
			List<FileInfo> result = new List<FileInfo>();
			if (info.Exists)
			{
				foreach (DirectoryInfo dir in info.GetDirectories())
					try
					{
						result.AddRange(GetFiles(dir));
					}
					catch (DirectoryNotFoundException e)
					{
						//Ignore, but log.
						Task.Log.LastSessionEntries.Add(new LogEntry(S._("Could not erase {0} because {1}",
							dir.FullName, e.Message), LogLevel.Error));
					}

				if (IncludeMask.Length == 0)
					result.AddRange(info.GetFiles());
				else
					result.AddRange(info.GetFiles(IncludeMask, SearchOption.TopDirectoryOnly));
			}

			return result.ToArray();
		}

		/// <summary>
		/// A wildcard expression stating the condition for the set of files to include.
		/// The include mask is applied before the exclude mask is applied. If this value
		/// is empty, all files and folders within the folder specified is included.
		/// </summary>
		public string IncludeMask { get; set; }

		/// <summary>
		/// A wildcard expression stating the condition for removing files from the set
		/// of included files. If this value is omitted, all files and folders extracted
		/// by the inclusion mask is erased.
		/// </summary>
		public string ExcludeMask { get; set; }

		/// <summary>
		/// Determines if Eraser should delete the folder after the erase process.
		/// </summary>
		public bool DeleteIfEmpty { get; set; }
	}

	[Serializable]
	public class RecycleBinTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected RecycleBinTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		public RecycleBinTarget()
		{
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<string> result = new List<string>();
			string[] rootDirectory = new string[] {
					"$RECYCLE.BIN",
					"RECYCLER"
				};

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				foreach (string rootDir in rootDirectory)
				{
					DirectoryInfo dir = new DirectoryInfo(
						System.IO.Path.Combine(
							System.IO.Path.Combine(drive.Name, rootDir),
							System.Security.Principal.WindowsIdentity.GetCurrent().
								User.ToString()));
					if (!dir.Exists)
						continue;

					GetRecyclerFiles(dir, ref result, ref totalSize);
				}
			}

			return result;
		}

		/// <summary>
		/// Retrieves all files within this folder, without exclusions.
		/// </summary>
		/// <param name="info">The DirectoryInfo object representing the folder to traverse.</param>
		/// <param name="paths">The list of files to store path information in.</param>
		/// <param name="totalSize">Receives the total size of the files.</param>
		private void GetRecyclerFiles(DirectoryInfo info, ref List<string> paths,
			ref long totalSize)
		{
			try
			{
				foreach (FileSystemInfo fsInfo in info.GetFileSystemInfos())
				{
					FileInfo fileInfo = fsInfo as FileInfo;
					if (fileInfo != null)
					{
						totalSize += fileInfo.Length;
						GetPathADSes(paths, out totalSize, fileInfo.FullName);
						paths.Add(fileInfo.FullName);
					}
					else
						GetRecyclerFiles((DirectoryInfo)fsInfo, ref paths, ref totalSize);
				}
			}
			catch (UnauthorizedAccessException e)
			{
				Task.Log.LastSessionEntries.Add(new LogEntry(e.Message, LogLevel.Error));
			}
		}

		/// <summary>
		/// Retrieves the text to display representing this task.
		/// </summary>
		public override string UIText
		{
			get
			{
				return S._("Recycle Bin");
			}
		}
	}

	/// <summary>
	/// Maintains a collection of erasure targets.
	/// </summary>
	[Serializable]
	public class ErasureTargetsCollection : IList<ErasureTarget>, ICollection<ErasureTarget>,
		IEnumerable<ErasureTarget>, ISerializable
	{
		#region Constructors
		internal ErasureTargetsCollection(Task owner)
		{
			this.list = new List<ErasureTarget>();
			this.owner = owner;
		}

		internal ErasureTargetsCollection(Task owner, int capacity)
			: this(owner)
		{
			list.Capacity = capacity;
		}

		internal ErasureTargetsCollection(Task owner, IEnumerable<ErasureTarget> targets)
			: this(owner)
		{
			list.AddRange(targets);
		}
		#endregion

		#region Serialization Code
		protected ErasureTargetsCollection(SerializationInfo info, StreamingContext context)
		{
			list = (List<ErasureTarget>)info.GetValue("list", typeof(List<ErasureTarget>));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("list", list);
		}
		#endregion

		#region IEnumerable<ErasureTarget> Members
		public IEnumerator<ErasureTarget> GetEnumerator()
		{
			return list.GetEnumerator();
		}
		#endregion

		#region IEnumerable Members
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion

		#region ICollection<ErasureTarget> Members
		public void Add(ErasureTarget item)
		{
			item.Task = owner;
			list.Add(item);
		}

		public void Clear()
		{
			foreach (ErasureTarget item in list)
				Remove(item);
		}

		public bool Contains(ErasureTarget item)
		{
			return list.Contains(item);
		}

		public void CopyTo(ErasureTarget[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				return list.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool Remove(ErasureTarget item)
		{
			int index = list.IndexOf(item);
			if (index < 0)
				return false;

			RemoveAt(index);
			return true;
		}
		#endregion

		#region IList<ErasureTarget> Members
		public int IndexOf(ErasureTarget item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, ErasureTarget item)
		{
			item.Task = owner;
			list.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
		}

		public ErasureTarget this[int index]
		{
			get
			{
				return list[index];
			}
			set
			{
				list[index] = value;
			}
		}
		#endregion

		/// <summary>
		/// The owner of this list of targets.
		/// </summary>
		public Task Owner
		{
			get
			{
				return owner;
			}
			internal set
			{
				owner = value;
				foreach (ErasureTarget target in list)
					target.Task = owner;
			}
		}

		/// <summary>
		/// The owner of this list of targets. All targets added to this list
		/// will have the owner set to this object.
		/// </summary>
		private Task owner;

		/// <summary>
		/// The list bring the data store behind this object.
		/// </summary>
		private List<ErasureTarget> list;
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
	/// A Event argument object containing the progress of the task.
	/// </summary>
	public class TaskProgressEventArgs : TaskEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="task">The task being run.</param>
		public TaskProgressEventArgs(Task task)
			: base(task)
		{
			CurrentItemPass = 1;
		}

		/// <summary>
		/// A number from 0 to 1 detailing the overall progress of the task.
		/// </summary>
		public float OverallProgress
		{
			get { return overallProgress; }
		}

		/// <summary>
		/// The amount of time left for the operation to complete, in seconds.
		/// </summary>
		public TimeSpan TimeLeft { get; internal set; }

		/// <summary>
		/// The current erasure target - the current item being erased.
		/// </summary>
		public ErasureTarget CurrentTarget { get; internal set; }

		/// <summary>
		/// The current index of the target.
		/// </summary>
		public int CurrentTargetIndex { get; internal set; }

		/// <summary>
		/// The total number of passes to complete before this erasure method is
		/// completed.
		/// </summary>
		public int CurrentTargetTotalPasses { get; internal set; }

		/// <summary>
		/// The stage of the erasure the executor is at.
		/// </summary>
		public string CurrentTargetStatus { get; internal set; }

		/// <summary>
		/// A number from 0 to 1 detailing the overall progress of the item.
		/// Negative numbers indicate indeterminate progress.
		/// </summary>
		public float CurrentItemProgress { get; internal set; }

		/// <summary>
		/// The file name of the item being erased.
		/// </summary>
		public string CurrentItemName { get; internal set; }

		/// <summary>
		/// The pass number of a multi-pass erasure method.
		/// </summary>
		public int CurrentItemPass { get; internal set; }

		/// <summary>
		/// The progress made by the current target.
		/// </summary>
		internal float CurrentTargetProgress
		{
			set
			{
				overallProgress = Math.Min(
					(value + (float)(CurrentTargetIndex - 1)) / Task.Targets.Count,
					1.0f);
			}
		}

		private float overallProgress;
	}
}