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
        var row1  = Field<FlowLayoutPanel>(panel, "_flowRow1");
        var row2  = Field<FlowLayoutPanel>(panel, "_flowRow2");
        var pic   = Field<PictureBox>(panel, "_picPreview");

        // ① 고정 2행 그룹 — 1행 = 저장/취소/복원/적용, 2행 = Connect/그랩/LiveStart/LiveStop
        Application.DoEvents();
        Ok("우측폭 > 430 (앵커 확장)", right.Width > 430, $"(got {right.Width})");
        Ok("1행 = 4버튼(저장 그룹), 줄바꿈 없음", row1.Controls.Count == 4 && !row1.WrapContents && FlowRows(row1) == 1);
        Ok("2행 = 4버튼(카메라 그룹), 줄바꿈 없음", row2.Controls.Count == 4 && !row2.WrapContents && FlowRows(row2) == 1);
        Ok("1행 버튼 순서 = 저장·취소·기본값 복원·실행 모듈에 적용",
            row1.Controls[0].Text == "저장" && row1.Controls[1].Text == "취소"
            && row1.Controls[2].Text == "기본값 복원" && row1.Controls[3].Text == "실행 모듈에 적용");
        Ok("2행 버튼 순서 = Connect·테스트 그랩·Live Start·Live Stop",
            row2.Controls[0].Text == "Connect" && row2.Controls[1].Text == "테스트 그랩"
            && row2.Controls[2].Text == "Live Start" && row2.Controls[3].Text == "Live Stop");
        Ok("2행이 1행 아래", row2.Top >= row1.Bottom);

        // ② 미리보기 = min(잔여, 상한 640×480)
        int remainWide = right.Height - row1.Height - row2.Height - 24;
        int picHWide = pic.Height;
        Ok("넓은창 — 미리보기 높이 = min(잔여, 480) (상한 동작)", picHWide == Math.Min(remainWide, 480),
            $"(pic {picHWide} vs min({remainWide},480))");
        Ok("넓은창 — 미리보기 폭 = min(우측폭, 640)", pic.Width == Math.Min(right.Width, 640),
            $"(pic {pic.Width} vs min({right.Width},640))");

        // ③ 작은 창 — 고정 2행 유지 + 미리보기 반응형 축소(높이 줄여 잔여<480 유도)
        form.Width = 1000; form.Height = 580;
        Application.DoEvents();
        int remainSmall = right.Height - row1.Height - row2.Height - 24;
        int picHSmall = pic.Height;
        Ok("작은창 — 여전히 고정 2행", FlowRows(row1) == 1 && FlowRows(row2) == 1);
        Ok("작은창 — 미리보기 = min(잔여, 480) 반응형 축소", picHSmall == Math.Min(remainSmall, 480) && picHSmall < picHWide,
            $"(pic {picHSmall} vs min({remainSmall},480), wide {picHWide})");

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
