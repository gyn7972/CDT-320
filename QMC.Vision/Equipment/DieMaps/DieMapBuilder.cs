using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.Vision.DieMaps
{
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

        /// <summary>
        /// 격자 내접 원형 다이맵 생성(핸들러 MapCreatePage.CreateCircleDieMapFromRecipe/IsInsideWaferCircle 이식).
        /// Grid 개수 + Pitch + (보조)직경 + Edge skip 기준. origin = -((grid-1)*pitch)/2.
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
