using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Editors
{
    /// <summary>
    /// 5종 InspectionParameters 편집기를 콤보로 선택하여 표시하는 호스트.
    /// 310 의 5 ParameterEditor 의 기능을 우리 스타일로 통합.
    /// Stage 91 — Designer/Code 분리(디자이너 로드 가능). shell 은 .Designer.cs, 콤보채움/편집기전환은 Code.
    /// </summary>
    public partial class ParameterEditorHost : UserControl
    {
        public ParameterEditorHost()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // 동적 채움 — 콤보 Items. SelectedIndex=0 이 OnToolChanged 를 통해 첫 편집기(BottomInspection) 표시.
            _cbTool.Items.AddRange(new object[]
            {
                "BottomInspection", "SideInspection", "DieGapInspection",
                "Distortion", "VisionScale"
            });
            _cbTool.SelectedIndex = 0;
        }

        private void OnToolChanged(object sender, EventArgs e)
        {
            ShowEditor((string)_cbTool.SelectedItem);
        }

        private void ShowEditor(string tool)
        {
            _content.Controls.Clear();
            var ed = InspectionEditorFactory.Create(tool);   // P3 — 문자열 switch 폐기(공용 팩토리)
            if (ed == null) return;
            ed.Dock = DockStyle.Fill;
            _content.Controls.Add(ed);
        }
    }
}
