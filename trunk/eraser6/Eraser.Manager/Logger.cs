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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Eraser.Manager
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
	/// The Logger class which handles log entries and manages entries.
	/// 
	/// The class has the notion of entries and sessions. Each session contains one
	/// or more (log) entries. This allows the program to determine if the last
	/// session had errors or not.
	/// </summary>
	[Serializable]
	public class Logger : ISerializable
	{
		#region Serialization code
		protected Logger(SerializationInfo info, StreamingContext context)
		{
			Entries = (LogSessionDictionary)info.GetValue("Entries", typeof(LogSessionDictionary));
			Entries.Owner = this;
			foreach (DateTime key in Entries.Keys)
				LastSession = key;
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter=true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Entries", Entries);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public Logger()
		{
			Entries = new LogSessionDictionary(this);
		}

		/// <summary>
		/// All the registered event handlers for the log event of this task.
		/// </summary>
		public EventHandler<LogEventArgs> Logged { get; set; }

		internal void OnLogged(object sender, LogEventArgs e)
		{
			if (Logged != null)
				Logged(sender, e);
		}

		/// <summary>
		/// All the registered event handlers for handling when a new session has been
		/// started.
		/// </summary>
		public EventHandler<EventArgs> NewSession { get; set; }

		internal void OnNewSession(object sender, EventArgs e)
		{
			if (NewSession != null)
				NewSession(sender, e);
		}

		/// <summary>
		/// Retrieves the log for this task.
		/// </summary>
		public LogSessionDictionary Entries { get; private set; }

		/// <summary>
		/// Retrieves the log entries from the previous session.
		/// </summary>
		public LogEntryCollection LastSessionEntries
		{
			get
			{
				return Entries[LastSession];
			}
		}

		/// <summary>
		/// Clears the log entries from the log.
		/// </summary>
		public void Clear()
		{
			LogEntryCollection lastSessionEntries = null;
			if (Entries.ContainsKey(LastSession))
				lastSessionEntries = Entries[LastSession];
			Entries.Clear();

			if (lastSessionEntries != null)
				Entries.Add(LastSession, lastSessionEntries);
		}

		/// <summary>
		/// The date and time of the last session.
		/// </summary>
		public DateTime LastSession
		{
			get { return lastSession; }
			internal set { lastSession = value; OnNewSession(null, EventArgs.Empty); }
		}

		private DateTime lastSession;
	}

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

	[Serializable]
	public class LogSessionDictionary : IDictionary<DateTime, LogEntryCollection>,
		ISerializable
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="logger">The logger object managing the logging.</param>
		public LogSessionDictionary(Logger logger)
		{
			Owner = logger;
		}

		public void NewSession()
		{
			DateTime sessionTime = DateTime.Now;
			Add(sessionTime, new LogEntryCollection(Owner));
			Owner.LastSession = sessionTime;
		}

		#region ISerializable Members
		protected LogSessionDictionary(SerializationInfo info, StreamingContext context)
		{
			dictionary = (Dictionary<DateTime, LogEntryCollection>)info.GetValue("Dictionary",
				dictionary.GetType());
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			lock (dictionary)
				info.AddValue("Dictionary", dictionary);
		}
		#endregion

		#region IDictionary<DateTime,LogSessionEntryCollection> Members
		public void Add(DateTime key, LogEntryCollection value)
		{
			lock (dictionary)
				dictionary.Add(key, value);
		}

		public bool ContainsKey(DateTime key)
		{
			lock (dictionary)
				return dictionary.ContainsKey(key);
		}

		public ICollection<DateTime> Keys
		{
			get
			{
				lock (dictionary)
				{
					DateTime[] result = new DateTime[dictionary.Keys.Count];
					dictionary.Keys.CopyTo(result, 0);
					return result;
				}
			}
		}

		public bool Remove(DateTime key)
		{
			lock (dictionary)
				return dictionary.Remove(key);
		}

		public bool TryGetValue(DateTime key, out LogEntryCollection value)
		{
			lock (dictionary)
				return dictionary.TryGetValue(key, out value);
		}

		public ICollection<LogEntryCollection> Values
		{
			get
			{
				lock (dictionary)
				{
					LogEntryCollection[] result = new LogEntryCollection[dictionary.Values.Count];
					dictionary.Values.CopyTo(result, 0);
					return result;
				}
			}
		}

		public LogEntryCollection this[DateTime key]
		{
			get
			{
				lock (dictionary)
					return dictionary[key];
			}
			set
			{
				lock (dictionary)
					dictionary[key] = value;
			}
		}
		#endregion

		#region ICollection<KeyValuePair<DateTime,LogSessionEntryCollection>> Members
		public void Add(KeyValuePair<DateTime, LogEntryCollection> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			lock (dictionary)
				dictionary.Clear();
		}

		public bool Contains(KeyValuePair<DateTime, LogEntryCollection> item)
		{
			lock (dictionary)
				return dictionary.ContainsKey(item.Key) && dictionary[item.Key] == item.Value;
		}

		public void CopyTo(KeyValuePair<DateTime, LogEntryCollection>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get
			{
				lock (dictionary)
					return dictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<DateTime, LogEntryCollection> item)
		{
			lock (dictionary)
				return dictionary.Remove(item.Key);
		}
		#endregion

		#region IEnumerable<KeyValuePair<DateTime,LogSessionEntryCollection>> Members
		public IEnumerator<KeyValuePair<DateTime, LogEntryCollection>> GetEnumerator()
		{
			return dictionary.GetEnumerator();
		}
		#endregion

		#region IEnumerable Members
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion

		/// <summary>
		/// The log manager.
		/// </summary>
		internal Logger Owner
		{
			get
			{
				return owner;
			}
			set
			{
				lock (dictionary)
					foreach (LogEntryCollection entries in dictionary.Values)
						entries.owner = value;
				owner = value;
			}
		}

		/// <summary>
		/// The log manager.
		/// </summary>
		private Logger owner;

		/// <summary>
		/// The store for this object.
		/// </summary>
		private Dictionary<DateTime, LogEntryCollection> dictionary =
			new Dictionary<DateTime, LogEntryCollection>();
	}

	[Serializable]
	public class LogEntryCollection : IList<LogEntry>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="logger">The <see cref="Logger"/> object handling logging.</param>
		internal LogEntryCollection(Logger logger)
		{
			owner = logger;
		}

		#region IList<LogEntry> Members
		public int IndexOf(LogEntry item)
		{
			lock (list)
				return list.IndexOf(item);
		}

		public void Insert(int index, LogEntry item)
		{
			lock (list)
				list.Insert(index, item);
			owner.OnLogged(owner, new LogEventArgs(item));
		}

		public void RemoveAt(int index)
		{
			throw new InvalidOperationException();
		}

		public LogEntry this[int index]
		{
			get
			{
				lock (list)
					return list[index];
			}
			set
			{
				throw new InvalidOperationException();
			}
		}
		#endregion

		#region ICollection<LogEntry> Members
		public void Add(LogEntry item)
		{
			Insert(Count, item);
		}

		public void Clear()
		{
			throw new InvalidOperationException();
		}

		public bool Contains(LogEntry item)
		{
			lock (list)
				return list.Contains(item);
		}

		public void CopyTo(LogEntry[] array, int arrayIndex)
		{
			lock (list)
				list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				lock (list)
					return list.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(LogEntry item)
		{
			lock (list)
				return list.Remove(item);
		}
		#endregion

		#region IEnumerable<LogEntry> Members
		public IEnumerator<LogEntry> GetEnumerator()
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

		/// <summary>
		/// The Logger object managing logging.
		/// </summary>
		internal Logger owner;

		/// <summary>
		/// The store for this object.
		/// </summary>
		private List<LogEntry> list = new List<LogEntry>();
	}

	/// <summary>
	/// Represents a log entry.
	/// </summary>
	[Serializable]
	public struct LogEntry : ISerializable
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
}
