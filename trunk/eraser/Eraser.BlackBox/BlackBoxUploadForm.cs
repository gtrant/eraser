/* 
 * $Id$
 * Copyright 2008-2019 The Eraser Project
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
using Eraser.Plugins;

using ProgressChangedEventArgs = System.ComponentModel.ProgressChangedEventArgs;
using EraserProgressChangedEventArgs = Eraser.Plugins.ProgressChangedEventArgs;

namespace Eraser.BlackBox
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
			Theming.ApplyTheme(this);
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
			SteppedProgressManager overallProgress = new SteppedProgressManager();

			for (int i = 0; i < reports.Count; ++i)
			{
				//Create the progress object that will handle the progress for this report.
				ProgressManager reportProgress = new ProgressManager();
				overallProgress.Steps.Add(new SteppedProgressManagerStep(reportProgress,
					1.0f / reports.Count));

				//Allow us to bail out.
				if (UploadWorker.CancellationPending)
					throw new OperationCanceledException();

				//If we have not submitted the report before upload it.
				if (reports[i].Status == BlackBoxReportStatus.New)
					Upload(reports[i], overallProgress, reportProgress);

				//Otherwise check for solutions.
				else
					CheckStatus(reports[i], overallProgress, reportProgress);
			}
		}

		private void Upload(BlackBoxReport report, SteppedProgressManager overallProgress,
			ProgressManager reportProgress)
		{
			//Upload the report.
			UploadWorker.ReportProgress((int)(overallProgress.Progress * 100),
				S._("Compressing Report {0}", report.Name));

			reportProgress.Total = int.MaxValue;
			BlackBoxReportUploader uploader = new BlackBoxReportUploader(report);
			uploader.Submit(delegate(object from, EraserProgressChangedEventArgs e2)
				{
					SteppedProgressManager reportSteps = (SteppedProgressManager)e2.Progress;
					reportProgress.Completed = (int)(reportSteps.Progress * reportProgress.Total);
					int step = reportSteps.Steps.IndexOf(reportSteps.CurrentStep);

					UploadWorker.ReportProgress((int)(overallProgress.Progress * 100),
						step == 0 ?
							S._("Compressing Report {0}", report.Name) :
							S._("Uploading Report {0}", report.Name));

					if (UploadWorker.CancellationPending)
						throw new OperationCanceledException();
				});
		}

		private void CheckStatus(BlackBoxReport report, SteppedProgressManager overallProgress,
			ProgressManager reportProgress)
		{
			//Upload the report.
			UploadWorker.ReportProgress((int)(overallProgress.Progress * 100),
				S._("Checking for solution for {0}...", report.Name));

			BlackBoxReportUploader uploader = new BlackBoxReportUploader(report);
			report.Status = uploader.Status;
		}

		private void UploadWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (e.UserState != null)
				ProgressLbl.Text = (string)e.UserState;
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
					MessageBoxDefaultButton.Button1, Localisation.IsRightToLeft(this) ?
						MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0);
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
}
