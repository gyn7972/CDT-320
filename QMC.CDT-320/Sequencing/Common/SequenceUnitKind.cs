using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>시퀀스 병렬 운영 단위를 나타내는 비트 플래그입니다.</summary>
    [Flags]
    public enum SequenceUnitKind
    {
        /// <summary>선택된 시퀀스 유닛이 없습니다.</summary>
        None = 0,

        /// <summary>로트포트 또는 카세트에서 InputStage로 웨이퍼를 공급하는 유닛입니다.</summary>
        InputLoader = 1 << 0,

        /// <summary>InputCassette와 InputStage 사이 웨이퍼 이송을 담당하는 InputFeeder 유닛입니다.</summary>
        InputFeeder = 1 << 6,

        /// <summary>웨이퍼 정렬, Origin 확정, 픽업 영역 준비를 담당하는 InputStage 유닛입니다.</summary>
        InputStage = 1 << 1,

        /// <summary>TransferPicker LeftArm을 사용하는 FRONT 픽업 유닛입니다.</summary>
        TpuLeft = 1 << 2,

        /// <summary>TransferPicker RightArm을 사용하는 REAR 픽업 유닛입니다.</summary>
        TpuRight = 1 << 3,

        /// <summary>Good 또는 NG 다이를 OutputStage에 안착하는 유닛입니다.</summary>
        OutputStage = 1 << 4,

        /// <summary>완료된 Output 카세트 적재를 담당하는 유닛입니다.</summary>
        OutputUnloader = 1 << 5,

        /// <summary>등록 가능한 모든 시퀀스 유닛을 선택합니다.</summary>
        All = InputLoader | InputFeeder | InputStage | TpuLeft | TpuRight | OutputStage | OutputUnloader
    }
}

