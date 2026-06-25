using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex VisionPro 기반 이미지 처리 백엔드.
    /// <para>
    /// 동적 로드:  Cognex.VisionPro.dll, ...PMAlign.dll, ...Blob.dll, ...Caliper.dll, ...ImageProcessing.dll
    /// 검색 경로:  설정(CognexBinPath) → 앱 폴더 → VPRO_ROOT → Program Files\Cognex\VisionPro*\{bin,CogPlus}
    /// (자세한 순서는 <see cref="BuildSearchDirs"/>)
    /// </para>
    /// 미로드 시 OpenCv/Sim fallback (각 Finder/Inspector 가 자동 처리).
    /// </summary>
    public class CognexBackend : IVisionBackend
    {
        public string Name          => "Cognex";
        public string VersionInfo   { get; private set; } = "Not initialized";
        public bool   IsInitialized { get; private set; }
        public bool   CognexLoaded  { get; private set; }

        // ─── 로드된 어셈블리 캐시 ────────────────────────
        public Assembly VisionProAssembly        { get; private set; }
        public Assembly PMAlignAssembly          { get; private set; }
        public Assembly BlobAssembly             { get; private set; }
        public Assembly CaliperAssembly          { get; private set; }
        public Assembly ImageProcessingAssembly  { get; private set; }
        public Assembly ImageFileAssembly        { get; private set; }   // CogImageFileBMP(Bitmap→ICogImage 변환) 보유

        public Assembly[] LoadedAssemblies =>
            new[] { VisionProAssembly, PMAlignAssembly, BlobAssembly, CaliperAssembly, ImageProcessingAssembly, ImageFileAssembly };

        // ─── DLL 검색 경로 ────────────────────────────────
        /// <summary>
        /// 검색 순서: ① 설정(CognexBinPath) → ② 앱 폴더 → ③ 환경변수 VPRO_ROOT →
        /// ④ Program Files\Cognex\VisionPro* 의 각 버전 폴더와 그 하위(bin / CogPlus).
        /// VisionPro 버전·설치 위치가 달라도 자동 인식하도록 정적 하드코딩 대신 동적으로 구성.
        /// </summary>
        private static string[] BuildSearchDirs()
        {
            var dirs = new List<string>();
            void Add(string d) { if (!string.IsNullOrWhiteSpace(d) && !dirs.Contains(d)) dirs.Add(d); }
            // 한 폴더에 대해 자신 + 표준 하위 폴더 변형을 모두 추가.
            // CogPlus = 코어/PMAlign, ReferencedAssemblies = Blob/Caliper/ImageProcessing 및 의존 DLL.
            void AddWithSub(string root)
            {
                if (string.IsNullOrWhiteSpace(root)) return;
                Add(root);
                Add(Path.Combine(root, "bin"));
                Add(Path.Combine(root, "bin", "Dependencies"));
                Add(Path.Combine(root, "CogPlus"));
                Add(Path.Combine(root, "ReferencedAssemblies"));
            }

            // ① 설정에서 직접 지정한 경로(최우선)
            try { AddWithSub(VisionConfigStore.Current?.CognexBinPath); } catch { }

            // ② 앱 실행 폴더(DLL 을 bin\Debug 에 복사해 둔 경우)
            Add(AppDomain.CurrentDomain.BaseDirectory);

            // ③ 환경변수
            try { AddWithSub(Environment.GetEnvironmentVariable("VPRO_ROOT")); } catch { }

            // ④ 표준 설치 루트 하위의 VisionPro* 버전 폴더 자동 탐색
            string[] cognexRoots =
            {
                @"C:\Program Files\Cognex\VisionPro",
                @"C:\Program Files (x86)\Cognex\VisionPro",
            };
            foreach (var cr in cognexRoots)
            {
                AddWithSub(cr);                                 // VisionPro\bin, VisionPro\CogPlus
                try
                {
                    var parent = Path.GetDirectoryName(cr);     // ...\Cognex
                    if (Directory.Exists(parent))
                        foreach (var ver in Directory.GetDirectories(parent, "VisionPro*"))
                            AddWithSub(ver);                     // VisionPro 25.2.0\bin, ...\CogPlus 등
                }
                catch { }
            }
            return dirs.ToArray();
        }

        private static string[] _searchDirs = BuildSearchDirs();

        private static bool _pathAugmented;
        /// <summary>
        /// VisionPro 관리 어셈블리는 네이티브 DLL(bin, bin\Dependencies)을 P/Invoke 한다.
        /// 비표준 설치라 시스템 PATH 에 없을 수 있으므로, 존재하는 검색 폴더를 프로세스 PATH 앞에 1회 추가.
        /// </summary>
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

        private static readonly (string file, string label, bool required)[] _dllSpec =
        {
            ("Cognex.VisionPro.dll",                "Core",            true),
            ("Cognex.VisionPro.PMAlign.dll",        "PMAlign",         true),
            ("Cognex.VisionPro.Blob.dll",           "Blob",            false),
            ("Cognex.VisionPro.Caliper.dll",        "Caliper",         false),
            ("Cognex.VisionPro.ImageProcessing.dll","ImageProcessing", false),
            ("Cognex.VisionPro.ImageFile.dll",      "ImageFile",       false),   // CogImageFileBMP — Bitmap→ICogImage 변환에 필요
        };

        private static bool _resolverHooked;

        public void Initialize()
        {
            HookAssemblyResolver();
            TryLoad();
            IsInitialized = true;
        }

        /// <summary>
        /// Cognex 어셈블리를 비표준 폴더(CogPlus 등)에서 LoadFrom 하면 그 의존 어셈블리(PMAlign→Core 등)를
        /// CLR 이 GAC/앱폴더에서만 찾아 실패할 수 있다. 검색 폴더들에서 직접 해결하도록 1회 등록.
        /// </summary>
        private static void HookAssemblyResolver()
        {
            if (_resolverHooked) return;
            _resolverHooked = true;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                try
                {
                    string simpleName = new AssemblyName(e.Name).Name;
                    if (!simpleName.StartsWith("Cognex", StringComparison.OrdinalIgnoreCase)) return null;
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

        private void TryLoad()
        {
            _searchDirs = BuildSearchDirs();   // Initialize 시점의 설정(CognexBinPath)·설치 폴더를 재반영
            AugmentNativePath();               // 네이티브(P/Invoke) DLL 의존성을 위해 PATH 보강
            var sb = new System.Text.StringBuilder();
            int loaded = 0;
            foreach (var spec in _dllSpec)
            {
                var asm = LoadDll(spec.file, out string err);
                switch (spec.label)
                {
                    case "Core":            VisionProAssembly       = asm; break;
                    case "PMAlign":         PMAlignAssembly         = asm; break;
                    case "Blob":            BlobAssembly            = asm; break;
                    case "Caliper":        CaliperAssembly         = asm; break;
                    case "ImageProcessing": ImageProcessingAssembly = asm; break;
                    case "ImageFile":       ImageFileAssembly       = asm; break;
                }
                if (asm != null)
                {
                    loaded++;
                    sb.Append(spec.label).Append("=").Append(asm.GetName().Version).Append(" ");
                }
                else if (spec.required)
                {
                    VersionInfo  = $"Cognex {spec.label} 로드 실패 — OpenCv/Sim fallback. ({err})";
                    CognexLoaded = false;
                    return;
                }
            }
            CognexLoaded = (VisionProAssembly != null && PMAlignAssembly != null);
            VersionInfo  = CognexLoaded
                ? "Cognex VisionPro " + VisionProAssembly.GetName().Version + "  [" + sb.ToString().Trim() + "]"
                : "Cognex VisionPro not found — OpenCv/Sim fallback";
        }

        private static Assembly LoadDll(string fileName, out string err)
        {
            err = null;
            foreach (var dir in _searchDirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                var path = Path.Combine(dir, fileName);
                if (!File.Exists(path)) continue;
                try { return Assembly.LoadFrom(path); }
                catch (Exception ex) { err = ex.Message; }
            }
            if (err == null) err = "not found in: " + string.Join("; ", _searchDirs);
            return null;
        }

        public IPatternFinder CreatePatternFinder(string id) => new CognexPatternFinder(id, this);
        public IInspector     CreateInspector   (string id)
            => QMC.Vision.Core.DomainInspectorFactory.TryCreate(id, out var di)   // 310 포팅(Bottom/Side/Placement)
                ? di
                : new CognexInspector(id, this);

        public void Dispose() { }
    }
}
