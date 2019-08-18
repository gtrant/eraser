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
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Eraser.Plugins
{
	public interface ITask : ISerializable, IXmlSerializable
	{
		/// <summary>
		/// Cancels the task from running, or, if the task is queued for running,
		/// removes the task from the queue.
		/// </summary>
		void Cancel();

		/// <summary>
		/// The name for this task. This is just an opaque value for the user to
		/// recognize the task.
		/// </summary>
		string Name
		{
			get;
			set;
		}

		/// <summary>
		/// The name of the task, used for display in UI elements.
		/// </summary>
		string ToString();

		/// <summary>
		/// Gets the status of the task - whether it is being executed.
		/// </summary>
		bool Executing
		{
			get;
		}

		/// <summary>
		/// Gets whether this task is currently queued to run. This is true only
		/// if the queue it is in is an explicit request, i.e will run when the
		/// executor is idle.
		/// </summary>
		bool Queued
		{
			get;
		}

		/// <summary>
		/// Gets whether the task has been cancelled from execution.
		/// </summary>
		bool Canceled
		{
			get;
		}

		/// <summary>
		/// The set of data to erase when this task is executed.
		/// </summary>
		ICollection<ExtensionPoints.IErasureTarget> Targets
		{
			get;
		}

		/// <summary>
		/// The progress manager object which manages the progress of this task.
		/// </summary>
		SteppedProgressManager Progress
		{
			get;
		}
	}
}
