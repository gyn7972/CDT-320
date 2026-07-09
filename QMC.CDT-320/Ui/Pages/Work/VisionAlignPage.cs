using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class VisionAlignPage : PageBase
    {
        private static readonly Color GroupTitleColor = Color.FromArgb(38, 50, 66);

        public VisionAlignPage()
        {
            InitializeComponent();
            BuildLayout();
            ApplyRuntimeUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("work.visionAlign");
            lblHeader.Tag = "i18n:work.visionAlign";
        }

        /// <summary>좌 7 : 우 3 배치. 좌=비전 화면 그대로, 우=ACTION/RESULT 그룹박스.
        /// 상단 헤더는 그룹박스 아닌 현재 바 유지, 여백 최소화. 결과 라벨은 아웃풋 BIN/DIE INFO 스타일.</summary>
        private void BuildLayout()
        {
            SuspendLayout();
            try
            {
                // 7:3 비율 + 여백 최소화
                rootLayout.ColumnStyles.Clear();
                rootLayout.ColumnCount = 2;
                rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
                rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
                rootLayout.Padding = new Padding(0);           // 페이지 가장자리 여백 제거
                lblHeader.Margin = new Padding(0);             // 헤더 주변 여백 제거(다른 페이지와 동일)
                camPanel.Margin = new Padding(0, 0, 3, 0);     // 비전 화면과 우측 그룹 사이 최소 구분선만

                // 기존 컨트롤을 사이드 레이아웃에서 분리
                Control[] actionButtons =
                {
                    btnAutoAlign, btnManualAlign, btnFirstMark, btnSecondMark,
                    btnThetaMatch, btnXyMatch, btnSave, btnClose
                };
                foreach (Control b in actionButtons)
                    b.Parent?.Controls.Remove(b);
                resultLayout.Parent?.Controls.Remove(resultLayout);
                sideLayout.Controls.Clear();

                // ACTION 그룹박스 — 버튼 상하 46px(아웃풋 전환 페이지와 동일), 폭은 그대로.
                const int actionRowH = 46;
                GroupBox grpAction = MakeGroup("ACTION");
                var actionBody = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(0),
                    Padding = new Padding(3, 1, 3, 1),
                    ColumnCount = 1,
                    RowCount = actionButtons.Length
                };
                actionBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                for (int i = 0; i < actionButtons.Length; i++)
                {
                    actionBody.RowStyles.Add(new RowStyle(SizeType.Absolute, actionRowH));
                    Control b = actionButtons[i];
                    b.Dock = DockStyle.Fill;
                    b.Margin = new Padding(3);
                    b.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                    actionBody.Controls.Add(b, 0, i);
                }
                grpAction.Controls.Add(actionBody);
                grpAction.Margin = new Padding(0);
                // 버튼 높이만큼만 그룹박스 축소(타이틀/패딩 포함)
                int actionGroupH = actionButtons.Length * actionRowH + 34;

                // RESULT 그룹박스 — 라벨을 아웃풋 BIN/DIE INFO 그룹과 동일 스타일로
                GroupBox grpResult = MakeGroup("RESULT");
                StyleResultLabelsLikeOutput();
                resultLayout.Dock = DockStyle.Fill;
                resultLayout.Margin = new Padding(0);
                resultLayout.Padding = new Padding(1);
                resultLayout.ColumnStyles.Clear();
                resultLayout.ColumnCount = 2;
                resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
                resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
                grpResult.Controls.Add(resultLayout);
                grpResult.Margin = new Padding(0);

                // sideLayout 재구성: ACTION(상단, 콘텐츠 높이) + 채움 + RESULT(하단 고정)
                sideLayout.RowStyles.Clear();
                sideLayout.ColumnStyles.Clear();
                sideLayout.Margin = new Padding(0);
                sideLayout.ColumnCount = 1;
                sideLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                sideLayout.RowCount = 3;
                sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, actionGroupH));  // ACTION(타이트)
                sideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));           // 채움
                sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 176F));          // RESULT(하단)
                sideLayout.Controls.Add(grpAction, 0, 0);
                sideLayout.Controls.Add(grpResult, 0, 2);
            }
            catch { }
            finally
            {
                ResumeLayout(true);
            }
        }

        private static GroupBox MakeGroup(string text)
        {
            return new GroupBox
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = GroupTitleColor,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                Padding = new Padding(4),
                TabStop = false
            };
        }

        /// <summary>RESULT 캡션/값 라벨을 아웃풋 전환 페이지 BIN/DIE INFO 그룹과 동일한 스타일로 통일.</summary>
        private void StyleResultLabelsLikeOutput()
        {
            Color capBack = Color.FromArgb(236, 238, 241);
            Color capFore = Color.FromArgb(70, 70, 70);
            Color valFore = Color.FromArgb(25, 29, 34);

            Label[] captions = { lblDeltaXCaption, lblDeltaYCaption, lblDeltaThetaCaption, lblScoreCaption };
            Label[] values = { lblDeltaXValue, lblDeltaYValue, lblDeltaThetaValue, lblScoreValue };

            foreach (Label c in captions)
            {
                if (c == null) continue;
                c.AutoEllipsis = true;
                c.BackColor = capBack;
                c.BorderStyle = BorderStyle.FixedSingle;
                c.Dock = DockStyle.Fill;
                c.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                c.ForeColor = capFore;
                c.Margin = new Padding(1);
                c.Padding = new Padding(6, 0, 0, 0);
                c.TextAlign = ContentAlignment.MiddleLeft;
            }
            foreach (Label v in values)
            {
                if (v == null) continue;
                v.AutoEllipsis = true;
                v.BackColor = Color.White;
                v.BorderStyle = BorderStyle.FixedSingle;
                v.Dock = DockStyle.Fill;
                v.Font = new Font("Consolas", 9F);
                v.ForeColor = valFore;
                v.Margin = new Padding(1);
                v.Padding = new Padding(0, 0, 6, 0);
                v.TextAlign = ContentAlignment.MiddleRight;
            }
        }
    }

    public partial class WaferMapOpenPage : PageBase
    {
        public WaferMapOpenPage()
        {
            InitializeComponent();
            BuildLayout();
            ApplyRuntimeUi();
            LoadSampleMapList();
        }

        /// <summary>비전 얼라인과 동일 구조: 좌 7(맵 뷰) : 우 3(파일 리스트 그룹박스), 헤더 풀폭·여백 0.</summary>
        private void BuildLayout()
        {
            SuspendLayout();
            try
            {
                lbMapFiles.Parent?.Controls.Remove(lbMapFiles);
                mapPanel.Parent?.Controls.Remove(mapPanel);
                rootLayout.Controls.Clear();

                rootLayout.ColumnStyles.Clear();
                rootLayout.RowStyles.Clear();
                rootLayout.Padding = new Padding(0);
                rootLayout.ColumnCount = 2;
                rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
                rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
                rootLayout.RowCount = 2;
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
                rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                lblHeader.Margin = new Padding(0);
                rootLayout.Controls.Add(lblHeader, 0, 0);
                rootLayout.SetColumnSpan(lblHeader, 2);

                // 좌: 맵 뷰
                mapPanel.Margin = new Padding(0, 0, 3, 0);
                rootLayout.Controls.Add(mapPanel, 0, 1);

                // 우: MAP FILE LIST 그룹박스
                var grpList = new GroupBox
                {
                    Text = "MAP FILE LIST",
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(38, 50, 66),
                    Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                    Margin = new Padding(0),
                    Padding = new Padding(4),
                    TabStop = false
                };
                lbMapFiles.Dock = DockStyle.Fill;
                lbMapFiles.Margin = new Padding(0);
                lbMapFiles.BorderStyle = BorderStyle.None;
                grpList.Controls.Add(lbMapFiles);
                rootLayout.Controls.Add(grpList, 1, 1);
            }
            catch { }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("work.waferMapOpen");
            lblHeader.Tag = "i18n:work.waferMapOpen";
        }

        private void LoadSampleMapList()
        {
            lbMapFiles.Items.Clear();
            lbMapFiles.Items.Add("Y482CB1_2026-04-24.map");
            lbMapFiles.Items.Add("Y482CB0_2026-04-23.map");
            lbMapFiles.Items.Add("SAMPLE.map");
        }
    }
}
