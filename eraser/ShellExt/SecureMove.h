#pragma once

#include "stdafx.h"
#include <windows.h>

#include <cstring>
#include <string>

using namespace std;
namespace Eraser
{
	class SecureMove : public CDialog
	{
	private:
		CFile m_fs, m_fd;
		HANDLE m_progressEvent;
		HANDLE m_completeEvent;
		CWinThread m_thread;
	public:
		volatile LONG progress;
		explicit SecureMove(string& dst, string& src) : CDialog(), progress(0),			
			m_fs(CreateFile(src.c_str(), FILE_GENERIC_READ , 0, NULL, OPEN_EXISTING, 0, NULL),
			m_fd(CreateFile(dst.c_str(), FILE_GENERIC_WRITE, 0, NULL, OPEN_ALWAYS  , 0, NULL),
			m_thread(&SecureMove::RunMove, NULL)  
		{
			if(m_fs == CFile::hFileNull)
				;
			if(m_fd == CFile::hFileNull)
				;
			m_progressEvent = CreateEvent(NULL, FALSE, TRUE, NULL);
			m_completeEvent = CreateEvent(NULL, TRUE , TRUE, NULL);
			m_thread.Run();
		}

		void Run()
		{
			static int lprog = 0;
			while(true)
			{
				WaitForSingleObject(m_progressEvent, INFINITE);
				// update ui progress value
				// progressBar.Value = progress
			}			
		}

		UINT Move(void *ignored)
		{
			static char buffer[512*64];
			static LONG lprog = 0;

			while(m_fs.GetPosition() < m_fs.GetLength())
			{
				m_fd.Write(buffer, m_fs.Read(buffer, 512*64));				
				if((lprog -= InterlockedExchange(&progress, (m_fs.GetPosition() * 100) / m_fs.GetLength()) > 0)
					SetEvent(m_progressEvent);
			}

			return SetEvent(m_completeEven);
		}

		~SecureMove()
		{
		}
	};
}