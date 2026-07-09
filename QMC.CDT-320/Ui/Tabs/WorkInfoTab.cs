using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.WorkInfo;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>?? ?? ? - INPUT/OUTPUT ?? ?? ??.</summary>
    public partial class WorkInfoTab : TabBase
    {
        public WorkInfoTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // 작업정보 탭 콘텐츠 배경 + 사이드바 헤더('작업정보' 라벨)를 흰색으로(페이지 흰 패널과 통일).
            PnlContent.BackColor = System.Drawing.Color.White;
            this.BackColor = System.Drawing.Color.White;

            SetSidebarHeader("tab.workInfo");
            LblSidebarHeader.BackColor = System.Drawing.Color.White;
            const UserLevel op = UserLevel.Operator;

            RegisterSidebarButton(BtnInputCassette,        "wi.inputCassette",     op, () => Whitened(new InputCassettePage()));
            RegisterSidebarButton(BtnInputFeeder,          "wi.inputFeeder",       op, () => Whitened(new InputFeederPage()));
            RegisterSidebarButton(BtnInputStage,           "wi.inputStage",        op, () => Whitened(new InputStagePage()));
            RegisterSidebarButton(BtnFrontHead,            "wi.frontHead",         op, () => Whitened(new FrontPickerPage()));
            RegisterSidebarButton(BtnRearHead,             "wi.rearHead",          op, () => Whitened(new RearPickerPage()));
            RegisterSidebarButton(BtnOutputStage,          "wi.outputStage",       op, () => Whitened(new OutputStagePage()));
            RegisterSidebarButton(BtnOutputFeeder,         "wi.outputFeeder",      op, () => Whitened(new OutputFeederPage()));
            RegisterSidebarButton(BtnOutputCassette,       "wi.outputCassette",    op, () => Whitened(new OutputCassettePage()));
            RegisterSidebarButton(BtnState,                "wi.state",             op, () => Whitened(new StatePage()));
            RegisterSidebarButton(BtnLogic,                "wi.logic", UserLevel.Engineer, () => Whitened(new LogicDetailPage()));
        }

        /// <summary>작업정보 페이지 트리 전체를 훑어 밝은 회색 배경 컨트롤을 흰색으로 통일한다.
        /// 버튼·표시등·어두운/컬러 배경은 그대로 두고, 값/캡션 필드는 기존 테두리로 구분된다.</summary>
        private static T Whitened<T>(T page) where T : System.Windows.Forms.Control
        {
            WhitenTree(page);
            return page;
        }

        private static void WhitenTree(System.Windows.Forms.Control root)
        {
            ApplyWhite(root);
            foreach (System.Windows.Forms.Control c in root.Controls)
                WhitenTree(c);
        }

        private static void ApplyWhite(System.Windows.Forms.Control c)
        {
            var dgv = c as System.Windows.Forms.DataGridView;
            if (dgv != null)
            {
                dgv.BackgroundColor = System.Drawing.Color.White;
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.White;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(35, 45, 57);
                return;
            }

            if (c is System.Windows.Forms.Button)
                return;                                   // 버튼(빨강/파랑/회색 액션) 색 유지

            if (IsLightGrayBg(c.BackColor))
                c.BackColor = System.Drawing.Color.White;
        }

        private static bool IsLightGrayBg(System.Drawing.Color x)
        {
            if (x.A == 0)
                return false;                             // 투명
            if (x.R == 255 && x.G == 255 && x.B == 255)
                return false;                             // 이미 흰색
            // 캡션(항목) 타일 회색(≈200)은 값(흰색)과 구분되도록 원래대로 유지 → 흰색화 제외
            if (x.R >= 196 && x.R <= 206 && x.G >= 196 && x.G <= 206 && x.B >= 196 && x.B <= 206)
                return false;
            return x.R >= 188 && x.G >= 188 && x.B >= 188
                && System.Math.Abs(x.R - x.G) <= 16
                && System.Math.Abs(x.G - x.B) <= 16;      // 밝은 무채색(회색) 계열만(캡션 200 제외)
        }
    }
}
