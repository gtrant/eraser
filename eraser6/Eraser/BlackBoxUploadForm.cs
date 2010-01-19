﻿/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
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
using System.IO;

using Eraser.Util;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;
using System.Net;
using System.Xml;

namespace Eraser
{
	public partial class BlackBoxUploadForm : Form
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="reports">The list of reports to upload.</param>
		public BlackBoxUploadForm(IList<BlackBoxReport> reports)
		{
			InitializeComponent();
			UXThemeApi.UpdateControlTheme(this);
			UploadWorker.RunWorkerAsync(reports);
		}

		private void BlackBoxUploadForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (UploadWorker.IsBusy)
			{
				UploadWorker.CancelAsync();
				e.Cancel = true;
			}
		}

		private void UploadWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			IList<BlackBoxReport> reports = (IList<BlackBoxReport>)e.Argument;
			for (int i = 0; i < reports.Count; ++i)
			{
				//Calculate the base progress percentage for this job.
				int progressPerReport = 100 / reports.Count;
				int baseProgress = i * progressPerReport;
				int stepsPerReport = 2;

				BlackBoxReportUploader uploader = new BlackBoxReportUploader(reports[i]);

				//Check that a similar report has not yet been uploaded.
				UploadWorker.ReportProgress(baseProgress,
					S._("Checking for status of report {0}...", reports[i].Name));
				if (!uploader.ReportIsNew())
					continue;

				if (UploadWorker.CancellationPending)
					throw new OperationCanceledException();

				//No similar reports have been uploaded. Compress the report.
				UploadWorker.ReportProgress(baseProgress,
					S._("Compressing Report {0}: {1}%", reports[i].Name, 0));
				uploader.Compress(delegate(object from, ProgressChangedEventArgs progress)
					{
						UploadWorker.ReportProgress(baseProgress +
							progress.ProgressPercentage * progressPerReport / 100 / stepsPerReport,
							S._("Compressing Report {0}: {1}%",
								reports[i].Name, progress.ProgressPercentage));

						if (UploadWorker.CancellationPending)
							throw new OperationCanceledException();
					});

				//Upload the report.
				UploadWorker.ReportProgress(baseProgress + progressPerReport / 2,
					S._("Uploading Report {0}: {1}%", reports[i].Name, 0));
				uploader.Upload(delegate(object from, ProgressChangedEventArgs progress)
					{
						UploadWorker.ReportProgress(baseProgress + progressPerReport / stepsPerReport +
							progress.ProgressPercentage * progressPerReport / 100 / stepsPerReport,
							S._("Uploading Report {0}: {1}%",
								reports[i].Name, progress.ProgressPercentage));

						if (UploadWorker.CancellationPending)
							throw new OperationCanceledException();
					});
			}
		}

		private void UploadWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (e.UserState != null)
				ProgressLbl.Text = e.UserState as string;
			ProgressPb.Value = e.ProgressPercentage;
		}

		private void UploadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error == null)
			{
				ProgressLbl.Text = S._("Reports submitted successfully.");
				ProgressPb.Value = ProgressPb.Maximum;
				CancelBtn.Text = S._("Close");
			}
			else if (e.Error is OperationCanceledException)
			{
				ProgressLbl.Text = S._("Submission was cancelled.");
				ProgressPb.Value = ProgressPb.Maximum;
				CancelBtn.Text = S._("Close");
			}
			else
			{
				MessageBox.Show(this, e.Error.Message,
					S._("Eraser"), MessageBoxButtons.OK, MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button1, S.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading : 0);
				Close();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			if (UploadWorker.IsBusy)
				UploadWorker.CancelAsync();
			else
				Close();
		}
	}

	class BlackBoxReportUploader
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="report">The report to upload.</param>
		public BlackBoxReportUploader(BlackBoxReport report)
		{
			Report = report;
			if (!Directory.Exists(UploadTempDir))
				Directory.CreateDirectory(UploadTempDir);

			ReportBaseName =  Path.Combine(UploadTempDir, Report.Name);
		}

		/// <summary>
		/// Verifies the stack trace against the server to see if the report is new.
		/// </summary>
		/// <returns>True if the report is new; false otherwise</returns>
		public bool ReportIsNew()
		{
			MultipartFormDataBuilder builder = new MultipartFormDataBuilder();
			builder.AddPart(new FormField("action", "status"));
			AddStackTraceToRequest(Report.StackTrace, builder);
			
			WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
			reportRequest.ContentType = "multipart/form-data; boundary=" + builder.Boundary;
			reportRequest.Method = "POST";
			using (Stream formStream = builder.Stream)
			{
				reportRequest.ContentLength = formStream.Length;
				using (Stream requestStream = reportRequest.GetRequestStream())
				{
					int lastRead = 0;
					byte[] buffer = new byte[32768];
					while ((lastRead = formStream.Read(buffer, 0, buffer.Length)) != 0)
						requestStream.Write(buffer, 0, lastRead);
				}
			}

			try
			{
				HttpWebResponse response = reportRequest.GetResponse() as HttpWebResponse;
				using (Stream responseStream = response.GetResponseStream())
				{
					XmlReader reader = XmlReader.Create(responseStream);
					reader.ReadToFollowing("crashReport");
					string reportStatus = reader.GetAttribute("status");
					switch (reportStatus)
					{
						case "exists":
							Report.Submitted = true;
							return false;

						case "new":
							return true;

						default:
							throw new InvalidDataException(S._("Unknown crash report server response."));
					}
				}
			}
			catch (WebException e)
			{
				using (Stream responseStream = e.Response.GetResponseStream())
				{
					try
					{
						XmlReader reader = XmlReader.Create(responseStream);
						reader.ReadToFollowing("error");
						throw new InvalidDataException(S._("The server encountered a problem " +
							"while processing the request: {0}", reader.ReadString()));
					}
					catch (XmlException)
					{
					}
				}

				throw new InvalidDataException(((HttpWebResponse)e.Response).StatusDescription);
			}
		}

		public void Compress(ProgressChangedEventHandler progressChanged)
		{
			using (FileStream archiveStream = new FileStream(ReportBaseName + ".tar",
					FileMode.Create, FileAccess.Write))
			{
				//Add the report into a tar file
				TarArchive archive = TarArchive.CreateOutputTarArchive(archiveStream);
				foreach (FileInfo file in Report.Files)
				{
					TarEntry entry = TarEntry.CreateEntryFromFile(file.FullName);
					entry.Name = Path.GetFileName(entry.Name);
					archive.WriteEntry(entry, false);
				}
				archive.Close();
			}

			using (FileStream bzipFile = new FileStream(ReportBaseName + ".tbz",
				FileMode.Create))
			using (FileStream tarStream = new FileStream(ReportBaseName + ".tar",
				FileMode.Open, FileAccess.Read, FileShare.Read, 262144, FileOptions.DeleteOnClose))
			using (BZip2OutputStream bzipStream = new BZip2OutputStream(bzipFile, 262144))
			{
				//Compress the tar file
				int lastRead = 0;
				byte[] buffer = new byte[524288];
				while ((lastRead = tarStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					bzipStream.Write(buffer, 0, lastRead);
					progressChanged(this, new ProgressChangedEventArgs(
						(int)(tarStream.Position * 100 / tarStream.Length), null));
				}
			}
		}

		public void Upload(ProgressChangedEventHandler progressChanged)
		{
			using (FileStream bzipFile = new FileStream(ReportBaseName + ".tbz",
				FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.DeleteOnClose))
			using (Stream logFile = Report.DebugLog)
			{
				//Build the POST request
				MultipartFormDataBuilder builder = new MultipartFormDataBuilder();
				builder.AddPart(new FormField("action", "upload"));
				builder.AddPart(new FormFileField("crashReport", "Report.tbz", bzipFile));
				AddStackTraceToRequest(Report.StackTrace, builder);

				//Upload the POST request
				WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
				reportRequest.ContentType = "multipart/form-data; boundary=" + builder.Boundary;
				reportRequest.Method = "POST";
				reportRequest.Timeout = int.MaxValue;
				using (Stream formStream = builder.Stream)
				{
					reportRequest.ContentLength = formStream.Length;
					using (Stream requestStream = reportRequest.GetRequestStream())
					{
						int lastRead = 0;
						byte[] buffer = new byte[32768];
						while ((lastRead = formStream.Read(buffer, 0, buffer.Length)) != 0)
						{
							requestStream.Write(buffer, 0, lastRead);
							progressChanged(this, new ProgressChangedEventArgs(
								(int)(formStream.Position * 100 / formStream.Length), null));
						}
					}
				}

				try
				{
					reportRequest.GetResponse();
					Report.Submitted = true;
				}
				catch (WebException e)
				{
					using (Stream responseStream = e.Response.GetResponseStream())
					{
						try
						{
							XmlReader reader = XmlReader.Create(responseStream);
							reader.ReadToFollowing("error");
							throw new InvalidDataException(S._("The server encountered a problem " +
								"while processing the request: {0}", reader.ReadString()));
						}
						catch (XmlException)
						{
						}
					}

					throw new InvalidDataException(((HttpWebResponse)e.Response).StatusDescription);
				}
			}
		}

		/// <summary>
		/// Adds the stack trace to the given form request.
		/// </summary>
		/// <param name="stackTrace">The stack trace to add.</param>
		/// <param name="builder">The Form request builder to add the stack trace to.</param>
		private static void AddStackTraceToRequest(IList<BlackBoxExceptionEntry> stackTrace,
			MultipartFormDataBuilder builder)
		{
			int exceptionIndex = 0;
			foreach (BlackBoxExceptionEntry exceptionStack in stackTrace)
			{
				foreach (string stackFrame in exceptionStack.StackTrace)
					builder.AddPart(new FormField(
						string.Format("stackTrace[{0}][]", exceptionIndex), stackFrame));
				builder.AddPart(new FormField(string.Format(
					"stackTrace[{0}][exception]", exceptionIndex), exceptionStack.ExceptionType));
				++exceptionIndex;
			}
		}

		/// <summary>
		/// The path to where the temporary files are stored before uploading.
		/// </summary>
		private static readonly string UploadTempDir =
			Path.Combine(Path.GetTempPath(), "Eraser Crash Reports");

		/// <summary>
		/// The URI to the BlackBox server.
		/// </summary>
		private static readonly Uri BlackBoxServer =
			new Uri("http://eraser.heidi.ie/scripts/blackbox/upload.php");

		/// <summary>
		/// The report being uploaded.
		/// </summary>
		private BlackBoxReport Report;

		/// <summary>
		/// The base name of the report.
		/// </summary>
		private readonly string ReportBaseName;
	}

	class MultipartFormDataBuilder
	{
		public MultipartFormDataBuilder()
		{
			FileName = Path.GetTempFileName();
		}

		public void AddPart(FormField field)
		{
			//Generate a random part boundary
			if (Boundary == null)
			{
				Random rand = new Random();
				for (int i = 0, j = 20 + rand.Next(40); i < j; ++i)
					Boundary += ValidBoundaryChars[rand.Next(ValidBoundaryChars.Length)];
			}

			using (FileStream stream = new FileStream(FileName, FileMode.Open, FileAccess.Write,
				FileShare.Read))
			{
				//Append data!
				stream.Seek(0, SeekOrigin.End);

				StringBuilder currentBoundary = new StringBuilder();
				currentBoundary.AppendFormat("--{0}\r\n", Boundary);
				if (field is FormFileField)
				{
					currentBoundary.AppendFormat(
						"Content-Disposition: file; name=\"{0}\"; filename=\"{1}\"\r\n",
						field.FieldName, ((FormFileField)field).FileName);
					currentBoundary.AppendLine("Content-Type: application/octet-stream");
				}
				else
				{
					currentBoundary.AppendFormat("Content-Disposition: form-data; name=\"{0}\"\r\n",
						field.FieldName);
				}
				
				currentBoundary.AppendLine();
				byte[] boundary = Encoding.UTF8.GetBytes(currentBoundary.ToString());
				stream.Write(boundary, 0, boundary.Length);
				
				int lastRead = 0;
				byte[] buffer = new byte[524288];
				while ((lastRead = field.Stream.Read(buffer, 0, buffer.Length)) != 0)
					stream.Write(buffer, 0, lastRead);

				currentBoundary = new StringBuilder();
				currentBoundary.AppendFormat("\r\n--{0}--\r\n", Boundary);
				boundary = Encoding.UTF8.GetBytes(currentBoundary.ToString());
				stream.Write(boundary, 0, boundary.Length);
			}
		}

		/// <summary>
		/// Gets a stream with which to read the data from.
		/// </summary>
		public Stream Stream
		{
			get
			{
				return new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
		}

		/// <summary>
		/// The Multipart/Form-Data boundary in use. If this is NULL, WritePostData will generate one
		/// and store it here.
		/// </summary>
		public string Boundary
		{
			get;
			set;
		}

		private string FileName;

		/// <summary>
		/// Characters valid for use in the multipart boundary.
		/// </summary>
		private static readonly string ValidBoundaryChars =
			"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
	}

	class FormField
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="stream">The stream containing the field data.</param>
		public FormField(string fieldName, Stream stream)
		{
			FieldName = fieldName;
			Stream = stream;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="stream">The content of the field.</param>
		public FormField(string fieldName, string content)
			: this(fieldName, new MemoryStream(Encoding.UTF8.GetBytes(content)))
		{
		}

		/// <summary>
		/// The name of the field.
		/// </summary>
		public string FieldName;

		/// <summary>
		/// The stream containing the data for this field.
		/// </summary>
		public Stream Stream;
	}

	class FormFileField : FormField
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fieldName">The name of the form field.</param>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="stream">The stream containing the field data.</param>
		public FormFileField(string fieldName, string fileName, Stream stream)
			: base(fieldName, stream)
		{
			FileName = fileName;
		}

		/// <summary>
		/// The name of the file.
		/// </summary>
		public string FileName;
	}
}
