/*
 ## Cypress CyUSB C# library source file (CyFX2Device.cs)
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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;

namespace CyUSB
{
    public class CyFX2Device : CyUSBDevice
    {
        private byte[] FwImage = new byte[Util.MaxFwSize];
        private ushort ImageLen = 0;
        private ushort FwOffset = 0;
        private ArrayList VendAX = new ArrayList();
        private ArrayList list = new ArrayList();
        private ArrayList list1 = new ArrayList();

        FileStream m_script_file_name;
        public void ScriptFileForDwld(FileStream file_name)
        { m_script_file_name = file_name; }

        public bool m_bRecording = false;
        public byte m_ConfigNum = 0;
        public byte m_IntfcNum = 0;
        public byte m_AltIntfc = 0;

        // Set RecordingFlag to bRecording(true) or bRecording(false)
        public void SetRecordingFlag(bool bRecording, byte ConfigNum, byte IntfcNum, byte AltIntfc)
        { m_bRecording = bRecording; m_ConfigNum = ConfigNum; m_IntfcNum = IntfcNum; m_AltIntfc = AltIntfc; }

        public bool IsRecordingFlagSet()
        { return m_bRecording; }

        internal CyFX2Device() : this(CyConst.CyGuid) { }

        internal CyFX2Device(Guid guid)
            : base(guid)
        {
            InitVendAX();
        }

        public string Vend_AX
        {
            get
            {
                string s = "";

                foreach (string line in VendAX)
                    s += line + "\r\n";

                return s;
            }

            set
            {
                if (!File.Exists(value)) return;

                VendAX.Clear();

                string line;

                StreamReader srcStream = new StreamReader(value);
                if (srcStream == null) return;
                while ((line = srcStream.ReadLine()) != null)
                    VendAX.Add(line);

                srcStream.Close();
            }
        }

        public bool LoadEEPROM(string fwFile)
        {
            return LoadEEPROM(fwFile, true);
        }

        public bool LoadEEPROM(string fwFile, bool isLargeEEprom)
        {
            if (!LoadRamHex("VendAX"))
                return false;

            // Note that .hex files are not in the right format to be loaded into the EEPROM
            // The FX2 expects EEPROM contents in .iic file format.
            if (fwFile.ToLower().Contains(".iic"))
            {
                if (!LoadEpromIIC(fwFile, isLargeEEprom))
                    return false;
            }
            else
                return false;

            if (!VerifyFW(isLargeEEprom))
                return false;

            return true;
        }

        public bool LoadExternalRam(string fwFile)
        {
            if (fwFile.ToLower().Contains(".iic"))
                return LoadRamIIC(fwFile);
            else if (fwFile.ToLower().Contains(".hex"))
            {
                if (!LoadRamHex("VendAX"))
                    return false;

                if (!LoadRamHex(fwFile, false)) /* Load High Addresses above 16K */
                {
                    return false;
                }

                if (!LoadRamHex(fwFile, true)) /* Load High Addresses below 16K */
                {
                    return false;
                }

                return true;
            }
            else
                return false;
        }

        private bool LoadRamHex(string fname, bool blow)
        {
            TTransaction m_Xaction = new TTransaction();

            list.Clear();
            list1.Clear();

            string line, sOffset, tmp;
            int v;

            FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            while (!sr.EndOfStream)
            {
                list.Add(sr.ReadLine());
            }
            sr.Close();
            fs.Close();

            int Ramsize = 0x4000;

            // Delete non-data records
            for (int i = list.Count - 1; i >= 0; i--)
            {
                line = (string)list[i];
                if (line.Length > 0)
                {
                    tmp = line.Substring(7, 2);   // Get the Record Type into v
                    v = (int)Util.HexToInt(tmp);
                    if (v != 0) list.Remove(list[i]);   // Data records are type == 0
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                line = (string)list[i];

                // Remove comments
                v = line.IndexOf("//");
                if (v > -1)
                    line = line.Substring(0, v - 1);

                // Build string that just contains the offset followed by the data bytes
                if (line.Length > 0)
                {
                    // Get the offset
                    sOffset = line.Substring(3, 4);

                    // Get the string of data chars
                    tmp = line.Substring(1, 2);
                    v = (int)Util.HexToInt(tmp) * 2;
                    string s = line.Substring(9, v);

                    list1.Add(sOffset + s);
                }

            }

            if (blow) Reset(1);

            byte Reqcode = blow ? (byte)0xA0 : (byte)0xA3;
            ushort windex = 0;

            int iindex = 0;
            string Datastring = "";
            int nxtoffset = 0;
            int xferLen = 0;
            ushort wvalue = 0;

            foreach (string lines in list1)
            {
                line = lines.Substring(0, 4);
                ushort offset = (ushort)Util.HexToInt(line);

                int slen = lines.Length;

                int no_bytes = (slen - 4) / 2;
                int lastaddr = offset + no_bytes;

                //if (blow && (offset < Ramsize) && (lastaddr > Ramsize))
                //    no_bytes = Ramsize - offset;

                //if (!blow && (offset < Ramsize) && (lastaddr > Ramsize))
                //{
                //    no_bytes = lastaddr - (int)Ramsize;
                //    string s = "xxxx" + lines.Substring(slen - (no_bytes * 2), no_bytes * 2);

                //    list1[iindex] = s;
                //    offset = (ushort)Ramsize;
                //    line = "4000";
                //}

                //if ((blow && (offset < Ramsize)) || (!blow && (offset >= Ramsize)))
                if ((blow && (lastaddr < Ramsize)) || (!blow && (lastaddr >= Ramsize)))
                {
                    xferLen += no_bytes;

                    if ((offset == nxtoffset) && (xferLen < 0x1000))
                    {
                        Datastring += lines.Substring(4, no_bytes * 2);
                    }
                    else
                    {
                        int len = Datastring.Length;


                        if (!len.Equals(0))
                        {
                            int bufLen = len / 2;
                            byte[] buf = new byte[bufLen];
                            string d;

                            for (int j = 0; j < bufLen; j++)
                            {
                                d = Datastring.Substring(j * 2, 2);
                                buf[j] = (byte)Util.HexToInt(d);
                            }

                            ControlEndPt.Target = CyConst.TGT_DEVICE;
                            ControlEndPt.ReqType = CyConst.REQ_VENDOR;
                            ControlEndPt.Direction = CyConst.DIR_TO_DEVICE;
                            ControlEndPt.ReqCode = Reqcode;
                            ControlEndPt.Value = wvalue;
                            ControlEndPt.Index = windex;

                            ControlEndPt.Write(ref buf, ref bufLen);
                            if (m_bRecording && (m_script_file_name != null))
                            {
                                m_Xaction.AltIntfc = m_AltIntfc;
                                m_Xaction.ConfigNum = m_ConfigNum;
                                m_Xaction.IntfcNum = m_IntfcNum;
                                m_Xaction.EndPtAddr = ControlEndPt.Address;
                                m_Xaction.Tag = 0;

                                m_Xaction.bReqType = (byte)(ControlEndPt.Direction | ControlEndPt.ReqType | ControlEndPt.Target);
                                m_Xaction.CtlReqCode = ControlEndPt.ReqCode;
                                m_Xaction.wValue = ControlEndPt.Value;
                                m_Xaction.wIndex = ControlEndPt.Index;
                                m_Xaction.DataLen = (uint)bufLen;
                                m_Xaction.Timeout = ControlEndPt.TimeOut / 1000;
                                m_Xaction.RecordSize = (uint)bufLen + TTransaction.TotalHeaderSize;

                                //Write m_Xaction and buffer
                                m_Xaction.WriteToStream(m_script_file_name);
                                m_Xaction.WriteFromBuffer(m_script_file_name, ref buf, ref bufLen);
                            }

                        }

                        wvalue = (ushort)Util.HexToInt(line);
                        Datastring = lines.Substring(4, no_bytes * 2);
                        xferLen = no_bytes;
                    }

                    nxtoffset = offset + no_bytes;
                }
                iindex++;
            }

            int len1 = Datastring.Length;
            if (!len1.Equals(0))
            {
                int bufLen = len1 / 2;
                byte[] buf1 = new byte[bufLen];
                string d;

                for (int j = 0; j < bufLen; j++)
                {
                    d = Datastring.Substring(j * 2, 2);
                    buf1[j] = (byte)Util.HexToInt(d);
                }


                ControlEndPt.Target = CyConst.TGT_DEVICE;
                ControlEndPt.ReqType = CyConst.REQ_VENDOR;
                ControlEndPt.Direction = CyConst.DIR_TO_DEVICE;
                ControlEndPt.ReqCode = Reqcode;
                ControlEndPt.Value = wvalue;
                ControlEndPt.Index = windex;

                ControlEndPt.Write(ref buf1, ref bufLen);

                if (m_bRecording && (m_script_file_name != null))
                {
                    m_Xaction.AltIntfc = m_AltIntfc;
                    m_Xaction.ConfigNum = m_ConfigNum;
                    m_Xaction.IntfcNum = m_IntfcNum;
                    m_Xaction.EndPtAddr = ControlEndPt.Address;
                    m_Xaction.Tag = 0;

                    m_Xaction.bReqType = (byte)(ControlEndPt.Direction | ControlEndPt.ReqType | ControlEndPt.Target);
                    m_Xaction.CtlReqCode = ControlEndPt.ReqCode;
                    m_Xaction.wValue = ControlEndPt.Value;
                    m_Xaction.wIndex = ControlEndPt.Index;
                    m_Xaction.DataLen = (uint)bufLen;
                    m_Xaction.Timeout = ControlEndPt.TimeOut / 1000;
                    m_Xaction.RecordSize = (uint)bufLen + TTransaction.TotalHeaderSize;

                    //Write m_Xaction and buffer
                    m_Xaction.WriteToStream(m_script_file_name);
                    m_Xaction.WriteFromBuffer(m_script_file_name, ref buf1, ref bufLen);
                }

            }

            if (blow) Reset(0);

            return true;
        }

        public bool LoadRAM(string fwFile)
        {
            if (fwFile.ToLower().Contains(".iic"))
                return LoadRamIIC(fwFile);
            else if (fwFile.ToLower().Contains(".hex") || fwFile.Equals("VendAX"))
                return LoadRamHex(fwFile);
            else
                return false;
        }


        public void Reset(int hold)
        {
            TTransaction m_FXaction = new TTransaction();
            byte[] dta = new byte[8];

            ControlEndPt.Target = CyConst.TGT_DEVICE;
            ControlEndPt.ReqType = CyConst.REQ_VENDOR;
            ControlEndPt.Value = 0xE600;
            ControlEndPt.Index = 0x0000;

            ControlEndPt.ReqCode = 0xA0;
            dta[0] = (byte)hold;
            int len = 1;
            ControlEndPt.Write(ref dta, ref len);
            len = 1;
            if (m_bRecording && (m_script_file_name != null))
            {
                m_FXaction.AltIntfc = m_AltIntfc;
                m_FXaction.ConfigNum = m_ConfigNum;
                m_FXaction.IntfcNum = m_IntfcNum;
                m_FXaction.EndPtAddr = ControlEndPt.Address;
                m_FXaction.Tag = 0;

                m_FXaction.bReqType = (byte)(ControlEndPt.Direction | ControlEndPt.ReqType | ControlEndPt.Target);
                m_FXaction.CtlReqCode = ControlEndPt.ReqCode;
                m_FXaction.wValue = ControlEndPt.Value;
                m_FXaction.wIndex = ControlEndPt.Index;
                m_FXaction.DataLen = (uint)len;
                m_FXaction.Timeout = ControlEndPt.TimeOut / 1000;
                m_FXaction.RecordSize = (uint)len + TTransaction.TotalHeaderSize;

                //Write m_FXaction and buffer
                m_FXaction.WriteToStream(m_script_file_name);
                m_FXaction.WriteFromBuffer(m_script_file_name, ref dta, ref len);

                Thread.Sleep(0);
            }


            Thread.Sleep(500);
        }


        private bool ReadConfigData(string cfgFile)
        {
            if (cfgFile.Equals("")) return false;

            // Suck-in the data from the .iic file
            Stream fStream = new FileStream(cfgFile, FileMode.Open, FileAccess.Read);
            if (fStream == null) return false;
            ImageLen = (ushort)fStream.Length;

            if (ImageLen > Util.MaxFwSize)
            {
                fStream.Close();
                return false;
            }

            fStream.Read(FwImage, 0, ImageLen);
            fStream.Close();

            return true;
        }


        private bool LoadRamHex(string fileName)
        {
            TTransaction m_Xaction = new TTransaction();
            // A .hex file is already in the correct format for the FX2 RAM
            bool rVal = false;

            if (fileName == "VendAX")
                rVal = Util.ParseHexData(VendAX, FwImage, ref ImageLen, ref FwOffset);
            else
                rVal = Util.ParseHexFile(fileName, FwImage, ref ImageLen, ref FwOffset);

            if (rVal)
            {
                ControlEndPt.Target = CyConst.TGT_DEVICE;
                ControlEndPt.ReqType = CyConst.REQ_VENDOR;
                ControlEndPt.ReqCode = 0xA0;
                ControlEndPt.Index = 0;

                Reset(1); // Halt

                ushort chunk = 2048;
                byte[] buffer = new byte[chunk];

                for (ushort i = FwOffset; i < ImageLen; i += chunk)
                {
                    ControlEndPt.Value = i;
                    int len = ((i + chunk) < ImageLen) ? chunk : ImageLen - i;
                    Array.Copy(FwImage, i, buffer, 0, len);

                    ControlEndPt.Write(ref buffer, ref len);

                    if (m_bRecording && (m_script_file_name != null))
                    {
                        m_Xaction.AltIntfc = m_AltIntfc;
                        m_Xaction.ConfigNum = m_ConfigNum;
                        m_Xaction.IntfcNum = m_IntfcNum;
                        m_Xaction.EndPtAddr = ControlEndPt.Address;
                        m_Xaction.Tag = 0;

                        m_Xaction.bReqType = (byte)(ControlEndPt.Direction | ControlEndPt.ReqType | ControlEndPt.Target);
                        m_Xaction.CtlReqCode = ControlEndPt.ReqCode;
                        m_Xaction.wValue = ControlEndPt.Value;
                        m_Xaction.wIndex = ControlEndPt.Index;
                        m_Xaction.DataLen = (uint)len;
                        m_Xaction.Timeout = ControlEndPt.TimeOut / 1000;
                        m_Xaction.RecordSize = (uint)len + TTransaction.TotalHeaderSize;

                        //Write m_Xaction and buffer
                        m_Xaction.WriteToStream(m_script_file_name);
                        m_Xaction.WriteFromBuffer(m_script_file_name, ref buffer, ref len);
                    }

                }

                Reset(0); // Run

                return true;
            }

            else
                return false;

        }


        private bool LoadRamIIC(string fwFile)
        {
            // Need to do this here, since FwImage contains VendAX at this point.
            for (int i = 0; i < Util.MaxFwSize; i++) FwImage[i] = 0xFF;

            ReadConfigData(fwFile);

            // FwImage holds the file contents, suitable for the EEPROM
            // Now, parse it into FwBuf, putting each record at the right offset,
            // suitable for the FX2 RAM
            byte[] FwBuf = new byte[Util.MaxFwSize];
            for (int i = 0; i < Util.MaxFwSize; i++) FwBuf[i] = 0xFF;
            Util.ParseIICData(FwImage, FwBuf, ref ImageLen, ref FwOffset);

            ControlEndPt.Target = CyConst.TGT_DEVICE;
            ControlEndPt.ReqType = CyConst.REQ_VENDOR;
            ControlEndPt.ReqCode = 0xA0;
            ControlEndPt.Index = 0;

            Reset(1); // Halt

            ushort chunk = 2048;
            byte[] buffer = new byte[chunk];

            for (ushort i = FwOffset; i < ImageLen; i += chunk)
            {
                ControlEndPt.Value = i;
                int len = ((i + chunk) < ImageLen) ? chunk : ImageLen - i;
                Array.Copy(FwBuf, i, buffer, 0, len);

                if (!ControlEndPt.Write(ref buffer, ref len))
                    return false;
            }

            Reset(0); // Run

            return true;

        }


        private bool LoadEpromIIC(string fwFile, bool isLargeEEprom)
        {
            // Need to do this here, since FwImage contains VendAX at this point.
            for (int i = 0; i < Util.MaxFwSize; i++) FwImage[i] = 0xFF;

            if (!ReadConfigData(fwFile))
                return false;

            ControlEndPt.Target = CyConst.TGT_DEVICE;
            ControlEndPt.ReqType = CyConst.REQ_VENDOR;

            // select variable is used as an indentifier to EEPROM... 1 for large EEPROM and 2 for small EEPROM

            //if(select==1)
            //    ControlEndPt.ReqCode = 0xA9;
            //else if(select==2)
            //    ControlEndPt.ReqCode = 0xA2;

            ControlEndPt.ReqCode = isLargeEEprom ? (byte)0xA9 : (byte)0xA2;

            ControlEndPt.Index = 0;

            uint toTemp = ControlEndPt.TimeOut;
            ControlEndPt.TimeOut = 25000;

            ushort chunk = 4096;
            byte[] buffer = new byte[chunk];

            for (ushort i = 0; i < ImageLen; i += chunk)
            {
                ControlEndPt.Value = i;
                int len = ((i + chunk) < ImageLen) ? chunk : ImageLen - i;

                Array.Copy(FwImage, i, buffer, 0, len);
                if (!ControlEndPt.Write(ref buffer, ref len))
                    return false;
            }

            ControlEndPt.TimeOut = toTemp;

            return true;
        }


        private bool VerifyFW(bool isLargeEEprom)
        {
            // The written .iic file data needs to still be in FwImage when
            // this function is invoked.
            ControlEndPt.Target = CyConst.TGT_DEVICE;
            ControlEndPt.ReqType = CyConst.REQ_VENDOR;
            ControlEndPt.Index = 0;
            if (isLargeEEprom)
                ControlEndPt.ReqCode = CyConst.LARGEEEPROM_FW_VERIFIATIONCODE;
            else
                ControlEndPt.ReqCode = CyConst.SMALLEEPROM_FW_VERIFIATIONCODE;

            uint toTemp = ControlEndPt.TimeOut;
            ControlEndPt.TimeOut = 25000;

            ushort chunk = 4096;
            byte[] buffer = new byte[chunk];

            for (ushort i = 0; i < ImageLen; i += chunk)
            {
                ControlEndPt.Value = i;
                int len = ((i + chunk) < ImageLen) ? chunk : ImageLen - i;

                if (!ControlEndPt.Read(ref buffer, ref len))
                    return false;

                for (int b = 0; b < len; b++)
                    if (buffer[b] != FwImage[b + i])
                        return false;
            }

            ControlEndPt.TimeOut = toTemp;

            return true;
        }


        private void InitVendAX()
        {
            VendAX.Add(":0A0D3E00000102020303040405058E");
            VendAX.Add(":10064D00E4F52CF52BF52AF529C203C200C202C22E");
            VendAX.Add(":10065D0001120C6C7E0A7F008E0A8F0B75120A75C3");
            VendAX.Add(":10066D00131275080A75091C75100A75114A75144F");
            VendAX.Add(":10067D000A751578EE54C07003020752752D00757A");
            VendAX.Add(":10068D002E808E2F8F30C3749A9FFF740A9ECF24B5");
            VendAX.Add(":10069D0002CF3400FEE48F288E27F526F525F524AC");
            VendAX.Add(":1006AD00F523F522F521AF28AE27AD26AC25AB24D9");
            VendAX.Add(":1006BD00AA23A922A821C3120D035037E530252402");
            VendAX.Add(":1006CD00F582E52F3523F583E0FFE52E2524F58210");
            VendAX.Add(":1006DD00E52D3523F583EFF0E4FAF9F8E52424014F");
            VendAX.Add(":1006ED00F524EA3523F523E93522F522E83521F500");
            VendAX.Add(":1006FD002180B3852D0A852E0B74002480FF740A8A");
            VendAX.Add(":10070D0034FFFEC3E5139FF513E5129EF512C3E505");
            VendAX.Add(":10071D000D9FF50DE50C9EF50CC3E50F9FF50FE54F");
            VendAX.Add(":10072D000E9EF50EC3E5099FF509E5089EF508C374");
            VendAX.Add(":10073D00E5119FF511E5109EF510C3E5159FF51513");
            VendAX.Add(":10074D00E5149EF514D2E843D82090E668E04409FC");
            VendAX.Add(":10075D00F090E65CE0443DF0D2AF90E680E054F7D7");
            VendAX.Add(":10076D00F0538EF8C2031207FF30010512039AC22F");
            VendAX.Add(":10077D00013003F2120D6450EDC203120C0D200076");
            VendAX.Add(":10078D001690E682E030E704E020E1EF90E682E0AB");
            VendAX.Add(":0F079D0030E604E020E0E4120BB6120D6680C7D0");
            VendAX.Add(":0107AC00222A");
            VendAX.Add(":0B0D330090E50DE030E402C322D32263");
            VendAX.Add(":10039A0090E6B9E0700302048514700302052E2466");
            VendAX.Add(":1003AA00FE70030205C424FB700302047F14700369");
            VendAX.Add(":1003BA0002047914700302046D1470030204732496");
            VendAX.Add(":1003CA00056003020639120D68400302064590E6ED");
            VendAX.Add(":1003DA00BBE024FE603B14605624FD6016146040A6");
            VendAX.Add(":1003EA0024067075E50A90E6B3F0E50B90E6B4F0E2");
            VendAX.Add(":1003FA00020645120D33500FE51290E6B3F0E513ED");
            VendAX.Add(":10040A0090E6B4F002064590E6A0E04401F0020648");
            VendAX.Add(":10041A0045E50C90E6B3F0E50D90E6B4F00206452A");
            VendAX.Add(":10042A00E50E90E6B3F0E50F90E6B4F002064590CB");
            VendAX.Add(":10043A00E6BAE0FF120BE2AA06A9077B01EA494BDA");
            VendAX.Add(":10044A00600DEE90E6B3F0EF90E6B4F00206459048");
            VendAX.Add(":10045A00E6A0E04401F002064590E6A0E04401F07F");
            VendAX.Add(":10046A00020645120CF1020645120D50020645120B");
            VendAX.Add(":10047A000D48020645120CDF020645120D6A4003BA");
            VendAX.Add(":10048A0002064590E6B8E0247F602B14603C240203");
            VendAX.Add(":10049A006003020524A200E433FF25E0FFA202E480");
            VendAX.Add(":1004AA00334F90E740F0E4A3F090E68AF090E68BB1");
            VendAX.Add(":1004BA007402F0020645E490E740F0A3F090E68A61");
            VendAX.Add(":1004CA00F090E68B7402F002064590E6BCE0547E9A");
            VendAX.Add(":1004DA00FF7E00E0D3948040067C007D0180047C8E");
            VendAX.Add(":1004EA00007D00EC4EFEED4F243EF582740D3EF584");
            VendAX.Add(":1004FA0083E493FF3395E0FEEF24A1FFEE34E68F09");
            VendAX.Add(":10050A0082F583E0540190E740F0E4A3F090E68A94");
            VendAX.Add(":10051A00F090E68B7402F002064590E6A0E04401F2");
            VendAX.Add(":10052A00F0020645120D6C400302064590E6B8E05B");
            VendAX.Add(":10053A0024FE601D2402600302064590E6BAE0B478");
            VendAX.Add(":10054A000105C20002064590E6A0E04401F0020659");
            VendAX.Add(":10055A004590E6BAE0705990E6BCE0547EFF7E0012");
            VendAX.Add(":10056A00E0D3948040067C007D0180047C007D00FD");
            VendAX.Add(":10057A00EC4EFEED4F243EF582740D3EF583E49376");
            VendAX.Add(":10058A00FF3395E0FEEF24A1FFEE34E68F82F58378");
            VendAX.Add(":10059A00E054FEF090E6BCE05480FF131313541F9E");
            VendAX.Add(":1005AA00FFE0540F2F90E683F0E04420F002064566");
            VendAX.Add(":1005BA0090E6A0E04401F0020645120D6E507C90D0");
            VendAX.Add(":1005CA00E6B8E024FE60202402705B90E6BAE0B44C");
            VendAX.Add(":1005DA000104D200806590E6BAE06402605D90E6AC");
            VendAX.Add(":1005EA00A0E04401F0805490E6BCE0547EFF7E0017");
            VendAX.Add(":1005FA00E0D3948040067C007D0180047C007D006D");
            VendAX.Add(":10060A00EC4EFEED4F243EF582740D3EF583E493E5");
            VendAX.Add(":10061A00FF3395E0FEEF24A1FFEE34E68F82F583E7");
            VendAX.Add(":10062A00E04401F0801590E6A0E04401F0800C124D");
            VendAX.Add(":10063A000080500790E6A0E04401F090E6A0E04474");
            VendAX.Add(":02064A0080F03E");
            VendAX.Add(":01064C00228B");
            VendAX.Add(":03003300020D605B");
            VendAX.Add(":040D600053D8EF3243");
            VendAX.Add(":100C6C00D200E4F51A90E678E05410FFC4540F4417");
            VendAX.Add(":090C7C0050F51713E433F51922B9");
            VendAX.Add(":0107FF0022D7");
            VendAX.Add(":020D6400D32298");
            VendAX.Add(":020D6600D32296");
            VendAX.Add(":020D6800D32294");
            VendAX.Add(":080D480090E6BAE0F518D32291");
            VendAX.Add(":100CDF0090E740E518F0E490E68AF090E68B04F098");
            VendAX.Add(":020CEF00D3220E");
            VendAX.Add(":080D500090E6BAE0F516D3228B");
            VendAX.Add(":100CF10090E740E516F0E490E68AF090E68B04F088");
            VendAX.Add(":020D0100D322FB");
            VendAX.Add(":020D6A00D32292");
            VendAX.Add(":020D6C00D32290");
            VendAX.Add(":020D6E00D3228E");
            VendAX.Add(":1000800090E6B9E0245EB40B0040030203989000B0");
            VendAX.Add(":100090009C75F003A4C58325F0C583730201920209");
            VendAX.Add(":1000A000019202010D0200BD0200D70200F302011D");
            VendAX.Add(":1000B0003C02018C02011602012902016290E74014");
            VendAX.Add(":1000C000E519F0E490E68AF090E68B04F090E6A063");
            VendAX.Add(":1000D000E04480F002039890E60AE090E740F0E404");
            VendAX.Add(":1000E00090E68AF090E68B04F090E6A0E04480F081");
            VendAX.Add(":1000F00002039890E740740FF0E490E68AF090E6EF");
            VendAX.Add(":100100008B04F090E6A0E04480F002039890E6BAF9");
            VendAX.Add(":10011000E0F51702039890E67AE054FEF0E490E6EA");
            VendAX.Add(":100120008AF090E68BF002039890E67AE04401F0C2");
            VendAX.Add(":10013000E490E68AF090E68BF002039890E7407432");
            VendAX.Add(":1001400007F0E490E68AF090E68B04F090E6A0E0F9");
            VendAX.Add(":100150004480F07FE87E031207ADD204120B8702C1");
            VendAX.Add(":10016000039890E6B5E054FEF090E6BFE090E68A92");
            VendAX.Add(":10017000F090E6BEE090E68BF090E6BBE090E6B350");
            VendAX.Add(":10018000F090E6BAE090E6B4F002039875190143E6");
            VendAX.Add(":10019000170190E6BAE0753100F532A3E0FEE4EE17");
            VendAX.Add(":1001A000423190E6BEE0753300F534A3E0FEE4EEA4");
            VendAX.Add(":1001B000423390E6B8E064C06003020282E5344551");
            VendAX.Add(":1001C00033700302039890E6A0E020E1F9C3E53420");
            VendAX.Add(":1001D0009440E533940050088533358534368006E5");
            VendAX.Add(":1001E00075350075364090E6B9E0B4A335E4F537CF");
            VendAX.Add(":1001F000F538C3E5389536E53795355060E5322555");
            VendAX.Add(":1002000038F582E5313537F583E0FF74402538F560");
            VendAX.Add(":1002100082E434E7F583EFF00538E53870020537FE");
            VendAX.Add(":1002200080D0E4F537F538C3E5389536E5379535B0");
            VendAX.Add(":10023000501874402538F582E434E7F58374CDF026");
            VendAX.Add(":100240000538E5387002053780DDAD367AE779404C");
            VendAX.Add(":100250007EE77F40AB07AF32AE311208B8E490E6DC");
            VendAX.Add(":100260008AF090E68BE536F02532F532E53535310A");
            VendAX.Add(":10027000F531C3E5349536F534E5339535F533027C");
            VendAX.Add(":1002800001BD90E6B8E064406003020398E51A708F");
            VendAX.Add(":10029000051209678F1AE53445337003020398E4A9");
            VendAX.Add(":1002A00090E68AF090E68BF090E6A0E020E1F990ED");
            VendAX.Add(":1002B000E68BE0753500F53690E6B9E0B4A338E496");
            VendAX.Add(":1002C000F537F538C3E5389536E5379535400302FF");
            VendAX.Add(":1002D000037C74402538F582E434E7F583E0FFE5DC");
            VendAX.Add(":1002E000322538F582E5313537F583EFF00538E50D");
            VendAX.Add(":1002F000387002053780CDE4F537F538C3E5389519");
            VendAX.Add(":0703000036E5379535507515");
            VendAX.Add(":10030700851A39E51A64016044E5322538FFE5317D");
            VendAX.Add(":100317003537FEE51A24FFFDE434FF5EFEEF5D4E40");
            VendAX.Add(":100327006010E5322538FFE51A145FFFC3E51A9F11");
            VendAX.Add(":10033700F539C3E5369538FFE5359537FEC3EF95B3");
            VendAX.Add(":1003470039EE94005007C3E5369538F539E532257F");
            VendAX.Add(":1003570038FFE5313537FE74402538F582E434E758");
            VendAX.Add(":10036700AD82FCAB39120A9CE5392538F538E435FE");
            VendAX.Add(":0303770037F53720");
            VendAX.Add(":10037A008080E5362532F532E5353531F531C3E58C");
            VendAX.Add(":0F038A00349536F534E5339535F533020296C3D5");
            VendAX.Add(":010399002241");
            VendAX.Add(":100C3200C0E0C083C08290E6B5E04401F0D2015327");
            VendAX.Add(":0F0C420091EF90E65D7401F0D082D083D0E03264");
            VendAX.Add(":100C9D00C0E0C083C0825391EF90E65D7404F0D044");
            VendAX.Add(":060CAD0082D083D0E0328A");
            VendAX.Add(":100CB300C0E0C083C0825391EF90E65D7402F0D030");
            VendAX.Add(":060CC30082D083D0E03274");
            VendAX.Add(":100B1900C0E0C083C08290E680E030E70E85080C13");
            VendAX.Add(":100B290085090D85100E85110F800C85100C851116");
            VendAX.Add(":100B39000D85080E85090F5391EF90E65D7410F04D");
            VendAX.Add(":070B4900D082D083D0E0321E");
            VendAX.Add(":100C8500C0E0C083C082D2035391EF90E65D740843");
            VendAX.Add(":080C9500F0D082D083D0E032E0");
            VendAX.Add(":100B5000C0E0C083C08290E680E030E70E85080CDC");
            VendAX.Add(":100B600085090D85100E85110F800C85100C8511DF");
            VendAX.Add(":100B70000D85080E85090F5391EF90E65D7420F006");
            VendAX.Add(":070B8000D082D083D0E032E7");
            VendAX.Add(":0109FF0032C5");
            VendAX.Add(":010D70003250");
            VendAX.Add(":010D7100324F");
            VendAX.Add(":010D7200324E");
            VendAX.Add(":010D7300324D");
            VendAX.Add(":010D7400324C");
            VendAX.Add(":010D7500324B");
            VendAX.Add(":010D7600324A");
            VendAX.Add(":010D77003249");
            VendAX.Add(":010D78003248");
            VendAX.Add(":010D79003247");
            VendAX.Add(":010D7A003246");
            VendAX.Add(":010D7B003245");
            VendAX.Add(":010D7C003244");
            VendAX.Add(":010D7D003243");
            VendAX.Add(":010D7E003242");
            VendAX.Add(":010D7F003241");
            VendAX.Add(":010D80003240");
            VendAX.Add(":010D8100323F");
            VendAX.Add(":010D8200323E");
            VendAX.Add(":010D8300323D");
            VendAX.Add(":010D8400323C");
            VendAX.Add(":010D8500323B");
            VendAX.Add(":010D8600323A");
            VendAX.Add(":010D87003239");
            VendAX.Add(":010D88003238");
            VendAX.Add(":010D89003237");
            VendAX.Add(":010D8A003236");
            VendAX.Add(":010D8B003235");
            VendAX.Add(":010D8C003234");
            VendAX.Add(":010D8D003233");
            VendAX.Add(":010D8E003232");
            VendAX.Add(":010D8F003231");
            VendAX.Add(":010D90003230");
            VendAX.Add(":010D9100322F");
            VendAX.Add(":010D9200322E");
            VendAX.Add(":100A00001201000200000040B404041000000102C2");
            VendAX.Add(":100A100000010A06000200000040010009022E0049");
            VendAX.Add(":100A200001010080320904000004FF0000000705F6");
            VendAX.Add(":100A30000202000200070504020002000705860208");
            VendAX.Add(":100A40000002000705880200020009022E000101D1");
            VendAX.Add(":100A50000080320904000004FF00000007050202C4");
            VendAX.Add(":100A60004000000705040240000007058602400020");
            VendAX.Add(":100A70000007058802400000040309041003430036");
            VendAX.Add(":100A80007900700072006500730073000E0345006A");
            VendAX.Add(":0C0A90005A002D005500530042000000E9");
            VendAX.Add(":100BB60090E682E030E004E020E60B90E682E0304A");
            VendAX.Add(":100BC600E119E030E71590E680E04401F07F147EFD");
            VendAX.Add(":0C0BD600001207AD90E680E054FEF02213");
            VendAX.Add(":100B870030040990E680E0440AF0800790E680E0B0");
            VendAX.Add(":100B97004408F07FDC7E051207AD90E65D74FFF038");
            VendAX.Add(":0F0BA70090E65FF05391EF90E680E054F7F02274");
            VendAX.Add(":1007AD008E3A8F3B90E600E054187012E53B240121");
            VendAX.Add(":1007BD00FFE4353AC313F53AEF13F53B801590E698");
            VendAX.Add(":1007CD0000E05418FFBF100BE53B25E0F53BE53A83");
            VendAX.Add(":1007DD0033F53AE53B153BAE3A7002153A4E6005DE");
            VendAX.Add(":0607ED00120C2180EE2237");
            VendAX.Add(":020BE200A90761");
            VendAX.Add(":100BE400AE14AF158F828E83A3E064037017AD013A");
            VendAX.Add(":100BF40019ED7001228F828E83E07C002FFDEC3E84");
            VendAX.Add(":080C0400FEAF0580DFE4FEFFF6");
            VendAX.Add(":010C0C0022C5");
            VendAX.Add(":100C0D0090E682E044C0F090E681F0438701000059");
            VendAX.Add(":040C1D0000000022B1");
            VendAX.Add(":100C21007400F58690FDA57C05A3E582458370F9E6");
            VendAX.Add(":010C310022A0");
            VendAX.Add(":03004300020800B0");
            VendAX.Add(":03005300020800A0");
            VendAX.Add(":10080000020C3200020CB300020C9D00020C8500A9");
            VendAX.Add(":10081000020B1900020B50000209FF00020D7000CC");
            VendAX.Add(":10082000020D7100020D7200020D7300020D7400C2");
            VendAX.Add(":10083000020D7500020D7600020D7700020D7800A2");
            VendAX.Add(":10084000020D7900020D7000020D7A00020D7B008E");
            VendAX.Add(":10085000020D7C00020D7D00020D7E00020D7F0066");
            VendAX.Add(":10086000020D8000020D7000020D7000020D70007C");
            VendAX.Add(":10087000020D8100020D8200020D8300020D840032");
            VendAX.Add(":10088000020D8500020D8600020D8700020D880012");
            VendAX.Add(":10089000020D8900020D8A00020D8B00020D8C00F2");
            VendAX.Add(":1008A000020D8D00020D8E00020D8F00020D9000D2");
            VendAX.Add(":0808B000020D9100020D9200FF");
            VendAX.Add(":0A0A9C008E3C8F3D8C3E8D3F8B4059");
            VendAX.Add(":100AA600C28743B280120D58120D24120CC950048D");
            VendAX.Add(":100AB600D2048059E519600FE53C90E679F0120CF6");
            VendAX.Add(":100AC600C95004D2048046E53D90E679F0120CC97F");
            VendAX.Add(":100AD6005004D2048037E4F541E541C395405021E6");
            VendAX.Add(":100AE600053FE53FAE3E7002053E14F5828E83E07B");
            VendAX.Add(":100AF60090E679F0120D145004D20480100541805E");
            VendAX.Add(":100B0600D890E678E04440F0120C51C20453B27F0C");
            VendAX.Add(":020B1600A20437");
            VendAX.Add(":010B180022BA");
            VendAX.Add(":0F0D240090E6787480F0E51725E090E679F022EC");
            VendAX.Add(":100C5100120D58120D24120D1490E678E04440F064");
            VendAX.Add(":0A0C6100120D5890E678E030E1E94A");
            VendAX.Add(":010C6B002266");
            VendAX.Add(":080D580090E678E020E6F922A4");
            VendAX.Add(":0A08B8008E3C8F3D8D3E8A3F8B4041");
            VendAX.Add(":1008C200120D58120D24120CC9500122E519600CA8");
            VendAX.Add(":1008D200E53C90E679F0120CC9500122E53D90E624");
            VendAX.Add(":1008E20079F0120CC950012290E6787480F0E51775");
            VendAX.Add(":1008F20025E0440190E679F0120D1450012290E6B1");
            VendAX.Add(":1009020079E0F541120D14500122E4F541E53E145F");
            VendAX.Add(":10091200FFE541C39F501C90E679E0FFE540254189");
            VendAX.Add(":10092200F582E4353FF583EFF0120D1450012205F4");
            VendAX.Add(":100932004180DA90E6787420F0120D145001229072");
            VendAX.Add(":10094200E679E0FFE5402541F582E4353FF583EFA6");
            VendAX.Add(":10095200F0120D1450012290E6787440F090E6797E");
            VendAX.Add(":04096200E0F541C3B8");
            VendAX.Add(":01096600226E");
            VendAX.Add(":0F0D140090E678E0FF30E0F8EF30E202D322C340");
            VendAX.Add(":010D230022AD");
            VendAX.Add(":100CC90090E678E0FF30E0F8EF30E202D322EF203F");
            VendAX.Add(":050CD900E102D322C37B");
            VendAX.Add(":010CDE0022F3");
            VendAX.Add(":10096700E51970037F01227A107B407D40E4FFFE8A");
            VendAX.Add(":100977001208B8E4F53A7400253AF582E43410F524");
            VendAX.Add(":1009870083E53AF0053AE53AB440EB7C107D007B0D");
            VendAX.Add(":1009970040E4FFFE120A9CE4F53AE53AF4FF7400DE");
            VendAX.Add(":1009A700253AF582E43410F583EFF0053AE53AB4D9");
            VendAX.Add(":1009B70040E87A107B007D40E4FFFE1208B89010F3");
            VendAX.Add(":1009C70000E0F53AE53A30E005753B018008633A07");
            VendAX.Add(":1009D7003F053A853A3BE4F53AE53AC3944050156A");
            VendAX.Add(":1009E700AF3A7E007C107D40AB3B120A9CE53B256D");
            VendAX.Add(":0709F7003AF53A80E4AF3B42");
            VendAX.Add(":0109FE0022D6");
            VendAX.Add(":030000000207F301");
            VendAX.Add(":0C07F300787FE4F6D8FD75814102064DC8");
            VendAX.Add(":100D0300EB9FF5F0EA9E42F0E99D42F0E89C45F046");
            VendAX.Add(":010D130022BD");
            VendAX.Add(":00000001FF");
        }

    }

}
