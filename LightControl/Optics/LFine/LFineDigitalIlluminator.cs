using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Common.Vision.Optics.LFine
{
    public class LFineDigitalIlluminator : Illuminator
    {
        public enum CommandType
        {
            StrobeTime,
            Power,
        }
        [NonSerialized]
        private BrightSettingParameter[] LastPowers = new BrightSettingParameter[32];
        public class StrobeData
        {
            public CommandType CommandType { set; get; }
            public int Channel { set; get; }
            public int Value { set; get; }
            
            public StrobeData() : this(CommandType.Power, 0, 0)
            {
            }

            public StrobeData(CommandType command, int nChannel, int nValue)
            {
                CommandType = command;
                Channel = nChannel;
                Value = nValue;
            }
        }

        [NonSerialized]
        private LFineDigitalIlluminatorCommunicator m_Communicator;
        private Task m_Task;
        private Queue<StrobeData> m_CommandQueue;
        private CancellationTokenSource m_Cts;
        private bool m_bStop;

        public LFineDigitalIlluminatorConfig LFineDigitalIlluminatorConfig
        {
            get
            {
                return Config as LFineDigitalIlluminatorConfig;
            }
            set
            {
                Config = value;
            }
        }

        public LFineDigitalIlluminator() : this("StrobeIlluminator")
        {
        }

        public LFineDigitalIlluminator(string strName) : base(strName)
        {
            LFineDigitalIlluminatorConfig = new LFineDigitalIlluminatorConfig();
            //LFineDigitalIlluminatorConfig.Name = strName;
            m_CommandQueue = new Queue<StrobeData>();
            m_bStop = false;
            Type = IlluminatorType.Strobe;
        }

        public override int Initialize()
        {
            int ret = 0;
            if ((ret = base.Initialize()) != 0) return ret;
            ResetLastPower();
            try
            {
                if (m_Communicator == null)
                    m_Communicator = new LFineDigitalIlluminatorCommunicator();

                if (m_Communicator.IsOpen)
                {
                    m_bStop = true;
                    if (m_Task != null)
                    {
                        m_Task.Wait();
                    }
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
                MessageBox.Show(ex.Message);

                ret = -1;
            }
            return ret;
        }

        public void ResetLastPower()
        {
            for(int i = 0; i < LastPowers.Length; i++)
            {
                LastPowers[i] = new BrightSettingParameter();
                LastPowers[i].Power = 0;
                LastPowers[i].OnTime = 0;
                LastPowers[i].Channel = i;
            }
        }

        public override void Close()
        {
            base.Close();
            if (m_Cts != null)
                m_Cts.Cancel();

            if (m_Communicator != null)
                m_Communicator.Close();
        }

        public void SetPower(int channel, int power)
        {
            if(m_Communicator != null && m_Communicator.IsOpen)
            {
                m_Communicator.SetPower(channel, power);
            }
        }

        public override int SetValue(int channel, int powerP, int onTimeP)
        {
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                int power = LFineDigitalIlluminatorConfig.MaxPower * powerP / 100;
                int onTime = LFineDigitalIlluminatorConfig.MaxOnTime * onTimeP / 100;
                if(power > LFineDigitalIlluminatorConfig.MaxPower)
                {
                    power = LFineDigitalIlluminatorConfig.MaxPower;
                }
                if (onTime > LFineDigitalIlluminatorConfig.MaxOnTime)
                {
                    onTime = LFineDigitalIlluminatorConfig.MaxOnTime;
                }
                if(LastPowers[channel].Power == power && LastPowers[channel].OnTime == onTime)
                {
                    return 0;
                }
                else
                {
                    LastPowers[channel].Power = power;
                    LastPowers[channel].OnTime = onTime;
                }
                m_Communicator.SetPower(channel, power);
                m_Communicator.SetTime(channel, onTime);
            }

            return 0;
        }

        public override int SetValue(BrightSettingParameter parameter)
        {
            int ret = 0;
                        
            if(parameter != null)
            {
                ret = SetValue(parameter.Channel, parameter.Power, parameter.OnTime);
            }

            return ret;
        }

        public override void Load(FileStream fs)
        {
            base.Load(fs);
            LFineDigitalIlluminatorConfig config = null;
            SaveManager.BinaryDeserialize<LFineDigitalIlluminatorConfig>(fs, out config);
            if(config == null)
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
    }

    public class LFineDigitalIlluminatorCommunicator : SerialComm
    {
        public const byte Stx = 0x02; //\r
        public const byte Etx = 0x03; //\n
        private byte[] Frame = { Etx };

        private readonly object m_SyncRoot;

        private const string PowerText = "P";
        private const string StrobeTimeText = "T";
        private const string RemoteText = "R";
        

        public int ReplyTimeout { set; get; }

        public bool IsOpen => m_SerialPort.IsOpen;

        public LFineDigitalIlluminatorCommunicator() : base()
        {
            m_SyncRoot = new object();
        }

        public int Send(string text)
        {
            int ret = 0;

            if (m_SerialPort.IsOpen == false)
            {
                Console.WriteLine("Failed this port open.");
                return -1;
            }

            if ((ret = SendFrame(text, Stx, Etx)) != 0)
            {
                Console.WriteLine("Send failed.");
                return -1;
            }

            Console.WriteLine(string.Format("[SEND] {0}", text));
            Log.Write("serial", string.Format("[SEND] {0}", text));
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
                if ((ret = this.ReceiveFrame(out data, Etx)) != 0)
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

            return ret;
        }

        protected string GetTimeCommand(int channel, int time)
        {
            return string.Format("{0}{1}{2:000}{3}", channel, StrobeTimeText, time, RemoteText);
        }

        protected string GetPowerCommand(int channel, int power)
        {
            return string.Format("{0}{1}{2:000}{3}", channel, PowerText, power, RemoteText);
        }
        public int SetTime(int channel, int time)
        {
            int ret = 0;
            string strRecv = string.Empty;
            ret = Send(GetTimeCommand(channel, time));

            return ret;
        }

        public int SetPower(int channel, int power)
        {
            int ret = 0;
            ret = Send(GetPowerCommand(channel, power));
            return ret;
        }

    }
}
