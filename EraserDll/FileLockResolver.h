#pragma once
#include "EraserDll.h"

class ERASER_API CFileLockResolver
{
public:
	
	CFileLockResolver(BOOL = FALSE);
	~CFileLockResolver(void);
	void Close();
private:
	
	CFileLockResolver(ERASER_HANDLE, BOOL);
	inline void AskUser(BOOL val)
	{
		m_bAskUser = val;
	}
public:
	void SetHandle(ERASER_HANDLE);
	static void Resolve(LPCTSTR szFileName, CStringArray&);
	static void Resolve(LPCTSTR szFileName);
private:
	BOOL m_bAskUser;	
	CString m_strLockFileList;
	ERASER_HANDLE m_hHandle;
private:
	void HandleError(LPCTSTR szFileName, DWORD dwErrorCode, int method, unsigned int passes);
	static DWORD ErrorHandler(LPCTSTR szFileName, DWORD dwErrorCode, void* ctx, void* param);
};

