using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 그리드 셀의 긴 텍스트(Description 등)를 큰 창으로 보여주는 공용 다이얼로그.
    /// <para>
    /// editable=false: 읽기 전용 뷰어(로그·알람 페이지). 텍스트 복사만 가능.
    /// editable=true : 편집 가능(메시지편집 페이지). 확인 시 <see cref="TextValue"/> 로 편집 결과를 돌려준다.
    /// </para>
    /// </summary>
    public partial class TextViewerDialog : Form
    {
        /// <summary>현재(편집 후) 텍스트. 편집 모드에서 확인을 눌렀을 때 호출자가 읽어 반영한다.</summary>
        public string TextValue
        {
            get { return txtContent.Text; }
        }

        public TextViewerDialog(string title, string content, bool editable)
        {
            InitializeComponent();
            StyleButtons();
            ApplyContent(title, content, editable);
        }

        // 플랫 버튼의 테두리/호버 색상을 정리한다(Designer 색상 보완).
        private void StyleButtons()
        {
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 152, 219);
            btnOk.FlatAppearance.MouseDownBackColor = Color.FromArgb(31, 108, 158);

            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 246, 250);
        }

        // 제목/내용/편집가능 여부를 창에 반영한다.
        private void ApplyContent(string title, string content, bool editable)
        {
            try
            {
                string caption = string.IsNullOrEmpty(title) ? "DESCRIPTION" : title;
                Text = caption;
                lblTitle.Text = caption;

                txtContent.Text = content ?? string.Empty;
                txtContent.ReadOnly = !editable;
                txtContent.TextChanged += (s, e) => UpdateInfo();

                // 읽기 전용이면 확인/취소 대신 '닫기' 버튼 하나만 노출한다.
                btnCancel.Visible = editable;
                btnOk.Text = editable ? "확인" : "닫기";
                btnOk.DialogResult = editable ? DialogResult.OK : DialogResult.Cancel;

                UpdateInfo();

                // 커서를 맨 앞으로 두어 긴 내용의 시작부터 보이게 한다.
                txtContent.SelectionStart = 0;
                txtContent.SelectionLength = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 글자 수 / 줄 수를 하단 왼쪽에 표시한다.
        private void UpdateInfo()
        {
            try
            {
                int chars = txtContent.TextLength;
                int lines = txtContent.Lines.Length;
                if (lines == 0) lines = 1;
                lblInfo.Text = string.Format("{0:N0}자 · {1:N0}줄", chars, lines);
            }
            catch
            {
                // 표시용 라벨 갱신 실패는 기능에 영향 없으므로 무시한다.
            }
        }
    }
}
