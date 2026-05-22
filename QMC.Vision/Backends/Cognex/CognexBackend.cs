using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex VisionPro 기반 이미지 처리 백엔드.
    /// <para>
    /// 동적 로드:  bin\Cognex.VisionPro.dll, ...PMAlign.dll, ...Blob.dll, ...Caliper.dll, ...ImageProcessing.dll
    /// 검색 경로:  AppDomain BaseDir →  C:\Program Files\Cognex\VisionPro 25.2.0\bin\
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

        public Assembly[] LoadedAssemblies =>
            new[] { VisionProAssembly, PMAlignAssembly, BlobAssembly, CaliperAssembly, ImageProcessingAssembly };

        // ─── DLL 검색 경로 ────────────────────────────────
        private static readonly string[] _searchDirs =
        {
            AppDomain.CurrentDomain.BaseDirectory,
            @"C:\Program Files\Cognex\VisionPro 25.2.0\bin",
            @"C:\Program Files\Cognex\VisionPro\bin",
            @"C:\Program Files (x86)\Cognex\VisionPro\bin",
        };

        private static readonly (string file, string label, bool required)[] _dllSpec =
        {
            ("Cognex.VisionPro.dll",                "Core",            true),
            ("Cognex.VisionPro.PMAlign.dll",        "PMAlign",         true),
            ("Cognex.VisionPro.Blob.dll",           "Blob",            false),
            ("Cognex.VisionPro.Caliper.dll",        "Caliper",         false),
            ("Cognex.VisionPro.ImageProcessing.dll","ImageProcessing", false),
        };

        public void Initialize()
        {
            TryLoad();
            IsInitialized = true;
        }

        private void TryLoad()
        {
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
        public IInspector     CreateInspector   (string id) => new CognexInspector(id, this);

        public void Dispose() { }
    }
}
