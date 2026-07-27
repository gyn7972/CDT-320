using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Lots
{
    /// <summary>Lot 저장소 + Active Lot 관리.</summary>
    public static class LotStorage
    {
        private static readonly ConcurrentDictionary<string, Lot> _lots
            = new ConcurrentDictionary<string, Lot>(StringComparer.Ordinal);

        public static IReadOnlyDictionary<string, Lot> Lots => _lots;

        /// <summary>현재 활성 Lot. 사이클 시작 시 OpenLot 으로 설정됨.</summary>
        public static Lot ActiveLot { get; private set; }

        /// <summary>현재 활성 Input DieMap (웨이퍼). LiveLotMapView 등 시각화 컨트롤이 폴링.</summary>
        public static QMC.CDT320.DieMaps.DieMap ActiveInputDieMap { get; set; }

        public static event Action<Lot> ActiveLotChanged;

        public static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Lots");

        static LotStorage()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            LoadHistoryFromDisk();
        }

        public static Lot OpenLot(string lotId, string recipeName, int totalDies)
        {
            if (string.IsNullOrEmpty(lotId)) lotId = "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var lot = _lots.GetOrAdd(lotId, k => new Lot
            {
                LotID = k, RecipeName = recipeName,
                StartedAt = DateTime.Now, State = LotState.Running,
                TotalDies = totalDies
            });
            // 이미 존재하면 카운터만 reset 안함 — 단순히 새 카운터 update
            lot.RecipeName = recipeName ?? lot.RecipeName;
            lot.TotalDies = totalDies;
            if (lot.State == LotState.Open) lot.State = LotState.Running;
            ActiveLot = lot;
            // [LOT 관리 2026-07-27] 시작 시점에 바로 파일로 남긴다.
            // 예전에는 CloseLot 에서만 저장해서, 프로그램을 재시작하면 진행 중이던 LOT 이
            // 이력에서 통째로 사라졌다. 진행 이력은 재시작 후에도 보여야 한다.
            SaveJson(lot);
            try { ActiveLotChanged?.Invoke(lot); } catch { }
            return lot;
        }

        public static void CloseLot(bool aborted = false)
        {
            if (ActiveLot == null) return;
            ActiveLot.State = aborted ? LotState.Aborted : LotState.Completed;
            ActiveLot.FinishedAt = DateTime.Now;
            SaveJson(ActiveLot);
            try { ActiveLotChanged?.Invoke(ActiveLot); } catch { }
            ActiveLot = null;
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
            try
            {
                string fn = $"{lot.StartedAt:yyyyMMdd}_{lot.LotID}.json";
                string path = Path.Combine(Dir, fn);
                using (var fs = File.Create(path))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(Lot), lot);
                }
            }
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
            if (string.IsNullOrWhiteSpace(lotId)) return false;
            if (ActiveLot != null) return false;

            Lot lot;
            if (!_lots.TryGetValue(lotId.Trim(), out lot) || lot == null) return false;
            if (lot.State != LotState.Running && lot.State != LotState.Open) return false;

            lot.State = LotState.Running;
            ActiveLot = lot;
            try { ActiveLotChanged?.Invoke(lot); } catch { }
            return true;
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
        }
    }
}
