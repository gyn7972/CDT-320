using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>콜렛 교체 — #1/#2 콜렛 교체 + 완료.</summary>
    public class ColletChangeDialog : ModeOverlayDialog
    {
        public ColletChangeDialog() : base("dlg.colletChange")
        {
            AddAction("#1 콜렛 교체", width: 160);
            AddAction("#2 콜렛 교체", width: 160);
            AddAction(Lang.T("common.complete"), width: 160).Tag = "i18n:common.complete";
        }
    }

    /// <summary>콜렛 클리닝 — 시작/완료.</summary>
    public class ColletCleaningDialog : ModeOverlayDialog
    {
        public ColletCleaningDialog() : base("dlg.colletCleaning")
        {
            AddAction("시작",   width: 160);
            AddAction("완료",   width: 160);
        }
    }

    /// <summary>니들 유닛 교체 — 시작/완료.</summary>
    public class NeedleChangeDialog : ModeOverlayDialog
    {
        public NeedleChangeDialog() : base("dlg.needleChange")
        {
            AddAction("시작", width: 180);
            AddAction("완료", width: 180);
        }
    }

    /// <summary>PICK 실패 — Retry/Continue/Stop.</summary>
    public class PickFailDialog : ModeOverlayDialog
    {
        public PickFailDialog() : base("dlg.pickFail")
        {
            AddAction("RETRY",    width: 140);
            AddAction("CONTINUE", width: 140);
            AddAction("STOP",     width: 140);
        }
    }

    /// <summary>PLACE 실패 — Retry/Continue/Stop.</summary>
    public class PlaceFailDialog : ModeOverlayDialog
    {
        public PlaceFailDialog() : base("dlg.placeFail")
        {
            AddAction("RETRY",    width: 140);
            AddAction("CONTINUE", width: 140);
            AddAction("STOP",     width: 140);
        }
    }

    /// <summary>바코드 확인 요청 — CONTINUE/STOP/부저 OFF.</summary>
    public class BarcodeConfirmDialog : ModeOverlayDialog
    {
        public BarcodeConfirmDialog() : base("dlg.barcodeConfirm")
        {
            AddAction("CONTINUE", width: 160);
            AddAction("STOP",     width: 120);
            AddAction("부저 OFF", width: 140);
        }
    }

    /// <summary>비전 얼라인 실패.</summary>
    public class VisionAlignFailDialog : ModeOverlayDialog
    {
        public VisionAlignFailDialog() : base("dlg.visionAlignFail")
        {
            AddAction("RETRY",    width: 140);
            AddAction("CONTINUE", width: 140);
            AddAction("STOP",     width: 140);
        }
    }

    /// <summary>얼라인 매칭 실패.</summary>
    public class AlignMatchFailDialog : ModeOverlayDialog
    {
        public AlignMatchFailDialog() : base("dlg.alignMatchFail")
        {
            AddAction("RETRY",    width: 140);
            AddAction("SKIP",     width: 120);
            AddAction("STOP",     width: 120);
        }
    }

    /// <summary>얼라인 확인 요청.</summary>
    public class AlignConfirmDialog : ModeOverlayDialog
    {
        public AlignConfirmDialog() : base("dlg.alignConfirm")
        {
            AddAction("CONTINUE", width: 160);
            AddAction("STOP",     width: 120);
        }
    }

    /// <summary>자동셋팅 위치.</summary>
    public class AutoPositionDialog : ModeOverlayDialog
    {
        public AutoPositionDialog() : base("dlg.autoPos")
        {
            AddAction("시작", width: 180);
            AddAction("완료", width: 180);
        }
    }

    /// <summary>POSITION CHECK.</summary>
    public class PositionCheckDialog : ModeOverlayDialog
    {
        public PositionCheckDialog() : base("dlg.posCheck")
        {
            AddAction("확인", width: 180);
            AddAction("완료", width: 180);
        }
    }

    /// <summary>자주 검사.</summary>
    public class SelfInspectionDialog : ModeOverlayDialog
    {
        public SelfInspectionDialog() : base("dlg.selfInspection")
        {
            AddAction("시작", width: 180);
            AddAction("완료", width: 180);
        }
    }

    /// <summary>CCS INSPECTION.</summary>
    public class CcsInspectionDialog : ModeOverlayDialog
    {
        public CcsInspectionDialog() : base("dlg.ccsInspection")
        {
            AddAction("OK",   width: 140);
            AddAction("NG",   width: 140);
            AddAction("STOP", width: 140);
        }
    }

    /// <summary>LOT ID 입력 — 오버레이가 아닌 입력 다이얼로그.</summary>
    public class LotIdInputDialog : Form
    {
        public string LotId { get; private set; }

        public LotIdInputDialog()
        {
            Text            = Lang.T("dlg.lotIdInput");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = MaximizeBox = false;
            ClientSize      = new Size(460, 220);
            BackColor       = Color.FromArgb(0xBB, 0xBB, 0xBB);

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 40,
                Text = Lang.T("dlg.lotIdInput"), Tag = "i18n:dlg.lotIdInput",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);

            var lbl = new Label   { Location = new Point(30, 70),  Size = new Size(80, 24), Text = "LOT ID", Font = UiTheme.ButtonFont };
            var tb  = new TextBox { Location = new Point(120, 66), Size = new Size(300, 28), Font = new Font("Consolas", 11F) };
            var ok  = new Button  { Location = new Point(120, 120),Size = new Size(140, 40), Text = "OK",     FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var cc  = new Button  { Location = new Point(280, 120),Size = new Size(140, 40), Text = Lang.T("common.cancel"), Tag = "i18n:common.cancel", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            ok.Click += (s, e) => { LotId = tb.Text.Trim(); DialogResult = DialogResult.OK; Close(); };
            cc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(lbl); Controls.Add(tb); Controls.Add(ok); Controls.Add(cc);

            AcceptButton = ok;
            CancelButton = cc;
            Load += (s, e) => { Lang.Apply(this); tb.Focus(); };
        }
    }
}
