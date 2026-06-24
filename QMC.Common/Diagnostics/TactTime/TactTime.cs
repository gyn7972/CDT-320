using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Diagnostics.TactTime
{
    public enum TactTimeCategory
    {
        Run,
        Unit,
        Process,
        Step,
        Motion,
        Vision,
        IO,
        Wait,
        Resource,
        Logic
    }

    public enum TactTimeResult
    {
        Ok,
        Failed,
        Stopped,
        Canceled,
        Skipped
    }

    public sealed class TactTimeRecord
    {
        public string RunId { get; set; } = "";
        public string ParentId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string EquipmentId { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string LotId { get; set; } = "";
        public string Mode { get; set; } = "";
        public string UnitName { get; set; } = "";
        public string SequenceName { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public string StepName { get; set; } = "";
        public TactTimeCategory Category { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public long ElapsedMs { get; set; }
        public TactTimeResult Result { get; set; }
        public string AlarmCode { get; set; } = "";
        public string Detail { get; set; } = "";

        public TactTimeRecord Clone()
        {
            return (TactTimeRecord)MemberwiseClone();
        }
    }

    public interface ITactTimeSink
    {
        void Write(TactTimeRecord record);
    }

    public sealed class MemoryTactTimeSink : ITactTimeSink
    {
        private readonly object _syncRoot = new object();
        private readonly int _capacity;
        private readonly Queue<TactTimeRecord> _records;

        public MemoryTactTimeSink()
            : this(5000)
        {
        }

        public MemoryTactTimeSink(int capacity)
        {
            _capacity = capacity > 0 ? capacity : 5000;
            _records = new Queue<TactTimeRecord>(_capacity);
        }

        public void Write(TactTimeRecord record)
        {
            if (record == null)
                return;

            lock (_syncRoot)
            {
                while (_records.Count >= _capacity)
                    _records.Dequeue();

                _records.Enqueue(record.Clone());
            }
        }

        public IReadOnlyList<TactTimeRecord> Snapshot()
        {
            lock (_syncRoot)
            {
                return new List<TactTimeRecord>(_records).AsReadOnly();
            }
        }
    }

    public sealed class CsvTactTimeSink : ITactTimeSink, IDisposable
    {
        private const string Header =
            "When,RunId,ParentId,CorrelationId,EquipmentId,ProjectName,LotId,Mode,UnitName,SequenceName,ProcessName,StepName,Category,StartedAt,EndedAt,ElapsedMs,Result,AlarmCode,Detail";

        private readonly string _rootPath;
        private readonly BlockingCollection<TactTimeRecord> _queue =
            new BlockingCollection<TactTimeRecord>(new ConcurrentQueue<TactTimeRecord>());
        private readonly Task _writerTask;
        private int _disposed;

        public CsvTactTimeSink(string rootPath)
        {
            _rootPath = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "TactTime")
                : rootPath;
            _writerTask = Task.Factory.StartNew(
                ProcessQueue,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Write(TactTimeRecord record)
        {
            try
            {
                if (record == null || Volatile.Read(ref _disposed) != 0)
                    return;

                _queue.Add(record.Clone());
            }
            catch
            {
            }
            finally
            {
            }
        }

        public void Dispose()
        {
            try
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                _queue.CompleteAdding();
                _writerTask.Wait(1000);
                _queue.Dispose();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ProcessQueue()
        {
            try
            {
                foreach (TactTimeRecord record in _queue.GetConsumingEnumerable())
                    WriteRecord(record);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void WriteRecord(TactTimeRecord record)
        {
            try
            {
                Directory.CreateDirectory(_rootPath);
                string path = Path.Combine(_rootPath, record.StartedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".csv");
                bool writeHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
                using (var writer = new StreamWriter(path, true, new UTF8Encoding(true)))
                {
                    if (writeHeader)
                        writer.WriteLine(Header);

                    writer.WriteLine(BuildCsvLine(record));
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string BuildCsvLine(TactTimeRecord record)
        {
            string when = record.EndedAt == DateTime.MinValue ? record.StartedAt.ToString("O") : record.EndedAt.ToString("O");
            return string.Join(",",
                Csv(when),
                Csv(record.RunId),
                Csv(record.ParentId),
                Csv(record.CorrelationId),
                Csv(record.EquipmentId),
                Csv(record.ProjectName),
                Csv(record.LotId),
                Csv(record.Mode),
                Csv(record.UnitName),
                Csv(record.SequenceName),
                Csv(record.ProcessName),
                Csv(record.StepName),
                Csv(record.Category.ToString()),
                Csv(record.StartedAt.ToString("O")),
                Csv(record.EndedAt.ToString("O")),
                Csv(record.ElapsedMs.ToString(CultureInfo.InvariantCulture)),
                Csv(record.Result.ToString()),
                Csv(record.AlarmCode),
                Csv(record.Detail));
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }

    public class TactTimeRecorder : IDisposable
    {
        private readonly List<ITactTimeSink> _sinks = new List<ITactTimeSink>();
        private readonly AsyncLocal<string> _currentScopeId = new AsyncLocal<string>();
        private readonly bool _enabled;
        private int _disposed;

        public TactTimeRecorder(
            string equipmentId,
            string projectName,
            string lotId,
            string mode,
            IEnumerable<ITactTimeSink> sinks)
            : this(true, equipmentId, projectName, lotId, mode, sinks)
        {
        }

        protected TactTimeRecorder(bool enabled)
            : this(enabled, "", "", "", "", null)
        {
        }

        private TactTimeRecorder(
            bool enabled,
            string equipmentId,
            string projectName,
            string lotId,
            string mode,
            IEnumerable<ITactTimeSink> sinks)
        {
            _enabled = enabled;
            RunId = Guid.NewGuid().ToString("N");
            EquipmentId = equipmentId ?? "";
            ProjectName = projectName ?? "";
            LotId = lotId ?? "";
            Mode = mode ?? "";

            if (sinks != null)
                _sinks.AddRange(sinks);
        }

        public string RunId { get; private set; }
        public string EquipmentId { get; set; }
        public string ProjectName { get; set; }
        public string LotId { get; set; }
        public string Mode { get; set; }

        public TactTimeScope BeginScope(
            TactTimeCategory category,
            string unitName,
            string sequenceName,
            string processName,
            string stepName,
            string correlationId = null,
            string detail = null)
        {
            if (!_enabled || Volatile.Read(ref _disposed) != 0)
                return TactTimeScope.CreateNoop();

            string parentId = _currentScopeId.Value ?? "";
            string scopeId = Guid.NewGuid().ToString("N");
            var record = new TactTimeRecord
            {
                RunId = RunId,
                ParentId = parentId,
                CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? scopeId : correlationId,
                EquipmentId = EquipmentId,
                ProjectName = ProjectName,
                LotId = LotId,
                Mode = Mode,
                UnitName = unitName ?? "",
                SequenceName = sequenceName ?? "",
                ProcessName = processName ?? "",
                StepName = stepName ?? "",
                Category = category,
                StartedAt = DateTime.Now,
                Result = TactTimeResult.Ok,
                Detail = detail ?? ""
            };

            _currentScopeId.Value = scopeId;
            return new TactTimeScope(this, record, parentId, scopeId);
        }

        public async Task MeasureAsync(
            TactTimeCategory category,
            string unitName,
            string sequenceName,
            string processName,
            string stepName,
            CancellationToken ct,
            Func<Task> action)
        {
            using (TactTimeScope scope = BeginScope(category, unitName, sequenceName, processName, stepName))
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    if (action != null)
                        await action().ConfigureAwait(false);
                    scope.Complete();
                }
                catch (OperationCanceledException)
                {
                    scope.Cancel();
                    throw;
                }
                catch (Exception ex)
                {
                    if (IsSequenceStopException(ex))
                        scope.Stop("", ex.Message);
                    else
                        scope.Fail("", ex.Message);
                    throw;
                }
                finally
                {
                }
            }
        }

        public async Task<int> MeasureAsync(
            TactTimeCategory category,
            string unitName,
            string sequenceName,
            string processName,
            string stepName,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            using (TactTimeScope scope = BeginScope(category, unitName, sequenceName, processName, stepName))
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    int result = action != null ? await action().ConfigureAwait(false) : 0;
                    if (result == 0)
                        scope.Complete();
                    else
                        scope.Fail("", "result=" + result);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    scope.Cancel();
                    throw;
                }
                catch (Exception ex)
                {
                    if (IsSequenceStopException(ex))
                        scope.Stop("", ex.Message);
                    else
                        scope.Fail("", ex.Message);
                    throw;
                }
                finally
                {
                }
            }
        }

        public async Task<bool> MeasureAsync(
            TactTimeCategory category,
            string unitName,
            string sequenceName,
            string processName,
            string stepName,
            CancellationToken ct,
            Func<Task<bool>> action)
        {
            using (TactTimeScope scope = BeginScope(category, unitName, sequenceName, processName, stepName))
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    bool result = action != null && await action().ConfigureAwait(false);
                    if (result)
                        scope.Complete();
                    else
                        scope.Fail("", "result=false");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    scope.Cancel();
                    throw;
                }
                catch (Exception ex)
                {
                    if (IsSequenceStopException(ex))
                        scope.Stop("", ex.Message);
                    else
                        scope.Fail("", ex.Message);
                    throw;
                }
                finally
                {
                }
            }
        }

        public IReadOnlyList<TactTimeRecord> Snapshot()
        {
            var list = new List<TactTimeRecord>();
            try
            {
                for (int i = 0; i < _sinks.Count; i++)
                {
                    var memory = _sinks[i] as MemoryTactTimeSink;
                    if (memory == null)
                        continue;

                    list.AddRange(memory.Snapshot());
                }
            }
            catch
            {
            }
            finally
            {
            }

            return list.AsReadOnly();
        }

        public void Dispose()
        {
            try
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                for (int i = 0; i < _sinks.Count; i++)
                {
                    var disposable = _sinks[i] as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        internal void FinishScope(TactTimeRecord record, string parentId)
        {
            try
            {
                _currentScopeId.Value = parentId ?? "";
                if (record == null || Volatile.Read(ref _disposed) != 0)
                    return;

                for (int i = 0; i < _sinks.Count; i++)
                {
                    try
                    {
                        _sinks[i].Write(record);
                    }
                    catch
                    {
                    }
                    finally
                    {
                    }
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool IsSequenceStopException(Exception ex)
        {
            try
            {
                while (ex != null)
                {
                    Type type = ex.GetType();
                    if (type != null && string.Equals(type.Name, "SequenceStopException", StringComparison.Ordinal))
                        return true;
                    ex = ex.InnerException;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return false;
        }
    }

    public sealed class NullTactTimeRecorder : TactTimeRecorder
    {
        public static readonly NullTactTimeRecorder Instance = new NullTactTimeRecorder();

        private NullTactTimeRecorder()
            : base(false)
        {
        }
    }

    public sealed class TactTimeScope : IDisposable
    {
        private readonly TactTimeRecorder _recorder;
        private readonly TactTimeRecord _record;
        private readonly string _parentId;
        private readonly Stopwatch _stopwatch;
        private int _completed;

        internal TactTimeScope(TactTimeRecorder recorder, TactTimeRecord record, string parentId, string scopeId)
        {
            _recorder = recorder;
            _record = record;
            _parentId = parentId ?? "";
            _stopwatch = Stopwatch.StartNew();
        }

        private TactTimeScope()
        {
        }

        internal static TactTimeScope CreateNoop()
        {
            return new TactTimeScope();
        }

        public void Complete(string detail = null)
        {
            SetResult(TactTimeResult.Ok, "", detail);
        }

        public void Fail(string alarmCode = null, string detail = null)
        {
            SetResult(TactTimeResult.Failed, alarmCode, detail);
        }

        public void Stop(string alarmCode = null, string detail = null)
        {
            SetResult(TactTimeResult.Stopped, alarmCode, detail);
        }

        public void Cancel(string detail = null)
        {
            SetResult(TactTimeResult.Canceled, "", detail);
        }

        public void Skip(string detail = null)
        {
            SetResult(TactTimeResult.Skipped, "", detail);
        }

        public void Dispose()
        {
            if (_recorder == null || _record == null)
                return;

            if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
                Finish(TactTimeResult.Ok, "", "");
        }

        private void SetResult(TactTimeResult result, string alarmCode, string detail)
        {
            if (_recorder == null || _record == null)
                return;

            if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0)
                return;

            Finish(result, alarmCode, detail);
        }

        private void Finish(TactTimeResult result, string alarmCode, string detail)
        {
            try
            {
                _stopwatch.Stop();
                _record.EndedAt = DateTime.Now;
                _record.ElapsedMs = _stopwatch.ElapsedMilliseconds;
                _record.Result = result;
                if (!string.IsNullOrWhiteSpace(alarmCode))
                    _record.AlarmCode = alarmCode;
                if (!string.IsNullOrWhiteSpace(detail))
                    _record.Detail = detail;
                _recorder.FinishScope(_record, _parentId);
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
