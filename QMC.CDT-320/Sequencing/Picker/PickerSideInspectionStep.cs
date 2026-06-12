namespace QMC.CDT320.Sequencing
{
    internal enum PickerSideInspectionStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        MoveAllPickerZToAvoid,
        SelectNextPicker,
        MoveSideXY,
        MoveSideZ,
        MoveSideT0,
        RequestSide0Inspection,
        MoveSideT90,
        RequestSide90Inspection,
        ApplySideInspectionResult,
        MoveSideZToAvoid,
        MoveSideTToSafe,
        MoveSideXYToAvoid,
        SelectNextPickerOrComplete,
        Complete,
        Error
    }
}
