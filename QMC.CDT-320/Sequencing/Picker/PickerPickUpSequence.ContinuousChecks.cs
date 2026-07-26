using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPickUpSequence
    {
        private async Task<int> EnsurePickerYAtAvoidBeforePickMoveAsync(CancellationToken ct)
        {
            try
            {
                string continuousDetail;
                if (CanKeepPickerYForwardForContinuousPick(out continuousDetail))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 연속 PickUp X/Y/T 제한 및 외부 간섭 확인 완료. PickerY Avoid 복귀를 생략합니다. " +
                        continuousDetail + " - Ok");
                    return 0;
                }

                double avoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (CanSkipPickerMoveCommand(PickerAxis.PickerY, avoid))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 안전 진입: PickerY가 이미 Avoid 위치입니다. " +
                        "Picker X/T 이동 완료 후에만 PickerY 전진을 시작합니다. " +
                        "pickIndex=" + (_pickCursor + 1) +
                        "/" + _pickBatchItems.Count +
                        ", pickerNo=" + _currentPickerNo + " - Check");
                    return 0;
                }

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "PickUp 안전 진입 PickerY Avoid 이동 전 PickerZ 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    avoid,
                    "pick picker Y avoid before X/T",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 안전 진입: Picker X/T 이동 전에 PickerY를 Avoid로 정리했습니다. " +
                    "pickIndex=" + (_pickCursor + 1) +
                    "/" + _pickBatchItems.Count +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Y-AVOID-EX", Name,
                    "PickUp 전 PickerY Avoid 이동 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool CanKeepPickerYForwardForContinuousPick(out string detail)
        {
            detail = string.Empty;

            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    detail = "runMode is not Auto.";
                    return false;
                }

                if (_pickCursor <= 0)
                {
                    detail = "first pick in batch.";
                    return false;
                }

                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis pickerY = GetPickerAxis(PickerAxis.PickerY);
                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
                BaseAxis pickerT = GetPickerAxis(tAxis);
                if (pickerX == null || pickerY == null || pickerT == null)
                {
                    detail = "picker axis missing. pickerX=" + FormatAxisForContinuousCheck(pickerX) +
                        ", pickerY=" + FormatAxisForContinuousCheck(pickerY) +
                        ", pickerT=" + FormatAxisForContinuousCheck(pickerT);
                    return false;
                }

                if (pickerX.IsMoving || pickerY.IsMoving || pickerT.IsMoving)
                {
                    detail = "picker X/Y/T is moving. pickerX=" + FormatAxisForContinuousCheck(pickerX) +
                        ", pickerY=" + FormatAxisForContinuousCheck(pickerY) +
                        ", pickerT=" + FormatAxisForContinuousCheck(pickerT);
                    return false;
                }

                double avoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, avoid))
                {
                    detail = "pickerY is already Avoid.";
                    return false;
                }

                double deltaX = Math.Abs(_targetPickerX - pickerX.ActualPosition);
                double deltaY = Math.Abs(_targetPickerY - pickerY.ActualPosition);
                double deltaT = Math.Abs(_targetPickerT - pickerT.ActualPosition);
                if (deltaX > ContinuousPickMaxDeltaX ||
                    deltaY > ContinuousPickMaxDeltaY ||
                    deltaT > ContinuousPickMaxDeltaT)
                {
                    detail = "continuous delta limit exceeded. deltaX=" + deltaX.ToString("0.###") +
                        "/" + ContinuousPickMaxDeltaX.ToString("0.###") +
                        ", deltaY=" + deltaY.ToString("0.###") +
                        "/" + ContinuousPickMaxDeltaY.ToString("0.###") +
                        ", deltaT=" + deltaT.ToString("0.###") +
                        "/" + ContinuousPickMaxDeltaT.ToString("0.###");
                    return false;
                }

                string zDetail;
                if (!ArePickerZAxesSafeForContinuousPick(out zDetail))
                {
                    detail = "PickerZ is not safe. " + zDetail;
                    return false;
                }

                InputStageUnit stage = ResolveInputStage();
                string visionDetail;
                if (!IsInputVisionXSafeForContinuousPick(stage, out visionDetail))
                {
                    detail = "InputVisionX is not safe. " + visionDetail;
                    return false;
                }

                string inputZDetail;
                if (!AreInputPickZAxesSafeBeforeContinuousXYT(stage, out inputZDetail))
                {
                    detail = "Input pick Z axes are not safe. " + inputZDetail;
                    return false;
                }

                string oppositeDetail;
                if (IsOppositePickerInputInterferenceActive(out oppositeDetail))
                {
                    detail = "opposite picker blocks Input. " + oppositeDetail;
                    return false;
                }

                string facingDetail;
                if (!IsFrontRearPickerXFacingPrecheckClear(_targetPickerX, out facingDetail))
                {
                    detail = "Front/Rear PickerX facing precheck blocked. " + facingDetail;
                    return false;
                }

                detail = "pickIndex=" + (_pickCursor + 1) +
                    "/" + _pickBatchItems.Count +
                    ", pickerNo=" + _currentPickerNo +
                    ", deltaX=" + deltaX.ToString("0.###") +
                    ", deltaY=" + deltaY.ToString("0.###") +
                    ", deltaT=" + deltaT.ToString("0.###") +
                    ", facing=" + facingDetail;
                return true;
            }
            catch (Exception ex)
            {
                detail = "continuous pick check exception. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerInputInterferenceActive(out string detail)
        {
            detail = string.Empty;

            try
            {
                bool oppositeIsFront = Side == PickerSequenceSide.Rear;
                if (!IsOppositePickerUnitAvailable())
                    return false;

                PickerWorkZone workZone;
                string owner;
                bool workAreaActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    oppositeIsFront,
                    out workZone,
                    out owner);
                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    Context != null ? Context.Machine : null,
                    oppositeIsFront,
                    PickerWorkZone.Input,
                    null,
                    "PickUp continuous input interference check");

                bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
                bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
                bool activeYInput = PickerZoneInterlockRules.GetPickerYActiveTargetZone(oppositeIsFront) == PickerWorkZone.Input;
                bool inputRelated = state != null &&
                    (state.CurrentZone == PickerWorkZone.Input ||
                     state.TargetZone == PickerWorkZone.Input ||
                     state.UnknownUnsafe ||
                     state.BlocksTransport);
                bool movingInputRisk = (xMoving || yMoving) && inputRelated;
                bool inputWorkArea = workAreaActive && workZone == PickerWorkZone.Input;
                bool blocks = inputWorkArea || activeYInput || inputRelated || movingInputRisk;

                detail = "opposite=" + (oppositeIsFront ? "FrontPicker" : "RearPicker") +
                    ", blocks=" + blocks +
                    ", inputWorkArea=" + inputWorkArea +
                    ", workArea=" + (workAreaActive ? workZone.ToString() : "None") +
                    ", owner=" + (workAreaActive ? owner : "-") +
                    ", activeYInput=" + activeYInput +
                    ", movingX=" + xMoving +
                    ", movingY=" + yMoving +
                    ", inputRelated=" + inputRelated +
                    ", movingInputRisk=" + movingInputRisk +
                    ", state=" + (state != null ? state.Describe() : "null");

                return blocks;
            }
            catch (Exception ex)
            {
                detail = "opposite picker Input interference check failed. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerUnitAvailable()
        {
            return Side == PickerSequenceSide.Front ? RearPicker != null : FrontPicker != null;
        }

        private bool ArePickerZAxesSafeForContinuousPick(out string detail)
        {
            detail = string.Empty;

            PickerAxis[] zAxes =
            {
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = GetPickerAxis(zAxis);
                if (axis == null)
                    continue;

                // [정정 2026-07-26, 사용자 승인] PickUpZRising: Avoid로 상승 중인 직전 픽업
                // 픽커 Z는 이동 중이어도 적격 — 웨이퍼에서 멀어지는 방향이며 도착은 백그라운드.
                if (HasActivePickUpZHold && zAxis == GetPickerZAxis(_pickUpZHoldPickerIndex))
                    continue;

                if (axis.IsMoving)
                {
                    detail = zAxis + " is moving. " + FormatAxisForContinuousCheck(axis);
                    return false;
                }

                double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.01;
                bool homeOrAbove = axis.ActualPosition >= -tolerance;
                bool atAvoid = Math.Abs(axis.ActualPosition - avoid) <= tolerance;
                if (!homeOrAbove && !atAvoid)
                {
                    detail = zAxis + " is not at home/avoid. actual=" +
                        axis.ActualPosition.ToString("0.###") +
                        ", avoid=" + avoid.ToString("0.###") +
                        ", tolerance=" + tolerance.ToString("0.###");
                    return false;
                }
            }

            detail = "PickerZ1~4 home/avoid.";
            return true;
        }

        private bool IsInputVisionXSafeForContinuousPick(InputStageUnit stage, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (stage == null || stage.CameraX == null)
                    return true;

                if (stage.CameraX.IsMoving)
                {
                    detail = "InputVisionX is moving. actual=" +
                        stage.CameraX.ActualPosition.ToString("0.###") +
                        ", command=" + stage.CameraX.CommandPosition.ToString("0.###");
                    return false;
                }

                // 기존 조건: 확정 회피 위치 + Actual ≤ 0(entryLimit=0 하드코딩)만 허용.
                // 현재 기준(사용자 승인 2026-07-24): 최소 회피 주차(>0)도 인정 — 확정 회피 위치에 있고
                // 배치 피커 목표들과 페어 간격(SafetyDistance, RetreatExtra 미포함)을 만족하면 통과.
                bool atEntryTarget = _inputVisionPickerEntryTargetPrepared &&
                    IsAxisInTarget(stage.CameraX, _inputVisionPickerEntryTarget);
                bool belowZero = stage.CameraX.ActualPosition <= 0.0;
                if (!atEntryTarget ||
                    (!belowZero && !IsInputVisionParkedClearOfBatchPickerTargets(stage)))
                {
                    detail = "InputVisionX가 확정된 피커 진입 회피 위치가 아닙니다. actual=" +
                        stage.CameraX.ActualPosition.ToString("0.###") +
                        ", target=" + (_inputVisionPickerEntryTargetPrepared
                            ? _inputVisionPickerEntryTarget.ToString("0.###")
                            : "미확정") +
                        ", entryLimit=0 또는 페어 간격 충족";
                    return false;
                }

                detail = "InputVisionX 피커 진입 회피 위치 확인 완료.";
                return true;
            }
            catch (Exception ex)
            {
                detail = "InputVisionX safety check failed. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        // 허가 소비 시점 판정(사용자 승인 2026-07-24): 비전이 정지 상태이고 양쪽 피커 X의
        // 현재 Actual/Command와 페어 간격(SafetyDistance, RetreatExtra 미포함)을 만족하면
        // 전체 Avoid가 아니어도(최소 회피 주차) 픽업 진행을 허용한다.
        // 확장 이력:
        // - 2026-07-26 1차: 이동 중이면 CommandPosition≈회피 목표 등가 비교 — 오탐.
        // - 2026-07-26 2차: 방향 판정 + "활성(미완료) 세션" 요구 — 이동 Task가 실제 축 도착 전에
        //   완료 상태가 되는 사례에서 3/3 결정적 오탐(01:41/01:49/01:52 실장비 알람).
        // - 2026-07-26 3·4차: Task 상태 비의존 + 방향 판정 — 보드 순간 명령값이 이전 목표에서
        //   출발해 내려오는 회피 초반 창에서 방향 판정이 항상 불일치(5/5 오탐, 03:42 실측 확정).
        // - 현재 기준(2026-07-26 5차): 세션 존재 + 도착 예정 목표 페어 간격으로만 판정.
        //   방향 휴리스틱 제거 근거는 이동 중 분기 주석 참조(배타 lease + 존 인터락 최후방어).
        //   모든 불통과 경로에 진단 로그를 남긴다.
        private bool IsInputVisionParkedClearOfPickers(InputStageUnit stage)
        {
            try
            {
                if (stage == null || stage.CameraX == null)
                    return LogInputVisionConsumeCheckFail(stage, "stage/CameraX 참조 없음");

                double vision;
                bool movingWithRetreatSession = false;
                if (stage.CameraX.IsMoving)
                {
                    double independentRetreatTarget;
                    string sessionDetail;
                    if (!VisionIndependentRetreatCoordinator.TryPeekInputRetreatTarget(
                        Side, out independentRetreatTarget, out sessionDetail))
                    {
                        return LogInputVisionConsumeCheckFail(stage,
                            "이동 중 + 독립 회피 세션 없음/사이드 불일치. " + sessionDetail);
                    }

                    // 기존 조건(2026-07-26 4차까지): 순간 명령값 기반 방향 판정(command≤actual+톨러런스)
                    //   — CommandPosition은 UpdateStatus가 보드 순간 프로파일 명령값으로 덮어쓰며,
                    //   회피 프로파일은 "이전 목표(촬영 위치)"에서 출발해 내려오므로 회피 시작 후
                    //   수십 ms 동안 command가 actual보다 위에 머문다(실측 03:42:47 actual=649.713,
                    //   command=650.031). 0.01mm 톨러런스 방향 판정은 이 창에서 항상 불일치 →
                    //   소비 시점이 항상 이 창에 들어와 5/5 결정적 오탐 알람의 직접 원인이었다.
                    // 현재 기준(2026-07-26 5차): 방향 휴리스틱을 제거한다. 안전 근거 —
                    //   ① B1은 픽업이 배타 InputStageArea lease 보유 중에만 실행되고 lease-free
                    //      비전 이동은 독립 회피(세션)뿐이므로 "세션 존재+이동 중"은 곧 회피 이동이다.
                    //   ② 페어 간격은 도착 예정 목표 기준으로 아래에서 검증한다.
                    //   ③ 실제 진입은 존 인터락 제3분기/팔로잉 safetyGap이 최후방어한다.
                    vision = independentRetreatTarget;
                    movingWithRetreatSession = true;
                }
                else
                {
                    vision = stage.CameraX.ActualPosition;
                }

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                if (service == null || Context == null || Context.Machine == null)
                    return LogInputVisionConsumeCheckFail(stage, "SharedRailX 서비스/컨텍스트 없음");

                BaseAxis frontX = Context.Machine.PickerFrontUnit != null ? Context.Machine.PickerFrontUnit.PickerX : null;
                BaseAxis rearX = Context.Machine.PickerRearUnit != null ? Context.Machine.PickerRearUnit.PickerX : null;
                string detail;
                if (frontX != null &&
                    (!service.IsPairClearanceSatisfied(frontX, frontX.ActualPosition, stage.CameraX, vision, out detail) ||
                     !service.IsPairClearanceSatisfied(frontX, frontX.CommandPosition, stage.CameraX, vision, out detail)))
                {
                    return LogInputVisionConsumeCheckFail(stage,
                        "FrontPickerX 페어 간격 부족. visionEval=" + vision.ToString("F6") +
                        ", frontActual=" + frontX.ActualPosition.ToString("F6") +
                        ", frontCommand=" + frontX.CommandPosition.ToString("F6") +
                        ", " + detail);
                }
                if (rearX != null &&
                    (!service.IsPairClearanceSatisfied(rearX, rearX.ActualPosition, stage.CameraX, vision, out detail) ||
                     !service.IsPairClearanceSatisfied(rearX, rearX.CommandPosition, stage.CameraX, vision, out detail)))
                {
                    return LogInputVisionConsumeCheckFail(stage,
                        "RearPickerX 페어 간격 부족. visionEval=" + vision.ToString("F6") +
                        ", rearActual=" + rearX.ActualPosition.ToString("F6") +
                        ", rearCommand=" + rearX.CommandPosition.ToString("F6") +
                        ", " + detail);
                }

                if (movingWithRetreatSession)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 허가 소비 검증(B1): 독립 회피 이동 중 통과 — 도착 예정 목표 기준 페어 간격 충족. " +
                        "retreatTarget=" + vision.ToString("F6") +
                        ", visionActual=" + stage.CameraX.ActualPosition.ToString("F6") +
                        ", visionCommand=" + stage.CameraX.CommandPosition.ToString("F6") +
                        ", side=" + Side + " - Ok");
                }

                return true;
            }
            catch (Exception ex)
            {
                return LogInputVisionConsumeCheckFail(stage, "예외 발생. error=" + ex.Message);
            }
        }

        // 현재 기준(사용자 승인 2026-07-26, 2번): 허가 소비 검증 불통과 사유를 상세 기록 —
        // PICKER-PICKUP-PERMISSION-VISIONX-NOT-AVOID 재발 시 로그만으로 원인을 확정하기 위한 진단.
        private bool LogInputVisionConsumeCheckFail(InputStageUnit stage, string failDetail)
        {
            try
            {
                BaseAxis visionX = stage != null ? stage.CameraX : null;
                WriteLog("PickerPickUpSequence",
                    Name + " 허가 소비 검증(B1) 불통과 상세. reason=" + failDetail +
                    ", visionActual=" + (visionX != null ? visionX.ActualPosition.ToString("F6") : "-") +
                    ", visionCommand=" + (visionX != null ? visionX.CommandPosition.ToString("F6") : "-") +
                    ", visionMoving=" + (visionX != null ? visionX.IsMoving.ToString() : "-") +
                    ", side=" + Side + " - Check");
            }
            catch
            {
            }

            return false;
        }

        // Conti 적격 판정(사용자 승인 2026-07-24): 정지한 비전 위치가 배치 전체 피커 X 목표와
        // 페어 간격(SafetyDistance, RetreatExtra 미포함)을 만족하는지 확인한다.
        private bool IsInputVisionParkedClearOfBatchPickerTargets(InputStageUnit stage)
        {
            try
            {
                if (stage == null || stage.CameraX == null || _pickBatchItems == null || _pickBatchItems.Count == 0)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                if (service == null || pickerX == null)
                    return false;

                double vision = stage.CameraX.ActualPosition;
                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    string detail;
                    if (!service.IsPairClearanceSatisfied(pickerX, _pickBatchItems[i].TargetPickerX, stage.CameraX, vision, out detail))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool AreInputPickZAxesSafeBeforeContinuousXYT(InputStageUnit stage, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (stage == null)
                    return true;

                if (stage.NeedleZ != null && stage.NeedleZ.IsMoving)
                {
                    detail = "NeedleZ is moving. actual=" + stage.NeedleZ.ActualPosition.ToString("0.###");
                    return false;
                }

                // [사용자 승인 2026-07-27] EjectPinZ가 픽업 후 Avoid로 백그라운드 복귀 중이면
                // (join 이연 설계) 이동 중/위치 요구를 면제한다 — 도착 보장·확인은
                // StageY/NeedleX 게이트의 join이 담당한다.
                bool ejectPinReturningToAvoid = _pickUpEjectPinAvoidTask != null;

                if (stage.EjectPinZ != null && !ejectPinReturningToAvoid && stage.EjectPinZ.IsMoving)
                {
                    detail = "EjectPinZ is moving. actual=" + stage.EjectPinZ.ActualPosition.ToString("0.###");
                    return false;
                }

                string needleZTeachingDetail = string.Empty;
                if (!stage.IsNeedleZInHomeOrSafePosition() &&
                    !CanKeepNeedleZAtTeachingPositionForPickMove(stage, out needleZTeachingDetail))
                {
                    detail = "NeedleZ is not home/safe. actual=" +
                        (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("0.###") : "null") +
                        ", teachingCheck=" + needleZTeachingDetail;
                    return false;
                }

                double ejectAvoid = stage.Recipe != null && stage.Recipe.EjectPinZ != null
                    ? stage.Recipe.EjectPinZ.AvoidPosition
                    : 0.0;
                if (stage.EjectPinZ != null && !ejectPinReturningToAvoid)
                {
                    double tolerance = stage.EjectPinZ.Config != null && stage.EjectPinZ.Config.InPositionTolerance > 0.0
                        ? stage.EjectPinZ.Config.InPositionTolerance
                        : 0.01;
                    bool atAvoid = Math.Abs(stage.EjectPinZ.ActualPosition - ejectAvoid) <= tolerance;
                    bool homeOrBelow = stage.EjectPinZ.ActualPosition <= tolerance;
                    if (!atAvoid && !homeOrBelow)
                    {
                        detail = "EjectPinZ is not avoid/home. actual=" +
                            stage.EjectPinZ.ActualPosition.ToString("0.###") +
                            ", avoid=" + ejectAvoid.ToString("0.###") +
                            ", tolerance=" + tolerance.ToString("0.###");
                        return false;
                    }
                }

                detail = "NeedleZ/EjectPinZ safe. " + needleZTeachingDetail;
                return true;
            }
            catch (Exception ex)
            {
                detail = "Input pick Z safety check failed. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool CanKeepNeedleZAtTeachingPositionForPickMove(InputStageUnit stage, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    detail = "runMode is not Auto.";
                    return false;
                }

                if (stage == null)
                {
                    detail = "InputStageUnit is null.";
                    return false;
                }

                BaseAxis needleZ = stage.NeedleZ;
                if (needleZ == null)
                {
                    detail = "NeedleZ axis is null.";
                    return true;
                }

                if (needleZ.IsMoving)
                {
                    detail = "NeedleZ is moving. actual=" + needleZ.ActualPosition.ToString("0.###");
                    return false;
                }

                double tolerance = needleZ.Config != null && needleZ.Config.InPositionTolerance > 0.0
                    ? needleZ.Config.InPositionTolerance
                    : 0.01;

                string teachingName;
                if (!IsNeedleZAtAutoPickTeachingPosition(stage, needleZ.ActualPosition, tolerance, out teachingName))
                {
                    detail = "NeedleZ is not at known Auto Pick teaching position. actual=" +
                        needleZ.ActualPosition.ToString("0.###") +
                        ", tolerance=" + tolerance.ToString("0.###");
                    return false;
                }

                double currentNeedleX = stage.NeedleBlockX != null
                    ? stage.NeedleBlockX.ActualPosition
                    : stage.ResolveNeedleWorkAreaCenterX();
                double currentStageY = stage.StageY != null
                    ? stage.StageY.ActualPosition
                    : stage.ResolveNeedleWorkAreaCenterY();

                string currentAreaReason;
                if (!stage.IsNeedleWorkPointInArea(currentNeedleX, currentStageY, out currentAreaReason))
                {
                    detail = "current Needle work point is outside area. needleX=" +
                        currentNeedleX.ToString("0.###") +
                        ", stageY=" + currentStageY.ToString("0.###") +
                        ", reason=" + currentAreaReason;
                    return false;
                }

                string targetAreaReason;
                if (!stage.IsNeedleWorkPointInArea(_targetNeedleX, _targetStageY, out targetAreaReason))
                {
                    detail = "target Needle work point is outside area. needleX=" +
                        _targetNeedleX.ToString("0.###") +
                        ", stageY=" + _targetStageY.ToString("0.###") +
                        ", reason=" + targetAreaReason;
                    return false;
                }

                bool moveNeedleXFirst;
                string orderReason;
                if (!stage.TryResolveNeedleWorkPointMoveOrder(
                    _targetNeedleX,
                    _targetStageY,
                    out moveNeedleXFirst,
                    out orderReason))
                {
                    detail = "NeedleX/StageY safe order not found. " + orderReason;
                    return false;
                }

                detail = "NeedleZ keep allowed. teaching=" + teachingName +
                    ", actual=" + needleZ.ActualPosition.ToString("0.###") +
                    ", currentNeedleX=" + currentNeedleX.ToString("0.###") +
                    ", currentStageY=" + currentStageY.ToString("0.###") +
                    ", targetNeedleX=" + _targetNeedleX.ToString("0.###") +
                    ", targetStageY=" + _targetStageY.ToString("0.###") +
                    ", order=" + (moveNeedleXFirst ? "NeedleX->StageY" : "StageY->NeedleX") +
                    ", reason=" + orderReason;
                return true;
            }
            catch (Exception ex)
            {
                detail = "NeedleZ teaching keep check failed. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool IsNeedleZAtAutoPickTeachingPosition(
            InputStageUnit stage,
            double actual,
            double tolerance,
            out string teachingName)
        {
            teachingName = string.Empty;

            if (actual <= 0.0 + tolerance)
            {
                teachingName = "Home";
                return true;
            }

            if (stage == null || stage.Recipe == null || stage.Recipe.NeedleZ == null)
                return false;

            stage.Recipe.EnsurePositionObjects();
            StageAxisPositions needleZ = stage.Recipe.NeedleZ;

            if (IsPositionNear(actual, needleZ.AvoidPosition, tolerance))
            {
                teachingName = "AvoidPosition";
                return true;
            }

            if (IsPositionNear(actual, needleZ.ProcessPosition, tolerance))
            {
                teachingName = "ProcessPosition";
                return true;
            }

            if (IsPositionNear(actual, needleZ.ReadyPosition, tolerance))
            {
                teachingName = "ReadyPosition";
                return true;
            }

            if (IsPositionNear(actual, needleZ.NeedlePinCalPosition, tolerance))
            {
                teachingName = "NeedlePinCalPosition";
                return true;
            }

            if (!double.IsNaN(_targetNeedleZ) &&
                !double.IsInfinity(_targetNeedleZ) &&
                IsPositionNear(actual, _targetNeedleZ, tolerance))
            {
                teachingName = "CurrentPickTarget";
                return true;
            }

            return false;
        }

        private static bool IsPositionNear(double actual, double target, double tolerance)
        {
            return Math.Abs(actual - target) <= tolerance;
        }

        private bool IsFrontRearPickerXFacingPrecheckClear(double ownTargetX, out string detail)
        {
            detail = string.Empty;

            try
            {
                CDT320_Machine machine = Context != null ? Context.Machine : null;
                if (machine == null)
                    return true;

                BaseAxis ownX = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis ownY = GetPickerAxis(PickerAxis.PickerY);
                BaseAxis otherX = GetOppositePickerAxis(PickerAxis.PickerX);
                BaseAxis otherY = GetOppositePickerAxis(PickerAxis.PickerY);
                if (ownX == null || otherX == null)
                    return true;

                bool ownYOut = ownY != null && !IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"));
                bool otherYOut = IsOppositePickerYOut(otherY);
                if (!ownYOut || !otherYOut)
                {
                    detail = "one picker Y is safe. ownYOut=" + ownYOut + ", otherYOut=" + otherYOut;
                    return true;
                }

                double otherTargetX = otherX.IsMoving ? otherX.CommandPosition : otherX.ActualPosition;
                double ownMin = Math.Min(ownX.ActualPosition, ownTargetX) - ContinuousPickFacingPrecheckClearance;
                double ownMax = Math.Max(ownX.ActualPosition, ownTargetX) + ContinuousPickFacingPrecheckClearance;
                double otherMin = Math.Min(otherX.ActualPosition, otherTargetX);
                double otherMax = Math.Max(otherX.ActualPosition, otherTargetX);
                bool overlap = otherMax >= ownMin && otherMin <= ownMax;

                detail = "clearance=" + ContinuousPickFacingPrecheckClearance.ToString("0.###") +
                    ", ownX=" + ownX.ActualPosition.ToString("0.###") +
                    "->" + ownTargetX.ToString("0.###") +
                    ", otherX=" + otherX.ActualPosition.ToString("0.###") +
                    "->" + otherTargetX.ToString("0.###") +
                    ", ownY=" + FormatAxisForContinuousCheck(ownY) +
                    ", otherY=" + FormatAxisForContinuousCheck(otherY);
                return !overlap;
            }
            catch (Exception ex)
            {
                detail = "facing precheck exception. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private BaseAxis GetOppositePickerAxis(PickerAxis axis)
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    BaseAxis item;
                    if (RearPicker != null && RearPicker.Axes != null && RearPicker.Axes.TryGetValue(axis, out item))
                        return item;
                    return null;
                }

                BaseAxis frontItem;
                if (FrontPicker != null && FrontPicker.Axes != null && FrontPicker.Axes.TryGetValue(axis, out frontItem))
                    return frontItem;
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerYOut(BaseAxis otherY)
        {
            try
            {
                if (otherY == null)
                    return false;

                if (Math.Abs(otherY.ActualPosition) <= 0.05)
                    return false;

                bool oppositeIsFront = Side == PickerSequenceSide.Rear;
                if (oppositeIsFront)
                    return FrontPicker == null || !FrontPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

                return RearPicker == null || !RearPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static string FormatAxisForContinuousCheck(BaseAxis axis)
        {
            if (axis == null)
                return "<null>";

            return axis.Name +
                "(actual=" + axis.ActualPosition.ToString("0.###") +
                ", command=" + axis.CommandPosition.ToString("0.###") +
                ", moving=" + (axis.IsMoving ? "Y" : "N") +
                ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") + ")";
        }

    }
}
