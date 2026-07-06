using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace QMC.Vision.Cameras.Mil
{
    /// <summary>
    /// Matrox DCF 의 Camera Configuration(Manual Feature Configuration) 항목 읽기/쓰기.
    /// DCF 는 Intellicam 이 만드는 파일이지만 카메라 feature 항목은 평문 텍스트
    /// (예: FLOAT_ExposureTime  "5000" / ENUM_PixelFormat  "Mono8")라 값 치환이 가능하다.
    /// <para>안전 규칙: 존재하는 키의 "값"만 치환(줄 구조/패딩/그 외 내용 불변), 저장 전 타임스탬프 백업.
    /// 새 feature 항목 추가는 하지 않는다 — 전체 상태 덤프는 Intellicam [Dump State to DCF] 사용.</para>
    /// </summary>
    public static class MilDcfFile
    {
        // 예: ENUM_PixelFormat        "Mono8"   /  FLOAT_AcquisitionFrameRate  "15"
        private static readonly Regex FeatureLine = new Regex(
            "^(?<head>\\s*(?:ENUM|INT|FLOAT|BOOL|STRING)_(?<name>[A-Za-z0-9_]+)\\s+)\"(?<value>[^\"]*)\"",
            RegexOptions.Compiled);

        /// <summary>DCF 에서 카메라 feature 항목(이름→값)을 읽는다. 실패/없음 시 빈 dict.</summary>
        public static Dictionary<string, string> ReadFeatures(string path)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return d;
                foreach (var line in File.ReadAllLines(path))
                {
                    var m = FeatureLine.Match(line);
                    if (m.Success) d[m.Groups["name"].Value] = m.Groups["value"].Value;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MilDcfFile] ReadFeatures 실패: " + ex.Message); }
            return d;
        }

        /// <summary>DCF 의 '기존' feature 값만 치환 저장 — 저장 전 같은 폴더에 백업 생성.
        /// 반환 = 실제 치환된 항목 수(0 = 변경 없음/해당 키 없음).</summary>
        public static int WriteFeatures(string path, IDictionary<string, string> values, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) { error = "DCF 파일이 없습니다: " + path; return 0; }
                var lines = File.ReadAllLines(path);
                int changed = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var m = FeatureLine.Match(lines[i]);
                    if (!m.Success) continue;
                    if (!values.TryGetValue(m.Groups["name"].Value, out var nv) || nv == null) continue;
                    if (m.Groups["value"].Value == nv) continue;
                    lines[i] = m.Groups["head"].Value + "\"" + nv + "\"" + lines[i].Substring(m.Length);
                    changed++;
                }
                if (changed == 0) return 0;
                string bak = Path.Combine(Path.GetDirectoryName(path) ?? "",
                    Path.GetFileNameWithoutExtension(path) + "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dcf");
                File.Copy(path, bak, true);
                File.WriteAllLines(path, lines);
                return changed;
            }
            catch (Exception ex) { error = ex.Message; return 0; }
        }
    }
}
