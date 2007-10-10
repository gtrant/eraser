/*********************************************************************
* IE-like Menu and Toolbar, version 1.5 (August 3, 2004)
* Copyright (C) 2002-2003 Michal Mecinski.
*
* You may freely use and modify this code, but don't remove
* this copyright note.
*
* THERE IS NO WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, FOR
* THIS CODE. THE AUTHOR DOES NOT TAKE THE RESPONSIBILITY
* FOR ANY DAMAGE RESULTING FROM THE USE OF IT.
*
* E-mail: mimec@mimec.org
* WWW: http://www.mimec.org
********************************************************************/

#pragma once

#include "AlphaImageList.h"

class CAlphaToolBar : public CToolBar
{
public:
	CAlphaToolBar();
	virtual ~CAlphaToolBar();

public:
	// Create toolbar
	BOOL Create(CWnd* pParentWnd, UINT nID=0);

	// Load toolbar and bitmap from resources
	BOOL LoadToolBar(UINT nID, int nStyle=AILS_OLD);

protected:
	CAlphaImageList m_ImgList;

protected:
	DECLARE_MESSAGE_MAP()
};
