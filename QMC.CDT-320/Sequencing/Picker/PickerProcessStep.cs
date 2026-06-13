namespace QMC.CDT320.Sequencing
{
    internal enum PickerProcessStep
    {
        Idle,
        CheckUnit,
        RunPickUp,
        RunBottomInspection,
        RunSideInspection,
        RunPlace,
        Complete,
        Error
    }
}
