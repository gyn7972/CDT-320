using System;
using System.Threading;
using System.Windows.Forms;
using QMC.Vision.Tools;

namespace QMC.Vision
{
    internal static class Program
    {
        private const string MUTEX_NAME = @"Global\QMC.Vision.SingleInstance";
        private static Mutex _instanceMutex;

        [STAThread]
        static int Main(string[] args)
        {
            // ── 헤드리스 CLI 분기 (Single Instance Mutex 우회) ──
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--ui-audit", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            return UiOverlapAuditor.Run();
                        }
                        catch (Exception ex) { Console.Error.WriteLine("FATAL: " + ex); return 99; }
                    }
                }
            }

            // ── GUI 모드 (기존 동작) ──
            bool created;
            _instanceMutex = new Mutex(true, MUTEX_NAME, out created);
            if (!created)
            {
                MessageBox.Show("CDT-320 Vision 이 이미 실행 중입니다.",
                                "Single Instance",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
                return 0;
            }
            finally
            {
                try { _instanceMutex?.ReleaseMutex(); } catch { }
                try { _instanceMutex?.Dispose(); } catch { }
            }
        }
    }
}
