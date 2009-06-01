// FillMemoryWith.cpp
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
#include "FillMemoryWith.h"
#include <stdio.h>

#define MAX_PATTERN_BYTES   3

void
FillMemoryWith(LPVOID destination, E_UINT32 bytes, E_UINT8 patternSize, ...)
{
    E_UINT8 patternBytes[MAX_PATTERN_BYTES];

    // "the error checking"
    if (destination == 0 || bytes == 0 ||
        patternSize == 0 || patternSize > MAX_PATTERN_BYTES) {
        ASSERT(0);
        return;
    }

    // read parameters
    va_list vlArgs;
    va_start(vlArgs, patternSize);

    for (E_UINT8 i = 0; i < patternSize; i++) {
        patternBytes[i] = va_arg(vlArgs, E_UINT8);
    }

    va_end(vlArgs);

    // fill memory block with desired string
 /*   __asm {
            mov     ecx, dword ptr destination  ; pointer to destination
            mov     esi, bytes                  ; number of bytes to write
            mov     dh, patternSize             ; pattern length (bytes)

        Start:
            mov     dl, dh
            lea     eax, dword ptr patternBytes ; pattern

        Inner:
            mov     bl, byte ptr [eax]          ; copy to destination
            inc     eax
            dec     esi
            mov     [ecx], bl

            je      Done

            inc     ecx
            dec     dl

            jne     Inner
            jmp     Start
        Done:
    }
*/
    /* So I felt like playing a bit with inline assembly.
       Oh well, in case someone needs this to be more portable... */

      E_PUINT8 pattern;
      E_UINT8  position;
    Start:
        pattern = patternBytes;
        position = patternSize;
    Inner:
        *((E_PUINT8)destination) = *pattern++;
        if (--bytes == 0)
            goto Done;

        destination = ((E_PUINT8)destination) + 1;

        if (--position > 0)
            goto Inner;

        goto Start;
    Done:
        return;
}
