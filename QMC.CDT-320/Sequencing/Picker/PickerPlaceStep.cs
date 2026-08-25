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
        WaitBottomFinalBeforePlaceMove,
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
        // [Good 선배출·NG 유예 2026-08-25 팀장님 지시] Good 전량 배출 후 유예 NG 패스 진입 전
        // 스테이지 전환 1회(전 픽커 Z Avoid 검증 + VisionX 재계산 후퇴 + Good 정리 + NgY 선행 정렬).
        TransitionOutputStageForNgPass,
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
