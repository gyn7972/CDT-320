using System;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    /// <summary>
    /// 픽커 ↔ 다이 스테이지 거리 인터록.
    /// 픽커가 스테이지 X 영역 내(좌우 SafetyDistance 안쪽)에 있고 Z 가 내려와 있으면 스테이지 XY 이동 차단.
    /// </summary>
    public class PickerVsStageInterlock : MotionInterlock
    {
        public BaseAxis PickerX     { get; }
        public BaseAxis PickerZ     { get; }
        public BaseAxis StageX      { get; }
        public BaseAxis StageY      { get; }
        /// <summary>픽커가 스테이지 X 작업 영역 안쪽으로 들어왔다고 판단하는 범위 (mm).</summary>
        public double   StageWorkspaceMin { get; }
        public double   StageWorkspaceMax { get; }
        /// <summary>픽커 Z 가 이 값 이하로 내려왔으면 "down" 으로 간주.</summary>
        public double   PickerZDownThreshold { get; }

        public PickerVsStageInterlock(string name,
                                      BaseAxis pickerX, BaseAxis pickerZ,
                                      BaseAxis stageX,  BaseAxis stageY,
                                      double pickerZDownThreshold,
                                      double stageWorkspaceMin, double stageWorkspaceMax)
            : base(name)
        {
            PickerX = pickerX; PickerZ = pickerZ;
            StageX  = stageX;  StageY  = stageY;
            PickerZDownThreshold = pickerZDownThreshold;
            StageWorkspaceMin = stageWorkspaceMin;
            StageWorkspaceMax = stageWorkspaceMax;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            // StageX 또는 StageY 가 움직이려는 경우만 검사
            if (StageX != null && string.Equals(axisName, StageX.Name, StringComparison.OrdinalIgnoreCase) ||
                StageY != null && string.Equals(axisName, StageY.Name, StringComparison.OrdinalIgnoreCase))
            {
                double pxPos = PickerX != null ? PickerX.ActualPosition : double.NaN;
                double pzPos = PickerZ != null ? PickerZ.ActualPosition : double.NaN;
                bool inWorkspace = !double.IsNaN(pxPos) && pxPos >= StageWorkspaceMin && pxPos <= StageWorkspaceMax;
                bool pickerDown  = !double.IsNaN(pzPos) && pzPos <= PickerZDownThreshold;
                if (inWorkspace && pickerDown)
                {
                    reason = $"Picker in stage workspace (X={pxPos:F1} ∈ [{StageWorkspaceMin},{StageWorkspaceMax}]) and Z down ({pzPos:F1} ≤ {PickerZDownThreshold})";
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>듀얼존 — Front/Rear 픽커 X 거리 안전.</summary>
    public class PickerVsPickerInterlock : MotionInterlock
    {
        public BaseAxis FrontX     { get; }
        public BaseAxis RearX      { get; }
        public double   MinDistance{ get; }

        public PickerVsPickerInterlock(string name, BaseAxis frontX, BaseAxis rearX, double minDistance)
            : base(name)
        {
            FrontX = frontX; RearX = rearX; MinDistance = minDistance;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (FrontX == null || RearX == null) return true;
            double frontPos = FrontX.ActualPosition;
            double rearPos  = RearX .ActualPosition;
            if (string.Equals(axisName, FrontX.Name, StringComparison.OrdinalIgnoreCase))
                frontPos = targetPos;
            else if (string.Equals(axisName, RearX.Name, StringComparison.OrdinalIgnoreCase))
                rearPos = targetPos;
            else
                return true;

            double dist = Math.Abs(frontPos - rearPos);
            if (dist < MinDistance)
            {
                reason = $"Front-Rear picker distance {dist:F1}mm < min {MinDistance:F1}mm";
                return false;
            }
            return true;
        }
    }

    /// <summary>비전 ↔ 픽커 인터록 (X축 충돌).</summary>
    public class VisionVsPickerInterlock : MotionInterlock
    {
        public BaseAxis VisionX  { get; }
        public BaseAxis PickerX  { get; }
        public double   MinDistance { get; }

        public VisionVsPickerInterlock(string name, BaseAxis visionX, BaseAxis pickerX, double minDistance)
            : base(name)
        {
            VisionX = visionX; PickerX = pickerX; MinDistance = minDistance;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (VisionX == null || PickerX == null) return true;
            double vp = VisionX.ActualPosition;
            double pp = PickerX.ActualPosition;
            if (string.Equals(axisName, VisionX.Name, StringComparison.OrdinalIgnoreCase)) vp = targetPos;
            else if (string.Equals(axisName, PickerX.Name, StringComparison.OrdinalIgnoreCase)) pp = targetPos;
            else return true;

            double dist = Math.Abs(vp - pp);
            if (dist < MinDistance)
            {
                reason = $"Vision-Picker X distance {dist:F1}mm < min {MinDistance:F1}mm";
                return false;
            }
            return true;
        }
    }

    /// <summary>다이 스테이지 Z (Lifter) ↔ Eject Pin Z 동시 상승 금지.</summary>
    public class StageVsEjectInterlock : MotionInterlock
    {
        public BaseAxis StageZ      { get; }
        public BaseAxis EjectZ      { get; }
        public double   MaxJointUp  { get; }   // Stage Z + Eject Z 합이 이 값을 넘으면 충돌

        public StageVsEjectInterlock(string name, BaseAxis stageZ, BaseAxis ejectZ, double maxJointUp)
            : base(name)
        {
            StageZ = stageZ; EjectZ = ejectZ; MaxJointUp = maxJointUp;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (StageZ == null || EjectZ == null) return true;
            double sz = StageZ.ActualPosition;
            double ez = EjectZ.ActualPosition;
            if (string.Equals(axisName, StageZ.Name, StringComparison.OrdinalIgnoreCase)) sz = targetPos;
            else if (string.Equals(axisName, EjectZ.Name, StringComparison.OrdinalIgnoreCase)) ez = targetPos;
            else return true;

            double total = sz + ez;
            if (total > MaxJointUp)
            {
                reason = $"Stage Z ({sz:F1}) + Eject Z ({ez:F1}) = {total:F1} > max {MaxJointUp:F1}";
                return false;
            }
            return true;
        }
    }

    /// <summary>스테이지 Lifter 가 올라간 상태에서 픽커 Z 가 내려오는 것 금지.</summary>
    public class StageLifterInterlock : MotionInterlock
    {
        public BaseAxis LifterZ      { get; }
        public BaseAxis PickerZ      { get; }
        public double   LifterUpThreshold  { get; }
        public double   PickerDownThreshold{ get; }

        public StageLifterInterlock(string name, BaseAxis lifterZ, BaseAxis pickerZ,
                                    double lifterUpThreshold, double pickerDownThreshold)
            : base(name)
        {
            LifterZ = lifterZ; PickerZ = pickerZ;
            LifterUpThreshold = lifterUpThreshold;
            PickerDownThreshold = pickerDownThreshold;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (LifterZ == null || PickerZ == null) return true;
            // PickerZ 만 검사 (PickerZ 이동 시 LifterZ 가 올라가 있으면 차단)
            if (!string.Equals(axisName, PickerZ.Name, StringComparison.OrdinalIgnoreCase)) return true;
            double lz = LifterZ.ActualPosition;
            if (lz >= LifterUpThreshold && targetPos <= PickerDownThreshold)
            {
                reason = $"Lifter up ({lz:F1} ≥ {LifterUpThreshold:F1}) blocks Picker Z down → {targetPos:F1}";
                return false;
            }
            return true;
        }
    }
}
