using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 선택된 비전 모듈 시퀀스를 병렬 실행/정지하는 조정자. 핸들러 AutoSequenceCoordinator 미러.
    /// Sim 자동 실행(핸들러 TCP 없이 자체 순차 처리)의 진입점이다.
    /// </summary>
    public sealed class AutoSequenceCoordinator
    {
        private readonly VisionSequenceContext _ctx;
        private readonly Dictionary<SequenceModuleKind, ModuleSequenceBase> _sequences
            = new Dictionary<SequenceModuleKind, ModuleSequenceBase>();
        private readonly List<Task> _running = new List<Task>();
        private CancellationTokenSource _cts;

        public AutoSequenceCoordinator(VisionSequenceContext ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

            Register(SequenceModuleKind.WaferVision,      _ctx.Machine.WaferVision      != null ? new WaferVisionSequence(_ctx)      : null);
            Register(SequenceModuleKind.BinVision,        _ctx.Machine.BinVision        != null ? new BinVisionSequence(_ctx)        : null);
            Register(SequenceModuleKind.BottomInspection, _ctx.Machine.BottomInspection != null ? new BottomInspectionSequence(_ctx) : null);
            Register(SequenceModuleKind.TopSideVision,    _ctx.Machine.TopSideVision    != null ? new TopSideVisionSequence(_ctx)    : null);
            Register(SequenceModuleKind.BottomSideVision, _ctx.Machine.BottomSideVision != null ? new BottomSideVisionSequence(_ctx) : null);
        }

        /// <summary>현재 실행 중 여부.</summary>
        public bool IsRunning { get { return _cts != null && !_cts.IsCancellationRequested; } }

        private void Register(SequenceModuleKind kind, ModuleSequenceBase seq)
        {
            if (seq != null) _sequences[kind] = seq;
        }

        /// <summary>선택 모듈 시퀀스를 지정 모드로 시작한다. 이미 실행 중이면 먼저 정지한다.</summary>
        public void Start(SequenceModuleKind selected, SequenceRunMode mode)
        {
            try
            {
                Stop();
                _cts = new CancellationTokenSource();
                CancellationToken ct = _cts.Token;

                foreach (var kv in _sequences)
                {
                    if ((selected & kv.Key) == 0 || kv.Value == null) continue;
                    kv.Value.Configure(mode);
                    ModuleSequenceBase seq = kv.Value;
                    _running.Add(Task.Run(() => seq.RunAsync(ct)));
                }

                _ctx.LogPublic("[SEQ] 자동 시퀀스 시작 — " + selected + " / " + mode);
            }
            catch (Exception ex)
            {
                _ctx.LogPublic("[SEQ] 자동 시퀀스 시작 실패: " + ex.Message);
            }
        }

        /// <summary>실행 중인 모든 시퀀스를 정지한다.</summary>
        public void Stop()
        {
            try { _cts?.Cancel(); }
            catch { }
            _running.Clear();
            _cts = null;
        }

        /// <summary>Manual/Step 모드에서 특정 모듈을 1단계 진행한다.</summary>
        public void StepModule(SequenceModuleKind kind)
        {
            if (_sequences.TryGetValue(kind, out var seq) && seq != null) seq.StepUnit();
        }

        /// <summary>모듈별 메트릭 한 항목(시퀀서 테스트 페이지 표시용).</summary>
        public struct SeqMetric { public SequenceModuleKind Kind; public string Name; public double CycleMs; public long Cycles; }

        /// <summary>현재 등록 시퀀스들의 메트릭 스냅샷.</summary>
        public System.Collections.Generic.List<SeqMetric> Snapshot()
        {
            var list = new System.Collections.Generic.List<SeqMetric>();
            foreach (var kv in _sequences)
                if (kv.Value != null)
                    list.Add(new SeqMetric { Kind = kv.Key, Name = kv.Value.Name, CycleMs = kv.Value.LastCycleMs, Cycles = kv.Value.CycleCount });
            return list;
        }
    }
}
