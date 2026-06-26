using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 실제 장비 운영뷰의 Map 영역 — 위치별 히트맵 4개(Width · Height · 1 Channel ChippingSize ·
    /// 2 Channel ChippingSize)를 가로로 나란히 배치. 각 칸은 <see cref="PositionMapPanel"/>.
    /// 자식 컨트롤은 코드로 구성(디자이너 변경 최소화) — 단일 맵(WaferMapPanel) 대체.
    /// </summary>
    public class PositionMapStrip : Panel
    {
        private readonly PositionMapPanel[] _maps = new PositionMapPanel[4];
        private static readonly string[] DefaultCaptions =
            { "Width", "Height", "1 Channel ChippingSize", "2 Channel ChippingSize" };

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
                table.Controls.Add(_maps[i], i, 0);
            }
            Controls.Add(table);
        }

        /// <summary>4개 맵 데이터 일괄 설정(각 grid[row,col]=0~1, NaN=빈칸). null 인 맵은 캡션만 유지.</summary>
        public void SetMaps(double[,] width, double[,] height, double[,] ch1, double[,] ch2)
        {
            _maps[0].SetData(DefaultCaptions[0], width);
            _maps[1].SetData(DefaultCaptions[1], height);
            _maps[2].SetData(DefaultCaptions[2], ch1);
            _maps[3].SetData(DefaultCaptions[3], ch2);
        }
    }
}
