﻿/* 
 * $Id: BlackBoxReportUploader.cs 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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

using System.IO;
using System.Net;
using System.Xml;
using System.Globalization;

using SevenZip;
using SevenZip.Compression.LZMA;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Win32.SafeHandles;

using Eraser.Util;
using Eraser.Plugins;
using ProgressChangedEventHandler = Eraser.Plugins.ProgressChangedEventHandler;
using ProgressChangedEventArgs = Eraser.Plugins.ProgressChangedEventArgs;

namespace Eraser.BlackBox
{
	/// <summary>
	/// Uploads <see cref="BlackBoxReport"/>s to the Eraser server.
	/// </summary>
	public class BlackBoxReportUploader
	{
		private class SevenZipProgressCallback : ICodeProgress
		{
			public SevenZipProgressCallback(BlackBoxReportUploader uploader,
				SteppedProgressManager progress, ProgressManager stepProgress,
				ProgressChangedEventHandler progressChanged)
			{
				Uploader = uploader;
				Progress = progress;
				StepProgress = stepProgress;
				EventHandler = progressChanged;
				LastProgressReport = DateTime.MinValue;
			}

			#region ICodeProgress Members

			public void SetProgress(long inSize, long outSize)
			{
				if ((DateTime.Now - LastProgressReport).Ticks > TimeSpan.TicksPerSecond)
				{
					StepProgress.Completed = inSize;
					EventHandler(Uploader, new ProgressChangedEventArgs(Progress, null));
					LastProgressReport = DateTime.Now;
				}
			}

			#endregion

			private BlackBoxReportUploader Uploader;
			private SteppedProgressManager Progress;
			private ProgressManager StepProgress;
			private ProgressChangedEventHandler EventHandler;

			private DateTime LastProgressReport;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="report">The report to upload.</param>
		public BlackBoxReportUploader(BlackBoxReport report)
		{
			Report = report;
			if (!Directory.Exists(UploadTempDir))
				Directory.CreateDirectory(UploadTempDir);

			ReportBaseName = Path.Combine(UploadTempDir, Report.Name);
		}

		/// <summary>
		/// Gets the status of the report.
		/// </summary>
		public BlackBoxReportStatus Status
		{
			get
			{
				//Get the status from the server.
				XmlDocument result = QueryServer("status", null,
					GetStackTraceField(Report.StackTrace).ToArray());

				//Parse the result document
				XmlNode node = result.SelectSingleNode("/crashReport");
				string reportStatus = node.Attributes.GetNamedItem("status").Value;
				try
				{
					return (BlackBoxReportStatus)Enum.Parse(typeof(BlackBoxReportStatus), reportStatus, true);
				}
				catch (ArgumentException e)
				{
					throw new InvalidDataException(
						"Unknown crash report server response.", e);
				}
			}
		}

		/// <summary>
		/// Compresses the report for uploading.
		/// </summary>
		/// <param name="progress">The <see cref="ProgressManager"/> instance that the
		/// Upload function is using.</param>
		/// <param name="progressChanged">The progress changed event handler that should
		/// be called for upload progress updates.</param>
		private void Compress(SteppedProgressManager progress,
			ProgressChangedEventHandler progressChanged)
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

			ProgressManager step = new ProgressManager();
			progress.Steps.Add(new SteppedProgressManagerStep(step, 0.5f, "Compressing"));
			CoderPropID[] propIDs = 
				{
					CoderPropID.DictionarySize,
					CoderPropID.PosStateBits,
					CoderPropID.LitContextBits,
					CoderPropID.LitPosBits,
					CoderPropID.Algorithm,
					CoderPropID.NumFastBytes,
					CoderPropID.MatchFinder,
					CoderPropID.EndMarker
				};
			object[] properties = 
				{
					(Int32)(1 << 24),			//Dictionary Size
					(Int32)2,					//PosState Bits
					(Int32)0,					//LitContext Bits
					(Int32)2,					//LitPos Bits
					(Int32)2,					//Algorithm
					(Int32)128,					//Fast Bytes
					"bt4",						//Match Finger
					true						//Write end-of-stream
				};

			SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
			encoder.SetCoderProperties(propIDs, properties);

			using (FileStream sevenZipFile = new FileStream(ReportBaseName + ".tar.7z",
				FileMode.Create))
			using (FileStream tarStream = new FileStream(ReportBaseName + ".tar",
				FileMode.Open, FileAccess.Read, FileShare.Read, 262144, FileOptions.DeleteOnClose))
			{
				encoder.WriteCoderProperties(sevenZipFile);
				Int64 fileSize = -1;
				for (int i = 0; i < 8; i++)
					sevenZipFile.WriteByte((Byte)(fileSize >> (8 * i)));

				step.Total = tarStream.Length;
				ICodeProgress callback = progressChanged == null ? null :
					new SevenZipProgressCallback(this, progress, step, progressChanged);
				encoder.Code(tarStream, sevenZipFile, -1, -1, callback);
			}
		}

		/// <summary>
		/// Compresses the report, then uploads it to the server.
		/// </summary>
		/// <param name="progressChanged">The progress changed event handler that should
		/// be called for upload progress updates.</param>
		public void Submit(ProgressChangedEventHandler progressChanged)
		{
			SteppedProgressManager overallProgress = new SteppedProgressManager();
			Compress(overallProgress, progressChanged);

			using (FileStream dumpFile = new FileStream(ReportBaseName + ".tar.7z",
				FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.DeleteOnClose))
			{
				List<PostDataField> fields = GetStackTraceField(Report.StackTrace);
				fields.Add(new PostDataFileField("crashReport", "Report.tar.7z", dumpFile));

				ProgressManager progress = new ProgressManager();
				overallProgress.Steps.Add(new SteppedProgressManagerStep(
					progress, 0.5f, "Uploading"));

				XmlDocument result = QueryServer("upload", delegate(long uploaded, long total)
					{
						progress.Total = total;
						progress.Completed = uploaded;
						progressChanged(this, new ProgressChangedEventArgs(overallProgress, null));
					}, fields.ToArray());

				//Parse the result document
				XmlNode node = result.SelectSingleNode("/crashReport");
				string reportStatus = node.Attributes.GetNamedItem("status").Value;
				if (reportStatus == "exists")
				{
					string reportId = node.Attributes.GetNamedItem("id").Value;
					Report.Status = BlackBoxReportStatus.Uploaded;
					Report.ID = Convert.ToInt32(reportId);
				}
			}
		}

		/// <summary>
		/// Builds the stackTrace POST data field and retrieves the Post data fields to include
		/// with the request.
		/// </summary>
		/// <param name="stackTrace">The stack trace to add.</param>
		/// <returns>A list of PostDataField objects which can be added to a PostDataBuilder
		/// object to add stack trace information to the request.</returns>
		private static List<PostDataField> GetStackTraceField(IList<BlackBoxExceptionEntry> stackTrace)
		{
			int exceptionIndex = 0;
			List<PostDataField> result = new List<PostDataField>();
			foreach (BlackBoxExceptionEntry exceptionStack in stackTrace)
			{
				foreach (string stackFrame in exceptionStack.StackTrace)
					result.Add(new PostDataField(
						string.Format(CultureInfo.InvariantCulture, "stackTrace[{0}][]", exceptionIndex), stackFrame));

				result.Add(new PostDataField(string.Format(CultureInfo.InvariantCulture,
					"stackTrace[{0}][exception]", exceptionIndex), exceptionStack.ExceptionType));
				++exceptionIndex;
			}

			return result;
		}

		/// <summary>
		/// Builds a WebRequest object and queries the server for a response.
		/// </summary>
		/// <param name="action">The action to perform.</param>
		/// <param name="progressChanged">A progress changed event handler receiving
		/// upload progress information.</param>
		/// <param name="fields">The POST fields to upload along with the request.</param>
		/// <returns>An XmlReader containing the response.</returns>
		private XmlDocument QueryServer(string action, QueryProgress progressChanged = null,
			params PostDataField[] fields)
		{
			PostDataBuilder builder = new PostDataBuilder();
			builder.AddPart(new PostDataField("action", action));
			builder.AddParts(fields);

			WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
			reportRequest.ContentType = builder.ContentType;
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
						if (progressChanged != null)
							progressChanged(formStream.Position, formStream.Length);
					}
				}
			}

			try
			{
				HttpWebResponse response = reportRequest.GetResponse() as HttpWebResponse;
				using (Stream responseStream = response.GetResponseStream())
				{
					XmlReader reader = XmlReader.Create(responseStream);
					XmlDocument result = new XmlDocument();
					result.Load(reader);
					return result;
				}
			}
			catch (XmlException)
			{
				return null;
			}
			catch (WebException e)
			{
				if (e.Response == null)
					throw;

				using (Stream responseStream = e.Response.GetResponseStream())
				{
					try
					{
						XmlReader reader = XmlReader.Create(responseStream);
						reader.ReadToFollowing("error");
						throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture,
							"The server encountered a problem while processing the request: {0}",
							reader.ReadString()));
					}
					catch (XmlException)
					{
					}
				}

				throw new InvalidDataException(((HttpWebResponse)e.Response).StatusDescription);
			}
		}

		/// <summary>
		/// Delegate object for upload progress callbacks.
		/// </summary>
		/// <param name="uploaded">The amount of data uploaded.</param>
		/// <param name="total">The amount of data to upload.</param>
		private delegate void QueryProgress(long uploaded, long total);

		/// <summary>
		/// The path to where the temporary files are stored before uploading.
		/// </summary>
		private static readonly string UploadTempDir =
			Path.Combine(Path.GetTempPath(), "Eraser Crash Reports");

		/// <summary>
		/// The URI to the BlackBox server.
		/// </summary>
		private static readonly Uri BlackBoxServer =
			new Uri("https://eraser.heidi.ie/scripts/blackbox/upload.php");

		/// <summary>
		/// The report being uploaded.
		/// </summary>
		private BlackBoxReport Report;

		/// <summary>
		/// The base name of the report.
		/// </summary>
		private readonly string ReportBaseName;
	}
}
