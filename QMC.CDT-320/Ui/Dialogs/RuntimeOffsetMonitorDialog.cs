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
    /// 필터값을 각 기구(메카) 오프셋으로 이관하거나 전체 리셋하는 창.
    ///
    /// Place 이관 부호(사용자 확정 2026-07-29): Place 런타임 보정은 이동식에서 X/Y 모두 감산
    ///   (pickerX = … − placeRuntimeOffsetX, outputStageY = … − placeRuntimeOffsetY)이고,
    ///   기구 오프셋은 같은 식에 가산(+ placeMechanicalOffsetX/Y)으로 들어간다.
    ///   따라서 폐루프와 같은 방향의 영구 보정으로 옮기려면 기구값 = 기구값 − 필터값이다.
    /// Pick 이관(2026-08-14 활성화): 런타임 X와 기구 X는 같은 진입점(PickerX/NeedleX)에
    ///   같은 부호(+)로 들어가므로 기구X′ = 기구X + 필터X 로 총합 불변 이관이 확정된다.
    ///   Y는 런타임이 StageY(+), 기구가 PickerY(+)로 서로 다른 축 — 실장비 이관 검증(2026-08-16)에서
    ///   두 축의 물리 + 방향이 같음이 확인되어 기구Y′ = 기구Y − 필터Y 로 확정.
    ///   근거·재검증 조건은 PickApplyYSign 주석 참조.
    /// T 기구 보정(2026-08-16 신설, 팀장님 지시): 기구T는 PickerT에 가산(+).
    ///   Pick: 런타임 T도 가산(2026-08-18 실장비 발산 확인으로 감산→가산 정정, 기구 적용은
    ///     가산 유지 확정) — 같은 방향이라 기구T′ = 기구T + 필터T 로 이관.
    ///   Place: 런타임 T는 감산(2026-08-19 01:26 실장비 재확정 — 가산 적용 시 bin각·필터 동반
    ///     램프로 반증됨, s_bin=+1) — 기구T′ = 기구T − 필터T 로 이관. Pick(가산)과 반대이니 주의.
    /// 이관·리셋은 운전/초기화 중 금지(CanApplyMechanicalOffset), 필터 설정 변경과 달리 게이트 유지.
    /// </summary>
    public partial class RuntimeOffsetMonitorDialog : Form
    {
        // Pick 이관 Y 부호 — 확정 -1 (2026-08-16 실장비 이관 검증, 팀장님 승인).
        // 근거: 런타임 Y는 InputStage Y축을 +로 움직여 보정하고, 기구 Y는 Picker Y축을 +로 움직인다.
        // 잠정 +1로 전 픽커 이관(+0.146~0.165)한 결과 Bottom 측정 Y가 전 픽커 약 2배(+0.26~0.30)
        // 점프(Front 4픽커·Rear 동일 방향) — 두 축의 물리 + 방향이 같아 스테이지 대신 픽커로 옮길 때는
        // 부호를 뒤집어야 상대 정렬이 유지된다. (대조: X는 런타임·기구가 같은 축(PickerX/NeedleX)·같은
        // 부호라 +1 이관이 정확히 상쇄됨 — 이관 후 필터 X 재학습값 ≈ 0으로 확인.)
        // 축 구성이나 좌표 산식(CalculatePickTarget/ApplyPickMechanicalOffsets)이 바뀌면 재검증할 것 —
        // 이관식 전 지점이 이 상수를 곱하므로 반전은 이 상수 한 곳만 바꾸면 된다. Front/Rear 공통.
        private const double PickApplyYSign = -1.0;

        private bool _allowClose;
        private bool _busy;

        public RuntimeOffsetMonitorDialog()
        {
            InitializeComponent();
            InitializeGridColumns(gridPlace);
            InitializeGridColumns(gridPick);
            InitializePickerZGridColumns(gridPickerZ);
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
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMechT", HeaderText = "기구 T (deg)", FillWeight = 95 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUpdated", HeaderText = "최종 갱신", FillWeight = 150 });

            for (int i = 2; i <= 7; i++)
                grid.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        // PICKER Z는 단채널(Z)이고 기구 이관 대상이 아니라 표시 열이 다르다(표시 전용).
        private static void InitializePickerZGridColumns(DataGridView grid)
        {
            grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(224, 224, 224);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSide", HeaderText = "SIDE", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPicker", HeaderText = "PICKER", FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colZ", HeaderText = "Z (mm)", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUpdated", HeaderText = "최종 갱신", FillWeight = 150 });

            grid.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
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
            if (_busy)
                return;

            try
            {
                _busy = true;
                ApplyPickRuntimeOffsetToMechanical();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Pick 런타임 보정 이관 중 오류가 발생했습니다.\n" + ex.Message);
                EventLogger.Write(EventKind.Alarm, "UI", "RUNTIME-OFFSET-APPLY",
                    "Pick 런타임 보정 기구 오프셋 이관 실패. error=" + ex.Message);
            }
            finally
            {
                _busy = false;
                RefreshValues();
            }
        }

        private void btnResetPick_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                ResetRuntimeOffsetsWithConfirm(FindHostForm(), false);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Pick 런타임 보정 리셋 중 오류가 발생했습니다.\n" + ex.Message);
                EventLogger.Write(EventKind.Alarm, "UI", "RUNTIME-OFFSET-RESET",
                    "Pick 런타임 보정 리셋 실패. error=" + ex.Message);
            }
            finally
            {
                _busy = false;
                RefreshValues();
            }
        }

        private void btnResetPlace_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                ResetRuntimeOffsetsWithConfirm(FindHostForm(), true);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("Place 런타임 보정 리셋 중 오류가 발생했습니다.\n" + ex.Message);
                EventLogger.Write(EventKind.Alarm, "UI", "RUNTIME-OFFSET-RESET",
                    "Place 런타임 보정 리셋 실패. error=" + ex.Message);
            }
            finally
            {
                _busy = false;
                RefreshValues();
            }
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
                FillPickerZGrid(gridPickerZ, PickerZRuntimeOffsetService.GetSnapshot());
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
                "적용식: 기구값 = 기구값 − 필터값 (X/Y/T).\n" +
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
                if (IsZero(row.X) && IsZero(row.Y) && IsZero(row.T))
                    continue;

                PickerPlaceMotionConfig config = ResolvePlaceConfig(machine, row.Side);
                if (config == null)
                    continue;

                int pickerIndex = row.PickerNo - 1;
                double beforeX = config.GetMechanicalOffsetX(pickerIndex);
                double beforeY = config.GetMechanicalOffsetY(pickerIndex);
                double beforeT = config.GetMechanicalOffsetT(pickerIndex);

                // 폐루프 적용식(pickerX = … − runtimeX, outputStageY = … − runtimeY,
                // pickerT = … − runtimeT)과 같은 방향의 영구 보정이 되도록 전 채널 감산 이관한다.
                // (T 가산 이관은 2026-08-19 01:26 실장비 반증으로 철회 — s_bin=+1, 감산 재확정.)
                double requestedX = beforeX - row.X;
                double requestedY = beforeY - row.Y;
                double requestedT = beforeT - row.T;

                // SetMechanicalOffset*은 설정 한계(X/Y: LimitMm, T: LimitTDeg)로 자동 클램프한다.
                config.SetMechanicalOffsetX(pickerIndex, requestedX);
                config.SetMechanicalOffsetY(pickerIndex, requestedY);
                config.SetMechanicalOffsetT(pickerIndex, requestedT);

                double afterX = config.GetMechanicalOffsetX(pickerIndex);
                double afterY = config.GetMechanicalOffsetY(pickerIndex);
                double afterT = config.GetMechanicalOffsetT(pickerIndex);
                bool clampedX = !IsZero(afterX - requestedX);
                bool clampedY = !IsZero(afterY - requestedY);
                bool clampedT = !IsZero(afterT - requestedT);
                if (clampedX || clampedY || clampedT)
                {
                    clamped.AppendLine(
                        row.Side + " P" + row.PickerNo +
                        (clampedX ? " X " + F(requestedX) + " → " + F(afterX) : string.Empty) +
                        (clampedY ? " Y " + F(requestedY) + " → " + F(afterY) : string.Empty) +
                        (clampedT ? " T " + F(requestedT) + " → " + F(afterT) : string.Empty));
                }

                EventLogger.Write(
                    EventKind.Event,
                    "COORD",
                    "PLACE-RUNTIME-OFFSET-APPLY",
                    "Place 런타임 보정을 기구 오프셋으로 이관했습니다. side=" + row.Side +
                    ", pickerNo=" + row.PickerNo +
                    ", filterX=" + F(row.X) +
                    ", filterY=" + F(row.Y) +
                    ", filterT=" + F(row.T) +
                    ", mechXBefore=" + F(beforeX) +
                    ", mechXRequested=" + F(requestedX) +
                    ", mechXAfter=" + F(afterX) +
                    ", mechXClamped=" + clampedX +
                    ", mechYBefore=" + F(beforeY) +
                    ", mechYRequested=" + F(requestedY) +
                    ", mechYAfter=" + F(afterY) +
                    ", mechYClamped=" + clampedY +
                    ", mechTBefore=" + F(beforeT) +
                    ", mechTRequested=" + F(requestedT) +
                    ", mechTAfter=" + F(afterT) +
                    ", mechTClamped=" + clampedT +
                    ", limitMm=" + F(config.MechanicalOffsetLimitMm) +
                    ", limitTDeg=" + F(config.MechanicalOffsetTLimitDeg));

                // 이관한 채널(X/Y/T)의 필터를 0으로 초기화한다 — 이후 이중 보정 방지.
                PlaceRuntimeOffsetService.Reset(row.Side, row.PickerNo);
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
                if (IsZero(row.X) && IsZero(row.Y) && IsZero(row.T))
                    continue;

                PickerPlaceMotionConfig config = ResolvePlaceConfig(machine, row.Side);
                if (config == null)
                    continue;

                int pickerIndex = row.PickerNo - 1;
                double currentX = config.GetMechanicalOffsetX(pickerIndex);
                double currentY = config.GetMechanicalOffsetY(pickerIndex);
                double currentT = config.GetMechanicalOffsetT(pickerIndex);
                builder.AppendLine(
                    row.Side + " P" + row.PickerNo +
                    " : X " + F(currentX) + " → " + F(currentX - row.X) +
                    " / Y " + F(currentY) + " → " + F(currentY - row.Y) +
                    // Place T는 감산 이관(2026-08-19 01:26 실장비 재확정) — ApplyPlaceRows와 동일 산식.
                    " / T " + F(currentT) + " → " + F(currentT - row.T));
                any = true;
            }

            return any ? builder.ToString() : null;
        }

        private void ApplyPickRuntimeOffsetToMechanical()
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

            RuntimeOffsetSnapshot[] rows = PickRuntimeOffsetService.GetSnapshot();
            string preview = BuildPickPreview(host.Machine, rows);
            if (preview == null)
            {
                QMC.Common.MessageDialog.Show("이관할 Pick 런타임 보정값이 없습니다(전 채널 0).");
                return;
            }

            DialogResult answer = QMC.Common.MessageDialog.Show(
                "Pick 런타임 보정값을 Pick 기구 오프셋으로 이관합니다.\n" +
                "적용식: 기구X = 기구X + 필터X, 기구Y = 기구Y " + (PickApplyYSign >= 0.0 ? "+" : "−") +
                " 필터Y, 기구T = 기구T + 필터T.\n" +
                "이관한 채널의 필터는 0으로 초기화됩니다.\n\n" +
                preview + "\n계속하시겠습니까?",
                "PICK RUNTIME OFFSET",
                MessageBoxButtons.YesNo);
            if (answer != DialogResult.Yes)
                return;

            ApplyPickRows(host, rows);
        }

        private void ApplyPickRows(Form1 host, RuntimeOffsetSnapshot[] rows)
        {
            CDT320_Machine machine = host.Machine;
            int appliedCount = 0;
            var clamped = new StringBuilder();

            for (int i = 0; i < rows.Length; i++)
            {
                RuntimeOffsetSnapshot row = rows[i];
                if (row == null)
                    continue;
                if (IsZero(row.X) && IsZero(row.Y) && IsZero(row.T))
                    continue;

                PickerPickUpMotionConfig config = ResolvePickUpConfig(machine, row.Side);
                if (config == null)
                    continue;

                int pickerIndex = row.PickerNo - 1;
                double beforeX = config.GetMechanicalOffsetX(pickerIndex);
                double beforeY = config.GetMechanicalOffsetY(pickerIndex);
                double beforeT = config.GetMechanicalOffsetT(pickerIndex);

                // 런타임 X와 기구 X는 같은 진입점(PickerX/NeedleX)에 같은 부호(+)로 들어가므로
                // 총합 불변 이관은 가산이다. Y는 축이 달라 부호 파라미터(PickApplyYSign)를 곱한다.
                // T는 런타임·기구 모두 PickerT 가산(2026-08-18 런타임 감산→가산 정정)이라 가산 이관.
                double requestedX = beforeX + row.X;
                double requestedY = beforeY + PickApplyYSign * row.Y;
                double requestedT = beforeT + row.T;

                // SetMechanicalOffset*은 설정 한계(X/Y: LimitMm, T: LimitTDeg)로 자동 클램프한다.
                config.SetMechanicalOffsetX(pickerIndex, requestedX);
                config.SetMechanicalOffsetY(pickerIndex, requestedY);
                config.SetMechanicalOffsetT(pickerIndex, requestedT);

                double afterX = config.GetMechanicalOffsetX(pickerIndex);
                double afterY = config.GetMechanicalOffsetY(pickerIndex);
                double afterT = config.GetMechanicalOffsetT(pickerIndex);
                bool clampedX = !IsZero(afterX - requestedX);
                bool clampedY = !IsZero(afterY - requestedY);
                bool clampedT = !IsZero(afterT - requestedT);
                if (clampedX || clampedY || clampedT)
                {
                    clamped.AppendLine(
                        row.Side + " P" + row.PickerNo +
                        (clampedX ? " X " + F(requestedX) + " → " + F(afterX) : string.Empty) +
                        (clampedY ? " Y " + F(requestedY) + " → " + F(afterY) : string.Empty) +
                        (clampedT ? " T " + F(requestedT) + " → " + F(afterT) : string.Empty));
                }

                EventLogger.Write(
                    EventKind.Event,
                    "COORD",
                    "PICK-RUNTIME-OFFSET-APPLY",
                    "Pick 런타임 보정을 기구 오프셋으로 이관했습니다. side=" + row.Side +
                    ", pickerNo=" + row.PickerNo +
                    ", filterX=" + F(row.X) +
                    ", filterY=" + F(row.Y) +
                    ", filterT=" + F(row.T) +
                    ", applyYSign=" + F(PickApplyYSign) +
                    ", mechXBefore=" + F(beforeX) +
                    ", mechXRequested=" + F(requestedX) +
                    ", mechXAfter=" + F(afterX) +
                    ", mechXClamped=" + clampedX +
                    ", mechYBefore=" + F(beforeY) +
                    ", mechYRequested=" + F(requestedY) +
                    ", mechYAfter=" + F(afterY) +
                    ", mechYClamped=" + clampedY +
                    ", mechTBefore=" + F(beforeT) +
                    ", mechTRequested=" + F(requestedT) +
                    ", mechTAfter=" + F(afterT) +
                    ", mechTClamped=" + clampedT +
                    ", limitMm=" + F(config.MechanicalOffsetLimitMm) +
                    ", limitTDeg=" + F(config.MechanicalOffsetTLimitDeg));

                // 이관한 채널(X/Y/T)의 필터를 0으로 초기화한다 — 이후 이중 보정 방지.
                PickRuntimeOffsetService.Reset(row.Side, row.PickerNo);
                appliedCount++;
            }

            if (appliedCount == 0)
            {
                QMC.Common.MessageDialog.Show("이관 대상이 없습니다.");
                return;
            }

            host.SaveMachineSettings();
            QMC.Common.Log.Write("Main", UserSession.Name, "RuntimeOffsetApply",
                "Pick 런타임 보정을 기구 오프셋으로 이관하고 설정을 저장했습니다. appliedRows=" +
                appliedCount + " - Ok");

            string message = "Pick 런타임 보정을 기구 오프셋으로 이관했습니다. (" + appliedCount + "개 픽커)";
            if (clamped.Length > 0)
            {
                message += "\n\n다음 항목은 설정 한계로 클램프되었습니다:\n" + clamped;
                EventLogger.Write(EventKind.Warning, "COORD", "PICK-RUNTIME-OFFSET-APPLY-CLAMP",
                    "Pick 런타임 보정 이관 중 기구 오프셋 한계 클램프가 발생했습니다. detail=" +
                    clamped.ToString().Replace(Environment.NewLine, " / "));
            }

            QMC.Common.MessageDialog.Show(message);
        }

        private string BuildPickPreview(CDT320_Machine machine, RuntimeOffsetSnapshot[] rows)
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
                if (IsZero(row.X) && IsZero(row.Y) && IsZero(row.T))
                    continue;

                PickerPickUpMotionConfig config = ResolvePickUpConfig(machine, row.Side);
                if (config == null)
                    continue;

                int pickerIndex = row.PickerNo - 1;
                double currentX = config.GetMechanicalOffsetX(pickerIndex);
                double currentY = config.GetMechanicalOffsetY(pickerIndex);
                double currentT = config.GetMechanicalOffsetT(pickerIndex);
                builder.AppendLine(
                    row.Side + " P" + row.PickerNo +
                    " : X " + F(currentX) + " → " + F(currentX + row.X) +
                    " / Y " + F(currentY) + " → " + F(currentY + PickApplyYSign * row.Y) +
                    // T는 가산 이관(2026-08-18 정정) — 19a55857에서 미리보기 갱신이 누락됐던 것을 정정.
                    " / T " + F(currentT) + " → " + F(currentT + row.T));
                any = true;
            }

            return any ? builder.ToString() : null;
        }

        /// <summary>
        /// PICK/PLACE 필터 전체(8세트 X/Y/T)를 0으로 리셋한다.
        /// 운전 중 금지 게이트 통과 후 리셋 전 학습값 요약을 확인받고, 같은 요약을 로그로 남긴다
        /// — 사후에 "리셋 전 값이 뭐였나"를 로그만으로 복원 가능하게 한다.
        /// GeneralPage의 RESET 버튼도 이 플로우를 공유한다(2026-08-16 팀장님 통일 지시).
        /// </summary>
        internal static void ResetRuntimeOffsetsWithConfirm(Form1 host, bool isPlace)
        {
            string label = isPlace ? "PLACE" : "PICK";
            string blockReason;
            if (!CanApplyMechanicalOffset(host, out blockReason))
            {
                QMC.Common.MessageDialog.Show(blockReason);
                return;
            }

            RuntimeOffsetSnapshot[] rows = isPlace
                ? PlaceRuntimeOffsetService.GetSnapshot()
                : PickRuntimeOffsetService.GetSnapshot();
            string summary = BuildSnapshotSummary(rows);

            DialogResult answer = QMC.Common.MessageDialog.Show(
                label + " 런타임 보정 필터(8세트 X/Y/T)를 모두 0으로 초기화합니다.\n\n" +
                "현재 학습값:\n" + summary + "\n계속하시겠습니까?",
                label + " RUNTIME OFFSET RESET",
                MessageBoxButtons.YesNo);
            if (answer != DialogResult.Yes)
                return;

            EventLogger.Write(
                EventKind.Event,
                "COORD",
                (isPlace ? "PLACE" : "PICK") + "-RUNTIME-OFFSET-RESET",
                label + " 런타임 보정 필터 전체를 리셋합니다. user=" + UserSession.Name +
                ", 리셋 전 값=[" + summary.Replace(Environment.NewLine, " / ") + "]");

            if (isPlace)
                PlaceRuntimeOffsetService.ResetAll();
            else
                PickRuntimeOffsetService.ResetAll();

            QMC.Common.Log.Write("Main", UserSession.Name, "RuntimeOffsetReset",
                label + " 런타임 보정 필터 전체를 리셋했습니다. - Ok");
            QMC.Common.MessageDialog.Show(label + " 런타임 보정 필터를 초기화했습니다.");
        }

        private static string BuildSnapshotSummary(RuntimeOffsetSnapshot[] rows)
        {
            if (rows == null || rows.Length == 0)
                return "(값 없음)";

            var builder = new StringBuilder();
            for (int i = 0; i < rows.Length; i++)
            {
                RuntimeOffsetSnapshot row = rows[i];
                if (row == null)
                    continue;

                builder.AppendLine(
                    row.Side + " P" + row.PickerNo +
                    " : X " + F(row.X) + " / Y " + F(row.Y) + " / T " + F(row.T));
            }

            return builder.Length > 0 ? builder.ToString() : "(값 없음)";
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
                double mechT;
                bool hasMech = TryResolveMechanicalOffset(row.Side, row.PickerNo, isPlace, out mechX, out mechY, out mechT);

                DataGridViewRow view = grid.Rows[i];
                view.Cells[0].Value = row.Side.ToString();
                view.Cells[1].Value = "P" + row.PickerNo.ToString(CultureInfo.InvariantCulture);
                view.Cells[2].Value = F(row.X);
                view.Cells[3].Value = F(row.Y);
                view.Cells[4].Value = F(row.T);
                view.Cells[5].Value = hasMech ? F(mechX) : "-";
                view.Cells[6].Value = hasMech ? F(mechY) : "-";
                view.Cells[7].Value = hasMech ? F(mechT) : "-";
                view.Cells[8].Value = row.HasSample
                    ? row.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss")
                    : "(샘플 없음)";
            }
        }

        private static void FillPickerZGrid(DataGridView grid, PickerZRuntimeOffsetSnapshot[] rows)
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
                PickerZRuntimeOffsetSnapshot row = rows[i];
                if (row == null)
                    continue;

                DataGridViewRow view = grid.Rows[i];
                view.Cells[0].Value = row.Side.ToString();
                view.Cells[1].Value = "P" + row.PickerNo.ToString(CultureInfo.InvariantCulture);
                view.Cells[2].Value = F(row.Z);
                view.Cells[3].Value = row.HasSample
                    ? row.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss")
                    : "(샘플 없음)";
            }
        }

        private bool TryResolveMechanicalOffset(
            PickerSequenceSide side,
            int pickerNo,
            bool isPlace,
            out double mechX,
            out double mechY,
            out double mechT)
        {
            mechX = 0.0;
            mechY = 0.0;
            mechT = 0.0;

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
                mechT = placeConfig.GetMechanicalOffsetT(pickerIndex);
                return true;
            }

            PickerPickUpMotionConfig pickConfig = ResolvePickUpConfig(machine, side);
            if (pickConfig == null)
                return false;

            mechX = pickConfig.GetMechanicalOffsetX(pickerIndex);
            mechY = pickConfig.GetMechanicalOffsetY(pickerIndex);
            mechT = pickConfig.GetMechanicalOffsetT(pickerIndex);
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
