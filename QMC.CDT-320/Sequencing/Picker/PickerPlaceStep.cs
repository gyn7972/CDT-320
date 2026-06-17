namespace QMC.CDT320.Sequencing
{
    internal enum PickerPlaceStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        ResolveOutputSide,
        VerifyOutputStageReady,
        ReserveOutputStageTarget,
        MoveOutputStageLoadPosition,
        MoveOutputStageReceivePosition,
        CalculatePlaceTarget,
        MovePickerXYPickT,
        VerifyPlaceTarget,
        MovePickerZPlace,
        VacuumOff,
        BlowOff,
        MovePickerZToAvoid,
        UpdateMaterialToOutputStage,
        SelectNextPickerOrComplete,
        Complete,
        Error
    }
}
