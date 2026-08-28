using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Lots
{
    /// <summary>Lot 저장소 + Active Lot 관리.</summary>
    public static class LotStorage
    {
        private static readonly ConcurrentDictionary<string, Lot> _lots
            = new ConcurrentDictionary<string, Lot>(StringComparer.Ordinal);
        private static readonly object _activeLotSync = new object();

        public static IReadOnlyDictionary<string, Lot> Lots => _lots;

        /// <summary>현재 활성 Lot. 사이클 시작 시 OpenLot 으로 설정됨.</summary>
        public static Lot ActiveLot { get; private set; }

        /// <summary>현재 활성 Input DieMap (웨이퍼). LiveLotMapView 등 시각화 컨트롤이 폴링.</summary>
        public static QMC.CDT320.DieMaps.DieMap ActiveInputDieMap { get; set; }

        public static event Action<Lot> ActiveLotChanged;

        public static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Lots");
        public static string ActiveLotPointerPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State", "active_lot.json");

        static LotStorage()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            LoadHistoryFromDisk();
        }

        public static Lot OpenLot(string lotId, string recipeName, int totalDies)
        {
            return OpenLot(lotId, recipeName, totalDies, Lot.DefaultReworkCount);
        }

        public static Lot OpenLot(string lotId, string recipeName, int totalDies, int reworkCount)
        {
            Lot lot;
            string error;
            if (!TryOpenLot(lotId, recipeName, totalDies, reworkCount, out lot, out error))
                throw new InvalidOperationException(error);

            return lot;
        }

        /// <summary>
        /// LOT 이력과 활성 LOT 포인터를 모두 저장한 뒤에만 메모리 ActiveLot을 공개한다.
        /// 활성 포인터는 Material 데이터와 분리되어 자재 초기화/레시피 변경 후에도 유지된다.
        /// </summary>
        public static bool TryOpenLot(
            string lotId,
            string recipeName,
            int totalDies,
            out Lot openedLot,
            out string error)
        {
            return TryOpenLot(
                lotId,
                recipeName,
                totalDies,
                Lot.DefaultReworkCount,
                out openedLot,
                out error);
        }

        public static bool TryOpenLot(
            string lotId,
            string recipeName,
            int totalDies,
            int reworkCount,
            out Lot openedLot,
            out string error)
        {
            openedLot = null;
            error = "";

            if (reworkCount < Lot.MinReworkCount || reworkCount > Lot.MaxReworkCount)
            {
                error = "Rework 값은 " + Lot.MinReworkCount + "~" + Lot.MaxReworkCount +
                        " 범위여야 합니다. value=" + reworkCount;
                return false;
            }

            string normalized = string.IsNullOrWhiteSpace(lotId)
                ? "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")
                : lotId.Trim();

            Lot lot;
            bool added = false;
            lock (_activeLotSync)
            {
                if (ActiveLot != null)
                {
                    error = "이미 진행 중인 LOT이 있습니다. lot=" + (ActiveLot.LotID ?? "");
                    return false;
                }

                if (!_lots.TryGetValue(normalized, out lot))
                {
                    lot = new Lot
                    {
                        LotID = normalized,
                        RecipeName = recipeName ?? "",
                        StartedAt = DateTime.Now,
                        State = LotState.Open,
                        TotalDies = totalDies,
                        ReworkCount = reworkCount
                    };
                    added = _lots.TryAdd(normalized, lot);
                    if (!added && !_lots.TryGetValue(normalized, out lot))
                    {
                        error = "LOT 이력 객체를 생성하지 못했습니다. lot=" + normalized;
                        return false;
                    }
                }

                string previousRecipeName = lot.RecipeName;
                int previousTotalDies = lot.TotalDies;
                int previousReworkCount = lot.ReworkCount;
                LotState previousState = lot.State;
                DateTime? previousFinishedAt = lot.FinishedAt;

                // 완료/중단 LOT ID도 다시 시작할 수 있다 — 종료 시각을 지우고 같은 이력으로 이어 집계한다.
                if (lot.State == LotState.Completed || lot.State == LotState.Aborted)
                {
                    lot.State = LotState.Open;
                    lot.FinishedAt = null;
                }

                lot.RecipeName = recipeName ?? lot.RecipeName;
                lot.TotalDies = totalDies;
                lot.ReworkCount = reworkCount;
                if (lot.State == LotState.Open)
                    lot.State = LotState.Running;

                try
                {
                    // LOT 시작 시 이력과 활성 포인터를 함께 영속화한다.
                    SaveJsonOrThrow(lot);
                    SaveActiveLotPointerOrThrow(normalized);
                    ActiveLot = lot;
                    openedLot = lot;
                }
                catch (Exception ex)
                {
                    lot.RecipeName = previousRecipeName;
                    lot.TotalDies = previousTotalDies;
                    lot.ReworkCount = previousReworkCount;
                    if (added)
                    {
                        // 이력 파일만 Running으로 남으면 다음 기동 때 잘못된 복구 후보가 된다.
                        lot.State = LotState.Aborted;
                        lot.FinishedAt = DateTime.Now;
                        try { SaveJsonOrThrow(lot); } catch { }
                        _lots.TryRemove(normalized, out lot);
                    }
                    else
                    {
                        lot.State = previousState;
                        lot.FinishedAt = previousFinishedAt;
                        try { SaveJsonOrThrow(lot); } catch { }
                    }

                    error = "활성 LOT 저장에 실패했습니다. lot=" + normalized + ", error=" + ex.Message;
                    return false;
                }
            }

            try { ActiveLotChanged?.Invoke(lot); } catch { }
            return true;
        }

        public static void CloseLot(bool aborted = false)
        {
            string error;
            TryCloseLot(aborted, out error);
        }

        /// <summary>
        /// LOT 완료 이력 저장과 활성 포인터 해제를 하나의 완료 경계로 처리한다.
        /// 저장 실패 시 메모리 LOT을 Running 상태로 되돌려 LOT이 조용히 종료되지 않게 한다.
        /// </summary>
        public static bool TryCloseLot(bool aborted, out string error)
        {
            error = "";
            Lot lot;

            lock (_activeLotSync)
            {
                lot = ActiveLot;
                if (lot == null)
                {
                    error = "진행 중인 LOT이 없습니다.";
                    return false;
                }

                LotState previousState = lot.State;
                DateTime? previousFinishedAt = lot.FinishedAt;
                lot.State = aborted ? LotState.Aborted : LotState.Completed;
                lot.FinishedAt = DateTime.Now;

                try
                {
                    SaveJsonOrThrow(lot);
                    // 빈 포인터를 원자적으로 기록한다. 이 파일이 있으면 과거 Running 이력을 임의 복원하지 않는다.
                    SaveActiveLotPointerOrThrow("");
                    ActiveLot = null;
                }
                catch (Exception ex)
                {
                    lot.State = previousState;
                    lot.FinishedAt = previousFinishedAt;
                    try { SaveJsonOrThrow(lot); } catch { }
                    error = "LOT 완료 상태 저장에 실패했습니다. lot=" + (lot.LotID ?? "") +
                            ", error=" + ex.Message;
                    return false;
                }
            }

            try { ActiveLotChanged?.Invoke(lot); } catch { }
            return true;
        }

        public static Lot Get(string lotId)
        {
            if (string.IsNullOrEmpty(lotId)) return null;
            _lots.TryGetValue(lotId, out var lot);
            return lot;
        }

        public static IReadOnlyList<Lot> ListByDate(DateTime date)
        {
            return _lots.Values.Where(l => l.StartedAt.Date == date.Date).ToList();
        }

        public static void SaveJson(Lot lot)
        {
            if (lot == null) return;
            try { SaveJsonOrThrow(lot); }
            catch { }
        }

        /// <summary>
        /// 진행 중인 LOT 을 종료하지 않고 현재 카운터만 파일에 반영한다.
        /// [LOT 관리 2026-07-27] 정지/프로그램 종료는 LOT 종료가 아니다.
        /// LOT 완료는 작업자가 [LOT 완료] 를 눌렀을 때만 발생한다(사용자 확정 사항).
        /// </summary>
        public static void SaveActiveLotProgress()
        {
            Lot lot = ActiveLot;
            if (lot == null) return;
            SaveJson(lot);
        }

        /// <summary>
        /// 재시작 후 진행 중이던 LOT 을 다시 활성 LOT 으로 되돌린다.
        /// 디스크에 Running 상태로 남아 있고 ID 가 일치할 때만 복구한다.
        /// </summary>
        public static bool TryRestoreActiveLot(string lotId)
        {
            string error;
            return TryRestoreActiveLot(lotId, out error);
        }

        public static bool TryRestoreActiveLot(string lotId, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(lotId))
            {
                error = "복원할 LOT ID가 없습니다.";
                return false;
            }

            Lot lot;
            lock (_activeLotSync)
            {
                if (ActiveLot != null)
                {
                    error = "이미 활성 LOT이 있습니다. lot=" + (ActiveLot.LotID ?? "");
                    return false;
                }

                string normalized = lotId.Trim();
                if (!_lots.TryGetValue(normalized, out lot) || lot == null)
                {
                    error = "LOT 이력에서 복원 대상을 찾지 못했습니다. lot=" + normalized;
                    return false;
                }

                if (lot.State != LotState.Running && lot.State != LotState.Open)
                {
                    error = "복원 대상 LOT이 진행 상태가 아닙니다. lot=" + normalized +
                            ", state=" + lot.State;
                    return false;
                }

                LotState previousState = lot.State;
                lot.State = LotState.Running;
                try
                {
                    SaveJsonOrThrow(lot);
                    SaveActiveLotPointerOrThrow(normalized);
                    ActiveLot = lot;
                }
                catch (Exception ex)
                {
                    lot.State = previousState;
                    try { SaveJsonOrThrow(lot); } catch { }
                    error = "활성 LOT 복원 저장에 실패했습니다. lot=" + normalized +
                            ", error=" + ex.Message;
                    return false;
                }
            }

            try { ActiveLotChanged?.Invoke(lot); } catch { }
            return true;
        }

        /// <summary>
        /// Material Snapshot과 독립적으로 저장된 활성 LOT 포인터를 읽는다.
        /// pointerExists=true, lotId=""이면 LOT 완료가 명시적으로 저장된 상태다.
        /// </summary>
        public static bool TryGetPersistedActiveLotId(
            out string lotId,
            out bool pointerExists,
            out string error)
        {
            lotId = "";
            pointerExists = false;
            error = "";

            lock (_activeLotSync)
            {
                try
                {
                    string path = ActiveLotPointerPath;
                    if (!File.Exists(path))
                        return true;

                    pointerExists = true;
                    var serializer = new DataContractJsonSerializer(typeof(ActiveLotPointer));
                    ActiveLotPointer pointer;
                    using (var fs = File.OpenRead(path))
                    {
                        pointer = serializer.ReadObject(fs) as ActiveLotPointer;
                    }

                    if (pointer == null)
                    {
                        error = "활성 LOT 포인터 내용이 비어 있습니다. path=" + path;
                        return false;
                    }

                    lotId = string.IsNullOrWhiteSpace(pointer.LotId) ? "" : pointer.LotId.Trim();
                    return true;
                }
                catch (Exception ex)
                {
                    error = "활성 LOT 포인터를 읽지 못했습니다. path=" + ActiveLotPointerPath +
                            ", error=" + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Log\Lots\*.json 을 읽어 LOT 이력을 메모리에 올린다.
        /// 정적 생성자에서 1회 호출된다. 파일 하나가 깨져도 나머지는 계속 읽는다.
        /// </summary>
        public static void LoadHistoryFromDisk()
        {
            try
            {
                if (!Directory.Exists(Dir)) return;

                string[] files = Directory.GetFiles(Dir, "*.json");
                var serializer = new DataContractJsonSerializer(typeof(Lot));

                foreach (string path in files)
                {
                    try
                    {
                        Lot lot;
                        using (var fs = File.OpenRead(path))
                        {
                            lot = serializer.ReadObject(fs) as Lot;
                        }

                        if (lot == null || string.IsNullOrEmpty(lot.LotID)) continue;
                        if (lot.BinDistribution == null) lot.BinDistribution = new Dictionary<int, int>();
                        lot.ReworkCount = Lot.NormalizeReworkCount(lot.ReworkCount);

                        _lots[lot.LotID] = lot;
                    }
                    catch
                    {
                        // 개별 파일 손상은 무시하고 나머지 이력을 계속 읽는다.
                    }
                }
            }
            catch
            {
            }
        }

        public static void Clear()
        {
            _lots.Clear();
            ActiveLot = null;
            try { SaveActiveLotPointerOrThrow(""); } catch { }
        }

        private static void SaveJsonOrThrow(Lot lot)
        {
            if (lot == null)
                throw new ArgumentNullException(nameof(lot));

            Directory.CreateDirectory(Dir);
            string fn = $"{lot.StartedAt:yyyyMMdd}_{lot.LotID}.json";
            string path = Path.Combine(Dir, fn);
            using (var fs = File.Create(path))
            {
                JsonPrettySerializer.WriteObject(fs, typeof(Lot), lot);
            }
        }

        private static void SaveActiveLotPointerOrThrow(string lotId)
        {
            string path = ActiveLotPointerPath;
            string directory = Path.GetDirectoryName(path);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            Directory.CreateDirectory(directory);
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            var pointer = new ActiveLotPointer
            {
                LotId = string.IsNullOrWhiteSpace(lotId) ? "" : lotId.Trim(),
                UpdatedAt = DateTime.Now
            };

            try
            {
                using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(ActiveLotPointer), pointer);
                    fs.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(tempPath, path, backupPath, true);
                else
                    File.Move(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [DataContract]
        private sealed class ActiveLotPointer
        {
            [DataMember] public string LotId { get; set; } = "";
            [DataMember] public DateTime UpdatedAt { get; set; }
        }
    }
}
