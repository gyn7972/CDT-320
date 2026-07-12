using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal sealed class AutoCalibrationProgress
    {
        public string CalibrationKind { get; set; }
        public VisionFocusPickerSide Side { get; set; }
        public int PickerNo { get; set; }
        public int CompletedCount { get; set; }
        public int TotalCount { get; set; }
        public string Step { get; set; }
        public string Message { get; set; }
    }

    internal sealed class AutoCalibrationSequence
    {
        private const string ResumeKey = "AutoCalibrationSequence";
        private static readonly int[] PickerOrder = { 4, 3, 2, 1 };
        private static readonly VisionFocusPickerSide[] SideOrder =
        {
            VisionFocusPickerSide.Front,
            VisionFocusPickerSide.Rear
        };

        private readonly MachineSequenceContext _context;
        private readonly AutoCalibrationSettings _settings;
        private PickerPickUpZCalibrationSequence _activePickUpZ;
        private PickerPlaceZCalibrationSequence _activePlaceZ;
        private int _completedCount;
        private int _totalCount;

        public AutoCalibrationSequence(
            MachineSequenceContext context,
            AutoCalibrationSettings settings)
        {
            _context = context;
            _settings = settings != null ? settings.Clone() : new AutoCalibrationSettings();
            _totalCount = (_settings.UseColletCalibration ? 8 : 0) +
                          (_settings.UsePickUpZCalibration ? 8 : 0) +
                          (_settings.UsePlaceZCalibration ? 8 : 0);
        }

        public event Action<AutoCalibrationProgress> ProgressChanged;

        public void RequestImmediateStop(string reason)
        {
            try
            {
                if (_activePickUpZ != null)
                    _activePickUpZ.RequestImmediateStop(reason);
                if (_activePlaceZ != null)
                    _activePlaceZ.RequestImmediateStop(reason);

                SequenceResumeStore.MarkStopped(ResumeKey, reason);
                QMC.Common.Log.Write("Calibration", "SYSTEM", "AutoCalibrationStop",
                    "Auto Calibration 즉시 정지 요청. reason=" + reason + " - Stop");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-STOP-EX",
                    "Auto Calibration 정지 요청 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                if (_context == null || _context.Machine == null || _context.Controller == null)
                    return Fail("AUTO-CAL-NO-CONTEXT", "Auto Calibration 실행 Context가 없습니다.");
                if (_totalCount <= 0)
                    return Fail("AUTO-CAL-NO-SELECTION", "선택된 Auto Calibration 항목이 없습니다.");

                _completedCount = 0;
                SequenceResumeStore.Clear(ResumeKey);
                SequenceResumeStore.MarkRunning(ResumeKey, "CheckReady");
                Publish("AUTO", VisionFocusPickerSide.Front, 0, "CheckReady",
                    "Auto Calibration을 시작합니다.");

                if (_settings.UseColletCalibration)
                {
                    int result = await RunColletCalibrationAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                if (_settings.UsePickUpZCalibration)
                {
                    int result = await RunPickUpZCalibrationAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                if (_settings.UsePlaceZCalibration)
                {
                    int result = await RunPlaceZCalibrationAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                SequenceResumeStore.MarkCompleted(ResumeKey);
                Publish("AUTO", VisionFocusPickerSide.Front, 0, "Complete",
                    "Auto Calibration 전체 작업을 완료했습니다.");
                QMC.Common.Log.Write("Calibration", "SYSTEM", "AutoCalibrationComplete",
                    "Auto Calibration 전체 완료. completed=" + _completedCount +
                    ", total=" + _totalCount + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                SequenceResumeStore.MarkStopped(ResumeKey, "사용자 정지 요청");
                throw;
            }
            catch (SequenceStopException)
            {
                SequenceResumeStore.MarkStopped(ResumeKey, "시퀀스 정지 요청");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("AUTO-CAL-EX", "Auto Calibration 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                _activePickUpZ = null;
                _activePlaceZ = null;
            }
        }

        private async Task<int> RunColletCalibrationAsync(CancellationToken ct)
        {
            foreach (VisionFocusPickerSide side in SideOrder)
            {
                foreach (int pickerNo in PickerOrder)
                {
                    ct.ThrowIfCancellationRequested();
                    string step = BuildStep("Collet", side, pickerNo, "Run");
                    SequenceResumeStore.MarkRunning(ResumeKey, step);
                    Publish("COLLET", side, pickerNo, "Run",
                        "Collet Calibration 실행 중입니다.");

                    PickerSequenceOptions options = CreatePickerOptions(pickerNo);
                    var sequence = new ColletCalibrationSequence(_context, side, pickerNo, false);
                    int result = await sequence.RunAsync(ct, options).ConfigureAwait(false);
                    if (result != 0)
                        return FailTarget("AUTO-CAL-COLLET", "Collet Calibration 실패", side, pickerNo, step, result);

                    step = BuildStep("Collet", side, pickerNo, "ApplyTHome");
                    SequenceResumeStore.MarkRunning(ResumeKey, step);
                    Publish("COLLET", side, pickerNo, "ApplyTHome",
                        "T축 HomeOffset을 저장하고 현재 좌표를 0으로 설정합니다.");
                    string applyMessage;
                    result = ColletCalibrationApplyService.ApplyTHomeOffsetAndZero(
                        _context.Machine,
                        side,
                        pickerNo,
                        out applyMessage);
                    if (result != 0)
                        return FailTarget("AUTO-CAL-COLLET-T-HOME", applyMessage, side, pickerNo, step, result);

                    step = BuildStep("Collet", side, pickerNo, "COC");
                    SequenceResumeStore.MarkRunning(ResumeKey, step);
                    Publish("COLLET", side, pickerNo, "COC",
                        "Collet 회전 중심 Calibration을 실행합니다.");
                    var coc = new ColletRotationCenterCalibrationSequence(
                        _context,
                        side,
                        pickerNo,
                        false);
                    result = await coc.RunAsync(ct, options).ConfigureAwait(false);
                    if (result != 0)
                        return FailTarget("AUTO-CAL-COLLET-COC", "회전 중심 Calibration 실패", side, pickerNo, step, result);

                    string recipeName = _context.Controller.ActiveRecipeName;
                    string recipeMessage;
                    result = ColletCalibrationApplyService.SaveRotationCenterToRecipe(
                        _context.Machine,
                        recipeName,
                        side,
                        pickerNo,
                        coc.RotationCenterMachineX,
                        coc.RotationCenterMachineY,
                        out recipeMessage);
                    if (result != 0)
                        return FailTarget("AUTO-CAL-COLLET-COC-SAVE", recipeMessage, side, pickerNo, step, result);

                    result = await MoveAllUpperAxesToAvoidAsync(side, pickerNo, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    MarkTargetCompleted("COLLET", side, pickerNo);
                }
            }

            return 0;
        }

        private async Task<int> RunPickUpZCalibrationAsync(CancellationToken ct)
        {
            foreach (VisionFocusPickerSide side in SideOrder)
            {
                foreach (int pickerNo in PickerOrder)
                {
                    ct.ThrowIfCancellationRequested();
                    string step = BuildStep("PickUpZ", side, pickerNo, "Run");
                    SequenceResumeStore.MarkRunning(ResumeKey, step);
                    Publish("PICK Z", side, pickerNo, "Run",
                        "PickZ Calibration을 실행합니다.");

                    PickerSequenceOptions options = CreatePickerOptions(pickerNo);
                    _activePickUpZ = new PickerPickUpZCalibrationSequence(_context, side, pickerNo);
                    int result = await _activePickUpZ.RunAsync(ct, options).ConfigureAwait(false);
                    _activePickUpZ = null;
                    if (result != 0)
                        return FailTarget("AUTO-CAL-PICK-Z", "PickZ Calibration 실패", side, pickerNo, step, result);

                    result = SaveActiveRecipe(side, pickerNo, "PickZ");
                    if (result != 0)
                        return result;

                    result = await MoveAllUpperAxesToAvoidAsync(side, pickerNo, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    MarkTargetCompleted("PICK Z", side, pickerNo);
                }
            }

            return 0;
        }

        private async Task<int> RunPlaceZCalibrationAsync(CancellationToken ct)
        {
            foreach (VisionFocusPickerSide side in SideOrder)
            {
                foreach (int pickerNo in PickerOrder)
                {
                    ct.ThrowIfCancellationRequested();
                    string step = BuildStep("PlaceZ", side, pickerNo, "RunGood");
                    SequenceResumeStore.MarkRunning(ResumeKey, step);
                    Publish("PLACE Z", side, pickerNo, "RunGood",
                        "Good Stage 기준 PlaceZ Calibration을 실행합니다.");

                    PickerSequenceOptions options = CreatePickerOptions(pickerNo);
                    _activePlaceZ = new PickerPlaceZCalibrationSequence(
                        _context,
                        side,
                        pickerNo,
                        BinSide.Good);
                    int result = await _activePlaceZ.RunAsync(ct, options).ConfigureAwait(false);
                    _activePlaceZ = null;
                    if (result != 0)
                        return FailTarget("AUTO-CAL-PLACE-Z", "PlaceZ Calibration 실패", side, pickerNo, step, result);

                    result = SaveActiveRecipe(side, pickerNo, "PlaceZ-Good");
                    if (result != 0)
                        return result;

                    result = await MoveAllUpperAxesToAvoidAsync(side, pickerNo, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    MarkTargetCompleted("PLACE Z", side, pickerNo);
                }
            }

            return 0;
        }

        private async Task<int> MoveAllUpperAxesToAvoidAsync(
            VisionFocusPickerSide side,
            int pickerNo,
            CancellationToken ct)
        {
            string step = BuildStep("Safe", side, pickerNo, "AllUpperAxesAvoid");
            SequenceResumeStore.MarkRunning(ResumeKey, step);
            Publish("SAFE", side, pickerNo, "AllUpperAxesAvoid",
                "Input/Output Camera X와 Front/Rear Picker 상부축 전체를 Avoid로 복귀합니다.");

            var safe = new AutoCalibrationSafePositionSequence(_context, side);
            int result = await safe.RunAsync(ct, CreatePickerOptions(pickerNo)).ConfigureAwait(false);
            if (result != 0)
                return FailTarget("AUTO-CAL-SAFE-POS", "상부축 전체 Avoid 복귀 실패", side, pickerNo, step, result);

            return 0;
        }

        private int SaveActiveRecipe(VisionFocusPickerSide side, int pickerNo, string kind)
        {
            string recipeName = _context.Controller.ActiveRecipeName;
            if (string.IsNullOrWhiteSpace(recipeName) || !_context.Machine.SaveRecipe(recipeName))
            {
                return Fail("AUTO-CAL-RECIPE-SAVE",
                    kind + " 결과의 활성 Recipe 저장에 실패했습니다. recipe=" +
                    (recipeName ?? string.Empty) + ", side=" + side + ", pickerNo=" + pickerNo);
            }

            if (!_context.Machine.SaveSettings())
            {
                return Fail("AUTO-CAL-SETTING-SAVE",
                    kind + " 결과의 장비 설정 저장에 실패했습니다. side=" + side +
                    ", pickerNo=" + pickerNo);
            }

            return 0;
        }

        private PickerSequenceOptions CreatePickerOptions(int pickerNo)
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Restart;
            options.PickerNo = pickerNo;
            options.RestrictToPickerNo = pickerNo;
            return options;
        }

        private void MarkTargetCompleted(string kind, VisionFocusPickerSide side, int pickerNo)
        {
            _completedCount++;
            string completedStep = BuildStep(kind, side, pickerNo, "Complete");
            SequenceResumeStore.MarkStepCompleted(ResumeKey, completedStep, completedStep);
            Publish(kind, side, pickerNo, "Complete", kind + " 완료");
        }

        private int FailTarget(
            string alarmCode,
            string cause,
            VisionFocusPickerSide side,
            int pickerNo,
            string step,
            int result)
        {
            string message = cause + ". side=" + side + ", pickerNo=" + pickerNo +
                             ", step=" + step + ", result=" + result;
            SequenceResumeStore.MarkAlarm(ResumeKey, step, message);
            Publish("ERROR", side, pickerNo, step, message);
            return Fail(alarmCode, message);
        }

        private int Fail(string alarmCode, string message)
        {
            EventLogger.Write(EventKind.Alarm, "CAL", alarmCode, message);
            QMC.Common.Log.Write("Calibration", "SYSTEM", alarmCode, message + " - Failed");
            return -1;
        }

        private void Publish(
            string kind,
            VisionFocusPickerSide side,
            int pickerNo,
            string step,
            string message)
        {
            Action<AutoCalibrationProgress> handler = ProgressChanged;
            if (handler == null)
                return;

            handler(new AutoCalibrationProgress
            {
                CalibrationKind = kind,
                Side = side,
                PickerNo = pickerNo,
                CompletedCount = _completedCount,
                TotalCount = _totalCount,
                Step = step,
                Message = message
            });
        }

        private static string BuildStep(
            string kind,
            VisionFocusPickerSide side,
            int pickerNo,
            string step)
        {
            return kind + ":" + side + ":" + pickerNo + ":" + step;
        }
    }
}
