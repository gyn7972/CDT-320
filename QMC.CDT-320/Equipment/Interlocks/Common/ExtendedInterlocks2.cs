using System;
using QMC.Common.Motion;
using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    /// <summary>Lifter ↔ Expander — Lifter 가 Stage 위 (위로 올림) 일 때 Expander 이동 금지.</summary>
    public class LifterVsExpanderInterlock : MotionInterlock
    {
        public BaseAxis LifterZ      { get; }
        public BaseAxis ExpanderAxis { get; }
        public double   LifterUpThreshold { get; }

        public LifterVsExpanderInterlock(string name, BaseAxis lifterZ, BaseAxis expanderAxis, double lifterUpThr)
            : base(name)
        {
            LifterZ = lifterZ; ExpanderAxis = expanderAxis; LifterUpThreshold = lifterUpThr;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (LifterZ == null || ExpanderAxis == null) return true;
            if (!string.Equals(axisName, ExpanderAxis.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (LifterZ.ActualPosition >= LifterUpThreshold)
            {
                reason = $"Lifter up ({LifterZ.ActualPosition:F1} ≥ {LifterUpThreshold:F1}) blocks Expander move";
                return false;
            }
            return true;
        }
    }

    /// <summary>Barcode reader 모터 ↔ Loader 모터 — Barcode reader 동작 중 Loader 이동 금지.</summary>
    public class BarcodeVsLoaderInterlock : MotionInterlock
    {
        public BaseDigitalInput BarcodeBusySignal { get; }
        public BaseAxis         LoaderX           { get; }

        public BarcodeVsLoaderInterlock(string name, BaseDigitalInput barcodeBusy, BaseAxis loaderX)
            : base(name)
        {
            BarcodeBusySignal = barcodeBusy; LoaderX = loaderX;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (BarcodeBusySignal == null || LoaderX == null) return true;
            if (!string.Equals(axisName, LoaderX.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (BarcodeBusySignal.IsOn)
            {
                reason = "Barcode reader busy — Loader X move blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>SubPort (불량 분리) ↔ Picker — SubPort 가 활성 (다이 분리 중) 일 때 일반 Picker 이동 제한.</summary>
    public class SubPortVsPickerInterlock : MotionInterlock
    {
        public BaseDigitalOutput SubPortGuide { get; }
        public BaseAxis          PickerX      { get; }
        public double            ForbiddenXMin { get; }
        public double            ForbiddenXMax { get; }

        public SubPortVsPickerInterlock(string name, BaseDigitalOutput subPortGuide,
                                        BaseAxis pickerX, double forbidMin, double forbidMax)
            : base(name)
        {
            SubPortGuide = subPortGuide; PickerX = pickerX;
            ForbiddenXMin = forbidMin; ForbiddenXMax = forbidMax;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (SubPortGuide == null || PickerX == null) return true;
            if (!string.Equals(axisName, PickerX.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (SubPortGuide.IsOn && targetPos >= ForbiddenXMin && targetPos <= ForbiddenXMax)
            {
                reason = $"SubPort active — Picker X target {targetPos:F1} in forbidden range [{ForbiddenXMin},{ForbiddenXMax}]";
                return false;
            }
            return true;
        }
    }

    /// <summary>Collet 청소 모듈 ↔ Picker — 청소 중 Picker 이동 금지 (청소기 위치).</summary>
    public class ColletCleanerVsPickerInterlock : MotionInterlock
    {
        public BaseDigitalInput CleanerActive { get; }
        public BaseAxis         PickerZ       { get; }
        public double           PickerDownThreshold { get; }

        public ColletCleanerVsPickerInterlock(string name, BaseDigitalInput cleanerActive,
                                              BaseAxis pickerZ, double pickerDownThr)
            : base(name)
        {
            CleanerActive = cleanerActive; PickerZ = pickerZ;
            PickerDownThreshold = pickerDownThr;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (CleanerActive == null || PickerZ == null) return true;
            if (!string.Equals(axisName, PickerZ.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (CleanerActive.IsOn && targetPos <= PickerDownThreshold)
            {
                reason = $"Collet cleaner active — Picker Z down to {targetPos:F1} blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>비상 정지 (E-Stop) — 모든 축 이동 금지. 다른 인터록과 별개로 즉시 차단.</summary>
    public class EmgStopVsAllInterlock : MotionInterlock
    {
        public BaseDigitalInput EmgInput { get; }

        public EmgStopVsAllInterlock(string name, BaseDigitalInput emgInput) : base(name)
        {
            EmgInput = emgInput;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (EmgInput == null) return true;
            // E-Stop 신호는 보통 NC (정상 시 ON, 누름 시 OFF) — IsOff = pressed
            if (EmgInput.IsOff)
            {
                reason = $"EMERGENCY STOP active — all motion blocked (target {targetPos:F1})";
                return false;
            }
            return true;
        }
    }
}
