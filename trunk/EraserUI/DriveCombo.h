// DriveCombo.h
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

#ifndef DRIVECOMBO_H
#define DRIVECOMBO_H

#define ALLOW_ALL_DRIVES

#ifdef ALLOW_ALL_DRIVES
const LPCTSTR DRIVE_ALL_LOCAL = TEXT(" :\\");
const LPCTSTR szLocalDrives   = TEXT("Local Hard Drives");

void GetLocalHardDrives(CStringArray& strDrives);
#endif

/////////////////////////////////////////////////////////////////////////////
// CDriveCombo window

class CDriveCombo : public CComboBox
{
// Construction
public:
    CDriveCombo();
    virtual ~CDriveCombo();

// Overrides
    // ClassWizard generated virtual function overrides
    //{{AFX_VIRTUAL(CDriveCombo)
    public:
    virtual void DrawItem(LPDRAWITEMSTRUCT lpDrawItemStruct);
    //}}AFX_VIRTUAL

// Implementation
public:
    void            GetSelectedDrive(CString&);
    int             SelectDrive(LPCTSTR szDrive);
    void            FillDrives();

protected:
    int             AddString(LPCTSTR lpszString);
    CStringArray    m_straDrives;

    // Generated message map functions
protected:
    //{{AFX_MSG(CDriveCombo)
        // NOTE - the ClassWizard will add and remove member functions here.
    //}}AFX_MSG

    DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

#endif
