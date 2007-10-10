// Item.cpp
// $Id$
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
#include "Eraser.h"
#include "Item.h"
//#include "EraserDll\Pass.h"

#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[]=__FILE__;
#define new DEBUG_NEW
#endif

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

CItem::CItem() :
m_bWildCardsInSubfolders(FALSE),
m_bUseWildcards(FALSE),
m_bRemoveFolder(FALSE),
m_bSubfolders(TRUE),
m_bRemoveOnlySub(FALSE),
m_tType(Drive),
m_bPersistent(FALSE)
{

}

CItem::CItem(const CItem& op)
{
    Copy(op);
}

CItem::~CItem()
{

}

CItem& CItem::operator=(const CItem& op)
{
    if (this != &op)
        Copy(op);

    return *this;
}

void CItem::Copy(const CItem& op)
{
    m_strData           = op.m_strData;
    m_bUseWildcards     = op.m_bUseWildcards;
    m_bWildCardsInSubfolders = op.m_bWildCardsInSubfolders;
    m_bRemoveFolder     = op.m_bRemoveFolder;
    m_bSubfolders       = op.m_bSubfolders;
    m_bRemoveOnlySub    = op.m_bRemoveOnlySub;
    m_tType             = op.m_tType;
    m_bPersistent       = op.m_bPersistent;
	m_iFinishAction		= op.m_iFinishAction;
}

BOOL CItem::SetDrive(const CString& str)
{
    if (str.GetLength() > _MAX_DRIVE || str.Find(":\\") != 1)
        return FALSE;
    else
    {
        m_tType   = Drive;
        m_strData = str;

        return TRUE;
    }
}

BOOL CItem::SetFolder(const CString& str)
{
    if (str.Find(":\\") != 1)
        return FALSE;
    else
    {
        m_tType   = Folder;
        m_strData = str;

        if (m_strData[m_strData.GetLength() - 1] != '\\')
            m_strData += "\\";

        return TRUE;
    }
}

BOOL CItem::SetFile(const CString& str)
{
    m_tType   = File;
    m_strData = str;

    return TRUE;
}
BOOL CItem::SetMask(const CString& str)
{
	m_tType = Mask;
	m_strData = str;
	return TRUE;
}

void CItem::Serialize(CArchive& ar)
{
    if (ar.IsStoring())
    {
        ar << static_cast<WORD>(m_tType);
        ar << m_strData;
        ar << m_bWildCardsInSubfolders;
        ar << m_bUseWildcards;
        ar << m_bRemoveFolder;
        ar << m_bRemoveOnlySub;
        ar << m_bSubfolders;
        ar << m_bPersistent;
		ar << m_iFinishAction;
    }
    else
    {
        WORD wTmp = 0;
        ar >> wTmp;
        m_tType = static_cast<Type>(wTmp);

        ar >> m_strData;
        ar >> m_bWildCardsInSubfolders;
        ar >> m_bUseWildcards;
        ar >> m_bRemoveFolder;
        ar >> m_bRemoveOnlySub;
        ar >> m_bSubfolders;
        ar >> m_bPersistent;
		ar >> m_iFinishAction;
    }
}

#ifdef SCHEDULER_IMPORT_COMPATIBLE
void CItem::Serialize40(CArchive& ar)
{
    if (ar.IsLoading())
    {
        WORD wTmp = 0;
        ar >> wTmp;
        m_tType = static_cast<Type>(wTmp);

        ar >> m_strData;
        ar >> m_bUseWildcards;
        ar >> m_bRemoveFolder;
        ar >> m_bRemoveOnlySub;
        ar >> m_bSubfolders;
        ar >> m_bPersistent;
		ar >> m_iFinishAction;
    }
}

void CItem::Serialize30(CArchive& ar)
{
    if (ar.IsLoading())
    {
        WORD wTmp = 0;
        ar >> wTmp;
        m_tType = static_cast<Type>(wTmp);

        ar >> m_strData;
        ar >> m_bRemoveFolder;
        ar >> m_bRemoveOnlySub;
        ar >> m_bSubfolders;
		ar >> m_iFinishAction;
    }
}
#endif // SCHEDULER_IMPORT_COMPATIBLE

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

CScheduleItem::CScheduleItem() :
CItem(),
m_dwTime(0),
m_scWhen(Day),
m_uTimerID(0),
m_bQueued(FALSE),
m_ehContext(ERASER_INVALID_CONTEXT),
m_bMethod(0)
{
    m_odtNext = GetTimeTimeZoneBased();
    m_odtLast.SetStatus(COleDateTime::null);
}

CScheduleItem::CScheduleItem(const CScheduleItem& op) :
CItem(),
m_bQueued(FALSE)
{
    Copy(op);
}

CScheduleItem::~CScheduleItem()
{
    if (m_ehContext != ERASER_INVALID_CONTEXT)
        eraserDestroyContext(m_ehContext);
}

void CScheduleItem::Copy(const CScheduleItem& op)
{
    // CItem
    CItem::Copy(op);

    // CScheduleItem
    m_ehContext = ERASER_INVALID_CONTEXT;

    m_dwTime       = op.m_dwTime;
    m_odtNext      = op.m_odtNext;
    m_odtLast      = op.m_odtLast;
    m_scWhen       = op.m_scWhen;
    m_uTimerID     = op.m_uTimerID;
    m_tsStatistics = op.m_tsStatistics;
}
UINT CScheduleItem::GetTimeSpan(const COleDateTime& odt) const
{
    // calculates the time in milliseconds
    // before the task is to be run

    COleDateTime        odtCurrent;
    COleDateTimeSpan    odtSpan;
    double              dSeconds;
    UINT                uResult;

    odtCurrent  = GetTimeTimeZoneBased();
    odtSpan     = odt - odtCurrent;

    dSeconds    = odtSpan.GetTotalSeconds();

    if (dSeconds < 0.0)
        return 0;
    else
    {
        uResult = static_cast<UINT>((dSeconds * 1000));
        return uResult;
    }
}

UINT CScheduleItem::GetTimeSpan() const
{
    return GetTimeSpan(m_odtNext);
}

BOOL CScheduleItem::ScheduledNow()
{
    // check whether the task is scheduled to be
    // executed in the near future or a while ago

    const double dTimeFrame = 120.0; // seconds

    COleDateTime        odtCurrent;
    COleDateTimeSpan    odtSpan;
    double              dSeconds;

    odtCurrent  = GetTimeTimeZoneBased();
    odtSpan     = m_odtNext - odtCurrent;

    dSeconds    = odtSpan.GetTotalSeconds();

    return ((dSeconds >= 0.0 && dSeconds < dTimeFrame) ||
            (dSeconds <= 0.0 && dSeconds > -dTimeFrame));
}

BOOL CScheduleItem::StillValid()
{
    return (GetTimeSpan() > 0);
}

BOOL CScheduleItem::CalcNextTime()
{
    // calculates the time of the next run
	if (m_scWhen == Reboot) 
	{return TRUE;}
	else
	{
    // now
    m_odtNext = GetTimeTimeZoneBased();

    m_odtNext.SetDateTime(m_odtNext.GetYear(),
                          m_odtNext.GetMonth(),
                          m_odtNext.GetDay(),
                          GetHour(),
                          GetMinute(),
                          0);

    // set the correct day
    if (m_scWhen == Day)
    {
        // daily

        if (StillValid())
            return TRUE;

        // not valid anymore, tomorrow again
        COleDateTimeSpan odtSpan;
        odtSpan.SetDateTimeSpan(1, 0, 0, 0);

        m_odtNext += odtSpan;
    }
    else
    {
        // weekly
        if (m_odtNext.GetDayOfWeek() == m_scWhen)
        {
            // correct day
            if (StillValid())
                return TRUE;

            // next week
            COleDateTimeSpan odtSpan;
            odtSpan.SetDateTimeSpan(7, 0, 0, 0);

            m_odtNext += odtSpan;
        }
        else
        {
            // wrong day
            int iDaysToAdd;
            int iDay = m_odtNext.GetDayOfWeek();

            for (iDaysToAdd = 0; iDay != m_scWhen; iDaysToAdd++)
            {
                if (iDay == 7) iDay = 0;
                iDay++;
            }

            COleDateTimeSpan odtSpan;
            odtSpan.SetDateTimeSpan(iDaysToAdd, 0, 0, 0);

            m_odtNext += odtSpan;
        }

        ASSERT(m_odtNext.GetDayOfWeek() == m_scWhen);
    }

    ASSERT(StillValid());
	}
    return TRUE;
}

BOOL CScheduleItem::SetTime(WORD wHour, WORD wMinute)
{
    if (wHour > 23 || wMinute > 59)
        return FALSE;
    else
    {
        m_dwTime = static_cast<DWORD>(MAKEWPARAM(wHour, wMinute));
        return TRUE;
    }
}

BOOL CScheduleItem::SetTime(DWORD dwTime)
{
    return SetTime(LOWORD(dwTime), HIWORD(dwTime));
}
CString		CScheduleItem::GetId() const
{ 
	CString m_strId;
	m_strId.Format("%d",m_uTimerID);
	return  "Eraser"+m_strId;
}
WORD CScheduleItem::GetHour() const
{
    return LOWORD(m_dwTime);
}

WORD CScheduleItem::GetMinute() const
{
    return HIWORD(m_dwTime);
}

BOOL CScheduleItem::GetNextTime(CString& str) const
{
    str = m_odtNext.Format();
    return TRUE;
}

BOOL CScheduleItem::SetNextTime(CString& str)
{
    BOOL bResult;

    try
    {
        bResult =
            m_odtNext.ParseDateTime((LPCTSTR) str);
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();

        bResult = FALSE;
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return bResult;
}

CScheduleItem& CScheduleItem::operator=(const CScheduleItem& op)
{
    if (this != &op)
        Copy(op);

    return *this;
}

void CScheduleItem::Serialize(CArchive& ar)
{
    if (ar.IsStoring())
    {
        ar << m_dwTime;
        ar << m_odtNext;
        ar << m_odtLast;
        ar << static_cast<WORD>(m_scWhen);
        m_tsStatistics.Serialize(ar);
		ar << m_bMethod;
		ar << m_nRndPass;
		ar << m_uEraseItems;
		
    }
    else
    {
        WORD wTmp = 0;
		ar >> m_dwTime;
        ar >> m_odtNext;
        ar >> m_odtLast;
        ar >> wTmp;
        m_scWhen = static_cast<Schedule>(wTmp);
        m_tsStatistics.Serialize(ar);
		
		ar >> m_bMethod;
		ar >> m_nRndPass;
		ar >> m_uEraseItems;
						
        m_uTimerID = 0;
        m_ehContext = ERASER_INVALID_CONTEXT;
    }

    CItem::Serialize(ar);
}

void CScheduleItem::Serialize41(CArchive& ar)
{
	if (ar.IsStoring())
	{		
		ar << m_dwTime;
		ar << m_odtNext;
		ar << m_odtLast;
		ar << static_cast<WORD>(m_scWhen);
		m_tsStatistics.Serialize(ar);
	}
	else
	{
		WORD wTmp = 0;
		ar >> m_dwTime;
		ar >> m_odtNext;
		ar >> m_odtLast;
		ar >> wTmp;
		m_scWhen = static_cast<Schedule>(wTmp);
		m_tsStatistics.Serialize(ar);
		        
		m_uTimerID = 0;
		m_ehContext = ERASER_INVALID_CONTEXT;
	}
	CItem::Serialize(ar);
}

#ifdef SCHEDULER_IMPORT_COMPATIBLE
void CScheduleItem::Serialize40(CArchive& ar)
{
    // support for loading only the old format used in
    // Eraser 4.0

    if (ar.IsLoading())
    {
        WORD wTmp = 0;

        ar >> m_dwTime;
        ar >> m_odtNext;
        ar >> m_odtLast;
        ar >> wTmp;
        m_scWhen = static_cast<Schedule>(wTmp);
        m_tsStatistics.Serialize(ar);

        m_uTimerID = 0;
        m_ehContext = ERASER_INVALID_CONTEXT;

        CItem::Serialize40(ar);
    }
}

void CScheduleItem::Serialize30(CArchive& ar)
{
    // support for loading only the old format used in
    // Eraser 3.0

    if (ar.IsLoading())
    {
        WORD wTmp = 0;

        ar >> m_dwTime;
        ar >> m_odtNext;
        ar >> m_odtLast;
        ar >> wTmp;
        m_scWhen = static_cast<Schedule>(wTmp);
        m_tsStatistics.Serialize(ar);

        m_uTimerID = 0;
        m_ehContext = ERASER_INVALID_CONTEXT;
    }

    CItem::Serialize30(ar);
}

void CScheduleItem::Serialize21(CArchive& ar)
{
    // support for loading only the old format used in
    // Eraser Scheduler 2.1

    if (ar.IsLoading())
    {
        WORD wTmp = 0;

        ar >> m_dwTime;
        ar >> m_odtNext;
        ar >> wTmp; m_scWhen = static_cast<Schedule>(wTmp);
        ar >> wTmp; m_tType = static_cast<Type>(wTmp);
        ar >> m_strData;
        ar >> m_bRemoveFolder;
        ar >> m_bRemoveOnlySub;
        ar >> m_bSubfolders;
        m_tsStatistics.Serialize(ar);

        m_uTimerID = 0;
        m_ehContext = ERASER_INVALID_CONTEXT;
    }
}
#endif // SCHEDULER_IMPORT_COMPATIBLE

BOOL CScheduleItem::IsRunning()
{
    E_UINT8 uRunning = 0;
    return (eraserOK(eraserIsRunning(m_ehContext, &uRunning)) && uRunning != 0);
}

void CScheduleItem::UpdateStatistics()
{
    try
    {
        E_UINT64 uWiped = 0;
        E_UINT64 uArea = 0;
        E_UINT32 uTime = 0;
        VERIFY(eraserOK(eraserStatGetWiped(m_ehContext, &uWiped)));
        VERIFY(eraserOK(eraserStatGetArea(m_ehContext, &uArea)));
        VERIFY(eraserOK(eraserStatGetTime(m_ehContext, &uTime)));

		if (m_tsStatistics.m_dwTimes > 0)
		{
			DWORD dwTmp;

			dwTmp = static_cast<DWORD>((uWiped + 512) / 1024);
			dwTmp += m_tsStatistics.m_dwAveWritten * (m_tsStatistics.m_dwTimes - 1);
			dwTmp /= m_tsStatistics.m_dwTimes;

			m_tsStatistics.m_dwAveWritten = dwTmp;

			dwTmp = static_cast<DWORD>((uArea + 512) / 1024);
			dwTmp += m_tsStatistics.m_dwAveArea * (m_tsStatistics.m_dwTimes - 1);
			dwTmp /= m_tsStatistics.m_dwTimes;

			m_tsStatistics.m_dwAveArea = dwTmp;

			dwTmp = uTime;
			dwTmp += m_tsStatistics.m_dwAveTime * (m_tsStatistics.m_dwTimes - 1);
			dwTmp /= m_tsStatistics.m_dwTimes;

			m_tsStatistics.m_dwAveTime = dwTmp;
		}
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

void CTaskStatistics::Serialize(CArchive& ar)
{
    if (ar.IsStoring())
    {
        ar << m_dwAveArea;
        ar << m_dwAveWritten;
        ar << m_dwAveTime;

        ar << m_dwTimes;
        ar << m_dwTimesInterrupted;
        ar << m_dwTimesSuccess;
    }
    else
    {
        ar >> m_dwAveArea;
        ar >> m_dwAveWritten;
        ar >> m_dwAveTime;

        ar >> m_dwTimes;
        ar >> m_dwTimesInterrupted;
        ar >> m_dwTimesSuccess;
    }
}
