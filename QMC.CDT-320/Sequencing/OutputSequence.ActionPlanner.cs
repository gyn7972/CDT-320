using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    // Output 자동 시퀀스의 "다음에 무엇을 할지" 판정 로직.
    // 모션을 일으키지 않는 순수 판정만 모았다. 실제 실행은 OutputSequence.UnitCalls.cs와 본체에 있다.
    // OutputSequence.cs에서 순수 이동한 것이다(2026-07-27, 동작 변경 없음).
    public partial class OutputSequence
    {
        private OutputSequenceAutoAction ResolveNextOutputAction()
        {
            // 현재 기준: 액션 판단 전 NG 사용 여부를 Material 상태에 반영한다. (변경 없으면 no-op)
            SyncNgCassetteEnabledWithConfig();

            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion != null && completion.Enabled)
            {
                completion.ObserveCompletionSignals();
                if (completion.IsDrainRequested)
                {
                    // 이미 OutputFeeder에 올라온 자재는 중간에 방치하지 않고 현재 이송만 안전하게 마무리합니다.
                    if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null)
                        return OutputSequenceAutoAction.ResumeOccupiedFeeder;

                    if (HasPickerHeldTargetDieForOutputDrain())
                        return ResolvePickerHeldDieDrainOutputAction();

                    return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
                }
            }

            if (IsOutputStageCompletionSignalSet(BinSide.Ng))
                return OutputSequenceAutoAction.StoreNgStageToCassette;

            if (IsOutputStageCompletionSignalSet(BinSide.Good))
                return OutputSequenceAutoAction.StoreGoodStageToCassette;

            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null)
                return OutputSequenceAutoAction.ResumeOccupiedFeeder;

            bool canSupplyGood = CanSupplyOutputStage(BinSide.Good);
            bool canSupplyNg = CanSupplyOutputStage(BinSide.Ng);

            if (canSupplyNg && (!canSupplyGood || AreBothOutputStagesEmpty()))
                return OutputSequenceAutoAction.SupplyNgCassetteToStage;

            if (canSupplyGood)
                return OutputSequenceAutoAction.SupplyGoodCassetteToStage;

            if (canSupplyNg)
                return OutputSequenceAutoAction.SupplyNgCassetteToStage;

            if (IsOutputAutoNoBinWorkComplete())
                return OutputSequenceAutoAction.StopNoOutputBinWork;

            return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
        }

        private OutputSequenceAutoAction ResolvePickerHeldDieDrainOutputAction()
        {
            // 현재 Place 정책은 검사 결과와 무관하게 Picker 보유 Die를 GOOD Stage로 배출한다.
            // Stop After Drain에서는 그 배출에 필요한 GOOD side 용량만 확보하고 반대 side 신규 교체는 시작하지 않는다.
            if (IsOutputStageCompletionSignalSet(BinSide.Good))
                return OutputSequenceAutoAction.StoreGoodStageToCassette;

            if (CanSupplyOutputStage(BinSide.Good))
                return OutputSequenceAutoAction.SupplyGoodCassetteToStage;

            if (!CanMaintainGoodStageForPickerHeldDieDrain())
                return OutputSequenceAutoAction.StopNoOutputBinWork;

            return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
        }

        private static bool CanMaintainGoodStageForPickerHeldDieDrain()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null ||
                   CanSupplyOutputStage(BinSide.Good);
        }

        private OutputSequenceAutoAction ResolveNextOutputActionForSide(BinSide side)
        {
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion != null && completion.Enabled)
            {
                completion.ObserveCompletionSignals();
                if (completion.IsDrainRequested)
                {
                    WaferMaterial drainFeederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                    BinSide drainFeederSide;
                    if (drainFeederWafer != null &&
                        TryResolveBinSide(drainFeederWafer, out drainFeederSide) &&
                        drainFeederSide == side)
                    {
                        return OutputSequenceAutoAction.ResumeOccupiedFeeder;
                    }

                    if (!HasPickerHeldTargetDieForOutputDrain() || side == BinSide.Ng)
                        return OutputSequenceAutoAction.None;
                }
            }

            if (IsOutputStageCompletionSignalSet(side))
            {
                return side == BinSide.Ng
                    ? OutputSequenceAutoAction.StoreNgStageToCassette
                    : OutputSequenceAutoAction.StoreGoodStageToCassette;
            }

            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (feederWafer != null)
            {
                BinSide feederSide;
                if (TryResolveBinSide(feederWafer, out feederSide) && feederSide == side)
                    return OutputSequenceAutoAction.ResumeOccupiedFeeder;
                return OutputSequenceAutoAction.None;
            }

            if (CanSupplyOutputStage(side))
            {
                return side == BinSide.Ng
                    ? OutputSequenceAutoAction.SupplyNgCassetteToStage
                    : OutputSequenceAutoAction.SupplyGoodCassetteToStage;
            }

            return OutputSequenceAutoAction.None;
        }

        private bool TryResolveImmediateSameSideSupply(
            BinSide side,
            out OutputSequenceAutoAction nextAction,
            out OutputSlotPlan supplyPlan,
            out string reason)
        {
            nextAction = OutputSequenceAutoAction.None;
            supplyPlan = null;
            reason = string.Empty;
            if (!IsAutoOutputLoaderBatchActive)
            {
                reason = "Auto Output Loader 단일 Side batch가 아닙니다.";
                return false;
            }

            string drainReason;
            if (!CanStartImmediateSameSideSupplyDuringDrain(side, out drainReason))
            {
                reason = drainReason;
                return false;
            }

            nextAction = ResolveNextOutputActionForSide(side);
            bool sameSideSupply =
                (side == BinSide.Good && nextAction == OutputSequenceAutoAction.SupplyGoodCassetteToStage) ||
                (side == BinSide.Ng && nextAction == OutputSequenceAutoAction.SupplyNgCassetteToStage);
            if (!sameSideSupply)
            {
                reason = "다음 작업이 동일 Side Supply가 아닙니다. nextAction=" + nextAction;
                return false;
            }

            string consistencyReason;
            if (!ValidateOutputSupplyConsistency(out consistencyReason))
            {
                reason = "Output 공급 상태 불일치. " + consistencyReason;
                return false;
            }

            string slotPlanReason;
            if (!OutputSlotPlanner.TryResolveNextSupplySlot(side, out supplyPlan, out slotPlanReason) ||
                supplyPlan == null ||
                supplyPlan.Side != side)
            {
                supplyPlan = null;
                reason = "동일 Side 즉시 공급 계획을 확정할 수 없습니다. " + slotPlanReason;
                return false;
            }

            return true;
        }

        private bool CanStartImmediateSameSideSupplyDuringDrain(BinSide side, out string reason)
        {
            if (!IsStopAfterDrainRequested())
            {
                reason = string.Empty;
                return true;
            }

            bool heldDieDrain = HasPickerHeldTargetDieForOutputDrain();
            if (heldDieDrain && side == BinSide.Good)
            {
                reason = string.Empty;
                return true;
            }

            reason = "Stop After Drain 요청으로 동일 Side 신규 Supply를 시작하지 않습니다. " +
                     "side=" + side + ", heldDieDrain=" + heldDieDrain;
            return false;
        }

        private bool TryResolveOutputActionSide(OutputSequenceAutoAction action, out BinSide side)
        {
            side = BinSide.Good;
            switch (action)
            {
                case OutputSequenceAutoAction.StoreNgStageToCassette:
                case OutputSequenceAutoAction.SupplyNgCassetteToStage:
                    side = BinSide.Ng;
                    return true;

                case OutputSequenceAutoAction.StoreGoodStageToCassette:
                case OutputSequenceAutoAction.SupplyGoodCassetteToStage:
                    side = BinSide.Good;
                    return true;

                case OutputSequenceAutoAction.ResumeOccupiedFeeder:
                    return TryResolveBinSide(
                        MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder),
                        out side);

                default:
                    return false;
            }
        }

        // 기존 조건: static 메서드로 Ng1 mapped를 무조건 요구했다 - NG 카세트에 빈이 없으면
        //           NG 맵핑이 등록되지 않아 오토 전체 준비가 진행 불가였다.
        // 현재 기준: Config.UseNgCassette=false면 NG mapped 요구를 건너뛴다. (Good2와 동일한 조건부 패턴)
        // To do: [NG 스킵] NG 맵핑 필수 요구를 사용 여부 조건부로 완화.
        private bool AreRequiredOutputCassettesMapped()
        {
            return IsRequiredOutputCassetteSideMapped(BinSide.Good) &&
                   IsRequiredOutputCassetteSideMapped(BinSide.Ng);
        }

        /// <summary>
        /// 선택한 출력 Side가 현재 Recipe 구성에서 Auto 공급에 사용할 수 있도록 Mapping 되었는지 확인한다.
        /// GOOD Mapping은 Good1과 활성화된 Good2를 한 묶음으로 취급한다.
        /// NG 미사용 설정에서는 NG를 Mapping 완료로 간주한다.
        /// </summary>
        private bool IsRequiredOutputCassetteSideMapped(BinSide side)
        {
            MaterialSnapshot state = MaterialStateService.State;
            if (state == null || state.Cassettes == null)
                return false;

            CassetteMaterial good1 = null;
            CassetteMaterial good2 = null;
            CassetteMaterial ng1 = null;
            foreach (CassetteMaterial cassette in state.Cassettes)
            {
                if (cassette == null)
                    continue;

                if (cassette.Role == CassetteMaterialRole.Good1)
                    good1 = cassette;
                else if (cassette.Role == CassetteMaterialRole.Good2)
                    good2 = cassette;
                else if (cassette.Role == CassetteMaterialRole.Ng1)
                    ng1 = cassette;
            }

            if (side == BinSide.Ng)
                return !IsNgCassetteUsed() || IsOutputCassetteMapped(ng1);

            if (!IsOutputCassetteMapped(good1))
                return false;

            var outputCassette = Context != null && Context.Machine != null
                ? Context.Machine.OutputCassetteUnit
                : null;
            bool good2Required = outputCassette != null && outputCassette.Config != null
                ? outputCassette.Config.SelectedCassetteLevel >= 2
                : good2 != null && good2.IsEnabled;
            if (good2 != null && good2.IsEnabled != good2Required)
                return false;
            return !good2Required || IsOutputCassetteMapped(good2);
        }

        private static bool IsOutputCassetteMapped(CassetteMaterial cassette)
        {
            return cassette != null && cassette.IsEnabled && cassette.IsPresent && cassette.IsMapped;
        }

        private static bool AreBothOutputStagesEmpty()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) == null;
        }

        private static bool CanSupplyOutputStage(BinSide side)
        {
            MaterialLocationKind location = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;

            OutputSlotPlan plan;
            return MaterialStateService.GetWaferAtLocation(location) == null &&
                   OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan);
        }

        private static bool IsStageReceiveComplete(BinSide side)
        {
            MaterialLocationKind location = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;

            return MaterialStateService.GetWaferAtLocation(location) != null &&
                   MaterialStateService.IsOutputStageReceiveComplete(side);
        }

        private bool IsOutputStageCompletionSignalSet(BinSide side)
        {
            string signal = side == BinSide.Ng
                ? "OutputNgStageReceiveComplete"
                : "OutputGoodStageReceiveComplete";

            return Context != null && Context.Bus != null && Context.Bus.IsSet(signal);
        }
    }
}
