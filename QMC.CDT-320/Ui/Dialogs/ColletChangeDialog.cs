using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.Common;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 콜렛 교체 모드: Front/Rear 픽커를 공용 X레일의 Input/Output 방향 끝(교체 위치)으로 빼고,
    /// 교체가 끝나면 Avoid로 복귀시킨다.
    /// 교체 위치(X)는 Recipe가 아니라 Config(ColletExchangeInputX / ColletExchangeOutputX)에 있어
    /// 레시피를 바꿔도 같은 위치를 사용한다. Y/T/Z는 항상 Avoid로 후퇴한다.
    /// </summary>
    public partial class ColletChangeDialog : Form
    {
        private static readonly PickerAxis[] ZAxes =
            { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
        private static readonly PickerAxis[] TAxes =
            { PickerAxis.PickerT0, PickerAxis.PickerT1, PickerAxis.PickerT2, PickerAxis.PickerT3 };

        private bool _busy;

        public ColletChangeDialog()
        {
            InitializeComponent();
            rdoFront.CheckedChanged += (s, e) => RefreshTargetText();
            rdoRear.CheckedChanged += (s, e) => RefreshTargetText();
            rdoInputSide.CheckedChanged += (s, e) => RefreshTargetText();
            rdoOutputSide.CheckedChanged += (s, e) => RefreshTargetText();
            RefreshTargetText();
        }

        private bool IsFront { get { return rdoFront.Checked; } }
        private bool IsInputSide { get { return rdoInputSide.Checked; } }

        private Form1 ResolveHost()
        {
            return (Owner as Form1) ?? (FindForm() as Form1);
        }

        /// <summary>선택한 픽커/방향의 교체 위치 X를 Config에서 읽는다. 못 읽으면 false.</summary>
        private bool TryResolveTargetX(out double targetX, out string reason)
        {
            targetX = 0.0;
            reason = string.Empty;
            try
            {
                Form1 host = ResolveHost();
                if (host == null || host.Machine == null)
                {
                    reason = "장비가 준비되지 않았습니다.";
                    return false;
                }

                if (IsFront)
                {
                    if (host.Machine.PickerFrontUnit == null || host.Machine.PickerFrontUnit.Config == null)
                    {
                        reason = "Front Picker Config를 찾을 수 없습니다.";
                        return false;
                    }
                    targetX = IsInputSide
                        ? host.Machine.PickerFrontUnit.Config.ColletExchangeInputX
                        : host.Machine.PickerFrontUnit.Config.ColletExchangeOutputX;
                }
                else
                {
                    if (host.Machine.PickerRearUnit == null || host.Machine.PickerRearUnit.Config == null)
                    {
                        reason = "Rear Picker Config를 찾을 수 없습니다.";
                        return false;
                    }
                    targetX = IsInputSide
                        ? host.Machine.PickerRearUnit.Config.ColletExchangeInputX
                        : host.Machine.PickerRearUnit.Config.ColletExchangeOutputX;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "교체 위치 조회 실패: " + ex.Message;
                return false;
            }
        }

        private void RefreshTargetText()
        {
            try
            {
                double targetX;
                string reason;
                lblTargetValue.Text = TryResolveTargetX(out targetX, out reason)
                    ? targetX.ToString("F3") + " mm"
                    : "-";
            }
            catch
            {
                lblTargetValue.Text = "-";
            }
        }

        private async void btnMove_Click(object sender, EventArgs e)
        {
            double targetX;
            string reason;
            if (!TryResolveTargetX(out targetX, out reason))
            {
                ShowWarning(reason);
                return;
            }

            if (targetX == 0.0 &&
                MessageDialog.Show(this,
                    "선택한 교체 위치 X가 0.000 mm입니다(티칭 전일 수 있음).\r\n그대로 이동할까요?",
                    "콜렛 교체", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            await RunMoveAsync("교체 위치 이동", targetX).ConfigureAwait(true);
        }

        private async void btnAvoid_Click(object sender, EventArgs e)
        {
            // Avoid 복귀는 X도 Avoid 티칭값으로 되돌린다(targetX=null 의미로 NaN 사용).
            await RunMoveAsync("AVOID 복귀", double.NaN).ConfigureAwait(true);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Y/T/Z를 Avoid로 후퇴한 뒤 X를 이동한다. targetX가 NaN이면 X도 Avoid 티칭값으로 이동한다.
        /// </summary>
        private async Task RunMoveAsync(string actionName, double targetX)
        {
            if (_busy)
                return;

            Form1 host = ResolveHost();
            if (host == null || host.Controller == null)
            {
                ShowWarning("장비가 준비되지 않았습니다.");
                return;
            }

            if (host.Controller.Status == EquipmentStatus.AutoRunning)
            {
                ShowWarning("자동 운전 중에는 콜렛 교체 위치로 이동할 수 없습니다.\r\n정지 후 다시 시도하세요.");
                return;
            }

            string pickerName = IsFront ? "FRONT" : "REAR";
            string sideName = IsInputSide ? "INPUT" : "OUTPUT";
            if (MessageDialog.Show(this,
                    pickerName + " PICKER를 " + sideName + " 쪽 " + actionName + " 하시겠습니까?\r\n" +
                    "Y/T/Z를 Avoid로 후퇴한 뒤 X가 이동합니다.",
                    "콜렛 교체", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            IDisposable actionScope = null;
            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                lblStatus.Text = actionName + " 진행 중입니다...";

                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence, "ColletChangeDialog:" + actionName);

                int result;
                // 이동 속도는 Manual Sequence 속도(%)를 쓴다. Jog Fine은 교체 이동에 너무 느리다.
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("ColletChangeDialog." + actionName))
                using (MotionSpeedScale.BeginManualSequenceScale())
                {
                    result = await MoveSelectedPickerAsync(host, targetX).ConfigureAwait(true);
                }

                lblStatus.Text = result == 0
                    ? pickerName + " PICKER " + actionName + " 완료."
                    : pickerName + " PICKER " + actionName + " 실패(result=" + result + "). Alarm/Event Log를 확인하세요.";
                EventLogger.Write(result == 0 ? EventKind.Event : EventKind.Alarm, "UI", "COLLET-CHANGE-MOVE",
                    pickerName + "/" + sideName + " " + actionName + " result=" + result +
                    ", targetX=" + (double.IsNaN(targetX) ? "AvoidPosition" : targetX.ToString("F3")));
            }
            catch (Exception ex)
            {
                lblStatus.Text = actionName + " 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "COLLET-CHANGE-MOVE-EX", lblStatus.Text);
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Alarm, "UI", "COLLET-CHANGE-CLEANUP",
                        "콜렛 교체 수동 스코프 정리 실패: " + ex.Message);
                }

                _busy = false;
                SetButtonsEnabled(true);
                RefreshTargetText();
            }
        }

        /// <summary>
        /// 선택 픽커를 Z 전체 Avoid → Y Avoid → T 전체 Avoid → X 목표 순으로 이동한다.
        /// 속도는 축 Default 속도 경로를 쓰며, 호출부의 BeginManualSequenceScale이 Manual Sequence %를 적용한다.
        /// </summary>
        private async Task<int> MoveSelectedPickerAsync(Form1 host, double targetX)
        {
            if (IsFront)
            {
                PickerFrontUnit unit = host.Machine.PickerFrontUnit;
                if (unit == null)
                    return -1;

                foreach (PickerAxis axis in ZAxes)
                {
                    int r = await unit.MovePickerAxisToTeachingPosition(axis, "AvoidPosition").ConfigureAwait(true);
                    if (r != 0)
                        return r;
                }

                int y = await unit.MovePickerAxisToTeachingPosition(PickerAxis.PickerY, "AvoidPosition").ConfigureAwait(true);
                if (y != 0)
                    return y;

                foreach (PickerAxis axis in TAxes)
                {
                    int r = await unit.MovePickerAxisToTeachingPosition(axis, "AvoidPosition").ConfigureAwait(true);
                    if (r != 0)
                        return r;
                }

                return double.IsNaN(targetX)
                    ? await unit.MovePickerAxisToTeachingPosition(PickerAxis.PickerX, "AvoidPosition").ConfigureAwait(true)
                    : await unit.MovePickerAxis(PickerAxis.PickerX, targetX, false, "ColletExchangePosition").ConfigureAwait(true);
            }

            PickerRearUnit rear = host.Machine.PickerRearUnit;
            if (rear == null)
                return -1;

            foreach (PickerAxis axis in ZAxes)
            {
                int r = await rear.MovePickerAxisToTeachingPosition(axis, "AvoidPosition").ConfigureAwait(true);
                if (r != 0)
                    return r;
            }

            int rearY = await rear.MovePickerAxisToTeachingPosition(PickerAxis.PickerY, "AvoidPosition").ConfigureAwait(true);
            if (rearY != 0)
                return rearY;

            foreach (PickerAxis axis in TAxes)
            {
                int r = await rear.MovePickerAxisToTeachingPosition(axis, "AvoidPosition").ConfigureAwait(true);
                if (r != 0)
                    return r;
            }

            return double.IsNaN(targetX)
                ? await rear.MovePickerAxisToTeachingPosition(PickerAxis.PickerX, "AvoidPosition").ConfigureAwait(true)
                : await rear.MovePickerAxis(PickerAxis.PickerX, targetX, false, "ColletExchangePosition").ConfigureAwait(true);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnMove.Enabled = enabled;
            btnAvoid.Enabled = enabled;
            btnClose.Enabled = enabled;
            groupSelect.Enabled = enabled;
        }

        private void ShowWarning(string message)
        {
            lblStatus.Text = message;
            MessageDialog.Show(this, message, "콜렛 교체", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
