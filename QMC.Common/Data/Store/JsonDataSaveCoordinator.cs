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
