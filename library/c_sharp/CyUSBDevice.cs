/*
 ## Cypress CyUSB C# library source file (CyUSBDevice.cs)
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
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security;
using Microsoft.Win32;

namespace CyUSB
{
    /// <summary>
    /// The CyUSBDevice Class
    /// </summary>
    public class CyUSBDevice : USBDevice
    {
        public CyUSBConfig[] USBCfgs;
        public CyUSBBOS USBBos;

        // Shortcut to USBCfgs[CfgNum].Interfaces[IntfcIndex].Endpoints
        public CyUSBEndPoint[] EndPoints;

        // These convenience references get re-assigned by SetEndpoints 
        // whenever the AltIntfc changes
        public CyControlEndPoint ControlEndPt;
        public CyIsocEndPoint IsocInEndPt;
        public CyIsocEndPoint IsocOutEndPt;
        public CyBulkEndPoint BulkInEndPt;
        public CyBulkEndPoint BulkOutEndPt;
        public CyInterruptEndPoint InterruptInEndPt;
        public CyInterruptEndPoint InterruptOutEndPt;

        // The constructors are internal because we don't want users to be able
        // to explicitly construct objects.  Construction should only happen
        // indirectly when a new USBDeviceList is constructed.

        // Default constructor attaches to CyUSB3.sys driver	
        internal CyUSBDevice() : this(CyConst.CyGuid) { }

        internal CyUSBDevice(Guid guid)
            : base(guid)
        {
            // Allocate the memory for up to 2 descriptors.
            _usbConfigDescriptors[0] = new byte[CyConst.MaxDescriptorBufferLength];
            _usbConfigDescriptors[1] = new byte[CyConst.MaxDescriptorBufferLength];
            USBCfgs = new CyUSBConfig[2];  //TODO : Add more than 2 configuration support
        }

        public override TreeNode Tree
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");

                if ((_bcdUSB & CyConst.bcdUSBJJMask) == CyConst.USB30MajorVer)
                {
                    int NumberOftreeNode = _configs + 1;//+1 for the BOS descriptor
                    TreeNode[] cfgTree = new TreeNode[NumberOftreeNode];
                    cfgTree[0] = USBBos.Tree;
                    for (int i = 1; i < NumberOftreeNode; i++)
                        cfgTree[i] = USBCfgs[(i - 1)].Tree;

                    TreeNode t = new TreeNode(FriendlyName, cfgTree);
                    t.Tag = this;
                    return t;
                }
                else
                {

                    TreeNode[] cfgTree = new TreeNode[_configs];
                    for (int i = 0; i < _configs; i++)
                        cfgTree[i] = USBCfgs[i].Tree;

                    TreeNode t = new TreeNode(FriendlyName, cfgTree);
                    t.Tag = this;
                    return t;
                }

            }
        }

        public override string ToString()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            StringBuilder s = new StringBuilder("<DEVICE>\r\n");

            s.Append(string.Format("\tFriendlyName=\"{0}\"\r\n", FriendlyName));
            s.Append(string.Format("\tManufacturer=\"{0}\"\r\n", Manufacturer));
            s.Append(string.Format("\tProduct=\"{0}\"\r\n", Product));
            s.Append(string.Format("\tSerialNumber=\"{0}\"\r\n", SerialNumber));
            s.Append(string.Format("\tConfigurations=\"{0}\"\r\n", ConfigCount));

            //if ((_bcdUSB & CyConst.bcdUSBJJMask) == CyConst.USB20MajorVer)
            s.Append(string.Format("\tMaxPacketSize=\"{0}\"\r\n", MaxPacketSize));
            //else
            //  s.Append(string.Format("\tMaxPacketSize=\"{0}\"\r\n", powerof2(MaxPacketSize))); // USB3.0 EP0 packet size = 2^maximumpacketsize

            s.Append(string.Format("\tVendorID=\"{0}\"\r\n", Util.byteStr(VendorID)));
            s.Append(string.Format("\tProductID=\"{0}\"\r\n", Util.byteStr(ProductID)));
            s.Append(string.Format("\tClass=\"{0:X2}h\"\r\n", _devClass));
            s.Append(string.Format("\tSubClass=\"{0:X2}h\"\r\n", _devSubClass));
            s.Append(string.Format("\tProtocol=\"{0:X2}h\"\r\n", _devProtocol));
            s.Append(string.Format("\tBcdDevice=\"{0}\"\r\n", Util.byteStr(_bcdDevice)));
            s.Append(string.Format("\tBcdUSB=\"{0}\"\r\n", Util.byteStr(_bcdUSB)));

            if ((_bcdUSB & CyConst.bcdUSBJJMask) == CyConst.USB30MajorVer)
                s.Append(USBBos.ToString());

            for (int i = 0; i < ConfigCount; i++)
                s.Append(USBCfgs[i].ToString());

            s.Append("</DEVICE>\r\n");
            return s.ToString();
        }

        private bool _IsFX2Device;
        public bool IsFX2Device
        {// This variable tells , if device is fx2:true or fx3:false
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _IsFX2Device;
            }
        }
        private ushort _strLangID;
        public ushort StrLangID
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _strLangID;
            }
        }

        private uint _driverVersion;
        public uint DriverVersion
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _driverVersion;
            }
        }

        private uint _usbdiVersion;
        public uint USBDIVersion
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _usbdiVersion;
            }
        }

        private int _maxPacketSize;
        public int MaxPacketSize
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _maxPacketSize; //note for usb3.0 device control endpoint the maximum packet size is 92^_maxPacketSize)

            }
        }
        private byte powerof2(byte exponent)
        {
            if (exponent <= 0) return 0; // we want a positive integer for the exponent
            else
            {
                byte c = 1;
                for (byte i = 0; i < exponent; i++)
                {
                    c *= 2;
                }
                return c;
            }
        }

        private ushort _bcdDevice;
        public ushort BcdDevice
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _bcdDevice;
            }
        }

        private byte _configValue;
        public byte ConfigValue
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _configValue;
            }
        }

        private byte _configAttrib;
        public byte ConfigAttrib
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _configAttrib;
            }
        }

        private byte _maxPower;
        public byte MaxPower
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _maxPower;
            }
        }

        private byte _intfcClass;
        public byte IntfcClass
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _intfcClass;
            }
        }

        private byte _intfcSubClass;
        public byte IntfcSubClass
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _intfcSubClass;
            }
        }

        private byte _intfcProtocol;
        public byte IntfcProtocol
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _intfcProtocol;
            }
        }
        private bool _bSuperSpeed;
        public bool bSuperSpeed
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _bSuperSpeed;
            }
        }
        private bool _bHighSpeed;
        public bool bHighSpeed
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _bHighSpeed;
            }
        }

        public byte ConfigCount
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _configs;
            }
        }


        // This method is never used because, in Windows, it always returns 1
        // This might return other than 1 when it is a composite device.
        public byte IntfcCount
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _interfaces;
            }
        }


        public byte AltIntfcCount
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _altInterfaces;
            }
        }


        public byte AltIntfc
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                byte[] alt = new byte[1];
                if (IoControl(CyConst.IOCTL_ADAPT_GET_ALT_INTERFACE_SETTING, alt, 1))
                    return alt[0];
                else
                    return 0xFF;
            }

            set
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");

                if (value == AltIntfc) return;

                /* This is a composite interface  so don't do Set/Select Interface calls */
                if (IntfcCount > 1) return;

                // Find match of IntfcNum and alt in table of interfaces
                if (USBCfgs[_cfgNum] != null)
                {
                    for (int j = 0; j < USBCfgs[_cfgNum].AltInterfaces; j++)
                        if (USBCfgs[_cfgNum].Interfaces[j].bAlternateSetting == value)
                        {
                            _intfcIndex = (byte)j;

                            // Actually change to the alt interface, calling the driver
                            byte[] alt = new byte[1];
                            alt[0] = value;
                            IoControl(CyConst.IOCTL_ADAPT_SELECT_INTERFACE, alt, 1);

                            _intfcClass = USBCfgs[_cfgNum].Interfaces[j].bInterfaceClass;
                            _intfcSubClass = USBCfgs[_cfgNum].Interfaces[j].bInterfaceSubClass; ;
                            _intfcProtocol = USBCfgs[_cfgNum].Interfaces[j].bInterfaceProtocol; ;

                            SetEndPoints();
                        }
                }
            }
        }


        public byte EndPointCount
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");

                if (_hDevice == CyConst.INVALID_HANDLE) return 0;

                if (USBCfgs[_cfgNum] != null)
                    return (byte)(USBCfgs[_cfgNum].Interfaces[_intfcIndex].bNumEndpoints + 1); // Include EndPt0

                return 0;
            }
        }


        public byte Config
        {

            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _cfgNum;
            }

            set
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");

                if (USBCfgs[0] == null) return;

                _cfgNum = 0;
                if ((USBCfgs[0] != null) && (USBCfgs[0].iConfiguration == value)) _cfgNum = value;
                if ((USBCfgs[1] != null) && (USBCfgs[1].iConfiguration == value)) _cfgNum = value;

                _configValue = USBCfgs[_cfgNum].bConfigurationValue;
                _configAttrib = USBCfgs[_cfgNum].bmAttributes;
                _maxPower = USBCfgs[_cfgNum].MaxPower;
                _interfaces = USBCfgs[_cfgNum].bNumInterfaces;
                _altInterfaces = USBCfgs[_cfgNum].AltInterfaces;
                _intfcNum = USBCfgs[_cfgNum].Interfaces[0].bInterfaceNumber;

                byte a = AltIntfc;  // Get the current alt setting from the device
                SetAltIntfcParams(a);   // Initializes endpts, IntfcIndex, etc. without actually setting the AltInterface

                if (USBCfgs[_cfgNum].Interfaces[_intfcIndex] != null)
                {
                    _intfcClass = USBCfgs[_cfgNum].Interfaces[_intfcIndex].bInterfaceClass;
                    _intfcSubClass = USBCfgs[_cfgNum].Interfaces[_intfcIndex].bInterfaceSubClass;
                    _intfcProtocol = USBCfgs[_cfgNum].Interfaces[_intfcIndex].bInterfaceProtocol;
                }

            }
        }



        // This method never used because, in Windows, it always returns 0
        private byte Interface
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _intfcNum;
            }  // Usually 0
            // No set method since only 1 intfc per device (per Windows)
        }


        //public Guid DriverGUID{ get{ return DrvGuid; }}


        public IntPtr DeviceHandle
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _hDevice;
            }
        }


        public static string UsbdStatusString(UInt32 stat)
        {
            UInt32 status = stat & 0x0FFFFFFF;
            UInt32 state = stat & 0xF0000000;

            string sState = "";
            string sStatus = "";

            if (status == 0)
            {
                sState = "[state=SUCCESS ";
                sStatus = "status=USBD_STATUS_SUCCESS]";
            }
            else
            {
                switch (state)
                {
                    case 0x00000000: sState = "[state=SUCCESS "; break;
                    case 0x40000000: sState = "[state=PENDING "; break;
                    case 0xC0000000: sState = "[state=HALTED "; break;
                    case 0x80000000: sState = "[state=ERROR "; break;
                    default: sState = "[state=UNKNOWN "; break;
                }

                switch (status)
                {
                    case 0xC0000001: sStatus = "status=USBD_STATUS_CRC]"; break;
                    case 0xC0000002: sStatus = "status=USBD_STATUS_BTSTUFF]"; break;
                    case 0xC0000003: sStatus = "status=USBD_STATUS_DATA_TOGGLE_MISMATCH]"; break;
                    case 0xC0000004: sStatus = "status=USBD_STATUS_STALL_PID]"; break;
                    case 0xC0000005: sStatus = "status=USBD_STATUS_DEV_NOT_RESPONDING]"; break;
                    case 0xC0000006: sStatus = "status=USBD_STATUS_PID_CHECK_FAILURE]"; break;
                    case 0xC0000007: sStatus = "status=USBD_STATUS_UNEXPECTED_PID]"; break;
                    case 0xC0000008: sStatus = "status=USBD_STATUS_DATA_OVERRUN]"; break;
                    case 0xC0000009: sStatus = "status=USBD_STATUS_DATA_UNDERRUN]"; break;
                    case 0xC000000A: sStatus = "status=USBD_STATUS_RESERVED1]"; break;
                    case 0xC000000B: sStatus = "status=USBD_STATUS_RESERVED2]"; break;
                    case 0xC000000C: sStatus = "status=USBD_STATUS_BUFFER_OVERRUN]"; break;
                    case 0xC000000D: sStatus = "status=USBD_STATUS_BUFFER_UNDERRUN]"; break;
                    case 0xC000000F: sStatus = "status=USBD_STATUS_NOT_ACCESSED]"; break;
                    case 0xC0000010: sStatus = "status=USBD_STATUS_FIFO]"; break;

                    case 0xC0000030: sStatus = "status=USBD_STATUS_CRC]"; break;
                    case 0xC0000100: sStatus = "status=USBD_STATUS_BTSTUFF]"; break;
                    case 0xC0000200: sStatus = "status=USBD_STATUS_DATA_TOGGLE_MISMATCH]"; break;
                    case 0xC0000300: sStatus = "status=USBD_STATUS_STALL_PID]"; break;
                    case 0xC0000400: sStatus = "status=USBD_STATUS_DEV_NOT_RESPONDING]"; break;
                    case 0xC0000500: sStatus = "status=USBD_STATUS_PID_CHECK_FAILURE]"; break;
                    case 0xC0000600: sStatus = "status=USBD_STATUS_UNEXPECTED_PID]"; break;
                    case 0xC0000700: sStatus = "status=USBD_STATUS_DATA_OVERRUN]"; break;
                    case 0xC0000800: sStatus = "status=USBD_STATUS_DATA_UNDERRUN]"; break;
                    case 0xC0000900: sStatus = "status=USBD_STATUS_RESERVED1]"; break;
                    case 0xC0000A00: sStatus = "status=USBD_STATUS_RESERVED2]"; break;
                    case 0xC0000B00: sStatus = "status=USBD_STATUS_BUFFER_OVERRUN]"; break;
                    case 0xC0000C00: sStatus = "status=USBD_STATUS_BUFFER_UNDERRUN]"; break;
                    case 0xC0000D00: sStatus = "status=USBD_STATUS_NOT_ACCESSED]"; break;
                    case 0xC0010000: sStatus = "status=USBD_STATUS_FIFO]"; break;
                    case 0xC0020000: sStatus = "status=USBD_STATUS_FIFO]"; break;
                    default: sStatus = "status=UNKNOWN]"; break;
                }
            }

            return sState + sStatus;
        }


        public CyUSBEndPoint EndPointOf(byte addr)
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            if (addr == 0) return ControlEndPt;

            CyUSBEndPoint ept;

            int n = EndPointCount;

            for (int i = 0; i < n; i++)
            {
                ept = USBCfgs[_cfgNum].Interfaces[_intfcIndex].EndPoints[i];

                if (ept != null)
                    if (addr == ept.Address) return ept;

            }

            return null; // Error
        }


        // Opens a handle to the devTH device attached the CYUSB3.SYS driver
        internal override bool Open(byte dev)
        {
            // If this CCyUSBDevice object already has the driver open, close it.
            if (_hDevice != CyConst.INVALID_HANDLE)
                Close();

            _devices = DeviceCount;
            if (_devices == 0) return false;
            if (dev > (_devices - 1)) return false;

            _path = PInvoke.GetDevicePath(_drvGuid, dev);
            _hDevice = PInvoke.GetDeviceHandle(_path, true);
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            _devNum = dev;
            GetDevDescriptor();
            SetStringDescrLanguage();

            _manufacturer = GetString(_usbDeviceDescriptor.iManufacturer);
            _product = GetString(_usbDeviceDescriptor.iProduct);
            _serialNumber = GetString(_usbDeviceDescriptor.iSerialNumber);

            // Get BOS descriptor
            if ((_bcdUSB & CyConst.bcdUSBJJMask) == CyConst.USB30MajorVer)
            {
                GetBosDescriptor(); // USB3.0 specific descriptor
                try
                {
                    USBBos = new CyUSBBOS(_hDevice, _usb30BosDescriptors);
                }
                catch (Exception exc)
                {
                    //Just to remove warning
                    exc.ToString();
                    _nullEndpointFlag = true;
                    MessageBox.Show("Please correct the firmware BOS descriptor table", "Invalid BOS Descriptor");
                    Close(); // Close the device handle ,as the device configuration is incorrect.
                    return false;
                }
            }

            GetUSBAddress();
            GetDeviceName();
            GetFriendlyName();
            GetDriverVer();
            GetUSBDIVer();
            GetSpeed();

            // Search the registry for this device - This must follow GetFriendlyName
            GetDriverName();

            // Create the Control Endpoint (EPT 0)
            ControlEndPt = new CyControlEndPoint(_hDevice, _maxPacketSize);

            // Gets and parses the config (including interface and endpoint) descriptors from the device
            for (int i = 0; i < _configs; i++)
            {
                GetCfgDescriptor(i);
                try
                {
                    if ((_bcdUSB & CyConst.bcdUSBJJMask) == CyConst.USB20MajorVer)
                        USBCfgs[i] = new CyUSBConfig(_hDevice, _usbConfigDescriptors[i], ControlEndPt);
                    else
                    {
                        byte usb30Dummy = 1; // it's dummy variable to call the usb3.0 specific constructor
                        USBCfgs[i] = new CyUSBConfig(_hDevice, _usbConfigDescriptors[i], ControlEndPt, usb30Dummy);
                    }
                }
                catch (Exception exc)
                {
                    //Just to remove warning
                    exc.ToString();
                    _nullEndpointFlag = true;
                    MessageBox.Show("Please correct the firmware descriptor table", "Invalid Device Configuration");
                    Close();
                    return false;
                }
            }

            // We succeeded in openning a handle to the device.  But, the device
            // is not returning descriptors properly.  We don't call Close( ) because
            // we want to leave the hDevice intact, giving the user the opportunity
            // to call the Reset( ) method

            if ((USBCfgs[0] == null) || (USBCfgs[0].Interfaces[0] == null)) return false;

            if (!_nullEndpointFlag)
            {
                try
                {
                    // This property assignment sets values for ConfigVal, ConfigAttrib, MaxPower, etc.		
                    Config = 0;
                }
                catch (Exception exc)
                {
                    //Just to remove warning
                    exc.ToString();
                    _nullEndpointFlag = true;
                    MessageBox.Show("Please Check the Device Configuration and try again.", "Invalid Firmware");
                }
            }

            if (_nullEndpointFlag)
                return false;

            return true;
        }


        public bool Reset()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            return IoControl(CyConst.IOCTL_ADAPT_RESET_PARENT_PORT, null, 0);
        }


        public bool ReConnect()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            return IoControl(CyConst.IOCTL_ADAPT_CYCLE_PORT, null, 0);
        }


        internal bool IsOpen
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return (_hDevice != CyConst.INVALID_HANDLE);
            }
        }



        public bool GetBosDescriptor(ref USB_BOS_DESCRIPTOR descr)
        {// USB3.0 device specific descriptor, for USB2.0 device this function will return false            
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            if ((_bcdUSB & CyConst.bcdUSBJJMask) != CyConst.USB30MajorVer)
                return false;
            descr.bLength = USBBos.Lenght;
            descr.bDescriptorType = USBBos.DescriptorType;
            descr.bNumDeviceCaps = USBBos.NumDeviceCaps;
            descr.wToatalLength = USBBos.ToatalLength;
            return true;
        }
        public bool GetBosUSB20DeviceExtensionDescriptor(ref USB_BOS_USB20_DEVICE_EXTENSION descr)
        {// USB3.0 device specific descriptor, for USB2.0 device this function will return false            
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            if ((_bcdUSB & CyConst.bcdUSBJJMask) != CyConst.USB30MajorVer)
                return false;
            if (USBBos.USB20_DeviceExt != null)
            {// USB20 device extesion is defined
                descr.bLength = USBBos.USB20_DeviceExt.Lenght;
                descr.bDescriptorType = USBBos.USB20_DeviceExt.DescriptorType;
                descr.bDevCapabilityType = USBBos.USB20_DeviceExt.DevCapabilityType;
                descr.bmAttribute = USBBos.USB20_DeviceExt.bmAttribute;
            }
            else// not defined
                return false;

            return true;
        }
        unsafe public bool GetBosContainedIDDescriptor(ref USB_BOS_CONTAINER_ID descr)
        {// USB3.0 device specific descriptor, for USB2.0 device this function will return false            
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            if ((_bcdUSB & CyConst.bcdUSBJJMask) != CyConst.USB30MajorVer)
                return false;
            if (USBBos.Container_ID != null)
            {
                descr.bLength = USBBos.Container_ID.Lenght;
                descr.bDescriptorType = USBBos.Container_ID.DescriptorType;
                descr.bDevCapabilityType = USBBos.Container_ID.DevCapabilityType;
                descr.bReserved = USBBos.Container_ID.Reserved;
                //for (int i = 0; i < CyConst.USB_BOS_CAPABILITY_TYPE_CONTAINER_ID_SIZE; i++)
                //  descr.ContainerID[i] = 0;
            }
            else
                return false;

            return true;
        }
        public bool GetBosSSCapabilityDescriptor(ref USB_BOS_SS_DEVICE_CAPABILITY descr)
        {// USB3.0 device specific descriptor, for USB2.0 device this function will return false            
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            if ((_bcdUSB & CyConst.bcdUSBJJMask) != CyConst.USB30MajorVer)
                return false;
            if (USBBos.SS_DeviceCap != null)
            {// SS capability is defined
                descr.bLength = USBBos.SS_DeviceCap.Lenght;
                descr.bDescriptorType = USBBos.SS_DeviceCap.DescriptorType;
                descr.bDevCapabilityType = USBBos.SS_DeviceCap.DevCapabilityType;
                descr.bFunctionalitySupporte = USBBos.SS_DeviceCap.FunctionalitySupporte;
                descr.bmAttribute = USBBos.SS_DeviceCap.bmAttribute;
                descr.bU1DevExitLat = USBBos.SS_DeviceCap.U1DevExitLat;
                descr.bU2DevExitLat = USBBos.SS_DeviceCap.U2DevExitLat;
            }
            else // not defined
                return false;

            return true;
        }
        public void GetDeviceDescriptor(ref USB_DEVICE_DESCRIPTOR descr)
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            descr = _usbDeviceDescriptor;
        }


        public void GetConfigDescriptor(ref USB_CONFIGURATION_DESCRIPTOR descr)
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            descr.bLength = USBCfgs[_cfgNum].bLength;
            descr.bDescriptorType = USBCfgs[_cfgNum].bDescriptorType;
            descr.wTotalLength = USBCfgs[_cfgNum].wTotalLength;
            descr.bNumInterfaces = USBCfgs[_cfgNum].bNumInterfaces;
            descr.bConfigurationValue = USBCfgs[_cfgNum].bConfigurationValue;
            descr.iConfiguration = USBCfgs[_cfgNum].iConfiguration;
            descr.bmAttributes = USBCfgs[_cfgNum].bmAttributes;
            descr.MaxPower = USBCfgs[_cfgNum].MaxPower;

        }


        public void GetIntfcDescriptor(ref USB_INTERFACE_DESCRIPTOR descr)
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");
            CyUSBInterface i = USBCfgs[_cfgNum].Interfaces[_intfcIndex];

            // Copy the internal private data to the passed parameter
            descr.bLength = i.bLength;
            descr.bDescriptorType = i.bDescriptorType;
            descr.bInterfaceNumber = i.bInterfaceNumber;
            descr.bAlternateSetting = i.bAlternateSetting;
            descr.bNumEndpoints = i.bNumEndpoints;
            descr.bInterfaceClass = i.bInterfaceClass;
            descr.bInterfaceSubClass = i.bInterfaceSubClass;
            descr.bInterfaceProtocol = i.bInterfaceProtocol;
            descr.iInterface = i.iInterface;
        }

        public unsafe bool CheckDeviceTypeFX3FX2()
        {//return value true : fx2, false:fx3

            int len = 39;									// total size of buffer
            byte[] buffer = new byte[len];
            bool bRetVal;

            fixed (byte* buf = buffer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_VENDOR | CyConst.DIR_FROM_DEVICE;
                transfer->SetupPacket.bRequest = 0xA0;
                transfer->SetupPacket.wValue = 0xE600;
                transfer->SetupPacket.wIndex = 0x0000;
                transfer->SetupPacket.wLength = 1;	// size of the USB_DEVICE_DESCRIPTOR part
                transfer->SetupPacket.dwTimeOut = 5;
                transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                transfer->BufferLength = 1;

                fixed (byte* lpInBuffer = buffer)
                {
                    fixed (byte* lpOutBuffer = buffer)
                    {
                        fixed (int* lpBytesXfered = _bytesXfered)
                        {
                            bRetVal = PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER,
                                            (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len, (IntPtr)lpBytesXfered, (IntPtr)null);
                        }
                    }
                }
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if (bRetVal)
                {
                    _IsFX2Device = true;
                    return true;
                }
            }
            _IsFX2Device = false;
            return false;
        }
        #region Private members
        USB_DEVICE_DESCRIPTOR _usbDeviceDescriptor;

        // USBConfigDescriptors contains the raw config descriptor data for
        // up to 2 Configuration descriptors.  Note that a config descriptor
        // includes all the interface and endpoint descriptors, too.
        byte[][] _usbConfigDescriptors = new byte[2][];
        byte[] _usb30BosDescriptors = new byte[CyConst.MaxDescriptorBufferLength];

        byte _interfaces;
        byte _altInterfaces;
        byte _configs;

        byte _cfgNum;
        byte _intfcNum;     // The current selected interface's bInterfaceNumber
        byte _intfcIndex;   // The entry in the Config's interfaces table matching to IntfcNum and AltSetting


        int[] _bytesXfered = new int[1];
        uint _usbdStatus;
        uint _ntStatus;

        unsafe bool IoControl(uint cmd, byte[] buf, int len)
        {
            fixed (byte* lpInBuffer = buf)
            {
                fixed (byte* lpOutBuffer = buf)
                {
                    fixed (int* lpBytesXfered = _bytesXfered)
                    {
                        return PInvoke.DeviceIoControl(_hDevice, cmd,
                            (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len, (IntPtr)lpBytesXfered, (IntPtr)null);
                    }
                }
            }
        }


        unsafe void GetDevDescriptor()
        {

            int len = 56;									// total size of buffer
            byte[] buffer = new byte[len];
            bool bRetVal;

            fixed (byte* buf = buffer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                transfer->SetupPacket.wValue = CyConst.USB_DEVICE_DESCRIPTOR_TYPE << 8;
                transfer->SetupPacket.wLength = 18;	// size of the USB_DEVICE_DESCRIPTOR part
                transfer->SetupPacket.dwTimeOut = 5;
                transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                transfer->BufferLength = 18;

                fixed (byte* lpInBuffer = buffer)
                {
                    fixed (byte* lpOutBuffer = buffer)
                    {
                        fixed (int* lpBytesXfered = _bytesXfered)
                        {
                            bRetVal = PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER,
                                            (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len, (IntPtr)lpBytesXfered, (IntPtr)null);
                        }
                    }
                }
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if (bRetVal)
                {
                    USB_DEVICE_DESCRIPTOR* descriptor = (USB_DEVICE_DESCRIPTOR*)(buf + 38);

                    _usbDeviceDescriptor = *descriptor;

                    _bcdUSB = descriptor->bcdUSB;
                    _vendorID = descriptor->idVendor;
                    _productID = descriptor->idProduct;
                    _devClass = descriptor->bDeviceClass;
                    _devSubClass = descriptor->bDeviceSubClass;
                    _devProtocol = descriptor->bDeviceProtocol;
                    if ((_bcdUSB & CyConst.bcdUSBJJMask) == CyConst.USB20MajorVer)
                        _maxPacketSize = descriptor->bMaxPacketSize0;
                    else
                        _maxPacketSize = (int)(1 << descriptor->bMaxPacketSize0);
                    _bcdDevice = descriptor->bcdDevice;
                    _configs = descriptor->bNumConfigurations;
                }
            }


        }
        unsafe bool GetBosDescriptor()
        { // false - device doesn't have a BOS , true - device have a BOS
            uint _MaxDescriptorBufferLength = CyConst.MaxDescriptorBufferLength + 38; // size of SINGLE_TRANSFER = 38

            //int len = 38 + 9;  // size of SINGLE_TRANSFER + USB_BOS_DESCRIPTOR
            int len = sizeof(SINGLE_TRANSFER) + CyConst.SIZEOF_USB_BOS_DESCRIPTOR;

            byte[] buffer = new byte[_MaxDescriptorBufferLength]; // big buffer . . . Bos descriptors can be large

            fixed (byte* buf = buffer)
            {
                // first get the BOS descriptor
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                transfer->SetupPacket.wValue = (ushort)(CyConst.USB_BOS_DESCRIPTOR_TYPE << 8);
                transfer->SetupPacket.wLength = CyConst.SIZEOF_USB_BOS_DESCRIPTOR;
                transfer->SetupPacket.dwTimeOut = 5;
                transfer->BufferOffset = (uint)sizeof(SINGLE_TRANSFER);	// size of the SINGLE_TRANSFER part
                transfer->BufferLength = CyConst.SIZEOF_USB_BOS_DESCRIPTOR;

                bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if (bRetVal)
                {
                    USB_BOS_DESCRIPTOR* descriptor = (USB_BOS_DESCRIPTOR*)(buf + 38);

                    // Get the entire descriptor
                    len = 38 + descriptor->wToatalLength;

                    if (len > _MaxDescriptorBufferLength) return false;

                    transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                    transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                    transfer->SetupPacket.wValue = (ushort)(CyConst.USB_BOS_DESCRIPTOR_TYPE << 8);
                    transfer->SetupPacket.wLength = descriptor->wToatalLength;
                    transfer->SetupPacket.dwTimeOut = 5;
                    transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                    transfer->BufferLength = descriptor->wToatalLength;

                    bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                    _usbdStatus = transfer->UsbdStatus;
                    _ntStatus = transfer->NtStatus;

                    if (bRetVal)
                    {
                        int lastByte = _bytesXfered[0] - 38;
                        for (int i = 0; i < lastByte; i++)
                            _usb30BosDescriptors[i] = buffer[i + 38];
                    }
                }
                else
                    return false;
            }
            return true;
        }


        unsafe void SetStringDescrLanguage()
        {
            // Get the header to find-out the number of languages, size of lang ID list
            int len = 38 + 2;  // size of SINGLE_TRANSFER) + USB_COMMON_DESCRIPTOR

            // DWY was 256
            byte[] buffer = new byte[512]; // extra big buffer . . . just in case

            fixed (byte* buf = buffer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                transfer->SetupPacket.wValue = CyConst.USB_STRING_DESCRIPTOR_TYPE << 8;
                transfer->SetupPacket.wLength = 2;	// size of the USB_COMMON_DESCRIPTOR part
                transfer->SetupPacket.dwTimeOut = 5;
                transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                transfer->BufferLength = 2;

                bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if (bRetVal)
                {
                    USB_COMMON_DESCRIPTOR* descriptor = (USB_COMMON_DESCRIPTOR*)(buf + 38);

                    int LangIDs = (descriptor->bLength - 2) / 2;

                    // Get the entire descriptor, all LangIDs
                    len = 38 + descriptor->bLength;

                    transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                    transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                    transfer->SetupPacket.wValue = CyConst.USB_STRING_DESCRIPTOR_TYPE << 8;
                    transfer->SetupPacket.wLength = descriptor->bLength;
                    transfer->SetupPacket.dwTimeOut = 5;
                    transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                    transfer->BufferLength = descriptor->bLength;

                    bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                    _usbdStatus = transfer->UsbdStatus;
                    _ntStatus = transfer->NtStatus;

                    if (bRetVal)
                    {
                        USB_STRING_DESCRIPTOR* IDs = (USB_STRING_DESCRIPTOR*)(buf + 38);

                        _strLangID = (LangIDs > 0) ? IDs[0].bString : (char)0;

                        for (int i = 0; i < LangIDs; i++)
                        {
                            UInt16 id = IDs[i].bString;
                            if (id == 0x0409) _strLangID = id;
                        }
                    }//if

                }//if

            }//fixed
        }


        //unsafe string GetString(ref string s, byte sIndex)
        unsafe string GetString(byte sIndex)
        {
            if (sIndex == 0) return "";

            // Get the header to find-out the number of languages, size of lang ID list
            int len = 38 + 2;  // size of SINGLE_TRANSFER) + USB_COMMON_DESCRIPTOR

            // dwy was 512
            byte[] buffer = new byte[1024]; // extra big buffer . . . just in case

            fixed (byte* buf = buffer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                transfer->SetupPacket.wValue = (ushort)((CyConst.USB_STRING_DESCRIPTOR_TYPE << 8) | sIndex);
                transfer->SetupPacket.wIndex = _strLangID;
                transfer->SetupPacket.wLength = 2;	// size of the USB_COMMON_DESCRIPTOR part
                transfer->SetupPacket.dwTimeOut = 5;
                transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                transfer->BufferLength = 2;

                bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if (bRetVal)
                {
                    USB_COMMON_DESCRIPTOR* descriptor = (USB_COMMON_DESCRIPTOR*)(buf + 38);

                    // Get the entire descriptor
                    len = 38 + descriptor->bLength;

                    transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                    transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                    transfer->SetupPacket.wValue = (ushort)((CyConst.USB_STRING_DESCRIPTOR_TYPE << 8) | sIndex);
                    transfer->SetupPacket.wIndex = _strLangID;
                    transfer->SetupPacket.wLength = descriptor->bLength;
                    transfer->SetupPacket.dwTimeOut = 5;
                    transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                    transfer->BufferLength = descriptor->bLength;

                    bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                    _usbdStatus = transfer->UsbdStatus;
                    _ntStatus = transfer->NtStatus;

                    if (bRetVal)
                    {
                        char* sChars = (char*)(buf + 40);
                        //s = new string(sChars);
                        return new string(sChars);
                    }

                }

                return "";
            }

        }


        void GetDriverName()
        {
            _driverName = "CyUSB3.sys";

            #region OLD_DRIVER_NAME_SEARCH
#if (FALSE)

            // See if we can find it in the registry -- This violates security in Vista 
            RegistryKey rkDriverNums = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\{36FC9E60-C465-11CF-8056-444553540000}\\");
            if (rkDriverNums != null)
            {
                string[] sDriverNums = rkDriverNums.GetSubKeyNames();

                foreach (string sDriver in sDriverNums)
                {
                    // SUKU:added exception handling. Test and remove this #if macro
                    RegistryKey rkDriver = null;
                    try
                    {
                        rkDriver = rkDriverNums.OpenSubKey(sDriver);
                    }
                    catch (SecurityException)
                    {
                    }
                    if (rkDriver != null)
                    {
                        string sDriverDesc = rkDriver.GetValue("DriverDesc") as string;

                        if ((sDriverDesc != null) && (_friendlyName.Equals(sDriverDesc)))
                        {
                            string sDriverName = rkDriver.GetValue("NTMPDriver") as string;
                            if (sDriverName != null)
                                _driverName = sDriverName;
                        }

                        rkDriver.Close();
                    }
                }

                rkDriverNums.Close();
            }
#endif
            #endregion

        }


        unsafe void GetDeviceName()
        {
            _name = "";
            if (_hDevice == CyConst.INVALID_HANDLE) return;

            byte[] buffer = new byte[CyConst.USB_STRING_MAXLEN];
            bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_GET_DEVICE_NAME, buffer, CyConst.USB_STRING_MAXLEN);

            if (bRetVal && (_bytesXfered[0] > 0))
                fixed (byte* buf = buffer)
                {
                    _name = new string((sbyte*)buf);
                }
        }


        unsafe void GetFriendlyName()
        {
            _friendlyName = "";
            if (_hDevice == CyConst.INVALID_HANDLE) return;

            byte[] buffer = new byte[CyConst.USB_STRING_MAXLEN];
            bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_GET_FRIENDLY_NAME, buffer, CyConst.USB_STRING_MAXLEN);

            if (bRetVal && (_bytesXfered[0] > 0))
                fixed (byte* buf = buffer)
                {
                    _friendlyName = new string((sbyte*)buf);
                }
        }


        unsafe void GetDriverVer()
        {
            _driverVersion = 0;
            if (_hDevice == CyConst.INVALID_HANDLE) return;

            byte[] buffer = new byte[4];
            bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_GET_DRIVER_VERSION, buffer, 4);

            if (bRetVal && (_bytesXfered[0] == 4))
                fixed (byte* buf = buffer)
                {
                    UInt32* ver = (UInt32*)buf;
                    _driverVersion = *ver;
                }
        }


        unsafe void GetUSBDIVer()
        {
            _usbdiVersion = 0;
            if (_hDevice == CyConst.INVALID_HANDLE) return;

            byte[] buffer = new byte[4];
            bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_GET_USBDI_VERSION, buffer, 4);

            if (bRetVal && (_bytesXfered[0] == 4))
                fixed (byte* buf = buffer)
                {
                    UInt32* ver = (UInt32*)buf;
                    _usbdiVersion = *ver;
                }
        }


        void GetUSBAddress()
        {
            _usbAddress = 0;
            if (_hDevice == CyConst.INVALID_HANDLE) return;

            byte[] buf = new byte[1];
            bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_GET_ADDRESS, buf, 1);

            if (bRetVal && (_bytesXfered[0] == 1)) _usbAddress = buf[0];
        }


        void GetSpeed()
        {
            if (_hDevice == CyConst.INVALID_HANDLE) return;

            byte[] buf = new byte[4];
            _bHighSpeed = false;
            _bSuperSpeed = false;
            bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_GET_DEVICE_SPEED, buf, 4);

            if (bRetVal && (_bytesXfered[0] == 4))
            {
                _bHighSpeed = (buf[0] == 2);
                _bSuperSpeed = (buf[0] == 4);
            }
        }



        void SetAltIntfcParams(byte alt)
        {
            // Find match of IntfcNum and alt in table of interfaces
            if (USBCfgs[_cfgNum] != null)
            {
                for (int j = 0; j < USBCfgs[_cfgNum].AltInterfaces; j++)
                    if (USBCfgs[_cfgNum].Interfaces[j].bAlternateSetting == alt)
                    {
                        _intfcIndex = (byte)j;
                        _intfcClass = USBCfgs[_cfgNum].Interfaces[j].bInterfaceClass;
                        _intfcSubClass = USBCfgs[_cfgNum].Interfaces[j].bInterfaceSubClass; ;
                        _intfcProtocol = USBCfgs[_cfgNum].Interfaces[j].bInterfaceProtocol; ;

                        SetEndPoints();
                        return;
                    }
            }
        }


        unsafe void GetCfgDescriptor(int descIndex)
        {
            uint _MaxDescriptorBufferLength = CyConst.MaxDescriptorBufferLength + 38; // size of SINGLE_TRANSFER = 38

            if (descIndex > _configs) return;

            int len = 38 + 9;  // size of SINGLE_TRANSFER + USB_CONFIGURATION_DESCRIPTOR

            byte[] buffer = new byte[_MaxDescriptorBufferLength]; // big buffer . . . config descriptors can be large

            fixed (byte* buf = buffer)
            {
                SINGLE_TRANSFER* transfer = (SINGLE_TRANSFER*)buf;
                transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                transfer->SetupPacket.wValue = (ushort)((CyConst.USB_CONFIGURATION_DESCRIPTOR_TYPE << 8) | descIndex);
                transfer->SetupPacket.wLength = 9;	// size of the USB_COMMON_DESCRIPTOR part
                transfer->SetupPacket.dwTimeOut = 5;
                transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                transfer->BufferLength = 9;

                bool bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus = transfer->NtStatus;

                if (bRetVal)
                {
                    USB_CONFIGURATION_DESCRIPTOR* descriptor = (USB_CONFIGURATION_DESCRIPTOR*)(buf + 38);

                    // Get the entire descriptor
                    len = 38 + descriptor->wTotalLength;

                    if (len > _MaxDescriptorBufferLength) return;

                    transfer->SetupPacket.bmRequest = CyConst.TGT_DEVICE | CyConst.REQ_STD | CyConst.DIR_FROM_DEVICE;
                    transfer->SetupPacket.bRequest = CyConst.USB_REQUEST_GET_DESCRIPTOR;
                    transfer->SetupPacket.wValue = (ushort)((CyConst.USB_CONFIGURATION_DESCRIPTOR_TYPE << 8) | descIndex);
                    transfer->SetupPacket.wLength = descriptor->wTotalLength;
                    transfer->SetupPacket.dwTimeOut = 5;
                    transfer->BufferOffset = 38;	// size of the SINGLE_TRANSFER part
                    transfer->BufferLength = descriptor->wTotalLength;

                    bRetVal = IoControl(CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER, buffer, len);
                    _usbdStatus = transfer->UsbdStatus;
                    _ntStatus = transfer->NtStatus;

                    if (bRetVal)
                    {
                        int lastByte = _bytesXfered[0] - 38;
                        for (int i = 0; i < lastByte; i++)
                            _usbConfigDescriptors[descIndex][i] = buffer[i + 38];
                    }
                }
            }
        }


        void SetEndPoints()
        {
            if (_configs == 0) return;
            if (_interfaces == 0) return;

            EndPoints = USBCfgs[_cfgNum].Interfaces[_intfcIndex].EndPoints;
            int eptCount = EndPointCount;

            IsocInEndPt = null;
            IsocOutEndPt = null;
            BulkInEndPt = null;
            BulkOutEndPt = null;
            InterruptInEndPt = null;
            InterruptOutEndPt = null;

            for (int i = 1; i < eptCount; i++)
            {
                bool bIn = (EndPoints[i].Address & 0x80) > 0;
                byte attrib = EndPoints[i].Attributes;

                if (EndPoints[i] != null) EndPoints[i].XferMode = XMODE.DIRECT;

                if ((IsocInEndPt == null) && (attrib == 1) && bIn) IsocInEndPt = EndPoints[i] as CyIsocEndPoint;
                if ((BulkInEndPt == null) && (attrib == 2) && bIn) BulkInEndPt = EndPoints[i] as CyBulkEndPoint;
                if ((InterruptInEndPt == null) && (attrib == 3) && bIn) InterruptInEndPt = EndPoints[i] as CyInterruptEndPoint;

                if ((IsocOutEndPt == null) && (attrib == 1) && !bIn) IsocOutEndPt = EndPoints[i] as CyIsocEndPoint;
                if ((BulkOutEndPt == null) && (attrib == 2) && !bIn) BulkOutEndPt = EndPoints[i] as CyBulkEndPoint;
                if ((InterruptOutEndPt == null) && (attrib == 3) && !bIn) InterruptOutEndPt = EndPoints[i] as CyInterruptEndPoint;
            }

        }

        #endregion
    }

}
