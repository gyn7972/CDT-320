using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPlaceSequence
    {
        private async Task<int> PublishOutputStageExchangeReadyAfterSafeCompletionAsync(CancellationToken ct)
        {
            try
            {
                if (!MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                    return 0;

                if (!_currentPlaceZSafeReturnCompleted || !IsCurrentPickerAtFullAvoidPosition())
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-UNSAFE", Name,
                        "OutputStage 마지막 Place 교체 준비 신호를 발행할 수 없습니다. " +
                        "마지막 Place Picker 전체 Avoid 복귀가 완료되지 않았습니다. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                if (Context == null || Context.Bus == null)
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-BUS", Name,
                        "OutputStage 마지막 Place 교체 준비 신호를 발행할 Bus가 없습니다. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                if (Context.OutputPostPlaceInspections != null)
                {
                    int idleResult = await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                        "OutputStageExchangeReady:" + Side + ":" + _currentOutputSide,
                        0,
                        ct).ConfigureAwait(false);
                    if (idleResult != 0)
                    {
                        return Fail("PICKER-PLACE-STAGE-COMPLETE-INSPECTION", Name,
                            "OutputStage 마지막 Place 후검사 완료 대기 실패. " +
                            "side=" + Side + ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + ", result=" + idleResult);
                    }
                }

                if (!MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide) ||
                    !IsCurrentPickerAtFullAvoidPosition())
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-FINAL-CHECK", Name,
                        "OutputStage 교체 준비 신호 직전 최종 안전 확인 실패. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                string signal = _currentOutputSide == BinSide.Ng
                    ? "OutputNgStageReceiveComplete"
                    : "OutputGoodStageReceiveComplete";

                Context.Bus.Set(signal);
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place 후 Picker 전체 Avoid 및 후검사 완료를 확인하고 교체 준비 신호를 발행했습니다. " +
                    "side=" + _currentOutputSide + ", signal=" + signal +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-COMPLETE-NOTIFY", Name,
                    "OutputStage 교체 준비 신호 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsCurrentPickerAtFullAvoidPosition()
        {
            PickerAxis[] axes =
            {
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3,
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            for (int i = 0; i < axes.Length; i++)
            {
                PickerAxis axis = axes[i];
                if (!IsPickerAxisInPosition(axis, GetPickerTeachingPosition(axis, "AvoidPosition")))
                    return false;
            }

            return true;
        }

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                CurrentStep = PickerPlaceStep.MovePickerToAvoidAfterPlace;
                return 0;
            }

            CurrentStep = PickerPlaceStep.SelectNextPicker;
            return 0;
        }

        private async Task<int> MovePickerToAvoidAfterPlaceAsync(CancellationToken ct)
        {
            try
            {
                // 현재 기준(사용자 지시 2026-07-26): 후검사 핸드오버를 X 복귀 완료가 아니라
                // "Z/Y 복귀 완료 직후(X −방향 복귀 시작 전)"으로 앞당긴다 — 픽커가 복귀하는 동안
                // 후검사 워커가 스테이지 정렬을 진행하고 OutputVisionX가 퇴장 픽커를 따라 진입한다.
                // 물리 안전 무변경: 스테이지 Z/Y 인터락(픽커 존 미검사), 비전 진입의 공유레일
                // 클리어 대기/return-follow/제3분기 페어 간격이 그대로 담당한다. 존 lease를
                // 물리 퇴장 전에 반환하는 것은 픽업→바텀 전환의 확립된 관례와 동일하다.
                int result = await MovePickerToAvoidAfterPlaceFastAsync(
                    "Place 완료 후 Picker 전체 Avoid 복귀",
                    ct,
                    HandOverToPostPlaceInspectionBeforeFinalReturn).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 아래 반환/종료 호출은 전부 멱등 — 핸드오버가 이미 수행했으면 무동작(백스톱).
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                EndOutputPostPlaceInspectionBatch();

                int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                    "Place 완료 후 후검사 대기 전");
                if (workZoneReleaseResult != 0)
                    return workZoneReleaseResult;

                int completionResult = await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
                if (completionResult != 0)
                    return completionResult;

                CurrentStep = PickerPlaceStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-AVOID-EX", Name,
                    "Place 완료 후 Picker Avoid 복귀 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(string description)
        {
            if (_parentOutputWorkZoneReleaseNotified)
                return 0;

            Func<string, bool> release = ReleaseParentOutputWorkZoneAfterSafeAvoid;
            if (release == null)
                return 0;

            try
            {
                bool released = release(description);
                if (!released)
                {
                    return Fail("PICKER-PLACE-PARENT-WORK-ZONE-RELEASE", Name,
                        "Place 완료 후 Output camera 후검사 대기 전 부모 Picker Output 작업영역을 해제하지 못했습니다. " +
                        "side=" + Side + ", description=" + description);
                }

                _parentOutputWorkZoneReleaseNotified = true;
                WriteLog("PickerPlaceSequence",
                    Name + " Picker 전체 Avoid 확인 후 Output camera 후검사 대기 전에 " +
                    "부모 Picker Output 작업영역을 해제했습니다. side=" + Side +
                    ", description=" + description + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-PARENT-WORK-ZONE-RELEASE-EX", Name,
                    "Place 완료 후 부모 Picker Output 작업영역 해제 중 예외가 발생했습니다. " +
                    "side=" + Side + ", description=" + description +
                    ", error=" + ex.Message);
            }
        }

        // 현재 기준(사용자 지시 2026-07-26): Z/Y 복귀 완료 직후(X 복귀 시작 전) 후검사 핸드오버 —
        // 영역(OutputPlace/Stage/Feeder) 반환 + 후검사 워커 기동 + 부모 Output 존 반환.
        // 전 호출이 멱등이라 완료 지점의 기존 백스톱 호출과 중복 실행돼도 무해하다.
        private int HandOverToPostPlaceInspectionBeforeFinalReturn()
        {
            ReleaseOutputPlaceArea();
            ReleaseOutputStageArea();
            ReleaseOutputFeederArea();
            EndOutputPostPlaceInspectionBatch();

            int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                "Place Z/Y 복귀 후 X 복귀 전 후검사 핸드오버");
            if (workZoneReleaseResult != 0)
                return workZoneReleaseResult;

            WriteLog("PickerPlaceSequence",
                Name + " Z/Y 복귀 완료 — X 복귀 전 후검사 핸드오버 완료(영역 반환+워커 기동). " +
                "OutputVisionX가 퇴장 픽커와 겹쳐 진입할 수 있습니다. side=" + Side + " - Ok");
            return 0;
        }

        private async Task<int> MovePickerToAvoidAfterPlaceFastAsync(string description, CancellationToken ct)
        {
            return await MovePickerToAvoidAfterPlaceFastAsync(description, ct, null).ConfigureAwait(false);
        }

        private async Task<int> MovePickerToAvoidAfterPlaceFastAsync(
            string description,
            CancellationToken ct,
            Func<int> beforeFinalReturnHandover)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    description + " Z축 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    description + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceDoneSafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 현재 기준(사용자 지시 2026-07-26): Y 복귀까지 끝난 시점 — X −방향 복귀 전에
                // 후검사 핸드오버(있으면)를 실행해 비전 진입/스테이지 정렬이 복귀와 겹치게 한다.
                if (beforeFinalReturnHandover != null)
                {
                    int handoverResult = beforeFinalReturnHandover();
                    if (handoverResult != 0)
                        return handoverResult;
                }

                var tTargets = new Dictionary<PickerAxis, double>();
                tTargets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                tTargets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                tTargets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                tTargets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");
                tTargets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");

                result = await MovePickerAxesAndVerifyAsync(
                    tTargets,
                    description + " X/T축 병렬 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceDoneSafeXT").ConfigureAwait(false);
                if (result != 0)
                    return result;

                PickerAxis[] finalAxes =
                {
                    PickerAxis.PickerX,
                    PickerAxis.PickerY,
                    PickerAxis.PickerT0,
                    PickerAxis.PickerT1,
                    PickerAxis.PickerT2,
                    PickerAxis.PickerT3,
                    PickerAxis.PickerZ0,
                    PickerAxis.PickerZ1,
                    PickerAxis.PickerZ2,
                    PickerAxis.PickerZ3
                };

                foreach (PickerAxis axis in finalAxes)
                {
                    double target = GetPickerTeachingPosition(axis, "AvoidPosition");
                    if (!IsPickerAxisInPosition(axis, target))
                    {
                        return Fail("PICKER-PLACE-AVOID-FINAL-POS", Name,
                            description + " 최종 Avoid 위치 확인 실패. " +
                            BuildPickerAxisState(axis, target));
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-AVOID-SEQ-EX", Name,
                    description + " 안전 순서 Avoid 복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildOutputStagePlaceMoveTargetName(string outputStageStep)
        {
            return BuildPlaceMoveTargetName() + ";OutputStageStep=" + outputStageStep;
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct)
        {
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, null, false).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct, string targetName)
        {
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, targetName, false).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(
            BinStageAxis axis,
            double target,
            string description,
            CancellationToken ct,
            string targetName,
            bool forceMove)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool logResumePlaceStageMove = description != null &&
                    description.IndexOf("Place 재시작", StringComparison.Ordinal) >= 0;
                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 시작. axis=" + axis +
                        ", target=" + target +
                        ", targetName=" + (targetName ?? "-") +
                        ", forceMove=" + forceMove +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Start");
                }

                int result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveStageAxis(axis, target, Options.FineMove, targetName, forceMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail("PICKER-PLACE-STAGE-MOVE", "OutputStage",
                        description + " move command failed. result=" + result + ". " +
                        OutputStage.BuildStageAxisState(axis, target));
                }

                // 기존 조건: 이동 후 재대기 + 스냅샷 최종 확인 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3/R4).
                double tolerance = ResolveOutputStageAxisTolerance(axis);

                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 완료. 이후 Picker X/T 완료 확인 후에만 PickerY 전진을 허용합니다. axis=" + axis +
                        ", target=" + target +
                        ", tolerance=" + tolerance +
                        ", targetName=" + (targetName ?? "-") +
                        ", forceMove=" + forceMove +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Ok");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-EX", "OutputStage", description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool CanSkipOutputFeederMoveCommand(OutputFeederUnit feeder, double target)
        {
            BaseAxis axis = feeder != null ? feeder.FeederY : null;
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return axis.IsAtTargetPosition(target, tolerance);
        }

        private double ResolveOutputStageAxisTolerance(BinStageAxis axis)
        {
            try
            {
                BaseAxis item = null;

                switch (axis)
                {
                    case BinStageAxis.GoodBinY:
                        item = OutputStage != null && OutputStage.GoodStage != null ? OutputStage.GoodStage.StageY : null;
                        break;
                    case BinStageAxis.GoodBinZ:
                        item = OutputStage != null && OutputStage.GoodStage != null ? OutputStage.GoodStage.StageZ : null;
                        break;
                    case BinStageAxis.NgBinY:
                        item = OutputStage != null && OutputStage.NgStage != null ? OutputStage.NgStage.StageY : null;
                        break;
                    case BinStageAxis.VisionX:
                        item = OutputStage != null ? OutputStage.OutputCameraX : null;
                        break;
                }

                if (item != null && item.Config != null && item.Config.InPositionTolerance > 0.0)
                    return item.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        private async Task<T> AwaitStepWithCancellationAsync<T>(Task<T> task, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAsync(task, default(T), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private int ResolveVacuumSettleMs()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            return 5; //100
        }

        private void ReleaseOutputStageArea()
        {
            try
            {
                if (_outputStageLease == null)
                    return;

                _outputStageLease.Dispose();
                _outputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputStageArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseOutputPlaceArea()
        {
            try
            {
                ReleasePickerWorkArea();

                if (_outputPlaceLease == null)
                    return;

                _outputPlaceLease.Dispose();
                _outputPlaceLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputPlaceArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseOutputFeederArea()
        {
            try
            {
                if (_outputFeederLease == null)
                    return;

                _outputFeederLease.Dispose();
                _outputFeederLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputFeederArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void BeginOutputPostPlaceInspectionBatch()
        {
            try
            {
                if (_outputInspectBatchOpen)
                    return;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.BeginBatch(Name);
                _outputInspectBatchOpen = true;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 시작 처리 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EndOutputPostPlaceInspectionBatch()
        {
            try
            {
                if (!_outputInspectBatchOpen)
                    return;

                _outputInspectBatchOpen = false;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.EndBatch(Name);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 종료 처리 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void CancelOutputPostPlaceInspectionBatch(string reason)
        {
            try
            {
                if (!_outputInspectBatchOpen)
                    return;

                _outputInspectBatchOpen = false;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.CancelBatch(Name, reason);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 취소 처리 중 예외가 발생했습니다. reason=" +
                    reason + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
