using System;
using System.IO;
using System.Reflection;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>
    /// OpenCV(EmguCV) 기반 이미지 처리 백엔드.
    /// EmguCV 설치 시 실제 CvInvoke 사용. 미설치 시 SAD 기반 BasicFallback.
    /// </summary>
    public class OpenCvBackend : IVisionBackend
    {
        public string Name          => "OpenCV";
        public string VersionInfo   { get; private set; } = "Not initialized";
        public bool   IsInitialized { get; private set; }
        public bool   EmguLoaded    { get; private set; }

        private Assembly _emgu;

        public void Initialize()
        {
            TryLoadEmgu();
            IsInitialized = true;
        }

        private void TryLoadEmgu()
        {
            foreach (var name in new[] { "Emgu.CV.World.dll", "Emgu.CV.dll" })
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
                if (!File.Exists(path)) continue;
                try
                {
                    _emgu = Assembly.LoadFrom(path);
                    VersionInfo = "EmguCV " + (_emgu.GetName().Version?.ToString() ?? "(unknown)");
                    EmguLoaded = true;
                    return;
                }
                catch (Exception ex) { VersionInfo = "EmguCV load failed: " + ex.Message; }
            }
            VersionInfo = "EmguCV not found — BasicFallback";
        }

        public IPatternFinder CreatePatternFinder(string id) => new OpenCvPatternFinder(id, this);
        public IInspector     CreateInspector   (string id) => new OpenCvInspector(id, this);

        public void Dispose() { }
    }
}
