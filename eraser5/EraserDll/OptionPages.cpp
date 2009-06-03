// OptionPages.cpp
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
#include "EraserDll.h"
#include "EraserDllInternal.h"
#include "Options.h"
#include "OptionPages.h"
#include "CustomMethodEdit.h"
#include "PassEditDlg.h"
#include "Common.h"
#include "resource.h"
#include <afxstat_.h>

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

IMPLEMENT_DYNCREATE(COptionsForFiles, CPropertyPage)

IMPLEMENT_DYNCREATE(COptionsForFreeSpace, CPropertyPage)

class COptionsForFilesData
{
public:
	COptionsForFilesData():m_plsSettings(NULL),
		m_ppgFreeSpace(NULL),
		m_nSelectedMethodID(DEFAULT_FILE_METHOD_ID),
		m_nSelectedMethod(-1)
	{  
		m_strSelected = _T("");
		m_bFileClusterTips = FALSE;
		m_bFileNames = FALSE;
		m_bFileAlternateDataStreams = FALSE;
		m_plsSettings = new LibrarySettings();
		if (!loadLibrarySettings(m_plsSettings))
			setLibraryDefaults(m_plsSettings);
		m_bSettingOwner = TRUE;


	}
	~COptionsForFilesData()
	{
		if (TRUE == m_bSettingOwner && NULL != m_plsSettings)
		{
			delete m_plsSettings;  
		}
	}
	LibrarySettings *m_plsSettings;
	BOOL m_bSettingOwner;
	COptionsForFreeSpace *m_ppgFreeSpace;
	BYTE m_nSelectedMethodID;
	int m_nSelectedMethod; 
	CFlatListCtrl   m_lcMethod;
	CString m_strSelected;
	BOOL m_bFileClusterTips;
	BOOL m_bFileNames;
	BOOL    m_bFileAlternateDataStreams;

};

LibrarySettings* COptionsForFiles::GetLibSettings()
{
	return m_pData->m_plsSettings;
}
void COptionsForFiles::SetLibSettings(LibrarySettings* val)
{
	if (TRUE == m_pData->m_bSettingOwner && NULL != m_pData->m_plsSettings)
	{
		delete m_pData->m_plsSettings;
		m_pData->m_bSettingOwner = FALSE;
	}
	m_pData->m_plsSettings=val;
}
COptionsForFreeSpace* COptionsForFiles::GetFreeSpaceOpt()
{
	return m_pData->m_ppgFreeSpace;
}
void COptionsForFiles::SetFreeSpaceOpt(COptionsForFreeSpace* val)
{
    m_pData->m_ppgFreeSpace=val;
}
BYTE COptionsForFiles::GetSelectedMethodId()
{
	return m_pData->m_nSelectedMethodID;
}
void COptionsForFiles::SetSelectedMethodId(BYTE val)
{
	m_pData->m_nSelectedMethodID=val;
}
int COptionsForFiles::GetSelectedMethod()
{
	return m_pData->m_nSelectedMethod;
}
void COptionsForFiles::SetSelectedMethod(int val)
{
	m_pData->m_nSelectedMethod=val;
}
CFlatListCtrl& COptionsForFiles::GetMethodList()
{
	return m_pData->m_lcMethod;
}
/*void COptionsForFiles::SetMethodList(CFlatListCtrl val)
{	
	m_pData->m_lcMethod=val;
}*/
CString& COptionsForFiles::GetSelectedStr()
{
	return m_pData->m_strSelected;
}
void COptionsForFiles::SetSelectedStr(CString val)
{
	m_pData->m_strSelected=val;
}
BOOL& COptionsForFiles::GetFileClusterTips()
{
	return m_pData->m_bFileClusterTips;
}
void COptionsForFiles::SetFileClusterTips(BOOL val)
{
	m_pData->m_bFileClusterTips=val;
}
BOOL& COptionsForFiles::GetFileNames()
{
	return m_pData->m_bFileNames;
}
void COptionsForFiles::SetFileNames(BOOL val)
{
	m_pData->m_bFileNames=val;
}
BOOL& COptionsForFiles::GetFileAltDataStreams()
{
	return m_pData->m_bFileAlternateDataStreams;
}
void COptionsForFiles::SetFileAltDataStreams(BOOL val)
{
	m_pData->m_bFileAlternateDataStreams=val;
}

static const int iColumnCount = 3;

static const LPTSTR szColumnNames[] =
{
    _T("#"),
    _T("Description"),
    _T("Passes")
};

static int iColumnWidths[] =
{
    30,
    -1,
    60
};

static inline void FormatSelectedField(CString& strOutput, const CString& strName, const CString& strPasses)
{
    strOutput = _T("Selected: ") + strName +
                _T(" (") + strPasses + ((strPasses == _T("1")) ? _T(" pass)") : _T(" passes)"));
}

static void CreateList(CListCtrl& lcMethod)
{
    CRect rClient;
    lcMethod.GetClientRect(&rClient);

    iColumnWidths[1] = rClient.Width() -
                       iColumnWidths[0] -
                       iColumnWidths[2] -
                       2 * GetSystemMetrics(SM_CXBORDER);

    LVCOLUMN lvc;
    ZeroMemory(&lvc, sizeof(LVCOLUMN));

    lvc.mask        = LVCF_FMT | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
    lvc.fmt         = LVCFMT_LEFT;
    lvc.pszText     = szColumnNames[0];
    lvc.cx          = iColumnWidths[0];
    lvc.iSubItem    = 0;
    lcMethod.InsertColumn(0, &lvc);

    lvc.mask        = LVCF_FMT | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
    lvc.fmt         = LVCFMT_LEFT;
    lvc.pszText     = szColumnNames[1];
    lvc.cx          = iColumnWidths[1];
    lvc.iSubItem    = 1;
    lcMethod.InsertColumn(1, &lvc);

    lvc.mask        = LVCF_FMT | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
    lvc.fmt         = LVCFMT_LEFT;
    lvc.pszText     = szColumnNames[2];
    lvc.cx          = iColumnWidths[2];
    lvc.iSubItem    = 2;
    lcMethod.InsertColumn(2, &lvc);

    lcMethod.SetExtendedStyle(LVS_EX_HEADERDRAGDROP | LVS_EX_FULLROWSELECT | LVS_EX_GRIDLINES);
}

// using the nRandomPasses parameter is just a silly hack, cause I wanted to be able to use
// the same code with both property pages...
static void UpdateFreeSpaceMethodList(CListCtrl& lcMethod, LibrarySettings *plsSettings, WORD& nRandomPasses)
{
    lcMethod.SetRedraw(FALSE);

    try
    {
        lcMethod.DeleteAllItems();

        CString         strTmp;
        BYTE            i;
        BYTE            nItem = 1;
        LV_ITEM         lvi;
        WORD            nPasses = 0;
        ZeroMemory(&lvi, sizeof(LV_ITEM));

        // built-in
        for (i = 0; i < nBuiltinMethods; i++, nItem++)
        {
            if (i!=4) //FirstLast2K
            {
            nPasses = (GetBMethods()[i].m_nMethodID == RANDOM_METHOD_ID) ?
                        nRandomPasses : GetBMethods()[i].m_nPasses;

            strTmp.Format(_T("%u"), (DWORD)nItem);
            lvi.mask        = LVIF_TEXT | LVIF_PARAM;
            lvi.lParam      = (LPARAM)GetBMethods()[i].m_nMethodID;
            lvi.iItem       = nItem;
            lvi.iSubItem    = 0;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lvi.iItem       = lcMethod.InsertItem(&lvi);
            strTmp.ReleaseBuffer();

            lvi.mask        = LVIF_TEXT;
            lvi.iSubItem    = 1;
            lvi.pszText     = (LPTSTR)GetBMethods()[i].m_szDescription;
            lcMethod.SetItem(&lvi);

            strTmp.Format(_T("%u"), (DWORD)nPasses);
            lvi.iSubItem    = 2;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lcMethod.SetItem(&lvi);
            strTmp.ReleaseBuffer();
            }
        }

        // custom
        for (i = 0; i < plsSettings->m_nCMethods; i++, nItem++)
        {
            strTmp.Format(_T("%u"), (DWORD)nItem);
            lvi.mask        = LVIF_TEXT | LVIF_PARAM;
            lvi.lParam      = (LPARAM)plsSettings->m_lpCMethods[i].m_nMethodID;
            lvi.iItem       = nItem;
            lvi.iSubItem    = 0;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lvi.iItem       = lcMethod.InsertItem(&lvi);
            strTmp.ReleaseBuffer();

            lvi.mask        = LVIF_TEXT;
            lvi.iSubItem    = 1;
            lvi.pszText     = (LPTSTR)plsSettings->m_lpCMethods[i].m_szDescription;
            lcMethod.SetItem(&lvi);

            strTmp.Format(_T("%u"), (DWORD)plsSettings->m_lpCMethods[i].m_nPasses);
            lvi.iSubItem    = 2;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lcMethod.SetItem(&lvi);
            strTmp.ReleaseBuffer();
        }

        CRect rList, rHeader;
        CSize size = lcMethod.ApproximateViewRect();

        lcMethod.GetClientRect(&rList);
        lcMethod.GetHeaderCtrl()->GetClientRect(&rHeader);

        if (size.cy > (rList.Height() + rHeader.Height()))
            lcMethod.SetColumnWidth(1, iColumnWidths[1] - GetSystemMetrics(SM_CXVSCROLL));
        else
            lcMethod.SetColumnWidth(1, iColumnWidths[1]);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    lcMethod.SetRedraw(TRUE);
}
static void UpdateMethodList(CListCtrl& lcMethod, LibrarySettings *plsSettings, WORD& nRandomPasses)
{
    lcMethod.SetRedraw(FALSE);

    try
    {
        lcMethod.DeleteAllItems();

        CString         strTmp;
        BYTE            i;
        BYTE            nItem = 1;
        LV_ITEM         lvi;
        WORD            nPasses = 0;
        ZeroMemory(&lvi, sizeof(LV_ITEM));

        // built-in
        for (i = 0; i < nBuiltinMethods; i++, nItem++)
        {
			
            nPasses = (GetBMethods()[i].m_nMethodID == RANDOM_METHOD_ID) ?
                        nRandomPasses : GetBMethods()[i].m_nPasses;

            strTmp.Format(_T("%u"), (DWORD)nItem);
            lvi.mask        = LVIF_TEXT | LVIF_PARAM;
            lvi.lParam      = (LPARAM)GetBMethods()[i].m_nMethodID;
            lvi.iItem       = nItem;
            lvi.iSubItem    = 0;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lvi.iItem       = lcMethod.InsertItem(&lvi);
            strTmp.ReleaseBuffer();

            lvi.mask        = LVIF_TEXT;
            lvi.iSubItem    = 1;
            lvi.pszText     = (LPTSTR)GetBMethods()[i].m_szDescription;
            lcMethod.SetItem(&lvi);

            strTmp.Format(_T("%u"), (DWORD)nPasses);
            lvi.iSubItem    = 2;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lcMethod.SetItem(&lvi);
            strTmp.ReleaseBuffer();
        }

        // custom
        for (i = 0; i < plsSettings->m_nCMethods; i++, nItem++)
        {
            strTmp.Format(_T("%u"), (DWORD)nItem);
            lvi.mask        = LVIF_TEXT | LVIF_PARAM;
            lvi.lParam      = (LPARAM)plsSettings->m_lpCMethods[i].m_nMethodID;
            lvi.iItem       = nItem;
            lvi.iSubItem    = 0;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lvi.iItem       = lcMethod.InsertItem(&lvi);
            strTmp.ReleaseBuffer();

            lvi.mask        = LVIF_TEXT;
            lvi.iSubItem    = 1;
            lvi.pszText     = (LPTSTR)plsSettings->m_lpCMethods[i].m_szDescription;
            lcMethod.SetItem(&lvi);

            strTmp.Format(_T("%u"), (DWORD)plsSettings->m_lpCMethods[i].m_nPasses);
            lvi.iSubItem    = 2;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lcMethod.SetItem(&lvi);
            strTmp.ReleaseBuffer();
        }

        CRect rList, rHeader;
        CSize size = lcMethod.ApproximateViewRect();

        lcMethod.GetClientRect(&rList);
        lcMethod.GetHeaderCtrl()->GetClientRect(&rHeader);

        if (size.cy > (rList.Height() + rHeader.Height()))
            lcMethod.SetColumnWidth(1, iColumnWidths[1] - GetSystemMetrics(SM_CXVSCROLL));
        else
            lcMethod.SetColumnWidth(1, iColumnWidths[1]);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    lcMethod.SetRedraw(TRUE);
}
static int SelectMethod(CListCtrl& lcMethod, BYTE nMethodID, CString& str)
{
    try
    {
        LVFINDINFO lvfi;
        ZeroMemory(&lvfi, sizeof(LVFINDINFO));

        lvfi.flags  = LVFI_PARAM;
        lvfi.lParam = (LPARAM)nMethodID;

        int iItem = lcMethod.FindItem(&lvfi);

        if (iItem != -1)
        {
            lcMethod.SetItemState(iItem, LVIS_SELECTED, LVIS_SELECTED);
            FormatSelectedField(str,
                                lcMethod.GetItemText(iItem, 1),
                                lcMethod.GetItemText(iItem, 2));
            return iItem;
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return -1;
}

static BOOL DeleteMethod(LibrarySettings *plsSettings, BYTE nSelectedMethodID)
{
    if (AfxMessageBox(IDS_METHOD_DELETE, MB_ICONQUESTION | MB_YESNO, 0) == IDYES)
    {
        try
        {
            LPMETHOD lpcmNew = 0;

            if (plsSettings->m_nCMethods > 1)
            {
                lpcmNew = new METHOD[plsSettings->m_nCMethods - 1];

                for (BYTE i = 0, j = 0; i < plsSettings->m_nCMethods; i++)
                {
                    if (plsSettings->m_lpCMethods[i].m_nMethodID != nSelectedMethodID)
                        lpcmNew[j++] = plsSettings->m_lpCMethods[i];
                }
            }

            plsSettings->m_nCMethods--;

            delete[] plsSettings->m_lpCMethods;
            plsSettings->m_lpCMethods = lpcmNew;

            return TRUE;
        }
        catch (CException *e)
        {
            e->ReportError(MB_ICONERROR);
            e->Delete();
        }
    }

    return FALSE;
}

// using the nRandomPasses parameter is just a silly hack, cause I wanted to be able to use
// the same code with both property pages...
static BOOL EditMethod(LibrarySettings *plsSettings, BYTE nSelectedMethodID, WORD& nRandomPasses)
{
    if (!bitSet(nSelectedMethodID, BUILTIN_METHOD_ID))
    {
        try
        {
            LPMETHOD lpcm = 0;

            // find the selected method
            for (BYTE i = 0; i < plsSettings->m_nCMethods; i++)
            {
                if (plsSettings->m_lpCMethods[i].m_nMethodID == nSelectedMethodID)
                {
                    lpcm = &plsSettings->m_lpCMethods[i];
                    break;
                }
            }

            if (lpcm == 0)
                return FALSE;

            CCustomMethodEdit cme;
            cme.LoadCustomMethod(lpcm);

            if (cme.DoModal() == IDOK)
            {
                cme.FillCustomMethod(lpcm);
                return TRUE;
            }
        }
        catch (CException *e)
        {
            e->ReportError(MB_ICONERROR);
            e->Delete();
        }
    }
    else if (nSelectedMethodID == RANDOM_METHOD_ID)
    {
        // edit the number of passes
        CPassEditDlg ped;
        ped.m_uPasses = nRandomPasses;

        if (ped.DoModal() == IDOK)
        {
            nRandomPasses = (WORD)ped.m_uPasses;
            return TRUE;
        }
    }

    return FALSE;
}

static BOOL NewMethod(LibrarySettings *plsSettings)
{
    try
    {
        METHOD cmNew;
        ZeroMemory(&cmNew, sizeof(METHOD));

        CCustomMethodEdit cme;
        cme.LoadCustomMethod(&cmNew);

        if (cme.DoModal() == IDOK)
        {
            BYTE i;
            cme.FillCustomMethod(&cmNew);

            // assign a method ID
            cmNew.m_nMethodID = (1 | CUSTOM_METHOD_ID);

            for (i = 0; i < plsSettings->m_nCMethods; i++)
            {
                if (plsSettings->m_lpCMethods[i].m_nMethodID == cmNew.m_nMethodID)
                {
                    i = 0;
                    cmNew.m_nMethodID++;
                }
            }

            LPMETHOD lpcm = new METHOD[plsSettings->m_nCMethods + 1];

            for (i = 0; i < plsSettings->m_nCMethods; i++)
                lpcm[i] = plsSettings->m_lpCMethods[i];

            lpcm[plsSettings->m_nCMethods] = cmNew;
            plsSettings->m_nCMethods++;

            if (plsSettings->m_lpCMethods)
                delete[] plsSettings->m_lpCMethods;

            plsSettings->m_lpCMethods = lpcm;

            return TRUE;
        }
    }
    catch (CException *e)
    {
        e->ReportError(MB_ICONERROR);
        e->Delete();
    }

    return FALSE;
}

/////////////////////////////////////////////////////////////////////////////
// COptionsForFiles property page

COptionsForFiles::COptionsForFiles() : CPropertyPage(IDD_PAGE_FILES),  m_pData(0)
{    
	m_pData = new COptionsForFilesData();
    m_psp.dwFlags &= (~PSP_HASHELP);
}
COptionsForFiles* 
COptionsForFiles::create()
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return new COptionsForFiles();
}
BOOL 
COptionsForFiles::OnSetActive()
{
	
	
	return CPropertyPage::OnSetActive();
}
COptionsForFiles::~COptionsForFiles()
{
	try
	{
		delete m_pData;
	}
	catch (...) {
	}
}

void COptionsForFiles::DoDataExchange(CDataExchange* pDX)
{
    CPropertyPage::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(COptionsForFiles)
    
	DDX_Control(pDX, IDC_LIST_METHOD,GetMethodList());
    DDX_Text(pDX, IDC_STATIC_SELECTED, GetSelectedStr());
	DDX_Check(pDX, IDC_CHECK_FILECLUSTERTIPS, GetFileClusterTips());
	DDX_Check(pDX, IDC_CHECK_FILENAMES, GetFileNames());
    DDX_Check(pDX, IDC_CHECK_ALTERNATESTREAMS, GetFileAltDataStreams());
	
	//}}AFX_DATA_MAP 
}


BEGIN_MESSAGE_MAP(COptionsForFiles, CPropertyPage)
    //{{AFX_MSG_MAP(COptionsForFiles)
    ON_BN_CLICKED(IDC_BUTTON_DELETE, OnButtonDelete)
    ON_BN_CLICKED(IDC_BUTTON_EDIT, OnButtonEdit)
    ON_BN_CLICKED(IDC_BUTTON_NEW, OnButtonNew)
    ON_NOTIFY(LVN_ITEMCHANGED, IDC_LIST_METHOD, OnItemchangedListMethod)
	ON_NOTIFY(NM_DBLCLK, IDC_LIST_METHOD, OnDblclkListMethod)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// COptionsForFreeSpace property page

COptionsForFreeSpace::COptionsForFreeSpace() :
CPropertyPage(IDD_PAGE_FREESPACE /*COptionsForFreeSpace::IDD*/),
m_plsSettings(0),
m_ppgFiles(0),
m_nSelectedMethodID(DEFAULT_UDS_METHOD_ID),
m_nSelectedMethod(-1)
{
    //{{AFX_DATA_INIT(COptionsForFreeSpace)
    m_bClusterTips = FALSE;
    m_bDirectoryEntries = FALSE;
    m_bFreeSpace = FALSE;
    m_strSelected = _T("");
    //}}AFX_DATA_INIT

    m_psp.dwFlags &= (~PSP_HASHELP);
}

COptionsForFreeSpace::~COptionsForFreeSpace()
{
}

void COptionsForFreeSpace::DoDataExchange(CDataExchange* pDX)
{
    CPropertyPage::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(COptionsForFreeSpace)
    DDX_Control(pDX, IDC_LIST_METHOD, m_lcMethod);
    DDX_Check(pDX, IDC_CHECK_CLUSTERTIPS, m_bClusterTips);
    DDX_Check(pDX, IDC_CHECK_DIRECTORYENTRIES, m_bDirectoryEntries);
    DDX_Check(pDX, IDC_CHECK_FREESPACE, m_bFreeSpace);
    DDX_Text(pDX, IDC_STATIC_SELECTED, m_strSelected);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(COptionsForFreeSpace, CPropertyPage)
    //{{AFX_MSG_MAP(COptionsForFreeSpace)
    ON_BN_CLICKED(IDC_BUTTON_EDIT, OnButtonEdit)
    ON_BN_CLICKED(IDC_BUTTON_NEW, OnButtonNew)
    ON_BN_CLICKED(IDC_BUTTON_DELETE, OnButtonDelete)
    ON_NOTIFY(LVN_ITEMCHANGED, IDC_LIST_METHOD, OnItemchangedListMethod)
	ON_NOTIFY(NM_DBLCLK, IDC_LIST_METHOD, OnDblclkListMethod)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()


BOOL COptionsForFiles::OnInitDialog()
{
    try
    {
		
        CPropertyPage::OnInitDialog();

         //m_nSelectedMethodID = m_plsSettings->m_nFileMethodID;
		SetSelectedMethodId(GetLibSettings()->m_nFileMethodID);

        // setup list
		 //CreateList(m_lcMethod);
		CreateList(GetMethodList());
        
        // add methods to the list
        UpdateList();
        // select the correct method
         //m_nSelectedMethod = SelectMethod(m_lcMethod, m_nSelectedMethodID, m_strSelected);
		SetSelectedMethod(SelectMethod(GetMethodList(), GetSelectedMethodId(), GetSelectedStr()));
         //m_bFileClusterTips = bitSet(m_plsSettings->m_uItems, fileClusterTips);
		SetFileClusterTips(bitSet(GetLibSettings()->m_uItems, fileClusterTips));

        if (!IsWindowsNT())
        {
            // file names can be deselected, alternate data streams aren't supported
             //m_bFileNames = bitSet(m_plsSettings->m_uItems, fileNames);
			SetFileNames(bitSet(GetLibSettings()->m_uItems, fileNames));

             //m_bFileAlternateDataStreams = FALSE;
			SetFileAltDataStreams(FALSE);
            GetDlgItem(IDC_CHECK_ALTERNATESTREAMS)->ShowWindow(SW_HIDE);
        }
        else
        {
            // alternate data streams are supported, file names will always be cleared
             //m_bFileAlternateDataStreams = bitSet(m_plsSettings->m_uItems, fileAlternateStreams);
			SetFileAltDataStreams(bitSet(GetLibSettings()->m_uItems, fileAlternateStreams));

             //m_bFileNames = TRUE;
			SetFileNames(TRUE);
            GetDlgItem(IDC_CHECK_FILENAMES)->EnableWindow(FALSE);
        }

         //EnableButtons(m_nSelectedMethodID);
		EnableButtons(GetSelectedMethodId());

        UpdateData(FALSE);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void COptionsForFiles::OnButtonDelete()
{
	/*if (DeleteMethod(m_plsSettings, m_nSelectedMethodID))
	{
	UpdateList();
	m_ppgFreeSpace->UpdateList();
	}*/
	if (DeleteMethod(GetLibSettings(), GetSelectedMethodId()))
    {
        UpdateList();
        GetFreeSpaceOpt()->UpdateList();
    }
}

void COptionsForFiles::OnButtonEdit()
{

	if (EditMethod(GetLibSettings(),GetSelectedMethodId(), GetLibSettings()->m_nFileRandom))
    {
        UpdateList();
        SelectMethod(GetMethodList(), GetSelectedMethodId(), GetSelectedStr());

        if (IsWindow(GetFreeSpaceOpt()->GetSafeHwnd()))
        {
            GetFreeSpaceOpt()->UpdateList();
            SelectMethod(GetFreeSpaceOpt()->m_lcMethod, GetFreeSpaceOpt()->m_nSelectedMethodID,
                         GetFreeSpaceOpt()->m_strSelected);
            GetFreeSpaceOpt()->UpdateData();
        }
    }
}

void COptionsForFiles::OnButtonNew()
{
	/*if (NewMethod(m_plsSettings))
	{
	UpdateList();
	m_ppgFreeSpace->UpdateList();
	}*/
	if (NewMethod(GetLibSettings()))
    {
        UpdateList();
        GetFreeSpaceOpt()->UpdateList();
    }
}

void COptionsForFiles::OnOK()
{
    try
    {
        UpdateData(TRUE);
        GetLibSettings()->m_nFileMethodID = GetSelectedMethodId();
        unsetBit(GetLibSettings()->m_uItems, fileClusterTips | fileNames | fileAlternateStreams);

        if (GetFileClusterTips())
            setBit(GetLibSettings()->m_uItems, fileClusterTips);
        if (GetFileNames())
            setBit(GetLibSettings()->m_uItems, fileNames);
        if (GetFileAltDataStreams())
            setBit(GetLibSettings()->m_uItems, fileAlternateStreams);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    CPropertyPage::OnOK();

}

void COptionsForFiles::UpdateList()
{
    if (IsWindow(GetSafeHwnd()))
    {
        UpdateMethodList(GetMethodList(), GetLibSettings(), GetLibSettings()->m_nFileRandom);

        if (GetSelectedMethod() >= GetMethodList().GetItemCount())
            GetMethodList().SetItemState(0, LVIS_SELECTED, LVIS_SELECTED);
    }
}

void COptionsForFiles::OnItemchangedListMethod(NMHDR* pNMHDR, LRESULT* pResult)
{
    try
    {
        NM_LISTVIEW* pNMListView = (NM_LISTVIEW*)pNMHDR;

        if (pNMListView->uNewState != pNMListView->uOldState &&
            pNMListView->uNewState & LVIS_SELECTED)
        {
            LVITEM  lvi;

            ZeroMemory(&lvi, sizeof(LVITEM));
            lvi.mask  = LVIF_PARAM;
            lvi.iItem = pNMListView->iItem;

            if (GetMethodList().GetItem(&lvi))
            {
                SetSelectedMethodId((BYTE)lvi.lParam);
                SetSelectedMethod(lvi.iItem);
            }

            // enable / disable buttons
            EnableButtons(GetSelectedMethodId());

            // selected
            FormatSelectedField(GetSelectedStr(),
                                GetMethodList().GetItemText(pNMListView->iItem, 1),
                                GetMethodList().GetItemText(pNMListView->iItem, 2));
            UpdateData(FALSE);
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    *pResult = 0;
}

void COptionsForFiles::EnableButtons(BYTE nMethodID)
{
    GetDlgItem(IDC_BUTTON_EDIT)->EnableWindow(!bitSet(nMethodID, BUILTIN_METHOD_ID) ||
                                              nMethodID == RANDOM_METHOD_ID);
    GetDlgItem(IDC_BUTTON_DELETE)->EnableWindow(!bitSet(nMethodID, BUILTIN_METHOD_ID));
}

void COptionsForFiles::OnDblclkListMethod(NMHDR* /*pNMHDR*/, LRESULT* pResult)
{
	OnButtonEdit();
	*pResult = 0;
}


BOOL COptionsForFreeSpace::OnInitDialog()
{
    try
    {
        CPropertyPage::OnInitDialog();

        m_nSelectedMethodID = m_plsSettings->m_nUDSMethodID;

        // setup list
        CreateList(m_lcMethod);
        // add methods to the list
        UpdateList();
        // select the correct method
        SelectMethod(m_lcMethod, m_nSelectedMethodID, m_strSelected);

        m_bFreeSpace        = bitSet(m_plsSettings->m_uItems, diskFreeSpace);
        m_bClusterTips      = bitSet(m_plsSettings->m_uItems, diskClusterTips);
        m_bDirectoryEntries = bitSet(m_plsSettings->m_uItems, diskDirEntries);

        EnableButtons(m_nSelectedMethodID);

        if (IsWindowsNT())
        {
            CString strTmp;
            GetDlgItem(IDC_CHECK_FREESPACE)->GetWindowText(strTmp);
            strTmp += " (and Master File Table Records)";
            GetDlgItem(IDC_CHECK_FREESPACE)->SetWindowText(strTmp);
        }

        UpdateData(FALSE);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void COptionsForFreeSpace::OnButtonEdit()
{
    if (EditMethod(m_plsSettings, m_nSelectedMethodID, m_plsSettings->m_nUDSRandom))
    {
        UpdateList();
        SelectMethod(m_lcMethod, m_nSelectedMethodID, m_strSelected);

        if (IsWindow(m_ppgFiles->GetSafeHwnd()))
        {
            m_ppgFiles->UpdateList();
            SelectMethod(m_ppgFiles->GetMethodList(), m_ppgFiles->GetSelectedMethodId(), m_ppgFiles->GetSelectedStr());
            m_ppgFiles->UpdateData();
        }
    }
}

void COptionsForFreeSpace::OnButtonNew()
{
    if (NewMethod(m_plsSettings))
    {
        UpdateList();
        m_ppgFiles->UpdateList();
    }
}

void COptionsForFreeSpace::OnButtonDelete()
{
    if (DeleteMethod(m_plsSettings, m_nSelectedMethodID))
    {
        UpdateList();
        m_ppgFiles->UpdateList();
    }
}

void COptionsForFreeSpace::OnOK()
{
    try
    {
        UpdateData(TRUE);

        m_plsSettings->m_nUDSMethodID       = m_nSelectedMethodID;
        unsetBit(m_plsSettings->m_uItems, diskFreeSpace | diskClusterTips | diskDirEntries);

        if (m_bFreeSpace)
            setBit(m_plsSettings->m_uItems, diskFreeSpace);
        if (m_bClusterTips)
            setBit(m_plsSettings->m_uItems, diskClusterTips);
        if (m_bDirectoryEntries)
            setBit(m_plsSettings->m_uItems, diskDirEntries);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    CPropertyPage::OnOK();
}

void COptionsForFreeSpace::UpdateList()
{
    if (IsWindow(GetSafeHwnd()))
    {
        UpdateFreeSpaceMethodList(m_lcMethod, m_plsSettings, m_plsSettings->m_nUDSRandom);

        if (m_nSelectedMethod >= m_lcMethod.GetItemCount())
            m_lcMethod.SetItemState(0, LVIS_SELECTED, LVIS_SELECTED);
    }
}

void COptionsForFreeSpace::EnableButtons(BYTE nMethodID)
{
    GetDlgItem(IDC_BUTTON_EDIT)->EnableWindow(!bitSet(nMethodID, BUILTIN_METHOD_ID) ||
                                              nMethodID == RANDOM_METHOD_ID);
    GetDlgItem(IDC_BUTTON_DELETE)->EnableWindow(!bitSet(nMethodID, BUILTIN_METHOD_ID));
}

void COptionsForFreeSpace::OnItemchangedListMethod(NMHDR* pNMHDR, LRESULT* pResult)
{
    try
    {
        NM_LISTVIEW* pNMListView = (NM_LISTVIEW*)pNMHDR;

        if (pNMListView->uNewState != pNMListView->uOldState &&
            pNMListView->uNewState & LVIS_SELECTED)
        {
            LVITEM  lvi;

            ZeroMemory(&lvi, sizeof(LVITEM));
            lvi.mask  = LVIF_PARAM;
            lvi.iItem = pNMListView->iItem;

            if (m_lcMethod.GetItem(&lvi))
            {
                m_nSelectedMethodID = (BYTE)lvi.lParam;
                m_nSelectedMethod = lvi.iItem;
            }

            // enable / disable buttons
            EnableButtons(m_nSelectedMethodID);

            // selected
            FormatSelectedField(m_strSelected,
                                m_lcMethod.GetItemText(pNMListView->iItem, 1),
                                m_lcMethod.GetItemText(pNMListView->iItem, 2));
            UpdateData(FALSE);
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    *pResult = 0;
}

void COptionsForFreeSpace::OnDblclkListMethod(NMHDR* /*pNMHDR*/, LRESULT* pResult)
{
	OnButtonEdit();
	*pResult = 0;
}