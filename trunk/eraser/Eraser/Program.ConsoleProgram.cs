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
using System.IO;

using ComLib.Arguments;

using Eraser.Manager;
using Eraser.Util;

namespace Eraser
{
	internal static partial class Program
	{
		/// <summary>
		/// Manages and runs a console program. This allows the program to switch
		/// between console and GUI subsystems at runtime. This class will manage
		/// the creation of a console window and destruction of the window upon
		/// exit of the program.
		/// </summary>
		class ConsoleProgram : IDisposable
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="commandLine">The raw command line arguments passed to the program.</param>
			public ConsoleProgram(string[] commandLine)
			{
				CommandLine = commandLine;
				Handlers = new Dictionary<string, ConsoleActionData>();

				//Try to parse the command line for a Quiet argument.
				Arguments = new ConsoleArguments();
				Args.Parse(commandLine, CommandLinePrefixes, CommandLineSeparators, Arguments);

				//Create the console window if we don't have the quiet argument.
				if (!Arguments.Quiet)
					ConsoleWindow = new ConsoleWindow();
			}

			#region IDisposable Members
			~ConsoleProgram()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (ConsoleWindow == null)
					return;

				//Flush the buffered output to the console
				Console.Out.Flush();

				//Don't ask for a key to press if the user specified Quiet.
				if (!Arguments.Quiet)
				{
					Console.Write("\nPress any key to continue . . . ");
					Console.Out.Flush();
					Console.ReadKey(true);

					if (disposing)
						ConsoleWindow.Dispose();
				}

				ConsoleWindow = null;
			}
			#endregion

			/// <summary>
			/// Runs the program, analogous to System.Windows.Forms.Application.Run.
			/// </summary>
			public void Run()
			{
				//Check that we've got an action corresponding to one the user requested.
				if (!Handlers.ContainsKey(Arguments.Action))
					throw new ArgumentException(S._("Unknown action {0}", Arguments.Action));

				//Re-parse the command line arguments as arguments for the given action.
				ConsoleActionData data = Handlers[Arguments.Action];
				ConsoleArguments arguments = data.Arguments;
				ComLib.BoolMessageItem<Args> parseResult = Args.Parse(CommandLine,
					CommandLinePrefixes, CommandLineSeparators, arguments);
				if (!parseResult.Success)
					throw new ArgumentException(parseResult.Message);

				//Remove the action from the positional arguments before sending it to the handler
				System.Diagnostics.Debug.Assert(Arguments.Action == parseResult.Item.Positional[0]);
				parseResult.Item.Positional.RemoveAt(0);
				arguments.PositionalArguments = parseResult.Item.Positional;

				//Then invoke the handler for this action.
				data.Handler(arguments);
			}

			/// <summary>
			/// The prototype of an action handler in the class which executes an
			/// action as specified in the command line.
			/// </summary>
			/// <param name="handler">The <see cref="Program.ConsoleHandler"/> instance
			/// that contains the parsed command line arguments.</param>
			public delegate void ActionHandler(ConsoleArguments handler);

			/// <summary>
			/// Matches an action to a handler.
			/// </summary>
			public Dictionary<string, ConsoleActionData> Handlers { get; private set; }

			/// <summary>
			/// The Console Window created by this object.
			/// </summary>
			private ConsoleWindow ConsoleWindow;

			/// <summary>
			/// The command line arguments the program was started with.
			/// </summary>
			private string[] CommandLine;

			/// <summary>
			/// Stores the common Console arguments which were given on the command line.
			/// </summary>
			private ConsoleArguments Arguments;
		}

		/// <summary>
		/// Stores information about an Action invoked from the command line.
		/// </summary>
		class ConsoleActionData
		{
			public ConsoleActionData(ConsoleProgram.ActionHandler handler,
				ConsoleArguments arguments)
			{
				Handler = handler;
				Arguments = arguments;
			}

			/// <summary>
			/// The Handler for this action.
			/// </summary>
			public ConsoleProgram.ActionHandler Handler { get; private set; }

			/// <summary>
			/// The <see cref="ConsoleArguments"/> object receiving arguments from the command line.
			/// </summary>
			public ConsoleArguments Arguments { get; private set; }
		}
	}
}
