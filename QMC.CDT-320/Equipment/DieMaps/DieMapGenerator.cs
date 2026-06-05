using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using QMC.CDT320.Materials;
using QMC.Common.Data.Store;

namespace QMC.CDT320.DieMaps
{
    /// <summary>
    /// DieMap 생성/저장/로드. 310 의 DieMapGenerateServiceExecutor 와 동등 기능.
    /// </summary>
    public static class DieMapGenerator
    {
        /// <summary>
        /// 300mm 웨이퍼(또는 임의 직경) 모양의 원형 다이맵 생성.
        /// 다이 크기 + 간격(gap) 으로 pitch 계산 후 격자 생성, 원 밖 다이는 IsTarget=false 로 비활성화.
        /// </summary>
        /// <param name="waferDiameterMm">웨이퍼 직경 [mm] (예: 300)</param>
        /// <param name="dieSizeXMm">다이 X 사이즈 [mm] (예: 8.12)</param>
        /// <param name="dieSizeYMm">다이 Y 사이즈 [mm] (예: 6.12)</param>
        /// <param name="gapXMm">X 간격 [mm] (예: 0.05)</param>
        /// <param name="gapYMm">Y 간격 [mm] (예: 0.05)</param>
        /// <param name="frameObjId">FrameObjId (예: "INPUT" / "OUTPUT")</param>
        public static DieMap GenerateWafer(double waferDiameterMm,
                                           double dieSizeXMm, double dieSizeYMm,
                                           double gapXMm, double gapYMm,
                                           string frameObjId = "WAFER")
        {
            double pitchX = dieSizeXMm + gapXMm;
            double pitchY = dieSizeYMm + gapYMm;
            double radius = waferDiameterMm / 2.0;
            // 사각 외접 격자 셀 수 (원 직경을 pitch 로 나눔)
            int gx = (int)Math.Floor(waferDiameterMm / pitchX);
            int gy = (int)Math.Floor(waferDiameterMm / pitchY);
            if (gx < 1) gx = 1;
            if (gy < 1) gy = 1;

            double originX = -gx * pitchX / 2.0;   // 격자 중심을 (0,0) 으로
            double originY = -gy * pitchY / 2.0;

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                GridX  = gx,
                GridY  = gy,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
            };

            int idx = 0;
            for (int y = 0; y < gy; y++)
            {
                for (int x = 0; x < gx; x++)
                {
                    // 셀 중심 좌표 (원 중심 기준)
                    double cx = originX + (x + 0.5) * pitchX;
                    double cy = originY + (y + 0.5) * pitchY;
                    // 원 안 판정: 셀 중심이 반지름 안에 있어야 활성
                    double distSq = cx * cx + cy * cy;
                    bool isTarget = distSq <= radius * radius;

                    map.Entries.Add(new DieMapEntry
                    {
                        Index    = idx++,
                        GridX    = x,
                        GridY    = y,
                        IsTarget = isTarget,
                        Result   = DieResult.Unknown,
                        BinCode  = 0,
                        X        = cx,
                        Y        = cy
                    });
                }
            }
            return map;
        }

        /// <summary>
        /// 사각 트레이 다이맵 생성 (Output BIN 용). 원 판정 없이 모든 셀 활성.
        /// </summary>
        public static DieMap GenerateRect(int gridX, int gridY,
                                          double dieSizeXMm, double dieSizeYMm,
                                          double gapXMm, double gapYMm,
                                          string frameObjId = "RECT")
        {
            double pitchX = dieSizeXMm + gapXMm;
            double pitchY = dieSizeYMm + gapYMm;
            if (gridX < 1) gridX = 1;
            if (gridY < 1) gridY = 1;
            double originX = -gridX * pitchX / 2.0;
            double originY = -gridY * pitchY / 2.0;

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                GridX  = gridX,
                GridY  = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
            };

            int idx = 0;
            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    double cx = originX + (x + 0.5) * pitchX;
                    double cy = originY + (y + 0.5) * pitchY;
                    map.Entries.Add(new DieMapEntry
                    {
                        Index    = idx++,
                        GridX    = x,
                        GridY    = y,
                        IsTarget = true,
                        Result   = DieResult.Unknown,
                        BinCode  = 0,
                        X        = cx,
                        Y        = cy
                    });
                }
            }
            return map;
        }

        /// <summary>
        /// 격자 기반 다이 맵 생성. 좌상단 (0,0) 에서 시작, x→오른쪽 / y→아래.
        /// 모터 좌표는 (originX + gx*pitchX, originY + gy*pitchY).
        /// 회전 적용: TapeFrame.Rotate 가 0/90/180/270 일 때 격자 인덱스 변환.
        /// </summary>
        public static DieMap Generate(DieTapeFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            int gx = Math.Max(1, frame.GridX);
            int gy = Math.Max(1, frame.GridY);

            var map = new DieMap
            {
                FrameObjId = frame.ObjId,
                GridX  = gx,
                GridY  = gy,
                PitchX = frame.PitchX,
                PitchY = frame.PitchY,
                OriginX= frame.OriginX,
                OriginY= frame.OriginY,
            };

            int idx = 0;
            for (int y = 0; y < gy; y++)
                for (int x = 0; x < gx; x++)
                {
                    // 회전 적용 — 회전 후 모터 좌표.
                    double mx, my;
                    ApplyRotation(frame.Rotate, x, y, gx, gy, frame.PitchX, frame.PitchY, out mx, out my);
                    map.Entries.Add(new DieMapEntry
                    {
                        Index    = idx++,
                        GridX    = x,
                        GridY    = y,
                        IsTarget = true,
                        Result   = DieResult.Unknown,
                        BinCode  = 0,
                        X        = frame.OriginX + mx,
                        Y        = frame.OriginY + my
                    });
                }
            return map;
        }

        private static void ApplyRotation(TapeFrameRotate rot, int gx, int gy, int totalX, int totalY,
                                          double pitchX, double pitchY, out double mx, out double my)
        {
            double bx = gx * pitchX;
            double by = gy * pitchY;
            switch (rot)
            {
                case TapeFrameRotate.None:
                    mx = bx; my = by; break;
                case TapeFrameRotate.R90:
                    mx = by;
                    my = (totalX - 1) * pitchX - bx;
                    break;
                case TapeFrameRotate.R180:
                    mx = (totalX - 1) * pitchX - bx;
                    my = (totalY - 1) * pitchY - by;
                    break;
                case TapeFrameRotate.R270:
                    mx = (totalY - 1) * pitchY - by;
                    my = bx;
                    break;
                default:
                    mx = bx; my = by; break;
            }
        }

        /// <summary>CSV 로 저장 (310 의 OutputDieMapDataSaver 동등).</summary>
        public static void SaveCsv(DieMap map, string path)
        {
            if (map == null) return;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("FrameObjId," + map.FrameObjId);
                    sw.WriteLine($"GridX,{map.GridX}");
                    sw.WriteLine($"GridY,{map.GridY}");
                    sw.WriteLine($"PitchX,{map.PitchX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"PitchY,{map.PitchY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OriginX,{map.OriginX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OriginY,{map.OriginY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"CreatedAt,{map.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine();
                    sw.WriteLine("Index,GridX,GridY,IsTarget,Result,BinCode,X,Y,DieUid");
                    foreach (var e in map.Entries)
                    {
                        sw.WriteLine(string.Join(",",
                            e.Index, e.GridX, e.GridY, e.IsTarget, e.Result,
                            e.BinCode,
                            e.X.ToString(CultureInfo.InvariantCulture),
                            e.Y.ToString(CultureInfo.InvariantCulture),
                            e.DieUid));
                    }
                }
            }
            catch { }
        }

        /// <summary>JSON 직렬화 저장 (raw).</summary>
        public static void SaveJson(DieMap map, string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var fs = File.Create(path))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(DieMap), map);
                }
            }
            catch { }
        }

        /// <summary>JSON 직렬화 로드.</summary>
        public static DieMap LoadJson(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(DieMap));
                    return (DieMap)ser.ReadObject(fs);
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// CSV 임포트 (Stage 22) — SaveCsv 가 만든 형식 또는 표준 SECS map CSV 둘 다 지원.
        /// 표준 형식: header section (key,value) + 빈 줄 + entries (Index,GridX,GridY,IsTarget,Result,BinCode,X,Y,DieUid).
        /// </summary>
        public static DieMap LoadCsv(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var lines = File.ReadAllLines(path);
                var map = new DieMap();
                int i = 0;
                // Header section
                for (; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) { i++; break; }
                    var parts = line.Split(new[] { ',' }, 2);
                    if (parts.Length < 2) continue;
                    string k = parts[0].Trim(), v = parts[1].Trim();
                    if (k.Equals("FrameObjId",  StringComparison.OrdinalIgnoreCase)) map.FrameObjId = v;
                    else if (k.Equals("GridX",  StringComparison.OrdinalIgnoreCase)) map.GridX   = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("GridY",  StringComparison.OrdinalIgnoreCase)) map.GridY   = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("PitchX", StringComparison.OrdinalIgnoreCase)) map.PitchX  = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("PitchY", StringComparison.OrdinalIgnoreCase)) map.PitchY  = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OriginX",StringComparison.OrdinalIgnoreCase)) map.OriginX = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OriginY",StringComparison.OrdinalIgnoreCase)) map.OriginY = double.Parse(v, CultureInfo.InvariantCulture);
                }
                // Skip column header
                if (i < lines.Length && lines[i].StartsWith("Index", StringComparison.OrdinalIgnoreCase)) i++;

                // Entries
                while (i < lines.Length)
                {
                    var line = lines[i++].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var p = line.Split(',');
                    if (p.Length < 8) continue;
                    var entry = new DieMapEntry
                    {
                        Index    = int.TryParse(p[0], out var idx) ? idx : 0,
                        GridX    = int.TryParse(p[1], out var gx) ? gx : 0,
                        GridY    = int.TryParse(p[2], out var gy) ? gy : 0,
                        IsTarget = bool.TryParse(p[3], out var isT) ? isT : true,
                        Result   = Enum.TryParse<QMC.CDT320.Materials.DieResult>(p[4], out var rr) ? rr : QMC.CDT320.Materials.DieResult.Unknown,
                        BinCode  = int.TryParse(p[5], out var bc) ? bc : 0,
                        X        = double.TryParse(p[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 0,
                        Y        = double.TryParse(p[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : 0,
                        DieUid   = p.Length >= 9 ? p[8] : ""
                    };
                    map.Entries.Add(entry);
                }
                return map;
            }
            catch { return null; }
        }

        /// <summary>JSON 또는 CSV 자동 감지 후 로드.</summary>
        public static DieMap Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".csv") return LoadCsv(path);
            return LoadJson(path);
        }

        /// <summary>일자별 Output 폴더에 자동 저장 — 310 의 OutputDieMapDataSaver 동등.</summary>
        public static string SaveToOutput(DieMap map, string lotId)
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "DieMap",
                                        DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(root);
            string baseName = $"{(string.IsNullOrEmpty(lotId) ? "lot" : lotId)}_{(map.FrameObjId ?? "frame")}_{DateTime.Now:HHmmss}";
            string csv  = Path.Combine(root, baseName + ".csv");
            string json = Path.Combine(root, baseName + ".json");
            SaveCsv(map, csv);
            SaveJson(map, json);
            return csv;
        }
    }
}
