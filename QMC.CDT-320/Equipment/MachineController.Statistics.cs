using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        public void RecordAutoDiePlacedForStats(BinSide side)
        {
            try
            {
                if (ActiveSequenceRunMode != QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    return;

                long cycleMs;
                lock (_productionStatsLock)
                {
                    if (_autoProductionStopwatch == null)
                        _autoProductionStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    cycleMs = _autoProductionStopwatch.ElapsedMilliseconds;
                    if (cycleMs <= 0)
                        cycleMs = 1;
                    _autoProductionStopwatch.Restart();
                }

                int good = side == BinSide.Good ? 1 : 0;
                int ng = side == BinSide.Ng ? 1 : 0;
                Stats.OnCycleCompleted(1, good, ng, cycleMs);

                // 콜렛 클리닝 "공정 n개마다" 트리거 계수(Die 단위 설정일 때만 누적).
                QMC.CDT320.Sequencing.Calibration.ColletCleaningTriggerService.NotifyDiePlaced();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "RecordAutoDiePlacedForStats",
                    "작업 시간 통계 업데이트 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public void RecordOutputStageProductReceivedForTact(
            BinSide side,
            string dieId,
            int pickerNo,
            OutputStageReceiveTarget receiveTarget)
        {
            try
            {
                TactTimeRecorder recorder = _activeTactTimeRecorder;
                if (recorder == null || object.ReferenceEquals(recorder, NullTactTimeRecorder.Instance))
                    return;

                DateTime now = DateTime.Now;
                DateTime previousAt;
                BinSide? previousSide;
                lock (_outputReceiveTactLock)
                {
                    previousAt = _lastOutputReceiveTactAt;
                    previousSide = _lastOutputReceiveTactSide;
                    _lastOutputReceiveTactAt = now;
                    _lastOutputReceiveTactSide = side;
                }

                if (previousAt == DateTime.MinValue)
                    return;

                long elapsedMs = Math.Max(0, (long)(now - previousAt).TotalMilliseconds);
                string sideName = ResolveOutputReceiveTactSideName(side);
                string previousSideName = previousSide.HasValue
                    ? ResolveOutputReceiveTactSideName(previousSide.Value)
                    : "-";

                recorder.Record(new TactTimeRecord
                {
                    UnitName = "OutputStage",
                    SequenceName = "OutputReceiveTact",
                    ProcessName = "Output Receive TactTime",
                    StepName = sideName,
                    Category = TactTimeCategory.Process,
                    StartedAt = previousAt,
                    EndedAt = now,
                    ElapsedMs = elapsedMs,
                    Result = TactTimeResult.Ok,
                    Detail =
                        "OutputStage가 제품 1개를 받은 간격입니다. side=" + sideName +
                        ", previousSide=" + previousSideName +
                        ", die=" + (dieId ?? "") +
                        ", pickerNo=" + pickerNo +
                        ", outputWafer=" + (receiveTarget != null ? receiveTarget.OutputWaferId : "-") +
                        ", order=" + (receiveTarget != null ? receiveTarget.OrderIndex.ToString() : "-") +
                        ", mapX=" + (receiveTarget != null ? receiveTarget.DieMapX.ToString() : "-") +
                        ", mapY=" + (receiveTarget != null ? receiveTarget.DieMapY.ToString() : "-")
                });
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "RecordOutputStageProductReceivedForTact",
                    "Output 수령 택타임 기록 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public void RecordInspectionCheckpointForTact(
            string key,
            string processName,
            string stepName,
            string pickerSide,
            string dieId,
            int pickerNo,
            string detail)
        {
            try
            {
                TactTimeRecorder recorder = _activeTactTimeRecorder;
                if (recorder == null || object.ReferenceEquals(recorder, NullTactTimeRecorder.Instance))
                    return;

                key = string.IsNullOrWhiteSpace(key) ? processName : key;
                DateTime now = DateTime.Now;
                DateTime previousAt;
                lock (_inspectionTactLock)
                {
                    _lastInspectionTactTimes.TryGetValue(key, out previousAt);
                    _lastInspectionTactTimes[key] = now;
                }

                if (previousAt == DateTime.MinValue)
                    return;

                long elapsedMs = Math.Max(0, (long)(now - previousAt).TotalMilliseconds);
                recorder.Record(new TactTimeRecord
                {
                    UnitName = "Inspection",
                    SequenceName = "InspectionTact",
                    ProcessName = processName ?? "",
                    StepName = stepName ?? "",
                    Category = TactTimeCategory.Process,
                    StartedAt = previousAt,
                    EndedAt = now,
                    ElapsedMs = elapsedMs,
                    Result = TactTimeResult.Ok,
                    Detail =
                        "검사 완료 후 다음 제품 검사 완료까지의 간격입니다. key=" + key +
                        ", side=" + (pickerSide ?? "") +
                        ", die=" + (dieId ?? "") +
                        ", pickerNo=" + pickerNo +
                        (string.IsNullOrWhiteSpace(detail) ? "" : ", " + detail)
                });
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "RecordInspectionCheckpointForTact",
                    "검사 택타임 기록 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void BeginAutoProductionStats()
        {
            try
            {
                string lotId = ResolveProductionStatsLotId();
                int totalDies = ResolveProductionStatsTotalDies();

                lock (_productionStatsLock)
                {
                    _autoProductionStopwatch = System.Diagnostics.Stopwatch.StartNew();
                }

                // 현재 기준: CycleStop 후 같은 LOT 재시작이면 작업 시간 통계를 이어간다.
                if (!Stats.TryResumeLot(lotId))
                    Stats.BeginLot(lotId, totalDies);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "BeginAutoProductionStats",
                    "작업 시간 통계 시작 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EndAutoProductionStats()
        {
            try
            {
                lock (_productionStatsLock)
                {
                    if (_autoProductionStopwatch != null)
                    {
                        _autoProductionStopwatch.Stop();
                        _autoProductionStopwatch = null;
                    }
                }

                Stats.EndLot();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "EndAutoProductionStats",
                    "작업 시간 통계 종료 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private string ResolveProductionStatsLotId()
        {
            return MaterialStateService.GetProductionLotId();
        }

        private int ResolveProductionStatsTotalDies()
        {
            try
            {
                if (LotStorage.ActiveLot != null && LotStorage.ActiveLot.TotalDies > 0)
                    return LotStorage.ActiveLot.TotalDies;

                MaterialSnapshot state = MaterialStorage.State;
                if (state != null && state.Dies != null)
                {
                    int count = 0;
                    foreach (DieMaterial die in state.Dies)
                    {
                        if (die != null && die.IsInputTarget)
                            count++;
                    }

                    if (count > 0)
                        return count;
                }
            }
            catch
            {
            }

            return 0;
        }

        private TactTimeRecorder CreateTactTimeRecorder(QMC.CDT320.Sequencing.SequenceRunOptions options)
        {
            try
            {
                var memorySink = new MemoryTactTimeSink();
                _tactTimeMemorySink = memorySink;

                string root = System.IO.Path.Combine(QMC.Common.Logging.EventLogger.LogRoot, "TactTime");
                var sinks = new ITactTimeSink[]
                {
                    memorySink,
                    new CsvTactTimeSink(root)
                };

                return new TactTimeRecorder(
                    "CDT-320",
                    ResolveTactProjectName(),
                    ResolveTactLotId(),
                    options != null ? options.Mode.ToString() : "",
                    sinks);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "TactTime",
                    "택타임 계측기 생성 실패. Null 계측기로 대체합니다. error=" + ex.Message + " - Failed");
                return NullTactTimeRecorder.Instance;
            }
            finally
            {
            }
        }

        private void ResetOutputReceiveTactTimeState()
        {
            lock (_outputReceiveTactLock)
            {
                _lastOutputReceiveTactAt = DateTime.MinValue;
                _lastOutputReceiveTactSide = null;
            }
        }

        private void ResetInspectionTactTimeState()
        {
            lock (_inspectionTactLock)
            {
                _lastInspectionTactTimes.Clear();
            }
        }

        private static string ResolveOutputReceiveTactSideName(BinSide side)
        {
            return side == BinSide.Good ? "OK" : "NG";
        }

        private string ResolveTactProjectName()
        {
            try
            {
                string name = RecipeStore.GetLastProjectName();
                return string.IsNullOrWhiteSpace(name) ? "" : System.IO.Path.GetFileNameWithoutExtension(name);
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private string ResolveTactLotId()
        {
            return MaterialStateService.GetProductionLotId();
        }

    }
}
