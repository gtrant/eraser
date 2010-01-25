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
using System.Linq;
using System.Text;

namespace Eraser.Manager
{
	/// <summary>
	/// Manages the progress for any operation.
	/// </summary>
	public abstract class ProgressManagerBase
	{
		/// <summary>
		/// Constructor.
		/// 
		/// This sets the starting time of this task to allow the computation of
		/// the estimated end time by extrapolating collected data based on the
		/// amount of time already elapsed.
		/// </summary>
		public ProgressManagerBase()
		{
			StartTime = DateTime.Now;
		}

		/// <summary>
		/// Resets the starting time of the task. The speed measurement is
		/// automatically started when the ProgressManagerBase object is created.
		/// </summary>
		public void Restart()
		{
			StartTime = DateTime.Now;
		}

		/// <summary>
		/// Gets the percentage of the operation completed.
		/// </summary>
		public abstract float Progress
		{
			get;
		}

		/// <summary>
		/// Computes the speed of the erase, in units of completion per second,
		/// based on the information collected in the previous 15 seconds.
		/// </summary>
		public abstract int Speed
		{
			get;
		}

		/// <summary>
		/// Calculates the estimated amount of time left based on the total
		/// amount of information to erase and the current speed of the erase
		/// </summary>
		public abstract TimeSpan TimeLeft
		{
			get;
		}

		/// <summary>
		/// The starting time of this task.
		/// </summary>
		public DateTime StartTime
		{
			get;
			private set;
		}
	}

	/// <summary>
	/// Manages progress based only on one input, set through the Completed and Total
	/// properties.
	/// </summary>
	public class ProgressManager : ProgressManagerBase
	{
		/// <summary>
		/// Marks this task as complete.
		/// </summary>
		public void MarkComplete()
		{
			if (total == 0)
				completed = total = 1;
			else
				completed = total;
		}

		/// <summary>
		/// Gets or sets the number of work units already completed.
		/// </summary>
		public long Completed
		{
			get
			{
				return completed;
			}
			set
			{
				if (value > Total)
					throw new ArgumentOutOfRangeException("value", value, "The Completed " +
						"property of the Progress Manager cannot exceed the total work units for " +
						"the task.");

				completed = value;
			}
		}

		/// <summary>
		/// Gets or sets the total number of work units that this task has.
		/// </summary>
		public long Total
		{
			get
			{
				return total;
			}
			set
			{
				if (value < Completed)
					throw new ArgumentOutOfRangeException("value", value, "The Total property " +
						"of the Progress Manager must be greater than or equal to the completed " +
						"work units for the task.");

				total = value;
			}
		}

		public override float Progress
		{
			get
			{
				if (Total == 0)
					return 0.0f;

				return (float)((double)Completed / Total);
			}
		}

		public override int Speed
		{
			get
			{
				if (DateTime.Now == StartTime)
					return 0;

				if ((DateTime.Now - lastSpeedCalc).Seconds < 5 && lastSpeed != 0)
					return lastSpeed;

				//Calculate how much time has passed
				double timeElapsed = (DateTime.Now - lastSpeedCalc).TotalSeconds;
				if (timeElapsed == 0.0)
					return 0;

				//Then compute the speed of the calculation
				lastSpeed = (int)((Completed - lastCompleted) / timeElapsed);
				lastSpeedCalc = DateTime.Now;
				lastCompleted = Completed;
				return lastSpeed;
			}
		}

		public override TimeSpan TimeLeft
		{
			get
			{
				if (Speed == 0)
					return TimeSpan.Zero;
				return new TimeSpan(0, 0, (int)((Total - Completed) / Speed));
			}
		}

		/// <summary>
		/// The last time a speed calculation was computed so that speed is not
		/// computed too often.
		/// </summary>
		private DateTime lastSpeedCalc;

		/// <summary>
		/// The amount of the operation completed at the last speed computation.
		/// </summary>
		private long lastCompleted;

		/// <summary>
		/// The last calculated speed of the operation.
		/// </summary>
		private int lastSpeed;

		/// <summary>
		/// The backing field for <see cref="Completed"/>
		/// </summary>
		private long completed;

		/// <summary>
		/// The backing field for <see cref="Total"/>
		/// </summary>
		private long total;
	}

	/// <summary>
	/// Manages progress based on sub-tasks.
	/// </summary>
	public abstract class ChainedProgressManager : ProgressManagerBase
	{
	}

	/// <summary>
	/// Manages progress based on sub-tasks, taking each sub-task to be a step
	/// in which the next step will not be executed until the current step is
	/// complete. Each step is also assign weights so that certain steps which
	/// take more time are given a larger amount of progress-bar space for finer
	/// grained progress reporting.
	/// </summary>
	public class SteppedProgressManager : ChainedProgressManager
	{
		/// <summary>
		/// Represents one step in the list of steps to complete.
		/// </summary>
		public class Step
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="progress">The <see cref="ProgressManagerBase"/> instance
			/// which measures the progress of this step.</param>
			/// <param name="weight">The weight of this step. The weight is a decimal
			/// number in the range [0.0, 1.0] which represents the percentage of the
			/// entire process this particular step is.</param>
			public Step(ProgressManagerBase progress, float weight)
				: this(progress, weight, null)
			{
			}

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="progress">The <see cref="ProgressManagerBase"/> instance
			/// which measures the progress of this step.</param>
			/// <param name="weight">The weight of this step. The weight is a decimal
			/// number in the range [0.0, 1.0] which represents the percentage of the
			/// entire process this particular step is.</param>
			/// <param name="name">A user-specified value of the name of this step.
			/// This value is not used by the class at all.</param>
			public Step(ProgressManagerBase progress, float weight, string name)
			{
				Progress = progress;
				Weight = weight;
				Name = name;
			}

			/// <summary>
			/// The <see cref="ProgressManagerBase"/> instance which measures the
			/// progress of the step.
			/// </summary>
			public ProgressManagerBase Progress
			{
				get;
				set;
			}

			/// <summary>
			/// The weight associated with this step.
			/// </summary>
			public float Weight
			{
				get;
				private set;
			}

			/// <summary>
			/// The name of this step.
			/// </summary>
			public string Name
			{
				get;
				set;
			}
		}

		/// <summary>
		/// The class which manages the steps which comprise the overall progress.
		/// </summary>
		public class StepsList : IList<Step>
		{
			public StepsList(SteppedProgressManager manager)
			{
				List = new List<Step>();
				ListLock = manager.ListLock;
				Manager = manager;
			}

			#region IList<Step> Members

			public int IndexOf(Step item)
			{
				lock (ListLock)
					return List.IndexOf(item);
			}

			public void Insert(int index, Step item)
			{
				lock (ListLock)
				{
					List.Insert(index, item);
					TotalWeights += item.Weight;
				}
			}

			public void RemoveAt(int index)
			{
				lock (ListLock)
				{
					TotalWeights -= List[index].Weight;
					List.RemoveAt(index);
				}
			}

			public Step this[int index]
			{
				get
				{
					lock (ListLock)
						return List[index];
				}
				set
				{
					lock (ListLock)
					{
						TotalWeights -= List[index].Weight;
						List[index] = value;
						TotalWeights += value.Weight;
					}
				}
			}

			#endregion

			#region ICollection<Step> Members

			public void Add(Step item)
			{
				lock (ListLock)
				{
					List.Add(item);
					TotalWeights += item.Weight;
				}
			}

			public void Clear()
			{
				lock (ListLock)
				{
					List.Clear();
					TotalWeights = 0;
				}
			}

			public bool Contains(Step item)
			{
				lock (ListLock)
					return List.Contains(item);
			}

			public void CopyTo(Step[] array, int arrayIndex)
			{
				lock (ListLock)
					List.CopyTo(array, arrayIndex);
			}

			public int Count
			{
				get
				{
					lock (ListLock) 
						return List.Count;
				}
			}

			public bool IsReadOnly
			{
				get { return false; }
			}

			public bool Remove(Step item)
			{
				int index = List.IndexOf(item);
				if (index != -1)
					TotalWeights -= List[index].Weight;

				return List.Remove(item);
			}

			#endregion

			#region IEnumerable<Step> Members

			public IEnumerator<Step> GetEnumerator()
			{
				return List.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return List.GetEnumerator();
			}

			#endregion

			/// <summary>
			/// The total weights of all the steps.
			/// </summary>
			private float TotalWeights
			{
				get
				{
					return totalWeights;
				}
				set
				{
					if (value >= 1.1f || value < 0.0f)
						throw new ArgumentOutOfRangeException("The total weights of all steps in " +
							"the task must be within the range [0.0, 1.0]");

					totalWeights = value;
				}
			}

			/// <summary>
			/// The list storing the steps for this instance.
			/// </summary>
			private List<Step> List;

			/// <summary>
			/// The lock object guarding the list against parallel writes.
			/// </summary>
			private object ListLock;

			/// <summary>
			/// The <see cref="SteppedProgressManager"/> instance which owns this list.
			/// </summary>
			private SteppedProgressManager Manager;

			/// <summary>
			/// The backing variable for the total weights of all the steps.
			/// </summary>
			private float totalWeights;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SteppedProgressManager()
		{
			ListLock = new object();
			Steps = new StepsList(this);
		}

		public override float Progress
		{
			get
			{
				float result = 0.0f;
				lock (ListLock)
					foreach (Step step in Steps)
						result += step.Progress.Progress * step.Weight;

				return result;
			}
		}

		public override int Speed
		{
			get
			{
				if (CurrentStep == null)
					return 0;

				return CurrentStep.Progress.Speed;
			}
		}

		public override TimeSpan TimeLeft
		{
			get
			{
				long ticksElapsed = (DateTime.Now - StartTime).Ticks;
				float progressRemaining = 1.0f - Progress;
				return new TimeSpan((long)
					(progressRemaining * (ticksElapsed / (double)Progress)));
			}
		}

		/// <summary>
		/// The list of steps involved in completion of the task.
		/// </summary>
		public StepsList Steps
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the current step which is executing. This property is null if
		/// no steps are executing (also when the task is complete)
		/// </summary>
		public Step CurrentStep
		{
			get
			{
				lock (ListLock)
				{
					if (Steps.Count == 0)
						return null;

					foreach (Step step in Steps)
						if (step.Progress.Progress < 1.0f)
							return step;

					//Return the last step since we don't have any
					return Steps[Steps.Count - 1];
				}
			}
		}

		/// <summary>
		/// The lock object guarding the list of steps against concurrent read and write.
		/// </summary>
		private object ListLock;
	}

	/// <summary>
	/// Manages progress based on sub-tasks, assuming each sub-task to be independent
	/// of the rest.
	/// </summary>
	public class ParallelProgressManager : ChainedProgressManager
	{
		/// <summary>
		/// The class which manages the progress of each dependent task.
		/// </summary>
		public class SubTasksList : IList<ProgressManagerBase>
		{
			public SubTasksList(ParallelProgressManager manager)
			{
				List = new List<ProgressManagerBase>();
				ListLock = manager.TaskLock;
			}

			#region IList<SubTasksList> Members

			public int IndexOf(ProgressManagerBase item)
			{
				lock (ListLock)
					return List.IndexOf(item);
			}

			public void Insert(int index, ProgressManagerBase item)
			{
				lock (ListLock)
					List.Insert(index, item);
			}

			public void RemoveAt(int index)
			{
				lock (ListLock)
					List.RemoveAt(index);
			}

			public ProgressManagerBase this[int index]
			{
				get
				{
					lock (ListLock)
						return List[index];
				}
				set
				{
					lock (ListLock)
						List[index] = value;
				}
			}

			#endregion

			#region ICollection<SteppedProgressManagerStep> Members

			public void Add(ProgressManagerBase item)
			{
				lock (ListLock)
					List.Add(item);
			}

			public void Clear()
			{
				lock (ListLock)
					List.Clear();
			}

			public bool Contains(ProgressManagerBase item)
			{
				return List.Contains(item);
			}

			public void CopyTo(ProgressManagerBase[] array, int arrayIndex)
			{
				lock (ListLock)
					List.CopyTo(array, arrayIndex);
			}

			public int Count
			{
				get
				{
					lock (ListLock) 
						return List.Count;
				}
			}

			public bool IsReadOnly
			{
				get { return false; }
			}

			public bool Remove(ProgressManagerBase item)
			{
				lock (ListLock)
					return List.Remove(item);
			}

			#endregion

			#region IEnumerable<ProgressManagerBase> Members

			public IEnumerator<ProgressManagerBase> GetEnumerator()
			{
				return List.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return List.GetEnumerator();
			}

			#endregion

			/// <summary>
			/// The list storing the steps for this instance.
			/// </summary>
			private List<ProgressManagerBase> List;

			/// <summary>
			/// The lock object guarding the list from concurrent read/writes.
			/// </summary>
			private object ListLock;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ParallelProgressManager()
		{
			Tasks = new SubTasksList(this);
			TaskLock = new object();
		}

		public override float Progress
		{
			get
			{
				float result = 0.0f;
				lock (TaskLock)
					foreach (ProgressManagerBase subTask in Tasks)
						result += subTask.Progress * (1.0f / Tasks.Count);

				return result;
			}
		}

		public override int Speed
		{
			get
			{
				int maxSpeed = 0;
				lock (TaskLock)
					foreach (ProgressManagerBase subTask in Tasks)
						maxSpeed = Math.Max(subTask.Speed, maxSpeed);

				return maxSpeed;
			}
		}

		public override TimeSpan TimeLeft
		{
			get
			{
				TimeSpan maxTime = TimeSpan.MinValue;
				lock (TaskLock)
					foreach (ProgressManagerBase subTask in Tasks)
						if (maxTime < subTask.TimeLeft)
							maxTime = subTask.TimeLeft;

				return maxTime;
			}
		}

		/// <summary>
		/// Gets the list of tasks which must complete execution before the task
		/// is completed.
		/// </summary>
		public SubTasksList Tasks
		{
			get;
			private set;
		}

		/// <summary>
		/// The lock object guarding the list of tasks against concurrent read and write.
		/// </summary>
		private object TaskLock;
	}

	/// <summary>
	/// Provides data for the Eraser.Manager.ProgressChanged event.
	/// </summary>
	public class ProgressChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="progress">The ProgressManagerBase object that stores the progress
		/// for the given task.</param>
		/// <param name="userState">A client-specified state object.</param>
		public ProgressChangedEventArgs(ProgressManagerBase progress, object userState)
		{
			Progress = progress;
			UserState = userState;
		}

		/// <summary>
		/// The ProgressManagerBase object that stores the progress for the given
		/// task.
		/// </summary>
		public ProgressManagerBase Progress { get; private set; }

		/// <summary>
		/// A client-specified state object.
		/// </summary>
		public object UserState { get; private set; }
	}

	/// <summary>
	/// Represents the method that will handle the ProgressChanged event from
	/// the <see cref="ProgressManagerBase"/> class.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="ProgressChangedEventArgs"/> event that
	/// stores the event data.</param>
	public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
}
