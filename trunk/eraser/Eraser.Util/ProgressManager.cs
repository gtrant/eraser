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

namespace Eraser.Util
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
		protected ProgressManagerBase()
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
			lock (Speeds)
				Speeds.Reset();
		}

		/// <summary>
		/// Gets the percentage of the operation completed.
		/// </summary>
		/// <remarks>If the <see cref="ProgressIndeterminate"/> property is true, this
		/// property will return <see cref="System.Float.NaN"/>.</remarks>
		public abstract float Progress
		{
			get;
		}

		/// <summary>
		/// Gets whether the current progress is undefined.
		/// </summary>
		public abstract bool ProgressIndeterminate
		{
			get;
		}

		/// <summary>
		/// Computes the speed of the erase, in percentage of completion per second,
		/// based on the information collected in the previous 15 seconds.
		/// </summary>
		public abstract float Speed
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

		/// <summary>
		/// Samples the current speed of the task.
		/// </summary>
		/// <param name="speed">The speed of the task.</param>
		protected void SampleSpeed(float speed)
		{
			lock (Speeds)
			{
				IList<KeyValuePair<DateTime, double>> outliers = Speeds.GetOutliers(0.95);
				if (outliers != null)
				{
					List<KeyValuePair<DateTime, double>> recentOutliers = outliers.Where(
						sample => (DateTime.Now - sample.Key).TotalSeconds <= 60).ToList();
					if (recentOutliers.Count >= 5)
					{
						Speeds.Reset();
						recentOutliers.ForEach(sample => Speeds.Add(sample.Value));
					}
				}

				Speeds.Add(speed);
				PredictedSpeed = Speeds.Predict(0.95);
			}
		}

		/// <summary>
		/// Predicts the speed of the operation, given the current samples.
		/// </summary>
		protected Interval PredictedSpeed
		{
			get;
			private set;
		}

		/// <summary>
		/// The speed sampler.
		/// </summary>
		private Sampler Speeds = new Sampler();
	}

	/// <summary>
	/// Manages progress based only on one input, set through the Completed and Total
	/// properties.
	/// </summary>
	public class ProgressManager : ProgressManagerBase
	{
		/// <summary>
		/// Marks this task's progress as indeterminate.
		/// </summary>
		public void MarkIndeterminate()
		{
			progressIndeterminate = true;
		}

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
				else if (ProgressIndeterminate)
					return float.NaN;

				return (float)((double)Completed / Total);
			}
		}

		public override bool ProgressIndeterminate
		{
			get
			{
				return progressIndeterminate;
			}
		}

		/// <summary>
		/// Stores whether the progress of the current task cannot be determined.
		/// </summary>
		/// <see cref="ProgressIndeterminate"/>
		private bool progressIndeterminate;

		public override float Speed
		{
			get
			{
				if ((DateTime.Now - lastSpeedCalc).Seconds <= 1 && lastSpeed != 0)
					return lastSpeed;

				//Calculate how much time has passed
				double timeElapsed = (DateTime.Now - lastSpeedCalc).TotalSeconds;
				if (timeElapsed == 0.0)
					return 0;

				//Then compute the speed of the calculation
				long progressDelta = Completed - lastCompleted;
				float currentSpeed = (float)(progressDelta / timeElapsed / total);
				lastSpeedCalc = DateTime.Now;
				lastCompleted = Completed;

				//We won't update the speed of the task if the current speed is within
				//the lower and upper prediction interval.
				Interval interval = PredictedSpeed;
				if (interval != null)
				{
					if (currentSpeed < interval.Minimum)
					{
						Restart();
						lastSpeed = currentSpeed;
					}
					else if (currentSpeed > interval.Maximum)
					{
						Restart();
						lastSpeed = currentSpeed;
					}
					else if (lastSpeed == 0.0f)
					{
						lastSpeed = currentSpeed;
					}
				}

				SampleSpeed(currentSpeed);
				return lastSpeed;
			}
		}

		public override TimeSpan TimeLeft
		{
			get
			{
				float speed = Speed;
				if (speed == 0.0)
					return TimeSpan.MinValue;

				return TimeSpan.FromSeconds((1.0f - Progress) / speed);
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
		private float lastSpeed;

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
		/// The class which manages the steps which comprise the overall progress.
		/// </summary>
		private class StepsList : IList<SteppedProgressManagerStep>
		{
			public StepsList(SteppedProgressManager manager)
			{
				List = new List<SteppedProgressManagerStep>();
				ListLock = manager.ListLock;
			}

			#region IList<SteppedProgressManagerStep> Members

			public int IndexOf(SteppedProgressManagerStep item)
			{
				lock (ListLock)
					return List.IndexOf(item);
			}

			public void Insert(int index, SteppedProgressManagerStep item)
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

			public SteppedProgressManagerStep this[int index]
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

			#region ICollection<SteppedProgressManagerStep> Members

			public void Add(SteppedProgressManagerStep item)
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

			public bool Contains(SteppedProgressManagerStep item)
			{
				lock (ListLock)
					return List.Contains(item);
			}

			public void CopyTo(SteppedProgressManagerStep[] array, int arrayIndex)
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

			public bool Remove(SteppedProgressManagerStep item)
			{
				int index = List.IndexOf(item);
				if (index != -1)
					TotalWeights -= List[index].Weight;

				return List.Remove(item);
			}

			#endregion

			#region IEnumerable<SteppedProgressManagerStep> Members

			public IEnumerator<SteppedProgressManagerStep> GetEnumerator()
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
						throw new ArgumentOutOfRangeException("value", "The total weights of " +
							"all steps in the task must be within the range [0.0, 1.0]");

					totalWeights = value;
				}
			}

			/// <summary>
			/// The list storing the steps for this instance.
			/// </summary>
			private List<SteppedProgressManagerStep> List;

			/// <summary>
			/// The lock object guarding the list against parallel writes.
			/// </summary>
			private object ListLock;

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
				lock (ListLock)
					return Steps.Sum(step => step.Progress.Progress * step.Weight);
			}
		}

		public override bool ProgressIndeterminate
		{
			get
			{
				lock (ListLock)
					return Steps.Any(x => x.Progress.ProgressIndeterminate);
			}
		}

		public override float Speed
		{
			get
			{
				if ((DateTime.Now - lastSpeedCalc).TotalSeconds < SpeedCalcInterval)
					return lastSpeed;

				//Calculate how much time has passed
				double timeElapsed = (DateTime.Now - lastSpeedCalc).TotalSeconds;

				//Then compute the speed of the calculation
				float currentProgress = Progress;
				float progressDelta = currentProgress - lastCompleted;
				float currentSpeed = (float)(progressDelta / timeElapsed);
				lastSpeedCalc = DateTime.Now;
				lastCompleted = Progress;

				//If the progress delta is zero, it usually means that the amount
				//completed within the calculation interval is too short -- lengthen
				//the interval so we can get a small difference, significant to make
				//a speed calculation. Likewise, if it is too great a difference,
				//we need to shorten the interval to get more accurate calculations
				if (progressDelta == 0.0)
					SpeedCalcInterval += SpeedCalcInterval / 3;
				else if (progressDelta > 0.01 && SpeedCalcInterval > 6)
					SpeedCalcInterval -= 3;

				//We won't update the speed of the task if the current speed is within
				//the lower and upper prediction interval.
				Interval interval = PredictedSpeed;
				if (interval != null)
				{
					if (currentSpeed < interval.Minimum)
					{
						Restart();
						lastSpeed = currentSpeed;
					}
					else if (currentSpeed > interval.Maximum)
					{
						Restart();
						lastSpeed = currentSpeed;
					}
					else if (lastSpeed == 0.0f)
					{
						lastSpeed = currentSpeed;
					}
				}

				SampleSpeed(currentSpeed);
				return lastSpeed;
			}
		}

		public override TimeSpan TimeLeft
		{
			get
			{
				float speed = Speed;
				float remaining = 1.0f - Progress;

				if (speed == 0)
					return TimeSpan.MinValue;
				else if (remaining <= 0)
					return TimeSpan.Zero;

				try
				{
					return TimeSpan.FromSeconds(remaining / speed);
				}
				catch (OverflowException)
				{
					return TimeSpan.MaxValue;
				}
			}
		}

		/// <summary>
		/// The list of steps involved in completion of the task.
		/// </summary>
		public IList<SteppedProgressManagerStep> Steps
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the current step which is executing. This property is null if
		/// no steps are executing (also when the task is complete)
		/// </summary>
		public SteppedProgressManagerStep CurrentStep
		{
			get
			{
				lock (ListLock)
				{
					if (Steps.Count == 0)
						return null;

					foreach (SteppedProgressManagerStep step in Steps)
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

		/// <summary>
		/// The amount of time elapsed before a new speed calculation is made.
		/// </summary>
		private int SpeedCalcInterval = 15;

		/// <summary>
		/// The last time a speed calculation was computed so that speed is not
		/// computed too often.
		/// </summary>
		private DateTime lastSpeedCalc;

		/// <summary>
		/// The amount of the operation completed at the last speed computation.
		/// </summary>
		private float lastCompleted;

		/// <summary>
		/// The last calculated speed of the operation.
		/// </summary>
		private float lastSpeed;
	}

	/// <summary>
	/// Represents one step in the list of steps to complete.
	/// </summary>
	public class SteppedProgressManagerStep
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="progress">The <see cref="ProgressManagerBase"/> instance
		/// which measures the progress of this step.</param>
		/// <param name="weight">The weight of this step. The weight is a decimal
		/// number in the range [0.0, 1.0] which represents the percentage of the
		/// entire process this particular step is.</param>
		public SteppedProgressManagerStep(ProgressManagerBase progress, float weight)
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
		public SteppedProgressManagerStep(ProgressManagerBase progress, float weight, string name)
		{
			if (float.IsInfinity(weight) || float.IsNaN(weight))
				throw new ArgumentException(S._("The weight of a progress manager step must be " +
					"a valid floatint-point value."));

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
	/// Manages progress based on sub-tasks, assuming each sub-task to be independent
	/// of the rest.
	/// </summary>
	public class ParallelProgressManager : ChainedProgressManager
	{
		/// <summary>
		/// The class which manages the progress of each dependent task.
		/// </summary>
		private class SubTasksList : IList<ProgressManagerBase>
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
				lock (TaskLock)
					return Tasks.Sum(task => task.Progress * (1.0f / Tasks.Count));
			}
		}

		public override bool ProgressIndeterminate
		{
			get
			{
				lock (TaskLock)
					return Tasks.Any(x => x.ProgressIndeterminate);
			}
		}

		public override float Speed
		{
			get
			{
				lock (TaskLock)
					return Tasks.Max(task => task.Speed);
			}
		}

		public override TimeSpan TimeLeft
		{
			get
			{
				lock (TaskLock)
					return Tasks.Max(task => task.TimeLeft);
			}
		}

		/// <summary>
		/// Gets the list of tasks which must complete execution before the task
		/// is completed.
		/// </summary>
		public IList<ProgressManagerBase> Tasks
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