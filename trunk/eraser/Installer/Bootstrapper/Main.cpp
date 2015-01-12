/* 
 * $Id$
 * Copyright 2008-2015 The Eraser Project
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

#include "stdafx.h"
#include "Bootstrapper.h"

//Common Controls Version 6
#pragma comment(linker, "\"/manifestdependency:type='Win32' " \
    "name='Microsoft.Windows.Common-Controls' version='6.0.0.0' "\
    "processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' "\
    "language='*'\"")

namespace {
	//Constants
	const wchar_t* STATIC_CLASS = L"STATIC";
	const wchar_t* BUTTON_CLASS = L"BUTTON";
	const wchar_t* szWindowClass = L"EraserBootstrapper";

	//Static variables
	HINSTANCE hInstance = NULL;
	LRESULT __stdcall WndProc(HWND, UINT, WPARAM, LPARAM);

	/// Creates a temporary directory with the given name. The directory and files in it
	/// are deleted when this object is destroyed.
	class TempDir
	{
	public:
		/// Constructor.
		///
		/// \param[in] dirName The path to the directory. This directory will be created.
		TempDir(std::wstring dirName)
			: DirName(dirName)
		{
			//Ensure there is a trailing slash
			if (std::wstring(L"\\/").find(dirName[dirName.length() - 1]) == std::wstring::npos)
				dirName += L"\\";

			if (!CreateDirectoryW(dirName.c_str(), NULL))
				switch (GetLastError())
				{
				case ERROR_ALREADY_EXISTS:
					DeleteContents();
					break;

				default:
					throw GetErrorMessage(GetLastError());
				}
		}

		~TempDir()
		{
			DeleteContents();
			RemoveDirectoryW(DirName.c_str());
		}

	private:
		/// The path to the directory.
		std::wstring DirName;

	private:
		void DeleteContents()
		{
			//Clean up the files in the directory.
			WIN32_FIND_DATAW findData;
			ZeroMemory(&findData, sizeof(findData));
			HANDLE findHandle = FindFirstFileW((DirName + L"*").c_str(), &findData);
			if (findHandle == INVALID_HANDLE_VALUE)
				throw GetErrorMessage(GetLastError());

			//Delete!
			do
				DeleteFileW((DirName + findData.cFileName).c_str());
			while (FindNextFileW(findHandle, &findData));

			//Clean up.
			FindClose(findHandle);
		}
	};
}

int Install(bool quiet);
int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/,
                       LPTSTR lpCmdLine, int /*nCmdShow*/)
{
	try
	{
		//Parse the command line.
		int argc = 0;
		LPWSTR* argv = CommandLineToArgvW(lpCmdLine, & argc);
		if (argv == NULL)
			throw GetErrorMessage(GetLastError());

		bool quiet = false;
		for (int i = 0; i != argc; ++i)
		{
			std::wstring arg(argv[i]);
			if (arg == L"--integrate")
			{
				//OK, integrate ourselves.
				std::wstring destItem, package;
				if (++i != argc)
					package = argv[i];

				for (++i; i < argc; ++i)
				{
					arg = argv[i];
					if (arg.substr(0, 9) == L"--out")
					{
						if (++i != argc)
							destItem = argv[i];
					}
					else if (arg.substr(0, 2) == L"-q" || arg.substr(0, 7) == L"--quiet")
					{
						quiet = true;
					}
				}

				if (!destItem.empty() && !package.empty())
					return Integrate(destItem, package);
			}
			else if (arg.substr(0, 2) == L"-q" || arg.substr(0, 7) == L"--quiet")
			{
				quiet = true;
			}
		}

		//Create the parent window and the child controls
		::hInstance = hInstance;
		return Install(quiet);

	}
	catch (const std::wstring& e)
	{
		MessageBoxW(Application::Get().GetTopWindow().GetHandle(), e.c_str(),
			L"Eraser Setup", MB_OK | MB_ICONERROR);
	}

	return 0;
}

int Install(bool quiet)
{
	Application& app = Application::Get();
	MainWindow& mainWin = app.GetTopWindow();
	mainWin.Show(!quiet);

	//OK, now we do the hard work. Create a folder to place our payload into
	wchar_t tempPath[MAX_PATH];
	DWORD result = GetTempPathW(sizeof(tempPath) / sizeof(tempPath[0]), tempPath);
	if (!result)
		throw GetErrorMessage(GetLastError());

	std::wstring tempDir(tempPath, result);
	if (std::wstring(L"\\/").find(tempDir[tempDir.length() - 1]) == std::wstring::npos)
		tempDir += L"\\";
	tempDir += L"eraserInstallBootstrapper\\";
	TempDir dir(tempDir);
	ExtractTempFiles(tempDir);
	mainWin.EnableCancellation(false);

	//Install the .NET framework
	if (!HasNetFramework())
		if (!InstallNetFramework(tempDir, quiet))
			return 0;

	//Then install Eraser!
	mainWin.Show(false);
	InstallEraser(tempDir, quiet);
	return 0;
}

Application::Application()
{
	MainWin.Create();
}

Application& Application::Get()
{
	static Application Instance;
	return Instance;
}

HINSTANCE Application::GetInstance()
{
	return ::hInstance;
}

MainWindow& Application::GetTopWindow()
{
	return MainWin;
}

void Application::Yield()
{
	MSG msg;
	while (PeekMessage(&msg, (HWND)0, 0, 0, PM_NOREMOVE) && msg.message != WM_QUIT)
	{
		if (GetMessageW(&msg, NULL, 0, 0) == 0)
			return;

		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
}

std::wstring Application::GetPath()
{
	wchar_t filePath[MAX_PATH];
	DWORD result = GetModuleFileNameW(hInstance, filePath,
		sizeof(filePath) / sizeof(filePath[0]));

	if (result == 0)
		throw GetErrorMessage(GetLastError());
	return std::wstring(filePath, result);
}

std::map<HWND, MainWindow::WndProcData> MainWindow::OldWndProcs;
MainWindow::~MainWindow()
{
	DestroyWindow(hWndStatusLbl);
	DestroyWindow(hWndProgressBar);
	DestroyWindow(hWndCancelBtn);
	DestroyWindow(hWndPanel);
	DestroyWindow(hWnd);
}

bool MainWindow::Create()
{
	if (!InitInstance())
		return false;

	hWndPanel = CreateWindowExW(0, STATIC_CLASS, NULL, WS_CHILD | WS_VISIBLE,
		0, 0, 294, 104, hWnd, NULL, hInstance, NULL);
	hWndStatusLbl = CreateWindowExW(0, STATIC_CLASS, L"Extracting setup files...",
		WS_CHILD | WS_VISIBLE, 13, 38, 270, 19, hWndPanel, NULL, hInstance, NULL);
	hWndProgressBar = CreateWindowExW(0, PROGRESS_CLASS, NULL,
		WS_CHILD | WS_VISIBLE | PBS_SMOOTH, 13, 13, 270, 24, hWndPanel, NULL,
		hInstance, NULL);
	hWndCancelBtn = CreateWindowExW(0, BUTTON_CLASS, L"Cancel", WS_TABSTOP |
		WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON, 193, 65, 90, 23, hWndPanel, NULL,
		hInstance, NULL);
	if (!hWndPanel || !hWndStatusLbl || !hWndProgressBar || !hWndCancelBtn)
		return false;

	SetWindowFont(hWndPanel);
	SetWindowFont(hWndStatusLbl);
	SetWindowFont(hWndProgressBar);
	SetWindowFont(hWndCancelBtn);

	SendMessage(hWndProgressBar, PBM_SETRANGE32, 0, 1000);
	SubclassWindow(*this, hWndCancelBtn, WndProc);
	SubclassWindow(*this, hWndPanel, WndProc);
	return true;
}

bool MainWindow::InitInstance()
{
	WNDCLASSEX wcex;
	::ZeroMemory(&wcex, sizeof(wcex));

	wcex.cbSize         = sizeof(WNDCLASSEX);
	wcex.style			= CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc	= WndProc;
	wcex.cbClsExtra		= 0;
	wcex.cbWndExtra		= 0;
	wcex.hInstance		= hInstance;
	wcex.hIcon			= LoadIcon(hInstance, MAKEINTRESOURCE(BOOTSTRAPPER_ICON));
	wcex.hCursor		= LoadCursor(NULL, IDC_ARROW);
	wcex.hbrBackground	= (HBRUSH)(COLOR_WINDOW + 1);
	wcex.lpszClassName	= szWindowClass;
	wcex.hIconSm		= LoadIcon(wcex.hInstance, MAKEINTRESOURCE(BOOTSTRAPPER_ICON));
	RegisterClassExW(&wcex);
	InitCommonControls();

	//Create the window
	hWnd = CreateWindowW(szWindowClass, L"Eraser Setup", WS_CAPTION,
		CW_USEDEFAULT, 0, 300, 130, NULL, NULL, hInstance, NULL);

	if (!hWnd)
		return false;

	//Set default settings (font)
	SetWindowFont(hWnd);
	return true;
}

void MainWindow::SetWindowFont(HWND hWnd)
{
	HFONT hWndFont = NULL;
	if (!hWndFont)
	{
		NONCLIENTMETRICS ncm;
		::ZeroMemory(&ncm, sizeof(ncm));
		ncm.cbSize = sizeof(ncm);

		if ( !::SystemParametersInfo(SPI_GETNONCLIENTMETRICS, 0, &ncm, 0) )
		{
#if WINVER >= 0x0600
			// a new field has been added to NONCLIENTMETRICS under Vista, so
			// the call to SystemParametersInfo() fails if we use the struct
			// size incorporating this new value on an older system -- retry
			// without it
			ncm.cbSize -= sizeof(int);
			if ( !::SystemParametersInfo(SPI_GETNONCLIENTMETRICS, 0, &ncm, 0) )
#endif
				return;
		}

		hWndFont = CreateFontIndirectW(&ncm.lfMessageFont);
	}

	SendMessage(hWnd, WM_SETFONT, (WPARAM)hWndFont, MAKELPARAM(TRUE, 0));
}

void MainWindow::Show(bool show)
{
	ShowWindow(hWnd, show ? SW_SHOW : SW_HIDE);

	if (show)
	{
		InvalidateRect(hWnd, NULL, true);
		UpdateWindow(hWnd);
		Application::Get().Yield();
	}
}

void MainWindow::EnableCancellation(bool enable)
{
	EnableWindow(hWndCancelBtn, enable);
	Application::Get().Yield();
}

void MainWindow::SetProgress(float progress)
{
	LONG_PTR pbStyle = GetWindowLongPtr(hWndProgressBar, GWL_STYLE);
	if (pbStyle & PBS_MARQUEE)
		SetWindowLongPtr(hWndProgressBar, GWL_STYLE, pbStyle & (~PBS_MARQUEE));
	SendMessage(hWndProgressBar, PBM_SETPOS, (int)(progress * 1000), 0);
	Application::Get().Yield();
}

void MainWindow::SetProgressIndeterminate()
{
	SetWindowLongPtr(hWndProgressBar, GWL_STYLE,
		GetWindowLongPtr(hWndProgressBar, GWL_STYLE) | PBS_MARQUEE);
	SendMessage(hWndProgressBar, PBM_SETMARQUEE, true, 100);
	Application::Get().Yield();
}

void MainWindow::SetMessage(std::wstring message)
{
	SetWindowTextW(hWndStatusLbl, message.c_str());
	Application::Get().Yield();
}

LRESULT MainWindow::WindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM /*lParam*/,
	bool& handled)
{
	handled = false;
	switch (message)
	{
	case WM_COMMAND:
		if (hWnd == hWndCancelBtn && HIWORD(wParam) == BN_CLICKED)
			PostQuitMessage(1);
		break;
	}

	return 0;
}

void MainWindow::SubclassWindow(MainWindow& owner, HWND hWnd, WNDPROC wndProc)
{
	OldWndProcs.insert(std::make_pair(
		hWnd, WndProcData(reinterpret_cast<WNDPROC>(SetWindowLongPtr(hWnd,
			GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(wndProc))), owner)
	));
}

LRESULT __stdcall MainWindow::WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	std::map<HWND, WndProcData>::const_iterator iter = OldWndProcs.find(hWnd);
	if (iter != OldWndProcs.end())
	{
		bool handled = false;
		LRESULT result = iter->second.Owner->WindowProc(hWnd, message, wParam,
			lParam, handled);
		if (handled)
			return result;

		return CallWindowProc(iter->second.OldWndProc, hWnd, message, wParam, lParam);
	}

	switch (message)
	{
	case WM_DESTROY:
		PostQuitMessage(0);
		break;

	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}

	return 0;
}

std::wstring GetErrorMessage(DWORD lastError)
{
	unsigned lastBufferSize = 128;
	wchar_t* buffer = NULL;
	DWORD result = 0;

	while (result == 0 || result == lastBufferSize - 1)
	{
		delete[] buffer;
		buffer = new wchar_t[lastBufferSize *= 2];
		result = FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM, NULL, lastError, 0, buffer,
			lastBufferSize, NULL);
	}

	std::wstring message(buffer);
	delete[] buffer;
	return message;
}
