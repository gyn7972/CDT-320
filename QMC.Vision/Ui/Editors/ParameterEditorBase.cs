using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    /// <summary>
    /// 5종 InspectionParameters 편집기 공통 베이스.
    /// 서브클래스: BuildEditor 에서 입력 컨트롤 + LoadFromParameters / SaveToParameters 구현.
    /// Stage 91 — Designer/Code 분리(디자이너 로드 가능). shell 은 .Designer.cs, 동적 입력은 자식 BuildEditor(Code).
    /// </summary>
    public abstract partial class ParameterEditorBase : UserControl
    {
        protected string _toolName;
        protected string _jsonPath;

        protected ParameterEditorBase(string toolName)
        {
            _toolName = toolName;
            _jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", toolName + ".json");

            InitializeComponent();
            _lblHeader.Text = $"{_toolName} Parameters";   // 동적(toolName 기준)
            _lblPath.Text   = _jsonPath;
            BuildEditor(_editorPanel);                      // 자식 동적 입력 폼

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            OnLoad();                                       // 런타임 — JSON 로드
        }

        protected abstract void BuildEditor(Panel container);
        protected abstract void LoadFromParameters();
        protected abstract void SaveToParameters();

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnReloadClick(object sender, EventArgs e) => OnLoad();
        private void OnSaveClick(object sender, EventArgs e)   => OnSave();

        private void OnLoad()
        {
            try { LoadFromParameters(); }
            catch (Exception ex) { MessageBox.Show("Load fail: " + ex.Message); }
        }

        private void OnSave()
        {
            try
            {
                SaveToParameters();
                MessageBox.Show("Saved: " + _jsonPath, "Parameters",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Save fail: " + ex.Message); }
        }

        // ── 편의 헬퍼 (자식 BuildEditor 에서 동적 컨트롤 생성) ──
        protected Label MakeLabel(string text, int x, int y, int w = 220)
            => new Label
            {
                Location = new Point(x, y), Size = new Size(w, 26),
                Text = text, Font = UiTheme.ButtonFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

        protected NumericUpDown MakeNum(int x, int y, decimal min, decimal max, int decimals = 0,
                                        int w = 160, decimal increment = 0)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y), Size = new Size(w, 26),
                Minimum = min, Maximum = max, DecimalPlaces = decimals,
                Increment = increment > 0 ? increment : (decimals > 0 ? (decimal)Math.Pow(0.1, decimals) : 1m),
                Font = UiTheme.ValueFont
            };
        }

        protected TextBox MakeText(int x, int y, int w = 240)
            => new TextBox
            {
                Location = new Point(x, y), Size = new Size(w, 26),
                Font = UiTheme.ValueFont
            };

        protected ComboBox MakeCombo(int x, int y, string[] items, int w = 200)
        {
            var cb = new ComboBox
            {
                Location = new Point(x, y), Size = new Size(w, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont
            };
            cb.Items.AddRange(items);
            return cb;
        }

        protected CheckBox MakeCheck(int x, int y, string text, int w = 300)
            => new CheckBox
            {
                Location = new Point(x, y), Size = new Size(w, 26),
                Text = text, Font = UiTheme.ButtonFont
            };
    }
}
