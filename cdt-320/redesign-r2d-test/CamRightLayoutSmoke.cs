using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.Vision.Ui.Pages;

// 우측 컬럼 반응형 — 패널을 넓게/좁게 리사이즈해 버튼 플로우 줄수와 미리보기 채움을 검증 + 스냅샷 PNG.
class CamRightLayoutSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static T Field<T>(object o, string f)
        => (T)o.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);

    static int FlowRows(FlowLayoutPanel flow)
        => flow.Controls.Cast<Control>().Select(c => c.Top).Distinct().Count();

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        var panel = new CameraMappingPanel();
        var form = new Form { Width = 1400, Height = 800 };
        panel.Dock = DockStyle.Fill;
        form.Controls.Add(panel);
        form.Show(); form.Left = -3000;   // 오프스크린
        Application.DoEvents();

        var right = Field<Panel>(panel, "_rightPanel");
        var flow  = Field<FlowLayoutPanel>(panel, "_flowButtons");
        var pic   = Field<PictureBox>(panel, "_picPreview");

        // ① 넓은 창 (1400) — 우측 컬럼 폭 확대 → 버튼 줄 수 감소 + 미리보기가 플로우 아래 잔여 전부
        Application.DoEvents();
        int rowsWide = FlowRows(flow);
        int picHWide = pic.Height, rightHWide = right.Height;
        Ok($"넓은창 — 우측폭 {right.Width} > 430(앵커 확장)", right.Width > 430, $"(got {right.Width})");
        Ok($"넓은창 — 버튼 {rowsWide}줄(2줄 이하)", rowsWide <= 2, $"(got {rowsWide})");
        Ok("넓은창 — 미리보기=잔여 채움(플로우+상태 아래)", picHWide == rightHWide - flow.Height - 24,
            $"(pic {picHWide} vs {rightHWide - flow.Height - 24})");

        // ② 좁은 창 (1000) — 버튼 줄 수 증가(접힘) + 미리보기 높이 그만큼 감소
        form.Width = 1000;
        Application.DoEvents();
        int rowsNarrow = FlowRows(flow);
        int picHNarrow = pic.Height;
        Ok($"좁은창 — 버튼 줄 증가({rowsWide}→{rowsNarrow})", rowsNarrow > rowsWide, $"(got {rowsNarrow})");
        Ok("좁은창 — 미리보기=잔여 채움 유지", picHNarrow == right.Height - flow.Height - 24,
            $"(pic {picHNarrow} vs {right.Height - flow.Height - 24})");
        Ok("좁은창 — 미리보기 높이 감소", picHNarrow < picHWide, $"({picHWide}→{picHNarrow})");

        // 스냅샷 PNG (육안 확인용)
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        form.Width = 1400; Application.DoEvents();
        using (var bmp = new Bitmap(panel.Width, panel.Height))
        { panel.DrawToBitmap(bmp, new Rectangle(0, 0, panel.Width, panel.Height)); bmp.Save(Path.Combine(dir, "cam_right_wide.png"), ImageFormat.Png); }
        form.Width = 1000; Application.DoEvents();
        using (var bmp = new Bitmap(panel.Width, panel.Height))
        { panel.DrawToBitmap(bmp, new Rectangle(0, 0, panel.Width, panel.Height)); bmp.Save(Path.Combine(dir, "cam_right_narrow.png"), ImageFormat.Png); }
        Console.WriteLine("  snapshots: cam_right_wide.png / cam_right_narrow.png");

        try { form.Close(); form.Dispose(); } catch { }
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
