using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 97 Phase4b-4 검증 — LightSystemSetupPage 파라미터리스 인스턴스화 + 렌더 무예외 + 정적 shell 컨트롤 존재 확인.
class LightSetupRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase4b4-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        var ctrl = Render(asm, "QMC.Vision.Ui.Pages.LightSystemSetupPage", new Size(1280, 860), dir + @"\LightSystemSetupPage.png");
        if (ctrl != null)
        {
            // 정적 shell 핵심 컨트롤(필드) 존재 + 그리드 컬럼 수 확인
            CheckField(ctrl, "_gridCtrl", 8);    // 6 text + Vendor + Mode
            CheckField(ctrl, "_gridLabel", 3);   // Channel/Name/Color
            CheckField(ctrl, "_gridSets", 2);    // ControllerPort + ChannelsCsv
            CheckExists(ctrl, "_treeWiring");
            CheckExists(ctrl, "_split");
            CheckExists(ctrl, "_bar");
        }
        CheckCtor(asm, "QMC.Vision.Ui.Pages.LightSystemSetupPage");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void CheckCtor(Assembly asm, string typeName)
    {
        var t = asm.GetType(typeName);
        bool c0 = t.GetConstructor(Type.EmptyTypes) != null;
        Console.WriteLine((c0 ? "  [PASS] " : "  [FAIL] ") + "파라미터리스 ctor 보존 = " + c0);
        if (!c0) _fail++;
    }

    static FieldInfo F(object o, string name)
        => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

    static void CheckExists(object ctrl, string field)
    {
        var fi = F(ctrl, field);
        bool ok = fi != null && fi.GetValue(ctrl) != null;
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + field + " 생성됨 = " + ok);
        if (!ok) _fail++;
    }

    static void CheckField(object ctrl, string field, int expectedCols)
    {
        var fi = F(ctrl, field);
        var grid = fi?.GetValue(ctrl) as DataGridView;
        bool ok = grid != null && grid.Columns.Count == expectedCols;
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + field + " 컬럼수 = " + (grid?.Columns.Count.ToString() ?? "null") + " (기대 " + expectedCols + ")");
        if (!ok) _fail++;
    }

    static Control Render(Assembly asm, string typeName, Size size, string png)
    {
        try
        {
            var t = asm.GetType(typeName);
            var ctrl = (Control)Activator.CreateInstance(t);
            ctrl.Size = size;
            using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Width = size.Width + 40, Height = size.Height + 60 })
            {
                ctrl.Dock = DockStyle.Fill;
                host.Controls.Add(ctrl);
                host.Show();
                Application.DoEvents();
                using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                {
                    ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height));
                    bmp.Save(png, ImageFormat.Png);
                }
                host.Hide();
                host.Controls.Remove(ctrl);   // host Dispose 가 자식 ctrl 을 Dispose 하지 않도록 분리
            }
            Console.WriteLine("  [PASS] " + typeName.Substring(typeName.LastIndexOf('.') + 1) + " 인스턴스화+렌더 OK");
            return ctrl;
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] " + typeName + " : " + (ex.InnerException ?? ex).Message); return null; }
    }
}
