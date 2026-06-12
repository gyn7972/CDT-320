namespace QMC.CDT320.Sequencing
{
    internal enum PickerBottomInspectionStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        MoveBottomXY,
        MoveBottomZ,
        MoveBottomT,
        RequestBottomInspection,
        ApplyBottomInspectionResult,
        MoveBottomZToAvoid,
        MoveBottomTToSafe,
        MoveBottomXYToAvoid,
        SelectNextPickerOrComplete,
        Complete,
        Error
    }
}
