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
        private static Form _notice;

        public static void Show(string message)
        {
            Invoke(() =>
            {
                if (_notice != null && !_notice.IsDisposed)
                    return;

                Form owner = ResolveOwner();
                if (owner == null)
                    return;

                _notice = BuildNotice(message);
                _notice.FormClosed += (s, e) => _notice = null;
                _notice.Show(owner);

                EventLogger.Write(EventKind.Warning, "SYSTEM", "BARCODE-DISABLED",
                    "바코드 판독 비활성 안내: " + (message ?? string.Empty).Replace("\r\n", " "));
            });
        }

        /// <summary>바코드가 다시 켜졌을 때 안내창을 닫는다.</summary>
        public static void Close()
        {
            Invoke(() =>
            {
                if (_notice != null && !_notice.IsDisposed)
                    _notice.Close();
            });
        }

        private static Form BuildNotice(string message)
        {
            var form = new Form
            {
                Text = "BARCODE 판독 비활성",
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

        private static Form ResolveOwner()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form != null && !form.IsDisposed && !ReferenceEquals(form, _notice))
                    return form;
            }
            return null;
        }
    }
}
