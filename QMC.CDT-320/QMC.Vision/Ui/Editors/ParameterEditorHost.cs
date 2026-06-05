using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    /// <summary>
    /// 5종 InspectionParameters 편집기를 콤보로 선택하여 표시하는 호스트.
    /// 310 의 5 ParameterEditor 의 기능을 우리 스타일로 통합.
    /// </summary>
    public class ParameterEditorHost : UserControl
    {
        private ComboBox _cbTool;
        private Panel    _content;

        public ParameterEditorHost()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
            ShowEditor("BottomInspection");
        }

        private void BuildLayout()
        {
            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "Inspection Parameter Editors",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.WhiteSmoke };
            bar.Controls.Add(new Label { Location = new Point(10, 10), AutoSize = true, Text = "Tool:", Font = UiTheme.ButtonFont });
            _cbTool = new ComboBox
            {
                Location = new Point(60, 6), Size = new Size(280, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ButtonFont
            };
            _cbTool.Items.AddRange(new object[]
            {
                "BottomInspection", "SideInspection", "DieGapInspection",
                "Distortion", "VisionScale"
            });
            _cbTool.SelectedIndex = 0;
            _cbTool.SelectedIndexChanged += (s, e) =>
            {
                ShowEditor((string)_cbTool.SelectedItem);
            };
            bar.Controls.Add(_cbTool);
            Controls.Add(bar);

            _content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            Controls.Add(_content);
        }

        private void ShowEditor(string tool)
        {
            _content.Controls.Clear();
            UserControl ed;
            switch (tool)
            {
                case "BottomInspection": ed = new BottomInspectionParameterEditor(); break;
                case "SideInspection":   ed = new SideInspectionParameterEditor();   break;
                case "DieGapInspection": ed = new DieGapInspectionParameterEditor(); break;
                case "Distortion":       ed = new DistortionParameterEditor();       break;
                case "VisionScale":      ed = new VisionScaleParameterEditor();      break;
                default: ed = null; break;
            }
            if (ed == null) return;
            ed.Dock = DockStyle.Fill;
            _content.Controls.Add(ed);
        }
    }
}
