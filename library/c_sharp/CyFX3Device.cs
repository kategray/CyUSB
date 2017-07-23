/*
 ## Cypress CyUSB C# library source file (CyFX3Device.cs)
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
using System.IO;


namespace CyUSB
{
    public class CyFX3Device : CyUSBDevice
    {
        internal const uint SPI_FLASH_PAGE_SIZE_IN_BYTE = 256;
        internal const uint SPI_FLASH_SECTOR_SIZE_IN_BYTE = (64 * 1024);
        internal const uint CYWB_BL_MAX_BUFFER_SIZE_WHEN_USING_EP0_TRANSPORT = CyConst.CONTROLTFRER_DATA_LENGTH; // (8 * 512); // 4KB
        
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

        internal CyFX3Device() : this(CyConst.CyGuid) { }

        internal CyFX3Device(Guid guid)
            : base(guid)
        {
        }
        public bool IsBootLoaderRunning()
        {
            //Dire : in, Target : Device, ReqCode:0xA0,wValue:0x0000,wIndex:0x0000
            // This function checks for bootloader,it will return false if it is not running.
            byte[] buf = new byte[1];
            uint len = 1;
            return Ep0VendorCommand(ref buf, ref len, true, 0xA0, 0x0000);
        }

        private void CYWB_BL_4_BYTES_COPY(ref uint output, ref byte[] input, ref uint index)
        {

            output = input[index]; // lsb-1
            output |= (uint)(input[index + 1] << 8); //lbs2
            output |= (uint)(input[index + 2] << 16); //lbs2
            output |= (uint)(input[index + 3] << 24); //lbs2

        }
        public System.String GetFwErrorString(FX3_FWDWNLOAD_ERROR_CODE eFwErrorCode)
        {
            switch (eFwErrorCode)
            {
                case FX3_FWDWNLOAD_ERROR_CODE.SUCCESS: return "Succeeded";
                case FX3_FWDWNLOAD_ERROR_CODE.FAILED: return "Failed";
                case FX3_FWDWNLOAD_ERROR_CODE.DEVICE_CREATE_FAILED: return "Device open failed";
                case FX3_FWDWNLOAD_ERROR_CODE.INCORRECT_IMAGE_LENGTH: return "File size does not match";
                case FX3_FWDWNLOAD_ERROR_CODE.INVALID_FILE: return "Invalid input file";
                case FX3_FWDWNLOAD_ERROR_CODE.INVALID_FWSIGNATURE: return "Invalid Firmware Signature";
                case FX3_FWDWNLOAD_ERROR_CODE.INVALID_MEDIA_TYPE: return "Invalid Download type";
                case FX3_FWDWNLOAD_ERROR_CODE.CORRUPT_FIRMWARE_IMAGE_FILE: return "Firmware Image file is Corrupted";
                case FX3_FWDWNLOAD_ERROR_CODE.SPIFLASH_ERASE_FAILED: return "Erase Failed";
                case FX3_FWDWNLOAD_ERROR_CODE.I2CEEPROM_UNKNOWN_I2C_SIZE: return "Unknown I2CE2PROM size, Unknown value parsed from 2nd Bytes of IMG file";
                default: return "Error code not found";
            }
        }
        private bool Ep0VendorCommand(ref byte[] buf, ref uint buflen, bool IsFromDevice, byte ReqCode, uint Value)
        {
            TTransaction m_Xaction = new TTransaction();
            ControlEndPt.TimeOut = 5000;
            ControlEndPt.Target = CyConst.TGT_DEVICE;
            ControlEndPt.ReqType = CyConst.REQ_VENDOR;
            if (IsFromDevice)
                ControlEndPt.Direction = CyConst.DIR_FROM_DEVICE;
            else
                ControlEndPt.Direction = CyConst.DIR_TO_DEVICE;
            ControlEndPt.ReqCode = ReqCode;
            ControlEndPt.Value = (ushort)(Value & 0x0000FFFF); // Get 16-bit LSB
            ControlEndPt.Index = (ushort)(Value >> 16);        // Get 16-bit MSB
            int len = (int)buflen;

            // Handle the case where transfer length is 0 (used to send the Program Entry)
            if (buflen == 0)
            {
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
                    m_Xaction.WriteFromBuffer(m_script_file_name, ref buf, ref len);
                }
                return ControlEndPt.XferData(ref buf, ref len);
            }

            else
            {
                bool bRetCode = false;
                int Stagelen = 0;
                int BufIndex = 0;
                while (len > 0)
                {
                    if (len >= 65535)
                        Stagelen = 65535;
                    else
                        Stagelen = (len) % 65535;

                    // Allocate the buffer
                    byte[] StageBuf = new byte[Stagelen];
                    if (!IsFromDevice)
                    {//write operation
                        for (int i = 0; i < Stagelen; i++)
                            StageBuf[i] = buf[BufIndex + i];
                    }

                    bRetCode = ControlEndPt.XferData(ref  StageBuf, ref Stagelen);
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
                        m_Xaction.WriteFromBuffer(m_script_file_name, ref buf, ref len);
                    }
                    if (!bRetCode)
                        return false;

                    if (IsFromDevice)
                    {//read operation
                        for (int i = 0; i < Stagelen; i++)
                            buf[BufIndex + i] = StageBuf[i];
                    }

                    len -= Stagelen;
                    BufIndex += Stagelen;
                }

            }
            return true;
        }
        private bool DownloadBufferToDevice(ref byte[] buf, ref uint buflen, uint DownloadAddress)
        {
            byte ReqCode = 0xA0;
            return Ep0VendorCommand(ref buf, ref buflen, false, ReqCode, DownloadAddress);
        }
        internal bool UploadBufferFromDevice(ref byte[] buf, ref uint buflen, uint UploadAddress)
        {
            byte ReqCode = 0xA0;
            return Ep0VendorCommand(ref buf, ref buflen, true, ReqCode, UploadAddress);
        }

        public FX3_FWDWNLOAD_ERROR_CODE DownloadFw(string filename, FX3_FWDWNLOAD_MEDIA_TYPE enMediaType)
        {
            uint fwSize = 0;
            if (filename.Equals("")) return FX3_FWDWNLOAD_ERROR_CODE.INVALID_FILE;

            // Suck-in the data from the .iic file
            Stream fStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            if (fStream == null) return FX3_FWDWNLOAD_ERROR_CODE.INVALID_FILE;
            fwSize = (uint)fStream.Length;

            if (fwSize <= 0)
            {
                fStream.Close();
                return FX3_FWDWNLOAD_ERROR_CODE.INVALID_FILE;
            }

            //allocate the memory to hold the firmware image
            byte[] FwImage = new byte[fwSize];

            fStream.Read(FwImage, 0, (int)fwSize);
            fStream.Close();
            // call api to download the image
            if (enMediaType == FX3_FWDWNLOAD_MEDIA_TYPE.RAM)
                return DownloadFwToRam(ref FwImage, ref fwSize);
            else if (enMediaType == FX3_FWDWNLOAD_MEDIA_TYPE.I2CE2PROM)
                    return DownloadUserIMGtoI2CE2PROM(ref FwImage, ref fwSize);
            else if (enMediaType == FX3_FWDWNLOAD_MEDIA_TYPE.SPIFLASH)
                return DownloadUserIMGtoSPIFLASH(ref FwImage, ref fwSize);
            else
                return FX3_FWDWNLOAD_ERROR_CODE.INVALID_MEDIA_TYPE;
        }

        unsafe internal FX3_FWDWNLOAD_ERROR_CODE DownloadFwToRam(ref byte[] buf, ref uint buflen)
        {
            const int BUFSIZE_UPORT = CyConst.CONTROLTFRER_DATA_LENGTH;

            byte[] downloadbuf = new byte[BUFSIZE_UPORT];
            byte[] uploadbuf = new byte[BUFSIZE_UPORT];
            uint ComputeCheckSum = 0;
            uint ExpectedCheckSum = 0;
            uint SectionLength = 0;
            uint SectionAddress = 0;
            uint DownloadAddress = 0;
            uint ProgramEntry = 0;
            uint FwImagePtr = 0;
            bool usbSuspendTestRequired = false;
            // Initialize computed checksum
            ComputeCheckSum = 0;
            // Check "CY" signature (0x43,0x59) and download the firmware image	        
            if ((buf[FwImagePtr] != 0x43) || (buf[FwImagePtr + 1] != 0x59))
            {// signature doesn't match		
                return FX3_FWDWNLOAD_ERROR_CODE.INVALID_FWSIGNATURE;
            }

            // Skip the two bytes signature and the following two bytes
            FwImagePtr += 4;
            // Download one section at a time to the device, compute checksum, and upload-verify it
            bool executeUsbSuspendTest = usbSuspendTestRequired;
            bool isTrue = true;
            while (isTrue)
            {
                SectionLength = 0;
                // Get section length (4 bytes) and convert it from byte arrya to 32-bit word count
                CYWB_BL_4_BYTES_COPY(ref SectionLength, ref buf, ref FwImagePtr);
                FwImagePtr += 4;
                SectionLength = SectionLength << 2;

                // If SectionLength = 0, the transfer is complete
                if (SectionLength == 0) break;

                // Get section address (4 bytes)
                CYWB_BL_4_BYTES_COPY(ref SectionAddress, ref buf, ref FwImagePtr);
                FwImagePtr += 4;
                // Download and upload-verify SSV_BUFFER_SIZE_FOR_DOWNLOAD_FROM_UPORT maximum bytes at a time
                uint bytesLeftToDownload = SectionLength;
                DownloadAddress = SectionAddress;

                // The FPGA does not seem to always be reliable: if data read back do not match data written try again once
                while (bytesLeftToDownload > 0)
                {
                    uint bytesToTransfer = BUFSIZE_UPORT;
                    if (bytesLeftToDownload < BUFSIZE_UPORT)
                        bytesToTransfer = bytesLeftToDownload;

                    //sanity check for incomplete fw with valid signatures. 
                    //Note: bytesToTransfer should never be greater then fw length i.e buflen
                    if (bytesToTransfer > buflen)
                        return FX3_FWDWNLOAD_ERROR_CODE.CORRUPT_FIRMWARE_IMAGE_FILE;

                    for (uint i = 0; i < bytesToTransfer; i++)
                        downloadbuf[i] = buf[FwImagePtr + i];

                    // Compute checksum: Here transferLength is assumed to be a multiple of 4. If it is not, the checksum will fail anyway
                    for (uint index = 0; index < bytesToTransfer; index += 4)
                    {
                        uint buf32bits = 0;
                        CYWB_BL_4_BYTES_COPY(ref buf32bits, ref downloadbuf, ref index);
                        ComputeCheckSum += buf32bits;
                    }
                    // The FPGA does not seem to always be reliable: if an error is encountered, try again twice
                    uint maxTryCount = 3;
                    for (uint tryCount = 1; tryCount <= maxTryCount; tryCount++)
                    {
                        // Download one buffer worth of data to the device                        
                        if (!DownloadBufferToDevice(ref downloadbuf, ref bytesToTransfer, DownloadAddress))
                        {
                            // Check if we exceeded the max try count
                            if (tryCount == maxTryCount)
                            {
                                //LogMessage(LOG_ERROR, 0, "Failure while downloading firmware to the device. Abort");
                                return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                            }
                            else
                            {
                                //LogMessage(LOG_WARNING, 0, " *** F/W buffer download failure. Trying writing/verifying current buffer again... ***");
                                continue;
                            }
                        }

                        // For verification, upload from the device what was just written
                        if (!UploadBufferFromDevice(ref uploadbuf, ref bytesToTransfer, DownloadAddress))
                        {
                            // Check if we exceeded the max try count
                            if (tryCount == maxTryCount)
                            {
                                //LogMessage(LOG_ERROR, 0, "Failure while uploading firmware from the device for verification. Abort");
                                return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                            }
                            else
                            {
                                //LogMessage(LOG_WARNING, 0, " *** F/W buffer upload failure. Trying writing/verifying current buffer again... ***");
                                continue;
                            }
                        }

                        //compare the downloaded and uploaded data, if doesn't match then return error
                        for (int i = 0; i < bytesToTransfer; i++)
                        {
                            if (downloadbuf[i] != uploadbuf[i])
                            {
                                // Check if we exceeded the max try count
                                if (tryCount == maxTryCount)
                                {
                                    //LogMessage(LOG_ERROR, 0, "Uploaded firmware data does not match downloaded data. Abort");
                                    return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                                }
                                else
                                {
                                    //LogMessage(LOG_WARNING, 0, " *** Uploaded data does not match downloaded data. Trying writing/verifying current buffer again... ***");
                                    continue;
                                }
                            }
                        }
                    }

                    if (executeUsbSuspendTest == true)
                    {
                        // We do it ony once before the start of the second transfer (there is usually more than one!)
                        executeUsbSuspendTest = false;
                        bool trySuspendAgain = true;
                        while (trySuspendAgain)
                        {
                            //LogMessage(LOG_INFO, 0, "");
                            //LogMessage(LOG_INFO, 0, " ######### You have 10 seconds to disconnect/reconnect USB cable #########");
                            //LogMessage(LOG_INFO, 0, "");                            
                            System.Threading.Thread.Sleep(10000);

                            // Check if we indeed disconnected the USB cable by uploading the data again: should fail!
                            if (UploadBufferFromDevice(ref uploadbuf, ref bytesToTransfer, DownloadAddress))
                            {
                                //LogMessage(LOG_INFO, 0, " !!!!!!!!!   USB cable apparently not disconnected/reconnected   !!!!!!!!!");
                            }
                            else
                            {
                                trySuspendAgain = false;
                            }
                        }

                        // Close and Re-open access to the device
                        Close();
                        if (Open(0) == false)
                        {
                            //LogMessage(LOG_ERROR, 0, "  Cannot re-open the USB device after a USB Suspend test has been executed");
                            return FX3_FWDWNLOAD_ERROR_CODE.DEVICE_CREATE_FAILED;
                        }
                        // Wait a bit before continuing                        
                        System.Threading.Thread.Sleep(100);

                        // Verify that we have access to the device
                        if (!(UploadBufferFromDevice(ref uploadbuf, ref bytesToTransfer, DownloadAddress)))
                        {
                            //LogMessage(LOG_ERROR, 0, " Could not recover from USB cable disconnect/connect (Manual USB Suspend test)");
                            return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                        }

                        //LogMessage(LOG_INFO, 0, " ######### USB cable disconnect/reconnect successful. Continuing #########");
                        //LogMessage(LOG_INFO, 0, "");
                    }

                    DownloadAddress += (uint)bytesToTransfer;
                    FwImagePtr += bytesToTransfer;
                    bytesLeftToDownload -= bytesToTransfer;
                    // Sanity check
                    if (FwImagePtr > (uint)buflen)
                    {
                        //LogMessage(LOG_ERROR, 0, "Incorrect image data structure: reading beyond the image file boundary");
                        return FX3_FWDWNLOAD_ERROR_CODE.INCORRECT_IMAGE_LENGTH;
                    }
                }
            }
            // Get Program Entry Address(4 bytes)
            CYWB_BL_4_BYTES_COPY(ref ProgramEntry, ref buf, ref FwImagePtr);
            FwImagePtr += 4;

            // Get expected checksum (4 bytes)
            CYWB_BL_4_BYTES_COPY(ref ExpectedCheckSum, ref buf, ref FwImagePtr);
            FwImagePtr += 4;

            // Compare computed checksum against expected value
            if (ComputeCheckSum != ExpectedCheckSum)
            {
                //sstr.str("");
                //sstr << "CheckSum mismatch. Expected=0x" << std::hex << expectedCheckSum << " Computed=0x" << std::hex << computedCheckSum;
                //LogMessage(LOG_ERROR, 0, sstr.str());
            }

            // Transfer execution to Program Entry            
            byte[] dummyBuffer = new byte[1];
            uint len = 0;
            // Some of the xHCI controller have issue with Control In transfer, due to this below request fail. 
            // This request send ProgramEntry.
            //if (DownloadBufferToDevice(ref dummyBuffer, ref len, ProgramEntry) == false)
            //{
            //    //LogMessage(LOG_ERROR, 0, "Downloading Program Entry failed");
            //    return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
            //}

            //NO ERROR CHECK 
            DownloadBufferToDevice(ref dummyBuffer, ref len, ProgramEntry);

            return FX3_FWDWNLOAD_ERROR_CODE.SUCCESS;
        }

        unsafe internal FX3_FWDWNLOAD_ERROR_CODE DownloadUserIMGtoI2CE2PROM(ref byte[] buf, ref uint buflen)
        {
            int STAGE_SIZE = CyConst.CONTROLTFRER_DATA_LENGTH;
            byte[] downloadbuf = new byte[STAGE_SIZE];
            int NoOfStage = ((int)buflen / STAGE_SIZE);
            int LastStage = ((int)buflen % STAGE_SIZE);
            uint DownloadAddress = 0;
            int FwImagePtr = 0;
            int StageSize = STAGE_SIZE;            
            int maxpkt = ControlEndPt.MaxPktSize;
            //Get the I2C addressing size
            byte ImgI2CSizeByte = buf[2]; // the 2nd byte of the IMG file will tell us the I2EPROM internal addressing.                         
            uint AddresingStageSize = 0;
            ImgI2CSizeByte = (byte)((ImgI2CSizeByte >> 1) & 0x07); // Bit3:1 represent the addressing            
            bool IsMicroShipE2Prom = false;

            switch (ImgI2CSizeByte)
            {
                case 0:
                case 1:
                    return FX3_FWDWNLOAD_ERROR_CODE.I2CEEPROM_UNKNOWN_I2C_SIZE;
                case 2:
                    AddresingStageSize = (4 * 1024); // 4KByte
                    break;
                case 3:
                    AddresingStageSize = (8 * 1024); // 8KByte
                    break;
                case 4:
                    AddresingStageSize = (16 * 1024); // 16KByte
                    break;
                case 5:
                    AddresingStageSize = (32 * 1024); // 32KByte
                    break;
                case 6:
                    AddresingStageSize = (64 * 1024); // 64KByte
                    break;
                case 7:
                    IsMicroShipE2Prom = true; // 128KByte Addressing for Microchip.
                    AddresingStageSize = (64 * 1024); // 64KByte // case 7 represent 128Kbyte but it follow 64Kbyte addressing
                    break;                
                default:
                    return FX3_FWDWNLOAD_ERROR_CODE.I2CEEPROM_UNKNOWN_I2C_SIZE;
            }

            ControlEndPt.TimeOut = 5000;
            ControlEndPt.Target = CyConst.TGT_DEVICE;
            ControlEndPt.ReqType = CyConst.REQ_VENDOR;
            ControlEndPt.Direction = CyConst.DIR_TO_DEVICE;
            ControlEndPt.ReqCode = 0xBA;
            ControlEndPt.Value = (ushort)(DownloadAddress & 0x0000FFFF); // Get 16-bit LSB
            ControlEndPt.Index = (ushort)(DownloadAddress >> 16);        // Get 16-bit MSB            

            for (uint i = 0; i < NoOfStage; i++)
            {
                //Copy data from main buffer to tmp buffer
                for (uint j = 0; j < STAGE_SIZE; j++)
                    downloadbuf[j] = buf[FwImagePtr + j];

                if (!ControlEndPt.XferData(ref downloadbuf, ref StageSize))
                {

                    return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                }

                ControlEndPt.Index += (ushort)StageSize; //Starting address  withing I2C chunk size(for I2C size :32Kbyte the Index will go like 0,4,8,12,16 etc..)
                FwImagePtr += STAGE_SIZE; //post transfer increament of buffer pointer

                // Address calculation done in the below box
                if (IsMicroShipE2Prom)
                {//Microchip Addressing(0-(1-64),4(64 to 128),1(128 to 192 ),5(192 to 256))
                    if (FwImagePtr >= (128 * 1024))
                    {
                        if ((FwImagePtr % AddresingStageSize) == 0)
                        {
                            if (ControlEndPt.Value == 0x04)
                                ControlEndPt.Value = 0x01;
                            else
                                ControlEndPt.Value = 0x05;

                            ControlEndPt.Index = 0;
                        }
                    }
                    else if ((FwImagePtr % AddresingStageSize) == 0)
                    {                      
                        ControlEndPt.Value = 0x04;
                        ControlEndPt.Index = 0;
                    }
                }
                else
                {//ATMEL addressing sequential
                    if ((FwImagePtr % AddresingStageSize)==0)
                    {// Increament the Value field to represent the address and reset the Index value to zero.
                        ControlEndPt.Value += 0x01;
                        if(ControlEndPt.Value>=8)
                            ControlEndPt.Value = 0x0; //reset the Address to ZERO

                        ControlEndPt.Index = 0;
                    }
                }               
            }

            if (LastStage != 0)
            {//check for last stage

                for (uint j = 0; j < LastStage; j++)
                    downloadbuf[j] = buf[FwImagePtr + j];

                if ((LastStage % maxpkt) != 0)
                {// make it multiple of max packet size
                    int diff = (maxpkt - (LastStage % maxpkt));
                    for (int j = LastStage; j < (LastStage + diff); j++)
                        downloadbuf[j] = 0;

                    LastStage += diff;
                }

                if (!ControlEndPt.XferData(ref downloadbuf, ref LastStage))
                {
                    return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                }
               /*Failure Case:
                 The device does not return failure message when file size is more than 128KByte and only one 128Byte E2PROM on the DVK. 
                 Solution:
                 Read back the last stage data to confirm that all data transferred successfully.*/
                ControlEndPt.ReqCode = 0xBB;
                ControlEndPt.Direction = CyConst.DIR_FROM_DEVICE;
                if (!ControlEndPt.XferData(ref downloadbuf, ref LastStage))
                {
                    return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                }


            }
            return FX3_FWDWNLOAD_ERROR_CODE.SUCCESS;
        }

        unsafe internal FX3_FWDWNLOAD_ERROR_CODE EraseSectorOfSPIFlash(uint SectorNumber)
        {
            bool ret;
            byte[] buf = new byte[1];
            byte ReqCode = 0xC4;
            uint buflen = 0;
            uint elapsed = 0;
            buf[0] = 1;

            // Value = isErase, index = sector number
            ret = Ep0VendorCommand(ref buf, ref buflen, false, ReqCode, (1 + (SectorNumber << 16)));
            //et = Ep0VendorCommand(usbDevice, CyFalse, 0xc4, (1 + (sectorNumber << 16)), 0, 0);
            if (ret)
            {
                // Check the status of erase for max of 10 Seconds. Value should be 0
                buflen = 1;
                while ((buf[0] != 0) && (elapsed < 10000))
                {
                    if (!Ep0VendorCommand(ref buf, ref buflen, true, ReqCode, (0 + (SectorNumber << 16))))
                        return FX3_FWDWNLOAD_ERROR_CODE.FAILED;

                    System.Threading.Thread.Sleep(1000);
                    elapsed += 1000;
                }
            }
            else
                return FX3_FWDWNLOAD_ERROR_CODE.FAILED;

            // Timeout reached
            if (elapsed >= 10000)
                return FX3_FWDWNLOAD_ERROR_CODE.FAILED;

            return FX3_FWDWNLOAD_ERROR_CODE.SUCCESS;
        }

        unsafe internal bool WriteToSPIFlash(ref byte[] Buf, ref uint buflen, ref uint ByteAddress)
        {
            byte ReqCode = 0xC2;
            return Ep0VendorCommand(ref Buf, ref buflen, false, ReqCode, ((ByteAddress / SPI_FLASH_PAGE_SIZE_IN_BYTE) << 16));
        }

        unsafe internal FX3_FWDWNLOAD_ERROR_CODE DownloadUserIMGtoSPIFLASH(ref byte[] buf, ref uint buflen)
        {
            // The size of the image needs to be rounded to a multiple of the SPI page size. */
            uint ImageSizeInPage = (buflen + SPI_FLASH_PAGE_SIZE_IN_BYTE - 1) / SPI_FLASH_PAGE_SIZE_IN_BYTE;
            uint TotalNumOfByteToWrote = ImageSizeInPage * SPI_FLASH_PAGE_SIZE_IN_BYTE;
            // Sectors needs to be erased in case of SPI. Sector size = 64k. Page Size = 256 bytes. 1 Sector = 256 pages.
            // Calculate the number of sectors needed to write firmware image and erase them.
            uint NumOfSector = buflen / SPI_FLASH_SECTOR_SIZE_IN_BYTE;
            if ((buflen % SPI_FLASH_SECTOR_SIZE_IN_BYTE) != 0)
                NumOfSector++;
            //Erase the sectors
            for (uint i = 0; i < NumOfSector; i++)
            {
                if (EraseSectorOfSPIFlash(i) != FX3_FWDWNLOAD_ERROR_CODE.SUCCESS)
                {
                    return FX3_FWDWNLOAD_ERROR_CODE.SPIFLASH_ERASE_FAILED;
                }
            }
            //Write the firmware to the SPI flash
            uint numberOfBytesLeftToWrite = TotalNumOfByteToWrote; // Current number of bytes left to write
            //byte *imagePointer_p = buf; // Current image pointer
            uint FwFilePointer = 0;
            uint massStorageByteAddress = 0; // Current Mass Storage Byte Address            
            byte[] WriteBuf = new byte[CYWB_BL_MAX_BUFFER_SIZE_WHEN_USING_EP0_TRANSPORT];

            while (numberOfBytesLeftToWrite > 0)
            {
                uint numberOfBytesToWrite = CYWB_BL_MAX_BUFFER_SIZE_WHEN_USING_EP0_TRANSPORT;

                if (numberOfBytesLeftToWrite < CYWB_BL_MAX_BUFFER_SIZE_WHEN_USING_EP0_TRANSPORT)
                {
                    numberOfBytesToWrite = numberOfBytesLeftToWrite;
                }
                // Trigger a mass storage write...
                for (int i = 0; i < numberOfBytesToWrite; i++)
                {
                    if ((FwFilePointer + i) < buflen)
                        WriteBuf[i] = buf[i + FwFilePointer];
                }


                if (WriteToSPIFlash(ref WriteBuf, ref numberOfBytesToWrite, ref massStorageByteAddress) == false)
                {
                    return FX3_FWDWNLOAD_ERROR_CODE.FAILED;
                }
                // Adjust pointers
                numberOfBytesLeftToWrite -= numberOfBytesToWrite;
                FwFilePointer += numberOfBytesToWrite;
                massStorageByteAddress += numberOfBytesToWrite;
            }
            return FX3_FWDWNLOAD_ERROR_CODE.SUCCESS;
        }

    }
}
