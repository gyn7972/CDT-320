using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using QMC.CDT320.Materials;
using QMC.Common.Data.Store;

namespace QMC.CDT320.DieMaps
{
    public enum WaferEdgeSkipMode
    {
        Grid = 0,
        Millimeter = 1
    }

    /// <summary>
    /// DieMap 생성/저장/로드. 310 의 DieMapGenerateServiceExecutor 와 동등 기능.
    /// </summary>
    public static class DieMapGenerator
    {
        private sealed class ExternalMapPoint
        {
            public int X;
            public int Y;
            public int Bin;
        }

        public static int CalculateWaferGridCount(double outerDiameterMm, double pitchMm, double dieSizeMm)
        {
            if (outerDiameterMm <= 0.0)
                return 1;

            double pitch = pitchMm > 0.0 ? pitchMm : 1.0;
            double dieSize = dieSizeMm > 0.0 ? dieSizeMm : pitch;
            if (Math.Abs(dieSize - pitch) < 0.000001)
            {
                int pitchCount = (int)Math.Round(outerDiameterMm / pitch, MidpointRounding.AwayFromZero);
                return Math.Max(1, pitchCount);
            }

            int count = (int)Math.Floor(Math.Max(0.0, outerDiameterMm - dieSize) / pitch) + 1;
            return Math.Max(1, count);
        }

        public static DieMap GenerateCircularWafer(
            double outerDiameterMm,
            double pitchX,
            double pitchY,
            double dieSizeX,
            double dieSizeY,
            WaferEdgeSkipMode edgeSkipMode,
            double sideEdgeSkip,
            double topBottomEdgeSkip,
            string frameObjId = "WAFER")
        {
            double diameter = outerDiameterMm > 0.0 ? outerDiameterMm : 1.0;
            double resolvedPitchX = pitchX > 0.0 ? pitchX : 1.0;
            double resolvedPitchY = pitchY > 0.0 ? pitchY : 1.0;
            double resolvedDieSizeX = dieSizeX > 0.0 ? dieSizeX : resolvedPitchX;
            double resolvedDieSizeY = dieSizeY > 0.0 ? dieSizeY : resolvedPitchY;

            // 현재 기준: 외경/피치/다이 크기로 실제 들어갈 Grid 수를 자동 계산한다.
            int gridX = CalculateWaferGridCount(diameter, resolvedPitchX, resolvedDieSizeX);
            int gridY = CalculateWaferGridCount(diameter, resolvedPitchY, resolvedDieSizeY);
            double originX = -Math.Max(0, gridX - 1) * resolvedPitchX / 2.0;
            double originY = -Math.Max(0, gridY - 1) * resolvedPitchY / 2.0;
            double radius = diameter / 2.0;

            int sideGridSkip = 0;
            int topBottomGridSkip = 0;
            double sideMmSkip = 0.0;
            double topBottomMmSkip = 0.0;

            if (edgeSkipMode == WaferEdgeSkipMode.Grid)
            {
                sideGridSkip = ClampEdgeGridSkip(sideEdgeSkip, gridX);
                topBottomGridSkip = ClampEdgeGridSkip(topBottomEdgeSkip, gridY);
            }
            else
            {
                sideMmSkip = Math.Max(0.0, sideEdgeSkip);
                topBottomMmSkip = Math.Max(0.0, topBottomEdgeSkip);
            }

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = resolvedPitchX,
                PitchY = resolvedPitchY,
                DieSizeX = resolvedDieSizeX,
                DieSizeY = resolvedDieSizeY,
                OuterDiameterMm = diameter,
                EdgeSkipMode = edgeSkipMode.ToString(),
                SideEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Grid ? sideGridSkip : sideMmSkip,
                TopBottomEdgeSkip = edgeSkipMode == WaferEdgeSkipMode.Grid ? topBottomGridSkip : topBottomMmSkip,
                OriginX = originX,
                OriginY = originY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            for (int row = 0; row < gridY; row++)
            {
                for (int col = 0; col < gridX; col++)
                {
                    double x = originX + col * resolvedPitchX;
                    double y = originY + row * resolvedPitchY;
                    bool target = IsInsideCircularWaferTarget(
                        col,
                        row,
                        gridX,
                        gridY,
                        x,
                        y,
                        radius,
                        resolvedDieSizeX / 2.0,
                        resolvedDieSizeY / 2.0,
                        edgeSkipMode,
                        sideGridSkip,
                        topBottomGridSkip,
                        sideMmSkip,
                        topBottomMmSkip);

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = DieResult.Unknown,
                        BinCode = 0,
                        PosX = x,
                        PosY = y
                    });
                }
            }

            return map;
        }

        private static int ClampEdgeGridSkip(double value, int gridCount)
        {
            int skip = (int)Math.Floor(Math.Max(0.0, value));
            int max = Math.Max(0, (gridCount - 1) / 2);
            return skip > max ? max : skip;
        }

        private static bool IsInsideCircularWaferTarget(
            int col,
            int row,
            int gridX,
            int gridY,
            double centerX,
            double centerY,
            double radius,
            double dieHalfX,
            double dieHalfY,
            WaferEdgeSkipMode edgeSkipMode,
            int sideGridSkip,
            int topBottomGridSkip,
            double sideMmSkip,
            double topBottomMmSkip)
        {
            if (gridX <= 0 || gridY <= 0 || radius <= 0.0)
                return false;

            // 현재 기준: GRID 모드는 행/열 개수, MM 모드는 외곽 물리 거리로 Target 제외한다.
            if (edgeSkipMode == WaferEdgeSkipMode.Grid)
            {
                if (col < sideGridSkip || col >= gridX - sideGridSkip)
                    return false;
                if (row < topBottomGridSkip || row >= gridY - topBottomGridSkip)
                    return false;
            }
            else
            {
                double usableHalfX = Math.Max(0.0, radius - sideMmSkip);
                double usableHalfY = Math.Max(0.0, radius - topBottomMmSkip);
                if (Math.Abs(centerX) + dieHalfX > usableHalfX)
                    return false;
                if (Math.Abs(centerY) + dieHalfY > usableHalfY)
                    return false;
            }

            double usableRadius = edgeSkipMode == WaferEdgeSkipMode.Millimeter
                ? Math.Max(0.0, radius - Math.Max(sideMmSkip, topBottomMmSkip))
                : radius;
            if (usableRadius <= 0.0)
                return false;

            return IsDieRectangleInsideCircle(centerX, centerY, dieHalfX, dieHalfY, usableRadius);
        }

        private static bool IsDieRectangleInsideCircle(double centerX, double centerY, double halfX, double halfY, double radius)
        {
            double radiusSq = radius * radius;
            double[] xs = { centerX - halfX, centerX + halfX };
            double[] ys = { centerY - halfY, centerY + halfY };
            for (int ix = 0; ix < xs.Length; ix++)
            {
                for (int iy = 0; iy < ys.Length; iy++)
                {
                    double x = xs[ix];
                    double y = ys[iy];
                    if ((x * x) + (y * y) > radiusSq)
                        return false;
                }
            }
            return true;
        }

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
            return GenerateCircularWafer(
                waferDiameterMm,
                pitchX,
                pitchY,
                dieSizeXMm,
                dieSizeYMm,
                WaferEdgeSkipMode.Grid,
                0,
                0,
                frameObjId);
        }

        /// <summary>
        /// 사각 트레이 다이맵 생성 (Output BIN 용). 원 판정 없이 모든 셀 활성.
        /// </summary>
        public static DieMap GenerateRect(int dieMapX, int dieMapY,
                                          double dieSizeXMm, double dieSizeYMm,
                                          double gapXMm, double gapYMm,
                                          string frameObjId = "RECT")
        {
            double pitchX = dieSizeXMm + gapXMm;
            double pitchY = dieSizeYMm + gapYMm;
            if (dieMapX < 1) dieMapX = 1;
            if (dieMapY < 1) dieMapY = 1;
            // 격자 index 기준 중심 좌표를 (0,0)으로 둔다.
            double originX = -Math.Max(0, dieMapX - 1) * pitchX / 2.0;
            double originY = -Math.Max(0, dieMapY - 1) * pitchY / 2.0;

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                DieMapX  = dieMapX,
                DieMapY  = dieMapY,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
            };

            int idx = 0;
            for (int y = 0; y < dieMapY; y++)
            {
                for (int x = 0; x < dieMapX; x++)
                {
                    double cx = originX + x * pitchX;
                    double cy = originY + y * pitchY;
                    map.Entries.Add(new DieMapEntry
                    {
                        Index    = idx++,
                        DieMapX    = x,
                        DieMapY    = y,
                        IsTarget = true,
                        Result   = DieResult.Unknown,
                        BinCode  = 0,
                        PosX        = cx,
                        PosY        = cy
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
            int gx = Math.Max(1, frame.DieMapX);
            int gy = Math.Max(1, frame.DieMapY);

            var map = new DieMap
            {
                FrameObjId = frame.ObjId,
                DieMapX  = gx,
                DieMapY  = gy,
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
                        DieMapX    = x,
                        DieMapY    = y,
                        IsTarget = true,
                        Result   = DieResult.Unknown,
                        BinCode  = 0,
                        PosX        = frame.OriginX + mx,
                        PosY        = frame.OriginY + my
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
                // 회전 없음 좌표 적용
                case TapeFrameRotate.None:
                    mx = bx; my = by; break;
                // 90도 회전 좌표 적용
                case TapeFrameRotate.R90:
                    mx = by;
                    my = (totalX - 1) * pitchX - bx;
                    break;
                // 180도 회전 좌표 적용
                case TapeFrameRotate.R180:
                    mx = (totalX - 1) * pitchX - bx;
                    my = (totalY - 1) * pitchY - by;
                    break;
                // 270도 회전 좌표 적용
                case TapeFrameRotate.R270:
                    mx = (totalY - 1) * pitchY - by;
                    my = bx;
                    break;
                default:
                    mx = bx; my = by; break;
            }
        }

        /// <summary>
        /// 저장/로드/화면 적용 전에 다이맵 엔트리 기본 정보를 정리한다.
        /// </summary>
        public static DieMap Normalize(DieMap map)
        {
            if (map == null)
                return null;

            if (map.Entries == null)
                map.Entries = new List<DieMapEntry>();
            else
                map.Entries = map.Entries.Where(e => e != null).ToList();

            if (string.IsNullOrWhiteSpace(map.FrameObjId))
                map.FrameObjId = "DIEMAP";
            if (map.CreatedAt == default(DateTime))
                map.CreatedAt = DateTime.Now;

            if ((map.DieMapX <= 0 || map.DieMapY <= 0) && map.Entries.Count > 0)
            {
                int maxX = map.Entries.Where(e => e != null).Select(e => e.DieMapX).DefaultIfEmpty(0).Max();
                int maxY = map.Entries.Where(e => e != null).Select(e => e.DieMapY).DefaultIfEmpty(0).Max();
                if (map.DieMapX <= 0) map.DieMapX = Math.Max(1, maxX + 1);
                if (map.DieMapY <= 0) map.DieMapY = Math.Max(1, maxY + 1);
            }

            var usedDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < map.Entries.Count; i++)
            {
                DieMapEntry entry = map.Entries[i];
                if (entry == null)
                    continue;

                entry.Index = i;
                // 현재 기준: 외부 웨이퍼맵 원본 인덱스가 있으면 DieMapX/Y도 그 값으로 통일한다.
                if (entry.OriginalMapX >= 0) entry.DieMapX = entry.OriginalMapX;
                if (entry.OriginalMapY >= 0) entry.DieMapY = entry.OriginalMapY;
                if (entry.DieMapX < 0) entry.DieMapX = 0;
                if (entry.DieMapY < 0) entry.DieMapY = 0;

                if (string.IsNullOrWhiteSpace(entry.DieUid))
                    entry.DieUid = BuildDefaultDieUid(map, entry);

                string dieUid = entry.DieUid;
                if (usedDieIds.Contains(dieUid))
                {
                    dieUid = BuildDefaultDieUid(map, entry) + "-" + entry.Index.ToString("000000", CultureInfo.InvariantCulture);
                    entry.DieUid = dieUid;
                }
                usedDieIds.Add(dieUid);

                if (!entry.IsTarget)
                {
                    entry.SequenceNo = 0;
                    if (entry.Result == DieResult.Unknown)
                        entry.Result = DieResult.NG;
                    if (entry.BinCode == 0)
                        entry.BinCode = 255;
                }
            }

            if (map.Entries.Count > 0)
            {
                int maxX = map.Entries.Where(e => e != null).Select(e => e.DieMapX).DefaultIfEmpty(0).Max();
                int maxY = map.Entries.Where(e => e != null).Select(e => e.DieMapY).DefaultIfEmpty(0).Max();
                if (map.DieMapX <= maxX) map.DieMapX = Math.Max(1, maxX + 1);
                if (map.DieMapY <= maxY) map.DieMapY = Math.Max(1, maxY + 1);
            }

            return map;
        }

        private static string BuildDefaultDieUid(DieMap map, DieMapEntry entry)
        {
            string frameId = SanitizeId(map != null ? map.FrameObjId : "DIEMAP");
            int row = ResolveMapIndexY(entry);
            int col = ResolveMapIndexX(entry);
            return frameId + "-D" + row.ToString("000", CultureInfo.InvariantCulture) + "-" + col.ToString("000", CultureInfo.InvariantCulture);
        }

        private static string SanitizeId(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "DIEMAP" : value.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '_');
            text = text.Replace(' ', '_');
            return text;
        }

        /// <summary>CSV 로 저장 (310 의 OutputDieMapDataSaver 동등).</summary>
        public static void SaveCsv(DieMap map, string path)
        {
            if (map == null) return;
            try
            {
                Normalize(map);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("FrameObjId," + map.FrameObjId);
                    sw.WriteLine($"DieMapX,{map.DieMapX}");
                    sw.WriteLine($"DieMapY,{map.DieMapY}");
                    sw.WriteLine($"PitchX,{map.PitchX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"PitchY,{map.PitchY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OriginX,{map.OriginX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OriginY,{map.OriginY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"CreatedAt,{map.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine();
                    sw.WriteLine("Index,SequenceNo,DieMapX,DieMapY,IsTarget,Result,BinCode,X,Y,DieUid");
                    foreach (var e in map.Entries)
                    {
                        sw.WriteLine(string.Join(",",
                            e.Index, e.SequenceNo, e.DieMapX, e.DieMapY, e.IsTarget, e.Result,
                            e.BinCode,
                            e.PosX.ToString(CultureInfo.InvariantCulture),
                            e.PosY.ToString(CultureInfo.InvariantCulture),
                            e.DieUid));
                    }
                }
            }
            catch { }
        }

        /// <summary>RAD 계열 TXT로 저장. X/Y는 원본 grid index, B는 Bin label로 기록한다.</summary>
        public static void SaveWaferMapText(DieMap map, string path)
        {
            if (map == null) return;
            try
            {
                Normalize(map);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                List<DieMapEntry> targets = map.Entries
                    .Where(e => e != null && e.IsTarget)
                    .OrderBy(e => ResolveMapIndexY(e))
                    .ThenBy(e => ResolveMapIndexX(e))
                    .ToList();

                int count = targets.Count;
                int pitchXum = Math.Max(1, (int)Math.Round((map.PitchX > 0.0 ? map.PitchX : 1.0) * 1000.0));
                int pitchYum = Math.Max(1, (int)Math.Round((map.PitchY > 0.0 ? map.PitchY : 1.0) * 1000.0));
                string frameId = string.IsNullOrWhiteSpace(map.FrameObjId)
                    ? Path.GetFileNameWithoutExtension(path)
                    : map.FrameObjId;
                if (string.IsNullOrWhiteSpace(frameId))
                    frameId = "CDT320";

                using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("[" + frameId.PadRight(80).Substring(0, 80) + "]");
                    sw.WriteLine("[CDT320/00/CDT320-WAFERMAP/CDT320/" +
                                 pitchXum.ToString("00000", CultureInfo.InvariantCulture) + "/" +
                                 pitchYum.ToString("00000", CultureInfo.InvariantCulture) + "/%" +
                                 count.ToString(CultureInfo.InvariantCulture) + "/&" +
                                 count.ToString(CultureInfo.InvariantCulture) + "/    /00]");
                    sw.WriteLine("[Y=0000 G=#00000001 1=#00000001 2=#00000000 P=#00000000 F=#FFFFFFFF       I=S]");
                    sw.WriteLine("[S=" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) +
                                 " E=" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) +
                                 " R=000.00NS 0100(0028) 0.00(0.00)]");
                    sw.WriteLine("\"[NORMAL         /000,000,000,                                                ]\"");
                    sw.WriteLine("[FIRST_X= " + ResolveFirstIndex(targets, true).ToString("000", CultureInfo.InvariantCulture) +
                                 " FIRST_Y= " + ResolveFirstIndex(targets, false).ToString("000", CultureInfo.InvariantCulture) +
                                 " TEMP: +0000.0 /CDT320                                ]");
                    sw.WriteLine("\"BO=1\"");
                    sw.WriteLine("\"BN=Good\"");

                    foreach (DieMapEntry entry in targets)
                    {
                        int x = ResolveMapIndexX(entry);
                        int y = ResolveMapIndexY(entry);
                        int bin = entry.BinCode > 0 && entry.BinCode < 1000 ? entry.BinCode : 1;
                        // 현재 기준: 저장 TXT도 X/Y는 좌표가 아니라 map index 그대로 쓴다.
                        sw.WriteLine("X= " + x.ToString("0000", CultureInfo.InvariantCulture) +
                                     " Y= " + y.ToString("0000", CultureInfo.InvariantCulture) +
                                     " B= " + bin.ToString("000", CultureInfo.InvariantCulture));
                    }

                    sw.WriteLine("E= EOW");
                    sw.WriteLine("E= EOW");
                }
            }
            catch { }
        }

        public static void Save(DieMap map, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".csv")
                SaveCsv(map, path);
            else if (ext == ".txt")
                SaveWaferMapText(map, path);
            else
                SaveJson(map, path);
        }

        public static int ResolveMapIndexX(DieMapEntry entry)
        {
            if (entry == null)
                return 0;
            return entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX;
        }

        public static int ResolveMapIndexY(DieMapEntry entry)
        {
            if (entry == null)
                return 0;
            return entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY;
        }

        private static int ResolveFirstIndex(List<DieMapEntry> entries, bool xAxis)
        {
            if (entries == null || entries.Count == 0)
                return 0;

            return xAxis
                ? ResolveMapIndexX(entries[0])
                : ResolveMapIndexY(entries[0]);
        }

        /// <summary>JSON 직렬화 저장 (raw).</summary>
        public static void SaveJson(DieMap map, string path)
        {
            try
            {
                Normalize(map);
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
                    return Normalize((DieMap)ser.ReadObject(fs));
                }
            }
            catch { return null; }
        }

        /// <summary>프로버 웨이퍼맵 TXT(RAD 계열) 로드. X/Y는 원본 인덱스, B는 Bin label로 사용한다.</summary>
        public static DieMap LoadWaferMapText(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                List<ExternalMapPoint> points = new List<ExternalMapPoint>();
                double pitchX = 1.0;
                double pitchY = 1.0;
                TryReadPitchFromWaferMapText(path, out pitchX, out pitchY);

                Regex pointRegex = new Regex(@"^X=\s*(?<x>[-+]?\d+)\s+Y=\s*(?<y>[-+]?\d+)\s+B=\s*(?<b>[-+]?\d+)", RegexOptions.Compiled);
                foreach (string line in File.ReadLines(path))
                {
                    Match match = pointRegex.Match(line ?? "");
                    if (!match.Success)
                        continue;

                    points.Add(new ExternalMapPoint
                    {
                        X = int.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
                        Y = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
                        Bin = int.Parse(match.Groups["b"].Value, CultureInfo.InvariantCulture)
                    });
                }

                if (points.Count == 0)
                    return null;

                int minX = points.Min(p => p.X);
                int maxX = points.Max(p => p.X);
                int minY = points.Min(p => p.Y);
                int maxY = points.Max(p => p.Y);
                int gridX = Math.Max(1, maxX + 1);
                int gridY = Math.Max(1, maxY + 1);
                double centerIndexX = (minX + maxX) / 2.0;
                double centerIndexY = (minY + maxY) / 2.0;
                double originX = -centerIndexX * pitchX;
                double originY = -centerIndexY * pitchY;
                string frameId = Path.GetFileNameWithoutExtension(path);

                var map = new DieMap
                {
                    FrameObjId = string.IsNullOrWhiteSpace(frameId) ? "WAFER-TXT" : frameId,
                    DieMapX = gridX,
                    DieMapY = gridY,
                    PitchX = pitchX,
                    PitchY = pitchY,
                    DieSizeX = pitchX,
                    DieSizeY = pitchY,
                    OuterDiameterMm = Math.Max((maxX - minX + 1) * pitchX, (maxY - minY + 1) * pitchY),
                    EdgeSkipMode = "ExternalMap",
                    OriginX = originX,
                    OriginY = originY,
                    CreatedAt = DateTime.Now
                };

                int index = 0;
                foreach (ExternalMapPoint point in points
                    .GroupBy(p => p.X.ToString(CultureInfo.InvariantCulture) + "," + p.Y.ToString(CultureInfo.InvariantCulture))
                    .Select(g => g.First())
                    .OrderBy(p => p.Y)
                    .ThenBy(p => p.X))
                {
                    int binCode = point.Bin;
                    bool target = binCode > 0;

                    // 현재 기준: TXT X/Y는 맵 좌표가 아니라 원본 grid index 그대로 사용한다.
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = point.X,
                        DieMapY = point.Y,
                        OriginalMapX = point.X,
                        OriginalMapY = point.Y,
                        IsTarget = target,
                        Result = DieResult.Unknown,
                        BinCode = target ? binCode : 0,
                        PosX = originX + point.X * pitchX,
                        PosY = originY + point.Y * pitchY,
                        DieUid = BuildExternalMapDieUid(frameId, point.X, point.Y)
                    });
                }

                return Normalize(map);
            }
            catch
            {
                return null;
            }
        }

        private static void TryReadPitchFromWaferMapText(string path, out double pitchX, out double pitchY)
        {
            pitchX = 1.0;
            pitchY = 1.0;
            try
            {
                Regex pitchRegex = new Regex(@"/(?<x>\d{5})/(?<y>\d{5})/", RegexOptions.Compiled);
                foreach (string line in File.ReadLines(path).Take(20))
                {
                    Match match = pitchRegex.Match(line ?? "");
                    if (!match.Success)
                        continue;

                    // 현재 기준: RAD 헤더의 08120/06120 값은 um 단위 die pitch로 보고 mm로 변환한다.
                    pitchX = Math.Max(0.001, double.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture) / 1000.0);
                    pitchY = Math.Max(0.001, double.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture) / 1000.0);
                    return;
                }
            }
            catch
            {
                pitchX = 1.0;
                pitchY = 1.0;
            }
        }

        private static string BuildExternalMapDieUid(string frameId, int originalX, int originalY)
        {
            string safeFrame = SanitizeId(frameId);
            return safeFrame + "-X" + originalX.ToString("0000", CultureInfo.InvariantCulture) +
                   "-Y" + originalY.ToString("0000", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// CSV 임포트 (Stage 22) — SaveCsv 가 만든 형식 또는 표준 SECS map CSV 둘 다 지원.
        /// 표준 형식: header section (key,value) + 빈 줄 + entries (Index,SequenceNo,DieMapX,DieMapY,IsTarget,Result,BinCode,X,Y,DieUid).
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
                    else if (k.Equals("DieMapX",  StringComparison.OrdinalIgnoreCase)) map.DieMapX   = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("DieMapY",  StringComparison.OrdinalIgnoreCase)) map.DieMapY   = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("PitchX", StringComparison.OrdinalIgnoreCase)) map.PitchX  = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("PitchY", StringComparison.OrdinalIgnoreCase)) map.PitchY  = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OriginX",StringComparison.OrdinalIgnoreCase)) map.OriginX = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OriginY",StringComparison.OrdinalIgnoreCase)) map.OriginY = double.Parse(v, CultureInfo.InvariantCulture);
                }
                // Skip column header
                bool hasSequenceNo = false;
                if (i < lines.Length && lines[i].StartsWith("Index", StringComparison.OrdinalIgnoreCase))
                {
                    string header = lines[i];
                    hasSequenceNo = header.IndexOf("SequenceNo", StringComparison.OrdinalIgnoreCase) >= 0;
                    i++;
                }

                // Entries
                while (i < lines.Length)
                {
                    var line = lines[i++].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var p = line.Split(',');
                    for (int j = 0; j < p.Length; j++)
                        p[j] = p[j] != null ? p[j].Trim() : "";
                    if (p.Length < 8) continue;
                    if (hasSequenceNo && p.Length < 9) continue;
                    int sequenceNo = 0;
                    int offset = 0;
                    if (hasSequenceNo)
                    {
                        sequenceNo = int.TryParse(p[1], out var seq) ? seq : 0;
                        offset = 1;
                    }
                    var entry = new DieMapEntry
                    {
                        Index    = int.TryParse(p[0], out var idx) ? idx : 0,
                        SequenceNo = sequenceNo,
                        DieMapX    = int.TryParse(p[1 + offset], out var gx) ? gx : 0,
                        DieMapY    = int.TryParse(p[2 + offset], out var gy) ? gy : 0,
                        IsTarget = bool.TryParse(p[3 + offset], out var isT) ? isT : true,
                        Result   = Enum.TryParse<QMC.CDT320.Materials.DieResult>(p[4 + offset], out var rr) ? rr : QMC.CDT320.Materials.DieResult.Unknown,
                        BinCode  = int.TryParse(p[5 + offset], out var bc) ? bc : 0,
                        PosX        = double.TryParse(p[6 + offset], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 0,
                        PosY        = double.TryParse(p[7 + offset], NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : 0,
                        DieUid   = p.Length >= 9 + offset ? p[8 + offset] : ""
                    };
                    entry.OriginalMapX = entry.DieMapX;
                    entry.OriginalMapY = entry.DieMapY;
                    map.Entries.Add(entry);
                }
                return Normalize(map);
            }
            catch { return null; }
        }

        /// <summary>JSON 또는 CSV 자동 감지 후 로드.</summary>
        public static DieMap Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".csv") return LoadCsv(path);
            if (ext == ".txt") return LoadWaferMapText(path);
            return LoadJson(path);
        }

        /// <summary>일자별 Output 폴더에 자동 저장 — 310 의 OutputDieMapDataSaver 동등.</summary>
        public static string SaveToOutput(DieMap map, string lotId)
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "DieMap",
                                        DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(root);
            string baseName = $"{(string.IsNullOrEmpty(lotId) ? "lot" : lotId)}_{(map.FrameObjId ?? "frame")}_{DateTime.Now:HHmmss}";
            string txt  = Path.Combine(root, baseName + ".txt");
            string csv  = Path.Combine(root, baseName + ".csv");
            string json = Path.Combine(root, baseName + ".json");
            SaveWaferMapText(map, txt);
            SaveCsv(map, csv);
            SaveJson(map, json);
            return txt;
        }
    }
}
