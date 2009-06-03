// Pass.cpp
// Definitions for built-in wipe methods
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

///////////////////////////////////////////////////////////////////////////////
// include here the header files for all built-in methods
#include "Gutmann.h"
#include "DoD.h"
#include "RND.h"
#include "Custom.h"
#include "FirstLast2kb.h"
#include "Schneier7pass.h"

///////////////////////////////////////////////////////////////////////////////
// pass arrays
const PASS passGutmann[PASSES_GUTMANN] = {
    passRandom,                        // 1
    passRandom,
    passRandom,
    passRandom,
    passOne(0x55),                     // 5
    passOne(0xAA),
    passThree(0x92, 0x49, 0x24),
    passThree(0x49, 0x24, 0x92),
    passThree(0x24, 0x92, 0x49),
    passOne(0x00),                     // 10
    passOne(0x11),
    passOne(0x22),
    passOne(0x33),
    passOne(0x44),
    passOne(0x55),                     // 15
    passOne(0x66),
    passOne(0x77),
    passOne(0x88),
    passOne(0x99),
    passOne(0xAA),                     // 20
    passOne(0xBB),
    passOne(0xCC),
    passOne(0xDD),
    passOne(0xEE),
    passOne(0xFF),                     // 25
    passThree(0x92, 0x49, 0x24),
    passThree(0x49, 0x24, 0x92),
    passThree(0x24, 0x92, 0x49),
    passThree(0x6D, 0xB6, 0xDB),
    passThree(0xB6, 0xDB, 0x6D),       // 30
    passThree(0xDB, 0x6D, 0xB6),
    passRandom,
    passRandom,
    passRandom,
    passRandom                         // 35
};

const PASS passDOD[PASSES_DOD] = {
    passOne(0x55),                     // E (replaced with a random character)
    passOne(0xAA),
    passRandom,
    passOne(0x00),                     // C (replaced with a random character)
    passOne(0x55),                     // E (replaced with a random character)
    passOne(0xAA),
    passRandom
};

const PASS passDOD_E[PASSES_DOD_E] = {
    passOne(0x00),                     // E
    passOne(0xFF),
    passRandom
};

///////////////////////////////////////////////////////////////////////////////
// the array of built-in methods (add #defines to Pass.h)
const BMETHOD bmMethods[nBuiltinMethods] = {
    bmEntry(GUTMANN_METHOD_ID,       // REQUIRED: ID
            _T("Gutmann"),               // REQUIRED: Description (max. 80 chars)
            PASSES_GUTMANN,          // REQUIRED: Passes
            wipeFileWithGutmann,     // REQUIRED: Wipe function
            1,                       // OPTIONAL: Shuffle passes (depends on wipe function)
            passGutmann),            // OPTIONAL: Pointer to pass array (depends on wipe function)

    bmEntry(DOD_METHOD_ID,
            _T("US DoD 5220.22-M (8-306. / E, C and E)"),
            PASSES_DOD,
            wipeFileWithDoD,
            0,
            passDOD),

    bmEntry(DOD_E_METHOD_ID,
            _T("US DoD 5220.22-M (8-306. / E)"),
            PASSES_DOD_E,
            wipeFileWithCustom,     // There's no need to write yet another wipe function...
            0,
            passDOD_E),

    bmEntry(RANDOM_METHOD_ID,
            _T("Pseudorandom Data"),
            PASSES_RND,
            wipeFileWithPseudoRandom,
            0,
            0),

	bmEntry(FL2KB_METHOD_ID,
			_T("Only first and last 2KB"),
			PASSES_FL2KB,
			wipeFileWithFirstLast2kb,
			0,
			0),

	bmEntry(SCHNEIER_METHOD_ID,
			_T("Schneier's 7 pass"),
			PASSES_SCHNEIER,
			wipeFileWithSchneier7Pass,
			0,
			0)			

};
const BMETHOD* GetBMethods() 
{
	return &bmMethods[0];
}