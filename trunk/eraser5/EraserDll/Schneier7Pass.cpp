#include "stdafx.h"
#include "Schneier7pass.h"
#include "EraserDll.h"
#include "Common.h"
enum
{
	SCHNEIER_PASS_COUNT = 7
};
typedef bool (*PFPassFillStrategy)(E_PUINT8, UINT_PTR);
static bool oneFill(E_PUINT8 pBuffer, UINT_PTR bufferSize)
{
	memset(pBuffer, 1, bufferSize);
	return true;
}
static bool zeroFill(E_PUINT8 pBuffer, UINT_PTR bufferSize)
{
	memset(pBuffer, 0, bufferSize);
	return true;
}
static PFPassFillStrategy sPassStrategy[SCHNEIER_PASS_COUNT ] =
{
	oneFill,
	zeroFill,
	isaacFill,
	isaacFill,
	isaacFill,
	isaacFill,
	isaacFill	
};

bool wipeFileWithSchneier7Pass(CEraserContext *context)
{
	E_UINT32 uStartTime = GetTickCount();
	E_UINT32 uUsedSize  = 0;
	E_UINT32 uSavedSize = 0;
	E_UINT64 uLength    = 0;
	E_UINT64 uWiped     = 0;
	E_UINT32 uWritten   = 0;
	bool     bCompleted = true;

	// send the begin message only once
	postStartNotification(context);

	setBufferSize(context, uSavedSize);

	for (E_UINT16 uCurrentPass = 0; uCurrentPass < SCHNEIER_PASS_COUNT; uCurrentPass++) 
	{
		eraserSafeAssign(context, context->m_uProgressCurrentPass, (E_UINT16)(uCurrentPass + 1));

		// start from the beginning again
		SetFilePointer(context->m_hFile, context->m_uiFileStart.LowPart,
			(E_PINT32)&context->m_uiFileStart.HighPart, FILE_BEGIN);

		uLength = context->m_uiFileSize.QuadPart;
		uUsedSize = uSavedSize;

		while (uLength > 0) 
		{			
			sPassStrategy[uCurrentPass]((E_PUINT8)context->m_puBuffer, uUsedSize);

			// use the whole buffer as long as we can
			if (uLength < (E_UINT64)uUsedSize) 
			{
				uUsedSize = (E_UINT32)uLength;
			}

			// completed if not terminated and write is successful
			bool terminated = eraserInternalTerminated(context);
			bCompleted = !terminated &&
				TRUE == WriteFile(context->m_hFile, context->m_puBuffer,
				uUsedSize, &uWritten, NULL) &&
				(uUsedSize == uWritten);

			// flush to disk
			FlushFileBuffers(context->m_hFile);

			// if not completed - stop!
			if (!bCompleted) 
			{
				break;
			}

			// set statistics
			context->m_uProgressWiped += (E_UINT64)uUsedSize;
			uWiped += (E_UINT64)uUsedSize;

			// how much left to go?
			uLength -= (E_UINT64)uUsedSize;

			// send update to window
			postUpdateNotification(context, context->m_lpmMethod->m_nPasses);
		}

		if (context->m_uTestMode && !eraserInternalTerminated(context)) 
		{
			// pause, so the results can be examined
			context->m_evTestContinue.ResetEvent();
			eraserTestPausedNotify(context);
			WaitForSingleObject(context->m_evTestContinue, INFINITE);
		}

		if (!bCompleted) 
		{
			break;
		}
	}

	// set statistics
	setEndStatistics(context, uWiped, uStartTime);
	return bCompleted;

}