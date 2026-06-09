using System;
using System.Collections.Generic;
using System.IO;
using QMC.Vision.Backends.Cognex;
using QMC.Vision.Core.Inspectors;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Modules;

namespace QMC.Vision.Config
{
    /// <summary>
    /// P2 — 부팅 시 ParameterStore 구성: 도메인 provider 등록(채널 라우팅) + 위임 저장 배선 + LoadAll + 마이그레이션.
    /// 호출: Form1 모듈 생성 직후. 기존 스토어(VisionConfigStore/AlgorithmCameraMapStore)는 이미 Load 된 상태 전제.
    /// </summary>
    public static class ParameterStoreBootstrap
    {
        // ② 영속 인스턴스(P2 한정 — 인스펙터 연결은 P3). 마이그레이션·왕복 대상.
        public static BottomInspectionParameters BottomParams { get; private set; }
        public static DistortionParameters       DistortionParams { get; private set; }
        public static VisionScaleParameters      VisionScaleParams { get; private set; }
        public static SideInspectionParameters   SideParams { get; private set; }
        public static DieGapInspectionParameters DieGapParams { get; private set; }

        public static string SetupPath { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Setup", "vision_setup.json");
        private static string OldRecipeDir { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes");

        public static ParameterStore Build(IEnumerable<VisionModule> modules)
        {
            var store = new ParameterStore { SetupFilePath = SetupPath };

            var moduleList = modules != null ? new List<VisionModule>(modules) : new List<VisionModule>();

            // ② 영속 인스턴스 — 레지스트리 단일 소스(G3). 인스펙터 등록 전 생성·주입.
            BottomParams      = (BottomInspectionParameters)  InspectionParamRegistry.ByTool("BottomInspection").Create();
            DistortionParams  = (DistortionParameters)        InspectionParamRegistry.ByTool("Distortion").Create();
            VisionScaleParams = (VisionScaleParameters)       InspectionParamRegistry.ByTool("VisionScale").Create();
            SideParams        = (SideInspectionParameters)    InspectionParamRegistry.ByTool("SideInspection").Create();
            DieGapParams      = (DieGapInspectionParameters)  InspectionParamRegistry.ByTool("DieGapInspection").Create();

            // P3 (G2) — Cognex SurfaceInspector 에 Bottom② 주입(소비 단일화). 등록 전 주입 → describe 가 inline Threshold 생략.
            foreach (var m in moduleList)
                foreach (var i in m.Inspectors.Values)
                    if (i is CognexInspector cog && string.Equals(cog.Id, BottomParams.ParameterTarget, StringComparison.OrdinalIgnoreCase))
                        cog.BottomParams = BottomParams;

            // finder/inspector → Snapshot (주입 후 등록)
            foreach (var m in moduleList)
            {
                foreach (var f in m.Finders.Values) store.Register(f as IParameterProvider, ParameterChannel.Snapshot);
                foreach (var i in m.Inspectors.Values) store.Register(i as IParameterProvider, ParameterChannel.Snapshot);
            }

            // ② → Snapshot (인스펙터 다음 — Bottom Threshold 는 inspector 키와 비충돌)
            store.Register(BottomParams,      ParameterChannel.Snapshot);
            store.Register(DistortionParams,  ParameterChannel.Snapshot);
            store.Register(VisionScaleParams, ParameterChannel.Snapshot);
            store.Register(SideParams,        ParameterChannel.Snapshot);
            store.Register(DieGapParams,      ParameterChannel.Snapshot);

            // VisionSettings → VisionConfig 위임(통째 vision.json)
            store.Register(new VisionSettingsParameters(VisionConfigStore.Current), ParameterChannel.VisionConfig);
            store.SetDelegateSave(ParameterChannel.VisionConfig, () => VisionConfigStore.Save());

            // Camera / Lighting → CameraMap 위임(algorithm_camera.json)
            var map = AlgorithmCameraMapStore.Current;
            if (map?.Items != null)
                foreach (var cam in map.Items)
                {
                    store.Register(new CameraParameters(cam), ParameterChannel.CameraMap);
                    if (cam.InspectionLights != null)
                        foreach (var ov in cam.InspectionLights)
                        {
                            if (ov?.Settings == null) continue;
                            foreach (var s in ov.Settings)
                                store.Register(new LightingParameters(ov.InspectionId, s), ParameterChannel.CameraMap);
                        }
                }
            store.SetDelegateSave(ParameterChannel.CameraMap, () => AlgorithmCameraMapStore.Save());

            // 로드(신 경로) — 위임 채널은 기존 스토어가 이미 로드
            store.LoadAll();

            // 마이그레이션: 신 Recipe 파일 비었고 구 Recipes\<tool>.json 있으면 이전(구파일 보존)
            MigrateOld(store, BottomParams,      "BottomInspection");
            MigrateOld(store, DistortionParams,  "Distortion");
            MigrateOld(store, VisionScaleParams, "VisionScale");

            ParameterStoreHost.Current = store;
            return store;
        }

        private static void MigrateOld(ParameterStore store, IParameterProvider registered, string oldToolName)
        {
            try
            {
                string target = registered.ParameterTarget;
                if (!store.RecipeTargetEmpty(target)) return;          // 신 파일 이미 존재 → 스킵
                string oldPath = Path.Combine(OldRecipeDir, oldToolName + ".json");
                if (!File.Exists(oldPath)) return;
                IParameterProvider old = LoadOld(oldToolName, oldPath);
                if (old == null) return;
                foreach (var d in old.DescribeParameters())
                    store.SetValue(target, d.Key, d.Getter());          // 구 값 → 등록 인스턴스
                store.SaveTarget(target);                               // 신 경로 저장(구파일 보존)
            }
            catch { }
        }

        private static IParameterProvider LoadOld(string tool, string path)
        {
            switch (tool)
            {
                case "BottomInspection": return BottomInspectionParameters.LoadJson(path);
                case "Distortion":       return DistortionParameters.LoadJson(path);
                case "VisionScale":      return VisionScaleParameters.LoadJson(path);
                default: return null;
            }
        }
    }
}
