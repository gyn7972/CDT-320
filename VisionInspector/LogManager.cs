using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // Ensure you have added a reference to System.Windows.Forms.dll in your project.

namespace QMC.Common
{
	[Serializable]
	public enum LogLevel
	{
		/// <summary>
		/// 가장 낮은 중요도입니다.
		/// </summary>
		Lowest,
		/// <summary>
		/// 보통보다 낮은 중요도입니다.
		/// </summary>
		BelowNormal,
		/// <summary>
		/// 보통의 중요도입니다.
		/// </summary>
		Normal,
		/// <summary>
		/// 보통보다 높은 중요도입니다.
		/// </summary>
		AboveNormal,
		/// <summary>
		/// 가장 높은 중요도입니다.
		/// </summary>
		Highest,
		/// <summary>
		/// 로그를 기록하지 않습니다.
		/// </summary>
		Skip
	}

	public class LogManager
    {
		private string m_strTemp;

        #region Singleton 
        private static LogManager g_logManager;
        public static LogManager Instance
        {
            get
            {
                if (g_logManager == null)
                    g_logManager = new LogManager();
                return g_logManager;
            }
        }
        #endregion

        #region field
		private LogLevel m_WriteLoglevel;
		private Queue m_queLogs;
		private Task m_LogWritor;
		private CancellationTokenSource m_tokenSourceCancel;
		private CancellationToken m_tokenCancel;
		private readonly string m_strLogPath;
		#endregion


		public LogManager()
        {
			m_strLogPath = Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Log";
			m_WriteLoglevel = LogLevel.Lowest;
			m_queLogs = new Queue();
			m_tokenSourceCancel = new CancellationTokenSource();
			m_tokenCancel = m_tokenSourceCancel.Token;
			m_LogWritor = new Task(() => WriteLogProcedure() );
			m_LogWritor.Start();
		}


		public void SetWriterLogLevel(LogLevel level)
        {
			m_WriteLoglevel = level;
        }
		protected bool CheckLogLevel()
        {
			return CheckLogLevel(LogLevel.Normal);
        }
		protected bool CheckLogLevel(LogLevel level)
        {
			bool bRet = false;

			if((int)m_WriteLoglevel <= (int)level)
            {
				bRet = true;
            }

			return bRet;
        }
		
		public void Write(LogLevel level, string strClassification, string message)
		{
			Write(level, strClassification, string.Empty, message);
		}

		public void Write(LogLevel level, string strClassification, string source, string message)
		{
			if (CheckLogLevel(level))
			{
				lock (m_queLogs.SyncRoot)
				{
					m_queLogs.Enqueue(new LogInfo(level, source, message, strClassification));
				}
			}
		}

        public void Write(LogLevel level, string strClassification, string Op_User, string source, string message)
        {
            if (CheckLogLevel(level))
            {
                lock (m_queLogs.SyncRoot)
                {
                    m_strTemp = string.Format(" [사용자 : {0}]  {1} ", Op_User, source);

                    m_queLogs.Enqueue(new LogInfo(level, m_strTemp, message, strClassification));
                }
            }
        }


        private void WriteLogProcedure()
        {
			List<LogInfo> listLog = new List<LogInfo>();
			bool bExit = false;
			m_tokenCancel.Register(() =>
			{
				bExit = true;
			});
			
			while(true)
            {
				try
				{

					if (bExit)
						break;

					lock (m_queLogs.SyncRoot)
					{
						listLog.Clear();
						while (m_queLogs.Count > 0)
						{
							LogInfo log = m_queLogs.Dequeue() as LogInfo;
							if (log != null)
								listLog.Add(log);
						}
					}

					WriteLog(listLog);
					Thread.Sleep(1);
				}
				catch (Exception ex)
				{	
				}
			}

			m_tokenSourceCancel.Dispose();

		}

		protected void WriteLog(List<LogInfo> listLog)
        {
			if(!Directory.Exists(m_strLogPath))
            {
				Directory.CreateDirectory(m_strLogPath);
			}

			foreach (LogInfo log in listLog)
            {
				WriteLog(log);
            }
        }

		protected void WriteLog(LogInfo log)
		{
			//	원래 것
            //string strFileName = string.Format("{0}\\{1}_{2}.log", m_strLogPath, log.Classification, log.CreationDate);
            //using (StreamWriter writer = new StreamWriter(strFileName, true))
            //{
            //	writer.WriteLine(log.ToString());
            //	writer.Close();
            //}


			//	용량 분할
            string strFileName_Target = "";
            string strFileName = string.Format("{0}\\{1}_{2}.log", m_strLogPath, log.Classification, log.CreationDate);

            //	용량이 4MB 이상일 경우, 현재 로그파일 이름을 변경하고 다시 저장하기 시작한다.
            if (GetFileSize(strFileName) > 4000000)
            {
                strFileName_Target = string.Format("{0}\\{1}_{2}_{3}.log", m_strLogPath, log.Classification, log.CreationDate, Environment.TickCount);
                System.IO.File.Move(strFileName, strFileName_Target);
            }

            using (StreamWriter writer = new StreamWriter(strFileName, true))
            {
                writer.WriteLine(log.ToString());
                writer.Close();
            }
		}

        public long GetFileSize(string filePath)
        {
            long fileSize = 0;
            if (File.Exists(filePath))
            {
                FileInfo info = new FileInfo(filePath);
                fileSize = info.Length;
            }

            return fileSize;
        }

        public void WriteTxt(string name, string message)
		{
			string strFileName = string.Format("{0}\\{1}.txt", m_strLogPath, name);

			using (StreamWriter writer = new StreamWriter(strFileName, true))
			{
				writer.WriteLine(message);
				writer.Close();
			}
		}

        public int WriteWorkLog(string strData)
        {
            int ret = 0;

            DateTime today = DateTime.Today;

            string strFolderPath = @"D:\Log\" + DateTime.Now.ToString("yyyyMMdd");
            DirectoryInfo di = new DirectoryInfo(strFolderPath);

            string strFilePath = strFolderPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            string strLog = strData;

            StreamWriter writer;
            if (File.Exists(strFilePath))
            {
                writer = File.AppendText(strFilePath);
                writer.WriteLine(strLog);
                writer.Close();
            }
            else
            {
                if (di.Exists == false)
                {
                    di.Create();
                }
                writer = File.CreateText(strFilePath);
                writer.WriteLine(strLog);
                writer.Close();
            }

            return ret;
        }

        public void Close()
        {
			m_tokenSourceCancel.Cancel();
		}

		public string GetLogPath()
		{
			return m_strLogPath;
		}
    }
}
