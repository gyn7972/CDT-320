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
    /// 레이아웃/컨트롤 = PaginationBar.Designer.cs. 본 파일은 로직(페이지 계산/이벤트)만.
    /// </summary>
    public partial class PaginationBar : FlowLayoutPanel
    {
        private int _total;

        /// <summary>페이지 또는 페이지 크기 변경 알림(호스트가 현재 페이지 구간을 다시 렌더).</summary>
        public event EventHandler PageChanged;

        public PaginationBar()
        {
            InitializeComponent();
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

        /// <summary>현재 페이지 구간만 잘라 반환. IList면 인덱스로 직접 잘라 O(PageSize)로 처리(대량 데이터 페이지 이동 버벅임 방지).</summary>
        public IEnumerable<T> PageSlice<T>(IEnumerable<T> all)
        {
            if (all == null) return Enumerable.Empty<T>();

            int start = (CurrentPage - 1) * PageSize;
            if (start < 0) start = 0;

            // IList<T>(예: List<T>)는 Skip 전체 순회 없이 인덱스로 바로 잘라낸다.
            if (all is IList<T> list)
            {
                if (start >= list.Count) return new List<T>(0);
                int count = Math.Min(PageSize, list.Count - start);
                var page = new List<T>(count);
                for (int i = 0; i < count; i++) page.Add(list[start + i]);
                return page;
            }

            return all.Skip(start).Take(PageSize);
        }

        // ── 이벤트 핸들러(Designer 에서 배선) ──
        private void _cmbSize_SelectedIndexChanged(object sender, EventArgs e) { CurrentPage = 1; Raise(); }
        private void _btnFirst_Click(object sender, EventArgs e) { CurrentPage = 1; Raise(); }
        private void _btnPrev_Click(object sender, EventArgs e)  { CurrentPage -= 1; Raise(); }
        private void _btnGo_Click(object sender, EventArgs e)    { GoToTyped(); }
        private void _btnNext_Click(object sender, EventArgs e)  { CurrentPage += 1; Raise(); }
        private void _btnLast_Click(object sender, EventArgs e)  { CurrentPage = TotalPages; Raise(); }
        private void _txtPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { GoToTyped(); e.SuppressKeyPress = true; }
        }

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
    }
}
