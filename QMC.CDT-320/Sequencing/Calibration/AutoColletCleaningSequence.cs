using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal sealed class ColletCleaningProgress
    {
        public VisionFocusPickerSide Side { get; set; }
        public int CompletedSideCount { get; set; }
        public int TotalSideCount { get; set; }
        public string Step { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 콜렛 클리닝 실행기.
    /// Front/Rear는 상대 Picker Avoid 인터락 때문에 동시에 작업할 수 없으므로 Side 순서대로 실행하고,
    /// 각 Side 안에서는 ColletCleaningSequence가 "전부 클린 -> 전부 검사 -> NG만 재시도"로 묶어 처리한다.
    /// 수동(다이얼로그)과 자동(트리거) 실행이 이 클래스를 공유한다.
    /// </summary>
    internal sealed class AutoColletCleaningSequence
    {
        private const string ResumeKey = "AutoColletCleaningSequence";

        private static readonly PickerSequenceSide[] SideOrder =
        {
            PickerSequenceSide.Front,
            PickerSequenceSide.Rear
        };

        private readonly MachineSequenceContext _context;
        private readonly ColletCleaningSettings _settings;
        private readonly SequenceRunMode _runMode;
        private readonly List<ColletCleaningItem> _allItems = new List<ColletCleaningItem>();

        public AutoColletCleaningSequence(
            MachineSequenceContext context,
            ColletCleaningSettings settings,
            SequenceRunMode runMode)
        {
            _context = context;
            _settings = settings != null ? settings.Clone() : new ColletCleaningSettings();
            _runMode = runMode;
        }

        public event Action<ColletCleaningProgress> ProgressChanged;

        /// <summary>NG Stage에 Bin이 없어 전체가 스킵된 경우 true(알람 아님).</summary>
        public bool SkippedNoBin { get; private set; }

        public string SkipReason { get; private set; } = string.Empty;

        public IReadOnlyList<ColletCleaningItem> Items { get { return _allItems; } }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                if (_context == null || _context.Machine == null)
                    return Fail("COLLET-CLEAN-NO-CONTEXT", "콜렛 클리닝 실행 Context가 없습니다.");

                _settings.EnsureObjects();
                if (!_settings.HasAnySelection())
                    return Fail("COLLET-CLEAN-NO-SELECTION", "선택된 콜렛이 없습니다.");

                _allItems.Clear();
                SkippedNoBin = false;
                SkipReason = string.Empty;

                List<PickerSequenceSide> sides = SideOrder
                    .Where(HasSelectionForSide)
                    .ToList();
                if (sides.Count == 0)
                    return Fail("COLLET-CLEAN-NO-SELECTION", "선택된 콜렛이 없습니다.");

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningRun",
                    "콜렛 클리닝 실행을 시작합니다. sides=" +
                    string.Join(",", sides.Select(s => s.ToString()).ToArray()) +
                    ", mode=" + _runMode + " - Start");

                int completed = 0;
                bool anySkipped = false;
                foreach (PickerSequenceSide side in sides)
                {
                    ct.ThrowIfCancellationRequested();
                    _context.StopIfCycleStopRequested(ResumeKey + ":" + side);

                    RaiseProgress(side, completed, sides.Count, "Start",
                        side + " 콜렛 클리닝을 시작합니다.");

                    var sequence = new ColletCleaningSequence(_context, side, _settings);
                    var options = PickerSequenceOptions.Default();
                    options.RunMode = _runMode;
                    options.StartMode = SequenceStartMode.Restart;

                    int result = await sequence.RunAsync(ct, options).ConfigureAwait(false);
                    _allItems.AddRange(sequence.Items);

                    if (sequence.SkippedNoBin)
                    {
                        anySkipped = true;
                        SkipReason = sequence.SkipReason;
                    }

                    if (result != 0)
                    {
                        RaiseProgress(side, completed, sides.Count, "Failed",
                            side + " 콜렛 클리닝이 실패했습니다. result=" + result);
                        SaveHistory();
                        return result;
                    }

                    completed++;
                    RaiseProgress(side, completed, sides.Count, "Complete",
                        side + " 콜렛 클리닝을 완료했습니다.");
                }

                SkippedNoBin = anySkipped && _allItems.Count == 0;
                SaveHistory();

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningRun",
                    "콜렛 클리닝 실행을 완료했습니다. sides=" + sides.Count +
                    ", collets=" + _allItems.Count +
                    ", ok=" + _allItems.Count(i => i.Result == ColletCleaningResult.Ok) +
                    ", skippedNoBin=" + SkippedNoBin + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                SaveHistory();
                throw;
            }
            catch (SequenceStopException)
            {
                SaveHistory();
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CLEAN-RUN-EX",
                    "콜렛 클리닝 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool HasSelectionForSide(PickerSequenceSide side)
        {
            VisionFocusPickerSide visionSide = side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;

            for (int colletNo = 1; colletNo <= 4; colletNo++)
            {
                if (_settings.IsColletSelected(visionSide, colletNo))
                    return true;
            }

            return false;
        }

        /// <summary>실행 결과를 CalibrationData의 콜렛 클리닝 이력에 저장한다.</summary>
        private void SaveHistory()
        {
            try
            {
                if (_allItems.Count == 0)
                    return;

                CalibrationData data = CalibrationDataStore.LoadOrCreate();
                if (data == null)
                    return;

                data.EnsureObjects();
                foreach (ColletCleaningItem item in _allItems)
                {
                    ColletCleaningResult result = item.Result;
                    if (result == ColletCleaningResult.None)
                        result = item.Inspected
                            ? (item.InspectionOk ? ColletCleaningResult.Ok : ColletCleaningResult.Ng)
                            : ColletCleaningResult.Skipped;

                    data.ColletCleaningHistory.Update(
                        item.Side,
                        item.ColletNo,
                        result,
                        item.RetryUsed,
                        item.Message);
                }

                data.Touch("ColletCleaning");
                CalibrationDataStore.Save(data);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-HISTORY",
                    "콜렛 클리닝 이력 저장 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private void RaiseProgress(
            PickerSequenceSide side,
            int completed,
            int total,
            string step,
            string message)
        {
            try
            {
                Action<ColletCleaningProgress> handler = ProgressChanged;
                if (handler == null)
                    return;

                handler(new ColletCleaningProgress
                {
                    Side = side == PickerSequenceSide.Front
                        ? VisionFocusPickerSide.Front
                        : VisionFocusPickerSide.Rear,
                    CompletedSideCount = completed,
                    TotalSideCount = total,
                    Step = step,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-PROGRESS",
                    "콜렛 클리닝 진행률 통지 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int Fail(string code, string message)
        {
            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningRun",
                message + " code=" + code + " - Failed");
            EventLogger.Write(EventKind.Alarm, "CAL", code, message);
            return -1;
        }
    }
}
