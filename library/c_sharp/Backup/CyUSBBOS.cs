/*
 ## Cypress CyUSB C# library source file (CyUSBBOS.cs)
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
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CyUSB
{
    public class CyUSBBOS
    {
        public CyBOS_USB20_DEVICE_EXT USB20_DeviceExt;
        public CyBOS_SS_DEVICE_CAPABILITY SS_DeviceCap;
        public CyBOS_CONTAINER_ID Container_ID;
        private InvalidDeviceCapability InvalidDevCap;

        protected byte _bLength;/* Descriptor length*/
        public byte Lenght { get { return _bLength; } }

        protected byte _bDescriptorType;/* Descriptor Type */
        public byte DescriptorType { get { return _bDescriptorType; } }

        protected ushort _wToatalLength;/* Total length of descriptor ( icluding device capability*/
        public ushort ToatalLength { get { return _wToatalLength; } }

        protected byte _bNumDeviceCaps;/* Number of device capability descriptors in BOS  */
        public byte NumDeviceCaps { get { return _bNumDeviceCaps; } }

        unsafe internal CyUSBBOS(IntPtr handle, byte[] BosDescrData)
        {
            // initialize to null
            USB20_DeviceExt = null;
            SS_DeviceCap = null;
            Container_ID = null;


            // parse the Bos Descriptor data
            fixed (byte* buf = BosDescrData)
            {
                USB_BOS_DESCRIPTOR* BosDesc = (USB_BOS_DESCRIPTOR*)buf;
                _bLength = BosDesc->bLength;
                _bDescriptorType = BosDesc->bDescriptorType;
                _bNumDeviceCaps = BosDesc->bNumDeviceCaps;
                _wToatalLength = BosDesc->wToatalLength;

                int totallen = _wToatalLength;
                totallen -= BosDesc->bLength;

                if (totallen < 0)
                    return;

                byte* DevCap = (byte*)(buf + BosDesc->bLength); // get nex descriptor

                for (int i = 0; i < _bNumDeviceCaps; i++)
                {
                    //check capability type
                    switch (DevCap[2])
                    {
                        case CyConst.USB_BOS_CAPABILITY_TYPE_USB20_EXT:
                            {
                                USB_BOS_USB20_DEVICE_EXTENSION* USB20_ext = (USB_BOS_USB20_DEVICE_EXTENSION*)DevCap;
                                totallen -= USB20_ext->bLength;
                                DevCap = (byte*)DevCap + USB20_ext->bLength;
                                USB20_DeviceExt = new CyBOS_USB20_DEVICE_EXT(handle, USB20_ext);
                                break;
                            }
                        case CyConst.USB_BOS_CAPABILITY_TYPE_SUPERSPEED_USB:
                            {
                                USB_BOS_SS_DEVICE_CAPABILITY* SS_Capability = (USB_BOS_SS_DEVICE_CAPABILITY*)DevCap;
                                totallen -= SS_Capability->bLength;
                                DevCap = (byte*)DevCap + SS_Capability->bLength;
                                SS_DeviceCap = new CyBOS_SS_DEVICE_CAPABILITY(handle, SS_Capability);
                                break;
                            }
                        case CyConst.USB_BOS_CAPABILITY_TYPE_CONTAINER_ID:
                            {
                                USB_BOS_CONTAINER_ID* USB_ContainerID = (USB_BOS_CONTAINER_ID*)DevCap;
                                totallen -= USB_ContainerID->bLength;
                                DevCap = (byte*)DevCap + USB_ContainerID->bLength;
                                Container_ID = new CyBOS_CONTAINER_ID(handle, USB_ContainerID);
                                break;
                            }
                        default:
                            {
                                InvalidDevCap = new InvalidDeviceCapability();
                                break;
                            }
                    }
                    if (totallen < 0)
                        break;
                }

            }
        }

        public TreeNode Tree
        {
            get
            {
                string tmp = "BOS";

                TreeNode[] iTree = new TreeNode[NumDeviceCaps];

                for (int i = 0; i < NumDeviceCaps; i++)
                {
                    if ((USB20_DeviceExt != null) && (i == 0))
                        iTree[i] = USB20_DeviceExt.Tree;
                    else if ((SS_DeviceCap != null) && (i == 1))
                        iTree[i] = SS_DeviceCap.Tree;
                    else if ((Container_ID != null) && (i == 2))
                        iTree[i] = Container_ID.Tree;
                    else
                    {
                        iTree[i] = InvalidDevCap.Tree;
                    }
                }

                TreeNode t = new TreeNode(tmp, iTree);
                t.Tag = this;

                return t;
            }

        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t<BOS>\r\n");

            s.Append(string.Format("\t\tNumberOfDeviceCapability=\"{0:X2}h\"\r\n", _bNumDeviceCaps));
            s.Append(string.Format("\t\tDescriptorType=\"{0}\"\r\n", _bDescriptorType));
            s.Append(string.Format("\t\tDescriptorLength=\"{0}\"\r\n", _bLength));
            s.Append(string.Format("\t\tTotalLength=\"{0}\"\r\n", _wToatalLength));
            for (int i = 0; i < NumDeviceCaps; i++)
            {
                if ((USB20_DeviceExt != null) && (i == 0))
                    s.Append(USB20_DeviceExt.ToString());
                else if ((SS_DeviceCap != null) && (i == 1))
                    s.Append(SS_DeviceCap.ToString());
                else if ((Container_ID != null) && (i == 2))
                    s.Append(Container_ID.ToString());
                else
                    s.Append(InvalidDevCap.ToString());

            }
            s.Append("\t</BOS>\r\n");
            return s.ToString();
        }
    }

    // This class defined to handle invalid BOS descriptor table configuration
    public class InvalidDeviceCapability
    {
        public TreeNode Tree
        {
            get
            {
                string tmp = "Invalid Device Capability";
                TreeNode t = new TreeNode(tmp);
                t.Tag = this;
                return t;
            }
        }
        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t\t<Please correct your BOS descriptor table in firmware>\r\n");
            return s.ToString();
        }
    }

    public class CyBOS_USB20_DEVICE_EXT
    {

        protected byte _bLength;/* Descriptor length*/
        public byte Lenght { get { return _bLength; } }

        protected byte _bDescriptorType;/* Descriptor Type */
        public byte DescriptorType { get { return _bDescriptorType; } }

        protected byte _bDevCapabilityType;/* Device capability type*/
        public byte DevCapabilityType { get { return _bDevCapabilityType; } }

        protected uint _bmAttribute;// Bitmap encoding for supprted feature and  Link power managment supprted if set
        public uint bmAttribute { get { return _bmAttribute; } }

        unsafe internal CyBOS_USB20_DEVICE_EXT(IntPtr handle, USB_BOS_USB20_DEVICE_EXTENSION* USB20_DeviceExt)
        {
            _bLength = USB20_DeviceExt->bLength;
            _bDescriptorType = USB20_DeviceExt->bDescriptorType;
            _bDevCapabilityType = USB20_DeviceExt->bDevCapabilityType;
            _bmAttribute = USB20_DeviceExt->bmAttribute;
        }

        public TreeNode Tree
        {
            get
            {
                string tmp = "USB20 Device Extension";
                TreeNode t = new TreeNode(tmp);
                t.Tag = this;
                return t;
            }

        }
        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t\t<USB20 Device Extension>\r\n");

            s.Append(string.Format("\t\t\tDescriptorLength=\"{0}\"\r\n", _bLength));
            s.Append(string.Format("\t\t\tDescriptorType=\"{0}\"\r\n", _bDescriptorType));
            s.Append(string.Format("\t\t\tDeviceCapabilityType=\"{0}\"\r\n", _bDevCapabilityType));
            s.Append(string.Format("\t\t\tbmAttribute=\"{0:X2}h\"\r\n", _bmAttribute));
            s.Append("\t\t</USB20 Device Extension>\r\n");
            return s.ToString();
        }
    }

    public class CyBOS_SS_DEVICE_CAPABILITY
    {
        protected byte _bLength;/* Descriptor length*/
        public byte Lenght { get { return _bLength; } }

        protected byte _bDescriptorType;/* Descriptor Type */
        public byte DescriptorType { get { return _bDescriptorType; } }

        protected byte _bDevCapabilityType;/* Device capability type*/
        public byte DevCapabilityType { get { return _bDevCapabilityType; } }

        protected byte _bmAttribute;// Bitmap encoding for supprted feature and  Link power managment supprted if set
        public byte bmAttribute { get { return _bmAttribute; } }

        protected ushort _wSpeedsSuported;//low speed supported if set,full speed supported if set,high speed supported if set,super speed supported if set,15:4 nt used
        public ushort SpeedsSuported { get { return _wSpeedsSuported; } }

        protected byte _bFunctionalitySupporte;
        public byte FunctionalitySupporte { get { return _bFunctionalitySupporte; } }

        protected byte _bU1DevExitLat;//U1 device exit latency
        public byte U1DevExitLat { get { return _bU1DevExitLat; } }

        protected ushort _bU2DevExitLat;//U2 device exit latency
        public ushort U2DevExitLat { get { return _bU2DevExitLat; } }

        unsafe internal CyBOS_SS_DEVICE_CAPABILITY(IntPtr handle, USB_BOS_SS_DEVICE_CAPABILITY* USB_SuperSpeedUsb)
        {
            _bLength = USB_SuperSpeedUsb->bLength;
            _bDescriptorType = USB_SuperSpeedUsb->bDescriptorType;
            _bDevCapabilityType = USB_SuperSpeedUsb->bDevCapabilityType;
            _bFunctionalitySupporte = USB_SuperSpeedUsb->bFunctionalitySupporte;
            _bmAttribute = USB_SuperSpeedUsb->bmAttribute;
            _bU1DevExitLat = USB_SuperSpeedUsb->bU1DevExitLat;
            _bU2DevExitLat = USB_SuperSpeedUsb->bU2DevExitLat;
        }
        public TreeNode Tree
        {
            get
            {
                string tmp = "SuperSpeed Device capability";
                TreeNode t = new TreeNode(tmp);
                t.Tag = this;
                return t;
            }

        }
        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t\t<SUPERSPEED USB>\r\n");

            s.Append(string.Format("\t\t\tDescriptorLength=\"{0}\"\r\n", _bLength));
            s.Append(string.Format("\t\t\tDescriptorType=\"{0}\"\r\n", _bDescriptorType));
            s.Append(string.Format("\t\t\tDeviceCapabilityType=\"{0}\"\r\n", _bDevCapabilityType));
            s.Append(string.Format("\t\t\tFunctionalitySupporte=\"{0}\"\r\n", _bFunctionalitySupporte));
            s.Append(string.Format("\t\t\tbmAttribute=\"{0:X2}h\"\r\n", _bmAttribute));
            s.Append(string.Format("\t\t\tU1Device Exit Latency=\"{0}\"\r\n", _bU1DevExitLat));
            s.Append(string.Format("\t\t\tU2Device Exit Latency=\"{0:X2}h\"\r\n", _bU2DevExitLat));
            s.Append("\t\t</SUPERSPEED USB>\r\n");
            return s.ToString();
        }
    }

    public class CyBOS_CONTAINER_ID
    {
        protected byte _bLength;/* Descriptor length*/
        public byte Lenght { get { return _bLength; } }

        protected byte _bDescriptorType;/* Descriptor Type */
        public byte DescriptorType { get { return _bDescriptorType; } }

        protected byte _bDevCapabilityType;/* Device capability type*/
        public byte DevCapabilityType { get { return _bDevCapabilityType; } }

        protected byte _bResrved; // no use
        public byte Reserved { get { return _bResrved; } }

        protected byte[] _ContainerID;/* UUID */
        public byte[] ContainerID { get { return _ContainerID; } }

        unsafe internal CyBOS_CONTAINER_ID(IntPtr handle, USB_BOS_CONTAINER_ID* USB_ContainerID)
        {
            _bLength = USB_ContainerID->bLength;
            _bDescriptorType = USB_ContainerID->bDescriptorType;
            _bDevCapabilityType = USB_ContainerID->bDevCapabilityType;
            _ContainerID = new byte[CyConst.USB_BOS_CAPABILITY_TYPE_CONTAINER_ID_SIZE];
            for (int i = 0; i < CyConst.USB_BOS_CAPABILITY_TYPE_CONTAINER_ID_SIZE; i++)
                _ContainerID[i] = USB_ContainerID->ContainerID[i];

        }
        public TreeNode Tree
        {
            get
            {
                string tmp = "Container ID";
                TreeNode t = new TreeNode(tmp);
                t.Tag = this;
                return t;
            }

        }
        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t\t<CONTAINER ID>\r\n");

            s.Append(string.Format("\t\t\tDescriptorLength=\"{0}\"\r\n", _bLength));
            s.Append(string.Format("\t\t\tDescriptorType=\"{0}\"\r\n", _bDescriptorType));
            s.Append(string.Format("\t\t\tDeviceCapabilityType=\"{0}\"\r\n", _bDevCapabilityType));
            //s.Append(string.Format("\t\tbmAttribute=\"{0:X2}h\"\r\n", _ContainerID.));            
            s.Append("\t\t</CONTAINER ID>\r\n");
            return s.ToString();
        }
    }
}
