using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 재사용 페이지네이션 바 — 페이지 크기 선택(드롭다운) + 페이지 번호 입력칸/이동 + 이전·다음(처음/끝).
    /// 데이터(메모리)는 호스트가 보유하고, 본 바는 현재 페이지/페이지 크기만 관리한다.
    /// 페이지·크기 변경 시 <see cref="PageChanged"/> 를 발생시키면, 호스트가
    /// <see cref="PageSlice{T}"/> 로 현재 페이지 구간만 그리드에 바인딩한다(= 한 번에 다 안 그려 버퍼링 방지).
    /// </summary>
    public class PaginationBar : FlowLayoutPanel
    {
        private readonly ComboBox _cmbSize = new ComboBox();
        private readonly Button   _btnFirst = new Button();
        private readonly Button   _btnPrev  = new Button();
        private readonly TextBox  _txtPage  = new TextBox();
        private readonly Button   _btnGo    = new Button();
        private readonly Label    _lblTotal = new Label();
        private readonly Button   _btnNext  = new Button();
        private readonly Button   _btnLast  = new Button();

        private int _total;

        /// <summary>페이지 또는 페이지 크기 변경 알림(호스트가 현재 페이지 구간을 다시 렌더).</summary>
        public event EventHandler PageChanged;

        public PaginationBar()
        {
            Dock = DockStyle.Bottom;
            Height = 36;
            WrapContents = false;
            FlowDirection = FlowDirection.LeftToRight;
            Padding = new Padding(8, 5, 8, 5);

            _cmbSize.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbSize.Width = 90;
            _cmbSize.Margin = new Padding(2, 2, 12, 2);
            _cmbSize.Items.AddRange(new object[] { "100행", "200행", "500행", "1000행" });
            _cmbSize.SelectedIndex = 1;   // 기본 200행
            _cmbSize.SelectedIndexChanged += (s, e) => { CurrentPage = 1; Raise(); };

            InitButton(_btnFirst, "◀◀", () => { CurrentPage = 1; Raise(); });
            InitButton(_btnPrev,  "◀",  () => { CurrentPage -= 1; Raise(); });

            _txtPage.Width = 48;
            _txtPage.Margin = new Padding(4, 4, 2, 2);
            _txtPage.TextAlign = HorizontalAlignment.Center;
            _txtPage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { GoToTyped(); e.SuppressKeyPress = true; }
            };

            InitButton(_btnGo, "이동", GoToTyped);

            _lblTotal.AutoSize = true;
            _lblTotal.Margin = new Padding(6, 8, 10, 2);
            _lblTotal.Text = "/ 1 (0행)";

            InitButton(_btnNext, "▶",  () => { CurrentPage += 1; Raise(); });
            InitButton(_btnLast, "▶▶", () => { CurrentPage = TotalPages; Raise(); });

            Controls.Add(_cmbSize);
            Controls.Add(_btnFirst);
            Controls.Add(_btnPrev);
            Controls.Add(_txtPage);
            Controls.Add(_btnGo);
            Controls.Add(_lblTotal);
            Controls.Add(_btnNext);
            Controls.Add(_btnLast);

            CurrentPage = 1;
            UpdateView();
        }

        /// <summary>현재 선택된 페이지 크기(행 수).</summary>
        public int PageSize
        {
            get
            {
                switch (_cmbSize.SelectedIndex)
                {
                    case 0:  return 100;
                    case 2:  return 500;
                    case 3:  return 1000;
                    default: return 200;
                }
            }
        }

        /// <summary>현재 페이지(1부터).</summary>
        public int CurrentPage { get; private set; }

        /// <summary>총 페이지 수(최소 1).</summary>
        public int TotalPages => _total <= 0 ? 1 : (_total + PageSize - 1) / PageSize;

        /// <summary>현재 페이지를 1로 되돌린다(이벤트 미발생). 새 조회/검색/정렬 시 호출.</summary>
        public void Reset() { CurrentPage = 1; }

        /// <summary>전체 행 수 설정 → 총 페이지/현재 페이지 클램프 + 표시 갱신.(이벤트 미발생)</summary>
        public void SetTotal(int total)
        {
            _total = total < 0 ? 0 : total;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;
            UpdateView();
        }

        /// <summary>현재 페이지 구간만 잘라 반환.</summary>
        public IEnumerable<T> PageSlice<T>(IEnumerable<T> all)
            => all == null ? Enumerable.Empty<T>()
                           : all.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        private void GoToTyped()
        {
            if (int.TryParse(_txtPage.Text.Trim(), out var p))
            {
                CurrentPage = p;
                Raise();
            }
            else UpdateView();   // 잘못 입력 → 현재값 복원
        }

        private void Raise()
        {
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            UpdateView();
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateView()
        {
            _txtPage.Text  = CurrentPage.ToString();
            _lblTotal.Text = "/ " + TotalPages + " (" + _total + "행)";
            _btnFirst.Enabled = _btnPrev.Enabled = CurrentPage > 1;
            _btnNext.Enabled  = _btnLast.Enabled = CurrentPage < TotalPages;
        }

        private void InitButton(Button b, string text, Action onClick)
        {
            b.Text = text;
            b.Width = text.Length <= 2 ? 38 : 50;
            b.Margin = new Padding(2, 2, 2, 2);
            b.FlatStyle = FlatStyle.System;
            b.Click += (s, e) => onClick();
        }
    }
}
