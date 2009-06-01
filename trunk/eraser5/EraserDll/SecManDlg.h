#pragma once


#include "resource.h"
class CSecManDlg : public CDialog
{
	DECLARE_DYNAMIC(CSecManDlg)

public:
	CSecManDlg(CWnd* pParent = NULL);   // standard constructor
	virtual ~CSecManDlg();

// Dialog Data
	enum { IDD = IDD_DIALOG_SEC_MAN };
	enum Mode{CHECKUP = 0, SETUP =1};

	inline void SetMode(Mode m)
	{
		m_mMode = m;
	}
	inline Mode GetMode() const
	{
		return m_mMode;
	}
	inline const CString& GetSecret() const
	{
		return m_Password;
	}
	inline void Clear()
	{
		m_Password = "";
		m_PasswordConfirm = "";
	}
protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
private:
	CString m_Password;
	CString m_PasswordConfirm;
	Mode m_mMode;
protected:
	virtual void OnOK();
public:
	virtual BOOL OnInitDialog();
};
