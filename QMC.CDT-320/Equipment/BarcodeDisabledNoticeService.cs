using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT320
{
    /// <summary>
    /// 바코드 판독이 꺼진 상태로 Auto가 진행될 때 띄우는 모달리스 안내창.
    /// 알람으로 세우지 않는다(테스트 중에는 바코드 없이 진행해야 함). 항상 위 + 최소화 가능.
    /// 이미 떠 있으면 아무 것도 하지 않는다 — 사이클마다 창이 쌓이거나, 숨겨둔 창이 다시 튀어나오지 않게 한다.
    /// (같은 이유로 매번 Activate하는 공용 ModelessDialogHost를 쓰지 않는다.)
    /// 상태는 UI 스레드에서만 접근하므로 별도 lock이 필요 없다.
    /// </summary>
    internal static class BarcodeDisabledNoticeService
    {
        // [P2 2026-08-22] 인풋/아웃풋 안내를 채널별 창으로 분리한다 — 단일 창이던 시절에는
        // 인풋 경로의 Close()가 아웃풋 안내까지 닫아버려 공존이 불가능했다.
        public const string InputChannel = "INPUT";
        public const string OutputChannel = "OUTPUT";

        private static readonly System.Collections.Generic.Dictionary<string, Form> _notices =
            new System.Collections.Generic.Dictionary<string, Form>(StringComparer.OrdinalIgnoreCase);
        // [검토수정 2026-08-22] Show/Close 진입은 시퀀스 스레드, 사전 변이는 UI 스레드(BeginInvoke)라
        // Dictionary 접근을 전부 락으로 감싼다 — 무락 동시 접근은 열거 예외/무한 루프가 가능하고,
        // 예외가 삼켜지면 안내창이 조용히 사라진다. (구 단일 참조 필드 시절에는 원자적이라 안전했다)
        private static readonly object NoticeSync = new object();

        public static void Show(string channel, string message)
        {
            string key = string.IsNullOrWhiteSpace(channel) ? InputChannel : channel;
            Invoke(() =>
            {
                Form existing;
                lock (NoticeSync)
                {
                    if (_notices.TryGetValue(key, out existing) && existing != null && !existing.IsDisposed)
                        return;
                }

                Form owner = ResolveOwner();
                if (owner == null)
                    return;

                Form notice = BuildNotice(key, message);
                notice.FormClosed += (s, e) => { lock (NoticeSync) _notices.Remove(key); };
                lock (NoticeSync)
                    _notices[key] = notice;
                notice.Show(owner);

                EventLogger.Write(EventKind.Warning, "SYSTEM", "BARCODE-DISABLED",
                    "바코드 판독 비활성 안내(" + key + "): " + (message ?? string.Empty).Replace("\r\n", " "));
            });
        }

        /// <summary>해당 채널의 바코드가 다시 켜졌을 때 그 채널 안내창만 닫는다.</summary>
        public static void Close(string channel)
        {
            string key = string.IsNullOrWhiteSpace(channel) ? InputChannel : channel;
            Invoke(() =>
            {
                Form existing;
                lock (NoticeSync)
                {
                    if (!_notices.TryGetValue(key, out existing))
                        existing = null;
                }
                if (existing != null && !existing.IsDisposed)
                    existing.Close();
            });
        }

        private static Form BuildNotice(string channel, string message)
        {
            var form = new Form
            {
                Text = channel + " BARCODE 판독 비활성",
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ClientSize = new Size(560, 150),
                MaximizeBox = false,
                ShowInTaskbar = true,   // 최소화한 뒤 다시 찾을 수 있게 한다.
                TopMost = true,
                BackColor = Color.FromArgb(255, 249, 224)
            };

            form.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 10.5F),
                ForeColor = Color.FromArgb(120, 63, 4),
                Padding = new Padding(16),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = message
            });
            return form;
        }

        // 시퀀스 스레드에서 호출되므로 UI 스레드로 넘긴다. 띄울 창이 없으면 조용히 무시한다.
        private static void Invoke(Action action)
        {
            try
            {
                Form owner = ResolveOwner();
                if (owner == null)
                    return;

                if (owner.InvokeRequired)
                    owner.BeginInvoke(action);
                else
                    action();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "SYSTEM", "BARCODE-NOTICE",
                    "바코드 비활성 안내창 처리 실패: " + ex.Message);
            }
        }

        // [2차 검토수정 2026-08-23] 소유자는 메인 폼(Form1)을 고정 사용하고 캐시한다.
        // 종전 "OpenForms 첫 비안내 폼" 선택은 메인 폼이 OpenForms에서 빠지는 WinForms 특성
        // (핸들 재생성 시 컬렉션 탈락)과 겹치면 일시 다이얼로그를 소유자로 잡았고, 그 다이얼로그가
        // 닫힐 때 안내창이 딸려 닫혀 야간 소크런에서 시간당 ~2회 재생성 로그를 만들었다.
        private static Form _cachedOwner;

        private static Form ResolveOwner()
        {
            Form cached = _cachedOwner;
            if (cached != null && !cached.IsDisposed)
                return cached;

            Form fallback = null;
            foreach (Form form in Application.OpenForms)
            {
                if (form == null || form.IsDisposed)
                    continue;

                bool isNoticeForm;
                lock (NoticeSync)
                    isNoticeForm = _notices.ContainsValue(form);
                if (isNoticeForm)
                    continue;

                if (form is QMC.CDT_320.Form1)
                {
                    _cachedOwner = form;
                    return form;
                }

                if (fallback == null)
                    fallback = form;
            }

            // 메인 폼을 못 찾은 순간의 폴백은 캐시하지 않는다 — 다음 호출에서 메인 폼 재탐색.
            return fallback;
        }
    }
}
