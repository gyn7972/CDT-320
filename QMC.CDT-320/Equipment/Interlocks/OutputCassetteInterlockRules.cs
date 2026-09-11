using QMC.Common;
using System;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class OutputCassetteInterlockRules
    {
        #region 규칙 진입

        // 인터락 항목: OutputLifterZ 이동 요청을 Output Cassette 리프터 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputLifterZ", "ElevatorZ_Output", "OutputLifterZ"))
            {
                OutputCassetteUnit cassette = request.Machine.OutputCassetteUnit;
                string lockReason = string.Empty;
                if (cassette == null || !cassette.CheckNgBinCassetteLockReady(out lockReason))
                {
                    string detail = cassette == null
                        ? "OutputCassetteUnit 상태를 확인할 수 없습니다."
                        : lockReason;
                    bool result = MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "NG BIN LOCK 미완료로 OutputLifterZ 이동이 차단되었습니다. " + detail,
                        out reason);
                    LogBlockedReason(reason);
                    return result;
                }

                if (request.IsSequenceProcess &&
                    request.MoveKind == MotionGuardMoveKind.AxisTeachingMove &&
                    string.Equals(
                        request.TargetName,
                        OutputCassetteUnit.UnloadReleaseLiftTargetName,
                        StringComparison.Ordinal))
                {
                    return VerifyUnloadReleaseLift(
                        request.Machine,
                        request.TargetValue,
                        out reason);
                }

                return VerifyBinLifterZ(request, out reason);
            }

            return true;
        }

        #endregion

        #region Bin Lifter Z

        // 인터락 항목: OutputLifterZ 이동 종류별로 홈/티칭 리프터 조건을 선택한다.
        private static bool VerifyBinLifterZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputCassetteUnit Cassette = request.Machine.OutputCassetteUnit;
                OutputFeederUnit feeder = request.Machine.OutputFeederUnit;
                PickerFrontUnit frontPicker = request.Machine.PickerFrontUnit;
                PickerRearUnit rearPicker = request.Machine.PickerRearUnit;

                switch (request.MoveKind)
                {
                    // 일반 이동 인터락 확인
                    case MotionGuardMoveKind.AxisMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                            return false;

                        return CanMoveBinLifterZ(Cassette, feeder, out reason);
                    // 홈 이동 인터락 확인
                    case MotionGuardMoveKind.AxisHome:
                        //if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                        //    return false;

                        //if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                        //    return false;
                        return CanHomeBinLifterZ(Cassette, feeder, out reason);
                    // 티칭 이동 인터락 확인
                    case MotionGuardMoveKind.AxisTeachingMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                            return false;

                        return CanMoveBinLifterZ(Cassette, feeder, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch(Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    $"Exception occurred while verifying OutputLifterZ motion guard rules: {ex.Message}",
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
            
        }

        #endregion

        #region Unload Release Lift

        internal static bool VerifyUnloadReleaseLift(
            CDT320_Machine machine,
            double targetPosition,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null ||
                    machine.OutputCassetteUnit == null ||
                    machine.OutputCassetteUnit.OutputLifterZ == null ||
                    machine.OutputCassetteUnit.Config == null ||
                    machine.OutputFeederUnit == null ||
                    machine.OutputFeederUnit.FeederY == null)
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "Unload release lift 상태를 확인할 수 없습니다.",
                        out reason);
                }

                OutputCassetteUnit cassette = machine.OutputCassetteUnit;
                OutputFeederUnit feeder = machine.OutputFeederUnit;

                if (!VerifyFrontPickerXAvoidPosition(machine.PickerFrontUnit, out reason))
                    return false;

                if (!VerifyRearPickerXAvoidPosition(machine.PickerRearUnit, out reason))
                    return false;

                if (!cassette.OutputLifterZ.IsServoOn ||
                    cassette.OutputLifterZ.IsAlarm ||
                    cassette.OutputLifterZ.IsMoving)
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "OutputLifterZ가 unload release lift 준비 상태가 아닙니다. " +
                        "servo=" + cassette.OutputLifterZ.IsServoOn +
                        ", alarm=" + cassette.OutputLifterZ.IsAlarm +
                        ", moving=" + cassette.OutputLifterZ.IsMoving,
                        out reason);
                }

                WaferMaterial wafer =
                    MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (wafer == null ||
                    wafer.SourceSlotNumber < 0 ||
                    cassette.Config.SlotCount <= wafer.SourceSlotNumber)
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "Unload release lift 대상 bin의 원본 cassette/slot을 확인할 수 없습니다.",
                        out reason);
                }

                TargetCassette targetCassette;
                BinSide side;
                switch (wafer.SourceCassetteRole)
                {
                    case CassetteMaterialRole.Good1:
                        targetCassette = TargetCassette.Good1;
                        side = BinSide.Good;
                        break;
                    case CassetteMaterialRole.Good2:
                        targetCassette = TargetCassette.Good2;
                        side = BinSide.Good;
                        break;
                    case CassetteMaterialRole.Ng1:
                        targetCassette = TargetCassette.Ng;
                        side = BinSide.Ng;
                        break;
                    default:
                        return MotionGuardRuleHelpers.Block(
                            "OutputLifterZ",
                            "Unload release lift 대상 bin의 Output cassette role이 올바르지 않습니다. role=" +
                            wafer.SourceCassetteRole,
                            out reason);
                }

                if (!feeder.FeederY.IsServoOn ||
                    feeder.FeederY.IsAlarm ||
                    feeder.FeederY.IsMoving ||
                    !feeder.FeederY.IsInPosition ||
                    !feeder.IsBinFeederYInCassetteUnloadPosition(side))
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "OutputFeederY가 대상 CassetteUnloadPosition에 정지·완료되어야 합니다. " +
                        "side=" + side +
                        ", servo=" + feeder.FeederY.IsServoOn +
                        ", alarm=" + feeder.FeederY.IsAlarm +
                        ", moving=" + feeder.FeederY.IsMoving +
                        ", inPosition=" + feeder.FeederY.IsInPosition +
                        ", actual=" + feeder.FeederY.ActualPosition.ToString("0.###"),
                        out reason);
                }

                if (!feeder.IsBinFeederDown() ||
                    feeder.IsBinFeederUp() ||
                    !feeder.IsBinFeederUnclamp() ||
                    feeder.IsBinFeederClamp())
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "Unload release lift 전 피더는 Down/Unclamp 상태여야 합니다.",
                        out reason);
                }

                if (!feeder.HasWaferOnFeeder())
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "Unload release lift 전 OutputFeeder bin 데이터 또는 Ring 감지가 없습니다.",
                        out reason);
                }

                double unloadOffset = cassette.Config.UnloadingPositionOffset;
                double releaseDistance = cassette.Config.UnloadReleaseLiftDistance;
                double tolerance =
                    cassette.OutputLifterZ.Config != null &&
                    cassette.OutputLifterZ.Config.InPositionTolerance > 0.0
                        ? cassette.OutputLifterZ.Config.InPositionTolerance
                        : 0.001;
                bool invalidConfig =
                    double.IsNaN(unloadOffset) ||
                    double.IsInfinity(unloadOffset) ||
                    double.IsNaN(releaseDistance) ||
                    double.IsInfinity(releaseDistance) ||
                    unloadOffset >= 0.0 ||
                    releaseDistance < OutputCassetteUnit.MinUnloadReleaseLiftDistanceMm ||
                    releaseDistance > OutputCassetteUnit.MaxUnloadReleaseLiftDistanceMm ||
                    releaseDistance > Math.Abs(unloadOffset);
                if (invalidConfig)
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "Unload release lift 설정이 안전 범위를 벗어났습니다. " +
                        "unloadOffset=" + unloadOffset.ToString("0.###") +
                        ", releaseDistance=" + releaseDistance.ToString("0.###") +
                        ", minReleaseDistance=" +
                        OutputCassetteUnit.MinUnloadReleaseLiftDistanceMm.ToString("0.###") +
                        ", maxReleaseDistance=" +
                        OutputCassetteUnit.MaxUnloadReleaseLiftDistanceMm.ToString("0.###"),
                        out reason);
                }

                double unloadTarget =
                    cassette.CalculateBinCassetteSlotTargetPosition(
                        targetCassette,
                        wafer.SourceSlotNumber) +
                    unloadOffset;
                double releaseTarget = unloadTarget + releaseDistance;
                double actual = cassette.OutputLifterZ.ActualPosition;
                double moveDistance = targetPosition - actual;
                bool actualAtUnload =
                    OutputCassetteUnit.IsUnloadReleasePositionMatch(
                        actual,
                        unloadTarget,
                        releaseTarget,
                        tolerance);
                bool commandAtUnload =
                    OutputCassetteUnit.IsUnloadReleasePositionMatch(
                        cassette.OutputLifterZ.CommandPosition,
                        unloadTarget,
                        releaseTarget,
                        tolerance);
                bool targetMatches =
                    OutputCassetteUnit.IsUnloadReleasePositionMatch(
                        targetPosition,
                        releaseTarget,
                        unloadTarget,
                        tolerance);
                bool moveDistanceMatches =
                    OutputCassetteUnit.IsUnloadReleasePositionMatch(
                        moveDistance,
                        releaseDistance,
                        0.0,
                        tolerance);
                if (!cassette.OutputLifterZ.IsInPosition ||
                    !actualAtUnload ||
                    !commandAtUnload ||
                    !targetMatches ||
                    !moveDistanceMatches)
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "Unload release lift 거리 조건이 맞지 않습니다. " +
                        "role=" + wafer.SourceCassetteRole +
                        ", slot=" + (wafer.SourceSlotNumber + 1) +
                        ", unloadOffset=" + unloadOffset.ToString("0.###") +
                        ", releaseDistance=" + releaseDistance.ToString("0.###") +
                        ", unloadTarget=" + unloadTarget.ToString("0.###") +
                        ", releaseTarget=" + releaseTarget.ToString("0.###") +
                        ", actual=" + actual.ToString("0.###") +
                        ", command=" + cassette.OutputLifterZ.CommandPosition.ToString("0.###") +
                        ", target=" + targetPosition.ToString("0.###") +
                        ", moveDistance=" + moveDistance.ToString("0.###") +
                        ", inPosition=" + cassette.OutputLifterZ.IsInPosition,
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "Unload release lift 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        #endregion

        #region Picker X Avoid 확인

        // 인터락 조건: OutputLifterZ 이동 전 FrontPickerX가 정확한 AvoidPosition인지 확인한다.
        private static bool VerifyFrontPickerXAvoidPosition(
            PickerFrontUnit frontPicker,
            out string reason)
        {
            reason = string.Empty;

            if (frontPicker == null ||
                frontPicker.PickerX == null ||
                frontPicker.Recipe == null ||
                frontPicker.Config.PickerX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "FrontPickerX AvoidPosition을 확인할 수 없습니다. OutputLifterZ 이동이 차단되었습니다.",
                    out reason);
            }

            double target = frontPicker.Config.PickerX.AvoidPosition;
            if (!frontPicker.IsFrontPickerAxisInTeachingPosition(
                PickerAxis.PickerX,
                "AvoidPosition"))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "FrontPickerX가 AvoidPosition에 있어야 합니다. " +
                    "target=" + target.ToString("0.###") +
                    ", actual=" + frontPicker.PickerX.ActualPosition.ToString("0.###"),
                    out reason);
            }

            return true;
        }

        // 인터락 조건: OutputLifterZ 이동 전 RearPickerX가 정확한 AvoidPosition인지 확인한다.
        // (기존에는 Front Picker만 확인해 Rear Picker 간섭 위치에서도 리프터 이동이 허용되었다.)
        private static bool VerifyRearPickerXAvoidPosition(
            PickerRearUnit rearPicker,
            out string reason)
        {
            reason = string.Empty;

            if (rearPicker == null ||
                rearPicker.PickerX == null ||
                rearPicker.Recipe == null ||
                rearPicker.Config.PickerX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "RearPickerX AvoidPosition을 확인할 수 없습니다. OutputLifterZ 이동이 차단되었습니다.",
                    out reason);
            }

            double target = rearPicker.Config.PickerX.AvoidPosition;
            if (!rearPicker.IsRearPickerAxisInTeachingPosition(
                PickerAxis.PickerX,
                "AvoidPosition"))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "RearPickerX가 AvoidPosition에 있어야 합니다. " +
                    "target=" + target.ToString("0.###") +
                    ", actual=" + rearPicker.PickerX.ActualPosition.ToString("0.###"),
                    out reason);
            }

            return true;
        }

        #endregion

        #region Home 및 이동 허용 조건

        // 인터락 항목: OutputLifterZ 홈은 Bin 돌출, OutputFeederY 이동 중, 카세트 안전 위치를 확인한다.
        private static bool CanHomeBinLifterZ(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            // 인터락 조건: Bin 돌출이 감지되면 OutputLifterZ 홈 이동을 차단한다.
            if (cassette != null && cassette.IsBinProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputCassette bin protrusion detected. OutputLifterZ home is blocked.",
                    out reason);
            }

            // 방어 조건: Feeder 참조가 없으면 Feeder 연동 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: OutputFeederY가 이동 중이면 OutputLifterZ 홈 이동을 차단한다.
            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY is moving. OutputLifterZ home is blocked.",
                    out reason);
            }

            // 인터락 조건: OutputFeederY가 카세트 안전 위치가 아니면 OutputLifterZ 홈 이동을 차단한다.
            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at a cassette-safe position before OutputLifterZ home.",
                    out reason);
            }

            return true;
        }

        // 인터락 항목: OutputLifterZ 이동은 Bin 돌출, OutputFeederY 이동 중, 카세트 안전 위치를 확인한다.
        private static bool CanMoveBinLifterZ(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            if (cassette != null && cassette.IsBinProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputCassette bin protrusion detected. OutputLifterZ move is blocked.",
                    out reason);
            }

            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY is moving. OutputLifterZ move is blocked.",
                    out reason);
            }

            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at a cassette-safe position before OutputLifterZ move.",
                    out reason);
            }

            return true;
        }

        #endregion

        #region Feeder 상태 및 차단 로그

        // 인터락 기준: OutputLifterZ 이동 전 OutputFeederY가 카세트 진입 구간 밖의 안전 위치인지 판단한다.
        private static bool IsOutputFeederYSafeForOutputLifterZ(OutputFeederUnit feeder)
        {
            string detail;
            return IsOutputFeederYSafeForOutputLifterZ(feeder, out detail);
        }

        // InputCassetteInterlockRules와 같은 기계 계약을 적용한다.
        // OutputFeederY는 음수 방향으로 이동할 때만 카세트 안으로 진입한다.
        // 기존 티칭 안전 위치는 유지하고, 정지된 엔코더 위치가 0 이상인 경우도 안전으로 인정한다.
        internal static bool IsOutputFeederYSafeForOutputLifterZ(
            OutputFeederUnit feeder,
            out string detail)
        {
            detail = string.Empty;
            if (feeder == null || feeder.FeederY == null)
                return true;

            QMC.Common.Motion.BaseAxis feederY = feeder.FeederY;
            double tolerance =
                feederY.Config != null &&
                feederY.Config.InPositionTolerance > 0.0
                    ? feederY.Config.InPositionTolerance
                    : 0.05;
            double actual = feederY.ActualPosition;
            detail =
                "OutputFeederY actual=" + actual.ToString("0.###") +
                ", cassetteEntryLimit=" + (-tolerance).ToString("0.###");

            if (feederY.IsMoving)
            {
                detail += ", moving=Y";
                return false;
            }

            if (feeder.IsBinFeederYInAvoidPosition())
                return true;

            return actual >= -tolerance;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputCassetteInterlock", reason + " - Blocked");
            }
            catch
            {
            }
            finally
            {
            }
        }

        #endregion
    }
}
