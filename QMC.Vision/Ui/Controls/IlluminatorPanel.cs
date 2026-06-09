using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>조명 4개 채널 밝기 슬라이더.
    /// Stage 90 — Designer/Code 분리(디자이너 로드 가능). 4채널 unroll + named 핸들러로 closure 제거. 레이아웃은 .Designer.cs.</summary>
    public partial class IlluminatorPanel : Panel
    {
        public IlluminatorPanel()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        }

        // 트랙바 값 변경 → 해당 채널 값 라벨 갱신 (sender 비교로 closure 캡처 없이).
        private void OnTrackValueChanged(object sender, EventArgs e)
        {
            var tb = sender as TrackBar;
            if (tb == null) return;
            if      (tb == _tb1) _val1.Text = tb.Value.ToString();
            else if (tb == _tb2) _val2.Text = tb.Value.ToString();
            else if (tb == _tb3) _val3.Text = tb.Value.ToString();
            else if (tb == _tb4) _val4.Text = tb.Value.ToString();
        }
    }
}
