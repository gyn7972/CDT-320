using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Recipes
{
    /// <summary>레시피 다이맵 종류. 입력 웨이퍼 맵 1종 + 출력(빈) 맵 GOOD/NG 2종.</summary>
    public enum RecipeMapKind
    {
        Input,
        GoodBin,
        NgBin
    }

    /// <summary>레시피 다이맵(입력 웨이퍼 / GOOD·NG 빈) 파일 경로·ID·파일명 해석 공용 헬퍼.
    /// MapCreatePage(에디터)와 MaterialStateService(런타임)가 공동 사용하여 경로 규칙 중복을 막는다.</summary>
    public static class RecipeMapPaths
    {
        /// <summary>레시피에 저장된 파일명을 절대 경로로 해석. 빈 빈맵 필드는 레거시 OutputDieMapFileName으로 폴백.
        /// 설정값이 없으면 빈 문자열을 반환한다.</summary>
        public static string ResolveConfigured(RecipeProject project, RecipeMapKind kind)
        {
            string configured = ConfiguredFileName(project, kind);
            return ResolveConfiguredPath(configured);
        }

        /// <summary>Recipe-owned Base WaferMap path. Empty means legacy name-based lookup.</summary>
        public static string ResolveBaseConfigured(RecipeProject project)
        {
            return ResolveConfiguredPath(project != null ? project.BaseWaferMapFileName : "");
        }

        /// <summary>Absolute paths are preserved; relative paths are based on the handler executable directory.</summary>
        public static string ResolveConfiguredPath(string configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
                return "";

            if (Path.IsPathRooted(configured))
                return configured;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured);
        }

        /// <summary>해당 종류의 레시피 파일명(상대/절대 원본). GOOD/NG는 비어 있으면 레거시 OutputDieMapFileName 폴백.</summary>
        public static string ConfiguredFileName(RecipeProject project, RecipeMapKind kind)
        {
            if (project == null)
                return "";

            switch (kind)
            {
                case RecipeMapKind.GoodBin:
                    // 현재 기준: 빈 맵 미설정 시 InputDieMap을 1차 폴백으로 공유 사용한다.
                    return !string.IsNullOrWhiteSpace(project.GoodBinDieMapFileName)
                        ? project.GoodBinDieMapFileName
                        : (!string.IsNullOrWhiteSpace(project.OutputDieMapFileName)
                            ? project.OutputDieMapFileName
                            : project.InputDieMapFileName);
                case RecipeMapKind.NgBin:
                    // 현재 기준: 빈 맵 미설정 시 InputDieMap을 1차 폴백으로 공유 사용한다.
                    return !string.IsNullOrWhiteSpace(project.NgBinDieMapFileName)
                        ? project.NgBinDieMapFileName
                        : (!string.IsNullOrWhiteSpace(project.OutputDieMapFileName)
                            ? project.OutputDieMapFileName
                            : project.InputDieMapFileName);
                default:
                    return project.InputDieMapFileName;
            }
        }

        /// <summary>
        /// Returns only the path explicitly owned by the requested role.
        /// Unlike ConfiguredFileName, this never falls back to another role.
        /// </summary>
        public static string ExactConfiguredFileName(RecipeProject project, RecipeMapKind kind)
        {
            if (project == null)
                return "";

            switch (kind)
            {
                case RecipeMapKind.GoodBin:
                    return project.GoodBinDieMapFileName ?? "";
                case RecipeMapKind.NgBin:
                    return project.NgBinDieMapFileName ?? "";
                default:
                    return project.InputDieMapFileName ?? "";
            }
        }

        /// <summary>레시피명 기반 기본 저장 경로 (예: &lt;recipe&gt;_GoodBinDieMap.json).</summary>
        public static string BuildDefaultPath(RecipeProject project, RecipeMapKind kind)
        {
            string recipeName = SanitizeFileName(project != null ? project.FileName : "Recipe");
            return Path.Combine(GetDieMapDirectory(), recipeName + "_" + FileSuffix(kind) + ".txt");
        }

        /// <summary>이름 기반 라이브러리 저장 경로. 접미사가 없으면 종류 접미사를 붙인다.</summary>
        public static string BuildPathByName(string name, RecipeMapKind kind)
        {
            string safeName = SanitizeFileName(name);
            string suffix = FileSuffix(kind);
            if (!safeName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                safeName += "_" + suffix;
            return Path.Combine(GetDieMapDirectory(), safeName + ".txt");
        }

        /// <summary>레시피명 기반 기본 맵 이름 (확장자 없음).</summary>
        public static string BuildDefaultName(RecipeProject project, RecipeMapKind kind)
        {
            string recipeName = SanitizeFileName(project != null ? project.FileName : "Recipe");
            return recipeName + "_" + FileSuffix(kind);
        }

        /// <summary>Config\DieMaps 디렉터리(없으면 생성).</summary>
        public static string GetDieMapDirectory()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "DieMaps");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Project-owned map directory used by cloned recipes.</summary>
        public static string GetProjectMapDirectory(string recipeName)
        {
            return Path.Combine(QMC.Common.Data.Store.RecipeDataStore.DirOf(recipeName), "Maps");
        }

        /// <summary>Builds the path persisted in a Project file for a project-owned map asset.</summary>
        public static string BuildProjectMapRelativePath(string recipeName, string fileName)
        {
            string safeFileName = SanitizeFileName(fileName);
            return MakeConfigRelativePath(Path.Combine(GetProjectMapDirectory(recipeName), safeFileName));
        }

        /// <summary>파일명 접미사: InputDieMap / GoodBinDieMap / NgBinDieMap.</summary>
        public static string FileSuffix(RecipeMapKind kind)
        {
            switch (kind)
            {
                case RecipeMapKind.GoodBin: return "GoodBinDieMap";
                case RecipeMapKind.NgBin: return "NgBinDieMap";
                default: return "InputDieMap";
            }
        }

        /// <summary>FrameObjId: &lt;recipe&gt;-INPUT-MAP / &lt;recipe&gt;-GOOD-BIN-MAP / &lt;recipe&gt;-NG-BIN-MAP.</summary>
        public static string BuildMapId(RecipeProject project, RecipeMapKind kind)
        {
            string recipeName = SanitizeFileName(project != null ? project.FileName : "Recipe");
            return recipeName + "-" + IdToken(kind) + "-MAP";
        }

        /// <summary>저장된 파일명을 해당 종류 필드에 기록(상대 경로 권장).</summary>
        public static void SetConfiguredFileName(RecipeProject project, RecipeMapKind kind, string relativePath)
        {
            if (project == null)
                return;

            switch (kind)
            {
                case RecipeMapKind.GoodBin:
                    project.GoodBinDieMapFileName = relativePath;
                    break;
                case RecipeMapKind.NgBin:
                    project.NgBinDieMapFileName = relativePath;
                    break;
                default:
                    project.InputDieMapFileName = relativePath;
                    break;
            }
        }

        /// <summary>새 Base/Pitch/Die로 역할 맵이 바뀌었음을 표시하여 Map Create FINAL APPLY를 다시 요구한다.</summary>
        public static void InvalidateApproval(RecipeProject project, bool input, bool output)
        {
            if (project == null)
                return;

            project.MapApprovalVersion = 1;
            if (input)
                project.InputMapApprovalHash = "";
            if (output)
            {
                project.GoodBinMapApprovalHash = "";
                project.NgBinMapApprovalHash = "";
            }
        }

        public static void ApproveMap(RecipeProject project, RecipeMapKind kind, DieMap map)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            project.MapApprovalVersion = 1;
            SetApprovalHash(project, kind, ComputeApprovalHash(map));
        }

        /// <summary>Version 0은 기존 Recipe 호환, Version 1부터는 저장된 FINAL APPLY hash를 검사한다.</summary>
        public static bool IsMapApproved(RecipeProject project, RecipeMapKind kind, DieMap map, out string reason)
        {
            reason = "";
            if (project == null)
            {
                reason = "Recipe Project가 없습니다.";
                return false;
            }

            if (project.MapApprovalVersion <= 0)
                return true;

            string approvedHash = GetApprovalHash(project, kind);
            if (string.IsNullOrWhiteSpace(approvedHash))
            {
                reason = kind + " 맵이 아직 Map Create에서 FINAL APPLY 되지 않았습니다.";
                return false;
            }
            if (map == null)
            {
                reason = kind + " 맵을 읽을 수 없어 승인 hash를 확인하지 못했습니다.";
                return false;
            }

            string currentHash = ComputeApprovalHash(map);
            if (!string.Equals(approvedHash, currentHash, StringComparison.OrdinalIgnoreCase))
            {
                reason = kind + " 맵이 FINAL APPLY 이후 변경되었습니다. Map Create에서 다시 확인하고 적용하세요.";
                return false;
            }
            return true;
        }

        public static string ComputeApprovalHash(DieMap map)
        {
            if (map == null)
                return "";

            DieMapGenerator.Normalize(map);
            var text = new StringBuilder();
            text.Append(map.FrameObjId ?? "").Append('|')
                .Append(map.EdgeSkipMode ?? "").Append('|')
                .Append(map.DieMapX).Append('|').Append(map.DieMapY).Append('|')
                .Append(FormatDouble(map.PitchX)).Append('|').Append(FormatDouble(map.PitchY)).Append('|')
                .Append(FormatDouble(map.DieSizeX)).Append('|').Append(FormatDouble(map.DieSizeY)).Append('|')
                .Append(FormatDouble(map.OriginX)).Append('|').Append(FormatDouble(map.OriginY)).AppendLine();

            foreach (DieMapEntry entry in (map.Entries ?? new System.Collections.Generic.List<DieMapEntry>())
                .Where(item => item != null)
                .OrderBy(item => item.OriginalMapY >= 0 ? item.OriginalMapY : item.DieMapY)
                .ThenBy(item => item.OriginalMapX >= 0 ? item.OriginalMapX : item.DieMapX))
            {
                text.Append(entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX).Append(',')
                    .Append(entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY).Append(',')
                    .Append(entry.DieMapX).Append(',').Append(entry.DieMapY).Append(',')
                    .Append(FormatDouble(entry.EquipmentGridX)).Append(',')
                    .Append(FormatDouble(entry.EquipmentGridY)).Append(',')
                    .Append(FormatDouble(entry.PosX)).Append(',').Append(FormatDouble(entry.PosY)).Append(',')
                    .Append(entry.IsTarget ? '1' : '0').Append(',')
                    .Append(entry.BinCode).AppendLine();
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                    hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }

        private static string GetApprovalHash(RecipeProject project, RecipeMapKind kind)
        {
            switch (kind)
            {
                case RecipeMapKind.GoodBin: return project.GoodBinMapApprovalHash ?? "";
                case RecipeMapKind.NgBin: return project.NgBinMapApprovalHash ?? "";
                default: return project.InputMapApprovalHash ?? "";
            }
        }

        private static void SetApprovalHash(RecipeProject project, RecipeMapKind kind, string hash)
        {
            switch (kind)
            {
                case RecipeMapKind.GoodBin:
                    project.GoodBinMapApprovalHash = hash ?? "";
                    break;
                case RecipeMapKind.NgBin:
                    project.NgBinMapApprovalHash = hash ?? "";
                    break;
                default:
                    project.InputMapApprovalHash = hash ?? "";
                    break;
            }
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>실행 기준 경로 이하면 상대 경로로, 아니면 원본 그대로.</summary>
        public static string MakeConfigRelativePath(string path)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!string.IsNullOrWhiteSpace(path) &&
                path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(baseDir.Length);
            }

            return path ?? "";
        }

        /// <summary>파일명 부적합 문자 치환.</summary>
        public static string SanitizeFileName(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "Recipe" : value.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '_');
            return text;
        }

        private static string IdToken(RecipeMapKind kind)
        {
            switch (kind)
            {
                case RecipeMapKind.GoodBin: return "GOOD-BIN";
                case RecipeMapKind.NgBin: return "NG-BIN";
                default: return "INPUT";
            }
        }
    }
}
