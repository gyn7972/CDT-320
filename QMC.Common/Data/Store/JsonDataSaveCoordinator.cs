using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Data.Store
{
    /// <summary>UI에서 캡처한 독립 사본을 순서대로 저장하고 기존 동기 저장과 파일별 순서를 공유합니다.</summary>
    public static class JsonDataSaveCoordinator
    {
        internal sealed class FileState
        {
            internal readonly object Sync = new object();
            internal readonly object Writer = new object();
            internal long Revision;
            internal PreparedSave Latest;
        }

        public sealed class PreparedSave
        {
            internal byte[] Payload;
            internal readonly Type DataType;
            internal FileState State;
            internal long Revision;
            internal TaskCompletionSource<DataStoreResult> Completion;
            internal DataStoreResult Result;
            public string Path { get; private set; }

            internal PreparedSave(string path, Type type, byte[] payload)
            {
                Path = path;
                DataType = type;
                Payload = payload;
            }
        }

        private static readonly object StatesSync = new object();
        private static readonly Dictionary<string, FileState> States =
            new Dictionary<string, FileState>(StringComparer.OrdinalIgnoreCase);
        private static readonly object QueueSync = new object();
        private static Task _tail = Task.FromResult(0);

        // 변경 유닛의 작은 저장 사본만 준비합니다. 객체를 역직렬화하면 OnDeserialized가 값/기본값을
        // 다시 보정하므로 저장할 JSON 자체를 동결하여 UI 적용값과 파일 내용이 달라지지 않게 합니다.
        public static PreparedSave Capture(object data, string path)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Capture(data, data.GetType(), path);
        }

        private static PreparedSave Capture(object data, Type type, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("저장 경로가 없습니다.", nameof(path));
            using (var stream = new MemoryStream())
            {
                JsonPrettySerializer.WriteObject(stream, type, data, JsonPrettySerializer.CreateSettings(true));
                return new PreparedSave(System.IO.Path.GetFullPath(path), type, stream.ToArray());
            }
        }

        public static Task<DataStoreResult> Enqueue(PreparedSave save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            FileState state = GetState(save.Path);
            lock (state.Sync)
            {
                if (save.Completion != null) throw new InvalidOperationException("이미 등록한 저장 요청입니다.");
                save.State = state;
                save.Revision = ++state.Revision;
                save.Completion = new TaskCompletionSource<DataStoreResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                state.Latest = save;
            }
            lock (QueueSync)
            {
                _tail = _tail.ContinueWith(_ => Commit(save), CancellationToken.None,
                    TaskContinuationOptions.None, TaskScheduler.Default);
            }
            return save.Completion.Task;
        }

        // 기존 동기 Save 계약을 유지합니다. 이후 등록된 동기 저장은 이전 UI 사본을 무효화합니다.
        internal static DataStoreResult SaveSynchronously(object data, Type type, string path)
        {
            FileState state = GetState(path);
            var current = new PreparedSave(System.IO.Path.GetFullPath(path), type, null);
            // 호출을 수락할 때 세대를 배정합니다. Writer를 기다리는 동안 접수된 더 최신 UI 값을
            // 뒤늦게 잠금을 얻은 동기 저장이 덮어쓰지 않으며, Load/종료도 이 저장 완료를 기다립니다.
            lock (state.Sync)
            {
                current.State = state;
                current.Revision = ++state.Revision;
                current.Completion = new TaskCompletionSource<DataStoreResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                state.Latest = current;
            }
            DataStoreResult result;
            bool superseded = false;
            try
            {
                current.Payload = Capture(data, type, path).Payload;
                lock (state.Writer)
                {
                    lock (state.Sync) superseded = state.Revision != current.Revision;
                    result = superseded
                        ? DataStoreResult.Ok(path, "최신 저장 요청으로 대체되었습니다.")
                        : JsonDataStore.SaveSerialized(current.Payload, path);
                }
            }
            catch (Exception ex)
            {
                result = DataStoreResult.Fail(path, ex.Message, ex);
            }
            lock (state.Sync) current.Result = result;
            current.Completion.TrySetResult(result);
            // 동기 호출자는 durable 완료 계약을 유지합니다. 후속 요청이 있다면 그 결과까지 확인합니다.
            return superseded ? FlushPath(path, false) : result;
        }
        public static bool HasUnfinishedSaves
        {
            get { return GetLatest(null).Any(s => s.Result == null || !s.Result.Success); }
        }

        public static DataStoreResult GetCurrentFailure(string path)
        {
            FileState state = GetState(path);
            lock (state.Sync)
                return state.Latest != null && state.Latest.Result != null && !state.Latest.Result.Success
                    ? state.Latest.Result : null;
        }

        public static Task<DataStoreResult> FlushAsync(int timeoutMs = 15000)
        {
            return FlushAsync(null, timeoutMs);
        }

        // 명시적 로드/복사/이름변경은 원래 동기 파일 작업입니다. 편집·일반 페이지 전환에서는 호출하지 않습니다.
        internal static DataStoreResult FlushPath(string path, bool directory)
        {
            string target = System.IO.Path.GetFullPath(path);
            if (directory) target = target.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            return FlushAsync(s => directory
                ? s.Path.StartsWith(target, StringComparison.OrdinalIgnoreCase)
                : string.Equals(s.Path, target, StringComparison.OrdinalIgnoreCase), 15000).GetAwaiter().GetResult();
        }

        public static Task<DataStoreResult> RetryFailedAsync()
        {
            foreach (PreparedSave failed in GetLatest(null))
            {
                lock (failed.State.Sync)
                {
                    if (!ReferenceEquals(failed.State.Latest, failed) || failed.Result == null || failed.Result.Success || failed.Payload == null)
                        continue;
                    Enqueue(new PreparedSave(failed.Path, failed.DataType, failed.Payload));
                }
            }
            return FlushAsync();
        }

        public sealed class TransactionSaveResult
        {
            public bool Success { get; private set; }
            public bool RecoveryRequired { get; private set; }
            public string Message { get; private set; }

            public TransactionSaveResult(bool success, bool recoveryRequired, string message)
            {
                Success = success;
                RecoveryRequired = recoveryRequired;
                Message = message ?? string.Empty;
            }
        }

        private sealed class TransactionFile
        {
            internal PreparedSave Save;
            internal FileState State;
            internal bool Existed;
            internal byte[] Original;
            internal string TemporaryPath;
        }

        /// <summary>
        /// 최대 5개 파일을 기존 파일별 저장 잠금 안에서 함께 저장합니다.
        /// 호출자는 장비/편집의 배타 범위를 유지하고 성공 후에만 런타임 사본을 반영해야 합니다.
        /// 일반 I/O 실패는 역순 복구하며, 전원 차단 또는 복구 I/O 실패까지 원자성을 보장하지 않습니다.
        /// </summary>
        public static TransactionSaveResult SaveTransaction(IReadOnlyList<PreparedSave> saves)
        {
            var heldLocks = new List<object>();
            var files = new List<TransactionFile>();
            var attempted = new List<TransactionFile>();
            bool reserved = false;
            try
            {
                if (saves == null || saves.Count == 0 || saves.Count > 5)
                    throw new ArgumentException("함께 저장할 파일은 1~5개여야 합니다.");
                if (HasUnfinishedSaves)
                    throw new InvalidOperationException("기존 설정 저장이 진행 중이거나 실패 상태입니다. 저장을 완료한 뒤 다시 실행하십시오.");

                foreach (PreparedSave save in saves)
                {
                    if (save == null || save.Payload == null || save.Completion != null)
                        throw new ArgumentException("새로 캡처한 저장 사본만 함께 저장할 수 있습니다.");
                    if (files.Any(file => string.Equals(file.Save.Path, save.Path, StringComparison.OrdinalIgnoreCase)))
                        throw new ArgumentException("같은 파일을 중복해서 저장할 수 없습니다. path=" + save.Path);
                    files.Add(new TransactionFile { Save = save, State = GetState(save.Path) });
                }
                files.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Save.Path, right.Save.Path));

                // 기존 Commit과 동일한 Writer -> Sync 순서를 지킵니다. 대상 전체를 확보한 동안
                // 기존 큐와 동기 저장의 신규 접수/교체를 막아 복구 중 다른 값을 덮어쓰지 않습니다.
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (TransactionFile file in files)
                    EnterTransactionLock(file.State.Writer, heldLocks, watch);
                foreach (TransactionFile file in files)
                    EnterTransactionLock(file.State.Sync, heldLocks, watch);
                foreach (TransactionFile file in files)
                {
                    if (file.Save.Completion != null ||
                        (file.State.Latest != null &&
                         (file.State.Latest.Result == null || !file.State.Latest.Result.Success)))
                        throw new InvalidOperationException("대상 파일의 기존 저장이 끝나지 않았습니다. path=" + file.Save.Path);
                }

                foreach (TransactionFile file in files)
                {
                    file.Save.State = file.State;
                    file.Save.Completion = new TaskCompletionSource<DataStoreResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                reserved = true;

                // 모든 원본 읽기와 새 파일 쓰기를 완료한 뒤에만 첫 운영 파일을 교체합니다.
                foreach (TransactionFile file in files)
                {
                    if (Directory.Exists(file.Save.Path))
                        throw new IOException("저장할 파일 경로에 폴더가 있습니다. path=" + file.Save.Path);
                    file.Existed = File.Exists(file.Save.Path);
                    file.Original = file.Existed ? File.ReadAllBytes(file.Save.Path) : null;
                    string directory = System.IO.Path.GetDirectoryName(file.Save.Path);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                    file.TemporaryPath = file.Save.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    File.WriteAllBytes(file.TemporaryPath, file.Save.Payload);
                }
                foreach (TransactionFile file in files)
                {
                    attempted.Add(file);
                    InstallTransactionFile(file.TemporaryPath, file.Save.Path, file.Existed);
                    file.TemporaryPath = null;
                }
                foreach (TransactionFile file in files)
                    CompleteTransactionFile(file, DataStoreResult.Ok(file.Save.Path), true, false);
                return new TransactionSaveResult(true, false, string.Empty);
            }
            catch (Exception ex)
            {
                var recoveryErrors = new List<string>();
                for (int i = attempted.Count - 1; i >= 0; i--)
                {
                    TransactionFile file = attempted[i];
                    try { RestoreTransactionFile(file); }
                    catch (Exception restoreEx)
                    {
                        recoveryErrors.Add(file.Save.Path + ": " + restoreEx.Message);
                    }
                }
                bool recoveryRequired = recoveryErrors.Count != 0;
                string message = "설정 묶음 저장에 실패했습니다. " + ex.Message;
                if (recoveryRequired)
                    message += " 원본 파일 복구를 완료하지 못했습니다. " + string.Join(" / ", recoveryErrors);
                else if (attempted.Count != 0)
                    message += " 변경한 파일은 저장 전 내용으로 복구했습니다.";

                if (reserved)
                {
                    foreach (TransactionFile file in files)
                    {
                        // 실패 후보를 일반 파일별 재시도에 남기면 일부 값만 다시 저장될 수 있습니다.
                        // 복구 성공은 기존 상태를 유지하고, 복구 실패만 재시도 payload 없이 차단 상태로 남깁니다.
                        CompleteTransactionFile(file, DataStoreResult.Fail(file.Save.Path, message, ex),
                            recoveryRequired, recoveryRequired);
                    }
                }
                return new TransactionSaveResult(false, recoveryRequired, message);
            }
            finally
            {
                foreach (TransactionFile file in files)
                {
                    if (string.IsNullOrEmpty(file.TemporaryPath)) continue;
                    try { if (File.Exists(file.TemporaryPath)) File.Delete(file.TemporaryPath); }
                    catch (Exception cleanupEx)
                    {
                        System.Diagnostics.Trace.TraceError("설정 저장 임시 파일 정리 실패. path=" + file.TemporaryPath + ", error=" + cleanupEx);
                    }
                }
                for (int i = heldLocks.Count - 1; i >= 0; i--) Monitor.Exit(heldLocks[i]);
            }
        }

        private static void EnterTransactionLock(object target, List<object> heldLocks, System.Diagnostics.Stopwatch watch)
        {
            int remaining = (int)Math.Max(0, 5000 - watch.ElapsedMilliseconds);
            if (remaining == 0 || !Monitor.TryEnter(target, remaining))
                throw new TimeoutException("다른 설정 저장이 파일을 사용 중이어서 함께 저장하지 못했습니다.");
            heldLocks.Add(target);
        }

        private static void InstallTransactionFile(string temporaryPath, string path, bool existed)
        {
            if (existed) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
        }

        private static void RestoreTransactionFile(TransactionFile file)
        {
            if (file.Existed)
            {
                if (File.Exists(file.Save.Path) && File.ReadAllBytes(file.Save.Path).SequenceEqual(file.Original)) return;
                string restorePath = file.Save.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllBytes(restorePath, file.Original);
                    InstallTransactionFile(restorePath, file.Save.Path, File.Exists(file.Save.Path));
                }
                finally
                {
                    if (File.Exists(restorePath)) File.Delete(restorePath);
                }
            }
            else if (File.Exists(file.Save.Path)) File.Delete(file.Save.Path);
        }

        private static void CompleteTransactionFile(TransactionFile file, DataStoreResult result,
            bool publishState, bool discardPayload)
        {
            file.Save.Result = result;
            if (discardPayload) file.Save.Payload = null;
            if (publishState)
            {
                file.Save.Revision = ++file.State.Revision;
                file.State.Latest = file.Save;
            }
            file.Save.Completion.TrySetResult(result);
        }

        private static async Task<DataStoreResult> FlushAsync(Func<PreparedSave, bool> filter, int timeoutMs)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(1, timeoutMs));
            while (true)
            {
                PreparedSave[] saves = GetLatest(filter);
                if (saves.Length == 0) return DataStoreResult.Ok(string.Empty);
                Task all = Task.WhenAll(saves.Select(s => s.Completion.Task));
                int remaining = (int)Math.Max(1, (deadline - DateTime.UtcNow).TotalMilliseconds);
                if (await Task.WhenAny(all, Task.Delay(remaining)).ConfigureAwait(false) != all)
                    return DataStoreResult.Fail(string.Empty, "설정 저장 대기 시간이 초과되었습니다. 저장은 계속 진행 중입니다.");
                await all.ConfigureAwait(false);
                PreparedSave[] latest = GetLatest(filter);
                PreparedSave failure = latest.FirstOrDefault(s => s.Result != null && !s.Result.Success);
                if (failure != null) return failure.Result;
                if (latest.All(s => s.Result != null && s.Result.Success)) return DataStoreResult.Ok(string.Empty);
                if (DateTime.UtcNow >= deadline)
                    return DataStoreResult.Fail(string.Empty, "설정 변경이 계속되어 저장 완료를 확인하지 못했습니다.");
            }
        }

        private static PreparedSave[] GetLatest(Func<PreparedSave, bool> filter)
        {
            FileState[] states;
            lock (StatesSync) states = States.Values.ToArray();
            var result = new List<PreparedSave>();
            foreach (FileState state in states)
            {
                lock (state.Sync)
                {
                    if (state.Latest != null && (filter == null || filter(state.Latest))) result.Add(state.Latest);
                }
            }
            return result.ToArray();
        }

        private static FileState GetState(string path)
        {
            string key = System.IO.Path.GetFullPath(path);
            lock (StatesSync)
            {
                FileState state;
                if (!States.TryGetValue(key, out state)) States.Add(key, state = new FileState());
                return state;
            }
        }

        private static void Commit(PreparedSave save)
        {
            DataStoreResult result;
            try
            {
                lock (save.State.Writer)
                {
                    bool current;
                    lock (save.State.Sync) current = save.State.Revision == save.Revision;
                    if (!current)
                        result = DataStoreResult.Ok(save.Path, "최신 저장 요청으로 대체되었습니다.");
                    else
                    {
                        result = JsonDataStore.SaveSerialized(save.Payload, save.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                result = DataStoreResult.Fail(save.Path, ex.Message, ex);
            }
            lock (save.State.Sync) save.Result = result;
            save.Completion.TrySetResult(result);
        }
    }
}
