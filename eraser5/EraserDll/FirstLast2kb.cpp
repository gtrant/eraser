
#include "stdafx.h"
#include "EraserDll.h"
#include "Common.h"
#include "FirstLast2kb.h"
#include "RND.h"

#define OFFSET_FL2K 2048

bool wipeFileWithFirstLast2kb(CEraserContext *context) 
{ 
   DWORD highPart; 
   DWORD lowPart(GetFileSize(context->m_hFile, &highPart)); 

   //Also check the HighPart here 
   bool first_only = lowPart < OFFSET_FL2K && highPart == 0; 
   context->m_uiFileSize.LowPart = /*first_only ? lowPart :*/ OFFSET_FL2K; 
   //Make sure HighPart is reset to 0 when wiping the 1st OFFSET_FL2K bytes 
   context->m_uiFileSize.HighPart = 0; 
   memset(&context->m_uiFileStart,0, sizeof(context->m_uiFileStart)); 
   if (!wipeFileWithPseudoRandom(context)) 
      return false; 

   if(first_only) 
      return true; 

   //context->m_uiFileSize.LowPart = OFFSET_FL2K; 
   //context->m_uiFileStart.LowPart = lowPart - OFFSET_FL2K; 
   //context->m_uiFileStart.HighPart = highPart; 

   context->m_uiFileStart.LowPart = lowPart; 
   context->m_uiFileStart.HighPart = highPart; 
   context->m_uiFileStart.QuadPart -= OFFSET_FL2K; 

   //Get the block size for this drive - From function fileSizeToArea() in "EraserDllInternal.h" 
   E_UINT64 uBlockSize = max(context->m_piCurrent.m_uCluster, 
         max(context->m_piCurrent.m_uSector, DEFAULT_SECTOR_SIZE)); 

   //Convert m_uiFileStart (ActualFileSize - OFFSET_FL2K) to an exact multiple "of the volume's sector size." 
   //This value will be >= m_uiFileStart 
   E_UINT64 uTotal = fileSizeToArea(context, context->m_uiFileStart.QuadPart); 

   //Now, since uTotal > (ActualFileSize - OFFSET_FL2K), we need to backup the starting position by 1 uBlockSize 
      //to make sure that the last OFFSET_FL2K bytes of the file are actually overwritten 
   context->m_uiFileStart.QuadPart = uTotal - uBlockSize; 

   //Reset m_uiFileSize to the actual filesize 
   context->m_uiFileSize.LowPart = lowPart; 
   context->m_uiFileSize.HighPart = highPart; 

   //Convert m_uiFileSize to an exact multiple "of the volume's sector size" so that the slack space 
      //at the end gets filled 
   context->m_uiFileSize.QuadPart = fileSizeToArea(context, context->m_uiFileSize.QuadPart); 

   //Convert m_uiFileSize to the amount of bytes we want to overwrite. 
   //This is equal to FileSizeOnDisk - m_uiFileStart 
   //This is also an exact multiple "of the volume's sector size" because with "FILE_FLAG_NO_BUFFERING" 
      //WriteFile has to write in multiples 
   context->m_uiFileSize.QuadPart -= context->m_uiFileStart.QuadPart; 

   return wipeFileWithPseudoRandom(context); 
} 

bool oldwipeFileWithFirstLast2kb(CEraserContext *context)
{

	struct CtxGuard
	{
		CEraserContext *ctx_;
		ULARGE_INTEGER m_uiFileSize;
		ULARGE_INTEGER m_uiFileStart;
		CtxGuard(CEraserContext *ctx)
			:ctx_(ctx)
		{
			memcpy(&m_uiFileSize, &ctx->m_uiFileSize, sizeof(m_uiFileSize));
			memcpy(&m_uiFileStart, &ctx->m_uiFileStart, sizeof(m_uiFileStart));
		}

		~CtxGuard()
		{
			memcpy(&ctx_->m_uiFileSize, &m_uiFileSize, sizeof(m_uiFileSize));
			memcpy(&ctx_->m_uiFileStart, &m_uiFileStart, sizeof(m_uiFileStart));

		}
	};

	if (context->m_uiFileSize.QuadPart <= OFFSET_FL2K )
	{
		return wipeFileWithPseudoRandom(context);
	}

	CtxGuard guard(context);
	
	context->m_uiFileSize.QuadPart = OFFSET_FL2K;
	
	
	if (!wipeFileWithPseudoRandom(context))
		return false;
	

	//context->m_uiFileSize.QuadPart = guard.m_uiFileSize.QuadPart;
	context->m_uiFileStart.QuadPart = guard.m_uiFileSize.QuadPart - OFFSET_FL2K;	

	return wipeFileWithPseudoRandom(context);

}