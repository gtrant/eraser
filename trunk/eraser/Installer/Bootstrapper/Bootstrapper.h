/* 
 * $Id$
 * Copyright 2008-2014 The Eraser Project
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

#pragma once

#include <windows.h>
#include <string>
#include <map>

#include "resource.h"
#undef Yield
#undef min

class MainWindow
{
public:
	/// Constructor.
	MainWindow()
	{
		hWnd = hWndStatusLbl = hWndProgressBar = hWndCancelBtn = NULL;
	}

	/// Destructor.
	~MainWindow();

	/// Creates the Window.
	bool Create();

	/// Shows or hides the window.
	void Show(bool show);

	/// Enables or disables the cancel button.
	void EnableCancellation(bool enable);

	/// Sets the progress of the current operation.
	/// 
	/// \param[in] progress The percentage of the operation complete. This is a real
	///                     number from 0 to 1.
	void SetProgress(float progress);

	/// Sets the progress bar to an indeterminate state.
	void SetProgressIndeterminate();

	/// Sets the dialog label to display a short message.
	void SetMessage(std::wstring message);

	/// Gets the raw window handle.
	HWND GetHandle()
	{
		return hWnd;
	}

private:
	HWND hWnd;
	HWND hWndPanel;
	HWND hWndStatusLbl;
	HWND hWndProgressBar;
	HWND hWndCancelBtn;

	struct WndProcData
	{
		WndProcData(WNDPROC oldWndProc, MainWindow& owner)
			: OldWndProc(oldWndProc), Owner(&owner)
		{
		}

		WNDPROC OldWndProc;
		MainWindow* Owner;
	};

	static std::map<HWND, WndProcData> OldWndProcs;

private:
	/// Registers the main window class and creates it.
	bool InitInstance();

	/// Helper function to set the window font for created windows to the system default.
	/// 
	/// \param[in] hWnd The window whose font needs to be set.
	void SetWindowFont(HWND hWnd);

	/// Handles messages from user interactions.
	LRESULT WindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam,
		bool& handled);

	/// Subclasses the given window.
	/// 
	/// \param[in] owner The owner of the window.
	/// \param[in] hWnd The handle to the window to subclass.
	/// \param[in] wndProc The window message processor.
	static void SubclassWindow(MainWindow& owner, HWND hWnd, WNDPROC wndProc);

	/// Processes messages for the main window.
	static LRESULT __stdcall WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);
};

class Application
{
public:
	/// Gets the Singleton instance of the Application object.
	static Application& Get();

	/// Gets the HINSTANCE variable for the current
	HINSTANCE GetInstance();

	/// Retrieves the path to the executable file.
	std::wstring GetPath();

	/// Processes messages in the message queue.
	void Yield();

	/// Retrieves the MainWindow object representing the Application's top window.
	MainWindow& GetTopWindow();

private:
	/// Constructor.
	Application();

private:
	/// The Application main window.
	MainWindow MainWin;
};

/// Formats the system error code using FormatMessage, returning the message as
/// a std::wstring.
std::wstring GetErrorMessage(DWORD lastError);

/// Integrates the distribution package to the installer.
int Integrate(const std::wstring& destItem, const std::wstring& package);

/// Extracts the setup files to the users' temporary folder.
/// 
/// \param[in] pathToExtract The path to extract the temporary files to.
void ExtractTempFiles(std::wstring pathToExtract);

/// Checks for the presence of the .NET Framework on the client computer.
bool HasNetFramework();

/// Extracts the included .NET framework installer and runs it.
/// 
/// \param[in] srcDir The path to the directory holding dotnetfx.exe.
/// \return True if the .NET framework was successfully installed.
bool InstallNetFramework(std::wstring srcDir, bool quiet);

/// Installs the version of Eraser suited for the current operating platform.
///
/// \param[in] srcDir The path to the directory holding the installers.
/// \return True if Eraser was successfully installed.
bool InstallEraser(std::wstring srcDir, bool quiet);
