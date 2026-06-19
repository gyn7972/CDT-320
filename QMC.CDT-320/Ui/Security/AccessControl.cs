using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Security
{
    /// <summary>
    /// 컨트롤 Tag 에 "level:<see cref="UserLevel"/>" 형식을 지정하면,
    /// <see cref="Apply"/> 호출 시 현재 세션 레벨이 부족한 컨트롤을 Enabled = false 로 설정한다.
    /// <para>
    /// 사용 예: <c>btnCycleRun.Tag = "level:Operator";</c>
    /// Tag 가 이미 다른 용도로 쓰이면 <c>"level:Engineer;i18n:key"</c> 처럼 세미콜론 구분 가능.
    /// </para>
    /// </summary>
    public static class AccessControl
    {
        public static void Apply(Control root)
        {
            if (root == null) return;
            foreach (Control c in Enumerate(root))
            {
                string tag = c.Tag as string;
                if (string.IsNullOrEmpty(tag)) continue;

                // 1순위: i18n 키를 권한키로 사용해 매트릭스(AccessPolicy) 조회.
                string permKey = ExtractI18n(tag);
                if (permKey != null && AccessPolicy.IsKnown(permKey))
                {
                    c.Enabled = AccessPolicy.Can(permKey);
                    continue;
                }

                // 2순위(폴백): "level:Xxx" 누적 기준.
                UserLevel? req = ExtractLevel(tag);
                if (req == null) continue;
                c.Enabled = UserSession.Has(req.Value);
            }
        }

        private static string ExtractI18n(string tag)
        {
            foreach (var part in tag.Split(';'))
            {
                var p = part.Trim();
                if (p.StartsWith("i18n:")) return p.Substring(5);
            }
            return null;
        }

        private static UserLevel? ExtractLevel(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            foreach (var part in tag.Split(';'))
            {
                var p = part.Trim();
                if (!p.StartsWith("level:")) continue;
                var name = p.Substring(6);
                if (Enum.TryParse<UserLevel>(name, ignoreCase: true, out var lv)) return lv;
            }
            return null;
        }

        private static IEnumerable<Control> Enumerate(Control root)
        {
            yield return root;
            foreach (Control c in root.Controls)
                foreach (var ch in Enumerate(c))
                    yield return ch;
        }
    }
}
