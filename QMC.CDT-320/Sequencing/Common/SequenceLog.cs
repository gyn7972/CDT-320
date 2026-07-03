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

        public string RunId;

        public string Parent;

        public int Depth;

        public string Mode;

        public string SequenceName;

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
        private const int RepeatLogSummaryIntervalMs = 1000;
        private const int MaxRepeatStateCount = 4096;
        private static readonly AsyncLocal<SequenceLogScope> _current = new AsyncLocal<SequenceLogScope>();
        private static readonly object _repeatSync = new object();
        private static readonly System.Collections.Generic.Dictionary<string, RepeatLogState> _repeatStates =
            new System.Collections.Generic.Dictionary<string, RepeatLogState>(System.StringComparer.Ordinal);

        /// <summary>현재 실행 흐름의 시퀀스 로그 스코프(없으면 null).</summary>
        public static SequenceLogScope Current
        {
            get { return _current.Value; }
        }

        /// <summary>현재 흐름에 스코프를 설정하고, Dispose 시 이전 스코프로 복원하는 핸들을 반환한다.</summary>
        public static IDisposable Push(EventKind kind, string unit, Func<string> stepProvider)
        {
            return Push(kind, unit, stepProvider, unit, null, null);
        }

        public static IDisposable Push(EventKind kind, string unit, Func<string> stepProvider, string sequenceName, string mode)
        {
            return Push(kind, unit, stepProvider, sequenceName, mode, null);
        }

        public static IDisposable Push(EventKind kind, string unit, Func<string> stepProvider, string sequenceName, string mode, string parent)
        {
            SequenceLogScope prev = _current.Value;
            string safeUnit = unit ?? string.Empty;
            string safeSequenceName = string.IsNullOrWhiteSpace(sequenceName) ? safeUnit : sequenceName;
            string inheritedRunId = prev != null ? prev.RunId : string.Empty;
            _current.Value = new SequenceLogScope
            {
                Kind = kind,
                Unit = safeUnit,
                StepProvider = stepProvider,
                RunId = string.IsNullOrWhiteSpace(inheritedRunId)
                    ? SequenceTrace.CreateRunId(kind, safeSequenceName)
                    : inheritedRunId,
                Parent = !string.IsNullOrWhiteSpace(parent)
                    ? parent
                    : prev != null ? (!string.IsNullOrWhiteSpace(prev.SequenceName) ? prev.SequenceName : prev.Unit) : string.Empty,
                Depth = prev != null ? prev.Depth + 1 : 0,
                Mode = mode ?? (prev != null ? prev.Mode : string.Empty),
                SequenceName = safeSequenceName
            };
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

        public static string FormatWithCurrentContext(string phase, string source, string message)
        {
            try
            {
                SequenceLogScope seq = _current.Value;
                if (seq == null)
                    return message ?? string.Empty;

                var parts = new System.Collections.Generic.List<string>();
                Add(parts, "trace", SequenceTrace.NextTraceId());
                Add(parts, "run", seq.RunId);
                Add(parts, "parent", seq.Parent);
                Add(parts, "seq", seq.SequenceName);
                Add(parts, "step", seq.Step);
                Add(parts, "phase", string.IsNullOrWhiteSpace(phase) ? "Log" : phase);
                Add(parts, "mode", seq.Mode);
                Add(parts, "depth", seq.Depth.ToString());
                Add(parts, "source", source);
                Add(parts, "msg", message);
                return string.Join(" ", parts.ToArray());
            }
            catch
            {
                return message ?? string.Empty;
            }
        }

        private static void Add(System.Collections.Generic.List<string> parts, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            parts.Add(key + "=" + Quote(value));
        }

        private static string Quote(string value)
        {
            if (value == null)
                return "";

            bool quote = value.Length == 0;
            for (int i = 0; i < value.Length && !quote; i++)
            {
                char c = value[i];
                quote = char.IsWhiteSpace(c) || c == ',' || c == '"' || c == '=';
            }

            if (!quote)
                return value;

            return "\"" + value.Replace("\"", "'") + "\"";
        }

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
                string filteredMessage;
                if (!TryFilterRepeatedLog(kind, code, source, message, out filteredMessage))
                    return;

                EventLogger.Write(kind, "SYSTEM", code, source ?? string.Empty, FormatWithCurrentContext("Log", source, filteredMessage));
            }
            catch
            {
            }
        }

        private static bool TryFilterRepeatedLog(EventKind kind, string code, string source, string message, out string filteredMessage)
        {
            filteredMessage = message ?? string.Empty;

            try
            {
                if (!IsRepeatThrottleCandidate(filteredMessage))
                    return true;

                string key = kind.ToString() + "\n" + (code ?? string.Empty) + "\n" + (source ?? string.Empty) + "\n" + filteredMessage;
                int now = Environment.TickCount;

                lock (_repeatSync)
                {
                    if (_repeatStates.Count > MaxRepeatStateCount)
                        _repeatStates.Clear();

                    RepeatLogState state;
                    if (!_repeatStates.TryGetValue(key, out state))
                    {
                        _repeatStates[key] = new RepeatLogState { LastEmitTick = now };
                        return true;
                    }

                    int elapsedMs = unchecked(now - state.LastEmitTick);
                    if (elapsedMs < RepeatLogSummaryIntervalMs)
                    {
                        state.SuppressedCount++;
                        return false;
                    }

                    int suppressed = state.SuppressedCount;
                    state.LastEmitTick = now;
                    state.SuppressedCount = 0;

                    if (suppressed > 0)
                        filteredMessage = filteredMessage + " [repeat suppressed: " + suppressed + ", intervalMs=" + elapsedMs + "]";

                    return true;
                }
            }
            catch
            {
                filteredMessage = message ?? string.Empty;
                return true;
            }
        }

        private static bool IsRepeatThrottleCandidate(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            if (Has(message, "- Failed") ||
                Has(message, "- Stopped") ||
                Has(message, "- Alarm") ||
                Has(message, "- Start") ||
                Has(message, "- Ok"))
            {
                return false;
            }

            return Has(message, "- Wait") ||
                   Has(message, " - Wait") ||
                   Has(message, "- Check") ||
                   Has(message, " - Check") ||
                   Has(message, "Gate") ||
                   Has(message, "Pending");
        }

        private sealed class RepeatLogState
        {
            public int LastEmitTick;
            public int SuppressedCount;
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
