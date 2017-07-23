/*
 ## Cypress CyUSB C# library source file (PInvoke.cs)
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
using System.Runtime.InteropServices;
using System.Text;


namespace CyUSB
{
    public static class PInvoke
    {
        #region kernel32.dll - DeviceIoControl, CreateFile, etc.
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool DeviceIoControl(
                   [In] IntPtr hDevice,
           [In] uint dwIoControlCode,
           [In, Out] IntPtr lpInBuffer,
           [In] int nInBufferSize,
           [In, Out] IntPtr lpOutBuffer,
           [In] int nOutBufferSize,
           [In, Out] IntPtr lpBytesReturned,
           [Out] IntPtr lpOverlapped);

        [DllImport("Kernel32.dll")]
        internal static extern IntPtr CreateFile(
           [In] byte[] filename,
           [In] int fileaccess,
           [In] int fileshare,
           [In] int lpSecurityattributes,
           [In] int creationdisposition,
           [In] int flags,
           [In] IntPtr template);

        [DllImport("Kernel32.dll")]
        internal static extern bool ReadFile(
           [In]  IntPtr hDevice,
           [In, Out] byte[] lpBuffer,
           [In]  int nNumberOfBytesToRead,
           [In, Out] ref int lpNumberOfBytesRead,
           [Out] IntPtr lpOverlapped);

        [DllImport("Kernel32.dll")]
        internal static extern bool WriteFile(
            [In] IntPtr hDevice,
            [In] byte[] lpBuffer,
            [In] int nNumberOfBytesToWrite,
            [In, Out] ref int lpNumberOfBytesWritten,
            [Out]    IntPtr lpOverlapped);

        [DllImport("Kernel32.dll")]
        public static extern IntPtr CreateEvent(
           [In] uint lpEventAttributes,
           [In] uint bManualReset,
           [In] uint bInitialState,
           [In] uint lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle([In] IntPtr hDevice);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Int32 WaitForSingleObject(
           [In] IntPtr h,
           [In] uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetOverlappedResult(
                   [In] IntPtr h,
           [In] byte[] lpOverlapped,
           [In, Out] ref uint bytesXferred,
           [In] uint bWait);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern UInt32 GetLastError();
        #endregion

        #region setupapi.dll
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
   [In] ref Guid ClassGuid,
   [In] byte[] Enumerator,
   [In] IntPtr hwndParent,
   [In] uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInterfaces(
   [In] IntPtr DeviceInfoSet,
   [In] uint DeviceInfoData,
   [In] ref Guid InterfaceClassGuid,
   [In] uint MemberIndex,
   [Out] SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);


        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
   [In] IntPtr DeviceInfoSet,				   // Handle passed in	
   [In] SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,		   // Ptr passed in	
   [Out] byte[] DeviceInterfaceDetailData,     // null passed in
   [In] int DeviceInterfaceDetailDataSize, // 0 passed in
   [In, Out] ref int RequiredSize,                  // size passed back
   [Out] SP_DEVINFO_DATA DeviceInfoData);			   // null - we don't want this data	


        [DllImport("setupapi.dll")]
        internal static extern bool
    SetupDiDestroyDeviceInfoList([In] IntPtr DeviceInfoSet);
        #endregion

        #region cfgmgr32.dll
        [DllImport("cfgmgr32.dll", SetLastError = true)]
        internal static extern UInt32 CM_Locate_DevNode(
    [In, Out]        ref IntPtr DeviceInfoSet,				   // Handle passed back	
                [In]            byte[] DevicePath,
                [In]            UInt64 Flags);
        #endregion

        #region user32.dll - Register Device Notification
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(
   [In] IntPtr h,
   [In] DEV_BROADCAST_DEVICEINTERFACE dFilter,
   [In] uint flags);


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(
   [In] IntPtr h,
   [In] DEV_BROADCAST_HANDLE dFilter,
   [In] uint flags);


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr UnregisterDeviceNotification([In] IntPtr h);
        #endregion

        #region Hid.dll Functions

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern void HidD_GetHidGuid([In, Out] ref Guid HidGuid);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetManufacturerString(
   [In] IntPtr h,
   [Out] byte[] Mfg,
   [In] uint slen);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetProductString(
   [In] IntPtr h,
   [Out] byte[] Mfg,
   [In] uint slen);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetSerialNumberString(
   [In] IntPtr h,
   [Out] byte[] SerialNum,
   [In] uint slen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_GetPreparsedData(
   [In] IntPtr h,
   [In, Out] ref byte* data);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_GetAttributes(
   [In] IntPtr h,
   [In, Out] ref HIDD_ATTRIBUTES attr);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_GetFeature(
   [In] IntPtr h,
   [In, Out] byte[] lpFeatureData,
   [In] int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_GetInputReport(
   [In] IntPtr h,
   [In] byte[] lpReportData,
   [In] int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_SetFeature(
   [In] IntPtr h,
   [In] byte[] lpFeatureData,
   [In] int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_SetOutputReport(
   [In] IntPtr h,
   [In] byte[] lpFeatureData,
   [In] int bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern uint HidP_SetUsageValue(
    [In] HIDP_REPORT_TYPE RptType,
   [In] ushort usagePage,
   [In] ushort linkCollection,
   [In] ushort usage,
   [In] uint usageValue,
   [In] byte* preparsedData,
   [In] ref byte lpReportData,
   [In] uint bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern uint HidP_SetUsages(
   [In] HIDP_REPORT_TYPE RptType,
   [In] ushort usagePage,
   [In] ushort linkCollection,
   [In] ref ushort usages,
   [In] ref uint numUsages,
   [In] byte* preparsedData,
   [In] ref byte lpReportData,
   [In] uint bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern uint HidP_UnsetUsages(
   [In] HIDP_REPORT_TYPE RptType,
   [In] ushort usagePage,
   [In] ushort linkCollection,
   [In] ref ushort usages,
   [In] ref uint numUsages,
   [In] byte* preparsedData,
   [In] ref byte lpReportData,
   [In] uint bufLen);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidP_GetCaps(
   [In] byte* preparsedData,
   [In, Out] ref HIDP_CAPS caps);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidP_GetButtonCaps(
   [In] HIDP_REPORT_TYPE RptType,
   [Out] byte* ButtonCaps,
   [In] ref int numCaps,
   [In] byte* preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidP_GetValueCaps(
   [In] HIDP_REPORT_TYPE RptType,
   [Out] byte* ValueCaps,
   [In] ref int numCaps,
   [In] byte* preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern uint HidP_MaxUsageListLength(
   [In] HIDP_REPORT_TYPE RptType,
   [In] ushort numCaps,
   [In] byte* preparsedData);


        [DllImport("hid.dll", SetLastError = true)]
        internal unsafe static extern bool HidD_FreePreparsedData(byte* data);

        #endregion

        #region Is handle Valid

        private static bool IsHandleValid(IntPtr handle)
        {
            bool valid = false;
            if (handle == null)
                return false;

            if (IntPtr.Size == 4)
            {
                Int32 value = handle.ToInt32();
                valid = (value != -1);
            }
            else if (IntPtr.Size == 8)
            {
                Int64 value = handle.ToInt64();
                valid = (value != -1);
            }

            return valid;
        }

        #endregion

        internal static byte CountDevices(Guid g)
        {
            byte Devices = 0;


            IntPtr hwDeviceInfo = SetupDiGetClassDevs(ref g, null, IntPtr.Zero, CyConst.DIGCF_PRESENT | CyConst.DIGCF_INTERFACEDEVICE);
            if (IsHandleValid(hwDeviceInfo))
            {
                SP_DEVICE_INTERFACE_DATA devInterfaceData = new SP_DEVICE_INTERFACE_DATA();

                devInterfaceData.cbSize = Marshal.SizeOf(devInterfaceData);
                // if (IntPtr.Size == 8)
                //devInterfaceData.cbSize = devInterfaceData.cbSize + 4;

                //devInterfaceData.InterfaceClassGuid = g;
                // Count the number of devices
                uint i = 0;
                bool bDone = false;


                while (!bDone)
                {
                    //SetupDiEnumDeviceInterfaces (hwDeviceInfo, 0, ref g, i, devInterfaceData)
                    if (SetupDiEnumDeviceInterfaces(hwDeviceInfo, 0, ref g, i, devInterfaceData))
                        Devices++;
                    else
                    {
                        int dwLastError = Marshal.GetLastWin32Error();
                        if (dwLastError == CyConst.ERROR_NO_MORE_ITEMS) bDone = true;
                    }

                    i++;
                }

                SetupDiDestroyDeviceInfoList(hwDeviceInfo);
            }

            return Devices;

        }


        internal static IntPtr GetDeviceHandle(string devPath, bool bOverlapped)
        {
            int dummy = (int)FileAccess.ReadWrite;

            // This call doesn't care about the file access returned.
            return GetDeviceHandle(devPath, bOverlapped, ref dummy);
        }


        // Returns the FileAccess level that was able to get a handle to the device.  
        // This is used by CyHidDevice's Open() method.
        internal static IntPtr GetDeviceHandle(string devPath, bool bOverlapped, ref int accessMode)
        {
            int sLen = devPath.Length;
            byte[] path = new byte[sLen + 1];

            // Move the chars of the DevicePath field to the front of the array.
            for (int i = 0; i < sLen; i++) path[i] = (byte)devPath[i];

            // Try to get a handle, with decreasing read/write privileges.
            // (Some HID devices won't allow Read or Write access mode.)

            accessMode = (int)FileAccess.ReadWrite;

            IntPtr handle = CreateFile(path,
                                        accessMode,
                                        (int)FileShare.ReadWrite,
                                                        0,
                                                        (int)FileMode.Open,
                                                        bOverlapped ? CyConst.FILE_FLAG_OVERLAPPED : 0,
                                                        IntPtr.Zero);

            // If open with FileAccess.ReadWrite failed, try with FileAccess.Read
            if (handle == CyConst.INVALID_HANDLE)
            {
                accessMode = (int)FileAccess.Read;

                handle = CreateFile(path,
                                    accessMode,
                                    (int)FileShare.Read,
                                    0,
                                    (int)FileMode.Open,
                                    bOverlapped ? CyConst.FILE_FLAG_OVERLAPPED : 0,
                                    IntPtr.Zero);
            }

            // If open with FileAccess.Read failed, try with access mode = 0
            if (handle == CyConst.INVALID_HANDLE)
            {
                accessMode = 0;
                handle = CreateFile(path,
                                    accessMode,
                                    (int)FileShare.ReadWrite,
                                    0,
                                    (int)FileMode.Open,
                                    bOverlapped ? CyConst.FILE_FLAG_OVERLAPPED : 0,
                                    IntPtr.Zero);
            }

            return handle;

        }


        internal static string GetDevicePath(Guid g, uint dev)
        {
            return GetDevicePath(g, dev, null);
        }


        internal static string GetDevicePath(Guid g, uint dev, byte[] enumer)
        {
            IntPtr hDevice = CyConst.INVALID_HANDLE;

            int predictedLength = 0;
            int actualLength = 0;

            uint flags = CyConst.DIGCF_PRESENT | (uint)((enumer == null) ? CyConst.DIGCF_INTERFACEDEVICE : CyConst.DIGCF_ALLCLASSES);

            IntPtr hwDeviceInfo = SetupDiGetClassDevs(ref g, enumer, IntPtr.Zero, flags);
            if (hwDeviceInfo.Equals(-1))
                return "";

            SP_DEVICE_INTERFACE_DATA devInterfaceData = new SP_DEVICE_INTERFACE_DATA();
            devInterfaceData.cbSize = Marshal.SizeOf(devInterfaceData);
            //if (IntPtr.Size == 8)
            //devInterfaceData.cbSize = devInterfaceData.cbSize + 4;

            int mysize = Marshal.SizeOf(devInterfaceData.Reserved);
            if (!SetupDiEnumDeviceInterfaces(hwDeviceInfo, 0, ref g, dev, devInterfaceData))
            {
                SetupDiDestroyDeviceInfoList(hwDeviceInfo);
                return "";
            }

            SetupDiGetDeviceInterfaceDetail(hwDeviceInfo, devInterfaceData, null, 0, ref predictedLength, null);
            //predictedLength = predictedLength+5;
            byte[] detailData = new byte[predictedLength];

            detailData[0] = 5;  // Set the cbSize field of what would be a SP_DEVICE_INTERFACE_DETAIL_DATA struct
            if (IntPtr.Size == 8)
                detailData[0] = 8;
            /*if (Marshal.SystemDefaultCharSize == 2)
                detailData[0] = 4 + 4;
            else
                detailData[0] = 4 + 1;*/

            if (!SetupDiGetDeviceInterfaceDetail(hwDeviceInfo, devInterfaceData, detailData,
                    predictedLength, ref actualLength, null))
            {
                int Error = Marshal.GetLastWin32Error();
                SetupDiDestroyDeviceInfoList(hwDeviceInfo);
                return "";
            }

            //
            // Convert to a string
            //
            //  DWY - Ensure null terminator for string class constructor.
            //
            char[] cData = new char[actualLength - 3];
            for (int i = 0; i < (actualLength - 4); i++)
            {
                cData[i] = (char)detailData[i + 4];
                cData[i + 1] = '\0';
            }
            string path = new string(cData);

            SetupDiDestroyDeviceInfoList(hwDeviceInfo);
            return path;
        }

    }


}
