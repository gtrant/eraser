/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Garrett Trant <gtrant@users.sourceforge.net>
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

using System.IO;
using System.Threading;

using Eraser.Util;
using Eraser.Util.ExtensionMethods;
using Eraser.Plugins;
using Eraser.Plugins.ExtensionPoints;

namespace Eraser.DefaultPlugins
{
	/// <summary>
	/// Base class for all Windows filesystems.
	/// </summary>
	abstract class WindowsFileSystem : FileSystemBase
	{
		public override void ResetFileTimes(FileSystemInfo info)
		{
			//Reset the file access times: after every rename the file times may change.
			info.SetTimes(MinTimestamp, MinTimestamp, MinTimestamp, MinTimestamp);
		}

		public override void DeleteFile(FileInfo info)
		{
			//If the user wants plausible deniability, find a random file from the
			//list of decoys and write it over.
			if (Host.Instance.Settings.PlausibleDeniability)
			{
				DeleteFileSystemInfo(info, false);
				CopyPlausibleDeniabilityFile(info);
			}
			else
			{ 
				DeleteFileSystemInfo(info,true); 
			}

		}

		public override void DeleteFolder(DirectoryInfo info, bool recursive)
		{
			if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
			{
				if (!recursive && info.GetFileSystemInfos().Length != 0)
					throw new InvalidOperationException(S._("The folder {0} cannot be deleted as it is " +
						"not empty."));

				foreach (DirectoryInfo dir in info.GetDirectories())
					DeleteFolder(dir, true);
				foreach (FileInfo file in info.GetFiles())
					DeleteFile(file);
			}

			DeleteFileSystemInfo(info,true);
		}

		/// <summary>
		/// Deletes a directory or file from disk. This assumes that directories already
		/// have been deleted.
		/// </summary>
		/// <param name="info">The file or directory to delete.</param>
		private void DeleteFileSystemInfo(FileSystemInfo info, Boolean RemoveFile)
		{
			//If the file/directory doesn't exist, pass.
			if (!info.Exists)
				return;

			//Reset the file attributes to non-content indexed so indexing
			//services will not lock the file.
			try
			{
				info.Attributes = FileAttributes.NotContentIndexed;
			}
			catch (ArgumentException e)
			{
				//This is an undocumented exception: when the path we are setting
				//cannot be accessed (ERROR_ACCESS_DENIED is returned) an
				//ArgumentException is raised (no idea why!)
				throw new UnauthorizedAccessException(e.Message, e);
			}

			//Rename the file a few times to erase the entry from the file system
			//table.
			for (int i = 0, tries = 0; i < FileNameErasePasses; ++tries)
			{
				//Generate a new file name for the file/directory.
				string newPath = GenerateRandomFileName(info.GetParent(), info.Name.Length);

				try
				{
					ResetFileTimes(info);

					//Try to rename the file. If it fails, it is probably due to another
					//process locking the file. Defer, then rename again.
					info.MoveTo(newPath);
					++i;
				}
				catch (IOException e)
				{
					switch (System.Runtime.InteropServices.Marshal.GetLastWin32Error())
					{
						case Win32ErrorCode.AccessDenied:
							throw new UnauthorizedAccessException(S._("The file {0} could not " +
								"be erased because the file's permissions prevent access to the file.",
								info.FullName));

						case Win32ErrorCode.SharingViolation:
							//If after FilenameEraseTries the file is still locked, some program is
							//definitely using the file; throw an exception.
							if (tries > FileNameEraseTries)
							{
								//Try to force the handle closed.
								if (tries > FileNameEraseTries + 1 ||
									!Host.Instance.Settings.ForceUnlockLockedFiles)
								{
									throw new IOException(S._("The file {0} is currently in use " +
										"and cannot be removed.", info.FullName), e);
								}

								//Either we could not close all instances, or we already tried twice. Report
								//the error.
								string processes = string.Empty;
								{
									StringBuilder processStr = new StringBuilder();
									foreach (OpenHandle handle in OpenHandle.Close(info.FullName))
									{
										try
										{
											processStr.AppendFormat(
												System.Globalization.CultureInfo.InvariantCulture,
												"{0}, ", handle.Process.MainModule.FileName);
										}
										catch (System.ComponentModel.Win32Exception)
										{
											processStr.AppendFormat(
												System.Globalization.CultureInfo.InvariantCulture,
												"Process ID {0}, ", handle.Process.Id);
										}
									}

									if (processStr.Length > 2)
									{
										processes = processStr.ToString().Remove(processStr.Length - 2).Trim();
									}
									else
									{
										processes = S._("(unknown)");
									}
								}

								throw new SharingViolationException(S._(
									"Could not force closure of file \"{0}\" {1}", info.FullName,
									S._("(locked by {0})", processes)));
							}

							//Let the process locking the file release the lock
							Thread.Sleep(100);
							break;

						case Win32ErrorCode.DiskFull:
							//If the disk is full, we can't do anything except manually deleting
							//the file, break out of this loop.
							i = FileNameEraseTries;
							break;

						default:
							throw;
					}
				}
			}
			if (RemoveFile == true)
			{
				//Then delete the file.
				for (int i = 0; i < FileNameEraseTries; ++i)
					try
					{
						info.Delete();
						break;
					}
					catch (IOException e)
					{
						switch (System.Runtime.InteropServices.Marshal.GetLastWin32Error())
						{
							case Win32ErrorCode.AccessDenied:
								throw new UnauthorizedAccessException(S._("The file {0} could not " +
									"be erased because the file's permissions prevent access to the file.",
									info.FullName), e);

							case Win32ErrorCode.SharingViolation:
								//If after FilenameEraseTries the file is still locked, some program is
								//definitely using the file; throw an exception.
								if (i > FileNameEraseTries)
									throw new IOException(S._("The file {0} is currently in use and " +
										"cannot be removed.", info.FullName), e);

								//Let the process locking the file release the lock
								Thread.Sleep(100);
								break;

							default:
								throw;
						}
					}
			}
		}


		/// <summary>
		/// Writes a file for plausible deniability over the current stream.
		/// </summary>
		/// <param name="stream">The stream to write the data to.</param>
		private static void CopyPlausibleDeniabilityFile(FileInfo info)
		{
			//Get the template file to copy
			FileInfo shadowFileInfo;
			{
				string shadowFile = null;
				List<string> entries = new List<string>(
					Host.Instance.Settings.PlausibleDeniabilityFiles);
				IPrng prng = Host.Instance.Prngs.ActivePrng;
				do
				{
					if (entries.Count == 0)
						throw new FatalException(S._("Plausible deniability was selected, " +
							"but no decoy files were found. The current file has been only " +
							"replaced with random data."));

					//Get an item from the list of files, and then check that the item exists.
					int index = prng.Next(entries.Count - 1);
					shadowFile = entries[index];
					if (File.Exists(shadowFile) || Directory.Exists(shadowFile))
					{
						if ((File.GetAttributes(shadowFile) & FileAttributes.Directory) != 0)
						{
							DirectoryInfo dir = new DirectoryInfo(shadowFile);
							FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);
							entries.Capacity += files.Length;
							foreach (FileInfo f in files)
								entries.Add(f.FullName);
							shadowFile = entries[prng.Next(entries.Count - 1)];
						}
						else
							shadowFile = entries[index];
					}
					else
						shadowFile = null;

					//entries.RemoveAt(index);

				}
				while (string.IsNullOrEmpty(shadowFile));
				shadowFileInfo = new FileInfo(shadowFile);
			}

			//First Lets Copy over the attributes and name
			if (shadowFileInfo.IsCompressed()) { info.Compress(); }
			info.CopyTimes(shadowFileInfo);
			info.SetAccessControl(shadowFileInfo.GetAccessControl());
			File.SetCreationTime(info.FullName, File.GetCreationTime(shadowFileInfo.FullName));
			string TargetName = String.Format("{0}\\{1}", info.DirectoryName, shadowFileInfo.Name);
			info.MoveTo(TargetName);

			// Now lets fill it with data from the source file.
			using (Stream stream = info.OpenWrite())
			{
				//Dump the copy (the first 4MB, or less, depending on the file size and size of
				//the original file)
				// At this point the original file has been marked to zero length
				long amountToCopy = Math.Min(4 * 1024 * 1024, shadowFileInfo.Length);
				using (FileStream shadowFileStream = shadowFileInfo.Open(
						FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					while (stream.Position < amountToCopy)
					{
						byte[] buf = new byte[524288];
						int bytesRead = shadowFileStream.Read(buf, 0, buf.Length);

						//Stop bothering if the input stream is at the end
						if (bytesRead == 0)
							break;

						//Dump the read contents onto the file to be deleted
						stream.Write(buf, 0,
							(int)Math.Min(bytesRead, amountToCopy - stream.Position));
					}
					shadowFileStream.Close();
				}
				// Make the file same length as copied file
				// This may produce an output with artifacts from the disk making the file more plausable
				stream.SetLength(shadowFileInfo.Length);
				stream.Close();
				// Delete normally
				File.Delete(TargetName);
			}
		}

		public override void EraseClusterTips(VolumeInfo info, IErasureMethod method,
			ClusterTipsSearchProgress searchCallback, ClusterTipsEraseProgress eraseCallback)
		{
			//List all the files which can be erased.
			List<string> files = new List<string>();
			if (!info.IsMounted)
				throw new InvalidOperationException(S._("Could not erase cluster tips in {0} " +
					"as the volume is not mounted.", info.VolumeId));
			ListFiles(info.MountPoints[0], files, searchCallback);

			//For every file, erase the cluster tips.
			for (int i = 0, j = files.Count; i != j; ++i)
			{
				//Get the file attributes for restoring later
				StreamInfo streamInfo = new StreamInfo(files[i]);
				if (!streamInfo.Exists)
					continue;

				try
				{
					EraseFileClusterTips(streamInfo, method);
				}
				catch (UnauthorizedAccessException)
				{
					Logger.Log(S._("{0} did not have its cluster tips erased because you do not " +
						"have the required permissions to erase the file cluster tips.", files[i]),
						LogLevel.Information);
				}
				catch (IOException e)
				{
					Logger.Log(S._("{0} did not have its cluster tips erased. The error returned " +
						"was: {1}", files[i], e.Message), LogLevel.Warning);
				}

				eraseCallback(i, files.Count, files[i]);
			}
		}

		private void ListFiles(DirectoryInfo info, List<string> files,
			ClusterTipsSearchProgress searchCallback)
		{
			try
			{
				//Skip this directory if it is a reparse point
				if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
				{
					Logger.Log(S._("Files in {0} did not have their cluster tips erased because " +
						"it is a hard link or a symbolic link.", info.FullName),
						LogLevel.Information);
					return;
				}

				foreach (FileInfo file in info.GetFiles())
					if (file.IsProtectedSystemFile())
						Logger.Log(S._("{0} did not have its cluster tips erased, because it is " +
							"a system file", file.FullName), LogLevel.Information);
					else if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
						Logger.Log(S._("{0} did not have its cluster tips erased because it is a " +
							"hard link or a symbolic link.", file.FullName), LogLevel.Information);
					else if ((file.Attributes & FileAttributes.Compressed) != 0 ||
						(file.Attributes & FileAttributes.Encrypted) != 0 ||
						(file.Attributes & FileAttributes.SparseFile) != 0)
					{
						Logger.Log(S._("{0} did not have its cluster tips erased because it is " +
							"compressed, encrypted or a sparse file.", file.FullName),
							LogLevel.Information);
					}
					else
					{
						try
						{
							foreach (StreamInfo stream in file.GetADSes())
								files.Add(stream.FullName);

							files.Add(file.FullName);
						}
						catch (UnauthorizedAccessException e)
						{
							Logger.Log(S._("{0} did not have its cluster tips erased because of " +
								"the following error: {1}", file.FullName, e.Message),
								LogLevel.Information);
						}
						catch (IOException e)
						{
							Logger.Log(S._("{0} did not have its cluster tips erased because of " +
								"the following error: {1}", file.FullName, e.Message),
								LogLevel.Warning);
						}
					}

				foreach (DirectoryInfo subDirInfo in info.GetDirectories())
				{
					searchCallback(subDirInfo.FullName);
					ListFiles(subDirInfo, files, searchCallback);
				}
			}
			catch (UnauthorizedAccessException e)
			{
				Logger.Log(S._("Files in {0} did not have its cluster tips erased because of the " +
					"following error: {1}", info.FullName, e.Message), LogLevel.Information);
			}
			catch (IOException e)
			{
				Logger.Log(S._("Files in {0} did not have its cluster tips erased because of the " +
					"following error: {1}", info.FullName, e.Message), LogLevel.Warning);
			}
		}

		/// <summary>
		/// Erases the cluster tips of the given file.
		/// </summary>
		/// <param name="stream">The stream to erase.</param>
		/// <param name="method">The erasure method to use.</param>
		private void EraseFileClusterTips(StreamInfo streamInfo, IErasureMethod method)
		{
			//Get the file access times
			DateTime lastAccess = streamInfo.LastAccessTime;
			DateTime lastWrite = streamInfo.LastWriteTime;
			DateTime created = streamInfo.CreationTime;

			//Get the file attributes
			FileAttributes attributes = streamInfo.Attributes;

			//And get the file lengths to know how much to overwrite
			long fileArea = GetFileArea(streamInfo);
			long fileLength = streamInfo.Length;

			//If the file length equals the file area there is no cluster tip to overwrite
			if (fileArea == fileLength)
				return;

			//Otherwise, unset any read-only flags, create the stream, lengthen the
			//file, then tell the erasure method to erase the cluster tips.
			try
			{
				streamInfo.Attributes = FileAttributes.Normal;
				FileStream stream = streamInfo.Open(FileMode.Open, FileAccess.Write,
					FileShare.None, FileOptions.WriteThrough);

				try
				{
					stream.SetLength(fileArea);
					stream.Seek(fileLength, SeekOrigin.Begin);

					//Erase the file
					method.Erase(stream, long.MaxValue, Host.Instance.Prngs.ActivePrng, null);
				}
				finally
				{
					//Make sure the file length is restored!
					stream.SetLength(fileLength);

					//Then destroy the stream
					stream.Close();
				}
			}
			catch (ArgumentException e)
			{
				//This is an undocumented exception: when the path we are setting
				//cannot be accessed (ERROR_ACCESS_DENIED is returned) an
				//ArgumentException is raised (no idea why!)
				throw new UnauthorizedAccessException(e.Message, e);
			}
			finally
			{
				//Reset the file attributes
				if (streamInfo.Attributes != attributes)
					streamInfo.Attributes = attributes;

				//Reset the file times
				streamInfo.SetTimes(MinTimestamp, created, lastWrite, lastAccess);
			}
		}

		public override long GetFileArea(StreamInfo info)
		{
			VolumeInfo volume = VolumeInfo.FromMountPoint(info.Directory.FullName);
			long clusterSize = volume.ClusterSize;
			return (info.Length + (clusterSize - 1)) & ~(clusterSize - 1);
		}

		/// <summary>
		/// The minimum timestamp the file system can take. This is for secure file
		/// deletion.
		/// </summary>
		protected abstract DateTime MinTimestamp { get; }
	}
}