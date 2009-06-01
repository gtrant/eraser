/* 
 * $Id$
 * Copyright 2008 The Eraser Project
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Reflection;
using System.IO;
using System.Xml;
using Eraser.Util;
using System.Net.Cache;
using System.Globalization;

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
			UXThemeApi.UpdateControlTheme(this);
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
			try
			{
				updates.OnProgressEvent += updateListDownloader_ProgressChanged;
				updates.DownloadUpdateList();
			}
			finally
			{
				updates.OnProgressEvent -= updateListDownloader_ProgressChanged;
			}
		}

		/// <summary>
		/// Called when progress has been made in the update list download.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void updateListDownloader_ProgressChanged(object sender, ProgressEventArgs e)
		{
			if (InvokeRequired)
			{
				if (updateListDownloader.CancellationPending)
					throw new OperationCanceledException();

				Invoke(new EventHandler<ProgressEventArgs>(
					updateListDownloader_ProgressChanged), sender, e);
				return;
			}

			progressPb.Style = ProgressBarStyle.Continuous;
			progressPb.Value = (int)(e.OverallProgressPercentage * 100);
			progressProgressLbl.Text = e.Message;

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
						S.IsRightToLeft(this) ? MessageBoxOptions.RtlReading : 0);

				Close();
				return;
			}

			progressPanel.Visible = false;
			updatesPanel.Visible = true;

			//First list all available mirrors
			Dictionary<string, Mirror>.Enumerator iter = updates.Mirrors.GetEnumerator();
			while (iter.MoveNext())
				updatesMirrorCmb.Items.Add(iter.Current.Value);
			updatesMirrorCmb.SelectedIndex = 0;

			//Get a list of translatable categories (this will change as more categories
			//are added)
			Dictionary<string, string> updateCategories = new Dictionary<string, string>();
			updateCategories.Add("update", S._("Updates"));
			updateCategories.Add("plugin", S._("Plugins"));

			//Only include those whose architecture is compatible with ours.
			List<string> compatibleArchs = new List<string>();
			{
				//any is always compatible.
				compatibleArchs.Add("any");

				switch (KernelApi.ProcessorArchitecture)
				{
					case ProcessorArchitecture.Amd64:
						compatibleArchs.Add("x64");
						break;
					case ProcessorArchitecture.IA64:
						compatibleArchs.Add("ia64");
						break;
					case ProcessorArchitecture.X86:
						compatibleArchs.Add("x86");
						break;
				}
			}

			foreach (string key in updates.Categories)
			{
				ListViewGroup group = new ListViewGroup(updateCategories.ContainsKey(key) ?
					updateCategories[key] : key);
				updatesLv.Groups.Add(group);

				foreach (UpdateInfo update in updates.Updates[key])
				{
					//Skip if this update won't work on our current architecture.
					if (compatibleArchs.IndexOf(update.Architecture) == -1)
						continue;

					ListViewItem item = new ListViewItem(update.Name);
					item.SubItems.Add(update.Version.ToString());
					item.SubItems.Add(update.Publisher);
					item.SubItems.Add(Util.File.GetHumanReadableFilesize(update.FileSize));

					item.Tag = update;
					item.Group = group;
					item.Checked = true;

					updatesLv.Items.Add(item);
					uiUpdates.Add(update, new UpdateData(update, item));
				}
			}

			updatesBtn.Enabled = updatesLv.Items.Count > 0;

			//Check if there are any updates at all.
			if (updatesLv.Items.Count == 0)
			{
				MessageBox.Show(this, S._("There are no new updates or plugins available for " +
					"Eraser."), S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Information,
					MessageBoxDefaultButton.Button1,
					S.IsRightToLeft(this) ? MessageBoxOptions.RtlReading : 0);
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
			if (selectedUpdates == -1 || updatesCount != updatesLv.Items.Count)
			{
				updatesCount = updatesLv.Items.Count;
				selectedUpdates = 0;
				foreach (ListViewItem item in updatesLv.Items)
					if (item.Checked)
						++selectedUpdates;
			}
			else
				selectedUpdates += e.Item.Checked ? 1 : -1;
			updatesBtn.Text = selectedUpdates == 0 ? S._("Close") : S._("Install");
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
			List<UpdateInfo> updatesToInstall = new List<UpdateInfo>();

			//Set the mirror
			updates.SelectedMirror = (Mirror)updatesMirrorCmb.SelectedItem;

			//Collect the items that need to be installed
			foreach (ListViewItem item in updatesLv.Items)
				if (item.Checked)
				{
					item.Remove();
					item.SubItems.RemoveAt(1);
					item.SubItems.RemoveAt(1);
					downloadingLv.Items.Add(item);

					updatesToInstall.Add((UpdateInfo)item.Tag);
				}
				else
					uiUpdates.Remove((UpdateInfo)item.Tag);

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
			try
			{
				updates.OnProgressEvent += downloader_ProgressChanged;
				object downloadedUpdates = updates.DownloadUpdates((List<UpdateInfo>)e.Argument);
				e.Result = downloadedUpdates;
			}
			finally
			{
				updates.OnProgressEvent -= downloader_ProgressChanged;
			}
		}

		/// <summary>
		/// Handles the download progress changed event.
		/// </summary>
		/// <param name="sender">The object triggering this event/</param>
		/// <param name="e">Event argument.</param>
		private void downloader_ProgressChanged(object sender, ProgressEventArgs e)
		{
			if (InvokeRequired)
			{
				if (updateListDownloader.CancellationPending)
					throw new OperationCanceledException();

				Invoke(new EventHandler<ProgressEventArgs>(downloader_ProgressChanged),
					sender, e);
				return;
			}

			UpdateData update = uiUpdates[(UpdateInfo)e.UserState];

			if (e is ProgressErrorEventArgs)
			{
				update.Error = ((ProgressErrorEventArgs)e).Exception;
				update.LVItem.ImageIndex = 3;
				update.LVItem.SubItems[1].Text = S._("Error");
				update.LVItem.ToolTipText = update.Error.Message;
			}
			else
			{
				if (e.ProgressPercentage >= 1.0f)
				{
					update.LVItem.ImageIndex = -1;
					update.LVItem.SubItems[1].Text = S._("Downloaded");
				}
				else
				{
					update.amountDownloaded = (long)(e.ProgressPercentage * update.Update.FileSize);
					update.LVItem.ImageIndex = 0;
					update.LVItem.SubItems[1].Text = Util.File.GetHumanReadableFilesize(
						update.Update.FileSize - update.amountDownloaded);
				}
			}

			downloadingItemLbl.Text = e.Message;
			downloadingItemPb.Value = (int)(e.ProgressPercentage * 100);
			downloadingOverallPb.Value = (int)(e.OverallProgressPercentage * 100);

			long amountToDownload = 0;
			foreach (UpdateData upd in uiUpdates.Values)
				amountToDownload += upd.Update.FileSize - upd.amountDownloaded;
			downloadingOverallLbl.Text = S._("Overall progress: {0} left",
				Util.File.GetHumanReadableFilesize(amountToDownload));
		}

		/// <summary>
		/// Handles the completion of updating event
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
						S.IsRightToLeft(this) ? MessageBoxOptions.RtlReading : 0);

				Close();
				return;
			}

			downloadingPnl.Visible = false;
			installingPnl.Visible = true;

			foreach (ListViewItem item in downloadingLv.Items)
			{
				item.Remove();
				installingLv.Items.Add(item);

				UpdateData update = uiUpdates[(UpdateInfo)item.Tag];
				if (update.Error == null)
					item.SubItems[1].Text = string.Empty;
				else
					item.SubItems[1].Text = S._("Error: {0}", update.Error.Message);
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
			try
			{
				updates.OnProgressEvent += installer_ProgressChanged;
				updates.InstallUpdates(e.Argument);
			}
			finally
			{
				updates.OnProgressEvent -= installer_ProgressChanged;
			}
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

				Invoke(new EventHandler<ProgressEventArgs>(installer_ProgressChanged),
					sender, e);
				return;
			}

			UpdateData update = uiUpdates[(UpdateInfo)e.UserState];
			if (e is ProgressErrorEventArgs)
			{
				update.Error = ((ProgressErrorEventArgs)e).Exception;
				update.LVItem.ImageIndex = 3;
				update.LVItem.SubItems[1].Text = S._("Error: {0}", update.Error.Message);
			}
			else
				switch (update.LVItem.ImageIndex)
				{
					case -1:
						update.LVItem.ImageIndex = 1;
						break;
					case 1:
						update.LVItem.ImageIndex = 2;
						break;
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
		/// The Update manager instance used by this form.
		/// </summary>
		UpdateManager updates = new UpdateManager();

		/// <summary>
		/// Maps listview items to the UpdateManager.Update object.
		/// </summary>
		Dictionary<UpdateInfo, UpdateData> uiUpdates = new Dictionary<UpdateInfo, UpdateData>();

		/// <summary>
		/// Manages information associated with the update.
		/// </summary>
		private class UpdateData
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="update">The UpdateManager.Update object containing the
			/// internal representation of the update.</param>
			/// <param name="item">The ListViewItem used for the display of the
			/// update.</param>
			public UpdateData(UpdateInfo update, ListViewItem item)
			{
				Update = update;
				LVItem = item;
			}

			/// <summary>
			/// The UpdateManager.Update object containing the internal representation
			/// of the update.
			/// </summary>
			public UpdateInfo Update;

			/// <summary>
			/// The ListViewItem used for the display of the update.
			/// </summary>
			public ListViewItem LVItem;

			/// <summary>
			/// The amount of the download already completed.
			/// </summary>
			public long amountDownloaded;

			/// <summary>
			/// The error raised when downloading/installing the update, if any. Null
			/// otherwise.
			/// </summary>
			public Exception Error;
		}

		/// <summary>
		/// The number of updates selected for download.
		/// </summary>
		private int selectedUpdates = -1;

		/// <summary>
		/// The number of updates present in the previous count, so the Selected
		/// Updates number can be deemed invalid.
		/// </summary>
		private int updatesCount = -1;
	}

	public class UpdateManager
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public UpdateManager()
		{
			Updates = new UpdateCategoriesDictionary();
		}

		/// <summary>
		/// Retrieves the update list from the server.
		/// </summary>
		public void DownloadUpdateList()
		{
			WebRequest.DefaultCachePolicy = new HttpRequestCachePolicy(
				HttpRequestCacheLevel.Refresh);
			HttpWebRequest req = (HttpWebRequest)
				WebRequest.Create(new Uri("http://eraser.heidi.ie/updates?action=listupdates&" +
					"version=" + Assembly.GetExecutingAssembly().GetName().Version.ToString()));
			
			using (WebResponse resp = req.GetResponse())
			using (Stream strm = resp.GetResponseStream())
			{
				//Download the response
				int bytesRead = 0;
				byte[] buffer = new byte[16384];
				List<byte> responseBuffer = new List<byte>();
				while ((bytesRead = strm.Read(buffer, 0, buffer.Length)) != 0)
				{
					byte[] tmpDest = new byte[bytesRead];
					Buffer.BlockCopy(buffer, 0, tmpDest, 0, bytesRead);
					responseBuffer.AddRange(tmpDest);

					float progress = responseBuffer.Count / (float)resp.ContentLength;
					OnProgress(new ProgressEventArgs(progress, progress, null,
						S._("{0} of {1} downloaded",
							Util.File.GetHumanReadableFilesize(responseBuffer.Count),
							Util.File.GetHumanReadableFilesize(resp.ContentLength))));
				}

				//Parse it.
				using (MemoryStream mStrm = new MemoryStream(responseBuffer.ToArray()))
					ParseUpdateList(mStrm);
			}
		}

		/// <summary>
		/// Parses the list of updates provided by the server
		/// </summary>
		/// <param name="strm">The stream containing the XML data.</param>
		private void ParseUpdateList(Stream strm)
		{
			//Move the XmlReader to the root node
			Updates.Clear();
			mirrors.Clear();
			XmlReader rdr = XmlReader.Create(strm);
			rdr.ReadToFollowing("updateList");

			//Read the descendants of the updateList node (which are categories,
			//except for the <mirrors> element)
			XmlReader categories = rdr.ReadSubtree();
			bool cont = categories.ReadToDescendant("mirrors");
			while (cont)
			{
				if (categories.NodeType == XmlNodeType.Element)
				{
					if (categories.Name == "mirrors")
					{
						Dictionary<string, string> mirrorsList =
							ParseMirror(categories.ReadSubtree());
						Dictionary<string, string>.Enumerator e = mirrorsList.GetEnumerator();
						while (e.MoveNext())
							this.mirrors.Add(e.Current.Key,
								new Mirror(e.Current.Value, e.Current.Key));
					}
					else
						Updates.Add(categories.Name, ParseUpdateCategory(categories.ReadSubtree()));
				}

				cont = categories.Read();
			}
		}

		/// <summary>
		/// Parses a list of mirrors.
		/// </summary>
		/// <param name="rdr">The XML reader object representing the &lt;mirrors&gt; node</param>
		/// <returns>The list of mirrors defined by the element.</returns>
		private static Dictionary<string, string> ParseMirror(XmlReader rdr)
		{
			Dictionary<string, string> result = new Dictionary<string,string>();
			if (!rdr.ReadToDescendant("mirror"))
				return result;

			//Load every element.
			do
			{
				if (rdr.NodeType != XmlNodeType.Element || rdr.Name != "mirror")
					continue;

				string location = rdr.GetAttribute("location");
				result.Add(rdr.ReadElementContentAsString(), location);
			}
			while (rdr.ReadToNextSibling("mirror"));

			return result;
		}

		/// <summary>
		/// Parses a specific category and its assocaited updates.
		/// </summary>
		/// <param name="rdr">The XML reader object representing the element and its children.</param>
		/// <returns>A list of updates in the category.</returns>
		private static UpdateCollection ParseUpdateCategory(XmlReader rdr)
		{
			UpdateCollection result = new UpdateCollection();
			if (!rdr.ReadToDescendant("item"))
				return result;

			//Load every element.
			do
			{
				if (rdr.Name != "item")
					continue;

				UpdateInfo update = new UpdateInfo();
				update.Name = rdr.GetAttribute("name");
				update.Version = new Version(rdr.GetAttribute("version"));
				update.Publisher = rdr.GetAttribute("publisher");
				update.Architecture = rdr.GetAttribute("architecture");
				update.FileSize = Convert.ToInt64(rdr.GetAttribute("filesize"),
					CultureInfo.InvariantCulture);
				update.Link = rdr.ReadElementContentAsString();

				result.Add(update);
			}
			while (rdr.ReadToNextSibling("item"));

			return result;
		}

		/// <summary>
		/// Downloads the list of updates.
		/// </summary>
		/// <param name="updates">The updates to retrieve and install.</param>
		/// <returns>An opaque object for use with InstallUpdates.</returns>
		public object DownloadUpdates(ICollection<UpdateInfo> downloadQueue)
		{
			//Create a folder to hold all our updates.
			DirectoryInfo tempDir = new DirectoryInfo(Path.GetTempPath());
			tempDir = tempDir.CreateSubdirectory("eraser" + Environment.TickCount.ToString(
				CultureInfo.InvariantCulture));

			int currUpdate = 0;
			Dictionary<string, UpdateInfo> tempFilesMap = new Dictionary<string, UpdateInfo>();
			foreach (UpdateInfo update in downloadQueue)
			{
				try
				{
					//Decide on the URL to connect to. The Link of the update may
					//be a relative path (relative to the selected mirror) or an
					//absolute path (which we have no choice)
					Uri reqUri = null;
					if (Uri.IsWellFormedUriString(update.Link, UriKind.Absolute))
						reqUri = new Uri(update.Link);
					else
						reqUri = new Uri(new Uri(SelectedMirror.Link), new Uri(update.Link));
					
					//Then grab the download.
					HttpWebRequest req = (HttpWebRequest)WebRequest.Create(reqUri);
					using (WebResponse resp = req.GetResponse())
					{
						byte[] tempBuffer = new byte[16384];
						string tempFilePath = Path.Combine(
							tempDir.FullName, string.Format(CultureInfo.InvariantCulture, "{0}-{1}",
							++currUpdate, Path.GetFileName(reqUri.GetComponents(UriComponents.Path,
								UriFormat.Unescaped))));

						using (Stream strm = resp.GetResponseStream())
						using (FileStream tempStrm = new FileStream(tempFilePath, FileMode.CreateNew))
						using (BufferedStream bufStrm = new BufferedStream(tempStrm))
						{
							//Copy the information into the file stream
							int readBytes = 0;
							while ((readBytes = strm.Read(tempBuffer, 0, tempBuffer.Length)) != 0)
							{
								bufStrm.Write(tempBuffer, 0, readBytes);

								//Compute progress
								float itemProgress = tempStrm.Position / (float)resp.ContentLength;
								float overallProgress = (currUpdate - 1 + itemProgress) / downloadQueue.Count;
								OnProgress(new ProgressEventArgs(itemProgress, overallProgress,
									update, S._("Downloading: {0}", update.Name)));
							}
						}

						//Store the filename-to-update mapping
						tempFilesMap.Add(tempFilePath, update);

						//Let the event handler know the download is complete.
						OnProgress(new ProgressEventArgs(1.0f, (float)currUpdate / downloadQueue.Count,
							update, S._("Downloaded: {0}", update.Name)));
					}
				}
				catch (Exception e)
				{
					OnProgress(new ProgressErrorEventArgs(new ProgressEventArgs(1.0f,
						(float)currUpdate / downloadQueue.Count, update,
							S._("Error downloading {0}: {1}", update.Name, e.Message)),
						e));
				}
			}

			return tempFilesMap;
		}

		/// <summary>
		/// Installs all updates downloaded.
		/// </summary>
		/// <param name="value">The value returned from a call to
		/// <see cref="DownloadUpdates"/>.</param>
		public void InstallUpdates(object value)
		{
			Dictionary<string, UpdateInfo> tempFiles = (Dictionary<string, UpdateInfo>)value;
			Dictionary<string, UpdateInfo>.KeyCollection files = tempFiles.Keys;
			int currItem = 0;

			try
			{
				foreach (string path in files)
				{
					UpdateInfo item = tempFiles[path];
					float progress = (float)currItem++ / files.Count;
					OnProgress(new ProgressEventArgs(0.0f, progress,
						item, S._("Installing {0}", item.Name)));

					System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
					info.FileName = path;
					info.UseShellExecute = true;

					System.Diagnostics.Process process = System.Diagnostics.Process.Start(info);
					process.WaitForExit(Int32.MaxValue);
					if (process.ExitCode == 0)
						OnProgress(new ProgressEventArgs(1.0f, progress,
							item, S._("Installed {0}", item.Name)));
					else
						OnProgress(new ProgressErrorEventArgs(new ProgressEventArgs(1.0f,
							progress, item, S._("Error installing {0}", item.Name)),
							new ApplicationException(S._("The installer exited with an error code {0}",
								process.ExitCode))));
				}
			}
			finally
			{
				//Clean up after ourselves
				foreach (string file in files)
				{
					DirectoryInfo tempDir = null;
					{
						FileInfo info = new FileInfo(file);
						tempDir = info.Directory;
					}

					tempDir.Delete(true);
					break;
				}
			}
		}

		/// <summary>
		/// Called when the progress of the operation changes.
		/// </summary>
		public EventHandler<ProgressEventArgs> OnProgressEvent { get; set; }

		/// <summary>
		/// Helper function: invokes the OnProgressEvent delegate.
		/// </summary>
		/// <param name="arg">The ProgressEventArgs object holding information
		/// about the progress of the current operation.</param>
		private void OnProgress(ProgressEventArgs arg)
		{
			if (OnProgressEvent != null)
				OnProgressEvent(this, arg);
		}

		/// <summary>
		/// Retrieves the list of mirrors which the server has indicated to exist.
		/// </summary>
		public Dictionary<string, Mirror> Mirrors
		{
			get
			{
				return mirrors;
			}
		}

		/// <summary>
		/// Gets or sets the active mirror to use to download mirrored updates.
		/// </summary>
		public Mirror SelectedMirror
		{
			get
			{
				if (selectedMirror.Link.Length == 0)
				{
					Dictionary<string, Mirror>.Enumerator iter = mirrors.GetEnumerator();
					if (iter.MoveNext())
						return iter.Current.Value;
				}
				return selectedMirror;
			}
			set
			{
				foreach (Mirror mirror in Mirrors.Values)
					if (mirror.Equals(value))
					{
						selectedMirror = value;
						return;
					}

				throw new ArgumentException(S._("Unknown mirror selected."));
			}
		}

		/// <summary>
		/// Retrieves the categories available.
		/// </summary>
		public ICollection<string> Categories
		{
			get
			{
				return Updates.Keys;
			}
		}

		/// <summary>
		/// Retrieves all updates available.
		/// </summary>
		public UpdateCategoriesDictionary Updates { get; private set; }

		/// <summary>
		/// The list of mirrors to download updates from.
		/// </summary>
		private Dictionary<string, Mirror> mirrors =
			new Dictionary<string, Mirror>();

		/// <summary>
		/// The currently selected mirror.
		/// </summary>
		private Mirror selectedMirror;
	}

	/// <summary>
	/// Manages a list of categories, mapping categories to a list of updates.
	/// </summary>
	public class UpdateCategoriesDictionary : IDictionary<string, UpdateCollection>,
		ICollection<KeyValuePair<string, UpdateCollection>>,
		IEnumerable<KeyValuePair<string, UpdateCollection>>
	{
		#region IDictionary<string,UpdateList> Members
		public void Add(string key, UpdateCollection value)
		{
			dictionary.Add(key, value);
		}

		public bool ContainsKey(string key)
		{
			return dictionary.ContainsKey(key);
		}

		public ICollection<string> Keys
		{
			get { return dictionary.Keys; }
		}

		public bool Remove(string key)
		{
			return dictionary.Remove(key);
		}

		public bool TryGetValue(string key, out UpdateCollection value)
		{
			return dictionary.TryGetValue(key, out value);
		}

		public ICollection<UpdateCollection> Values
		{
			get { return dictionary.Values; }
		}

		public UpdateCollection this[string key]
		{
			get
			{
				return dictionary[key];
			}
			set
			{
				dictionary[key] = value;
			}
		}
		#endregion

		#region ICollection<KeyValuePair<string,UpdateList>> Members
		public void Add(KeyValuePair<string, UpdateCollection> item)
		{
			dictionary.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			dictionary.Clear();
		}

		public bool Contains(KeyValuePair<string, UpdateCollection> item)
		{
			return dictionary.ContainsKey(item.Key) && dictionary[item.Key] == item.Value;
		}

		public void CopyTo(KeyValuePair<string, UpdateCollection>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { return dictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(KeyValuePair<string, UpdateCollection> item)
		{
			return dictionary.Remove(item.Key);
		}
		#endregion

		#region IEnumerable<KeyValuePair<string,UpdateList>> Members
		public IEnumerator<KeyValuePair<string, UpdateCollection>> GetEnumerator()
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
		/// The store for the current object.
		/// </summary>
		private Dictionary<string, UpdateCollection> dictionary =
			new Dictionary<string, UpdateCollection>();
	}

	/// <summary>
	/// Manages a category, containing a list of updates.
	/// </summary>
	public class UpdateCollection : IList<UpdateInfo>, ICollection<UpdateInfo>,
		IEnumerable<UpdateInfo>
	{
		#region IList<UpdateInfo> Members
		public int IndexOf(UpdateInfo item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, UpdateInfo item)
		{
			list.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
		}

		public UpdateInfo this[int index]
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

		#region ICollection<UpdateInfo> Members
		public void Add(UpdateInfo item)
		{
			list.Add(item);
		}

		public void Clear()
		{
			list.Clear();
		}

		public bool Contains(UpdateInfo item)
		{
			return list.Contains(item);
		}

		public void CopyTo(UpdateInfo[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return list.Count; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(UpdateInfo item)
		{
			return list.Remove(item);
		}
		#endregion

		#region IEnumerable<UpdateInfo> Members
		public IEnumerator<UpdateInfo> GetEnumerator()
		{
			return list.GetEnumerator();
		}
		#endregion

		#region IEnumerable Members
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
		#endregion

		/// <summary>
		/// The store for this object.
		/// </summary>
		private List<UpdateInfo> list = new List<UpdateInfo>();
	}

	/// <summary>
	/// Represents a download mirror.
	/// </summary>
	public struct Mirror
	{
		public Mirror(string location, string link)
			: this()
		{
			Location = location;
			Link = link;
		}

		/// <summary>
		/// The location where the mirror is at.
		/// </summary>
		public string Location { get; set; }

		/// <summary>
		/// The URL prefix to utilise the mirror.
		/// </summary>
		public string Link { get; set; }

		public override string ToString()
		{
			return Location;
		}
	}

	/// <summary>
	/// Represents an update available on the server.
	/// </summary>
	public struct UpdateInfo
	{
		public string Name { get; set; }
		public Version Version { get; set; }
		public string Publisher { get; set; }
		public string Architecture { get; set; }
		public long FileSize { get; set; }
		public string Link { get; set; }
	}

	/// <summary>
	/// Specialised progress event argument, containing message describing
	/// current action, and overall progress percentage.
	/// </summary>
	public class ProgressEventArgs : ProgressChangedEventArgs
	{
		public ProgressEventArgs(float progressPercentage, float overallPercentage,
			object userState, string message)
			: base((int)(progressPercentage * 100), userState)
		{
			ProgressPercentage = progressPercentage;
			OverallProgressPercentage = overallPercentage;
			Message = message;
		}

		/// <summary>
		/// Gets the asynchronous task progress percentage.
		/// </summary>
		public new float ProgressPercentage { get; private set; }

		/// <summary>
		/// Gets the asynchronous task overall progress percentage.
		/// </summary>
		public float OverallProgressPercentage { get; private set; }

		/// <summary>
		/// Gets the message associated with the current task.
		/// </summary>
		public string Message { get; private set; }
	}

	/// <summary>
	/// Extends the ProgressEventArgs further by allowing for the inclusion of
	/// an exception.
	/// </summary>
	public class ProgressErrorEventArgs : ProgressEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="e">The base ProgressEventArgs object.</param>
		/// <param name="ex">The exception</param>
		public ProgressErrorEventArgs(ProgressEventArgs e, Exception ex)
			: base(e.ProgressPercentage, e.OverallProgressPercentage, e.UserState, e.Message)
		{
			Exception = ex;
		}

		/// <summary>
		/// The exception associated with the progress event.
		/// </summary>
		public Exception Exception { get; private set; }
	}
}
