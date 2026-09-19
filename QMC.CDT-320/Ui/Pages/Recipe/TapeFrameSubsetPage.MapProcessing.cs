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
                _project = RecipeStore.Load(_project.FileName) ?? throw new InvalidDataException("현재 레시피를 다시 읽지 못했습니다.");
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
            _lastWaferStatus = "[공정 맵 설정 저장 완료] " + RecipeInputMapSource.DescribeMode(updated, AppSettingsStore.Current) +
                (updated.NextInputUseRemoteWaferMap.HasValue ? " · 현재 웨이퍼는 유지하며 다음 웨이퍼 투입부터 적용합니다." : " · 현재 레시피에 반영했습니다.");
            UpdateMapSourceInfo();
            QMC.Common.Log.Write("Main", "SYSTEM", "RecipeMapProcessing",
                "공정 맵 설정 저장 완료. recipe=" + updated.FileName + ", " + RecipeInputMapSource.DescribeMode(updated, AppSettingsStore.Current) +
                ", input=" + WaferMapProcessService.GetSettingsKey(input) + ", output=" + WaferMapProcessService.GetSettingsKey(output));
        }
    }
}
