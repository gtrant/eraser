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
using System.Windows.Forms;

using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Xml;

using System.Net;
using System.Net.Cache;
using System.Net.Mime;
using System.Globalization;

using Eraser.Util;

using DoWorkEventArgs = System.ComponentModel.DoWorkEventArgs;
using RunWorkerCompletedEventArgs = System.ComponentModel.RunWorkerCompletedEventArgs;

namespace Eraser
{
	public partial class UpdateForm : Form
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public UpdateForm()
		{
			InitializeComponent();
			Theming.ApplyTheme(this);
			updateListDownloader.RunWorkerAsync();
		}

		/// <summary>
		/// Called when the form is about to be closed.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void UpdateForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			//Cancel all running background tasks
			if (updateListDownloader.IsBusy || downloader.IsBusy || installer.IsBusy)
			{
				updateListDownloader.CancelAsync();
				downloader.CancelAsync();
				installer.CancelAsync();
				e.Cancel = true;
			}
		}

		/// <summary>
		/// Called when any of the Cancel buttons are clicked.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void cancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		#region Update List retrieval
		/// <summary>
		/// Downloads and parses the list of updates available for this client.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void updateListDownloader_DoWork(object sender, DoWorkEventArgs e)
		{
			e.Result = DownloadManager.GetDownloads(updateListDownloader_ProgressChanged);
		}

		/// <summary>
		/// Called when progress has been made in the update list download.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void updateListDownloader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (InvokeRequired)
			{
				if (updateListDownloader.CancellationPending)
					throw new OperationCanceledException();

				Invoke((EventHandler<ProgressChangedEventArgs>)updateListDownloader_ProgressChanged,
					sender, e);
				return;
			}

			progressPb.Style = ProgressBarStyle.Continuous;
			progressPb.Value = (int)(e.Progress.Progress * 100);
			progressProgressLbl.Text = e.UserState as string;

			if (progressPb.Value == 100)
				progressProgressLbl.Text = S._("Processing update list...");
		}

		/// <summary>
		/// Displays the parsed updates on the updates list view, filtering and displaying
		/// only those relevant to the current system's architecture.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void updateListDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//The Error property will normally be null unless there are errors during the download.
			if (e.Error != null)
			{
				if (!(e.Error is OperationCanceledException))
					MessageBox.Show(this, e.Error.Message, S._("Eraser"), MessageBoxButtons.OK,
						MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
						Localisation.IsRightToLeft(this) ?
							MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);

				Close();
				return;
			}

			progressPanel.Visible = false;
			updatesPanel.Visible = true;

			//First list all available mirrors
			IList<DownloadInfo> downloads = (IList<DownloadInfo>)e.Result;

			//Get a list of translatable categories (this will change as more categories
			//are added)
			updatesLv.Groups.Add(DownloadType.Update.ToString(), S._("Updates"));
			updatesLv.Groups.Add(DownloadType.Plugin.ToString(), S._("Plugins"));
			updatesLv.Groups.Add(DownloadType.Build.ToString(), S._("Nightly builds"));

			//Only include those whose architecture is compatible with ours.
			List<string> architectures = new List<string>();
			{
				//any is always compatible.
				architectures.Add("any");

				switch (SystemInfo.ProcessorArchitecture)
				{
					case ProcessorArchitecture.Amd64:
						architectures.Add("x64");
						break;
					case ProcessorArchitecture.IA64:
						architectures.Add("ia64");
						break;
					case ProcessorArchitecture.X86:
						architectures.Add("x86");
						break;
				}
			}

			foreach (DownloadInfo download in downloads)
			{
				//Skip this download if it is not for our current architecture.
				if (architectures.IndexOf(download.Architecture) == -1)
					continue;

				//Get the group this download belongs to.
				ListViewGroup group = updatesLv.Groups[download.Type.ToString()];

				//Add the item to the list of downloads available.
				ListViewItem item = new ListViewItem(download.Name);
				item.SubItems.Add(download.Version.ToString());
				item.SubItems.Add(download.Publisher);
				item.SubItems.Add(FileSize.ToString(download.FileSize));

				item.Tag = download;
				item.Group = group;
				item.Checked = true;

				updatesLv.Items.Add(item);
			}

			updatesBtn.Enabled = updatesLv.Items.Count > 0;

			//Check if there are any updates at all.
			if (updatesLv.Items.Count == 0)
			{
				MessageBox.Show(this, S._("There are no new updates or plugins available for " +
					"Eraser."), S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Information,
					MessageBoxDefaultButton.Button1,
					Localisation.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
				Close();
			}
		}
		#endregion

		#region Update downloader
		/// <summary>
		/// Handles the update checked event.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void updatesLv_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			updatesBtn.Text = updatesLv.CheckedIndices.Count == 0 ? S._("Close") : S._("Install");
		}

		/// <summary>
		/// Handles the Install button click; fetches and installs the updates selected.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void updatesBtn_Click(object sender, EventArgs e)
		{
			updatesPanel.Visible = false;
			downloadingPnl.Visible = true;
			List<DownloadInfo> updatesToInstall = new List<DownloadInfo>();

			//Collect the items that need to be installed
			foreach (ListViewItem item in updatesLv.CheckedItems)
			{
				item.Remove();
				item.SubItems.RemoveAt(1);
				item.SubItems.RemoveAt(1);
				downloadingLv.Items.Add(item);

				DownloadInfo download = (DownloadInfo)item.Tag;
				updatesToInstall.Add(download);
				DownloadItems.Add(download, new DownloadUIInfo(download, item));
			}

			//Then run the thread if there are updates.
			if (updatesToInstall.Count > 0)
				downloader.RunWorkerAsync(updatesToInstall);
			else
				Close();
		}

		/// <summary>
		/// Background thread to do the downloading and installing of updates.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void downloader_DoWork(object sender, DoWorkEventArgs e)
		{
			List<DownloadInfo> downloads = (List<DownloadInfo>)e.Argument;
			SteppedProgressManager overallProgress = new SteppedProgressManager();
			long totalDownloadSize = downloads.Sum(delegate(DownloadInfo download)
				{
					return download.FileSize;
				});
			
			foreach (DownloadInfo download in downloads)
			{
				ProgressManagerBase downloadProgress = null;
				ProgressChangedEventHandler localHandler =
					delegate(object sender2, ProgressChangedEventArgs e2)
					{
						DownloadInfo downloadInfo = (DownloadInfo)sender2;
						if (downloadProgress == null)
						{
							downloadProgress = e2.Progress;
							overallProgress.Steps.Add(new SteppedProgressManagerStep(
								e2.Progress, download.FileSize / (float)totalDownloadSize));
						}

						downloader_ProgressChanged(sender2,
							new ProgressChangedEventArgs(overallProgress, e2.UserState));
					};

				download.Download(localHandler);
			}

			e.Result = e.Argument;
		}

		/// <summary>
		/// Handles the download progress changed event.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void downloader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (InvokeRequired)
			{
				if (updateListDownloader.CancellationPending)
					throw new OperationCanceledException();

				Invoke((EventHandler<ProgressChangedEventArgs>)downloader_ProgressChanged,
					sender, e);
				return;
			}

			DownloadInfo download = (DownloadInfo)sender;
			DownloadUIInfo downloadUIInfo = DownloadItems[download];
			SteppedProgressManager overallProgress = (SteppedProgressManager)e.Progress;

			if (e.UserState is Exception)
			{
				downloadUIInfo.ListViewItem.ImageIndex = 3;
				downloadUIInfo.ListViewItem.SubItems[1].Text = S._("Error");
				downloadUIInfo.ListViewItem.ToolTipText = ((Exception)e.UserState).Message;
			}
			else
			{
				if (overallProgress.CurrentStep.Progress.Progress >= 1.0f)
				{
					downloadUIInfo.ListViewItem.ImageIndex = -1;
					downloadUIInfo.ListViewItem.SubItems[1].Text = S._("Downloaded");
				}
				else
				{
					downloadUIInfo.Downloaded = (long)
						(overallProgress.CurrentStep.Progress.Progress * download.FileSize);
					downloadUIInfo.ListViewItem.ImageIndex = 0;
					downloadUIInfo.ListViewItem.SubItems[1].Text = FileSize.ToString(download.FileSize -
						downloadUIInfo.Downloaded);
				}
			}

			downloadingItemLbl.Text = S._("Downloading: {0}", download.Name);
			downloadingItemPb.Value = (int)(overallProgress.CurrentStep.Progress.Progress * 100);
			downloadingOverallPb.Value = (int)(overallProgress.Progress * 100);
			downloadingOverallLbl.Text = S._("Overall progress: {0} left",
				FileSize.ToString(DownloadItems.Values.Sum(delegate(DownloadUIInfo item)
					{
						return item.Download.FileSize - item.Downloaded;
					}
			)));
		}

		/// <summary>
		/// Handles the completion of download event
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void downloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				if (!(e.Error is OperationCanceledException))
					MessageBox.Show(this, e.Error.Message, S._("Eraser"),
						MessageBoxButtons.OK, MessageBoxIcon.Error,
						MessageBoxDefaultButton.Button1,
						Localisation.IsRightToLeft(this) ?
							MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);

				Close();
				return;
			}

			downloadingPnl.Visible = false;
			installingPnl.Visible = true;

			foreach (DownloadUIInfo download in DownloadItems.Values)
			{
				download.ListViewItem.Remove();
				installingLv.Items.Add(download.ListViewItem);

				if (download.Error == null)
					download.ListViewItem.SubItems[1].Text = string.Empty;
				else
					download.ListViewItem.SubItems[1].Text = S._("Error: {0}", download.Error.Message);
			}

			installer.RunWorkerAsync(e.Result);
		}
		#endregion

		#region Update installer
		/// <summary>
		/// Background thread to install downloaded updates
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void installer_DoWork(object sender, DoWorkEventArgs e)
		{
			List<DownloadInfo> downloads = (List<DownloadInfo>)e.Argument;
			ProgressManager progress = new ProgressManager();
			progress.Total = downloads.Count;

			foreach (DownloadInfo download in downloads)
			{
				++progress.Completed;

				try
				{
					installer_ProgressChanged(download,
						new ProgressChangedEventArgs(progress, null));
					download.Install();
					installer_ProgressChanged(download,
						new ProgressChangedEventArgs(progress, null));
				}
				catch (Exception ex)
				{
					installer_ProgressChanged(download,
						new ProgressChangedEventArgs(progress, ex));
				}
			}

			e.Result = e.Argument;
		}

		/// <summary>
		/// Handles the progress events generated during update installation.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void installer_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (InvokeRequired)
			{
				if (updateListDownloader.CancellationPending)
					throw new OperationCanceledException();

				Invoke((EventHandler<ProgressChangedEventArgs>)installer_ProgressChanged,
					sender, e);
				return;
			}

			DownloadInfo download = (DownloadInfo)sender;
			DownloadUIInfo downloadUIInfo = DownloadItems[download];

			if (e.UserState is Exception)
			{
				downloadUIInfo.Error = (Exception)e.UserState;
				downloadUIInfo.ListViewItem.ImageIndex = 3;
				downloadUIInfo.ListViewItem.SubItems[1].Text =
					S._("Error: {0}", downloadUIInfo.Error.Message);
			}
			else
			{
				switch (downloadUIInfo.ListViewItem.ImageIndex)
				{
					case -1:
						downloadUIInfo.ListViewItem.SubItems[1].Text =
							S._("Installing {0}", download.Name);
						downloadUIInfo.ListViewItem.ImageIndex = 1;
						break;
					case 1:
						downloadUIInfo.ListViewItem.SubItems[1].Text =
							S._("Installed {0}", download.Name);
						downloadUIInfo.ListViewItem.ImageIndex = 2;
						break;
				}
			}
		}

		/// <summary>
		/// Re-enables the close dialog button.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void installer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error is OperationCanceledException)
				Close();

			installingPnl.UseWaitCursor = false;
		}
		#endregion

		/// <summary>
		/// Manages information associated with the update.
		/// </summary>
		private class DownloadUIInfo
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="update">The DownloadInfo object containing the
			/// information about the download.</param>
			/// <param name="item">The ListViewItem used for the display of the
			/// update.</param>
			public DownloadUIInfo(DownloadInfo download, ListViewItem item)
			{
				Download = download;
				ListViewItem = item;
			}

			/// <summary>
			/// The DownloadInfo object containing information about the update.
			/// </summary>
			public DownloadInfo Download { get; private set; }

			/// <summary>
			/// The ListViewItem used for the display of the update.
			/// </summary>
			public ListViewItem ListViewItem { get; private set; }

			/// <summary>
			/// The amount of the download already completed.
			/// </summary>
			public long Downloaded { get; set; }

			/// <summary>
			/// The error raised when downloading/installing the update, if any. Null
			/// otherwise.
			/// </summary>
			public Exception Error { get; set; }
		}

		/// <summary>
		/// Maps downloads to the list view items.
		/// </summary>
		private Dictionary<DownloadInfo, DownloadUIInfo> DownloadItems =
			new Dictionary<DownloadInfo, DownloadUIInfo>();
	}

	/// <summary>
	/// Manages the list of downloads that can be retrieved from the Eraser update server.
	/// </summary>
	public static class DownloadManager
	{
		public static IList<DownloadInfo> GetDownloads(Eraser.Util.ProgressChangedEventHandler handler)
		{
			WebRequest.DefaultCachePolicy = new HttpRequestCachePolicy(
				HttpRequestCacheLevel.Revalidate);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
				new Uri("http://eraser.heidi.ie/scripts/updates?action=listupdates&version=" +
					BuildInfo.AssemblyFileVersion));

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream responseStream = response.GetResponseStream())
			using (MemoryStream memoryStream = new MemoryStream())
			{
				Util.ProgressManager progress = new Util.ProgressManager();
				progress.Total = response.ContentLength;

				//Download the response
				int lastRead = 0;
				byte[] buffer = new byte[16384];
				while ((lastRead = responseStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					memoryStream.Write(buffer, 0, lastRead);
					progress.Completed = memoryStream.Position;
					if (handler != null)
						handler(null, new Eraser.Util.ProgressChangedEventArgs(progress,
							S._("{0} of {1} downloaded", FileSize.ToString(progress.Completed),
								FileSize.ToString(progress.Total))));
				}

				//Parse it.
				memoryStream.Position = 0;
				return ParseDownloadList(memoryStream).AsReadOnly();
			}
		}

		/// <summary>
		/// Parses the list of updates provided by the server
		/// </summary>
		/// <param name="strm">The stream containing the XML data.</param>
		private static List<DownloadInfo> ParseDownloadList(Stream strm)
		{
			//Move the XmlReader to the root node
			XmlReader reader = XmlReader.Create(strm);
			reader.ReadToFollowing("updateList");

			//Read the descendants of the updateList node (ignoring the <mirrors> element)
			//These are categories.
			bool cont = reader.Read();
			while (reader.NodeType != XmlNodeType.Element)
				cont = reader.Read();
			if (reader.NodeType != XmlNodeType.Element)
				return new List<DownloadInfo>();

			List<DownloadInfo> result = new List<DownloadInfo>();
			do
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					result.AddRange(ParseDownloadCategory(reader.Name, reader.ReadSubtree()));
				}

				cont = reader.Read();
			}
			while (cont);

			return result;
		}

		/// <summary>
		/// Parses a specific category and its assocaited updates.
		/// </summary>
		/// <param name="category">The name of the category.</param>
		/// <param name="rdr">The XML reader object representing the element and its children.</param>
		/// <returns>A list of downloads in the category.</returns>
		private static List<DownloadInfo> ParseDownloadCategory(string category, XmlReader rdr)
		{
			List<DownloadInfo> result = new List<DownloadInfo>();
			if (!rdr.ReadToDescendant("item"))
				return result;

			//Load every element.
			do
			{
				if (rdr.Name != "item")
					continue;

				result.Add(new DownloadInfo(rdr.GetAttribute("name"),
					(DownloadType)Enum.Parse(typeof(DownloadType), category, true),
					new Version(rdr.GetAttribute("version")), rdr.GetAttribute("publisher"),
					rdr.GetAttribute("architecture"), Convert.ToInt64(rdr.GetAttribute("filesize")),
					new Uri(rdr.ReadElementContentAsString())));
			}
			while (rdr.ReadToNextSibling("item"));

			return result;
		}
	}

	/// <summary>
	/// The types of downloads we support.
	/// </summary>
	public enum DownloadType
	{
		/// <summary>
		/// The type of the download is unknown.
		/// </summary>
		Unknown,

		/// <summary>
		/// The download is an update.
		/// </summary>
		Update,

		/// <summary>
		/// The download is a plugin.
		/// </summary>
		Plugin,

		/// <summary>
		/// The download is a nightly build.
		/// </summary>
		Build
	}

	/// <summary>
	/// Represents an update available on the server.
	/// </summary>
	public class DownloadInfo
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name">The name of the download.</param>
		/// <param name="type">The type of the download.</param>
		/// <param name="version">The version of the download.</param>
		/// <param name="publisher">The publisher of the download.</param>
		/// <param name="architecture">The architecture of the binaries.</param>
		/// <param name="fileSize">The size of the download.</param>
		/// <param name="link">The link to the download.</param>
		internal DownloadInfo(string name, DownloadType type, Version version,
			string publisher, string architecture, long fileSize, Uri link)
		{
			Name = name;
			Type = type;
			Version = version;
			Publisher = publisher;
			Architecture = architecture;
			FileSize = fileSize;
			Link = link;
		}

		public string Name { get; private set; }
		public DownloadType Type { get; private set; }
		public Version Version { get; private set; }
		public string Publisher { get; private set; }
		public string Architecture { get; private set; }
		public long FileSize { get; private set; }
		public Uri Link { get; private set; }

		/// <summary>
		/// Downloads the file to disk, storing the path into the DownloadedFile field.
		/// </summary>
		public void Download(Eraser.Util.ProgressChangedEventHandler handler)
		{
			if (DownloadedFile != null && DownloadedFile.Length > 0)
				throw new InvalidOperationException("The Download method cannot be called " +
					"before the Download method has been called.");

			//Create a folder to hold all our updates.
			lock (TempPathLock)
			{
				if (TempPath == null)
				{
					TempPath = new DirectoryInfo(Path.GetTempPath());
					TempPath = TempPath.CreateSubdirectory("eraser" + Environment.TickCount.ToString(
						CultureInfo.InvariantCulture));
				}
			}

			//Create the progress manager for this download.
			ProgressManager progress = new ProgressManager();

			try
			{
				//Request the download.
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Link);
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					//Do the progress calculations
					progress.Total = response.ContentLength;

					//Check for a suggested filename.
					ContentDisposition contentDisposition = null;
					foreach (string header in response.Headers.AllKeys)
						if (header.ToUpperInvariant() == "CONTENT-DISPOSITION")
							contentDisposition = new ContentDisposition(response.Headers[header]);

					//Create the file name.
					DownloadedFile = new FileInfo(Path.Combine(
						TempPath.FullName, string.Format(CultureInfo.InvariantCulture,
							"{0:00}-{1}", ++DownloadFileIndex, contentDisposition == null ?
								Path.GetFileName(Link.GetComponents(UriComponents.Path, UriFormat.Unescaped)) :
								contentDisposition.FileName)));

					using (Stream responseStream = response.GetResponseStream())
					using (FileStream fileStream = DownloadedFile.OpenWrite())
					{
						//Copy the information into the file stream
						int lastRead = 0;
						byte[] buffer = new byte[16384];
						while ((lastRead = responseStream.Read(buffer, 0, buffer.Length)) != 0)
						{
							fileStream.Write(buffer, 0, lastRead);

							//Compute progress
							progress.Completed = fileStream.Position;

							//Call the progress handler
							if (handler != null)
								handler(this, new ProgressChangedEventArgs(progress, null));
						}
					}

					//Let the event handler know the download is complete.
					progress.MarkComplete();
					if (handler != null)
						handler(this, new ProgressChangedEventArgs(progress, null));
				}
			}
			catch (Exception e)
			{
				if (handler != null)
					handler(this, new ProgressChangedEventArgs(progress, e));
			}
		}

		/// <summary>
		/// Installs the file, by calling Process.Start on the file.
		/// </summary>
		/// <returns>The exit code of the program.</returns>
		public void Install()
		{
			if (DownloadedFile == null || !DownloadedFile.Exists || DownloadedFile.Length == 0)
				throw new InvalidOperationException("The Install method cannot be called " +
					"before the Download method has been called.");

			ProcessStartInfo info = new ProcessStartInfo();
			info.FileName = DownloadedFile.FullName;
			info.UseShellExecute = true;

			Process process = Process.Start(info);
			process.WaitForExit(Int32.MaxValue);
		}

		/// <summary>
		/// The lock object for the TempPath field.
		/// </summary>
		private static object TempPathLock = new object();

		/// <summary>
		/// The temporary path we are storing our downloads in.
		/// </summary>
		private static DirectoryInfo TempPath;

		/// <summary>
		/// Counter to ensure that files downloaded with a similar name are not overwritten
		/// over each other
		/// </summary>
		private static int DownloadFileIndex;

		/// <summary>
		/// Stores information about the temporary file which the download was stored into.
		/// </summary>
		private FileInfo DownloadedFile;
	}
}