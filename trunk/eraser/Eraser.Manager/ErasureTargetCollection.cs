/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Permissions;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;
using System.IO;

namespace Eraser.Manager
{
	/// <summary>
	/// Maintains a collection of erasure targets.
	/// </summary>
	[Serializable]
	public class ErasureTargetCollection : IList<IErasureTarget>, ISerializable,
		                                   IXmlSerializable
	{
		#region Constructors
		public ErasureTargetCollection()
		{
			this.list = new List<IErasureTarget>();
		}

		internal ErasureTargetCollection(Task owner)
			: this()
		{
			this.owner = owner;
		}

		internal ErasureTargetCollection(Task owner, int capacity)
			: this(owner)
		{
			list.Capacity = capacity;
		}

		internal ErasureTargetCollection(Task owner, IEnumerable<IErasureTarget> targets)
			: this(owner)
		{
			list.AddRange(targets);
		}
		#endregion

		#region Serialization Code
		protected ErasureTargetCollection(SerializationInfo info, StreamingContext context)
		{
			list = (List<IErasureTarget>)info.GetValue("list", typeof(List<IErasureTarget>));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("list", list);
		}

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			List<XmlSerializer> targetSerializers = new List<XmlSerializer>();
			foreach (IErasureTarget target in Host.Instance.ErasureTargetFactories)
				targetSerializers.Add(new XmlSerializer(target.GetType()));

			list.Clear();
			bool empty = reader.IsEmptyElement;
			reader.ReadStartElement("ErasureTargetCollection");
			if (!empty)
			{
				while (reader.NodeType != XmlNodeType.EndElement)
				{
					foreach (XmlSerializer serializer in targetSerializers)
					{
						bool targetEmpty = reader.IsEmptyElement;
						if (serializer.CanDeserialize(reader))
						{
							IErasureTarget target = (IErasureTarget)
								serializer.Deserialize(reader);
							list.Add(target);
							break;
						}
					}
				}

				if (reader.Name != "ErasureTargetCollection")
					throw new InvalidDataException();
				reader.ReadEndElement();
			}
		}

		public void WriteXml(XmlWriter writer)
		{
			foreach (IErasureTarget target in this)
			{
				XmlSerializer serializer = new XmlSerializer(target.GetType());
				serializer.Serialize(writer, target);
			}
		}
		#endregion

		#region IEnumerable<ErasureTarget> Members
		public IEnumerator<IErasureTarget> GetEnumerator()
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
		public void Add(IErasureTarget item)
		{
			list.Add(item);
			if (item.Task != null)
				throw new ArgumentException("Erasure targets can only belong " +
					"to one Task at any one time.");
			item.Task = Owner;
		}

		public void Clear()
		{
			foreach (IErasureTarget item in list)
				Remove(item);
		}

		public bool Contains(IErasureTarget item)
		{
			return list.Contains(item);
		}

		public void CopyTo(IErasureTarget[] array, int arrayIndex)
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

		public bool Remove(IErasureTarget item)
		{
			int index = list.IndexOf(item);
			if (index < 0)
				return false;

			RemoveAt(index);
			return true;
		}
		#endregion

		#region IList<ErasureTarget> Members
		public int IndexOf(IErasureTarget item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, IErasureTarget item)
		{
			list.Insert(index, item);
			if (item.Task != Owner && item.Task != null)
				throw new ArgumentException("Erasure targets can only belong " +
					"to one Task at any one time.");
			item.Task = Owner;
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
		}

		public IErasureTarget this[int index]
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
				foreach (IErasureTarget item in list)
				{
					if (item.Task != null)
						throw new ArgumentException("Erasure targets can only belong " +
							"to one Task at any one time.");
					item.Task = owner;
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
		private List<IErasureTarget> list;
	}
}
