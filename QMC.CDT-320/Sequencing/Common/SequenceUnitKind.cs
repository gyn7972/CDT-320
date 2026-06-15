using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>시퀀스 병렬 운영 단위를 나타내는 비트 플래그입니다.</summary>
    [Flags]
    public enum SequenceUnitKind
    {
        /// <summary>선택된 시퀀스 유닛이 없습니다.</summary>
        None = 0,

        /// <summary>InputCassette, InputFeeder, InputStage를 순서대로 운용하는 입력 라인입니다.</summary>
        InputLoader = 1 << 0,

        /// <summary>Front picker 공정 라인입니다.</summary>
        PickerFront = 1 << 1,

        /// <summary>Rear picker 공정 라인입니다.</summary>
        PickerRear = 1 << 2,

        /// <summary>OutputCassette, OutputFeeder, OutputStage를 순서대로 운용하는 출력 라인입니다.</summary>
        OutputUnloader = 1 << 3,

        /// <summary>등록 가능한 모든 시퀀스 유닛을 선택합니다.</summary>
        All = InputLoader | PickerFront | PickerRear | OutputUnloader
    }
}

