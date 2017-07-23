/*
 ## Cypress CyUSB C# library source file (CyHidReport.cs)
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
using System.Runtime.InteropServices;

namespace CyUSB
{
    /// <summary>
    /// Summary description for CyHidReport.
    /// </summary>
    public unsafe class CyHidReport
    {
        HIDP_REPORT_TYPE _rptType;

        HID_DATA[] Items;
        public CyHidButton[] Buttons;
        public CyHidValue[] Values;


        public byte[] DataBuf;

        byte _ReportID;
        public byte ID
        {
            get { return _ReportID; }
        }

        int _RptByteLen;
        public int RptByteLen
        {
            get { return _RptByteLen; }
        }

        int _NumBtnCaps;
        public int NumBtnCaps
        {
            get { return _NumBtnCaps; }
        }

        int _NumValCaps;
        public int NumValCaps
        {
            get { return _NumValCaps; }
        }

        int _NumValues;
        public int NumValues
        {
            get { return _NumValues; }
        }

        int _NumItems;
        public int NumItems
        {
            get { return _NumItems; }
        }

        internal unsafe CyHidReport(HIDP_REPORT_TYPE rType, HIDP_CAPS hidCaps, byte* PreparsedDta)
        {
            _rptType = rType;

            if (rType == HIDP_REPORT_TYPE.HidP_Input)
            {
                _RptByteLen = hidCaps.InputReportByteLength;
                _NumBtnCaps = hidCaps.NumberInputButtonCaps;
                _NumValCaps = hidCaps.NumberInputValueCaps;
            }
            else if (rType == HIDP_REPORT_TYPE.HidP_Output)
            {
                _RptByteLen = hidCaps.OutputReportByteLength;
                _NumBtnCaps = hidCaps.NumberOutputButtonCaps;
                _NumValCaps = hidCaps.NumberOutputValueCaps;
            }
            else
            {
                _RptByteLen = hidCaps.FeatureReportByteLength;
                _NumBtnCaps = hidCaps.NumberFeatureButtonCaps;
                _NumValCaps = hidCaps.NumberFeatureValueCaps;
            }

            // Big enough to hold the report ID and the report data
            if (_RptByteLen > 0) DataBuf = new byte[_RptByteLen + 1];

            if (_NumBtnCaps > 0)
            {
                HIDP_BTN_VAL_CAPS ButtonCaps;
                Buttons = new CyHidButton[_NumBtnCaps];

                HIDP_BTN_VAL_CAPS bc = new HIDP_BTN_VAL_CAPS();
                byte[] buffer = new byte[_NumBtnCaps * Marshal.SizeOf(bc)];

                fixed (byte* buf = buffer)
                {
                    int numCaps = _NumBtnCaps;
                    //
                    //  BUGFIX 3/07/2008 - HidP_GetButtonCaps will modify numCaps to the
                    //      "actual number of elements that the routine returns".
                    //      In the somewhat rare event that numcaps is < _NumBtnCaps
                    //      on return, the reference to bCaps[i] in the loop below
                    //      will throw an "Index Was Outside the Bounds of the Array"
                    //      Exception.  This would occur for example when the
                    //      top-level HID report (being reported here) contains
                    //      a subset of the available buttons in the full HID interface.
                    //
                    PInvoke.HidP_GetButtonCaps(rType, buf, ref numCaps, PreparsedDta);

                    //
                    //  Reset _NumBtnCaps to the actual returned value.
                    //
                    _NumBtnCaps = numCaps;

                    HIDP_BTN_VAL_CAPS* bCaps = (HIDP_BTN_VAL_CAPS*)buf;
                    for (int i = 0; i < _NumBtnCaps; i++)
                    {
                        // This assignment copies values from buf into ButtonCaps.
                        ButtonCaps = bCaps[i];

                        // Note that you must pass ButtonCaps to the
                        // below constructor and not bCaps[i]
                        Buttons[i] = new CyHidButton(ButtonCaps);

                        // Each button should have the same ReportID
                        _ReportID = ButtonCaps.ReportID;
                    }

                }
            }

            if (_NumValCaps > 0)
            {
                Values = new CyHidValue[_NumValCaps];

                HIDP_BTN_VAL_CAPS vc = new HIDP_BTN_VAL_CAPS();
                byte[] buffer = new byte[_NumValCaps * Marshal.SizeOf(vc)];

                fixed (byte* buf = buffer)
                {
                    int numCaps = _NumValCaps;
                    PInvoke.HidP_GetValueCaps(rType, buf, ref numCaps, PreparsedDta);

                    HIDP_BTN_VAL_CAPS* vCaps = (HIDP_BTN_VAL_CAPS*)buf;
                    for (int i = 0; i < _NumValCaps; i++)
                    {
                        // This assignment copies values from buf into ValueCaps.
                        HIDP_BTN_VAL_CAPS ValueCaps = vCaps[i];

                        // Note that you must pass ValueCaps[i] to the
                        // below constructor and not vCaps[i]
                        Values[i] = new CyHidValue(ValueCaps);

                        // Each value should have the same ReportID
                        _ReportID = ValueCaps.ReportID;
                    }
                }
            }


            _NumValues = 0;
            for (int i = 0; i < _NumValCaps; i++)
                //if (Values[i].IsRange)
                //    _NumValues += Values[i].UsageMax - Values[i].Usage + 1;
                //else
                _NumValues++;


            _NumItems = _NumBtnCaps + _NumValues;

            if (_NumItems > 0) Items = new HID_DATA[_NumItems];

            //if ((ButtonCaps != null) && (Items != null))
            if ((_NumBtnCaps > 0) && (Items != null))
            {
                for (int i = 0; i < _NumBtnCaps; i++)
                {
                    Items[i].IsButtonData = 1;
                    Items[i].Status = CyConst.HIDP_STATUS_SUCCESS;
                    Items[i].UsagePage = Buttons[i].UsagePage;

                    if (Buttons[i].IsRange)
                    {
                        Items[i].Usage = Buttons[i].Usage;
                        Items[i].UsageMax = Buttons[i].UsageMax;
                    }
                    else
                        Items[i].Usage = Items[i].UsageMax = Buttons[i].Usage;

                    Items[i].MaxUsageLength = PInvoke.HidP_MaxUsageListLength(
                        rType,
                        Buttons[i].UsagePage,
                        PreparsedDta);

                    Items[i].Usages = new ushort[Items[i].MaxUsageLength];

                    Items[i].ReportID = Buttons[i].ReportID;
                }
            }


            for (int i = 0; i < _NumValues; i++)
            {
                if (Values[i].IsRange)
                {
                    for (ushort usage = Values[i].Usage;
                        usage <= Values[i].UsageMax;
                        usage++)
                    {
                        Items[i].IsButtonData = 0;
                        Items[i].Status = CyConst.HIDP_STATUS_SUCCESS;
                        Items[i].UsagePage = Values[i].UsagePage;
                        Items[i].Usage = usage;
                        Items[i].ReportID = Values[i].ReportID;
                    }
                }
                else
                {
                    Items[i].IsButtonData = 0;
                    Items[i].Status = CyConst.HIDP_STATUS_SUCCESS;
                    Items[i].UsagePage = Values[i].UsagePage;
                    Items[i].Usage = Values[i].Usage;
                    Items[i].ReportID = Values[i].ReportID;
                }
            }



        }  // End of CyHidReport constructor

        public void Clear()
        {
            for (int i = 0; i <= RptByteLen; i++)
                DataBuf[i] = 0;
        }

        public TreeNode Tree
        {
            get
            {
                string sType = "";

                if (_rptType == HIDP_REPORT_TYPE.HidP_Input) sType = "Input";
                else if (_rptType == HIDP_REPORT_TYPE.HidP_Output) sType = "Output";
                else if (_rptType == HIDP_REPORT_TYPE.HidP_Feature) sType = "Feature";

                if (_NumItems > 0)
                {
                    TreeNode[] subTree = new TreeNode[_NumItems];

                    int b = 0;
                    for (b = 0; b < _NumBtnCaps; b++)
                    {
                        TreeNode t = new TreeNode("Button");
                        t.Tag = Buttons[b];
                        subTree[b] = t;
                    }

                    for (int v = 0; v < _NumValCaps; v++)
                    {
                        TreeNode t = new TreeNode("Value");
                        t.Tag = Values[v];
                        subTree[b + v] = t;
                    }

                    TreeNode tr = new TreeNode(sType, subTree);
                    tr.Tag = this;

                    return tr;
                }
                else
                    return null;
            }
        }




        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            string sRptType = "";

            if (_rptType == HIDP_REPORT_TYPE.HidP_Feature) sRptType = "FEATURE";
            else if (_rptType == HIDP_REPORT_TYPE.HidP_Input) sRptType = "INPUT";
            else if (_rptType == HIDP_REPORT_TYPE.HidP_Output) sRptType = "OUTPUT";

            s.Append(string.Format("\t<{0}>\r\n", sRptType));
            //s.Append(string.Format("\t\tReportID=\"{0}\"\r\n", _ReportID));
            s.Append(string.Format("\t\tRptByteLen=\"{0}\"\r\n", RptByteLen));

            s.Append(string.Format("\t\tButtons=\"{0}\"\r\n", _NumBtnCaps));
            s.Append(string.Format("\t\tValues=\"{0}\"\r\n", _NumValues));

            if (NumBtnCaps > 0)
                foreach (CyHidButton btn in Buttons)
                    s.Append(btn.ToString());

            if (NumValCaps > 0)
                foreach (CyHidValue val in Values)
                    s.Append(val.ToString());

            s.Append(string.Format("\t</{0}>\r\n", sRptType));

            return s.ToString();
        }

    }  // End of CyHidReport class


    public class CyHidButton
    {
        protected HIDP_BTN_VAL_CAPS Caps;

        public CyHidButton(HIDP_BTN_VAL_CAPS bc)
        {
            Caps = bc;
        }

        public ushort ReportID
        {
            get { return Caps.ReportID; }
        }

        public ushort BitField
        {
            get { return Caps.BitField; }
        }

        public ushort LinkUsage
        {
            get { return Caps.LinkUsage; }
        }

        public ushort LinkUsagePage
        {
            get { return Caps.LinkUsagePage; }
        }

        public ushort LinkCollection
        {
            get { return Caps.LinkCollection; }
        }

        public ushort DataIndex
        {
            get { return Caps.DataIndex; }
        }

        public ushort DataIndexMax
        {
            get { return Caps.DataIndexMax; }
        }

        public ushort StringIndex
        {
            get { return Caps.StringIndex; }
        }

        public ushort StringMax
        {
            get { return Caps.StringMax; }
        }

        public ushort DesignatorIndex
        {
            get { return Caps.DesignatorIndex; }
        }

        public ushort DesignatorIndexMax
        {
            get { return Caps.DesignatorMax; }
        }

        public ushort Usage
        {
            get { return Caps.Usage; }
        }

        public ushort UsagePage
        {
            get { return Caps.UsagePage; }
        }

        public ushort UsageMax
        {
            get { return Caps.UsageMax; }
        }

        public bool IsAlias
        {
            get { return Caps.IsAlias > 0; }
        }

        public bool IsRange
        {
            get { return Caps.IsRange > 0; }
        }

        public bool IsStringRange
        {
            get { return Caps.IsStringRange > 0; }
        }

        public bool IsDesignatorRange
        {
            get { return Caps.IsDesignatorRange > 0; }
        }

        public bool IsAbsolute
        {
            get { return Caps.IsAbsolute > 0; }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append(string.Format("\t\t<BUTTON>\r\n"));
            s.Append(string.Format("\t\t\tReportID=\"{0}\"\r\n", Caps.ReportID));
            s.Append(string.Format("\t\t\tUsage=\"{0}\"\r\n", Util.byteStr(Caps.Usage)));
            s.Append(string.Format("\t\t\tUsagePage=\"{0}\"\r\n", Util.byteStr(Caps.UsagePage)));
            s.Append(string.Format("\t\t\tUsageMax=\"{0}\"\r\n", Util.byteStr(Caps.UsageMax)));

            s.Append(string.Format("\t\t\tBitField=\"{0}\"\r\n", Util.byteStr(Caps.BitField)));
            s.Append(string.Format("\t\t\tLinkCollection=\"{0}\"\r\n", Util.byteStr(Caps.LinkCollection)));
            s.Append(string.Format("\t\t\tLinkUsage=\"{0}\"\r\n", Util.byteStr(Caps.LinkUsage)));
            s.Append(string.Format("\t\t\tLinkUsagePage=\"{0}\"\r\n", Util.byteStr(Caps.LinkUsagePage)));

            s.Append(string.Format("\t\t\tIsAlias=\"{0}\"\r\n", (Caps.IsAlias > 0)));
            s.Append(string.Format("\t\t\tIsRange=\"{0}\"\r\n", (Caps.IsRange > 0)));
            s.Append(string.Format("\t\t\tIsStringRange=\"{0}\"\r\n", (Caps.IsStringRange > 0)));
            s.Append(string.Format("\t\t\tIsDesignatorRange=\"{0}\"\r\n", (Caps.IsDesignatorRange > 0)));
            s.Append(string.Format("\t\t\tIsAbsolute=\"{0}\"\r\n", (Caps.IsAbsolute > 0)));

            s.Append(string.Format("\t\t\tStringIndex=\"{0}\"\r\n", Caps.StringIndex));
            s.Append(string.Format("\t\t\tStringMax=\"{0}\"\r\n", Caps.StringMax));
            s.Append(string.Format("\t\t\tDesignatorIndex=\"{0}\"\r\n", Caps.DesignatorIndex));
            s.Append(string.Format("\t\t\tDesignatorMax=\"{0}\"\r\n", Caps.DesignatorMax));
            s.Append(string.Format("\t\t\tDataIndex=\"{0}\"\r\n", Caps.DataIndex));
            s.Append(string.Format("\t\t\tDataIndexMax=\"{0}\"\r\n", Caps.DataIndexMax));
            s.Append(string.Format("\t\t</BUTTON>\r\n"));

            return s.ToString();
        }
    }

    // A HidValueCaps struct is a superset of a HidButtonCaps
    public class CyHidValue : CyHidButton
    {
        // Just invoke the base constructor
        public CyHidValue(HIDP_BTN_VAL_CAPS vc)
            : base(vc)
        {
        }

        public ushort BitSize
        {
            get { return Caps.BitSize; }
        }

        public bool HasNull
        {
            get { return Caps.HasNull > 0; }
        }

        public uint Units
        {
            get { return Caps.Units; }
        }

        public uint UnitsExp
        {
            get { return Caps.UnitsExp; }
        }

        public int LogicalMin
        {
            get { return Caps.LogicalMin; }
        }

        public int LogicalMax
        {
            get { return Caps.LogicalMax; }
        }

        public int PhysicalMin
        {
            get { return Caps.PhysicalMin; }
        }

        public int PhysicalMax
        {
            get { return Caps.PhysicalMax; }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append(string.Format("\t\t<VALUE>\r\n"));
            s.Append(string.Format("\t\t\tReportID=\"{0}\"\r\n", Caps.ReportID));
            s.Append(string.Format("\t\t\tUsage=\"{0}\"\r\n", Util.byteStr(Caps.Usage)));
            s.Append(string.Format("\t\t\tUsagePage=\"{0}\"\r\n", Util.byteStr(Caps.UsagePage)));
            s.Append(string.Format("\t\t\tUsageMax=\"{0}\"\r\n", Util.byteStr(Caps.UsageMax)));

            s.Append(string.Format("\t\t\tBitField=\"{0}\"\r\n", Util.byteStr(Caps.BitField)));
            s.Append(string.Format("\t\t\tLinkCollection=\"{0}\"\r\n", Util.byteStr(Caps.LinkCollection)));
            s.Append(string.Format("\t\t\tLinkUsage=\"{0}\"\r\n", Util.byteStr(Caps.LinkUsage)));
            s.Append(string.Format("\t\t\tLinkUsagePage=\"{0}\"\r\n", Util.byteStr(Caps.LinkUsagePage)));

            s.Append(string.Format("\t\t\tIsAlias=\"{0}\"\r\n", (Caps.IsAlias > 0)));
            s.Append(string.Format("\t\t\tIsRange=\"{0}\"\r\n", (Caps.IsRange > 0)));
            s.Append(string.Format("\t\t\tIsStringRange=\"{0}\"\r\n", (Caps.IsStringRange > 0)));
            s.Append(string.Format("\t\t\tIsDesignatorRange=\"{0}\"\r\n", (Caps.IsDesignatorRange > 0)));
            s.Append(string.Format("\t\t\tIsAbsolute=\"{0}\"\r\n", (Caps.IsAbsolute > 0)));
            s.Append(string.Format("\t\t\tHasNull=\"{0}\"\r\n", (Caps.HasNull > 0)));

            s.Append(string.Format("\t\t\tStringIndex=\"{0}\"\r\n", Caps.StringIndex));
            s.Append(string.Format("\t\t\tStringMax=\"{0}\"\r\n", Caps.StringMax));
            s.Append(string.Format("\t\t\tDesignatorIndex=\"{0}\"\r\n", Caps.DesignatorIndex));
            s.Append(string.Format("\t\t\tDesignatorMax=\"{0}\"\r\n", Caps.DesignatorMax));
            s.Append(string.Format("\t\t\tDataIndex=\"{0}\"\r\n", Caps.DataIndex));
            s.Append(string.Format("\t\t\tDataIndexMax=\"{0}\"\r\n", Caps.DataIndexMax));

            s.Append(string.Format("\t\t\tBitField=\"{0}\"\r\n", Util.byteStr(Caps.BitField)));
            s.Append(string.Format("\t\t\tLinkCollection=\"{0}\"\r\n", Util.byteStr(Caps.LinkCollection)));
            s.Append(string.Format("\t\t\tLinkUsage=\"{0}\"\r\n", Util.byteStr(Caps.LinkUsage)));
            s.Append(string.Format("\t\t\tLinkUsagePage=\"{0}\"\r\n", Util.byteStr(Caps.LinkUsagePage)));

            s.Append(string.Format("\t\t\tBitSize=\"{0}\"\r\n", Caps.BitSize));
            s.Append(string.Format("\t\t\tReportCount=\"{0}\"\r\n", Caps.ReportCount));
            s.Append(string.Format("\t\t\tUnits=\"{0}\"\r\n", Caps.Units));
            s.Append(string.Format("\t\t\tUnitsExp=\"{0}\"\r\n", Caps.UnitsExp));

            s.Append(string.Format("\t\t\tLogicalMin=\"{0}\"\r\n", Caps.LogicalMin));
            s.Append(string.Format("\t\t\tLogicalMax=\"{0}\"\r\n", Caps.LogicalMax));
            s.Append(string.Format("\t\t\tPhysicalMin=\"{0}\"\r\n", Caps.PhysicalMin));
            s.Append(string.Format("\t\t\tPhysicalMax=\"{0}\"\r\n", Caps.PhysicalMax));

            s.Append(string.Format("\t\t</VALUE>\r\n"));

            return s.ToString();
        }
    }

}
