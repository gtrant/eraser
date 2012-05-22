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

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Eraser.Util
{
	public class VolumeInfo
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="volumeId">The ID of the volume, either in the form
		/// "\\?\Volume{GUID}\" or as a valid UNC path.</param>
		public VolumeInfo(string volumeId)
		{
			//We only accept UNC paths as well as volume identifiers.
			if (!(volumeId.StartsWith("\\\\?\\") || volumeId.StartsWith("\\\\")))
				throw new ArgumentException("The volumeId parameter only accepts volume GUID " +
					"and UNC paths", "volumeId");

			//Verify that the path ends with a trailing backslash
			if (!volumeId.EndsWith("\\"))
				throw new ArgumentException("The volumeId parameter must end with a trailing " +
					"backslash.", "volumeId");

			//Set the volume ID
			VolumeId = volumeId;

			//Fill up the remaining members of the structure: file system, label, etc.
			StringBuilder volumeName = new StringBuilder(NativeMethods.MaxPath * sizeof(char)),
				fileSystemName = new StringBuilder(NativeMethods.MaxPath * sizeof(char));
			uint serialNumber, maxComponentLength, filesystemFlags;
			if (NativeMethods.GetVolumeInformation(volumeId, volumeName, NativeMethods.MaxPath,
				out serialNumber, out maxComponentLength, out filesystemFlags, fileSystemName,
				NativeMethods.MaxPath))
			{
				IsReady = true;
			}

			//If GetVolumeInformation returns zero some of the information may
			//have been stored, so we just try to extract it.
			VolumeLabel = volumeName.Length == 0 ? null : volumeName.ToString();
			VolumeFormat = fileSystemName.Length == 0 ? null : fileSystemName.ToString();

			//Determine whether it is FAT12 or FAT16
			if (VolumeFormat == "FAT")
			{
				uint clusterSize, sectorSize, freeClusters, totalClusters;
				if (NativeMethods.GetDiskFreeSpace(VolumeId, out clusterSize,
					out sectorSize, out freeClusters, out totalClusters))
				{
					if (totalClusters <= 0xFF0)
						VolumeFormat += "12";
					else
						VolumeFormat += "16";
				}
			}
		}

		/// <summary>
		/// Gets the mountpoints associated with the current volume.
		/// </summary>
		/// <returns>A list of volume mount points for the current volume.</returns>
		private List<string> GetLocalVolumeMountPoints()
		{
			return new List<string>(NativeMethods.GetVolumePathNamesForVolumeName(VolumeId));
		}

		/// <summary>
		/// Gets the mountpoints associated with the network share.
		/// </summary>
		/// <returns>A list of network mount points for the given network share.</returns>
		private List<string> GetNetworkMountPoints()
		{
			List<string> result = new List<string>();
			foreach (KeyValuePair<string, string> mountpoint in GetNetworkDrivesInternal())
				if (mountpoint.Value == VolumeId)
					result.Add(mountpoint.Key);

			return result;
		}

		/// <summary>
		/// Lists all the volumes in the system.
		/// </summary>
		/// <returns>Returns a list of volumes representing all volumes present in
		/// the system.</returns>
		public static IList<VolumeInfo> Volumes
		{
			get
			{
				List<VolumeInfo> result = new List<VolumeInfo>();
				StringBuilder nextVolume = new StringBuilder(NativeMethods.LongPath * sizeof(char));
				using (NativeMethods.SafeFindVolumeHandle handle = NativeMethods.FindFirstVolume(
					nextVolume, NativeMethods.LongPath))
				{
					if (handle.IsInvalid)
						return result;

					//Iterate over the volume mountpoints
					do
						result.Add(new VolumeInfo(nextVolume.ToString()));
					while (NativeMethods.FindNextVolume(handle, nextVolume, NativeMethods.LongPath));

					return result.AsReadOnly();
				}
			}
		}

		/// <summary>
		/// Lists all mounted network drives on the current computer.
		/// </summary>
		public static IList<VolumeInfo> NetworkDrives
		{
			get
			{
				Dictionary<string, string> localToRemote = GetNetworkDrivesInternal();
				Dictionary<string, string> remoteToLocal = new Dictionary<string, string>();

				//Flip the dictionary to be indexed by value so we can map UNC paths to
				//drive letters/mount points.
				foreach (KeyValuePair<string, string> mountpoint in localToRemote)
				{
					//If there are no UNC path for this current mount point, we just add it.
					if (!remoteToLocal.ContainsKey(mountpoint.Value))
						remoteToLocal.Add(mountpoint.Value, mountpoint.Key);

					//Otherwise, we try to maintain the shortest path.
					else if (remoteToLocal[mountpoint.Value].Length > mountpoint.Key.Length)
						remoteToLocal[mountpoint.Value] = mountpoint.Key;
				}

				//Return the list of UNC paths mounted.
				List<VolumeInfo> result = new List<VolumeInfo>();
				foreach (string uncPath in remoteToLocal.Keys)
					result.Add(new VolumeInfo(uncPath));

				return result.AsReadOnly();
			}
		}

		/// <summary>
		/// Lists all mounted network drives on the current computer. The key is
		/// the local path, the value is the remote path.
		/// </summary>
		private static Dictionary<string, string> GetNetworkDrivesInternal()
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			//Open an enumeration handle to list mount points.
			IntPtr enumHandle;
			uint errorCode = NativeMethods.WNetOpenEnum(NativeMethods.RESOURCE_CONNECTED,
				NativeMethods.RESOURCETYPE_DISK, 0, IntPtr.Zero, out enumHandle);
			if (errorCode != Win32ErrorCode.Success)
				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

			try
			{
				int resultBufferCount = 32;
				int resultBufferSize = resultBufferCount *
					Marshal.SizeOf(typeof(NativeMethods.NETRESOURCE));
				IntPtr resultBuffer = Marshal.AllocHGlobal(resultBufferSize);

				try
				{
					for ( ; ; )
					{
						uint resultBufferStored = (uint)resultBufferCount;
						uint resultBufferRequiredSize = (uint)resultBufferSize;
						errorCode = NativeMethods.WNetEnumResource(enumHandle,
							ref resultBufferStored, resultBuffer,
							ref resultBufferRequiredSize);

						if (errorCode == Win32ErrorCode.NoMoreItems)
							break;
						else if (errorCode != Win32ErrorCode.Success)
							throw new Win32Exception((int)errorCode);

						unsafe
						{
							//Marshal the memory block to managed structures.
							byte* pointer = (byte*)resultBuffer.ToPointer();

							for (uint i = 0; i < resultBufferStored;
								++i, pointer += Marshal.SizeOf(typeof(NativeMethods.NETRESOURCE)))
							{
								NativeMethods.NETRESOURCE resource =
									(NativeMethods.NETRESOURCE)Marshal.PtrToStructure(
										(IntPtr)pointer, typeof(NativeMethods.NETRESOURCE));

								//Skip all UNC paths without a local mount point
								if (resource.lpLocalName == null)
									continue;

								//Ensure that the path in the resource structure ends with a trailing
								//backslash as out volume ID ends with one.
								if (string.IsNullOrEmpty(resource.lpRemoteName))
									continue;
								if (resource.lpRemoteName[resource.lpRemoteName.Length - 1] != '\\')
									resource.lpRemoteName += '\\';
								result.Add(resource.lpLocalName, resource.lpRemoteName);
							}
						}
					}
				}
				finally
				{
					Marshal.FreeHGlobal(resultBuffer);
				}
			}
			finally
			{
				NativeMethods.WNetCloseEnum(enumHandle);
			}

			return result;
		}

		/// <summary>
		/// Creates a Volume object from its mountpoint.
		/// </summary>
		/// <param name="mountPoint">The path to the mountpoint.</param>
		/// <returns>The volume object if such a volume exists, or an exception
		/// is thrown.</returns>
		public static VolumeInfo FromMountPoint(string mountPoint)
		{
			//Verify that the mountpoint given exists; if it doesn't we'll raise
			//a DirectoryNotFound exception.
			DirectoryInfo mountpointDir = new DirectoryInfo(mountPoint);
			if (!mountpointDir.Exists)
				throw new DirectoryNotFoundException();

			do
			{
				//Ensure that the current path has a trailing backslash
				string currentDir = mountpointDir.FullName;
				if (currentDir.Length > 0 && currentDir[currentDir.Length - 1] != '\\')
					currentDir += '\\';

				//The path cannot be empty.
				if (string.IsNullOrEmpty(currentDir))
					throw new DirectoryNotFoundException();

				//Get the type of the drive
				DriveType driveType = (DriveType)NativeMethods.GetDriveType(currentDir);

				//We do different things for different kinds of drives. Network drives
				//will need us to resolve the drive to a UNC path. Local drives will
				//be resolved to a volume GUID
				StringBuilder volumeID = new StringBuilder(NativeMethods.MaxPath);
				if (driveType == DriveType.Network)
				{
					//If the current directory is a UNC path, then return the VolumeInfo instance
					//directly
					if (currentDir.Substring(0, 2) == "\\\\" && currentDir.IndexOf('\\', 2) != -1)
						return new VolumeInfo(currentDir);

					//Otherwise, resolve the mountpoint to a UNC path
					uint bufferCapacity = (uint)volumeID.Capacity;
					uint errorCode = NativeMethods.WNetGetConnection(
						currentDir.Substring(0, currentDir.Length - 1),
						volumeID, ref bufferCapacity);

					switch (errorCode)
					{
						case Win32ErrorCode.Success:
							return new VolumeInfo(volumeID.ToString() + '\\');

						case Win32ErrorCode.BadDevice: //Path is not a network share
							break;

						default:
							throw new Win32Exception((int)errorCode);
					}
				}
				else
				{
					//If this is a mountpoint, resolve it before calling
					//GetVolumeNameForVolumeMountPoint since it will return an error if
					//the path given is a reparse point, but not a volume reparse point.
					while (mountpointDir.Exists &&
						(mountpointDir.Attributes & FileAttributes.ReparsePoint) != 0)
					{
						currentDir = ExtensionMethods.PathUtil.ResolveReparsePoint(currentDir);

						//If we get a volume identifier, we need to see if it is the only thing
						//in the path. If it is, we found our volume GUID and we won't have to
						//call GetVolumeNameForVolumeMountPoint.
						if (currentDir.StartsWith("\\??\\Volume{"))
						{
							if (currentDir.Length == 49 && currentDir.EndsWith("}\\"))
								return new VolumeInfo(string.Format(CultureInfo.InvariantCulture,
									"\\\\?\\Volume{{{0}}}\\", currentDir.Substring(11, 36)));
							else
								throw new ArgumentException(S._("The path provided includes a " +
									"reparse point which references another volume."));
						}

						//Strip the NT namespace bit
						else
							currentDir = currentDir.Substring(4);
						mountpointDir = new DirectoryInfo(currentDir);
					}

					if (!NativeMethods.GetVolumeNameForVolumeMountPoint(currentDir, volumeID, 50))
					{
						int errorCode = Marshal.GetLastWin32Error();
						switch (errorCode)
						{
							case Win32ErrorCode.InvalidFunction:
							case Win32ErrorCode.FileNotFound:
							case Win32ErrorCode.PathNotFound:
							case Win32ErrorCode.NotAReparsePoint:
								break;
							default:
								throw Win32ErrorCode.GetExceptionForWin32Error(
									Marshal.GetLastWin32Error());
						}
					}
					else
					{
						return new VolumeInfo(volumeID.ToString());
					}
				}

				mountpointDir = mountpointDir.Parent;
			}
			while (mountpointDir != null);

			throw Win32ErrorCode.GetExceptionForWin32Error(Win32ErrorCode.NotAReparsePoint);
		}

		/// <summary>
		/// Returns the volume identifier as would be returned from FindFirstVolume.
		/// </summary>
		public string VolumeId { get; private set; }

		/// <summary>
		/// Gets or sets the volume label of a drive.
		/// </summary>
		public string VolumeLabel { get; private set; }

		/// <summary>
		/// Gets the name of the file system, such as NTFS or FAT32.
		/// </summary>
		public string VolumeFormat { get; private set; }

		/// <summary>
		/// Gets the drive type; returns one of the System.IO.DriveType values.
		/// </summary>
		public DriveType VolumeType
		{
			get
			{
				return (DriveType)NativeMethods.GetDriveType(VolumeId);
			}
		}

		/// <summary>
		/// Determines the cluster size of the current volume.
		/// </summary>
		public int ClusterSize
		{
			get
			{
				if (!IsReady)
					throw new InvalidOperationException("The volume has not been mounted or is not " +
						"currently ready.");

				uint clusterSize, sectorSize, freeClusters, totalClusters;
				if (NativeMethods.GetDiskFreeSpace(VolumeId, out clusterSize,
					out sectorSize, out freeClusters, out totalClusters))
				{
					return (int)(clusterSize * sectorSize);
				}

				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Determines the sector size of the current volume.
		/// </summary>
		public int SectorSize
		{
			get
			{
				if (!IsReady)
					throw new InvalidOperationException("The volume has not been mounted or is not " +
						"currently ready.");

				uint clusterSize, sectorSize, freeClusters, totalClusters;
				if (NativeMethods.GetDiskFreeSpace(VolumeId, out clusterSize,
					out sectorSize, out freeClusters, out totalClusters))
				{
					return (int)sectorSize;
				}

				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Checks if the current user has disk quotas on the current volume.
		/// </summary>
		public bool HasQuota
		{
			get
			{
				ulong freeBytesAvailable, totalNumberOfBytes, totalNumberOfFreeBytes;
				if (NativeMethods.GetDiskFreeSpaceEx(VolumeId, out freeBytesAvailable,
					out totalNumberOfBytes, out totalNumberOfFreeBytes))
				{
					return totalNumberOfFreeBytes != freeBytesAvailable;
				}
				else if (Marshal.GetLastWin32Error() == Win32ErrorCode.NotReady)
				{
					//For the lack of more appropriate responses.
					return false;
				}

				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Gets a value indicating whether a drive is ready.
		/// </summary>
		public bool IsReady { get; private set; }

		/// <summary>
		/// Gets the total amount of free space available on a drive.
		/// </summary>
		public long TotalFreeSpace
		{
			get
			{
				if (!IsReady)
					throw new InvalidOperationException("The volume has not been mounted or is not " +
						"currently ready.");

				ulong result, dummy;
				if (NativeMethods.GetDiskFreeSpaceEx(VolumeId, out dummy, out dummy, out result))
				{
					return (long)result;
				}

				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}
		
		/// <summary>
		/// Gets the total size of storage space on a drive.
		/// </summary>
		public long TotalSize
		{
			get
			{
				ulong result, dummy;
				if (NativeMethods.GetDiskFreeSpaceEx(VolumeId, out dummy, out result, out dummy))
				{
					return (long)result;
				}

				//Try the alternative method
				using (SafeFileHandle handle = OpenHandle(NativeMethods.GENERIC_READ,
					FileShare.ReadWrite, FileOptions.None))
				{
					if (handle.IsInvalid)
						throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

					long result2;
					uint returned = 0;
					if (NativeMethods.DeviceIoControl(handle,
						NativeMethods.IOCTL_DISK_GET_LENGTH_INFO, IntPtr.Zero, 0, out result2,
						sizeof(long), out returned, IntPtr.Zero))
					{
						return result2;
					}
				}

				//Otherwise, throw
				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Indicates the amount of available free space on a drive.
		/// </summary>
		public long AvailableFreeSpace
		{
			get
			{
				if (!IsReady)
					throw new InvalidOperationException("The volume has not been mounted or is not " +
						"currently ready.");

				ulong result, dummy;
				if (NativeMethods.GetDiskFreeSpaceEx(VolumeId, out result, out dummy, out dummy))
				{
					return (long)result;
				}

				throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// Retrieves all mountpoints in the current volume, if the current volume
		/// contains volume mountpoints.
		/// </summary>
		public IList<VolumeInfo> MountedVolumes
		{
			get
			{
				if (!IsReady)
					throw new InvalidOperationException("The volume has not been mounted or is not " +
						"currently ready.");

				List<VolumeInfo> result = new List<VolumeInfo>();
				StringBuilder nextMountpoint = new StringBuilder(NativeMethods.LongPath * sizeof(char));

				using (NativeMethods.SafeFindVolumeMountPointHandle handle =
					NativeMethods.FindFirstVolumeMountPoint(VolumeId, nextMountpoint,
					NativeMethods.LongPath))
				{
					if (handle.IsInvalid)
						return result;

					//Iterate over the volume mountpoints
					while (NativeMethods.FindNextVolumeMountPoint(handle, nextMountpoint,
						NativeMethods.LongPath))
					{
						result.Add(new VolumeInfo(nextMountpoint.ToString()));
					}

					return result.AsReadOnly();
				}
			}
		}

		/// <summary>
		/// The various mountpoints to the root of the volume. This list contains
		/// paths which may be a drive or a mountpoint. Every string includes the
		/// trailing backslash.
		/// </summary>
		public IList<DirectoryInfo> MountPoints
		{
			get
			{
				List<string> paths = VolumeType == DriveType.Network ?
					GetNetworkMountPoints() : GetLocalVolumeMountPoints();
				return new List<DirectoryInfo>(
					paths.Select(x => new DirectoryInfo(x))).AsReadOnly();
			}
		}

		/// <summary>
		/// Gets whether the current volume is mounted at any place.
		/// </summary>
		public bool IsMounted
		{
			get
			{
				try
				{
					return MountPoints.Count != 0;
				}
				catch (FileNotFoundException)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Gets the Physical Drive this Volume is in.
		/// </summary>
		public PhysicalDriveInfo PhysicalDrive
		{
			get
			{
				IntPtr buffer = IntPtr.Zero;
				List<NativeMethods.DISK_EXTENT> extents = new List<NativeMethods.DISK_EXTENT>();
				SafeFileHandle handle = OpenHandle(NativeMethods.FILE_READ_ATTRIBUTES,
					FileShare.ReadWrite, FileOptions.None);

				try
				{
					uint returnSize = 0;
					int bufferSize = Marshal.SizeOf(typeof(NativeMethods.VOLUME_DISK_EXTENTS));
					buffer = Marshal.AllocHGlobal(bufferSize);
					NativeMethods.VOLUME_DISK_EXTENTS header;

					while (!NativeMethods.DeviceIoControl(handle,
						NativeMethods.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0,
						buffer, (uint)bufferSize, out returnSize, IntPtr.Zero))
					{
						int error = Marshal.GetLastWin32Error();
						if (error == Win32ErrorCode.InvalidFunction ||
							error == Win32ErrorCode.NotSupported)
							return null;
						else if (error != Win32ErrorCode.MoreData)
							throw Win32ErrorCode.GetExceptionForWin32Error(error);

						buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)(bufferSize *= 2));
					}

					//Parse the structure.
					header = (NativeMethods.VOLUME_DISK_EXTENTS)Marshal.PtrToStructure(buffer,
						typeof(NativeMethods.VOLUME_DISK_EXTENTS));
					extents.Add(header.Extent);

					for (long i = 1, offset = (uint)Marshal.SizeOf(typeof(
						NativeMethods.VOLUME_DISK_EXTENTS)); i < header.NumberOfDiskExtents;
						++i, offset += Marshal.SizeOf(typeof(NativeMethods.DISK_EXTENT)))
					{
						NativeMethods.DISK_EXTENT extent = (NativeMethods.DISK_EXTENT)
							Marshal.PtrToStructure(new IntPtr(buffer.ToInt64() + offset),
							typeof(NativeMethods.DISK_EXTENT));
						extents.Add(extent);
					}
				}
				finally
				{
					handle.Close();
					Marshal.FreeHGlobal(buffer);
				}

				if (extents.Count == 1)
					return new PhysicalDriveInfo((int)extents[0].DiskNumber);
				return null;
			}
		}

		/// <summary>
		/// Opens a file with read, write, or read/write access.
		/// </summary>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <returns>A System.IO.FileStream object opened in the specified mode
		/// and access, unshared, and no special file options.</returns>
		public FileStream Open(FileAccess access)
		{
			return Open(access, FileShare.None, FileOptions.None);
		}

		/// <summary>
		/// Opens a file with read, write, or read/write access and the specified
		/// sharing option.
		/// </summary>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <param name="share">A System.IO.FileShare constant specifying the type
		/// of access other FileStream objects have to this file.</param>
		/// <returns>A System.IO.FileStream object opened with the specified mode,
		/// access, sharing options, and no special file options.</returns>
		public FileStream Open(FileAccess access, FileShare share)
		{
			return Open(access, share, FileOptions.None);
		}

		/// <summary>
		/// Opens a file with read, write, or read/write access, the specified
		/// sharing option, and other advanced options.
		/// </summary>
		/// <param name="mode">A System.IO.FileMode constant specifying the mode
		/// (for example, Open or Append) in which to open the file.</param>
		/// <param name="access">A System.IO.FileAccess constant specifying whether
		/// to open the file with Read, Write, or ReadWrite file access.</param>
		/// <param name="share">A System.IO.FileShare constant specifying the type
		/// of access other FileStream objects have to this file.</param>
		/// <param name="options">The System.IO.FileOptions constant specifying
		/// the advanced file options to use when opening the file.</param>
		/// <returns>A System.IO.FileStream object opened with the specified mode,
		/// access, sharing options, and special file options.</returns>
		public FileStream Open(FileAccess access, FileShare share, FileOptions options)
		{
			return new VolumeStream(this, OpenHandle(access, share, options), access);
		}

		private SafeFileHandle OpenHandle(FileAccess access, FileShare share, FileOptions options)
		{
			//Access mode
			uint iAccess = 0;
			switch (access)
			{
				case FileAccess.Read:
					iAccess = NativeMethods.GENERIC_READ;
					break;
				case FileAccess.ReadWrite:
					iAccess = NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE;
					break;
				case FileAccess.Write:
					iAccess = NativeMethods.GENERIC_WRITE;
					break;
			}

			return OpenHandle(iAccess, share, options);
		}

		private SafeFileHandle OpenHandle(uint access, FileShare share, FileOptions options)
		{
			//Sharing mode
			if ((share & FileShare.Inheritable) != 0)
				throw new NotSupportedException("Inheritable handles are not supported.");

			//Advanced options
			if ((options & FileOptions.Asynchronous) != 0)
				throw new NotSupportedException("Asynchronous handles are not implemented.");

			//Create the handle
			string openPath = VolumeId;
			if (openPath.Length > 0 && openPath[openPath.Length - 1] == '\\')
				openPath = openPath.Remove(openPath.Length - 1);

			SafeFileHandle result = NativeMethods.CreateFile(openPath, access, (uint)share,
				IntPtr.Zero, (uint)FileMode.Open, (uint)options, IntPtr.Zero);

			//Check that the handle is valid
			if (result.IsInvalid)
			{
				int errorCode = Marshal.GetLastWin32Error();
				result.Close();
				throw Win32ErrorCode.GetExceptionForWin32Error(errorCode);
			}

			return result;
		}

		/// <summary>
		/// Queries the performance information for the given disk.
		/// </summary>
		public DiskPerformanceInfo Performance
		{
			get
			{
				using (SafeFileHandle handle = OpenHandle(NativeMethods.FILE_READ_ATTRIBUTES,
					FileShare.ReadWrite, FileOptions.None))
				{
					//This only works if the user has turned on the disk performance
					//counters with 'diskperf -y'. These counters are off by default
					NativeMethods.DiskPerformanceInfoInternal result =
						new NativeMethods.DiskPerformanceInfoInternal();
					uint bytesReturned = 0;
					if (NativeMethods.DeviceIoControl(handle, NativeMethods.IOCTL_DISK_PERFORMANCE,
						IntPtr.Zero, 0, out result, (uint)Marshal.SizeOf(result),
						out bytesReturned, IntPtr.Zero))
					{
						return new DiskPerformanceInfo(result);
					}

					return null;
				}
			}
		}

		/// <summary>
		/// Gets the mount point of the volume, or the volume ID if the volume is
		/// not currently mounted.
		/// </summary>
		/// <returns>A string containing the mount point of the volume or the volume
		/// ID.</returns>
		public override string ToString()
		{
			IList<DirectoryInfo> mountPoints = MountPoints;
			return mountPoints.Count == 0 ? VolumeId : mountPoints[0].FullName;
		}

		public override bool Equals(object obj)
		{
			VolumeInfo rhs = obj as VolumeInfo;
			if (rhs == null)
				return base.Equals(obj);

			return VolumeId == rhs.VolumeId;
		}

		public override int GetHashCode()
		{
			return VolumeId.GetHashCode();
		}
	}

	public class VolumeStream : FileStream
	{
		internal VolumeStream(VolumeInfo volume, SafeFileHandle handle, FileAccess access)
			: base(handle, access)
		{
			Volume = volume;
			Access = access;

			if (Access == FileAccess.Write || Access == FileAccess.ReadWrite)
				LockVolume();
		}

		protected override void Dispose(bool disposing)
		{
			if (Access == FileAccess.Write || Access == FileAccess.ReadWrite)
				UnlockVolume();
			base.Dispose(disposing);
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		public override long Length
		{
			get
			{
				if (IsLocked)
					return LengthCache;
				return Volume.TotalSize;
			}
		}

		/// <summary>
		/// Temporarily stores the length of the disk while the disk is locked.
		/// </summary>
		private long LengthCache;

		private void LockVolume()
		{
			LengthCache = Length;
			uint result = 0;
			for (int i = 0; !NativeMethods.DeviceIoControl(SafeFileHandle,
					NativeMethods.FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero,
					0, out result, IntPtr.Zero); ++i)
			{
				if (i > 100)
					throw Win32ErrorCode.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
				System.Threading.Thread.Sleep(100);
			}

			IsLocked = true;
		}

		private void UnlockVolume()
		{
			//Flush the contents of the buffer to disk since after we unlock the volume
			//we can no longer write to the volume.
			Flush();

			uint result = 0;
			if (!NativeMethods.DeviceIoControl(SafeFileHandle, NativeMethods.FSCTL_UNLOCK_VOLUME,
				IntPtr.Zero, 0, IntPtr.Zero, 0, out result, IntPtr.Zero))
			{
				throw new IOException("Could not unlock volume.");
			}

			LengthCache = 0;
			IsLocked = false;
		}

		/// <summary>
		/// Reflects whether the current handle has exclusive access to the volume.
		/// </summary>
		private bool IsLocked;

		/// <summary>
		/// The <see cref="VolumeInfo"/> object this stream is encapsulating.
		/// </summary>
		private VolumeInfo Volume;

		/// <summary>
		/// The access parameter for this stream.
		/// </summary>
		private FileAccess Access;
	}

	public class DiskPerformanceInfo
	{
		internal DiskPerformanceInfo(NativeMethods.DiskPerformanceInfoInternal info)
		{
			BytesRead = info.BytesRead;
			BytesWritten = info.BytesWritten;
			ReadTime = info.ReadTime;
			WriteTime = info.WriteTime;
			IdleTime = info.IdleTime;
			ReadCount = info.ReadCount;
			WriteCount = info.WriteCount;
			QueueDepth = info.QueueDepth;
			SplitCount = info.SplitCount;
			QueryTime = info.QueryTime;
			StorageDeviceNumber = info.StorageDeviceNumber;
			StorageManagerName = info.StorageManagerName;
		}

		public long BytesRead { get; private set; }
		public long BytesWritten { get; private set; }
		public long ReadTime { get; private set; }
		public long WriteTime { get; private set; }
		public long IdleTime { get; private set; }
		public uint ReadCount { get; private set; }
		public uint WriteCount { get; private set; }
		public uint QueueDepth { get; private set; }
		public uint SplitCount { get; private set; }
		public long QueryTime { get; private set; }
		public uint StorageDeviceNumber { get; private set; }
		public string StorageManagerName { get; private set; }
	}
}