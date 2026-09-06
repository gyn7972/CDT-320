using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    internal static class RecipePickerFlowRecovery
    {
        internal static IoCylinderItem CreateItem(
            Control owner, MaterialLocationKind location, int pickerNo, Func<bool> stateGetter)
        {
            var item = IoCylinderItem.Input("P" + pickerNo + " FLOW", stateGetter);
            item.InputOverrideGetter = () =>
            {
                var host = owner.FindForm() as Form1;
                return host != null && host.Controller != null &&
                    MaterialStateService.IsPickerFlowRecoveryActive(
                        host.Machine, host.Controller.GlobalDryRun, location, pickerNo);
            };
            item.InputDoubleClickCommand = () => RequestRecoveryAsync(owner, location, pickerNo);
            return item;
        }

        private static Task<int> RequestRecoveryAsync(Control owner, MaterialLocationKind location, int pickerNo)
        {
            try
            {
                var host = owner.FindForm() as Form1;
                if (host == null || host.Controller == null)
                {
                    MessageDialog.Show(owner, "장비 제어 정보를 확인할 수 없습니다.", "FLOW 강제 ON",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Task.FromResult(-1);
                }

                MaterialStateService.PickerFlowRecoveryRequest request;
                string reason;
                if (!MaterialStateService.TryPreparePickerFlowRecovery(
                    host.Machine, host.Controller.GlobalDryRun, location, pickerNo, out request, out reason))
                {
                    MessageDialog.Show(owner, reason, "FLOW 강제 ON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Task.FromResult(-1);
                }

                string sideText = location == MaterialLocationKind.PickerFront ? "Front" : "Rear";
                string message = sideText + " P" + pickerNo + " FLOW 신호를 강제로 살리겠습니까?\r\n\r\n" +
                    "현재 다이: " + request.DieId + "\r\n" +
                    "실제 다이를 보유한 것을 확인한 경우에만 ‘예’를 선택해 주세요.\r\n" +
                    "현재 다이의 Place 완료 후 강제 ON은 자동 해제됩니다.\r\n" +
                    "이미 알람으로 정지한 경우에는 적용 후 RESET ALARM → START로 재개해 주세요.";
                if (MessageDialog.Show(owner, message, "FLOW 강제 ON",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return Task.FromResult(0);

                // 확인창 동안 권한/모드/센서/현재 다이가 바뀔 수 있으므로 승인 직전에 다시 검증한다.
                if (!MaterialStateService.TryApprovePickerFlowRecovery(
                    request, host.Machine, host.Controller.GlobalDryRun, out reason))
                {
                    MessageDialog.Show(owner, reason, "FLOW 강제 ON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return Task.FromResult(-1);
                }
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                Log.Write("Main", QMC.CDT_320.Ui.Security.UserSession.Name, "PickerFlowRecovery",
                    "FLOW 강제 ON 요청 중 오류가 발생했습니다. picker=" + location + "/P" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
                MessageDialog.Show(owner, "FLOW 강제 ON 요청을 완료하지 못했습니다.\r\n" + ex.Message,
                    "FLOW 강제 ON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Task.FromResult(-1);
            }
        }
    }
}
