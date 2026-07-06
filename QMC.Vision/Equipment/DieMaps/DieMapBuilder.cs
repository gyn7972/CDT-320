using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.Vision.DieMaps
{
    /// <summary>Edge skip 모드(핸들러 QMC.CDT320.DieMaps.WaferEdgeSkipMode 이식) — Grid=행/열 수, Millimeter=외곽 물리 거리(mm).</summary>
    public enum WaferEdgeSkipMode
    {
        Grid = 0,
        Millimeter = 1
    }

    /// <summary>
    /// 웨이퍼 다이맵 생성/저장/로드(핸들러 QMC.CDT320.DieMaps.DieMapGenerator 의 기하 이식).
    /// 핸들러와 동일한 pitch/origin/원형 판정으로 맵을 만들어 Bottom 매핑이 핸들러 데이터와 일치하도록 한다.
    /// </summary>
    public static class DieMapBuilder
    {
        /// <summary>
        /// 원형 웨이퍼 다이맵 생성. pitch = dieSize + gap. 셀 중심이 반지름 안이면 IsTarget=true.
        /// </summary>
        public static DieMap GenerateWafer(double waferDiameterMm,
                                           double dieSizeXMm, double dieSizeYMm,
                                           double gapXMm, double gapYMm,
                                           string frameObjId = "WAFER")
        {
            double pitchX = dieSizeXMm + gapXMm;
            double pitchY = dieSizeYMm + gapYMm;
            if (pitchX <= 0) pitchX = 1.0;
            if (pitchY <= 0) pitchY = 1.0;
            if (waferDiameterMm <= 0) waferDiameterMm = 1.0;

            double radius = waferDiameterMm / 2.0;
            int gx = (int)Math.Floor(waferDiameterMm / pitchX);
            int gy = (int)Math.Floor(waferDiameterMm / pitchY);
            if (gx < 1) gx = 1;
            if (gy < 1) gy = 1;

            double originX = -gx * pitchX / 2.0;
            double originY = -gy * pitchY / 2.0;

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                DieMapX = gx,
                DieMapY = gy,
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
                    double cx = originX + (x + 0.5) * pitchX;
                    double cy = originY + (y + 0.5) * pitchY;
                    double distSq = cx * cx + cy * cy;
                    bool isTarget = distSq <= radius * radius;

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = idx++,
                        DieMapX = x,
                        DieMapY = y,
                        IsTarget = isTarget,
                        Result = DieResult.Unknown,
                        BinCode = 0,
                        PosX = cx,
                        PosY = cy
                    });
                }
            }
            return map;
        }

        /// <summary>사각 트레이 다이맵 생성(원 판정 없이 모든 셀 활성).</summary>
        public static DieMap GenerateRect(int dieMapX, int dieMapY,
                                          double dieSizeXMm, double dieSizeYMm,
                                          double gapXMm, double gapYMm,
                                          string frameObjId = "RECT")
        {
            double pitchX = dieSizeXMm + gapXMm;
            double pitchY = dieSizeYMm + gapYMm;
            if (pitchX <= 0) pitchX = 1.0;
            if (pitchY <= 0) pitchY = 1.0;
            if (dieMapX < 1) dieMapX = 1;
            if (dieMapY < 1) dieMapY = 1;
            double originX = -dieMapX * pitchX / 2.0;
            double originY = -dieMapY * pitchY / 2.0;

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                DieMapX = dieMapX,
                DieMapY = dieMapY,
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
                    double cx = originX + (x + 0.5) * pitchX;
                    double cy = originY + (y + 0.5) * pitchY;
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = idx++,
                        DieMapX = x,
                        DieMapY = y,
                        IsTarget = true,
                        Result = DieResult.Unknown,
                        BinCode = 0,
                        PosX = cx,
                        PosY = cy
                    });
                }
            }
            return map;
        }

        /// <summary>외경/피치/다이 크기로 실제 들어갈 Grid 수 자동 계산 — 핸들러 DieMapGenerator.CalculateWaferGridCount 동일.</summary>
        public static int CalculateWaferGridCount(double outerDiameterMm, double pitchMm, double dieSizeMm)
        {
            if (outerDiameterMm <= 0.0)
                return 1;

            double pitch = pitchMm > 0.0 ? pitchMm : 1.0;
            double dieSize = dieSizeMm > 0.0 ? dieSizeMm : pitch;
            int count = (int)Math.Floor(Math.Max(0.0, outerDiameterMm - dieSize) / pitch) + 1;
            return Math.Max(1, count);
        }

        /// <summary>EdgeSkipMode 문자열이 MM(물리 거리) 모드인지 — 핸들러 MapCreatePage.IsMillimeterEdgeSkipMode 동일.</summary>
        public static bool IsMillimeterEdgeSkipMode(string mode)
        {
            return !string.IsNullOrWhiteSpace(mode) &&
                   (mode.IndexOf("MM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    mode.IndexOf("MILLI", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 원형 웨이퍼 다이맵 생성 — 핸들러 DieMapGenerator.GenerateCircularWafer 와 동일 기하(2026-07-06 이식).
        /// 외경/피치/다이 크기로 Grid 수 자동 계산, Grid/MM edge skip, 다이 사각형 4코너 원 내접 판정.
        /// 핸들러와 같은 사양 값이면 같은 맵(활성 다이/픽업 순서)이 나온다 — die_index↔grid 정합의 근거.
        /// </summary>
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
                FrameObjId = string.IsNullOrWhiteSpace(frameObjId) ? "WAFER" : frameObjId,
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = resolvedPitchX,
                PitchY = resolvedPitchY,
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
                        col, row, gridX, gridY, x, y, radius,
                        resolvedDieSizeX / 2.0, resolvedDieSizeY / 2.0,
                        edgeSkipMode, sideGridSkip, topBottomGridSkip, sideMmSkip, topBottomMmSkip);

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = target ? DieResult.Unknown : DieResult.NG,
                        BinCode = target ? 0 : 255,
                        PosX = x,
                        PosY = y
                    });
                }
            }

            return map;
        }

        /// <summary>레시피 웨이퍼 사양으로 원형 다이맵 생성.
        /// <para>규칙(2026-07-06 확정): Grid X/Y 가 명시(>0)돼 있으면 <b>격자 수 기준</b>(격자 내접 원 —
        /// 핸들러의 저장 INPUT DIE 맵(45×45=2025 등)과 동일 기하, 외경은 보조 제한)으로 생성하고,
        /// Grid 미지정(0)일 때만 외경/피치/다이크기 자동 계산(핸들러 현행 생성식)을 쓴다.</para></summary>
        public static DieMap GenerateWaferSpecMap(QMC.Vision.Modules.VisionMachineRecipe r, string frameObjId)
        {
            if (r == null) return null;

            // Grid 명시 — 격자 수가 곧 웨이퍼 정의(45x45 → 2025칸). 사용자/핸들러 저장맵 기대와 일치.
            if (r.WaferGridX > 0 && r.WaferGridY > 0)
            {
                return GenerateCircleDieMap(
                    r.WaferGridX, r.WaferGridY, r.WaferPitchX, r.WaferPitchY,
                    r.WaferOuterDiameterMm, r.WaferSideEdgeSkip, r.WaferTopBottomEdgeSkip, frameObjId);
            }

            WaferEdgeSkipMode mode = IsMillimeterEdgeSkipMode(r.WaferEdgeSkipMode)
                ? WaferEdgeSkipMode.Millimeter
                : WaferEdgeSkipMode.Grid;
            double sideSkip = mode == WaferEdgeSkipMode.Millimeter ? r.WaferSideEdgeSkipMm : r.WaferSideEdgeSkip;
            double tbSkip = mode == WaferEdgeSkipMode.Millimeter ? r.WaferTopBottomEdgeSkipMm : r.WaferTopBottomEdgeSkip;
            return GenerateCircularWafer(
                r.WaferOuterDiameterMm, r.WaferPitchX, r.WaferPitchY,
                r.WaferDieSizeX, r.WaferDieSizeY,
                mode, sideSkip, tbSkip, frameObjId);
        }

        private static int ClampEdgeGridSkip(double value, int gridCount)
        {
            int skip = (int)Math.Floor(Math.Max(0.0, value));
            int max = Math.Max(0, (gridCount - 1) / 2);
            return skip > max ? max : skip;
        }

        private static bool IsInsideCircularWaferTarget(
            int col, int row, int gridX, int gridY,
            double centerX, double centerY, double radius,
            double dieHalfX, double dieHalfY,
            WaferEdgeSkipMode edgeSkipMode,
            int sideGridSkip, int topBottomGridSkip,
            double sideMmSkip, double topBottomMmSkip)
        {
            if (gridX <= 0 || gridY <= 0 || radius <= 0.0)
                return false;

            // 핸들러 기준: GRID 모드는 행/열 개수, MM 모드는 외곽 물리 거리로 Target 제외한다.
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
        /// 격자 내접 원형 다이맵 생성(구 MapCreatePage.IsInsideWaferCircle 이식) — Grid X/Y 명시 시 표준 경로.
        /// 핸들러의 저장 INPUT DIE 맵(예: 45×45=2025, 타겟 1517)과 동일 기하. 외경은 보조 직경 제한.
        /// </summary>
        public static DieMap GenerateCircleDieMap(int gridX, int gridY,
                                                  double pitchX, double pitchY,
                                                  double diameterMm,
                                                  int sideEdgeSkip, int topBottomEdgeSkip,
                                                  string frameObjId = "INPUT")
        {
            gridX = Math.Max(1, gridX);
            gridY = Math.Max(1, gridY);
            if (pitchX <= 0) pitchX = 1.0;
            if (pitchY <= 0) pitchY = 1.0;
            double originX = -((gridX - 1) * pitchX) / 2.0;
            double originY = -((gridY - 1) * pitchY) / 2.0;
            sideEdgeSkip = ClampEdgeSkip(sideEdgeSkip, gridX);
            topBottomEdgeSkip = ClampEdgeSkip(topBottomEdgeSkip, gridY);
            if (diameterMm < 0) diameterMm = 0;

            var map = new DieMap
            {
                FrameObjId = string.IsNullOrWhiteSpace(frameObjId) ? "INPUT" : frameObjId,
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                OriginX = originX,
                OriginY = originY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            for (int row = 0; row < gridY; row++)
            {
                for (int col = 0; col < gridX; col++)
                {
                    double x = originX + col * pitchX;
                    double y = originY + row * pitchY;
                    bool target = IsInsideWaferCircle(col, row, gridX, gridY,
                        sideEdgeSkip, topBottomEdgeSkip, x, y, pitchX, pitchY, diameterMm);
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        DieMapX = col,
                        DieMapY = row,
                        IsTarget = target,
                        Result = target ? DieResult.Unknown : DieResult.NG,
                        BinCode = target ? 0 : 255,
                        PosX = x,
                        PosY = y
                    });
                }
            }
            return map;
        }

        /// <summary>모든 셀의 IsTarget 을 반전(INVERT TARGET).</summary>
        public static void InvertTarget(DieMap map)
        {
            if (map == null || map.Entries == null) return;
            foreach (var e in map.Entries)
            {
                if (e == null) continue;
                e.IsTarget = !e.IsTarget;
                if (!e.IsTarget) e.SequenceNo = 0;
            }
        }

        private static int ClampEdgeSkip(int value, int gridCount)
        {
            int max = Math.Max(0, (gridCount - 1) / 2);
            if (value < 0) return 0;
            if (value > max) return max;
            return value;
        }

        private static bool IsInsideWaferCircle(int col, int row, int gridX, int gridY,
            int sideEdgeSkip, int topBottomEdgeSkip,
            double x, double y, double pitchX, double pitchY, double diameterMm)
        {
            if (gridX <= 0 || gridY <= 0)
                return false;

            if (col < sideEdgeSkip || col >= gridX - sideEdgeSkip)
                return false;
            if (row < topBottomEdgeSkip || row >= gridY - topBottomEdgeSkip)
                return false;

            double centerX = (gridX - 1) / 2.0;
            double centerY = (gridY - 1) / 2.0;
            double radiusX = Math.Max(0.5, (gridX - 1 - (sideEdgeSkip * 2)) / 2.0);
            double radiusY = Math.Max(0.5, (gridY - 1 - (topBottomEdgeSkip * 2)) / 2.0);
            double nx = (col - centerX) / radiusX;
            double ny = (row - centerY) / radiusY;
            bool insideGridCircle = (nx * nx) + (ny * ny) <= 1.0;
            if (!insideGridCircle)
                return false;

            if (diameterMm <= 0.0)
                return true;

            double activeSpanX = Math.Max(pitchX, (gridX - 1 - (sideEdgeSkip * 2)) * pitchX);
            double activeSpanY = Math.Max(pitchY, (gridY - 1 - (topBottomEdgeSkip * 2)) * pitchY);
            double activeDiameter = Math.Min(activeSpanX, activeSpanY);
            if (diameterMm >= activeDiameter)
                return true;

            double radiusMm = diameterMm / 2.0;
            return (x * x) + (y * y) <= radiusMm * radiusMm;
        }

        /// <summary>저장/로드/화면 적용 전 엔트리 기본 정보를 정리한다.</summary>
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
                int maxX = map.Entries.Select(e => e.DieMapX).DefaultIfEmpty(0).Max();
                int maxY = map.Entries.Select(e => e.DieMapY).DefaultIfEmpty(0).Max();
                if (map.DieMapX <= 0) map.DieMapX = Math.Max(1, maxX + 1);
                if (map.DieMapY <= 0) map.DieMapY = Math.Max(1, maxY + 1);
            }

            for (int i = 0; i < map.Entries.Count; i++)
            {
                DieMapEntry entry = map.Entries[i];
                if (entry == null)
                    continue;
                entry.Index = i;
                if (entry.DieMapX < 0) entry.DieMapX = 0;
                if (entry.DieMapY < 0) entry.DieMapY = 0;
            }

            return map;
        }

        /// <summary>활성(IsTarget) 셀 수.</summary>
        public static int CountTargets(DieMap map)
        {
            if (map == null || map.Entries == null) return 0;
            int n = 0;
            foreach (var e in map.Entries)
                if (e != null && e.IsTarget) n++;
            return n;
        }

        /// <summary>JSON pretty 직렬화 저장(AGENTS.md JSON Save Rule).</summary>
        public static bool SaveJson(DieMap map, string path)
        {
            if (map == null || string.IsNullOrEmpty(path))
                return false;
            try
            {
                Normalize(map);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var fs = File.Create(path))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(DieMap), map);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>CSV 저장(핸들러 DieMapGenerator.SaveCsv 동일 포맷 — 헤더 섹션 + 엔트리 표).</summary>
        public static bool SaveCsv(DieMap map, string path)
        {
            if (map == null || string.IsNullOrEmpty(path))
                return false;
            try
            {
                Normalize(map);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                using (var sw = new StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
                {
                    sw.WriteLine("FrameObjId," + map.FrameObjId);
                    sw.WriteLine("DieMapX," + map.DieMapX);
                    sw.WriteLine("DieMapY," + map.DieMapY);
                    sw.WriteLine("PitchX," + map.PitchX.ToString(ci));
                    sw.WriteLine("PitchY," + map.PitchY.ToString(ci));
                    sw.WriteLine("OriginX," + map.OriginX.ToString(ci));
                    sw.WriteLine("OriginY," + map.OriginY.ToString(ci));
                    sw.WriteLine("CreatedAt," + map.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine();
                    sw.WriteLine("Index,SequenceNo,DieMapX,DieMapY,IsTarget,Result,BinCode,X,Y,DieUid");
                    foreach (var e in map.Entries)
                    {
                        if (e == null) continue;
                        sw.WriteLine(string.Join(",",
                            e.Index, e.SequenceNo, e.DieMapX, e.DieMapY, e.IsTarget, e.Result,
                            e.BinCode, e.PosX.ToString(ci), e.PosY.ToString(ci), e.DieUid));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>CSV 로드 — 핸들러 DieMapGenerator.SaveCsv/LoadCsv 와 동일 형식
        /// (헤더 key,value 구간 + 빈 줄 + "Index,[SequenceNo,]DieMapX,DieMapY,IsTarget,Result,BinCode,X,Y,DieUid").
        /// 핸들러에서 EXPORT 한 INPUT DIE 맵 CSV 를 그대로 수입할 수 있다(2026-07-06). 실패 시 null.</summary>
        public static DieMap LoadCsv(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                var lines = File.ReadAllLines(path);
                var map = new DieMap();
                int i = 0;
                // 헤더 구간(key,value)
                for (; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) { i++; break; }
                    var parts = line.Split(new[] { ',' }, 2);
                    if (parts.Length < 2) continue;
                    string k = parts[0].Trim(), v = parts[1].Trim();
                    if (k.Equals("FrameObjId", StringComparison.OrdinalIgnoreCase)) map.FrameObjId = v;
                    else if (k.Equals("DieMapX", StringComparison.OrdinalIgnoreCase)) { int t; if (int.TryParse(v, out t)) map.DieMapX = t; }
                    else if (k.Equals("DieMapY", StringComparison.OrdinalIgnoreCase)) { int t; if (int.TryParse(v, out t)) map.DieMapY = t; }
                    else if (k.Equals("PitchX", StringComparison.OrdinalIgnoreCase)) { double t; if (double.TryParse(v, System.Globalization.NumberStyles.Any, ci, out t)) map.PitchX = t; }
                    else if (k.Equals("PitchY", StringComparison.OrdinalIgnoreCase)) { double t; if (double.TryParse(v, System.Globalization.NumberStyles.Any, ci, out t)) map.PitchY = t; }
                    else if (k.Equals("OriginX", StringComparison.OrdinalIgnoreCase)) { double t; if (double.TryParse(v, System.Globalization.NumberStyles.Any, ci, out t)) map.OriginX = t; }
                    else if (k.Equals("OriginY", StringComparison.OrdinalIgnoreCase)) { double t; if (double.TryParse(v, System.Globalization.NumberStyles.Any, ci, out t)) map.OriginY = t; }
                }
                // 컬럼 헤더(SequenceNo 유무 감지 — 구/신 형식 모두 지원)
                bool hasSequenceNo = false;
                if (i < lines.Length && lines[i].StartsWith("Index", StringComparison.OrdinalIgnoreCase))
                {
                    hasSequenceNo = lines[i].IndexOf("SequenceNo", StringComparison.OrdinalIgnoreCase) >= 0;
                    i++;
                }
                // 엔트리 구간
                while (i < lines.Length)
                {
                    var line = lines[i++].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var p = line.Split(',');
                    for (int j = 0; j < p.Length; j++)
                        p[j] = p[j] != null ? p[j].Trim() : "";
                    int need = hasSequenceNo ? 9 : 8;
                    if (p.Length < need) continue;
                    int off = hasSequenceNo ? 1 : 0;
                    int idx, seq = 0, gx, gy, bc; bool isT; double x, y;
                    int.TryParse(p[0], out idx);
                    if (hasSequenceNo) int.TryParse(p[1], out seq);
                    if (!int.TryParse(p[1 + off], out gx)) continue;
                    if (!int.TryParse(p[2 + off], out gy)) continue;
                    if (!bool.TryParse(p[3 + off], out isT)) isT = true;
                    DieResult rr; if (!Enum.TryParse(p[4 + off], true, out rr)) rr = DieResult.Unknown;
                    int.TryParse(p[5 + off], out bc);
                    double.TryParse(p[6 + off], System.Globalization.NumberStyles.Any, ci, out x);
                    double.TryParse(p[7 + off], System.Globalization.NumberStyles.Any, ci, out y);
                    map.Entries.Add(new DieMapEntry
                    {
                        Index = idx,
                        SequenceNo = seq,
                        DieMapX = gx,
                        DieMapY = gy,
                        IsTarget = isT,
                        Result = rr,
                        BinCode = bc,
                        PosX = x,
                        PosY = y,
                        DieUid = p.Length >= 9 + off ? p[8 + off] : ""
                    });
                }
                if (map.Entries.Count == 0) return null;
                return Normalize(map);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>확장자 자동 감지 로드 — .csv=핸들러 형식 CSV, 그 외=JSON.</summary>
        public static DieMap Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".csv" ? LoadCsv(path) : LoadJson(path);
        }

        /// <summary>JSON 직렬화 로드(실패 시 null).</summary>
        public static DieMap LoadJson(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(DieMap));
                    return Normalize((DieMap)ser.ReadObject(fs));
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
