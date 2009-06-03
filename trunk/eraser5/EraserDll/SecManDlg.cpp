// SecManDlg.cpp : implementation file
//

#include "stdafx.h"
#include "EraserDll.h"
#include "SecManDlg.h"
#include ".\secmandlg.h"


// CSecManDlg dialog

IMPLEMENT_DYNAMIC(CSecManDlg, CDialog)
CSecManDlg::CSecManDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CSecManDlg::IDD, pParent)
	, m_Password(_T(""))
	, m_PasswordConfirm(_T(""))
	, m_mMode(CHECKUP)
{
}

CSecManDlg::~CSecManDlg()
{
}

void CSecManDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX, IDC_EDIT_SECMAN_PASSWD, m_Password);
	DDX_Text(pDX, IDC_EDIT_SECMAN_PASSWDCONFIRM, m_PasswordConfirm);
	
}


BEGIN_MESSAGE_MAP(CSecManDlg, CDialog)
END_MESSAGE_MAP()


// CSecManDlg message handlers

void CSecManDlg::OnOK()
{
	// TODO: Add your specialized code here and/or call the base class
	UpdateData();
	if (SETUP == m_mMode && m_Password != m_PasswordConfirm)
	{
		Clear();
		UpdateData(FALSE);
		this->MessageBox(CString(MAKEINTRESOURCE(IDS_PASSWDNOTMATCH)), _T("Error"), MB_OK | MB_ICONERROR);
		GetDlgItem(IDC_EDIT_SECMAN_PASSWD)->SetFocus();		
	}
	else if (m_Password.IsEmpty())
	{
		UpdateData(FALSE);
		MessageBox(CString(MAKEINTRESOURCE(IDS_PASSWDEMPTY)), _T("Error"), MB_OK | MB_ICONERROR);
		GetDlgItem(IDC_EDIT_SECMAN_PASSWD)->SetFocus();		
	}
	else
		CDialog::OnOK();
	
}

BOOL CSecManDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	if (SETUP == m_mMode)
	{
		GetDlgItem(IDC_EDIT_SECMAN_PASSWDCONFIRM)->ShowWindow(SW_SHOW);
		GetDlgItem(IDC_STATIC_CONFIRM)->ShowWindow(SW_SHOW);
	}

	return TRUE;  // return TRUE unless you set the focus to a control
	// EXCEPTION: OCX Property Pages should return FALSE
}