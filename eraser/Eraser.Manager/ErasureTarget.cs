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

using System.Security.Permissions;
using System.Runtime.Serialization;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser.Manager
{
	/// <summary>
	/// Represents a generic target of erasure
	/// </summary>
	[Serializable]
	public abstract class ErasureTarget : ISerializable, IRegisterable
	{
		#region Serialization code
		protected ErasureTarget(SerializationInfo info, StreamingContext context)
		{
			Guid methodGuid = (Guid)info.GetValue("Method", typeof(Guid));
			if (methodGuid == Guid.Empty)
				Method = ErasureMethodRegistrar.Default;
			else
				Method = ManagerLibrary.Instance.ErasureMethodRegistrar[methodGuid];
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Method", Method.Guid);
		}
		#endregion

		#region IRegisterable Members

		public abstract Guid Guid
		{
			get;
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected ErasureTarget()
		{
			Method = ErasureMethodRegistrar.Default;
		}

		/// <summary>
		/// The task which owns this target.
		/// </summary>
		public Task Task { get; internal set; }

		/// <summary>
		/// The name of the type of the Erasure target.
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// The method used for erasing the file.
		/// </summary>
		public ErasureMethod Method
		{
			get
			{
				return method;
			}
			set
			{
				if (!SupportsMethod(value))
					throw new ArgumentException(S._("The selected erasure method is not " +
						"supported for this erasure target."));
				method = value;
			}
		}

		/// <summary>
		/// Gets the effective erasure method for the current target (i.e., returns
		/// the correct erasure method for cases where the <see cref="Method"/>
		/// property is <see cref="ErasureMethodRegistrar.Default"/>
		/// </summary>
		/// <returns>The Erasure method which the target should be erased with.
		/// This function will never return <see cref="ErasureMethodRegistrar.Default"/></returns>
		public virtual ErasureMethod EffectiveMethod
		{
			get
			{
				if (Method != ErasureMethodRegistrar.Default)
					return Method;

				throw new InvalidOperationException("The effective method of the erasure " +
					"target cannot be ErasureMethodRegistrar.Default");
			}
		}

		/// <summary>
		/// Checks whether the provided erasure method is supported by this current
		/// target.
		/// </summary>
		/// <param name="method">The erasure method to check.</param>
		/// <returns>True if the erasure method is supported, false otherwise.</returns>
		public virtual bool SupportsMethod(ErasureMethod method)
		{
			return true;
		}

		/// <summary>
		/// Retrieves the text to display representing this target.
		/// </summary>
		public abstract string UIText
		{
			get;
		}

		/// <summary>
		/// The progress of this target.
		/// </summary>
		public ProgressManagerBase Progress
		{
			get;
			protected set;
		}
		
		/// <summary>
		/// The Progress Changed event handler of the owning task.
		/// </summary>
		protected internal Action<ErasureTarget, ProgressChangedEventArgs> OnProgressChanged
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets an <see cref="IErasureTargetConfigurer"/> which contains settings for
		/// configuring this task, or null if this erasure target has no settings to be set.
		/// </summary>
		/// <remarks>The result should be able to be passed to the <see cref="Configure"/>
		/// function, and settings for this task will be according to the returned
		/// control.</remarks>
		public abstract IErasureTargetConfigurer Configurer
		{
			get;
		}

		/// <summary>
		/// Executes the given target.
		/// </summary>
		public abstract void Execute();

		/// <summary>
		/// The backing variable for the <see cref="Method"/> property.
		/// </summary>
		private ErasureMethod method;
	}

	/// <summary>
	/// Represents an interface for an abstract erasure target configuration
	/// object.
	/// </summary>
	public interface IErasureTargetConfigurer : ICliConfigurer<ErasureTarget>
	{
	}

	public class ErasureTargetRegistrar : FactoryRegistrar<ErasureTarget>
	{
	}

	/// <summary>
	/// Maintains a collection of erasure targets.
	/// </summary>
	[Serializable]
	public class ErasureTargetsCollection : IList<ErasureTarget>, ISerializable
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
			item.OnProgressChanged = owner.OnProgressChanged;
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
			item.OnProgressChanged = owner.OnProgressChanged;
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
				{
					target.Task = owner;
					target.OnProgressChanged = owner.OnProgressChanged;
				}
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
}
