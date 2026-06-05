using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    /// <summary>
    /// 5종 InspectionParameters 편집기 공통 베이스.
    /// 서브클래스: BuildEditor 에서 입력 컨트롤 + LoadFromParameters / SaveToParameters 구현.
    /// </summary>
    public abstract class ParameterEditorBase : UserControl
    {
        protected Panel _editorPanel;
        protected Label _lblPath;
        protected string _toolName;
        protected string _jsonPath;

        protected ParameterEditorBase(string toolName)
        {
            _toolName = toolName;
            _jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", toolName + ".json");
            BuildShell();
            try { OnLoad(); } catch { }
        }

        private void BuildShell()
        {
            BackColor = UiTheme.MainBg;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30,
                Text = $"{_toolName} Parameters",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.WhiteSmoke };
            var btnLoad = new Button
            {
                Location = new Point(8, 4), Size = new Size(100, 32),
                Text = "Reload", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White
            };
            btnLoad.Click += (s, e) => OnLoad();
            bar.Controls.Add(btnLoad);

            var btnSave = new Button
            {
                Location = new Point(116, 4), Size = new Size(120, 32),
                Text = "SAVE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = UiTheme.Accent, ForeColor = Color.White
            };
            btnSave.Click += (s, e) => OnSave();
            bar.Controls.Add(btnSave);

            _lblPath = new Label
            {
                Location = new Point(244, 10), Size = new Size(640, 24),
                Text = _jsonPath, Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            bar.Controls.Add(_lblPath);
            Controls.Add(bar);

            _editorPanel = new Panel
            {
                Dock = DockStyle.Fill, BackColor = UiTheme.MainBg,
                AutoScroll = true, Padding = new Padding(10)
            };
            Controls.Add(_editorPanel);
            BuildEditor(_editorPanel);
        }

        protected abstract void BuildEditor(Panel container);
        protected abstract void LoadFromParameters();
        protected abstract void SaveToParameters();

        private void OnLoad()
        {
            try
            {
                LoadFromParameters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load fail: " + ex.Message);
            }
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

        // ── 편의 헬퍼 ──
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
