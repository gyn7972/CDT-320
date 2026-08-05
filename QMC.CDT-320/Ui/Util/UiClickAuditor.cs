using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Util
{
    /// <summary>
    /// Stage 60 ???섏씠吏???대┃ 媛?ν븳 而⑦듃濡?以?Click ?몃뱾?ш? 遺李⑸릺吏 ?딆? 寃껋뿉
    /// ?ъ슜???쇰뱶諛깆슜 placeholder ?몃뱾?щ? ?먮룞 遺李⑺븳??
    /// ?ъ슜??蹂닿퀬: "?대┃ ?덈릺??踰꾪듉???덈Т 留롫떎".
    /// 由ы뵆?됱뀡?쇰줈 EventHandlerList瑜?寃?ы븯???ㅼ젣 ?몃뱾??遺李??щ?瑜??먮퀎?쒕떎.
    /// </summary>
    public static class UiClickAuditor
    {
        private static readonly object _eventClickKey;
        private static readonly FieldInfo _eventsField;
        private static readonly bool _initOk;

        // 지금 깜빡임(플래시) 진행 중인 컨트롤 목록. 깜빡이는 220ms 안에 또 클릭되면
        // 연노랑을 '원래 색'으로 잘못 기억해 버튼이 노란색으로 눌러붙는 문제를 막는다.
        // (클릭 핸들러/타이머 모두 UI 스레드에서만 실행되므로 별도 잠금은 필요 없다)
        private static readonly HashSet<Control> _flashing = new HashSet<Control>();

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

        /// <summary>?대떦 而⑦듃濡ㅼ뿉 Click ?대깽???몃뱾?ш? 遺李⑸릺???덈뒗吏 寃??</summary>
        public static bool HasClickHandler(Control c)
        {
            if (!_initOk || c == null) return true;
            try
            {
                var list = _eventsField.GetValue(c) as EventHandlerList;
                return list != null && list[_eventClickKey] != null;
            }
            catch { return true; }
        }

        /// <summary>
        /// root ??紐⑤뱺 ?먯넀 而⑦듃濡?Button / ActionButton / SidebarButton) 以?        /// Click ?몃뱾?ш? ?녿뒗 寃껋뿉 placeholder ?쇰뱶諛??몃뱾?щ? 遺李⑺븳??
        /// 遺李⑸맂 而⑦듃濡?媛쒖닔瑜?諛섑솚.
        /// </summary>
        public static int EnsureFeedback(Control root)
        {
            if (root == null) return 0;
            int wired = 0, total = 0;
            foreach (var c in EnumerateClickable(root))
            {
                total++;
                // ?ъ슜??蹂닿퀬: "?대┃?대룄 蹂???놁쓬" ??dead button 寃??*?꾩뿉* 紐⑤뱺 而⑦듃濡ㅼ뿉
                // ?쒓컖 ?쇰뱶諛??몃? 源쒕묀??留?遺李? 湲곗〈 ?ㅼ젣 ?몃뱾?щ뒗 洹몃?濡??숈옉.
                bool hadHandler = HasClickHandler(c);
                var captured = c;
                c.Click += (s, e) => FlashOnly(captured);

                if (!hadHandler)
                {
                    // 吏꾩쭨 dead button ??placeholder ?몃뱾??異붽? (EventLog 湲곕줉源뚯?)
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

        // 사용자 시각 피드백 전용 — EventLog 기록 X (실제 핸들러도 같이 동작).
        // 배경을 연노랑으로 220ms 깜빡여 "클릭이 받아졌음"을 보여준다.
        // 흰 글씨 버튼에서도 읽히도록 깜빡이는 동안 글자색도 어두운 색으로 함께 바꾼다.
        private static void FlashOnly(Control c)
        {
            try
            {
                if (c == null || _flashing.Contains(c))
                    return;   // 이미 깜빡이는 중 — 원래 색을 다시 기억하면 연노랑이 고착되므로 무시

                Color origBack = c.BackColor;
                Color origFore = c.ForeColor;
                _flashing.Add(c);

                c.BackColor = Color.FromArgb(0xFF, 0xF1, 0x9C);
                c.ForeColor = Color.FromArgb(0x33, 0x33, 0x33);
                c.Invalidate();

                var t = new Timer { Interval = 220 };
                t.Tick += (ts, te) =>
                {
                    try { c.BackColor = origBack; c.ForeColor = origFore; c.Invalidate(); } catch { }
                    _flashing.Remove(c);
                    t.Stop();
                    t.Dispose();
                };
                t.Start();
            }
            catch
            {
                _flashing.Remove(c);   // 도중 실패 시에도 다음 클릭의 깜빡임이 막히지 않게 정리
            }
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

        // ??????????????????????????????????????????
        //  Placeholder ?쇰뱶諛?        // ??????????????????????????????????????????
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

            // FlashOnly 와 같은 클릭에서 겹쳐 호출되면(모든 버튼에 FlashOnly 가 먼저 연결됨)
            // 가드 덕에 두 번째 깜빡임은 무시되어 연노랑이 '원래 색'으로 고착되지 않는다.
            FlashOnly(c);
        }
    }
}

