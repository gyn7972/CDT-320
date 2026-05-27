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
            EnsureDefaults(Current);
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AlgorithmCameraSubset));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        /// <summary>5 알고리즘 항목 보강 (Common 의 EnsureDefaults 위임).</summary>
        public static void EnsureDefaults(AlgorithmCameraSubset map) => map?.EnsureDefaults();
    }
}
