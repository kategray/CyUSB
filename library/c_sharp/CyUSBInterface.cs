/*
 ## Cypress CyUSB C# library source file (CyUSBInterface.cs)
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
using System.Windows.Forms;
using System.Text;

namespace CyUSB
{
    public class CyUSBInterfaceContainer
    {
        public CyUSBInterface[] Interfaces;

        byte _bInterfaceNumber;
        public byte bInterfaceNumber
        {
            get { return _bInterfaceNumber; }
        }

        byte _AltInterfacesCount;
        public byte AltInterfacesCount
        {
            get { return _AltInterfacesCount; }
        }

        public CyUSBInterfaceContainer(byte intfcNum, byte altIntfcCount)
        {
            _bInterfaceNumber = intfcNum;
            _AltInterfacesCount = altIntfcCount;
            Interfaces = new CyUSBInterface[altIntfcCount];
        }

        public TreeNode Tree
        {
            get
            {
                string itmp = "Interface " + bInterfaceNumber.ToString();
                TreeNode[] altTree = new TreeNode[AltInterfacesCount];
                for (int i = 0; i < AltInterfacesCount; i++)
                {
                    altTree[i] = Interfaces[i].Tree;
                }
                TreeNode iNode = new TreeNode(itmp, altTree);
                iNode.Tag = this;

                return iNode;
            }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t<INTERFACE " + bInterfaceNumber.ToString() + ">\r\n");

            for (int i = 0; i < AltInterfacesCount; i++)
                s.Append(Interfaces[i].ToString());

            s.Append("\t<INTERFACE " + bInterfaceNumber.ToString() + ">\r\n");
            return s.ToString();
        }

    }

    /// <summary>
    /// The CyUSBInterface Class
    /// </summary>
    public class CyUSBInterface
    {
        public CyUSBEndPoint[] EndPoints;  // Holds pointers to all the interface's endpoints, plus a pointer to the Control endpoint zero

        byte _bLength;
        public byte bLength
        {
            get { return _bLength; }
        }

        byte _bDescriptorType;
        public byte bDescriptorType
        {
            get { return _bDescriptorType; }
        }

        byte _bInterfaceNumber;
        public byte bInterfaceNumber
        {
            get { return _bInterfaceNumber; }
        }

        byte _bAlternateSetting;
        public byte bAlternateSetting
        {
            get { return _bAlternateSetting; }
        }

        byte _bNumEndpoints;           // Not counting the control endpoint
        public byte bNumEndpoints
        {
            get { return _bNumEndpoints; }
        }

        byte _bInterfaceClass;
        public byte bInterfaceClass
        {
            get { return _bInterfaceClass; }
        }

        byte _bInterfaceSubClass;
        public byte bInterfaceSubClass
        {
            get { return _bInterfaceSubClass; }
        }

        byte _bInterfaceProtocol;
        public byte bInterfaceProtocol
        {
            get { return _bInterfaceProtocol; }
        }

        byte _iInterface;
        public byte iInterface
        {
            get { return _iInterface; }
        }

        internal byte _bAltSettings;
        public byte bAltSettings
        {
            get { return _bAltSettings; }
        }

        ushort _wTotalLength;          // Needed in case Intfc has additional (non-endpt) descriptors
        public ushort wTotalLength
        {
            get { return _wTotalLength; }
        }

        unsafe internal CyUSBInterface(IntPtr handle, byte* DescrData, CyControlEndPoint ctlEndPt)
        {

            USB_INTERFACE_DESCRIPTOR* pIntfcDescriptor = (USB_INTERFACE_DESCRIPTOR*)DescrData;


            _bLength = pIntfcDescriptor->bLength;
            _bDescriptorType = pIntfcDescriptor->bDescriptorType;
            _bInterfaceNumber = pIntfcDescriptor->bInterfaceNumber;
            _bAlternateSetting = pIntfcDescriptor->bAlternateSetting;
            _bNumEndpoints = pIntfcDescriptor->bNumEndpoints;
            _bInterfaceClass = pIntfcDescriptor->bInterfaceClass;
            _bInterfaceSubClass = pIntfcDescriptor->bInterfaceSubClass;
            _bInterfaceProtocol = pIntfcDescriptor->bInterfaceProtocol;
            _iInterface = pIntfcDescriptor->iInterface;

            _bAltSettings = 0;
            _wTotalLength = bLength;

            byte* desc = (byte*)(DescrData + pIntfcDescriptor->bLength);

            int i;
            int unexpected = 0;

            EndPoints = new CyUSBEndPoint[bNumEndpoints + 1];
            EndPoints[0] = ctlEndPt;

            for (i = 1; i <= bNumEndpoints; i++)
            {

                USB_ENDPOINT_DESCRIPTOR* endPtDesc = (USB_ENDPOINT_DESCRIPTOR*)desc;
                _wTotalLength += endPtDesc->bLength;


                if (endPtDesc->bDescriptorType == CyConst.USB_ENDPOINT_DESCRIPTOR_TYPE)
                {
                    switch (endPtDesc->bmAttributes)
                    {
                        case 0:
                            EndPoints[i] = ctlEndPt;
                            break;
                        case 1:
                            EndPoints[i] = new CyIsocEndPoint(handle, endPtDesc);
                            break;
                        case 2:
                            EndPoints[i] = new CyBulkEndPoint(handle, endPtDesc);
                            break;
                        case 3:
                            EndPoints[i] = new CyInterruptEndPoint(handle, endPtDesc);
                            break;
                    }

                    desc += endPtDesc->bLength;
                }
                else
                {
                    unexpected++;
                    if (unexpected < 12)
                    {  // Sanity check - prevent infinite loop

                        // This may have been a class-specific descriptor (like HID).  Skip it.
                        desc += endPtDesc->bLength;

                        // Stay in the loop, grabbing the next descriptor
                        i--;
                    }

                }

            }
        }
        unsafe internal CyUSBInterface(IntPtr handle, byte* DescrData, CyControlEndPoint ctlEndPt, byte usb30dummy)
        {

            USB_INTERFACE_DESCRIPTOR* pIntfcDescriptor = (USB_INTERFACE_DESCRIPTOR*)DescrData;


            _bLength = pIntfcDescriptor->bLength;
            _bDescriptorType = pIntfcDescriptor->bDescriptorType;
            _bInterfaceNumber = pIntfcDescriptor->bInterfaceNumber;
            _bAlternateSetting = pIntfcDescriptor->bAlternateSetting;
            _bNumEndpoints = pIntfcDescriptor->bNumEndpoints;
            _bInterfaceClass = pIntfcDescriptor->bInterfaceClass;
            _bInterfaceSubClass = pIntfcDescriptor->bInterfaceSubClass;
            _bInterfaceProtocol = pIntfcDescriptor->bInterfaceProtocol;
            _iInterface = pIntfcDescriptor->iInterface;

            _bAltSettings = 0;
            _wTotalLength = bLength;

            byte* desc = (byte*)(DescrData + pIntfcDescriptor->bLength);

            int i;
            int unexpected = 0;

            EndPoints = new CyUSBEndPoint[bNumEndpoints + 1];
            EndPoints[0] = ctlEndPt;

            for (i = 1; i <= bNumEndpoints; i++)
            {

                bool bSSDec = false;
                USB_ENDPOINT_DESCRIPTOR* endPtDesc = (USB_ENDPOINT_DESCRIPTOR*)desc;
                desc += endPtDesc->bLength;
                USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR* ssendPtDesc = (USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR*)desc;
                _wTotalLength += endPtDesc->bLength;

                if (ssendPtDesc != null)
                    bSSDec = (ssendPtDesc->bDescriptorType == CyConst.USB_SUPERSPEED_ENDPOINT_COMPANION);


                if ((endPtDesc->bDescriptorType == CyConst.USB_ENDPOINT_DESCRIPTOR_TYPE) && bSSDec)
                {
                    switch (endPtDesc->bmAttributes)
                    {
                        case 0:
                            EndPoints[i] = ctlEndPt;
                            break;
                        case 1:
                            EndPoints[i] = new CyIsocEndPoint(handle, endPtDesc, ssendPtDesc);
                            break;
                        case 2:
                            EndPoints[i] = new CyBulkEndPoint(handle, endPtDesc, ssendPtDesc);
                            break;
                        case 3:
                            EndPoints[i] = new CyInterruptEndPoint(handle, endPtDesc, ssendPtDesc);
                            break;
                    }
                    _wTotalLength += ssendPtDesc->bLength;
                    desc += ssendPtDesc->bLength;
                }
                else if ((endPtDesc->bDescriptorType == CyConst.USB_ENDPOINT_DESCRIPTOR_TYPE))
                {
                    switch (endPtDesc->bmAttributes)
                    {
                        case 0:
                            EndPoints[i] = ctlEndPt;
                            break;
                        case 1:
                            EndPoints[i] = new CyIsocEndPoint(handle, endPtDesc);
                            break;
                        case 2:
                            EndPoints[i] = new CyBulkEndPoint(handle, endPtDesc);
                            break;
                        case 3:
                            EndPoints[i] = new CyInterruptEndPoint(handle, endPtDesc);
                            break;
                    }
                }
                else
                {
                    unexpected++;
                    if (unexpected < 12)
                    {  // Sanity check - prevent infinite loop

                        // This may have been a class-specific descriptor (like HID).  Skip it.
                        desc += endPtDesc->bLength;

                        // Stay in the loop, grabbing the next descriptor
                        i--;
                    }

                }

            }
        }

        public TreeNode Tree
        {
            get
            {
                string tmp = "Alternate Setting " + bAlternateSetting.ToString();

                //string tmp = "Interface " + bInterfaceNumber.ToString();

                TreeNode[] eTree = new TreeNode[_bNumEndpoints];
                for (int i = 0; i < _bNumEndpoints; i++)
                    eTree[i] = EndPoints[i + 1].Tree;

                TreeNode t = new TreeNode(tmp, eTree);
                t.Tag = this;

                return t;
            }

        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t\t<INTERFACE>\r\n");

            s.Append(string.Format("\t\t\tInterface=\"{0}\"\r\n", _iInterface));
            s.Append(string.Format("\t\t\tInterfaceNumber=\"{0}\"\r\n", _bInterfaceNumber));
            s.Append(string.Format("\t\t\tAltSetting=\"{0}\"\r\n", _bAlternateSetting));
            s.Append(string.Format("\t\t\tClass=\"{0:X2}h\"\r\n", _bInterfaceClass));
            s.Append(string.Format("\t\t\tSubclass=\"{0:X2}h\"\r\n", _bInterfaceSubClass));
            s.Append(string.Format("\t\t\tProtocol=\"{0}\"\r\n", _bInterfaceProtocol));
            s.Append(string.Format("\t\t\tEndpoints=\"{0}\"\r\n", _bNumEndpoints));
            s.Append(string.Format("\t\t\tDescriptorType=\"{0}\"\r\n", _bDescriptorType));
            s.Append(string.Format("\t\t\tDescriptorLength=\"{0}\"\r\n", _bLength));

            for (int i = 0; i < _bNumEndpoints; i++)
                s.Append(EndPoints[i + 1].ToString());

            s.Append("\t\t</INTERFACE>\r\n");
            return s.ToString();
        }


    }
}
