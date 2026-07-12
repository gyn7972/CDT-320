namespace QMC.CDT320.Sequencing.Calibration
{
    public enum ColletCalibrationStep
    {
        Idle,
        CheckUnit,
        MoveColletToBottomView,
        FindCollet,
        AdjustThetaToZero,
        AdjustXyToCenter,
        FindColletAgain,
        CalculateOffset,
        SaveColletCalibration,
        RunCocAndSideAutoFocus,
        Complete,
        Error
    }
}
