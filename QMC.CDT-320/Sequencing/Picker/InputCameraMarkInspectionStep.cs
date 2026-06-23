namespace QMC.CDT320.Sequencing
{
    internal enum InputCameraMarkInspectionStep
    {
        Idle,
        CheckUnit,
        BuildEnabledPickerList,
        RunInputCameraMarkInspection,
        MoveInputVisionXToAvoid,
        GrantPickUpPermission,
        Complete,
        Error
    }
}
