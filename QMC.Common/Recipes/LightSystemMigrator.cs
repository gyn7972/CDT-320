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
    /// (C3b-3) 결선(AlgorithmWirings) 제안은 폐기 — 컨트롤러 정의만 생성, 검사별 컨트롤러/페이지는 노드 Setup 지정.
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

            // (C3b-3) 알고리즘 결선(AlgorithmWirings) 휴리스틱 제거 — 결선 개념 폐기, 검사별 컨트롤러/페이지는 노드 Setup 지정.
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

        private static string Safe(List<string> row, int idx)
            => (idx >= 0 && idx < row.Count) ? row[idx] : "";
    }
}
