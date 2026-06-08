using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 91 Phase2 검증 — Editors 인스턴스화 + 렌더 무예외(동작 무변경 스모크) + Load/Save 경로 점검.
// LoadJson 은 누락 시 기본값(예외 없음) → MessageBox 없이 안전.
class EditorsRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase2-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        // Host (콤보 + 첫 편집기 BottomInspection 표시)
        Render(asm, "QMC.Vision.Ui.Editors.ParameterEditorHost", new Size(900, 600), dir + @"\host.png");

        // 자식 6 (각각 base 통해 shell + 동적 BuildEditor + 기본값 Load)
        foreach (var n in new[] {
            "BottomInspectionParameterEditor", "SideInspectionParameterEditor",
            "DieGapInspectionParameterEditor", "DistortionParameterEditor",
            "VisionScaleParameterEditor" })
        {
            Render(asm, "QMC.Vision.Ui.Editors." + n, new Size(900, 700), dir + @"\" + n + ".png");
        }

        // Load/Save 경로 1회 점검: BottomInspection 편집기의 LoadFromParameters/SaveToParameters 시그니처 존재
        var t = asm.GetType("QMC.Vision.Ui.Editors.BottomInspectionParameterEditor");
        var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        bool hasLoad = t.GetMethod("LoadFromParameters", bf) != null;
        bool hasSave = t.GetMethod("SaveToParameters", bf) != null;
        Check("Load/Save 시그니처 보존", hasLoad && hasSave, $"load={hasLoad}, save={hasSave}");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void Render(Assembly asm, string typeName, Size size, string png)
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
            }
            Console.WriteLine("  [PASS] " + typeName.Substring(typeName.LastIndexOf('.') + 1) + " 인스턴스화+렌더 OK");
        }
        catch (Exception ex)
        {
            _fail++;
            Console.WriteLine("  [FAIL] " + typeName + " : " + (ex.InnerException ?? ex).Message);
        }
    }

    static void Check(string name, bool ok, string detail)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")"));
        if (!ok) _fail++;
    }
}
