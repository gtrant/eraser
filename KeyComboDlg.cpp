// KeyComboDlg.cpp : implementation file
//

#include "stdafx.h"
#include "Eraser.h"
#include "KeyComboDlg.h"
#include <shared/key.h>


const LPCTSTR szAccelerKey = "Acceler";
// CKeyComboDlg dialog

IMPLEMENT_DYNAMIC(CKeyComboDlg, CDialog)
CKeyComboDlg::CKeyComboDlg(CString wValue /* ="" */, CString strValName /* ="" */, CWnd* pParent /*=NULL*/)
	: CDialog(CKeyComboDlg::IDD, pParent), m_strValue(wValue),m_strRegKey(strValName)
{
}

CKeyComboDlg::~CKeyComboDlg()
{
}

void CKeyComboDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDITTMP, m_eKey);
}


BEGIN_MESSAGE_MAP(CKeyComboDlg, CDialog)
	ON_BN_CLICKED(IDOK, OnBnClickedOk)
	ON_WM_ACTIVATE()
	ON_EN_CHANGE(IDC_EDITTMP, OnEnChangeEdittmp)
END_MESSAGE_MAP()


// CKeyComboDlg message handlers

BOOL CKeyComboDlg::OnInitDialog()
{
	CDialog::OnInitDialog();
	
	m_eKey.SetLimitText(1);
	m_eKey.SetWindowText(m_strValue);
	
	// TODO:  Add extra initialization here

	return TRUE;  // return TRUE unless you set the focus to a control
	// EXCEPTION: OCX Property Pages should return FALSE
}

void CKeyComboDlg::OnBnClickedOk()
{
	char ch[10];	
	m_eKey.GetLine(0,ch,1);
	m_strValue = ch;
	OnOK();
}

void CKeyComboDlg::OnActivate(UINT nState, CWnd* pWndOther, BOOL bMinimized)
{
	CDialog::OnActivate(nState, pWndOther, bMinimized);
	m_eKey.SetFocus();
	// TODO: Add your message handler code here
}


void CKeyComboDlg::OnEnChangeEdittmp()
{
	// TODO:  If this is a RICHEDIT control, the control will not
	// send this notification unless you override the CDialog::OnInitDialog()
	// function and call CRichEditCtrl().SetEventMask()
	// with the ENM_CHANGE flag ORed into the mask.

	// TODO:  Add your control notification handler code here
	//char cLine[10];
	char ch[10];
	m_eKey.GetLine(0,ch,1);
	CString strLine(ch);
	if (!strLine.IsEmpty())
	{
        CString strTmp(m_strRegKey.MakeUpper());
		strLine.MakeUpper();
		if (strTmp.Find(strLine[0]) ==-1 ) {
			m_eKey.Undo();
			m_eKey.SetWindowText("");
		}
	}

}
