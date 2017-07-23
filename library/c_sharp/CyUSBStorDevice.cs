/*
 ## Cypress CyUSB C# library source file (CyUSBStorDevice.cs)
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
    /// Summary description for CyUSBStorDevice.
    /// </summary>
    public class CyUSBStorDevice : USBDevice
    {
        int _BlockSize;
        public int BlockSize
        {
            get { return _BlockSize; }
        }

        uint _TimeOut;
        public uint TimeOut
        {
            get { return _TimeOut; }
            set { _TimeOut = value; }
        }

        //internal CyUSBStorDevice(Guid g):base(Guid.Empty)	
        internal CyUSBStorDevice(Guid g)
            : base(g)
        {
            _BlockSize = 512;
            _TimeOut = 20;
        }


        // Opens a handle to the devTH device attached the USBSTOR.SYS driver
        internal override bool Open(byte dev)
        {
            // If this CCyUSBDevice object already has the driver open, close it.
            if (_hDevice != CyConst.INVALID_HANDLE)
                Close();

            _devices = DeviceCount;
            if (_devices == 0) return false;
            if (dev > (_devices - 1)) return false;

            _path = GetDevicePath(dev);  // Also sets the DrvGuid to either CdGuid or DiskGuid.
            _hDevice = PInvoke.GetDeviceHandle(_path, true);
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            _devNum = dev;
            GetDeviceParameters();

            _driverName = "usbstor.sys";

            return true;

        }


        private void GetDeviceParameters()
        {
            int a = Path.IndexOf("usbstor");
            int len = Path.IndexOf("#{") - a;
            string tmp = Path.Substring(a, len).Replace("#", "\\");
            string DevKeyString = "SYSTEM\\CurrentControlSet\\Enum\\" + tmp; // Location in XP

            RegistryKey LocalMachine = Registry.LocalMachine;
            RegistryKey DevKey = LocalMachine.OpenSubKey(DevKeyString);
            if (DevKey == null)
            {
                DevKeyString = "Enum\\" + tmp; // Location in 98
                DevKey = LocalMachine.OpenSubKey(DevKeyString);
                if (DevKey == null) return;
            }

            _friendlyName = (string)DevKey.GetValue("FriendlyName");


            int x = tmp.LastIndexOf("\\") + 1;
            len = tmp.Length;
            _serialNumber = tmp.Substring(x, len - x).ToUpper();

            // Many Serial Numbers have '&0' tacked-on at the end
            x = _serialNumber.LastIndexOf("&");
            if (x > 0)
            {
                len = _serialNumber.Length;
                _serialNumber = _serialNumber.Substring(0, x);
            }

            int lastAmpersand = _serialNumber.LastIndexOf("&");
            string ParentIDPrefix = (lastAmpersand >= 0) ? _serialNumber.Substring(0, lastAmpersand) : "";

            DevKey = GetUSBDevKey(ParentIDPrefix);
            if (DevKey == null) return;

            _manufacturer = (string)DevKey.GetValue("Mfg");
            _name = (string)DevKey.GetValue("DeviceDesc");
            _product = (string)DevKey.GetValue("LocationInformation");

            string[] vidpid = (string[])DevKey.GetValue("HardwareID");

            x = vidpid[0].LastIndexOf("Vid_");
            if (x == -1)
                x = vidpid[0].LastIndexOf("VID_");
            x = x + 4;
            string sVid = vidpid[0].Substring(x, 4);
            _vendorID = (ushort)Util.HexToInt(sVid);

            x = vidpid[0].LastIndexOf("Pid_");
            if (x == -1)
                x = vidpid[0].LastIndexOf("PID_");
            x = x + 4;
            string sPid = vidpid[0].Substring(x, 4);
            _productID = (ushort)Util.HexToInt(sPid);

            x = vidpid[0].LastIndexOf("Rev_");
            if (x == -1)
                x = vidpid[0].LastIndexOf("REV_");
            x = x + 4;
            string sRev = vidpid[0].Substring(x, 4);
            _bcdUSB = (ushort)Util.HexToInt(sRev);



            string[] classids = (string[])DevKey.GetValue("CompatibleIDs");

            x = classids[0].IndexOf("Class_") + 6;
            string sClass = classids[0].Substring(x, 2);
            _devClass = (byte)Util.HexToInt(sClass);

            x = classids[0].IndexOf("SubClass_") + 9;
            string sSub = classids[0].Substring(x, 2);
            _devSubClass = (byte)Util.HexToInt(sSub);

            x = classids[0].IndexOf("Prot_") + 5;
            string sProt = classids[0].Substring(x, 2);
            _devProtocol = (byte)Util.HexToInt(sProt);
        }


        private RegistryKey GetUSBDevKey(string ParentPrefix)
        {
            RegistryKey LocalMachine = Registry.LocalMachine;
            RegistryKey DevKey = LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\USB");
            if (DevKey == null)
            {
                //DevKey = LocalMachine.OpenSubKey("Enum\\USB"); // Location in 98
                //if (DevKey == null) return null;
            }

            int usbKeys = DevKey.SubKeyCount;
            string[] sUKeys = DevKey.GetSubKeyNames();

            for (int i = 0; i < usbKeys; i++)
            {
                RegistryKey usbKey = null;
                try
                {
                    usbKey = DevKey.OpenSubKey(sUKeys[i]);
                }
                catch (SecurityException)
                {
                }
                int dKeys = usbKey.SubKeyCount;
                string[] sDKeys = usbKey.GetSubKeyNames();

                for (int j = 0; j < dKeys; j++)
                {
                    RegistryKey snKey = null;
                    try
                    {
                        snKey = usbKey.OpenSubKey(SerialNumber); // The Serial # key
                    }
                    catch (SecurityException)
                    {
                    }

                    if (snKey != null)
                        return snKey;

                    // Not a real Serial Number - Try to match ParentPrefix string to ParentIdPrefix value
                    RegistryKey itemKey = null;
                    try
                    {
                        itemKey = usbKey.OpenSubKey(sDKeys[j]);
                    }
                    catch (SecurityException)
                    {
                    }

                    string tmp = (string)itemKey.GetValue("ParentIdPrefix");
                    if (tmp != null)
                    {
                        if (ParentPrefix.Equals(tmp.ToUpper()))
                        {
                            _serialNumber = "";
                            return itemKey;
                        }
                    }
                }
            }

            return null;

        }


        private string GetDevicePath(byte dev)
        {
            string dPath;
            byte dCnt = 0;

            //string s = "USBSTOR";
            //byte[] sClass = new byte[s.Length+1];
            //Encoding.ASCII.GetBytes(s,0,s.Length,sClass,0);

            //dPath = PInvoke.GetDevicePath(Guid.Empty,0,sClass);

            // First look for the devTH USBSTOR Disk
            byte i = 0;
            do
            {
                dPath = PInvoke.GetDevicePath(CyConst.DiskGuid, i);

                if (dPath.IndexOf("\\usbstor#") > -1)
                {
                    if (dev == dCnt)
                    {
                        _drvGuid = CyConst.DiskGuid;
                        return dPath;
                    }
                    else
                        dCnt++;
                }

                i++;
            }
            while (dPath.Length > 0);



            // Next look for the devTH USBSTOR CD
            i = 0;
            do
            {
                dPath = PInvoke.GetDevicePath(CyConst.CdGuid, i);

                if (dPath.IndexOf("\\usbstor#") > -1)
                {
                    if (dev == dCnt)
                    {
                        _drvGuid = CyConst.CdGuid;
                        return dPath;
                    }
                    else
                        dCnt++;
                }

                i++;
            }
            while (dPath.Length > 0);




            return "";
        }


        internal override byte DeviceCount
        {
            get
            {
                string dPath;
                uint i;
                byte dCount = 0;

                i = 0;
                do
                {
                    dPath = PInvoke.GetDevicePath(CyConst.DiskGuid, i);
                    if (dPath.IndexOf("\\usbstor#") > -1) dCount++;
                    i++;
                }
                while (dPath.Length > 0);

                i = 0;
                do
                {
                    dPath = PInvoke.GetDevicePath(CyConst.CdGuid, i);
                    if (dPath.IndexOf("\\usbstor#") > -1) dCount++;
                    i++;
                }
                while (dPath.Length > 0);


                return dCount;
            }
        }


        public override string ToString()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            StringBuilder s = new StringBuilder("<MSC_DEVICE>\r\n");

            s.Append(string.Format("\tFriendlyName=\"{0}\"\r\n", FriendlyName));
            s.Append(string.Format("\tManufacturer=\"{0}\"\r\n", Manufacturer));
            s.Append(string.Format("\tProduct=\"{0}\"\r\n", Product));
            s.Append(string.Format("\tSerialNumber=\"{0}\"\r\n", SerialNumber));
            s.Append(string.Format("\tVendorID=\"{0}\"\r\n", Util.byteStr(VendorID)));
            s.Append(string.Format("\tProductID=\"{0}\"\r\n", Util.byteStr(ProductID)));
            s.Append(string.Format("\tClass=\"{0:X2}h\"\r\n", _devClass));
            s.Append(string.Format("\tSubClass=\"{0:X2}h\"\r\n", _devSubClass));
            s.Append(string.Format("\tProtocol=\"{0:X2}h\"\r\n", _devProtocol));
            s.Append(string.Format("\tBcdUSB=\"{0}\"\r\n", Util.byteStr(_bcdUSB)));

            s.Append("</MSC_DEVICE>\r\n");
            return s.ToString();
        }

        private unsafe bool SendScsiCmd64(byte cmd, byte op, byte lun, byte dirIn, int bank, int lba, int bytes, byte[] data)
        {
            SCSI_PASS_THROUGH_WITH_BUFFERS sptB = new SCSI_PASS_THROUGH_WITH_BUFFERS();
            int len = Marshal.SizeOf(sptB) + bytes;		// total size of buffer
            byte[] buffer = new byte[len];


            fixed (byte* buf = buffer)
            {
                SCSI_PASS_THROUGH_WITH_BUFFERS* sptBuf = (SCSI_PASS_THROUGH_WITH_BUFFERS*)buf;
                SCSI_PASS_THROUGH* spt = (SCSI_PASS_THROUGH*)buf;
                CDB10* cdb = (CDB10*)&(spt->Cdb);
                bool bRetVal;

                sptBuf->totalSize = (uint)len;

                spt->Length = (ushort)Marshal.SizeOf(*spt);
                spt->Lun = lun;
                spt->CdbLength = 10;
                spt->SenseInfoLength = 18;
                spt->DataIn = dirIn;
                spt->DataTransferLength = (uint)bytes;
                spt->TimeOutValue = _TimeOut;
                spt->SenseInfoOffset = (uint)Marshal.SizeOf(*spt) + 4;
                spt->DataBufferOffset = (uint)Marshal.SizeOf(sptB);

                cdb->Cmd = cmd;
                cdb->OpCode = op;
                cdb->LBA = (uint)lba;
                cdb->Bank = (byte)bank;
                Util.ReverseBytes((byte*)&(cdb->LBA), 4);

                cdb->Blocks = (ushort)(bytes / _BlockSize);
                Util.ReverseBytes((byte*)&(cdb->Blocks), 2);

                if ((dirIn == 0) && (data != null))
                    Marshal.Copy(data, 0, (IntPtr)(buf + Marshal.SizeOf(sptB)), bytes);

                int[] BytesXfered = new int[1];
                BytesXfered[0] = 0;
                fixed (byte* lpInBuffer = buffer)
                {
                    fixed (byte* lpOutBuffer = buffer)
                    {
                        fixed (int* lpBytesXferred = BytesXfered)
                        {
                            bRetVal = PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_SCSI_PASS_THROUGH,
                                (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len, (IntPtr)lpBytesXferred, (IntPtr)null);
                        }
                    }
                }
                int error = Marshal.GetLastWin32Error();

                if (dirIn == 1)
                    Marshal.Copy((IntPtr)(buf + Marshal.SizeOf(sptB)), data, 0, bytes);

                return bRetVal;
            }
        }
        private unsafe bool SendScsiCmd32(byte cmd, byte op, byte lun, byte dirIn, int bank, int lba, int bytes, byte[] data)
        {
            SCSI_PASS_THROUGH_WITH_BUFFERS32 sptB = new SCSI_PASS_THROUGH_WITH_BUFFERS32();
            int len = Marshal.SizeOf(sptB) + bytes;		// total size of buffer
            byte[] buffer = new byte[len];


            fixed (byte* buf = buffer)
            {
                SCSI_PASS_THROUGH_WITH_BUFFERS32* sptBuf = (SCSI_PASS_THROUGH_WITH_BUFFERS32*)buf;
                SCSI_PASS_THROUGH32* spt = (SCSI_PASS_THROUGH32*)buf;
                CDB10* cdb = (CDB10*)&(spt->Cdb);
                bool bRetVal;

                sptBuf->totalSize = (uint)len;

                spt->Length = (ushort)Marshal.SizeOf(*spt);
                spt->Lun = lun;
                spt->CdbLength = 10;
                spt->SenseInfoLength = 18;
                spt->DataIn = dirIn;
                spt->DataTransferLength = (uint)bytes;
                spt->TimeOutValue = _TimeOut;
                spt->SenseInfoOffset = (uint)Marshal.SizeOf(*spt) + 4;
                spt->DataBufferOffset = (uint)Marshal.SizeOf(sptB);

                cdb->Cmd = cmd;
                cdb->OpCode = op;
                cdb->LBA = (uint)lba;
                cdb->Bank = (byte)bank;
                Util.ReverseBytes((byte*)&(cdb->LBA), 4);

                cdb->Blocks = (ushort)(bytes / _BlockSize);
                Util.ReverseBytes((byte*)&(cdb->Blocks), 2);

                if ((dirIn == 0) && (data != null))
                    Marshal.Copy(data, 0, (IntPtr)(buf + Marshal.SizeOf(sptB)), bytes);

                int[] BytesXfered = new int[1];
                BytesXfered[0] = 0;
                fixed (byte* lpInBuffer = buffer)
                {
                    fixed (byte* lpOutBuffer = buffer)
                    {
                        fixed (int* lpBytesXferred = BytesXfered)
                        {
                            bRetVal = PInvoke.DeviceIoControl(_hDevice, CyConst.IOCTL_SCSI_PASS_THROUGH,
                                (IntPtr)lpInBuffer, len, (IntPtr)lpOutBuffer, len, (IntPtr)lpBytesXferred, (IntPtr)null);
                        }
                    }
                }
                int error = Marshal.GetLastWin32Error();

                if (dirIn == 1)
                    Marshal.Copy((IntPtr)(buf + Marshal.SizeOf(sptB)), data, 0, bytes);

                return bRetVal;
            }
        }
        public unsafe bool SendScsiCmd(byte cmd, byte op, byte lun, byte dirIn, int bank, int lba, int bytes, byte[] data)
        {
            if (IntPtr.Size == 8)
                return SendScsiCmd64(cmd, op, lun, dirIn, bank, lba, bytes, data); //64bit process
            else
                return SendScsiCmd32(cmd, op, lun, dirIn, bank, lba, bytes, data); //32 bit process
        }

    }
}
