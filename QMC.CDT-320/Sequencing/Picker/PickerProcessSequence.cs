using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerProcessSequence : PickerSequenceBase<PickerProcessStep>
    {
        private PickerPickUpSequence _pickUpSequence;
        private PickerBottomInspectionSequence _bottomInspectionSequence;
        private PickerSideInspectionSequence _sideInspectionSequence;
        private PickerPlaceSequence _placeSequence;

        public PickerProcessSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Process, side == PickerSequenceSide.Front ? "FrontPickerSequence" : "RearPickerSequence")
        {
            CurrentStep = PickerProcessStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerProcessStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                if (_pickUpSequence != null)
                    _pickUpSequence.Abort();
                if (_bottomInspectionSequence != null)
                    _bottomInspectionSequence.Abort();
                if (_sideInspectionSequence != null)
                    _sideInspectionSequence.Abort();
                if (_placeSequence != null)
                    _placeSequence.Abort();

                _pickUpSequence = null;
                _bottomInspectionSequence = null;
                _sideInspectionSequence = null;
                _placeSequence = null;
                CurrentStep = PickerProcessStep.Complete;
            }
            catch
            {
            }
            finally
            {
            }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                if (IsStepRunMode())
                {
                    return await ExecuteSingleProcessStepAsync(ct).ConfigureAwait(false);
                }

                return await ExecuteProcessUntilCompleteAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-EX", Name, "Picker process failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsStepRunMode()
        {
            return Options != null && Options.RunMode != SequenceRunMode.Auto;
        }

        private async Task<int> ExecuteSingleProcessStepAsync(CancellationToken ct)
        {
            if (CurrentStep == PickerProcessStep.Complete)
                return 0;

            ct.ThrowIfCancellationRequested();
            return await ExecuteStepAsync(ct).ConfigureAwait(false);
        }

        private async Task<int> ExecuteProcessUntilCompleteAsync(CancellationToken ct)
        {
            while (CurrentStep != PickerProcessStep.Complete)
            {
                ct.ThrowIfCancellationRequested();

                int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                // 유닛 확인
                case PickerProcessStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                // 픽업 업 실행
                case PickerProcessStep.RunPickUp:
                    return RunPickUpAsync(ct);

                // 하단 검사 실행
                case PickerProcessStep.RunBottomInspection:
                    return RunBottomInspectionAsync(ct);

                // 사이드 검사 실행
                case PickerProcessStep.RunSideInspection:
                    return RunSideInspectionAsync(ct);

                // 플레이스 실행
                case PickerProcessStep.RunPlace:
                    return RunPlaceAsync(ct);

                default:
                    return Task.FromResult(Fail("PICKER-PROCESS-STEP", Name, "Unsupported picker process step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker == null)
                return Fail("PICKER-FRONT-NO-UNIT", Name, "FrontPickerUnit is null.");

            if (Side == PickerSequenceSide.Rear && RearPicker == null)
                return Fail("PICKER-REAR-NO-UNIT", Name, "RearPickerUnit is null.");

            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerProcessSequence", Name + " skipped because picker side is disabled. side=" + Side + " - Check");
                CurrentStep = PickerProcessStep.Complete;
                return 0;
            }

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-AXIS-NOT-READY", Name, "Picker 축 준비 상태가 아닙니다. side=" + Side + ", reason=" + axisReason);

            return ResolveNextProcessStepFromMaterial();
        }

        private int ResolveNextProcessStepFromMaterial()
        {
            try
            {
                List<int> enabled = BuildEnabledPickerIndexes();
                if (enabled == null || enabled.Count == 0)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " 사용 설정된 Picker가 없어 공정을 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = PickerProcessStep.Complete;
                    return 0;
                }

                int occupiedCount = 0;
                int bottomRequiredCount = 0;
                int sideRequiredCount = 0;
                int placeReadyCount = 0;

                for (int i = 0; i < enabled.Count; i++)
                {
                    int pickerIndex = enabled[i];
                    int pickerNo = ToPickerNo(pickerIndex);
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                        continue;

                    occupiedCount++;

                    bool bottomDone = HasInspectionResult(die, "Bottom");
                    bool side0Done = HasInspectionResult(die, "Side0");
                    bool side90Done = HasInspectionResult(die, "Side90");

                    if (!bottomDone)
                    {
                        bottomRequiredCount++;
                        continue;
                    }

                    if (!side0Done || !side90Done)
                    {
                        sideRequiredCount++;
                        continue;
                    }

                    placeReadyCount++;
                }

                if (occupiedCount == 0)
                {
                    CurrentStep = PickerProcessStep.RunPickUp;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker에 Die가 없어 PickUp부터 시작합니다. side=" + Side +
                        ", enabledPickerCount=" + enabled.Count + " - Check");
                    return 0;
                }

                if (bottomRequiredCount > 0)
                {
                    CurrentStep = PickerProcessStep.RunBottomInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker가 Die를 가지고 있어 PickUp을 건너뛰고 Bottom 검사부터 재개합니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", bottomRequiredCount=" + bottomRequiredCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount + " - Check");
                    return 0;
                }

                if (sideRequiredCount > 0)
                {
                    CurrentStep = PickerProcessStep.RunSideInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Bottom 검사가 완료된 Die가 있어 Side 검사부터 재개합니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount + " - Check");
                    return 0;
                }

                CurrentStep = PickerProcessStep.RunPlace;
                WriteLog("PickerProcessSequence",
                    Name + " Picker Die 검사 상태가 Place 가능 상태라 Place부터 재개합니다. side=" + Side +
                    ", occupiedPickerCount=" + occupiedCount +
                    ", placeReadyCount=" + placeReadyCount + " - Check");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-ROUTE", Name,
                    "Picker 공정 시작 스텝 판단 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool HasInspectionResult(DieMaterial die, string inspectionType)
        {
            try
            {
                if (die == null || die.Inspections == null || string.IsNullOrWhiteSpace(inspectionType))
                    return false;

                for (int i = 0; i < die.Inspections.Count; i++)
                {
                    DieInspectionRecord record = die.Inspections[i];
                    if (record == null)
                        continue;

                    if (!string.Equals(record.InspectionType, inspectionType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return record.Result != MaterialInspectionResult.Unknown;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> RunPickUpAsync(CancellationToken ct)
        {
            if (_pickUpSequence == null || _pickUpSequence.IsComplete)
                _pickUpSequence = new PickerPickUpSequence(Context, Side);

            int result = await _pickUpSequence
                .RunAsync(ct, BuildChildSequenceOptions())
                .ConfigureAwait(false);

            if (result != 0)
                return result;

            if (_pickUpSequence.IsComplete)
            {
                _pickUpSequence = null;
                CurrentStep = PickerProcessStep.RunBottomInspection;
            }

            return 0;
        }

        private async Task<int> RunBottomInspectionAsync(CancellationToken ct)
        {
            if (_bottomInspectionSequence == null || _bottomInspectionSequence.IsComplete)
                _bottomInspectionSequence = new PickerBottomInspectionSequence(Context, Side);

            int result = await _bottomInspectionSequence
                .RunAsync(ct, BuildChildSequenceOptions())
                .ConfigureAwait(false);

            if (result != 0)
                return result;

            if (_bottomInspectionSequence.IsComplete)
            {
                _bottomInspectionSequence = null;
                CurrentStep = PickerProcessStep.RunSideInspection;
            }

            return 0;
        }

        private async Task<int> RunSideInspectionAsync(CancellationToken ct)
        {
            if (_sideInspectionSequence == null || _sideInspectionSequence.IsComplete)
                _sideInspectionSequence = new PickerSideInspectionSequence(Context, Side);

            int result = await _sideInspectionSequence
                .RunAsync(ct, BuildChildSequenceOptions())
                .ConfigureAwait(false);

            if (result != 0)
                return result;

            if (_sideInspectionSequence.IsComplete)
            {
                _sideInspectionSequence = null;
                CurrentStep = PickerProcessStep.RunPlace;
            }

            return 0;
        }

        private async Task<int> RunPlaceAsync(CancellationToken ct)
        {
            if (_placeSequence == null || _placeSequence.IsComplete)
                _placeSequence = new PickerPlaceSequence(Context, Side);

            int result = await _placeSequence
                .RunAsync(ct, BuildChildSequenceOptions())
                .ConfigureAwait(false);

            if (result != 0)
                return result;

            if (_placeSequence.IsComplete)
            {
                _placeSequence = null;
                CurrentStep = PickerProcessStep.Complete;
            }

            return 0;
        }

        private PickerSequenceOptions BuildChildSequenceOptions()
        {
            PickerSequenceOptions source = Options ?? PickerSequenceOptions.Default();
            return new PickerSequenceOptions
            {
                RunMode = source.RunMode,
                StartMode = source.StartMode,
                FineMove = source.FineMove,
                MoveTimeoutMs = source.MoveTimeoutMs,
                ResourceTimeoutMs = source.ResourceTimeoutMs,
                PickerNo = source.PickerNo,
                VisionRetryCount = source.VisionRetryCount,
                SimulateVisionResult = source.SimulateVisionResult
            };
        }
    }
}

