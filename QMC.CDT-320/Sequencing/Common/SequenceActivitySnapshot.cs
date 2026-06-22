using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 시퀀스 유닛 1개의 현재 동작 상태 스냅샷(불변).<br/>
    /// UI 는 이 스냅샷만 읽어 표시한다. (UI 스레드에서 직접 시퀀스 상태에 접근하지 않는다)
    /// </summary>
    public sealed class SequenceActivitySnapshot
    {
        public SequenceActivitySnapshot(
            SequenceUnitKind unit,
            string displayName,
            SequenceActivityState state,
            string actionName,
            string stepName,
            string detail,
            DateTime startedAt,
            DateTime updatedAt,
            TimeSpan elapsed)
        {
            Unit = unit;
            DisplayName = displayName ?? string.Empty;
            State = state;
            ActionName = actionName ?? string.Empty;
            StepName = stepName ?? string.Empty;
            Detail = detail ?? string.Empty;
            StartedAt = startedAt;
            UpdatedAt = updatedAt;
            Elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }

        /// <summary>유닛 종류.</summary>
        public SequenceUnitKind Unit { get; private set; }

        /// <summary>화면 표시용 이름(INPUT / FRONT / REAR / OUTPUT).</summary>
        public string DisplayName { get; private set; }

        /// <summary>현재 상태.</summary>
        public SequenceActivityState State { get; private set; }

        /// <summary>현재 작업명(한글). 예: "웨이퍼 로딩".</summary>
        public string ActionName { get; private set; }

        /// <summary>현재 Step 이름. 예: "LoadFeederToStage".</summary>
        public string StepName { get; private set; }

        /// <summary>상세 설명(한글).</summary>
        public string Detail { get; private set; }

        /// <summary>현재 동작 시작 시각.</summary>
        public DateTime StartedAt { get; private set; }

        /// <summary>마지막 갱신 시각.</summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>경과 시간. (진행/대기 중이면 현재까지, 그 외는 마지막 갱신 기준)</summary>
        public TimeSpan Elapsed { get; private set; }
    }
}
