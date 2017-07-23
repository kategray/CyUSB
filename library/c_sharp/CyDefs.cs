/*
 ## Cypress CyUSB C# library source file (CyDefs.cs)
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


namespace CyUSB
{

    /// <summary>
    /// Structures and Function prototypes for accessing
    /// SetupApi and other necessary Win32 APIs.
    /// The function prototypes are encapsulated
    /// in the PInvoke class.
    /// </summary>
    /// 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;    // DEVINST handle
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    #region Unused Struct SP_DEVICE_INTERFACE_DETAIL_DATA
#if (FALSE)
		[StructLayout(LayoutKind.Sequential,Pack=1)]
		public unsafe struct SP_DEVICE_INTERFACE_DETAIL_DATA 
		{
			public int  cbSize;  
			public byte[] DevicePath;  
		} 
#endif
    #endregion

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public uint dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public int dbcc_name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class DEV_BROADCAST_HANDLE
    {
        public int dbch_size;
        public uint dbch_devicetype;
        public IntPtr dbch_reserved;
        public IntPtr dbch_handle;
        public IntPtr dbch_hdevnotify;
        public Guid dbch_eventguid;
        public uint dbch_nameoffset;
        // actually a byte, but slight over-allocation is historical in this S/W & has been working well.
        public uint dbch_data;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DEV_BROADCAST_HDR
    {
        public int dbch_size;
        public uint dbch_devicetype;
        public IntPtr dbch_reserved;
        public IntPtr dbch_handle;
    }

    //
    //  3/07/2008 Modified to handle either 32 or 64 bit platforms.
    //
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OVERLAPPED
    {
        public IntPtr Internal;
        public IntPtr InternalHigh;
        public uint UnionPointerOffsetLow;
        public uint UnionPointerOffsetHigh;
        public IntPtr hEvent;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_DEVICE_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public ushort bcdUSB;
        public byte bDeviceClass;
        public byte bDeviceSubClass;
        public byte bDeviceProtocol;
        public byte bMaxPacketSize0;
        public ushort idVendor;
        public ushort idProduct;
        public ushort bcdDevice;
        public byte iManufacturer;
        public byte iProduct;
        public byte iSerialNumber;
        public byte bNumConfigurations;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct USB_ENDPOINT_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public byte bEndpointAddress;
        public byte bmAttributes;
        public ushort wMaxPacketSize;
        public byte bInterval;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public byte bMaxBurst;
        public byte bmAttributes;
        public ushort bBytesPerInterval;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_CONFIGURATION_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public ushort wTotalLength;
        public byte bNumInterfaces;
        public byte bConfigurationValue;
        public byte iConfiguration;
        public byte bmAttributes;
        public byte MaxPower;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_BOS_DESCRIPTOR
    {
        public byte bLength;/* Descriptor length*/
        public byte bDescriptorType;/* Descriptor Type */
        public ushort wToatalLength;/* Total length of descriptor ( icluding device capability*/
        public byte bNumDeviceCaps;/* Number of device capability descriptors in BOS  */
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_BOS_USB20_DEVICE_EXTENSION
    {
        public byte bLength;/* Descriptor length*/
        public byte bDescriptorType;/* Descriptor Type */
        public byte bDevCapabilityType;/* Device capability type*/
        public uint bmAttribute;// Bitmap encoding for supprted feature and  Link power managment supprted if set
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_BOS_SS_DEVICE_CAPABILITY
    {
        public byte bLength;/* Descriptor length*/
        public byte bDescriptorType;/* Descriptor Type */
        public byte bDevCapabilityType;/* Device capability type*/
        public byte bmAttribute;// Bitmap encoding for supprted feature and  Link power managment supprted if set
        public ushort wSpeedsSuported;//low speed supported if set,full speed supported if set,high speed supported if set,super speed supported if set,15:4 nt used        
        public byte bFunctionalitySupporte;
        public byte bU1DevExitLat;//U1 device exit latency        
        public ushort bU2DevExitLat;//U2 device exit latency        
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_BOS_CONTAINER_ID
    {
        public byte bLength;/* Descriptor length*/
        public byte bDescriptorType;/* Descriptor Type */
        public byte bDevCapabilityType;/* Device capability type*/
        public byte bReserved; // no use
        unsafe public fixed byte ContainerID[CyConst.USB_BOS_CAPABILITY_TYPE_CONTAINER_ID_SIZE];/* UUID */
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_INTERFACE_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public byte bInterfaceNumber;
        public byte bAlternateSetting;
        public byte bNumEndpoints;
        public byte bInterfaceClass;
        public byte bInterfaceSubClass;
        public byte bInterfaceProtocol;
        public byte iInterface;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct USB_STRING_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public ushort bString;  // This will be a wide char
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct USB_COMMON_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ISO_PKT_INFO
    {
        public uint Status;
        public uint Length;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SETUP_PACKET
    {
        public byte bmRequest;
        public byte bRequest;
        public ushort wValue;
        public ushort wIndex;
        public ushort wLength;
        public uint dwTimeOut;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SINGLE_TRANSFER
    {
        public SETUP_PACKET SetupPacket;

        public byte WaitForever;
        public byte ucEndpointAddress;
        public uint NtStatus;
        public uint UsbdStatus;
        public uint IsoPacketOffset;
        public uint IsoPacketLength;
        public uint BufferOffset;
        public uint BufferLength;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SET_TRANSFER_SIZE_INFO
    {
        public byte EndpointAddress;
        public int TransferSize;
    }


    // HID Structs
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HIDD_ATTRIBUTES
    {
        public uint size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        public ushort R0, R1, R2, R3, R4, R5, R6, R7, R8, R9;
        public ushort R10, R11, R12, R13, R14, R15, R16;

        public ushort NumberLinkCollectionNodes;

        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;

        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;

        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    #region Unused struct HIDP_BUTTON_CAPS
#if (FALSE)

	[StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct HIDP_BUTTON_CAPS
	{
		public ushort	UsagePage;
		public byte		ReportID;
		public byte		IsAlias;

		public ushort	BitField;
		public ushort	LinkCollection;   

		public ushort	LinkUsage;
		public ushort	LinkUsagePage;

		public byte		IsRange;
		public byte		IsStringRange;
		public byte		IsDesignatorRange;
		public byte		IsAbsolute;

		public uint		R0,R1,R2,R3,R4,R5,R6,R7,R8,R9;		// Reserved

		public ushort	Usage,				UsageMax;
		public ushort	StringIndex,        StringMax;
		public ushort	DesignatorIndex,    DesignatorMax;
		public ushort	DataIndex,			DataIndexMax;
	}
  
#endif
    #endregion


    // This struct holds HIDP_BUTTON or HIDP_VALUE CAPS - They're both the same size
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HIDP_BTN_VAL_CAPS
    {
        public ushort UsagePage;
        public byte ReportID;
        public byte IsAlias;

        public ushort BitField;
        public ushort LinkCollection;

        public ushort LinkUsage;
        public ushort LinkUsagePage;

        public byte IsRange;
        public byte IsStringRange;
        public byte IsDesignatorRange;
        public byte IsAbsolute;

        public byte HasNull;            // Value caps only, reserved in Button Caps
        public byte Reserved;           // Value caps only, reserved in Button Caps
        public ushort BitSize;            // Value caps only, reserved in Button Caps

        public ushort ReportCount;        // Value caps only, reserved in Button Caps
        public ushort R0, R1, R2, R3, R4;		// Reserved

        public uint UnitsExp;           // Value caps only, reserved in Button Caps
        public uint Units;              // Value caps only, reserved in Button Caps

        public int LogicalMin, LogicalMax;     // Value caps only, reserved in Button Caps
        public int PhysicalMin, PhysicalMax;   // Value caps only, reserved in Button Caps

        public ushort Usage, UsageMax;
        public ushort StringIndex, StringMax;
        public ushort DesignatorIndex, DesignatorMax;
        public ushort DataIndex, DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HID_DATA
    {
        public byte IsButtonData;
        public byte Reserved;
        public ushort UsagePage;
        public uint Status;
        public uint ReportID;
        public byte IsDataSet;

        public ushort Usage;
        public uint Value;
        public uint UsageMin;
        public ushort UsageMax;
        public uint MaxUsageLength;
        public ushort[] Usages;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 56)]
    internal struct SCSI_PASS_THROUGH
    {
        public ushort Length;
        public byte ScsiStatus;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte CdbLength;
        public byte SenseInfoLength;
        public byte DataIn;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public UInt64 DataBufferOffset;
        public uint SenseInfoOffset;
        public byte Cdb;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 44)]
    internal struct SCSI_PASS_THROUGH32
    {
        public ushort Length;
        public byte ScsiStatus;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte CdbLength;
        public byte SenseInfoLength;
        public byte DataIn;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public uint DataBufferOffset;
        public uint SenseInfoOffset;
        public byte Cdb;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 80)]
    internal struct SCSI_PASS_THROUGH_WITH_BUFFERS
    {
        public SCSI_PASS_THROUGH spt;
        public uint totalSize;
        public byte senseInfoBuffer; // 18 bytes
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 68)]
    internal struct SCSI_PASS_THROUGH_WITH_BUFFERS32
    {
        public SCSI_PASS_THROUGH32 spt;
        public uint totalSize;
        public byte senseInfoBuffer; // 18 bytes
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct CDB10
    {
        public byte Cmd;
        public byte OpCode;
        public uint LBA;
        public byte Bank;
        public ushort Blocks;
        public byte Control;
    }


    public enum HIDP_REPORT_TYPE { HidP_Input, HidP_Output, HidP_Feature };

    public enum XMODE { DIRECT, BUFFERED };

    public enum FX3_FWDWNLOAD_MEDIA_TYPE { RAM = 1, I2CE2PROM, SPIFLASH };
    public enum FX3_FWDWNLOAD_ERROR_CODE { SUCCESS = 0, FAILED, INVALID_MEDIA_TYPE, INVALID_FWSIGNATURE, DEVICE_CREATE_FAILED, INCORRECT_IMAGE_LENGTH, INVALID_FILE, SPIFLASH_ERASE_FAILED, CORRUPT_FIRMWARE_IMAGE_FILE, EXCEED_IMAGE_LENGTH, I2CEEPROM_UNKNOWN_I2C_SIZE };

    public static class CyConst
    {
        // USB3.0 specific constant defination
        internal const ushort bcdUSBJJMask = 0xFF00; //(0xJJMN JJ - Major version,M Minor version, N sub-minor vesion)
        internal const ushort USB30MajorVer = 0x0300;
        internal const ushort USB20MajorVer = 0x0200;

        internal static Guid CyGuid = new Guid("{0xae18aa60, 0x7f6a, 0x11d4, {0x97, 0xdd, 0x0, 0x1, 0x2, 0x29, 0xb9, 0x59}}");
        internal static Guid DiskGuid = new Guid("{0x53f56307, 0xb6bf, 0x11d0, {0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b}}");
        internal static Guid CdGuid = new Guid("{0x53f56308, 0xb6bf, 0x11d0, {0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b}}");
        internal static Guid StorGuid = new Guid("{0x4D36E965, 0xE325, 0x11CE, {0xBF, 0xC1, 0x08, 0x00, 0x2B, 0xE1, 0x03, 0x18}}");
        internal static Guid Customer_DriverGuid = new Guid("{0x00000000, 0x0000, 0x0000, {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}}");
        internal static Guid Customer_ClassGuid = new Guid("{0x00000000, 0x0000, 0x0000, {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}}");

        public static IntPtr INVALID_HANDLE = new IntPtr(-1);
        //public static int OverlapSignalAllocSize = IntPtr.Size;
        public static int OverlapSignalAllocSize = Marshal.SizeOf(typeof(OVERLAPPED));

        internal const uint DIGCF_PRESENT = 0x00000002;
        internal const uint DIGCF_ALLCLASSES = 0x00000004;
        internal const uint DIGCF_INTERFACEDEVICE = 0x00000010;
        internal const int FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const int OPEN_EXISTING = 3;

        internal const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        internal const uint DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        internal const uint DBT_DEVTYP_HANDLE = 0x00000006;

        public static IntPtr DBT_DEVICEARRIVAL = new IntPtr(0x8000);
        public static IntPtr DBT_DEVICEREMOVECOMPLETE = new IntPtr(0x8004);
        internal static IntPtr DBT_DEVNODES_CHANGED = new IntPtr(0x0007);
        internal const int WM_DEVICECHANGE = 0x0219;

        internal const int WM_POWERBROADCAST = 0x0218;
        internal static IntPtr PBT_APMRESUMEAUTOMATIC = new IntPtr(0x0012);
        internal static IntPtr PBT_APMSUSPEND = new IntPtr(0x0004);
        internal static bool Hibernate_first_call = false;

        internal const byte SIZEOF_USB_BOS_DESCRIPTOR = 0x5;
        public const byte TGT_DEVICE = 0x00;
        public const byte TGT_INTFC = 0x01;
        public const byte TGT_ENDPT = 0x02;
        public const byte TGT_OTHER = 0x03;

        public const byte REQ_STD = 0x00;
        public const byte REQ_CLASS = 0x20;
        public const byte REQ_VENDOR = 0x40;

        public const byte DIR_TO_DEVICE = 0x00;
        public const byte DIR_FROM_DEVICE = 0x80;

        public const byte DEVICES_CYUSB = 0x01;
        public const byte DEVICES_MSC = 0x02;
        public const byte DEVICES_HID = 0x04;

        internal const byte USB_DEVICE_DESCRIPTOR_TYPE = 0x01;
        internal const byte USB_CONFIGURATION_DESCRIPTOR_TYPE = 0x02;
        internal const byte USB_STRING_DESCRIPTOR_TYPE = 0x03;
        internal const byte USB_INTERFACE_DESCRIPTOR_TYPE = 0x04;
        internal const byte USB_ENDPOINT_DESCRIPTOR_TYPE = 0x05;
        internal const byte USB_BOS_DESCRIPTOR_TYPE = 0x0F;
        internal const byte USB_DEVICE_CAPABILITY = 0x10;
        internal const byte USB_SUPERSPEED_ENDPOINT_COMPANION = 0x30;

        internal const byte USB_BOS_CAPABILITY_TYPE_Wireless_USB = 0x01;
        internal const byte USB_BOS_CAPABILITY_TYPE_USB20_EXT = 0x02;
        internal const byte USB_BOS_CAPABILITY_TYPE_SUPERSPEED_USB = 0x03;
        internal const byte USB_BOS_CAPABILITY_TYPE_CONTAINER_ID = 0x04;
        internal const byte USB_BOS_CAPABILITY_TYPE_CONTAINER_ID_SIZE = 0x10;


        internal const byte USB_REQUEST_GET_DESCRIPTOR = 0x06;

        internal const uint IOCTL_ADAPT_GET_DRIVER_VERSION = 0x00220000 + (0 * 4);
        internal const uint IOCTL_ADAPT_GET_USBDI_VERSION = 0x00220000 + (1 * 4);
        internal const uint IOCTL_ADAPT_GET_ALT_INTERFACE_SETTING = 0x00220000 + (2 * 4);
        internal const uint IOCTL_ADAPT_SELECT_INTERFACE = 0x00220000 + (3 * 4);
        internal const uint IOCTL_ADAPT_GET_ADDRESS = 0x00220000 + (4 * 4);
        internal const uint IOCTL_ADAPT_GET_NUMBER_ENDPOINTS = 0x00220000 + (5 * 4);
        internal const uint IOCTL_ADAPT_GET_DEVICE_POWER_STATE = 0x00220000 + (6 * 4);
        internal const uint IOCTL_ADAPT_SET_DEVICE_POWER_STATE = 0x00220000 + (7 * 4);
        internal const uint IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER = 0x00220000 + (8 * 4);
        internal const uint IOCTL_ADAPT_SEND_NON_EP0_TRANSFER = 0x00220000 + (9 * 4);
        internal const uint IOCTL_ADAPT_CYCLE_PORT = 0x00220000 + (10 * 4);
        internal const uint IOCTL_ADAPT_RESET_PIPE = 0x00220000 + (11 * 4);
        internal const uint IOCTL_ADAPT_RESET_PARENT_PORT = 0x00220000 + (12 * 4);
        internal const uint IOCTL_ADAPT_GET_TRANSFER_SIZE = 0x00220000 + (13 * 4);
        internal const uint IOCTL_ADAPT_SET_TRANSFER_SIZE = 0x00220000 + (14 * 4);
        internal const uint IOCTL_ADAPT_GET_DEVICE_NAME = 0x00220000 + (15 * 4);
        internal const uint IOCTL_ADAPT_GET_FRIENDLY_NAME = 0x00220000 + (16 * 4);
        internal const uint IOCTL_ADAPT_ABORT_PIPE = 0x00220000 + (17 * 4);
        internal const uint IOCTL_ADAPT_SEND_NON_EP0_DIRECT = 0x00220003 + (18 * 4);
        internal const uint IOCTL_ADAPT_GET_DEVICE_SPEED = 0x00220000 + (19 * 4);


        internal const uint IOCTL_SCSI_PASS_THROUGH = 0x0004d004;

        internal const uint ERROR_SUCCESS = 0;
        internal const uint ERROR_IO_PENDING = 997;
        internal const uint WAIT_OBJECT_0 = 0;
        internal const uint WAIT_TIMEOUT = 0x00000102;
        public const uint INFINITE = 0xFFFFFFFF;

        internal const uint ERROR_NO_MORE_ITEMS = 259;
        internal const ushort USB_STRING_MAXLEN = 256;
        internal const byte MAX_INTERFACES = 255;
        internal const byte MAX_ENDPOINTS = 16;

        public const byte SINGLE_XFER_LEN = 38;

        internal const uint HIDP_STATUS_SUCCESS = 0x00110000;
        internal const uint MaxDescriptorBufferLength = 1024;

        internal const byte LARGEEEPROM_FW_VERIFIATIONCODE = 0xA9;
        internal const byte SMALLEEPROM_FW_VERIFIATIONCODE = 0xA2;

        internal const int CONTROLTFRER_DATA_LENGTH = 2048; // 4096 changing it 2048 as per CDT 130492

        public static void SetCustomerGUID(string ClassGuid, string DriverGuid)
        {
            Customer_ClassGuid = GuidFromString(ClassGuid);
            Customer_DriverGuid = GuidFromString(DriverGuid);
        }

        public static void SetClassGuid(string ClassGuid)
        {
            SetCustomerGUID(ClassGuid, "0");
        }

        private static Guid GuidFromString(string sguid)
        {
            // This routine created because, occasionally, ill-formed GUIDs were being
            // found in the registry.  This routine handles such situations, returning Guid.Empty.
            try
            {
                Guid g = new Guid(sguid);
                return g;
            }
            catch
            {
                return Guid.Empty;
            }
        }
    }

    public delegate void App_PnP_Callback(IntPtr pnpEvent, IntPtr hRemovedDevice);

}
