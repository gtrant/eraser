/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net> @17/10/2008
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
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.IO;

using Eraser.Util;
using System.Security.Principal;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;

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
			if (disposing)
			{
				thread.Abort();
				schedulerInterrupt.Set();
				schedulerInterrupt.Close();
			}

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
				ManagerLibrary.Settings.ExecuteMissedTasksImmediately) ?
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

		private void OnTaskEdited(object sender, TaskEventArgs e)
		{
			//Find all schedule entries containing the task - since the user cannot make
			//edits to the task when it is queued (only if it is scheduled) remove
			//all task references and add them back
			lock (tasksLock)
				for (int i = 0; i != scheduledTasks.Count; ++i)
					for (int j = 0; j < scheduledTasks.Values[i].Count; )
					{
						Task currentTask = scheduledTasks.Values[i][j];
						if (currentTask == e.Task)
							scheduledTasks.Values[i].RemoveAt(j);
						else
							j++;
					}

			//Then reschedule the task
			if (e.Task.Schedule is RecurringSchedule)
				ScheduleTask(e.Task);
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
					//Set the currently executing task.
					currentTask = task;

					try
					{
						//Prevent the system from sleeping.
						KernelApi.SetThreadExecutionState(ThreadExecutionState.Continuous |
							ThreadExecutionState.SystemRequired);

						//Broadcast the task started event.
						task.Canceled = false;
						task.OnTaskStarted(new TaskEventArgs(task));
						OnTaskProcessing(new TaskEventArgs(task));

						//Start a new log session to separate this session's events
						//from previous ones.
						task.Log.Entries.NewSession();

						//Run the task
						foreach (ErasureTarget target in task.Targets)
							try
							{
								UnusedSpaceTarget unusedSpaceTarget =
									target as UnusedSpaceTarget;
								FileSystemObjectTarget fileSystemObjectTarget =
									target as FileSystemObjectTarget;

								if (unusedSpaceTarget != null)
									EraseUnusedSpace(task, unusedSpaceTarget);
								else if (fileSystemObjectTarget != null)
									EraseFilesystemObject(task, fileSystemObjectTarget);
								else
									throw new ArgumentException(S._("Unknown erasure target."));
							}
							catch (FatalException)
							{
								throw;
							}
							catch (OperationCanceledException)
							{
								throw;
							}
							catch (Exception e)
							{
								task.Log.LastSessionEntries.Add(new LogEntry(e.Message, LogLevel.Error));
								BlackBox.Get().CreateReport(e);
							}
					}
					catch (FatalException e)
					{
						task.Log.LastSessionEntries.Add(new LogEntry(e.Message, LogLevel.Fatal));
					}
					catch (OperationCanceledException e)
					{
						task.Log.LastSessionEntries.Add(new LogEntry(e.Message, LogLevel.Fatal));
					}
					catch (Exception e)
					{
						task.Log.LastSessionEntries.Add(new LogEntry(e.Message, LogLevel.Error));
						BlackBox.Get().CreateReport(e);
					}
					finally
					{
						//Allow the system to sleep again.
						KernelApi.SetThreadExecutionState(ThreadExecutionState.Continuous);

						//If the task is a recurring task, reschedule it since we are done.
						if (task.Schedule is RecurringSchedule)
							((RecurringSchedule)task.Schedule).Reschedule(DateTime.Now);

						//If the task is an execute on restart task, it is only run
						//once and can now be restored to an immediately executed task
						if (task.Schedule == Schedule.RunOnRestart)
							task.Schedule = Schedule.RunNow;

						//And the task finished event.
						task.OnTaskFinished(new TaskEventArgs(task));
						OnTaskProcessed(new TaskEventArgs(task));

						//Remove the actively executing task from our instance variable
						currentTask = null;
					}
				}

				//Wait for half a minute to check for the next scheduled task.
				schedulerInterrupt.WaitOne(30000, false);
			}
		}

		/// <summary>
		/// Executes a unused space erase.
		/// </summary>
		/// <param name="task">The task currently being executed</param>
		/// <param name="target">The target of the unused space erase.</param>
		private void EraseUnusedSpace(Task task, UnusedSpaceTarget target)
		{
			//Check for sufficient privileges to run the unused space erasure.
			if (!AdvApi.IsAdministrator())
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
					Environment.OSVersion.Version >= new Version(6, 0))
				{
					throw new UnauthorizedAccessException(S._("The program does not have the " +
						"required permissions to erase the unused space on disk. Run the program " +
						"as an administrator and retry the operation."));
				}
				else
					throw new UnauthorizedAccessException(S._("The program does not have the " +
						"required permissions to erase the unused space on disk."));
			}

			//Check whether System Restore has any available checkpoints.
			if (SystemRestore.GetInstances().Count != 0)
			{
				task.Log.LastSessionEntries.Add(new LogEntry(S._("The drive {0} has System " +
					"Restore or Volume Shadow Copies enabled. This may allow copies of files " +
					"stored on the disk to be recovered and pose a security concern.",
					target.Drive), LogLevel.Warning));
			}
			
			//If the user is under disk quotas, log a warning message
			if (VolumeInfo.FromMountpoint(target.Drive).HasQuota)
				task.Log.LastSessionEntries.Add(new LogEntry(S._("The drive {0} has disk quotas " +
					"active. This will prevent the complete erasure of unused space and may pose " +
					"a security concern.", target.Drive), LogLevel.Warning));

			//Get the erasure method if the user specified he wants the default.
			ErasureMethod method = target.Method;

			//Make a folder to dump our temporary files in
			DirectoryInfo info = new DirectoryInfo(target.Drive);
			VolumeInfo volInfo = VolumeInfo.FromMountpoint(target.Drive);
			FileSystem fsManager = FileSystemManager.Get(volInfo);

			//Start sampling the speed of the task.
			SteppedProgressManager progress = new SteppedProgressManager();
			target.Progress = progress;
			task.Progress.Steps.Add(new SteppedProgressManager.Step(
				progress, 1.0f / task.Targets.Count));

			//Erase the cluster tips of every file on the drive.
			if (target.EraseClusterTips)
			{
				//Define the callback handlers
				ProgressManager tipSearch = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(tipSearch, 
					0.0f, S._("Searching for files' cluster tips...")));
				tipSearch.Total = 1;
				ClusterTipsSearchProgress searchProgress = delegate(string path)
					{
						if (currentTask.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));

						task.OnProgressChanged(target,
							new ProgressChangedEventArgs(tipSearch,
								new TaskProgressChangedEventArgs(path, 0, 0)));
					};

				ProgressManager tipProgress = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(tipProgress, 0.1f,
					S._("Erasing cluster tips...")));
				ClusterTipsEraseProgress eraseProgress =
					delegate(int currentFile, int totalFiles, string currentFilePath)
					{
						tipSearch.Completed = tipSearch.Total;
						tipProgress.Total = totalFiles;
						tipProgress.Completed = currentFile;
						task.OnProgressChanged(target,
							new ProgressChangedEventArgs(tipProgress,
								new TaskProgressChangedEventArgs(currentFilePath, 0, 0)));

						if (currentTask.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));
					};

				//Start counting statistics
				fsManager.EraseClusterTips(VolumeInfo.FromMountpoint(target.Drive),
					method, task.Log, searchProgress, eraseProgress);
			}

			info = info.CreateSubdirectory(Path.GetFileName(
				FileSystem.GenerateRandomFileName(info, 18)));
			try
			{
				//Set the folder's compression flag off since we want to use as much
				//space as possible
				if (Eraser.Util.File.IsCompressed(info.FullName))
					Eraser.Util.File.SetCompression(info.FullName, false);

				ProgressManager mainProgress = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(mainProgress,
					target.EraseClusterTips ? 0.8f : 0.9f, S._("Erasing unused space...")));

				//Continue creating files while there is free space.
				while (volInfo.AvailableFreeSpace > 0)
				{
					//Generate a non-existant file name
					string currFile = FileSystem.GenerateRandomFileName(info, 18);

					//Create the stream
					using (FileStream stream = new FileStream(currFile, FileMode.CreateNew,
						FileAccess.Write, FileShare.None, 8, FileOptions.WriteThrough))
					{
						//Set the length of the file to be the amount of free space left
						//or the maximum size of one of these dumps.
						mainProgress.Total = mainProgress.Completed + volInfo.AvailableFreeSpace;
						long streamLength = Math.Min(ErasureMethod.FreeSpaceFileUnit,
							mainProgress.Total);

						//Handle IO exceptions gracefully, because the filesystem
						//may require more space than demanded by us for file allocation.
						while (true)
							try
							{
								stream.SetLength(streamLength);
								break;
							}
							catch (IOException)
							{
								if (streamLength > volInfo.ClusterSize)
									streamLength -= volInfo.ClusterSize;
								else
									throw;
							}

						//Then run the erase task
						method.Erase(stream, long.MaxValue,
							PrngManager.GetInstance(ManagerLibrary.Settings.ActivePrng),
							delegate(long lastWritten, long totalData, int currentPass)
							{
								mainProgress.Completed += lastWritten;
								task.OnProgressChanged(target,
									new ProgressChangedEventArgs(mainProgress,
										new TaskProgressChangedEventArgs(target.Drive, currentPass, method.Passes)));

								if (currentTask.Canceled)
									throw new OperationCanceledException(S._("The task was cancelled."));
							}
						);
					}
				}

				//Mark the main bulk of the progress as complete
				mainProgress.Completed = mainProgress.Total;

				//Erase old resident file system table files
				ProgressManager residentProgress = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(residentProgress,
					0.05f, S._("Old resident file system table files")));
				fsManager.EraseOldFileSystemResidentFiles(volInfo, info, method,
					delegate(int currentFile, int totalFiles)
					{
						residentProgress.Completed = currentFile;
						residentProgress.Total = totalFiles;
						task.OnProgressChanged(target,
							new ProgressChangedEventArgs(residentProgress,
								new TaskProgressChangedEventArgs(string.Empty, 0, 0)));

						if (currentTask.Canceled)
							throw new OperationCanceledException(S._("The task was cancelled."));
					}
				);

				residentProgress.Completed = residentProgress.Total = 1;
			}
			finally
			{
				//Remove the folder holding all our temporary files.
				ProgressManager tempFiles = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(tempFiles,
					0.0f, S._("Removing temporary files...")));
				task.OnProgressChanged(target, new ProgressChangedEventArgs(tempFiles,
					new TaskProgressChangedEventArgs(string.Empty, 0, 0)));
				fsManager.DeleteFolder(info);
				tempFiles.Completed = tempFiles.Total = 1;
			}

			//Then clean the old file system entries
			ProgressManager structureProgress = new ProgressManager();
			progress.Steps.Add(new SteppedProgressManager.Step(structureProgress,
				0.05f, S._("Erasing unused directory structures...")));
			fsManager.EraseDirectoryStructures(volInfo,
				delegate(int currentFile, int totalFiles)
				{
					if (currentTask.Canceled)
						throw new OperationCanceledException(S._("The task was cancelled."));

					//Compute the progress
					structureProgress.Total = totalFiles;
					structureProgress.Completed = currentFile;

					//Set the event parameters, then broadcast the progress event.
					task.OnProgressChanged(target,
						new ProgressChangedEventArgs(structureProgress,
							new TaskProgressChangedEventArgs(string.Empty, 0, 0)));
				}
			);

			structureProgress.Completed = structureProgress.Total;
			target.Progress = null;
		}

		/// <summary>
		/// Erases a file or folder on the volume.
		/// </summary>
		/// <param name="task">The task currently being processed.</param>
		/// <param name="target">The target of the erasure.</param>
		/// <param name="progress">The progress manager for the current task.</param>
		private void EraseFilesystemObject(Task task, FileSystemObjectTarget target)
		{
			//Retrieve the list of files to erase.
			long dataTotal = 0;
			List<string> paths = target.GetPaths(out dataTotal);

			//Get the erasure method if the user specified he wants the default.
			ErasureMethod method = target.Method;

			//Calculate the total amount of data required to finish the wipe.
			//dataTotal = method.CalculateEraseDataSize(paths, dataTotal);

			//Set the event's current target status.
			TaskEventArgs eventArgs = new TaskEventArgs(task);
			SteppedProgressManager progress = new SteppedProgressManager();
			target.Progress = progress;
			task.Progress.Steps.Add(new SteppedProgressManager.Step(progress, 1.0f / task.Targets.Count));

			//Iterate over every path, and erase the path.
			for (int i = 0; i < paths.Count; ++i)
			{
				//Update the task progress
				ProgressManager step = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(step,
					1.0f / paths.Count, S._("Erasing files...")));
				task.OnProgressChanged(target,
					new ProgressChangedEventArgs(step,
						new TaskProgressChangedEventArgs(paths[i], 0, method.Passes)));

				//Check that the file exists - we do not want to bother erasing nonexistant files
				StreamInfo info = new StreamInfo(paths[i]);
				if (!info.Exists)
				{
					task.Log.LastSessionEntries.Add(new LogEntry(S._("The file {0} was not erased " +
						"as the file does not exist.", paths[i]), LogLevel.Notice));
					continue;
				}

				//Get the filesystem provider to handle the secure file erasures
				FileSystem fsManager = FileSystemManager.Get(
					VolumeInfo.FromMountpoint(info.DirectoryName));

				bool isReadOnly = false;
				
				try
				{
					//Remove the read-only flag, if it is set.
					if (isReadOnly = info.IsReadOnly)
						info.IsReadOnly = false;

					//Make sure the file does not have any attributes which may affect
					//the erasure process
					if ((info.Attributes & FileAttributes.Compressed) != 0 || 
						(info.Attributes & FileAttributes.Encrypted) != 0 ||
						(info.Attributes & FileAttributes.SparseFile) != 0)
					{
						//Log the error
						task.Log.LastSessionEntries.Add(new LogEntry(S._("The file {0} could " +
							"not be erased because the file was either compressed, encrypted or " +
							"a sparse file.", info.FullName), LogLevel.Error));
					}

					fsManager.EraseFileSystemObject(info, method,
						delegate(long lastWritten, long totalData, int currentPass)
						{
							if (currentTask.Canceled)
								throw new OperationCanceledException(S._("The task was cancelled."));

							step.Completed += lastWritten;
							step.Total = totalData;
							task.OnProgressChanged(target,
								new ProgressChangedEventArgs(step,
									new TaskProgressChangedEventArgs(info.FullName, currentPass, method.Passes)));
						});

					//Remove the file.
					FileInfo fileInfo = info.File;
					if (fileInfo != null)
						fsManager.DeleteFile(fileInfo);
					step.Completed = step.Total = 1;
				}
				catch (UnauthorizedAccessException)
				{
					task.Log.LastSessionEntries.Add(new LogEntry(S._("The file {0} could not " +
						"be erased because the file's permissions prevent access to the file.",
						info.FullName), LogLevel.Error));
				}
				catch (FileLoadException)
				{
					if (!ManagerLibrary.Settings.ForceUnlockLockedFiles)
						throw;

					List<System.Diagnostics.Process> processes = new List<System.Diagnostics.Process>();
					foreach (OpenHandle handle in OpenHandle.Items)
						if (handle.Path == paths[i])
							processes.Add(System.Diagnostics.Process.GetProcessById(handle.ProcessId));

					string lockedBy = null;
					if (processes.Count > 0)
					{
						StringBuilder processStr = new StringBuilder();
						foreach (System.Diagnostics.Process process in processes)
							processStr.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
								"{0}, ", process.MainModule.FileName);

						lockedBy = S._("(locked by {0})", processStr.ToString().Remove(processStr.Length - 2));
					}

					task.Log.LastSessionEntries.Add(new LogEntry(S._(
						"Could not force closure of file \"{0}\" {1}", paths[i],
						lockedBy == null ? string.Empty : lockedBy).Trim(), LogLevel.Error));
				}
				finally
				{
					//Re-set the read-only flag if the file exists (i.e. there was an error)
					if (isReadOnly && info.Exists && !info.IsReadOnly)
						info.IsReadOnly = isReadOnly;
				}
			}

			//If the user requested a folder removal, do it.
			if (target is FolderTarget)
			{
				ProgressManager step = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(step,
					0.0f, S._("Removing folders...")));
				
				//Remove all subfolders which are empty.
				FolderTarget fldr = (FolderTarget)target;
				FileSystem fsManager = FileSystemManager.Get(VolumeInfo.FromMountpoint(fldr.Path));
				Action<DirectoryInfo> eraseEmptySubFolders = null;
				eraseEmptySubFolders = delegate(DirectoryInfo info)
				{
					 foreach (DirectoryInfo subDir in info.GetDirectories())
						 eraseEmptySubFolders(subDir);
					 task.OnProgressChanged(target,
						 new ProgressChangedEventArgs(step,
							 new TaskProgressChangedEventArgs(info.FullName, 0, 0)));

					 FileSystemInfo[] files = info.GetFileSystemInfos();
					 if (files.Length == 0)
						 fsManager.DeleteFolder(info);
				};
				eraseEmptySubFolders(new DirectoryInfo(fldr.Path));

				if (fldr.DeleteIfEmpty)
				{
					DirectoryInfo info = new DirectoryInfo(fldr.Path);
					task.OnProgressChanged(target,
						new ProgressChangedEventArgs(step,
							new TaskProgressChangedEventArgs(info.FullName, 0, 0)));

					//See if this is the root of a volume.
					bool isVolumeRoot = info.Parent == null;
					foreach (VolumeInfo volume in VolumeInfo.Volumes)
						foreach (string mountPoint in volume.MountPoints)
							if (info.FullName == mountPoint)
								isVolumeRoot = true;

					//If the folder is a mount point, then don't delete it. If it isn't,
					//search for files under the folder to see if it is empty.
					if (!isVolumeRoot && info.Exists && info.GetFiles("*", SearchOption.AllDirectories).Length == 0)
						fsManager.DeleteFolder(info);
				}
			}

			//If the user was erasing the recycle bin, clear the bin.
			if (target is RecycleBinTarget)
			{
				ProgressManager step = new ProgressManager();
				progress.Steps.Add(new SteppedProgressManager.Step(step,
					0.0f, S._("Emptying recycle bin...")));
				task.OnProgressChanged(target,
					new ProgressChangedEventArgs(step,
						new TaskProgressChangedEventArgs(string.Empty, 0, 0)));

				ShellApi.EmptyRecycleBin(EmptyRecycleBinOptions.NoConfirmation |
					EmptyRecycleBinOptions.NoProgressUI | EmptyRecycleBinOptions.NoSound);
			}

			target.Progress = null;
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
					new BinaryFormatter().Serialize(stream, list);
			}

			public override void LoadFromStream(Stream stream)
			{
				//Load the list into the dictionary
				StreamingContext context = new StreamingContext(
					StreamingContextStates.All, Owner);
				BinaryFormatter formatter = new BinaryFormatter(null, context);
				List<Task> deserialised = (List<Task>)formatter.Deserialize(stream);
				list.AddRange(deserialised);

				foreach (Task task in deserialised)
				{
					Owner.OnTaskAdded(new TaskEventArgs(task));
					if (task.Schedule is RecurringSchedule)
						Owner.ScheduleTask(task);
				}
			}

			/// <summary>
			/// The data store for this object.
			/// </summary>
			private List<Task> list = new List<Task>();
		}
	}
}