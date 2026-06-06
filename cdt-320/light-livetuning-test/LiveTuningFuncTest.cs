using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

// LightLiveTuningPanel 라이브 동작 로직 검증 (GUI 없이 핸들러/비즈니스 직접 호출).
//  토글 시작/중지, 단발 송신 카운터, 전체 OFF — LightHub 의 Sim 컨트롤러 상대로.
class LiveTuningFuncTest
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
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        // LightHub 에 Sim 컨트롤러 COM1 등록
        var tHub = asm.GetType("QMC.Vision.Comm.LightHub");
        var tSetup = asm.GetType("QMC.Common.Recipes.LightSystemSetup", false)
                     ?? Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Common.dll").GetType("QMC.Common.Recipes.LightSystemSetup");
        var asmCommon = tSetup.Assembly;
        var tEntry = asmCommon.GetType("QMC.Common.Recipes.LightControllerEntry");
        var setup = Activator.CreateInstance(tSetup);
        var entry = Activator.CreateInstance(tEntry);
        tEntry.GetProperty("PortName").SetValue(entry, "COM1");
        tEntry.GetProperty("ChannelCount").SetValue(entry, 8);
        var controllers = tSetup.GetProperty("Controllers").GetValue(setup);
        ((IList)controllers).Add(entry);
        tHub.GetMethod("Initialize").Invoke(null, new object[] { setup, true });   // useSim=true

        // 패널 생성 + provider
        var tPanel = asm.GetType("QMC.Vision.Ui.Controls.LightLiveTuningPanel");
        var panel = (Control)Activator.CreateInstance(tPanel);
        var tRow = tPanel.GetNestedType("TuningRow");
        // Initialize(Func<IEnumerable<TuningRow>>) — 정확한 delegate 타입으로 provider 생성
        var providerDel = BuildProvider(tRow);
        tPanel.GetMethod("Initialize").Invoke(panel, new object[] { providerDel });

        var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        using (var host = new Form { Width = 300, Height = 400, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) })
        {
            host.Controls.Add(panel);
            host.Show();
            Application.DoEvents();

            var fLiveOn = tPanel.GetField("_liveOn", bf);
            var fCount  = tPanel.GetField("_sendCount", bf);
            var fTimer  = tPanel.GetField("_timer", bf);
            var fBtn    = tPanel.GetField("_btnToggleLive", bf);
            var onToggle = tPanel.GetMethod("OnToggleLiveClick", bf);
            var sendOnce = tPanel.GetMethod("SendOnceAsync", bf);

            // 1) 라이브 시작 토글
            onToggle.Invoke(panel, new object[] { null, EventArgs.Empty });
            Application.DoEvents();
            bool liveOn = (bool)fLiveOn.GetValue(panel);
            var timer = (Timer)fTimer.GetValue(panel);
            var btn = (Button)fBtn.GetValue(panel);
            Check("토글 시작 → _liveOn=true", liveOn, liveOn.ToString());
            Check("토글 시작 → timer.Enabled", timer.Enabled, timer.Enabled.ToString());
            Check("토글 시작 → 버튼 '중지'", btn.Text.Contains("중지"), btn.Text);

            // 2) 단발 송신 (Sim → OK), 카운터 증가
            int before = (int)fCount.GetValue(panel);
            Await((Task)sendOnce.Invoke(panel, new object[] { false }));
            Application.DoEvents();
            int after = (int)fCount.GetValue(panel);
            Check("SendOnce → 카운터 증가", after == before + 1, $"{before}->{after}");

            // 3) 전체 OFF 송신 (allZero=true)
            int b2 = (int)fCount.GetValue(panel);
            Await((Task)sendOnce.Invoke(panel, new object[] { true }));
            int a2 = (int)fCount.GetValue(panel);
            Check("전체 OFF → 카운터 증가 + 예외 없음", a2 == b2 + 1, $"{b2}->{a2}");

            // 4) 라이브 중지 토글
            onToggle.Invoke(panel, new object[] { null, EventArgs.Empty });
            Application.DoEvents();
            bool liveOff = !(bool)fLiveOn.GetValue(panel);
            Check("토글 중지 → _liveOn=false + timer 정지", liveOff && !timer.Enabled, $"liveOn={!liveOff}, timer={timer.Enabled}");

            host.Hide();
        }

        try { tHub.GetMethod("DisposeAll").Invoke(null, null); } catch { }
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void Await(Task t)
    {
        int spins = 0;
        while (!t.IsCompleted && spins++ < 500) { Application.DoEvents(); System.Threading.Thread.Sleep(5); }
        if (t.IsFaulted) throw t.Exception;
    }

    // provider delegate 를 패널이 받는 정확한 타입(Func<IEnumerable<TuningRow>>)으로 생성
    static Delegate BuildProvider(Type tRow)
    {
        var ienum = typeof(IEnumerable<>).MakeGenericType(tRow);
        var funcType = typeof(Func<>).MakeGenericType(ienum);
        var mi = typeof(LiveTuningFuncTest).GetMethod(nameof(MakeRows), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(tRow);
        return Delegate.CreateDelegate(funcType, mi);
    }

    static IEnumerable<T> MakeRows<T>()
    {
        var t = typeof(T);
        var row = Activator.CreateInstance(t);
        t.GetField("ControllerPort").SetValue(row, "COM1");
        t.GetField("Channel").SetValue(row, 1);
        t.GetField("Level").SetValue(row, 200);
        return new List<T> { (T)row };
    }
}
