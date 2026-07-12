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

        private sealed class WaferMapTextHeader
        {
            public double PitchX = 1.0;
            public double PitchY = 1.0;
            public bool HasPitch;
            public int DeclaredCount;
            public int FirstX = -1;
            public int FirstY = -1;
            public double FirstPosX = double.NaN;
            public double FirstPosY = double.NaN;
        }

        /// <summary>
        /// Recipe/TapeFrame의 Pitch 값은 Die 사이의 빈 간격(gap)이다.
        /// DieMap과 장비 좌표가 사용하는 중심 간격은 Die Size + Gap으로 계산한다.
        /// </summary>
        public static double CalculateCenterStep(double dieSizeMm, double pitchGapMm)
        {
            double dieSize = !double.IsNaN(dieSizeMm) && !double.IsInfinity(dieSizeMm) && dieSizeMm > 0.0
                ? dieSizeMm
                : 1.0;
            double gap = !double.IsNaN(pitchGapMm) && !double.IsInfinity(pitchGapMm) && pitchGapMm >= 0.0
                ? pitchGapMm
                : 0.0;
            return dieSize + gap;
        }

        public static double CalculatePitchGap(double centerStepMm, double dieSizeMm)
        {
            if (double.IsNaN(centerStepMm) || double.IsInfinity(centerStepMm) || centerStepMm <= 0.0)
                return 0.0;
            double dieSize = !double.IsNaN(dieSizeMm) && !double.IsInfinity(dieSizeMm) && dieSizeMm > 0.0
                ? dieSizeMm
                : 0.0;
            return Math.Max(0.0, centerStepMm - dieSize);
        }

        public static double CalculateEquipmentGridY(int localY, int gridY)
        {
            // 장비 Y 엔코더 기준으로 화면 상단은 음수, 하단은 양수가 되도록 변환한다.
            return localY - Math.Max(0, gridY - 1) / 2.0;
        }

        public static double CalculateCenteredOriginY(int gridY, double pitchY)
        {
            return CalculateEquipmentGridY(0, gridY) * pitchY;
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
            double originY = CalculateCenteredOriginY(gridY, resolvedPitchY);
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
                        EquipmentGridX = col - Math.Max(0, gridX - 1) / 2.0,
                        EquipmentGridY = CalculateEquipmentGridY(row, gridY),
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
            double pitchX = CalculateCenterStep(dieSizeXMm, gapXMm);
            double pitchY = CalculateCenterStep(dieSizeYMm, gapYMm);
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
            double pitchX = CalculateCenterStep(dieSizeXMm, gapXMm);
            double pitchY = CalculateCenterStep(dieSizeYMm, gapYMm);
            if (dieMapX < 1) dieMapX = 1;
            if (dieMapY < 1) dieMapY = 1;
            // 격자 index 기준 중심 좌표를 (0,0)으로 둔다.
            double originX = -Math.Max(0, dieMapX - 1) * pitchX / 2.0;
            double originY = CalculateCenteredOriginY(dieMapY, pitchY);

            var map = new DieMap
            {
                FrameObjId = frameObjId,
                DieMapX  = dieMapX,
                DieMapY  = dieMapY,
                PitchX = pitchX,
                PitchY = pitchY,
                DieSizeX = dieSizeXMm,
                DieSizeY = dieSizeYMm,
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
                        EquipmentGridX = x - Math.Max(0, dieMapX - 1) / 2.0,
                        EquipmentGridY = CalculateEquipmentGridY(y, dieMapY),
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
            if (map.SourceFileName == null)
                map.SourceFileName = "";
            if (map.SourceFormat == null)
                map.SourceFormat = "";
            if (map.CreatedAt == default(DateTime))
                map.CreatedAt = DateTime.Now;

            if (map.Entries.Count > 0)
            {
                int maxX = map.Entries.Where(e => e != null).Select(e => e.DieMapX).DefaultIfEmpty(0).Max();
                int maxY = map.Entries.Where(e => e != null).Select(e => e.DieMapY).DefaultIfEmpty(0).Max();
                map.DieMapX = Math.Max(Math.Max(1, map.DieMapX), maxX + 1);
                map.DieMapY = Math.Max(Math.Max(1, map.DieMapY), maxY + 1);
            }

            double centerGridX = Math.Max(0, map.DieMapX - 1) / 2.0;

            var usedDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < map.Entries.Count; i++)
            {
                DieMapEntry entry = map.Entries[i];
                if (entry == null)
                    continue;

                entry.Index = i;
                // DieMapX/Y는 화면·배열용 local index, OriginalMapX/Y는 외부 파일 주소다.
                // 두 주소를 다시 합치면 RAD1의 35x47 사용 영역이 201x228로 잘못 확대된다.
                if (entry.DieMapX < 0) entry.DieMapX = 0;
                if (entry.DieMapY < 0) entry.DieMapY = 0;
                if (entry.OriginalMapX < 0) entry.OriginalMapX = entry.DieMapX;
                if (entry.OriginalMapY < 0) entry.OriginalMapY = entry.DieMapY;
                if (double.IsNaN(entry.EquipmentGridX) || double.IsInfinity(entry.EquipmentGridX))
                    entry.EquipmentGridX = entry.DieMapX - centerGridX;
                if (double.IsNaN(entry.EquipmentGridY) || double.IsInfinity(entry.EquipmentGridY))
                    entry.EquipmentGridY = CalculateEquipmentGridY(entry.DieMapY, map.DieMapY);

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
            int row = ResolveOriginalMapIndexY(entry);
            int col = ResolveOriginalMapIndexX(entry);
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
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("CSV 저장 경로가 비어 있습니다.", nameof(path));
            try
            {
                Normalize(map);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("FrameObjId," + EscapeCsv(map.FrameObjId));
                    sw.WriteLine($"DieMapX,{map.DieMapX}");
                    sw.WriteLine($"DieMapY,{map.DieMapY}");
                    sw.WriteLine($"PitchX,{map.PitchX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"PitchY,{map.PitchY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"DieSizeX,{map.DieSizeX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"DieSizeY,{map.DieSizeY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OuterDiameterMm,{map.OuterDiameterMm.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine("EdgeSkipMode," + EscapeCsv(map.EdgeSkipMode));
                    sw.WriteLine($"SideEdgeSkip,{map.SideEdgeSkip.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"TopBottomEdgeSkip,{map.TopBottomEdgeSkip.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OriginX,{map.OriginX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"OriginY,{map.OriginY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine("SourceFileName," + EscapeCsv(map.SourceFileName));
                    sw.WriteLine("SourceFormat," + EscapeCsv(map.SourceFormat));
                    sw.WriteLine($"SourcePitchFromFile,{map.SourcePitchFromFile}");
                    sw.WriteLine($"SourceDeclaredCount,{map.SourceDeclaredCount}");
                    sw.WriteLine($"SourceFirstX,{map.SourceFirstX}");
                    sw.WriteLine($"SourceFirstY,{map.SourceFirstY}");
                    sw.WriteLine($"SourceFirstPosX,{map.SourceFirstPosX.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"SourceFirstPosY,{map.SourceFirstPosY.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"CreatedAt,{map.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine();
                    sw.WriteLine("Index,SequenceNo,DieMapX,DieMapY,OriginalMapX,OriginalMapY,IsTarget,Result,BinCode,X,Y,EquipmentGridX,EquipmentGridY,DieUid");
                    foreach (var e in map.Entries)
                    {
                        sw.WriteLine(string.Join(",",
                            e.Index, e.SequenceNo, e.DieMapX, e.DieMapY, e.OriginalMapX, e.OriginalMapY, e.IsTarget, e.Result,
                             e.BinCode,
                             e.PosX.ToString(CultureInfo.InvariantCulture),
                             e.PosY.ToString(CultureInfo.InvariantCulture),
                             e.EquipmentGridX.ToString(CultureInfo.InvariantCulture),
                             e.EquipmentGridY.ToString(CultureInfo.InvariantCulture),
                             EscapeCsv(e.DieUid)));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException("DieMap CSV 저장에 실패했습니다: " + path, ex);
            }
            finally
            {
            }
        }

        /// <summary>RAD 계열 TXT로 저장. X/Y는 원본 grid index, B는 Bin label로 기록한다.</summary>
        public static void SaveWaferMapText(DieMap map, string path)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("WaferMap TXT 저장 경로가 비어 있습니다.", nameof(path));
            try
            {
                Normalize(map);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                List<DieMapEntry> targets = map.Entries
                    .Where(e => e != null && e.IsTarget)
                    .OrderBy(e => ResolveOriginalMapIndexY(e))
                    .ThenBy(e => ResolveOriginalMapIndexX(e))
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
                        int x = ResolveOriginalMapIndexX(entry);
                        int y = ResolveOriginalMapIndexY(entry);
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
            catch (Exception ex)
            {
                throw new IOException("WaferMap TXT 저장에 실패했습니다: " + path, ex);
            }
            finally
            {
            }
        }

        public static void Save(DieMap map, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("DieMap 저장 경로가 비어 있습니다.", nameof(path));

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
            return entry != null ? entry.DieMapX : 0;
        }

        public static int ResolveMapIndexY(DieMapEntry entry)
        {
            return entry != null ? entry.DieMapY : 0;
        }

        public static int ResolveOriginalMapIndexX(DieMapEntry entry)
        {
            if (entry == null)
                return 0;
            return entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX;
        }

        public static int ResolveOriginalMapIndexY(DieMapEntry entry)
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
                ? ResolveOriginalMapIndexX(entries[0])
                : ResolveOriginalMapIndexY(entries[0]);
        }

        /// <summary>JSON 직렬화 저장 (raw).</summary>
        public static void SaveJson(DieMap map, string path)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("JSON 저장 경로가 비어 있습니다.", nameof(path));
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
            catch (Exception ex)
            {
                throw new IOException("DieMap JSON 저장에 실패했습니다: " + path, ex);
            }
            finally
            {
            }
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
            try
            {
                return LoadWaferMapTextOrThrow(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// RAD TXT를 진단 가능한 예외와 함께 읽는다. Local index와 원본 index를 분리하고,
        /// 장비 Grid는 웨이퍼 중심 (0,0), X 좌-/우+, Y 아래-/위+로 만든다.
        /// </summary>
        public static DieMap LoadWaferMapTextOrThrow(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("WaferMap TXT 경로가 비어 있습니다.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("WaferMap TXT 파일을 찾을 수 없습니다.", path);

            DieMap placeGridMap;
            if (TryLoadPlaceWaferGridText(path, out placeGridMap))
                return placeGridMap;

            WaferMapTextHeader header = ReadWaferMapTextHeader(path);
            var points = new List<ExternalMapPoint>();
            Regex pointRegex = new Regex(@"^X=\s*(?<x>[-+]?\d+)\s+Y=\s*(?<y>[-+]?\d+)\s+B=\s*(?<b>[-+]?\d+)", RegexOptions.Compiled);
            int lineNumber = 0;
            foreach (string line in File.ReadLines(path))
            {
                lineNumber++;
                Match match = pointRegex.Match(line ?? "");
                if (!match.Success)
                    continue;

                int x;
                int y;
                int bin;
                if (!int.TryParse(match.Groups["x"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out x) ||
                    !int.TryParse(match.Groups["y"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out y) ||
                    !int.TryParse(match.Groups["b"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out bin))
                    throw new InvalidDataException("WaferMap 좌표/Bin 숫자 형식이 잘못되었습니다. line=" + lineNumber);

                points.Add(new ExternalMapPoint { X = x, Y = y, Bin = bin });
            }

            if (points.Count == 0)
                throw new InvalidDataException("RAD WaferMap의 'X= ... Y= ... B= ...' Die record를 찾지 못했습니다.");

            IGrouping<string, ExternalMapPoint> duplicate = points
                .GroupBy(point => point.X.ToString(CultureInfo.InvariantCulture) + "," + point.Y.ToString(CultureInfo.InvariantCulture))
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidDataException("RAD WaferMap에 중복 원본 Grid 주소가 있습니다: " + duplicate.Key);
            if (header.DeclaredCount > 0 && header.DeclaredCount != points.Count)
                throw new InvalidDataException(
                    "RAD Header Die count와 실제 record 수가 다릅니다. header=" + header.DeclaredCount +
                    ", records=" + points.Count);

            int minX = points.Min(point => point.X);
            int maxX = points.Max(point => point.X);
            int minY = points.Min(point => point.Y);
            int maxY = points.Max(point => point.Y);
            int gridX = Math.Max(1, maxX - minX + 1);
            int gridY = Math.Max(1, maxY - minY + 1);
            double centerGridX = Math.Max(0, gridX - 1) / 2.0;
            double pitchX = header.PitchX;
            double pitchY = header.PitchY;
            string frameId = Path.GetFileNameWithoutExtension(path);

            var map = new DieMap
            {
                FrameObjId = string.IsNullOrWhiteSpace(frameId) ? "WAFER-TXT" : frameId,
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                // RAD1 Header에는 독립 Die body size가 없다. 실제 크기는 Recipe → 다이 사양을 사용한다.
                DieSizeX = 0.0,
                DieSizeY = 0.0,
                OuterDiameterMm = Math.Max(gridX * pitchX, gridY * pitchY),
                EdgeSkipMode = "ExternalMap",
                OriginX = -centerGridX * pitchX,
                OriginY = CalculateCenteredOriginY(gridY, pitchY),
                SourceFileName = Path.GetFileName(path),
                SourceFormat = "RAD TXT",
                SourcePitchFromFile = header.HasPitch,
                SourceDeclaredCount = header.DeclaredCount,
                SourceFirstX = header.FirstX,
                SourceFirstY = header.FirstY,
                SourceFirstPosX = header.FirstPosX,
                SourceFirstPosY = header.FirstPosY,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            foreach (ExternalMapPoint point in points.OrderByDescending(point => point.Y).ThenBy(point => point.X))
            {
                int localX = point.X - minX;
                int localY = maxY - point.Y;
                double equipmentGridX = localX - centerGridX;
                double equipmentGridY = CalculateEquipmentGridY(localY, gridY);
                bool target = point.Bin > 0;
                map.Entries.Add(new DieMapEntry
                {
                    Index = index++,
                    DieMapX = localX,
                    DieMapY = localY,
                    OriginalMapX = point.X,
                    OriginalMapY = point.Y,
                    IsTarget = target,
                    Result = DieResult.Unknown,
                    BinCode = target ? point.Bin : 0,
                    EquipmentGridX = equipmentGridX,
                    EquipmentGridY = equipmentGridY,
                    PosX = equipmentGridX * pitchX,
                    PosY = equipmentGridY * pitchY,
                    DieUid = BuildExternalMapDieUid(frameId, point.X, point.Y)
                });
            }

            return Normalize(map);
        }

        private static bool TryLoadPlaceWaferGridText(string path, out DieMap map)
        {
            map = null;
            var points = new List<ExternalMapPoint>();
            bool headerFound = false;
            int rowIndex = -1;
            int colIndex = -1;
            int lineNumber = 0;

            foreach (string line in File.ReadLines(path))
            {
                lineNumber++;
                string text = (line ?? "").Trim();
                if (text.Length == 0)
                    continue;

                string[] tokens = text
                    .Replace(",", "\t")
                    .Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                    continue;

                if (!headerFound)
                {
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string token = tokens[i].Trim();
                        if (token.Equals("PLACE_WAFER_ROW", StringComparison.OrdinalIgnoreCase))
                            rowIndex = i;
                        if (token.Equals("PLACE_WAFER_COL", StringComparison.OrdinalIgnoreCase) ||
                            token.Equals("PLACE_WAFER_COLUMN", StringComparison.OrdinalIgnoreCase))
                            colIndex = i;
                    }

                    if (rowIndex >= 0 && colIndex >= 0)
                    {
                        headerFound = true;
                        continue;
                    }

                    return false;
                }

                if (rowIndex >= tokens.Length || colIndex >= tokens.Length)
                    throw new InvalidDataException("PLACE_WAFER_ROW/COL data column count is invalid. line=" + lineNumber);

                int row;
                int col;
                if (!int.TryParse(tokens[rowIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out row) ||
                    !int.TryParse(tokens[colIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out col))
                {
                    throw new InvalidDataException("PLACE_WAFER_ROW/COL value is not numeric. line=" + lineNumber);
                }

                if (row <= 0 || col <= 0)
                    throw new InvalidDataException("PLACE_WAFER_ROW/COL must be 1 or greater. line=" + lineNumber);

                points.Add(new ExternalMapPoint { X = col, Y = row, Bin = 1 });
            }

            if (!headerFound)
                return false;
            if (points.Count == 0)
                throw new InvalidDataException("PLACE_WAFER_ROW/COL record was not found.");

            IGrouping<string, ExternalMapPoint> duplicate = points
                .GroupBy(point => point.X.ToString(CultureInfo.InvariantCulture) + "," + point.Y.ToString(CultureInfo.InvariantCulture))
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidDataException("PLACE_WAFER_ROW/COL has duplicate grid address: " + duplicate.Key);

            int minX = points.Min(point => point.X);
            int maxX = points.Max(point => point.X);
            int minY = points.Min(point => point.Y);
            int maxY = points.Max(point => point.Y);
            int gridX = Math.Max(1, maxX - minX + 1);
            int gridY = Math.Max(1, maxY - minY + 1);
            double centerGridX = Math.Max(0, gridX - 1) / 2.0;
            string frameId = Path.GetFileNameWithoutExtension(path);
            double pitchX = 1.0;
            double pitchY = 1.0;

            map = new DieMap
            {
                FrameObjId = string.IsNullOrWhiteSpace(frameId) ? "PLACE-GRID-TXT" : frameId,
                DieMapX = gridX,
                DieMapY = gridY,
                PitchX = pitchX,
                PitchY = pitchY,
                DieSizeX = 0.0,
                DieSizeY = 0.0,
                OuterDiameterMm = Math.Max(gridX * pitchX, gridY * pitchY),
                EdgeSkipMode = "ExternalMap",
                OriginX = -centerGridX * pitchX,
                OriginY = CalculateCenteredOriginY(gridY, pitchY),
                SourceFileName = Path.GetFileName(path),
                SourceFormat = "PLACE GRID TXT",
                SourcePitchFromFile = false,
                SourceDeclaredCount = points.Count,
                SourceFirstX = points[0].X,
                SourceFirstY = points[0].Y,
                CreatedAt = DateTime.Now
            };

            int index = 0;
            foreach (ExternalMapPoint point in points.OrderByDescending(point => point.Y).ThenBy(point => point.X))
            {
                int localX = point.X - minX;
                int localY = maxY - point.Y;
                double equipmentGridX = localX - centerGridX;
                double equipmentGridY = CalculateEquipmentGridY(localY, gridY);
                map.Entries.Add(new DieMapEntry
                {
                    Index = index++,
                    DieMapX = localX,
                    DieMapY = localY,
                    OriginalMapX = point.X,
                    OriginalMapY = point.Y,
                    IsTarget = true,
                    Result = DieResult.Unknown,
                    BinCode = 1,
                    EquipmentGridX = equipmentGridX,
                    EquipmentGridY = equipmentGridY,
                    PosX = equipmentGridX * pitchX,
                    PosY = equipmentGridY * pitchY,
                    DieUid = BuildExternalMapDieUid(frameId, point.X, point.Y)
                });
            }

            map = Normalize(map);
            return true;
        }

        private static WaferMapTextHeader ReadWaferMapTextHeader(string path)
        {
            var header = new WaferMapTextHeader();
            Regex pitchRegex = new Regex(@"/(?<x>\d{5})/(?<y>\d{5})/", RegexOptions.Compiled);
            Regex countRegex = new Regex(@"/%(?<first>\d+)/&(?<second>\d+)/", RegexOptions.Compiled);
            Regex firstRegex = new Regex(@"FIRST_X=\s*(?<x>[-+]?\d+)\s+FIRST_Y=\s*(?<y>[-+]?\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex positionRegex = new Regex(@"FX=\s*(?<x>[-+]?\d+)\s+FY=\s*(?<y>[-+]?\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (string line in File.ReadLines(path).Take(30))
            {
                string text = line ?? "";
                Match pitch = pitchRegex.Match(text);
                if (!header.HasPitch && pitch.Success)
                {
                    header.PitchX = Math.Max(0.001, double.Parse(pitch.Groups["x"].Value, CultureInfo.InvariantCulture) / 1000.0);
                    header.PitchY = Math.Max(0.001, double.Parse(pitch.Groups["y"].Value, CultureInfo.InvariantCulture) / 1000.0);
                    header.HasPitch = true;
                }

                Match count = countRegex.Match(text);
                if (count.Success)
                {
                    int first = int.Parse(count.Groups["first"].Value, CultureInfo.InvariantCulture);
                    int second = int.Parse(count.Groups["second"].Value, CultureInfo.InvariantCulture);
                    if (first != second)
                        throw new InvalidDataException("RAD Header의 % count와 & count가 다릅니다. %=" + first + ", &=" + second);
                    header.DeclaredCount = first;
                }

                Match firstIndex = firstRegex.Match(text);
                if (firstIndex.Success)
                {
                    header.FirstX = int.Parse(firstIndex.Groups["x"].Value, CultureInfo.InvariantCulture);
                    header.FirstY = int.Parse(firstIndex.Groups["y"].Value, CultureInfo.InvariantCulture);
                }

                Match firstPosition = positionRegex.Match(text);
                if (firstPosition.Success)
                {
                    header.FirstPosX = double.Parse(firstPosition.Groups["x"].Value, CultureInfo.InvariantCulture) / 1000.0;
                    header.FirstPosY = double.Parse(firstPosition.Groups["y"].Value, CultureInfo.InvariantCulture) / 1000.0;
                }
            }

            return header;
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
                var lines = ReadCsvRecords(path).ToArray();
                var map = new DieMap();
                int i = 0;
                // Header section
                for (; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) { i++; break; }
                    var parts = ParseCsvLine(line);
                    if (parts.Length < 2) continue;
                    string k = parts[0].Trim();
                    string v = parts.Length == 2 ? parts[1].Trim() : string.Join(",", parts.Skip(1)).Trim();
                    if (k.Equals("FrameObjId",  StringComparison.OrdinalIgnoreCase)) map.FrameObjId = v;
                    else if (k.Equals("DieMapX",  StringComparison.OrdinalIgnoreCase)) map.DieMapX   = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("DieMapY",  StringComparison.OrdinalIgnoreCase)) map.DieMapY   = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("PitchX", StringComparison.OrdinalIgnoreCase)) map.PitchX  = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("PitchY", StringComparison.OrdinalIgnoreCase)) map.PitchY  = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("DieSizeX", StringComparison.OrdinalIgnoreCase)) map.DieSizeX = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("DieSizeY", StringComparison.OrdinalIgnoreCase)) map.DieSizeY = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OuterDiameterMm", StringComparison.OrdinalIgnoreCase)) map.OuterDiameterMm = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("EdgeSkipMode", StringComparison.OrdinalIgnoreCase)) map.EdgeSkipMode = v;
                    else if (k.Equals("SideEdgeSkip", StringComparison.OrdinalIgnoreCase)) map.SideEdgeSkip = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("TopBottomEdgeSkip", StringComparison.OrdinalIgnoreCase)) map.TopBottomEdgeSkip = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OriginX",StringComparison.OrdinalIgnoreCase)) map.OriginX = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("OriginY",StringComparison.OrdinalIgnoreCase)) map.OriginY = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("SourceFileName", StringComparison.OrdinalIgnoreCase)) map.SourceFileName = v;
                    else if (k.Equals("SourceFormat", StringComparison.OrdinalIgnoreCase)) map.SourceFormat = v;
                    else if (k.Equals("SourcePitchFromFile", StringComparison.OrdinalIgnoreCase)) map.SourcePitchFromFile = bool.Parse(v);
                    else if (k.Equals("SourceDeclaredCount", StringComparison.OrdinalIgnoreCase)) map.SourceDeclaredCount = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("SourceFirstX", StringComparison.OrdinalIgnoreCase)) map.SourceFirstX = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("SourceFirstY", StringComparison.OrdinalIgnoreCase)) map.SourceFirstY = int.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("SourceFirstPosX", StringComparison.OrdinalIgnoreCase)) map.SourceFirstPosX = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("SourceFirstPosY", StringComparison.OrdinalIgnoreCase)) map.SourceFirstPosY = double.Parse(v, CultureInfo.InvariantCulture);
                    else if (k.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) &&
                             DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var createdAt))
                        map.CreatedAt = createdAt;
                }
                // Skip column header
                bool hasSequenceNo = false;
                bool hasOriginalMapAddress = false;
                bool hasEquipmentGrid = false;
                if (i < lines.Length && lines[i].StartsWith("Index", StringComparison.OrdinalIgnoreCase))
                {
                    string header = lines[i];
                    hasSequenceNo = header.IndexOf("SequenceNo", StringComparison.OrdinalIgnoreCase) >= 0;
                    hasOriginalMapAddress =
                        header.IndexOf("OriginalMapX", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        header.IndexOf("OriginalMapY", StringComparison.OrdinalIgnoreCase) >= 0;
                    hasEquipmentGrid =
                        header.IndexOf("EquipmentGridX", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        header.IndexOf("EquipmentGridY", StringComparison.OrdinalIgnoreCase) >= 0;
                    i++;
                }

                // Entries
                while (i < lines.Length)
                {
                    var line = lines[i++].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var p = ParseCsvLine(line);
                    for (int j = 0; j < p.Length; j++)
                        p[j] = p[j] != null ? p[j].Trim() : "";
                    int sequenceOffset = hasSequenceNo ? 1 : 0;
                    int originalAddressOffset = hasOriginalMapAddress ? 2 : 0;
                    int equipmentGridOffset = hasEquipmentGrid ? 2 : 0;
                    int requiredLength = 8 + sequenceOffset + originalAddressOffset + equipmentGridOffset;
                    if (p.Length < requiredLength) continue;
                    int sequenceNo = 0;
                    if (hasSequenceNo)
                    {
                        sequenceNo = int.TryParse(p[1], out var seq) ? seq : 0;
                    }
                    int mapXIndex = 1 + sequenceOffset;
                    int mapYIndex = 2 + sequenceOffset;
                    int originalXIndex = 3 + sequenceOffset;
                    int originalYIndex = 4 + sequenceOffset;
                    int valueOffset = sequenceOffset + originalAddressOffset;
                    int mapX = int.TryParse(p[mapXIndex], out var gx) ? gx : 0;
                    int mapY = int.TryParse(p[mapYIndex], out var gy) ? gy : 0;
                    int originalX = hasOriginalMapAddress && int.TryParse(p[originalXIndex], out var ox) ? ox : mapX;
                    int originalY = hasOriginalMapAddress && int.TryParse(p[originalYIndex], out var oy) ? oy : mapY;
                    var entry = new DieMapEntry
                    {
                        Index    = int.TryParse(p[0], out var idx) ? idx : 0,
                        SequenceNo = sequenceNo,
                        DieMapX = mapX,
                        DieMapY = mapY,
                        OriginalMapX = originalX,
                        OriginalMapY = originalY,
                        IsTarget = bool.TryParse(p[3 + valueOffset], out var isT) ? isT : true,
                        Result   = Enum.TryParse<QMC.CDT320.Materials.DieResult>(p[4 + valueOffset], out var rr) ? rr : QMC.CDT320.Materials.DieResult.Unknown,
                        BinCode  = int.TryParse(p[5 + valueOffset], out var bc) ? bc : 0,
                        PosX     = double.TryParse(p[6 + valueOffset], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 0,
                        PosY     = double.TryParse(p[7 + valueOffset], NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : 0,
                        EquipmentGridX = hasEquipmentGrid && double.TryParse(p[8 + valueOffset], NumberStyles.Any, CultureInfo.InvariantCulture, out var equipmentX)
                            ? equipmentX
                            : double.NaN,
                        EquipmentGridY = hasEquipmentGrid && double.TryParse(p[9 + valueOffset], NumberStyles.Any, CultureInfo.InvariantCulture, out var equipmentY)
                            ? equipmentY
                            : double.NaN,
                        DieUid   = p.Length >= 9 + valueOffset + equipmentGridOffset
                            ? string.Join(",", p.Skip(8 + valueOffset + equipmentGridOffset))
                            : ""
                    };
                    map.Entries.Add(entry);
                }
                return Normalize(map);
            }
            catch { return null; }
        }

        private static string EscapeCsv(string value)
        {
            string text = value ?? "";
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return text;

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var value = new StringBuilder();
            bool quoted = false;
            string text = line ?? "";

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (current == '"')
                {
                    if (quoted && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (current == ',' && !quoted)
                {
                    fields.Add(value.ToString());
                    value.Clear();
                }
                else
                {
                    value.Append(current);
                }
            }

            fields.Add(value.ToString());
            return fields.ToArray();
        }

        private static IEnumerable<string> ReadCsvRecords(string path)
        {
            var records = new List<string>();
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                var record = new StringBuilder();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (record.Length > 0)
                        record.Append(Environment.NewLine);
                    record.Append(line);

                    if (!HasOpenCsvQuote(record))
                    {
                        records.Add(record.ToString());
                        record.Clear();
                    }
                }

                if (record.Length > 0)
                    records.Add(record.ToString());
            }

            return records;
        }

        private static bool HasOpenCsvQuote(StringBuilder record)
        {
            bool quoted = false;
            for (int i = 0; i < record.Length; i++)
            {
                if (record[i] != '"')
                    continue;

                if (quoted && i + 1 < record.Length && record[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                quoted = !quoted;
            }

            return quoted;
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
