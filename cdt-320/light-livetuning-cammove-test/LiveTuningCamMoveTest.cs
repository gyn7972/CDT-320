using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 87 검증 — 라이브 튜닝 패널 카메라 라이브 통합 + Finder/Inspector 이동 + InspectionLightPanel 호스팅 제거.
class LiveTuningCamMoveTest
{
    static int _fail = 0;
    static void Check(string name, bool ok, string detail)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")"));
        if (!ok) _fail++;
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\light-livetuning-cammove-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        var tPanel = asm.GetType("QMC.Vision.Ui.Controls.LightLiveTuningPanel");

        // ── (A) 렌더 (540×264, 신규 _lblPeriodHz/_lblCamInfo 포함) ──
        var panel = (Control)Activator.CreateInstance(tPanel);
        panel.Size = new Size(540, 264);
        using (var host = new Form { Width = 580, Height = 320, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) })
        {
            panel.Location = new Point(6, 6);
            host.Controls.Add(panel);
            host.Show(); Application.DoEvents();
            using (var bmp = new Bitmap(panel.Width, panel.Height))
            {
                panel.DrawToBitmap(bmp, new Rectangle(0, 0, panel.Width, panel.Height));
                bmp.Save(dir + @"\panel_cammove.png", ImageFormat.Png);
            }
            // 신규 라벨 존재 + 초기 Hz
            var hz = (Label)tPanel.GetField("_lblPeriodHz", bf).GetValue(panel);
            var cam = (Label)tPanel.GetField("_lblCamInfo", bf).GetValue(panel);
            Check("_lblPeriodHz 초기 표시(≈ 20.0 Hz)", hz != null && hz.Text.Contains("20.0"), hz?.Text);
            Check("_lblCamInfo 안내 표시", cam != null && cam.Text.Contains("Bottom"), cam?.Text);
            host.Hide();
        }
        Console.WriteLine("  [OK] panel_cammove.png 저장");

        // ── (B) 기능: 카메라 콜백 + fps + null 안전 ──
        var panel2 = (Control)Activator.CreateInstance(tPanel);
        int started = 0, stopped = 0;
        Action onStart = () => started++;
        Action onStop  = () => stopped++;
        tPanel.GetMethod("BindCameraLive").Invoke(panel2, new object[] { onStart, onStop });

        using (var host = new Form { Width = 580, Height = 320, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) })
        {
            host.Controls.Add(panel2); host.Show(); Application.DoEvents();
            var fLiveOn = tPanel.GetField("_liveOn", bf);
            var fTimer  = tPanel.GetField("_timer", bf);
            var onToggle = tPanel.GetMethod("OnToggleLiveClick", bf);
            var onPeriod = tPanel.GetMethod("OnPeriodChanged", bf);
            var fNum = tPanel.GetField("_numPeriod", bf);
            var num = (NumericUpDown)fNum.GetValue(panel2);
            var fHz = tPanel.GetField("_lblPeriodHz", bf);

            // 시작
            onToggle.Invoke(panel2, new object[] { null, EventArgs.Empty }); Application.DoEvents();
            bool liveOn = (bool)fLiveOn.GetValue(panel2);
            var timer = (Timer)fTimer.GetValue(panel2);
            Check("StartLive → 카메라 start 콜백 호출", started == 1, "started=" + started);
            Check("StartLive → _liveOn + timer", liveOn && timer.Enabled, $"liveOn={liveOn}, timer={timer.Enabled}");

            // 정지
            onToggle.Invoke(panel2, new object[] { null, EventArgs.Empty }); Application.DoEvents();
            Check("StopLive → 카메라 stop 콜백 호출", stopped == 1, "stopped=" + stopped);
            Check("StopLive → _liveOn=false", !(bool)fLiveOn.GetValue(panel2), "");

            // fps 환산: 100ms → ≈ 10.0 Hz
            num.Value = 100; onPeriod.Invoke(panel2, new object[] { null, EventArgs.Empty }); Application.DoEvents();
            var hz = (Label)fHz.GetValue(panel2);
            Check("fps 환산 100ms→10.0 Hz", hz.Text.Contains("10.0"), hz.Text);

            host.Hide();
        }

        // null 콜백 안전
        var panel3 = (Control)Activator.CreateInstance(tPanel);
        tPanel.GetMethod("BindCameraLive").Invoke(panel3, new object[] { null, null });
        using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) })
        {
            host.Controls.Add(panel3); host.Show(); Application.DoEvents();
            var onToggle = tPanel.GetMethod("OnToggleLiveClick", bf);
            bool threw = false;
            try { onToggle.Invoke(panel3, new object[] { null, EventArgs.Empty }); onToggle.Invoke(panel3, new object[] { null, EventArgs.Empty }); }
            catch (Exception ex) { threw = true; Console.WriteLine("    null-cb ex: " + (ex.InnerException ?? ex).Message); }
            Check("null 카메라 콜백 안전(예외 없음)", !threw, "");
            host.Hide();
        }

        // ── (C) Finder/Inspector 메서드 존재 + InspectionLightPanel 호스팅 제거 ──
        foreach (var tn in new[] { "QMC.Vision.Ui.Pages.FinderPage", "QMC.Vision.Ui.Pages.InspectorPage" })
        {
            var t = asm.GetType(tn);
            bool hasStart = t.GetMethod("StartLive") != null;
            bool hasStop  = t.GetMethod("StopLive") != null;
            bool hasCollect = t.GetMethod("CollectRowsForLiveTuning", bf) != null;
            bool hasDispose = t.GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool) }, null)?.DeclaringType == t;
            string shortName = tn.Substring(tn.LastIndexOf('.') + 1);
            Check(shortName + " StartLive/StopLive/CollectRows/Dispose", hasStart && hasStop && hasCollect && hasDispose,
                  $"start={hasStart},stop={hasStop},collect={hasCollect},dispose={hasDispose}");
        }
        var tIlp = asm.GetType("QMC.Vision.Ui.Pages.InspectionLightPanel");
        bool removed = tIlp.GetField("_liveTuning", bf) == null && tIlp.GetMethod("CollectRowsForLiveTuning", bf) == null;
        Check("InspectionLightPanel 호스팅 제거(_liveTuning/CollectRows 없음)", removed, "");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }
}
