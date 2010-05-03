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

using System.Text.RegularExpressions;
using System.Security.Permissions;
using System.IO;
using System.Runtime.Serialization;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;

namespace Eraser.Manager
{
	/// <summary>
	/// Represents a generic target of erasure
	/// </summary>
	[Serializable]
	public abstract class ErasureTarget : ISerializable
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
		/// Retrieves the text to display representing this task.
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
		/// Executes the given task.
		/// </summary>
		/// <param name="progress">The progress manager instance which is used to
		/// track the progress of the current target's erasure.</param>
		public virtual void Execute(ProgressManagerBase progress)
		{
		}

		/// <summary>
		/// The backing variable for the <see cref="Method"/> property.
		/// </summary>
		private ErasureMethod method;
	}

	/// <summary>
	/// Represents an interface for an abstract erasure target configuration
	/// object.
	/// </summary>
	public interface IErasureTargetConfigurer
	{
		/// <summary>
		/// Loads the configuration from the provided erasure target.
		/// </summary>
		/// <param name="target">The erasure target to load the configuration from.</param>
		void LoadFrom(ErasureTarget target);

		/// <summary>
		/// Configures the provided erasure target.
		/// </summary>
		/// <param name="target">The erasure target to configure.</param>
		/// <returns>True if the configuration was valid and the save operation
		/// succeeded.</returns>
		bool SaveTo(ErasureTarget target);
	}

	/// <summary>
	/// Class representing a tangible object (file/folder) to be erased.
	/// </summary>
	[Serializable]
	public abstract class FileSystemObjectTarget : ErasureTarget
	{
		#region Serialization code
		protected FileSystemObjectTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Path = (string)info.GetValue("Path", typeof(string));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Path", Path);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected FileSystemObjectTarget()
			: base()
		{
		}

		/// <summary>
		/// Retrieves the list of files/folders to erase as a list.
		/// </summary>
		/// <param name="totalSize">Returns the total size in bytes of the
		/// items.</param>
		/// <returns>A list containing the paths to all the files to be erased.</returns>
		internal abstract List<string> GetPaths(out long totalSize);

		/// <summary>
		/// Adds ADSes of the given file to the list.
		/// </summary>
		/// <param name="list">The list to add the ADS paths to.</param>
		/// <param name="file">The file to look for ADSes</param>
		protected void GetPathADSes(ICollection<string> list, out long totalSize, string file)
		{
			totalSize = 0;

			try
			{
				//Get the ADS names
				IList<string> adses = new FileInfo(file).GetADSes();

				//Then prepend the path.
				foreach (string adsName in adses)
				{
					string adsPath = file + ':' + adsName;
					list.Add(adsPath);
					StreamInfo info = new StreamInfo(adsPath);
					totalSize += info.Length;
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (SharingViolationException)
			{
				//The system cannot open the file, try to force the file handle to close.
				if (!ManagerLibrary.Settings.ForceUnlockLockedFiles)
					throw;

				StringBuilder processStr = new StringBuilder();
				foreach (OpenHandle handle in OpenHandle.Close(file))
				{
					try
					{
						processStr.AppendFormat(
							System.Globalization.CultureInfo.InvariantCulture,
							"{0}, ", (System.Diagnostics.Process.GetProcessById(handle.ProcessId)).MainModule.FileName);
					}
					catch (System.ComponentModel.Win32Exception)
					{
						processStr.AppendFormat(
							System.Globalization.CultureInfo.InvariantCulture,
							"Process ID {0}, ", handle.ProcessId);
					}
				}

				if (processStr.Length == 0)
				{
					GetPathADSes(list, out totalSize, file);
					return;
				}
				else
					throw;
			}
			catch (UnauthorizedAccessException e)
			{
				//The system cannot read the file, assume no ADSes for lack of
				//more information.
				Logger.Log(e.Message, LogLevel.Error);
			}
		}

		/// <summary>
		/// The path to the file or folder referred to by this object.
		/// </summary>
		public string Path { get; set; }

		public sealed override ErasureMethod EffectiveMethod
		{
			get
			{
				if (Method != ErasureMethodRegistrar.Default)
					return base.EffectiveMethod;

				return ManagerLibrary.Instance.ErasureMethodRegistrar[
					ManagerLibrary.Settings.DefaultFileErasureMethod];
			}
		}

		public override string UIText
		{
			get
			{
				string fileName = System.IO.Path.GetFileName(Path);
				string directoryName = System.IO.Path.GetDirectoryName(Path);
				return string.IsNullOrEmpty(fileName) ?
						(string.IsNullOrEmpty(directoryName) ? Path : directoryName)
					: fileName;
			}
		}
	}

	/// <summary>
	/// Class representing a unused space erase.
	/// </summary>
	[Serializable]
	public class UnusedSpaceTarget : ErasureTarget
	{
		#region Serialization code
		protected UnusedSpaceTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Drive = (string)info.GetValue("Drive", typeof(string));
			EraseClusterTips = (bool)info.GetValue("EraseClusterTips", typeof(bool));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Drive", Drive);
			info.AddValue("EraseClusterTips", EraseClusterTips);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public UnusedSpaceTarget()
			: base()
		{
		}

		public sealed override ErasureMethod EffectiveMethod
		{
			get
			{
				if (Method == ErasureMethodRegistrar.Default)
					return base.EffectiveMethod;

				return ManagerLibrary.Instance.ErasureMethodRegistrar[
					ManagerLibrary.Settings.DefaultUnusedSpaceErasureMethod];
			}
		}

		public override bool SupportsMethod(ErasureMethod method)
		{
			return method == ErasureMethodRegistrar.Default ||
				method is UnusedSpaceErasureMethod;
		}

		public override string UIText
		{
			get { return S._("Unused disk space ({0})", Drive); }
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new UnusedSpaceErasureTargetSettings(); }
		}

		/// <summary>
		/// The drive to erase
		/// </summary>
		public string Drive { get; set; }

		/// <summary>
		/// Whether cluster tips should be erased.
		/// </summary>
		public bool EraseClusterTips { get; set; }
	}

	/// <summary>
	/// Class representing a file to be erased.
	/// </summary>
	[Serializable]
	public class FileTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected FileTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FileTarget()
		{
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FileErasureTargetSettings(); }
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<string> result = new List<string>();
			FileInfo fileInfo = new FileInfo(Path);

			if (fileInfo.Exists)
			{
				GetPathADSes(result, out totalSize, Path);
				totalSize += fileInfo.Length;
			}

			result.Add(Path);
			return result;
		}
	}

	/// <summary>
	/// Represents a folder and its files which are to be erased.
	/// </summary>
	[Serializable]
	public class FolderTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected FolderTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			IncludeMask = (string)info.GetValue("IncludeMask", typeof(string));
			ExcludeMask = (string)info.GetValue("ExcludeMask", typeof(string));
			DeleteIfEmpty = (bool)info.GetValue("DeleteIfEmpty", typeof(bool));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("IncludeMask", IncludeMask);
			info.AddValue("ExcludeMask", ExcludeMask);
			info.AddValue("DeleteIfEmpty", DeleteIfEmpty);
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public FolderTarget()
		{
			IncludeMask = string.Empty;
			ExcludeMask = string.Empty;
			DeleteIfEmpty = true;
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return new FolderErasureTargetSettings(); }
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			//Get a list to hold all the resulting paths.
			List<string> result = new List<string>();

			//Open the root of the search, including every file matching the pattern
			DirectoryInfo dir = new DirectoryInfo(Path);

			//List recursively all the files which match the include pattern.
			FileInfo[] files = GetFiles(dir);

			//Then exclude each file and finalize the list and total file size
			totalSize = 0;
			if (ExcludeMask.Length != 0)
			{
				string regex = Regex.Escape(ExcludeMask).Replace("\\*", ".*").
					Replace("\\?", ".");
				Regex excludePattern = new Regex(regex, RegexOptions.IgnoreCase);
				foreach (FileInfo file in files)
					if (file.Exists &&
						(file.Attributes & FileAttributes.ReparsePoint) == 0 &&
						excludePattern.Matches(file.FullName).Count == 0)
					{
						totalSize += file.Length;
						GetPathADSes(result, out totalSize, file.FullName);
						result.Add(file.FullName);
					}
			}
			else
				foreach (FileInfo file in files)
				{
					if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
						continue;

					//Get the size of the file and its ADSes
					totalSize += file.Length;
					long adsesSize = 0;
					GetPathADSes(result, out adsesSize, file.FullName);
					totalSize += adsesSize;

					//Append this file to the list of files to erase.
					result.Add(file.FullName);
				}

			//Return the filtered list.
			return result;
		}

		/// <summary>
		/// Gets all files in the provided directory.
		/// </summary>
		/// <param name="info">The directory to look files in.</param>
		/// <returns>A list of files found in the directory matching the IncludeMask
		/// property.</returns>
		private FileInfo[] GetFiles(DirectoryInfo info)
		{
			List<FileInfo> result = new List<FileInfo>();
			if (info.Exists)
			{
				try
				{
					foreach (DirectoryInfo dir in info.GetDirectories())
						result.AddRange(GetFiles(dir));

					if (IncludeMask.Length == 0)
						result.AddRange(info.GetFiles());
					else
						result.AddRange(info.GetFiles(IncludeMask, SearchOption.TopDirectoryOnly));
				}
				catch (UnauthorizedAccessException e)
				{
					Logger.Log(S._("Could not erase files and subfolders in {0} because {1}",
						info.FullName, e.Message), LogLevel.Error);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// A wildcard expression stating the condition for the set of files to include.
		/// The include mask is applied before the exclude mask is applied. If this value
		/// is empty, all files and folders within the folder specified is included.
		/// </summary>
		public string IncludeMask { get; set; }

		/// <summary>
		/// A wildcard expression stating the condition for removing files from the set
		/// of included files. If this value is omitted, all files and folders extracted
		/// by the inclusion mask is erased.
		/// </summary>
		public string ExcludeMask { get; set; }

		/// <summary>
		/// Determines if Eraser should delete the folder after the erase process.
		/// </summary>
		public bool DeleteIfEmpty { get; set; }
	}

	[Serializable]
	public class RecycleBinTarget : FileSystemObjectTarget
	{
		#region Serialization code
		protected RecycleBinTarget(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		public RecycleBinTarget()
		{
		}

		public override IErasureTargetConfigurer Configurer
		{
			get { return null; }
		}

		internal override List<string> GetPaths(out long totalSize)
		{
			totalSize = 0;
			List<string> result = new List<string>();
			string[] rootDirectory = new string[] {
					"$RECYCLE.BIN",
					"RECYCLER"
				};

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				foreach (string rootDir in rootDirectory)
				{
					DirectoryInfo dir = new DirectoryInfo(
						System.IO.Path.Combine(
							System.IO.Path.Combine(drive.Name, rootDir),
							System.Security.Principal.WindowsIdentity.GetCurrent().
								User.ToString()));
					if (!dir.Exists)
						continue;

					GetRecyclerFiles(dir, result, ref totalSize);
				}
			}

			return result;
		}

		/// <summary>
		/// Retrieves all files within this folder, without exclusions.
		/// </summary>
		/// <param name="info">The DirectoryInfo object representing the folder to traverse.</param>
		/// <param name="paths">The list of files to store path information in.</param>
		/// <param name="totalSize">Receives the total size of the files.</param>
		private void GetRecyclerFiles(DirectoryInfo info, List<string> paths,
			ref long totalSize)
		{
			try
			{
				foreach (FileInfo fileInfo in info.GetFiles())
				{
					if (!fileInfo.Exists || (fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
						continue;

					long adsSize = 0;
					GetPathADSes(paths, out adsSize, fileInfo.FullName);
					totalSize += adsSize;
					totalSize += fileInfo.Length;
					paths.Add(fileInfo.FullName);
				}

				foreach (DirectoryInfo directoryInfo in info.GetDirectories())
					if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) == 0)
						GetRecyclerFiles(directoryInfo, paths, ref totalSize);
			}
			catch (UnauthorizedAccessException e)
			{
				Logger.Log(e.Message, LogLevel.Error);
			}
		}

		/// <summary>
		/// Retrieves the text to display representing this task.
		/// </summary>
		public override string UIText
		{
			get
			{
				return S._("Recycle Bin");
			}
		}
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
					target.Task = owner;
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
