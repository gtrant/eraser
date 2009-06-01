// SeException.h

// This code was contributed by Martin Ziacek (Martin.Ziacek@swh.sk) and was
// found from http://www.codeguru.com/

// From http://www.codeguru.com/submission_guide.shtml :
//
// "While we are talking about copyrights, you retain copyright of
//  your article and code but by submitting it to CodeGuru you give it
//  permission to use it in a fair manner and also permit all developers
//  to freely use the code in their own applications - even if they are
//  commercial."

#ifndef __SEEXCEPTION_H__
#define __SEEXCEPTION_H__

class CSeException : public CException
{
    DECLARE_DYNAMIC(CSeException)
public:
    CSeException(UINT nSeCode, _EXCEPTION_POINTERS* pExcPointers);
    CSeException(CSeException & CseExc);

    UINT                    GetSeCode(void);
    _EXCEPTION_POINTERS*    GetSePointers(void);
    PVOID                   GetExceptionAddress(void);

    void                    Delete(void);
    int                     ReportError(UINT nType = MB_OK, UINT nIDHelp = 0);
    BOOL                    GetErrorMessage(CString & CsErrDescr,
                                            PUINT pnHelpContext = NULL);
    BOOL                    GetErrorMessage(LPTSTR lpszError, UINT nMaxError,
                                            PUINT pnHelpContext = NULL);
private:
    UINT                    m_nSeCode;
    _EXCEPTION_POINTERS     *m_pExcPointers;
};

void SeTranslator(UINT nSeCode, _EXCEPTION_POINTERS* pExcPointers);

#endif //__SEEXCEPTION_H__
