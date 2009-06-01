// ConfirmReplaceDlg.h
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

#if !defined(AFX_CONFIRMREPLACEDLG_H__E1E50051_2FC1_11D3_8212_00105AAF62C4__INCLUDED_)
#define AFX_CONFIRMREPLACEDLG_H__E1E50051_2FC1_11D3_8212_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CConfirmReplaceDlg dialog

class CConfirmReplaceDlg : public CDialog
{
// Construction
public:
    CConfirmReplaceDlg(CWnd* pParent = NULL);   // standard constructor
    void SetExisting(LPCTSTR sz)    { m_strExistingFile = sz; }
    void SetSource(LPCTSTR sz)      { m_strSourceFile = sz; }

    BOOL ApplyToAll()               { return m_bApplyToAll; }

// Dialog Data
    //{{AFX_DATA(CConfirmReplaceDlg)
    enum { IDD = IDD_DIALOG_REPLACE };
    CStatic m_stIconSource;
    CStatic m_stIconExisting;
    CString m_strSource;
    CString m_strExisting;
    CString m_strHeader;
    //}}AFX_DATA


// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CConfirmReplaceDlg)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:

    BOOL FormatInfo(LPCTSTR szFile, CString& strInfo);
    BOOL GetFileSizeAndModifiedData(LPCTSTR szFile, ULARGE_INTEGER& uiSize, COleDateTime& odtModified);

    CString m_strExistingFile;
    CString m_strSourceFile;

    BOOL m_bApplyToAll;

    // Generated message map functions
    //{{AFX_MSG(CConfirmReplaceDlg)
    virtual BOOL OnInitDialog();
	afx_msg void OnNoToAll();
	afx_msg void OnYesToAll();
	//}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_CONFIRMREPLACEDLG_H__E1E50051_2FC1_11D3_8212_00105AAF62C4__INCLUDED_)
