namespace QMC.CDT320.Sequencing.Calibration
{
    /// <summary>
    /// 콜렛 클리닝 시퀀스 스텝.
    /// 한 Side(Front 또는 Rear)의 선택 콜렛 전체를 묶어서
    /// "전부 클린 -> 전부 검사 -> NG만 재시도" 순서로 처리한다.
    /// (Front/Rear는 상대 Picker Avoid 인터락 때문에 동시에 작업할 수 없으므로 Side 단위가 최대 묶음이다.)
    /// </summary>
    public enum ColletCleaningStep
    {
        Idle,
        CheckUnit,
        CheckSafety,
        ReserveArea,
        CleanAllSelectedCollets,
        MoveToInspectionZone,
        InspectAllSelectedCollets,
        EvaluateAndRetry,
        MoveAvoid,
        Complete,
        Error
    }
}
