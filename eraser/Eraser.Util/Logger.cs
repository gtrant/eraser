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
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Collections.ObjectModel;
using System.Threading;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Permissions;
using System.IO;

namespace Eraser.Util
{
	/// <summary>
	/// The levels of logging allowing for the filtering of messages.
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Informative messages.
		/// </summary>
		Information,

		/// <summary>
		/// Notice messages.
		/// </summary>
		Notice,

		/// <summary>
		/// Warning messages.
		/// </summary>
		Warning,

		/// <summary>
		/// Error messages.
		/// </summary>
		Error,

		/// <summary>
		/// Fatal errors.
		/// </summary>
		Fatal
	}

	/// <summary>
	/// Represents a log entry.
	/// </summary>
	[Serializable]
	public struct LogEntry : ISerializable, IXmlSerializable
	{
		#region Serialization code
		private LogEntry(SerializationInfo info, StreamingContext context)
			: this()
		{
			Level = (LogLevel)info.GetValue("Level", typeof(LogLevel));
			Timestamp = (DateTime)info.GetValue("Timestamp", typeof(DateTime));
			Message = (string)info.GetValue("Message", typeof(string));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Level", Level);
			info.AddValue("Timestamp", Timestamp);
			info.AddValue("Message", Message);
		}

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			LogLevel level;
			DateTime timestamp;
			if (!Enum.TryParse<LogLevel>(reader.GetAttribute("level"), out level))
				throw new InvalidDataException();
			if (!DateTime.TryParse(reader.GetAttribute("timestamp"), out timestamp))
				throw new InvalidDataException();

			Level = level;
			Timestamp = timestamp;
			Message = reader.ReadString();
			reader.Read();
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("level", Level.ToString());
			writer.WriteAttributeString("timestamp", Timestamp.ToString("O"));
			writer.WriteString(Message);
		}
		#endregion

		/// <summary>
		/// Creates a LogEntry structure.
		/// </summary>
		/// <param name="message">The log message.</param>
		/// <param name="level">The type of log entry.</param>
		public LogEntry(string message, LogLevel level)
			: this()
		{
			Message = message;
			Level = level;
			Timestamp = DateTime.Now;
		}

		/// <summary>
		/// The type of log entry.
		/// </summary>
		public LogLevel Level { get; private set; }

		/// <summary>
		/// The time which the message was logged.
		/// </summary>
		public DateTime Timestamp { get; private set; }

		/// <summary>
		/// The log message.
		/// </summary>
		public string Message { get; private set; }
	}

	/// <summary>
	/// Event Data for all Logger events.
	/// </summary>
	public class LogEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="entry">The log entry that was just logged.</param>
		public LogEventArgs(LogEntry entry)
		{
			LogEntry = entry;
		}

		/// <summary>
		/// The log entry that was just logged.
		/// </summary>
		public LogEntry LogEntry { get; private set; }
	}

	/// <summary>
	/// Provides a standard logging interface to the rest of the Eraser classes.
	/// </summary>
	public static class Logger
	{
		static Logger()
		{
			Listeners = new LogThreadDictionary();
		}

		/// <summary>
		/// Logs an informational message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public static void Log(string message)
		{
			Log(new LogEntry(message, LogLevel.Information));
		}

		/// <summary>
		/// Logs a message to the logger.
		/// </summary>
		/// <param name="message">The message to store.</param>
		/// <param name="level">The level of the message.</param>
		public static void Log(string message, LogLevel level)
		{
			Log(new LogEntry(message, level));
		}

		/// <summary>
		/// Logs the provided entry to the logger.
		/// </summary>
		/// <param name="entry">The log entry to store.</param>
		public static void Log(LogEntry entry)
		{
			Thread currentThread = Thread.CurrentThread;
			List<ILogTarget> targets = new List<ILogTarget>();

			if (Listeners.ContainsKey(currentThread))
			{
				LogThreadTargets threadTargets = Listeners[currentThread];
				if (threadTargets != null)
					targets.AddRange(threadTargets);
			}

			targets.ForEach(
				target => target.OnEventLogged(currentThread, new LogEventArgs(entry)));
		}

		/// <summary>
		/// The list of listeners for events on a particular thread.
		/// </summary>
		public static LogThreadDictionary Listeners { get; private set; }
	}

	/// <summary>
	/// The Logger Thread Dictionary, which maps log event listeners to threads.
	/// This mainly serves as a thread-safe Dictionary.
	/// </summary>
	public class LogThreadDictionary : IDictionary<Thread, LogThreadTargets>
	{
		#region IDictionary<Thread,LogThreadTargets> Members

		public void Add(Thread key, LogThreadTargets value)
		{
			lock (Dictionary)
				Dictionary.Add(key, value);
		}

		public bool ContainsKey(Thread key)
		{
			return Dictionary.ContainsKey(key);
		}

		public ICollection<Thread> Keys
		{
			get
			{
				lock (Dictionary)
				{
					Thread[] result = new Thread[Dictionary.Keys.Count];
					Dictionary.Keys.CopyTo(result, 0);

					return new ReadOnlyCollection<Thread>(result);
				}
			}
		}

		public bool Remove(Thread key)
		{
			lock (Dictionary)
				return Dictionary.Remove(key);
		}

		public bool TryGetValue(Thread key, out LogThreadTargets value)
		{
			lock (Dictionary)
				return Dictionary.TryGetValue(key, out value);
		}

		public ICollection<LogThreadTargets> Values
		{
			get
			{
				lock (Dictionary)
				{
					LogThreadTargets[] result =
						new LogThreadTargets[Dictionary.Values.Count];
					Dictionary.Values.CopyTo(result, 0);

					return new ReadOnlyCollection<LogThreadTargets>(result);
				}
			}
		}

		public LogThreadTargets this[Thread key]
		{
			get
			{
				lock (Dictionary)
					return Dictionary[key];
			}
			set
			{
				lock (Dictionary)
					Dictionary[key] = value;
			}
		}

		#endregion

		#region ICollection<KeyValuePair<Thread,LogThreadTargets>> Members

		public void Add(KeyValuePair<Thread, LogThreadTargets> item)
		{
			lock (Dictionary)
				Dictionary.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			lock (Dictionary)
				Dictionary.Clear();
		}

		public bool Contains(KeyValuePair<Thread, LogThreadTargets> item)
		{
			lock (Dictionary)
				return Dictionary.ContainsKey(item.Key) && Dictionary[item.Key] == item.Value;
		}

		public void CopyTo(KeyValuePair<Thread, LogThreadTargets>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get
			{
				lock (Dictionary)
					return Dictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<Thread, LogThreadTargets> item)
		{
			lock (Dictionary)
				return Dictionary.Remove(item.Key);
		}

		#endregion

		#region IEnumerable<KeyValuePair<Thread,LogThreadTargets>> Members

		public IEnumerator<KeyValuePair<Thread, LogThreadTargets>> GetEnumerator()
		{
			return Dictionary.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Dictionary.GetEnumerator();
		}

		#endregion

		/// <summary>
		/// The backing store for this dictionary.
		/// </summary>
		private Dictionary<Thread, LogThreadTargets> Dictionary =
			new Dictionary<Thread, LogThreadTargets>();
	}

	public class LogThreadTargets : IList<ILogTarget>
	{
		#region IList<ILogTarget> Members

		public int IndexOf(ILogTarget item)
		{
			lock (List)
				return List.IndexOf(item);
		}

		public void Insert(int index, ILogTarget item)
		{
			lock (List)
				List.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			lock (List)
				List.RemoveAt(index);
		}

		public ILogTarget this[int index]
		{
			get
			{
				lock (List)
					return List[index];
			}
			set
			{
				lock (List)
					List[index] = value;
			}
		}

		#endregion

		#region ICollection<ILogTarget> Members

		public void Add(ILogTarget item)
		{
			lock (List)
				List.Add(item);
		}

		public void Clear()
		{
			lock (List)
				List.Clear();
		}

		public bool Contains(ILogTarget item)
		{
			lock (List)
				return List.Contains(item);
		}

		public void CopyTo(ILogTarget[] array, int arrayIndex)
		{
			lock (List)
				List.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				lock (List)
					return List.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(ILogTarget item)
		{
			lock (List)
				return List.Remove(item);
		}

		#endregion

		#region IEnumerable<ILogTarget> Members

		public IEnumerator<ILogTarget> GetEnumerator()
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
		/// The backing store for this list.
		/// </summary>
		private List<ILogTarget> List = new List<ILogTarget>();
	}

	/// <summary>
	/// The logger target interface which all interested listeners of log events must
	/// implement.
	/// </summary>
	public interface ILogTarget
	{
		/// <summary>
		/// The handler for events.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The event data associated with the event.</param>
		void OnEventLogged(object sender, LogEventArgs e);

		/// <summary>
		/// Chains the provided target to the current target, so that when this
		/// target receives an event, the provided target is also executed.
		/// </summary>
		/// <param name="target">The target to chain with the current one.</param>
		/// <remarks>Chaining a target multiple times will cause the target to
		/// be invoked multiple times for every event.</remarks>
		void Chain(ILogTarget target);

		/// <summary>
		/// Unchains the provided target from the current target, so that the
		/// provided target is no longer invoked when this target receives an event.
		/// </summary>
		/// <param name="target">The target to unchain</param>
		/// <remarks>Multiply-chained targets need to be unchained the same amount
		/// of time to be completely removed.</remarks>
		void Unchain(ILogTarget target);
	}

	/// <summary>
	/// Registers a provided log target to receive log messages for the lifespan
	/// of this object.
	/// </summary>
	public sealed class LogSession : IDisposable
	{
		/// <summary>
		/// Constructor. Registers the given log target with the provided threads
		/// for listening for log messages.
		/// </summary>
		/// <param name="target">The target that should receive events.</param>
		/// <param name="threads">The threads which the target will be registered
		/// with for event notifications.</param>
		public LogSession(ILogTarget target, params Thread[] threads)
		{
			Target = target;
			Threads = threads.Distinct().ToArray();

			foreach (Thread thread in Threads)
			{
				if (!Logger.Listeners.ContainsKey(thread))
					Logger.Listeners.Add(thread, new LogThreadTargets());
				Logger.Listeners[thread].Add(target);
			}

			Target.OnEventLogged(this, new LogEventArgs(
				new LogEntry(S._("Session started"), LogLevel.Information)));
		}

		/// <summary>
		/// Constructor. Registered the given log target with the current thread
		/// for listening for log messages.
		/// </summary>
		/// <param name="target">The target which should receive events</param>
		public LogSession(ILogTarget target)
			: this(target, Thread.CurrentThread)
		{
		}

		#region IDisposable Members

		~LogSession()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (Threads == null || Target == null)
				return;

			if (disposing)
			{
				//Disconnect the event handler from the threads.
				foreach (Thread thread in Threads)
					Logger.Listeners[thread].Remove(Target);
			}

			Target.OnEventLogged(this, new LogEventArgs(
				new LogEntry(S._("Session ended"), LogLevel.Information)));
			Threads = null;
			Target = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		/// <summary>
		/// The target that should receive events. If this is null, the object
		/// has been disposed.
		/// </summary>
		private ILogTarget Target;

		/// <summary>
		/// The list of threads which the target will be registered with for event
		/// notifications. If this is null, the object is disposd.
		/// </summary>
		private Thread[] Threads;
	}

	/// <summary>
	/// Collects a list of log entries into one session.
	/// </summary>
	/// <remarks>Instance functions of this class are thread-safe.</remarks>
	[Serializable]
	public abstract class LogSinkBase : ISerializable, ILogTarget, IList<LogEntry>
	{
		public LogSinkBase()
		{
		}

		#region ISerializable Members

		public LogSinkBase(SerializationInfo info, StreamingContext context)
		{
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

		#endregion 

		#region ILoggerTarget Members

		public void OnEventLogged(object sender, LogEventArgs e)
		{
			lock (List)
				List.Add(e.LogEntry);

			lock (ChainedTargets)
				ChainedTargets.ForEach(target => target.OnEventLogged(sender, e));
		}

		public void Chain(ILogTarget target)
		{
			lock (ChainedTargets)
				ChainedTargets.Add(target);
		}

		public void Unchain(ILogTarget target)
		{
			lock (ChainedTargets)
				ChainedTargets.Remove(target);
		}

		/// <summary>
		/// The list of targets which are chained to this one.
		/// </summary>
		private List<ILogTarget> ChainedTargets = new List<ILogTarget>();

		#endregion

		#region IList<LogEntry> Members

		public int IndexOf(LogEntry item)
		{
			lock (List)
				return IndexOf(item);
		}

		public void Insert(int index, LogEntry item)
		{
			lock (List)
				List.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			lock (List)
				List.RemoveAt(index);
		}

		public LogEntry this[int index]
		{
			get
			{
				lock (List)
					return List[index];
			}
			set
			{
				lock (List)
					List[index] = value;
			}
		}

		#endregion

		#region ICollection<LogEntry> Members

		public void Add(LogEntry item)
		{
			lock (List)
				List.Add(item);
		}

		public void Clear()
		{
			lock (List)
				List.Clear();
		}

		public bool Contains(LogEntry item)
		{
			lock (List)
				return List.Contains(item);
		}

		public void CopyTo(LogEntry[] array, int arrayIndex)
		{
			lock (List)
				List.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				lock (List)
					return List.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(LogEntry item)
		{
			lock (List)
				return List.Remove(item);
		}

		#endregion

		#region IEnumerable<LogEntry> Members

		public IEnumerator<LogEntry> GetEnumerator()
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
		/// Gets the highest log level in the current log sink.
		/// </summary>
		public LogLevel Highest
		{
			get
			{
				lock (List)
					return List.Max(delegate(LogEntry e) { return e.Level; });
			}
		}

		/// <summary>
		/// Gets the time the first message was logged.
		/// </summary>
		public DateTime StartTime
		{
			get
			{
				lock (List)
					return List.First().Timestamp;
			}
		}

		/// <summary>
		/// Gets the time the last message was logged.
		/// </summary>
		public DateTime EndTime
		{
			get
			{
				lock (List)
					return List.Last().Timestamp;
			}
		}

		/// <summary>
		/// Saves the log to the given path in an XML format which can be read
		/// by <see cref="LazyLogSink"/>.
		/// </summary>
		/// <param name="path">The path to save the log contents to.</param>
		public void Save(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
				Save(stream);
		}

		public void Save(Stream stream)
		{
			IList<LogEntry> list = List;
			lock (list)
			{
				XmlRootAttribute root = new XmlRootAttribute("Log");
				XmlSerializer serializer = new XmlSerializer(list.GetType(), root);

				serializer.Serialize(stream, list);
			}
		}

		/// <summary>
		/// The backing store of this session.
		/// </summary>
		protected abstract IList<LogEntry> List
		{
			get;
		}
	}

	[Serializable]
	public class LogSink : LogSinkBase
	{
		public LogSink()
		{
			list = new List<LogEntry>();
		}

		#region ISerializable Members

		public LogSink(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			list = (List<LogEntry>)info.GetValue("List", typeof(List<LogEntry>));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("List", List);
		}

		#endregion 

		protected override IList<LogEntry> List
		{
			get { return list; }
		}
		private List<LogEntry> list;
	}

	[Serializable]
	public class LazyLogSink : LogSinkBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="path">The path to the log file.</param>
		public LazyLogSink(string path)
		{
			SavePath = path;
		}

		#region ISerializable Members

		public LazyLogSink(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			SavePath = (string)info.GetValue("SavePath", typeof(string));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("SavePath", SavePath);
		}

		#endregion

		private void LoadList()
		{
			XmlRootAttribute root = new XmlRootAttribute("Log");
			XmlSerializer serializer = new XmlSerializer(typeof(List<LogEntry>), root);

			using (FileStream stream = new FileStream(SavePath, FileMode.Open))
				list = (List<LogEntry>)serializer.Deserialize(stream);
		}

		/// <summary>
		/// The path the log was saved to.
		/// </summary>
		public string SavePath
		{
			get;
			private set;
		}

		protected override IList<LogEntry> List
		{
			get
			{
				lock (Synchronise)
				{
					if (list == null)
						LoadList();
					return list;
				}
			}
		}

		private List<LogEntry> list;

		/// <summary>
		/// Private object to makw sure LoadList is not called on multiple threads.
		/// </summary>
		private object Synchronise = new object();
	}
}