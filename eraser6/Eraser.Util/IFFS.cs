using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.IO;
using System.Text;

namespace Eraser.Util
{
	/// <summary>
	/// In File File System used for Eraser settings
	/// </summary>
	public class IFFS
	{
		private static readonly char[] Seperators =
			new char[] { '\\', '/' };

		[Serializable]
		public class FSNode : ISerializable
		{
			#region ISerializable
			public FSNode(SerializationInfo info, StreamingContext context)
			{
				Name = (String)info.GetValue("IFFS.FSNode.Name", typeof(String));
				Parent = (FSNode)info.GetValue("IFFS.FSNode.Parent", typeof(FSNode));
				OpenHandles = new List<Handle>();
			}

			[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.AddValue("IFFS.FSNode.Parent", Parent);
				info.AddValue("IFFS.FSNode.Name", Name);
			}
			#endregion

			public FSNode(FSNode parent, String name)
			{
				Name = name;
				Parent = parent;
				OpenHandles = new List<Handle>();
			}

			/// <summary>
			/// The parent directory this object belongs to
			/// </summary>
			public FSNode Parent { get; set; }

			/// <summary>
			/// Returns a boolean value indicating if this object
			/// is the root objects of a file system
			/// </summary>
			public bool IsRoot
			{
				get { return Parent == null; }
			}

			/// <summary>
			/// Get or set the name of this object
			/// </summary>
			public String Name { get; set; }

			/// <summary>
			/// Get the full name of this object
			/// </summary>
			public String FullName
			{
				get
				{
					if (IsRoot) return Name;
					return Path.Combine(Parent.FullName, Name);
				}
			}

			/// <summary>
			/// The mutex of this object used for locking the contents
			/// </summary>
			public bool IsLocked { get; internal set; }

			/// <summary>
			/// List of the open handles to this object
			/// </summary>
			public List<Handle> OpenHandles { get; internal set; }

			internal void Lock()
			{
				IsLocked = true;
			}

			internal void Unlock()
			{
				IsLocked = false;
			}
		}

		[Serializable]
		public class DirectoryNode : FSNode, ISerializable
		{
			#region ISerializable
			public DirectoryNode(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				Files = (List<FileNode>)info.GetValue("IFFS.DirectoryNode.Files", typeof(List<FileNode>));
				SubDirectories = (List<DirectoryNode>)info.GetValue("IFFS.DirectoryNode.SubDirectories", typeof(List<DirectoryNode>));
			}

			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.AddValue("IFFS.FSNode.Parent", Parent);
				info.AddValue("IFFS.FSNode.Name", Name);
				info.AddValue("IFFS.DirectoryNode.Files", Files);
				info.AddValue("IFFS.DirectoryNode.SubDirectories", SubDirectories);
			}
			#endregion

			public DirectoryNode(DirectoryNode parent, String name)
				: base(parent, name)
			{
				Files = new List<FileNode>();
				SubDirectories = new List<DirectoryNode>();
			}

			public List<FileNode> Files { get; set; }

			public List<DirectoryNode> SubDirectories { get; set; }

			public FSNode GetEntry(String name)
			{
				FileNode fNode = GetFile(name);
				if (fNode != null) return fNode;

				DirectoryNode dNode = GetDirectory(name);
				if (dNode != null) return dNode;

				return null;
			}

			public FSNode[] GetEntries()
			{
				List<FSNode> nodes = new List<FSNode>();
				foreach (FileNode fNode in Files)
					nodes.Add(fNode);

				foreach (DirectoryNode dNode in SubDirectories)
					nodes.Add(dNode);

				return nodes.ToArray();
			}

			public FileNode GetFile(String name)
			{
				FileNode fNode = Files.Find(delegate(FileNode node)
				{
					return node.Name == name;
				});

				if (fNode == null)
					return null;
				else
					return fNode;
			}

			public List<FileNode>.Enumerator GetFileEnumurator()
			{
				return Files.GetEnumerator();
			}

			public FileNode[] GetFiles()
			{
				return Files.ToArray();
			}

			public DirectoryNode GetDirectory(String name)
			{
				DirectoryNode dNode = SubDirectories.Find(delegate(DirectoryNode node)
				{
					return node.Name == name;
				});

				if (dNode == null)
					return null;
				else
					return dNode;
			}

			public List<DirectoryNode>.Enumerator GetDirectoryEnumurator()
			{
				return SubDirectories.GetEnumerator();
			}

			public DirectoryNode[] GetDirectories()
			{
				return SubDirectories.ToArray();
			}

			public bool IsEmpty
			{
				get { return Files.Count <= 0 && SubDirectories.Count <= 0; }
			}
		}

		[Serializable]
		public class FileNode : FSNode, ISerializable
		{
			#region ISerializable
			public FileNode(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				Data = (Object)info.GetValue("IFFS.FileNode.Data", typeof(Object));
				DataType = (Type)info.GetValue("IFFS.FileNode.DataType", typeof(Type));
			}

			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.AddValue("IFFS.FSNode.Parent", Parent);
				info.AddValue("IFFS.FSNode.Name", Name);
				info.AddValue("IFFS.FileNode.Data", Data);
				info.AddValue("IFFS.FileNode.DataType", DataType);
			}
			#endregion

			public FileNode(DirectoryNode parent, String name)
				: base(parent, name)
			{
				Data = null;
				DataType = null;
			}

			/// <summary>
			/// Type of the contents
			/// </summary>
			public Type DataType { get; set; }

			/// <summary>
			/// The contents
			/// </summary>
			public object Data { get; set; }

			public void SetData(object data, Type dataType)
			{
				Data = data;
				DataType = dataType;
			}

			public T GetDate<T>()
			{
				return (T)Data;
			}
		}

		public class Handle : IDisposable
		{
			internal Handle(IFFS fs)
			{
				this.fs = fs;
				File = null;
				Dir = null;
			}

			internal Handle(IFFS fs, FSNode n)
				: this(fs)
			{
				if (n is FileNode)
					File = n as FileNode;
				else if (n is DirectoryNode)
					Dir = n as DirectoryNode;

				n.OpenHandles.Add(this);
			}

			internal Handle(IFFS fs, FileNode n)
				: this(fs)
			{
				File = n;
				n.OpenHandles.Add(this);
			}

			internal Handle(IFFS fs, DirectoryNode n)
				: this(fs)
			{
				Dir = n;
				n.OpenHandles.Add(this);
			}

			~Handle()
			{
				if (this != null)
					Dispose();
			}

			public void Dispose()
			{
				Close();
				GC.SuppressFinalize(this);
			}

			public void Close()
			{
				if (!IsValid)
					return;

				else if (IsFile)
					if (File.OpenHandles != null)
					{
						if (File.OpenHandles.Count > 0)
							File.OpenHandles.Remove(this);

						if (File.OpenHandles.Count == 0)
							File.Unlock();
					}

					else if (IsDirectory)
						if (Dir.OpenHandles != null)
						{
							if (Dir.OpenHandles.Count > 0)
								Dir.OpenHandles.Remove(this);

							if (Dir.OpenHandles.Count == 0)
								Dir.Unlock();
						}
			}

			public bool IsValid
			{
				get { return Dir != null || File != null; }
				set
				{
					if (value != false) return;
					Dir = null;
					File = null;
				}
			}

			public bool IsDirectory
			{
				get { return Dir != null && File == null; }
			}

			public bool IsFile
			{
				get { return Dir == null && File != null; }
			}

			internal FileNode File;
			internal DirectoryNode Dir;
			internal IFFS fs;
		}

		public IFFS()
		{
			Formatter = new BinaryFormatter();
			Root = new DirectoryNode(null, "");
		}

		public IFFS(Stream stream)
			: this()
		{
			Deserialize(stream);
		}

		~IFFS()
		{
			foreach (FSNode node in Root.GetEntries())
			{
			}
		}

		public Stream Deserialize(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			Root = Formatter.Deserialize(stream) as DirectoryNode;
			return stream;
		}

		public Stream Serialize(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			Formatter.Serialize(stream, Root);
			return stream;
		}

		public DirectoryNode Root { get; set; }

		public Handle RootHandle
		{
			get { return new Handle(this, Root); }
		}

		[Flags]
		public enum AccessMode : uint
		{
			EMPTY = 0 << 0,
			LOCKED = 1U << 0,
			OPEN_ONLY = 1U << 1,
			CREATE_ONLY = 1U << 2,
			OPEN_OR_CREATE = 1U << 3,
		}

		public static Handle Open(Handle n, String name)
		{
			if (!n.IsDirectory)
				return new Handle(n.fs);

			FileNode fNode = n.Dir.GetFile(name);
			if (fNode != null) return new Handle(n.fs, fNode);

			DirectoryNode dNode = n.Dir.GetDirectory(name);
			if (dNode != null) return new Handle(n.fs, dNode);

			return new Handle(n.fs);
		}

		public static Handle Open(IFFS fs, String[] resolved, uint access)
		{
			int i;
			FSNode node = null;
			DirectoryNode Node = fs.Root, TNode = null;

			for (i = 0; i < resolved.Length; i++)
			{
				TNode = Node.GetDirectory(resolved[i]);
				if (TNode != null)
					Node = TNode;
				else
					break;
			}

			// is this a directory? if so return the handle to it
			if (TNode != null)
			{
				node = Node;
				goto results_out;
			}

			// this must be a file, if it isn't return null
			if (resolved.Length - i != 1)
				return new Handle(fs);
			else
				node = Node.GetFile(resolved[i]);

		results_out:
			{ // access: bit flags operation

				// check: locked handle
				if ((access & (uint)AccessMode.LOCKED) != 0)
				{
					// if the node is already locked, we can't
					// full fill the request
					if (node.IsLocked)
						return new Handle(fs);
					else
						node.Lock();
				}
			}

			return new Handle(fs, node);
		}

		public static Handle Open(IFFS fs, String s, uint access)
		{
			return Open(fs, GetResolvedName(s), access);
		}

		public static Handle Open(IFFS fs, String s)
		{
			return Open(fs, s, (uint)AccessMode.LOCKED);
		}

		public static Handle OpenFile(IFFS fs, String s)
		{
			Handle h = Open(fs, s);
			if (h.IsFile)
				return h;
			else
				return new Handle(fs);
		}

		public static bool Delete(IFFS fs, String s)
		{
			return Delete(Open(fs, s, (uint)AccessMode.EMPTY));
		}

		public static bool Delete(Handle n)
		{
			if (!n.IsValid)
				return false;

			if (n.IsFile)
			{
				DirectoryNode parent = n.File.Parent as DirectoryNode;

				// check to see if the file is locked

				do
					if (n.File.IsLocked)
					{
						if (n.File.OpenHandles.Count > 2)
						{
							// the file is locked by another handle
							return false;
						}
						else
						{
							int i = 0;
							for (i = 0; i < n.File.OpenHandles.Count; i++)
								if (n.File.OpenHandles[i] == n)
									break;
							if (i < n.File.OpenHandles.Count)
								break;
							else
								return false;
						}
					}
					else n.File.Lock(); // lock the object
				while (false);

				bool deleted = false;

				if (deleted = parent.Files.Remove(n.File))
					foreach (Handle hndl in n.File.OpenHandles)
						hndl.IsValid = false;

				return deleted;
			}
			else if (n.IsDirectory)
			{
				DirectoryNode parent = n.Dir.Parent as DirectoryNode;

				// check to see if the file is locked
				if (n.Dir.IsLocked)
				{
					if (n.Dir.OpenHandles.Count > 1)
					{
						// the file is locked by another handle
						return false;
					}
					else if (n.Dir.OpenHandles.Count == 1)
					{
						// the file is locked but not with this handle
						if (n.Dir.OpenHandles[0] != n)
							return false;
					}
				}
				else n.Dir.Lock(); // lock the object

				bool deleted = false;

				if (deleted = parent.SubDirectories.Remove(n.Dir))
					foreach (Handle hndl in n.Dir.OpenHandles)
						hndl.IsValid = false;

				return deleted;
			}
			else
			{
				return false;
			}
		}

		public static Handle CreateFile(Handle n, String name)
		{
			return CreateFile(n, name, (uint)AccessMode.LOCKED);
		}

		public static Handle CreateFile(Handle n, String name, uint access)
		{
			if (n.IsFile)
				throw new DirectoryNotFoundException(
					"Directory was not found");

			FileNode file = n.Dir.Files.Find(delegate(FileNode node)
			{
				return node.Name == name;
			});

			if (file != null)
				if ((access & (uint)AccessMode.CREATE_ONLY) != 0)
					throw new System.IO.IOException(
						"File already exists");
				else
				{
					if (file.IsLocked)
						return new Handle(n.fs);
					else
						return new Handle(n.fs, file);
				}

			FileNode fNode = new FileNode(n.Dir, name);

			if ((access & (uint)AccessMode.LOCKED) != 0)
				fNode.Lock();

			n.Dir.Files.Add(fNode);

			return new Handle(n.fs, fNode);
		}

		public static Handle CreateDirectory(Handle n, String name)
		{
			return CreateDirectory(n, name, (uint)AccessMode.LOCKED);
		}

		public static Handle CreateDirectory(Handle n, String name, uint access)
		{
			if (n.IsFile)
				throw new DirectoryNotFoundException(
					"Directory was not found");

			DirectoryNode dir = n.Dir.SubDirectories.Find(delegate(DirectoryNode node)
			{
				return node.Name == name;
			});

			if (dir != null)
			{
				if ((access & (uint)AccessMode.OPEN_OR_CREATE) == 0)
				{
					if ((access & (uint)AccessMode.CREATE_ONLY) != 0)
						throw new System.IO.IOException(
							"Directory already exists");
					if ((access & (uint)AccessMode.OPEN_ONLY) != 0)
						throw new System.IO.IOException(
							"Directory doest not exists");
				}

				return new Handle(n.fs, dir);
			}

			DirectoryNode dNode = new DirectoryNode(n.Dir, name);

			if ((access & (uint)AccessMode.LOCKED) != 0)
				dNode.Lock();

			n.Dir.SubDirectories.Add(dNode);

			return new Handle(n.fs, dNode);
		}

		public static FileNode GetFile(Handle h)
		{
			if (h.IsFile)
				return h.File;
			else
				return null;
		}

		public static object GetFileData(Handle h)
		{
			if (h.IsFile)
				return h.File.Data;
			else
				return null;
		}

		public static Type GetFileDataType(Handle h)
		{
			if (h.IsFile)
				return h.File.DataType;
			else
				return null;
		}

		public static DirectoryNode GetDirectory(Handle h)
		{
			if (h.IsDirectory)
				return h.Dir;
			else
				return null;
		}

		public static String[] GetResolvedName(String str)
		{
			String[] resolved = str.Split(Seperators);

			if (resolved[resolved.Length - 1].Length == 0)
				Array.Resize(ref resolved, resolved.Length - 1);

			return resolved;
		}

		private BinaryFormatter Formatter { get; set; }
	}
}
