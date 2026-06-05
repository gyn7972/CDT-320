using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common.Vision.Optics.ImageFocus
{
    public class LSDigitalIlluminatorCommunicator: SerialComm
    {
        [Serializable]
        public enum Commands
        {
            //STATUS,
            //ON,
            //OFF,
            //Volume,
            STATUS,
            ON,
            OFF,
            Volume,
            CheckPower
        }
        private delegate int TurnOnOffDelegate(LSDigitalIlluminatorCommunicator.Commands onoff, int channel);
        private delegate int SetVolumeDelegate(int volume, int channel);

        //#region Field
        private const byte Stx = 0x02; //\r
        private const byte Etx = 0x03; //\n
        private byte[] Frame = { (byte)LSDigitalIlluminatorCommunicator.Stx, (byte)LSDigitalIlluminatorCommunicator.Etx };

        //private MethodCallerPool m_Methods;
        //#endregion

        private readonly object m_SyncRoot;

        //private const byte Etx1 = 0x0D; //\r
        //private const byte Etx2 = 0x0A; //\n
        //private byte[] Etx = { LSDigitalIlluminatorCommunicator.Etx1, LSDigitalIlluminatorCommunicator.Etx2 };

        private const string StartText = "R";
        private const string ErrorCode = "ERR";

        // On/Off
        private const string StatusOn = "ON";
        private const string StatusOff = "OF";

        // status
        private const string StatusBright = "00";
        private const string StatusOnOff = "01";
        private const string StatusError = "02";

        // Power
        private const string CheckPower = "SPWR";

        public int ReplyTimeout { set; get; }

        public bool IsOpen => m_SerialPort.IsOpen;

        public LSDigitalIlluminatorCommunicator() : base()
        {
            m_SyncRoot = new object();
        }

        private int Send(string text)
        {
            int ret = 0;

            // ------- INFO ------
            // "H1ON" send -> "R1ON" receive
            // "C255" send -> "R255" receive
            // 일정시간마다 "FMT"라는 메세지를 받는다.
            // 지정 포맷에 맞지않거나 유효하지 않은 명령을 보내올 경우 "RERR"을 receive한다.

            if (m_SerialPort.IsOpen == false)
            {
                Console.WriteLine("Failed this port open.");
                return -1;
            }

            if ((ret = SendFrame(text, Frame)) != 0)
            {
                Console.WriteLine("Send failed.");
                return -1;
            }

            Console.WriteLine(string.Format("[SEND] {0}", text));

            return ret;
        }

        private int Receive(ref string response)
        {
            int ret = 0;
            byte[] data = null;

            if (m_SerialPort.IsOpen == false)
            {
                Console.WriteLine("Failed this port open.");
                return -1;
            }

            if (this.WaitRecv(this.ReplyTimeout) == true)
            {
                if ((ret = this.ReceiveFrame(out data, LSDigitalIlluminatorCommunicator.Etx)) != 0)
                {
                    return ret;
                }
            }
            else
            {
                return -1;
            }

            if (data == null || data.Length == 0) return ret;

            response = BytesConverter.ToString(data);
            Console.WriteLine("[RECV] {0}", response);

            //command를 보내서 받은 receive data는 'R'로 시작한다.
            //수동으로 LED Controller를 조작했을 때 받은 receive data는 'T'로 시작한다.
            //if (response.StartsWith(LeesosPdFn300wIlluminatorCommunicator.StartText) == false)
            //{
            //    if ((ret = this.Alarms[LeesosPdFn300wIlluminatorCommunicator.AlarmKeys.NotReceiveInvalidCommand].Post(this)) != 0) return ret;
            //}
            //else if(response.EndsWith(LeesosPdFn300wIlluminatorCommunicator.ErrorCode) == true)
            //{
            //    if ((ret = this.Alarms[LeesosPdFn300wIlluminatorCommunicator.AlarmKeys.ReceiveError].Post(this)) != 0) return ret;
            //}
            return ret;
        }

        private String GetCommandText(LSDigitalIlluminatorCommunicator.Commands command)
        {
            string com = "";

            switch (command)
            {
                case Commands.OFF:
                    com = "OFF";
                    break;
                case Commands.ON:
                    com = "1ON,2ON,3ON,4ON,5ON,6ON";
                    break;
                case Commands.Volume:
                    com = "L";
                    break;
                case Commands.CheckPower:
                    //com = "E1";
                    break;
                default:
                    break;
            }

            return com;
            //string com = "";
            //switch (command)
            //{
            //    case Commands.STATUS:
            //        com = "LS";
            //        break;
            //    case Commands.ON:
            //    case Commands.OFF:
            //        com = "LH";
            //        break;
            //    case Commands.Volume:
            //        com = "LC";
            //        break;
            //    default:
            //        break;
            //}
            //return com;
        }
        #region CheckPowerOn
        public int CheckPowerOn(int channel)
        {
            return this.CheckPowerOnProcedure(channel);
        }

        private int CheckPowerOnProcedure(int channel)
        {
            int ret = 0;
            if ((ret = this.OnCheckPowerOn(channel)) != 0) return ret;
            return ret;
        }

        protected virtual int OnCheckPowerOn(int channel)
        {
            //int ret = 0;
            //string commandText = "";
            //string response = "";
            ////string text = "";

            //lock (m_SyncRoot)
            //{
            //    commandText = GetCommandText(Commands.CheckPower);
            //    if ((ret = this.Send(commandText + channel.ToString() + "01")) != 0) return ret;
            //    if ((ret = this.Receive(ref response)) != 0) return -1;
            //    Console.WriteLine(string.Format("{0} : ok", response.Length));

            //    if (response.Length < 4) return -1;

            //    if (response.Substring(2, 2) == "ON")
            //    {
            //        return 0;
            //    }
            //    else return -1;
            //}
            int ret = 0;
            string commandText = "";
            string response = "";

           

            lock (this.m_SyncRoot)
            {
                commandText = GetCommandText(Commands.CheckPower);
                if ((ret = this.Send(commandText)) != 0) return ret;
                if ((ret = this.Receive(ref response)) != 0) return -1;
                //this.WriteLog(LogLevel.Highest, string.Format("{0} : ok", response.Length));

                return ret;
            }
        }
        #endregion

        #region TurnOnOff()

        public int TurnOnOff(LSDigitalIlluminatorCommunicator.Commands onoff, int channel)
        {
            return this.TurnOnOffProcedure(onoff, channel);
        }

        private int TurnOnOffProcedure(LSDigitalIlluminatorCommunicator.Commands onoff, int channel)
        {
            int ret = 0;
            if ((ret = this.OnTurnOnOff(onoff, channel)) != 0) return ret;
            return ret;
        }

        private string MakeFrame(string strData)
        {
            string strRet;
            strRet = string.Format("{0}{1}{2}", (char)Stx, strData, (char)Etx);
            return strRet;
        }

        protected virtual int OnTurnOnOff(LSDigitalIlluminatorCommunicator.Commands onoff, int channel)
        {

            //int ret = 0;
            //string commandText = "", receiveText = "";
            //string switchOnOff = "";

            //lock (this.m_SyncRoot)
            //{
            //    if (onoff == LSDigitalIlluminatorCommunicator.Commands.ON)
            //        switchOnOff = LSDigitalIlluminatorCommunicator.StatusOn;
            //    else
            //        switchOnOff = LSDigitalIlluminatorCommunicator.StatusOff;

            //    commandText = this.GetCommandText(onoff);
            //    if ((ret = this.Send(commandText + channel.ToString() + switchOnOff.ToString())) != 0) return ret;
            //    if ((ret = this.Receive(ref receiveText)) != 0) return ret;
            //}

            //return ret;

            int ret = 0;
            string strCommand = "";
            switch (onoff)
            {
                case Commands.ON:
                    strCommand = GetTurnOnCommand(channel);
                    break;
                case Commands.OFF:
                    strCommand = GetTurnOffCommand(channel);
                    break;
                case Commands.Volume:
                    strCommand = GetVolume(channel);
                    break;
                case Commands.CheckPower:
                    strCommand = GetCheckPower(channel);
                    break;

            }
            ret = Send(strCommand);
            return ret;

        }
        public string GetTurnOnCommand(int nChannel)
        {
            string strData;
            strData = string.Format("{0}{1}ON", nChannel, nChannel);
            strData = MakeFrame(strData);
            return strData;

        }
        public string GetTurnOffCommand(int nChannel)
        {
            string strData;
            strData = string.Format("{0}OFF", nChannel);
            strData = MakeFrame(strData);
            return strData;

        }
        public string GetCheckPower(int nChannel)
        {
            string strData;
            strData = string.Format("{0}{0}RT", nChannel);
            strData = MakeFrame(strData);
            return strData;
        }
        public string GetVolume(int nChannel)
        {
            string strData;
            strData = string.Format("{0}RET", nChannel);
            strData = MakeFrame(strData);
            return strData;
        }
        #endregion

        #region SetVolume()
        public int SetVolume(int volume, int channel)
        {
            return this.SetVolumeProcedure(volume, channel);
        }

        private int SetVolumeProcedure(int volume, int channel)
        {
            int ret = 0;
            if ((ret = this.OnSetVolume(volume, channel)) != 0) return ret;
            return ret;
        }
        public string GetVolumeControl(int nChannel, int nPower)
        {
            string strData;
            strData = string.Format("{0}{1}", nChannel, ((int)nPower).ToString("D3"));
            strData = MakeFrame(strData);
            return strData;
        }
        protected virtual int OnSetVolume(int volume, int channel)
        {
            //int ret = 0;
            //string commandText = "", receiveText = "";

            //lock (this.m_SyncRoot)
            //{
            //    commandText = this.GetCommandText(LSDigitalIlluminatorCommunicator.Commands.Volume);
            //    if ((ret = this.Send(commandText + channel.ToString() + volume.ToString("X2"))) != 0) return ret;
            //    if ((ret = this.Receive(ref receiveText)) != 0) return ret;
            //}

            //return ret;
            int ret = 0;
            string receiveText = "";

            lock (this.m_SyncRoot)
            {
                string strData = GetVolumeControl(channel, volume);
                if ((ret = this.Send(strData)) != 0) return ret;

                //if ((ret = this.Receive(ref receiveText)) != 0) return ret;
            }

            return ret;
        }
        #endregion
    }
}

