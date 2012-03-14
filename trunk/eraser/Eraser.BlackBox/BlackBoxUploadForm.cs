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

				BlackBoxReportUploader uploader = new BlackBoxReportUploader(reports[i]);

				//Check that a similar report has not yet been uploaded.
				UploadWorker.ReportProgress((int)(overallProgress.Progress * 100),
					S._("Checking for status of report {0}...", reports[i].Name));
				if (!uploader.IsNew)
					continue;

				if (UploadWorker.CancellationPending)
					throw new OperationCanceledException();

				//Upload the report.
				UploadWorker.ReportProgress((int)(overallProgress.Progress * 100),
					S._("Compressing Report {0}: {1:#0.00%}", reports[i].Name, 0));

				uploader.Submit(delegate(object from, EraserProgressChangedEventArgs e2)
					{
						reportProgress.Completed = (int)(e2.Progress.Progress * reportProgress.Total);
						SteppedProgressManager reportSteps = (SteppedProgressManager)e2.Progress;
						int step = reportSteps.Steps.IndexOf(reportSteps.CurrentStep);

						UploadWorker.ReportProgress((int)overallProgress.Progress,
							step == 0 ? 
								S._("Compressing Report {0}: {1:#0.00%}",
									reports[i].Name, reportSteps.Progress) :
								S._("Uploading Report {0}: {1:#0.00%}",
									reports[i].Name, reportSteps.Progress));

						if (UploadWorker.CancellationPending)
							throw new OperationCanceledException();
					});
			}
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
