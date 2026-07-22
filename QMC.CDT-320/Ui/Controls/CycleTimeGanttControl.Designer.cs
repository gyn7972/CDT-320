using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT320.Ui.Controls
{
    partial class CycleTimeGanttControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null) { _timer.Stop(); _timer.Dispose(); _timer = null; }
                if (_tip != null) { _tip.Dispose(); _tip = null; }
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private Panel _toolbar;
        private Button _btnModePicker;
        private Button _btnModeUnit;
        private Button _btnModeCycle;
        private ComboBox _cmbWindow;
        private Button _btnPause;
        private Label _lblTitle;
        private Label _lblMeasure;
        private ListView _lstCycles;
        private ColumnHeader _colTime;
        private ColumnHeader _colUnit;
        private ColumnHeader _colDie;
        private ColumnHeader _colPicker;
        private ColumnHeader _colTotal;
        private ColumnHeader _colState;

        private void InitializeComponent()
        {
            _toolbar = new Panel();
            _btnModePicker = new Button();
            _btnModeUnit = new Button();
            _btnModeCycle = new Button();
            _cmbWindow = new ComboBox();
            _btnPause = new Button();
            _lblTitle = new Label();
            _lblMeasure = new Label();
            _lstCycles = new ListView();
            _colTime = new ColumnHeader();
            _colUnit = new ColumnHeader();
            _colDie = new ColumnHeader();
            _colPicker = new ColumnHeader();
            _colTotal = new ColumnHeader();
            _colState = new ColumnHeader();
            SuspendLayout();
            // ── 상단 툴바 ──
            _toolbar.Dock = DockStyle.Top;
            _toolbar.Height = 34;
            _toolbar.BackColor = Color.FromArgb(0x1a, 0x1f, 0x25);
            _toolbar.Name = "_toolbar";
            _toolbar.Controls.Add(_lblTitle);
            _toolbar.Controls.Add(_btnModePicker);
            _toolbar.Controls.Add(_btnModeUnit);
            _toolbar.Controls.Add(_btnModeCycle);
            _toolbar.Controls.Add(_cmbWindow);
            _toolbar.Controls.Add(_btnPause);
            _toolbar.Controls.Add(_lblMeasure);
            _lblTitle.Text = "CYCLE TIME";
            _lblTitle.ForeColor = Color.FromArgb(0xd9, 0x77, 0x06);
            _lblTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblTitle.AutoSize = false;
            _lblTitle.Size = new Size(110, 26);
            _lblTitle.Location = new Point(10, 4);
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            _btnModePicker.Text = "픽커";
            _btnModePicker.Size = new Size(64, 26);
            _btnModePicker.Location = new Point(120, 4);
            _btnModePicker.FlatStyle = FlatStyle.Flat;
            _btnModePicker.ForeColor = Color.White;
            _btnModePicker.BackColor = Color.FromArgb(0xd9, 0x77, 0x06);
            _btnModePicker.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            _btnModePicker.UseVisualStyleBackColor = false;
            _btnModeUnit.Text = "유닛";
            _btnModeUnit.Size = new Size(64, 26);
            _btnModeUnit.Location = new Point(188, 4);
            _btnModeUnit.FlatStyle = FlatStyle.Flat;
            _btnModeUnit.ForeColor = Color.White;
            _btnModeUnit.BackColor = Color.FromArgb(0x3a, 0x3a, 0x3e);
            _btnModeUnit.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            _btnModeUnit.UseVisualStyleBackColor = false;
            _btnModeCycle.Text = "사이클";
            _btnModeCycle.Size = new Size(64, 26);
            _btnModeCycle.Location = new Point(256, 4);
            _btnModeCycle.FlatStyle = FlatStyle.Flat;
            _btnModeCycle.ForeColor = Color.White;
            _btnModeCycle.BackColor = Color.FromArgb(0x3a, 0x3a, 0x3e);
            _btnModeCycle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            _btnModeCycle.UseVisualStyleBackColor = false;
            _cmbWindow.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbWindow.Size = new Size(72, 26);
            _cmbWindow.Location = new Point(336, 6);
            _cmbWindow.Font = new Font("맑은 고딕", 8.5F);
            _cmbWindow.Items.AddRange(new object[] { "5초", "10초", "30초", "60초", "5분" });
            _btnPause.Text = "일시정지";
            _btnPause.Size = new Size(72, 26);
            _btnPause.Location = new Point(416, 4);
            _btnPause.FlatStyle = FlatStyle.Flat;
            _btnPause.ForeColor = Color.White;
            _btnPause.BackColor = Color.FromArgb(0x3a, 0x3a, 0x3e);
            _btnPause.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            _btnPause.UseVisualStyleBackColor = false;
            // 구간 측정 표시 — 차트 클릭(시작)→클릭(끝) 간격을 ms 로 표시. 우클릭 = 해제.
            _lblMeasure.Text = "구간측정: 차트 클릭→클릭 (우클릭 해제)";
            _lblMeasure.AutoSize = false;
            _lblMeasure.Size = new Size(340, 26);
            _lblMeasure.Location = new Point(500, 4);
            _lblMeasure.ForeColor = Color.FromArgb(0x9f, 0xb2, 0xc8);
            _lblMeasure.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            _lblMeasure.TextAlign = ContentAlignment.MiddleLeft;
            // ── 사이클 모드 목록(좌측) — 기본 숨김 ──
            _lstCycles.Dock = DockStyle.Left;
            _lstCycles.Width = 380;
            _lstCycles.View = View.Details;
            _lstCycles.FullRowSelect = true;
            _lstCycles.HideSelection = false;
            _lstCycles.MultiSelect = false;
            _lstCycles.BackColor = Color.FromArgb(0x14, 0x18, 0x1e);
            _lstCycles.ForeColor = Color.Gainsboro;
            _lstCycles.Font = new Font("맑은 고딕", 8.5F);
            _lstCycles.Visible = false;
            _lstCycles.Columns.AddRange(new[] { _colTime, _colUnit, _colDie, _colPicker, _colTotal, _colState });
            _colTime.Text = "시간"; _colTime.Width = 74;
            _colUnit.Text = "유닛"; _colUnit.Width = 84;
            _colDie.Text = "Die"; _colDie.Width = 46;
            _colPicker.Text = "픽커"; _colPicker.Width = 44;
            _colTotal.Text = "Total(ms)"; _colTotal.Width = 70;
            _colState.Text = "상태"; _colState.Width = 48;
            _btnModePicker.Click += btnModePicker_Click;
            _btnModeUnit.Click += btnModeUnit_Click;
            _btnModeCycle.Click += btnModeCycle_Click;
            _cmbWindow.SelectedIndexChanged += cmbWindow_SelectedIndexChanged;
            _btnPause.Click += btnPause_Click;
            _lstCycles.SelectedIndexChanged += lstCycles_SelectedIndexChanged;
            Controls.Add(_lstCycles);
            Controls.Add(_toolbar);
            BackColor = Color.FromArgb(0x0e, 0x11, 0x15);
            Name = "CycleTimeGanttControl";
            Size = new Size(1100, 700);
            ResumeLayout(false);
        }
    }
}
