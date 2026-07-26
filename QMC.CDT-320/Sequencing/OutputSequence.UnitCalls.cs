using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    // 하위 유닛 시퀀스(OutputCassette / OutputStage / OutputFeeder) 호출부.
    // 대부분 옵션을 만들어 하위 시퀀스로 위임하는 얇은 래퍼다.
    // OutputSequence.cs에서 순수 이동한 것이다(2026-07-27, 동작 변경 없음).
    public partial class OutputSequence
    {
        public Task<int> ExecuteCassetteLoadingAsync(CancellationToken ct, TargetCassette target = TargetCassette.Good1, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "Loading",
                () => sequence.RunLoadingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode)),
                "target=" + target);
        }

        public async Task<int> ExecuteCassetteMappingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            // GOOD 카세트 맵핑: 레시피 1단/2단(GoodLevelCount)을 반영해 Good1(+2단 구성 시 Good2) 존을 스캔/등록한다.
            int goodResult = await ExecuteCassetteMappingForSideAsync(
                BinSide.Good,
                ct,
                bFine,
                moveTimeoutMs,
                startMode).ConfigureAwait(false);
            if (goodResult != 0)
                return goodResult;

            // 현재 기준: NG 카세트 미사용(UseNgCassette=false)이면 자동 NG 맵핑도 건너뛴다.
            // To do: [NG 스킵] 자동 맵핑에서 NG 스캔 제외.
            if (!IsNgCassetteUsed())
            {
                Context.LogPublic("[OUTPUT-CASSETTE] NG 카세트 미사용(UseNgCassette=false) - 자동 NG 맵핑을 건너뜁니다.");
                return 0;
            }

            // NG 카세트는 GOOD 맵핑 완료 후, NG에 웨이퍼 Material 정보가 없을 때만 맵핑한다.
            // NG bin에 진행 중 자재가 있으면 재스캔이 추적 상태를 덮어쓰므로 건너뛴다.
            if (HasNgOutputCassetteWaferInfo())
            {
                Context.LogPublic("[OUTPUT-CASSETTE] NG 카세트에 웨이퍼 Material 정보가 있어 자동 NG 맵핑을 건너뜁니다.");
                return 0;
            }

            return await ExecuteCassetteMappingForSideAsync(
                BinSide.Ng,
                ct,
                bFine,
                moveTimeoutMs,
                startMode).ConfigureAwait(false);
        }

        /// <summary>
        /// Auto 시작/재개 시 실제로 Mapping이 무효화된 Side만 다시 Mapping한다.
        /// 반대 Side의 Mapping 및 Material 정보는 유지하며, 대상 Side에 기존 Cassette Material이
        /// 남아 있으면 덮어쓰지 않고 실패 처리한다.
        /// </summary>
        private async Task<int> ExecuteRequiredCassetteMappingsAsync(
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            bool mapGood = !IsRequiredOutputCassetteSideMapped(BinSide.Good);
            bool mapNg = IsNgCassetteUsed() && !IsRequiredOutputCassetteSideMapped(BinSide.Ng);

            if (!mapGood && !mapNg)
            {
                Context.LogPublic("[OUTPUT-CASSETTE] 선택적 Mapping 대상이 없습니다. GOOD/NG Mapping 상태를 유지합니다.");
                return 0;
            }

            Context.LogPublic(
                "[OUTPUT-CASSETTE] 선택적 Mapping을 시작합니다. mapGood=" + mapGood +
                ", mapNg=" + mapNg);

            string preflightAlarmCode;
            string preflightReason;
            if (!TryValidateRequiredCassetteMappingPreconditions(out preflightAlarmCode, out preflightReason))
                return Fail(preflightAlarmCode, "OutputSequence", preflightReason);

            if (mapGood)
            {
                int goodResult = await ExecuteCassetteMappingForSideAsync(
                    BinSide.Good,
                    ct,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
                if (goodResult != 0)
                    return goodResult;
            }

            if (mapNg)
            {
                int ngResult = await ExecuteCassetteMappingForSideAsync(
                    BinSide.Ng,
                    ct,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
                if (ngResult != 0)
                    return ngResult;
            }

            if (!AreRequiredOutputCassettesMapped())
            {
                return Fail(
                    "OUT-CST-SELECTIVE-MAP-INCOMPLETE",
                    "OutputSequence",
                    "선택적 Output Cassette Mapping 후에도 필수 Mapping 상태가 완료되지 않았습니다. " +
                    "mapGood=" + mapGood + ", mapNg=" + mapNg);
            }

            Context.LogPublic(
                "[OUTPUT-CASSETTE] 선택적 Mapping을 완료했습니다. mapGood=" + mapGood +
                ", mapNg=" + mapNg);
            return 0;
        }

        private bool TryValidateRequiredCassetteMappingPreconditions(out string alarmCode, out string reason)
        {
            bool mapGood = !IsRequiredOutputCassetteSideMapped(BinSide.Good);
            bool mapNg = IsNgCassetteUsed() && !IsRequiredOutputCassetteSideMapped(BinSide.Ng);

            if (mapGood)
            {
                if (IsSelectiveCassetteMappingBlockedByActiveMaterial(BinSide.Good, out reason))
                {
                    alarmCode = "OUT-CST-SELECTIVE-GOOD-ACTIVE";
                    return false;
                }

                if (HasOutputCassetteWaferInfo(BinSide.Good))
                {
                    alarmCode = "OUT-CST-SELECTIVE-GOOD-DATA";
                    reason = "GOOD 카세트 Mapping은 무효 상태이지만 기존 GOOD Cassette Material 정보가 남아 있어 자동으로 덮어쓸 수 없습니다.";
                    return false;
                }
            }

            if (mapNg)
            {
                if (IsSelectiveCassetteMappingBlockedByActiveMaterial(BinSide.Ng, out reason))
                {
                    alarmCode = "OUT-CST-SELECTIVE-NG-ACTIVE";
                    return false;
                }

                if (HasOutputCassetteWaferInfo(BinSide.Ng))
                {
                    alarmCode = "OUT-CST-SELECTIVE-NG-DATA";
                    reason = "NG 카세트 Mapping은 무효 상태이지만 기존 NG Cassette Material 정보가 남아 있어 자동으로 덮어쓸 수 없습니다.";
                    return false;
                }
            }

            alarmCode = string.Empty;
            reason = string.Empty;
            return true;
        }

        private static bool IsSelectiveCassetteMappingBlockedByActiveMaterial(BinSide side, out string reason)
        {
            MaterialLocationKind stageLocation = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;
            WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(stageLocation);
            if (stageWafer != null)
            {
                reason = side + " OutputStage에 진행 중인 Material이 있어 같은 Side 카세트를 자동 Mapping할 수 없습니다. " +
                         "wafer=" + stageWafer.WaferId;
                return true;
            }

            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (feederWafer != null)
            {
                BinSide feederSide;
                string feederSideText = TryResolveBinSide(feederWafer, out feederSide)
                    ? feederSide.ToString()
                    : "Unknown";
                reason = "OutputFeeder에 진행 중인 Material이 있어 카세트 Mapping 안전 자세를 확보할 수 없습니다. " +
                         "wafer=" + feederWafer.WaferId + ", feederSide=" + feederSideText +
                         ", mappingSide=" + side;
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private async Task<int> ExecuteCassetteMappingForSideAsync(
            BinSide side,
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            if (side == BinSide.Ng && !IsNgCassetteUsed())
            {
                Context.LogPublic("[OUTPUT-CASSETTE] NG 카세트 미사용(UseNgCassette=false) - NG Mapping을 건너뜁니다.");
                return 0;
            }

            TargetCassette target = side == BinSide.Ng ? TargetCassette.Ng : TargetCassette.Good1;
            string traceStep = side == BinSide.Ng ? "MappingNg" : "Mapping";
            string traceTarget = side == BinSide.Ng ? "target=Ng" : "target=Good";
            var sequence = new OutputCassetteSequence(Context);
            return await SequenceTrace.ChildAsync(
                "OutputCassetteSequence",
                traceStep,
                () => sequence.RunMappingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode)),
                traceTarget).ConfigureAwait(false);
        }

        // 판정 기준(옵션 1): NG(Ng1) 출력 카세트에 웨이퍼 Material 기록이 하나도 없으면 "정보 없음"으로 보고 자동 NG 맵핑을 허용한다.
        // - NG 카세트 슬롯에 배정된 WaferId/HasWafer가 있거나
        // - 위치가 NG 출력 카세트(OutputCassette/Ng1)인 비어있지 않은 웨이퍼가 있으면 정보 있음으로 판정한다.
        private static bool HasNgOutputCassetteWaferInfo()
        {
            return HasOutputCassetteWaferInfo(BinSide.Ng);
        }

        private static bool HasOutputCassetteWaferInfo(BinSide side)
        {
            MaterialSnapshot state = MaterialStateService.State;
            if (state == null)
                return false;

            if (state.Cassettes != null)
            {
                foreach (CassetteMaterial cassette in state.Cassettes)
                {
                    if (cassette == null || !IsOutputCassetteRoleForSide(cassette.Role, side) || cassette.Slots == null)
                        continue;

                    foreach (CassetteSlotMaterial slot in cassette.Slots)
                    {
                        if (slot != null && (slot.HasWafer || !string.IsNullOrWhiteSpace(slot.WaferId)))
                            return true;
                    }
                }
            }

            if (state.Wafers != null)
            {
                foreach (WaferMaterial wafer in state.Wafers)
                {
                    if (wafer == null)
                        continue;
                    if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                        continue;
                    if (wafer.CurrentLocation != null &&
                        wafer.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                        IsOutputCassetteRoleForSide(wafer.CurrentLocation.CassetteRole, side))
                        return true;
                }
            }

            return false;
        }

        private static bool IsOutputCassetteRoleForSide(CassetteMaterialRole role, BinSide side)
        {
            if (side == BinSide.Ng)
                return role == CassetteMaterialRole.Ng1;

            return role == CassetteMaterialRole.Good1 ||
                   role == CassetteMaterialRole.Good2;
        }

        public Task<int> ExecuteCassetteUnloadingAsync(CancellationToken ct, TargetCassette target = TargetCassette.Good1, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "Unloading",
                () => sequence.RunUnloadingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode)),
                "target=" + target);
        }

        public Task<int> ExecuteCassetteMoveToSlotAsync(CancellationToken ct, TargetCassette target, int slotIndex, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            var options = BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode);
            options.SlotIndex = slotIndex;
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "MoveSlot",
                () => sequence.RunMoveSlotAsync(ct, options),
                "target=" + target,
                "slot=" + slotIndex);
        }

        public Task<int> ExecuteStagePrepareLoadAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "PrepareLoad",
                () => sequence.RunPrepareLoadAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStagePrepareUnloadAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "PrepareUnload",
                () => sequence.RunPrepareUnloadAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStageReceiveDieAsync(
            CancellationToken ct,
            DieGrade grade,
            double tpuOffsetX = 0.0,
            double tpuOffsetY = 0.0,
            double visionOffsetX = 0.0,
            double visionOffsetY = 0.0,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            OutputStageSequenceOptions options = BuildStageOptions(
                grade == DieGrade.Ng ? BinSide.Ng : BinSide.Good,
                bFine,
                moveTimeoutMs,
                startMode);

            options.Grade = grade;
            options.TpuOffsetX = tpuOffsetX;
            options.TpuOffsetY = tpuOffsetY;
            options.VisionOffsetX = visionOffsetX;
            options.VisionOffsetY = visionOffsetY;
            return SequenceTrace.ChildAsync("OutputStageSequence", "ReceiveDie",
                () => sequence.RunReceiveDieAsync(ct, options),
                "grade=" + grade,
                "side=" + options.Side);
        }

        public Task<int> ExecuteStageInspectBinAsync(
            CancellationToken ct,
            BinSide side = BinSide.Good,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "InspectBin",
                () => sequence.RunInspectBinAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStageMoveAvoidAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "MoveAvoid",
                () => sequence.RunMoveAvoidAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStageMoveProcessAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "MoveProcess",
                () => sequence.RunMoveProcessAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "LoadFromCassette",
                () => sequence.RunLoadFromCassetteAsync(ct, BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side,
                "slot=" + slotIndex);
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            return ExecuteFeederLoadFromCassetteAsync(ct, slotIndex, cassetteRole, "", bFine, moveTimeoutMs, startMode);
        }

        private static bool ValidateOutputSupplyConsistency(out string reason)
        {
            string goodReason;
            if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(BinSide.Good, out goodReason))
            {
                reason = "GOOD 출력 카세트 센서/Material 데이터가 불일치합니다. " + goodReason;
                return false;
            }

            string ngReason;
            if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(BinSide.Ng, out ngReason))
            {
                reason = "NG 출력 카세트 센서/Material 데이터가 불일치합니다. " + ngReason;
                return false;
            }

            reason = "";
            return true;
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, string expectedWaferId, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode);
            options.CassetteRole = cassetteRole;
            options.ExpectedWaferId = string.IsNullOrWhiteSpace(expectedWaferId)
                ? ResolveExpectedOutputWaferId(side, cassetteRole, slotIndex)
                : expectedWaferId;
            return SequenceTrace.ChildAsync("OutputFeederSequence", "LoadFromCassette",
                () => sequence.RunLoadFromCassetteAsync(ct, options),
                "side=" + side,
                "slot=" + slotIndex,
                "cassetteRole=" + cassetteRole);
        }


        public Task<int> ExecuteFeederLoadToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "LoadToStage",
                () => sequence.RunLoadToStageAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteFeederUnloadFromStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "UnloadFromStage",
                () => sequence.RunUnloadFromStageAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteFeederUnloadToCassetteAsync(CancellationToken ct, int slotIndex, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            CassetteMaterialRole cassetteRole = side == BinSide.Ng
                ? CassetteMaterialRole.Ng1
                : CassetteMaterialRole.Good1;
            return ExecuteFeederUnloadToCassetteWithAcquiredResourcesAsync(
                ct,
                slotIndex,
                cassetteRole,
                bFine,
                moveTimeoutMs,
                startMode);
        }

        public Task<int> ExecuteFeederUnloadToCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            return ExecuteFeederUnloadToCassetteWithAcquiredResourcesAsync(
                ct,
                slotIndex,
                cassetteRole,
                bFine,
                moveTimeoutMs,
                startMode);
        }

        private async Task<int> ExecuteFeederUnloadToCassetteWithAcquiredResourcesAsync(
            CancellationToken ct,
            int slotIndex,
            CassetteMaterialRole cassetteRole,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync(
                "OutputUnloadToCassette",
                ct).ConfigureAwait(false))
            {
                if (placeLease == null)
                    return Fail("OUT-RESOURCE-PLACE", "OutputSequence",
                        "Output UnloadToCassette의 Output Place 영역 리소스 점유에 실패했습니다. side=" + side);

                using (SequenceResourceLease stageLease = await AcquireOutputStageAreaAsync(
                    side,
                    "OutputUnloadToCassette",
                    ct).ConfigureAwait(false))
                {
                    if (stageLease == null)
                        return Fail("OUT-RESOURCE-STAGE", "OutputSequence",
                            "Output UnloadToCassette의 대상 Stage 영역 리소스 점유에 실패했습니다. side=" + side);

                    return await ExecuteFeederUnloadToCassetteWithHeldResourcesAsync(
                        ct,
                        slotIndex,
                        cassetteRole,
                        placeLease,
                        stageLease,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                }
            }
        }

        private Task<int> ExecuteFeederUnloadToCassetteWithHeldResourcesAsync(
            CancellationToken ct,
            int slotIndex,
            CassetteMaterialRole cassetteRole,
            SequenceResourceLease placeLease,
            SequenceResourceLease stageLease,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode,
            bool keepCassetteAtSlotForNextAccess = false)
        {
            var sequence = new OutputFeederSequence(Context);
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildFeederOptions(
                slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode, keepCassetteAtSlotForNextAccess);
            options.CassetteRole = cassetteRole;
            return SequenceTrace.ChildAsync("OutputFeederSequence", "UnloadToCassette",
                () => sequence.RunUnloadToCassetteWithHeldResourcesAsync(
                    ct,
                    options,
                    placeLease,
                    stageLease),
                "side=" + side,
                "slot=" + slotIndex,
                "cassetteRole=" + cassetteRole);
        }

        public Task<int> ExecuteRecoverAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "Recover",
                () => sequence.RunRecoverAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        private async Task<int> ExecuteOutputCompletePostureAsync(string holder, BinSide loadedSide, CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputCompletePosture" : holder;

            try
            {
                if (loadedSide == BinSide.Good)
                {
                    return await ExecuteWithOutputPickerAvoidGateAsync(safeHolder, ct,
                        () => ExecuteOutputCompletePostureCoreAsync(ct, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                }

                using (SequenceResourceLease goodStageLease = await AcquireOutputStageAreaAsync(BinSide.Good, safeHolder, ct).ConfigureAwait(false))
                {
                    if (goodStageLease == null)
                        return Fail("OUT-RESOURCE-GOOD-STAGE", "OutputSequence",
                            safeHolder + " 중 GoodStage 영역 리소스 점유에 실패했습니다.");

                    return await ExecuteWithOutputPickerAvoidGateAsync(safeHolder, ct,
                        () => ExecuteOutputCompletePostureCoreAsync(ct, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-COMPLETE-POSTURE-EX", "OutputSequence",
                    safeHolder + " 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> ExecuteOutputCompletePostureCoreAsync(CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var sequence = new OutputStageSequence(Context);
            OutputStageSequenceOptions options = BuildStageOptions(BinSide.Good, bFine, moveTimeoutMs, startMode);
            // 현재 기준: Output 완료 전 GoodStage는 Process, OutputVisionX는 Avoid 자세로 고정한다.
            options.KeepVisionXAvoidOnProcessMove = true;

            return SequenceTrace.ChildAsync("OutputStageSequence", "CompletePosture",
                () => sequence.RunMoveProcessAsync(ct, options),
                "side=Good",
                "vision=Avoid");
        }
    }
}
