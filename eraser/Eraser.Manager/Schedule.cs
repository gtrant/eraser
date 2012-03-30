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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Permissions;
using System.Globalization;
using Eraser.Util;

namespace Eraser.Manager
{
	/// <summary>
	/// Base class for all schedule types.
	/// </summary>
	[Serializable]
	public abstract class Schedule : ISerializable, IXmlSerializable
	{
		#region IXmlSerializable Members

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public abstract void ReadXml(XmlReader reader);

		public abstract void WriteXml(XmlWriter writer);

		#endregion

		#region Default values
		[Serializable]
		private class RunManuallySchedule : Schedule
		{
			#region Object serialization
			public RunManuallySchedule(SerializationInfo info, StreamingContext context)
			{
			}

			[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
			}

			public override void ReadXml(XmlReader reader)
			{
				if (reader.GetAttribute("type") != "Manual")
					throw new InvalidDataException();
				reader.Read();
			}

			public override void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString("type", "Manual");
			}
			#endregion

			public RunManuallySchedule()
			{
			}

			public override string ToString()
			{
				return string.Empty;
			}
		}

		[Serializable]
		private class RunNowSchedule : Schedule
		{
			#region Object serialization
			public RunNowSchedule(SerializationInfo info, StreamingContext context)
			{
			}

			[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
			}

			public override void ReadXml(XmlReader reader)
			{
				if (reader.GetAttribute("type") != "Now")
					throw new InvalidDataException();
				reader.Read();
			}

			public override void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString("type", "Now");
			}
			#endregion

			public RunNowSchedule()
			{
			}

			public override string ToString()
			{
				return string.Empty;
			}
		}

		[Serializable]
		private class RunOnRestartSchedule : Schedule
		{
			#region Object serialization
			public RunOnRestartSchedule(SerializationInfo info, StreamingContext context)
			{
			}

			[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
			}

			public override void ReadXml(XmlReader reader)
			{
				if (reader.GetAttribute("type") != "Restart")
					throw new InvalidDataException();
				reader.Read();
			}

			public override void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString("type", "Restart");
			}
			#endregion

			public RunOnRestartSchedule()
			{
			}

			public override string ToString()
			{
				return S._("Running on restart");
			}
		}
		#endregion

		/// <summary>
		/// Retrieves the text that should be displayed detailing the nature of
		/// the schedule for use in user interface elements.
		/// </summary>
		public abstract string ToString();

		/// <summary>
		/// The owner of this schedule item.
		/// </summary>
		public Task Owner
		{
			get;
			internal set;
		}

		/// <summary>
		/// Populates a SerializationInfo with the data needed to serialize the
		/// target object.
		/// </summary>
		/// <param name="info">The SerializationInfo to populate with data.</param>
		/// <param name="context">The destination for this serialization.</param>
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

		/// <summary>
		/// The global value for tasks which should be run manually.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly Schedule RunManually = new RunManuallySchedule();

		/// <summary>
		/// The global value for tasks which should be run immediately.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly Schedule RunNow = new RunNowSchedule();

		/// <summary>
		/// The global value for tasks which should be run when the computer is
		/// restarted
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly Schedule RunOnRestart = new RunOnRestartSchedule();
	}

	/// <summary>
	/// Recurring runs schedule type.
	/// </summary>
	[Serializable]
	public class RecurringSchedule : Schedule
	{
		#region Overridden members
		public override string ToString()
		{
			string result = string.Empty;
			switch (type)
			{
				case RecurringScheduleUnit.Daily:
					if (frequency != 1)
						result = S._("Once every {0} days", frequency);
					else
						result = S._("Once every day");
					break;
				case RecurringScheduleUnit.Weekdays:
					result = S._("Every weekday");
					break;
				case RecurringScheduleUnit.Weekly:
					if ((weeklySchedule & DaysOfWeek.Monday) != 0)
						result = S._("Every Monday, {0}");
					if ((weeklySchedule & DaysOfWeek.Tuesday) != 0)
						result += S._("Every Tuesday, {0}");
					if ((weeklySchedule & DaysOfWeek.Wednesday) != 0)
						result += S._("Every Wednesday, {0}");
					if ((weeklySchedule & DaysOfWeek.Thursday) != 0)
						result += S._("Every Thursday, {0}");
					if ((weeklySchedule & DaysOfWeek.Friday) != 0)
						result += S._("Every Friday, {0}");
					if ((weeklySchedule & DaysOfWeek.Saturday) != 0)
						result += S._("Every Saturday, {0}");
					if ((weeklySchedule & DaysOfWeek.Sunday) != 0)
						result += S._("Every Sunday, {0}");

					result = string.Format(CultureInfo.CurrentCulture, result,
						frequency == 1 ?
							S._("once every {0} week.", frequency) :
							S._("once every {0} weeks.", frequency));
					break;
				case RecurringScheduleUnit.Monthly:
					if (frequency == 1)
						result = S._("On day {0} of every month", monthlySchedule);
					else
						result = S._("On day {0} of every {1} months", monthlySchedule,
							frequency);
					break;
			}

			return result + S._(", at {0}", executionTime.TimeOfDay.ToString());
		}
		#endregion

		#region Object serialization
		protected RecurringSchedule(SerializationInfo info, StreamingContext context)
		{
			type = (RecurringScheduleUnit)info.GetValue("Type", typeof(RecurringScheduleUnit));
			frequency = (int)info.GetValue("Frequency", typeof(int));
			executionTime = (DateTime)info.GetValue("ExecutionTime", typeof(DateTime));
			weeklySchedule = (DaysOfWeek)info.GetValue("WeeklySchedule", typeof(DaysOfWeek));
			monthlySchedule = (int)info.GetValue("MonthlySchedule", typeof(int));

			LastRun = (DateTime)info.GetDateTime("LastRun");
			NextRunCache = (DateTime)info.GetDateTime("NextRun");
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Type", type);
			info.AddValue("Frequency", frequency);
			info.AddValue("ExecutionTime", executionTime);
			info.AddValue("WeeklySchedule", weeklySchedule);
			info.AddValue("MonthlySchedule", monthlySchedule);
			info.AddValue("LastRun", LastRun);
			info.AddValue("NextRun", NextRunCache);
		}

		public override void ReadXml(XmlReader reader)
		{
			if (!Enum.TryParse<RecurringScheduleUnit>(reader.GetAttribute("type"), out type))
				throw new InvalidDataException();
			if (!int.TryParse(reader.GetAttribute("frequency"), out frequency))
				throw new InvalidDataException();
			if (!DateTime.TryParse(reader.GetAttribute("executionTime"), out executionTime))
				throw new InvalidDataException();
			if (!Enum.TryParse<DaysOfWeek>(reader.GetAttribute("weeklySchedule"),
				out weeklySchedule))
				throw new InvalidDataException();
			if (!int.TryParse(reader.GetAttribute("monthlySchedule"), out monthlySchedule))
				throw new InvalidDataException();

			DateTime lastRun;
			DateTime nextRunCache;
			if (!DateTime.TryParse(reader.GetAttribute("lastRun"), out lastRun))
				throw new InvalidDataException();
			if (!DateTime.TryParse(reader.GetAttribute("nextRun"), out nextRunCache))
				throw new InvalidDataException();
			LastRun = lastRun;
			NextRunCache = nextRunCache;
			reader.Read();
		}

		public override void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("type", type.ToString());
			writer.WriteAttributeString("frequency", frequency.ToString());
			writer.WriteAttributeString("executionTime", executionTime.ToString("O"));
			writer.WriteAttributeString("weeklySchedule", weeklySchedule.ToString());
			writer.WriteAttributeString("monthlySchedule", monthlySchedule.ToString());
			writer.WriteAttributeString("lastRun", LastRun.ToString("O"));
			writer.WriteAttributeString("nextRun", NextRunCache.ToString("O"));
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecurringSchedule()
		{
		}

		/// <summary>
		/// The type of schedule.
		/// </summary>
		public RecurringScheduleUnit ScheduleType
		{
			get { return type; }
			set
			{
				type = value;
				if (Owner != null)
					Owner.OnTaskEdited();
			}
		}

		/// <summary>
		/// The frequency of the event. This value is valid only with Daily,
		/// Weekly and Monthly schedules.
		/// </summary>
		public int Frequency
		{
			get
			{
				if (ScheduleType != RecurringScheduleUnit.Daily && ScheduleType != RecurringScheduleUnit.Weekly &&
					ScheduleType != RecurringScheduleUnit.Monthly)
					throw new InvalidOperationException("The ScheduleUnit of the schedule " +
						"does not require a frequency value, this field would contain garbage.");

				return frequency;
			}
			set
			{
				if (value == 0)
					throw new ArgumentException(S._("The frequency of the recurrence should " +
						"be greater than one"));

				frequency = value;
				if (Owner != null)
					Owner.OnTaskEdited();
			}
		}

		/// <summary>
		/// The time of day where the task will be executed.
		/// </summary>
		public DateTime ExecutionTime
		{
			get { return executionTime; }
			set
			{
				executionTime = value;
				if (Owner != null)
					Owner.OnTaskEdited();
			}
		}

		/// <summary>
		/// The days of the week which this task should be run. This is valid only
		/// with Weekly schedules. This field is the DaysOfWeek enumerations
		/// ORed together.
		/// </summary>
		public DaysOfWeek WeeklySchedule
		{
			get
			{
				if (ScheduleType != RecurringScheduleUnit.Weekly)
					throw new InvalidOperationException("The ScheduleUnit of the schedule " +
						"does not require the WeeklySchedule value, this field would contain garbage");

				return weeklySchedule;
			}
			set
			{
				if (value == 0)
					throw new ArgumentException(S._("The WeeklySchedule should have at " +
						"least one day where the task should be run."));

				weeklySchedule = value;
				if (Owner != null)
					Owner.OnTaskEdited();
			}
		}

		/// <summary>
		/// The nth day of the month on which this task will run. This is valid
		/// only with Monthly schedules
		/// </summary>
		public int MonthlySchedule
		{
			get
			{
				if (ScheduleType != RecurringScheduleUnit.Monthly)
					throw new InvalidOperationException("The ScheduleUnit of the schedule does " +
						"not require the MonthlySchedule value, this field would contain garbage");

				return monthlySchedule;
			}
			set
			{
				monthlySchedule = value;

				if (Owner != null)
					Owner.OnTaskEdited();
			}
		}

		/// <summary>
		/// The last time this task was executed. This value is used for computing
		/// the next time the task should be run.
		/// </summary>
		public DateTime LastRun
		{
			get;
			private set;
		}

		/// <summary>
		/// Computes the next run time based on the last run time, the current
		/// schedule, and the current time. The timestamp returned will be the next
		/// time from now which fulfils the schedule.
		/// </summary>
		public DateTime NextRun
		{
			get
			{
				//Get a good starting point, either now, or the last time the task
				//was run.
				DateTime nextRun = LastRun;
				if (nextRun == DateTime.MinValue)
					nextRun = DateTime.Now;
				nextRun = new DateTime(nextRun.Year, nextRun.Month, nextRun.Day, executionTime.Hour,
					executionTime.Minute, executionTime.Second);

				switch (ScheduleType)
				{
					case RecurringScheduleUnit.Daily:
					{
						//First assume that it is today that we are running the schedule
						long daysToAdd = (DateTime.Now - nextRun).Days;
						nextRun = nextRun.AddDays(daysToAdd);

						//If we have passed today's run time, schedule it after the next
						//frequency
						if (nextRun < DateTime.Now)
							nextRun = nextRun.AddDays(frequency);
						break;
					}
					case RecurringScheduleUnit.Weekdays:
					{
						while (nextRun < DateTime.Now ||
							LastRun.DayOfWeek == DayOfWeek.Saturday ||
							LastRun.DayOfWeek == DayOfWeek.Sunday)
							nextRun = nextRun.AddDays(1);
						break;
					}
					case RecurringScheduleUnit.Weekly:
					{
						if (weeklySchedule == 0)
							break;

						//Find the next eligible day to run the task within this week.
						do
						{
							if (CanRunOnDay(nextRun) && nextRun >= DateTime.Now)
								break;
							nextRun = nextRun.AddDays(1);
						}
						while (nextRun.DayOfWeek < DayOfWeek.Saturday);

						while (nextRun < DateTime.Now || !CanRunOnDay(nextRun))
						{
							//Find the next eligible day to run the task
							nextRun = nextRun.AddDays(7 * (frequency - 1));
							for (int daysInWeek = 7; daysInWeek-- != 0; nextRun = nextRun.AddDays(1))
							{
								if (CanRunOnDay(nextRun) && nextRun >= DateTime.Now)
									break;
							}
						}

						break;
					}
					case RecurringScheduleUnit.Monthly:
						if (LastRun == DateTime.MinValue)
						{
							//Since the schedule has never been used, find the next time
							//to run the task. If the current day is less than the
							//scheduled day, leave it alone. Otherwise, go to the
							//following month.
							while (monthlySchedule < nextRun.Day)
								nextRun = nextRun.AddDays(1);
						}
						else
						{
							//Step the number of months since the last run
							while (nextRun < DateTime.Now)
								nextRun = nextRun.AddMonths(frequency);
						}

						//Set the day of the month which the task is supposed to run.
						nextRun = nextRun.AddDays(monthlySchedule - nextRun.Day);
						break;
				}

				return nextRun;
			}
		}

		/// <summary>
		/// Gets whether the previous run was missed.
		/// </summary>
		public bool MissedPreviousSchedule
		{
			get
			{
				return LastRun != DateTime.MinValue && NextRun != NextRunCache;
			}
		}

		/// <summary>
		/// Returns true if the task can run on the given date. Applies only for
		/// weekly tasks.
		/// </summary>
		/// <param name="date">The date to run on.</param>
		/// <returns>True if the task will be run on the date.</returns>
		private bool CanRunOnDay(DateTime date)
		{
			if (ScheduleType != RecurringScheduleUnit.Weekly)
				throw new ArgumentException("The ScheduleUnit of the schedule does " +
					"not use the WeeklySchedule value, this field would contain garbage");
			return ((int)weeklySchedule & (1 << (int)date.DayOfWeek)) != 0;
		}

		/// <summary>
		/// Reschedules the task.
		/// </summary>
		/// <param name="lastRun">The last time the task was run.</param>
		internal void Reschedule(DateTime lastRun)
		{
			LastRun = lastRun;
			NextRunCache = NextRun;
		}

		private RecurringScheduleUnit type;
		private int frequency;
		private DateTime executionTime;
		private DaysOfWeek weeklySchedule;
		private int monthlySchedule;

		/// <summary>
		/// The next time the task is scheduled to run - this is cached from the previous
		/// calculation of the next run time to determine if the task's schedule was missed
		/// </summary>
		private DateTime NextRunCache;
	}

	/// <summary>
	/// The types of schedule
	/// </summary>
	public enum RecurringScheduleUnit
	{
		/// <summary>
		/// Daily schedule type
		/// </summary>
		Daily,

		/// <summary>
		/// Weekdays-only schedule type
		/// </summary>
		Weekdays,

		/// <summary>
		/// Weekly schedule type
		/// </summary>
		Weekly,

		/// <summary>
		/// Monthly schedule type
		/// </summary>
		Monthly
	}

	/// <summary>
	/// The days of the week, with values usable in a bitfield.
	/// </summary>
	[Flags]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
	public enum DaysOfWeek
	{
		None = 0,
		Sunday = 1 << DayOfWeek.Sunday,
		Monday = 1 << DayOfWeek.Monday,
		Tuesday = 1 << DayOfWeek.Tuesday,
		Wednesday = 1 << DayOfWeek.Wednesday,
		Thursday = 1 << DayOfWeek.Thursday,
		Friday = 1 << DayOfWeek.Friday,
		Saturday = 1 << DayOfWeek.Saturday
	}
}
