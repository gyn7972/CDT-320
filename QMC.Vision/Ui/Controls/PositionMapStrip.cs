using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 실제 장비 운영뷰의 Map 영역 — 위치별 히트맵 4개(Width · Height · 1 Channel ChippingSize ·
    /// 2 Channel ChippingSize)를 가로로 나란히 배치. 각 칸은 <see cref="PositionMapPanel"/>.
    /// 자식 컨트롤은 코드로 구성(디자이너 변경 최소화) — 단일 맵(WaferMapPanel) 대체.
    ///
    /// 상호작용은 4개 맵을 동기화한다: 한 맵에서 확대/팬/셀선택하면 나머지 3개도 동일 배율·위치·선택으로
    /// 맞춰 같은 Die 를 4지표로 비교할 수 있다. 셀 클릭은 <see cref="CellClicked"/>(IndexX, IndexY)로
    /// 상위(InspectionViewerControl)에 전달돼 결과 표 행 선택/툴팁에 쓰인다.
    /// </summary>
    public class PositionMapStrip : Panel
    {
        private readonly PositionMapPanel[] _maps = new PositionMapPanel[4];
        private static readonly string[] DefaultCaptions =
            { "Width", "Height", "1 Channel ChippingSize", "2 Channel ChippingSize" };

        private bool _syncing;   // 동기화 재귀 가드

        /// <summary>어느 맵에서든 셀 클릭 시 (IndexX=col, IndexY=row) 발생.</summary>
        public event Action<int, int> CellClicked;
        /// <summary>선택 셀 변경/해제 시 (col, row) 발생. (-1,-1)=해제.</summary>
        public event Action<int, int> SelectionChanged;

        public PositionMapStrip()
        {
            BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E);
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E),
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            for (int i = 0; i < 4; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                _maps[i] = new PositionMapPanel { Dock = DockStyle.Fill, Margin = new Padding(1) };
                _maps[i].SetData(DefaultCaptions[i], null);
                _maps[i].ViewChanged += OnMapViewChanged;
                _maps[i].SelectionChanged += OnMapSelectionChanged;
                _maps[i].CellClicked += OnMapCellClicked;
                table.Controls.Add(_maps[i], i, 0);
            }
            Controls.Add(table);
        }

        // ── 4맵 동기화 ─────────────────────────────────────────────────────
        private void OnMapViewChanged(float scale, float offX, float offY)
        {
            if (_syncing) return;
            _syncing = true;
            try { foreach (var m in _maps) m.SetView(scale, offX, offY); }
            finally { _syncing = false; }
        }

        private void OnMapSelectionChanged(int col, int row)
        {
            if (_syncing)
            {
                SelectionChanged?.Invoke(col, row);
                return;
            }
            _syncing = true;
            try { foreach (var m in _maps) m.SetSelection(col, row); }
            finally { _syncing = false; }
            SelectionChanged?.Invoke(col, row);
        }

        private void OnMapCellClicked(int col, int row) => CellClicked?.Invoke(col, row);

        /// <summary>4개 맵 공통 툴팁 텍스트 공급자 설정((col,row)→문자열, null=미표시).</summary>
        public void SetCellInfoProvider(Func<int, int, string> provider)
        {
            foreach (var m in _maps) m.CellInfoProvider = provider;
        }

        /// <summary>선택/확대 상태 초기화(외부 리셋 버튼 등).</summary>
        public void ResetView()
        {
            foreach (var m in _maps) { m.SetSelection(-1, -1); }
            _maps[0].ResetView();   // ViewChanged 로 나머지도 동기화됨
        }

        /// <summary>4개 맵 데이터 일괄 설정(각 grid[row,col]=0~1, NaN=빈칸). null 인 맵은 캡션만 유지.</summary>
        public void SetMaps(double[,] width, double[,] height, double[,] ch1, double[,] ch2)
        {
            _maps[0].SetData(DefaultCaptions[0], width);
            _maps[1].SetData(DefaultCaptions[1], height);
            _maps[2].SetData(DefaultCaptions[2], ch1);
            _maps[3].SetData(DefaultCaptions[3], ch2);
        }

        /// <summary>맵 1칸(i=0~3)의 캡션+데이터 설정. 모드별로 지표가 달라 개별 지정용(data=null 이면 빈 칸).</summary>
        public void SetMap(int i, string caption, double[,] data)
        {
            if (i < 0 || i >= _maps.Length) return;
            _maps[i].SetData(caption ?? "", data);
        }
    }
}
