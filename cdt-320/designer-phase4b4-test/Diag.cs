using System;
using System.Reflection;
using System.Windows.Forms;

class Diag
{
    [STAThread]
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        var t = asm.GetType("QMC.Vision.Ui.Pages.LightSystemSetupPage");
        var ctrl = Activator.CreateInstance(t);
        foreach (var fn in new[] { "_gridCtrl", "_gridLabel", "_gridSets" })
        {
            var fi = t.GetField(fn, BindingFlags.NonPublic | BindingFlags.Instance);
            var g = fi.GetValue(ctrl) as DataGridView;
            Console.WriteLine(fn + " cols=" + (g == null ? "NULL" : g.Columns.Count.ToString()) + " rows=" + (g == null ? "-" : g.Rows.Count.ToString()));
        }
    }
}
