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
            ActiveLot = lot;
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

        public static void Clear()
        {
            _lots.Clear();
            ActiveLot = null;
        }
    }
}
