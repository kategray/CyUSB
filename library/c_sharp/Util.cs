/*
 ## Cypress CyUSB C# library source file (Util.cs)
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
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Collections;

namespace CyUSB
{
    /// <summary>
    /// Summary description for Util.
    /// </summary>
    public static class Util
    {
        private static ushort _MAX_FW_SIZE = 0xFFFF; // 64KB 

        public static ushort MaxFwSize
        {
            get { return _MAX_FW_SIZE; }
            set { _MAX_FW_SIZE = value; }
        }

        public static int ReverseBytes(byte[] dta, int xStart, int bytes)
        {
            if (bytes < 0) return 0;

            byte[] tmp = new byte[bytes];

            for (byte i = 0; i < bytes; i++) tmp[i] = dta[xStart + bytes - i - 1];	// Reverse
            for (byte i = 0; i < bytes; i++) dta[xStart + i] = tmp[i];			// Copy back

            int v = (bytes > 2) ? BitConverter.ToInt32(tmp, 0) : BitConverter.ToInt16(tmp, 0);

            return v;
        }

        public unsafe static int ReverseBytes(byte* dta, int bytes)
        {
            if (bytes < 0) return 0;

            byte[] tmp = new byte[bytes];

            for (byte i = 0; i < bytes; i++) tmp[i] = *(dta + bytes - i - 1);			// Reverse
            for (byte i = 0; i < bytes; i++) *(dta + i) = tmp[i];					// Copy back

            int v = (bytes > 2) ? BitConverter.ToInt32(tmp, 0) : BitConverter.ToInt16(tmp, 0);

            return v;
        }

        public static ulong HexToInt(String hexString)
        {
            string HexChars = "0123456789abcdef";

            string s = hexString.ToLower();

            // Trim off the 0x prefix
            if (s.Length > 2)
                if (s.Substring(0, 2).Equals("0x"))
                    s = s.Substring(2, s.Length - 2);


            string _s = "";
            int len = s.Length;

            // Reverse the digits
            for (int i = len - 1; i >= 0; i--) _s += s[i];

            ulong sum = 0;
            ulong pwrF = 1;
            for (int i = 0; i < len; i++)
            {
                uint ordinal = (uint)HexChars.IndexOf(_s[i]);
                sum += (i == 0) ? ordinal : pwrF * ordinal;
                pwrF *= 16;
            }


            return sum;
        }

        public static string Assemblies
        {
            get
            {
                // Get all the assemblies currently loaded in the application domain.
                Assembly[] myAssemblies = Thread.GetDomain().GetAssemblies();

                string assemblyList = "";// "Assemblies\r\n----------\r\n\n";

                foreach (Assembly a in myAssemblies)
                {
                    string assName = a.GetName().Name;
                    string assVer = a.GetName().Version.ToString();

                    int a1 = assName.Length;
                    int a2 = assVer.Length;

                    if ((assName.IndexOf("System") == -1) &&
        (assName.IndexOf("mscorlib") == -1) &&
        (assName.IndexOf("Microsoft") == -1) &&
        (assName.IndexOf("vshost") == -1))
                        assemblyList += string.Format("Assembly:  {0}  ({1})\r\n", assName, assVer);
                }

                return assemblyList;
            }

        }


        public static bool ParseHexFile(String fName, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            if (!File.Exists(fName)) return false;

            ArrayList rawList = new ArrayList();

            string line;

            StreamReader srcStream = new StreamReader(fName);
            if (srcStream == null) return false;
            while ((line = srcStream.ReadLine()) != null)
                rawList.Add(line);

            srcStream.Close();
            return ParseHexData(rawList, FwBuf, ref FwLen, ref FwOff);

        }


        public static bool ParseHexData(ArrayList rawList, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            string line, tmp;
            int v;

            // Delete non-data records
            for (int i = rawList.Count - 1; i >= 0; i--)
            {
                line = (string)rawList[i];
                if (line.Length > 0)
                {
                    tmp = line.Substring(7, 2);   // Get the Record Type into v
                    v = (int)Util.HexToInt(tmp);
                    if (v != 0) rawList.Remove(rawList[i]);   // Data records are type == 0
                }
            }

            FwLen = 0;
            FwOff = _MAX_FW_SIZE;

            // Initialize the FwImage[] buffer
            for (int i = 0; i < _MAX_FW_SIZE; i++) FwBuf[i] = 0xFF;

            // Extract the FW data bytes, placing into location of FwImage indicated
            for (int i = 0; i < rawList.Count; i++)
            {
                line = (string)rawList[i];

                // Remove comments
                v = line.IndexOf("//");
                if (v > -1)
                    line = line.Substring(0, v - 1);

                // Build string that just contains the offset followed by the data bytes
                if (line.Length > 0)
                {
                    // Get the offset
                    string sOffset = line.Substring(3, 4);
                    ushort dx = (ushort)Util.HexToInt(sOffset);
                    if (dx >= _MAX_FW_SIZE) return false;

                    if (dx < FwOff) FwOff = dx;

                    // Get the string of data chars
                    tmp = line.Substring(1, 2);
                    v = (int)Util.HexToInt(tmp) * 2;
                    string s = line.Substring(9, v);

                    int bytes = v / 2;

                    for (int b = 0; b < bytes; b++, dx++)
                        FwBuf[dx] = (byte)Util.HexToInt(s.Substring((b * 2), 2));

                    if (dx > FwLen) FwLen = dx;
                }

            }

            return true;
        }


        public static unsafe bool ParseIICFile(String fName, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            if (!File.Exists(fName)) return false;

            // Suck-in the data from the .iic file
            Stream fStream = new FileStream(fName, FileMode.Open, FileAccess.Read);
            if (fStream == null) return false;
            int fSize = (int)fStream.Length;
            byte[] fData = new byte[fSize];
            fStream.Read(fData, 0, fSize);
            fStream.Close();

            if (fSize > _MAX_FW_SIZE) return false;

            ParseIICData(fData, FwBuf, ref FwLen, ref FwOff);

            return true;
        }


        public static unsafe void ParseIICData(byte[] fData, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            ushort dx = 8;
            FwLen = 0;
            FwOff = _MAX_FW_SIZE;

            for (int i = 0; i < _MAX_FW_SIZE; i++) FwBuf[i] = 0xFF;

            fixed (byte* buf = fData)
            {
                ushort* dLen;
                ushort* addr;

                do
                {
                    Util.ReverseBytes(fData, dx, 2);
                    Util.ReverseBytes(fData, dx + 2, 2);
                    dLen = (ushort*)(buf + dx);
                    addr = (ushort*)(buf + dx + 2);

                    if ((*dLen) != 0x8001)
                    {
                        Array.Copy(fData, dx + 4, FwBuf, *addr, *dLen);
                        ushort lastDta = (ushort)(*addr + *dLen);
                        if (lastDta > FwLen) FwLen = lastDta;
                        if (*addr < FwOff) FwOff = *addr;
                    }

                    dx += (ushort)(*dLen + 4);
                } while (((*dLen) != 0x8001) && (dx < fData.Length));
            }

        }

        public static string byteStr(ushort val)
        {
            byte b1 = (byte)((val >> 8) & 0x00FF);
            byte b2 = (byte)(val & 0x00FF);
            return string.Format("{0:X2} {1:X2}", b1, b2);
        }



    }


}
