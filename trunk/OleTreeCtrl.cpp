// OleTreeCtrl.cpp (Ripped from MFC sources)
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

#include "stdafx.h"
#include "eraser.h"
#include "OleTreeCtrl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CDropTargetTreeCtrl

CDropTargetTreeCtrl::CDropTargetTreeCtrl()
{
    // initialize local state
    m_lpDataObject = NULL;
    m_bRegistered = FALSE;
    ASSERT_VALID(this);
}

CDropTargetTreeCtrl::~CDropTargetTreeCtrl()
{
    ASSERT_VALID(this);
    ASSERT(m_bRegistered == FALSE);
}

BEGIN_MESSAGE_MAP(CDropTargetTreeCtrl, CTreeCtrl)
    //{{AFX_MSG_MAP(CDropTargetTreeCtrl)
    ON_WM_DESTROY()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CDropTargetTreeCtrl message handlers


BOOL CDropTargetTreeCtrl::Register()
{
    ASSERT_VALID(this);
    ASSERT(m_bRegistered == FALSE);

    LPUNKNOWN lpUnknown = (LPUNKNOWN)GetInterface(&IID_IUnknown);
    ASSERT(lpUnknown != NULL);

    // the object must be locked externally to keep LRPC connections alive
    if (CoLockObjectExternal(lpUnknown, TRUE, FALSE) != S_OK)
        return FALSE;

    // connect the HWND to the IDropTarget implementation
    if (RegisterDragDrop(m_hWnd, (LPDROPTARGET)GetInterface(&IID_IDropTarget)) != S_OK)
    {
        CoLockObjectExternal(lpUnknown, FALSE, FALSE);
        return FALSE;
    }

    m_bRegistered = TRUE;

    return TRUE;
}

void CDropTargetTreeCtrl::Revoke()
{
    ASSERT_VALID(this);
    ASSERT(m_lpDataObject == NULL);
    ASSERT(m_bRegistered != FALSE);

    // disconnect from OLE
    RevokeDragDrop(m_hWnd);
    CoLockObjectExternal((LPUNKNOWN)GetInterface(&IID_IUnknown), FALSE, TRUE);

    // disconnect internal data
    m_pDropTarget = NULL;
    m_bRegistered = FALSE;
}

/////////////////////////////////////////////////////////////////////////////
// default implementation of drag/drop scrolling

DROPEFFECT CDropTargetTreeCtrl::OnDragScroll(DWORD dwKeyState, CPoint point)
{
    UNUSED_ALWAYS(dwKeyState);
    UNUSED_ALWAYS(point);

    ASSERT_VALID(this);
    return DROPEFFECT_NONE;
}

/////////////////////////////////////////////////////////////////////////////
// CDropTargetTreeCtrl drop/ drop query handling

DROPEFFECT CDropTargetTreeCtrl::OnDragEnter(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    UNUSED_ALWAYS(pDataObject);
    UNUSED_ALWAYS(dwKeyState);
    UNUSED_ALWAYS(point);

    ASSERT_VALID(this);
    return DROPEFFECT_NONE;
}

DROPEFFECT CDropTargetTreeCtrl::OnDragOver(COleDataObject* pDataObject, DWORD dwKeyState, CPoint point)
{
    UNUSED_ALWAYS(pDataObject);
    UNUSED_ALWAYS(dwKeyState);
    UNUSED_ALWAYS(point);

    ASSERT_VALID(this);
    return DROPEFFECT_NONE;
}

BOOL CDropTargetTreeCtrl::OnDrop(COleDataObject* pDataObject, DROPEFFECT dropEffect, CPoint point)
{
    UNUSED_ALWAYS(pDataObject);
    UNUSED_ALWAYS(dropEffect);
    UNUSED_ALWAYS(point);

    ASSERT_VALID(this);
    return DROPEFFECT_NONE;
}

DROPEFFECT CDropTargetTreeCtrl::OnDropEx(COleDataObject* pDataObject,
    DROPEFFECT dropEffect, DROPEFFECT dropEffectList, CPoint point)
{
    UNUSED_ALWAYS(pDataObject);
    UNUSED_ALWAYS(dropEffect);
    UNUSED_ALWAYS(dropEffectList);
    UNUSED_ALWAYS(point);

    ASSERT_VALID(this);
    return (DROPEFFECT)-1;  // not implemented
}

void CDropTargetTreeCtrl::OnDragLeave()
{
    ASSERT_VALID(this);
    return;
}

/////////////////////////////////////////////////////////////////////////////
// CDropTargetTreeCtrl::CDropTargetTreeCtrl implementation

BEGIN_INTERFACE_MAP(CDropTargetTreeCtrl, CTreeCtrl)
    INTERFACE_PART(CDropTargetTreeCtrl, IID_IDropTarget, DropTarget)
END_INTERFACE_MAP()

STDMETHODIMP_(ULONG) CDropTargetTreeCtrl::XDropTarget::AddRef()
{
    METHOD_PROLOGUE_EX_(CDropTargetTreeCtrl, DropTarget)
    return pThis->ExternalAddRef();
}

STDMETHODIMP_(ULONG) CDropTargetTreeCtrl::XDropTarget::Release()
{
    METHOD_PROLOGUE_EX_(CDropTargetTreeCtrl, DropTarget)
    return pThis->ExternalRelease();
}

STDMETHODIMP CDropTargetTreeCtrl::XDropTarget::QueryInterface(
    REFIID iid, LPVOID* ppvObj)
{
    METHOD_PROLOGUE_EX_(CDropTargetTreeCtrl, DropTarget)
    return pThis->ExternalQueryInterface(&iid, ppvObj);
}

// helper to filter out invalid DROPEFFECTs
static DROPEFFECT FilterDropEffect(DROPEFFECT dropEffect, DROPEFFECT dwEffects)
{
    // return allowed dropEffect and DROPEFFECT_NONE
    if ((dropEffect & dwEffects) != 0)
        return dropEffect;

    // map common operations (copy/move) to alternates, but give negative
    //  feedback for DROPEFFECT_LINK.
    switch (dropEffect)
    {
    case DROPEFFECT_COPY:
        if (dwEffects & DROPEFFECT_MOVE)
            return DROPEFFECT_MOVE;
        else if (dwEffects & DROPEFFECT_LINK)
            return DROPEFFECT_LINK;
        break;
    case DROPEFFECT_MOVE:
        if (dwEffects & DROPEFFECT_COPY)
            return DROPEFFECT_COPY;
        else if (dwEffects & DROPEFFECT_LINK)
            return DROPEFFECT_LINK;
        break;
    case DROPEFFECT_LINK:
        break;
    }

    return DROPEFFECT_NONE;
}

STDMETHODIMP CDropTargetTreeCtrl::XDropTarget::DragEnter(THIS_ LPDATAOBJECT lpDataObject,
    DWORD dwKeyState, POINTL pt, LPDWORD pdwEffect)
{
    METHOD_PROLOGUE_EX(CDropTargetTreeCtrl, DropTarget)
    ASSERT_VALID(pThis);

    ASSERT(pdwEffect != NULL);
    ASSERT(lpDataObject != NULL);

    SCODE sc = E_UNEXPECTED;

    try
    {
        // cache lpDataObject
        lpDataObject->AddRef();

        if (pThis->m_lpDataObject)
        {
            pThis->m_lpDataObject->Release();
            pThis->m_lpDataObject = NULL;
        }

        pThis->m_lpDataObject = lpDataObject;

        CPoint point((int)pt.x, (int)pt.y);
        pThis->ScreenToClient(&point);

        // check first for entering scroll area
        DROPEFFECT dropEffect = pThis->OnDragScroll(dwKeyState, point);

        if ((dropEffect & DROPEFFECT_SCROLL) == 0)
        {
            // funnel through OnDragEnter since not in scroll region
            COleDataObject dataObject;
            dataObject.Attach(lpDataObject, FALSE);
            dropEffect = pThis->OnDragEnter(&dataObject, dwKeyState, point);
        }
        *pdwEffect = FilterDropEffect(dropEffect, *pdwEffect);
        sc = S_OK;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return sc;
}

STDMETHODIMP CDropTargetTreeCtrl::XDropTarget::DragOver(THIS_ DWORD dwKeyState,
    POINTL pt, LPDWORD pdwEffect)
{
    METHOD_PROLOGUE_EX(CDropTargetTreeCtrl, DropTarget)
    ASSERT_VALID(pThis);

    ASSERT(pdwEffect != NULL);
    ASSERT(pThis->m_lpDataObject != NULL);

    SCODE sc = E_UNEXPECTED;

    try
    {
        CPoint point((int)pt.x, (int)pt.y);
        pThis->ScreenToClient(&point);

        // check first for entering scroll area
        DROPEFFECT dropEffect = pThis->OnDragScroll(dwKeyState, point);

        if ((dropEffect & DROPEFFECT_SCROLL) == 0)
        {
            // funnel through OnDragOver
            COleDataObject dataObject;
            dataObject.Attach(pThis->m_lpDataObject, FALSE);
            dropEffect = pThis->OnDragOver(&dataObject, dwKeyState, point);
        }

        *pdwEffect = FilterDropEffect(dropEffect, *pdwEffect);
        sc = S_OK;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return sc;
}

STDMETHODIMP CDropTargetTreeCtrl::XDropTarget::DragLeave(THIS)
{
    METHOD_PROLOGUE_EX(CDropTargetTreeCtrl, DropTarget)
    ASSERT_VALID(pThis);

    // allow derivative to do own cleanup
    COleDataObject dataObject;
    dataObject.Attach(pThis->m_lpDataObject, FALSE);
    pThis->OnDragLeave();

    // release cached data object
    if (pThis->m_lpDataObject)
    {
        pThis->m_lpDataObject->Release();
        pThis->m_lpDataObject = NULL;
    }

    return S_OK;
}

STDMETHODIMP CDropTargetTreeCtrl::XDropTarget::Drop(THIS_ LPDATAOBJECT lpDataObject,
    DWORD dwKeyState, POINTL pt, LPDWORD pdwEffect)
{
    METHOD_PROLOGUE_EX(CDropTargetTreeCtrl, DropTarget)
    ASSERT_VALID(pThis);

    ASSERT(pdwEffect != NULL);
    ASSERT(lpDataObject != NULL);

    SCODE sc = E_UNEXPECTED;

    try
    {
        COleDataObject dataObject;
        dataObject.Attach(lpDataObject, FALSE);
        CPoint point((int)pt.x, (int)pt.y);

        pThis->ScreenToClient(&point);

        // verify that drop is legal
        DROPEFFECT dropEffect = FilterDropEffect(pThis->OnDragOver(&dataObject,
                                                 dwKeyState, point), *pdwEffect);

        // execute the drop (try OnDropEx then OnDrop for backward compatibility)
        DROPEFFECT temp = pThis->OnDropEx(&dataObject, dropEffect, *pdwEffect, point);

        if (temp != -1)
        {
            // OnDropEx was implemented, return its drop effect
            dropEffect = temp;
        }
        else if (dropEffect != DROPEFFECT_NONE)
        {
            // OnDropEx not implemented
            if (!pThis->OnDrop(&dataObject, dropEffect, point))
                dropEffect = DROPEFFECT_NONE;
        }
        else
        {
            // drop not accepted, allow cleanup
            pThis->OnDragLeave();
        }

        // release potentially cached data object
        if (pThis->m_lpDataObject)
        {
            pThis->m_lpDataObject->Release();
            pThis->m_lpDataObject = NULL;
        }

        *pdwEffect = dropEffect;
        sc = S_OK;
    }
    catch (CException *e)
    {
        ASSERT(FALSE);
        REPORT_ERROR(e);
        e->Delete();
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return sc;
}


void CDropTargetTreeCtrl::OnDestroy()
{
    if (m_bRegistered)
        Revoke();

    CTreeCtrl::OnDestroy();
}
