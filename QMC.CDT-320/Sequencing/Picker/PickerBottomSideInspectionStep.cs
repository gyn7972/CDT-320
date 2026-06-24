namespace QMC.CDT320.Sequencing
{
    internal enum PickerBottomSideInspectionStep
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
