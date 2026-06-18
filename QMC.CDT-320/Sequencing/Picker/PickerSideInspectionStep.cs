namespace QMC.CDT320.Sequencing
{
    internal enum PickerSideInspectionStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        MoveSideEntryYToAvoid,
        MoveSideXToInspection,
        MoveSideYToInspection,
        MoveSideZ,
        MoveSideT0,
        RequestSide0Inspection,
        MoveSideT90,
        RequestSide90Inspection,
        MoveSideT180,
        ApplySideInspectionResult,
        MoveSideZToAvoid,
        MoveSideTToSafe,
        MoveSideYToAvoid,
        MoveSideXToAvoid,
        SelectNextPickerOrComplete,
        Complete,
        Error
    }
}
