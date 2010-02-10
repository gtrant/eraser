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
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Eraser.Manager
{
	/// <summary>
	/// The abstract registrar interface.
	/// </summary>
	public interface IRegistrar<T> : IList<T> where T: IRegisterable
	{
		/// <summary>
		/// Gets the registerable object from its Guid.
		/// </summary>
		/// <param name="key">The Guid of the registerable object to find.</param>
		/// <returns>The registerable object.</returns>
		/// <exception cref="KeyNotFoundException">When the Guid provided is not of
		/// any registered object.</exception>
		T this[Guid key] { get; }

		/// <summary>
		/// Gets whether a registerable object with the given Guid is registered.
		/// </summary>
		/// <param name="key">The Guid of the object to find.</param>
		/// <returns>True if such an object is registered, otherwise false</returns>
		bool Contains(Guid key);

		/// <summary>
		/// Unregisters the registerable object with the given Guid.
		/// </summary>
		/// <param name="key">The Guid to unregister.</param>
		/// <returns>True if an object was unregistered.</returns>
		bool Remove(Guid key);

		/// <summary>
		/// The event raised when an <see cref="IRegisterable"/> object is
		/// registered.
		/// </summary>
		EventHandler<EventArgs> Registered { get; set; }

		/// <summary>
		/// The event raised when an <see cref="IRegisterable"/> object is
		/// unregistered.
		/// </summary>
		EventHandler<EventArgs> Unregistered { get; set; }
	}

	/// <summary>
	/// Represents an object registerable to an <see cref="IRegistrar"/>
	/// </summary>
	public interface IRegisterable
	{
		/// <summary>
		/// The GUID of the current object.
		/// </summary>
		Guid Guid { get; }
	}

	/// <summary>
	/// Provides a simple Registrar implementation.
	/// </summary>
	/// <typeparam name="T">The registerable's type.</typeparam>
	public class Registrar<T> : IRegistrar<T> where T: IRegisterable
	{
		#region IList<T> Members

		public int IndexOf(T item)
		{
			lock (List)
				return List.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			lock (List)
			{
				List.Insert(index, item);
				Dictionary.Add(item.Guid, item);
			}

			if (Registered != null)
				Registered(item, EventArgs.Empty);
		}

		public void RemoveAt(int index)
		{
			T value = default(T);
			lock (List)
			{
				value = List[index];
				List.RemoveAt(index);
				Dictionary.Remove(value.Guid);
			}

			if (Unregistered != null)
				Unregistered(value, EventArgs.Empty);
		}

		public T this[int index]
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

		#region ICollection<T> Members

		/// <remarks>If the registerable object is added twice, the second registration
		/// is ignored.</remarks>
		public void Add(T item)
		{
			lock (List)
			{
				if (Dictionary.ContainsKey(item.Guid))
					return;

				List.Add(item);
				Dictionary.Add(item.Guid, item);
			}

			if (Registered != null)
				Registered(item, EventArgs.Empty);
		}

		public void Clear()
		{
			lock (List)
			{
				if (Unregistered != null)
					List.ForEach(item => Unregistered(item, EventArgs.Empty));
				List.Clear();
				Dictionary.Clear();
			}
		}

		public bool Contains(T item)
		{
			lock (List)
				return List.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (List)
				List.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				lock (List)
					return Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			bool result = false;
			lock (List)
			{
				result = List.Remove(item);
				Dictionary.Remove(item.Guid);
			}

			if (result && Unregistered != null)
				Unregistered(item, EventArgs.Empty);
			return result;
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
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

		#region IRegistrar<T> Members

		public T this[Guid key]
		{
			get
			{
				lock (List)
					return Dictionary[key];
			}
		}

		public bool Contains(Guid key)
		{
			lock (List)
				return Dictionary.ContainsKey(key);
		}

		public bool Remove(Guid key)
		{
			lock (List)
			{
				if (!Dictionary.ContainsKey(key))
					return false;
				return Remove(Dictionary[key]);
			}
		}

		public EventHandler<EventArgs> Registered { get; set; }

		public EventHandler<EventArgs> Unregistered { get; set; }

		#endregion

		/// <summary>
		/// The backing list for this object.
		/// </summary>
		private List<T> List = new List<T>();

		/// <summary>
		/// The backing dictionary for this object.
		/// </summary>
		private Dictionary<Guid, T> Dictionary = new Dictionary<Guid, T>();
	}
}
