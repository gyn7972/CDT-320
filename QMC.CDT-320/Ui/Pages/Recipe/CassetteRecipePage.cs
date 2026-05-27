using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 - INPUT/OUTPUT CASSETTE (공용).
    /// 좌측 동작 버튼 + 중앙 옵션 + 대기시간 + 실린더 I/O + 우측 조그/속도.
    /// </summary>
    public partial class CassetteRecipePage : PageBase
    {
        private string _titleI18n = "INPUT/OUTPUT CASSETTE";

        public CassetteRecipePage() : this("INPUT/OUTPUT CASSETTE")
        {
        }

        public CassetteRecipePage(string titleI18n)
        {
            InitializeComponent();

            _titleI18n = string.IsNullOrWhiteSpace(titleI18n) ? "INPUT/OUTPUT CASSETTE" : titleI18n;
            lblHeader.Text = _titleI18n; // 디자이너 필드 접근
        }

        // TODO: 여기부터 기능/이벤트/바인딩 로직만 작성
        private void btnLoadingMove_Click(object sender, EventArgs e) { }
        private void btnUnloadingMove_Click(object sender, EventArgs e) { }
        private void btnReadyMove_Click(object sender, EventArgs e) { }
        private void btnSlotLoadingMove_Click(object sender, EventArgs e) { }
        private void btnSlotUnloadingMove_Click(object sender, EventArgs e) { }

        private void btnJogPlus_Click(object sender, EventArgs e) { }
        private void btnJogMinus_Click(object sender, EventArgs e) { }
        private void btnJogStop_Click(object sender, EventArgs e) { }
        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            lblSpeedValue.Text = $"{trkSpeed.Value} %";
        }
    }

    //public class CassetteRecipePage : PageBase
    //{
    //    private bool _uiBuilt;
    //    private string _titleI18n = "INPUT/OUTPUT CASSETTE";

    //    public CassetteRecipePage() : this("INPUT/OUTPUT CASSETTE") { }

    //    public CassetteRecipePage(string titleI18n)
    //    {
    //        _titleI18n = string.IsNullOrWhiteSpace(titleI18n) ? "INPUT/OUTPUT CASSETTE" : titleI18n;

    //        SuspendLayout();

    //        // 좌표 기반 UI라 디자인/런타임 스케일 차이 최소화
    //        AutoScaleMode = AutoScaleMode.None;
    //        AutoScroll = true;
    //        BackColor = Color.White;

    //        SafeBuildUi(_titleI18n);

    //        ResumeLayout(false);
    //    }

    //    protected override void OnCreateControl()
    //    {
    //        base.OnCreateControl();

    //        // 디자이너에서 생성 순서 때문에 생성자 시점 빌드가 실패하는 경우가 있어 재시도
    //        if (!_uiBuilt)
    //            SafeBuildUi(_titleI18n);
    //    }

    //    private void SafeBuildUi(string titleI18n)
    //    {
    //        try
    //        {
    //            BuildUi(titleI18n);
    //            _uiBuilt = true;
    //        }
    //        catch (Exception ex)
    //        {
    //            // 디자인타임 예외 시 빈 화면 대신 에러 표시
    //            Controls.Clear();
    //            Controls.Add(new Label
    //            {
    //                Dock = DockStyle.Fill,
    //                BackColor = Color.White,
    //                ForeColor = Color.DarkRed,
    //                TextAlign = ContentAlignment.MiddleCenter,
    //                Text = "CassetteRecipePage 디자인 렌더 실패\r\n" + ex.GetType().Name + ": " + ex.Message
    //            });
    //        }
    //    }

    //    private void BuildUi(string titleI18n)
    //    {
    //        Controls.Clear();

    //        Controls.Add(CreateSectionHeader(titleI18n));

    //        // 좌측 동작
    //        Controls.Add(RecipeLayout.OrangeBar(8, 36, 240, "동작"));
    //        int y = 70;
    //        foreach (var a in new[] { "LOADING 위치 이동", "UNLOADING 위치 이동", "준비 위치 이동" })
    //        {
    //            Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = a });
    //            y += 52;
    //        }
    //        y += 180;
    //        Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = "SLOT 위치 이동 (LOADING)" }); y += 52;
    //        Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = "SLOT 위치 이동 (UNLOADING)" }); y += 52;

    //        // 좌하단 실린더 & I/O
    //        Controls.Add(RecipeLayout.OrangeBar(8, y + 8, 240, "실린더 & I/O"));
    //        y += 44;
    //        foreach (var t in new[] { "CASSETTE 감지 SENSOR 1", "CASSETTE 감지 SENSOR 2", "돌출 감지 SENSOR", "맵핑센서" })
    //        {
    //            Controls.Add(new IndicatorDot { Location = new Point(14, y + 6), Size = new Size(12, 12), OnColor = Color.LimeGreen });
    //            Controls.Add(new Label
    //            {
    //                Location = new Point(30, y),
    //                Size = new Size(212, 22),
    //                Text = t,
    //                BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
    //                Font = new Font("맑은 고딕", 9F),
    //                TextAlign = ContentAlignment.MiddleLeft,
    //                Padding = new Padding(6, 0, 0, 0),
    //                BorderStyle = BorderStyle.FixedSingle
    //            });
    //            y += 24;
    //        }

    //        // 중앙 옵션
    //        Controls.Add(RecipeLayout.OrangeBar(260, 36, 600, "옵션"));
    //        y = 70;
    //        foreach (var kv in new (string, string)[]
    //        {
    //            ("LOADING Z", "121070 um"),
    //            ("UNLOADING Z", "117000 um"),
    //            ("READY POSITION", "129771 um"),
    //            ("MAPPING Z", "104745 um"),
    //            ("SLOT PITCH", "19000 um"),
    //            ("카세트 간격", "59000 um"),
    //            ("8인치 or 12인치", "12인치"),
    //            ("카세트 2단 활성화", "1단"),
    //        })
    //        {
    //            RecipeLayout.AddPair(this, 264, y, 200, 380, kv.Item1, kv.Item2);
    //            y += 32;
    //        }

    //        // 대기시간
    //        Controls.Add(RecipeLayout.OrangeBar(260, y + 8, 600, "대기시간"));
    //        y += 44;
    //        RecipeLayout.AddPair(this, 264, y, 200, 380, "이동 후 대기시간", "100 ms");

    //        // 조그 영역
    //        Controls.Add(RecipeLayout.OrangeBar(880, 540, 300, "조그 운전"));
    //        var jog = RecipeLayout.SimpleJog(880, 570, 300, 320, "AXIS Z");
    //        Controls.Add(jog);

    //        // 속도
    //        Controls.Add(RecipeLayout.VerticalSpeedBar(1200, 36, 140, 880));
    //    }

    //    // 디자이너 기본 크기
    //    protected override Size DefaultSize => new Size(1360, 920);
    //}
}
