/* 
 * $Id$
 * Copyright 2008-2013 The Eraser Project
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

using System.Windows.Forms;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// Provides an abstract interface to allow plugins to provide notifications
	/// which will be shown by Eraser.
	/// </summary>
	public interface INotifier : IRegisterable
	{
		/// <summary>
		/// Called by hosts to set the notification sink to call when a
		/// notification should be shown. This parameter may be null when
		/// the plugin is being unregistered or before the plugin has a host.
		/// </summary>
		INotificationSink Sink
		{
			set;
		}

		/// <summary>
		/// Occurs when the balloon tip is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Clicked(object sender, EventArgs e);

		/// <summary>
		/// Occurs when the balloon tip is closed or times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Closed(object sender, EventArgs e);

		/// <summary>
		/// Occurs when the balloon tip is displayed on the screen.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Shown(object sender, EventArgs e);
	}

	/// <summary>
	/// A Notification Sink object which will be passed to all INotifier plugin
	/// classes. Call the <see cref="INotificationSink.ShowNotification"/> function
	/// to display a notification.
	/// </summary>
	public interface INotificationSink
	{
		/// <summary>
		/// Queues a notification for display.
		/// </summary>
		/// <param name="source">The source of the notification. This object
		/// will receive the Clicked, Closed and Shown events.</param>
		/// <param name="timeout">The time period, in milliseconds, the balloon
		/// tip should display</param>
		/// <param name="icon">The icon to display on the balloon.</param>
		/// <param name="title">The title of the balloon.</param>
		/// <param name="message">The message to display on the balloon.</param>
		/// <remarks>The notification may not be shown immediately. Override the
		/// <see cref="INotifier.Shown"/> function to be notified when the notification
		/// is shown.</remarks>
		void ShowNotification(INotifier source, int timeout, ToolTipIcon icon, string title,
			string message);
	}
}
