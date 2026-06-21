using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class FrontPickerSequence : UnitSequenceBase
    {
        private PickerProcessSequence _stepSequence;

        public FrontPickerSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.PickerFront, "FrontPicker")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await WaitForPickerWorkAsync(ct).ConfigureAwait(false);

                    PickerSequenceOptions options = BuildSequenceOptions();
                    int result = await new PickerProcessSequence(Context, PickerSequenceSide.Front)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);
                    if (result != 0)
                        throw new System.InvalidOperationException(
                            SequenceFailureStore.AppendRecentDetail(
                                "FrontPicker 자동 시퀀스 실패. result=" + result,
                                "FrontPicker",
                                "FRONT-PICKER-SEQUENCE"));

                    Context.StopIfCycleStopRequested("FrontPickerSequence.ProcessComplete");
                }
            }
            catch (System.OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "FrontPicker 자동 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                WriteLog("ExecuteAutoAsync", "FrontPicker 자동 시퀀스 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                if (_stepSequence == null || _stepSequence.IsComplete)
                    _stepSequence = new PickerProcessSequence(Context, PickerSequenceSide.Front);

                PickerSequenceOptions options = BuildSequenceOptions();
                await RunStepSequenceAsync(ct, options).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "FrontPicker 수동/스텝 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                WriteLog("ExecuteStepAsync", "FrontPicker 수동/스텝 시퀀스 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private PickerSequenceOptions BuildSequenceOptions()
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = Mode;
            options.SimulateVisionResult = IsSimulationOrDryRun();
            return options;
        }

        private bool IsSimulationOrDryRun()
        {
            try
            {
                if (QMC.CDT320.AppSettingsStore.Current != null &&
                    (QMC.CDT320.AppSettingsStore.Current.SimulationMode ||
                     QMC.CDT320.AppSettingsStore.Current.DryRunMode))
                    return true;

                if (Context != null && Context.Controller != null && Context.Controller.GlobalDryRun)
                    return true;

                if (Context != null &&
                    Context.Machine != null &&
                    Context.Machine.InputStageUnit != null &&
                    Context.Machine.InputStageUnit.IsInputStageSimulationOrDryRun())
                    return true;

                if (Context != null &&
                    Context.Machine != null &&
                    Context.Machine.PickerFrontUnit != null &&
                    Context.Machine.PickerFrontUnit.Config != null &&
                    Context.Machine.PickerFrontUnit.Config.bDryRun)
                    return true;

                return false;
            }
            catch (System.Exception ex)
            {
                WriteLog("IsSimulationOrDryRun", "FrontPicker 시뮬레이션/드라이런 상태 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private async Task RunStepSequenceAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            try
            {
                int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
                if (result != 0)
                    throw new System.InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "FrontPicker 수동/스텝 시퀀스 실패. result=" + result,
                            "FrontPicker",
                            "FRONT-PICKER-STEP"));

                if (_stepSequence.IsComplete)
                    _stepSequence = null;
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task WaitForPickerWorkAsync(CancellationToken ct)
        {
            try
            {
                while (!HasPickerWork())
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested("FrontPickerSequence.WaitForWork");

                    await Task.Delay(200, ct).ConfigureAwait(false);
                }
            }
            catch (System.OperationCanceledException)
            {
                WriteLog("WaitForPickerWorkAsync", "FrontPicker 작업 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                WriteLog("WaitForPickerWorkAsync", "FrontPicker 작업 대기 중 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private bool HasPickerWork()
        {
            try
            {
                if (HasLoadedDieOnPicker())
                    return true;

                if (!IsFrontPickerEnabled())
                    return false;

                bool inputStageReady = Context != null &&
                                       Context.Bus != null &&
                                       Context.Bus.IsSet("InputStageReady");

                if (!inputStageReady)
                    return false;

                return MaterialStateService.HasReadyInputStagePickTarget();
            }
            catch (System.Exception ex)
            {
                WriteLog("HasPickerWork", "FrontPicker 작업 조건 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool IsFrontPickerEnabled()
        {
            try
            {
                return Context != null &&
                       Context.Machine != null &&
                       Context.Machine.PickerFrontUnit != null &&
                       Context.Machine.PickerFrontUnit.Config != null &&
                       Context.Machine.PickerFrontUnit.Config.UseUnit;
            }
            catch (System.Exception ex)
            {
                WriteLog("IsFrontPickerEnabled", "FrontPicker 사용 여부 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool HasLoadedDieOnPicker()
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerFront, pickerNo) != null)
                    return true;
            }
            return false;
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FrontPickerSequence log failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
