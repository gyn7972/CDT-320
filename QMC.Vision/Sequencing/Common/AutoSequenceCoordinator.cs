using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 비전 시퀀스 조정자 — "도구(Finder/Inspector) 단위" 실행 모델.
    /// 모듈 안의 각 도구가 개별 시퀀서(<see cref="ToolSequence"/>)이며, 사용자는
    ///   · 모듈 전체(선택 모듈의 모든 도구를 순서대로) 또는
    ///   · 특정 도구 하나
    /// 를 Auto 연속/Step 1회로 구동할 수 있다. 순서 정의는 <see cref="SequenceToolCatalog"/> 단일 소스.
    /// </summary>
    public sealed class AutoSequenceCoordinator
    {
        private readonly VisionSequenceContext _ctx;
        // 모듈별 도구 시퀀서 목록(카탈로그 순서, 모듈에 실제 존재하는 도구만).
        private readonly Dictionary<SequenceModuleKind, List<ToolSequence>> _tools
            = new Dictionary<SequenceModuleKind, List<ToolSequence>>();
        // StepNextTool 진행 인덱스(모듈별).
        private readonly Dictionary<SequenceModuleKind, int> _stepIdx
            = new Dictionary<SequenceModuleKind, int>();
        private readonly List<Task> _running = new List<Task>();
        private CancellationTokenSource _cts;
        private SequenceRunMode _mode = SequenceRunMode.Auto;

        public AutoSequenceCoordinator(VisionSequenceContext ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

            BuildTools(SequenceModuleKind.WaferVision,      _ctx.Machine.WaferVision,      "WaferVision");
            BuildTools(SequenceModuleKind.BinVision,        _ctx.Machine.BinVision,        "BinVision");
            BuildTools(SequenceModuleKind.BottomInspection, _ctx.Machine.BottomInspection, "BottomInspection");
            BuildTools(SequenceModuleKind.TopSideVision,    _ctx.Machine.TopSideVision,    "TopSideVision");
            BuildTools(SequenceModuleKind.BottomSideVision, _ctx.Machine.BottomSideVision, "BottomSideVision");
        }

        private void BuildTools(SequenceModuleKind kind, IVisionModule module, string name)
        {
            if (module == null) return;
            var list = new List<ToolSequence>();
            foreach (var t in SequenceToolCatalog.Tools(kind))
            {
                string cmd = t.Key, id = t.Value;
                bool exists = cmd == "MATCH" ? module.Finders.ContainsKey(id)
                                             : module.Inspectors.ContainsKey(id);
                if (!exists) continue;   // 모듈에 정의되지 않은 도구는 제외(안전)
                list.Add(new ToolSequence(_ctx, kind, module, name, cmd, id));
            }
            if (list.Count > 0) _tools[kind] = list;
        }

        /// <summary>현재 실행 중 여부.</summary>
        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        /// <summary>선택 모듈 전체를 시작 — Auto: 모듈마다 도구를 순서대로 연속 실행(모듈 병렬).
        /// Step/Manual: 태스크 없이 토큰만 준비(도구를 StepTool/StepNextTool 로 한 단계씩 진행).</summary>
        public void Start(SequenceModuleKind selected, SequenceRunMode mode)
        {
            try
            {
                Stop();
                _mode = mode;
                _cts = new CancellationTokenSource();
                CancellationToken ct = _cts.Token;

                foreach (var kv in _tools)
                {
                    if ((selected & kv.Key) == 0) continue;
                    foreach (var ts in kv.Value) ts.Configure(mode);
                    _stepIdx[kv.Key] = 0;
                    if (mode == SequenceRunMode.Auto)
                    {
                        var tools = kv.Value;
                        _running.Add(Task.Run(() => RunModuleLoopAsync(tools, ct)));
                    }
                }

                _ctx.LogPublic("[SEQ] 시퀀스 시작 — " + selected + " / " + mode
                    + (mode == SequenceRunMode.Auto ? "" : " (도구를 '한 단계'로 진행)"));
            }
            catch (Exception ex)
            {
                _ctx.LogPublic("[SEQ] 시퀀스 시작 실패: " + ex.Message);
            }
        }

        /// <summary>특정 도구 하나만 시작 — Auto: 단독 연속, Step/Manual: 토큰만 준비(StepTool 로 진행).</summary>
        public void StartTool(SequenceModuleKind kind, string toolId, SequenceRunMode mode)
        {
            try
            {
                Stop();
                _mode = mode;
                var ts = Find(kind, toolId);
                if (ts == null) { _ctx.LogPublic("[SEQ] 도구 없음: " + kind + "." + toolId); return; }
                ts.Configure(mode);
                _cts = new CancellationTokenSource();
                if (mode == SequenceRunMode.Auto)
                {
                    var ct = _cts.Token;
                    _running.Add(Task.Run(() => ts.RunAutoAsync(ct)));
                }
                _ctx.LogPublic("[SEQ] 도구 시작 — " + ts.Name + " / " + mode
                    + (mode == SequenceRunMode.Auto ? "" : " ('한 단계'로 진행)"));
            }
            catch (Exception ex)
            {
                _ctx.LogPublic("[SEQ] 도구 시작 실패: " + ex.Message);
            }
        }

        // 모듈의 도구들을 순서대로 1회 통과(연속 루프의 한 패스).
        private async Task RunModuleLoopAsync(List<ToolSequence> tools, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var ts in tools)
                    {
                        ct.ThrowIfCancellationRequested();
                        await ts.RunCycleAsync(ct, grab: true).ConfigureAwait(false);
                    }
                    await Task.Delay(_ctx.CycleIntervalMs, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /* 정상 정지 */ }
            catch (Exception ex) { _ctx.LogPublic("[SEQ] 모듈 루프 실패: " + ex.Message); }
        }

        /// <summary>Step/Manual — 지정 도구 1회 실행.</summary>
        public void StepTool(SequenceModuleKind kind, string toolId)
        {
            var ts = Find(kind, toolId);
            if (ts == null) { _ctx.LogPublic("[SEQ] step 도구 없음: " + kind + "." + toolId); return; }
            FireOnce(ts);
        }

        /// <summary>Step/Manual — 모듈 순서상 '다음 도구' 1회 실행(반복 호출 시 순환).</summary>
        public void StepNextTool(SequenceModuleKind kind)
        {
            if (!_tools.TryGetValue(kind, out var list) || list.Count == 0)
            {
                _ctx.LogPublic("[SEQ] step 대상 도구 없음: " + kind);
                return;
            }
            int idx; _stepIdx.TryGetValue(kind, out idx);
            if (idx < 0 || idx >= list.Count) idx = 0;
            var ts = list[idx];
            _stepIdx[kind] = (idx + 1) % list.Count;
            _ctx.LogPublic("[SEQ] 다음 도구 [" + (idx + 1) + "/" + list.Count + "] " + ts.Name);
            FireOnce(ts);
        }

        private void FireOnce(ToolSequence ts)
        {
            CancellationToken ct = _cts?.Token ?? CancellationToken.None;
            _running.Add(Task.Run(() => ts.RunCycleAsync(ct, grab: true)));
        }

        private ToolSequence Find(SequenceModuleKind kind, string toolId)
        {
            if (_tools.TryGetValue(kind, out var list))
                foreach (var ts in list)
                    if (string.Equals(ts.ToolId, toolId, StringComparison.Ordinal)) return ts;
            return null;
        }

        /// <summary>실행 중인 모든 시퀀스를 정지한다.</summary>
        public void Stop()
        {
            var cts = _cts;
            _cts = null;
            try { cts?.Cancel(); } catch { }
            try { cts?.Dispose(); } catch { }
            _running.Clear();
        }

        /// <summary>도구별 메트릭 한 항목(시퀀서 테스트 페이지 표시용).</summary>
        public struct ToolMetric
        {
            public SequenceModuleKind Kind;
            public string ModuleName;
            public string ToolId;
            public string Name;
            public bool IsFinder;
            public double CycleMs;
            public long Cycles;
            public string Status;
            public string LastResult;
        }

        /// <summary>현재 등록 도구들의 메트릭 스냅샷.</summary>
        public List<ToolMetric> Snapshot()
        {
            var list = new List<ToolMetric>();
            foreach (var kv in _tools)
                foreach (var ts in kv.Value)
                    list.Add(new ToolMetric
                    {
                        Kind = kv.Key, ModuleName = ts.ModuleName, ToolId = ts.ToolId, Name = ts.Name,
                        IsFinder = ts.IsFinder, CycleMs = ts.LastCycleMs, Cycles = ts.CycleCount,
                        Status = ts.Status, LastResult = ts.LastResult
                    });
            return list;
        }
    }
}
