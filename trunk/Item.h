// Item.h
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

#if !defined(AFX_ITEM_H__44195821_F0FC_11D2_BBF3_00105AAF62C4__INCLUDED_)
#define AFX_ITEM_H__44195821_F0FC_11D2_BBF3_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "EraserDll\EraserDll.h"


// should older file formats be supported?
#define SCHEDULER_IMPORT_COMPATIBLE

// task version information
#define ITEMVERSION     7   // Eraser 7.1 -
#define ITEMVERSION_41  6   // Eraser 4.1 -
#define ITEMVERSION_40  5   // Eraser 3.5 - 4.0
#define ITEMVERSION_30  4   // Eraser 3.0
#define ITEMVERSION_21  3   // Eraser Scheduler 2.1

// ítem IDs (Eraser 3.0 -)
#define ITEM_ID         0   // On-Demand
#define SCHEDULE_ID     1   // Scheduler

// data type
enum Type
{
    Drive,
    Folder,
    File
};

// schedule IDs
enum Schedule
{
    Day,
    Sunday,     // 1
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,    // 7
	Reboot
};

// schedule names
const LPCTSTR szScheduleName[] =
{
    "Day",
    "Sunday",
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
	"Reboot"
};

// helper for scheduler task statistics

class CTaskStatistics
{
public:
    CTaskStatistics()           { Reset(); }

    void Reset()
    {
        m_dwAveArea             = 0;
        m_dwAveWritten          = 0;
        m_dwAveTime             = 0;
        m_dwTimes               = 0;
        m_dwTimesInterrupted    = 0;
        m_dwTimesSuccess        = 0;
    };

    void    Serialize(CArchive&);

    DWORD   m_dwAveArea;
    DWORD   m_dwAveWritten;
    DWORD   m_dwAveTime;

    DWORD   m_dwTimes;
    DWORD   m_dwTimesInterrupted;
    DWORD   m_dwTimesSuccess;

};

typedef CTaskStatistics TASKSTATISTICS, *LPTASKSTATISTICS;

// the On-Demand item
class CItem
{
public:
    CItem();
    CItem(const CItem&);
    virtual ~CItem();

    CItem&  operator=(const CItem&);

    BOOL    SetFolder(const CString& str);
    BOOL    SetDrive(const CString&);
    BOOL    SetFile(const CString&);

	CString GetData() const                    { return m_strData; }
    void    GetData(CString& str) const        { str = m_strData; }
    Type    GetType() const                    { return m_tType; }

    BOOL    IsPersistent()                     { return m_bPersistent; }
    void    SetPersistent(BOOL bPersistent)    { m_bPersistent = bPersistent; }

    BOOL    UseWildcards()                     { return m_bUseWildcards; }
    void    UseWildcards(BOOL bUseWildcards)   { m_bUseWildcards = bUseWildcards; }

    BOOL    WildcardsInSubfolders()            { return m_bWildCardsInSubfolders; }
    void    WildcardsInSubfolders(BOOL bSF)    { m_bWildCardsInSubfolders = bSF; }

    void    RemoveFolder(BOOL bRemove)         { m_bRemoveFolder = bRemove; }
    BOOL    RemoveFolder() const               { return m_bRemoveFolder; }
    void    Subfolders(BOOL bSub)              { m_bSubfolders = bSub; }
    BOOL    Subfolders() const                 { return m_bSubfolders; }
    void    OnlySubfolders(BOOL bOnly)         { m_bRemoveOnlySub = bOnly; }
    BOOL    OnlySubfolders() const             { return m_bRemoveOnlySub; }
	inline int FinishAction() const			   { return m_iFinishAction; }
	inline void FinishAction(DWORD iAct)          {m_iFinishAction = iAct;}  

    void    Serialize(CArchive& ar);
#ifdef SCHEDULER_IMPORT_COMPATIBLE
    void    Serialize30(CArchive& ar);         // Eraser 3.0 load support
    void    Serialize40(CArchive& ar);         // Eraser 3.5 - 4.0 load support
#endif

protected:
    void    Copy(const CItem&);

    CString m_strData;
    Type    m_tType;
    BOOL    m_bPersistent;

    BOOL    m_bUseWildcards;
    BOOL    m_bWildCardsInSubfolders;
    BOOL    m_bRemoveFolder;
    BOOL    m_bRemoveOnlySub;
    BOOL    m_bSubfolders;
	int		m_iFinishAction;
};

// the scheduler item
class CScheduleItem  : public CItem
{
public:

    CScheduleItem();
    CScheduleItem(const CScheduleItem&);
    virtual ~CScheduleItem();

    CScheduleItem&  operator=(const CScheduleItem&);

    // time

    BOOL            CalcNextTime();
    BOOL            StillValid();
    BOOL            ScheduledNow();
    UINT            GetTimeSpan() const;
    UINT            GetTimeSpan(const COleDateTime&) const;

    WORD            GetHour() const;
    WORD            GetMinute() const;
	CString			GetId() const;
    void            SetSchedule(Schedule sc)    { m_scWhen = sc; }
    BOOL            SetTime(WORD, WORD);
    BOOL            SetTime(DWORD);

    DWORD           GetTime() const             { return m_dwTime; }
    DWORD           GetSchedule() const         { return (DWORD) m_scWhen; }

    BOOL            GetNextTime(CString&) const;
    BOOL            SetNextTime(CString&);
    COleDateTime    GetNextTime() const         { return m_odtNext; }

    void            SetLastTime(COleDateTime& odt)
                                                { m_odtLast = odt; }
    COleDateTime    GetLastTime() const         { return m_odtLast; }

    void            SetStatistics(const TASKSTATISTICS& ts)
                                                { m_tsStatistics = ts; }
    LPTASKSTATISTICS GetStatistics()            { return &m_tsStatistics; }
    void            UpdateStatistics();

    void            Serialize(CArchive&);
#ifdef SCHEDULER_IMPORT_COMPATIBLE
	void			Serialize41(CArchive& ar);
    void            Serialize40(CArchive&);     // Eraser 3.5 - 4.0 load support
    void            Serialize30(CArchive&);     // Eraser 3.0 load support
    void            Serialize21(CArchive&);     // Eraser Scheduler 2.1 load support
#endif

    // timer

    UINT            m_uTimerID;

    // wiping

    void            SetQueued(BOOL bOnQueue)    { m_bQueued = bOnQueue; }
    BOOL            IsQueued()                  { return m_bQueued; }

    BOOL            IsRunning();

    ERASER_HANDLE   m_ehContext;
	E_UINT8			m_bMethod;
	E_UINT16		m_nRndPass;
	E_UINT8			m_uEraseItems;


protected:
	void            Copy(const CScheduleItem&);

	static UINT     LastID;
    COleDateTime    m_odtNext;
    COleDateTime    m_odtLast;
    Schedule        m_scWhen;
    DWORD           m_dwTime;
    TASKSTATISTICS  m_tsStatistics;
	CString         m_strID;

    BOOL            m_bQueued;  // not serialized!
};


#endif // !defined(AFX_ITEM_H__44195821_F0FC_11D2_BBF3_00105AAF62C4__INCLUDED_)
