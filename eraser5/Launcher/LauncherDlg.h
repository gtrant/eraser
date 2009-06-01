// LauncherDlg.h
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

#if !defined(AFX_LAUNCHERDLG_H__DC6635CA_F67B_11D2_BBF6_00105AAF62C4__INCLUDED_)
#define AFX_LAUNCHERDLG_H__DC6635CA_F67B_11D2_BBF6_00105AAF62C4__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CLauncherDlg dialog

class CLauncherDlg : public CDialog
{
// Construction
public:
    BOOL m_bUseFiles;
    BOOL m_bFolders;
    BOOL m_bSubFolders;
    BOOL m_bKeepFolder;
    BOOL m_bUseEmptySpace;
    BOOL m_bResults;
    BOOL m_bResultsOnError;
    BOOL m_bRecycled;

    ERASER_METHOD m_emMethod;
    E_UINT16 m_uPasses;

    ERASER_HANDLE m_ehContext;

    CStringArray m_saFiles;
    CStringArray m_saFolders;

    BOOL Erase();
    void Options();

    CLauncherDlg(CWnd* pParent = NULL); // standard constructor

// Dialog Data
    //{{AFX_DATA(CLauncherDlg)
    enum { IDD = IDD_LAUNCHER_DIALOG };
    CProgressCtrl   m_pcProgress;
    CProgressCtrl   m_pcProgressTotal;
    CString m_strData;
    CString m_strErasing;
    CString m_strMessage;
    CString m_strPass;
    CString m_strPercent;
    CString m_strPercentTotal;
    CString m_strTime;
    //}}AFX_DATA

    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CLauncherDlg)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:
    HICON m_hIcon;

    // Generated message map functions
    //{{AFX_MSG(CLauncherDlg)
    virtual BOOL OnInitDialog();
    afx_msg void OnPaint();
    afx_msg HCURSOR OnQueryDragIcon();
    virtual void OnCancel();
    afx_msg void OnDestroy();
    //}}AFX_MSG
    afx_msg LRESULT OnEraserNotify(WPARAM wParam, LPARAM lParam);
    DECLARE_MESSAGE_MAP()

    BOOL EraserWipeBegin();
    BOOL EraserWipeUpdate();
    BOOL EraserWipeDone();


};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_LAUNCHERDLG_H__DC6635CA_F67B_11D2_BBF6_00105AAF62C4__INCLUDED_)
