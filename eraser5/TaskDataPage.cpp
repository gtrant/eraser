// TaskDataPage.cpp
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
// Copyright © 2007 The Eraser Project
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
#include "resource.h"
#include "EraserUI\DirDialog.h"
#include "EraserUI\NewDialog.h"
#include "TaskDataPage.h"
#include ".\taskdatapage.h"

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

IMPLEMENT_DYNCREATE(CTaskDataPage, CPropertyPage)
IMPLEMENT_DYNCREATE(CTaskSchedulePage, CPropertyPage)
IMPLEMENT_DYNCREATE(CTaskStatisticsPage, CPropertyPage)


/////////////////////////////////////////////////////////////////////////////
// CTaskDataPage property page

CTaskDataPage::CTaskDataPage() :
CPropertyPage(CTaskDataPage::IDD),
m_tType(Drive),
m_bShowPersistent(FALSE),
m_dwFinishAction(0)
{
    //{{AFX_DATA_INIT(CTaskDataPage)
    m_strFolder = _T("");
    m_strFile = _T("");
    m_bRemoveOnlySub = FALSE;
    m_bSubfolders = FALSE;
    m_bRemoveFolder = FALSE;
    m_bPersistent = FALSE;
    m_bUseWildCards = FALSE;
	m_bWildCardsInSubfolders = FALSE;
	//}}AFX_DATA_INIT
}

CTaskDataPage::~CTaskDataPage()
{
}

void CTaskDataPage::DoDataExchange(CDataExchange* pDX)
{
	CPropertyPage::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CTaskDataPage)
	DDX_Control(pDX, IDC_COMBO_DRIVES, m_comboDrives);
	DDX_Text(pDX, IDC_EDIT_FOLDER, m_strFolder);
	DDX_Text(pDX, IDC_EDIT_FILE, m_strFile);
	DDX_Check(pDX, IDC_CHECK_ONLYSUB, m_bRemoveOnlySub);
	DDX_Check(pDX, IDC_CHECK_SUBFOLDERS, m_bSubfolders);
	DDX_Check(pDX, IDC_CHECK_FOLDER, m_bRemoveFolder);
	DDX_Control(pDX, IDC_RADIO_DISK, m_buRadioDisk);
	DDX_Control(pDX, IDC_RADIO_FILES, m_buRadioFiles);
	DDX_Control(pDX, IDC_RADIO_FILE, m_buRadioFile);
	DDX_Check(pDX, IDC_PERSISTENT_CHECK, m_bPersistent);
	DDX_Check(pDX, IDC_CHECK_WILDCARDS, m_bUseWildCards);
	DDX_Check(pDX, IDC_CHECK_WILDCARDS_SF, m_bWildCardsInSubfolders);
	DDX_CBIndex(pDX, IDC_COMBO_WHENFINISH, m_dwFinishAction);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CTaskDataPage, CPropertyPage)
    //{{AFX_MSG_MAP(CTaskDataPage)
    ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnBrowse)
    ON_BN_CLICKED(IDC_BUTTON_BROWSE_FILES, OnBrowseFiles)
    ON_BN_CLICKED(IDC_CHECK_FOLDER, OnRemoveFolder)
    ON_BN_CLICKED(IDC_RADIO_DISK, OnRadioDisk)
    ON_BN_CLICKED(IDC_RADIO_FILES, OnRadioFiles)
    ON_BN_CLICKED(IDC_RADIO_FILE, OnRadioFile)
    ON_BN_CLICKED(IDC_CHECK_WILDCARDS, OnCheckWildcards)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// CTaskSchedulePage property page

CTaskSchedulePage::CTaskSchedulePage() :
CPropertyPage(CTaskSchedulePage::IDD),
m_b24Hour(TRUE)
{
    //{{AFX_DATA_INIT(CTaskSchedulePage)
    m_bPM = FALSE;
    m_iWhen = Day;
    //}}AFX_DATA_INIT
}

CTaskSchedulePage::~CTaskSchedulePage()
{
}

void CTaskSchedulePage::DoDataExchange(CDataExchange* pDX)
{
    CPropertyPage::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CTaskSchedulePage)
    DDX_Control(pDX, IDC_EDIT_TIME, m_editTime);
    DDX_Check(pDX, IDC_CHECK_PM, m_bPM);
    DDX_CBIndex(pDX, IDC_COMBO_WHEN, m_iWhen);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CTaskSchedulePage, CPropertyPage)
    //{{AFX_MSG_MAP(CTaskSchedulePage)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// CTaskSchedulePage property page

CTaskStatisticsPage::CTaskStatisticsPage() :
CPropertyPage(CTaskStatisticsPage::IDD),
m_lpts(0)
{
    //{{AFX_DATA_INIT(CTaskStatisticsPage)
	m_strStatistics = _T("");
	//}}AFX_DATA_INIT
}

CTaskStatisticsPage::~CTaskStatisticsPage()
{
}

void CTaskStatisticsPage::DoDataExchange(CDataExchange* pDX)
{
    CPropertyPage::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CTaskStatisticsPage)
	DDX_Text(pDX, IDC_EDIT_STATISTICS, m_strStatistics);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CTaskStatisticsPage, CPropertyPage)
    //{{AFX_MSG_MAP(CTaskStatisticsPage)
	ON_BN_CLICKED(IDC_BUTTON_RESET, OnButtonReset)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()



void CTaskDataPage::OnBrowse()
{
    UpdateData(TRUE);

	CDirectoryDialog dd;
	CString tempPath(m_strFolder);
    if (dd.DoModal(tempPath, _T("Select the Folder to be erased.")))
		m_strFolder = tempPath;
    
    UpdateData(FALSE);
}

void CTaskDataPage::OnBrowseFiles()
{
    UpdateData(TRUE);

	CFileDialog fd(TRUE, NULL, m_strFile, 4 | 2, NULL, this, 0, true);
	if (fd.DoModal() == IDOK)
		m_strFile = fd.GetPathName();

    UpdateData(FALSE);
}

void CTaskDataPage::OnRemoveFolder()
{
    UpdateData(TRUE);

    // if the user wants to remove the specified folder, we
    // must remove the subfolders as well

    if (m_bRemoveFolder)
    {
        m_bSubfolders = TRUE;
        GetDlgItem(IDC_CHECK_SUBFOLDERS)->EnableWindow(FALSE);
        GetDlgItem(IDC_CHECK_ONLYSUB)->EnableWindow(TRUE);
    }
    else
    {
        GetDlgItem(IDC_CHECK_SUBFOLDERS)->EnableWindow(TRUE);
        GetDlgItem(IDC_CHECK_ONLYSUB)->EnableWindow(FALSE);
    }

    UpdateData(FALSE);
}

void CTaskDataPage::OnRadioDisk()
{
    // enable disk section
    GetDlgItem(IDC_COMBO_DRIVES)->EnableWindow(TRUE);

    // disable other sections
    GetDlgItem(IDC_BUTTON_BROWSE)->EnableWindow(FALSE);
    GetDlgItem(IDC_EDIT_FOLDER)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_FOLDER)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_SUBFOLDERS)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_ONLYSUB)->EnableWindow(FALSE);

    GetDlgItem(IDC_BUTTON_BROWSE_FILES)->EnableWindow(FALSE);
    GetDlgItem(IDC_EDIT_FILE)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_WILDCARDS)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_WILDCARDS_SF)->EnableWindow(FALSE);
}

void CTaskDataPage::OnRadioFiles()
{
    // enable folder section
    GetDlgItem(IDC_BUTTON_BROWSE)->EnableWindow(TRUE);
    GetDlgItem(IDC_EDIT_FOLDER)->EnableWindow(TRUE);
    GetDlgItem(IDC_CHECK_FOLDER)->EnableWindow(TRUE);
    GetDlgItem(IDC_CHECK_SUBFOLDERS)->EnableWindow(!m_bRemoveFolder);
    GetDlgItem(IDC_CHECK_ONLYSUB)->EnableWindow(m_bRemoveFolder);

    // disable other sections
    GetDlgItem(IDC_COMBO_DRIVES)->EnableWindow(FALSE);

    GetDlgItem(IDC_BUTTON_BROWSE_FILES)->EnableWindow(FALSE);
    GetDlgItem(IDC_EDIT_FILE)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_WILDCARDS)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_WILDCARDS_SF)->EnableWindow(FALSE);
}

void CTaskDataPage::OnRadioFile()
{
    // enable file section
    GetDlgItem(IDC_BUTTON_BROWSE_FILES)->EnableWindow(TRUE);
    GetDlgItem(IDC_EDIT_FILE)->EnableWindow(TRUE);
    ((CEdit*)GetDlgItem(IDC_EDIT_FILE))->SetReadOnly(!m_bUseWildCards);
    GetDlgItem(IDC_CHECK_WILDCARDS)->EnableWindow(TRUE);
    GetDlgItem(IDC_CHECK_WILDCARDS_SF)->EnableWindow(m_bUseWildCards);

    // disable other sections
    GetDlgItem(IDC_COMBO_DRIVES)->EnableWindow(FALSE);
    GetDlgItem(IDC_BUTTON_BROWSE)->EnableWindow(FALSE);
    GetDlgItem(IDC_EDIT_FOLDER)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_FOLDER)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_SUBFOLDERS)->EnableWindow(FALSE);
    GetDlgItem(IDC_CHECK_ONLYSUB)->EnableWindow(FALSE);

}

void CTaskDataPage::OnCheckWildcards()
{
    UpdateData(TRUE);
    ((CEdit*)GetDlgItem(IDC_EDIT_FILE))->SetReadOnly(!m_bUseWildCards);
    GetDlgItem(IDC_CHECK_WILDCARDS_SF)->EnableWindow(m_bUseWildCards);
}

BOOL CTaskDataPage::OnInitDialog()
{
    CPropertyPage::OnInitDialog();

    BOOL bDrive     = (m_tType == Drive);
    BOOL bFolder    = (m_tType == Folder);
    BOOL bFile      = (m_tType == File);

    m_comboDrives.FillDrives();

    if (!m_strSelectedDrive.IsEmpty())
        m_comboDrives.SelectDrive(m_strSelectedDrive);

    m_buRadioDisk.SetCheck(bDrive);
    m_buRadioFiles.SetCheck(bFolder);
    m_buRadioFile.SetCheck(bFile);

    // drive
    GetDlgItem(IDC_COMBO_DRIVES)->EnableWindow(bDrive);

    // folder
    GetDlgItem(IDC_BUTTON_BROWSE)->EnableWindow(bFolder);
    GetDlgItem(IDC_EDIT_FOLDER)->EnableWindow(bFolder);
    GetDlgItem(IDC_CHECK_FOLDER)->EnableWindow(bFolder);
    GetDlgItem(IDC_CHECK_SUBFOLDERS)->EnableWindow(bFolder && !m_bRemoveFolder);
    GetDlgItem(IDC_CHECK_ONLYSUB)->EnableWindow(bFolder && m_bRemoveFolder);

    // file
    GetDlgItem(IDC_BUTTON_BROWSE_FILES)->EnableWindow(bFile);
    GetDlgItem(IDC_EDIT_FILE)->EnableWindow(bFile);
    ((CEdit*)GetDlgItem(IDC_EDIT_FILE))->SetReadOnly(!m_bUseWildCards);
    GetDlgItem(IDC_CHECK_WILDCARDS)->EnableWindow(bFile);
    GetDlgItem(IDC_CHECK_WILDCARDS_SF)->EnableWindow(bFile && m_bUseWildCards);

    if (!m_bShowPersistent)
        GetDlgItem(IDC_PERSISTENT_CHECK)->ShowWindow(SW_HIDE);

	CComboBox* finish_action  = (CComboBox*)GetDlgItem(IDC_COMBO_WHENFINISH);
	int selstr;
	finish_action->SetItemDataPtr(selstr = finish_action->AddString(_T("None")), NULL);
	finish_action->SetItemData(finish_action->AddString(_T("Shutdown system")), EWX_POWEROFF);
	finish_action->SetItemData(finish_action->AddString(_T("Restart")), EWX_REBOOT);
	finish_action->SetItemData(finish_action->AddString(_T("Sleep")), (DWORD_PTR)-1);

	OSVERSIONINFO version;
	::ZeroMemory(&version, sizeof(version));
	version.dwOSVersionInfoSize = sizeof(version);
	if (GetVersionEx(&version) && (
		version.dwPlatformId != VER_PLATFORM_WIN32_NT ||
		version.dwMajorVersion < 5))
	{
		finish_action->EnableWindow(false);
	}
	finish_action->SetCurSel(m_dwFinishAction);
	

    UpdateData(FALSE);
    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CTaskDataPage::OnOK()
{
    UpdateData(TRUE);

    if (m_buRadioDisk.GetCheck())
        m_tType = Drive;
    else if (m_buRadioFiles.GetCheck())
        m_tType = Folder;
    else
        m_tType = File;

    if (m_tType == Drive)
        m_comboDrives.GetSelectedDrive(m_strSelectedDrive);
    else if (m_tType == Folder && !m_strFolder.IsEmpty())
    {
        if (m_strFolder[m_strFolder.GetLength() - 1] != '\\')
            m_strFolder += "\\";
    }

    CPropertyPage::OnOK();
}

void CTaskSchedulePage::OnOK()
{
    CPropertyPage::OnOK();

    UpdateData(TRUE);

    m_odtTime = m_editTime.GetTime();

    if (!m_b24Hour)
    {
        // 12-hour clock

        int iHour = m_odtTime.GetHour();
        COleDateTimeSpan odtSpan(0, 12, 0, 0);

        if ((iHour < 12 && m_bPM) ||
            (iHour == 12 && !m_bPM))
        {
            m_odtTime += odtSpan;
        }
    }
}

BOOL CTaskSchedulePage::OnInitDialog()
{
    CPropertyPage::OnInitDialog();

    TCHAR szLocale[2] = { 0, 0 };

    if (GetLocaleInfo(LOCALE_USER_DEFAULT,
                      LOCALE_ITIME,
                      szLocale, 2))
    {
        // 0 -> 12-hour clock
        // 1 -> 24-hour clock

        m_b24Hour = (szLocale[0] != '0');
    }

    if (!m_b24Hour)
    {
        // 12-hour clock requires some special arrangements
        int iHour;
        CString str;

        m_editTime.SetHours(12);
        m_editTime.SetMinHours(1);

        GetDlgItem(IDC_CHECK_PM)->ShowWindow(SW_SHOW);

        iHour = m_odtTime.GetHour();

        // 12 - 11 PM == 12 - 23
        // 12 - 11 AM == 00 - 11

        if (iHour < 1)
        {
            // 00 == 12 AM
            iHour = 12;
            m_bPM = FALSE;
        }
        else if (iHour > 12)
        {
            // 13 - 23 == 01 - 11 PM
            iHour -= 12;
            m_bPM = TRUE;
        }
        else if (iHour == 12)
        {
            // 12 == 12 PM
            m_bPM = TRUE;
        }
        else
        {
            // 01 - 11 == 01 - 11 AM
            m_bPM = FALSE;
        }

        str.Format(_T("%.2d:%.2d"), iHour, m_odtTime.GetMinute());
        m_editTime.SetTime(str);
    }
    else
    {
        // 24-hour clock
        m_editTime.SetTime(m_odtTime);
    }

    UpdateData(FALSE);

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

BOOL CTaskStatisticsPage::OnInitDialog()
{
    UpdateStatistics();
    CPropertyPage::OnInitDialog();

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CTaskStatisticsPage::OnButtonReset()
{
    if (m_lpts)
    {
        m_lpts->Reset();
        UpdateStatistics();
        UpdateData(FALSE);
    }
}

void CTaskStatisticsPage::UpdateStatistics()
{
    if (m_lpts)
    {
        CString strTemp;
        double dTime;

        // report header
        m_strStatistics = _T("Task Report:\r\n\r\n");
        // run
        strTemp.Format(_T("    Processed\t\t=  %u times\r\n"), m_lpts->m_dwTimes);
        m_strStatistics += strTemp;
        // successful
        strTemp.Format(_T("    Successful\t\t=  %u times\r\n"), m_lpts->m_dwTimesSuccess);
        m_strStatistics += strTemp;
        // terminated
        strTemp.Format(_T("    Terminated\t\t=  %u times\r\n"), m_lpts->m_dwTimesInterrupted);
        m_strStatistics += strTemp;
        // failure
        strTemp.Format(_T("    Possible failure\t\t=  %u times\r\n"),
                        m_lpts->m_dwTimes - m_lpts->m_dwTimesSuccess - m_lpts->m_dwTimesInterrupted);
        m_strStatistics += strTemp;

        // statistics header
        m_strStatistics += _T("\r\nStatistics (average):\r\n\r\n");
        // erased area
        strTemp.Format(_T("    Erased area\t\t=  %u %s\r\n"), m_lpts->m_dwAveArea, _T("kB"));
        m_strStatistics += strTemp;
        // written
        strTemp.Format(_T("    Data written\t\t=  %u %s\r\n"), m_lpts->m_dwAveWritten, _T("kB"));
        m_strStatistics += strTemp;
        // time
        dTime = (double)m_lpts->m_dwAveTime / 1000.0f;
        strTemp.Format(_T("    Write time\t\t=  %.2f %s"), dTime, _T("s"));
        m_strStatistics += strTemp;
        // speed
        if (dTime > 0)
        {
            strTemp.Format(_T("\r\n    Write speed\t\t=  %u %s"), (DWORD)((double)m_lpts->m_dwAveWritten /
                dTime), _T("kB/s"));
            m_strStatistics += strTemp;
        }

        UpdateData(FALSE);
    }
}
