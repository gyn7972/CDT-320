using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// CameraMappingPanel 검증: 구성/SelectAlgorithm 무예외 + AutoScroll + 콤보 스타일 + 프리뷰 도달성.
class UiProbe
{
    static Control FindByType(Control root, string typeName)
    {
        foreach (Control c in root.Controls)
        { if (c.GetType().Name == typeName) return c; var r = FindByType(c, typeName); if (r != null) return r; }
        return null;
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        var asm = Assembly.LoadFrom("QMC.Vision.exe");
        var t = asm.GetType("QMC.Vision.Ui.Pages.CameraMappingPanel");
        var panel = (Control)Activator.CreateInstance(t);

        using (var f = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false,
                                  StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) })
        {
            f.ClientSize = new Size(1100, 600);   // 일부러 작은 높이로 클리핑 상황 재현
            panel.Dock = DockStyle.Fill;
            f.Controls.Add(panel);
            var _ = f.Handle; f.Show();
            for (int i = 0; i < 4; i++) { Application.DoEvents(); f.PerformLayout(); panel.PerformLayout(); }

            try { t.GetMethod("SelectAlgorithm").Invoke(panel, new object[] { "BottomInspection" }); }
            catch (Exception ex) { Console.WriteLine("SelectAlgorithm THROW | " + (ex.InnerException ?? ex).Message); }
            for (int i = 0; i < 4; i++) { Application.DoEvents(); panel.PerformLayout(); }

            var combo = FindByType(panel, "ComboBox") as ComboBox;       // 첫 콤보 = 카메라 ID
            var pic   = FindByType(panel, "PictureBox") as PictureBox;   // 프리뷰
            // _body 패널: AutoScroll 인 Panel 찾기
            Panel body = null;
            foreach (Control c in panel.Controls) if (c is Panel p && p.AutoScroll) { body = p; break; }

            Console.WriteLine("combo.DropDownStyle=" + (combo != null ? combo.DropDownStyle.ToString() : "null"));
            Console.WriteLine("body.AutoScroll=" + (body != null ? body.AutoScroll.ToString() : "null")
                + " AutoScrollMinSize.H=" + (body != null ? body.AutoScrollMinSize.Height : -1)
                + " client.H=" + (body != null ? body.ClientSize.Height : -1));
            Console.WriteLine("preview found=" + (pic != null) + (pic != null ? (" bottom(in body)=" + (pic.Top + pic.Height)) : ""));
            // MIL System 제거 확인: TextBox 개수(이전엔 DCF+System 2개, 이제 DCF 1개만, 게다가 숨김)
            int tb = 0; CountType(panel, "TextBox", ref tb);
            Console.WriteLine("TextBox count=" + tb + " (MIL System 제거되어 DCF 1개만)");
            f.Close();
        }
    }
    static void CountType(Control root, string typeName, ref int n)
    { foreach (Control c in root.Controls) { if (c.GetType().Name == typeName) n++; CountType(c, typeName, ref n); } }
}
