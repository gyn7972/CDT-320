using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QMC.Vision.Core
{
    /// <summary>
    /// CPU(프로세스)/GPU(시스템)/메모리 부하 모니터 — PC 사양 산정용.
    /// PerformanceCounter 없이 Process.TotalProcessorTime 델타로 CPU% 계산(권한 불필요).
    /// GPU%는 Windows "GPU Engine" 성능 카운터(벤더 무관)로 시스템 전체 사용률을 산출한다(미지원 환경이면 비활성).
    /// 1초 간격으로 <see cref="Sample"/> 호출하면 현재/피크 값을 갱신한다.
    /// </summary>
    public sealed class ResourceMonitor
    {
        private const string GpuCategoryName = "GPU Engine";
        private const string GpuCounterName  = "Utilization Percentage";
        private const int    GpuSampleMs     = 2000;   // GPU 샘플 주기(스로틀) — 카운터 열거 비용 절감

        private readonly Process _proc = Process.GetCurrentProcess();
        private DateTime _lastT;
        private TimeSpan _lastCpu;

        // GPU 카운터(엔진 인스턴스별)는 rate 계산을 위해 재사용해야 하므로 캐시한다(3D/Compute 엔진만).
        private readonly Dictionary<string, PerformanceCounter> _gpuCounters = new Dictionary<string, PerformanceCounter>();
        private DateTime _lastGpuT;
        private bool _gpuInit;
        private bool _gpuAvailable;

        public double CpuPercent       { get; private set; }   // 전 코어 대비 % (0~100)
        public double GpuPercent       { get; private set; }   // 시스템 GPU 사용률 % (0~100, 가장 바쁜 엔진)
        public double WorkingSetMB     { get; private set; }
        public double PrivateMB        { get; private set; }
        public double GcHeapMB         { get; private set; }
        public int    Threads          { get; private set; }
        public int    Handles          { get; private set; }
        public double PeakCpu          { get; private set; }
        public double PeakGpu          { get; private set; }
        public double PeakWorkingSetMB { get; private set; }
        public double DiskCFreeGB      { get; private set; }   // C: 여유 (없으면 -1)
        public double DiskDFreeGB      { get; private set; }   // D: 여유 (없으면 -1)
        public bool   GpuAvailable => _gpuAvailable;           // GPU Engine 카운터 사용 가능 여부
        public int    CoreCount => Environment.ProcessorCount;
        public TimeSpan Uptime { get { try { return DateTime.Now - _proc.StartTime; } catch { return TimeSpan.Zero; } } }

        public ResourceMonitor()
        {
            _lastT   = DateTime.UtcNow;
            try { _lastCpu = _proc.TotalProcessorTime; } catch { _lastCpu = TimeSpan.Zero; }
        }

        /// <summary>현재값 샘플링(주기 호출). 직전 호출과의 시간차로 CPU% 산출.</summary>
        public void Sample()
        {
            try
            {
                _proc.Refresh();
                var now = DateTime.UtcNow;
                var cpu = _proc.TotalProcessorTime;
                double secs = (now - _lastT).TotalSeconds;
                if (secs > 0.001)
                {
                    double used = (cpu - _lastCpu).TotalSeconds;
                    double pct  = used / (secs * Math.Max(1, Environment.ProcessorCount)) * 100.0;
                    CpuPercent  = pct < 0 ? 0 : (pct > 100 ? 100 : pct);
                }
                _lastT = now; _lastCpu = cpu;

                WorkingSetMB = _proc.WorkingSet64        / (1024.0 * 1024.0);
                PrivateMB    = _proc.PrivateMemorySize64 / (1024.0 * 1024.0);
                GcHeapMB     = GC.GetTotalMemory(false)  / (1024.0 * 1024.0);
                Threads      = _proc.Threads.Count;
                Handles      = _proc.HandleCount;

                SampleGpu();

                if (CpuPercent   > PeakCpu)          PeakCpu = CpuPercent;
                if (GpuPercent   > PeakGpu)          PeakGpu = GpuPercent;
                if (WorkingSetMB > PeakWorkingSetMB) PeakWorkingSetMB = WorkingSetMB;

                DiskCFreeGB = DriveFreeGB("C:\\");
                DiskDFreeGB = DriveFreeGB("D:\\");
            }
            catch { }
        }

        /// <summary>피크 초기화.</summary>
        public void ResetPeaks() { PeakCpu = 0; PeakGpu = 0; PeakWorkingSetMB = 0; }

        /// <summary>
        /// 시스템 전체 GPU 사용률(%) 샘플링. 비용 절감을 위해 부하 지표가 되는 3D/Compute 엔진 인스턴스만
        /// 엔진별로 합산한 뒤 가장 바쁜 엔진 값을 사용한다(Windows 작업 관리자 헤드라인과 유사). 미지원/실패 시 0.
        /// 카운터 열거 비용이 크므로 <see cref="GpuSampleMs"/> 주기로만 갱신하고 그 사이엔 직전 값을 유지한다.
        /// </summary>
        private void SampleGpu()
        {
            if (!_gpuInit)
            {
                try { _gpuAvailable = PerformanceCounterCategory.Exists(GpuCategoryName); }
                catch { _gpuAvailable = false; }   // 구버전 Windows·권한 등으로 미제공 — 모니터링만 비활성
                _gpuInit = true;
            }
            if (!_gpuAvailable) { GpuPercent = 0; return; }

            // 스로틀: 주기 미도달이면 직전 값 유지(첫 호출은 _lastGpuT 기본값이라 즉시 수행)
            var nowT = DateTime.UtcNow;
            if (_gpuCounters.Count > 0 && (nowT - _lastGpuT).TotalMilliseconds < GpuSampleMs) return;
            _lastGpuT = nowT;

            try
            {
                var category = new PerformanceCounterCategory(GpuCategoryName);
                var live = new HashSet<string>();
                var byEngine = new Dictionary<string, double>();   // engtype별 사용률 합

                foreach (var name in category.GetInstanceNames())
                {
                    if (!IsLoadEngine(name)) continue;             // 3D/Compute 외 엔진은 제외 — 인스턴스 수 급감
                    live.Add(name);
                    if (!_gpuCounters.TryGetValue(name, out var pc))
                    {
                        // 신규 인스턴스: 기준점만 잡고(첫 NextValue는 0) 다음 주기부터 합산
                        pc = new PerformanceCounter(GpuCategoryName, GpuCounterName, name, readOnly: true);
                        _gpuCounters[name] = pc;
                        try { pc.NextValue(); } catch { }
                        continue;
                    }

                    double val;
                    try { val = pc.NextValue(); } catch { continue; }
                    if (val <= 0) continue;

                    string engine = EngineTypeOf(name);
                    byEngine.TryGetValue(engine, out double acc);
                    byEngine[engine] = acc + val;
                }

                // 종료된 프로세스/엔진 인스턴스 카운터 정리
                if (_gpuCounters.Count > live.Count)
                {
                    var dead = new List<string>();
                    foreach (var key in _gpuCounters.Keys) if (!live.Contains(key)) dead.Add(key);
                    foreach (var key in dead)
                    {
                        try { _gpuCounters[key].Dispose(); } catch { }
                        _gpuCounters.Remove(key);
                    }
                }

                double max = 0;
                foreach (var v in byEngine.Values) if (v > max) max = v;
                GpuPercent = max > 100 ? 100 : max;
            }
            catch
            {
                // 모니터링 실패가 UI/시퀀스를 멈추면 안 되므로 흡수하고 다음 주기에 재시도
                GpuPercent = 0;
            }
        }

        /// <summary>부하 지표 엔진(3D/Compute)인지 — 그 외(Copy/VideoDecode 등)는 비용 절감 위해 제외.</summary>
        private static bool IsLoadEngine(string instanceName)
        {
            string e = EngineTypeOf(instanceName);
            return string.Equals(e, "3D", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e, "Compute", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>"GPU Engine" 인스턴스명에서 engtype_xxx 엔진 종류 추출(없으면 "etc").</summary>
        private static string EngineTypeOf(string instanceName)
        {
            const string tag = "engtype_";
            int i = instanceName.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
            return i < 0 ? "etc" : instanceName.Substring(i + tag.Length);
        }

        private static double DriveFreeGB(string root)
        {
            try { var di = new System.IO.DriveInfo(root); if (di.IsReady) return di.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0); }
            catch { }
            return -1;
        }

        /// <summary>C:/D: 여유 디스크 표기(준비된 드라이브만).</summary>
        private string DiskText()
        {
            string s = "";
            if (DiskCFreeGB >= 0) s += $"C {DiskCFreeGB:F0}GB ";
            if (DiskDFreeGB >= 0) s += $"D {DiskDFreeGB:F0}GB";
            return s.Trim();
        }

        /// <summary>GPU 짧은 표기(미지원 시 'n/a').</summary>
        private string GpuText()
            => _gpuAvailable ? $"GPU {GpuPercent:F0}%·peak{PeakGpu:F0}%" : "GPU n/a";

        /// <summary>상태바용 짧은 표기(CPU/GPU/MEM/DISK).</summary>
        public string ShortText()
            => $"CPU {CpuPercent:F0}%·peak{PeakCpu:F0}%   {GpuText()}   MEM {WorkingSetMB:F0}·peak{PeakWorkingSetMB:F0}MB   DISK {DiskText()}";

        /// <summary>툴팁용 상세.</summary>
        public string DetailText()
            => $"코어 {CoreCount}개\nCPU {CpuPercent:F1}% / peak {PeakCpu:F1}%\n"
             + $"GPU {(_gpuAvailable ? $"{GpuPercent:F1}% / peak {PeakGpu:F1}%" : "미지원")}\n"
             + $"WorkingSet {WorkingSetMB:F0}MB / peak {PeakWorkingSetMB:F0}MB\n"
             + $"Private {PrivateMB:F0}MB  GC {GcHeapMB:F0}MB\n"
             + $"Threads {Threads}  Handles {Handles}\n"
             + $"DISK {DiskText()} 여유\n가동 {Uptime:hh\\:mm\\:ss}";

        public static string CsvHeader() => "time,cpu_pct,gpu_pct,working_mb,private_mb,gc_mb,threads,handles,disk_c_gb,disk_d_gb";
        public string CsvLine()
            => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{CpuPercent:F1},{GpuPercent:F1},{WorkingSetMB:F1},{PrivateMB:F1},{GcHeapMB:F1},{Threads},{Handles},{DiskCFreeGB:F1},{DiskDFreeGB:F1}";
    }
}
