using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using QMC.Common.Recipes;   // VisionAlgorithm, AlgorithmCameraMapping, AlgorithmCameraSubset

namespace QMC.Vision.Config
{
    /// <summary>
    /// 알고리즘-카메라 매핑 영속화 — Config\algorithm_camera.json.
    /// Stage 62: 모델은 QMC.Common.Recipes 로 이동, 여기는 Vision 전용 전역 fallback storage 만 유지.
    /// (Project 별 매핑은 Handler 의 RecipeProject.VisionCameras 에 저장됨.)
    /// </summary>
    public static class AlgorithmCameraMapStore
    {
        public static string Dir  { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } = System.IO.Path.Combine(Dir, "algorithm_camera.json");

        public static AlgorithmCameraSubset Current { get; private set; } = new AlgorithmCameraSubset();

        static AlgorithmCameraMapStore() { Directory.CreateDirectory(Dir); }

        public static AlgorithmCameraSubset Load()
        {
            if (!File.Exists(Path_))
            {
                Current = new AlgorithmCameraSubset();
                EnsureDefaults(Current);
                Save();
                return Current;
            }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AlgorithmCameraSubset));
                    Current = (AlgorithmCameraSubset)ser.ReadObject(fs);
                }
            }
            catch { Current = new AlgorithmCameraSubset(); }
            if (Current == null) Current = new AlgorithmCameraSubset();
            // Stage 63 — 구버전 알고리즘 이름(TopSide/BottomSide) 자동 마이그레이션 + 누락 보강.
            // 역직렬화 성공/실패와 무관하게 항상 정규화 저장 → 디스크가 항상 최신 스키마로 유지.
            Current.MigrateLegacyAlgorithmNames();
            EnsureDefaults(Current);
            Save();
            return Current;
        }

        public static void Save()
        {
            try
            {
                PruneEmptyOverrides(Current);   // Stage 64 — 빈 override 정리 (JSON 비대화 방지)
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AlgorithmCameraSubset));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        /// <summary>Stage 64 — 각 알고리즘의 Inspections 에서 IsEmpty override 제거 + 동일 InspectionId 중복 제거(첫 항목 우선).
        /// 모든 override 가 비면 Inspections 자체를 null 로 — 구버전 호환 JSON 유지.</summary>
        private static void PruneEmptyOverrides(AlgorithmCameraSubset map)
        {
            if (map?.Items == null) return;
            foreach (var m in map.Items)
            {
                if (m.Inspections == null) continue;
                var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                m.Inspections = m.Inspections
                    .Where(o => o != null && !o.IsEmpty() && seen.Add(o.InspectionId))
                    .ToList();
                if (m.Inspections.Count == 0) m.Inspections = null;
            }
        }

        /// <summary>5 알고리즘 항목 보강 (Common 의 EnsureDefaults 위임).</summary>
        public static void EnsureDefaults(AlgorithmCameraSubset map) => map?.EnsureDefaults();
    }
}
