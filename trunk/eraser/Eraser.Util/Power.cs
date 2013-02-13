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
using System.Text;

namespace Eraser.Util
{
	public static class Power
	{
		/// <summary>
		/// Enables an application to inform the system that it is in use, thereby
		/// preventing the system from entering sleep or turning off the display
		/// while the application is running.
		/// </summary>
		/// <remarks>
		/// The system automatically detects activities such as local keyboard
		/// or mouse input, server activity, and changing window focus. Activities
		/// that are not automatically detected include disk or CPU activity and
		/// video display.
		/// 
		/// Calling SetThreadExecutionState without ES_CONTINUOUS simply resets
		/// the idle timer; to keep the display or system in the working state,
		/// the thread must call SetThreadExecutionState periodically.
		/// 
		/// To run properly on a power-managed computer, applications such as fax
		/// servers, answering machines, backup agents, and network management
		/// applications must use both ES_SYSTEM_REQUIRED and ES_CONTINUOUS when
		/// they process events. Multimedia applications, such as video players
		/// and presentation applications, must use ES_DISPLAY_REQUIRED when they
		/// display video for long periods of time without user input. Applications
		/// such as word processors, spreadsheets, browsers, and games do not need
		/// to call SetThreadExecutionState.
		/// 
		/// The ES_AWAYMODE_REQUIRED value should be used only when absolutely
		/// necessary by media applications that require the system to perform
		/// background tasks such as recording television content or streaming media
		/// to other devices while the system appears to be sleeping. Applications
		/// that do not require critical background processing or that run on
		/// portable computers should not enable away mode because it prevents
		/// the system from conserving power by entering true sleep.
		/// 
		/// To enable away mode, an application uses both ES_AWAYMODE_REQUIRED and
		/// ES_CONTINUOUS; to disable away mode, an application calls
		/// SetThreadExecutionState with ES_CONTINUOUS and clears
		/// ES_AWAYMODE_REQUIRED. When away mode is enabled, any operation that
		/// would put the computer to sleep puts it in away mode instead. The computer
		/// appears to be sleeping while the system continues to perform tasks that
		/// do not require user input. Away mode does not affect the sleep idle
		/// timer; to prevent the system from entering sleep when the timer expires,
		/// an application must also set the ES_SYSTEM_REQUIRED value.
		/// 
		/// The SetThreadExecutionState function cannot be used to prevent the user
		/// from putting the computer to sleep. Applications should respect that
		/// the user expects a certain behavior when they close the lid on their
		/// laptop or press the power button.
		/// 
		/// This function does not stop the screen saver from executing. 
		/// </remarks>
		public static ExecutionState ExecutionState
		{
			set
			{
				NativeMethods.SetThreadExecutionState((NativeMethods.EXECUTION_STATE)value);
			}
		}
	}

	public enum ExecutionState
	{
		/// <summary>
		/// No specific state
		/// </summary>
		None = 0,

		/// <summary>
		/// Enables away mode. This value must be specified with ES_CONTINUOUS.
		/// 
		/// Away mode should be used only by media-recording and media-distribution
		/// applications that must perform critical background processing on
		/// desktop computers while the computer appears to be sleeping.
		/// See remarks.
		/// 
		/// Windows Server 2003 and Windows XP/2000: ES_AWAYMODE_REQUIRED is
		/// not supported.
		/// </summary>
		AwayModeRequired = (int)NativeMethods.EXECUTION_STATE.ES_AWAYMODE_REQUIRED,

		/// <summary>
		/// Informs the system that the state being set should remain in effect
		/// until the next call that uses ES_CONTINUOUS and one of the other
		/// state flags is cleared.
		/// </summary>
		Continuous = unchecked((int)NativeMethods.EXECUTION_STATE.ES_CONTINUOUS),

		/// <summary>
		/// Forces the display to be on by resetting the display idle timer.
		/// </summary>
		DisplayRequired = (int)NativeMethods.EXECUTION_STATE.ES_DISPLAY_REQUIRED,

		/// <summary>
		/// Forces the system to be in the working state by resetting the system
		/// idle timer.
		/// </summary>
		SystemRequired = (int)NativeMethods.EXECUTION_STATE.ES_SYSTEM_REQUIRED,

		/// <summary>
		/// This value is not supported. If ES_USER_PRESENT is combined with
		/// other esFlags values, the call will fail and none of the specified
		/// states will be set.
		/// 
		/// Windows Server 2003 and Windows XP/2000: Informs the system that a
		/// user is present and resets the display and system idle timers.
		/// ES_USER_PRESENT must be called with ES_CONTINUOUS.
		/// </summary>
		UserPresent = (int)NativeMethods.EXECUTION_STATE.ES_USER_PRESENT
	}
}
