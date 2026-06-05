using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Vision.Optics.LFine
{
    public class LFinePSDigitalIlluminator : Illuminator
    {
        public class LastPowerInfo
        {
            public int Page = 0;
            
            public int[] OnTimes = new int[16];
            public LastPowerInfo() 
            {
            }
            
        }
        [NonSerialized]
        private LFinePSDigitalIlluminatorCommunicator m_Communicator;
        [NonSerialized]
        private LastPowerInfo LastPowers = new LastPowerInfo();
        public void ResetLastPower()
        {
            for(int i=0; i<16; i++)
            {
                LastPowers.OnTimes[i] = 0;
            }
            LastPowers.Page = 0;
        }
        public LFineDigitalIlluminatorConfig LFineDigitalIlluminatorConfig
        {
            set
            {
                Config = value;
            }
            get
            {
                return Config as LFineDigitalIlluminatorConfig;
            }
        }
        public override void Load(FileStream fs)
        {
            base.Load(fs);
            LFineDigitalIlluminatorConfig config = null;
            SaveManager.BinaryDeserialize<LFineDigitalIlluminatorConfig>(fs, out config);
            if (config == null)
            {
                config = new LFineDigitalIlluminatorConfig();
            }
            LFineDigitalIlluminatorConfig = config;
            LFineDigitalIlluminatorConfig.Init();
        }

        public override void Save(FileStream fs)
        {
            base.Save(fs);
            SaveManager.BinarySerialize(fs, LFineDigitalIlluminatorConfig);
        }

        public override int Initialize()
        {
            int ret = 0;
            if ((ret = base.Initialize()) != 0) return ret;

            try
            {
                ResetLastPower();
        
                if (m_Communicator == null)
                {
                    m_Communicator = new LFinePSDigitalIlluminatorCommunicator();
                }

                if (m_Communicator.IsOpen)
                {
                    m_Communicator.Close();
                }

                m_Communicator.PortName = LFineDigitalIlluminatorConfig.PortName;
                m_Communicator.BaudRate = LFineDigitalIlluminatorConfig.BaudRate;
                m_Communicator.DataBits = LFineDigitalIlluminatorConfig.DataBits;
                m_Communicator.Parity = LFineDigitalIlluminatorConfig.Parity;
                m_Communicator.StopBits = LFineDigitalIlluminatorConfig.StopBits;
                m_Communicator.Handshake = LFineDigitalIlluminatorConfig.Handshake;
                m_Communicator.ReplyTimeout = LFineDigitalIlluminatorConfig.TimeOut;

                m_Communicator.Open();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                Log.Write("LFinePSDigitalIlluminator", ex.Message);

                ret = -1;
            }
            return ret;
        }

        public override void Close()
        {
            base.Close();
            if (m_Communicator != null)
                m_Communicator.Close();
        }

        public void SetOnTime(int page, int channel, int time)
        {
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                m_Communicator.SetTime(page, channel, time);
            }
        }

        //public void SetOnTime(int page, int[] time)
        //{
        //    if (m_Communicator != null && m_Communicator.IsOpen)
        //    {
        //        m_Communicator.SetTime(page, time);
        //    }
        //}
        public override int SetStrobeOnTime(int page, int[] times)
        {
            int ret = 0;
            if(IsSameLastPower(page, times))
            {
                return ret;
            }

            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                ret = m_Communicator.SetTime(page, times);
            }
            SetLastPower(page, times);
            return ret;
        }
        private bool IsSameLastPower(int page, int[] times)
        {
            if(LastPowers.Page != page)
            {
                return false;
            }
            for(int i=0; i< times.Length; i++)
            {
                if(i< 16)
                {
                    if(LastPowers.OnTimes[i] != times[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private void SetLastPower(int page, int[] times)
        {
            LastPowers.Page = page;
            for(int i=0; i< times.Length; i++)
            {
                if(i< 16)
                {
                    LastPowers.OnTimes[i] = times[i];
                }
            }
        }

        public override int SetValue(BrightSettingParameter parameter)
        {
            int ret = 0;
            lock (this)
            {

                TopBrightSettingParameter topParameter = parameter as TopBrightSettingParameter;
                BottomBrightSettingParameter bottomParameter = parameter as BottomBrightSettingParameter;

                if (topParameter != null)
                {
                    int[] times = new int[16];
                    for (int i = 0; i < 16; i++)
                    {
                        times[i] = 0;
                    }
                    times[topParameter.Channel - 1] = topParameter.OnTime;
                    times[topParameter.Channel2 - 1] = topParameter.OnTime2;
                    times[topParameter.Channel3 - 1] = topParameter.OnTime3;
                    times[topParameter.Channel4 - 1] = topParameter.OnTime4;

                    ret = SetStrobeOnTime(topParameter.Page, times);
                }
                else if (bottomParameter != null)
                {
                    int[] times = new int[16];
                    for (int i = 0; i < 16; i++)
                    {
                        times[i] = 0;
                    }

                    times[bottomParameter.Channel - 1] = bottomParameter.OnTime;
                    times[bottomParameter.Channel2 - 1] = bottomParameter.OnTime2;

                    ret = SetStrobeOnTime(bottomParameter.Page, times);
                }

            }





            return ret;
        }
    }

    public class LFinePSDigitalIlluminatorCommunicator : SerialComm
    {
        public const byte Stx = 0x40; //\r
        public const byte Etx1 = 0x0D; //\n
        public const byte Etx2 = 0x0A; //\n

        private byte[] arrStx = { Stx };
        private byte[] arrEtx = { Etx1, Etx2 };

        //private byte[] Frame = { Etx };

        private readonly object m_SyncRoot;

        private const string PageOnTime = "SP";
        private const string ChannelOnTime = "SC";


        public int ReplyTimeout { set; get; }

        public bool IsOpen => m_SerialPort.IsOpen;

        public LFinePSDigitalIlluminatorCommunicator() : base()
        {
            m_SyncRoot = new object();
        }

        public int Send(string text)
        {
            int ret = 0;
            lock (m_SyncRoot)
            {

                if (m_SerialPort.IsOpen == false)
                {
                    Log.Write("LFinePSDigitalIlluminatorCommunicator", "Failed this port open.");
                    return -1;
                }
                lock (m_SerialPort)
                {
                    if ((ret = SendFrame(text, arrStx, arrEtx)) != 0)
                    {
                        Log.Write("LFinePSDigitalIlluminatorCommunicator", "Send failed.");
                        return -1;
                    }
                }
                Log.Write("LFinePSDigitalIlluminatorCommunicator", string.Format("[SEND] {0}", text));
            }
            return ret;
        }

        private int Receive(ref string response)
        {
            int ret = 0;
            byte[] data = null;

            if (m_SerialPort.IsOpen == false)
            {
                Log.Write("LFinePSDigitalIlluminatorCommunicator", "Failed this port open.");
                return -1;
            }

            if (this.WaitRecv(this.ReplyTimeout) == true)
            {
                if ((ret = this.ReceiveFrame(out data, arrStx, arrEtx)) != 0)
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
            Log.Write("LFinePSDigitalIlluminatorCommunicator", string.Format("[RECV] {0}", response));
            return ret;
        }

        protected string GetPageOnTimeCommand(int page, int[] time)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0}{1:00}00", PageOnTime, page);
            foreach (int t in time)
            {
                builder.AppendFormat(";{0:000}", t / 10);
            }

            return builder.ToString();
        }

        protected string GetChannelOnTimeCommand(int page, int channel, int time)
        {
            return string.Format("{0}{1:00}{2:00};{3:000}", ChannelOnTime, page, channel, time);
        }
        public int SetTime(int page, int channel, int time)
        {
            int ret = 0;
            string strRecv = string.Empty;
            ret = Send(GetChannelOnTimeCommand(page, channel, time));

            return ret;
        }

        public int SetTime(int page, int[] time)
        {
            int ret = 0;
            string strRecv = string.Empty;
            ret = Send(GetPageOnTimeCommand(page, time));

            return ret;
        }



    }
}
