using System;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    /// <summary>Eject Pin 이 다이를 밀어올리는 동안 Stage 회전 금지.</summary>
    public class EjectVsStageInterlock : MotionInterlock
    {
        public BaseAxis EjectZ      { get; }
        public BaseAxis StageR      { get; }   // 스테이지 회전축 (있는 경우)
        public double   EjectUpThreshold { get; }

        public EjectVsStageInterlock(string name, BaseAxis ejectZ, BaseAxis stageR, double ejectUpThreshold)
            : base(name)
        {
            EjectZ = ejectZ; StageR = stageR; EjectUpThreshold = ejectUpThreshold;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (EjectZ == null || StageR == null) return true;
            if (!string.Equals(axisName, StageR.Name, StringComparison.OrdinalIgnoreCase)) return true;
            double ez = EjectZ.ActualPosition;
            if (ez >= EjectUpThreshold)
            {
                reason = $"Eject Z up ({ez:F1} ≥ {EjectUpThreshold:F1}) blocks Stage R rotation";
                return false;
            }
            return true;
        }
    }

    /// <summary>Loader 가 Stage 위에 있을 때 Stage Z 금지.</summary>
    public class LoaderVsStageInterlock : MotionInterlock
    {
        public BaseAxis LoaderX     { get; }
        public BaseAxis StageZ      { get; }
        public double   LoaderOverStageMin { get; }
        public double   LoaderOverStageMax { get; }

        public LoaderVsStageInterlock(string name, BaseAxis loaderX, BaseAxis stageZ,
                                      double overMin, double overMax) : base(name)
        {
            LoaderX = loaderX; StageZ = stageZ;
            LoaderOverStageMin = overMin; LoaderOverStageMax = overMax;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (LoaderX == null || StageZ == null) return true;
            if (!string.Equals(axisName, StageZ.Name, StringComparison.OrdinalIgnoreCase)) return true;
            double lx = LoaderX.ActualPosition;
            if (lx >= LoaderOverStageMin && lx <= LoaderOverStageMax)
            {
                reason = $"Loader X={lx:F1} in stage range [{LoaderOverStageMin},{LoaderOverStageMax}] — Stage Z blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>Unloader 가 Stage 위에 있을 때 Stage Z 금지.</summary>
    public class UnloaderVsStageInterlock : MotionInterlock
    {
        public BaseAxis UnloaderX   { get; }
        public BaseAxis StageZ      { get; }
        public double   UnloaderOverStageMin { get; }
        public double   UnloaderOverStageMax { get; }

        public UnloaderVsStageInterlock(string name, BaseAxis unloaderX, BaseAxis stageZ,
                                        double overMin, double overMax) : base(name)
        {
            UnloaderX = unloaderX; StageZ = stageZ;
            UnloaderOverStageMin = overMin; UnloaderOverStageMax = overMax;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (UnloaderX == null || StageZ == null) return true;
            if (!string.Equals(axisName, StageZ.Name, StringComparison.OrdinalIgnoreCase)) return true;
            double ux = UnloaderX.ActualPosition;
            if (ux >= UnloaderOverStageMin && ux <= UnloaderOverStageMax)
            {
                reason = $"Unloader X={ux:F1} in stage range [{UnloaderOverStageMin},{UnloaderOverStageMax}] — Stage Z blocked";
                return false;
            }
            return true;
        }
    }

    /// <summary>Eject Pin 상승 + Picker Z 하강 동시 금지.</summary>
    public class EjectVsPickerInterlock : MotionInterlock
    {
        public BaseAxis EjectZ           { get; }
        public BaseAxis PickerZ          { get; }
        public double   EjectUpThreshold { get; }
        public double   PickerDownThreshold { get; }

        public EjectVsPickerInterlock(string name, BaseAxis ejectZ, BaseAxis pickerZ,
                                      double ejectUpThr, double pickerDownThr) : base(name)
        {
            EjectZ = ejectZ; PickerZ = pickerZ;
            EjectUpThreshold = ejectUpThr; PickerDownThreshold = pickerDownThr;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (EjectZ == null || PickerZ == null) return true;
            if (!string.Equals(axisName, PickerZ.Name, StringComparison.OrdinalIgnoreCase)) return true;
            double ez = EjectZ.ActualPosition;
            if (ez >= EjectUpThreshold && targetPos <= PickerDownThreshold)
            {
                reason = $"Eject up ({ez:F1} ≥ {EjectUpThreshold:F1}) blocks Picker Z down → {targetPos:F1}";
                return false;
            }
            return true;
        }
    }

    /// <summary>BinGuide(액세서리) 위치 시 Picker XY 일정 영역 금지.</summary>
    public class BinGuideVsPickerInterlock : MotionInterlock
    {
        public BaseAxis BinGuideAxis { get; }    // 가이드의 위치 또는 신호축
        public BaseAxis PickerX      { get; }
        public BaseAxis PickerY      { get; }
        public double   GuideExtendedThreshold { get; }   // 이 값 이상 = "extended"
        public double   ForbiddenXMin { get; }
        public double   ForbiddenXMax { get; }
        public double   ForbiddenYMin { get; }
        public double   ForbiddenYMax { get; }

        public BinGuideVsPickerInterlock(string name, BaseAxis guideAxis,
                                         BaseAxis pickerX, BaseAxis pickerY,
                                         double extendedThr,
                                         double xMin, double xMax,
                                         double yMin, double yMax)
            : base(name)
        {
            BinGuideAxis = guideAxis; PickerX = pickerX; PickerY = pickerY;
            GuideExtendedThreshold = extendedThr;
            ForbiddenXMin = xMin; ForbiddenXMax = xMax;
            ForbiddenYMin = yMin; ForbiddenYMax = yMax;
        }

        public override bool VerifyMove(string axisName, double targetPos, out string reason)
        {
            reason = null;
            if (BinGuideAxis == null || PickerX == null || PickerY == null) return true;
            if (BinGuideAxis.ActualPosition < GuideExtendedThreshold) return true;

            double px = PickerX.ActualPosition;
            double py = PickerY.ActualPosition;
            if (string.Equals(axisName, PickerX.Name, StringComparison.OrdinalIgnoreCase)) px = targetPos;
            else if (string.Equals(axisName, PickerY.Name, StringComparison.OrdinalIgnoreCase)) py = targetPos;
            else return true;

            if (px >= ForbiddenXMin && px <= ForbiddenXMax &&
                py >= ForbiddenYMin && py <= ForbiddenYMax)
            {
                reason = $"BinGuide extended; Picker target ({px:F1},{py:F1}) in forbidden zone";
                return false;
            }
            return true;
        }
    }
}
