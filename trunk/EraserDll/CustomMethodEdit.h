// CustomEdit.h
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

#if !defined(AFX_CUSTOMMETHODEDIT_H__0C74CCD1_BC7D_11D3_82A0_000000000000__INCLUDED_)
#define AFX_CUSTOMMETHODEDIT_H__0C74CCD1_BC7D_11D3_82A0_000000000000__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "../EraserUI/FlatListCtrl.h"
#include "ByteEdit.h"
#include "Pass.h"
#include "commctrl.h"

/////////////////////////////////////////////////////////////////////////////
// CCustomMethodEdit dialog

class ERASER_API CCustomMethodEdit : public CDialog
{
// Construction
public:
    void SaveSelectedPass();
    void EnablePattern(BOOL bEnable);
    void UpdateList();
    BOOL LoadCustomMethod(LPMETHOD);
    BOOL FillCustomMethod(LPMETHOD);
    CCustomMethodEdit(CWnd* pParent = NULL);   // standard constructor

    WORD m_nSelectedPass;
    CArray<PASS, PASS&> m_aPasses;

// Dialog Data
    //{{AFX_DATA(CCustomMethodEdit)
    enum { IDD = IDD_DIALOG_METHODEDIT };
    CByteEdit   m_editByte2;
    CByteEdit   m_editByte3;
    CByteEdit   m_editByte1;
    CFlatListCtrl   m_lcPasses;
    BOOL    m_bByte2;
    BOOL    m_bByte3;
    CString m_strDescription;
    BOOL    m_bShuffle;
    //}}AFX_DATA


// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CCustomMethodEdit)
    protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //}}AFX_VIRTUAL

// Implementation
protected:

    // Generated message map functions
    //{{AFX_MSG(CCustomMethodEdit)
    virtual BOOL OnInitDialog();
    afx_msg void OnButtonAdd();
    afx_msg void OnButtonCopy();
    afx_msg void OnButtonDelete();
    afx_msg void OnButtonDown();
    afx_msg void OnButtonUp();
    afx_msg void OnCheckByte2();
    afx_msg void OnCheckByte3();
    virtual void OnOK();
    afx_msg void OnRadioPattern();
    afx_msg void OnRadioPseudorandom();
    afx_msg void OnItemchangedListPasses(NMHDR* pNMHDR, LRESULT* pResult);
    afx_msg void OnChangeEditDescription();
    afx_msg void OnChangeEditByte1();
    afx_msg void OnChangeEditByte2();
    afx_msg void OnChangeEditByte3();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_CUSTOMMETHODEDIT_H__0C74CCD1_BC7D_11D3_82A0_000000000000__INCLUDED_)
