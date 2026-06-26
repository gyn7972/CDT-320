using System;
using System.Windows.Forms;

namespace ColletFinder
{
    internal static class Program
    {
        /// <summary>애플리케이션 진입점.</summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
