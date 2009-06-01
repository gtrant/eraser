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

#include "stdafx.h"
#include "AlphaToolBar.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


CAlphaToolBar::CAlphaToolBar()
{
}

CAlphaToolBar::~CAlphaToolBar()
{
}

BEGIN_MESSAGE_MAP(CAlphaToolBar, CToolBar)
END_MESSAGE_MAP()


BOOL CAlphaToolBar::Create(CWnd* pParentWnd, UINT nID)
{
	// create flat transparent toolbar for use in a rebar control
	return CToolBar::CreateEx(pParentWnd, TBSTYLE_FLAT | TBSTYLE_TRANSPARENT,
		WS_CHILD | WS_VISIBLE | CBRS_ALIGN_TOP | CBRS_TOOLTIPS | CBRS_FLYBY,
		CRect(0,0,0,0), nID);
}


BOOL CAlphaToolBar::LoadToolBar(UINT nID, int nStyle)
{
	HINSTANCE hInst = AfxFindResourceHandle(MAKEINTRESOURCE(nID), RT_TOOLBAR);
	HRSRC hRsrc = ::FindResource(hInst, MAKEINTRESOURCE(nID), RT_TOOLBAR);
	if (hRsrc == NULL)
		return FALSE;

	HGLOBAL hGlobal = LoadResource(hInst, hRsrc);
	if (hGlobal == NULL)
		return FALSE;

	struct TBDATA
	{
		WORD wVersion;
		WORD wWidth;
		WORD wHeight;
		WORD wItemCount;
		WORD aItems[1];
	};

	// get the toolbar data
	TBDATA* pData = (TBDATA*)LockResource(hGlobal);
	if (pData == NULL)
		return FALSE;
	ASSERT(pData->wVersion == 1);

	// create toolbar buttons
	UINT* pItems = new UINT[pData->wItemCount];
	for (int i = 0; i < pData->wItemCount; i++)
		pItems[i] = pData->aItems[i];
	BOOL bResult = SetButtons(pItems, pData->wItemCount);
	delete[] pItems;

	if (bResult)
	{
		CSize sizeImage(pData->wWidth, pData->wHeight);
		CSize sizeButton(pData->wWidth + 8, pData->wHeight + 7);
		SetSizes(sizeButton, sizeImage);

		// load images from bitmap resource
		if (m_ImgList.Create(pData->wWidth, pData->wHeight, nStyle,
			pData->wItemCount))
		{
			bResult = m_ImgList.AddBitmap(nID);
		}
		else
			bResult = FALSE;
	}

	UnlockResource(hGlobal);
	FreeResource(hGlobal);

	if (bResult)	// let the toolbar use our image lists
	{
		SendMessage(TB_SETIMAGELIST, 0, (LPARAM)m_ImgList.GetImageList(AIL_NORMAL));
		SendMessage(TB_SETHOTIMAGELIST, 0, (LPARAM)m_ImgList.GetImageList(AIL_HOT));
		SendMessage(TB_SETDISABLEDIMAGELIST, 0, (LPARAM)m_ImgList.GetImageList(AIL_DISABLED));
	}

	return bResult;
}
