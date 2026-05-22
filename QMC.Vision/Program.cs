using System;
using System.Threading;
using System.Windows.Forms;

namespace QMC.Vision
{
    internal static class Program
    {
        private const string MUTEX_NAME = @"Global\QMC.Vision.SingleInstance";
        private static Mutex _instanceMutex;

        [STAThread]
        static void Main()
        {
            bool created;
            _instanceMutex = new Mutex(true, MUTEX_NAME, out created);
            if (!created)
            {
                MessageBox.Show("CDT-320 Vision 이 이미 실행 중입니다.",
                                "Single Instance",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
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
    }
}
