// BulkLoopDlg.h : header file
//

#if !defined(AFX_BULKLOOPDLG_H__D3E75ECD_0ADD_4838_B4EE_E39E6FC7B4B8__INCLUDED_)
#define AFX_BULKLOOPDLG_H__D3E75ECD_0ADD_4838_B4EE_E39E6FC7B4B8__INCLUDED_

#pragma once
#include "afxwin.h"
//#include "..\Library\inc\cyapi.h"
#include "CyAPI.h"

#define WM_EXIT_APP     WM_USER+152

typedef enum { CONSTANT = 0, RANDOM, INCREMENTING_BYTE, INCREMENTING_INTEGER } packageType;
// CBulkLoopAppDlg dialog
class CBulkLoopAppDlg : public CDialog
{
// Construction
public:
	CBulkLoopAppDlg(CWnd* pParent = NULL);	// standard constructor
    virtual ~CBulkLoopAppDlg();

// Dialog Data
	enum { IDD = IDD_BULKLOOP_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
    virtual LRESULT DefWindowProc(UINT message, WPARAM wParam, LPARAM lParam);

	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
    afx_msg LRESULT OnUserAppClose(WPARAM wParam, LPARAM lParam);
	DECLARE_MESSAGE_MAP()
public:
    afx_msg void OnBnClickedCancel();
    afx_msg void OnBnClickedOk();
    afx_msg void OnBnClickedBtnStart();
private:
    CComboBox m_cboDevices;
    CComboBox m_cboEndpointIN;
    CComboBox m_cboEndpointOUT;
    CEdit m_edtBytesOut;
    CEdit m_edtBytesIN;
    CButton m_btnStart;
    CButton m_btnConstant;
    CButton m_btnRandom;
    CButton m_btnIncrementByte;
    CButton m_btnIncrementInteger;

///////////Variable declaration specific to functionality//////
    CCyUSBDevice	*m_selectedUSBDevice;
    static BOOL     m_bBulkLoopCompleted;
    static BOOL     m_bQuitApp;
    static BOOL     m_bDeviceChanging;
    CWinThread      *m_pThread;
    HDEVNOTIFY      *m_hDeviceNotify;

// Function Declaration Starts here.........
    BOOL SurveyExistingDevices();
    BOOL EnumerateEndpointForTheSelectedDevice();
    static DWORD WINAPI PerformBulkloopTransfer(LPVOID lParam);
    void stuffBuff(PUCHAR buf, LONG len, LONG& seed, int method);
    BOOL RegisterDeviceInterface();
    
public:
    afx_msg void OnCbnSelchangeCboDevices();
    ULONG m_nStartValue;
};

#endif
