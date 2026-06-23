using System;
using QMC.Common;

namespace QMC.CDT320.Stats
{
    /// <summary>
    /// 오토 사이클/상태 전이로부터 생산 통계(사이클 타임·UPH·가동/정지 시간·MTBF/MTTR)를
    /// 계산하는 단일 엔진입니다.
    /// <para>
    /// 계산은 모두 이 엔진(시퀀스/상태 스레드)에서 수행하고, 갱신마다 불변
    /// <see cref="ProductionStatsSnapshot"/>을 새로 만들어 <c>volatile</c> 필드에 통째로 교체합니다.
    /// UI는 <see cref="GetSnapshot"/>으로 그 참조 하나만 lock 없이 읽으므로 락 경합이 0입니다.
    /// </para>
    /// <para>이 엔진은 UI 타입(Control/Form)을 절대 참조하지 않습니다.</para>
    /// </summary>
    public sealed class ProductionStatsEngine
    {
        // CYCLE TIME Rolling 윈도우(다이당 ms 표본 개수).
        private const int RollingWindow = 20;

        private readonly object _sync = new object();
        private volatile ProductionStatsSnapshot _current = ProductionStatsSnapshot.Empty;

        // 카운트 누적.
        private int _processedDies;
        private int _goodCount;
        private int _ngCount;

        // CYCLE TIME Rolling 순환 버퍼(다이당 ms). O(1) 갱신을 위해 합계를 함께 유지.
        private readonly long[] _ring = new long[RollingWindow];
        private int _ringIndex;
        private int _ringCount;
        private long _ringSum;
        private double _cycleMsInstant;

        // 상태별 확정(완료된 구간) 누적 시간.
        private TimeSpan _upTime;
        private TimeSpan _normalDownTime;
        private TimeSpan _errorDownTime;
        private TimeSpan _recoveryTime;
        private TimeSpan _lastContUp;

        // 현재 상태와 그 진입 시각(in-progress 구간 산출용).
        private EquipmentStatus _currentState = EquipmentStatus.Idle;
        private DateTime _stateEnterUtc = DateTime.UtcNow;

        // 부하 시간(사이클 Start 기준).
        private bool _lotStarted;
        private DateTime _loadStartUtc;
        private DateTime? _loadEndUtc;

        // 연속 가동/이상 복귀 추적.
        private DateTime _contUpStartUtc;
        private DateTime _recoveryStartUtc;
        private bool _afterAlarm;

        private int _errorCount;
        private string _activeLotId = string.Empty;

        /// <summary>UI가 호출하는 lock-free 스냅샷 읽기입니다.</summary>
        public ProductionStatsSnapshot GetSnapshot()
        {
            return _current;
        }

        /// <summary>사이클 Start 시점에 호출합니다. 작업 변수를 리셋하고 부하 시간을 시작합니다.</summary>
        public void BeginLot(string lotId, int totalDies)
        {
            try
            {
                lock (_sync)
                {
                    DateTime now = DateTime.UtcNow;

                    _processedDies = 0;
                    _goodCount = 0;
                    _ngCount = 0;

                    Array.Clear(_ring, 0, _ring.Length);
                    _ringIndex = 0;
                    _ringCount = 0;
                    _ringSum = 0;
                    _cycleMsInstant = 0;

                    _upTime = TimeSpan.Zero;
                    _normalDownTime = TimeSpan.Zero;
                    _errorDownTime = TimeSpan.Zero;
                    _recoveryTime = TimeSpan.Zero;
                    _lastContUp = TimeSpan.Zero;

                    _currentState = EquipmentStatus.Ready;
                    _stateEnterUtc = now;

                    _lotStarted = true;
                    _loadStartUtc = now;
                    _loadEndUtc = null;

                    _contUpStartUtc = now;
                    _recoveryStartUtc = now;
                    _afterAlarm = false;

                    _errorCount = 0;
                    _activeLotId = lotId ?? string.Empty;

                    PublishLocked(now);
                }
            }
            catch (Exception ex)
            {
                Log.Write("WorkStats", "ProductionStatsEngine", "BeginLot failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>사이클 종료(정상/중단/예외) 시 호출합니다. 현재 구간을 마감하고 부하 시간을 확정합니다.</summary>
        public void EndLot()
        {
            try
            {
                lock (_sync)
                {
                    DateTime now = DateTime.UtcNow;

                    // 진행 중이던 현재 구간을 확정 버킷에 적립한다.
                    AccumulateCurrentSegmentLocked(_currentState, now);
                    if (_currentState == EquipmentStatus.AutoRunning)
                        _lastContUp = ClampNonNegative(now - _contUpStartUtc);

                    _loadEndUtc = now;
                    _stateEnterUtc = now;

                    PublishLocked(now);
                }
            }
            catch (Exception ex)
            {
                Log.Write("WorkStats", "ProductionStatsEngine", "EndLot failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>1 사이클(picker 수만큼의 다이) 완료 직후 호출합니다.</summary>
        public void OnCycleCompleted(int diesInCycle, int good, int ng, long cycleMs)
        {
            try
            {
                lock (_sync)
                {
                    if (diesInCycle > 0)
                        _processedDies += diesInCycle;
                    if (good > 0)
                        _goodCount += good;
                    if (ng > 0)
                        _ngCount += ng;

                    if (diesInCycle > 0 && cycleMs >= 0)
                    {
                        long perDie = cycleMs / diesInCycle;
                        // 순환 버퍼 O(1) 갱신: 가장 오래된 표본을 새 표본으로 교체.
                        _ringSum -= _ring[_ringIndex];
                        _ring[_ringIndex] = perDie;
                        _ringSum += perDie;
                        _ringIndex = (_ringIndex + 1) % RollingWindow;
                        if (_ringCount < RollingWindow)
                            _ringCount++;
                    }

                    _cycleMsInstant = cycleMs;

                    PublishLocked(DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                Log.Write("WorkStats", "ProductionStatsEngine", "OnCycleCompleted failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>장비 상태가 실제로 변경될 때 호출합니다. 직전 구간 시간을 해당 버킷에 적립합니다.</summary>
        public void OnStateChanged(EquipmentStatus old, EquipmentStatus now, DateTime utcNow)
        {
            try
            {
                lock (_sync)
                {
                    // 직전(old) 상태에 머문 구간을 해당 버킷에 적립한다.
                    AccumulateCurrentSegmentLocked(old, utcNow);

                    // AutoRunning 이탈 → 연속 가동 구간 확정.
                    if (old == EquipmentStatus.AutoRunning)
                        _lastContUp = ClampNonNegative(utcNow - _contUpStartUtc);

                    // Alarm 이탈 → 복귀 시계 시작.
                    if (old == EquipmentStatus.Alarm)
                    {
                        _afterAlarm = true;
                        _recoveryStartUtc = utcNow;
                    }

                    // Alarm 진입 → 이상 정지 횟수 증가.
                    if (now == EquipmentStatus.Alarm)
                        _errorCount++;

                    // AutoRunning 진입 → 연속 가동 시작, 직전 Alarm이면 복귀 시간 적립.
                    if (now == EquipmentStatus.AutoRunning)
                    {
                        _contUpStartUtc = utcNow;
                        if (_afterAlarm)
                        {
                            _recoveryTime += ClampNonNegative(utcNow - _recoveryStartUtc);
                            _afterAlarm = false;
                        }
                    }

                    _stateEnterUtc = utcNow;
                    _currentState = now;

                    PublishLocked(utcNow);
                }
            }
            catch (Exception ex)
            {
                Log.Write("WorkStats", "ProductionStatsEngine", "OnStateChanged failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>직전 상태(state)에 머문 (utcNow - 진입시각) 구간을 해당 버킷에 적립한다. (호출 시 lock 보유)</summary>
        private void AccumulateCurrentSegmentLocked(EquipmentStatus state, DateTime utcNow)
        {
            TimeSpan seg = ClampNonNegative(utcNow - _stateEnterUtc);
            switch (state)
            {
                case EquipmentStatus.AutoRunning:
                    _upTime += seg;
                    break;
                case EquipmentStatus.Stopped:
                case EquipmentStatus.CycleStopped:
                    _normalDownTime += seg;
                    break;
                case EquipmentStatus.Alarm:
                    _errorDownTime += seg;
                    break;
            }
        }

        /// <summary>현 작업 변수로 새 불변 스냅샷을 만들어 <c>_current</c>에 원자 교체한다. (호출 시 lock 보유)</summary>
        private void PublishLocked(DateTime utcNow)
        {
            // 확정 버킷 + 진행 중 구간을 합쳐 표시값을 만든다.
            TimeSpan inProgress = ClampNonNegative(utcNow - _stateEnterUtc);

            double up = _upTime.TotalSeconds;
            double normalDown = _normalDownTime.TotalSeconds;
            double errorDown = _errorDownTime.TotalSeconds;

            switch (_currentState)
            {
                case EquipmentStatus.AutoRunning:
                    up += inProgress.TotalSeconds;
                    break;
                case EquipmentStatus.Stopped:
                case EquipmentStatus.CycleStopped:
                    normalDown += inProgress.TotalSeconds;
                    break;
                case EquipmentStatus.Alarm:
                    errorDown += inProgress.TotalSeconds;
                    break;
            }

            double recovery = _recoveryTime.TotalSeconds;

            double load = 0;
            if (_lotStarted)
            {
                DateTime end = _loadEndUtc ?? utcNow;
                load = ClampNonNegative(end - _loadStartUtc).TotalSeconds;
            }

            double contUp = _currentState == EquipmentStatus.AutoRunning
                ? ClampNonNegative(utcNow - _contUpStartUtc).TotalSeconds
                : _lastContUp.TotalSeconds;

            double cycleMsPerDieRolling = _ringCount > 0 ? (double)_ringSum / _ringCount : 0;
            double uphInstant = cycleMsPerDieRolling > 0 ? 3600000.0 / cycleMsPerDieRolling : 0;
            double uphEffective = up > 0 ? _goodCount * 3600.0 / up : 0;
            double uptimeRate = load > 0 ? up / load * 100.0 : 0;
            double mtbf = _errorCount > 0 ? up / _errorCount : 0;
            double mttr = _errorCount > 0 ? errorDown / _errorCount : 0;

            _current = new ProductionStatsSnapshot(
                _processedDies,
                _goodCount,
                _ngCount,
                cycleMsPerDieRolling,
                _cycleMsInstant,
                uphInstant,
                uphEffective,
                load,
                up,
                contUp,
                normalDown,
                errorDown,
                recovery,
                _errorCount,
                mtbf,
                mttr,
                uptimeRate,
                _activeLotId);
        }

        private static TimeSpan ClampNonNegative(TimeSpan ts)
        {
            return ts < TimeSpan.Zero ? TimeSpan.Zero : ts;
        }
    }
}
