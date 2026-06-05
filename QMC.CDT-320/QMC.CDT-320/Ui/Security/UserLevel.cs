namespace QMC.CDT_320.Ui.Security
{
    /// <summary>
    /// 설비 UI 권한 레벨. 값이 클수록 상위 권한.
    /// 하위 권한은 상위 권한의 모든 조작을 포함하지 않으며, 반대로 상위는 하위의 모든 조작이 가능하다.
    /// </summary>
    public enum UserLevel
    {
        /// <summary>비로그인 상태. 읽기 전용.</summary>
        None        = 0,

        /// <summary>오퍼레이터. 작업 시작/정지, 자재 교환, 카세트 상태 확인.</summary>
        Operator    = 1,

        /// <summary>엔지니어. + 레시피 변경, 수동 이동, 비전 설정.</summary>
        Engineer    = 2,

        /// <summary>메인테넌스. + 축 개별 제어, IO 강제 조작, 캘리브레이션.</summary>
        Maintenance = 3,

        /// <summary>관리자. + 사용자 관리, 시스템 설정, 로그 초기화.</summary>
        Admin       = 4
    }
}
