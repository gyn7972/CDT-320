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

        /// <summary>등록 가능한 모든 비전 모듈.</summary>
        All = WaferVision | BinVision | BottomInspection | TopSideVision | BottomSideVision
    }
}
