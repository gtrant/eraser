#include "stdafx.h"

BOOL IsNewWindows()
{
 OSVERSIONINFO ovi;
 ovi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
 BOOL bRet = ::GetVersionEx(&ovi);
 return (bRet && ((ovi.dwMajorVersion >= 5) || (ovi.dwMajorVersion == 4 &&
ovi.dwMinorVersion >= 90)));
}