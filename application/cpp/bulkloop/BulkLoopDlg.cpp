
// BulkLoopDlg.cpp : implementation file
//

#include "stdafx.h"
#include "BulkLoop.h"
#include "BulkLoopDlg.h"
#include <dbt.h>


#ifdef _DEBUG
#define new DEBUG_NEW
#endif
#define USB30 0x0300
#define     PACKETS_PER_TRANSFER                2
#define     NUM_TRANSFER_PER_TRANSACTION        16
#define     MAX_QUEUE_SZ                        64
#define     TIMEOUT_PER_TRANSFER_MILLI_SEC      1500

// CAboutDlg dialog used for App About

class CAboutDlg : public CDialog
{
public:
	CAboutDlg();

// Dialog Data
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
END_MESSAGE_MAP()


// CBulkLoopAppDlg dialog



BOOL CBulkLoopAppDlg::m_bBulkLoopCompleted = TRUE;
BOOL CBulkLoopAppDlg::m_bQuitApp = FALSE;
BOOL CBulkLoopAppDlg::m_bDeviceChanging = FALSE;
CBulkLoopAppDlg::CBulkLoopAppDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CBulkLoopAppDlg::IDD, pParent)
    , m_nStartValue(10)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDI_CYICON);
    m_selectedUSBDevice = NULL;
}

CBulkLoopAppDlg::~CBulkLoopAppDlg()
{
    if (m_selectedUSBDevice ) 
    {
        if (m_selectedUSBDevice->IsOpen() ) m_selectedUSBDevice->Close();
        delete m_selectedUSBDevice;
    }
}

void CBulkLoopAppDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    DDX_Control(pDX, IDC_CBO_DEVICES, m_cboDevices);
    DDX_Control(pDX, IDC_CBO_IN_ENDPOINTS, m_cboEndpointIN);
    DDX_Control(pDX, IDC_CBO_OUTENDPOINTS, m_cboEndpointOUT);
    DDX_Control(pDX, IDC_EDT_BYTE_OUT, m_edtBytesOut);
    DDX_Control(pDX, IDC_EDT_BYTE_IN, m_edtBytesIN);
    DDX_Control(pDX, IDC_BTN_START, m_btnStart);
    DDX_Control(pDX, IDC_RAD_CONSTANTBYTE, m_btnConstant);
    DDX_Control(pDX, IDC_RAD_RANDOMBYTE, m_btnRandom);
    DDX_Control(pDX, IDC_RAD_INCREMENTING, m_btnIncrementByte);
    DDX_Control(pDX, IDC_RAD_INCREMENTING_INT, m_btnIncrementInteger);
    DDX_Text(pDX, IDC_EDT_START_VALUE, m_nStartValue);
}

BEGIN_MESSAGE_MAP(CBulkLoopAppDlg, CDialog)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
    ON_MESSAGE(WM_EXIT_APP, OnUserAppClose)
    ON_BN_CLICKED(IDCANCEL, &CBulkLoopAppDlg::OnBnClickedCancel)
    ON_BN_CLICKED(IDOK, &CBulkLoopAppDlg::OnBnClickedOk)
    ON_BN_CLICKED(IDC_BTN_START, &CBulkLoopAppDlg::OnBnClickedBtnStart)
    ON_CBN_SELCHANGE(IDC_CBO_DEVICES, &CBulkLoopAppDlg::OnCbnSelchangeCboDevices)
END_MESSAGE_MAP()


// CBulkLoopAppDlg message handlers

BOOL CBulkLoopAppDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
    m_selectedUSBDevice = new CCyUSBDevice(this->m_hWnd, CYUSBDRV_GUID, true);
    this->m_btnStart.EnableWindow(FALSE);
    SurveyExistingDevices();
    EnumerateEndpointForTheSelectedDevice();
    m_btnConstant.SetCheck(BST_CHECKED);    
    m_edtBytesIN.SetWindowText(L"0");
    m_edtBytesOut.SetWindowText(L"0");
    m_bQuitApp = FALSE;
    m_bDeviceChanging = FALSE;
    m_hDeviceNotify = NULL;

    ((CEdit *)(GetDlgItem(IDC_EDT_START_VALUE)))->SetLimitText(10);

    RegisterDeviceInterface();

    UpdateData(FALSE);

    m_pThread = NULL;
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CBulkLoopAppDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CBulkLoopAppDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CBulkLoopAppDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}


void CBulkLoopAppDlg::OnBnClickedCancel()
{
    // TODO: Add your control notification handler code here
    if (m_pThread != NULL )
    {
        m_bQuitApp = TRUE;
        m_bBulkLoopCompleted = TRUE;
        return;
    }

    if (m_hDeviceNotify != NULL )
    {
        UnregisterDeviceNotification(*m_hDeviceNotify);    
        delete m_hDeviceNotify;
        m_hDeviceNotify = NULL;
    }
    OnCancel();
}

void CBulkLoopAppDlg::OnBnClickedOk()
{
    
}

void CBulkLoopAppDlg::OnBnClickedBtnStart()
{
    // TODO: Add your control notification handler code here
    CString strButtonText;
    m_btnStart.GetWindowText(strButtonText);

    UpdateData(TRUE);
    if (strButtonText == L"Start" )
    {
        m_btnStart.SetWindowText(L"Stop");
        m_pThread = AfxBeginThread((AFX_THREADPROC)PerformBulkloopTransfer, (LPVOID)this);
        m_bBulkLoopCompleted = FALSE;
        return;
    }
    m_bBulkLoopCompleted = TRUE;
    WaitForSingleObject(m_pThread->m_hThread, 100);    
    m_pThread = NULL;
    m_btnStart.SetWindowText(L"Start");
}

void CBulkLoopAppDlg::stuffBuff(PUCHAR buf, LONG len, LONG& seed, int method) 
{

    DWORD *dwBuf = (DWORD *) buf;
    srand((UINT)seed);

    int cnt = (method == INCREMENTING_INTEGER) ? len / 4 : len;
    for (int i=0; i<cnt; i++) 
    {
        switch (method) 
        {
                case CONSTANT:
                    buf[i] = (CHAR) seed;
                    break;
                case RANDOM:
                    buf[i] = rand();
                    break;
                case INCREMENTING_BYTE:
                    buf[i] = (UCHAR)seed + i;
                    break;
                case INCREMENTING_INTEGER:
                    dwBuf[i] = seed + i;
                    break;
        }
    }
    if (method >= INCREMENTING_BYTE) seed += cnt;
}

DWORD WINAPI CBulkLoopAppDlg::PerformBulkloopTransfer(LPVOID lParam)
{
    CBulkLoopAppDlg *pThis = (CBulkLoopAppDlg *)lParam;
    if ((pThis->m_cboEndpointIN.GetCount() == 0) || (pThis->m_cboEndpointOUT.GetCount() == 0) ) return 0;

    pThis->m_edtBytesIN.SetWindowText(L"0x0");
    pThis->m_edtBytesOut.SetWindowText(L"0x0");

    pThis->m_cboEndpointIN.EnableWindow(FALSE);
    pThis->m_cboEndpointOUT.EnableWindow(FALSE);
    pThis->m_btnConstant.EnableWindow(FALSE);
    pThis->m_btnRandom.EnableWindow(FALSE);
    pThis->m_btnIncrementByte.EnableWindow(FALSE);
    pThis->m_btnIncrementInteger.EnableWindow(FALSE);
    pThis->GetDlgItem(IDC_EDT_START_VALUE)->EnableWindow(FALSE);

    CString strINData, strOutData;
    TCHAR *pEnd;
    BYTE inEpAddress = 0x0, outEpAddress = 0x0;
    pThis->m_cboEndpointIN.GetWindowText(strINData);
    pThis->m_cboEndpointOUT.GetWindowText(strOutData);

    // Extract the endpoint addresses........
    strINData = strINData.Right(4);
    strOutData = strOutData.Right(4);

    inEpAddress = (BYTE)wcstoul(strINData.GetBuffer(0), &pEnd, 16);
    outEpAddress = (BYTE)wcstoul(strOutData.GetBuffer(0), &pEnd, 16);
    CCyUSBEndPoint *epBulkOut   = pThis->m_selectedUSBDevice->EndPointOf(outEpAddress);
    CCyUSBEndPoint *epBulkIn    = pThis->m_selectedUSBDevice->EndPointOf(inEpAddress);

    if (epBulkOut == NULL || epBulkIn == NULL ) return 1;

    //
    // Get the max packet size (USB Frame Size).
    // For bulk burst transfer, this size represent bulk burst size.
    // Transfer size is now multiple USB frames defined by PACKETS_PER_TRANSFER
    //
    UCHAR QueueSize = NUM_TRANSFER_PER_TRANSACTION;
    long totalTransferSize = epBulkIn->MaxPktSize * PACKETS_PER_TRANSFER;
    epBulkIn->SetXferSize(totalTransferSize);

    long totalOutTransferSize = epBulkOut->MaxPktSize * PACKETS_PER_TRANSFER;
    epBulkOut->SetXferSize(totalOutTransferSize);

    PUCHAR			*buffersInput		= new PUCHAR[QueueSize];
    PUCHAR			*contextsInput		= new PUCHAR[QueueSize];
    OVERLAPPED		inOvLap[MAX_QUEUE_SZ];

    // Allocate all the buffers for the queues
    for (int nCount = 0; nCount < QueueSize; nCount++) 
    { 
        buffersInput[nCount]        = new UCHAR[totalTransferSize];
        inOvLap[nCount].hEvent = CreateEvent(NULL, false, false, NULL);

        memset(buffersInput[nCount],0xEF,totalTransferSize);
    }

    OVERLAPPED  outOvLap;
    UCHAR      *bufferOutput   = new UCHAR[totalOutTransferSize];
    outOvLap.hEvent = CreateEvent(NULL, false, false, NULL);

    CString strStartValue;    
    strStartValue.Format(L"%d", pThis->m_nStartValue);
    strStartValue.Trim();
    UINT nRadix = 10;
    if (strStartValue.Left(2).CompareNoCase(L"0x") == 0 ) nRadix = 16;
    LONG byteStart = (DWORD)wcstoul(strStartValue.GetBuffer(0), &pEnd, nRadix);

    if (pThis->m_btnConstant.GetCheck() == BST_CHECKED )
        memset(bufferOutput, (BYTE)byteStart,totalOutTransferSize);
    else {
        memset(bufferOutput,0x0,totalOutTransferSize);    
    }

    epBulkOut->TimeOut = TIMEOUT_PER_TRANSFER_MILLI_SEC;

    // Queue-up the first batch of transfer requests
    for (int nCount = 0; nCount < QueueSize; nCount++)	
    {        
        ////////////////////BeginDataXFer will kick start the IN transactions.................
        contextsInput[nCount] = epBulkIn->BeginDataXfer(buffersInput[nCount], totalTransferSize, &inOvLap[nCount]);
        if (epBulkIn->NtStatus || epBulkIn->UsbdStatus) 
        {
            // BeginDataXfer failed
            // Handle the error now.
            epBulkIn->Abort();
            for (int j=0; j< QueueSize; j++)
            {
                CloseHandle(inOvLap[j].hEvent);
                delete [] buffersInput[j];        
            }   

            // Bail out......
            delete []contextsInput;
            delete [] buffersInput;
            CString strMsg;
            strMsg.Format(L"BeginDataXfer Failed with (NT Status = 0x%X and USBD Status = 0x%X). Bailing out...", epBulkIn->NtStatus, epBulkIn->UsbdStatus);
            AfxMessageBox(strMsg);
            return 0;
        }
    }

    // Mark the start time
    /*SYSTEMTIME objStartTime;
    GetSystemTime(&objStartTime);*/

    long nCount = 0;
    long BytesXferred = 0;
    long outTransferred = 0;
    while (m_bBulkLoopCompleted == FALSE )
    {
        long readLength = totalTransferSize;
        long writeLength = totalOutTransferSize;

        if (pThis->m_btnConstant.GetCheck() != BST_CHECKED )
        {
            int nMenthod = (int)((pThis->m_btnRandom.GetCheck() == BST_CHECKED) ? RANDOM : ((pThis->m_btnIncrementByte.GetCheck() == BST_CHECKED) ? INCREMENTING_BYTE : INCREMENTING_INTEGER));
            pThis->stuffBuff(bufferOutput, writeLength, byteStart, nMenthod);
        }

        if (epBulkOut->XferData(bufferOutput, writeLength) == TRUE )
            outTransferred += totalOutTransferSize;

        //////////Wait till the transfer completion..///////////////////////////
        if (!epBulkIn->WaitForXfer(&inOvLap[nCount], TIMEOUT_PER_TRANSFER_MILLI_SEC))
        {
            epBulkIn->Abort();
            if (epBulkIn->LastError == ERROR_IO_PENDING)
                WaitForSingleObject(inOvLap[nCount].hEvent, TIMEOUT_PER_TRANSFER_MILLI_SEC);
        }
        
        ////////////Read the trasnferred data from the device///////////////////////////////////////
        if (epBulkIn->FinishDataXfer(buffersInput[nCount], readLength, &inOvLap[nCount], contextsInput[nCount])) 
            BytesXferred += totalTransferSize;

        //////////BytesXFerred is need for current data rate calculation.
        ///////// Refer to CalculateTransferSpeed function for the exact 
        ///////// calculation.............................
        //if (BytesXferred < 0) // Rollover - reset counters
        //{
        //    BytesXferred = 0;
        //    GetSystemTime(&objStartTime);
        //}

        CString strBytes;
        strBytes.Format(L"0x%X", BytesXferred);
        pThis->m_edtBytesIN.SetWindowText(strBytes);
        strBytes.Format(L"0x%X", outTransferred);
        pThis->m_edtBytesOut.SetWindowText(strBytes);

        // Re-submit this queue element to keep the queue full
        contextsInput[nCount] = epBulkIn->BeginDataXfer(buffersInput[nCount], totalTransferSize, &inOvLap[nCount]);
        if (epBulkIn->NtStatus || epBulkIn->UsbdStatus) 
        {
            // BeginDataXfer failed............
            // Time to bail out now............
            epBulkIn->Abort();
            for (int j=0; j< QueueSize; j++)
            {
                CloseHandle(inOvLap[j].hEvent);
                delete [] buffersInput[j];        
             }   
            delete []contextsInput;

            CString strMsg;
            strMsg.Format(L"BeginDataXfer Failed during buffer re-cycle (NT Status = 0x%X and USBD Status = 0x%X). Bailing out...", epBulkIn->NtStatus, epBulkIn->UsbdStatus);
            AfxMessageBox(strMsg);            
            return 0;
        }
        if (++nCount >= QueueSize) nCount = 0;
    }

    epBulkIn->Abort();
    for (int j=0; j< QueueSize; j++)
    {
        CloseHandle(inOvLap[j].hEvent);
        delete [] buffersInput[j];        
    }   

    // Bail out......
    delete []contextsInput;
    delete [] buffersInput;
    delete [] bufferOutput;
    CloseHandle(outOvLap.hEvent);

    pThis->m_cboEndpointIN.EnableWindow(TRUE);
    pThis->m_cboEndpointOUT.EnableWindow(TRUE);
    pThis->m_btnConstant.EnableWindow(TRUE);
    pThis->m_btnRandom.EnableWindow(TRUE);
    pThis->m_btnIncrementByte.EnableWindow(TRUE);
    pThis->m_btnIncrementInteger.EnableWindow(TRUE);
    pThis->GetDlgItem(IDC_EDT_START_VALUE)->EnableWindow(TRUE);

    if (m_bQuitApp || m_bDeviceChanging ) pThis->PostMessage(WM_EXIT_APP, 0, 0);

    return 1;
}

LRESULT CBulkLoopAppDlg::OnUserAppClose(WPARAM wParam, LPARAM lParam)
{
    if (m_bQuitApp == TRUE)
    {
        if (m_hDeviceNotify != NULL )
        {
            UnregisterDeviceNotification(*m_hDeviceNotify);    
            delete m_hDeviceNotify;
            m_hDeviceNotify = NULL;
        }

        OnCancel();
    }
    else if (m_bDeviceChanging == TRUE )
    {
        m_pThread = NULL;
        m_bDeviceChanging = FALSE;
        this->m_btnStart.SetWindowText(L"Start");

        EnumerateEndpointForTheSelectedDevice();
        m_edtBytesIN.SetWindowText(L"0x0");
        m_edtBytesOut.SetWindowText(L"0x0");

    }
    return 1L;
}

BOOL CBulkLoopAppDlg::SurveyExistingDevices()
{
    CCyUSBDevice	*USBDevice;
    USBDevice = new CCyUSBDevice(this->m_hWnd, CYUSBDRV_GUID, true);
    CString strDevice(L"");
    int nCboIndex = -1;
    if (m_cboDevices.GetCount() > 0 ) m_cboDevices.GetWindowText(strDevice);
    
    m_cboDevices.ResetContent();

    if (USBDevice != NULL) 
    {
        int nInsertionCount = 0;
        int nDeviceCount = USBDevice->DeviceCount();
        for (int nCount = 0; nCount < nDeviceCount; nCount++ )
        {
            CString strDeviceData;
            USBDevice->Open(nCount);
            strDeviceData.Format(L"(0x%04X - 0x%04X) %s", USBDevice->VendorID, USBDevice->ProductID, CString(USBDevice->FriendlyName));
            m_cboDevices.InsertString(nInsertionCount++, strDeviceData);
            if (nCboIndex == -1 && strDevice.IsEmpty() == FALSE && strDevice == strDeviceData ) 
                nCboIndex = nCount;

            USBDevice->Close();
        }
        delete USBDevice;
        if (m_cboDevices.GetCount() >= 1 ) 
        {   
            if (nCboIndex != -1 ) m_cboDevices.SetCurSel(nCboIndex);
            else m_cboDevices.SetCurSel(0);
        }
        SetFocus();
    }
    else return FALSE;

    return TRUE;
}

BOOL CBulkLoopAppDlg::EnumerateEndpointForTheSelectedDevice()
{
    int nDeviceIndex = 0;
    // Is there any FX device connected to system?
    if ((nDeviceIndex = m_cboDevices.GetCurSel()) == -1 || m_selectedUSBDevice == NULL ) return FALSE;
    
    // There are devices connected in the system.       
    m_selectedUSBDevice->Open(nDeviceIndex);
    int interfaces = this->m_selectedUSBDevice->AltIntfcCount()+1;
    m_cboEndpointIN.ResetContent();
    m_cboEndpointOUT.ResetContent();

    for (int nDeviceInterfaces = 0; nDeviceInterfaces < interfaces; nDeviceInterfaces++ )
    {
        m_selectedUSBDevice->SetAltIntfc(nDeviceInterfaces);
        int eptCnt = m_selectedUSBDevice->EndPointCount();

        // Fill the EndPointsBox
        for (int endPoint = 1; endPoint < eptCnt; endPoint++)
        {
            CCyUSBEndPoint *ept = m_selectedUSBDevice->EndPoints[endPoint];

            // INTR, BULK and ISO endpoints are supported.
            if (ept->Attributes == 2)
            {
                CString strData(L""), strTemp;
                
                strData += ((ept->Attributes == 1) ? L"ISOC " : ((ept->Attributes == 2) ? L"BULK " : L"INTR "));
                strData += (ept->bIn ? L"IN, " : L"OUT, ");
                //strTemp.Format(L"%d  Bytes,", ept->MaxPktSize);
                //strData += strTemp;
                //
                //if(m_selectedUSBDevice->BcdUSB == USB30)
                //{
                //    strTemp.Format(L"%d  MaxBurst,", ept->ssmaxburst);
                //    strData += strTemp;
                //}

                strTemp.Format(L"AltInt - %d and EpAddr - 0x%02X", nDeviceInterfaces, ept->Address);
                strData += strTemp;
                if (ept->bIn ) this->m_cboEndpointIN.AddString(strData);
                else this->m_cboEndpointOUT.AddString(strData);
            }
        }        
    }

    if (m_cboEndpointOUT.GetCount() > 0 ) m_cboEndpointOUT.SetCurSel(0);
    if (m_cboEndpointIN.GetCount() > 0 ) m_cboEndpointIN.SetCurSel(0);

    this->m_btnStart.EnableWindow((m_cboEndpointIN.GetCount() > 0 && m_cboEndpointIN.GetCount() > 0));

    return TRUE;

}
void CBulkLoopAppDlg::OnCbnSelchangeCboDevices()
{
    if (this->m_pThread == NULL )
    {
        EnumerateEndpointForTheSelectedDevice();
        m_edtBytesIN.SetWindowText(L"0x0");
        m_edtBytesOut.SetWindowText(L"0x0");
    }
    else
    {
        m_bDeviceChanging = TRUE;
        this->m_bBulkLoopCompleted = TRUE;
    }    
}

BOOL CBulkLoopAppDlg::RegisterDeviceInterface()
{
    DEV_BROADCAST_DEVICEINTERFACE NotificationFilter;

    ZeroMemory( &NotificationFilter, sizeof(NotificationFilter) );
    NotificationFilter.dbcc_size = sizeof(DEV_BROADCAST_DEVICEINTERFACE);
    NotificationFilter.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
    NotificationFilter.dbcc_classguid = CYUSBDRV_GUID;

    if (m_hDeviceNotify == NULL ) m_hDeviceNotify = new HDEVNOTIFY;
    *m_hDeviceNotify = RegisterDeviceNotification( 
        this->m_hWnd,                       // events recipient
        &NotificationFilter,        // type of device
        DEVICE_NOTIFY_WINDOW_HANDLE // type of recipient handle
        );

    if ( NULL == *m_hDeviceNotify ) 
    {
        //ErrorHandler(TEXT("RegisterDeviceNotification"));
        delete m_hDeviceNotify;
        m_hDeviceNotify = NULL;
        return FALSE;
    }

    return TRUE;
}

LRESULT CBulkLoopAppDlg::DefWindowProc(UINT message, WPARAM wParam, LPARAM lParam)
{
    if (message == WM_DEVICECHANGE && wParam >= DBT_DEVICEARRIVAL)
    {
        PDEV_BROADCAST_HDR lpdb = (PDEV_BROADCAST_HDR)lParam;
        if (wParam == DBT_DEVICEARRIVAL && lpdb->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
        {               
            SurveyExistingDevices();
            if (this->m_pThread == NULL) EnumerateEndpointForTheSelectedDevice();
        }
        else if (wParam == DBT_DEVICEREMOVECOMPLETE && lpdb->dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
        {
            SurveyExistingDevices();
            if (this->m_pThread == NULL) EnumerateEndpointForTheSelectedDevice();
        }
        lpdb->dbch_devicetype;
        lpdb->dbch_size;
    }
    return CDialog::DefWindowProc(message, wParam, lParam);
}