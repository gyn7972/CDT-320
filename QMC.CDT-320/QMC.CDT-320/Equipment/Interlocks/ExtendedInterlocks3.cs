using System;
using QMC.Common.Motion;
using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    /// <summary>Door Open ↔ All motion — 도어 열림 시 모든 모션 차단.</summary>
    public class DoorVsAllInterlock : MotionInterlock
    {
        public BaseDigitalInput DoorClosed { get; }
        public DoorVsAllInterlock(string name, BaseDigitalInput doorClosed) : base(name) { DoorClosed = doorClosed; }
        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (DoorClosed == null) return true;
            if (DoorClosed.IsOff)
            {
                reason = "Door open — all motion blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>Wafer Vision Z down ↔ Wafer Stage Lift up — 충돌 회피.</summary>
    public class WaferVisionZVsStageLifterInterlock : MotionInterlock
    {
        public BaseAxis WaferVisionZ { get; }
        public BaseAxis StageLifter  { get; }
        public double   VisionDownThreshold  { get; }
        public double   LifterUpThreshold    { get; }

        public WaferVisionZVsStageLifterInterlock(string name, BaseAxis visionZ, BaseAxis lifter,
                                                  double visionDown, double lifterUp) : base(name)
        {
            WaferVisionZ = visionZ; StageLifter = lifter;
            VisionDownThreshold = visionDown; LifterUpThreshold = lifterUp;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (WaferVisionZ == null || StageLifter == null) return true;
            // VisionZ 가 down 일 때 Lifter UP 차단, 반대도 차단
            if (string.Equals(axisName, StageLifter.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (WaferVisionZ.ActualPosition <= VisionDownThreshold && targetPos >= LifterUpThreshold)
                {
                    reason = $"Wafer Vision Z down ({WaferVisionZ.ActualPosition:F1}) blocks Stage Lifter UP";
                    return false;
                }
            }
            else if (string.Equals(axisName, WaferVisionZ.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (StageLifter.ActualPosition >= LifterUpThreshold && targetPos <= VisionDownThreshold)
                {
                    reason = $"Stage Lifter up ({StageLifter.ActualPosition:F1}) blocks Vision Z DOWN";
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>Vacuum 신호 ↔ Picker Z up — 다이가 빨려 있을 때 Picker Z up 만 허용 (down 차단).</summary>
    public class VacuumVsPickerInterlock : MotionInterlock
    {
        public BaseDigitalInput VacuumOn { get; }
        public BaseAxis         PickerZ  { get; }
        public double           PickerDownThreshold { get; }

        public VacuumVsPickerInterlock(string name, BaseDigitalInput vacOn, BaseAxis pickerZ, double pickerDown)
            : base(name)
        {
            VacuumOn = vacOn; PickerZ = pickerZ; PickerDownThreshold = pickerDown;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (VacuumOn == null || PickerZ == null) return true;
            if (!string.Equals(axisName, PickerZ.Name, StringComparison.OrdinalIgnoreCase)) return true;
            // 다이가 빨려있는데 (vacuum on) 추가로 Z 를 down 으로 → 충돌
            if (VacuumOn.IsOn && targetPos <= PickerDownThreshold && PickerZ.ActualPosition <= PickerDownThreshold)
            {
                reason = "Vacuum on (die held) — additional Picker Z down blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>Bin Lid (보호 커버) 닫힘 ↔ Bin Vision 동작.</summary>
    public class BinLidVsBinVisionInterlock : MotionInterlock
    {
        public BaseDigitalInput BinLidOpen   { get; }
        public BaseAxis         BinVisionAxis { get; }

        public BinLidVsBinVisionInterlock(string name, BaseDigitalInput lidOpen, BaseAxis visionAxis)
            : base(name) { BinLidOpen = lidOpen; BinVisionAxis = visionAxis; }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (BinLidOpen == null || BinVisionAxis == null) return true;
            if (!string.Equals(axisName, BinVisionAxis.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (BinLidOpen.IsOn)
            {
                reason = "Bin Lid open — Bin Vision motion blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>Servo OFF 시 모든 모션 차단 (서보 ON 안된 축에 명령 → 즉시 차단).</summary>
    public class ServoOffInterlock : MotionInterlock
    {
        public BaseAxis Axis { get; }
        public ServoOffInterlock(string name, BaseAxis axis) : base(name) { Axis = axis; }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (Axis == null) return true;
            if (!string.Equals(axisName, Axis.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (!Axis.IsServoOn)
            {
                reason = $"{Axis.Name}: Servo OFF — move blocked";
                return false;
            }
            return true;
        }
    }
}
