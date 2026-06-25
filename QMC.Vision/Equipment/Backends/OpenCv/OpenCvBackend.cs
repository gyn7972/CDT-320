using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>
    /// OpenCV(EmguCV) 기반 이미지 처리 백엔드.
    /// EmguCV(Emgu.CV.dll + 네이티브 cvextern.dll) 로드 시 실제 CvInvoke 사용. 미로드 시 SAD/NCC BasicFallback.
    /// 용량이 큰 네이티브를 bin 에 두지 않도록 설정 폴더(OpenCvBinPath, 기본 D:\CDT-320\EmguCV)에서 로드한다.
    /// </summary>
    public class OpenCvBackend : IVisionBackend
    {
        public string Name          => "OpenCV";
        public string VersionInfo   { get; private set; } = "Not initialized";
        public bool   IsInitialized { get; private set; }
        public bool   EmguLoaded    { get; private set; }

        public Assembly EmguAssembly { get; private set; }

        private static string[] _searchDirs = BuildSearchDirs();
        private static bool _resolverHooked;
        private static bool _pathAugmented;

        /// <summary>검색 순서: 설정(OpenCvBinPath) → 기본 D:\CDT-320\EmguCV → 앱 폴더(+x64 하위).</summary>
        private static string[] BuildSearchDirs()
        {
            var dirs = new List<string>();
            void Add(string d) { if (!string.IsNullOrWhiteSpace(d) && !dirs.Contains(d)) dirs.Add(d); }
            void AddWithSub(string root)
            {
                if (string.IsNullOrWhiteSpace(root)) return;
                Add(root);
                Add(Path.Combine(root, "x64"));   // 네이티브가 x64 하위에 있을 수 있음
            }

            try { AddWithSub(VisionConfigStore.Current?.OpenCvBinPath); } catch { }
            AddWithSub(VisionSettings.DefaultOpenCvBinPath);
            AddWithSub(AppDomain.CurrentDomain.BaseDirectory);
            return dirs.ToArray();
        }

        public void Initialize()
        {
            _searchDirs = BuildSearchDirs();
            HookAssemblyResolver();
            AugmentNativePath();      // cvextern.dll(네이티브) 의존성 해결
            TryLoadEmgu();
            IsInitialized = true;
        }

        private void TryLoadEmgu()
        {
            foreach (var dir in _searchDirs)
            {
                foreach (var name in new[] { "Emgu.CV.World.dll", "Emgu.CV.dll" })
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    var path = Path.Combine(dir, name);
                    if (!File.Exists(path)) continue;
                    try
                    {
                        EmguAssembly = Assembly.LoadFrom(path);
                        VersionInfo  = "EmguCV " + (EmguAssembly.GetName().Version?.ToString() ?? "(unknown)")
                                     + "  [" + dir + "]";
                        EmguLoaded = true;
                        return;
                    }
                    catch (Exception ex) { VersionInfo = "EmguCV load failed: " + ex.Message; }
                }
            }
            VersionInfo = "EmguCV not found — BasicFallback (탐색: " + string.Join("; ", _searchDirs) + ")";
        }

        /// <summary>Emgu.CV 의 의존 관리 어셈블리를 검색 폴더에서 해결(비표준 폴더 LoadFrom 대비).</summary>
        private static void HookAssemblyResolver()
        {
            if (_resolverHooked) return;
            _resolverHooked = true;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                try
                {
                    string simpleName = new AssemblyName(e.Name).Name;
                    if (simpleName.IndexOf("Emgu", StringComparison.OrdinalIgnoreCase) < 0) return null;
                    foreach (var dir in _searchDirs)
                    {
                        if (string.IsNullOrEmpty(dir)) continue;
                        var path = Path.Combine(dir, simpleName + ".dll");
                        if (File.Exists(path)) return Assembly.LoadFrom(path);
                    }
                }
                catch { }
                return null;
            };
        }

        /// <summary>네이티브 cvextern.dll 의 P/Invoke 해결을 위해 검색 폴더를 프로세스 PATH 앞에 1회 추가.</summary>
        private static void AugmentNativePath()
        {
            if (_pathAugmented) return;
            try
            {
                string cur = Environment.GetEnvironmentVariable("PATH") ?? "";
                var prepend = new List<string>();
                foreach (var d in _searchDirs)
                    if (!string.IsNullOrEmpty(d) && Directory.Exists(d)
                        && (";" + cur + ";").IndexOf(";" + d + ";", StringComparison.OrdinalIgnoreCase) < 0)
                        prepend.Add(d);
                if (prepend.Count > 0)
                    Environment.SetEnvironmentVariable("PATH", string.Join(";", prepend) + ";" + cur);
                _pathAugmented = true;
            }
            catch { }
        }

        public IPatternFinder CreatePatternFinder(string id) => new OpenCvPatternFinder(id, this);
        public IInspector     CreateInspector   (string id)
            => QMC.Vision.Core.DomainInspectorFactory.TryCreate(id, out var di) ? di : new OpenCvInspector(id, this);

        public void Dispose() { }
    }
}
