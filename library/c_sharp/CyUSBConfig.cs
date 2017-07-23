/*
 ## Cypress CyUSB C# library source file (CyUSBConfig.cs)
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
    /// <summary>
    /// The CyUSBConfig Class
    /// </summary>
    public class CyUSBConfig
    {
        public CyUSBInterface[] Interfaces;

        CyUSBInterfaceContainer[] IntfcContainer;

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

        ushort _wTotalLength;
        public ushort wTotalLength
        {
            get { return _wTotalLength; }
        }

        byte _bNumInterfaces;
        public byte bNumInterfaces
        {
            get { return _bNumInterfaces; }
        }

        byte _bConfigurationValue;
        public byte bConfigurationValue
        {
            get { return _bConfigurationValue; }
        }

        byte _iConfiguration;
        public byte iConfiguration
        {
            get { return _iConfiguration; }
        }

        byte _bmAttributes;
        public byte bmAttributes
        {
            get { return _bmAttributes; }
        }

        byte _MaxPower;
        public byte MaxPower
        {
            get { return _MaxPower; }
        }

        byte _AltInterfaces;
        public byte AltInterfaces
        {
            get { return _AltInterfaces; }
        }

        unsafe internal CyUSBConfig(IntPtr handle, byte[] DescrData, CyControlEndPoint ctlEndPt)
        {// This contructore is to initialize usb2.0 device
            fixed (byte* buf = DescrData)
            {
                USB_CONFIGURATION_DESCRIPTOR* ConfigDescr = (USB_CONFIGURATION_DESCRIPTOR*)buf;

                _bLength = ConfigDescr->bLength;
                _bDescriptorType = ConfigDescr->bDescriptorType;
                _wTotalLength = ConfigDescr->wTotalLength;
                _bNumInterfaces = ConfigDescr->bNumInterfaces;
                _AltInterfaces = 0;
                _bConfigurationValue = ConfigDescr->bConfigurationValue;
                _iConfiguration = ConfigDescr->iConfiguration;
                _bmAttributes = ConfigDescr->bmAttributes;
                _MaxPower = ConfigDescr->MaxPower;

                int tLen = ConfigDescr->wTotalLength;

                byte* desc = (byte*)(buf + ConfigDescr->bLength);
                int bytesConsumed = ConfigDescr->bLength;

                Interfaces = new CyUSBInterface[CyConst.MAX_INTERFACES];

                int i = 0;
                do
                {
                    USB_INTERFACE_DESCRIPTOR* interfaceDesc = (USB_INTERFACE_DESCRIPTOR*)desc;

                    if (interfaceDesc->bDescriptorType == CyConst.USB_INTERFACE_DESCRIPTOR_TYPE)
                    {
                        Interfaces[i] = new CyUSBInterface(handle, desc, ctlEndPt);
                        i++;
                        _AltInterfaces++;  // Actually the total number of interfaces for the config
                        bytesConsumed += Interfaces[i - 1].wTotalLength;
                    }
                    else
                    {
                        // Unexpected descriptor type
                        // Just skip it and go on  - could have thrown an exception instead
                        // since this indicates that the descriptor structure is invalid.
                        bytesConsumed += interfaceDesc->bLength;
                    }


                    desc = (byte*)(buf + bytesConsumed);

                } while ((bytesConsumed < tLen) && (i < CyConst.MAX_INTERFACES));
                // Count the alt interfaces for each interface number
                for (i = 0; i < _AltInterfaces; i++)
                {
                    Interfaces[i]._bAltSettings = 0;

                    for (int j = 0; j < AltInterfaces; j++) // Walk the list looking for identical bInterfaceNumbers
                        if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            Interfaces[i]._bAltSettings++;

                }

                // Create the Interface Container (this is done only for Tree view purpose).
                IntfcContainer = new CyUSBInterfaceContainer[bNumInterfaces];

                Dictionary<int, bool> altDict = new Dictionary<int, bool>();
                int intfcCount = 0;

                for (i = 0; i < _AltInterfaces; i++)
                {
                    if (altDict.ContainsKey(Interfaces[i].bInterfaceNumber) == false)
                    {
                        int altIntfcCount = 0;
                        IntfcContainer[intfcCount] = new CyUSBInterfaceContainer(Interfaces[i].bInterfaceNumber, Interfaces[i].bAltSettings);

                        for (int j = i; j < AltInterfaces; j++)
                        {
                            if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            {
                                IntfcContainer[intfcCount].Interfaces[altIntfcCount] = Interfaces[j];
                                altIntfcCount++;
                            }
                        }
                        intfcCount++;
                        altDict.Add(Interfaces[i].bInterfaceNumber, true);
                    }

                }
            } /* end of fixed loop */

        }
        unsafe internal CyUSBConfig(IntPtr handle, byte[] DescrData, CyControlEndPoint ctlEndPt, byte usb30Dummy)
        {// This constructure will be called for USB3.0 device initialization
            fixed (byte* buf = DescrData)
            {
                USB_CONFIGURATION_DESCRIPTOR* ConfigDescr = (USB_CONFIGURATION_DESCRIPTOR*)buf;

                _bLength = ConfigDescr->bLength;
                _bDescriptorType = ConfigDescr->bDescriptorType;
                _wTotalLength = ConfigDescr->wTotalLength;
                _bNumInterfaces = ConfigDescr->bNumInterfaces;
                _AltInterfaces = 0;
                _bConfigurationValue = ConfigDescr->bConfigurationValue;
                _iConfiguration = ConfigDescr->iConfiguration;
                _bmAttributes = ConfigDescr->bmAttributes;
                _MaxPower = ConfigDescr->MaxPower;

                int tLen = ConfigDescr->wTotalLength;

                byte* desc = (byte*)(buf + ConfigDescr->bLength);
                int bytesConsumed = ConfigDescr->bLength;

                Interfaces = new CyUSBInterface[CyConst.MAX_INTERFACES];

                int i = 0;
                do
                {
                    USB_INTERFACE_DESCRIPTOR* interfaceDesc = (USB_INTERFACE_DESCRIPTOR*)desc;

                    if (interfaceDesc->bDescriptorType == CyConst.USB_INTERFACE_DESCRIPTOR_TYPE)
                    {
                        Interfaces[i] = new CyUSBInterface(handle, desc, ctlEndPt, usb30Dummy);
                        i++;
                        _AltInterfaces++;  // Actually the total number of interfaces for the config
                        bytesConsumed += Interfaces[i - 1].wTotalLength;
                    }
                    else
                    {
                        // Unexpected descriptor type
                        // Just skip it and go on  - could have thrown an exception instead
                        // since this indicates that the descriptor structure is invalid.
                        bytesConsumed += interfaceDesc->bLength;
                    }


                    desc = (byte*)(buf + bytesConsumed);

                } while ((bytesConsumed < tLen) && (i < CyConst.MAX_INTERFACES));
                // Count the alt interfaces for each interface number
                for (i = 0; i < _AltInterfaces; i++)
                {
                    Interfaces[i]._bAltSettings = 0;

                    for (int j = 0; j < AltInterfaces; j++) // Walk the list looking for identical bInterfaceNumbers
                        if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            Interfaces[i]._bAltSettings++;

                }

                // Create the Interface Container (this is done only for Tree view purpose).
                IntfcContainer = new CyUSBInterfaceContainer[bNumInterfaces];

                Dictionary<int, bool> altDict = new Dictionary<int, bool>();
                int intfcCount = 0;

                for (i = 0; i < _AltInterfaces; i++)
                {
                    if (altDict.ContainsKey(Interfaces[i].bInterfaceNumber) == false)
                    {
                        int altIntfcCount = 0;
                        IntfcContainer[intfcCount] = new CyUSBInterfaceContainer(Interfaces[i].bInterfaceNumber, Interfaces[i].bAltSettings);

                        for (int j = i; j < AltInterfaces; j++)
                        {
                            if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            {
                                IntfcContainer[intfcCount].Interfaces[altIntfcCount] = Interfaces[j];
                                altIntfcCount++;
                            }
                        }
                        intfcCount++;
                        altDict.Add(Interfaces[i].bInterfaceNumber, true);
                    }

                }
            } /* end of fixed loop */

        }

        public TreeNode Tree
        {
            get
            {
                string tmp = "Configuration " + bConfigurationValue.ToString();
                //string tmp = "Primary Configuration";
                //if (iConfiguration == 1)
                //    tmp = "Secondary Configuration";

                //TreeNode[] iTree = new TreeNode[_AltInterfaces + 1];
                TreeNode[] iTree = new TreeNode[bNumInterfaces + 1];

                iTree[0] = new TreeNode("Control endpoint (0x00)");
                iTree[0].Tag = Interfaces[0].EndPoints[0];

                for (int i = 0; i < bNumInterfaces; i++)
                    iTree[i + 1] = IntfcContainer[i].Tree;

                //for (int i = 0; i < _AltInterfaces; i++)
                //    iTree[i + 1] = Interfaces[i].Tree;

                TreeNode t = new TreeNode(tmp, iTree);
                t.Tag = this;

                return t;
            }

        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder("\t<CONFIGURATION>\r\n");

            s.Append(string.Format("\t\tConfiguration=\"{0}\"\r\n", _iConfiguration));
            s.Append(string.Format("\t\tConfigurationValue=\"{0}\"\r\n", _bConfigurationValue));
            s.Append(string.Format("\t\tAttributes=\"{0:X2}h\"\r\n", _bmAttributes));
            s.Append(string.Format("\t\tInterfaces=\"{0}\"\r\n", _bNumInterfaces));
            s.Append(string.Format("\t\tDescriptorType=\"{0}\"\r\n", _bDescriptorType));
            s.Append(string.Format("\t\tDescriptorLength=\"{0}\"\r\n", _bLength));
            s.Append(string.Format("\t\tTotalLength=\"{0}\"\r\n", _wTotalLength));
            s.Append(string.Format("\t\tMaxPower=\"{0}\"\r\n", _MaxPower));

            for (int i = 0; i < _AltInterfaces; i++)
                s.Append(Interfaces[i].ToString());

            s.Append("\t</CONFIGURATION>\r\n");
            return s.ToString();
        }


    }
}
