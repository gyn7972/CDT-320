using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class ModeOverlayDialog : Form
    {
        private const int CompactDialogWidth = 380;
        private const int CompactTitleHeight = 68;

        private readonly List<ActionButton> _actionButtons = new List<ActionButton>();
        private bool _compactCommandLayout;
        private int _actionColumnCount = 3;
        private int _compactActionRowHeight = 52;

        public string SelectedAction { get; private set; }

        public ModeOverlayDialog()
            : this("dlg.mode")
        {
        }

        public ModeOverlayDialog(string titleI18n)
        {
            InitializeComponent();
            Text = Lang.T(titleI18n);
            SetTitle(titleI18n);
            Load += (s, e) => Lang.Apply(this);
        }

        public ActionButton AddAction(string text, Action onClick = null, int width = 140)
        {
            int index = _actionButtons.Count;
            int columnCount = Math.Max(1, _actionColumnCount);
            int column = index % columnCount;
            int row = index / columnCount;
            while (_actionArea.RowCount <= row)
            {
                _actionArea.RowCount++;
                _actionArea.RowStyles.Add(_compactCommandLayout
                    ? new RowStyle(SizeType.Absolute, _compactActionRowHeight)
                    : new RowStyle(SizeType.Percent, 33.33333F));
            }

            var btn = new ActionButton
            {
                Dock = DockStyle.Fill,
                Text = text,
                Margin = new Padding(6)
            };
            if (_compactCommandLayout)
            {
                StyleCompactActionButton(btn);
                ApplyCompactActionSpacing(btn);
            }

            btn.Click += (s, e) =>
            {
                SelectedAction = text;
                try { onClick?.Invoke(); } catch { }
                DialogResult = DialogResult.OK;
                Close();
            };

            _actionButtons.Add(btn);
            _actionArea.Controls.Add(btn, column, row);
            UpdateCompactDialogSize();
            return btn;
        }

        protected void UseCompactCommandLayout(int actionColumns = 2)
        {
            _compactCommandLayout = true;
            _actionColumnCount = Math.Max(1, actionColumns);

            SuspendLayout();
            try
            {
                ClientSize = new Size(CompactDialogWidth, CompactTitleHeight + _compactActionRowHeight);
                BackColor = Color.White;

                rootLayout.BackColor = Color.White;
                rootLayout.Margin = Padding.Empty;
                rootLayout.Padding = Padding.Empty;
                rootLayout.RowStyles.Clear();
                rootLayout.RowCount = 2;
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, CompactTitleHeight));
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, _compactActionRowHeight));

                _topPanel.BackColor = Color.FromArgb(38, 50, 66);
                _topPanel.Margin = Padding.Empty;
                _titleLabel.BackColor = Color.FromArgb(38, 50, 66);
                _titleLabel.ForeColor = Color.White;
                _titleLabel.Font = new Font("Malgun Gothic", 20F, FontStyle.Bold);
                _titleLabel.Margin = Padding.Empty;
                _titleLabel.Padding = new Padding(8, 0, 8, 0);
                _titleLabel.TextAlign = ContentAlignment.MiddleCenter;

                _actionArea.Controls.Clear();
                _actionArea.BackColor = Color.White;
                _actionArea.Margin = Padding.Empty;
                _actionArea.Padding = new Padding(4);
                _actionArea.ColumnStyles.Clear();
                _actionArea.RowStyles.Clear();
                _actionArea.ColumnCount = _actionColumnCount;
                _actionArea.RowCount = 0;
                for (int i = 0; i < _actionColumnCount; i++)
                    _actionArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / _actionColumnCount));
                UpdateCompactDialogSize();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        protected void SetActionColumnSpan(Control action, int span)
        {
            if (action == null || _actionArea == null || span <= 1)
                return;

            _actionArea.SetColumnSpan(action, Math.Min(span, Math.Max(1, _actionArea.ColumnCount)));
        }

        protected void SetTitleText(string title)
        {
            Text = title ?? string.Empty;
            _titleLabel.Tag = null;
            _titleLabel.Text = Text;
        }

        private static void StyleCompactActionButton(ActionButton button)
        {
            if (button == null)
                return;

            button.BackColor = Color.FromArgb(128, 128, 128);
            button.ForeColor = Color.White;
            button.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
            button.Margin = Padding.Empty;
            button.MinimumSize = new Size(0, 0);
        }

        private static void ApplyCompactActionSpacing(ActionButton button)
        {
            if (button == null)
                return;

            button.Margin = new Padding(4);
        }

        private void UpdateCompactDialogSize()
        {
            if (!_compactCommandLayout || rootLayout.RowStyles.Count < 2)
                return;

            int rowCount = Math.Max(1, _actionArea.RowCount);
            int actionHeight = _actionArea.Padding.Vertical + (_compactActionRowHeight * rowCount);
            rootLayout.RowStyles[1].Height = actionHeight;
            ClientSize = new Size(CompactDialogWidth, CompactTitleHeight + actionHeight);
        }

        public void SetTitle(string titleI18n)
        {
            Text = Lang.T(titleI18n);
            _titleLabel.Tag = "i18n:" + titleI18n;
            _titleLabel.Text = Text;
        }
    }
}
