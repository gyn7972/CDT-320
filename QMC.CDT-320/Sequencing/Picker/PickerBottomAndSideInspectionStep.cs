namespace QMC.CDT320.Sequencing
{
    internal enum PickerBottomAndSideInspectionStep
    {
        Idle,
        CheckUnit,
        BuildPickedPickerList,
        AcquireInspectionArea,
        MoveOppositePickerToAvoidBeforeInspection,
        RunBottomPipeline,
        RunSidePipeline,
        MoveFinalZToAvoid,
        CompletePendingT0Return,
        MoveFinalYToAvoid,
        MoveFinalXToAvoid,
        Complete,
        Error
    }
}
