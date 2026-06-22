using System;

namespace QMC.Vision.Sequencing
{
    /// <summary>병렬 운영 단위(비전 모듈)를 나타내는 비트 플래그.
    /// 핸들러 SequenceUnitKind(유닛) 미러를 비전 모듈 단위로 치환.</summary>
    [Flags]
    public enum SequenceModuleKind
    {
        /// <summary>선택된 모듈 없음.</summary>
        None = 0,

        WaferVision      = 1 << 0,
        BinVision        = 1 << 1,
        BottomInspection = 1 << 2,
        TopSideVision    = 1 << 3,
        BottomSideVision = 1 << 4,

        /// <summary>측면검사 통합 단위 — 앞(Top)+뒤(Bottom) 측면을 한 단위로 동시 구동(실제 운전/동시 실행).
        /// 모듈 자체는 분리 유지(개별 테스트 가능)하며, 이 합성값은 '둘을 동시에' 묶어 돌리기 위한 선택용이다.
        /// 코디네이터 Start 는 비트마스크로 두 모듈 루프를 병렬 시작한다.</summary>
        SideVision = TopSideVision | BottomSideVision,

        /// <summary>등록 가능한 모든 비전 모듈.</summary>
        All = WaferVision | BinVision | BottomInspection | TopSideVision | BottomSideVision
    }
}
