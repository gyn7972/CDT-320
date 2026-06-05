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
        private readonly List<ActionButton> _actionButtons = new List<ActionButton>();

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
            int column = index % 3;
            int row = index / 3;
            while (_actionArea.RowCount <= row)
            {
                _actionArea.RowCount++;
                _actionArea.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33333F));
            }

            var btn = new ActionButton
            {
                Dock = DockStyle.Fill,
                Text = text,
                Margin = new Padding(6)
            };

            btn.Click += (s, e) =>
            {
                SelectedAction = text;
                try { onClick?.Invoke(); } catch { }
                DialogResult = DialogResult.OK;
                Close();
            };

            _actionButtons.Add(btn);
            _actionArea.Controls.Add(btn, column, row);
            return btn;
        }

        public void SetTitle(string titleI18n)
        {
            _titleLabel.Tag = "i18n:" + titleI18n;
            _titleLabel.Text = Lang.T(titleI18n);
        }
    }
}
