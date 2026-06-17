using System;
using System.Threading;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 현재 실행 중인 시퀀스의 로그 분류 스코프.
    /// <para>
    /// 시퀀스 베이스가 RunAsync 진입 시 <see cref="SequenceLog.Push"/> 하면, 그 실행 흐름(AsyncLocal) 내에서
    /// 발생하는 모든 <c>Context.LogPublic</c> 호출이 해당 <see cref="EventKind"/>(시퀀스 종류) / 유닛명 / 스텝으로
    /// 분류된다. 분류는 Form1 의 LogMessage 싱크가 <see cref="SequenceLog.Current"/> 를 참조해 수행한다.
    /// </para>
    /// </summary>
    public sealed class SequenceLogScope
    {
        /// <summary>로그를 기록할 이벤트 종류(InputSeq/OutputSeq/FrontHeadSeq/RearHeadSeq 등).</summary>
        public EventKind Kind;

        /// <summary>로그 SOURCE 로 쓸 유닛명.</summary>
        public string Unit;

        /// <summary>로그 CODE 로 쓸 현재 스텝 제공자(호출 시점의 현재 스텝을 반환).</summary>
        public Func<string> StepProvider;

        /// <summary>현재 스텝 문자열(없으면 빈 문자열).</summary>
        public string Step
        {
            get
            {
                try { return StepProvider != null ? (StepProvider() ?? string.Empty) : string.Empty; }
                catch { return string.Empty; }
            }
        }
    }

    /// <summary>시퀀스 로그 분류 스코프의 AsyncLocal 컨테이너.</summary>
    public static class SequenceLog
    {
        private static readonly AsyncLocal<SequenceLogScope> _current = new AsyncLocal<SequenceLogScope>();

        /// <summary>현재 실행 흐름의 시퀀스 로그 스코프(없으면 null).</summary>
        public static SequenceLogScope Current
        {
            get { return _current.Value; }
        }

        /// <summary>현재 흐름에 스코프를 설정하고, Dispose 시 이전 스코프로 복원하는 핸들을 반환한다.</summary>
        public static IDisposable Push(EventKind kind, string unit, Func<string> stepProvider)
        {
            SequenceLogScope prev = _current.Value;
            _current.Value = new SequenceLogScope { Kind = kind, Unit = unit ?? string.Empty, StepProvider = stepProvider };
            return new Pop(prev);
        }

        /// <summary><see cref="SequenceUnitKind"/> → <see cref="EventKind"/> 매핑.</summary>
        public static EventKind FromUnitKind(SequenceUnitKind kind)
        {
            switch (kind)
            {
                case SequenceUnitKind.InputLoader:    return EventKind.InputSeq;
                case SequenceUnitKind.PickerFront:    return EventKind.FrontHeadSeq;
                case SequenceUnitKind.PickerRear:     return EventKind.RearHeadSeq;
                case SequenceUnitKind.OutputUnloader: return EventKind.OutputSeq;
                default:                              return EventKind.Event;
            }
        }

        private sealed class Pop : IDisposable
        {
            private readonly SequenceLogScope _prev;
            private bool _done;

            public Pop(SequenceLogScope prev)
            {
                _prev = prev;
            }

            public void Dispose()
            {
                if (_done) return;
                _done = true;
                _current.Value = _prev;
            }
        }
    }
}
