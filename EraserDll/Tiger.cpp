// Tiger.cpp
//
// An implementation of Tiger/192 based on the reference code,
// assumes little-endian architecture.
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
#include "Tiger.h"

/*
** Word size
*/

#define ARCHITECTURE_WORDSIZE   32

/*
** Magic numbers
*/

#define I0      0x0123456789ABCDEF  /* Initialization values */
#define I1      0xFEDCBA9876543210
#define I2      0xF096A5B4C3B2E187
#define KS1     0xA5A5A5A5A5A5A5A5  /* In key schedule */
#define KS2     I0
#define KSS1    19                  /* Shift values */
#define KSS2    23
#define M1      5                   /* Multiplicative constants for each pass */
#define M2      7
#define MN      9

/*
** S-boxes
*/

extern E_UINT64 table[4 * 256];

#define T1 (table)
#define T2 (table + 256)
#define T3 (table + 256 * 2)
#define T4 (table + 256 * 3)

/*
** The compression function
*/

#if ARCHITECTURE_WORDSIZE == 64
    /* Return the ith byte of word */
    #define IB64(x, i) \
        ( ((x) >> ((i) << 3)) & 0xFF)
    #define IB0(x)  IB64(x, 0)
    #define IB1(x)  IB64(x, 1)
    #define IB2(x)  IB64(x, 2)
    #define IB3(x)  IB64(x, 3)
    #define IB4(x)  IB64(x, 4)
    #define IB5(x)  IB64(x, 5)
    #define IB6(x)  IB64(x, 6)
    #define IB7(x)  IB64(x, 7)
#else
    /* Do the same and avoid 64-bit operations */
    #define IBL32(x, i) \
        ((E_UINT8)(((E_UINT32)(x)) >> ((i) << 3)))
    #define IBH32(x, i) \
        ((E_UINT8)(((E_UINT32)((x) >> (4 << 3))) >> ((i - 4) << 3)))

    #define IB0(x)  IBL32(x, 0)
    #define IB1(x)  IBL32(x, 1)
    #define IB2(x)  IBL32(x, 2)
    #define IB3(x)  IBL32(x, 3)
    #define IB4(x)  IBH32(x, 4)
    #define IB5(x)  IBH32(x, 5)
    #define IB6(x)  IBH32(x, 6)
    #define IB7(x)  IBH32(x, 7)
#endif

#define saveState \
    sa = A; \
    sb = B; \
    sc = C;

#define round(A, B, C, D, M) \
    C ^= D; \
    A -= T1[IB0(C)] ^ T2[IB2(C)] ^ T3[IB4(C)] ^ T4[IB6(C)];  \
    B += T4[IB1(C)] ^ T3[IB3(C)] ^ T2[IB5(C)] ^ T1[IB7(C)];  \
    B *= M;

#define pass(A, B, C, M) \
    round(A, B, C, D0, M) \
    round(B, C, A, D1, M) \
    round(C, A, B, D2, M) \
    round(A, B, C, D3, M) \
    round(B, C, A, D4, M) \
    round(C, A, B, D5, M) \
    round(A, B, C, D6, M) \
    round(B, C, A, D7, M)

#define keySchedule \
    D0 -= D7 ^ KS1; \
    D1 ^= D0; \
    D2 += D1; \
    D3 -= D2 ^ ((~D1) << KSS1); \
    D4 ^= D3; \
    D5 += D4; \
    D6 -= D5 ^ ((~D4) >> KSS2); \
    D7 ^= D6; \
    D0 += D7; \
    D1 -= D0 ^ ((~D7) << KSS1); \
    D2 ^= D1; \
    D3 += D2; \
    D4 -= D3 ^ ((~D2) >> KSS2); \
    D5 ^= D4; \
    D6 += D5; \
    D7 -= D6 ^ KS2;

#define feedForward \
    A ^= sa; \
    B -= sb; \
    C += sc;

#define compress \
    saveState \
    pass(A, B, C, M1) \
    keySchedule \
    pass(C, A, B, M2) \
    keySchedule \
    pass(B, C, A, MN) \
    feedForward

#define DECLARE_COMPRESS_VARIABLES \
    E_UINT64 A, B, C; \
    E_UINT64 D0, D1, D2, D3, D4, D5, D6, D7; \
    E_UINT64 sa, sb, sc

#define tiger_compress_macro(block, state) \
{ \
    A = state[0]; \
    B = state[1]; \
    C = state[2]; \
\
    D0 = block[0]; \
    D1 = block[1]; \
    D2 = block[2]; \
    D3 = block[3]; \
    D4 = block[4]; \
    D5 = block[5]; \
    D6 = block[6]; \
    D7 = block[7]; \
\
    compress; \
\
    state[0] = A; \
    state[1] = B; \
    state[2] = C; \
}

/*
** Exported functions
*/

void tiger_compress(E_PUINT64 block, E_PUINT64 state)
{
    DECLARE_COMPRESS_VARIABLES;
    tiger_compress_macro(block, state);
}

void tiger(E_PUINT64 buffer, E_UINT64 length, E_PUINT64 state, E_PUINT8 work)
{
    /*
    ** Use macro version of tiger_compress for speed
    */

    #define tiger_compress(str, state) \
        tiger_compress_macro(((E_PUINT64)(str)), ((E_PUINT64)(state)))

    /*
    ** Variables
    */

    DECLARE_COMPRESS_VARIABLES;
    E_UINT64 i, j;

    /*
    ** h0
    */

    state[0] = I0;
    state[1] = I1;
    state[2] = I2;

    /*
    ** Process all available 64-byte blocks
    */

    for (i = length; i >= 64; i -= 64) {
        tiger_compress(buffer, state);
        buffer += 8;
    }

    /*
    ** Process remaining < 64 bytes
    */

    /*
    ** Copy remaining data to work buffer (if any)
    */

    for (j = 0; j < i; j++) {
        work[j] = ((E_PUINT8)buffer)[j];
    }

    /*
    ** MD4-compliant padding, starts with single bit 1 followed by a
    ** string of 0's and the message length in bits as a 64-byte word
    */

    work[j++] = 0x01;

    for (; j & 7; j++) {
        work[j] = 0;
    }

    if (j > 56) {
        /*
        ** Message length won't fit anymore, need to process another block
        */

        for(; j < 64; j++) {
            work[j] = 0;
        }

        tiger_compress((E_PUINT64)work, state);
        j = 0;
    }

    for (; j < 56; j++) {
        work[j] = 0;
    }

    /*
    ** Bit length
    */

    *((E_PUINT64)&work[56]) = length << 3;

    /*
    ** Final compress and we're done
    */

    tiger_compress((E_PUINT64)work, state);
}

void tiger(E_PUINT64 buffer, E_UINT64 length, E_PUINT64 state)
{
    E_UINT8 work[64];
    tiger(buffer, length, state, work);
}
