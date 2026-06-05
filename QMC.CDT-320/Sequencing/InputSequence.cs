using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Bin;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    public class InputSequence : UnitSequenceBase
    {
        public InputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "InputLoader")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                int result = await ExecuteMappingFirstAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input auto sequence failed at cassette mapping. result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "Input auto sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-EX", "InputSequence", "Input auto sequence failed: " + ex.Message);
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
                int result = await ExecuteMappingFirstAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input step sequence failed at cassette mapping. result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "Input step sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-STEP-EX", "InputSequence", "Input step sequence failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteMappingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                var sequence = new InputCassetteSequence(Context);
                return await sequence.RunMappingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteMappingAsync", "Input cassette mapping sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MAP-EX", "InputSequence", "Input cassette mapping sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCassetteLoadingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                var sequence = new InputCassetteSequence(Context);
                return await sequence.RunLoadingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCassetteLoadingAsync", "Input cassette loading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-LOAD-EX", "InputSequence", "Input cassette loading sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCassetteUnloadingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                var sequence = new InputCassetteSequence(Context);
                return await sequence.RunUnloadingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCassetteUnloadingAsync", "Input cassette unloading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-UNLOAD-EX", "InputSequence", "Input cassette unloading sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteMappingFirstAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT-LOADER] Input cassette mapping start");
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping requested as first input sequence step. - Start");

                int result = await ExecuteMappingAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (result != 0)
                    return Fail("SEQ-IN-MAPPING", "InputLoaderSequence", "Input cassette mapping failed. result=" + result);

                Context.Bus.Set("InputCassetteMapped");
                LogPublic("[UNIT-INPUT-LOADER] Input cassette mapping complete");
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping completed as first input sequence step. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MAPPING-EX", "InputLoaderSequence", "Input cassette mapping exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteLoadOnceAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer start");
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer requested. - Start");

                bool ok = await Context.Controller.LoadNextWaferAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (!ok)
                    return Fail("SEQ-INLOAD", "InputLoaderSequence", "InputLoader failed to load next wafer.");

                Context.Bus.Set("InputStageReady");
                LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer complete");
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-INLOAD-EX", "InputLoaderSequence", "LoadNextWafer exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private InputCassetteSequenceOptions BuildCassetteSequenceOptions(bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            try
            {
                var options = InputCassetteSequenceOptions.Default();
                options.FineMove = bFine;
                options.MoveTimeoutMs = moveTimeoutMs;
                options.RunMode = Mode;
                options.StartMode = startMode;
                return options;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private int Fail(string alarmCode, string source, string message)
        {
            try
            {
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                LogPublic("[UNIT-INPUT-LOADER] FAIL " + alarmCode + " - " + message);
            }
            catch (Exception ex)
            {
                WriteLog(source, "Failure handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private void LogPublic(string message)
        {
            try
            {
                Context.LogPublic(message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
