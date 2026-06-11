namespace QMC.CDT320.Sequencing
{
    internal enum PickerProcessStep
    {
        Idle,
        CheckUnit,
        CheckPickerSafe,
        WaitInputReady,
        AcquireInputArea,
        MoveInputStageToReservedDie,
        MoveInputPickPosition,
        PickDieFromInput,
        MoveInputAvoidPosition,
        MoveInspectionPosition,
        InspectDie,
        WaitOutputReady,
        ResolveOutputSide,
        AcquireOutputArea,
        PrepareOutputReceivePosition,
        MoveOutputPlacePosition,
        PlaceDieToOutput,
        MoveOutputAvoidPosition,
        InspectOutputPlacement,
        Complete,
        Error
    }
}
