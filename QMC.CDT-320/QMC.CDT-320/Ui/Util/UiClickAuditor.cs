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

        // ?ъ슜???쒓컖 ?쇰뱶諛??꾩슜 ??EventLog 湲곕줉 X (?ㅼ젣 ?몃뱾?щ룄 媛숈씠 ?숈옉)
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
        /// Stage 60 R12 ??root ??紐⑤뱺 Button/ActionButton/SidebarButton ??PerformClick() ?몄텧.
        /// audit ?쇰줈 遺李⑸맂 placeholder ?몃뱾?ш? ?묐룞??UI-CLICK-STUB 濡쒓렇 ?ㅼ닔 諛쒖깮.
        /// ???섎뒗 (?덉쇅 throw) 而⑦듃濡ㅼ? EventLog ??UI-CLICK-FAIL 湲곕줉.
        /// 諛섑솚: (?쒕룄 ?? ?깃났 ?? ?ㅽ뙣 ??
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
                        // ActionButton / SidebarButton ? Button 誘몄긽??(Control 吏곸젒 ?곸냽)
                        // 吏곸젒 OnClick ?몃━嫄???Control.OnClick(EventArgs) 媛 protected ??reflection
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
                                "UI-CLICK-FAIL", "OnClick reflection ?ㅽ뙣: " + c.GetType().Name);
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
                            "UI-CLICK-FAIL", "Click ?덉쇅: " + label + " ??" + ex.GetType().Name + ": " + (ex.Message ?? "(no msg)"));
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

            // 吏㏃? ?쒓컖 源쒕묀?????대┃???ㅼ젣濡?諛쏆븘議뚯쓬???뚮┝
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

