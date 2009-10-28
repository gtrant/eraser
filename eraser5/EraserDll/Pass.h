// Pass.h
// Tools for handling overwriting passes and pass shuffling
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

#ifndef PASS_H
#define PASS_H
#ifdef DMARS
#define WIPEFUNCTION bool
#endif
#include "Random.h"
#include "FillMemoryWith.h"

///////////////////////////////////////////////////////////////////////////////
// definitions

// method IDs
#define BUILTIN_METHOD_ID       0x80
#define CUSTOM_METHOD_ID        0x00

#define MAX_CUSTOM_METHODS      (BUILTIN_METHOD_ID - 1)
#define MAX_CUSTOM_METHOD_ID    (MAX_CUSTOM_METHODS | CUSTOM_METHOD_ID)

#define MAX_BUILTIN_METHODS     MAX_CUSTOM_METHODS
#define MAX_BUILTIN_METHOD_ID   (MAX_BUILTIN_METHODS | BUILTIN_METHOD_ID)

// special data
#define RND_DATA                ((E_UINT16)-1)
#define RND_CHAR                (RND_DATA - 1)

// pass definitions
#define passThree(x, y, z)      { 3, (x), (y), (z) }
#define passTwo(x, y)           { 2, (x), (y),  0  }
#define passOne(x)              { 1, (x),  0,   0  }
#define passRandom              passOne(RND_DATA)

#define setPassThree(pass, x, y, z) \
    { (pass).bytes = 3; (pass).byte1 = (x); (pass).byte2 = (y); (pass).byte3 = (z); }
#define setPassTwo(pass, x, y) \
    { (pass).bytes = 2; (pass).byte1 = (x); (pass).byte2 = (y); (pass).byte3 = 0; }
#define setPassOne(pass, x) \
    { (pass).bytes = 1; (pass).byte1 = (x); (pass).byte2 = (pass).byte3 = 0; }
#define setPassRandom(pass, x) \
    setPassOne(pass, RND_DATA)

#define isRandomPass(pass) \
    ((pass).byte1 == RND_DATA)

///////////////////////////////////////////////////////////////////////////////
// structures

#pragma pack(1)

// pass structure - we'll settle for at most three adjacent bytes
typedef struct _ThreeBytePass {
    E_UINT8  bytes;                  // number of bytes used
    E_UINT16 byte1;
    E_UINT16 byte2;
    E_UINT16 byte3;
} PASS, *LPPASS;

#define DESCRIPTION_SIZE    (80+1)

typedef class _MethodBase {
public:
    E_UINT8      m_nMethodID;                        // ID
    TCHAR        m_szDescription[DESCRIPTION_SIZE];  // description
    E_UINT16     m_nPasses;                          // number of PASSes
    WIPEFUNCTION m_pwfFunction;                      // the wipe function

    E_UINT8      m_bShuffle;                         // shuffle passes
    LPPASS       m_lpPasses;                         // pointer to PASSes
} BMETHOD, *LPBMETHOD;

// Old ANSI Eraser Custom Method as stored in the registry
struct MethodBaseA {
	E_UINT8      m_nMethodID;                        // ID
	char         m_szDescription[DESCRIPTION_SIZE];  // description
	E_UINT16     m_nPasses;                          // number of PASSes
	WIPEFUNCTION m_pwfFunction;                      // the wipe function

	E_UINT8      m_bShuffle;                         // shuffle passes
};

#pragma pack()

#define bmEntry(a, b, c, d, e, f)   { a, b, c, d, e, (LPPASS)f }

// method definition - everything needed to save pass data
typedef class _Method : public _MethodBase {
public:
    _Method() {
        ZeroMemory(this, sizeof(_Method));
    }

    ~_Method() {
        if (m_lpPasses) {
            try {
                if (m_nPasses > 0) {
                    ZeroMemory(m_lpPasses, m_nPasses * sizeof(PASS));
                }
                delete[] m_lpPasses;
                m_lpPasses = 0;
            } catch (...) {
                ASSERT(0);
            }
        }
        ZeroMemory(this, sizeof(_Method));
    }

    _Method& operator=(const _MethodBase& rs) {
        return copy(*(const _Method*)&rs); // way ugly
    }

    _Method& operator=(const _Method& rs) {
        return copy(rs);
    }
	/*void Serialize(CArchive& ar)
	{
		int i=0;
		if (ar.IsStoring())
		{
			ar << (BYTE)m_nMethodID;
			for (i=0; i<DESCRIPTION_SIZE; i++)	ar << (char)m_szDescription[i];
			ar << (WORD)m_nPasses;
			
			ar << (BYTE)m_bShuffle;
			for (i=0; i<m_nPasses; i++)
			{
                ar << (BYTE)m_lpPasses[i].bytes;
				ar << (WORD)m_lpPasses[i].byte1;
				ar << (WORD)m_lpPasses[i].byte2;
				ar << (WORD)m_lpPasses[i].byte3;
			}
		}
		else
		{			
			ar >> (BYTE)m_nMethodID;
			for (i=0; i<DESCRIPTION_SIZE; i++)	ar >> (char)m_szDescription[i];
			ar >> (WORD)m_nPasses;
			
			ar >> (BYTE)m_bShuffle;
			m_lpPasses = new PASS[m_nPasses];
			for (i=0; i<m_nPasses; i++)
			{
				ar >> (BYTE)m_lpPasses[i].bytes;
				ar >> (WORD)m_lpPasses[i].byte1;
				ar >> (WORD)m_lpPasses[i].byte2;
				ar >> (WORD)m_lpPasses[i].byte3;
			}
			//m_pwfFunction = 0;
		}
	}*/

private:
    _Method& copy(const _Method& rs) {
        if (this != &rs) {
            try {
                if (m_lpPasses) {
                    if (m_nPasses > 0) {
                        ZeroMemory(m_lpPasses, m_nPasses * sizeof(PASS));
                    }
                    delete[] m_lpPasses;
                    m_lpPasses = 0;
                    m_nPasses = 0;
                }

                m_nMethodID = rs.m_nMethodID;
                lstrcpyn(m_szDescription, rs.m_szDescription, DESCRIPTION_SIZE);

                m_nPasses = rs.m_nPasses;
                m_pwfFunction = rs.m_pwfFunction;

                if (rs.m_lpPasses && rs.m_nPasses > 0) {
                    m_lpPasses = new PASS[rs.m_nPasses];
                    for (E_UINT16 i = 0; i < rs.m_nPasses; i++) {
                        m_lpPasses[i] = rs.m_lpPasses[i];
                    }
                }

                m_bShuffle = rs.m_bShuffle;
            } catch (...) {
                ASSERT(0);
            }
        }
        return *this;
    }
} METHOD, *LPMETHOD;

ERASER_API const BMETHOD*  GetBMethods() ;
///////////////////////////////////////////////////////////////////////////////
// built-in methods

const E_UINT8 nBuiltinMethods       = 6;

// maximum number of passes allowed
#define PASSES_MAX                  ((E_UINT16)-1)

#define GUTMANN_METHOD_ID           ((1 << 0) | BUILTIN_METHOD_ID)
#define PASSES_GUTMANN              35
#define PASSES_GUTMANN_RANDOM       4

#define DOD_METHOD_ID               ((1 << 1) | BUILTIN_METHOD_ID)
#define PASSES_DOD                  7

#define DOD_E_METHOD_ID             ((1 << 2) | BUILTIN_METHOD_ID)
#define PASSES_DOD_E                3

#define RANDOM_METHOD_ID            ((1 << 3) | BUILTIN_METHOD_ID)
#define PASSES_RND                  1
#define PASSES_RND_MAX              PASSES_MAX

#define FL2KB_METHOD_ID            ((1 << 4) | BUILTIN_METHOD_ID)
#define PASSES_FL2KB                  1
#define PASSES_FL2KB_MAX              2

#define SCHNEIER_METHOD_ID            ((1 << 5) | BUILTIN_METHOD_ID)
#define PASSES_SCHNEIER                  7
#define PASSES_SCHNEIER_MAX              7

// default for files
#define DEFAULT_FILE_METHOD_ID      GUTMANN_METHOD_ID
#define DEFAULT_FILE_PASSES         PASSES_GUTMANN
// default for unused disk space
#define DEFAULT_UDS_METHOD_ID       RANDOM_METHOD_ID
#define DEFAULT_UDS_PASSES          PASSES_RND

// global array of built-in methods
extern const BMETHOD bmMethods[nBuiltinMethods];

#endif // PASS_H
