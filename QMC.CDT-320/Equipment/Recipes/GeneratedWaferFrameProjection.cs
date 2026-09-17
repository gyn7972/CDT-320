using System;
using System.IO;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Recipes
{
    /// <summary>저장된 생성 맵과 기존 장비 사양의 사용 가능 여부를 MaterialSpecs 저장 전에 확인한다.</summary>
    public static class GeneratedWaferFrameProjection
    {
        public static TapeFrameSubset Resolve(RecipeProject project, TapeFrameSubset frame, string mapFileName)
        {
            if (project == null || frame == null)
                return frame;

            // 분리 이전의 별도 이름 Frame 사양은 Input 생성 사양의 별칭이 아니다.
            if (string.IsNullOrWhiteSpace(mapFileName) && ReferenceEquals(frame, project.Frame) &&
                project.InputFrame != null && !ReferenceEquals(frame, project.InputFrame) &&
                !string.Equals((frame.FrameSpecName ?? "").Trim(), (project.InputFrame.FrameSpecName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return frame;

            RecipeMapKind kind = ResolveKind(project, frame, mapFileName);
            string basePath = RecipeMapPaths.ResolveBaseConfigured(project, kind);
            string rolePath = !string.IsNullOrWhiteSpace(mapFileName)
                ? RecipeMapPaths.ResolveConfiguredPath(mapFileName)
                : RecipeMapPaths.ResolveConfiguredPath(RecipeMapPaths.ExactConfiguredFileName(project, kind));
            // Base는 FINAL APPLY 전에도 저장된 생성 조건을 갖는다. 승인 여부는 기존 운전 진입 검증이 담당한다.
            DieMap map = LoadAvailableMap(basePath) ?? LoadAvailableMap(rolePath);
            if (map == null || (map.Generation == null &&
                !string.Equals(map.SourceFormat, GeneratedWaferMapCodec.SourceFormat, StringComparison.Ordinal)))
                return frame;

            string reason;
            if (!GeneratedWaferMapCodec.Validate(map, out reason) ||
                !RecipeDieMapResolver.IsCompatibleWithFrame(map, frame, out reason))
                throw new InvalidDataException("생성 맵의 물리 사양을 읽을 수 없습니다. 맵 생성 미리보기에서 다시 저장하십시오. " + reason);
            // 90/270도는 미리보기·초안 저장만 허용한다. 현재 장비의 정렬/검사 계약을
            // 바꾸지 않도록 MaterialSpecs 기록 전에 장비 사용 가능 각도를 확인한다.
            if (!RecipeDieMapResolver.IsSupportedForEquipment(map, out reason))
                throw new InvalidDataException(reason);
            return frame;
        }

        private static RecipeMapKind ResolveKind(RecipeProject project, TapeFrameSubset frame, string mapFileName)
        {
            if (!string.IsNullOrWhiteSpace(mapFileName))
            {
                string explicitPath = RecipeMapPaths.ResolveConfiguredPath(mapFileName);
                foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
                {
                    string configured = RecipeMapPaths.ExactConfiguredFileName(project, kind);
                    if (!string.IsNullOrWhiteSpace(configured) && SamePath(explicitPath, RecipeMapPaths.ResolveConfiguredPath(configured)))
                        return kind;
                }
            }
            return ReferenceEquals(frame, project.OutputFrame) ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
        }

        private static DieMap LoadAvailableMap(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            bool exists = File.Exists(path);
            DieMap map = exists ? DieMapGenerator.Load(path) : null;
            if (map != null)
                return map;
            if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            {
                string csvPath = Path.ChangeExtension(path, ".csv");
                if (File.Exists(csvPath))
                {
                    exists = true;
                    map = DieMapGenerator.LoadCsv(csvPath);
                    if (map != null)
                        return map;
                }
            }
            if (exists)
                throw new InvalidDataException("웨이퍼 사양의 맵 파일을 읽을 수 없습니다. 맵 생성 미리보기 또는 LOAD WAFER MAP으로 다시 저장하십시오. 파일=" + path);
            return null;
        }

        private static bool SamePath(string left, string right)
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
    }
}
