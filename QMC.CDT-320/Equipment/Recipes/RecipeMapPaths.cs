using System;
using System.IO;

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
                    return !string.IsNullOrWhiteSpace(project.GoodBinDieMapFileName)
                        ? project.GoodBinDieMapFileName
                        : project.OutputDieMapFileName;
                case RecipeMapKind.NgBin:
                    return !string.IsNullOrWhiteSpace(project.NgBinDieMapFileName)
                        ? project.NgBinDieMapFileName
                        : project.OutputDieMapFileName;
                default:
                    return project.InputDieMapFileName;
            }
        }

        /// <summary>레시피명 기반 기본 저장 경로 (예: &lt;recipe&gt;_GoodBinDieMap.json).</summary>
        public static string BuildDefaultPath(RecipeProject project, RecipeMapKind kind)
        {
            string recipeName = SanitizeFileName(project != null ? project.FileName : "Recipe");
            return Path.Combine(GetDieMapDirectory(), recipeName + "_" + FileSuffix(kind) + ".json");
        }

        /// <summary>이름 기반 라이브러리 저장 경로. 접미사가 없으면 종류 접미사를 붙인다.</summary>
        public static string BuildPathByName(string name, RecipeMapKind kind)
        {
            string safeName = SanitizeFileName(name);
            string suffix = FileSuffix(kind);
            if (!safeName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                safeName += "_" + suffix;
            return Path.Combine(GetDieMapDirectory(), safeName + ".json");
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
