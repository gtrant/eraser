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

namespace Eraser
{
	partial class TaskPropertiesForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TaskPropertiesForm));
			this.eraseLbl = new System.Windows.Forms.Label();
			this.typeLbl = new System.Windows.Forms.Label();
			this.typeImmediate = new System.Windows.Forms.RadioButton();
			this.typeRecurring = new System.Windows.Forms.RadioButton();
			this.data = new System.Windows.Forms.ListView();
			this.dataColData = new System.Windows.Forms.ColumnHeader();
			this.dataColMethod = new System.Windows.Forms.ColumnHeader();
			this.dataContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dataAdd = new System.Windows.Forms.Button();
			this.ok = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			this.container = new System.Windows.Forms.TabControl();
			this.containerTask = new System.Windows.Forms.TabPage();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.nameLbl = new System.Windows.Forms.Label();
			this.typeRestart = new System.Windows.Forms.RadioButton();
			this.typeManual = new System.Windows.Forms.RadioButton();
			this.name = new System.Windows.Forms.TextBox();
			this.containerSchedule = new System.Windows.Forms.TabPage();
			this.containerSchedulePanel = new System.Windows.Forms.TableLayoutPanel();
			this.nonRecurringPanel = new System.Windows.Forms.Panel();
			this.nonRecurringLbl = new System.Windows.Forms.Label();
			this.nonRecurringBitmap = new System.Windows.Forms.PictureBox();
			this.scheduleTimePanel = new System.Windows.Forms.FlowLayoutPanel();
			this.scheduleTimeLbl = new System.Windows.Forms.Label();
			this.scheduleTime = new System.Windows.Forms.DateTimePicker();
			this.schedulePattern = new System.Windows.Forms.GroupBox();
			this.schedulePanel = new System.Windows.Forms.FlowLayoutPanel();
			this.scheduleDaily = new System.Windows.Forms.RadioButton();
			this.scheduleDailyByDayPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.scheduleDailyByDay = new System.Windows.Forms.RadioButton();
			this.scheduleDailyByDayFreq = new System.Windows.Forms.NumericUpDown();
			this.scheduleDailyByDayLbl = new System.Windows.Forms.Label();
			this.scheduleDailyByWeekday = new System.Windows.Forms.RadioButton();
			this.scheduleWeekly = new System.Windows.Forms.RadioButton();
			this.scheduleWeeklyFrequencyPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.scheduleWeeklyLbl = new System.Windows.Forms.Label();
			this.scheduleWeeklyFreq = new System.Windows.Forms.NumericUpDown();
			this.scheduleWeeklyFreqLbl = new System.Windows.Forms.Label();
			this.scheduleWeeklyDays = new System.Windows.Forms.FlowLayoutPanel();
			this.scheduleWeeklyMonday = new System.Windows.Forms.CheckBox();
			this.scheduleWeeklyTuesday = new System.Windows.Forms.CheckBox();
			this.scheduleWeeklyWednesday = new System.Windows.Forms.CheckBox();
			this.scheduleWeeklyThursday = new System.Windows.Forms.CheckBox();
			this.scheduleWeeklyFriday = new System.Windows.Forms.CheckBox();
			this.scheduleWeeklySaturday = new System.Windows.Forms.CheckBox();
			this.scheduleWeeklySunday = new System.Windows.Forms.CheckBox();
			this.scheduleMonthly = new System.Windows.Forms.RadioButton();
			this.scheduleMonthlyFrequencyPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.scheduleMonthlyLbl = new System.Windows.Forms.Label();
			this.scheduleMonthlyDayNumber = new System.Windows.Forms.NumericUpDown();
			this.scheduleMonthlyEveryLbl = new System.Windows.Forms.Label();
			this.scheduleMonthlyFreq = new System.Windows.Forms.NumericUpDown();
			this.scheduleMonthlyMonthLbl = new System.Windows.Forms.Label();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.dataContextMenuStrip.SuspendLayout();
			this.container.SuspendLayout();
			this.containerTask.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.containerSchedule.SuspendLayout();
			this.containerSchedulePanel.SuspendLayout();
			this.nonRecurringPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nonRecurringBitmap)).BeginInit();
			this.scheduleTimePanel.SuspendLayout();
			this.schedulePattern.SuspendLayout();
			this.schedulePanel.SuspendLayout();
			this.scheduleDailyByDayPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.scheduleDailyByDayFreq)).BeginInit();
			this.scheduleWeeklyFrequencyPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.scheduleWeeklyFreq)).BeginInit();
			this.scheduleWeeklyDays.SuspendLayout();
			this.scheduleMonthlyFrequencyPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.scheduleMonthlyDayNumber)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.scheduleMonthlyFreq)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// eraseLbl
			// 
			resources.ApplyResources(this.eraseLbl, "eraseLbl");
			this.eraseLbl.Name = "eraseLbl";
			// 
			// typeLbl
			// 
			resources.ApplyResources(this.typeLbl, "typeLbl");
			this.typeLbl.Name = "typeLbl";
			// 
			// typeImmediate
			// 
			resources.ApplyResources(this.typeImmediate, "typeImmediate");
			this.typeImmediate.Name = "typeImmediate";
			this.typeImmediate.UseVisualStyleBackColor = true;
			this.typeImmediate.CheckedChanged += new System.EventHandler(this.taskType_CheckedChanged);
			// 
			// typeRecurring
			// 
			resources.ApplyResources(this.typeRecurring, "typeRecurring");
			this.typeRecurring.Name = "typeRecurring";
			this.typeRecurring.UseVisualStyleBackColor = true;
			this.typeRecurring.CheckedChanged += new System.EventHandler(this.taskType_CheckedChanged);
			// 
			// data
			// 
			resources.ApplyResources(this.data, "data");
			this.data.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.dataColData,
            this.dataColMethod});
			this.data.ContextMenuStrip = this.dataContextMenuStrip;
			this.data.FullRowSelect = true;
			this.data.MultiSelect = false;
			this.data.Name = "data";
			this.data.ShowItemToolTips = true;
			this.data.UseCompatibleStateImageBehavior = false;
			this.data.View = System.Windows.Forms.View.Details;
			this.data.ItemActivate += new System.EventHandler(this.data_ItemActivate);
			// 
			// dataColData
			// 
			resources.ApplyResources(this.dataColData, "dataColData");
			// 
			// dataColMethod
			// 
			resources.ApplyResources(this.dataColMethod, "dataColMethod");
			// 
			// dataContextMenuStrip
			// 
			this.dataContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteDataToolStripMenuItem});
			this.dataContextMenuStrip.Name = "dataContextMenuStrip";
			resources.ApplyResources(this.dataContextMenuStrip, "dataContextMenuStrip");
			this.dataContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.dataContextMenuStrip_Opening);
			// 
			// deleteDataToolStripMenuItem
			// 
			this.deleteDataToolStripMenuItem.Name = "deleteDataToolStripMenuItem";
			resources.ApplyResources(this.deleteDataToolStripMenuItem, "deleteDataToolStripMenuItem");
			this.deleteDataToolStripMenuItem.Click += new System.EventHandler(this.deleteDataToolStripMenuItem_Click);
			// 
			// dataAdd
			// 
			resources.ApplyResources(this.dataAdd, "dataAdd");
			this.dataAdd.Name = "dataAdd";
			this.dataAdd.UseVisualStyleBackColor = true;
			this.dataAdd.Click += new System.EventHandler(this.dataAdd_Click);
			// 
			// ok
			// 
			resources.ApplyResources(this.ok, "ok");
			this.ok.Name = "ok";
			this.ok.UseVisualStyleBackColor = true;
			this.ok.Click += new System.EventHandler(this.ok_Click);
			// 
			// cancel
			// 
			resources.ApplyResources(this.cancel, "cancel");
			this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancel.Name = "cancel";
			this.cancel.UseVisualStyleBackColor = true;
			// 
			// container
			// 
			resources.ApplyResources(this.container, "container");
			this.container.Controls.Add(this.containerTask);
			this.container.Controls.Add(this.containerSchedule);
			this.container.Name = "container";
			this.container.SelectedIndex = 0;
			// 
			// containerTask
			// 
			this.containerTask.Controls.Add(this.tableLayoutPanel2);
			this.containerTask.Controls.Add(this.eraseLbl);
			this.containerTask.Controls.Add(this.data);
			this.containerTask.Controls.Add(this.dataAdd);
			resources.ApplyResources(this.containerTask, "containerTask");
			this.containerTask.Name = "containerTask";
			this.containerTask.UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel2
			// 
			resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
			this.tableLayoutPanel2.Controls.Add(this.nameLbl, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this.typeRecurring, 1, 4);
			this.tableLayoutPanel2.Controls.Add(this.typeRestart, 1, 3);
			this.tableLayoutPanel2.Controls.Add(this.typeManual, 1, 1);
			this.tableLayoutPanel2.Controls.Add(this.typeImmediate, 1, 2);
			this.tableLayoutPanel2.Controls.Add(this.name, 1, 0);
			this.tableLayoutPanel2.Controls.Add(this.typeLbl, 0, 1);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			// 
			// nameLbl
			// 
			resources.ApplyResources(this.nameLbl, "nameLbl");
			this.nameLbl.Name = "nameLbl";
			// 
			// typeRestart
			// 
			resources.ApplyResources(this.typeRestart, "typeRestart");
			this.typeRestart.Name = "typeRestart";
			this.typeRestart.TabStop = true;
			this.typeRestart.UseVisualStyleBackColor = true;
			// 
			// typeManual
			// 
			resources.ApplyResources(this.typeManual, "typeManual");
			this.typeManual.Name = "typeManual";
			this.typeManual.TabStop = true;
			this.typeManual.UseVisualStyleBackColor = true;
			this.typeManual.CheckedChanged += new System.EventHandler(this.taskType_CheckedChanged);
			// 
			// name
			// 
			resources.ApplyResources(this.name, "name");
			this.name.Name = "name";
			// 
			// containerSchedule
			// 
			this.containerSchedule.Controls.Add(this.containerSchedulePanel);
			resources.ApplyResources(this.containerSchedule, "containerSchedule");
			this.containerSchedule.Name = "containerSchedule";
			this.containerSchedule.UseVisualStyleBackColor = true;
			// 
			// containerSchedulePanel
			// 
			resources.ApplyResources(this.containerSchedulePanel, "containerSchedulePanel");
			this.containerSchedulePanel.Controls.Add(this.nonRecurringPanel, 0, 0);
			this.containerSchedulePanel.Controls.Add(this.scheduleTimePanel, 0, 1);
			this.containerSchedulePanel.Controls.Add(this.schedulePattern, 0, 2);
			this.containerSchedulePanel.Name = "containerSchedulePanel";
			// 
			// nonRecurringPanel
			// 
			this.nonRecurringPanel.Controls.Add(this.nonRecurringLbl);
			this.nonRecurringPanel.Controls.Add(this.nonRecurringBitmap);
			resources.ApplyResources(this.nonRecurringPanel, "nonRecurringPanel");
			this.nonRecurringPanel.Name = "nonRecurringPanel";
			// 
			// nonRecurringLbl
			// 
			resources.ApplyResources(this.nonRecurringLbl, "nonRecurringLbl");
			this.nonRecurringLbl.Name = "nonRecurringLbl";
			// 
			// nonRecurringBitmap
			// 
			this.nonRecurringBitmap.Image = global::Eraser.Properties.Resources.Information;
			resources.ApplyResources(this.nonRecurringBitmap, "nonRecurringBitmap");
			this.nonRecurringBitmap.Name = "nonRecurringBitmap";
			this.nonRecurringBitmap.TabStop = false;
			// 
			// scheduleTimePanel
			// 
			resources.ApplyResources(this.scheduleTimePanel, "scheduleTimePanel");
			this.scheduleTimePanel.Controls.Add(this.scheduleTimeLbl);
			this.scheduleTimePanel.Controls.Add(this.scheduleTime);
			this.scheduleTimePanel.Name = "scheduleTimePanel";
			// 
			// scheduleTimeLbl
			// 
			resources.ApplyResources(this.scheduleTimeLbl, "scheduleTimeLbl");
			this.scheduleTimeLbl.Name = "scheduleTimeLbl";
			// 
			// scheduleTime
			// 
			this.scheduleTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			resources.ApplyResources(this.scheduleTime, "scheduleTime");
			this.scheduleTime.Name = "scheduleTime";
			this.scheduleTime.ShowUpDown = true;
			// 
			// schedulePattern
			// 
			this.schedulePattern.Controls.Add(this.schedulePanel);
			resources.ApplyResources(this.schedulePattern, "schedulePattern");
			this.schedulePattern.Name = "schedulePattern";
			this.schedulePattern.TabStop = false;
			// 
			// schedulePanel
			// 
			resources.ApplyResources(this.schedulePanel, "schedulePanel");
			this.schedulePanel.Controls.Add(this.scheduleDaily);
			this.schedulePanel.Controls.Add(this.scheduleDailyByDayPanel);
			this.schedulePanel.Controls.Add(this.scheduleDailyByWeekday);
			this.schedulePanel.Controls.Add(this.scheduleWeekly);
			this.schedulePanel.Controls.Add(this.scheduleWeeklyFrequencyPanel);
			this.schedulePanel.Controls.Add(this.scheduleWeeklyDays);
			this.schedulePanel.Controls.Add(this.scheduleMonthly);
			this.schedulePanel.Controls.Add(this.scheduleMonthlyFrequencyPanel);
			this.schedulePanel.Name = "schedulePanel";
			// 
			// scheduleDaily
			// 
			this.scheduleDaily.AutoCheck = false;
			resources.ApplyResources(this.scheduleDaily, "scheduleDaily");
			this.scheduleDaily.Name = "scheduleDaily";
			this.scheduleDaily.TabStop = true;
			this.scheduleDaily.UseVisualStyleBackColor = true;
			this.scheduleDaily.Click += new System.EventHandler(this.scheduleSpan_Clicked);
			// 
			// scheduleDailyByDayPanel
			// 
			resources.ApplyResources(this.scheduleDailyByDayPanel, "scheduleDailyByDayPanel");
			this.scheduleDailyByDayPanel.Controls.Add(this.scheduleDailyByDay);
			this.scheduleDailyByDayPanel.Controls.Add(this.scheduleDailyByDayFreq);
			this.scheduleDailyByDayPanel.Controls.Add(this.scheduleDailyByDayLbl);
			this.scheduleDailyByDayPanel.Name = "scheduleDailyByDayPanel";
			// 
			// scheduleDailyByDay
			// 
			this.scheduleDailyByDay.AutoCheck = false;
			resources.ApplyResources(this.scheduleDailyByDay, "scheduleDailyByDay");
			this.scheduleDailyByDay.Checked = true;
			this.scheduleDailyByDay.Name = "scheduleDailyByDay";
			this.scheduleDailyByDay.TabStop = true;
			this.scheduleDailyByDay.UseVisualStyleBackColor = true;
			this.scheduleDailyByDay.Click += new System.EventHandler(this.scheduleDailySpan_Clicked);
			this.scheduleDailyByDay.CheckedChanged += new System.EventHandler(this.scheduleDailySpan_CheckedChanged);
			// 
			// scheduleDailyByDayFreq
			// 
			resources.ApplyResources(this.scheduleDailyByDayFreq, "scheduleDailyByDayFreq");
			this.scheduleDailyByDayFreq.Maximum = new decimal(new int[] {
            366,
            0,
            0,
            0});
			this.scheduleDailyByDayFreq.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.scheduleDailyByDayFreq.Name = "scheduleDailyByDayFreq";
			this.scheduleDailyByDayFreq.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// scheduleDailyByDayLbl
			// 
			resources.ApplyResources(this.scheduleDailyByDayLbl, "scheduleDailyByDayLbl");
			this.scheduleDailyByDayLbl.Name = "scheduleDailyByDayLbl";
			// 
			// scheduleDailyByWeekday
			// 
			this.scheduleDailyByWeekday.AutoCheck = false;
			resources.ApplyResources(this.scheduleDailyByWeekday, "scheduleDailyByWeekday");
			this.scheduleDailyByWeekday.Name = "scheduleDailyByWeekday";
			this.scheduleDailyByWeekday.UseVisualStyleBackColor = true;
			this.scheduleDailyByWeekday.Click += new System.EventHandler(this.scheduleDailySpan_Clicked);
			this.scheduleDailyByWeekday.CheckedChanged += new System.EventHandler(this.scheduleDailySpan_CheckedChanged);
			// 
			// scheduleWeekly
			// 
			this.scheduleWeekly.AutoCheck = false;
			resources.ApplyResources(this.scheduleWeekly, "scheduleWeekly");
			this.scheduleWeekly.Name = "scheduleWeekly";
			this.scheduleWeekly.TabStop = true;
			this.scheduleWeekly.UseVisualStyleBackColor = true;
			this.scheduleWeekly.Click += new System.EventHandler(this.scheduleSpan_Clicked);
			// 
			// scheduleWeeklyFrequencyPanel
			// 
			resources.ApplyResources(this.scheduleWeeklyFrequencyPanel, "scheduleWeeklyFrequencyPanel");
			this.scheduleWeeklyFrequencyPanel.Controls.Add(this.scheduleWeeklyLbl);
			this.scheduleWeeklyFrequencyPanel.Controls.Add(this.scheduleWeeklyFreq);
			this.scheduleWeeklyFrequencyPanel.Controls.Add(this.scheduleWeeklyFreqLbl);
			this.scheduleWeeklyFrequencyPanel.Name = "scheduleWeeklyFrequencyPanel";
			// 
			// scheduleWeeklyLbl
			// 
			resources.ApplyResources(this.scheduleWeeklyLbl, "scheduleWeeklyLbl");
			this.scheduleWeeklyLbl.Name = "scheduleWeeklyLbl";
			// 
			// scheduleWeeklyFreq
			// 
			resources.ApplyResources(this.scheduleWeeklyFreq, "scheduleWeeklyFreq");
			this.scheduleWeeklyFreq.Maximum = new decimal(new int[] {
            104,
            0,
            0,
            0});
			this.scheduleWeeklyFreq.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.scheduleWeeklyFreq.Name = "scheduleWeeklyFreq";
			this.scheduleWeeklyFreq.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// scheduleWeeklyFreqLbl
			// 
			resources.ApplyResources(this.scheduleWeeklyFreqLbl, "scheduleWeeklyFreqLbl");
			this.scheduleWeeklyFreqLbl.Name = "scheduleWeeklyFreqLbl";
			// 
			// scheduleWeeklyDays
			// 
			resources.ApplyResources(this.scheduleWeeklyDays, "scheduleWeeklyDays");
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklyMonday);
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklyTuesday);
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklyWednesday);
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklyThursday);
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklyFriday);
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklySaturday);
			this.scheduleWeeklyDays.Controls.Add(this.scheduleWeeklySunday);
			this.scheduleWeeklyDays.Name = "scheduleWeeklyDays";
			// 
			// scheduleWeeklyMonday
			// 
			resources.ApplyResources(this.scheduleWeeklyMonday, "scheduleWeeklyMonday");
			this.scheduleWeeklyMonday.Name = "scheduleWeeklyMonday";
			this.scheduleWeeklyMonday.UseVisualStyleBackColor = true;
			// 
			// scheduleWeeklyTuesday
			// 
			resources.ApplyResources(this.scheduleWeeklyTuesday, "scheduleWeeklyTuesday");
			this.scheduleWeeklyTuesday.Name = "scheduleWeeklyTuesday";
			this.scheduleWeeklyTuesday.UseVisualStyleBackColor = true;
			// 
			// scheduleWeeklyWednesday
			// 
			resources.ApplyResources(this.scheduleWeeklyWednesday, "scheduleWeeklyWednesday");
			this.scheduleWeeklyWednesday.Name = "scheduleWeeklyWednesday";
			this.scheduleWeeklyWednesday.UseVisualStyleBackColor = true;
			// 
			// scheduleWeeklyThursday
			// 
			resources.ApplyResources(this.scheduleWeeklyThursday, "scheduleWeeklyThursday");
			this.scheduleWeeklyThursday.Name = "scheduleWeeklyThursday";
			this.scheduleWeeklyThursday.UseVisualStyleBackColor = true;
			// 
			// scheduleWeeklyFriday
			// 
			resources.ApplyResources(this.scheduleWeeklyFriday, "scheduleWeeklyFriday");
			this.scheduleWeeklyFriday.Name = "scheduleWeeklyFriday";
			this.scheduleWeeklyFriday.UseVisualStyleBackColor = true;
			// 
			// scheduleWeeklySaturday
			// 
			resources.ApplyResources(this.scheduleWeeklySaturday, "scheduleWeeklySaturday");
			this.scheduleWeeklySaturday.Name = "scheduleWeeklySaturday";
			this.scheduleWeeklySaturday.UseVisualStyleBackColor = true;
			// 
			// scheduleWeeklySunday
			// 
			resources.ApplyResources(this.scheduleWeeklySunday, "scheduleWeeklySunday");
			this.scheduleWeeklySunday.Name = "scheduleWeeklySunday";
			this.scheduleWeeklySunday.UseVisualStyleBackColor = true;
			// 
			// scheduleMonthly
			// 
			this.scheduleMonthly.AutoCheck = false;
			resources.ApplyResources(this.scheduleMonthly, "scheduleMonthly");
			this.scheduleMonthly.Name = "scheduleMonthly";
			this.scheduleMonthly.TabStop = true;
			this.scheduleMonthly.UseVisualStyleBackColor = true;
			this.scheduleMonthly.Click += new System.EventHandler(this.scheduleSpan_Clicked);
			// 
			// scheduleMonthlyFrequencyPanel
			// 
			resources.ApplyResources(this.scheduleMonthlyFrequencyPanel, "scheduleMonthlyFrequencyPanel");
			this.scheduleMonthlyFrequencyPanel.Controls.Add(this.scheduleMonthlyLbl);
			this.scheduleMonthlyFrequencyPanel.Controls.Add(this.scheduleMonthlyDayNumber);
			this.scheduleMonthlyFrequencyPanel.Controls.Add(this.scheduleMonthlyEveryLbl);
			this.scheduleMonthlyFrequencyPanel.Controls.Add(this.scheduleMonthlyFreq);
			this.scheduleMonthlyFrequencyPanel.Controls.Add(this.scheduleMonthlyMonthLbl);
			this.scheduleMonthlyFrequencyPanel.MinimumSize = new System.Drawing.Size(294, 23);
			this.scheduleMonthlyFrequencyPanel.Name = "scheduleMonthlyFrequencyPanel";
			// 
			// scheduleMonthlyLbl
			// 
			resources.ApplyResources(this.scheduleMonthlyLbl, "scheduleMonthlyLbl");
			this.scheduleMonthlyLbl.Name = "scheduleMonthlyLbl";
			// 
			// scheduleMonthlyDayNumber
			// 
			resources.ApplyResources(this.scheduleMonthlyDayNumber, "scheduleMonthlyDayNumber");
			this.scheduleMonthlyDayNumber.Maximum = new decimal(new int[] {
            31,
            0,
            0,
            0});
			this.scheduleMonthlyDayNumber.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.scheduleMonthlyDayNumber.Name = "scheduleMonthlyDayNumber";
			this.scheduleMonthlyDayNumber.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// scheduleMonthlyEveryLbl
			// 
			resources.ApplyResources(this.scheduleMonthlyEveryLbl, "scheduleMonthlyEveryLbl");
			this.scheduleMonthlyEveryLbl.Name = "scheduleMonthlyEveryLbl";
			// 
			// scheduleMonthlyFreq
			// 
			resources.ApplyResources(this.scheduleMonthlyFreq, "scheduleMonthlyFreq");
			this.scheduleMonthlyFreq.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
			this.scheduleMonthlyFreq.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.scheduleMonthlyFreq.Name = "scheduleMonthlyFreq";
			this.scheduleMonthlyFreq.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// scheduleMonthlyMonthLbl
			// 
			resources.ApplyResources(this.scheduleMonthlyMonthLbl, "scheduleMonthlyMonthLbl");
			this.scheduleMonthlyMonthLbl.Name = "scheduleMonthlyMonthLbl";
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// TaskPropertiesForm
			// 
			this.AcceptButton = this.ok;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancel;
			this.Controls.Add(this.container);
			this.Controls.Add(this.cancel);
			this.Controls.Add(this.ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TaskPropertiesForm";
			this.ShowInTaskbar = false;
			this.dataContextMenuStrip.ResumeLayout(false);
			this.container.ResumeLayout(false);
			this.containerTask.ResumeLayout(false);
			this.containerTask.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.containerSchedule.ResumeLayout(false);
			this.containerSchedulePanel.ResumeLayout(false);
			this.containerSchedulePanel.PerformLayout();
			this.nonRecurringPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.nonRecurringBitmap)).EndInit();
			this.scheduleTimePanel.ResumeLayout(false);
			this.scheduleTimePanel.PerformLayout();
			this.schedulePattern.ResumeLayout(false);
			this.schedulePanel.ResumeLayout(false);
			this.schedulePanel.PerformLayout();
			this.scheduleDailyByDayPanel.ResumeLayout(false);
			this.scheduleDailyByDayPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.scheduleDailyByDayFreq)).EndInit();
			this.scheduleWeeklyFrequencyPanel.ResumeLayout(false);
			this.scheduleWeeklyFrequencyPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.scheduleWeeklyFreq)).EndInit();
			this.scheduleWeeklyDays.ResumeLayout(false);
			this.scheduleWeeklyDays.PerformLayout();
			this.scheduleMonthlyFrequencyPanel.ResumeLayout(false);
			this.scheduleMonthlyFrequencyPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.scheduleMonthlyDayNumber)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.scheduleMonthlyFreq)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label eraseLbl;
		private System.Windows.Forms.Label typeLbl;
		private System.Windows.Forms.RadioButton typeImmediate;
		private System.Windows.Forms.RadioButton typeRecurring;
		private System.Windows.Forms.ListView data;
		private System.Windows.Forms.ColumnHeader dataColData;
		private System.Windows.Forms.ColumnHeader dataColMethod;
		private System.Windows.Forms.Button dataAdd;
		private System.Windows.Forms.Button ok;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.TabControl container;
		private System.Windows.Forms.TabPage containerTask;
		private System.Windows.Forms.TabPage containerSchedule;
		private System.Windows.Forms.TableLayoutPanel containerSchedulePanel;
		private System.Windows.Forms.GroupBox schedulePattern;
		private System.Windows.Forms.Panel nonRecurringPanel;
		private System.Windows.Forms.Label nonRecurringLbl;
		private System.Windows.Forms.PictureBox nonRecurringBitmap;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.RadioButton typeRestart;
		private System.Windows.Forms.ContextMenuStrip dataContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem deleteDataToolStripMenuItem;
		private System.Windows.Forms.RadioButton typeManual;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private System.Windows.Forms.Label nameLbl;
		private System.Windows.Forms.TextBox name;
		private System.Windows.Forms.FlowLayoutPanel scheduleTimePanel;
		private System.Windows.Forms.Label scheduleTimeLbl;
		private System.Windows.Forms.DateTimePicker scheduleTime;
		private System.Windows.Forms.FlowLayoutPanel schedulePanel;
		private System.Windows.Forms.RadioButton scheduleDaily;
		private System.Windows.Forms.FlowLayoutPanel scheduleDailyByDayPanel;
		private System.Windows.Forms.RadioButton scheduleDailyByDay;
		private System.Windows.Forms.NumericUpDown scheduleDailyByDayFreq;
		private System.Windows.Forms.Label scheduleDailyByDayLbl;
		private System.Windows.Forms.RadioButton scheduleDailyByWeekday;
		private System.Windows.Forms.RadioButton scheduleWeekly;
		private System.Windows.Forms.FlowLayoutPanel scheduleWeeklyFrequencyPanel;
		private System.Windows.Forms.Label scheduleWeeklyLbl;
		private System.Windows.Forms.NumericUpDown scheduleWeeklyFreq;
		private System.Windows.Forms.Label scheduleWeeklyFreqLbl;
		private System.Windows.Forms.RadioButton scheduleMonthly;
		private System.Windows.Forms.FlowLayoutPanel scheduleMonthlyFrequencyPanel;
		private System.Windows.Forms.Label scheduleMonthlyLbl;
		private System.Windows.Forms.NumericUpDown scheduleMonthlyDayNumber;
		private System.Windows.Forms.Label scheduleMonthlyEveryLbl;
		private System.Windows.Forms.NumericUpDown scheduleMonthlyFreq;
		private System.Windows.Forms.Label scheduleMonthlyMonthLbl;
		private System.Windows.Forms.FlowLayoutPanel scheduleWeeklyDays;
		private System.Windows.Forms.CheckBox scheduleWeeklyMonday;
		private System.Windows.Forms.CheckBox scheduleWeeklyTuesday;
		private System.Windows.Forms.CheckBox scheduleWeeklyWednesday;
		private System.Windows.Forms.CheckBox scheduleWeeklyThursday;
		private System.Windows.Forms.CheckBox scheduleWeeklyFriday;
		private System.Windows.Forms.CheckBox scheduleWeeklySaturday;
		private System.Windows.Forms.CheckBox scheduleWeeklySunday;
	}
}