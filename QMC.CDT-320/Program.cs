using System;
using System.Threading;
using System.Windows.Forms;
using QMC.Common.Diagnostics;

namespace QMC.CDT_320
{
    internal static class Program
    {
        // Single-instance mutex
        private const string MUTEX_NAME = @"Global\QMC.CDT-320.SingleInstance";
        private static Mutex _instanceMutex;
        private static int _fatalHandling;

        [STAThread]
        static void Main()
        {
            // Single-instance check — 이미 실행 중이면 종료
            bool created;
            _instanceMutex = new Mutex(true, MUTEX_NAME, out created);
            if (!created)
            {
                QMC.Common.MessageDialog.Show("CDT-320 Handler 가 이미 실행 중입니다.",
                                "Single Instance",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                try
                {
                    if (QMC.Common.Win32Timer.SetHighResolution())
                    {
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Event,
                            "NONE",
                            "WIN32-TIMER",
                            "High resolution timer enabled. resolution=1ms");
                    }
                }
                catch (Exception ex)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning,
                        "NONE",
                        "WIN32-TIMER",
                        "High resolution timer enable failed: " + ex.Message);
                }

                // 전역 예외 핸들러 — 시작/런타임 중 처리되지 않은 예외가 조용히 프로세스를
                // 종료시키는 것을 막고, 원인을 로그 + 메시지박스로 남긴다 (AGENTS.md 예외 규칙).
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => HandleFatalException(e.Exception, "UI-THREAD");
                AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleFatalException(e.ExceptionObject as Exception, "APP-DOMAIN");

                // 저장된 로그 저장방식/경로를 로거에 주입한다(로그 쓰기/읽기 경로 일치). 앱 시작 시 1회, 이후 로그부터 적용.
                try
                {
                    var logCfg = QMC.CDT320.AppSettingsStore.Current;
                    QMC.Common.Logging.EventLogger.ConfigureLogPathsByName(logCfg.LogSplitByKind, logCfg.LogAllDir, logCfg.LogKindPaths);
                }
                catch { }

                // 기존에 쌓인 이벤트 로그의 메시지 종류를 번역 카탈로그에 1회 시드한다(백그라운드).
                // 메시지편집 페이지가 과거 메시지까지 바로 보이도록 하되, UI 시작은 막지 않는다(마커로 1회만 실행).
                QMC.Common.Logging.MessageCatalog.SeedFromLogsInBackground();

                // 로그 보존기간 관리 시작 — 보존일수(설정)가 지난 로그를 Log\Archive 에 압축 보관한다.
                // (시작 30초 후 1회 + 24시간마다, 백그라운드. 보존일수 0 이면 아무것도 하지 않음)
                QMC.CDT320.LogRetentionService.Start();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            finally
            {
                try { QMC.Common.Win32Timer.RestoreResolution(); } catch { }
                try { _instanceMutex?.ReleaseMutex(); } catch { }
                try { _instanceMutex?.Dispose(); } catch { }
            }
        }

        /// <summary>처리되지 않은 예외를 로그에 기록하고 사용자에게 원인을 표시한다.</summary>
        private static void HandleFatalException(Exception ex, string source)
        {
            if (Interlocked.Exchange(ref _fatalHandling, 1) != 0)
            {
                try { Environment.FailFast("Recursive fatal exception: " + source, ex); } catch { }
                return;
            }

            string detail = ex?.ToString() ?? "Unknown exception (null)";
            string dumpPath = null;

            try
            {
                dumpPath = CrashDumpWriter.WriteCurrentProcessDump(source, ex);
            }
            catch (Exception dumpEx)
            {
                try
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        "NONE",
                        "FATAL-DUMP",
                        "Crash dump write failed: " + dumpEx);
                }
                catch { }
            }

            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "NONE",
                    "FATAL-" + source,
                    "Unhandled exception. dump=" + (dumpPath ?? "(failed)") + "\r\n" + detail);
                QMC.Common.Logging.EventLogger.FlushPending(1000);
            }
            catch { /* 로깅 실패는 메시지박스 표시를 막지 않는다. */ }

            try
            {
                QMC.Common.MessageDialog.Show(
                    "처리되지 않은 오류가 발생했습니다 (" + source + ").\r\n" +
                    "프로그램을 종료합니다.\r\n\r\n" +
                    "Dump: " + (dumpPath ?? "생성 실패") + "\r\n\r\n" + detail,
                    "CDT-320 Fatal Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { /* 메시지박스 표시 실패는 무시. */ }

            try { QMC.Common.Win32Timer.RestoreResolution(); } catch { }
            try { QMC.Common.Logging.EventLogger.FlushPending(1000); } catch { }
            Environment.Exit(-1);
        }
    }
}

