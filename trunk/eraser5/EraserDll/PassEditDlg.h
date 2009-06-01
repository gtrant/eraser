// PassEditDlg.h
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

#if !defined(AFX_PASSEDITDLG_H__007F8270_BFB9_11D3_82A0_000000000000__INCLUDED_)
#define AFX_PASSEDITDLG_H__007F8270_BFB9_11D3_82A0_000000000000__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CPassEditDlg dialog

class ERASER_API CPassEditDlg : public CDialog
{
// Construction
public:
    CPassEditDlg(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
    //{{AFX_DATA(CPassEditDlg)
    enum { IDD = IDD_DIALOG_PASSEDIT };
    CSpinButtonCtrl m_spinPasses;
    DWORD    m_uPasses;
    //}}AFX_DATA


// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CPassEditDlg)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    bool m_bIgnoreChange;

    // Generated message map functions
    //{{AFX_MSG(CPassEditDlg)
    virtual BOOL OnInitDialog();
	afx_msg void OnChangeEditPasses();
	//}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_PASSEDITDLG_H__007F8270_BFB9_11D3_82A0_000000000000__INCLUDED_)
