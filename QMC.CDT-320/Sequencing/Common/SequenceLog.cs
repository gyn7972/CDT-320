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

        /// <summary>
        /// 스코프(Push)가 없는 직접 호출 경로 로그를 메시지 접두어/키워드로 시퀀스 종류로 추정한다.
        /// <para>
        /// 입력/출력은 소유 모듈 대괄호 접두어( [UNIT-INPUT] / [INPUT-*] / [OUTPUT] / [OUTPUT-*] )로,
        /// 픽커는 이름( FrontPicker.../PickerFront..., RearPicker.../PickerRear... )으로 구분한다.
        /// 어느 것에도 안 걸리면 <see cref="EventKind.Event"/>.
        /// </para>
        /// </summary>
        public static EventKind ClassifyByMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return EventKind.Event;

            // 1) 입력/출력 — 소유 모듈 대괄호 접두어 기준(픽커 언급이 섞여도 모듈 소유 우선)
            if (Has(msg, "[UNIT-INPUT") || Has(msg, "[INPUT-CASSETTE]") || Has(msg, "[INPUT-FEEDER]") || Has(msg, "[INPUT-STAGE]"))
                return EventKind.InputSeq;
            if (Has(msg, "[UNIT-OUTPUT") || Has(msg, "[OUTPUT]") || Has(msg, "[OUTPUT-CASSETTE]") || Has(msg, "[OUTPUT-FEEDER]") || Has(msg, "[OUTPUT-STAGE]"))
                return EventKind.OutputSeq;

            // 2) 픽커 — 이름 기준
            if (Has(msg, "FrontPicker") || Has(msg, "PickerFront"))
                return EventKind.FrontHeadSeq;
            if (Has(msg, "RearPicker") || Has(msg, "PickerRear"))
                return EventKind.RearHeadSeq;

            return EventKind.Event;
        }

        private static bool Has(string s, string sub) => s.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// 시퀀스 베이스의 <c>WriteLog</c> 헬퍼가 호출하는 이력 라우팅 진입점.
        /// <para>
        /// 현재 시퀀스 스코프(Push)가 있으면 그 <see cref="EventKind"/>·스텝(CODE)으로, 없으면 호출한 베이스가 넘긴
        /// <paramref name="fallbackKind"/>(그 베이스의 고정 종류)로 <see cref="EventLogger"/> 에 기록한다. 메시지 내용
        /// 추정에 의존하지 않으므로, WriteLog 로 남기던 시퀀스 로그가 이력 페이지(InputSeq/OutputSeq/Front·RearHeadSeq)에
        /// 정확히 분류되어 표시된다.
        /// </para>
        /// </summary>
        public static void EmitTrace(EventKind fallbackKind, string source, string message)
        {
            try
            {
                SequenceLogScope seq = _current.Value;
                EventKind kind = seq != null ? seq.Kind : fallbackKind;
                string code = seq != null ? seq.Step : "SEQ";
                EventLogger.Write(kind, "SYSTEM", code, source ?? string.Empty, message ?? string.Empty);
            }
            catch
            {
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
