using System;
using QMC.Vision.Backends.Cognex;
using QMC.Vision.Backends.OpenCv;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Config;

namespace QMC.Vision.Core
{
    /// <summary>VisionSettings.Provider 에 따라 백엔드 인스턴스를 생성.</summary>
    public static class VisionFactory
    {
        private static IVisionBackend _cached;

        public static IVisionBackend Create(VisionProvider provider)
        {
            switch (provider)
            {
                case VisionProvider.Cognex: return new CognexBackend();
                case VisionProvider.OpenCv: return new OpenCvBackend();
                case VisionProvider.Sim:
                default:                    return new SimBackend();
            }
        }

        /// <summary>전역 싱글톤 백엔드 (설정에 따라 생성/교체).</summary>
        public static IVisionBackend Global
        {
            get
            {
                if (_cached == null)
                {
                    VisionConfigStore.Load();
                    _cached = Create(VisionConfigStore.Current.Provider);
                    _cached.Initialize();
                }
                return _cached;
            }
        }

        /// <summary>백엔드 변경 — Provider 전환 시 기존 것 Dispose.</summary>
        public static void Switch(VisionProvider provider)
        {
            _cached?.Dispose();
            _cached = Create(provider);
            _cached.Initialize();
            VisionConfigStore.Current.Provider = provider;
            VisionConfigStore.Save();
        }
    }
}
