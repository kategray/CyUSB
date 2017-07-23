/*
 ## Cypress CyUSB C# library source file (CyHidDevice.cs)
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
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CyUSB
{
    /// <summary>
    /// Summary description for CyHidDevice.
    /// </summary>
    public unsafe class CyHidDevice : USBDevice
    {
        byte* PreParsedData;
        internal HIDD_ATTRIBUTES Attributes;

        HIDP_CAPS _Capabilities;
        public HIDP_CAPS Capabilities
        {
            get { return _Capabilities; }
        }


        ushort _Usage;
        public ushort Usage
        {
            get { return _Usage; }
        }

        ushort _UsagePage;
        public ushort UsagePage
        {
            get { return _UsagePage; }
        }

        ushort _Version;
        public ushort Version
        {
            get { return _Version; }
        }

        CyHidReport _Inputs;
        public CyHidReport Inputs
        {
            get { return _Inputs; }
        }

        CyHidReport _Outputs;
        public CyHidReport Outputs
        {
            get { return _Outputs; }
        }

        CyHidReport _Features;
        public CyHidReport Features
        {
            get { return _Features; }
        }

        internal CyHidDevice(Guid g)
            : base(g)
        {
        }

        internal static Guid Guid
        {
            get
            {
                Guid hG = Guid.Empty;
                PInvoke.HidD_GetHidGuid(ref hG);
                return hG;
            }
        }

        public override TreeNode Tree
        {
            get
            {
                int nodes = 0;
                if (Features.NumItems > 0) nodes++;
                if (Inputs.NumItems > 0) nodes++;
                if (Outputs.NumItems > 0) nodes++;

                int n = 0;

                TreeNode[] hidTree = new TreeNode[nodes];
                if (Features.NumItems > 0)
                    hidTree[n++] = Features.Tree;
                if (Inputs.NumItems > 0)
                    hidTree[n++] = Inputs.Tree;
                if (Outputs.NumItems > 0)
                    hidTree[n++] = Outputs.Tree;

                TreeNode t = new TreeNode(Product, hidTree);
                t.Tag = this;

                return t;
            }
        }


        public override string ToString()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            StringBuilder s = new StringBuilder("<HID_DEVICE>\r\n");

            s.Append(string.Format("\tFriendlyName=\"{0}\"\r\n", FriendlyName));
            s.Append(string.Format("\tManufacturer=\"{0}\"\r\n", Manufacturer));
            s.Append(string.Format("\tProduct=\"{0}\"\r\n", Product));
            s.Append(string.Format("\tSerialNumber=\"{0}\"\r\n", SerialNumber));
            //s.Append(string.Format("\tVendorID=\"{0:X4}\"\r\n", VendorID));
            s.Append(string.Format("\tVendorID=\"{0}\"\r\n", Util.byteStr(VendorID)));
            s.Append(string.Format("\tProductID=\"{0}\"\r\n", Util.byteStr(ProductID)));
            s.Append(string.Format("\tClass=\"{0:X2}h\"\r\n", _devClass));
            s.Append(string.Format("\tSubClass=\"{0:X2}h\"\r\n", _devSubClass));
            s.Append(string.Format("\tProtocol=\"{0:X2}h\"\r\n", _devProtocol));
            s.Append(string.Format("\tBcdUSB=\"{0}\"\r\n", Util.byteStr(_bcdUSB)));
            s.Append(string.Format("\tUsage=\"{0}\"\r\n", Util.byteStr(_Usage)));
            s.Append(string.Format("\tUsagePage=\"{0}\"\r\n", Util.byteStr(_UsagePage)));
            s.Append(string.Format("\tVersion=\"{0}\"\r\n", Util.byteStr(_Version)));

            if (_Features.NumItems > 0)
                s.Append(_Features.ToString());

            if (_Inputs.NumItems > 0)
                s.Append(_Inputs.ToString());

            if (_Outputs.NumItems > 0)
                s.Append(_Outputs.ToString());

            s.Append("</HID_DEVICE>\r\n");
            return s.ToString();
        }

        private int _Access = 0;
        public bool RwAccessible
        {
            get { return (_Access > 0); }
        }

        // Opens a handle to the devTH device attached the HIDUSB.SYS driver
        internal unsafe override bool Open(byte dev)
        {

            // If this object already has the driver open, close it.
            if (_hDevice != CyConst.INVALID_HANDLE)
                Close();

            int Devices = DeviceCount;
            if (Devices == 0) return false;
            if (dev > (Devices - 1)) return false;

            string pathDetect;
            _path = PInvoke.GetDevicePath(_drvGuid, dev);
            pathDetect = _path;
            if (pathDetect.Contains("&mi_00#") == true)
                return false;
            _hDevice = PInvoke.GetDeviceHandle(_path, false, ref _Access);
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            _devNum = dev;

            PInvoke.HidD_GetPreparsedData(_hDevice, ref PreParsedData);
            PInvoke.HidD_GetAttributes(_hDevice, ref Attributes);
            PInvoke.HidP_GetCaps(PreParsedData, ref _Capabilities);

            _Inputs = new CyHidReport(HIDP_REPORT_TYPE.HidP_Input, _Capabilities, PreParsedData);
            _Outputs = new CyHidReport(HIDP_REPORT_TYPE.HidP_Output, _Capabilities, PreParsedData);
            _Features = new CyHidReport(HIDP_REPORT_TYPE.HidP_Feature, _Capabilities, PreParsedData);

            PInvoke.HidD_FreePreparsedData(PreParsedData);
            PreParsedData = null;

            byte[] buffer = new byte[512];

            fixed (byte* buf = buffer)
            {
                char* sChars = (char*)buf;

                if (PInvoke.HidD_GetManufacturerString(_hDevice, buffer, 512))
                    _manufacturer = new string(sChars);

                if (PInvoke.HidD_GetProductString(_hDevice, buffer, 512))
                    _product = new string(sChars);

                if (PInvoke.HidD_GetSerialNumberString(_hDevice, buffer, 512))
                    _serialNumber = new string(sChars);

            }

            // Shortcut members.
            _vendorID = Attributes.VendorID;
            _productID = Attributes.ProductID;
            _Version = Attributes.VersionNumber;
            _Usage = _Capabilities.Usage;
            _UsagePage = _Capabilities.UsagePage;

            _driverName = "usbhid.sys";

            return true;
        }


        public bool GetFeature(int rptID)
        {
            if (_Features.RptByteLen == 0) return false;
            //if (!RwAccessible) return false;

            _Features.Clear();
            _Features.DataBuf[0] = (byte)rptID;

            fixed (byte* buf = _Features.DataBuf)
            {
                return PInvoke.HidD_GetFeature(_hDevice, _Features.DataBuf, _Features.RptByteLen);
            }
            //return PInvoke.HidD_GetFeature(_hDevice, ref _Features.DataBuf[0], _Features.RptByteLen);
        }


        public bool SetFeature(int rptID)
        {
            if (_Features.RptByteLen == 0) return false;
            //if (!RwAccessible) return false;

            _Features.DataBuf[0] = (byte)rptID;

            fixed (byte* buf = _Features.DataBuf)
            {
                return PInvoke.HidD_SetFeature(_hDevice, _Features.DataBuf, _Features.RptByteLen);
            }
            //return PInvoke.HidD_SetFeature(_hDevice, ref _Features.DataBuf[0], _Features.RptByteLen);

        }


        public bool GetInput(int rptID)
        {
            if (_Inputs.RptByteLen == 0) return false;
            if (!RwAccessible) return false;

            _Inputs.Clear();
            _Inputs.DataBuf[0] = (byte)rptID;

            // ReadFile will hang if the device does not have an input report ready.
            //int bytesRead = 0;
            //return PInvoke.ReadFile(_hDevice, ref _Inputs.DataBuf[0], _Inputs.RptByteLen, ref bytesRead, null);

            // GetInputReport always returns right away
            fixed (byte* buf = _Inputs.DataBuf)
            {
                return PInvoke.HidD_GetInputReport(_hDevice, _Inputs.DataBuf, _Inputs.RptByteLen);
            }
            //return PInvoke.HidD_GetInputReport(_hDevice, ref _Inputs.DataBuf[0], _Inputs.RptByteLen);
        }



        public bool SetOutput(int rptID)
        {
            if (_Outputs.RptByteLen == 0) return false;
            if (!RwAccessible) return false;

            _Outputs.DataBuf[0] = (byte)rptID;

            fixed (byte* buf = _Outputs.DataBuf)
            {
                return PInvoke.HidD_SetOutputReport(_hDevice, _Outputs.DataBuf, _Outputs.RptByteLen);
            }
            //return PInvoke.HidD_SetOutputReport(_hDevice, ref _Outputs.DataBuf[0], _Outputs.RptByteLen);
        }


        public bool WriteOutput()
        {
            int bytesWritten = 0;

            if (_Outputs.RptByteLen == 0) return false;
            if (!RwAccessible) return false;

            _Outputs.DataBuf[0] = _Outputs.ID;

            fixed (byte* buf = _Outputs.DataBuf)
            {
                return PInvoke.WriteFile(_hDevice, _Outputs.DataBuf, _Outputs.RptByteLen, ref bytesWritten, IntPtr.Zero);
            }
            //return PInvoke.WriteFile(_hDevice, ref _Outputs.DataBuf[0], _Outputs.RptByteLen, ref bytesWritten, IntPtr.Zero);
        }


        public bool ReadInput()
        {
            if (_Inputs.RptByteLen == 0) return false;
            if (!RwAccessible) return false;

            if (CyConst.Hibernate_first_call == true)
            {
                CyConst.Hibernate_first_call = false;
                return false;
            }

            _Inputs.Clear();

            // ReadFile will hang if the device does not have an input report ready.
            int bytesRead = 0;

            fixed (byte* buf = _Inputs.DataBuf)
            {
                return PInvoke.ReadFile(_hDevice, _Inputs.DataBuf, _Inputs.RptByteLen, ref bytesRead, IntPtr.Zero);
            }
            //return PInvoke.ReadFile(_hDevice, ref _Inputs.DataBuf[0], _Inputs.RptByteLen, ref bytesRead, IntPtr.Zero);
        }


    }
}
