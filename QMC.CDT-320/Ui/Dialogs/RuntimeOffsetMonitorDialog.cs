using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Pick / Place 런타임 보정 필터(LowPassFilter)의 현재 값을 보여주고,
    /// Place 필터값을 Place 기구(메카) 오프셋으로 이관하는 창.
    ///
    /// 이관 부호(사용자 확정 2026-07-29): Place 런타임 보정은 이동식에서 X/Y 모두 감산
    ///   (pickerX = … − placeRuntimeOffsetX, outputStageY = … − placeRuntimeOffsetY)이고,
    ///   기구 오프셋은 같은 식에 가산(+ placeMechanicalOffsetX/Y)으로 들어간다.
    ///   따라서 폐루프와 같은 방향의 영구 보정으로 옮기려면 기구값 = 기구값 − 필터값이다.
    /// Pick 이관은 부호 확정 전이라 표시만 하고 버튼은 비활성으로 둔다.
    /// T 채널은 기구 오프셋에 대응 항이 없어 표시 전용이다.
    /// </summary>
    public partial class RuntimeOffsetMonitorDialog : Form
    {
        private bool _allowClose;
        private bool _busy;

        public RuntimeOffsetMonitorDialog()
        {
            InitializeComponent();
            InitializeGridColumns(gridPlace);
            InitializeGridColumns(gridPick);
            RefreshValues();
            timerRefresh.Start();
        }

        private static void InitializeGridColumns(DataGridView grid)
        {
            grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(224, 224, 224);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSide", HeaderText = "SIDE", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPicker", HeaderText = "PICKER", FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colX", HeaderText = "X (mm)", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colY", HeaderText = "Y (mm)", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colT", HeaderText = "T (deg)", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMechX", HeaderText = "기구 X (mm)", FillWeight = 95 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMechY", HeaderText = "기구 Y (mm)", FillWeight = 95 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUpdated", HeaderText = "최종 갱신", FillWeight = 150 });

            for (int i = 2; i <= 6; i++)
                grid.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        // ── Event Methods ─────────────────────────────────────

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            if (_busy)
                return;

            RefreshValues();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshValues();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            Close();
        }

        private void btnApplyPick_Click(object sender, EventArgs e)
        {
            // 부호 확정 전까지 비활성 — 실수로 눌려도 아무 동작 없이 안내만 한다.
            QMC.Common.MessageDialog.Show(
                "PICK 런타임 보정의 기구 오프셋 이관은 부호 확정 후 활성화됩니다.");
        }

        private void btnApplyPlace_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                ApplyPlaceRuntimeOffsetToMechanical();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Place 런타임 보정 이관 중 오류가 발생했습니다.\n" + ex.Message);
                EventLogger.Write(EventKind.Alarm, "UI", "RUNTIME-OFFSET-APPLY",
                    "Place 런타임 보정 기구 오프셋 이관 실패. error=" + ex.Message);
            }
            finally
            {
                _busy = false;
                RefreshValues();
            }
        }

        private void RuntimeOffsetMonitorDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 타이틀바 X는 막고 CLOSE 버튼으로만 닫는다(다른 다이얼로그와 동일 정책).
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }

            timerRefresh.Stop();
        }

        // ── Public Methods ────────────────────────────────────

        /// <summary>필터 현재값과 기구 오프셋을 다시 읽어 그리드를 갱신한다.</summary>
        public void RefreshValues()
        {
            try
            {
                FillGrid(gridPlace, PlaceRuntimeOffsetService.GetSnapshot(), true);
                FillGrid(gridPick, PickRuntimeOffsetService.GetSnapshot(), false);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "런타임 보정 값 조회 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        // ── Private Methods ───────────────────────────────────

        private void ApplyPlaceRuntimeOffsetToMechanical()
        {
            Form1 host = FindHostForm();
            if (host == null || host.Machine == null)
            {
                QMC.Common.MessageDialog.Show("장비 객체를 찾을 수 없어 적용할 수 없습니다.");
                return;
            }

            string blockReason;
            if (!CanApplyMechanicalOffset(host, out blockReason))
            {
                QMC.Common.MessageDialog.Show(blockReason);
                return;
            }

            RuntimeOffsetSnapshot[] rows = PlaceRuntimeOffsetService.GetSnapshot();
            string preview = BuildPlacePreview(host.Machine, rows);
            if (preview == null)
            {
                QMC.Common.MessageDialog.Show("이관할 Place 런타임 보정값이 없습니다(전 채널 0).");
                return;
            }

            DialogResult answer = QMC.Common.MessageDialog.Show(
                "Place 런타임 보정값을 Place 기구 오프셋으로 이관합니다.\n" +
                "적용식: 기구값 = 기구값 − 필터값 (X/Y), T는 대응 항이 없어 제외합니다.\n" +
                "이관한 채널의 필터는 0으로 초기화됩니다.\n\n" +
                preview + "\n계속하시겠습니까?",
                "PLACE RUNTIME OFFSET",
                MessageBoxButtons.YesNo);
            if (answer != DialogResult.Yes)
                return;

            ApplyPlaceRows(host, rows);
        }

        private void ApplyPlaceRows(Form1 host, RuntimeOffsetSnapshot[] rows)
        {
            CDT320_Machine machine = host.Machine;
            int appliedCount = 0;
            var clamped = new StringBuilder();

            for (int i = 0; i < rows.Length; i++)
            {
                RuntimeOffsetSnapshot row = rows[i];
                if (row == null)
                    continue;
                if (IsZero(row.X) && IsZero(row.Y))
                    continue;

                PickerPlaceMotionConfig config = ResolvePlaceConfig(machine, row.Side);
                if (config == null)
                    continue;

                int pickerIndex = row.PickerNo - 1;
                double beforeX = config.GetMechanicalOffsetX(pickerIndex);
                double beforeY = config.GetMechanicalOffsetY(pickerIndex);

                // 폐루프 적용식(pickerX = … − runtimeX, outputStageY = … − runtimeY)과
                // 같은 방향의 영구 보정이 되도록 감산 이관한다.
                double requestedX = beforeX - row.X;
                double requestedY = beforeY - row.Y;

                // SetMechanicalOffset*은 설정 한계(MechanicalOffsetLimitMm)로 자동 클램프한다.
                config.SetMechanicalOffsetX(pickerIndex, requestedX);
                config.SetMechanicalOffsetY(pickerIndex, requestedY);

                double afterX = config.GetMechanicalOffsetX(pickerIndex);
                double afterY = config.GetMechanicalOffsetY(pickerIndex);
                bool clampedX = !IsZero(afterX - requestedX);
                bool clampedY = !IsZero(afterY - requestedY);
                if (clampedX || clampedY)
                {
                    clamped.AppendLine(
                        row.Side + " P" + row.PickerNo +
                        (clampedX ? " X " + F(requestedX) + " → " + F(afterX) : string.Empty) +
                        (clampedY ? " Y " + F(requestedY) + " → " + F(afterY) : string.Empty));
                }

                EventLogger.Write(
                    EventKind.Event,
                    "COORD",
                    "PLACE-RUNTIME-OFFSET-APPLY",
                    "Place 런타임 보정을 기구 오프셋으로 이관했습니다. side=" + row.Side +
                    ", pickerNo=" + row.PickerNo +
                    ", filterX=" + F(row.X) +
                    ", filterY=" + F(row.Y) +
                    ", mechXBefore=" + F(beforeX) +
                    ", mechXRequested=" + F(requestedX) +
                    ", mechXAfter=" + F(afterX) +
                    ", mechXClamped=" + clampedX +
                    ", mechYBefore=" + F(beforeY) +
                    ", mechYRequested=" + F(requestedY) +
                    ", mechYAfter=" + F(afterY) +
                    ", mechYClamped=" + clampedY +
                    ", limitMm=" + F(config.MechanicalOffsetLimitMm));

                // 이관한 채널의 필터를 0으로 초기화한다 — 이후 Enable 시 이중 보정 방지.
                PlaceRuntimeOffsetService.ResetXy(row.Side, row.PickerNo);
                appliedCount++;
            }

            if (appliedCount == 0)
            {
                QMC.Common.MessageDialog.Show("이관 대상이 없습니다.");
                return;
            }

            host.SaveMachineSettings();
            QMC.Common.Log.Write("Main", UserSession.Name, "RuntimeOffsetApply",
                "Place 런타임 보정을 기구 오프셋으로 이관하고 설정을 저장했습니다. appliedRows=" +
                appliedCount + " - Ok");

            string message = "Place 런타임 보정을 기구 오프셋으로 이관했습니다. (" + appliedCount + "개 픽커)";
            if (clamped.Length > 0)
            {
                message += "\n\n다음 항목은 설정 한계로 클램프되었습니다:\n" + clamped;
                EventLogger.Write(EventKind.Warning, "COORD", "PLACE-RUNTIME-OFFSET-APPLY-CLAMP",
                    "Place 런타임 보정 이관 중 기구 오프셋 한계 클램프가 발생했습니다. detail=" +
                    clamped.ToString().Replace(Environment.NewLine, " / "));
            }

            QMC.Common.MessageDialog.Show(message);
        }

        private string BuildPlacePreview(CDT320_Machine machine, RuntimeOffsetSnapshot[] rows)
        {
            if (rows == null || rows.Length == 0)
                return null;

            var builder = new StringBuilder();
            bool any = false;
            for (int i = 0; i < rows.Length; i++)
            {
                RuntimeOffsetSnapshot row = rows[i];
                if (row == null)
                    continue;
                if (IsZero(row.X) && IsZero(row.Y))
                    continue;

                PickerPlaceMotionConfig config = ResolvePlaceConfig(machine, row.Side);
                if (config == null)
                    continue;

                int pickerIndex = row.PickerNo - 1;
                double currentX = config.GetMechanicalOffsetX(pickerIndex);
                double currentY = config.GetMechanicalOffsetY(pickerIndex);
                builder.AppendLine(
                    row.Side + " P" + row.PickerNo +
                    " : X " + F(currentX) + " → " + F(currentX - row.X) +
                    " / Y " + F(currentY) + " → " + F(currentY - row.Y));
                any = true;
            }

            return any ? builder.ToString() : null;
        }

        private void FillGrid(DataGridView grid, RuntimeOffsetSnapshot[] rows, bool isPlace)
        {
            if (rows == null)
                return;

            // 행 수가 같으면 값만 갱신한다(주기 갱신에서 선택/스크롤 유지).
            if (grid.Rows.Count != rows.Length)
            {
                grid.Rows.Clear();
                for (int i = 0; i < rows.Length; i++)
                    grid.Rows.Add();
            }

            for (int i = 0; i < rows.Length; i++)
            {
                RuntimeOffsetSnapshot row = rows[i];
                if (row == null)
                    continue;

                double mechX;
                double mechY;
                bool hasMech = TryResolveMechanicalOffset(row.Side, row.PickerNo, isPlace, out mechX, out mechY);

                DataGridViewRow view = grid.Rows[i];
                view.Cells[0].Value = row.Side.ToString();
                view.Cells[1].Value = "P" + row.PickerNo.ToString(CultureInfo.InvariantCulture);
                view.Cells[2].Value = F(row.X);
                view.Cells[3].Value = F(row.Y);
                view.Cells[4].Value = F(row.T);
                view.Cells[5].Value = hasMech ? F(mechX) : "-";
                view.Cells[6].Value = hasMech ? F(mechY) : "-";
                view.Cells[7].Value = row.HasSample
                    ? row.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss")
                    : "(샘플 없음)";
            }
        }

        private bool TryResolveMechanicalOffset(
            PickerSequenceSide side,
            int pickerNo,
            bool isPlace,
            out double mechX,
            out double mechY)
        {
            mechX = 0.0;
            mechY = 0.0;

            Form1 host = FindHostForm();
            CDT320_Machine machine = host != null ? host.Machine : null;
            if (machine == null)
                return false;

            int pickerIndex = pickerNo - 1;
            if (isPlace)
            {
                PickerPlaceMotionConfig placeConfig = ResolvePlaceConfig(machine, side);
                if (placeConfig == null)
                    return false;

                mechX = placeConfig.GetMechanicalOffsetX(pickerIndex);
                mechY = placeConfig.GetMechanicalOffsetY(pickerIndex);
                return true;
            }

            PickerPickUpMotionConfig pickConfig = ResolvePickUpConfig(machine, side);
            if (pickConfig == null)
                return false;

            mechX = pickConfig.GetMechanicalOffsetX(pickerIndex);
            mechY = pickConfig.GetMechanicalOffsetY(pickerIndex);
            return true;
        }

        private Form1 FindHostForm()
        {
            Form1 host = Owner as Form1;
            if (host != null)
                return host;

            foreach (Form form in Application.OpenForms)
            {
                host = form as Form1;
                if (host != null)
                    return host;
            }

            return null;
        }

        // ── Check Methods ─────────────────────────────────────

        /// <summary>기구 오프셋 변경은 운전/초기화 중 금지 — 레시피 화면과 동일 정책.</summary>
        private static bool CanApplyMechanicalOffset(Form1 host, out string reason)
        {
            reason = string.Empty;
            MachineController controller = host != null ? host.Controller : null;
            if (controller == null)
                return true;

            EquipmentStatus status = controller.Status;
            bool busy =
                status == EquipmentStatus.AutoRunning ||
                status == EquipmentStatus.ManualRunning ||
                status == EquipmentStatus.Initializing ||
                controller.IsSequenceRunning ||
                controller.IsManualBusy;
            if (!busy)
                return true;

            reason = "장비 동작 중에는 기구 오프셋을 변경할 수 없습니다. 동작을 정지한 뒤 다시 시도하십시오.";
            return false;
        }

        // ── Utility Methods ───────────────────────────────────

        private static PickerPlaceMotionConfig ResolvePlaceConfig(CDT320_Machine machine, PickerSequenceSide side)
        {
            if (side == PickerSequenceSide.Front)
            {
                return machine.PickerFrontUnit != null && machine.PickerFrontUnit.Config != null
                    ? machine.PickerFrontUnit.Config.Place
                    : null;
            }

            return machine.PickerRearUnit != null && machine.PickerRearUnit.Config != null
                ? machine.PickerRearUnit.Config.Place
                : null;
        }

        private static PickerPickUpMotionConfig ResolvePickUpConfig(CDT320_Machine machine, PickerSequenceSide side)
        {
            if (side == PickerSequenceSide.Front)
            {
                return machine.PickerFrontUnit != null && machine.PickerFrontUnit.Config != null
                    ? machine.PickerFrontUnit.Config.PickUp
                    : null;
            }

            return machine.PickerRearUnit != null && machine.PickerRearUnit.Config != null
                ? machine.PickerRearUnit.Config.PickUp
                : null;
        }

        private static bool IsZero(double value)
        {
            return Math.Abs(value) < 0.000001;
        }

        private static string F(double value)
        {
            return value.ToString("F4", CultureInfo.InvariantCulture);
        }
    }
}
