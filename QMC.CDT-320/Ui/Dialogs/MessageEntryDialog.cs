using System;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>메시지 카탈로그 1건을 코드/설명/영문/종류로 나눠 입력받는 작은 대화상자.</summary>
    public partial class MessageEntryDialog : Form
    {
        public MessageDefinition Result { get; private set; }

        public MessageEntryDialog(MessageDefinition existing = null)
        {
            InitializeComponent();

            cmbKind.Items.Clear();
            foreach (var k in Enum.GetNames(typeof(EventKind))) cmbKind.Items.Add(k);

            if (existing != null)
            {
                Text = "MESSAGE - EDIT";
                txtCode.Text = existing.Code ?? string.Empty;
                txtKo.Text   = existing.Ko ?? string.Empty;
                txtEn.Text   = (existing.En ?? string.Empty).ToUpperInvariant();
                cmbKind.SelectedItem = existing.Kind.ToString();
            }
            else
            {
                Text = "MESSAGE - ADD";
            }
            if (cmbKind.SelectedIndex < 0) cmbKind.SelectedIndex = 0;

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string code = (txtCode.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show(this, "CODE 를 입력하세요.", "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EventKind kind;
            if (!Enum.TryParse(cmbKind.SelectedItem as string, out kind)) kind = EventKind.Event;

            Result = new MessageDefinition
            {
                Code = code,
                Ko   = (txtKo.Text ?? string.Empty).Trim(),
                En   = (txtEn.Text ?? string.Empty).Trim().ToUpperInvariant(), // 영어는 대문자
                Kind = kind
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
