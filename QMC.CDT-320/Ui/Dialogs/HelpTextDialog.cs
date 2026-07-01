using System;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class HelpTextDialog : Form
    {
        private readonly string _title;
        private readonly string _content;

        public HelpTextDialog(string title, string content)
        {
            try
            {
                _title = string.IsNullOrWhiteSpace(title) ? "HELP" : title;
                _content = content ?? string.Empty;

                InitializeComponent();
                ApplyText();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "HELP-TEXT-DIALOG-INIT", "도움말 창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        public static DialogResult ShowDialog(IWin32Window owner, string title, string content)
        {
            using (HelpTextDialog dialog = new HelpTextDialog(title, content))
            {
                return dialog.ShowDialog(owner);
            }
        }

        private void ApplyText()
        {
            try
            {
                Text = _title;
                lblTitle.Text = _title;
                txtContent.Text = _content;
                txtContent.SelectionStart = 0;
                txtContent.SelectionLength = 0;
                txtContent.ScrollToCaret();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "HELP-TEXT-DIALOG-TEXT", "도움말 내용 표시 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "HELP-TEXT-DIALOG-CLOSE", "도움말 창 닫기 실패: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
