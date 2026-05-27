using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using QMC.CDT320.Logging;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Util
{
    /// <summary>
    /// Stage 60 — 페이지의 클릭 가능한 컨트롤 중 Click 핸들러가 부착되지 않은 것에
    /// 사용자 피드백용 placeholder 핸들러를 자동 부착한다.
    /// 사용자 보고: "클릭 안되는 버튼들 너무 많다".
    /// 리플렉션으로 EventHandlerList를 검사하여 실제 핸들러 부착 여부를 판별한다.
    /// </summary>
    public static class UiClickAuditor
    {
        private static readonly object _eventClickKey;
        private static readonly FieldInfo _eventsField;
        private static readonly bool _initOk;

        static UiClickAuditor()
        {
            try
            {
                _eventClickKey = typeof(Control)
                    .GetField("EventClick", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetValue(null);
                _eventsField = typeof(Component)
                    .GetField("events", BindingFlags.NonPublic | BindingFlags.Instance);
                _initOk = _eventClickKey != null && _eventsField != null;
            }
            catch { _initOk = false; }
        }

        /// <summary>해당 컨트롤에 Click 이벤트 핸들러가 부착되어 있는지 검사.</summary>
        public static bool HasClickHandler(Control c)
        {
            if (!_initOk || c == null) return true; // fail-safe — 핸들된 것으로 가정
            try
            {
                var list = _eventsField.GetValue(c) as EventHandlerList;
                return list != null && list[_eventClickKey] != null;
            }
            catch { return true; }
        }

        /// <summary>
        /// root 의 모든 자손 컨트롤(Button / ActionButton / SidebarButton) 중
        /// Click 핸들러가 없는 것에 placeholder 피드백 핸들러를 부착한다.
        /// 부착된 컨트롤 개수를 반환.
        /// </summary>
        public static int EnsureFeedback(Control root)
        {
            if (root == null) return 0;
            int wired = 0, total = 0;
            foreach (var c in EnumerateClickable(root))
            {
                total++;
                // 사용자 보고: "클릭해도 변화 없음" — dead button 검사 *전에* 모든 컨트롤에
                // 시각 피드백(노란 깜빡임)만 부착. 기존 실제 핸들러는 그대로 동작.
                bool hadHandler = HasClickHandler(c);
                var captured = c;
                c.Click += (s, e) => FlashOnly(captured);

                if (!hadHandler)
                {
                    // 진짜 dead button — placeholder 핸들러 추가 (EventLog 기록까지)
                    c.Click += (s, e) => StubFeedback(captured);
                    wired++;
                }
            }
            if (total > 0)
            {
                try
                {
                    EventLogger.Write(EventKind.Event, UserSession.Name,
                        "UI-AUDIT", root.GetType().Name + ": stubbed " + wired + " / " + total);
                }
                catch { }
            }
            return wired;
        }

        // 사용자 시각 피드백 전용 — EventLog 기록 X (실제 핸들러도 같이 동작)
        private static void FlashOnly(Control c)
        {
            try
            {
                Color orig = c.BackColor;
                c.BackColor = Color.FromArgb(0xFF, 0xF1, 0x9C);
                c.Invalidate();
                var t = new Timer { Interval = 220 };
                t.Tick += (ts, te) =>
                {
                    try { c.BackColor = orig; c.Invalidate(); } catch { }
                    t.Stop();
                    t.Dispose();
                };
                t.Start();
            }
            catch { }
        }

        private static IEnumerable<Control> EnumerateClickable(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button || c is ActionButton || c is SidebarButton)
                    yield return c;
                foreach (var sub in EnumerateClickable(c))
                    yield return sub;
            }
        }

        /// <summary>
        /// Stage 60 R12 — root 의 모든 Button/ActionButton/SidebarButton 에 PerformClick() 호출.
        /// audit 으로 부착된 placeholder 핸들러가 작동해 UI-CLICK-STUB 로그 다수 발생.
        /// 안 되는 (예외 throw) 컨트롤은 EventLog 에 UI-CLICK-FAIL 기록.
        /// 반환: (시도 수, 성공 수, 실패 수)
        /// </summary>
        public static (int tried, int success, int failed) PerformClickAll(Control root)
        {
            if (root == null) return (0, 0, 0);
            int tried = 0, success = 0, failed = 0;
            foreach (var c in EnumerateClickable(root))
            {
                tried++;
                try
                {
                    if (c is Button btn) btn.PerformClick();
                    else
                    {
                        // ActionButton / SidebarButton 은 Button 미상속 (Control 직접 상속)
                        // 직접 OnClick 트리거 — Control.OnClick(EventArgs) 가 protected 라 reflection
                        var mi = typeof(Control).GetMethod("OnClick",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mi != null)
                        {
                            mi.Invoke(c, new object[] { EventArgs.Empty });
                        }
                        else
                        {
                            failed++;
                            EventLogger.Write(EventKind.Event, UserSession.Name,
                                "UI-CLICK-FAIL", "OnClick reflection 실패: " + c.GetType().Name);
                            continue;
                        }
                    }
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    string label = c?.Text ?? c?.GetType().Name ?? "<null>";
                    try
                    {
                        EventLogger.Write(EventKind.Event, UserSession.Name,
                            "UI-CLICK-FAIL", "Click 예외: " + label + " — " + ex.GetType().Name + ": " + (ex.Message ?? "(no msg)"));
                    }
                    catch { }
                }
            }
            try
            {
                EventLogger.Write(EventKind.Event, UserSession.Name,
                    "UI-CLICK-TEST", root.GetType().Name +
                    ": tried=" + tried + " success=" + success + " failed=" + failed);
            }
            catch { }
            return (tried, success, failed);
        }

        // ──────────────────────────────────────────
        //  Placeholder 피드백
        // ──────────────────────────────────────────
        private static void StubFeedback(Control c)
        {
            string label = c?.Text;
            if (string.IsNullOrEmpty(label) && c != null) label = c.GetType().Name;

            try
            {
                EventLogger.Write(EventKind.Event, UserSession.Name,
                    "UI-CLICK-STUB", "Click(no handler): " + label);
            }
            catch { }

            // 짧은 시각 깜빡임 — 클릭이 실제로 받아졌음을 알림
            try
            {
                Color orig = c.BackColor;
                c.BackColor = Color.FromArgb(0xFF, 0xF1, 0x9C);
                c.Invalidate();
                var t = new Timer { Interval = 220 };
                t.Tick += (ts, te) =>
                {
                    try { c.BackColor = orig; c.Invalidate(); } catch { }
                    t.Stop();
                    t.Dispose();
                };
                t.Start();
            }
            catch { }
        }
    }
}
