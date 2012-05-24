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
			}

			#region ICodeProgress Members

			public void SetProgress(long inSize, long outSize)
			{
				StepProgress.Completed = inSize;
				EventHandler(Uploader, new ProgressChangedEventArgs(Progress, null));
			}

			#endregion

			private BlackBoxReportUploader Uploader;
			private SteppedProgressManager Progress;
			private ProgressManager StepProgress;
			private ProgressChangedEventHandler EventHandler;
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
		/// Gets from the server based on the stack trace whether this report is
		/// new.
		/// </summary>
		public bool IsNew
		{
			get
			{
				PostDataBuilder builder = new PostDataBuilder();
				builder.AddPart(new PostDataField("action", "status"));
				AddStackTraceToRequest(Report.StackTrace, builder);

				WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
				reportRequest.ContentType = builder.ContentType;
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
								throw new InvalidDataException(
									"Unknown crash report server response.");
						}
					}
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

			using (FileStream bzipFile = new FileStream(ReportBaseName + ".tar.7z",
				FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.DeleteOnClose))
			using (Stream logFile = Report.DebugLog)
			{
				//Build the POST request
				PostDataBuilder builder = new PostDataBuilder();
				builder.AddPart(new PostDataField("action", "upload"));
				builder.AddPart(new PostDataFileField("crashReport", "Report.tar.7z", bzipFile));
				AddStackTraceToRequest(Report.StackTrace, builder);

				//Upload the POST request
				WebRequest reportRequest = HttpWebRequest.Create(BlackBoxServer);
				reportRequest.ContentType = builder.ContentType;
				reportRequest.Method = "POST";
				reportRequest.Timeout = int.MaxValue;
				using (Stream formStream = builder.Stream)
				{
					ProgressManager progress = new ProgressManager();
					overallProgress.Steps.Add(new SteppedProgressManagerStep(
						progress, 0.5f, "Uploading"));
					reportRequest.ContentLength = formStream.Length;

					using (Stream requestStream = reportRequest.GetRequestStream())
					{
						int lastRead = 0;
						byte[] buffer = new byte[32768];
						while ((lastRead = formStream.Read(buffer, 0, buffer.Length)) != 0)
						{
							requestStream.Write(buffer, 0, lastRead);

							progress.Total = formStream.Length;
							progress.Completed = formStream.Position;
							progressChanged(this, new ProgressChangedEventArgs(overallProgress, null));
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
		}

		/// <summary>
		/// Adds the stack trace to the given form request.
		/// </summary>
		/// <param name="stackTrace">The stack trace to add.</param>
		/// <param name="builder">The Form request builder to add the stack trace to.</param>
		private static void AddStackTraceToRequest(IList<BlackBoxExceptionEntry> stackTrace,
			PostDataBuilder builder)
		{
			int exceptionIndex = 0;
			foreach (BlackBoxExceptionEntry exceptionStack in stackTrace)
			{
				foreach (string stackFrame in exceptionStack.StackTrace)
					builder.AddPart(new PostDataField(
						string.Format(CultureInfo.InvariantCulture, "stackTrace[{0}][]", exceptionIndex), stackFrame));
				builder.AddPart(new PostDataField(string.Format(CultureInfo.InvariantCulture,
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
}
