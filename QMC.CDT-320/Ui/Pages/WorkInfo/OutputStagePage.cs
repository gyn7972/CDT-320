using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputStagePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private Timer _timer;

        public OutputStagePage()
        {
            InitializeComponent();

            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshData();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost()
        {
            return FindForm() as Form1;
        }

        private void RefreshData()
        {
            try
            {
                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.OutputStageUnit : null;
                if (stage != null && stage.GoodStage != null && stage.GoodStage.StageZ != null)
                    lblStageZValue.Text = AxisUnitConverter.FormatDisplay(stage.GoodStage.StageZ.ActualPosition, stage.GoodStage.StageZ, "0.###", true);

                WaferMaterial good = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood);
                WaferMaterial ng = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);
                lblGoodCountValue.Text = good != null ? "1 ea" : "0 ea";
                lblNgCountValue.Text = ng != null ? "1 ea" : "0 ea";

                WaferMaterial wafer = good ?? ng;
                string side = good != null ? "Good" : (ng != null ? "NG" : "-");
                string title = good != null ? "OUTPUT STAGE GOOD MATERIAL" : (ng != null ? "OUTPUT STAGE NG MATERIAL" : "OUTPUT STAGE MATERIAL");
                materialDetailView.SetRows(title, BuildStageMaterialRows(wafer, side));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (QMC.Common.MessageDialog.Show(this, "Output Stage Good 위치에 Material Data를 생성하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.OutputStageGood,
                    "OUTPUT-STAGE-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.WorkReady);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Stage Material Data 생성 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (QMC.Common.MessageDialog.Show(this, "Output Stage의 Material Data를 초기화하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.OutputStageGood);
                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.OutputStageNg);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Stage Material Data 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static IEnumerable<MaterialDetailRow> BuildStageMaterialRows(WaferMaterial wafer, string stageSide)
        {
            return new[]
            {
                Row("Unit", "Output Stage"),
                Row("Side", stageSide),
                Row("Wafer ID", wafer != null ? wafer.WaferId : ""),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "EMPTY"),
                Row("Source Cassette", wafer != null ? wafer.SourceCassetteRole.ToString() : ""),
                Row("Source Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : ""),
                Row("Current Loc", wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : ""),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : ""),
                Row("TapeFrame Spec", wafer != null ? wafer.TapeFrameSpecName : ""),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "")
            };
        }

        private static MaterialDetailRow Row(string name, string value)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = string.IsNullOrWhiteSpace(value) ? "-" : value,
                Editable = false
            };
        }
    }
}
