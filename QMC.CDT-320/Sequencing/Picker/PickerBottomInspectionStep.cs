namespace QMC.CDT320.Sequencing
{
    internal enum PickerBottomInspectionStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        MoveBottomYToAvoidBeforeInspection,
        MoveBottomXToInspection,
        MoveBottomYToInspection,
        MoveBottomZ,
        MoveBottomT,
        RequestBottomInspection,
        ApplyBottomInspectionResult,
        MoveBottomZToAvoid,
        MoveBottomTToSafe,
        MoveBottomYToAvoid,
        MoveBottomXToAvoid,
        SelectNextPickerOrComplete,
        Complete,
        Error
    }
}
