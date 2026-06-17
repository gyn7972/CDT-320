using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace QMC.Vision.Tools
{
    /// <summary>
    /// QMC.Vision UI 트리 전수 감사 — 형제 컨트롤끼리 겹침/부모초과/0크기 탐지.
    /// <para>
    /// 사용: <c>QMC.Vision.exe --ui-audit</c>
    /// 출력: 콘솔 + <c>docs\UI_OVERLAP_AUDIT.md</c>
    /// 종료 코드: 0 = ERROR 0건, 1 = ERROR 발견.
    /// </para>
    /// 헤드리스 (Form 은 Show 하지 않고 핸들 생성 + PerformLayout 만 수행).
    /// </summary>
    public static class UiOverlapAuditor
    {
        public sealed class Finding
        {
            public string Severity;     // ERROR / WARN
            public string Category;     // off-parent / overlap / zero-size / negative-coord
            public string PageContext;  // "Operation" 등
            public string ParentDesc;
            public string A;
            public string B;
            public string Detail;

            public override string ToString()
                => $"[{Severity}] [{Category}] {PageContext} | {ParentDesc} | {A}" +
                   (B != null ? "  ⟷  " + B : "") + (string.IsNullOrEmpty(Detail) ? "" : "  " + Detail);
        }

        public static int Run()
        {
            var findings = new List<Finding>();
            Form1 form = null;
            try
            {
                form = new Form1();
                form.WindowState  = FormWindowState.Normal;
                form.StartPosition = FormStartPosition.Manual;
                form.Location     = new Point(0, 0);
                form.Size         = new Size(2560, 1440);   // dev PC 실 해상도
                _ = form.Handle;                              // 핸들 강제 생성 → Load 이벤트 발생
                form.Show();
                Application.DoEvents();
                form.PerformLayout();
                Application.DoEvents();

                // 탭 enum 순회
                var tabType   = typeof(Form1).Assembly.GetType("QMC.Vision.Tab");
                var showTabMi = typeof(Form1).GetMethod("ShowTab", BindingFlags.Instance | BindingFlags.NonPublic);
                if (tabType != null && showTabMi != null)
                {
                    foreach (var tabName in Enum.GetNames(tabType))
                    {
                        object tabVal = Enum.Parse(tabType, tabName);
                        showTabMi.Invoke(form, new[] { tabVal });
                        form.PerformLayout();
                        Application.DoEvents();
                        Audit(form, tabName, findings);
                    }
                }
                else
                {
                    Audit(form, "Form1", findings);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FATAL: " + ex);
                return 2;
            }
            finally
            {
                try { form?.Close(); form?.Dispose(); } catch { }
            }

            int errorCount = findings.Count(f => f.Severity == "ERROR");
            int warnCount  = findings.Count(f => f.Severity == "WARN");

            // 콘솔
            Console.Out.WriteLine($"AUDIT: errors={errorCount} warns={warnCount} total={findings.Count}");
            foreach (var f in findings)
                Console.Out.WriteLine(f.ToString());

            // Markdown 파일
            try
            {
                string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\docs"));
                if (!Directory.Exists(root))
                    root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                Directory.CreateDirectory(root);
                string mdPath = Path.Combine(root, "UI_OVERLAP_AUDIT.md");
                File.WriteAllText(mdPath, BuildMarkdown(findings, errorCount, warnCount), Encoding.UTF8);
                Console.Out.WriteLine("MD: " + mdPath);
            }
            catch (Exception ex) { Console.Error.WriteLine("MD write fail: " + ex.Message); }

            return errorCount > 0 ? 1 : 0;
        }

        // ──────────────────────────────────────────
        //  핵심 감사 — 컨트롤 트리 재귀 walk + 형제 비교
        // ──────────────────────────────────────────

        private static void Audit(Control root, string pageContext, List<Finding> findings)
        {
            if (root == null) return;
            var stack = new Stack<Control>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (c == null || !c.Visible) continue;
                CheckChildrenAt(c, pageContext, findings);
                foreach (Control ch in c.Controls) stack.Push(ch);
            }
        }

        private static readonly Type[] _interactive = new[]
        {
            typeof(Button), typeof(ComboBox), typeof(TextBox), typeof(NumericUpDown),
            typeof(CheckBox), typeof(RadioButton), typeof(ListBox), typeof(TreeView),
            typeof(PictureBox)
        };

        private static bool IsInteractive(Control c)
        {
            var t = c.GetType();
            if (_interactive.Any(it => it.IsAssignableFrom(t))) return true;
            // BottomMenuButton 등 Control 직속 커스텀 버튼
            if (c is Control && t.Name.EndsWith("Button", StringComparison.Ordinal)) return true;
            return false;
        }

        private static void CheckChildrenAt(Control parent, string pageContext, List<Finding> findings)
        {
            var visibleChildren = new List<Control>();
            foreach (Control ch in parent.Controls) if (ch.Visible) visibleChildren.Add(ch);
            if (visibleChildren.Count == 0) return;

            var clientR = parent.ClientRectangle;
            string parentDesc = $"{parent.GetType().Name}({(string.IsNullOrEmpty(parent.Name) ? "_" : parent.Name)})  Client={Fmt(clientR)}";

            foreach (var ch in visibleChildren)
            {
                // 1) 0 크기 / 음수 좌표
                if (ch.Width <= 0 || ch.Height <= 0)
                {
                    findings.Add(new Finding {
                        Severity = "WARN", Category = "zero-size",
                        PageContext = pageContext, ParentDesc = parentDesc,
                        A = Desc(ch), Detail = $"size W={ch.Width} H={ch.Height}"
                    });
                }
                if ((ch.Left < 0 || ch.Top < 0) && !(parent is Form))
                {
                    findings.Add(new Finding {
                        Severity = "WARN", Category = "negative-coord",
                        PageContext = pageContext, ParentDesc = parentDesc,
                        A = Desc(ch), Detail = $"Left={ch.Left} Top={ch.Top}"
                    });
                }

                // 2) 부모 영역 초과
                var b = ch.Bounds;
                if (b.Right > clientR.Right + 1 || b.Bottom > clientR.Bottom + 1 ||
                    b.Left < clientR.Left - 1 || b.Top < clientR.Top - 1)
                {
                    // Dock 컨트롤은 부모와 동일 영역이어야 하므로 약간의 +/- 1 허용
                    if (ch.Dock == DockStyle.None)
                    {
                        findings.Add(new Finding {
                            Severity = IsInteractive(ch) ? "ERROR" : "WARN",
                            Category = "off-parent",
                            PageContext = pageContext, ParentDesc = parentDesc,
                            A = Desc(ch),
                            Detail = "child outside parent ClientRect"
                        });
                    }
                }
            }

            // 3) 형제 상호 겹침 — N^2
            for (int i = 0; i < visibleChildren.Count; i++)
            {
                var a = visibleChildren[i];
                if (a.Dock != DockStyle.None) continue; // Dock 자식들 사이 겹침은 도킹 순서 문제일 뿐 — 시각상 노 issue
                for (int j = i + 1; j < visibleChildren.Count; j++)
                {
                    var b = visibleChildren[j];
                    if (b.Dock != DockStyle.None) continue;
                    if (a.Bounds.IntersectsWith(b.Bounds))
                    {
                        bool eitherInteractive = IsInteractive(a) || IsInteractive(b);
                        findings.Add(new Finding {
                            Severity = eitherInteractive ? "ERROR" : "WARN",
                            Category = "overlap",
                            PageContext = pageContext, ParentDesc = parentDesc,
                            A = Desc(a), B = Desc(b),
                            Detail = $"intersect={Fmt(Rectangle.Intersect(a.Bounds, b.Bounds))}"
                        });
                    }
                }
            }
        }

        private static string Desc(Control c)
        {
            string txt = string.IsNullOrEmpty(c.Text) ? "" : ($" text=\"{c.Text.Replace("\n", " ")}\"");
            return $"{c.GetType().Name}({(string.IsNullOrEmpty(c.Name) ? "_" : c.Name)}){txt} Bounds={Fmt(c.Bounds)} Dock={c.Dock}";
        }

        private static string Fmt(Rectangle r)
            => $"({r.X},{r.Y},{r.Width}x{r.Height})";

        private static string BuildMarkdown(List<Finding> all, int errs, int warns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# QMC.Vision UI Overlap Audit");
            sb.AppendLine();
            sb.AppendLine($"- 실행 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- ERROR: **{errs}**  WARN: **{warns}**  TOTAL: {all.Count}");
            sb.AppendLine();
            var groups = all.GroupBy(f => f.PageContext).OrderBy(g => g.Key);
            foreach (var g in groups)
            {
                sb.AppendLine($"## {g.Key} (errors={g.Count(f => f.Severity == "ERROR")}, warns={g.Count(f => f.Severity == "WARN")})");
                sb.AppendLine();
                foreach (var f in g.OrderByDescending(x => x.Severity))
                {
                    sb.AppendLine($"- **[{f.Severity}] [{f.Category}]** parent: `{f.ParentDesc}`");
                    sb.AppendLine($"  - A: `{f.A}`");
                    if (f.B != null) sb.AppendLine($"  - B: `{f.B}`");
                    if (!string.IsNullOrEmpty(f.Detail)) sb.AppendLine($"  - detail: {f.Detail}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
