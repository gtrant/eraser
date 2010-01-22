/* 
 * $Id$
 * Copyright 2008-2009 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: Kasra Nassiri <cjax@users.sourceforge.net>
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
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using Eraser.Util;
using System.Diagnostics;
using System.Globalization;

namespace Eraser
{
	public partial class AboutForm : Form
	{
		private Bitmap AboutBitmap;
		private Point AboutBitmapPos;
		private string AboutText;
		private Bitmap AboutTextBitmap;
		private Rectangle AboutTextRect;

		private Bitmap ParentBitmap;
		private int ParentOpacity;
		private int AboutTextScrollTop;

		private Rectangle WebsiteRect;
		private Rectangle DonateRect;

		private Bitmap DoubleBufferBitmap;

		public AboutForm(Control parent)
		{
			//Create and position the dialog
			InitializeComponent();
			UXThemeApi.UpdateControlTheme(this);
			ClientSize = new Size(parent.ClientSize.Width, parent.ClientSize.Height);
			Point point = parent.PointToScreen(new Point(0, 0));
			Left = point.X;
			Top = point.Y;

			//Load the localised About Text
			AboutText = S._(@"Eraser is an advanced security tool for Windows, which allows you to completely remove sensitive data from your hard drive by overwriting it several times with carefully selected patterns. Eraser is Free software and its source code is released under GNU General Public License.

Erasure Method Credits:
Gutmann (35 Pass):	 	Copyright Peter Gutmann & Colin Plumb
Gutmann Lite (10 pass): 		Copyright Peter Gutmann & Colin Plumb
US DoD (7 pass):			Copyright United States Department of
				Defense
US DoD (3 pass):			Copyright United States Department of
				Defense
US Air Force 5020 (3 pass):	Copyright United States Department of
				Defense
US Army AR380-19 (3 pass):	Copyright United States Department of
				Defense
RCMP TSSIT OPS-II (7 pass):	Copyright Government of Canada,
				Royal Canadian Mounted Police
Schneier's Method (7 pass):	Copyright Bruce Schneier
German VSITR (7 pass):		Copyright Germany BSI Verschlusssachen
				-IT-Richtlinien
British HMG IS5 (3 pass):		Copyright British Government
British HMG IS5 (1 pass):		Copyright British Government
Russian GOST P50739-95 (2 pass):	Copyright Government of the Soviet
				Union 
Pseudorandom data (1 pass):	Public Domain
First/Last 16Kb:			Copyright The Eraser Project

Eraser Copyright " + "\u00A9" + @" The Eraser Project

Eraser Project Members:
" + "\u2022" + @" Sami Tolvanen:		Mentor/Initiator
" + "\u2022" + @" Garrett Trant:		Mentor/Researcher
" + "\u2022" + @" Joel Low:		Lead Developer
" + "\u2022" + @" Kasra Nassiri:		Developer/Security Researcher
" + "\u2022" + @" Dennis van Lith:		Designer");

			//Create the About bitmap localised for the current version (sans scrolling
			//text) so it can be drawn quickly later.
			AboutBitmap = Properties.Resources.AboutDialog;
			AboutBitmap = AboutBitmap.Clone(new Rectangle(0, 0, AboutBitmap.Width,
				AboutBitmap.Height), PixelFormat.DontCare);
			using (Graphics g = Graphics.FromImage(AboutBitmap))
			{
				//Version number
				Font boldFont = new Font(Font, FontStyle.Bold);
				Font underlineFont = new Font(Font, FontStyle.Underline);
				Brush textBrush = new SolidBrush(Color.White);
				PointF eraserPos = new PointF(168, 80);
				SizeF eraserSize = g.MeasureString(S._("Eraser"), boldFont);
				g.DrawString(S._("Eraser"), boldFont, textBrush, eraserPos);

				#pragma warning disable 429
				string versionString = BuildInfo.CustomBuild ?
					S._("{0} (Built: {1:F}, special build)") : S._("{0} (Built: {1:F})");
				#pragma warning restore 429

				g.DrawString(string.Format(CultureInfo.CurrentCulture, versionString,
					Assembly.GetExecutingAssembly().GetName().Version, BuildInfo.BuildDate),
					Font, textBrush, new PointF(eraserPos.X + eraserSize.Width + 3, eraserPos.Y));

				//Copyright and Website
				string copyrightText = S._("copyright \u00a9 2008-2009 The Eraser Project");
				PointF copyrightPos = new PointF(eraserPos.X, eraserPos.Y + eraserSize.Height);
				SizeF copyrightSize = g.MeasureString(copyrightText, Font);
				g.DrawString(copyrightText, Font, textBrush, copyrightPos);

				string websiteText = "http://eraser.heidi.ie/";
				PointF websitePos = new PointF(copyrightPos.X, copyrightPos.Y + copyrightSize.Height);
				SizeF websiteSize = g.MeasureString(websiteText, Font);
				g.DrawString(websiteText, underlineFont, textBrush, websitePos);
				WebsiteRect = new Rectangle((int)websitePos.X, (int)websitePos.Y,
					(int)websiteSize.Width, (int)websiteSize.Height);

				//Open source disclaimer.
				string disclaimerText = S._("Eraser is free open-source software!");
				PointF disclaimerPos = new PointF(websitePos.X, websitePos.Y + websiteSize.Height * 1.5f);
				g.DrawString(disclaimerText, Font, textBrush, disclaimerPos);

				//Donation statement
				string donationText = S._("Please help us to continue developing Eraser - donate some coffee!");
				PointF donationPos = new PointF(disclaimerPos.X, disclaimerPos.Y + 170);
				SizeF donationSize = g.MeasureString(donationText, Font);
				g.DrawString(donationText, Font, textBrush, donationPos);
				DonateRect = new Rectangle((int)donationPos.X, (int)donationPos.Y,
					(int)donationSize.Width, (int)donationSize.Height);
			}

			//Calculate the position of the About bitmap
			AboutBitmapPos = new Point((ClientSize.Width - AboutBitmap.Width) / 2,
				(ClientSize.Height - AboutBitmap.Height) / 2);
			WebsiteRect.X += AboutBitmapPos.X;
			WebsiteRect.Y += AboutBitmapPos.Y;
			DonateRect.X += AboutBitmapPos.X;
			DonateRect.Y += AboutBitmapPos.Y;

			//And calculate the bounds of the About Text.
			AboutTextRect = new Rectangle(AboutBitmapPos.X + 19 + 149, AboutBitmapPos.Y + 20 + 147,
				AboutBitmap.Width - 19 - 149 - 20, 130);

			//Create the About Text laid out on screen.
			SizeF aboutTextSize = SizeF.Empty;
			using (Bitmap b = new Bitmap(1, 1))
			using (Graphics g = Graphics.FromImage(b))
				aboutTextSize = g.MeasureString(AboutText, Font, AboutTextRect.Width);
			AboutTextBitmap = new Bitmap(AboutTextRect.Width, (int)aboutTextSize.Height);
			using (Graphics g = Graphics.FromImage(AboutTextBitmap))
			{
				g.Clear(Color.FromArgb(0, 0, 0, 0));
				g.DrawString(AboutText, Font, new SolidBrush(Color.White), new RectangleF(
					0.0f, 0.0f, AboutTextRect.Width, aboutTextSize.Height));
			}

			//Get the parent dialog's screen buffer.
			ParentBitmap = new Bitmap(parent.ClientSize.Width, parent.ClientSize.Height);
			using (Graphics dest = Graphics.FromImage(ParentBitmap))
			{
				parent.Refresh();
				Point parentPos = parent.PointToScreen(new Point(0, 0));
				dest.CopyFromScreen(parentPos, new Point(0, 0), parent.ClientSize);
			}

			AboutTextScrollTop = AboutTextRect.Height / 2;
			animationTimer_Tick(null, null);
		}

		private void AboutForm_Click(object sender, EventArgs e)
		{
			Point cursorPos = PointToClient(Cursor.Position);
			try
			{
				if (WebsiteRect.IntersectsWith(new Rectangle(cursorPos, new Size(1, 1))))
					Process.Start("http://eraser.heidi.ie/");
				else if (DonateRect.IntersectsWith(new Rectangle(cursorPos, new Size(1, 1))))
					Process.Start("https://euro.swreg.org/cgi-bin/s.cgi?r=1&s=80181&db_key=1512312&x=0&lang=&lnk=");
				else if ((DateTime.Now - mouseDownTime < mouseSpeedUpSpan))
					//Dismiss the dialog.
					Close();
			}
			catch (Win32Exception ex)
			{
				//We've got an error executing the the browser to pass the links: show an error
				//to the user.
				MessageBox.Show(S._("Could not open the required web page. The error returned " +
					"was: {0}", ex.Message), S._("Eraser"), MessageBoxButtons.OK,
					MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
					S.IsRightToLeft(null) ? MessageBoxOptions.RtlReading : 0);
			}
		}

		private void AboutForm_Paint(object sender, PaintEventArgs e)
		{
			DrawComposite(e.Graphics);
		}

		private void AboutForm_MouseMove(object sender, MouseEventArgs e)
		{
			Cursor.Current = Cursors.Default;
			if (WebsiteRect.IntersectsWith(new Rectangle(e.Location, new Size(1, 1))) ||
				DonateRect.IntersectsWith(new Rectangle(e.Location, new Size(1, 1))))
				Cursor.Current = Cursors.Hand;
		}

		private void AboutForm_MouseLeave(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.Default;
		}

		private void animationTimer_Tick(object sender, EventArgs e)
		{
			if (ParentOpacity <= 128)
				ParentOpacity += 8;

			if (AboutTextBitmap.Height < -AboutTextScrollTop)
				AboutTextScrollTop = AboutTextRect.Height;
			else if (AboutTextBitmap.Height < AboutTextScrollTop)
				AboutTextScrollTop = -AboutTextRect.Height;
			else
			{
				if (mouseSpeed == 0.0)
				{
					AboutTextScrollTop -= 1;
				}
				else
				{
					int speed = (mouseBotton == MouseButtons.Left ? -1 : +1);
					speed *= (int)mouseSpeed;

					AboutTextScrollTop += speed;

					// clamp so it doesnt go out of hand
					mouseSpeed = Math.Min(8.0, mouseSpeed + 0.1);
				}
			}

			using (Graphics g = CreateGraphics())
				DrawComposite(g);
		}

		private void DrawComposite(Graphics g)
		{
			if (DoubleBufferBitmap == null)
				DoubleBufferBitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
			using (Graphics bg = Graphics.FromImage(DoubleBufferBitmap))
			{
				//Draw the parent image with a fading out effect
				if (ParentOpacity > 128)
					bg.Clip = new Region(AboutTextRect);

				Brush brush = new SolidBrush(Color.FromArgb(ParentOpacity, 0, 0, 0));
				bg.DrawImageUnscaled(ParentBitmap, 0, 0);
				bg.FillRectangle(brush, ClientRectangle);

				//Then draw the About bitmap (which we cached in the constructor)
				bg.DrawImageUnscaled(AboutBitmap, AboutBitmapPos);

				//And the scrolling text
				bg.Clip = new Region(AboutTextRect);
				bg.DrawImageUnscaled(AboutTextBitmap, AboutTextRect.Left,
					AboutTextRect.Top + AboutTextScrollTop);
				bg.ResetClip();
			}

			if (ParentOpacity > 128)
				if (g.Clip != null)
					g.Clip.Complement(new Region(AboutTextRect));
				else
					g.Clip = new Region(AboutTextRect);
			g.DrawImageUnscaled(DoubleBufferBitmap, 0, 0);
		}

		private double mouseSpeed = 0.0;
		private DateTime mouseDownTime = DateTime.MinValue;
		private TimeSpan mouseSpeedUpSpan = new TimeSpan(0, 0, 0, 0, 230);
		private MouseButtons mouseBotton;
		private void AboutForm_MouseDown(object sender, MouseEventArgs e)
		{
			mouseSpeed = 2.0;
			mouseBotton = e.Button;
			animationTimer.Interval = 20;
			mouseDownTime = DateTime.Now;
		}

		private void AboutForm_MouseUp(object sender, MouseEventArgs e)
		{
			mouseSpeed = 0.0;
			animationTimer.Interval = 50;
		}

	}
}