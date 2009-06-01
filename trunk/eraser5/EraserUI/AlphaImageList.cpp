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
#include "AlphaImageList.h"
#include <shlwapi.h>
#include <math.h>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// transparent color in non-32bit bitmaps
#define AIL_TRANSPARENT		RGB(192,192,192)


CAlphaImageList::CAlphaImageList()
{
	m_szImg = CSize(-1, -1);
}

CAlphaImageList::~CAlphaImageList()
{
}


BOOL CAlphaImageList::Create(int nWidth, int nHeight, int nStyle, int nCnt)
{
	m_szImg = CSize(-1, -1);

	m_ilNormal.DeleteImageList();
	m_ilHot.DeleteImageList();
	m_ilDisabled.DeleteImageList();

	if (nStyle == AILS_NEW)
	{
		// check if comctl32.dll version 6.00 is present

		BOOL bIsComCtl6 = FALSE;

		HMODULE hComCtlDll = LoadLibrary(_T("comctl32.dll"));

		if (hComCtlDll)
		{
			typedef HRESULT (CALLBACK *PFNDLLGETVERSION)(DLLVERSIONINFO*);

			PFNDLLGETVERSION pfnDllGetVersion = (PFNDLLGETVERSION)GetProcAddress(hComCtlDll, "DllGetVersion");

			if (pfnDllGetVersion)
			{
				DLLVERSIONINFO dvi;
				ZeroMemory(&dvi, sizeof(dvi));
				dvi.cbSize = sizeof(dvi);

				HRESULT hRes = (*pfnDllGetVersion)(&dvi);

				if (SUCCEEDED(hRes) && dvi.dwMajorVersion >= 6)
					bIsComCtl6 = TRUE;
			}

			FreeLibrary(hComCtlDll);
		}

		if (bIsComCtl6)
			m_nBmpDepth = ILC_COLOR32;	// 32-bit images are supported
		else
			m_nBmpDepth = ILC_COLOR24;
	}
	else
		m_nBmpDepth = ILC_COLOR4;	// old style images

	if (!m_ilNormal.Create(nWidth, nHeight, m_nBmpDepth | ILC_MASK, nCnt, 1))
		return FALSE;

	if (nStyle == AILS_NEW)
	{
		if (!m_ilHot.Create(nWidth, nHeight, m_nBmpDepth | ILC_MASK, nCnt, 1))
			return FALSE;
		if (!m_ilDisabled.Create(nWidth, nHeight, m_nBmpDepth | ILC_MASK, nCnt, 1))
			return FALSE;
	}

	m_szImg = CSize(nWidth, nHeight);

	return TRUE;
}


#pragma warning(push)
#pragma warning(disable: 4310)
BOOL CAlphaImageList::AddBitmap(UINT nID)
{
	if (m_szImg.cx <= 0)
		return FALSE;

	// load DIB from resources
	HINSTANCE hInst = AfxFindResourceHandle(MAKEINTRESOURCE(nID), RT_BITMAP);
	HBITMAP hBmp = (HBITMAP)LoadImage(hInst, MAKEINTRESOURCE(nID),
		IMAGE_BITMAP, 0, 0, LR_DEFAULTSIZE | LR_CREATEDIBSECTION);

	if (!hBmp)
		return FALSE;

	CBitmap bmp;
	bmp.Attach(hBmp);

	if (m_nBmpDepth == ILC_COLOR4)
	{
		// only one image list is needed
		if (m_ilNormal.Add(&bmp, AIL_TRANSPARENT) < 0)
			return FALSE;
	}
	else
	{
		CDC dcSrc;
		dcSrc.CreateCompatibleDC(NULL);

		struct
		{
			BITMAPINFOHEADER header;
			COLORREF col[256];
		} bmpi;

		ZeroMemory(&bmpi, sizeof(BITMAPINFOHEADER));
		bmpi.header.biSize = sizeof(BITMAPINFOHEADER);

		GetDIBits(dcSrc, bmp, 0, 0, NULL, (BITMAPINFO*)&bmpi, DIB_RGB_COLORS);

		int nDepth = bmpi.header.biBitCount;

		if (m_nBmpDepth != ILC_COLOR32 || nDepth != 32)
		{
			bmpi.header.biBitCount = 24;	// convert to 24-bit image
			bmpi.header.biCompression = BI_RGB;

			nDepth = 24;
		}

		int nLineSize = ((bmpi.header.biWidth * (nDepth==32 ? 4 : 3) + 3) & ~3);
		int nLineCnt = bmpi.header.biHeight;
		int nSize = nLineCnt * nLineSize;

		// get source bitmap data
		BYTE* pData = new BYTE[nSize];
		GetDIBits(dcSrc, bmp, 0, nLineCnt, pData, (BITMAPINFO*)&bmpi, DIB_RGB_COLORS);

		// create new bitmap
		BYTE* pDest = NULL;
		hBmp = CreateDIBSection(dcSrc, (BITMAPINFO*)&bmpi, DIB_RGB_COLORS, (void**)&pDest, NULL, 0);

		if (!hBmp)
		{
			delete[] pData;
			return FALSE;
		}

		CBitmap bmpDest;
		bmpDest.Attach(hBmp);

		if (nDepth == 32)
		{
			memcpy(pDest, pData, nSize);

			// alpha channel is used as image mask
			if (m_ilNormal.Add(&bmpDest, (CBitmap*)NULL) < 0)
			{
				delete[] pData;
				return FALSE;
			}

			// create grayscale image
			for (int i=0; i<nSize; i+=4)
			{
				const double dGamma = 0.5;	// lighten image
				double dGray = (pData[i+2] * 0.299) + (pData[i+1] * 0.587) + (pData[i+0] * 0.114);
				pDest[i+0] = pDest[i+1] = pDest[i+2] = (BYTE)(pow(dGray / 255.0, dGamma) * 255.0);
			}

			if (m_ilDisabled.Add(&bmpDest, (CBitmap*)NULL) < 0)
			{
				delete[] pData;
				return FALSE;
			}

			// create saturated image
			for (int i=0; i<nSize; i+=4)
			{
				const double dGamma = 1.4;	// darken image
				pDest[i+0] = (BYTE)(pow(pData[i+0] / 255.0, dGamma) * 255.0);
				pDest[i+1] = (BYTE)(pow(pData[i+1] / 255.0, dGamma) * 255.0);
				pDest[i+2] = (BYTE)(pow(pData[i+2] / 255.0, dGamma) * 255.0);
			}

			if (m_ilHot.Add(&bmpDest, (CBitmap*)NULL) < 0)
			{
				delete[] pData;
				return FALSE;
			}

			delete[] pData;
		}
		else
		{
			memcpy(pDest, pData, nSize);
		
			// create image mask from transparent color
			if (m_ilNormal.Add(&bmpDest, AIL_TRANSPARENT) < 0)
			{
				delete[] pData;
				return FALSE;
			}

			for (int y=0; y<nLineCnt; y++)
			{
				for (int x=0; x<nLineSize; x+=3)
				{
					// align index to line size
					int i = y*nLineSize + x;

					// transparent color is not modified
					if (RGB(pData[i+2], pData[i+1], pData[i+0]) == AIL_TRANSPARENT)
					{
						pDest[i+2] = GetRValue(AIL_TRANSPARENT);
						pDest[i+1] = GetGValue(AIL_TRANSPARENT);
						pDest[i+0] = GetBValue(AIL_TRANSPARENT);
					}
					else
					{
						const double dGamma = 0.5;
						double dGray = (pData[i+2] * 0.299) + (pData[i+1] * 0.587) + (pData[i+0] * 0.114);
						pDest[i+0] = pDest[i+1] = pDest[i+2] = (BYTE)(pow(dGray / 255.0, dGamma) * 255.0);
					}
				}
			}

			if (m_ilDisabled.Add(&bmpDest, AIL_TRANSPARENT) < 0)
			{
				delete[] pData;
				return FALSE;
			}

			for (int y=0; y<nLineCnt; y++)
			{
				for (int x=0; x<nLineSize; x+=3)
				{
					int i = y*nLineSize + x;

					if (RGB(pData[i+2], pData[i+1], pData[i+0]) == AIL_TRANSPARENT)
					{
						pDest[i+2] = GetRValue(AIL_TRANSPARENT);
						pDest[i+1] = GetGValue(AIL_TRANSPARENT);
						pDest[i+0] = GetBValue(AIL_TRANSPARENT);
					}
					else
					{
						const double dGamma = 1.4;
						pDest[i+0] = (BYTE)(pow(pData[i+0] / 255.0, dGamma) * 255.0);
						pDest[i+1] = (BYTE)(pow(pData[i+1] / 255.0, dGamma) * 255.0);
						pDest[i+2] = (BYTE)(pow(pData[i+2] / 255.0, dGamma) * 255.0);
					}
				}
			}

			if (m_ilHot.Add(&bmpDest, AIL_TRANSPARENT) < 0)
			{
				delete[] pData;
				return FALSE;
			}

			delete[] pData;
		}
	}

	return TRUE;
}
#pragma warning(pop)


BOOL CAlphaImageList::Draw(CDC* pDC, CPoint ptPos, int nImgList, int nIndex)
{
	if (m_szImg.cx <= 0)
		return FALSE;

	if (m_nBmpDepth >= ILC_COLOR24)
	{
		switch (nImgList)
		{
		case AIL_NORMAL:
			return m_ilNormal.Draw(pDC, nIndex, ptPos, ILD_NORMAL);
		case AIL_HOT:
			return m_ilHot.Draw(pDC, nIndex, ptPos, ILD_NORMAL);
		case AIL_DISABLED:
			return m_ilDisabled.Draw(pDC, nIndex, ptPos, ILD_NORMAL);
		default:
			return FALSE;
		}
	}
	else	// old style image
	{
		switch (nImgList)
		{
		case AIL_NORMAL:
		case AIL_HOT:
			return m_ilNormal.Draw(pDC, nIndex, ptPos, ILD_NORMAL);

		case AIL_DISABLED:
			{
				CDC dcBmpBW;
				dcBmpBW.CreateCompatibleDC(pDC);

				// create a black and white bitmap
				struct {
					BITMAPINFOHEADER bmiHeader; 
					RGBQUAD bmiColors[2]; 
				} bmpiBW = {{
					sizeof(BITMAPINFOHEADER),
					m_szImg.cx, m_szImg.cy, 1, 1, BI_RGB, 0, 0, 0, 0, 0
				}, {
				  { 0x00, 0x00, 0x00, 0x00 }, { 0xFF, 0xFF, 0xFF, 0x00 }
				}};

				VOID *pTemp;
				HBITMAP hBmpBW = CreateDIBSection(dcBmpBW, (LPBITMAPINFO)&bmpiBW,
					DIB_RGB_COLORS, &pTemp, NULL, 0);

				if (!hBmpBW)
					return FALSE;

				CBitmap bmpBW;
				bmpBW.Attach(hBmpBW);

				dcBmpBW.SelectObject(&bmpBW);

				// draw the image with white background
				COLORREF crOld = m_ilNormal.SetBkColor(RGB(255,255,255));

				if (!m_ilNormal.Draw(&dcBmpBW, nIndex, CPoint(0,0), ILD_NORMAL))
					return FALSE;

				m_ilNormal.SetBkColor(crOld);

				BOOL bFlat = FALSE;
#ifndef SPI_GETFLATMENU
#	define SPI_GETFLATMENU		0x1022
#endif
				SystemParametersInfo(SPI_GETFLATMENU, 0, &bFlat, 0);

				CBrush brush;

				if (!bFlat)	// highlight is not drawn in XP-style menus
				{
					brush.CreateSysColorBrush(COLOR_3DHILIGHT);
					pDC->SelectObject(&brush);
					pDC->BitBlt(ptPos.x + 1, ptPos.y + 1, m_szImg.cx, m_szImg.cy, &dcBmpBW, 0, 0, 0xB8074A);
					brush.DeleteObject();
				}

				// draw image with the shadow color
				brush.CreateSysColorBrush(COLOR_3DSHADOW);
				pDC->SelectObject(&brush);
				pDC->BitBlt(ptPos.x, ptPos.y, m_szImg.cx, m_szImg.cy, &dcBmpBW, 0, 0, 0xB8074A);
			}

		default:
			return FALSE;
		}
	}
}


HIMAGELIST CAlphaImageList::GetImageList(int nImgList)
{
	switch (nImgList)
	{
	case AIL_NORMAL:
		return m_ilNormal.m_hImageList;
	case AIL_HOT:
		return m_ilHot.m_hImageList;
	case AIL_DISABLED:
		return m_ilDisabled.m_hImageList;
	default:
		return NULL;
	}
}

