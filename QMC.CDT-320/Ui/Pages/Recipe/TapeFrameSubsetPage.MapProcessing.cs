using System;
using System.IO;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class TapeFrameSubsetPage
    {
        private void btnProcessMapSettings_Click(object sender, EventArgs e)
        {
            try
            {
                if (_project == null) throw new InvalidOperationException("현재 레시피를 먼저 불러오세요.");
                using (var dialog = new WaferMapProcessSettingsDialog(_project, SaveAndApplyProcessMapSettings))
                    dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "레시피 맵 설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAndApplyProcessMapSettings(WaferMapProcessSettings input, WaferMapProcessSettings output,
            WaferMapProcessSettingsDialog editor)
        {
            var host = FindForm() as Form1;
            if (host == null || host.Controller == null || _project == null)
                throw new InvalidOperationException("현재 장비와 활성 레시피를 확인할 수 없습니다.");
            string reason;
            RecipeProject updated;
            if (!host.TrySaveAndApplyWaferMapProcessing(_project, input, output, editor, out updated, out reason))
                throw new InvalidOperationException(reason);
            _project = updated;
            _lastWaferStatus = "[공정 맵 설정 저장 완료] 현재 레시피에 반영했습니다. 입력 사용 모드는 레시피에 저장되며, 원격 다운로드만 지정한 형식으로 검사합니다. 입력/출력 등록 맵은 구분자와 관계없이 사용합니다.";
            UpdateMapSourceInfo();
            QMC.Common.Log.Write("Main", "SYSTEM", "RecipeMapProcessing",
                "공정 맵 설정 저장 및 활성 적용 완료. recipe=" + updated.FileName +
                ", input=" + WaferMapProcessService.GetSettingsKey(input) + ", output=" + WaferMapProcessService.GetSettingsKey(output));
        }
    }
}
