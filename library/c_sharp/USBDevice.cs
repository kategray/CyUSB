/*
 ## Cypress CyUSB C# library source file (USBDevice.cs)
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
using Microsoft.Win32;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CyUSB
{
    /// <summary>
    /// Summary description for USBDevice.
    /// </summary>
    public abstract class USBDevice : IDisposable
    {
        protected bool _alreadyDisposed;  // Auto initialized to false.
        protected bool _nullEndpointFlag;

        internal IntPtr _hDevice = CyConst.INVALID_HANDLE;
        internal IntPtr _hHndNotification;

        internal Guid _drvGuid;
        internal Guid _hidGuid;

        internal byte _devices;
        internal byte _devNum;

        internal USBDevice(Guid g)
        {
            _drvGuid = g;

            // Find-out the HID GUID
            PInvoke.HidD_GetHidGuid(ref _hidGuid);
        }

        internal abstract unsafe bool Open(byte dev);

        // finalizer
        ~USBDevice()
        {
            Dispose(false);
        }

        // IDisposable implementation 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_alreadyDisposed) return;

            if (isDisposing)
            {
                // Free managed members that implement IDisposable
            }

            // Free the un-managed resources (handles)
            Close();

            _alreadyDisposed = true;
        }

        internal void Close()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            if (_hDevice != CyConst.INVALID_HANDLE) PInvoke.CloseHandle(_hDevice);
            _hDevice = CyConst.INVALID_HANDLE;

            if (_hHndNotification != IntPtr.Zero)
                PInvoke.UnregisterDeviceNotification(_hHndNotification);
        }

        internal virtual byte DeviceCount
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return PInvoke.CountDevices(_drvGuid);
            }
        }


        internal bool RegisterForPnPEvents(IntPtr hWnd)
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            DEV_BROADCAST_HANDLE hFilter = new DEV_BROADCAST_HANDLE();
            hFilter.dbch_size = Marshal.SizeOf(hFilter);
            hFilter.dbch_devicetype = CyConst.DBT_DEVTYP_HANDLE;
            hFilter.dbch_handle = _hDevice;

            _hHndNotification = PInvoke.RegisterDeviceNotification(hWnd, hFilter, CyConst.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (_hHndNotification == IntPtr.Zero) return false;

            return true;
        }

        public override bool Equals(object right)
        {
            if (right == null) return false;

            if (object.ReferenceEquals(this, right)) return true;

            if (this.GetType() != right.GetType()) return false;

            USBDevice dev = right as USBDevice;

            // The device paths of 2 different devices are unique in Windows
            return this._path.Equals(dev._path);
        }


        public override int GetHashCode()
        {
            Random rnd = new Random();
            int nRandom = rnd.Next(Int32.MinValue, Int32.MaxValue);
            
            return (nRandom ^ this.GetType().ToString().GetHashCode());
        }

        protected string _name;
        public string Name
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _name;
            }
        }

        protected string _friendlyName;
        public string FriendlyName
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _friendlyName;
            }
        }

        protected string _manufacturer;
        public string Manufacturer
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _manufacturer;
            }
        }

        protected string _product;
        public string Product
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _product;
            }
        }

        protected string _serialNumber;
        public string SerialNumber
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _serialNumber;
            }
        }

        protected ushort _vendorID;
        public ushort VendorID
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _vendorID;
            }
        }

        protected ushort _productID;
        public ushort ProductID
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _productID;
            }
        }

        public string _path;
        public string Path
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _path;
            }
        }

        protected byte _usbAddress;
        public byte USBAddress
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _usbAddress;
            }
        }

        protected ushort _bcdUSB;
        public ushort BcdUSB
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _bcdUSB;
            }
        }

        protected byte _devClass;
        public byte DevClass
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _devClass;
            }
        }

        protected byte _devSubClass;
        public byte DevSubClass
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _devSubClass;
            }
        }

        protected byte _devProtocol;
        public byte DevProtocol
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _devProtocol;
            }
        }


        public virtual TreeNode Tree
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");

                TreeNode t = new TreeNode(FriendlyName);

                t.Tag = this;

                return t;
            }
        }


        protected string _driverName;
        public string DriverName
        {
            get { return _driverName; }
        }

    }
}
