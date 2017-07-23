using System;
using System.Collections;
using System.Runtime.InteropServices;


namespace CyUSB
{
    /// <summary>
    /// Summary description for CyHidDeviceList.
    /// </summary>
    public class CyHidDeviceList
    {
        private ArrayList Items;
        private IntPtr hDevNotification;
        private Guid HidGuid;

        private static MsgForm MsgWin = new MsgForm();  // Only want 1 of these.

        public void PnpNotify(App_PnP_Callback cb)
        {
            MsgWin.AppCallback = new App_PnP_Callback(cb);
        }

        public CyHidDeviceList()
        {
            hDevNotification = IntPtr.Zero;

            RegisterForPnpEvents(MsgWin.Handle);

            CyHidDevice tmpDev = new CyHidDevice();
            int devs = tmpDev.DeviceCount;

            Items = new ArrayList(devs);

            for (int i = 0; i < devs; i++)
            {
                CyHidDevice tmp = new CyHidDevice();
                if (tmp.Open((byte)i)) Items.Add(tmp);
            }
        }


        bool RegisterForPnpEvents(IntPtr h)
        {
            PInvoke.HidD_GetHidGuid(ref HidGuid);

            DEV_BROADCAST_DEVICEINTERFACE dFilter = new DEV_BROADCAST_DEVICEINTERFACE();
            dFilter.dbcc_size = Marshal.SizeOf(dFilter);
            dFilter.dbcc_devicetype = gc.DBT_DEVTYP_DEVICEINTERFACE;
            dFilter.dbcc_classguid = HidGuid;

            hDevNotification = PInvoke.RegisterDeviceNotification(h, dFilter, gc.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (hDevNotification == IntPtr.Zero) return false;

            return true;
        }


        // Indexers
        public CyHidDevice this[int index]
        {
            get { return (CyHidDevice)Items[index]; }

            set { Items[index] = value; }
        }

        public CyHidDevice this[int VID, int PID]
        {
            get
            {
                for (byte i = 0; i < Count; i++)
                {
                    CyHidDevice tmp = (CyHidDevice)Items[i];
                    if ((VID == tmp.VendorID) && (PID == tmp.ProductID)) return tmp;
                }

                return null;
            }
        }

        public CyHidDevice this[int VID, int PID, int UsagePg, int Usage]
        {
            get
            {
                for (byte i = 0; i < Count; i++)
                {
                    CyHidDevice tmp = (CyHidDevice)Items[i];
                    if ((VID == tmp.VendorID) && (PID == tmp.ProductID) &&
                            (UsagePg == tmp.UsagePage) && (Usage == tmp.Usage)) return tmp;
                }

                return null;
            }
        }

        public CyHidDevice this[string sMfg, string sProd]
        {
            get
            {
                for (byte i = 0; i < Count; i++)
                {
                    CyHidDevice tmp = (CyHidDevice)Items[i];
                    if (sMfg.Equals(tmp.Manufacturer) && sProd.Equals(tmp.Product)) return tmp;
                }

                return null;
            }
        }

        public CyHidDevice this[string sMfg, string sProd, int UsagePg, int Usage]
        {
            get
            {
                for (byte i = 0; i < Count; i++)
                {
                    CyHidDevice tmp = (CyHidDevice)Items[i];
                    if (sMfg.Equals(tmp.Manufacturer) && sProd.Equals(tmp.Product) &&
                            (UsagePg == tmp.UsagePage) && (Usage == tmp.Usage)) return tmp;
                }

                return null;
            }
        }


        public int Count
        {
            get { return Items.Count; }
        }

    }
}
