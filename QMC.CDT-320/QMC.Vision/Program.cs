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
            // ── 헤드리스 CLI 분기 (Single Instance Mutex 우회 — GUI 와 동시 실행 가능) ──
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--cam-test", StringComparison.OrdinalIgnoreCase))
                    {
                        // --save <dir> 옵션 추출
                        string saveDir = null;
                        for (int j = 0; j < args.Length - 1; j++)
                        {
                            if (string.Equals(args[j], "--save", StringComparison.OrdinalIgnoreCase))
                            {
                                saveDir = args[j + 1];
                                break;
                            }
                        }
                        try
                        {
                            return CameraConnectTest.Run(saveDir);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("FATAL: " + ex);
                            return 99;
                        }
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
