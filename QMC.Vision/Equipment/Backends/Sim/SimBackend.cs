using QMC.Vision.Core;

namespace QMC.Vision.Backends.Sim
{
    /// <summary>Sim 이미지 처리 백엔드 — Finder/Inspector 합성 결과.</summary>
    public class SimBackend : IVisionBackend
    {
        public string Name          => "Sim";
        public string VersionInfo   => "Synthetic matcher";
        public bool   IsInitialized { get; private set; }

        public void Initialize() { IsInitialized = true; }

        public IPatternFinder CreatePatternFinder(string id) => new SimPatternFinder(id);
        public IInspector     CreateInspector   (string id) => new SimInspector(id);

        public void Dispose() { }
    }
}
