using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 이미지 처리(Finder/Inspector) 백엔드. 카메라는 별도 <see cref="ICamera"/> 로 분리.
    /// 현재 지원: Sim / OpenCV / Cognex VisionPro.
    /// </summary>
    public interface IVisionBackend : IDisposable
    {
        string Name        { get; }
        string VersionInfo { get; }
        bool   IsInitialized { get; }

        void Initialize();

        IPatternFinder CreatePatternFinder(string id);
        IInspector     CreateInspector    (string id);
    }
}
