// CustomMethodEdit.cpp
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
#include "eraser.h"
#include "EraserDll.h"
#include "commctrl.h"
#include "CustomMethodEdit.h"

#ifdef DMARS
	#define LVCOLUMN _LV_COLUMNA
	#define LVITEM _LV_ITEMA
#endif
#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

static const int iColumnCount = 2;

static const LPTSTR szColumnNames[] =
{
    _T("#"),
    _T("Data")
};

static int iColumnWidths[] =
{
    40,
    -1
};

static void CreateList(CListCtrl& lcMethod)
{
    CRect rClient;
    lcMethod.GetClientRect(&rClient);

    iColumnWidths[1] = rClient.Width() -
                       iColumnWidths[0] -
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
#ifndef DMARS
    lcMethod.SetExtendedStyle(LVS_EX_HEADERDRAGDROP | LVS_EX_FULLROWSELECT | LVS_EX_GRIDLINES);
#endif
}

/////////////////////////////////////////////////////////////////////////////
// CCustomMethodEdit dialog


CCustomMethodEdit::CCustomMethodEdit(CWnd* pParent /*=NULL*/) :
CDialog(CCustomMethodEdit::IDD, pParent),
m_nSelectedPass((WORD)-1)
{
    //{{AFX_DATA_INIT(CCustomMethodEdit)
    m_bByte2 = FALSE;
    m_bByte3 = FALSE;
    m_strDescription = _T("");
    m_bShuffle = FALSE;
    //}}AFX_DATA_INIT

    m_aPasses.RemoveAll();
}


void CCustomMethodEdit::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CCustomMethodEdit)
    DDX_Control(pDX, IDC_EDIT_BYTE2, m_editByte2);
    DDX_Control(pDX, IDC_EDIT_BYTE3, m_editByte3);
    DDX_Control(pDX, IDC_EDIT_BYTE1, m_editByte1);
    DDX_Control(pDX, IDC_LIST_PASSES, m_lcPasses);
    DDX_Check(pDX, IDC_CHECK_BYTE2, m_bByte2);
    DDX_Check(pDX, IDC_CHECK_BYTE3, m_bByte3);
    DDX_Text(pDX, IDC_EDIT_DESCRIPTION, m_strDescription);
    DDV_MaxChars(pDX, m_strDescription, (DESCRIPTION_SIZE - 1));
    DDX_Check(pDX, IDC_CHECK_SHUFFLE, m_bShuffle);
    //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CCustomMethodEdit, CDialog)
    //{{AFX_MSG_MAP(CCustomMethodEdit)
    ON_BN_CLICKED(IDC_BUTTON_ADD, OnButtonAdd)
    ON_BN_CLICKED(IDC_BUTTON_COPY, OnButtonCopy)
    ON_BN_CLICKED(IDC_BUTTON_DELETE, OnButtonDelete)
    ON_BN_CLICKED(IDC_BUTTON_DOWN, OnButtonDown)
    ON_BN_CLICKED(IDC_BUTTON_UP, OnButtonUp)
    ON_BN_CLICKED(IDC_CHECK_BYTE2, OnCheckByte2)
    ON_BN_CLICKED(IDC_CHECK_BYTE3, OnCheckByte3)
    ON_BN_CLICKED(IDC_RADIO_PATTERN, OnRadioPattern)
    ON_BN_CLICKED(IDC_RADIO_PSEUDORANDOM, OnRadioPseudorandom)
    ON_NOTIFY(LVN_ITEMCHANGED, IDC_LIST_PASSES, OnItemchangedListPasses)
    ON_EN_CHANGE(IDC_EDIT_DESCRIPTION, OnChangeEditDescription)
    ON_EN_CHANGE(IDC_EDIT_BYTE1, OnChangeEditByte1)
    ON_EN_CHANGE(IDC_EDIT_BYTE2, OnChangeEditByte2)
    ON_EN_CHANGE(IDC_EDIT_BYTE3, OnChangeEditByte3)
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CCustomMethodEdit message handlers

BOOL CCustomMethodEdit::OnInitDialog()
{
    CDialog::OnInitDialog();

    m_editByte1.SetByte(0);
    m_editByte2.SetByte(0);
    m_editByte3.SetByte(0);

    CreateList(m_lcPasses);
    UpdateList();

    return TRUE;  // return TRUE unless you set the focus to a control
                  // EXCEPTION: OCX Property Pages should return FALSE
}

void CCustomMethodEdit::OnButtonAdd()
{
    if (m_aPasses.GetSize() <= PASSES_MAX)
    {
        PASS passNew;
        setPassOne(passNew, 0);

        // array index can be used as list index as the list won't be sorted
        INT_PTR iItem = m_aPasses.Add(passNew);

        UpdateList();
        m_lcPasses.SetItemState(static_cast<int>(iItem), LVIS_SELECTED, LVIS_SELECTED);
    }
}

void CCustomMethodEdit::OnButtonCopy()
{
    if (m_aPasses.GetSize() <= PASSES_MAX &&
        m_nSelectedPass < m_aPasses.GetSize())
    {
        PASS passTmp = m_aPasses[m_nSelectedPass];
        m_aPasses.InsertAt(m_nSelectedPass + 1, passTmp);
        UpdateList();
    }
}

void CCustomMethodEdit::OnButtonDelete()
{
    if (m_nSelectedPass < m_aPasses.GetSize())
    {
        m_aPasses.RemoveAt(m_nSelectedPass);
        UpdateList();
    }
}

void CCustomMethodEdit::OnButtonDown()
{
    if (m_nSelectedPass < (m_aPasses.GetSize() - 1))
    {
        PASS passTmp = m_aPasses[m_nSelectedPass + 1];
        m_aPasses.SetAt(m_nSelectedPass + 1, m_aPasses[m_nSelectedPass]);
        m_aPasses.SetAt(m_nSelectedPass, passTmp);

        UpdateList();
        m_lcPasses.SetItemState(m_nSelectedPass + 1, LVIS_SELECTED, LVIS_SELECTED);
    }
}

void CCustomMethodEdit::OnButtonUp()
{
    if (m_nSelectedPass < m_aPasses.GetSize() &&
        m_nSelectedPass > 0)
    {
        PASS passTmp = m_aPasses[m_nSelectedPass - 1];
        m_aPasses.SetAt(m_nSelectedPass - 1, m_aPasses[m_nSelectedPass]);
        m_aPasses.SetAt(m_nSelectedPass, passTmp);

        UpdateList();
        m_lcPasses.SetItemState(m_nSelectedPass - 1, LVIS_SELECTED, LVIS_SELECTED);
    }
}

void CCustomMethodEdit::OnCheckByte2()
{
    try
    {
        UpdateData(TRUE);

        if (!m_bByte2)
            m_bByte3 = FALSE;

        GetDlgItem(IDC_EDIT_BYTE2)->EnableWindow(m_bByte2);
        GetDlgItem(IDC_CHECK_BYTE3)->EnableWindow(m_bByte2);
        GetDlgItem(IDC_EDIT_BYTE3)->EnableWindow(m_bByte2 && m_bByte3);

        UpdateData(FALSE);

        SaveSelectedPass();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

void CCustomMethodEdit::OnCheckByte3()
{
    try
    {
        SaveSelectedPass();
        GetDlgItem(IDC_EDIT_BYTE3)->EnableWindow(m_bByte3);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

void CCustomMethodEdit::OnOK()
{
    if (m_aPasses.GetSize() == 0)
    {
        if (AfxMessageBox(IDS_METHOD_NOPASSES, MB_ICONWARNING | MB_YESNO, 0) == IDYES)
            CDialog::OnCancel();

        return;
    }

    CDialog::OnOK();
}

void CCustomMethodEdit::OnRadioPattern()
{
    try
    {
        CButton *pRadioPattern = (CButton*)GetDlgItem(IDC_RADIO_PATTERN);
        CButton *pRadioRandom  = (CButton*)GetDlgItem(IDC_RADIO_PSEUDORANDOM);

        pRadioPattern->SetCheck(1);
        pRadioRandom->SetCheck(0);
        EnablePattern(TRUE);

        SaveSelectedPass();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

void CCustomMethodEdit::OnRadioPseudorandom()
{
    try
    {
        CButton *pRadioPattern = (CButton*)GetDlgItem(IDC_RADIO_PATTERN);
        CButton *pRadioRandom  = (CButton*)GetDlgItem(IDC_RADIO_PSEUDORANDOM);

        pRadioPattern->SetCheck(0);
        pRadioRandom->SetCheck(1);
        EnablePattern(FALSE);

        SaveSelectedPass();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

BOOL CCustomMethodEdit::FillCustomMethod(LPMETHOD lpcm)
{
    try
    {
        if (m_aPasses.GetSize() == 0)
            return FALSE;

        lstrcpyn(lpcm->m_szDescription, (LPCTSTR)m_strDescription, DESCRIPTION_SIZE);
        lpcm->m_bShuffle = (BYTE)((m_aPasses.GetSize() < 2) ? 0 : m_bShuffle);

        if (lpcm->m_lpPasses)
        {
            delete[] lpcm->m_lpPasses;
            lpcm->m_lpPasses = 0;
        }

        lpcm->m_nPasses  = (WORD)m_aPasses.GetSize();
        lpcm->m_lpPasses = new PASS[lpcm->m_nPasses];

        for (WORD i = 0; i < lpcm->m_nPasses; i++)
            lpcm->m_lpPasses[i] = m_aPasses[i];

        return TRUE;
    }
    catch (CException *e)
    {
        e->ReportError(MB_ICONERROR);
        e->Delete();
    }

    return FALSE;
}

BOOL CCustomMethodEdit::LoadCustomMethod(LPMETHOD lpcm)
{
    try
    {
        m_strDescription = lpcm->m_szDescription;
        m_bShuffle       = lpcm->m_bShuffle;

        for (WORD i = 0; i < lpcm->m_nPasses; i++)
            m_aPasses.Add(lpcm->m_lpPasses[i]);

        return TRUE;
    }
    catch (CException *e)
    {
        e->ReportError(MB_ICONERROR);
        e->Delete();
    }

    return FALSE;
}

inline static CString GetByteStr(BYTE byte)
{
    CString str = _T("00000000");

	for (BYTE j = 0; j < 8; j++) {
        str.SetAt(7 - j, (byte & (1 << j)) ? '1' : '0');
	}

    return str;
}

inline static void GetPassStr(CString& str, const PASS& pass)
{
    if (pass.byte1 == RND_DATA)
        str = _T("Pseudorandom Data");
    else
    {
        str = _T("Pattern (") + GetByteStr((BYTE)pass.byte1);

        if (pass.bytes >= 2)
            str += _T(" ") + GetByteStr((BYTE)pass.byte2);
        if (pass.bytes == 3)
            str += _T(" ") + GetByteStr((BYTE)pass.byte3);

        str += _T(")");
    }
}

void CCustomMethodEdit::UpdateList()
{
    m_lcPasses.SetRedraw(FALSE);

    try
    {
        m_lcPasses.DeleteAllItems();

        CString         strTmp;
        PASS            pass;
        LV_ITEM         lvi;
        ZeroMemory(&lvi, sizeof(LV_ITEM));

        // built-in
        for (WORD i = 0; i < m_aPasses.GetSize(); i++)
        {
            strTmp.Format(_T("%u"), (DWORD)i + 1);
            lvi.mask        = LVIF_TEXT | LVIF_PARAM;
            lvi.lParam      = (LPARAM)i;
            lvi.iItem       = i;
            lvi.iSubItem    = 0;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            lvi.iItem       = m_lcPasses.InsertItem(&lvi);
            strTmp.ReleaseBuffer();

            pass = m_aPasses[i];
            GetPassStr(strTmp, pass);

            lvi.mask        = LVIF_TEXT;
            lvi.iSubItem    = 1;
            lvi.pszText     = strTmp.GetBuffer(strTmp.GetLength());
            m_lcPasses.SetItem(&lvi);
            strTmp.ReleaseBuffer();
        }

        CRect rList, rHeader;
#ifndef DMARS
        CSize size = m_lcPasses.ApproximateViewRect();
#endif

        m_lcPasses.GetClientRect(&rList);
#ifndef DMARS
        m_lcPasses.GetHeaderCtrl()->GetClientRect(&rHeader);
#endif

#ifndef DMARS
        if (size.cy > (rList.Height() + rHeader.Height()))
            m_lcPasses.SetColumnWidth(1, iColumnWidths[1] - GetSystemMetrics(SM_CXVSCROLL));
        else
#endif
            m_lcPasses.SetColumnWidth(1, iColumnWidths[1]);

        if (m_aPasses.GetSize() == 0)
        {
            GetDlgItem(IDC_RADIO_PATTERN)->EnableWindow(FALSE);
            GetDlgItem(IDC_RADIO_PSEUDORANDOM)->EnableWindow(FALSE);
            EnablePattern(FALSE);
        }
        else
        {
            if (m_nSelectedPass >= m_aPasses.GetSize())
                m_nSelectedPass = 0;

            m_lcPasses.EnsureVisible(m_nSelectedPass, FALSE);
            m_lcPasses.SetItemState(m_nSelectedPass, LVIS_SELECTED, LVIS_SELECTED);
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    m_lcPasses.SetRedraw(TRUE);
}

void CCustomMethodEdit::OnItemchangedListPasses(NMHDR* pNMHDR, LRESULT* pResult)
{
    try
    {
        NM_LISTVIEW* pNMListView = (NM_LISTVIEW*)pNMHDR;

        if (pNMListView->uNewState != pNMListView->uOldState &&
            pNMListView->uNewState & LVIS_SELECTED)
        {
            CButton *pRadioPattern = (CButton*)GetDlgItem(IDC_RADIO_PATTERN);
            CButton *pRadioRandom  = (CButton*)GetDlgItem(IDC_RADIO_PSEUDORANDOM);
            PASS    passTmp;
            CString strTmp;

            // get new selected item
            LVITEM  lvi;
            ZeroMemory(&lvi, sizeof(LVITEM));

            lvi.mask  = LVIF_PARAM;
            lvi.iItem = pNMListView->iItem;

            if (m_lcPasses.GetItem(&lvi))
            {
                GetDlgItem(IDC_RADIO_PATTERN)->EnableWindow(TRUE);
                GetDlgItem(IDC_RADIO_PSEUDORANDOM)->EnableWindow(TRUE);

                m_nSelectedPass = (WORD)lvi.lParam;

                passTmp = m_aPasses[m_nSelectedPass];

                // set buttons and pass data
                if (passTmp.byte1 == RND_DATA)
                {
                    pRadioRandom->SetCheck(1);
                    pRadioPattern->SetCheck(0);

                    m_bByte2 = FALSE;
                    m_bByte3 = FALSE;

                    UpdateData(FALSE);

                    m_editByte1.SetByte(0);
                    m_editByte2.SetByte(0);
                    m_editByte3.SetByte(0);

                    EnablePattern(FALSE);
                }
                else
                {
                    pRadioPattern->SetCheck(1);
                    pRadioRandom->SetCheck(0);

                    m_bByte2 = (passTmp.bytes >= 2);
                    m_bByte3 = (passTmp.bytes == 3);

                    UpdateData(FALSE);

                    m_editByte1.SetByte((BYTE)passTmp.byte1);
                    m_editByte2.SetByte(((m_bByte2) ? (BYTE)passTmp.byte2 : (BYTE)0));
                    m_editByte3.SetByte(((m_bByte3) ? (BYTE)passTmp.byte3 : (BYTE)0));

                    EnablePattern(TRUE);
                }
            }
        }
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    *pResult = 0;}

void CCustomMethodEdit::EnablePattern(BOOL bEnable)
{
    try
    {
        GetDlgItem(IDC_STATIC_BYTE1)->EnableWindow(bEnable);
        GetDlgItem(IDC_CHECK_BYTE2)->EnableWindow(bEnable);
        GetDlgItem(IDC_CHECK_BYTE3)->EnableWindow(bEnable && m_bByte2);

        GetDlgItem(IDC_EDIT_BYTE1)->EnableWindow(bEnable);
        GetDlgItem(IDC_EDIT_BYTE2)->EnableWindow(bEnable && m_bByte2);
        GetDlgItem(IDC_EDIT_BYTE3)->EnableWindow(bEnable && m_bByte3);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }
}

void CCustomMethodEdit::OnChangeEditDescription()
{
    UpdateData(TRUE);
}

void CCustomMethodEdit::SaveSelectedPass()
{
    if (m_nSelectedPass < m_aPasses.GetSize())
    {
        UpdateData(TRUE);

        // save possible changes to the selected pass
        CButton *pRadioPattern = (CButton*)GetDlgItem(IDC_RADIO_PATTERN);
        PASS    passTmp = m_aPasses[m_nSelectedPass];
        CString strTmp;

        if (pRadioPattern->GetCheck() == 1)
        {
            setPassOne(passTmp, 0);
            passTmp.bytes = 1;

            if (m_bByte2)
                passTmp.bytes++;
            if (m_bByte3)
                passTmp.bytes++;

            passTmp.byte1 = (WORD)m_editByte1.GetByte();

            if (passTmp.bytes >= 2)
                passTmp.byte2 = (WORD)m_editByte2.GetByte();
            if (passTmp.bytes == 3)
                passTmp.byte3 = (WORD)m_editByte3.GetByte();
        }
        else
        {
            setPassOne(passTmp, RND_DATA);
        }

        m_aPasses.SetAt(m_nSelectedPass, passTmp);

        GetPassStr(strTmp, passTmp);
        m_lcPasses.SetItemText((int)m_nSelectedPass, 1, strTmp);
    }
}

void CCustomMethodEdit::OnChangeEditByte1()
{
    SaveSelectedPass();
}

void CCustomMethodEdit::OnChangeEditByte2()
{
    SaveSelectedPass();
}

void CCustomMethodEdit::OnChangeEditByte3()
{
    SaveSelectedPass();
}