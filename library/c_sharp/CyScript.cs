/*
 ## Cypress CyUSB C# library source file (CyScript.cs)
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
    public static class TTLock
    {
        static readonly Object thisLock = new Object();        
        public static Object GlobalWriteLock
        {
            get
            {
                return thisLock;
            }
        }
    }
    
    public class TTransaction
    {
        public const byte ReqType_DIR_MASK = 0x80;
        public const byte ReqType_TYPE_MASK = 0x60;
        public const byte ReqType_TGT_MASK = 0x03;
        public const byte TotalHeaderSize = 32;

        public uint Signature; //4 
        public uint RecordSize; //8
        public ushort HeaderSize; //10
        public byte Tag; //11
        public byte ConfigNum; //12
        public byte IntfcNum; //13
        public byte AltIntfc; //14
        public byte EndPtAddr; //15

        public byte bReqType; //16 //EP0 Xfer
        public byte CtlReqCode; //17  //EP0 Xfer 
        public byte reserved0; //18 

        public ushort wValue; //20
        public ushort wIndex; //22
        public byte reserved1; //23 
        public byte reserved2; //24

        public uint Timeout; //28
        public uint DataLen; //32        

        public TTransaction()
        {
            this.Signature = 0x54505343;
            this.HeaderSize = TotalHeaderSize;

            this.ConfigNum = 0;
            this.IntfcNum = 0;
            this.AltIntfc = 0;
            this.EndPtAddr = 0;

            this.Tag = 0;
            this.bReqType = 0;
           
            //this.Target = 0x00;//TGT_DEVICE
            //this.ReqType = 0x40;//REQ_VENDOR
            //this.Direction = 0x00; //DIR_TO_DEVICE           
        }

        public void WriteToStream(FileStream f)
        {
            lock (TTLock.GlobalWriteLock)
            {
                BinaryWriter wr = new BinaryWriter(f);
                wr.Write(this.Signature);
                wr.Write(this.RecordSize);
                wr.Write(this.HeaderSize);
                wr.Write(this.Tag);
                wr.Write(this.ConfigNum);
                wr.Write(this.IntfcNum);
                wr.Write(this.AltIntfc);
                wr.Write(this.EndPtAddr);
                wr.Write(this.bReqType);
                wr.Write(this.CtlReqCode);
                wr.Write(this.reserved0);
                wr.Write(this.wValue);
                wr.Write(this.wIndex);
                wr.Write(this.reserved1);
                wr.Write(this.reserved2);
                wr.Write(this.Timeout);
                wr.Write(this.DataLen);
                Thread.Sleep(0);

            }

        }

        public void ReadFromStream(FileStream f)
        {
            lock (TTLock.GlobalWriteLock)
            {
                BinaryReader rd = new BinaryReader(f);

                this.Signature = rd.ReadUInt32();
                this.RecordSize = rd.ReadUInt32();
                this.HeaderSize = rd.ReadUInt16();
                this.Tag = rd.ReadByte();
                this.ConfigNum = rd.ReadByte();
                this.IntfcNum = rd.ReadByte();
                this.AltIntfc = rd.ReadByte();
                this.EndPtAddr = rd.ReadByte();
                this.bReqType = rd.ReadByte();
                this.CtlReqCode = rd.ReadByte();
                this.reserved0 = rd.ReadByte();
                this.wValue = rd.ReadUInt16();
                this.wIndex = rd.ReadUInt16();
                this.reserved1 = rd.ReadByte();
                this.reserved2 = rd.ReadByte();
                this.Timeout = rd.ReadUInt32();
                this.DataLen = rd.ReadUInt32();
            }
        }

        public void ReadToBuffer(FileStream f, ref byte[] buffer, ref int len)
        {
            if (len > 0)
            {
                lock (TTLock.GlobalWriteLock)
                {
                    BinaryReader rd = new BinaryReader(f);
                    rd.Read(buffer, 0, len);
                }
            }
        }

        public void WriteFromBuffer(FileStream f, ref byte[] buffer, ref int len)
        {
            if (len > 0)
            {
                lock (TTLock.GlobalWriteLock)
                {
                    BinaryWriter wr = new BinaryWriter(f);
                    wr.Write(buffer, 0, len);
                    Thread.Sleep(0);
                }
            }
        }
    }
}
