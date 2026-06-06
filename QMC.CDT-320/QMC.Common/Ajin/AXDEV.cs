using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace QMC.Common.Motion.Ajin
{
    public static class AXDEV
    {
        #region Define
        public const string LibraryFileName = "AXL.dll";
        #endregion

        #region Dll Imports
        // 지정 축에 32bit byte Setting
        [DllImport(LibraryFileName)]
        private static extern uint AxmSetCommandData32Qi(int nAxisNo, byte sCommand, uint uData);
        // 지정 축에 32bit byte 가져오기
        [DllImport(LibraryFileName)]
        private static extern uint AxmGetCommandData32Qi(int nAxisNo, byte sCommand, ref uint upData);

        // 지정 축에 스크립트 내부 Queue Index를 Clear 시킨다.
        // uSelect IP. 
        // uSelect(0): 스크립트 Queue Index 를 Clear한다.
        //        (1): 캡션 Queue를 Index Clear한다.

        // uSelect QI. 
        // uSelect(0): 스크립트 Queue 1 Index 을 Clear한다.
        //        (1): 스크립트 Queue 2 Index 를 Clear한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxmSetScriptCaptionQueueClear(int lAxisNo, uint uSelect);

        // 지정 축에 스크립트를 설정한다. - QI
        // sc    : 스크립트 번호 (1 - 4)
        // event : 발생할 이벤트 SCRCON 을 정의한다.
        //         이벤트 설정 축갯수설정, 이벤트 발생할 축, 이벤트 내용 1,2 속성 설정한다.
        // cmd   : 어떤 내용을 바꿀것인지 선택 SCRCMD를 정의한다.
        // data  : 어떤 Data를 바꿀것인지 선택
        [DllImport(LibraryFileName)]
        private static extern uint AxmSetScriptCaptionQi(int lAxisNo, int sc, uint occurredEvent, uint cmd, uint data);

        // EtherCAT 관련 Ecat 함수 DllImport (CAXDev)
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetProductInfo(uint dwStationAddress, ref uint upVendorID, ref uint upProductCode, ref uint upRevisionNo);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetProductInfoEx(int lBoardNo, uint dwStationAddress, ref uint pdwVendorID, ref uint pdwProductCode, ref uint pdwRevisionNo);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetModuleStatus(uint dwStationAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadPdoInput(uint dwBitOffset, uint dwDataBitLength, byte[] pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadPdoInputEx(int lBoardNo, uint dwBitOffset, uint dwDataBitLength, byte[] pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadPdoOutput(uint dwBitOffset, uint dwDataBitLength, byte[] pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadPdoOutputEx(int lBoardNo, uint dwBitOffset, uint dwDataBitLength, byte[] pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWritePdoOutput(uint dwBitOffset, uint dwDataBitLength, byte[] pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWritePdoOutputEx(int lBoardNo, uint dwBitOffset, uint dwDataBitLength, byte[] pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadSdo(uint dwStationAddress, ushort wObjectIndex, byte byObjectSubIndex, byte[] pbyData, uint dwDataLength, ref uint pdwReadDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadSdoEx(int lBoardNo, uint dwStationAddress, ushort wObjectIndex, byte byObjectSubIndex, byte[] pbyData, uint dwDataLength, ref uint pdwReadDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteSdo(uint dwStationAddress, ushort wObjectIndex, byte byObjectSubIndex, byte[] pbyData, uint dwDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteSdoEx(int lBoardNo, uint dwStationAddress, ushort wObjectIndex, byte byObjectSubIndex, byte[] pbyData, uint dwDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadSdoFromAxisDouble(int lAxisNo, uint wObjectIndex, byte byObjectSubIndex, ref double pdData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteSdoFromAxisDouble(int lAxisNo, uint wObjectIndex, byte byObjectSubIndex, ref double dData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadSdoFromAxisDword(int lAxisNo, ushort wObjectIndex, byte byObjectSubIndex, ref uint pdwData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteSdoFromAxisDword(int lAxisNo, ushort wObjectIndex, byte byObjectSubIndex, ref uint dwData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadSdoFromAxisWord(int lAxisNo, ushort wObjectIndex, byte byObjectSubIndex, ref ushort pwData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteSdoFromAxisWord(int lAxisNo, ushort wObjectIndex, byte byObjectSubIndex, ref ushort wData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadSdoFromAxisByte(int lAxisNo, ushort wObjectIndex, byte byObjectSubIndex, ref byte pbyData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteSdoFromAxisByte(int lAxisNo, ushort wObjectIndex, byte byObjectSubIndex, ref byte byData);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadEEPRom(uint dwStationAddress, ushort wEEPRomStartOffset, ref ushort pwData, uint dwDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteEEPRom(uint dwStationAddress, ushort wEEPRomStartOffset, ref ushort pwData, uint dwDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadEEPRomEx(int lBoardNo, uint dwStationAddress, uint wEEPRomStartOffset, ref uint pwData, uint dwDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteEEPRomEx(int lBoardNo, uint dwStationAddress, uint wEEPRomStartOffset, ref uint pwData, uint dwDataLength);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReadRegister(uint dwStationAddress, ushort wRegisterOffset, object pvData, ushort wLen);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatWriteRegister(uint dwStationAddress, ushort wRegisterOffset, object pvData, ushort wLen);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatSaveHotSwapData(uint dwStationAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatLoadHotSwapData(uint dwStationAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatSetHotSwap(uint dwStationAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatIsSetHotSwap(uint dwStationAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatReSetHotSwap(uint dwStationAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatSetMasterMode(uint dwMasterMode);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetMasterMode(ref uint pdMasterMode);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatSetMasterOperationMode(uint dwOperationMode);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetMasterOperationMode(ref uint pdwOperationMode);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatRequestScanData();
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetSlaveInfoByIndex(int lIndex, ref uint dwpVendorID, ref uint dwpProductCode, ref uint dwpRevisionNumber, ref uint dwpSerialNumber, ref uint dwpPhysAddress, ref uint dwpAliasAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetScanSlaveCount(ref uint pdwSlaveCount);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetStatus(ref int pnECMasterStatus, ref int pnECSlaveStatus, ref int pnECConnectedSlave, ref int pnECConfiguredSlave, ref int pnJobTaskCycleCnt, ref uint pdwECMasterNotification);
        [DllImport(LibraryFileName)]
        private static extern uint AxlEcatReConnect();
        [DllImport(LibraryFileName)]
        private static extern uint AxmECatReadAddress(int nAxisNo, ref uint dwpStationAddress, ref int npAutoIncAddress, ref uint dwpAliasAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxdECatReadAddress(int nModuleNo, ref uint dwpStationAddress, ref int npAutoIncAddress, ref uint dwpAliasAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxaECatReadAddress(int nModuleNo, ref uint dwpStationAddress, ref int npAutoIncAddress, ref uint dwpAliasAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxsECatReadAddress(int lPortNo, ref uint dwpStationAddress, ref int lpAutoIncAddress, ref uint dwpAliasAddress);
        [DllImport(LibraryFileName)]
        private static extern uint AxlECatGetCycleTime(int nBoardNo, ref uint dwpCycleTime);
        [DllImport(LibraryFileName)]
        private static extern uint AxlReadDcCtrlError(int nBoardNo, int nNode, ref int npDcCtrlError);
        #endregion

        #region Script 함수
        public static int SetScriptCaptionQueueClear(int axisNo, uint select)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxmSetScriptCaptionQueueClear", AXDEV.AxmSetScriptCaptionQueueClear(axisNo, select))) != 0) return ret;
            return ret;
        }

        public static int SetScriptCaptionQi(int axisNo, int sc, uint occurredEvent, uint cmd, uint data)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxmSetScriptCaptionQi", AXDEV.AxmSetScriptCaptionQi(axisNo, sc, occurredEvent, cmd, data))) != 0) return ret;
            return ret;
        }
        #endregion

        #region 지정축 trigger setting
        public static int SetCommandData32Qi(int axisNo, byte command, uint data)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxmSetCommandData32Qi", AXDEV.AxmSetCommandData32Qi(axisNo, command, data))) != 0) return ret;
            return ret;
        }

        public static int GetCommandData32Qi(int axisNo, byte command, ref uint data)
        {
            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxmGetCommandData32Qi", AXDEV.AxmGetCommandData32Qi(axisNo, command, ref data))) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT Product Info
        public static int ECatGetProductInfo(uint stationAddress, ref uint vendorID, ref uint productCode, ref uint revisionNo)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetProductInfo", AxlECatGetProductInfo(stationAddress, ref vendorID, ref productCode, ref revisionNo))) != 0) return ret;
            return ret;
        }
        public static int ECatGetProductInfoEx(int boardNo, uint stationAddress, ref uint vendorID, ref uint productCode, ref uint revisionNo)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetProductInfoEx", AxlECatGetProductInfoEx(boardNo, stationAddress, ref vendorID, ref productCode, ref revisionNo))) != 0) return ret;
            return ret;
        }
        public static int ECatGetModuleStatus(uint stationAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetModuleStatus", AxlECatGetModuleStatus(stationAddress))) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT PDO
        public static int ECatReadPdoInput(uint bitOffset, uint dataBitLength, byte[] data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadPdoInput", AxlECatReadPdoInput(bitOffset, dataBitLength, data))) != 0) return ret;
            return ret;
        }
        public static int ECatReadPdoInputEx(int boardNo, uint bitOffset, uint dataBitLength, byte[] data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadPdoInputEx", AxlECatReadPdoInputEx(boardNo, bitOffset, dataBitLength, data))) != 0) return ret;
            return ret;
        }
        public static int ECatReadPdoOutput(uint bitOffset, uint dataBitLength, byte[] data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadPdoOutput", AxlECatReadPdoOutput(bitOffset, dataBitLength, data))) != 0) return ret;
            return ret;
        }
        public static int ECatReadPdoOutputEx(int boardNo, uint bitOffset, uint dataBitLength, byte[] data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadPdoOutputEx", AxlECatReadPdoOutputEx(boardNo, bitOffset, dataBitLength, data))) != 0) return ret;
            return ret;
        }
        public static int ECatWritePdoOutput(uint bitOffset, uint dataBitLength, byte[] data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWritePdoOutput", AxlECatWritePdoOutput(bitOffset, dataBitLength, data))) != 0) return ret;
            return ret;
        }
        public static int ECatWritePdoOutputEx(int boardNo, uint bitOffset, uint dataBitLength, byte[] data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWritePdoOutputEx", AxlECatWritePdoOutputEx(boardNo, bitOffset, dataBitLength, data))) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT SDO
        public static int ECatReadSdo(uint stationAddress, ushort objectIndex, byte objectSubIndex, byte[] data, uint dataLength, ref uint readDataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadSdo", AxlECatReadSdo(stationAddress, objectIndex, objectSubIndex, data, dataLength, ref readDataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatReadSdoEx(int boardNo, uint stationAddress, ushort objectIndex, byte objectSubIndex, byte[] data, uint dataLength, ref uint readDataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadSdoEx", AxlECatReadSdoEx(boardNo, stationAddress, objectIndex, objectSubIndex, data, dataLength, ref readDataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteSdo(uint stationAddress, ushort objectIndex, byte objectSubIndex, byte[] data, uint dataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteSdo", AxlECatWriteSdo(stationAddress, objectIndex, objectSubIndex, data, dataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteSdoEx(int boardNo, uint stationAddress, ushort objectIndex, byte objectSubIndex, byte[] data, uint dataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteSdoEx", AxlECatWriteSdoEx(boardNo, stationAddress, objectIndex, objectSubIndex, data, dataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatReadSdoFromAxisDouble(int axisNo, uint objectIndex, byte objectSubIndex, ref double data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadSdoFromAxisDouble", AxlECatReadSdoFromAxisDouble(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteSdoFromAxisDouble(int axisNo, uint objectIndex, byte objectSubIndex, ref double data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteSdoFromAxisDouble", AxlECatWriteSdoFromAxisDouble(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatReadSdoFromAxisDword(int axisNo, ushort objectIndex, byte objectSubIndex, ref uint data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadSdoFromAxisDword", AxlECatReadSdoFromAxisDword(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteSdoFromAxisDword(int axisNo, ushort objectIndex, byte objectSubIndex, ref uint data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteSdoFromAxisDword", AxlECatWriteSdoFromAxisDword(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatReadSdoFromAxisWord(int axisNo, ushort objectIndex, byte objectSubIndex, ref ushort data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadSdoFromAxisWord", AxlECatReadSdoFromAxisWord(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteSdoFromAxisWord(int axisNo, ushort objectIndex, byte objectSubIndex, ref ushort data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteSdoFromAxisWord", AxlECatWriteSdoFromAxisWord(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatReadSdoFromAxisByte(int axisNo, ushort objectIndex, byte objectSubIndex, ref byte data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadSdoFromAxisByte", AxlECatReadSdoFromAxisByte(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteSdoFromAxisByte(int axisNo, ushort objectIndex, byte objectSubIndex, ref byte data)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteSdoFromAxisByte", AxlECatWriteSdoFromAxisByte(axisNo, objectIndex, objectSubIndex, ref data))) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT EEPROM
        public static int ECatReadEEPRom(uint stationAddress, ushort startOffset, ref ushort data, uint dataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadEEPRom", AxlECatReadEEPRom(stationAddress, startOffset, ref data, dataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteEEPRom(uint stationAddress, ushort startOffset, ref ushort data, uint dataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteEEPRom", AxlECatWriteEEPRom(stationAddress, startOffset, ref data, dataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatReadEEPRomEx(int boardNo, uint stationAddress, uint startOffset, ref uint data, uint dataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadEEPRomEx", AxlECatReadEEPRomEx(boardNo, stationAddress, startOffset, ref data, dataLength))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteEEPRomEx(int boardNo, uint stationAddress, uint startOffset, ref uint data, uint dataLength)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteEEPRomEx", AxlECatWriteEEPRomEx(boardNo, stationAddress, startOffset, ref data, dataLength))) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT Register/HotSwap
        public static int ECatReadRegister(uint stationAddress, ushort registerOffset, object data, ushort len)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReadRegister", AxlECatReadRegister(stationAddress, registerOffset, data, len))) != 0) return ret;
            return ret;
        }
        public static int ECatWriteRegister(uint stationAddress, ushort registerOffset, object data, ushort len)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatWriteRegister", AxlECatWriteRegister(stationAddress, registerOffset, data, len))) != 0) return ret;
            return ret;
        }
        public static int ECatSaveHotSwapData(uint stationAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatSaveHotSwapData", AxlECatSaveHotSwapData(stationAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatLoadHotSwapData(uint stationAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatLoadHotSwapData", AxlECatLoadHotSwapData(stationAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatSetHotSwap(uint stationAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatSetHotSwap", AxlECatSetHotSwap(stationAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatIsSetHotSwap(uint stationAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatIsSetHotSwap", AxlECatIsSetHotSwap(stationAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatReSetHotSwap(uint stationAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatReSetHotSwap", AxlECatReSetHotSwap(stationAddress))) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT Master/Scan/Status
        public static int ECatSetMasterMode(uint masterMode)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatSetMasterMode", AxlECatSetMasterMode(masterMode))) != 0) return ret;
            return ret;
        }
        public static int ECatGetMasterMode(ref uint masterMode)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetMasterMode", AxlECatGetMasterMode(ref masterMode))) != 0) return ret;
            return ret;
        }
        public static int ECatSetMasterOperationMode(uint operationMode)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatSetMasterOperationMode", AxlECatSetMasterOperationMode(operationMode))) != 0) return ret;
            return ret;
        }
        public static int ECatGetMasterOperationMode(ref uint operationMode)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetMasterOperationMode", AxlECatGetMasterOperationMode(ref operationMode))) != 0) return ret;
            return ret;
        }
        public static int ECatRequestScanData()
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatRequestScanData", AxlECatRequestScanData())) != 0) return ret;
            return ret;
        }
        public static int ECatGetSlaveInfoByIndex(int index, ref uint vendorID, ref uint productCode, ref uint revisionNumber, ref uint serialNumber, ref uint physAddress, ref uint aliasAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetSlaveInfoByIndex", AxlECatGetSlaveInfoByIndex(index, ref vendorID, ref productCode, ref revisionNumber, ref serialNumber, ref physAddress, ref aliasAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatGetScanSlaveCount(ref uint slaveCount)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetScanSlaveCount", AxlECatGetScanSlaveCount(ref slaveCount))) != 0) return ret;
            return ret;
        }
        public static int ECatGetStatus(ref int masterStatus, ref int slaveStatus, ref int connectedSlave, ref int configuredSlave, ref int jobTaskCycleCnt, ref uint masterNotification)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetStatus", AxlECatGetStatus(ref masterStatus, ref slaveStatus, ref connectedSlave, ref configuredSlave, ref jobTaskCycleCnt, ref masterNotification))) != 0) return ret;
            return ret;
        }
        public static int ECatReConnect()
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlEcatReConnect", AxlEcatReConnect())) != 0) return ret;
            return ret;
        }
        #endregion

        #region EtherCAT Address/Cycle/DC
        public static int ECatReadAddress_Axm(int axisNo, ref uint stationAddress, ref int autoIncAddress, ref uint aliasAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxmECatReadAddress", AxmECatReadAddress(axisNo, ref stationAddress, ref autoIncAddress, ref aliasAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatReadAddress_Axd(int moduleNo, ref uint stationAddress, ref int autoIncAddress, ref uint aliasAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxdECatReadAddress", AxdECatReadAddress(moduleNo, ref stationAddress, ref autoIncAddress, ref aliasAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatReadAddress_Axa(int moduleNo, ref uint stationAddress, ref int autoIncAddress, ref uint aliasAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxaECatReadAddress", AxaECatReadAddress(moduleNo, ref stationAddress, ref autoIncAddress, ref aliasAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatReadAddress_Axs(int portNo, ref uint stationAddress, ref int autoIncAddress, ref uint aliasAddress)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxsECatReadAddress", AxsECatReadAddress(portNo, ref stationAddress, ref autoIncAddress, ref aliasAddress))) != 0) return ret;
            return ret;
        }
        public static int ECatGetCycleTime(int boardNo, ref uint cycleTime)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlECatGetCycleTime", AxlECatGetCycleTime(boardNo, ref cycleTime))) != 0) return ret;
            return ret;
        }
        public static int ECatReadDcCtrlError(int boardNo, int node, ref int dcCtrlError)
        {
            if (AXL.IsOpened() == false)
                return -1;

            int ret = 0;
            if ((ret = AXL.CheckErrorCode("AXDEV.AxlReadDcCtrlError", AxlReadDcCtrlError(boardNo, node, ref dcCtrlError))) != 0) return ret;
            return ret;
        }
        #endregion
    }
}
