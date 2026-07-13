namespace QMC.CDT320.Sequencing
{
    internal enum InputDieVisionPrepareStep
    {
        Idle,
        CheckUnit,
        BuildPickBatch,
        SelectNextInspectionTarget,
        VerifyReservedInputDie,
        MovePickersToAvoidForInputVisionMove,
        MoveInputStageAndVisionToDie,
        StartInputDieVisionInspection,
        ApplyInputDieVisionOffset,
        Complete,
        Error
    }
}
