using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        private const int RepeatLogSummaryIntervalMs = 1000;
        private const int MaxRepeatStateCount = 4096;
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
        private AutoResetEvent m_logQueued;
        private readonly object m_repeatSync = new object();
        private readonly Dictionary<string, RepeatLogState> m_repeatStates = new Dictionary<string, RepeatLogState>(StringComparer.Ordinal);
		private readonly string m_strLogPath;
		#endregion


		public LogManager()
        {
			m_strLogPath = Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\")) + "\\Log";
			m_WriteLoglevel = LogLevel.Lowest;
			m_queLogs = new Queue();
			m_tokenSourceCancel = new CancellationTokenSource();
			m_tokenCancel = m_tokenSourceCancel.Token;
            m_logQueued = new AutoResetEvent(false);
            m_LogWritor = Task.Factory.StartNew(
                () => WriteLogProcedure(),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
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
                string filteredMessage;
                if (!TryFilterRepeatedLog(level, strClassification, source, message, out filteredMessage))
                    return;

				lock (m_queLogs.SyncRoot)
				{
					m_queLogs.Enqueue(new LogInfo(level, source, filteredMessage, strClassification));
				}
                SignalLogQueued();
			}
		}

        public void Write(LogLevel level, string strClassification, string Op_User, string source, string message)
        {
            if (CheckLogLevel(level))
            {
                string logSource = string.Format(" [사용자 : {0}]  {1} ", Op_User, source);
                string filteredMessage;
                if (!TryFilterRepeatedLog(level, strClassification, logSource, message, out filteredMessage))
                    return;

                lock (m_queLogs.SyncRoot)
                {
                    m_strTemp = logSource;

                    m_queLogs.Enqueue(new LogInfo(level, m_strTemp, filteredMessage, strClassification));
                }
                SignalLogQueued();
            }
        }

        private bool TryFilterRepeatedLog(LogLevel level, string classification, string source, string message, out string filteredMessage)
        {
            filteredMessage = message ?? string.Empty;

            try
            {
                if (level >= LogLevel.AboveNormal || !IsRepeatThrottleCandidate(filteredMessage))
                    return true;

                string key = (classification ?? string.Empty) + "\n" + (source ?? string.Empty) + "\n" + filteredMessage;
                int now = Environment.TickCount;

                lock (m_repeatSync)
                {
                    if (m_repeatStates.Count > MaxRepeatStateCount)
                        m_repeatStates.Clear();

                    RepeatLogState state;
                    if (!m_repeatStates.TryGetValue(key, out state))
                    {
                        m_repeatStates[key] = new RepeatLogState { LastEmitTick = now };
                        return true;
                    }

                    int elapsedMs = unchecked(now - state.LastEmitTick);
                    if (elapsedMs < RepeatLogSummaryIntervalMs)
                    {
                        state.SuppressedCount++;
                        return false;
                    }

                    int suppressed = state.SuppressedCount;
                    state.LastEmitTick = now;
                    state.SuppressedCount = 0;

                    if (suppressed > 0)
                        filteredMessage = filteredMessage + " [repeat suppressed: " + suppressed + ", intervalMs=" + elapsedMs + "]";

                    return true;
                }
            }
            catch
            {
                filteredMessage = message ?? string.Empty;
                return true;
            }
        }

        private static bool IsRepeatThrottleCandidate(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            if (IndexOf(message, "- Failed") >= 0 ||
                IndexOf(message, "- Stopped") >= 0 ||
                IndexOf(message, "- Alarm") >= 0 ||
                IndexOf(message, "- Start") >= 0 ||
                IndexOf(message, "- Ok") >= 0)
            {
                return false;
            }

            return IndexOf(message, "- Wait") >= 0 ||
                   IndexOf(message, " - Wait") >= 0 ||
                   IndexOf(message, "- Check") >= 0 ||
                   IndexOf(message, " - Check") >= 0 ||
                   IndexOf(message, "Gate") >= 0 ||
                   IndexOf(message, "Pending") >= 0;
        }

        private static int IndexOf(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class RepeatLogState
        {
            public int LastEmitTick;
            public int SuppressedCount;
        }

        private void SignalLogQueued()
        {
            try
            {
                AutoResetEvent signal = m_logQueued;
                if (signal != null)
                    signal.Set();
            }
            catch
            {
            }
        }


        private void WriteLogProcedure()
        {
			List<LogInfo> listLog = new List<LogInfo>();
			bool bExit = false;
			m_tokenCancel.Register(() =>
			{
				bExit = true;
                SignalLogQueued();
			});
			
			while(true)
            {
				try
				{
                    AutoResetEvent signal = m_logQueued;
                    if (signal != null)
                        signal.WaitOne(100);

                    DrainQueuedLogs(listLog);
					WriteLog(listLog);

					if (bExit)
                    {
                        DrainQueuedLogs(listLog);
                        WriteLog(listLog);
						break;
                    }
				}
				catch (Exception ex)
				{
					Log.Write(ex);
                }
			}

			m_tokenSourceCancel.Dispose();
            try { if (m_logQueued != null) m_logQueued.Dispose(); } catch { }

		}

        private void DrainQueuedLogs(List<LogInfo> listLog)
        {
            if (listLog == null)
                return;

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
        }

		protected void WriteLog(List<LogInfo> listLog)
        {
            if (listLog == null || listLog.Count == 0)
                return;

			if(!Directory.Exists(m_strLogPath))
            {
				Directory.CreateDirectory(m_strLogPath);
			}

            Dictionary<string, StringBuilder> buffers = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
			foreach (LogInfo log in listLog)
            {
				BufferLogLine(log, buffers);
            }

            foreach (KeyValuePair<string, StringBuilder> item in buffers)
                WriteLog(item.Value, item.Key);
        }

        private void BufferLogLine(LogInfo log, Dictionary<string, StringBuilder> buffers)
        {
            if (log == null || buffers == null)
                return;

            string strFileName = string.Format("{0}\\{1}_{2}.log", m_strLogPath, log.Classification, log.CreationDate);
            string strAllLog = string.Format("{0}\\CDT-320{1}.log", m_strLogPath, log.CreationDate);

            if (GetFileSize(strAllLog) > 4000000)
            {
                string strAllLog_Target = string.Format("{0}\\CDT-320{1}_{2}.log", m_strLogPath, log.CreationDate, Environment.TickCount);
                System.IO.File.Move(strAllLog, strAllLog_Target);
            }

            if (GetFileSize(strFileName) > 4000000)
            {
                string strFileName_Target = string.Format("{0}\\{1}_{2}_{3}.log", m_strLogPath, log.Classification, log.CreationDate, Environment.TickCount);
                System.IO.File.Move(strFileName, strFileName_Target);
            }

            AppendBufferedLine(buffers, strFileName, log.ToString());
            // LCP_280 통합 미러: 기존에는 Normal 이상 전부를 복제해 분류별 로그와 이중 저장됐다.
            // 최소 로그 정책에 따라 AboveNormal(Warning) 이상만 통합 파일에 복제한다.
            if (log.Level >= LogLevel.AboveNormal)
                AppendBufferedLine(buffers, strAllLog, log.ToString());
        }

        private static void AppendBufferedLine(Dictionary<string, StringBuilder> buffers, string path, string line)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            StringBuilder buffer;
            if (!buffers.TryGetValue(path, out buffer))
            {
                buffer = new StringBuilder();
                buffers[path] = buffer;
            }

            buffer.AppendLine(line ?? string.Empty);
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


            string strAllLog_Target = "";
            string strAllLog = string.Format("{0}\\CDT-320{1}.log", m_strLogPath, log.CreationDate);
            //	용량이 4MB 이상일 경우, 현재 로그파일 이름을 변경하고 다시 저장하기 시작한다.
            if (GetFileSize(strAllLog) > 4000000)
            {
                strAllLog_Target = string.Format("{0}\\CDT-320{1}_{2}.log", m_strLogPath, log.CreationDate, Environment.TickCount);
                System.IO.File.Move(strAllLog, strAllLog_Target);
            }

            if (GetFileSize(strFileName) > 4000000)
            {
                strFileName_Target = string.Format("{0}\\{1}_{2}_{3}.log", m_strLogPath, log.Classification, log.CreationDate, Environment.TickCount);
                System.IO.File.Move(strFileName, strFileName_Target);
            }

            WriteLog(log, strFileName);
            // LCP_280 통합 미러는 AboveNormal(Warning) 이상만 복제한다(정상 로그 이중 저장 제거).
            if (log.Level >= LogLevel.AboveNormal)
            {
                WriteLog(log, strAllLog);
            }
        }

        private static void WriteLog(LogInfo log, string strFileName)
        {
            using (StreamWriter writer = new StreamWriter(strFileName, true))
            {
                writer.WriteLine(log.ToString());
                writer.Close();
            }
        }

        private static void WriteLog(StringBuilder buffer, string strFileName)
        {
            if (buffer == null || buffer.Length == 0)
                return;

            using (StreamWriter writer = new StreamWriter(strFileName, true))
            {
                writer.Write(buffer.ToString());
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
            SignalLogQueued();
		}

		public string GetLogPath()
		{
			return m_strLogPath;
		}


        // [추가] 오래된 로그 삭제 기능 구현
        // [수정] 기존 로그 삭제 함수는 범용 함수를 호출하도록 변경 (하위 호환성 유지)
        public void DeleteOldLogs(int keepDays)
        {
            // 기본 로그 경로, .log 파일 대상
            DeleteOldFiles(m_strLogPath, keepDays, "*.log");
        }

        // [추가] 폴더 경로와 파일 패턴을 지정하여 오래된 파일 삭제 (이미지 등 다른 파일 정리에 사용 가능)
        public void DeleteOldFiles(string folderPath, int keepDays, string searchPattern = "*.*")
        {
            // 백그라운드 태스크로 실행
            Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                        return;

                    // 기준 날짜 계산 (오늘 - 보관일수)
                    DateTime limitDate = DateTime.Today.AddDays(-keepDays);

                    // 해당 폴더에서 패턴에 맞는 파일 목록 조회
                    string[] files = Directory.GetFiles(folderPath, searchPattern);

                    // 날짜 패턴 (YYYY-MM-DD) 찾기 위한 정규식
                    Regex dateRegex = new Regex(@"(\d{4}-\d{2}-\d{2})");

                    foreach (string file in files)
                    {
                        try
                        {
                            bool bDelete = false;
                            string fileName = Path.GetFileName(file);
                            Match match = dateRegex.Match(fileName);

                            if (match.Success)
                            {
                                // 1. 파일명에 날짜가 있는 경우 (예: Image_2026-02-04.jpg)
                                if (DateTime.TryParse(match.Value, out DateTime fileDate))
                                {
                                    if (fileDate < limitDate)
                                        bDelete = true;
                                }
                            }
                            else
                            {
                                // 2. 파일명에 날짜가 없는 경우 -> 파일의 수정 날짜(LastWriteTime) 기준
                                FileInfo fi = new FileInfo(file);
                                if (fi.LastWriteTime.Date < limitDate)
                                    bDelete = true;
                            }

                            if (bDelete)
                            {
                                File.Delete(file);
                                // System.Diagnostics.Debug.WriteLine($"Deleted: {file}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 개별 파일 삭제 실패(사용중 등) 시 로그만 남기고 계속 진행
                            System.Diagnostics.Debug.WriteLine($"Failed to delete {file}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 폴더 접근 권한 등 전체 에러
                    System.Diagnostics.Debug.WriteLine($"File cleanup failed: {ex.Message}");
                }
            });
        }

        // [추가] 오래된 폴더 삭제 기능 (하위 폴더 포함 삭제)
        public void DeleteOldFolders(string rootPath, int keepDays)
        {
            Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                        return;

                    DateTime limitDate = DateTime.Today.AddDays(-keepDays);
                    string[] directories = Directory.GetDirectories(rootPath);
                    Regex dateRegex = new Regex(@"(\d{4}-\d{2}-\d{2})|(\d{8})"); // YYYY-MM-DD 또는 YYYYMMDD

                    foreach (string dir in directories)
                    {
                        try
                        {
                            bool bDelete = false;
                            string dirName = new DirectoryInfo(dir).Name;
                            Match match = dateRegex.Match(dirName);

                            if (match.Success)
                            {
                                // 1. 폴더명에 날짜가 있는 경우 (예: 2024-02-04 또는 20240204)
                                string dateStr = match.Value;

                                // YYYYMMDD 형식이면 YYYY-MM-DD로 변환 시도
                                if (dateStr.Length == 8 && !dateStr.Contains("-"))
                                {
                                    dateStr = dateStr.Insert(4, "-").Insert(7, "-");
                                }

                                if (DateTime.TryParse(dateStr, out DateTime folderDate))
                                {
                                    if (folderDate < limitDate)
                                        bDelete = true;
                                }
                            }
                            else
                            {
                                // 2. 폴더명에 날짜가 없는 경우 -> 폴더 생성일(CreationTime) 기준
                                DirectoryInfo di = new DirectoryInfo(dir);
                                if (di.CreationTime.Date < limitDate)
                                    bDelete = true;
                            }

                            if (bDelete)
                            {
                                // recursive: true -> 하위 파일/폴더까지 모두 삭제
                                Directory.Delete(dir, true);
                                // System.Diagnostics.Debug.WriteLine($"Deleted folder: {dir}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete folder {dir}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Folder cleanup failed: {ex.Message}");
                }
            });
        }
    }
}
