using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    public class ColletChangeDialog : ModeOverlayDialog
    {
        public ColletChangeDialog() : base("dlg.colletChange")
        {
            AddAction("#1 COLLET CHANGE", width: 160);
            AddAction("#2 COLLET CHANGE", width: 160);
            AddAction(Lang.T("common.complete"), width: 160).Tag = "i18n:common.complete";
        }
    }

    public class ColletCleaningDialog : ModeOverlayDialog
    {
        public ColletCleaningDialog() : base("dlg.colletCleaning")
        {
            AddAction("START", width: 160);
            AddAction("COMPLETE", width: 160);
        }
    }

    public class NeedleChangeDialog : ModeOverlayDialog
    {
        public NeedleChangeDialog() : base("dlg.needleChange")
        {
            AddAction("START", width: 180);
            AddAction("COMPLETE", width: 180);
        }
    }

    public class PickFailDialog : ModeOverlayDialog
    {
        public PickFailDialog() : base("dlg.pickFail")
        {
            AddAction("RETRY", width: 140);
            AddAction("CONTINUE", width: 140);
            AddAction("STOP", width: 140);
        }
    }

    public class PlaceFailDialog : ModeOverlayDialog
    {
        public PlaceFailDialog() : base("dlg.placeFail")
        {
            AddAction("RETRY", width: 140);
            AddAction("CONTINUE", width: 140);
            AddAction("STOP", width: 140);
        }
    }

    public class BarcodeConfirmDialog : ModeOverlayDialog
    {
        public BarcodeConfirmDialog() : base("dlg.barcodeConfirm")
        {
            AddAction("CONTINUE", width: 160);
            AddAction("STOP", width: 120);
            AddAction("BUZZER OFF", width: 140);
        }
    }

    public class VisionAlignFailDialog : ModeOverlayDialog
    {
        public VisionAlignFailDialog() : base("dlg.visionAlignFail")
        {
            AddAction("RETRY", width: 140);
            AddAction("CONTINUE", width: 140);
            AddAction("STOP", width: 140);
        }
    }

    public class AlignMatchFailDialog : ModeOverlayDialog
    {
        public AlignMatchFailDialog() : base("dlg.alignMatchFail")
        {
            AddAction("RETRY", width: 140);
            AddAction("SKIP", width: 120);
            AddAction("STOP", width: 120);
        }
    }

    public class AlignConfirmDialog : ModeOverlayDialog
    {
        public AlignConfirmDialog() : base("dlg.alignConfirm")
        {
            AddAction("CONTINUE", width: 160);
            AddAction("STOP", width: 120);
        }
    }

    public class AutoPositionDialog : ModeOverlayDialog
    {
        public AutoPositionDialog() : base("dlg.autoPos")
        {
            AddAction("START", width: 180);
            AddAction("COMPLETE", width: 180);
        }
    }

    public class PositionCheckDialog : ModeOverlayDialog
    {
        public PositionCheckDialog() : base("dlg.posCheck")
        {
            AddAction("CHECK", width: 180);
            AddAction("COMPLETE", width: 180);
        }
    }

    public class SelfInspectionDialog : ModeOverlayDialog
    {
        public SelfInspectionDialog() : base("dlg.selfInspection")
        {
            AddAction("START", width: 180);
            AddAction("COMPLETE", width: 180);
        }
    }

    public class CcsInspectionDialog : ModeOverlayDialog
    {
        public CcsInspectionDialog() : base("dlg.ccsInspection")
        {
            AddAction("OK", width: 140);
            AddAction("NG", width: 140);
            AddAction("STOP", width: 140);
        }
    }

    public partial class LotIdInputDialog : Form
    {
        public string LotId { get; private set; }

        public LotIdInputDialog()
        {
            InitializeComponent();
            Text = Lang.T("dlg.lotIdInput");
            lblTitle.Text = Text;
            btnCancel.Text = Lang.T("common.cancel");
            btnOk.Click += (s, e) => { LotId = tbLotId.Text.Trim(); DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Load += (s, e) => { Lang.Apply(this); tbLotId.Focus(); };
        }
    }
}
