using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Forms.Semi;

using QMC.Hmi.Forms;

namespace QMC.Hmi
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int ret = 0;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ProgramX program = null;

            program = new ProgramX();

            if((ret = program.Prepare("QMC", out var formMain)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if(error != null)
                    MsgBox.ShowError(error.Message);
                else
                    MsgBox.ShowError("Unknown error occurred in preparing application.");
                return;
            }

            Application.Run(formMain);

            program.Terminate("QMC");
        }
    }
}
