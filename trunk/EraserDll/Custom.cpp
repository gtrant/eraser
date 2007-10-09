// Custom.cpp
//
// Generic wipe function.
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
// 02111-1307, USA.

#include "stdafx.h"
#include "EraserDll.h"
#include "Common.h"
#include "Custom.h"

bool
wipeFileWithCustom(CEraserContext *context)
{
    if (!AfxIsValidAddress(context->m_lpmMethod, sizeof(METHOD)) ||
        !AfxIsValidAddress(context->m_lpmMethod->m_lpPasses,
                           sizeof(PASS) * context->m_lpmMethod->m_nPasses)) {
        return false;
    }

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

    // shuffle the pass array
    if (context->m_lpmMethod->m_bShuffle) {
        shufflePassArray((LPPASS)context->m_lpmMethod->m_lpPasses,
                         context->m_lpmMethod->m_nPasses);
    }

    for (E_UINT16 uCurrentPass = 0;
         uCurrentPass < context->m_lpmMethod->m_nPasses; uCurrentPass++) {
        eraserSafeAssign(context, context->m_uProgressCurrentPass,
                         (E_UINT16)(uCurrentPass + 1));

        // start from the beginning again
        SetFilePointer(context->m_hFile, context->m_uiFileStart.LowPart,
                       (E_PINT32)&context->m_uiFileStart.HighPart, FILE_BEGIN);

        uLength = context->m_uiFileSize.QuadPart;
        uUsedSize = uSavedSize;

        // fill buffer
        fillPassData(context->m_puBuffer, uUsedSize,
                     &context->m_lpmMethod->m_lpPasses[uCurrentPass]);

        while (uLength > 0) {
            // random data needs refilling
            if (isRandomPass(context->m_lpmMethod->m_lpPasses[uCurrentPass])) {
                isaacFill((E_PUINT8)context->m_puBuffer, uUsedSize);
            }

            // use the whole buffer as long as we can
            if (uLength < (E_UINT64)uUsedSize) {
                uUsedSize = (E_UINT32)uLength;
            }

            // completed if not terminated and write is successful
            bCompleted = !eraserInternalTerminated(context) &&
                         WriteFile(context->m_hFile, context->m_puBuffer,
                                   uUsedSize, &uWritten, NULL) &&
                         (uUsedSize == uWritten);

            // flush to disk
            FlushFileBuffers(context->m_hFile);

            // if not completed - stop!
            if (!bCompleted) {
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

        if (context->m_uTestMode && !eraserInternalTerminated(context)) {
            // pause, so the results can be examined
            context->m_evTestContinue.ResetEvent();
            eraserTestPausedNotify(context);
            WaitForSingleObject(context->m_evTestContinue, INFINITE);
        }

        if (!bCompleted) {
            break;
        }
    }

    // set statistics
    setEndStatistics(context, uWiped, uStartTime);

    // shuffle the pass array again to remove traces of pass order
    if (context->m_lpmMethod->m_bShuffle) {
        shufflePassArray((LPPASS)context->m_lpmMethod->m_lpPasses,
                         context->m_lpmMethod->m_nPasses);
    }

    return bCompleted;
}
