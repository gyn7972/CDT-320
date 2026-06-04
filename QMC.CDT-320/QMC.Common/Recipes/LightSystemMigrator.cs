using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Common.Recipes
{
    /// <summary>
    /// Stage 69 — 레거시 io_set.lightSource.json (Columns/Rows 그리드) → LightSystemSetup 변환.
    /// PORT 별 그룹핑으로 LightControllerEntry 생성 + 채널 라벨 채움.
    /// 채널 이름 휴리스틱으로 AlgorithmWirings 초기 제안 (모호 항목은 미할당 — 사용자 검토).
    /// </summary>
    public static class LightSystemMigrator
    {
        [DataContract]
        private class LegacyGrid
        {
            [DataMember] public List<string>       Columns { get; set; }
            [DataMember] public List<List<string>> Rows    { get; set; }
        }

        /// <summary>io_set 경로에서 Setup 생성. 실패/미존재 시 null.</summary>
        public static LightSystemSetup MigrateFromLegacy(string ioSetPath)
        {
            if (string.IsNullOrEmpty(ioSetPath) || !File.Exists(ioSetPath)) return null;
            LegacyGrid grid;
            try
            {
                using (var fs = File.OpenRead(ioSetPath))
                {
                    var ser = new DataContractJsonSerializer(typeof(LegacyGrid));
                    grid = (LegacyGrid)ser.ReadObject(fs);
                }
            }
            catch { return null; }
            if (grid?.Columns == null || grid.Rows == null) return null;

            int iIdx  = grid.Columns.FindIndex(c => c.Equals("INDEX", StringComparison.OrdinalIgnoreCase));
            int iName = grid.Columns.FindIndex(c => c.Equals("NAME",  StringComparison.OrdinalIgnoreCase));
            int iPort = grid.Columns.FindIndex(c => c.Equals("PORT",  StringComparison.OrdinalIgnoreCase));
            int iLv   = grid.Columns.FindIndex(c => c.Equals("LEVEL", StringComparison.OrdinalIgnoreCase));
            if (iName < 0 || iPort < 0) return null;

            var setup = new LightSystemSetup();

            // PORT 별 그룹핑 → 컨트롤러 생성. 채널은 포트 내 1-기반 재부여.
            foreach (var row in grid.Rows)
            {
                if (row == null || row.Count <= iPort) continue;
                string port = Safe(row, iPort);
                string name = Safe(row, iName);
                if (string.IsNullOrEmpty(port)) continue;

                var ctrl = setup.GetController(port);
                if (ctrl == null)
                {
                    ctrl = new LightControllerEntry { PortName = port, Name = port + " Illuminator" };
                    setup.Controllers.Add(ctrl);
                }
                int ch = ctrl.ChannelLabels.Count + 1;   // 포트 내 채널 1-기반 재부여
                ctrl.ChannelLabels.Add(new LightChannelLabel { Channel = ch, Name = name });
            }
            // 각 컨트롤러 ChannelCount = 채널 라벨 수
            foreach (var c in setup.Controllers)
                c.ChannelCount = Math.Max(1, c.ChannelLabels.Count);

            // 알고리즘 결선 휴리스틱 — 채널 이름에서 알고리즘 추정. 모호하면 미할당.
            setup.EnsureWirings();
            foreach (var c in setup.Controllers)
            {
                foreach (var lbl in c.ChannelLabels)
                {
                    string alg = GuessAlgorithm(lbl.Name);
                    if (alg == null) continue;     // 모호 — 사용자 검토
                    var w = setup.GetWiring(alg);
                    if (w == null) continue;
                    // Stage 81 — 다중 컨트롤러: 알고리즘에 이 포트의 ControllerSet 을 get-or-create 후 채널 추가.
                    var cs = w.GetSet(c.PortName);
                    if (cs == null) { cs = new ControllerChannels { ControllerPort = c.PortName }; w.ControllerSets.Add(cs); }
                    if (!cs.Channels.Contains(lbl.Channel)) cs.Channels.Add(lbl.Channel);
                }
            }
            return setup;
        }

        /// <summary>원본 백업 (.bak.YYYYMMDD). 이미 있으면 덮지 않음.</summary>
        public static void BackupLegacy(string ioSetPath, string yyyymmdd)
        {
            try
            {
                if (!File.Exists(ioSetPath)) return;
                string bak = ioSetPath + ".bak." + yyyymmdd;
                if (!File.Exists(bak)) File.Copy(ioSetPath, bak);
            }
            catch { }
        }

        /// <summary>채널 이름 → 알고리즘 추정. 모호하면 null.</summary>
        public static string GuessAlgorithm(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return null;
            string n = channelName.ToUpperInvariant();
            if (n.Contains("WAFER")) return VisionAlgorithm.Wafer;
            if (n.Contains("BIN"))   return VisionAlgorithm.Bin;
            // "BOTTOM SIDE" / "REAR SIDE" 먼저 (BOTTOM VISION 보다 구체적)
            if (n.Contains("BOTTOM SIDE") || n.Contains("REAR SIDE")) return VisionAlgorithm.RearSide;
            if (n.Contains("TOP SIDE")    || n.Contains("FRONT SIDE")) return VisionAlgorithm.FrontSide;
            if (n.Contains("BOTTOM VISION") || n.Contains("BOTTOM INSP")) return VisionAlgorithm.BottomInspection;
            // RING / ALIGN MARK 등 모호 → null (사용자 검토)
            return null;
        }

        private static string Safe(List<string> row, int idx)
            => (idx >= 0 && idx < row.Count) ? row[idx] : "";
    }
}
