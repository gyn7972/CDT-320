namespace QMC.CDT320.Sequencing
{
    internal enum PickerPickUpStep
    {
        Idle,
        CheckUnit,
        CheckPickerSideEnabled,
        BuildEnabledPickerList,
        CheckInputStageReady,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        ReserveNextInputDie,
        MoveInputStageAndVisionToDie,
        RequestInputDieVisionInspection,
        ApplyInputDieVisionOffset,
        CalculatePickTarget,
        MovePickerXStageYPickerT,
        VerifyPickTarget,
        MovePickerZPick,
        VacuumOn,
        VerifyDiePicked,
        MovePickerZToAvoid,
        UpdateMaterialToPicker,
        SelectNextPickerOrComplete,
        Complete,
        Error
    }
}
