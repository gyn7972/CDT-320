using QMC.Common.Vision.Tools;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Vision.Optics.ImageFocus
{
    public class LSStrobeIlluminatorCommunicator : SerialComm
    {
        //#region Field
        public const byte Stx = 0x02; //\r
        public const byte Etx = 0x03; //\n
        private byte[] Frame = { (byte)LSStrobeIlluminatorCommunicator.Etx };

        //private MethodCallerPool m_Methods;
        //#endregion

        private readonly object m_SyncRoot;

        //private const byte Etx1 = 0x0D; //\r
        //private const byte Etx2 = 0x0A; //\n
        //private byte[] Etx = { LSStrobeIlluminator.Etx1, LSStrobeIlluminator.Etx2 };

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
        public int MaxRetryCount { set; get; } = 3;
        public int RetryDelay { set; get; } = 100; // ms

        public bool IsOpen => m_SerialPort.IsOpen;

        List<string> m_ReceiveDataList = new List<string>();
        Queue<CommandInfo> m_SendDataList = new Queue<CommandInfo>();
        private List<byte> m_ReceiveBuffer = new List<byte>();
        private readonly object m_ReceiveBufferLock = new object();

        public LSStrobeIlluminatorCommunicator() : base()
        {
            m_SyncRoot = new object();
            this.m_SerialPort.DataReceived += M_SerialPort_DataReceived;
        }

        private class CommandInfo
        {
            public string Command { get; set; }
            public int RetryCount { get; set; }
            public DateTime SendTime { get; set; }

            public CommandInfo(string command)
            {
                Command = command;
                RetryCount = 0;
                SendTime = DateTime.Now;
            }
        }

        private void M_SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (m_SerialPort.IsOpen == false)
                    return;

                int bytesToRead = m_SerialPort.BytesToRead;
                if (bytesToRead <= 0)
                    return;

                byte[] buffer = new byte[bytesToRead];
                int bytesRead = m_SerialPort.Read(buffer, 0, bytesToRead);

                lock (m_ReceiveBufferLock)
                {
                    // 수신한 바이트를 버퍼에 추가
                    for (int i = 0; i < bytesRead; i++)
                    {
                        m_ReceiveBuffer.Add(buffer[i]);
                    }

                    // 완전한 프레임이 있는지 확인 (STX로 시작, ETX로 끝)
                    ProcessCompleteFrames();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[EXCEPTION] M_SerialPort_DataReceived: {0}", ex.Message));
                Log.Write("serial", string.Format("[EXCEPTION] M_SerialPort_DataReceived: {0}", ex.Message));
            }
        }

        private void ProcessCompleteFrames()
        {
            while (true)
            {
                // STX 찾기
                int stxIndex = m_ReceiveBuffer.IndexOf(Stx);
                
                // STX가 없으면 종료
                if (stxIndex < 0)
                {
                    // STX가 없으면 버퍼의 모든 데이터 삭제 (불완전한 데이터)
                    m_ReceiveBuffer.Clear();
                    break;
                }

                // STX 이전의 데이터 제거
                if (stxIndex > 0)
                {
                    m_ReceiveBuffer.RemoveRange(0, stxIndex);
                }

                // ETX 찾기 (STX 다음부터)
                int etxIndex = -1;
                for (int i = 1; i < m_ReceiveBuffer.Count; i++)
                {
                    if (m_ReceiveBuffer[i] == Etx)
                    {
                        etxIndex = i;
                        break;
                    }
                }

                // ETX가 없으면 아직 완전한 프레임이 아니므로 대기
                if (etxIndex < 0)
                {
                    break;
                }

                // 완전한 프레임 추출 (STX ~ ETX)
                int frameLength = etxIndex + 1;
                byte[] frameData = m_ReceiveBuffer.GetRange(0, frameLength).ToArray();
                
                // 처리된 프레임을 버퍼에서 제거
                m_ReceiveBuffer.RemoveRange(0, frameLength);

                // 프레임 처리 (STX와 ETX를 제외한 실제 데이터만 추출)
                if (frameLength > 2) // STX + 데이터 + ETX 최소 길이
                {
                    byte[] actualData = new byte[frameLength - 2];
                    Array.Copy(frameData, 1, actualData, 0, frameLength - 2);
                    
                    string receivedData = Encoding.ASCII.GetString(actualData).Trim();
                    
                    Console.WriteLine(string.Format("[RECV] {0}", receivedData));
                    Log.Write("serial", string.Format("[RECV] {0}", receivedData));

                    lock (m_ReceiveDataList)
                    {
                        m_ReceiveDataList.Add(receivedData);
                    }

                    // 응답 분석
                    AnalyzeResponse(receivedData);
                }
            }
        }

        private void AnalyzeResponse(string receivedData)
        {
            // RERR 체크 - 에러 응답인 경우 명령어를 다시 보냄
            if (receivedData.Contains("RERR") || receivedData.EndsWith(ErrorCode))
            {
                Console.WriteLine("[ERROR] Invalid command error received: RERR");
                Log.Write("serial", "[ERROR] Invalid command error received: RERR");
                
                // 전송했던 명령어 확인 및 재전송
                lock (m_SendDataList)
                {
                    if (m_SendDataList.Count > 0)
                    {
                        CommandInfo failedCommand = m_SendDataList.Dequeue();
                        
                        Console.WriteLine(string.Format("[ERROR] Failed command: {0}, RetryCount: {1}", 
                            failedCommand.Command, failedCommand.RetryCount));
                        Log.Write("serial", string.Format("[ERROR] Failed command: {0}, RetryCount: {1}", 
                            failedCommand.Command, failedCommand.RetryCount));

                        // 재시도 횟수 확인
                        if (failedCommand.RetryCount < MaxRetryCount)
                        {
                            failedCommand.RetryCount++;
                            Task.Run(() =>
                            {
                                Thread.Sleep(RetryDelay);
                                ResendCommand(failedCommand);
                            });
                        }
                        else
                        {
                            Console.WriteLine(string.Format("[ERROR] Max retry count exceeded for command: {0}", 
                                failedCommand.Command));
                            Log.Write("serial", string.Format("[ERROR] Max retry count exceeded for command: {0}", 
                                failedCommand.Command));
                        }
                    }
                }
            }
            // 정상 응답인 경우 (R로 시작)
            else if (receivedData.StartsWith(StartText))
            {
                Console.WriteLine(string.Format("[SUCCESS] Valid response: {0}", receivedData));
                Log.Write("serial", string.Format("[SUCCESS] Valid response: {0}", receivedData));
                
                lock (m_SendDataList)
                {
                    if (m_SendDataList.Count > 0)
                    {
                        CommandInfo successCommand = m_SendDataList.Dequeue();
                        Console.WriteLine(string.Format("[SUCCESS] Command completed: {0}", successCommand.Command));
                        Log.Write("serial", string.Format("[SUCCESS] Command completed: {0}", successCommand.Command));
                    }
                }
            }
            // FMT 메시지인 경우 (일정 시간마다 수신)
            else if (receivedData.Contains("FMT"))
            {
                Console.WriteLine("[INFO] FMT message received");
                Log.Write("serial", "[INFO] FMT message received");
            }
            // 수동 조작 응답인 경우 (T로 시작)
            else if (receivedData.StartsWith("T"))
            {
                Console.WriteLine(string.Format("[INFO] Manual control response: {0}", receivedData));
                Log.Write("serial", string.Format("[INFO] Manual control response: {0}", receivedData));
            }
            // 기타 응답
            else
            {
                Console.WriteLine(string.Format("[WARN] Unexpected response: {0}", receivedData));
                Log.Write("serial", string.Format("[WARN] Unexpected response: {0}", receivedData));
            }
        }

        private void ResendCommand(CommandInfo commandInfo)
        {
            try
            {
                if (m_SerialPort.IsOpen == false)
                {
                    Console.WriteLine("[ERROR] Cannot resend - port is closed.");
                    return;
                }

                lock (m_SendDataList)
                {
                    m_SendDataList.Enqueue(commandInfo);
                }

                commandInfo.SendTime = DateTime.Now;
                int ret = SendFrame(commandInfo.Command, Stx, Etx);
                
                if (ret != 0)
                {
                    Console.WriteLine(string.Format("[ERROR] Resend failed for command: {0}", commandInfo.Command));
                    Log.Write("serial", string.Format("[ERROR] Resend failed for command: {0}", commandInfo.Command));
                }
                else
                {
                    Console.WriteLine(string.Format("[RESEND] {0}", commandInfo.Command));
                    Log.Write("serial", string.Format("[RESEND] {0}", commandInfo.Command));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[EXCEPTION] ResendCommand: {0}", ex.Message));
                Log.Write("serial", string.Format("[EXCEPTION] ResendCommand: {0}", ex.Message));
            }
        }

        public int Send(string text)
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

            // m_SendDataList가 비워질 때까지 대기 (최대 5초)
            DateTime waitStartTime = DateTime.Now;
            bool isCleared = false;
            
            while (true)
            {
                int queueCount = 0;
                lock (m_SendDataList)
                {
                    queueCount = m_SendDataList.Count;
                }

                // 큐가 비어있으면 전송 진행
                if (queueCount == 0)
                {
                    break;
                }

                // 5초 이상 대기 시 큐 강제 클리어
                TimeSpan waitTime = DateTime.Now - waitStartTime;
                if (waitTime.TotalSeconds >= 5.0)
                {
                    lock (m_SendDataList)
                    {
                        Console.WriteLine(string.Format("[WARN] Send queue timeout ({0} items). Clearing queue.", m_SendDataList.Count));
                        Log.Write("serial", string.Format("[WARN] Send queue timeout ({0} items). Clearing queue.", m_SendDataList.Count));
                        m_SendDataList.Clear();
                        isCleared = true;
                    }
                    break;
                }

                // 10ms 대기 후 재확인
                Thread.Sleep(10);
            }

            if (isCleared)
            {
                Console.WriteLine("[INFO] Queue cleared. Proceeding with new command.");
                Log.Write("serial", "[INFO] Queue cleared. Proceeding with new command.");
            }

            // 명령어를 큐에 추가
            CommandInfo newCommand = new CommandInfo(text);
            lock(m_SendDataList)
            {
                m_SendDataList.Enqueue(newCommand);
            }

            // 프레임 전송
            if ((ret = SendFrame(text, Stx, Etx)) != 0)
            {
                Console.WriteLine("Send failed.");
                
                // 전송 실패 시 큐에서 제거
                lock(m_SendDataList)
                {
                    if (m_SendDataList.Count > 0)
                    {
                        // 방금 추가한 명령어가 맨 앞에 있는지 확인하고 제거
                        CommandInfo dequeuedCommand = m_SendDataList.Peek();
                        if (dequeuedCommand.Command == text)
                        {
                            m_SendDataList.Dequeue();
                        }
                    }
                }
                return -1;
            }

            Console.WriteLine(string.Format("[SEND] {0}", text));
            Log.Write("serial", string.Format("[SEND] {0}", text));

            // 응답 대기 (큐가 비워질 때까지 대기)
            DateTime responseWaitStartTime = DateTime.Now;
            int responseTimeout = ReplyTimeout > 0 ? ReplyTimeout : 5000; // 기본 5초
            
            while (true)
            {
                int queueCount = 0;
                lock (m_SendDataList)
                {
                    queueCount = m_SendDataList.Count;
                }

                // 큐가 비어있으면 응답을 받은 것으로 간주
                if (queueCount == 0)
                {
                    Console.WriteLine(string.Format("[SUCCESS] Response received for command: {0}", text));
                    Log.Write("serial", string.Format("[SUCCESS] Response received for command: {0}", text));
                    return 0;
                }

                // 타임아웃 체크
                TimeSpan responseWaitTime = DateTime.Now - responseWaitStartTime;
                if (responseWaitTime.TotalMilliseconds >= responseTimeout)
                {
                    Console.WriteLine(string.Format("[TIMEOUT] No response received for command: {0} (waited {1}ms)", 
                        text, responseWaitTime.TotalMilliseconds));
                    Log.Write("serial", string.Format("[TIMEOUT] No response received for command: {0} (waited {1}ms)", 
                        text, responseWaitTime.TotalMilliseconds));
                    
                    // 타임아웃 시 큐에서 제거
                    lock (m_SendDataList)
                    {
                        if (m_SendDataList.Count > 0)
                        {
                            CommandInfo timeoutCommand = m_SendDataList.Peek();
                            if (timeoutCommand.Command == text)
                            {
                                m_SendDataList.Dequeue();
                                Console.WriteLine(string.Format("[TIMEOUT] Removed timed-out command from queue: {0}", text));
                                Log.Write("serial", string.Format("[TIMEOUT] Removed timed-out command from queue: {0}", text));
                            }
                        }
                    }
                    
                    return -2; // 타임아웃 에러 코드
                }

                // 10ms 대기 후 재확인
                Thread.Sleep(10);
            }
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
                if ((ret = this.ReceiveFrame(out data, LSStrobeIlluminatorCommunicator.Etx)) != 0)
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

        public int StrobeOn(int channel)
        {
            int ret = 0;
            ret = Send(GetTRGCommand(channel));
            return ret;
        }

        public int SetTime(int channel, double settime)
        {
            int ret = 0;
            string strRecv = string.Empty;
            ret = Send(GetTimeCommand(channel, settime));
            
            return ret;
        }

        public string SetTimeCommand(int channel, double settime)
        {
            return GetTimeCommand(channel, settime);
        }

        public int SetEdgeMode(int channel, bool mode)
        {
            int ret = 0;
            ret = Send(GetEdgeCommand(channel, mode));
            return ret;
        }

        private string MakeFrame(string strData)
        {
            string strRet;
            strRet = string.Format("{0}", strData);
            return strRet;
        }

        private string GetTRGCommand(int channel)
        {
            string strData;
            strData = string.Format("{0}TRG-ON", channel);
            
            return MakeFrame(strData);
        }

        private string GetEdgeCommand(int channel, bool mode)
        {
            string strData;

            if (mode==false)
            {
                strData = string.Format("{0}W-EG-F", channel);
            }
            else
            {
                strData = string.Format("{0}W-EG-R", channel);
            }
            
            return MakeFrame(strData);
        }

        private string GetTimeCommand(int channel, double settime)
        {
            string strData;
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat("{0}", channel);
            //if (settime < 10.000)
            //{
            //    stringBuilder.Append(" ");
            //}
            stringBuilder.AppendFormat("{0:0.000}", settime);
            strData = stringBuilder.ToString();
            
            return MakeFrame(strData);
        }

        public string GetTimeCommand(double settime)
        {
            string strData;
            StringBuilder stringBuilder = new StringBuilder();

            if (settime < 10.000)
            {
                stringBuilder.Append(" ");
            }
            stringBuilder.AppendFormat("{0:0.000}", settime);
            strData = stringBuilder.ToString();

            return MakeFrame(strData);
        }
    }
}
