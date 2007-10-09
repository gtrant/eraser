// ViewerDlg.cpp : implementation file
//

#include "stdafx.h"
#include "..\EraserDll\EraserDll.h"
#include "Verify.h"
#include "ViewerDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

#define DISPLAY_LAST    1

const LPCTSTR szWindowTitle   = "Eraser: Verify: File Viewer [%s]";
const LPCTSTR szHeader        = "%s (%I64u bytes)\r\n\r\nCluster %u (%u bytes, offset: %I64u bytes):\r\n";
const LPCTSTR szLineFormat    = "%.4X:  %.8X  %.8X  %.8X  %.8X   : %s\r\n";
const LPCTSTR szEndOfFile     = "\r\nEnd Of File at 0x%.4X.\r\n";

const int iOffsetLength       = 5;
const int iStringStart        = 48;
const int iStringEnd          = 66;

/////////////////////////////////////////////////////////////////////////////
// CViewerDlg dialog


CViewerDlg::CViewerDlg(CWnd* pParent /*=NULL*/) :
CDialog(CViewerDlg::IDD, pParent),
m_hFile(INVALID_HANDLE_VALUE),
m_dwClusterSize(0)
{
	//{{AFX_DATA_INIT(CViewerDlg)
	m_dwCurrentCluster = 0;
	//}}AFX_DATA_INIT
}


void CViewerDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CViewerDlg)
	DDX_Control(pDX, IDC_RICHEDIT_VIEW, m_recView);
	DDX_Text(pDX, IDC_EDIT_CLUSTER, m_dwCurrentCluster);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CViewerDlg, CDialog)
	//{{AFX_MSG_MAP(CViewerDlg)
	ON_BN_CLICKED(IDC_BUTTON_FIRST, OnButtonFirst)
	ON_BN_CLICKED(IDC_BUTTON_GO, OnButtonGo)
	ON_BN_CLICKED(IDC_BUTTON_LAST, OnButtonLast)
	ON_BN_CLICKED(IDC_BUTTON_NEXT, OnButtonNext)
	ON_BN_CLICKED(IDC_BUTTON_PREVIOUS, OnButtonPrevious)
	ON_WM_DESTROY()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CViewerDlg message handlers

void CViewerDlg::OnButtonFirst() 
{
    m_dwCurrentCluster = 0;
    DisplayCluster(0);
    UpdateData(FALSE);
}

void CViewerDlg::OnButtonGo() 
{
	UpdateData();
    DisplayCluster(m_dwCurrentCluster);
    UpdateData(FALSE);
}

void CViewerDlg::OnButtonLast() 
{
	DisplayCluster(0, DISPLAY_LAST);
    UpdateData(FALSE);
}

void CViewerDlg::OnButtonNext() 
{
    DisplayCluster(++m_dwCurrentCluster);
    UpdateData(FALSE);	
}

void CViewerDlg::OnButtonPrevious() 
{
    if (m_dwCurrentCluster > 0) {
        DisplayCluster(--m_dwCurrentCluster);
    } else {
        DisplayCluster(0, DISPLAY_LAST);
    }
    UpdateData(FALSE);
}

BOOL CViewerDlg::OnInitDialog() 
{
    if (!m_strMessage.IsEmpty()) {
        CString strTemp;
        strTemp.Format(szWindowTitle, (LPCTSTR)m_strMessage);
        SetWindowText((LPCTSTR)strTemp);
    }
	CDialog::OnInitDialog();
	
    // set default font
  	CHARFORMAT cf;

	cf.cbSize = sizeof (CHARFORMAT);  
	cf.dwMask = CFM_FACE; 
	lstrcpyn(cf.szFaceName, "Courier New", LF_FACESIZE); 
 
    m_recView.SetDefaultCharFormat(cf); 

    // open file
    if (m_strFileName.GetLength() <= _MAX_DRIVE) {
        m_recView.SetWindowText("No file selected.");
    } else {
        // get cluster size
        TCHAR szDrive[] = " :\\";
        szDrive[0] = m_strFileName[0];

        if (eraserError(eraserGetClusterSize((E_IN LPVOID)szDrive, 3,
                &m_dwClusterSize))) {
            m_dwClusterSize = 2048;
        }

        m_hFile = CreateFile((LPCTSTR)m_strFileName,
                             GENERIC_READ | GENERIC_WRITE,
                             FILE_SHARE_READ | FILE_SHARE_WRITE,
                             NULL,
                             OPEN_EXISTING,
                             0,
                             NULL);

        if (m_hFile == INVALID_HANDLE_VALUE) {
            m_recView.SetWindowText("Failed to open file.");
        } else {
            DisplayCluster(0);
        }
    }
	
	return TRUE;  // return TRUE unless you set the focus to a control
	              // EXCEPTION: OCX Property Pages should return FALSE
}

void CViewerDlg::OnDestroy() 
{
	CDialog::OnDestroy();
	
	CloseHandle(m_hFile);
    m_hFile = INVALID_HANDLE_VALUE;
}

BOOL CViewerDlg::DisplayCluster(DWORD dwCluster, DWORD dwSpecial)
{
    // get file size
    ULARGE_INTEGER uiFileSize;
    uiFileSize.LowPart = GetFileSize(m_hFile, &uiFileSize.HighPart);

    if (uiFileSize.LowPart == 0xFFFFFFFF && GetLastError() != NO_ERROR) {
        m_recView.SetWindowText("Failed to get file size.");
        return FALSE;
    } else {
        CString strText;
        ULARGE_INTEGER uiStart;
        BYTE szString[16 + 1];
        DWORD dwData[4];
        LPBYTE pbData = (LPBYTE)dwData;
        DWORD dwRead = 0;
    
        szString[16] = 0;
        uiStart.QuadPart = UInt32x32To64(dwCluster, m_dwClusterSize);

        if (dwSpecial == DISPLAY_LAST) {
            dwCluster = (DWORD)(uiFileSize.QuadPart / m_dwClusterSize);
            uiStart.QuadPart = UInt32x32To64(dwCluster, m_dwClusterSize);
            if (uiStart.QuadPart == uiFileSize.QuadPart) {
                dwCluster--;
                uiStart.QuadPart -= m_dwClusterSize;
            }
            m_dwCurrentCluster = dwCluster;
        } else if (uiStart.QuadPart >= uiFileSize.QuadPart) {
            dwCluster = 0;
            m_dwCurrentCluster = 0;
            uiStart.QuadPart = 0;
        }

        // don't draw to window until everything is formatted properly
        m_recView.SetRedraw(FALSE);

        // print headers
        strText.Format(szHeader, (LPCTSTR)m_strFileName, uiFileSize.QuadPart,
            dwCluster, m_dwClusterSize, uiStart.QuadPart);
        m_recView.SetWindowText((LPCTSTR)strText);

        // read the given cluster
        SetFilePointer(m_hFile, uiStart.LowPart, (LPLONG)&uiStart.HighPart, FILE_BEGIN);

        for (DWORD i = 0; i < m_dwClusterSize; i += 16) {
            memset(dwData, 0, 16);
            ReadFile(m_hFile, (LPVOID)dwData, 16, &dwRead, NULL);

            for (DWORD j = 0; j < 16; j++) {
                szString[j] = (TCHAR) ((isgraph((int)pbData[j])) ? pbData[j] : '.');
            }

            AddDataLine(i, dwData, (LPCTSTR)szString);

            if ((uiStart.QuadPart + i + 16) >= uiFileSize.QuadPart) {
                strText.Format(szEndOfFile, i + dwRead);
                AppendText((LPCTSTR)strText);
                break;
            }
        }

        // and draw to window
        m_recView.SetRedraw(TRUE);
        m_recView.Invalidate();

        return TRUE;
    }
}

void CViewerDlg::AddDataLine(DWORD dwOffset, DWORD *pdwData, LPCTSTR szString)
{
    CString strFormat;
    CHARFORMAT cf;

    // bold font
	cf.cbSize = sizeof(CHARFORMAT);
	cf.dwMask = CFM_BOLD;
	cf.dwEffects = CFE_BOLD;

    strFormat.Format(szLineFormat, dwOffset, pdwData[0], pdwData[1],
        pdwData[2], pdwData[3], szString);

    // this is faster than calling AppendFormattedText
    int iTextStart = m_recView.GetWindowTextLength();
    m_recView.SetSel(iTextStart, -1);
	m_recView.ReplaceSel(strFormat);

    // set bold text for offset
    m_recView.SetSel(iTextStart, iTextStart + iOffsetLength);
	m_recView.SetSelectionCharFormat(cf);

    // set bold text for the string
    m_recView.SetSel(iTextStart + iStringStart, iTextStart + iStringEnd);
	m_recView.SetSelectionCharFormat(cf);
}

int CViewerDlg::AppendText(LPCTSTR szString)
{
   	int iTextStart = m_recView.GetWindowTextLength();

    m_recView.SetSel(iTextStart, -1);
	m_recView.ReplaceSel(szString);
    
    return iTextStart;
}

void CViewerDlg::AppendFormattedText(LPCTSTR szString, CHARFORMAT& cf)
{
    m_recView.SetSel(AppendText(szString), -1);
	m_recView.SetSelectionCharFormat(cf);
}