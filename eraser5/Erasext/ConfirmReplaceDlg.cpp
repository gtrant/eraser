// ConfirmReplaceDlg.cpp
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
#include "resource.h"
#include "ConfirmReplaceDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif


const LPCTSTR szHeaderFormat    = TEXT("This folder already contains a file called '%s'.");
const LPCTSTR szKBInfoFormat    = TEXT("%I64u KB\r\nmodified on %s, %s");
const LPCTSTR szMBInfoFormat    = TEXT("%I64u MB\r\nmodified on %s, %s");

/////////////////////////////////////////////////////////////////////////////
// CConfirmReplaceDlg dialog


CConfirmReplaceDlg::CConfirmReplaceDlg(CWnd* pParent /*=NULL*/) :
CDialog(CConfirmReplaceDlg::IDD, pParent),
m_bApplyToAll(FALSE)
{
    //{{AFX_DATA_INIT(CConfirmReplaceDlg)
    m_strSource = _T("");
    m_strExisting = _T("");
    m_strHeader = _T("");
    //}}AFX_DATA_INIT
}


void CConfirmReplaceDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CConfirmReplaceDlg)
    DDX_Control(pDX, IDI_ICON_SOURCE, m_stIconSource);
    DDX_Control(pDX, IDI_ICON_EXISTING, m_stIconExisting);
    DDX_Text(pDX, IDC_STATIC_SOURCE, m_strSource);
    DDX_Text(pDX, IDC_STATIC_EXISTING, m_strExisting);
    DDX_Text(pDX, IDC_STATIC_HEADER, m_strHeader);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CConfirmReplaceDlg, CDialog)
    //{{AFX_MSG_MAP(CConfirmReplaceDlg)
	ON_BN_CLICKED(IDC_BUTTON_NOTOALL, OnNoToAll)
	ON_BN_CLICKED(IDC_BUTTON_YESTOALL, OnYesToAll)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CConfirmReplaceDlg message handlers

BOOL CConfirmReplaceDlg::OnInitDialog()
{
    CString     strFile;
    TCHAR       szFile[_MAX_FNAME];
    TCHAR       szExt[_MAX_EXT];
    SHFILEINFO  sfi;

    ZeroMemory(szFile, _MAX_FNAME);
    ZeroMemory(szExt, _MAX_EXT);
    ZeroMemory(&sfi, sizeof(SHFILEINFO));

    // filename to the header

    _tsplitpath((LPCTSTR)m_strExistingFile, NULL, NULL, szFile, szExt);

    strFile = szFile;
    strFile += szExt;

    m_strHeader.Format(szHeaderFormat, strFile);

    // size and modified date to information sections

    FormatInfo((LPCTSTR)m_strExistingFile, m_strExisting);
    FormatInfo((LPCTSTR)m_strSourceFile, m_strSource);

    CDialog::OnInitDialog();

    // icons

    SHGetFileInfo((LPCTSTR)m_strExistingFile,
                   0,
                   &sfi,
                   sizeof(SHFILEINFO),
                   SHGFI_ICON | SHGFI_LARGEICON);

    m_stIconExisting.SetIcon(sfi.hIcon);

    SHGetFileInfo((LPCTSTR)m_strSourceFile,
                   0,
                   &sfi,
                   sizeof(SHFILEINFO),
                   SHGFI_ICON | SHGFI_LARGEICON);

    m_stIconSource.SetIcon(sfi.hIcon);

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

BOOL CConfirmReplaceDlg::GetFileSizeAndModifiedData(LPCTSTR szFile, ULARGE_INTEGER& uiSize, COleDateTime& odtModified)
{
    WIN32_FIND_DATA findFileData;
    HANDLE          hFind = INVALID_HANDLE_VALUE;

    hFind = FindFirstFile((LPTSTR)szFile, &findFileData);

    if (hFind == INVALID_HANDLE_VALUE)
        return FALSE;

    VERIFY(FindClose(hFind));

    uiSize.LowPart  = findFileData.nFileSizeLow;
    uiSize.HighPart = findFileData.nFileSizeHigh;

    odtModified = COleDateTime(findFileData.ftLastWriteTime);

    return TRUE;
}

BOOL CConfirmReplaceDlg::FormatInfo(LPCTSTR szFile, CString& strInfo)
{
    ULARGE_INTEGER  uiSize;
    COleDateTime    odtModified;

    if (GetFileSizeAndModifiedData(szFile, uiSize, odtModified))
    {
        if (uiSize.QuadPart % 1024 > 0)
        {
            uiSize.QuadPart /= 1024;
            uiSize.QuadPart += 1;
        }
        else
            uiSize.QuadPart /= 1024;

        if (uiSize.QuadPart > 100000) // > 100 000 KB ~ 97,7 MB
        {
            strInfo.Format(szMBInfoFormat, uiSize.QuadPart / 1024,
                           odtModified.Format(VAR_DATEVALUEONLY),
                           odtModified.Format(VAR_TIMEVALUEONLY));
        }
        else
        {
            strInfo.Format(szKBInfoFormat, uiSize.QuadPart,
                           odtModified.Format(VAR_DATEVALUEONLY),
                           odtModified.Format(VAR_TIMEVALUEONLY));
        }

        return TRUE;
    }

    return FALSE;
}

void CConfirmReplaceDlg::OnNoToAll()
{
	m_bApplyToAll = TRUE;
    CDialog::OnCancel();
}

void CConfirmReplaceDlg::OnYesToAll()
{
	m_bApplyToAll = TRUE;
    CDialog::OnOK();
}
