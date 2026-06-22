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
        RequestInputDieVisionInspection,
        ApplyInputDieVisionOffset,
        Complete,
        Error
    }
}
