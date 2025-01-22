/* 
 * $Id: Bootstrapper.cpp 2993 2021-09-25 17:23:27Z gtrant $
 * Copyright 2008-2021 The Eraser Project
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
#include "Handle.h"

const wchar_t *ResourceName = MAKEINTRESOURCE(101);

int Integrate(const std::wstring& destItem, const std::wstring& package)
{
	//Open a handle to ourselves
	DWORD lastOperation = 0;
	{
		Handle<HANDLE> srcFile(CreateFileW(Application::Get().GetPath().c_str(), GENERIC_READ,
			FILE_SHARE_READ, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
		if (srcFile == INVALID_HANDLE_VALUE)
			throw GetErrorMessage(GetLastError());

		//Copy ourselves
		Handle<HANDLE> destFile(CreateFileW(destItem.c_str(), GENERIC_WRITE, 0, NULL,
			CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
		if (destFile == INVALID_HANDLE_VALUE)
			throw GetErrorMessage(GetLastError());

		char buffer[262144];
		while (ReadFile(srcFile, buffer, sizeof(buffer), &lastOperation, NULL) && lastOperation)
			WriteFile(destFile, buffer, lastOperation, &lastOperation, NULL);
	}

	//Start updating the resource in the destination item
	HANDLE resHandle(BeginUpdateResource(destItem.c_str(), false));
	if (resHandle == NULL)
		throw GetErrorMessage(GetLastError());

	//Read the package into memory
	Handle<HANDLE> packageFile(CreateFileW(package.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
	if (packageFile == INVALID_HANDLE_VALUE)
		throw GetErrorMessage(GetLastError());

	unsigned long packageSize = GetFileSize(packageFile, NULL);
	char* inputData = new char[packageSize];
	if (!ReadFile(packageFile, inputData, packageSize, &lastOperation, NULL) || lastOperation != packageSize)
		throw GetErrorMessage(GetLastError());

	//Add the package to the application resource section
	if (!UpdateResource(resHandle, RT_RCDATA, ResourceName, MAKELANGID(LANG_NEUTRAL,
		SUBLANG_DEFAULT), inputData, packageSize))
	{
		throw GetErrorMessage(GetLastError());
	}

	//Complete the update
	if (!EndUpdateResource(resHandle, false)) 
		throw GetErrorMessage(GetLastError());
	return 0;
}

/// ISzInStream interface for extracting the archives.
struct LZMemStream
{
public:
	/// Constructor.
	/// 
	/// \param[in] buffer The buffer containing the data to present as a stream.
	/// \param[in] bufferSize The size of the buffer passed in.
	/// \param[in] deleteOnDestroy True if the the buffer should be freed (using delete[])
	///                            after the stream is destroyed.
	LZMemStream(void* buffer, size_t bufferSize, bool deleteOnDestroy)
	{
		InStream.Look = LZMemStreamLook;
		InStream.Skip = LzMemStreamSkip;
		InStream.Read = LZMemStreamRead;
		InStream.Seek = LzMemStreamSeek;

		Buffer = static_cast<char*>(buffer);
		BufferRead = 0;
		BufferSize = bufferSize;

		DeleteOnDestroy = deleteOnDestroy;
		CurrentOffset = 0;
	}

	~LZMemStream()
	{
		if (DeleteOnDestroy)
			delete[] Buffer;
	}

	ILookInStream InStream;

private:
	bool DeleteOnDestroy;
	char* Buffer;
	size_t BufferRead;
	size_t BufferSize;
	size_t CurrentOffset;

	static SRes LZMemStreamLook(void* object, const void** buf, size_t* size)
	{
		if (*size == 0)
			return SZ_OK;

		LZMemStream* s = static_cast<LZMemStream*>(object);

		//Copy the memory to the provided buffer.
		*size = std::min(std::min(*size, s->BufferSize - s->CurrentOffset),
			static_cast<size_t>(32768));
		char* dstBuffer = reinterpret_cast<char*>(SzAlloc(object, *size));
		memcpy(dstBuffer, s->Buffer + s->CurrentOffset, *size);

		*buf = dstBuffer;
		s->BufferRead += *size;

		MainWindow& mainWin = Application::Get().GetTopWindow();
		mainWin.SetProgress((float)((double)s->BufferRead / s->BufferSize));
		return SZ_OK;
	}

	static SRes LZMemStreamRead(void* object, void* buf, size_t* size)
	{
		LZMemStream* s = static_cast<LZMemStream*>(object);

		//Copy the memory to the provided buffer.
		*size = std::min(std::min(*size, s->BufferSize - s->CurrentOffset),
			static_cast<size_t>(32768));
		memcpy(buf, s->Buffer + s->CurrentOffset, *size);

		s->CurrentOffset += *size;
		s->BufferRead += *size;

		MainWindow& mainWin = Application::Get().GetTopWindow();
		mainWin.SetProgress((float)((double)s->BufferRead / s->BufferSize));
		return SZ_OK;
	}

	static SRes LzMemStreamSkip(void* object, size_t offset)
	{
		LZMemStream* s = static_cast<LZMemStream*>(object);

		if (offset + s->CurrentOffset > s->BufferSize)
			return SZ_ERROR_INPUT_EOF;
		s->CurrentOffset += offset;
		return SZ_OK;
	}

	static SRes LzMemStreamSeek(void* object, long long* position, ESzSeek origin)
	{
		LZMemStream* s = static_cast<LZMemStream*>(object);
		long long newPos = *position;
		switch (origin)
		{
		case SZ_SEEK_CUR:
			newPos += s->CurrentOffset;
			break;
		case SZ_SEEK_END:
			newPos = s->BufferSize - *position;
			break;
		}

		if (newPos > s->BufferSize || newPos < 0)
			return SZ_ERROR_INPUT_EOF;
		s->CurrentOffset = static_cast<size_t>(newPos);
		*position = newPos;
		return SZ_OK;
	}
};

void ExtractTempFiles(std::wstring pathToExtract)
{
	if (std::wstring(L"\\/").find(pathToExtract[pathToExtract.length() - 1]) == std::wstring::npos)
		pathToExtract += L"\\";

	//Open the file
	HMODULE currProcess = static_cast<HMODULE>(Application::Get().GetInstance());
	HANDLE hResource(FindResource(currProcess, ResourceName, RT_RCDATA));
	if (!hResource)
		throw GetErrorMessage(GetLastError());

	HANDLE hResLoad(LoadResource(currProcess, static_cast<HRSRC>(static_cast<HANDLE>(hResource))));
	if (!hResLoad)
		throw GetErrorMessage(GetLastError());

	//Lock the data into global memory.
	unsigned long resourceSize = SizeofResource(currProcess, static_cast<HRSRC>(
		static_cast<HANDLE>(hResource)));
	void* resourceBuffer = LockResource(hResLoad);
	if (!resourceBuffer)
		throw GetErrorMessage(GetLastError());

	//7z archive database structure
	CSzArEx db;

	//memory functions
	ISzAlloc allocImp;
	ISzAlloc allocTempImp;
	allocTempImp.Alloc = allocImp.Alloc = SzAlloc;
	allocTempImp.Free = allocImp.Free = SzFree;

	//Initialize the CRC and database structures
	LZMemStream stream(resourceBuffer, resourceSize, false);
	CrcGenerateTable();
	SzArEx_Init(&db);
	if (SzArEx_Open(&db, &stream.InStream, &allocImp, &allocTempImp) != SZ_OK)
		throw std::wstring(L"Could not open archive for reading.");

	//Read the database for files
	unsigned blockIndex = 0;
	Byte* outBuffer = NULL;
	size_t outBufferSize = 0;
	for (unsigned i = 0; i < db.db.NumFiles; ++i)
	{
		size_t offset = 0;
		size_t processedSize = 0;
		CSzFileItem* file = db.db.Files + i;
		SRes result = SZ_OK;

		//Create the output file
		wchar_t fileName[MAX_PATH];
		SzArEx_GetFileNameUtf16(&db, i, reinterpret_cast<UInt16*>(fileName));
		
		//Split the path to get the file name only.
		wchar_t baseFileName[MAX_PATH];
		wchar_t fileExt[MAX_PATH];
		_wsplitpath_s(fileName, NULL, NULL, NULL, NULL, baseFileName,
			sizeof(baseFileName) / sizeof(baseFileName[0]), fileExt,
			sizeof(fileExt) / sizeof(fileExt[0]));
		wcscpy_s(fileName, baseFileName);
		wcscpy_s(fileName + wcslen(baseFileName),
			sizeof(fileName) / sizeof(fileName[0]) - wcslen(baseFileName), fileExt);

		Handle<HANDLE> destFile(CreateFileW((pathToExtract + fileName).c_str(), GENERIC_WRITE,
			0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
		if (destFile == INVALID_HANDLE_VALUE)
			throw GetErrorMessage(GetLastError());
		unsigned long long destFileSize = file->Size;

		//Extract the file
		while (result == SZ_OK && destFileSize)
		{
			result = SzArEx_Extract(&db, &stream.InStream, i, &blockIndex,
				&outBuffer, &outBufferSize, &offset, &processedSize, &allocImp,
				&allocTempImp);
			if (result != SZ_OK)
				throw std::wstring(L"Could not decompress data as it is corrupt.");

			DWORD bytesWritten = 0;
			if (!WriteFile(destFile, outBuffer + offset, processedSize, &bytesWritten, NULL) ||
				bytesWritten != processedSize)
				throw GetErrorMessage(GetLastError());
			destFileSize -= bytesWritten;
			Application::Get().Yield();
		}
	}

	SzArEx_Free(&db, &allocImp);
}

bool HasNetFramework()
{
	const std::wstring versionKey(L"v4");

	//Open the key for reading
	Handle<HKEY> key;
	DWORD result = RegOpenKeyEx(HKEY_LOCAL_MACHINE,
		(L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\" + versionKey + L"\\Full").c_str(),
		0, KEY_READ, key);

	//Retry for 64-bit WoW
	if (result == ERROR_FILE_NOT_FOUND)
	{
		result = RegOpenKeyEx(HKEY_LOCAL_MACHINE,
			(L"SOFTWARE\\Wow6432Node\\Microsoft\\NET Framework Setup\\NDP\\" + versionKey + L"\\Full").c_str(),
			0, KEY_READ, key);

		if (result == ERROR_FILE_NOT_FOUND)
			return false;
	}

	if (result != ERROR_SUCCESS)
		throw GetErrorMessage(result);

	//Query the Install string
	wchar_t buffer[32];
	DWORD bufferSize = sizeof(buffer);
	::ZeroMemory(buffer, sizeof(buffer));
	result = RegQueryValueExW(key, L"Install", NULL, NULL, reinterpret_cast<BYTE*>(buffer),
		&bufferSize);
	if (result != ERROR_SUCCESS && result != ERROR_MORE_DATA)
		throw GetErrorMessage(result);
	
	//If we got more data than we wanted or if the value is not zero, it's invalid.
	if (bufferSize != sizeof(DWORD) || *reinterpret_cast<DWORD*>(buffer) == 0)
		return false;

	//Next get the exact version installed
	bufferSize = sizeof(buffer);
	::ZeroMemory(buffer, sizeof(buffer));
	result = RegQueryValueExW(key, L"Version", NULL, NULL, reinterpret_cast<BYTE*>(buffer),
		&bufferSize);
	if ((result != ERROR_SUCCESS && result != ERROR_MORE_DATA) || bufferSize == sizeof(buffer))
		throw GetErrorMessage(result);

	//Ensure that the version string is NULL terminated
	buffer[bufferSize / sizeof(wchar_t)] = L'\0';

	//Split the version into its four components
	int versionComponents[] = { 0, 0, 0, 0 };
	wchar_t* previousDot = buffer - 1;
	wchar_t* nextDot = NULL;
	for (unsigned i = 0; i < sizeof(versionComponents) / sizeof(versionComponents[0]); ++i)
	{
		nextDot = wcschr(previousDot + 1, L'.');
		versionComponents[i] = boost::lexical_cast<int>(
			nextDot ? std::wstring(++previousDot, nextDot) : std::wstring(++previousDot));
		if (!nextDot)
			break;

		previousDot = nextDot;
	}

	return versionComponents[0] == 4 && versionComponents[1] >= 0 && versionComponents[2] >= 30319;
}

int CreateProcessAndWait(const std::wstring& commandLine, const std::wstring& appName)
{
	//Get a mutable version of the command line
	wchar_t* cmdLine = new wchar_t[commandLine.length() + 1];
	wcscpy_s(cmdLine, commandLine.length() + 1, commandLine.c_str());

	//Launch the process
	STARTUPINFOW startupInfo;
	PROCESS_INFORMATION pInfo;
	::ZeroMemory(&startupInfo, sizeof(startupInfo));
	::ZeroMemory(&pInfo, sizeof(pInfo));
	if (!CreateProcessW(NULL, cmdLine, NULL, NULL, false, 0, NULL,  NULL, &startupInfo,
		&pInfo))
	{
		delete[] cmdLine;
		throw L"Error while executing " + appName + L": " + GetErrorMessage(GetLastError());
	}
	delete[] cmdLine;

	//Ok the process was created, wait for it to terminate.
	DWORD lastWait = 0;
	while ((lastWait = WaitForSingleObject(pInfo.hProcess, 50)) == WAIT_TIMEOUT)
		Application::Get().Yield();
	if (lastWait == WAIT_ABANDONED)
		throw std::wstring(L"The condition waiting on the termination of the .NET installer was abandoned.");

	//Get the exit code
	DWORD exitCode = 0;
	if (!GetExitCodeProcess(pInfo.hProcess, &exitCode))
		throw GetErrorMessage(GetLastError());

	//Clean up
	CloseHandle(pInfo.hProcess);
	CloseHandle(pInfo.hThread);

	//Return the exit code.
	return exitCode;
}

bool InstallNetFramework(std::wstring tempDir, bool quiet)
{
	//Update the UI
	MainWindow& mainWin = Application::Get().GetTopWindow();
	mainWin.SetProgressIndeterminate();
	mainWin.SetMessage(L"Installing .NET Framework...");

	//Get the path to the installer
	if (std::wstring(L"\\/").find(tempDir[tempDir.length() - 1]) == std::wstring::npos)
		tempDir += L"\\";
	std::wstring commandLine(L'"' + tempDir);
	//commandLine += L"dotNetFx40_Full_x86_x64.exe\" /norestart";
	//commandLine += L"dotNetFx40_Full_setup.exe\" /norestart";
	commandLine += L"NDP46-KB3045557-x86-x64-AllOS-ENU.exe\" /norestart /quiet";

	
	//Due to virus false positives we base64 encode some strings namely "NDP46-KB3045557-x86-x64-AllOS-ENU.exe /norestart /quiet"
	//std::vector<BYTE> myData;
	//std::string encodedData = base64_encode(&myData[0], myData.size());
	//std::vector<BYTE> decodedData = base64_decode("TkRQNDYtS0IzMDQ1NTU3LXg4Ni14NjQtQWxsT1MtRU5VLmV4ZSAvbm9yZXN0YXJ0IC9xdWlldA==");
	//std::wstring ustr(decodedData.begin(), decodedData.end());
	//commandLine += ustr; 
	
	//If the user wants it quiet then pass the /q switch
	//if (quiet)
	//	commandLine += L" /q";

	//And the return code is true if the process exited with 0.
	return CreateProcessAndWait(commandLine, L".NET Framework Installer") == 0;
}

bool InstallEraser(std::wstring tempDir, bool quiet)
{
	MainWindow& mainWin = Application::Get().GetTopWindow();
	mainWin.SetProgressIndeterminate();
	mainWin.SetMessage(L"Installing Eraser...");

	//Determine the system architecture.
	SYSTEM_INFO sysInfo;
	ZeroMemory(&sysInfo, sizeof(sysInfo));
	GetNativeSystemInfo(&sysInfo);

	if (std::wstring(L"\\/").find(tempDir[tempDir.length() - 1]) == std::wstring::npos)
		tempDir += L"\\";
	switch (sysInfo.wProcessorArchitecture)
	{
	case PROCESSOR_ARCHITECTURE_AMD64:
		tempDir += L"Eraser (x64).msi";
		break;

	default:
		tempDir += L"Eraser (x86).msi";
		break;
	}

	std::wstring commandLine(L"msiexec.exe /i ");
	commandLine += L'"' + tempDir + L'"';

	//Add the quiet command line parameter if a quiet command line parameter was
	//specified
	if (quiet)
		commandLine += L" /quiet /norestart";
	
	//And the return code is true if the process exited with 0.
	return CreateProcessAndWait(commandLine, L"Eraser") == 0;
}
