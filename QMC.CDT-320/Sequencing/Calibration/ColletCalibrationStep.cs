namespace QMC.CDT320.Sequencing.Calibration
{
    public enum ColletCalibrationStep
    {
        Idle,
        CheckUnit,
        MoveColletToBottomView,
        FindCollet,
        AdjustThetaToZero,
        FindColletAgain,
        CalculateOffset,
        SaveColletCalibration,
        Complete,
        Error
    }
}
