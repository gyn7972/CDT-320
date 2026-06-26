using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320
{
    internal static class Program
    {
        // Single-instance mutex
        private const string MUTEX_NAME = @"Global\QMC.CDT-320.SingleInstance";
        private static Mutex _instanceMutex;

        /// <summary>auto-cycle 모드 — 명령행 `--auto-cycle N` 일 때 N개 사이클 자동 실행 후 종료.</summary>
        public static int AutoCycleCount { get; private set; } = 0;
        /// <summary>auto-cycle 시작 후 종료까지 추가 대기 (ms).</summary>
        public static int AutoCycleEndDelayMs { get; private set; } = 3000;
        /// <summary>true 면 auto-cycle 실행 후에도 핸들러 GUI 를 닫지 않고 사용자가 직접 닫을 때까지 유지.</summary>
        public static bool AutoCycleKeepOpen { get; private set; } = false;
        /// <summary>true 면 auto-cycle 모드와 동일한 Init 만 수행하고 사이클은 시작하지 않음 (GUI 유지).</summary>
        public static bool AutoInitOnly { get; private set; } = false;
        /// <summary>시작 시 자동 선택할 설정 페이지의 i18n 키 (예: "set.teach"). null 이면 기본 페이지.</summary>
        public static string StartPage { get; private set; } = null;
        /// <summary>Stage 60 — true 면 모든 탭의 모든 페이지를 한번씩 순회해 UiClickAuditor 결과를 로그로 남김.</summary>
        public static bool AuditAll { get; private set; } = false;
        /// <summary>Stage 60 R12 — true 면 audit-all 후에 모든 dead button 의 PerformClick 을 호출해 UI-CLICK-STUB 다수 발생을 EventLog 에 남긴다.</summary>
        public static bool ClickTestAll { get; private set; } = false;

        [STAThread]
        static void Main(string[] args)
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
            // Stage 24 — 명령행 인수 파싱
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--auto-cycle" && i + 1 < args.Length
                    && int.TryParse(args[i + 1], out var n)) AutoCycleCount = n;
                else if (args[i] == "--keep-open") AutoCycleKeepOpen = true;
                else if (args[i] == "--auto-init") AutoInitOnly = true;
                else if (args[i] == "--end-delay-ms" && i + 1 < args.Length
                    && int.TryParse(args[i + 1], out var d)) AutoCycleEndDelayMs = d;
                else if (args[i] == "--start-page" && i + 1 < args.Length) StartPage = args[i + 1];
                else if (args[i] == "--audit-all") AuditAll = true;
                else if (args[i] == "--click-test-all") { AuditAll = true; ClickTestAll = true; }
            }

            // 전역 예외 핸들러 — 시작/런타임 중 처리되지 않은 예외가 조용히 프로세스를
            // 종료시키는 것을 막고, 원인을 로그 + 메시지박스로 남긴다 (AGENTS.md 예외 규칙).
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => HandleFatalException(e.Exception, "UI-THREAD");
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleFatalException(e.ExceptionObject as Exception, "APP-DOMAIN");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            }
            finally
            {
                try { _instanceMutex?.ReleaseMutex(); } catch { }
                try { _instanceMutex?.Dispose(); } catch { }
            }
        }

        /// <summary>처리되지 않은 예외를 로그에 기록하고 사용자에게 원인을 표시한다.</summary>
        private static void HandleFatalException(Exception ex, string source)
        {
            string detail = ex?.ToString() ?? "Unknown exception (null)";
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "NONE",
                    "FATAL-" + source,
                    "Unhandled exception: " + detail);
            }
            catch { /* 로깅 실패는 메시지박스 표시를 막지 않는다. */ }

            try
            {
                QMC.Common.MessageDialog.Show(
                    "처리되지 않은 오류가 발생했습니다 (" + source + ").\r\n\r\n" + detail,
                    "CDT-320 Fatal Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { /* 메시지박스 표시 실패는 무시. */ }
        }
    }
}

