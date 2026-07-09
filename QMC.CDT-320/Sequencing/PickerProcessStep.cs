namespace QMC.CDT320.Sequencing
{
    internal enum PickerProcessStep
    {
        Idle,
        CheckUnit,
        RunInputCameraMarkInspection,
        RunPickUp,
        RunBottomInspection,
        RunSideInspection,
        RunPlace,
        Complete,
        Error
    }
}
