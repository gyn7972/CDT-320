using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 단일 producer→consumer 핸드오프 채널. 병렬 시퀀스 간 비동기 티켓 전달.
    /// <para>
    /// 내부적으로 bounded <see cref="BlockingCollection{T}"/> 를 사용하여 backpressure 제공.
    /// (소비자가 느리면 producer 가 Post 에서 대기 → 파이프라인 과적재 방지.)
    /// </para>
    /// </summary>
    public sealed class Handoff<T>
    {
        private readonly BlockingCollection<T> _queue;

        /// <summary>핸드오프 채널 이름.</summary>
        public string Name { get; }

        public Handoff(string name, int boundedCapacity = 8)
        {
            Name = name;
            _queue = new BlockingCollection<T>(boundedCapacity);
        }

        /// <summary>대기 중인 티켓 수.</summary>
        public int Count => _queue.Count;

        /// <summary>티켓 게시 (채널이 가득 차면 공간이 생길 때까지 대기).</summary>
        public void Post(T item, CancellationToken ct = default)
        {
            _queue.Add(item, ct);
        }

        /// <summary>티켓 1개 수신 (없으면 도착할 때까지 대기). 비동기 래퍼.</summary>
        public Task<T> ReceiveAsync(CancellationToken ct)
        {
            return Task.Run(() => _queue.Take(ct), ct);
        }

        /// <summary>티켓 1개 수신 시도 (timeout 내 미도착 시 false).</summary>
        public bool TryReceive(out T item, int timeoutMs, CancellationToken ct)
        {
            return _queue.TryTake(out item, timeoutMs, ct);
        }

        /// <summary>더 이상 producer 가 없음을 표시 (소비자 루프 종료용).</summary>
        public void Complete()
        {
            try { _queue.CompleteAdding(); } catch { }
        }

        /// <summary>producer 가 완료를 표시했는지.</summary>
        public bool IsCompleted => _queue.IsAddingCompleted;
    }

    // ──────────────────────────────────────────
    //  티켓 DTO ? 유닛 간 전달 데이터
    // ──────────────────────────────────────────

    /// <summary>InputLoader → InputStage : 웨이퍼가 교환 위치에 준비됨.</summary>
    public sealed class WaferTicket
    {
        /// <summary>카세트 슬롯 인덱스 (0-base).</summary>
        public int SlotIndex { get; set; }

        /// <summary>웨이퍼 ID (바코드).</summary>
        public string WaferId { get; set; }
    }

    /// <summary>InputStage → TPU : 다음 배치(4 picker) 픽업 대상 다이.</summary>
    public sealed class DieBatchTicket
    {
        /// <summary>사이클 인덱스.</summary>
        public int CycleIdx { get; set; }

        /// <summary>이 배치의 다이 시퀀스 인덱스들.</summary>
        public int[] DieIndices { get; set; }
    }

    /// <summary>TPU → OutputStage : 검사 완료된 다이 Place 요청.</summary>
    public sealed class DiePlaceTicket
    {
        /// <summary>다이 시퀀스 인덱스.</summary>
        public int DieIndex { get; set; }

        /// <summary>Good 판정 여부.</summary>
        public bool IsGood { get; set; }

        /// <summary>Place 보정 X [mm].</summary>
        public double OffX { get; set; }

        /// <summary>Place 보정 Y [mm].</summary>
        public double OffY { get; set; }
    }

    /// <summary>OutputStage → OutputUnloader : 카세트 교체 요청.</summary>
    public sealed class BinChangeTicket
    {
        /// <summary>등급 (Good/NG).</summary>
        public bool IsGood { get; set; }
    }

    /// <summary>
    /// 모든 유닛 간 핸드오프 채널을 모은 시그널 버스.
    /// Coordinator 가 사이클 시작 시 1회 생성하여 ISequenceContext 로 공유.
    /// </summary>
    public sealed class SequenceSignalBus
    {
        /// <summary>InputLoader → InputStage.</summary>
        public Handoff<WaferTicket> LoaderToStage { get; } =
            new Handoff<WaferTicket>("LoaderToStage");

        /// <summary>InputStage → TPU.</summary>
        public Handoff<DieBatchTicket> StageToTpu { get; } =
            new Handoff<DieBatchTicket>("StageToTpu");

        /// <summary>TPU → OutputStage.</summary>
        public Handoff<DiePlaceTicket> TpuToOutStage { get; } =
            new Handoff<DiePlaceTicket>("TpuToOutStage");

        /// <summary>OutputStage → OutputUnloader.</summary>
        public Handoff<BinChangeTicket> OutStageToUnloader { get; } =
            new Handoff<BinChangeTicket>("OutStageToUnloader");

        /// <summary>모든 채널에 완료 표시 (사이클 종료 시).</summary>
        public void CompleteAll()
        {
            LoaderToStage.Complete();
            StageToTpu.Complete();
            TpuToOutStage.Complete();
            OutStageToUnloader.Complete();
        }
    }
}
