using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.Common.Recipes;
using QMC.Vision.Optics;
using QMC.Vision.Optics.LFine;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// Stage 69 — 프로세스 전역 조명 컨트롤러 허브 (VisionHub 패턴).
    /// PortName → ILightController 매핑. Form1.Load 가 LightSystemSetup.Controllers 순회하여 등록.
    /// 호출자는 알고리즘 결선의 ControllerPort 로 Get(port) 라우팅.
    /// </summary>
    public static class LightHub
    {
        private static readonly Dictionary<string, ILightController> _byPort
            = new Dictionary<string, ILightController>(StringComparer.OrdinalIgnoreCase);

        public static event Action<string> Log;

        /// <summary>Setup 의 컨트롤러들을 인스턴스화하여 등록. useSim 시 전부 Sim.
        /// Stage 77 — entry.Vendor 로 벤더별 컨트롤러 생성 (Factory 가 분기).</summary>
        public static void Initialize(LightSystemSetup setup, bool useSim)
        {
            DisposeAll();
            if (setup?.Controllers == null) return;
            foreach (var entry in setup.Controllers)
            {
                if (string.IsNullOrEmpty(entry.PortName)) continue;
                ILightController ctrl = LightControllerFactory.Create(entry, useSim);
                _byPort[entry.PortName] = ctrl;
                Emit($"register {entry.PortName} [{entry.Vendor}] ({ctrl.GetType().Name})");
            }
        }

        /// <summary>Stage 73 — 등록된 모든 컨트롤러의 시리얼 Open. port → 성공여부 맵 반환.
        /// (Sim 컨트롤러는 항상 true. 실장비는 LIGHT-OPEN-FAIL 알람 후 false 가능.)</summary>
        public static async Task<Dictionary<string, bool>> ConnectAllAsync()
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _byPort)
            {
                bool ok;
                try { ok = await kv.Value.ConnectAsync().ConfigureAwait(false); } catch { ok = false; }
                result[kv.Key] = ok;
                Emit($"connect {kv.Key} => {ok}");
            }
            return result;
        }

        /// <summary>Stage 73 — 등록된 모든 컨트롤러의 시리얼 Close.</summary>
        public static async Task DisconnectAllAsync()
        {
            foreach (var c in _byPort.Values)
            {
                try { await c.DisconnectAsync().ConfigureAwait(false); } catch { }
            }
        }

        /// <summary>포트로 컨트롤러 조회. 미등록 시 null.</summary>
        public static ILightController Get(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return null;
            _byPort.TryGetValue(portName, out var c);
            return c;
        }

        public static IReadOnlyCollection<string> Ports => _byPort.Keys;

        public static void DisposeAll()
        {
            foreach (var c in _byPort.Values) { try { c.Dispose(); } catch { } }
            _byPort.Clear();
        }

        private static void Emit(string msg) { try { Log?.Invoke("[LightHub] " + msg); } catch { } }
    }
}
