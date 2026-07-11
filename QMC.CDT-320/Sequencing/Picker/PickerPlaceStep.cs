namespace QMC.CDT320.Sequencing
{
    internal enum PickerPlaceStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        VerifyPickedPickerFlow,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        ResolveOutputSide,
        VerifyOutputStageReady,
        ReserveOutputStageTarget,
        MoveOutputStageAvoidPosition,
        MoveOutputStageReceivePosition,
        CalculatePlaceTarget,
        MovePickerXYAndTToPlace,
        VerifyPlaceTarget,
        MovePickerZPlace,
        VacuumOff,
        BlowOff,
        MovePickerZToAvoid,
        UpdateMaterialToOutputStage,
        RecoverOutputStageAfterPlace,
        SelectNextPickerOrComplete,
        MovePickerToAvoidAfterPlace,
        Complete,
        Error
    }

    public enum PickerPlaceManualStep
    {
        PreparePlaceTarget = 0,
        MoveStagePickerToPlace = 1,
        VerifyPlaceTarget = 2,
        MovePickerZPlace = 3,
        VacuumOffBlow = 4,
        MovePickerZToAvoid = 5,
        UpdateMaterialToOutputStage = 6,
        RecoverAfterPlace = 7
    }
}
