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
using System.Text;

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Collections.ObjectModel;

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
			StringBuilder volumeName = new StringBuilder(KernelApi.NativeMethods.MaxPath);
			StringBuilder fileSystemName = new StringBuilder(KernelApi.NativeMethods.MaxPath);
			uint serialNumber, maxComponentLength, filesystemFlags;
			if (!KernelApi.NativeMethods.GetVolumeInformation(volumeId, volumeName,
				KernelApi.NativeMethods.MaxPath, out serialNumber, out maxComponentLength,
				out filesystemFlags, fileSystemName, KernelApi.NativeMethods.MaxPath))
			{
				int lastError = Marshal.GetLastWin32Error();
				switch (lastError)
				{
					case 0:		//ERROR_NO_ERROR
					case 21:	//ERROR_NOT_READY
					case 87:	//ERROR_INVALID_PARAMETER: when the volume given is not mounted.
					case 1005:	//ERROR_UNRECOGNIZED_VOLUME
					case 1392:  //ERROR_FILE_CORRUPT
						break;

					default:
						throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
				}
			}
			else
			{
				IsReady = true;
				VolumeLabel = volumeName.ToString();
				VolumeFormat = fileSystemName.ToString();

				//Determine whether it is FAT12 or FAT16
				if (VolumeFormat == "FAT")
				{
					uint clusterSize, sectorSize, freeClusters, totalClusters;
					if (KernelApi.NativeMethods.GetDiskFreeSpace(VolumeId, out clusterSize,
						out sectorSize, out freeClusters, out totalClusters))
					{
						if (totalClusters <= 0xFF0)
							VolumeFormat += "12";
						else
							VolumeFormat += "16";
					}
				}
			}
		}

		/// <summary>
		/// Gets the mountpoints associated with the current volume.
		/// </summary>
		/// <returns>A list of volume mount points for the current volume.</returns>
		private List<string> GetLocalVolumeMountPoints()
		{
			List<string> result = new List<string>();

			//Get the paths of the said volume
			IntPtr pathNamesBuffer = IntPtr.Zero;
			string pathNames = string.Empty;
			try
			{
				uint currentBufferSize = KernelApi.NativeMethods.MaxPath;
				uint returnLength = 0;
				pathNamesBuffer = Marshal.AllocHGlobal((int)(currentBufferSize * sizeof(char)));
				while (!KernelApi.NativeMethods.GetVolumePathNamesForVolumeName(VolumeId,
					pathNamesBuffer, currentBufferSize, out returnLength))
				{
					if (Marshal.GetLastWin32Error() == 234/*ERROR_MORE_DATA*/)
					{
						Marshal.FreeHGlobal(pathNamesBuffer);
						currentBufferSize *= 2;
						pathNamesBuffer = Marshal.AllocHGlobal((int)(currentBufferSize * sizeof(char)));
					}
					else
						throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
				}

				pathNames = Marshal.PtrToStringUni(pathNamesBuffer, (int)returnLength);
			}
			finally
			{
				if (pathNamesBuffer != IntPtr.Zero)
					Marshal.FreeHGlobal(pathNamesBuffer);
			}

			//OK, the marshalling is complete. Convert the pathNames string into a list
			//of strings containing all of the volumes mountpoints; because the
			//GetVolumePathNamesForVolumeName function returns a convoluted structure
			//containing the path names.
			for (int lastIndex = 0, i = 0; i != pathNames.Length; ++i)
			{
				if (pathNames[i] == '\0')
				{
					//If there are no mount points for this volume, the string will only
					//have one NULL
					if (i - lastIndex == 0)
						break;

					result.Add(pathNames.Substring(lastIndex, i - lastIndex));

					lastIndex = i + 1;
					if (pathNames[lastIndex] == '\0')
						break;
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the mountpoints associated with the network share.
		/// </summary>
		/// <returns>A list of network mount points for the given network share.</returns>
		private List<string> GetNetworkMountPoints()
		{
			List<string> result = new List<string>();

			//Open an enumeration handle to list mount points.
			IntPtr enumHandle;
			uint errorCode = KernelApi.NativeMethods.WNetOpenEnum(
				KernelApi.NativeMethods.RESOURCE_CONNECTED,
				KernelApi.NativeMethods.RESOURCETYPE_DISK, 0, IntPtr.Zero, out enumHandle);
			if (errorCode != 0 /*ERROR_SUCCESS*/)
				throw new Win32Exception((int)errorCode);

			try
			{
				int resultBufferCount = 32;
				int resultBufferSize = resultBufferCount *
					Marshal.SizeOf(typeof(KernelApi.NativeMethods.NETRESOURCE));
				IntPtr resultBuffer = Marshal.AllocHGlobal(resultBufferSize);

				try
				{
					for ( ; ; )
					{
						uint resultBufferStored = (uint)resultBufferCount;
						uint resultBufferRequiredSize = (uint)resultBufferSize;
						errorCode = KernelApi.NativeMethods.WNetEnumResource(
							enumHandle, ref resultBufferStored, resultBuffer,
							ref resultBufferRequiredSize);

						if (errorCode == 259 /*ERROR_NO_MORE_ITEMS*/)
							break;
						else if (errorCode != 0 /*ERROR_SUCCESS*/)
							throw new Win32Exception((int)errorCode);

						unsafe
						{
							//Marshal the memory block to managed structures.
							byte* pointer = (byte*)resultBuffer.ToPointer();

							for (uint i = 0; i < resultBufferStored;
								++i, pointer += Marshal.SizeOf(typeof(KernelApi.NativeMethods.NETRESOURCE)))
							{
								KernelApi.NativeMethods.NETRESOURCE resource =
									(KernelApi.NativeMethods.NETRESOURCE)Marshal.PtrToStructure(
										(IntPtr)pointer, typeof(KernelApi.NativeMethods.NETRESOURCE));

								//Ensure that the path in the resource structure ends with a trailing
								//backslash as out volume ID ends with one.
								if (string.IsNullOrEmpty(resource.lpRemoteName))
									continue;
								if (resource.lpRemoteName[resource.lpRemoteName.Length - 1] != '\\')
									resource.lpRemoteName += '\\';

								if (resource.lpRemoteName == VolumeId)
									result.Add(resource.lpLocalName);
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
				KernelApi.NativeMethods.WNetCloseEnum(enumHandle);
			}

			return result;
		}

		/// <summary>
		/// Lists all the volumes in the system.
		/// </summary>
		/// <returns>Returns a list of volumes representing all volumes present in
		/// the system.</returns>
		public static ICollection<VolumeInfo> Volumes
		{
			get
			{
				List<VolumeInfo> result = new List<VolumeInfo>();
				StringBuilder nextVolume = new StringBuilder(
					KernelApi.NativeMethods.LongPath * sizeof(char));
				SafeHandle handle = KernelApi.NativeMethods.FindFirstVolume(nextVolume,
					KernelApi.NativeMethods.LongPath);
				if (handle.IsInvalid)
					return result;

				//Iterate over the volume mountpoints
				do
					result.Add(new VolumeInfo(nextVolume.ToString()));
				while (KernelApi.NativeMethods.FindNextVolume(handle, nextVolume,
					KernelApi.NativeMethods.LongPath));

				//Close the handle
				if (Marshal.GetLastWin32Error() == 18 /*ERROR_NO_MORE_FILES*/)
					KernelApi.NativeMethods.FindVolumeClose(handle);

				return result.AsReadOnly();
			}
		}

		/// <summary>
		/// Creates a Volume object from its mountpoint.
		/// </summary>
		/// <param name="mountpoint">The path to the mountpoint.</param>
		/// <returns>The volume object if such a volume exists, or an exception
		/// is thrown.</returns>
		public static VolumeInfo FromMountpoint(string mountpoint)
		{
			DirectoryInfo mountpointDir = new DirectoryInfo(mountpoint);
			
			//Verify that the mountpoint given exists; if it doesn't we'll raise
			//a DirectoryNotFound exception.
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
				DriveType driveType = (DriveType)KernelApi.NativeMethods.GetDriveType(mountpoint);

				//We do different things for different kinds of drives. Network drives
				//will need us to resolve the drive to a UNC path. Local drives will
				//be resolved to a volume GUID
				StringBuilder volumeID = new StringBuilder(KernelApi.NativeMethods.MaxPath);
				if (driveType == DriveType.Network)
				{
					//Resolve the mountpoint to a UNC path
					uint bufferCapacity = (uint)volumeID.Capacity;
					uint errorCode = KernelApi.NativeMethods.WNetGetConnection(
						currentDir.Substring(0, currentDir.Length - 1),
						volumeID, ref bufferCapacity);

					switch (errorCode)
					{
						case 0: //ERROR_SUCCESS
							return new VolumeInfo(volumeID.ToString() + '\\');

						case 1200: //ERROR_BAD_DEVICE: path is not a network share
							break;

						default:
							throw new Win32Exception((int)errorCode);
					}
				}
				else
				{
					if (KernelApi.NativeMethods.GetVolumeNameForVolumeMountPoint(
						currentDir, volumeID, 50))
					{
						return new VolumeInfo(volumeID.ToString());
					}
					else
					{
						switch (Marshal.GetLastWin32Error())
						{
							case 1: //ERROR_INVALID_FUNCTION
							case 2: //ERROR_FILE_NOT_FOUND
							case 3: //ERROR_PATH_NOT_FOUND
							case 4390: //ERROR_NOT_A_REPARSE_POINT
								break;
							default:
								throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
						}
					}
				}

				mountpointDir = mountpointDir.Parent;
			}
			while (mountpointDir != null);

			throw Marshal.GetExceptionForHR(KernelApi.GetHRForWin32Error(
				4390 /*ERROR_NOT_A_REPARSE_POINT*/));
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
				return (DriveType)KernelApi.NativeMethods.GetDriveType(VolumeId);
			}
		}

		/// <summary>
		/// Determines the cluster size of the current volume.
		/// </summary>
		public int ClusterSize
		{
			get
			{
				uint clusterSize, sectorSize, freeClusters, totalClusters;
				if (KernelApi.NativeMethods.GetDiskFreeSpace(VolumeId, out clusterSize,
					out sectorSize, out freeClusters, out totalClusters))
				{
					return (int)(clusterSize * sectorSize);
				}

				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
		}

		/// <summary>
		/// Determines the sector size of the current volume.
		/// </summary>
		public int SectorSize
		{
			get
			{
				uint clusterSize, sectorSize, freeClusters, totalClusters;
				if (KernelApi.NativeMethods.GetDiskFreeSpace(VolumeId, out clusterSize,
					out sectorSize, out freeClusters, out totalClusters))
				{
					return (int)sectorSize;
				}

				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
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
				if (KernelApi.NativeMethods.GetDiskFreeSpaceEx(VolumeId, out freeBytesAvailable,
					out totalNumberOfBytes, out totalNumberOfFreeBytes))
				{
					return totalNumberOfFreeBytes != freeBytesAvailable;
				}
				else if (Marshal.GetLastWin32Error() == 21 /*ERROR_NOT_READY*/)
				{
					//For the lack of more appropriate responses.
					return false;
				}

				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
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
				ulong result, dummy;
				if (KernelApi.NativeMethods.GetDiskFreeSpaceEx(VolumeId, out dummy,
					out dummy, out result))
				{
					return (long)result;
				}

				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
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
				if (KernelApi.NativeMethods.GetDiskFreeSpaceEx(VolumeId, out dummy,
					out result, out dummy))
				{
					return (long)result;
				}

				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
		}

		/// <summary>
		/// Indicates the amount of available free space on a drive.
		/// </summary>
		public long AvailableFreeSpace
		{
			get
			{
				ulong result, dummy;
				if (KernelApi.NativeMethods.GetDiskFreeSpaceEx(VolumeId, out result,
					out dummy, out dummy))
				{
					return (long)result;
				}

				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
		}

		/// <summary>
		/// Retrieves all mountpoints in the current volume, if the current volume
		/// contains volume mountpoints.
		/// </summary>
		public ICollection<VolumeInfo> MountedVolumes
		{
			get
			{
				List<VolumeInfo> result = new List<VolumeInfo>();
				StringBuilder nextMountpoint = new StringBuilder(
					KernelApi.NativeMethods.LongPath * sizeof(char));

				SafeHandle handle = KernelApi.NativeMethods.FindFirstVolumeMountPoint(VolumeId,
					nextMountpoint, KernelApi.NativeMethods.LongPath);
				if (handle.IsInvalid)
					return result;

				//Iterate over the volume mountpoints
				while (KernelApi.NativeMethods.FindNextVolumeMountPoint(handle,
					nextMountpoint, KernelApi.NativeMethods.LongPath))
				{
					result.Add(new VolumeInfo(nextMountpoint.ToString()));
				}

				//Close the handle
				if (Marshal.GetLastWin32Error() == 18 /*ERROR_NO_MORE_FILES*/)
					KernelApi.NativeMethods.FindVolumeMountPointClose(handle);

				return result.AsReadOnly();
			}
		}

		/// <summary>
		/// The various mountpoints to the root of the volume. This list contains
		/// paths which may be a drive or a mountpoint. Every string includes the
		/// trailing backslash.
		/// </summary>
		public ReadOnlyCollection<string> MountPoints
		{
			get
			{
				return (VolumeType == DriveType.Network ?
					GetNetworkMountPoints() : GetLocalVolumeMountPoints()).AsReadOnly();
			}
		}

		/// <summary>
		/// Gets whether the current volume is mounted at any place.
		/// </summary>
		public bool IsMounted
		{
			get { return MountPoints.Count != 0; }
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
			SafeFileHandle handle = OpenHandle(access, share, options);

			//Check that the handle is valid
			if (handle.IsInvalid)
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

			//Return the FileStream
			return new FileStream(handle, access);
		}

		private SafeFileHandle OpenHandle(FileAccess access, FileShare share, FileOptions options)
		{
			//Access mode
			uint iAccess = 0;
			switch (access)
			{
				case FileAccess.Read:
					iAccess = KernelApi.NativeMethods.GENERIC_READ;
					break;
				case FileAccess.ReadWrite:
					iAccess = KernelApi.NativeMethods.GENERIC_READ |
						KernelApi.NativeMethods.GENERIC_WRITE;
					break;
				case FileAccess.Write:
					iAccess = KernelApi.NativeMethods.GENERIC_WRITE;
					break;
			}

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
			SafeFileHandle result = KernelApi.NativeMethods.CreateFile(openPath, iAccess,
				(uint)share, IntPtr.Zero, (uint)FileMode.Open, (uint)options, IntPtr.Zero);
			if (result.IsInvalid)
				throw KernelApi.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

			return result;
		}

		public VolumeLock LockVolume(FileStream stream)
		{
			return new VolumeLock(stream);
		}
	}

	public class VolumeLock : IDisposable
	{
		internal VolumeLock(FileStream stream)
		{
			uint result = 0;
			for (int i = 0; !KernelApi.NativeMethods.DeviceIoControl(stream.SafeFileHandle,
					KernelApi.NativeMethods.FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero,
					0, out result, IntPtr.Zero); ++i)
			{
				if (i > 100)
					throw new IOException("Could not lock volume.");
				System.Threading.Thread.Sleep(100);
			}

			Stream = stream;
		}

		~VolumeLock()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			//Flush the contents of the buffer to disk since after we unlock the volume
			//we can no longer write to the volume.
			Stream.Flush();

			uint result = 0;
			if (!KernelApi.NativeMethods.DeviceIoControl(Stream.SafeFileHandle,
				KernelApi.NativeMethods.FSCTL_UNLOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero,
				0, out result, IntPtr.Zero))
			{
				throw new IOException("Could not unlock volume.");
			}
		}

		private FileStream Stream;
	}
}