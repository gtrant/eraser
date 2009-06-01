// ConfirmDialog.h
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

#ifndef CONFIRMDIALOG_H
#define CONFIRMDIALOG_H

/////////////////////////////////////////////////////////////////////////////
// CConfirmDialog dialog

class CConfirmDialog : public CDialog
{
// Construction
public:
    BOOL    m_bSingleFile;
    BOOL    m_bUseFiles;
    BOOL    m_bMove;
    CString m_strData;
    CString m_strTarget;

    CConfirmDialog(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
    //{{AFX_DATA(CConfirmDialog)
    enum { IDD = IDD_DIALOG_CONFIRM };
    CString m_strLineOne;
    CString m_strLineTwo;
    //}}AFX_DATA


// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CConfirmDialog)
    public:
    virtual BOOL PreTranslateMessage(MSG* pMsg);
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    HACCEL m_hAccel;

    // Generated message map functions
    //{{AFX_MSG(CConfirmDialog)
    virtual BOOL OnInitDialog();
    afx_msg void OnOptions();
    afx_msg void OnYes();
    virtual void OnCancel();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

#endif
