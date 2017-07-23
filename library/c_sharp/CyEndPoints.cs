/*
 ## Cypress CyUSB C# library source file (CyEndPoints.cs)
 ## =======================================================
 ##
 ##  Copyright Cypress Semiconductor Corporation, 2009-2012,
 ##  All Rights Reserved
 ##  UNPUBLISHED, LICENSED SOFTWARE.
 ##
 ##  CONFIDENTIAL AND PROPRIETARY INFORMATION
 ##  WHICH IS THE PROPERTY OF CYPRESS.
 ##
 ##  Use of this file is governed
 ##  by the license agreement included in the file
 ##
 ##  <install>/license/license.rtf
 ##
 ##  where <install> is the Cypress software
 ##  install root directory path.
 ##
 ## =======================================================
*/
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Runtime.Remoting;



namespace CyUSB
{
    /// <summary>
    /// The CyUSBEndPoint ABSTRACT Class
    /// </summary>
    public abstract class CyUSBEndPoint
    {
        protected IntPtr _hDevice;
        public IntPtr hDevice
        {
            get { return _hDevice; }
        }

        // The fields of an EndPoint Descriptor
        protected byte _dscLen;
        public byte DscLen { get { return _dscLen; } }

        protected byte _dscType;
        public byte DscType { get { return _dscType; } }

        protected byte _address;
        public byte Address { get { return _address; } }

        protected byte _attributes;
        public byte Attributes { get { return _attributes; } }

        protected int _maxPktSize;
        public int MaxPktSize { get { return _maxPktSize; } }

        protected byte _interval;
        public byte Interval { get { return _interval; } }

        // Super speed endpoint companion
        protected byte _ssdscLen;
        public byte SSDscLen { get { return _ssdscLen; } }

        protected byte _ssdscType;
        public byte SSDscType { get { return _ssdscType; } }

        protected byte _ssmaxburst; /* Maximum number of packets endpoint can send in one burst*/
        public byte SSMaxBurst { get { return _ssmaxburst; } }

        protected byte _ssbmAttribute; // store endpoint attribute like for bulk it will be number of streams
        public byte SSBmAttribute { get { return _ssbmAttribute; } }

        protected ushort _ssbytesperinterval;
        public ushort SSBytePerInterval { get { return _ssbytesperinterval; } }

        // Other fields
        protected uint _timeOut = 10000;  // 10 Sec timeout is default;
        public uint TimeOut
        {
            get { return _timeOut; }
            set { _timeOut = value; }
        }

        protected uint _usbdStatus;
        public uint UsbdStatus { get { return _usbdStatus; } }

        protected uint _ntStatus;
        public uint NtStatus { get { return _ntStatus; } }

        protected uint _bytesWritten;
        public uint BytesWritten { get { return _bytesWritten; } }

        protected uint _lastError;
        public uint LastError { get { return _lastError; } }

        protected bool _bIn;
        public bool bIn { get { return _bIn; } }

        unsafe protected OVERLAPPED Ovlap;
        public int OverlapSignalAllocSize { get { return Marshal.SizeOf(Ovlap); } }

        // Will be XMODE.DIRECT if the driver is version 1.05.0500 or later.
        public XMODE _xferMode = XMODE.DIRECT;
        public XMODE XferMode
        {
            get { return _xferMode; }
            set { _xferMode = value; }
        }

        // Constructor
        internal unsafe CyUSBEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor)
        {
            _hDevice = h;

            if (EndPtDescriptor != (USB_ENDPOINT_DESCRIPTOR*)0)
            {
                int pkts = (EndPtDescriptor->wMaxPacketSize & 0x1800) >> 11;
                pkts++;

                _dscLen = EndPtDescriptor->bLength;
                _dscType = EndPtDescriptor->bDescriptorType;
                _address = EndPtDescriptor->bEndpointAddress;
                _attributes = EndPtDescriptor->bmAttributes;
                _maxPktSize = (EndPtDescriptor->wMaxPacketSize & 0x7ff) * pkts;
                _interval = EndPtDescriptor->bInterval;
                _bIn = (Address & 0x80) > 0;
            }
            // initialize all SS paramter to zero
            _ssdscLen = 0;
            _ssdscType = 0;
            _ssmaxburst = 0;
            _ssbmAttribute = 0;
            _ssbytesperinterval = 0;
        }
        internal unsafe CyUSBEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor, USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR* SSEndPtDescriptor)
        {
            _hDevice = h;

            if (EndPtDescriptor != (USB_ENDPOINT_DESCRIPTOR*)0)
            {
                int pkts = (EndPtDescriptor->wMaxPacketSize & 0x1800) >> 11;
                pkts++;

                _dscLen = EndPtDescriptor->bLength;
                _dscType = EndPtDescriptor->bDescriptorType;
                _address = EndPtDescriptor->bEndpointAddress;
                _attributes = EndPtDescriptor->bmAttributes;
                _maxPktSize = (EndPtDescriptor->wMaxPacketSize & 0x7ff) * pkts;
                _interval = EndPtDescriptor->bInterval;
                _bIn = (Address & 0x80) > 0;
            }
            if (SSEndPtDescriptor != (USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR*)0)
            {
                _ssdscLen = SSEndPtDescriptor->bLength;
                _ssdscType = SSEndPtDescriptor->bDescriptorType;
                _ssmaxburst = SSEndPtDescriptor->bMaxBurst;
                _maxPktSize *= (_ssmaxburst + 1); // Multiply the Maxpacket size with Max burst
                _ssbmAttribute = SSEndPtDescriptor->bmAttributes;
                if ((_attributes & 0x03) == 1) // MULT is valid for Isochronous transfer only
                    _maxPktSize *= ((SSEndPtDescriptor->bmAttributes & 0x03) + 1); // Adding the MULT fields.

                _ssbytesperinterval = SSEndPtDescriptor->bBytesPerInterval;

            }
        }

        public TreeNode Tree
        {
            get
            {
                string sType = "Bulk";
                if (Attributes == 1) sType = "Isoc";
                if (Attributes == 3) sType = "Interrupt";

                string sIn = (bIn) ? "in" : "out";

                string tmp = string.Format("{0} {1} endpoint (0x{2:X2})", sType, sIn, Address);
                TreeNode t = new TreeNode(tmp);
                t.Tag = this;

                return t;
            }

        }

        public override string ToString()
        {
            string sType = "BULK";
            if (Attributes == 0) sType = "CONTROL";
            if (Attributes == 1) sType = "ISOC";
            if (Attributes == 3) sType = "INTERRUPT";

            string sIn = (bIn) ? "IN" : "OUT";
            if (Attributes == 0) sIn = "BIDI";

            StringBuilder s = new StringBuilder("\t\t\t<ENDPOINT>\r\n");

            s.Append(string.Format("\t\t\t\tType=\"{0}\"\r\n", sType));
            s.Append(string.Format("\t\t\t\tDirection=\"{0}\"\r\n", sIn));
            s.Append(string.Format("\t\t\t\tAddress=\"{0:X2}h\"\r\n", Address));
            s.Append(string.Format("\t\t\t\tAttributes=\"{0:X2}h\"\r\n", Attributes));
            s.Append(string.Format("\t\t\t\tMaxPktSize=\"{0}\"\r\n", MaxPktSize));
            s.Append(string.Format("\t\t\t\tDescriptorType=\"{0}\"\r\n", DscType));
            s.Append(string.Format("\t\t\t\tDescriptorLength=\"{0}\"\r\n", DscLen));
            s.Append(string.Format("\t\t\t\tInterval=\"{0}\"\r\n", Interval));

            if (_ssdscLen != 0)
            {// USB3.0 super speed device endpoint
                string sSSType = "SUPERSPEED_USB_ENDPOINT_COMPANION";

                s.Append(string.Format("\t\t\t<SUPER SPEED ENDPOINT COMPANION>\r\n"));
                s.Append(string.Format("\t\t\t\tType=\"{0}\"\r\n", sSSType));
                s.Append(string.Format("\t\t\t\tMaxBurst=\"{0}\"\r\n", SSMaxBurst));
                s.Append(string.Format("\t\t\t\tAttributes=\"{0:X2}h\"\r\n", SSBmAttribute));
                s.Append(string.Format("\t\t\t\tBytesPerInterval=\"{0:X2}h\"\r\n", SSBytePerInterval));
            }
            s.Append("\t\t\t</ENDPOINT>\r\n");
            return s.ToString();
        }

        public unsafe bool XferData(ref byte[] buf, ref int len, bool PacketMode)
        {
            if ((bIn == false) || (PacketMode == false))
            {
                return XferData(ref buf, ref len);
            }
            else
            {
                int size = 0;
                int xferLen = MaxPktSize;
                bool status = true;
                byte[] bufPtr = new byte[MaxPktSize];

                while (status && (size < len))
                {
                    if ((len - size) < MaxPktSize)
                        xferLen = len - size;

                    status = XferData(ref bufPtr, ref xferLen);
                    if (status)
                    {
                        for (int i = 0; i < xferLen; ++i)
                        {
                            buf[size + i] = bufPtr[i];
                        }
                        size += xferLen;

                        if (xferLen < MaxPktSize)
                            break;
                    }
                }

                len = size;
                if (len > 0)
                    return true;
                return status;
            }
        }

        // These used by both BULK and INTERRUPT endpoints
        public unsafe virtual bool XferData(ref byte[] buf, ref int len)
        {
            byte[] ovLap = new byte[OverlapSignalAllocSize];

            fixed (byte* fixedOvLap = ovLap)
            {
                OVERLAPPED* ovLapStatus = (OVERLAPPED*)fixedOvLap;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                // This SINGLE_TRANSFER buffer must be allocated at this level.
                int bufSz = CyConst.SINGLE_XFER_LEN + ((XferMode == XMODE.DIRECT) ? 0 : len);
                byte[] cmdBuf = new byte[bufSz];

                // These nested fixed blocks ensure that the buffers don't move in memory
                // While we're doing the asynchronous IO - Begin/Wait/Finish
                fixed (byte* tmp1 = cmdBuf, tmp2 = buf)
                {
                    bool bResult = BeginDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);
                    //
                    //  This waits for driver to call IoRequestComplete on the IRP
                    //  we just sent.
                    //
                    bool wResult = WaitForIO(ovLapStatus->hEvent);
                    bool fResult = FinishDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);

                    PInvoke.CloseHandle(ovLapStatus->hEvent);

                    return wResult && fResult;
                }
            }
        }

        public unsafe virtual bool BeginDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov)
        {
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            Int32 cmdLen = singleXfer.Length;

            fixed (byte* buf = singleXfer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->WaitForever = 0;
                transfer->ucEndpointAddress = Address;
                transfer->IsoPacketLength = 0;

                int[] Xferred = new int[1];
                Xferred[0] = 0;
                uint IOCTL;

                if (XferMode == XMODE.BUFFERED)
                {
                    transfer->BufferOffset = CyConst.SINGLE_XFER_LEN;
                    transfer->BufferLength = (uint)len;
                    IOCTL = CyConst.IOCTL_ADAPT_SEND_NON_EP0_TRANSFER;

                    for (int i = 0; i < len; i++) buf[CyConst.SINGLE_XFER_LEN + i] = buffer[i];

                    fixed (byte* lpInBuffer = singleXfer)
                    {
                        fixed (byte* lpOutBuffer = singleXfer)
                        {
                            fixed (byte* lpOvLap = ov)
                            {
                                fixed (int* lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice, IOCTL,
                                        (IntPtr)lpInBuffer, cmdLen, (IntPtr)lpOutBuffer, cmdLen,
                                        (IntPtr)lpBytesXfered, (IntPtr)lpOvLap);
                                }
                            }
                        }
                    }
                }
                else
                {
                    transfer->BufferOffset = 0;
                    transfer->BufferLength = 0;
                    IOCTL = CyConst.IOCTL_ADAPT_SEND_NON_EP0_DIRECT;

                    fixed (byte* lpInBuffer = singleXfer)
                    {
                        fixed (byte* lpOutBuffer = buffer)
                        {
                            fixed (byte* lpOvLap = ov)
                            {
                                fixed (int* lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice, IOCTL,
                                        (IntPtr)lpInBuffer, cmdLen, (IntPtr)lpOutBuffer, len,
                                        (IntPtr)lpBytesXfered, (IntPtr)lpOvLap);
                                }
                            }
                        }
                    }

                }

                if (Xferred[0] > 0)
                    len = Xferred[0];

                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;
            }

            _lastError = (uint)Marshal.GetLastWin32Error();

            return true;

        }


        public unsafe virtual bool FinishDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov)
        {
            bool rResult;
            uint[] bytes = new uint[1];

            // DWY fix single variable during call by converting it into an 'array of 1'
            //  and fixing it to a pointer.
            fixed (uint* buf0 = bytes)
            {
                fixed (byte* buf = singleXfer)
                {
                    SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                    rResult = PInvoke.GetOverlappedResult(_hDevice, ov, ref bytes[0], 0);
                    if (rResult == false) transfer->NtStatus = PInvoke.GetLastError();
                }
            }

            len = (int)bytes[0];

            fixed (byte* buf = singleXfer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if ((XferMode == XMODE.BUFFERED) && (len > 0))
                {
                    //len -= (int)transfer->BufferOffset; This is not required becuse we pass the actual data buffer length
                    for (int i = 0; i < len; i++) buffer[i] = buf[transfer->BufferOffset + i];
                }
            }

            _bytesWritten = (uint)len;

            return rResult && (_usbdStatus == 0) && (_ntStatus == 0);
        }


        public bool WaitForXfer(IntPtr ovlapEvent, uint tOut)
        {
            
            Int32 waitResult = PInvoke.WaitForSingleObject(ovlapEvent, tOut);
            if (waitResult == CyConst.WAIT_OBJECT_0)
            {
                return true;
            }            
            return false;
        }


        internal bool WaitForIO(IntPtr ovlapEvent)
        {
            //_lastError = (ushort)Marshal.GetLastWin32Error();

            //if (_lastError == CyConst.ERROR_SUCCESS) return true;  // The command completed

            //if (_lastError == CyConst.ERROR_IO_PENDING)
            {
                Int32 waitResult = PInvoke.WaitForSingleObject(ovlapEvent, TimeOut);

                if (waitResult == CyConst.WAIT_OBJECT_0) return true;

                if (waitResult == CyConst.WAIT_TIMEOUT)
                {
                    Abort();
                    // Wait for the stalled command to complete - should be done already
                    PInvoke.WaitForSingleObject(ovlapEvent, CyConst.INFINITE);
                }
            }

            return false;
        }


        // Implemented as a property
        public unsafe int XferSize
        {
            get
            {
                if (_hDevice == CyConst.INVALID_HANDLE) return 0;

                SET_TRANSFER_SIZE_INFO IdleXferVar = new SET_TRANSFER_SIZE_INFO();

                // The size of SET_TRANSFER_SIZE_INFO
                Int32 len = Marshal.SizeOf(IdleXferVar);

                byte[] buffer = new byte[len];

                bool bRetVal;

                fixed (byte* buf = buffer)
                {
                    SET_TRANSFER_SIZE_INFO* SetTransferInfo = (SET_TRANSFER_SIZE_INFO*)buf;
                    SetTransferInfo->EndpointAddress = Address;

                    int[] Xferred = new int[1];
                    Xferred[0] = 0;

                    fixed (byte* lpInBuffer = buffer)
                    {
                        fixed (byte* lpOutBuffer = buffer)
                        {
                            fixed (int* lpBytesXfered = Xferred)
                            {
                                bRetVal = PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_ADAPT_GET_TRANSFER_SIZE,
                                    (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len,
                                    (IntPtr)lpBytesXfered, (IntPtr)null);
                            }
                        }
                    }

                    if (bRetVal && Xferred[0] >= len)
                        return SetTransferInfo->TransferSize;
                }

                return 0;
            }

            set
            {
                if (_hDevice == CyConst.INVALID_HANDLE) return;

                if (MaxPktSize == 0)
                    return;
                // Force a multiple of MaxPktSize
                int pkts = ((value % MaxPktSize) > 0) ? 1 + (value / MaxPktSize) : (value / MaxPktSize);
                int xferSize = pkts * MaxPktSize;

                int len = 5;  // The size of SET_TRANSFER_SIZE_INFO
                byte[] buffer = new byte[len];

                fixed (byte* buf = buffer)
                {
                    SET_TRANSFER_SIZE_INFO* SetTransferInfo = (SET_TRANSFER_SIZE_INFO*)buf;
                    SetTransferInfo->EndpointAddress = Address;
                    SetTransferInfo->TransferSize = xferSize;

                    int[] Xferred = new int[1];
                    Xferred[0] = 0;

                    fixed (byte* lpInBuffer = buffer)
                    {
                        fixed (byte* lpOutBuffer = buffer)
                        {
                            fixed (int* lpBytesXfered = Xferred)
                            {
                                PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_ADAPT_SET_TRANSFER_SIZE,
                                        (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len,
                                        (IntPtr)lpBytesXfered, (IntPtr)null);
                            }
                        }
                    }

                }

            }

        }



        private unsafe bool UnsafeReset()
        {
            int[] dwBytes = new int[1];
            dwBytes[0] = 0;
            byte[] buffer = new byte[1];
            buffer[0] = Address;

            fixed (byte* lpInBuffer = buffer)
            {
                fixed (int* lpBytesXfered = dwBytes)
                {

                    return PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_ADAPT_RESET_PIPE,
                        (IntPtr)lpInBuffer, 1, (IntPtr)null, 0,
                        (IntPtr)lpBytesXfered, (IntPtr)null);
                }
            }
        }

        public bool Reset()
        {
            return UnsafeReset();

        }


        private unsafe bool UnsafeAbort()
        {
            int[] dwBytes = new int[1];
            dwBytes[0] = 0;
            byte[] buffer = new byte[1];
            buffer[0] = Address;

            fixed (byte* lpInBuffer = buffer)
            {
                fixed (int* lpBytesXfered = dwBytes)
                {
                    return PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_ADAPT_ABORT_PIPE,
                        (IntPtr)lpInBuffer, 1, (IntPtr)null, 0,
                        (IntPtr)lpBytesXfered, (IntPtr)null);
                }
            }

        }

        public bool Abort()
        {
            return (UnsafeAbort());
        }


    }


    /// <summary>
    /// The Control Endpoint Class
    /// </summary>
    public class CyControlEndPoint : CyUSBEndPoint
    {
        unsafe internal CyControlEndPoint(IntPtr h, int MaxPacketSize)
            : base(h, null)
        {
            _bIn = false;
            _dscLen = 7;
            _dscType = 5;
            _maxPktSize = MaxPacketSize;
        }

        byte _Target = CyConst.TGT_DEVICE;
        public byte Target
        {
            get { return _Target; }
            set { _Target = value; }
        }

        byte _ReqType = CyConst.REQ_VENDOR;
        public byte ReqType
        {
            get { return _ReqType; }
            set { _ReqType = value; }
        }

        byte _Direction = CyConst.DIR_TO_DEVICE;
        public byte Direction
        {
            get { return _Direction; }
            set
            {
                _Direction = value;
                _bIn = (_Direction == CyConst.DIR_FROM_DEVICE);
            }
        }

        byte _ReqCode;
        public byte ReqCode
        {
            get { return _ReqCode; }
            set { _ReqCode = value; }
        }

        ushort _Value;
        public ushort Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        ushort _Index;
        public ushort Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        public bool Read(ref byte[] buf, ref int len)
        {
            Direction = CyConst.DIR_FROM_DEVICE;
            return XferData(ref buf, ref len);
        }


        public bool Write(ref byte[] buf, ref int len)
        {
            Direction = CyConst.DIR_TO_DEVICE;
            return XferData(ref buf, ref len);
        }


        // Control endpoint uses the BUFFERED xfer method.  So that it
        // doesn't collide with the base class' XferData, we declare it 'new'
        public new unsafe bool XferData(ref byte[] buf, ref int len)
        {
            byte[] ovLap = new byte[sizeof(OVERLAPPED)];

            fixed (byte* tmp0 = ovLap)
            {
                OVERLAPPED* ovLapStatus = (OVERLAPPED*)tmp0;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                bool bResult, wResult, fResult;

                // Create a temporary buffer that will contain a SINGLE_TRANSFER structure
                // followed by the actual data.
                byte[] tmpBuf = new byte[CyConst.SINGLE_XFER_LEN + len];
                for (int i = 0; i < len; i++)
                    tmpBuf[CyConst.SINGLE_XFER_LEN + i] = buf[i];

                GCHandle bufSingleTransfer = GCHandle.Alloc(tmpBuf, GCHandleType.Pinned);
                GCHandle bufDataAllocation = GCHandle.Alloc(buf, GCHandleType.Pinned);

                fixed (int* lenTemp = &len)
                {
                    bResult = BeginDataXfer(ref tmpBuf, ref *lenTemp, ref ovLap);
                    wResult = WaitForIO(ovLapStatus->hEvent);
                    fResult = FinishDataXfer(ref buf, ref tmpBuf, ref *lenTemp, ref ovLap);
                }

                PInvoke.CloseHandle(ovLapStatus->hEvent);
                bufSingleTransfer.Free();
                bufDataAllocation.Free();

                return wResult && fResult;
            }
        }


        // Control Endpoints don't support the Begin/Wait/Finish advanced technique from the
        // app level.  So, these methods are declared private.
        unsafe bool BeginDataXfer(ref byte[] buffer, ref Int32 len, ref byte[] ov)
        {
            bool bRetVal = false;

            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            uint tmo = ((TimeOut > 0) && (TimeOut < 1000)) ? 1 : TimeOut / 1000;

            _bIn = (Direction == CyConst.DIR_FROM_DEVICE);

            int bufSz = len + CyConst.SINGLE_XFER_LEN;

            fixed (byte* buf = buffer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = (byte)(Target | ReqType | Direction);
                transfer->SetupPacket.bRequest = ReqCode;
                transfer->SetupPacket.wValue = Value;
                transfer->SetupPacket.wLength = (ushort)len;
                transfer->SetupPacket.wIndex = Index;
                transfer->SetupPacket.dwTimeOut = tmo;
                transfer->WaitForever = 0;
                transfer->ucEndpointAddress = 0x00;     // control pipe
                transfer->IsoPacketLength = 0;
                transfer->BufferOffset = CyConst.SINGLE_XFER_LEN;		// size of the SINGLE_TRANSFER part
                transfer->BufferLength = (uint)len;

                int[] Xferred = new int[1];
                Xferred[0] = 0;

                fixed (byte* lpInBuffer = buffer)
                {
                    fixed (byte* lpOutBuffer = buffer)
                    {
                        fixed (byte* lpOv = ov)
                        {
                            fixed (int* lpBytesXfered = Xferred)
                            {
                                bRetVal = PInvoke.DeviceIoControl(_hDevice,
                                              CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER,
                                              (IntPtr)lpInBuffer, bufSz, (IntPtr)lpOutBuffer, bufSz,
                                              (IntPtr)lpBytesXfered, (IntPtr)lpOv);
                            }
                        }
                    }
                }

                len = Xferred[0];

                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;
            }

            _lastError = (uint)Marshal.GetLastWin32Error();

            return bRetVal;

        }


        // This is a BUFFERED xfer method - specific version of FinishDataXfer.  So that it
        // doesn't collide with the base class' FinishDataXfer, we declare it 'new'
        new unsafe bool FinishDataXfer(ref byte[] userBuf, ref byte[] xferBuf, ref int len, ref byte[] ov)
        {
            uint bytes = 0;
            bool rResult = PInvoke.GetOverlappedResult(_hDevice, ov, ref bytes, 0);

            uint dataOffset;

            fixed (byte* buf = xferBuf)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;

                len = (bytes > CyConst.SINGLE_XFER_LEN) ? (int)bytes - (int)transfer->BufferOffset : 0;
                _bytesWritten = (uint)len;

                dataOffset = transfer->BufferOffset;
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

            }

            // Extract the acquired data and move from xferBuf to userBuf
            if (bIn)
                for (int i = 0; i < len; i++)
                    userBuf[i] = xferBuf[dataOffset + i];

            return rResult && (_usbdStatus == 0) && (_ntStatus == 0);
        }

    }


    /// <summary>
    /// The Isoc Endpoint Class
    /// </summary>
    public class CyIsocEndPoint : CyUSBEndPoint
    {
        unsafe internal CyIsocEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor) : base(h, EndPtDescriptor) { }
        unsafe internal CyIsocEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor, USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR* SSEndPtDescriptor) : base(h, EndPtDescriptor, SSEndPtDescriptor) { }

        public int GetPktBlockSize(Int32 len)
        {
            if (MaxPktSize==0)
                return 0;

            int pkts = len / MaxPktSize;
            if ((len % MaxPktSize) > 0) pkts++;
            if (pkts == 0) return 0;

            ISO_PKT_INFO p = new ISO_PKT_INFO();

            int blkSize = pkts * Marshal.SizeOf(p);
            return blkSize;
        }

        public int GetPktCount(Int32 len)
        {
            if (MaxPktSize==0)
                return 0;

            int pkts = len / MaxPktSize;
            if ((len % MaxPktSize) > 0) pkts++;
            return pkts;
        }


        // Call this one if you don't care about the PacketInfo information
        public unsafe override bool XferData(ref  byte[] buf, ref Int32 len)
        {
            byte[] ovLap = new byte[OverlapSignalAllocSize];

            fixed (byte* tmp0 = ovLap)
            {
                OVERLAPPED* ovLapStatus = (OVERLAPPED*)tmp0;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                // This SINGLE_TRANSFER buffer must be allocated at this level.
                int bufSz = CyConst.SINGLE_XFER_LEN + GetPktBlockSize(len) + ((XferMode == XMODE.DIRECT) ? 0 : len);
                byte[] cmdBuf = new byte[bufSz];

                fixed (byte* tmp1 = cmdBuf, tmp2 = buf)
                {
                    bool bResult = BeginDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);
                    bool wResult = WaitForIO(ovLapStatus->hEvent);
                    bool fResult = FinishDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);

                    PInvoke.CloseHandle(ovLapStatus->hEvent);

                    return wResult && fResult;
                }
            }

        }


        // Call this one if you want the PacketInfo data passed-back
        public unsafe bool XferData(ref  byte[] buf, ref int len, ref ISO_PKT_INFO[] pktInfos)
        {
            byte[] ovLap = new byte[OverlapSignalAllocSize];

            fixed (byte* tmp0 = ovLap)
            {
                OVERLAPPED* ovLapStatus = (OVERLAPPED*)tmp0;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                // This SINGLE_TRANSFER buffer must be allocated at this level.
                int bufSz = CyConst.SINGLE_XFER_LEN + GetPktBlockSize(len) + ((XferMode == XMODE.DIRECT) ? 0 : len);
                byte[] cmdBuf = new byte[bufSz];

                fixed (byte* tmp1 = cmdBuf, tmp2 = buf)
                {
                    bool bResult = BeginDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);
                    bool wResult = WaitForIO(ovLapStatus->hEvent);
                    bool fResult = FinishDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap, ref pktInfos);

                    PInvoke.CloseHandle(ovLapStatus->hEvent);

                    return wResult && fResult;
                }
            }
        }


        public unsafe override bool BeginDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov)
        {
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            int pktBlockSize = GetPktBlockSize(len);
            int cmdLen = singleXfer.Length;

            fixed (byte* buf = singleXfer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->WaitForever = 0;
                transfer->ucEndpointAddress = Address;
                transfer->IsoPacketOffset = CyConst.SINGLE_XFER_LEN;
                transfer->IsoPacketLength = (uint)pktBlockSize;

                int[] Xferred = new int[1];
                Xferred[0] = 0;

                uint IOCTL;

                if (XferMode == XMODE.BUFFERED)
                {
                    transfer->BufferOffset = (uint)(CyConst.SINGLE_XFER_LEN + pktBlockSize);
                    transfer->BufferLength = (uint)len;

                    IOCTL = CyConst.IOCTL_ADAPT_SEND_NON_EP0_TRANSFER;

                    for (int i = 0; i < len; i++) buf[CyConst.SINGLE_XFER_LEN + pktBlockSize + i] = buffer[i];

                    fixed (byte* lpInBuffer = singleXfer)
                    {
                        fixed (byte* lpOutBuffer = singleXfer)
                        {
                            fixed (byte* lpOv = ov)
                            {
                                fixed (int* lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice, IOCTL,
                                        (IntPtr)lpInBuffer, cmdLen, (IntPtr)lpOutBuffer, cmdLen,
                                        (IntPtr)lpBytesXfered, (IntPtr)lpOv);
                                }
                            }
                        }
                    }

                }
                else
                {
                    transfer->BufferOffset = 0;
                    transfer->BufferLength = 0;

                    IOCTL = CyConst.IOCTL_ADAPT_SEND_NON_EP0_DIRECT;

                    fixed (byte* lpInBuffer = singleXfer)
                    {
                        fixed (byte* lpOutBuffer = buffer)
                        {
                            fixed (byte* lpOv = ov)
                            {
                                fixed (int* lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice, IOCTL,
                                        (IntPtr)lpInBuffer, cmdLen, (IntPtr)lpOutBuffer, len,
                                        (IntPtr)lpBytesXfered, (IntPtr)lpOv);
                                }
                            }
                        }
                    }
                }

                if (Xferred[0] > 0)
                    len = Xferred[0];

                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;
            }

            _lastError = (uint)Marshal.GetLastWin32Error();

            return true;

        }


        // This FinishDataXfer is only called by the second XferData method of CyIsocEndPoint
        // This called when ISO_PKT_INFO data is requested
        public unsafe virtual bool FinishDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov, ref ISO_PKT_INFO[] pktInfo)
        {
            // Call the base class' FinishDataXfer to do most of the work
            bool rResult = FinishDataXfer(ref singleXfer, ref buffer, ref len, ref ov);

            // Pass-back the Isoc packet info records
            if (len > 0)
            {
                fixed (byte* buf = singleXfer)
                {
                    SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                    ISO_PKT_INFO* packets = (ISO_PKT_INFO*)(buf + transfer->IsoPacketOffset);

                    int pktCnt = (int)transfer->IsoPacketLength / Marshal.SizeOf(*packets);

                    for (int i = 0; i < pktCnt; i++)
                        pktInfo[i] = packets[i];
                }
            }

            return rResult;
        }

    }


    /// <summary>
    /// The Bulk Endpoint Class
    /// </summary>

    public class CyBulkEndPoint : CyUSBEndPoint
    {
        unsafe internal CyBulkEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor) : base(h, EndPtDescriptor) { }
        unsafe internal CyBulkEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor, USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR* SSEndPtDescriptor) : base(h, EndPtDescriptor, SSEndPtDescriptor) { }
    }


    /// <summary>
    /// The Interrupt Endpoint Class
    /// </summary>
    public class CyInterruptEndPoint : CyUSBEndPoint
    {
        unsafe internal CyInterruptEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor) : base(h, EndPtDescriptor) { }
        unsafe internal CyInterruptEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR* EndPtDescriptor, USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR* SSEndPtDescriptor) : base(h, EndPtDescriptor, SSEndPtDescriptor) { }
    }

}
