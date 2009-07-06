// TimeOutMessageBox.cpp
// Found from http://www.codeguru.com/. Modified for Eraser - sami@tolvanen.com

// From http://www.codeguru.com/submission_guide.shtml :
//
// "While we are talking about copyrights, you retain copyright of
//  your article and code but by submitting it to CodeGuru you give it
//  permission to use it in a fair manner and also permit all developers
//  to freely use the code in their own applications - even if they are
//  commercial."

#include "stdafx.h"
#include "windows.h"
#include <process.h>

#define TIME_TO_APPEAR        1000
#define IDCLOSED_BY_TIMEOUT   50

static HWND hwndMsgBox  = NULL;
static BOOL bUserIsHere = FALSE;

typedef struct
{
    DWORD   CurrentThreadID;
    HANDLE  EventHandle;
    UINT    uElapse;
    UINT    uBlinkingTime;
} trMyData;

static BOOL CALLBACK FindMsgBox(HWND hwnd, LPARAM /*lParam*/)
{
    TCHAR   ClassNameBuf[256];
    BOOL    RetVal = TRUE;

    if (!_tcscmp(ClassNameBuf, _T("#32770")))
    {
        hwndMsgBox  = hwnd;
        RetVal      = FALSE;
    }
    return RetVal;
}

static LRESULT CALLBACK MyWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    if ((WM_KEYFIRST <= uMsg && uMsg <= WM_KEYLAST)     ||
        (WM_MOUSEFIRST <= uMsg && uMsg <= WM_MOUSELAST) ||
        (WM_NCMOUSEMOVE <= uMsg && uMsg <= WM_NCMBUTTONDBLCLK))
    {
        bUserIsHere = TRUE;
    }

    return CallWindowProc((WNDPROC)GetWindowLongPtr(hwnd, -21 /*GWL_USERDATA*/), hwnd,
                          uMsg, wParam, lParam);
}

static BOOL CALLBACK EnumChildProc(HWND hwnd, LPARAM lParam)
{
    if (lParam)
    {
        SetWindowLongPtr(hwnd, -21 /*GWL_USERDATA*/, SetWindowLongPtr(hwnd, -4 /*GWL_WNDPROC*/,
            (LONG_PTR)MyWindowProc));
    }
    else
		SetWindowLongPtr(hwnd, -4 /*GWL_WNDPROC*/, GetWindowLongPtr(hwnd, -21 /*GWL_USERDATA*/));

    return TRUE;
}

static UINT _stdcall TimeoutMsgBox(LPVOID pParam)
{
    trMyData    *prMyData       = (trMyData *)pParam;
    UINT        uTime;
    UINT        uBlinkingTime   = prMyData->uBlinkingTime;
    UINT        uElapse;
    UINT        uCaretBlinkTime;
    DWORD       dWaitRetVal;

    if (prMyData->uElapse <= TIME_TO_APPEAR)
        return 0;

    prMyData->uElapse -= TIME_TO_APPEAR;

    //Give time for MessageBox to appear
    Sleep(TIME_TO_APPEAR);

    EnumThreadWindows(prMyData->CurrentThreadID, FindMsgBox, NULL);

    if (!hwndMsgBox)
        return 0;

	SetWindowLongPtr(hwndMsgBox, -21 /*GWL_USERDATA*/, SetWindowLongPtr(hwndMsgBox,-4 /* GWL_WNDPROC*/, (LONG_PTR)MyWindowProc));
    EnumChildWindows(hwndMsgBox, EnumChildProc, TRUE);

    if (uBlinkingTime > prMyData->uElapse)
        uBlinkingTime = prMyData->uElapse;

    BOOL bAgain = TRUE;

    do
    {
        bAgain = FALSE;

        uElapse = prMyData->uElapse - uBlinkingTime;
        dWaitRetVal = WaitForSingleObject(prMyData->EventHandle, uElapse);

        if (dWaitRetVal == WAIT_TIMEOUT)
        {
            if (bUserIsHere)
            {
                bUserIsHere = FALSE;

                bAgain = TRUE;
                continue;
            }
            if ((int)uBlinkingTime > 0)
            {
                SetForegroundWindow(hwndMsgBox);
                uCaretBlinkTime = GetCaretBlinkTime();
                uTime = uBlinkingTime;

                while ((int)uTime > 0)
                {
                    FlashWindow(hwndMsgBox, TRUE);
                    dWaitRetVal = WaitForSingleObject(prMyData->EventHandle, uCaretBlinkTime);

                    if (dWaitRetVal != WAIT_TIMEOUT)
                        break;

                    if (bUserIsHere)
                    {
                        bUserIsHere = FALSE;
                        SendMessage(hwndMsgBox, WM_NCACTIVATE, (WPARAM)(GetForegroundWindow() == hwndMsgBox), 0);

                        bAgain = TRUE;
                        continue;
                    }

                    uTime -= uCaretBlinkTime;
                }
            }
        }
	}
	while (bAgain);

    SetWindowLong(hwndMsgBox, -4 /*GWL_WNDPROC*/, GetWindowLong(hwndMsgBox, -21/*GWL_USERDATA*/));
    EnumChildWindows(hwndMsgBox, EnumChildProc, FALSE);

    if (dWaitRetVal == WAIT_TIMEOUT)
        EndDialog(hwndMsgBox, IDCLOSED_BY_TIMEOUT);

    return 0;
}

static int MsgBoxWithTimeout(HWND hWnd, LPCTSTR lpText, LPCTSTR lpCaption, UINT uType, UINT uElapse, UINT uBlinkingTime)
{
    trMyData rMyData;
    rMyData.CurrentThreadID = GetCurrentThreadId();
    rMyData.uElapse = uElapse;
    rMyData.uBlinkingTime = uBlinkingTime;
    rMyData.EventHandle = CreateEvent(NULL, TRUE, FALSE, NULL);

    UINT uThreadID;
    HANDLE hThreadHandle = (HANDLE)_beginthreadex(NULL, 0, TimeoutMsgBox,
        (LPVOID)&rMyData, 0, &uThreadID);

    if (hThreadHandle)
        CloseHandle(hThreadHandle);

    int Res = MessageBox(hWnd, lpText, lpCaption, uType);

    SetEvent(rMyData.EventHandle);
    CloseHandle(rMyData.EventHandle);

    return Res;
}

#define AFX_ELAPSE_TIME     15000
#define AFX_BLINKING_TIME   3000

int AFXAPI AfxTimeOutMessageBox(LPCTSTR lpszText, UINT nType)
{
    CWinApp *pApp   = AfxGetApp();
    int iResult     = 0;

    if (pApp != NULL)
    {
        iResult =
            MsgBoxWithTimeout(AfxGetMainWnd()->GetSafeHwnd(),
                             lpszText, pApp->m_pszAppName,
                             nType, AFX_ELAPSE_TIME, AFX_BLINKING_TIME);
    }

    return iResult;
}

int AFXAPI AfxTimeOutMessageBox(UINT nIDPrompt, UINT nType)
{
    CString string;

    try
    {
        if (!string.LoadString(nIDPrompt))
        {
            TRACE1("Error: failed to load message box prompt string 0x%04x.\n",
                nIDPrompt);
            ASSERT(FALSE);
        }

        return AfxTimeOutMessageBox(string, nType);
    }
    catch (...)
    {
        ASSERT(FALSE);
    }

    return -1;
}
