
using QMC.Common.Vision.Optics.LFine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Common.Vision.Optics.ImageFocus
{
    public class LSStrobeIlluminator : Illuminator
    {
        public class SetTimeData
        {
            public int Channel { set; get; }
            public double SetTime { set; get; }

            public SetTimeData() : this(0, 0)
            {
            }

            public SetTimeData(int nChannel, double nTime)
            {
                Channel = nChannel;
                SetTime = nTime;
            }
        }

        [NonSerialized]
        private LSStrobeIlluminatorCommunicator m_Communicator;
        private Task m_Task;
        private Queue<SetTimeData> m_CommandQueue;
        private CancellationTokenSource m_Cts;
        private bool m_bStop;
        private object m_QueueLock;
        private List<BrightSettingParameter> m_listBP;
        public LSStrobeIlluminatorConfig LSStrobeIlluminatorConfig
        {
            get
            {
                return Config as LSStrobeIlluminatorConfig;
            }
            set
            {
                Config = value;
            }
        }



        public LSStrobeIlluminator() : this("StrobeIlluminator")
        {
            m_listBP = new List<BrightSettingParameter>();
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter()); 
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());

        }

        public LSStrobeIlluminator(string strName) : base(strName)
        {
            LSStrobeIlluminatorConfig = new LSStrobeIlluminatorConfig();
            LSStrobeIlluminatorConfig.Name = strName;
            m_CommandQueue = new Queue<SetTimeData>();
            m_QueueLock = new object();
            m_bStop = false;
            Type = IlluminatorType.Strobe;
            m_listBP = new List<BrightSettingParameter>();
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());
            m_listBP.Add(new BrightSettingParameter());


        }

        public override int Initialize()
        {
            int ret = 0;
            if ((ret = base.Initialize()) != 0) return ret;

            try
            {
                if (m_Communicator == null)
                    m_Communicator = new LSStrobeIlluminatorCommunicator();

                if (m_Communicator.IsOpen)
                {
                    m_bStop = true;
                    if (m_Task != null)
                    {
                        m_Task.Wait();
                    }
                    m_Communicator.Close();
                }

                m_Communicator.PortName = LSStrobeIlluminatorConfig.PortName;
                m_Communicator.BaudRate = LSStrobeIlluminatorConfig.BaudRate;
                m_Communicator.DataBits = LSStrobeIlluminatorConfig.DataBits;
                m_Communicator.Parity = LSStrobeIlluminatorConfig.Parity;
                m_Communicator.StopBits = LSStrobeIlluminatorConfig.StopBits;
                m_Communicator.Handshake = LSStrobeIlluminatorConfig.Handshake;
                m_Communicator.ReplyTimeout = LSStrobeIlluminatorConfig.TimeOut;

                m_Communicator.Open();

                //if (m_Communicator.IsOpen)
                //{
                //    m_bStop = false;
                //    m_Cts = new CancellationTokenSource();
                //    CancellationToken token = m_Cts.Token;
                //    m_Task = Task.Factory.StartNew(() =>
                //    {
                //        while (true)
                //        {
                //            if (m_bStop)
                //                break;

                //            if (CommandQueueCount() > 0)
                //            {
                //                SendTime();
                //            }

                //            token.Register(() =>
                //            {
                //                m_bStop = true;
                //            });
                //            Thread.Sleep(100);
                //        }
                //    });
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);

                ret = -1;
            }
            return ret;
        }

        public override void Close()
        {
            base.Close();
            if (m_Cts != null)
                m_Cts.Cancel();

            if (m_Communicator != null)
                m_Communicator.Close();
        }

        public override int StrobeOn(int channel)
        {
            int ret = 0;
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                m_Communicator.StrobeOn(channel);
            }
            return ret;
        }

        /*public override int SetStrobeOnTime(int channel, double settime)
        {
            int ret = 0;
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                double dValue = (int)settime;
                //수정 필요
                dValue += 0.005;

                //m_Communicator.SetTime(channel, settime);
                m_Communicator.SetTime(channel, dValue);
            }
            return ret;
        }*/

        public override int SetStrobeOnTime(int channel, double settime)
        {
            int ret = 0;
            if (IsSamePower(channel, settime))
            {
                return 0;
            }
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                double dValue = settime / 1000.0; // us -> ms

                // Min: 10us ~ Max: 60ms
                if (dValue < 0.01)
                    dValue = 0.01;
                else if (dValue > 60.0)
                    dValue = 60.0;

                m_Communicator.SetTime(channel, dValue);
            }
            return ret;
        }

        public override int SetStrobeOnTime(IlluminationDataSet illuminationDataSet)
        {
            int ret = 0;
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                StringBuilder builder = new StringBuilder();
                if (illuminationDataSet != null)
                {
                    int nTotalLength = 0;
                    List<byte[]> sendByteList = new List<byte[]>();

                    //TO DO : 동작은 되나, 더 나은 방법으로 수정 필요.
                    if (illuminationDataSet.Values.Count == 4)
                    {
                        string strCommand = string.Empty;
                        StringBuilder command = new StringBuilder();
                        command.Append("A");
                        int i = 0;
                        foreach (var item in illuminationDataSet.Values)
                        {
                            if(i != 0)
                                command.Append(",");

                            double value = (int)item.StrobeValue;
                            value += 0.005;

                            //command.Append(m_Communicator.GetTimeCommand(item.StrobeValue));
                            command.Append(m_Communicator.GetTimeCommand(value));
                            i++;
                        }

                        byte[] data = Encoding.ASCII.GetBytes(command.ToString());
                        int nLength = data.Length + 2;
                        byte[] array = new byte[nLength];
                        array[0] = LSStrobeIlluminatorCommunicator.Stx;
                        Array.Copy(data, 0, array, 1, data.Length);
                        array[nLength - 1] = LSStrobeIlluminatorCommunicator.Etx;

                        nTotalLength += nLength;
                        sendByteList.Add(array);
                        Log.Write("TopInspection", "SendBytes");
                        m_Communicator.SendBytes(array);
                        byte[] byteRecv;
                        m_Communicator.ReceiveFrame(out byteRecv, LSStrobeIlluminatorCommunicator.Etx);
                        //Console.WriteLine(byteRecv[0]);
                        Log.Write("TopInspection", "ReceiveFrame");
                    }
                    else
                    {
                        foreach (var item in illuminationDataSet.Values)
                        {
                            string strCommand = m_Communicator.SetTimeCommand(item.Channel, item.StrobeValue);

                            byte[] data = Encoding.ASCII.GetBytes(strCommand);
                            int nLength = data.Length + 2;
                            byte[] array = new byte[nLength];
                            array[0] = LSStrobeIlluminatorCommunicator.Stx;
                            Array.Copy(data, 0, array, 1, data.Length);
                            array[nLength - 1] = LSStrobeIlluminatorCommunicator.Etx;

                            nTotalLength += nLength;
                            sendByteList.Add(array);
                            builder.Append(strCommand);
                            m_Communicator.SendBytes(array);
                            byte[] byteRecv;
                            m_Communicator.ReceiveFrame(out byteRecv, LSStrobeIlluminatorCommunicator.Etx);
                            //Thread.Sleep(100);
                        }
                    }

                    byte[] sendBytes = new byte[nTotalLength];
                    int nStartIndex = 0;
                    foreach (var item in sendByteList)
                    {
                        Array.Copy(item, 0, sendBytes, nStartIndex, item.Length);
                        nStartIndex += item.Length;
                    }
                    //m_Communicator.SendBytes(sendBytes);
                    //m_Communicator.Send(builder.ToString());
                }



            }
            return ret;
        }

        public override int SetStrobeEdge(int channel, bool mode)
        {
            int ret = 0;
            if (m_Communicator != null && m_Communicator.IsOpen)
            {
                m_Communicator.SetEdgeMode(channel, mode);
            }
            return ret;
        }

        public override int SetValue(BrightSettingParameter parameter)
        {
            int ret = 0;

            TopBrightSettingParameter topParameter = parameter as TopBrightSettingParameter;
            BottomBrightSettingParameter bottomParameter = parameter as BottomBrightSettingParameter;
            
            if (topParameter != null)
            {
                double onTime = (double)topParameter.OnTime;
                double onTime2 = (double)topParameter.OnTime2;
                double onTime3 = (double)topParameter.OnTime3;
                double onTime4 = (double)topParameter.OnTime4;
                int ret1 = 0;
                int ret2 = 0;
                int ret3 = 0;
                int ret4 = 0;



                ret1 = SetStrobeOnTime(topParameter.Channel, onTime);

                ret2 = SetStrobeOnTime(topParameter.Channel2, onTime2);

                ret3 = SetStrobeOnTime(topParameter.Channel3, onTime3);
                ret4 = SetStrobeOnTime(topParameter.Channel4, onTime4);




                if (ret1 != 0)
                    ret = ret1;
                else if (ret2 != 0)
                    ret = ret2;
                else if (ret3 != 0)
                    ret = ret3;
                else if (ret4 != 0)
                    ret = ret4;
            }
            else if (bottomParameter != null)
            {
                double onTime = (double)bottomParameter.OnTime;
                double onTime2 = (double)bottomParameter.OnTime2;

                int ret1 = SetStrobeOnTime(bottomParameter.Channel, onTime);
                int ret2 = SetStrobeOnTime(bottomParameter.Channel2, onTime2);

                if (ret1 != 0)
                    ret = ret1;
                else if (ret2 != 0)
                    ret = ret2;
            }
            else if (parameter != null)
            {
                ret = SetStrobeOnTime(parameter.Channel, parameter.OnTime);
            }

            return ret;
        }
        bool IsEpsilon(double value)
        {
            return Math.Abs(value) < 0.00001; 
        }

        private bool IsSamePower(int channel, double onTime)
        {
            bool bresult = false;
            if(m_listBP.Count()> channel)
            {
                if (IsEpsilon(m_listBP[channel].OnTime - onTime))
                {
                    return true;
                }
                m_listBP[channel].OnTime = (int)onTime;
            }
            return bresult;
        }

        public override void Load(FileStream fs)
        {
            base.Load(fs);
            LSStrobeIlluminatorConfig config = null;
            SaveManager.BinaryDeserialize<LSStrobeIlluminatorConfig>(fs, out config);
            if (config == null)
            {
                config = new LSStrobeIlluminatorConfig();
            }
            LSStrobeIlluminatorConfig = config;
            LSStrobeIlluminatorConfig.Init();
        }

        public override void Save(FileStream fs)
        {
            base.Save(fs);
            SaveManager.BinarySerialize(fs, LSStrobeIlluminatorConfig);
        }
    }
}
