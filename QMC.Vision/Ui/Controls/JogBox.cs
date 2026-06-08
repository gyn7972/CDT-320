using System.ComponentModel;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>핸들러 축 이동용 Jog Box — X/Y/Z/R + 속도.
    /// Stage 90 — Designer/Code 분리(디자이너 로드 가능). 레이아웃은 JogBox.Designer.cs.</summary>
    public partial class JogBox : Panel
    {
        public JogBox()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // 동적 채움(콤보 Items) — 런타임.
            _cmbSpeed.Items.AddRange(new object[] { "Coarse", "Fine" });
            _cmbSpeed.SelectedIndex = 1;
        }
    }
}
