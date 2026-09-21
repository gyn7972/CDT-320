using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    // ColletChangeDialog는 동작 없는 오버레이 껍데기였다(#1/#2/COMPLETE 버튼만).
    // 실제 교체 위치 이동을 수행하는 폼(Ui\Dialogs\ColletChangeDialog.cs)으로 교체했다.

    public class ColletCleaningDialog : ModeOverlayDialog
    {
        public ColletCleaningDialog() : base("dlg.colletCleaning")
        {
            SetTitle("work.colletCleanMode");
            UseCompactCommandLayout(actionColumns: 1);
            AddAction("START", width: 160, displayKey: "dialog.action.start");
            AddAction("COMPLETE", width: 160, displayKey: "dialog.action.complete");
        }
    }

    public class NeedleChangeDialog : ModeOverlayDialog
    {
        public NeedleChangeDialog() : base("dlg.needleChange")
        {
            SetTitle("work.needleMode");
            UseCompactCommandLayout(actionColumns: 1);
            AddAction("START", width: 180, displayKey: "dialog.action.start");
            AddAction("COMPLETE", width: 180, displayKey: "dialog.action.complete");
        }
    }

    public class PickFailDialog : ModeOverlayDialog
    {
        public PickFailDialog() : base("dlg.pickFail")
        {
            AddAction("RETRY", width: 140, displayKey: "dialog.action.retry");
            AddAction("CONTINUE", width: 140, displayKey: "dialog.action.continue");
            AddAction("STOP", width: 140, displayKey: "dialog.action.stop");
        }
    }

    public class PlaceFailDialog : ModeOverlayDialog
    {
        public PlaceFailDialog() : base("dlg.placeFail")
        {
            AddAction("RETRY", width: 140, displayKey: "dialog.action.retry");
            AddAction("CONTINUE", width: 140, displayKey: "dialog.action.continue");
            AddAction("STOP", width: 140, displayKey: "dialog.action.stop");
        }
    }

    public class BarcodeConfirmDialog : ModeOverlayDialog
    {
        public BarcodeConfirmDialog() : base("dlg.barcodeConfirm")
        {
            AddAction("CONTINUE", width: 160, displayKey: "dialog.action.continue");
            AddAction("STOP", width: 120, displayKey: "dialog.action.stop");
            AddAction("BUZZER OFF", width: 140, displayKey: "dialog.action.buzzerOff");
        }
    }

    public class VisionAlignFailDialog : ModeOverlayDialog
    {
        public VisionAlignFailDialog() : base("dlg.visionAlignFail")
        {
            AddAction("RETRY", width: 140, displayKey: "dialog.action.retry");
            AddAction("CONTINUE", width: 140, displayKey: "dialog.action.continue");
            AddAction("STOP", width: 140, displayKey: "dialog.action.stop");
        }
    }

    public class AlignMatchFailDialog : ModeOverlayDialog
    {
        public AlignMatchFailDialog() : base("dlg.alignMatchFail")
        {
            AddAction("RETRY", width: 140, displayKey: "dialog.action.retry");
            AddAction("SKIP", width: 120, displayKey: "dialog.action.skip");
            AddAction("STOP", width: 120, displayKey: "dialog.action.stop");
        }
    }

    public class AlignConfirmDialog : ModeOverlayDialog
    {
        public AlignConfirmDialog() : base("dlg.alignConfirm")
        {
            AddAction("CONTINUE", width: 160, displayKey: "dialog.action.continue");
            AddAction("STOP", width: 120, displayKey: "dialog.action.stop");
        }
    }

    public class AutoPositionDialog : ModeOverlayDialog
    {
        public AutoPositionDialog() : base("dlg.autoPos")
        {
            SetTitle("work.autoPosMode");
            UseCompactCommandLayout(actionColumns: 1);
            AddAction("START", width: 180, displayKey: "dialog.action.start");
            AddAction("COMPLETE", width: 180, displayKey: "dialog.action.complete");
        }
    }

    public class PositionCheckDialog : ModeOverlayDialog
    {
        public PositionCheckDialog() : this("work.posCheck")
        {
        }

        public PositionCheckDialog(string titleI18n) : base("dlg.posCheck")
        {
            SetTitle(titleI18n);
            UseCompactCommandLayout(actionColumns: 1);
            AddAction("CHECK", width: 180, displayKey: "dialog.action.check");
            AddAction("COMPLETE", width: 180, displayKey: "dialog.action.complete");
        }
    }

    public class SelfInspectionDialog : ModeOverlayDialog
    {
        public SelfInspectionDialog() : base("dlg.selfInspection")
        {
            SetTitle("work.selfCheckMode");
            UseCompactCommandLayout(actionColumns: 1);
            AddAction("START", width: 180, displayKey: "dialog.action.start");
            AddAction("COMPLETE", width: 180, displayKey: "dialog.action.complete");
        }
    }

    public class CcsInspectionDialog : ModeOverlayDialog
    {
        public CcsInspectionDialog() : base("dlg.ccsInspection")
        {
            // 검사 결과 토큰 OK는 유지하고, 확인 버튼과 구분되는 결과 표시명을 사용한다.
            AddAction("OK", width: 140, displayKey: "inspection.good");
            AddAction("NG", width: 140, displayKey: "dialog.action.bad");
            AddAction("STOP", width: 140, displayKey: "dialog.action.stop");
        }
    }

    public partial class LotIdInputDialog : Form
    {
        public string LotId { get; private set; }

        public LotIdInputDialog()
        {
            InitializeComponent();
            Lang.BindKey(this, "dlg.lotIdInput");
            Lang.BindKey(lblTitle, "dlg.lotIdInput");
            Lang.BindKey(btnCancel, "common.cancel");
            Lang.BindKey(btnOk, "common.ok");
            btnOk.Click += (s, e) => { LotId = tbLotId.Text.Trim(); DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Load += (s, e) => { Lang.Apply(this); tbLotId.Focus(); };
        }
    }
}
